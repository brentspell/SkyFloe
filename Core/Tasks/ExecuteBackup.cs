//===========================================================================
// MODULE:  ExecuteBackup.cs
// PURPOSE: backup session execution task
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
using System.Security.Cryptography;
using System.Transactions;
// Project References

namespace SkyFloe.Tasks
{
   /// <summary>
   /// Execute backup task
   /// </summary>
   /// <remarks>
   /// This task starts or resumes a backup session prepared by the 
   /// CreateBackup task. It enumerates over all backup entries in the 
   /// session and submits them to the backup archive plugin for storage.
   /// </remarks>
   public class ExecuteBackup : Task
   {
      private Store.IBackup backup;
      private IO.RateLimiter limiter;

      /// <summary>
      /// The backup session to start/resume
      /// </summary>
      public SkyFloe.Backup.Session Session { get; set; }

      /// <summary>
      /// Releases resources associated with the task
      /// </summary>
      public override void Dispose ()
      {
         base.Dispose();
         if (this.backup != null)
            this.backup.Dispose();
         this.backup = null;
      }
      /// <summary>
      /// Task validation override
      /// </summary>
      protected override void DoValidate ()
      {
         if (this.Session == null)
            throw new ArgumentException("Session");
         if (this.Session.State == Backup.SessionState.Completed)
            throw new ArgumentException("Session.State");
      }
      /// <summary>
      /// Task execution override
      /// </summary>
      protected override void DoExecute ()
      {
         // prepare the backup plugin for the session
         // and mark it as in-progress
         this.backup = this.Archive.PrepareBackup(this.Session);
         if (this.Session.State == SkyFloe.Backup.SessionState.Pending)
         {
            this.Session.State = SkyFloe.Backup.SessionState.InProgress;
            this.Archive.BackupIndex.UpdateSession(this.Session);
            Checkpoint();
         }
         try
         {
            this.limiter = new IO.RateLimiter(this.Session.RateLimit);
            var checkpointSize = 0L;
            for (; ; )
            {
               this.Canceler.ThrowIfCancellationRequested();
               // fetch the next pending backup entry and 
               // send it to the archive
               var entry = this.Archive.BackupIndex.LookupNextEntry(this.Session);
               if (entry == null)
                  break;
               BackupEntry(entry);
               // if we have reached the configured checkpoint
               // size, then force a checkpoint
               checkpointSize += entry.Length;
               if (checkpointSize > this.Session.CheckpointLength)
               {
                  checkpointSize = 0;
                  Checkpoint();
               }
            }
            // there are no more pending backup entries, so complete the session
            this.Session.State = SkyFloe.Backup.SessionState.Completed;
            this.Archive.BackupIndex.UpdateSession(this.Session);
            Checkpoint();
         }
         catch (OperationCanceledException)
         {
            // if the client requested cancellation, attempt to
            // checkpoint the backup to save our progress
            Checkpoint();
            throw;
         }
      }
      /// <summary>
      /// Backs up a single file entry to the attached archive
      /// </summary>
      /// <param name="entry">
      /// The entry to store
      /// </param>
      private void BackupEntry (SkyFloe.Backup.Entry entry)
      {
         ReportProgress(
            new ProgressEventArgs()
            {
               Operation = "BeginBackupEntry",
               BackupSession = this.Session,
               BackupEntry = entry
            }
         );
         try
         {
            using (var input = new IO.StreamStack())
            {
               // create the input stream stack, consisting of the following
               // . source file
               // . CRC calculation filter
               // . optional compressor
               // . encryptor
               // . read rate limiter
               input.Push(
                  IO.FileSystem.Open(entry.Node.GetAbsolutePath())
               );
               input.Push(
                  new IO.CrcFilter(input.Top)
               );
               if (this.Session.Compress)
                  input.Push(
                     new IO.CompressionStream(
                        input.Top, 
                        IO.CompressionMode.Compress
                     )
                  );
               input.Push(
                  new CryptoStream(
                     input.Top, 
                     this.Crypto.CreateEncryptor(), 
                     CryptoStreamMode.Read
                  )
               );
               input.Push(
                  this.limiter.CreateStreamFilter(input.Top)
               );
               // send the stream stack to the backup plugin
               // and record the calculated CRC value in the entry
               this.backup.Backup(entry, input);
               entry.Crc32 = input.GetStream<IO.CrcFilter>().Value;
            }
            // commit the archive entry to the index
            // at this point, the entry is locally durable
            // until the next checkpoint, when it becomes permanent
            entry.State = SkyFloe.Backup.EntryState.Completed;
            entry.Blob.Length += entry.Length;
            this.Session.ActualLength += entry.Length;
            using (var txn = new TransactionScope())
            {
               this.Archive.BackupIndex.UpdateEntry(entry);
               this.Archive.BackupIndex.UpdateBlob(entry.Blob);
               this.Archive.BackupIndex.UpdateSession(this.Session);
               txn.Complete();
            }
         }
         catch (Exception e)
         {
            switch (ReportError("BackupEntry", e))
            {
               case ErrorResult.Retry:
                  // the client chose to retry, so leave the entry pending
                  break;
               case ErrorResult.Fail:
                  // the client chose to ignore the failed entry,
                  // so mark it as failed and continue
                  entry = this.Archive.BackupIndex.FetchEntry(entry.ID);
                  entry.State = SkyFloe.Backup.EntryState.Failed;
                  this.Archive.BackupIndex.UpdateEntry(entry);
                  break;
               default:
                  // the client chose to abort, so propagate the exception
                  throw;
            }
         }
         if (entry.State == SkyFloe.Backup.EntryState.Completed)
            ReportProgress(
               new ProgressEventArgs()
               {
                  Operation = "EndBackupEntry",
                  BackupSession = this.Session,
                  BackupEntry = entry
               }
            );
      }
      /// <summary>
      /// Checkpoints the current state of the backup
      /// session with the backup plugin, making it
      /// durable for restore
      /// </summary>
      private void Checkpoint ()
      {
         ReportProgress(
            new ProgressEventArgs()
            {
               Operation = "BeginCheckpoint",
               BackupSession = this.Session
            }
         );
         WithRetry(
            "Checkpoint",
            () => this.backup.Checkpoint()
         );
         ReportProgress(
            new ProgressEventArgs()
            {
               Operation = "EndCheckpoint",
               BackupSession = this.Session
            }
         );
      }
   }
}
