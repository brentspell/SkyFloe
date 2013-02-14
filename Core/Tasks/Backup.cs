using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Transactions;

namespace SkyFloe.Tasks
{
   public class Backup : Task
   {
      private Store.IBackup backup;
      private IO.RateLimiter limiter;
      public SkyFloe.Backup.Session Session { get; set; }

      public override void Dispose ()
      {
         if (this.backup != null)
            this.backup.Dispose();
         this.backup = null;
      }
      protected override void Execute ()
      {
         this.backup = this.Archive.PrepareBackup(this.Session);
         if (this.Session.State == SkyFloe.Backup.SessionState.Pending)
         {
            this.Session.State = SkyFloe.Backup.SessionState.InProgress;
            this.Archive.BackupIndex.UpdateSession(this.Session);
            Checkpoint();
         }
         this.limiter = new IO.RateLimiter(this.Session.RateLimit);
         Int64 checkpointSize = 0;
         while (!this.Cancel.IsCancellationRequested)
         {
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
      private void BackupEntry (SkyFloe.Backup.Entry entry)
      {
         ReportProgress("BeginBackupEntry", entry);
         try
         {
            using (Stream fileStream = IO.FileSystem.Open(entry.Node.GetAbsolutePath()))
            using (IO.Crc32Stream crcStream = new IO.Crc32Stream(fileStream, IO.StreamMode.Read))
            using (Stream cryptoStream = new CryptoStream(crcStream, this.Crypto.CreateEncryptor(), CryptoStreamMode.Read))
            using (Stream limiterStream = this.limiter.CreateStream(cryptoStream, IO.StreamMode.Read))
            {
               this.backup.Backup(entry, limiterStream);
               entry.Crc32 = crcStream.Value;
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
         catch (Exception e)
         {
            switch (ReportError(e))
            {
               case ErrorResult.Retry:
                  break;
               case ErrorResult.Fail:
                  entry = this.Archive.BackupIndex.FetchEntry(entry.ID);
                  entry.State = SkyFloe.Backup.EntryState.Failed;
                  this.Archive.BackupIndex.UpdateEntry(entry);
                  break;
               default:
                  throw;
            }
         }
         if (entry.State == SkyFloe.Backup.EntryState.Completed)
            ReportProgress("EndBackupEntry", entry);
      }
      private void Checkpoint ()
      {
         ReportProgress("BeginBackupCheckpoint", this.Session);
         for ( ; ; )
         {
            try
            {
               this.backup.Checkpoint();
               break;
            }
            catch (Exception e)
            {
               if (ReportError(e) != ErrorResult.Retry)
                  throw;
            }
         }
         ReportProgress("EndBackupCheckpoint", this.Session);
      }
   }
}
