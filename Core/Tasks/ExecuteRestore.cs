using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Transactions;

namespace SkyFloe.Tasks
{
   public class ExecuteRestore : Task
   {
      private Store.IRestore restore;
      private IO.RateLimiter limiter;
      public SkyFloe.Restore.Session Session { get; set; }

      public override void Dispose ()
      {
         base.Dispose();
         if (this.restore != null)
            this.restore.Dispose();
         this.restore = null;
      }
      protected override void DoValidate ()
      {
         if (this.Session == null)
            throw new ArgumentException("Session");
         if (this.Session.State == Restore.SessionState.Completed)
            throw new ArgumentException("Session.State");
      }
      protected override void DoExecute ()
      {
         using (TransactionScope txn = new TransactionScope(TransactionScopeOption.Required, TimeSpan.MaxValue))
         {
            this.restore = this.Archive.PrepareRestore(this.Session);
            if (this.Session.State == SkyFloe.Restore.SessionState.Pending)
            {
               this.Session.State = SkyFloe.Restore.SessionState.InProgress;
               this.Archive.RestoreIndex.UpdateSession(this.Session);
            }
            txn.Complete();
         }
         this.limiter = new IO.RateLimiter(this.Session.RateLimit);
         for (; ; )
         {
            this.Canceler.ThrowIfCancellationRequested();
            SkyFloe.Restore.Entry entry = this.Archive.RestoreIndex.LookupNextEntry(this.Session);
            if (entry == null)
               break;
            RestoreEntry(entry);
         }
         if (this.Archive.RestoreIndex.LookupNextEntry(this.Session) == null)
         {
            this.Session.State = SkyFloe.Restore.SessionState.Completed;
            this.Archive.RestoreIndex.UpdateSession(this.Session);
         }
      }
      void RestoreEntry (SkyFloe.Restore.Entry restoreEntry)
      {
         SkyFloe.Backup.Entry backupEntry = this.Archive.BackupIndex.FetchEntry(restoreEntry.BackupEntryID);
         ReportProgress(
            new Engine.ProgressEventArgs()
            {
               Action = "BeginRestoreEntry",
               BackupSession = backupEntry.Session,
               BackupEntry = backupEntry,
               RestoreSession = this.Session,
               RestoreEntry = restoreEntry
            }
         );
         SkyFloe.Backup.Node rootNode = backupEntry.Node.GetRoot();
         SkyFloe.Restore.PathMap rootMap = this.Archive.RestoreIndex.LookupPathMap(this.Session, rootNode.ID);
         IO.Path rootPath = (rootMap != null) ? rootMap.Path : rootNode.Name;
         IO.Path path = rootPath + backupEntry.Node.GetRelativePath();
         IO.FileSystem.CreateDirectory(path.Parent);
         IO.FileSystem.Metadata metadata = IO.FileSystem.GetMetadata(path);
         // TODO: cleanup restore/delete logic
         Boolean restoreFile = true;
         if (metadata.Exists)
         {
            if (this.Session.SkipExisting && metadata.Length > 0)
               restoreFile = false;
            else if (metadata.IsReadOnly)
               if (this.Session.SkipReadOnly)
                  restoreFile = false;
               else
                  IO.FileSystem.MakeWritable(path);
         }
         else if (backupEntry.State == SkyFloe.Backup.EntryState.Deleted)
            restoreFile = false;
         if (restoreFile)
         {
            try
            {
               if (backupEntry.State != SkyFloe.Backup.EntryState.Deleted)
               {
                  using (Stream fileStream = IO.FileSystem.Truncate(path))
                  using (IO.Crc32Filter crcFilter = new IO.Crc32Filter(fileStream))
                  using (Stream limiterFilter = this.limiter.CreateStreamFilter(crcFilter))
                  using (Stream archiveFilter = this.restore.Restore(restoreEntry))
                  using (Stream cryptoFilter = new CryptoStream(archiveFilter, this.Crypto.CreateDecryptor(), CryptoStreamMode.Read))
                  {
                     cryptoFilter.CopyTo(limiterFilter);
                     limiterFilter.Flush();
                     if (this.Session.VerifyResults && crcFilter.Value != backupEntry.Crc32)
                        throw new InvalidOperationException("TODO: CRC does not match");
                  }
               }
               else if (this.Session.EnableDeletes)
               {
                  IO.FileSystem.Delete(path);
               }
               restoreEntry.State = SkyFloe.Restore.EntryState.Completed;
               this.Session.RestoreLength += restoreEntry.Length;
               using (TransactionScope txn = new TransactionScope())
               {
                  this.Archive.RestoreIndex.UpdateEntry(restoreEntry);
                  this.Archive.RestoreIndex.UpdateSession(this.Session);
                  txn.Complete();
               }
            }
            catch (OperationCanceledException)
            {
               throw;
            }
            catch (Exception e)
            {
               try { IO.FileSystem.Delete(path); } catch { }
               switch (ReportError("RestoreEntry", e))
               {
                  case Engine.ErrorResult.Retry:
                     break;
                  case Engine.ErrorResult.Fail:
                     restoreEntry = this.Archive.RestoreIndex.FetchEntry(restoreEntry.ID);
                     restoreEntry.State = SkyFloe.Restore.EntryState.Failed;
                     this.Archive.RestoreIndex.UpdateEntry(restoreEntry);
                     break;
                  default:
                     throw;
               }
            }
         }
         if (restoreEntry.State == SkyFloe.Restore.EntryState.Completed)
            ReportProgress(
               new Engine.ProgressEventArgs()
               {
                  Action = "EndRestoreEntry",
                  BackupSession = backupEntry.Session,
                  BackupEntry = backupEntry,
                  RestoreSession = this.Session,
                  RestoreEntry = restoreEntry
               }
            );
      }
   }
}
