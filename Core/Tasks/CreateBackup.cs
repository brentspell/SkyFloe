using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Transactions;

namespace SkyFloe.Tasks
{
   public class CreateBackup : Task
   {
      public BackupRequest Request { get; set; }
      public Backup.Session Session { get; private set; }

      protected override void DoValidate ()
      {
         if (this.Request == null)
            throw new ArgumentException("Request");
         if (!this.Request.Sources.Any())
            throw new ArgumentException("Request.Sources");
         foreach (var source in this.Request.Sources)
         {
            var metadata = IO.FileSystem.GetMetadata(source);
            if (!metadata.Exists)
               throw new InvalidOperationException("TODO: source path not found");
            if (!metadata.IsDirectory)
               throw new InvalidOperationException("TODO: source path not a directory");
         }
         if (!this.Request.Filter.IsValid)
            throw new ArgumentException("Request.Filter");
         switch (this.Request.DiffMethod)
         {
            case DiffMethod.Timestamp: break;
            case DiffMethod.Digest: break;
            default:
               throw new ArgumentException("Request.DiffMethod");
         }
         if (this.Request.RateLimit <= 0)
            throw new ArgumentOutOfRangeException("Request.RateLimit");
         if (this.Request.CheckpointLength <= 0)
            throw new ArgumentOutOfRangeException("Request.CheckpointLength");
         if (this.Archive.BackupIndex.ListSessions()
               .Any(s => s.State != Backup.SessionState.Completed))
            throw new InvalidOperationException("TODO: session in progress");
      }
      protected override void DoExecute ()
      {
         using (var txn = new TransactionScope(TransactionScopeOption.Required, TimeSpan.MaxValue))
         {
            var session = this.Archive.BackupIndex.InsertSession(
               new Backup.Session()
               {
                  State = Backup.SessionState.Pending,
                  CheckpointLength = this.Request.CheckpointLength,
                  RateLimit = this.Request.RateLimit
               }
            );
            var differenceTask = new Difference()
            {
               Archive = this.Archive,
               Crypto = this.Crypto,
               Canceler = this.Canceler,
               OnProgress = this.OnProgress,
               OnError = this.OnError,
               Request = new DiffRequest()
               {
                  Method = this.Request.DiffMethod,
                  Filter = this.Request.Filter
               }
            };
            foreach (var source in this.Request.Sources)
            {
               var root = 
                  this.Archive.BackupIndex.ListNodes(null)
                     .FirstOrDefault(n => StringComparer.OrdinalIgnoreCase.Equals(n.Name, source)) ??
                  this.Archive.BackupIndex.InsertNode(
                     new Backup.Node()
                     {
                        Type = Backup.NodeType.Root,
                        Name = source
                     }
                  );
               foreach (var diff in differenceTask.Enumerate(root))
               {
                  if (diff.Node.ID == 0)
                     this.Archive.BackupIndex.InsertNode(diff.Node);
                  if (diff.Node.Type == Backup.NodeType.File)
                  {
                     var entry = this.Archive.BackupIndex.InsertEntry(
                        new Backup.Entry()
                        {
                           Session = session,
                           Node = diff.Node,
                           State = (diff.Type != DiffType.Deleted) ?
                              Backup.EntryState.Pending :
                              Backup.EntryState.Deleted,
                           Offset = -1,
                           Length = IO.FileSystem.GetMetadata(diff.Node.GetAbsolutePath()).Length,
                           Crc32 = IO.Crc32Filter.InitialValue
                        }
                     );
                     ReportProgress(
                        new Engine.ProgressEventArgs()
                        {
                           Action = "CreateBackupEntry",
                           BackupSession = session,
                           BackupEntry = entry
                        }
                     );
                     session.EstimatedLength += entry.Length;
                  }
               }
            }
            txn.Complete();
            this.Session = session;
         }
      }
   }
}
