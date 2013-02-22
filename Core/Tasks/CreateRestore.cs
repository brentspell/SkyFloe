using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Transactions;

namespace SkyFloe.Tasks
{
   public class CreateRestore : Task
   {
      public RestoreRequest Request { get; set; }
      public Restore.Session Session { get; private set; }

      protected override void DoValidate ()
      {
         if (this.Request == null)
            throw new ArgumentException("Request");
         foreach (KeyValuePair<IO.Path, IO.Path> map in this.Request.RootPathMap)
         {
            if (map.Key.IsEmpty)
               throw new ArgumentException("Request.RootPathMap.Key");
            if (map.Value.IsEmpty)
               throw new ArgumentException("Request.RootPathMap.Value");
         }
         if (!this.Request.Filter.IsValid)
            throw new ArgumentException("Request.Filter");
         if (this.Request.RateLimit <= 0)
            throw new ArgumentException("Request.RateLimit");
      }
      protected override void DoExecute ()
      {
         using (TransactionScope txn = new TransactionScope(TransactionScopeOption.Required, TimeSpan.MaxValue))
         {
            Restore.Session session = this.Archive.RestoreIndex.InsertSession(
               new Restore.Session()
               {
                  State = Restore.SessionState.Pending,
                  Flags =
                     ((this.Request.SkipExisting) ? Restore.SessionFlags.SkipExisting : 0) |
                     ((this.Request.SkipReadOnly) ? Restore.SessionFlags.SkipReadOnly : 0) |
                     ((this.Request.VerifyResults) ? Restore.SessionFlags.VerifyResults : 0) |
                     ((this.Request.EnableDeletes) ? Restore.SessionFlags.EnableDeletes : 0),
                  RateLimit = this.Request.RateLimit
               }
            );
            IEnumerable<Backup.Node> roots = this.Archive.BackupIndex.ListNodes(null);
            foreach (KeyValuePair<IO.Path, IO.Path> pathMap in this.Request.RootPathMap)
            {
               Backup.Node root = roots.FirstOrDefault(
                  n => n.Name == pathMap.Key
               );
               if (root != null)
                  this.Archive.RestoreIndex.InsertPathMap(
                     new Restore.PathMap()
                     {
                        Session = session,
                        NodeID = root.ID,
                        Path = pathMap.Value
                     }
                  );
            }
            foreach (Int32 backupEntryID in this.Request.Entries)
            {
               Backup.Entry backupEntry = this.Archive.BackupIndex.FetchEntry(backupEntryID);
               if (this.Request.Filter.Evaluate(backupEntry.Node.GetAbsolutePath()))
               {
                  Restore.Entry restoreEntry = null;
                  switch (backupEntry.State)
                  {
                     case Backup.EntryState.Completed:
                        Restore.Retrieval retrieval =
                           this.Archive.RestoreIndex
                              .ListBlobRetrievals(session, backupEntry.Blob.Name)
                              .FirstOrDefault() ??
                           this.Archive.RestoreIndex.InsertRetrieval(
                              new Restore.Retrieval()
                              {
                                 Session = session,
                                 Blob = backupEntry.Blob.Name,
                                 Offset = 0,
                                 Length = backupEntry.Blob.Length
                              }
                           );
                        restoreEntry = this.Archive.RestoreIndex.InsertEntry(
                          new Restore.Entry()
                           {
                              BackupEntryID = backupEntry.ID,
                              Session = session,
                              Retrieval = retrieval,
                              State = Restore.EntryState.Pending,
                              Offset = backupEntry.Offset,
                              Length = backupEntry.Length
                           }
                        );
                        session.TotalLength += backupEntry.Length;
                        break;
                     case Backup.EntryState.Deleted:
                        restoreEntry = this.Archive.RestoreIndex.InsertEntry(
                           new Restore.Entry()
                           {
                              BackupEntryID = backupEntry.ID,
                              Session = session,
                              State = Restore.EntryState.Pending,
                              Offset = -1,
                              Length = 0
                           }
                        );
                        break;
                  }
                  if (restoreEntry != null)
                     ReportProgress(
                        new Engine.ProgressEventArgs()
                        {
                           Action = "CreateRestoreEntry",
                           BackupSession = backupEntry.Session,
                           BackupEntry = backupEntry,
                           RestoreSession = session,
                           RestoreEntry = restoreEntry
                        }
                     );
               }
            }
            this.Archive.RestoreIndex.UpdateSession(session);
            txn.Complete();
            this.Session = session;
         }
      }
   }
}
