using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using SkyFloe.Store;

namespace SkyFloe.Aws
{
   public class GlacierRestore : IRestore
   {
      public const Int32 MinRetrievalSize = 1024 * 1024;
      public const Int32 PartSize = 16 * 1024 * 1024;
      private IRestoreIndex restoreIndex;
      private Amazon.Glacier.AmazonGlacierClient glacier;
      private String vault;
      private GlacierDownloader downloader;
      private DateTime restoreStarted;
      private Int64 restoreRetrieving;
      private Double maxRetrievalRate;

      public GlacierRestore (
         IBackupIndex backupIndex,
         IRestoreIndex restoreIndex,
         Restore.Session session,
         Amazon.Glacier.AmazonGlacierClient glacier,
         String vault)
      {
         this.restoreIndex = restoreIndex;
         this.glacier = glacier;
         this.vault = vault;
         if (session.State == SkyFloe.Restore.SessionState.Pending)
         {
            foreach (Restore.Retrieval oldRetrieval in this.restoreIndex.ListRetrievals(session))
            {
               Backup.Blob blob = backupIndex.LookupBlob(oldRetrieval.Blob);
               Restore.Retrieval newRetrieval = this.restoreIndex.FetchRetrieval(oldRetrieval.ID);
               newRetrieval.Length = 0;
               this.restoreIndex.UpdateRetrieval(newRetrieval);
               foreach (Restore.Entry entry in this.restoreIndex.ListRetrievalEntries(oldRetrieval))
               {
                  if (entry.Offset < newRetrieval.Offset + newRetrieval.Length + MinRetrievalSize)
                  {
                     newRetrieval.Length = entry.Offset - newRetrieval.Offset + entry.Length;
                     newRetrieval.Length += MinRetrievalSize - newRetrieval.Length % MinRetrievalSize;
                     this.restoreIndex.UpdateRetrieval(newRetrieval);
                  }
                  else
                  {
                     newRetrieval = this.restoreIndex.InsertRetrieval(
                        new Restore.Retrieval()
                        {
                           Session = session,
                           Blob = newRetrieval.Blob,
                           Offset = entry.Offset - entry.Offset % MinRetrievalSize,
                           Length = entry.Length + (MinRetrievalSize - entry.Length % MinRetrievalSize)
                        }
                     );
                  }
                  entry.Retrieval = newRetrieval;
                  entry.Offset -= entry.Retrieval.Offset;
                  this.restoreIndex.UpdateEntry(entry);
                  if (newRetrieval.Offset + newRetrieval.Length > blob.Length)
                  {
                     newRetrieval.Length = blob.Length - newRetrieval.Offset;
                     this.restoreIndex.UpdateRetrieval(newRetrieval);
                  }
               }
            }
         }
         /* TODO
          * deprecate session.retrieved
         this.maxRetrievalRate =
            0.05d * backupIndex.ListSessions().Sum(s => s.ActualLength) /
            TimeSpan.FromDays(30).TotalSeconds;
         */
         this.maxRetrievalRate = 1024 * 1024 * 1024 / TimeSpan.FromHours(1).TotalSeconds;
         this.restoreStarted = DateTime.UtcNow;
         this.downloader = new GlacierDownloader(this.glacier, this.vault);
         foreach (Restore.Retrieval retrieval in this.restoreIndex.ListRetrievals(session))
         {
            Boolean clearRetrieval = false;
            try
            {
               if (retrieval.Name != null)
                  if (!this.restoreIndex.ListRetrievalEntries(retrieval).Any(e => e.State == SkyFloe.Restore.EntryState.Pending))
                     clearRetrieval = true;
                  else if (!this.downloader.QueryJob(retrieval.Name))
                     this.restoreRetrieving += retrieval.Length;
            }
            catch
            {
               clearRetrieval = true;
            }
            if (clearRetrieval)
            {
               retrieval.Name = null;
               this.restoreIndex.UpdateRetrieval(retrieval);
            }
         }
      }

      public void Dispose ()
      {
         if (this.downloader != null)
            this.downloader.Dispose();
         this.downloader = null;
      }

      #region IRestore Implementation
      public Stream Restore (Restore.Entry entry)
      {
         Boolean ready = false;
         while (!ready)
         {
            try
            {
               if (entry.Retrieval.Name != null)
                  ready = this.downloader.QueryJob(entry.Retrieval.Name);
            }
            catch
            {
               this.restoreRetrieving -= entry.Retrieval.Length;
               entry.Retrieval.Name = null;
               this.restoreIndex.UpdateRetrieval(entry.Retrieval);
            }
            foreach (Restore.Retrieval retrieval in this.restoreIndex
               .ListRetrievals(entry.Retrieval.Session)
               .SkipWhile(r => r.ID != entry.Retrieval.ID)
            )
            {
               Double retrievalRate =
                  (Double)this.restoreRetrieving /
                  (DateTime.UtcNow - this.restoreStarted).TotalSeconds;
               if (retrievalRate > this.maxRetrievalRate)
                  if (retrieval.ID != entry.Retrieval.ID)
                     break;
               if (retrieval.Name == null)
               {
                  // TODO: remove
                  Console.WriteLine(
                     "{0:MMM dd, hh:mm}: Retrieving blob {1}..., offset = {2}, length = {3}",
                     DateTime.Now,
                     retrieval.Blob.Substring(0, 20),
                     retrieval.Offset,
                     retrieval.Length
                  );
                  retrieval.Name = this.downloader.StartJob(
                     retrieval.Blob,
                     retrieval.Offset,
                     retrieval.Length
                  );
                  this.restoreIndex.UpdateRetrieval(retrieval);
                  if (retrieval.ID == entry.Retrieval.ID)
                     entry.Retrieval = retrieval;
                  this.restoreRetrieving += retrieval.Length;
               }
            }
            foreach (Restore.Retrieval retrieval in this.restoreIndex
               .ListRetrievals(entry.Retrieval.Session)
               .TakeWhile(r => r.ID != entry.Retrieval.ID)
               .Where(r => r.Name != null)
            )
               this.downloader.DeleteJob(retrieval.Name);
            if (!ready)
               System.Threading.Thread.Sleep(
                  (Int32)TimeSpan.FromMinutes(20).TotalMilliseconds
               );
         }
         return this.downloader.GetJobStream(
            entry.Retrieval.Name,
            entry.Offset,
            entry.Length
         );
      }
      #endregion
   }
}
