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
      private GlacierArchive archive;
      private GlacierDownloader downloader;
      private IO.RateLimiter retrievalLimiter;

      public GlacierRestore (GlacierArchive archive, Restore.Session session)
      {
         this.archive = archive;
         if (session.State == SkyFloe.Restore.SessionState.Pending)
         {
            foreach (var oldRetrieval in this.archive.RestoreIndex.ListRetrievals(session))
            {
               var blob = this.archive.BackupIndex.LookupBlob(oldRetrieval.Blob);
               var newRetrieval = this.archive.RestoreIndex.FetchRetrieval(oldRetrieval.ID);
               newRetrieval.Length = 0;
               this.archive.RestoreIndex.UpdateRetrieval(newRetrieval);
               foreach (var entry in this.archive.RestoreIndex.ListRetrievalEntries(oldRetrieval))
               {
                  if (entry.Offset < newRetrieval.Offset + newRetrieval.Length + MinRetrievalSize)
                  {
                     newRetrieval.Length = entry.Offset - newRetrieval.Offset + entry.Length;
                     newRetrieval.Length += MinRetrievalSize - newRetrieval.Length % MinRetrievalSize;
                     this.archive.RestoreIndex.UpdateRetrieval(newRetrieval);
                  }
                  else
                  {
                     newRetrieval = this.archive.RestoreIndex.InsertRetrieval(
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
                  this.archive.RestoreIndex.UpdateEntry(entry);
                  if (newRetrieval.Offset + newRetrieval.Length > blob.Length)
                  {
                     newRetrieval.Length = blob.Length - newRetrieval.Offset;
                     this.archive.RestoreIndex.UpdateRetrieval(newRetrieval);
                  }
               }
            }
         }
         this.retrievalLimiter = new IO.RateLimiter(session.RateLimit);
         this.downloader = new GlacierDownloader(this.archive.Glacier, this.archive.Vault);
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
         var ready = false;
         while (!ready)
         {
            try
            {
               if (entry.Retrieval.Name != null)
                  ready = this.downloader.QueryJob(entry.Retrieval.Name);
            }
            catch
            {
               entry.Retrieval.Name = null;
               this.archive.RestoreIndex.UpdateRetrieval(entry.Retrieval);
            }
            foreach (var retrieval in this.archive.RestoreIndex
               .ListRetrievals(entry.Retrieval.Session)
               .SkipWhile(r => r.ID != entry.Retrieval.ID)
            )
            {
               if (this.retrievalLimiter.OutOfControl)
                  if (retrieval.ID != entry.Retrieval.ID)
                     break;
               if (retrieval.Name == null)
               {
                  retrieval.Name = this.downloader.StartJob(
                     retrieval.Blob,
                     retrieval.Offset,
                     retrieval.Length
                  );
                  this.archive.RestoreIndex.UpdateRetrieval(retrieval);
                  if (retrieval.ID == entry.Retrieval.ID)
                     entry.Retrieval = retrieval;
                  this.retrievalLimiter.Register(retrieval.Length);
               }
            }
            foreach (var retrieval in this.archive.RestoreIndex
               .ListRetrievals(entry.Retrieval.Session)
               .TakeWhile(r => r.ID != entry.Retrieval.ID)
               .Where(r => r.Name != null)
            )
               this.downloader.DeleteJob(retrieval.Name);
            if (!ready)
               System.Threading.Thread.Sleep(
                  (Int32)TimeSpan.FromMinutes(5).TotalMilliseconds
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
