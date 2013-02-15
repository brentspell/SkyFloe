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

      public override void Execute ()
      {
         using (TransactionScope txn = new TransactionScope(TransactionScopeOption.Required, TimeSpan.MaxValue))
         {
            Backup.Session session = this.Archive.BackupIndex.InsertSession(
               new Backup.Session()
               {
                  State = Backup.SessionState.Pending,
                  CheckpointLength = this.Request.CheckpointLength,
                  RateLimit = this.Request.RateLimit
               }
            );
            Difference differenceTask = new Difference()
            {
               Archive = this.Archive,
               Crypto = this.Crypto,
               Canceler = this.Canceler,
               Request = new DiffRequest()
               {
                  RootPathMap = new Dictionary<IO.Path, IO.Path>(),
                  Method = this.Request.DiffMethod
               }
            };
            foreach (String source in this.Request.Sources)
            {
               Backup.Node root = 
                  this.Archive.BackupIndex.ListNodes(null)
                     .FirstOrDefault(n => String.Compare(n.Name, source, true) == 0) ??
                  this.Archive.BackupIndex.InsertNode(
                     new Backup.Node()
                     {
                        Type = Backup.NodeType.Root,
                        Name = source
                     }
                  );
               foreach (DiffEntry diff in differenceTask.Enumerate(root))
               {
                  if (diff.Node.ID == 0)
                     this.Archive.BackupIndex.InsertNode(diff.Node);
                  if (diff.Node.Type == Backup.NodeType.File)
                  {
                     Backup.Entry entry = this.Archive.BackupIndex.InsertEntry(
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
