using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SkyFloe.Aws
{
   public class GlacierDownloader
   {
      private Amazon.Glacier.AmazonGlacierClient glacier;
      private String vault;
      private Double maxRetrievalRate;
      private List<Restore.Retrieval> retrievals;
      private IDictionary<Int32, GlacierStream> jobMap;
      private DateTime started;
      private Int64 retrieved;
      
      public Double RetrievalRate
      {
         get
         {
            return (Double)this.retrieved / 
                      (DateTime.UtcNow - this.started).TotalSeconds;
         }
      }

      public GlacierDownloader (
         Amazon.Glacier.AmazonGlacierClient glacier,
         String vault,
         Double maxRetrievalRate,
         List<Restore.Retrieval> retrievals)
      {
         this.glacier = glacier;
         this.vault = vault;
         this.maxRetrievalRate = maxRetrievalRate;
         this.retrievals = retrievals;
         this.jobMap = retrievals.ToDictionary(r => r.ID, r => (GlacierStream)null);
         this.started = DateTime.UtcNow;
         this.retrieved = 0;
      }

      public Stream Download (Restore.Retrieval retrieval, Int64 offset, Int64 length)
      {
         // TODO: remove
#if false
         if (this.jobMap[retrieval.ID] == null)
            this.jobMap[retrieval.ID] = new JobStream(
               this.glacier,
               this.vault,
               "VCB_2CFxiWAw1urgdDeRkA8rS5ROiosJ_rO_sPRhR4O-6ZcKVuPPw-yCgi9iUbYNxFmnTU4o2RN6NAiG_tbTrmpBhRJq",
               retrieval.Length
            );
#endif
         // dispose any retrieval jobs that we have already processed,
         // based on the scheduled retrieval order
         List<Restore.Retrieval> processed = this.retrievals
            .TakeWhile(r => r.ID != retrieval.ID)
            .ToList();
         foreach (Restore.Retrieval prev in processed)
         {
            GlacierStream stream = this.jobMap[prev.ID];
            if (stream != null)
               stream.Dispose();
            this.jobMap.Remove(prev.ID);
            this.retrievals.Remove(prev);
         }
         try
         {
            for (; ; )
            {
               foreach (Restore.Retrieval next in this.retrievals.SkipWhile(r => r.ID != retrieval.ID))
               {
                  if (next.ID != retrieval.ID && this.RetrievalRate > this.maxRetrievalRate)
                     break;
                  if (this.jobMap[next.ID] == null)
                     StartRetrieval(next);
               }
               Amazon.Glacier.Model.DescribeJobResult job = this.glacier.DescribeJob(
                  new Amazon.Glacier.Model.DescribeJobRequest()
                  {
                     VaultName = this.vault,
                     JobId = this.jobMap[retrieval.ID].JobID
                  }
               ).DescribeJobResult;
               if (job.Completed)
                  break;
               System.Threading.Thread.Sleep((Int32)TimeSpan.FromMinutes(20).TotalMilliseconds);
            }
            return new IO.SubStream(
               this.jobMap[retrieval.ID],
               offset,
               length
            );
         }
         catch
         {
            this.jobMap[retrieval.ID] = null;
            throw;
         }
      }

      private void StartRetrieval (Restore.Retrieval retrieval)
      {
         String jobID = this.glacier.InitiateJob(
            new Amazon.Glacier.Model.InitiateJobRequest()
            {
               VaultName = this.vault,
               JobParameters = new Amazon.Glacier.Model.JobParameters()
               {
                  Type = "archive-retrieval",
                  ArchiveId = retrieval.Name,
                  RetrievalByteRange = String.Format(
                     "{0}-{1}", 
                     retrieval.Offset, 
                     retrieval.Offset + retrieval.Length - 1
                  )
               }
            }
         ).InitiateJobResult.JobId;
         this.jobMap[retrieval.ID] = new GlacierStream(
            this.glacier,
            this.vault,
            jobID,
            retrieval.Length
         );
         this.retrieved += retrieval.Length;
      }
   }
}
