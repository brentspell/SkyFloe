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
         using (var txn = new TransactionScope(TransactionScopeOption.Required, TimeSpan.MaxValue))
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
            var entry = this.Archive.RestoreIndex.LookupNextEntry(this.Session);
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
         var backupEntry = this.Archive.BackupIndex.FetchEntry(restoreEntry.BackupEntryID);
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
         var rootNode = backupEntry.Node.GetRoot();
         var rootMap = this.Archive.RestoreIndex.LookupPathMap(this.Session, rootNode.ID);
         var rootPath = (IO.Path)(rootMap != null ? rootMap.Path : rootNode.Name);
         var path = rootPath + backupEntry.Node.GetRelativePath();
         IO.FileSystem.CreateDirectory(path.Parent);
         var metadata = IO.FileSystem.GetMetadata(path);
         // TODO: cleanup restore/delete logic
         var restoreFile = true;
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
                  using (var fileStream = IO.FileSystem.Truncate(path))
                  using (var crcFilter = new IO.Crc32Filter(fileStream))
                  using (var limiterFilter = this.limiter.CreateStreamFilter(crcFilter))
                  using (var archiveFilter = this.restore.Restore(restoreEntry))
                  using (var cryptoFilter = new CryptoStream(archiveFilter, this.Crypto.CreateDecryptor(), CryptoStreamMode.Read))
                  using (var compressor = backupEntry.Session.Compress ? new IO.CompressionStream(cryptoFilter, IO.CompressionMode.Decompress) : (Stream)cryptoFilter)
                  {
                     compressor.CopyTo(limiterFilter);
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
               using (var txn = new TransactionScope())
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
