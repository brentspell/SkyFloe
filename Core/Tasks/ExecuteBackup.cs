﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Transactions;

namespace SkyFloe.Tasks
{
   public class ExecuteBackup : Task
   {
      private Store.IBackup backup;
      private IO.RateLimiter limiter;
      public SkyFloe.Backup.Session Session { get; set; }

      public override void Dispose ()
      {
         base.Dispose();
         if (this.backup != null)
            this.backup.Dispose();
         this.backup = null;
      }
      public override void Execute ()
      {
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
            Int64 checkpointSize = 0;
            for (; ; )
            {
               this.Canceler.ThrowIfCancellationRequested();
               SkyFloe.Backup.Entry entry = this.Archive.BackupIndex.LookupNextEntry(this.Session);
               if (entry == null)
                  break;
               BackupEntry(entry);
               checkpointSize += entry.Length;
               if (checkpointSize > this.Session.CheckpointLength)
               {
                  checkpointSize = 0;
                  Checkpoint();
               }
            }
            if (this.Archive.BackupIndex.LookupNextEntry(this.Session) == null)
            {
               this.Session.State = SkyFloe.Backup.SessionState.Completed;
               this.Archive.BackupIndex.UpdateSession(this.Session);
            }
            Checkpoint();
         }
         catch (OperationCanceledException)
         {
            Checkpoint();
            throw;
         }
      }
      private void BackupEntry (SkyFloe.Backup.Entry entry)
      {
         ReportProgress(
            new Engine.ProgressEventArgs()
            {
               Action = "BeginBackupEntry",
               BackupSession = this.Session,
               BackupEntry = entry
            }
         );
         try
         {
            using (Stream fileStream = IO.FileSystem.Open(entry.Node.GetAbsolutePath()))
            using (IO.Crc32Filter crcFilter = new IO.Crc32Filter(fileStream))
            using (Stream cryptoFilter = new CryptoStream(crcFilter, this.Crypto.CreateEncryptor(), CryptoStreamMode.Read))
            using (Stream cancelFilter = new IO.CancelFilter(cryptoFilter, this.Canceler))
            using (Stream limiterFilter = this.limiter.CreateStreamFilter(cancelFilter))
            {
               this.backup.Backup(entry, limiterFilter);
               entry.Crc32 = crcFilter.Value;
            }
            entry.State = SkyFloe.Backup.EntryState.Completed;
            entry.Blob.Length += entry.Length;
            this.Session.ActualLength += entry.Length;
            using (TransactionScope txn = new TransactionScope())
            {
               this.Archive.BackupIndex.UpdateEntry(entry);
               this.Archive.BackupIndex.UpdateBlob(entry.Blob);
               this.Archive.BackupIndex.UpdateSession(this.Session);
               txn.Complete();
            }
         }
         catch (OperationCanceledException)
         {
            throw;
         }
         catch (Exception e)
         {
            switch (ReportError("BackupEntry", e))
            {
               case Engine.ErrorResult.Retry:
                  break;
               case Engine.ErrorResult.Fail:
                  entry = this.Archive.BackupIndex.FetchEntry(entry.ID);
                  entry.State = SkyFloe.Backup.EntryState.Failed;
                  this.Archive.BackupIndex.UpdateEntry(entry);
                  break;
               default:
                  throw;
            }
         }
         if (entry.State == SkyFloe.Backup.EntryState.Completed)
            ReportProgress(
               new Engine.ProgressEventArgs()
               {
                  Action = "EndBackupEntry",
                  BackupSession = this.Session,
                  BackupEntry = entry
               }
            );
      }
      private void Checkpoint ()
      {
         ReportProgress(
            new Engine.ProgressEventArgs()
            {
               Action = "BeginCheckpoint",
               BackupSession = this.Session
            }
         );
         for ( ; ; )
         {
            try
            {
               this.backup.Checkpoint();
               break;
            }
            catch (Exception e)
            {
               if (ReportError("Checkpoint", e) != Engine.ErrorResult.Retry)
                  throw;
            }
         }
         ReportProgress(
            new Engine.ProgressEventArgs()
            {
               Action = "EndCheckpoint",
               BackupSession = this.Session
            }
         );
      }
   }
}
