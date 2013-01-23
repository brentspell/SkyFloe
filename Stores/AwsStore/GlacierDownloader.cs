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
      private Dictionary<String, GlacierStream> jobStreams;
      
      public GlacierDownloader (
         Amazon.Glacier.AmazonGlacierClient glacier,
         String vault)
      {
         this.glacier = glacier;
         this.vault = vault;
         this.jobStreams = new Dictionary<String, GlacierStream>();
      }

      public String StartJob (String archiveID, Int64 offset, Int64 length)
      {
         return this.glacier.InitiateJob(
            new Amazon.Glacier.Model.InitiateJobRequest()
            {
               VaultName = this.vault,
               JobParameters = new Amazon.Glacier.Model.JobParameters()
               {
                  Type = "archive-retrieval",
                  ArchiveId = archiveID,
                  RetrievalByteRange = String.Format(
                     "{0}-{1}",
                     offset,
                     offset + length - 1
                  )
               }
            }
         ).InitiateJobResult.JobId;
      }

      public Boolean QueryJob (String jobID)
      {
         if (this.jobStreams.ContainsKey(jobID))
            return true;
         Amazon.Glacier.Model.DescribeJobResult jobInfo = this.glacier.DescribeJob(
            new Amazon.Glacier.Model.DescribeJobRequest()
            {
               VaultName = this.vault,
               JobId = jobID
            }
         ).DescribeJobResult;
         if (jobInfo.Completed)
         {
            String range = jobInfo.RetrievalByteRange;
            String start = range.Substring(0, range.IndexOf('-'));
            String stop = range.Substring(range.IndexOf('-') + 1);
            Int64 length = Convert.ToInt64(stop) - Convert.ToInt64(start) + 1;
            this.jobStreams.Add(
               jobID, 
               new GlacierStream(this.glacier, this.vault, jobID, length)
            );
            return true;
         }
         return false;
      }

      public void DeleteJob (String jobID)
      {
         GlacierStream jobStream = null;
         if (this.jobStreams.TryGetValue(jobID, out jobStream))
         {
            this.jobStreams.Remove(jobID);
            jobStream.Dispose();
         }
      }

      public Stream GetJobStream (String jobID, Int64 offset, Int64 length)
      {
         GlacierStream stream = null;
         if (!this.jobStreams.TryGetValue(jobID, out stream))
            throw new InvalidOperationException("TODO: stream not found");
         try
         {
            return new IO.SubStream(stream, offset, length);
         }
         catch
         {
            this.jobStreams.Remove(jobID);
            stream.Dispose();
            throw;
         }
      }
   }
}
