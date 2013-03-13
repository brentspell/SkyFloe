//===========================================================================
// MODULE:  GlacierRestore.cs
// PURPOSE: AWS glacier restore implementation
// 
// Copyright © 2013
// Brent M. Spell. All rights reserved.
//
// This library is free software; you can redistribute it and/or modify it 
// under the terms of the GNU Lesser General Public License as published 
// by the Free Software Foundation; either version 3 of the License, or 
// (at your option) any later version. This library is distributed in the 
// hope that it will be useful, but WITHOUT ANY WARRANTY; without even the 
// implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
// See the GNU Lesser General Public License for more details. You should 
// have received a copy of the GNU Lesser General Public License along with 
// this library; if not, write to 
//    Free Software Foundation, Inc. 
//    51 Franklin Street, Fifth Floor 
//    Boston, MA 02110-1301 USA
//===========================================================================
// System References
using System;
using System.Collections.Generic;
using System.Linq;
using Stream = System.IO.Stream;
// Project References
using SkyFloe.Store;

namespace SkyFloe.Aws
{
   /// <summary>
   /// Glacier restore
   /// </summary>
   /// <remarks>
   /// This class implements the IRestore interface for AWS Glacier. It
   /// uses the GlacierDownloader class to start retrieval jobs and
   /// download their results. Because retrieval jobs can take a long time
   /// (currently 4+ hours), the restore ensures that there are enough
   /// pending jobs to keep it busy downloading vault archives, up to the
   /// configured rate limit on the restore session.
   /// Because SkyFloe may retrieve blobs in a sparse manner (due to
   /// incremental backups), the restore implements a scheduling algorithm 
   /// that retrieves only the parts of archives that are actually needed to 
   /// restore, batching those parts that are nearby.
   /// </remarks>
   public class GlacierRestore : IRestore
   {
      public const Int32 MinRetrievalSize = 1024 * 1024;
      public const Int32 MaxRetrievalRate = 5 * 1024 * 1024;
      private GlacierArchive archive;
      private GlacierDownloader downloader;
      private IO.RateLimiter retrievalLimiter;

      /// <summary>
      /// Initializes a new restore instance
      /// </summary>
      /// <param name="archive">
      /// The SkyFloe archive for the restore
      /// </param>
      /// <param name="session">
      /// The restore session being processed
      /// </param>
      public GlacierRestore (GlacierArchive archive, Restore.Session session)
      {
         this.archive = archive;
         if (session.State == SkyFloe.Restore.SessionState.Pending)
            ScheduleRetrievals(session);
         this.downloader = new GlacierDownloader(
            this.archive.Glacier, 
            this.archive.Vault
         );
         this.retrievalLimiter = new IO.RateLimiter(
            Math.Min(session.RateLimit, MaxRetrievalRate)
         );
      }
      /// <summary>
      /// Releases the resources associated with the restore
      /// </summary>
      public void Dispose ()
      {
         if (this.downloader != null)
            this.downloader.Dispose();
         this.downloader = null;
      }

      #region IRestore Implementation
      /// <summary>
      /// Retrieves a single backup entry from a vault archive
      /// </summary>
      /// <param name="entry">
      /// The entry to retrieve
      /// </param>
      /// <returns>
      /// A stream containing the backup entry's data
      /// </returns>
      public Stream Restore (Restore.Entry entry)
      {
         // process the entry's vault archive retrieval
         // . poll the current status of the retrieval first,
         //   so that it can be requested again if invalid/failed
         // . start up any retrievals as long as we are within the
         //   tolerance of the rate limiter
         // . clean up any retrievals that are no longer needed
         // . go to sleep until the next polling interval
         var ready = false;
         do
         {
            if (entry.Retrieval.Name != null)
               ready = CheckRetrieval(entry.Retrieval);
            StartRetrievals(entry.Retrieval);
            CleanRetrievals(entry.Retrieval);
            if (!ready)
               System.Threading.Thread.Sleep(
                  (Int32)TimeSpan.FromMinutes(5).TotalMilliseconds
               );
         } while (!ready);
         // the retrieval is now ready, so open up a stream for it
         return this.downloader.GetJobStream(
            entry.Retrieval.Name,
            entry.Offset,
            entry.Length
         );
      }
      #endregion

      #region Restore Scheduling
      /// <summary>
      /// Schedules blob retrievals for a restore session
      /// </summary>
      /// <param name="session">
      /// The restore session to process
      /// </param>
      private void ScheduleRetrievals (Restore.Session session)
      {
         foreach (var retrieval in this.archive.RestoreIndex.ListRetrievals(session))
            ScheduleRetrieval(
               retrieval, 
               this.archive.BackupIndex.LookupBlob(retrieval.Blob),
               this.archive.RestoreIndex.ListRetrievalEntries(retrieval)
            );
      }
      /// <summary>
      /// (Re)schedules the retrieval of a vault archive
      /// </summary>
      /// <remarks>
      /// This method splits up a complete blob retrieval into those
      /// sub-regions that are actually needed in the restore. Rather than
      /// generate a separate retrieval for each entry (which would be 
      /// wasteful), the method attempts to coalesce any nearby backup
      /// entries (within 1MMB) into a single retrieval.
      /// </remarks>
      /// <param name="retrieval">
      /// The initial (full) blob retrieval to optimize
      /// </param>
      /// <param name="blob">
      /// The blob to retrieve
      /// </param>
      /// <param name="entries">
      /// The list of backup entries to retrieve from this blob
      /// </param>
      private void ScheduleRetrieval (
         Restore.Retrieval retrieval,
         Backup.Blob blob,
         IEnumerable<Restore.Entry> entries)
      {
         // fetch the retrieval again, so that we do not modify the
         // copy attached to each of the incoming restore entries,
         // and set it to zero length to seed the scheduler
         retrieval = this.archive.RestoreIndex.FetchRetrieval(retrieval.ID);
         retrieval.Length = 0;
         this.archive.RestoreIndex.UpdateRetrieval(retrieval);
         foreach (var entry in entries)
         {
            // if the offset of the current entry is within 1MB of the end of 
            // the current retrieval, then extend the retrieval to include 
            // the current entry
            if (entry.Offset < retrieval.Offset + retrieval.Length + MinRetrievalSize)
            {
               retrieval.Length = entry.Offset - retrieval.Offset + entry.Length;
               retrieval.Length += MinRetrievalSize - retrieval.Length % MinRetrievalSize;
               this.archive.RestoreIndex.UpdateRetrieval(retrieval);
            }
            else
            {
               // otherwise, create a new retrieval to avoid retrieving
               // the space between the old retrieval and the new entry
               retrieval = this.archive.RestoreIndex.InsertRetrieval(
                  new Restore.Retrieval()
                  {
                     Session = entry.Session,
                     Blob = retrieval.Blob,
                     Offset = entry.Offset - entry.Offset % MinRetrievalSize,
                     Length = entry.Length + (MinRetrievalSize - entry.Length % MinRetrievalSize)
                  }
               );
            }
            // attach the new retrieval to the entry and
            // rebase it to the start of the retrieval
            entry.Retrieval = retrieval;
            entry.Offset -= entry.Retrieval.Offset;
            this.archive.RestoreIndex.UpdateEntry(entry);
            // trim the end of the retrieval if it exceeds the blob
            if (retrieval.Offset + retrieval.Length > blob.Length)
            {
               retrieval.Length = blob.Length - retrieval.Offset;
               this.archive.RestoreIndex.UpdateRetrieval(retrieval);
            }
         }
      }
      #endregion

      #region Restore Retrieval
      /// <summary>
      /// Checks the status of a restore retrieval job
      /// </summary>
      /// <param name="retrieval">
      /// The retrieval to check
      /// </param>
      /// <returns>
      /// True if the Glacier job is ready for download
      /// False if it has not yet completed or was invalidated
      /// </returns>
      private Boolean CheckRetrieval (Restore.Retrieval retrieval)
      {
         switch (this.downloader.QueryJob(retrieval.Name))
         {
            case JobStatus.Completed:
               return true;
            case JobStatus.InProgress:
               break;
            default:
               // the job failed or was not found
               // remove it so that it can be resubmitted
               retrieval.Name = null;
               this.archive.RestoreIndex.UpdateRetrieval(retrieval);
               break;
         }
         return false;
      }
      /// <summary>
      /// Initiates Glacier retrieval jobs for each retrieval in
      /// the restore, until the rate limit has been reached
      /// </summary>
      /// <param name="current">
      /// The current retrieval, which must be scheduled unconditionally
      /// </param>
      private void StartRetrievals (Restore.Retrieval current)
      {
         // skip over any retrieval jobs that are earlier than the requested,
         // as these should already have been processed
         foreach (var retrieval in this.archive.RestoreIndex
            .ListRetrievals(current.Session)
            .SkipWhile(r => r.ID != current.ID)
         )
         {
            // stop processing retrievals if we have reached the limit,
            // but make sure that we always request the current
            if (this.retrievalLimiter.OutOfControl)
               if (retrieval.ID != current.ID)
                  break;
            if (retrieval.Name == null)
            {
               // start the Glacier retrieval job and assign the
               // job identifier to the retrieval
               retrieval.Name = this.downloader.StartJob(
                  retrieval.Blob,
                  retrieval.Offset,
                  retrieval.Length
               );
               this.archive.RestoreIndex.UpdateRetrieval(retrieval);
               this.retrievalLimiter.Process(retrieval.Length);
               // return the updated retrieval name so that it
               // can be attached to the restore entry
               if (retrieval.ID == current.ID)
                  current.Name = retrieval.Name;
            }
         }
      }
      /// <summary>
      /// Releases the resources associated with any retrievals
      /// that are no longer needed
      /// </summary>
      /// <param name="current">
      /// The current retrieval being processed
      /// </param>
      private void CleanRetrievals (Restore.Retrieval current)
      {
         foreach (var retrieval in this.archive.RestoreIndex
            .ListRetrievals(current.Session)
            .TakeWhile(r => r.ID != current.ID)
            .Where(r => r.Name != null)
         )
            this.downloader.DeleteJob(retrieval.Name);
      }
      #endregion
   }
}
