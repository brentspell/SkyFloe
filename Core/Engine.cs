using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Transactions;

namespace SkyFloe
{
   public class Engine : IDisposable
   {
      private const Int32 CryptoHashLength = 256;
      private const Int32 CryptoSaltLength = 128;
      private const Int32 CryptoIterations = 1000;
      private Store.IArchive archive;
      private AesCryptoServiceProvider aes;

      public event Action<ProgressEvent> OnProgress;
      public event Action<ErrorEvent> OnError;

      public void Dispose ()
      {
         if (this.archive != null)
            this.archive.Dispose();
         if (this.aes != null)
            this.aes.Dispose();
         this.archive = null;
         this.aes = null;
      }

      public Connection Connection
      {
         get; set; 
      }

      public void CreateArchive (String name, String password)
      {
         Store.IStore store = this.Connection.Store;
         try
         {
            if (store.ListArchives().Contains(name, StringComparer.OrdinalIgnoreCase))
               throw new InvalidOperationException("TODO: archive exists");
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            Backup.Header header = new Backup.Header()
            {
               CryptoIterations = Engine.CryptoIterations,
               ArchiveSalt = new Byte[CryptoSaltLength],
               PasswordSalt = new Byte[CryptoSaltLength]
            };
            rng.GetBytes(header.ArchiveSalt);
            rng.GetBytes(header.PasswordSalt);
            using (Rfc2898DeriveBytes crypto =
               new Rfc2898DeriveBytes(
                  password,
                  header.PasswordSalt,
                  header.CryptoIterations
               )
            )
               header.PasswordHash = crypto.GetBytes(CryptoHashLength);
            this.aes = new AesCryptoServiceProvider();
            using (Rfc2898DeriveBytes crypto =
               new Rfc2898DeriveBytes(
                  password,
                  header.ArchiveSalt,
                  header.CryptoIterations
               )
            )
            {
               this.aes.Key = crypto.GetBytes(this.aes.KeySize / 8);
               this.aes.IV = header.ArchiveSalt.Take(this.aes.BlockSize / 8).ToArray();
            }
            this.archive = store.CreateArchive(name, header);
         }
         catch
         {
            if (this.archive != null)
               this.archive.Dispose();
            if (this.aes != null)
               this.aes.Dispose();
            this.archive = null;
            this.aes = null;
            throw;
         }
      }
      public void OpenArchive (String name, String password)
      {
         Store.IStore store = this.Connection.Store;
         try
         {
            if (!store.ListArchives().Contains(name, StringComparer.OrdinalIgnoreCase))
               throw new InvalidOperationException("TODO: archive not found");
            this.archive = store.OpenArchive(name);
            Backup.Header header = this.archive.BackupIndex.FetchHeader();
            using (Rfc2898DeriveBytes crypto =
               new Rfc2898DeriveBytes(
                  password,
                  header.PasswordSalt,
                  header.CryptoIterations
               )
            )
            {
               Byte[] hash = crypto.GetBytes(header.PasswordHash.Length);
               if (!hash.SequenceEqual(header.PasswordHash))
                  throw new InvalidOperationException("TODO: authentication failed");
            }
            this.aes = new AesCryptoServiceProvider();
            using (Rfc2898DeriveBytes crypto =
               new Rfc2898DeriveBytes(
                  password,
                  header.ArchiveSalt,
                  header.CryptoIterations
               )
            )
            {
               this.aes.Key = crypto.GetBytes(this.aes.KeySize / 8);
               this.aes.IV = header.ArchiveSalt.Take(this.aes.BlockSize / 8).ToArray();
            }
         }
         catch
         {
            if (this.archive != null)
               this.archive.Dispose();
            if (this.aes != null)
               this.aes.Dispose();
            this.archive = null;
            this.aes = null;
            throw;
         }
      }
      public void DeleteArchive (String archive)
      {
         this.Connection.Store.DeleteArchive(archive);
      }

      public void DeleteRestore (Restore.Session session)
      {
         this.archive.RestoreIndex.DeleteSession(session);
      }

      public Backup.Session CreateBackup (BackupRequest request)
      {
         // TODO: validate request
         if (this.archive == null)
            throw new InvalidOperationException("TODO: Not connected");
         Backup.Header header = this.archive.BackupIndex.FetchHeader();
         if (this.archive.BackupIndex.ListSessions().Any(s => s.State != Backup.SessionState.Completed))
            throw new InvalidOperationException("TODO: session already exists");
         Backup.Session session = this.archive.BackupIndex.InsertSession(
            new Backup.Session()
            {
               State = Backup.SessionState.Pending
            }
         );
         foreach (String source in request.Sources)
         {
            // TODO: validate source is directory
            using (TransactionScope txn = new TransactionScope(TransactionScopeOption.Required, TimeSpan.MaxValue))
            {
               Backup.Node root =
                  this.archive.BackupIndex
                     .ListNodes(null)
                     .FirstOrDefault(n => String.Compare(n.Name, source, true) == 0) ??
                  this.archive.BackupIndex.InsertNode(
                     new Backup.Node()
                     {
                        Type = Backup.NodeType.Root,
                        Name = source
                     }
                  );
               Differencer differencer = new Differencer()
               {
                  Method = request.DiffMethod,
                  Index = this.archive.BackupIndex,
                  Root = root,
                  Path = source
               };
               foreach (Differencer.Diff diff in differencer.Enumerate())
               {
                  if (diff.Node.ID == 0)
                     this.archive.BackupIndex.InsertNode(diff.Node);
                  if (diff.Node.Type == Backup.NodeType.File)
                  {
                     this.archive.BackupIndex.InsertEntry(
                        new Backup.Entry()
                        {
                           Session = session,
                           Node = diff.Node,
                           State = (diff.Type != DiffType.Deleted) ?
                              Backup.EntryState.Pending :
                              Backup.EntryState.Deleted,
                           Offset = -1,
                           Length = -1,
                           Crc32 = IO.Crc32Stream.InitialValue
                        }
                     );
                     if (diff.Type != DiffType.Deleted)
                        session.EstimatedLength += new FileInfo(diff.Node.GetAbsolutePath()).Length;
                  }
               }
               txn.Complete();
            }
         }
         return session;
      }

      public void StartBackup (Backup.Session session)
      {
         // TODO: validate request
         if (this.archive == null)
            throw new InvalidOperationException("TODO: Not connected");
         if (session.State == Backup.SessionState.Completed)
            throw new InvalidOperationException("TODO: session already completed");
         Store.IBackup backup = this.archive.PrepareBackup(session);
         try
         {
            if (session.State == Backup.SessionState.Pending)
            {
               session.State = Backup.SessionState.InProgress;
               this.archive.BackupIndex.UpdateSession(session);
               if (this.OnProgress != null)
               {
                  ProgressEvent progress = new ProgressEvent()
                  {
                     Type = EventType.BeginBackupCheckpoint,
                     BackupSession = session
                  };
                  this.OnProgress(progress);
                  if (progress.Cancel)
                     return;  // TODO: fix
               }
               backup.Checkpoint();
               if (this.OnProgress != null)
               {
                  ProgressEvent progress = new ProgressEvent()
                  {
                     Type = EventType.EndBackupCheckpoint,
                     BackupSession = session
                  };
                  this.OnProgress(progress);
                  if (progress.Cancel)
                     return;  // TODO: fix
               }
            }
            Backup.Header header = this.archive.BackupIndex.FetchHeader();
            Int64 checkpointSize = 0;
            for (; ; )
            {
               Backup.Entry entry = this.archive.BackupIndex.LookupNextEntry(session);
               if (entry == null)
                  break;
               try
               {
                  entry.Length = new FileInfo(entry.Node.GetAbsolutePath()).Length;
                  if (this.OnProgress != null)
                  {
                     ProgressEvent progress = new ProgressEvent()
                     {
                        Type = EventType.BeginBackupEntry,
                        BackupSession = session,
                        BackupEntry = entry
                     };
                     this.OnProgress(progress);
                     if (progress.Cancel)
                        break;
                  }
                  using (Stream fileStream = IO.FileSystem.Open(entry.Node.GetAbsolutePath()))
                  using (IO.Crc32Stream crcStream = new IO.Crc32Stream(fileStream, IO.StreamMode.Read))
                  using (Stream cryptoStream = new CryptoStream(crcStream, this.aes.CreateEncryptor(), CryptoStreamMode.Read))
                  {
                     backup.Backup(entry, cryptoStream);
                     entry.Crc32 = crcStream.Value;
                  }
               }
               catch (Exception e)
               {
                  ErrorEvent error = new ErrorEvent()
                  {
                     Type = EventType.BeginBackupEntry,
                     BackupSession = session,
                     BackupEntry = entry,
                     Exception = e,
                     Result = ErrorResult.Abort
                  };
                  try
                  {
                     if (this.OnError != null)
                        this.OnError(error);
                  }
                  catch { }
                  switch (error.Result)
                  {
                     case ErrorResult.Abort:
                        throw;
                     case ErrorResult.Retry:
                        continue;
                     case ErrorResult.Fail:
                        entry = this.archive.BackupIndex.FetchEntry(entry.ID);
                        entry.State = Backup.EntryState.Failed;
                        this.archive.BackupIndex.UpdateEntry(entry);
                        continue;
                  }
               }
               entry.State = Backup.EntryState.Completed;
               entry.Blob.Length += entry.Length;
               session.ActualLength += entry.Length;
               using (TransactionScope txn = new TransactionScope())
               {
                  this.archive.BackupIndex.UpdateEntry(entry);
                  this.archive.BackupIndex.UpdateBlob(entry.Blob);
                  this.archive.BackupIndex.UpdateSession(session);
                  txn.Complete();
               }
               if (this.OnProgress != null)
               {
                  ProgressEvent progress = new ProgressEvent()
                  {
                     Type = EventType.EndBackupEntry,
                     BackupSession = session,
                     BackupEntry = entry
                  };
                  this.OnProgress(progress);
                  if (progress.Cancel)
                     break;
               }
               // TODO: set checkpoint size based on configuration/request
               checkpointSize += entry.Length;
               if (checkpointSize > 1024 * 1024 * 1024)
               {
                  checkpointSize = 0;
                  if (this.OnProgress != null)
                  {
                     ProgressEvent progress = new ProgressEvent()
                     {
                        Type = EventType.BeginBackupCheckpoint,
                        BackupSession = session
                     };
                     this.OnProgress(progress);
                     if (progress.Cancel)
                        return;  // TODO: fix
                  }
                  try
                  {
                     backup.Checkpoint();
                  }
                  catch (Exception e)
                  {
                     ErrorEvent error = new ErrorEvent()
                     {
                        Type = EventType.BeginBackupCheckpoint,
                        BackupSession = session,
                        Exception = e,
                        Result = ErrorResult.Abort
                     };
                     try
                     {
                        if (this.OnError != null)
                           this.OnError(error);
                     }
                     catch { }
                     switch (error.Result)
                     {
                        case ErrorResult.Retry:
                           String name = this.archive.Name;
                           try { this.archive.Dispose(); }
                           catch { }
                           this.archive = this.Connection.Store.OpenArchive(name);
                           backup = this.archive.PrepareBackup(session);
                           session = this.archive.BackupIndex.FetchSession(session.ID);
                           continue;
                        default:
                           throw;
                     }
                  }
                  if (this.OnProgress != null)
                  {
                     ProgressEvent progress = new ProgressEvent()
                     {
                        Type = EventType.EndBackupCheckpoint,
                        BackupSession = session
                     };
                     this.OnProgress(progress);
                     if (progress.Cancel)
                        return;  // TODO: fix
                  }
               }
            }
            if (this.archive.BackupIndex.LookupNextEntry(session) == null)
            {
               session.State = Backup.SessionState.Completed;
               this.archive.BackupIndex.UpdateSession(session);
            }
            if (this.OnProgress != null)
            {
               ProgressEvent progress = new ProgressEvent()
               {
                  Type = EventType.BeginBackupCheckpoint,
                  BackupSession = session
               };
               this.OnProgress(progress);
            }
            // TODO: retry on final checkpoint failure
            backup.Checkpoint();
            if (this.OnProgress != null)
            {
               ProgressEvent progress = new ProgressEvent()
               {
                  Type = EventType.EndBackupCheckpoint,
                  BackupSession = session
               };
               this.OnProgress(progress);
            }
         }
         finally
         {
            if (backup != null)
               backup.Dispose();
         }
      }

      public Restore.Session CreateRestore (RestoreRequest request)
      {
         // TODO: validate request
         if (this.archive == null)
            throw new InvalidOperationException("TODO: Not connected");
         Restore.Session session = this.archive.RestoreIndex.InsertSession(
            new Restore.Session()
            {
               State = Restore.SessionState.Pending,
               Flags = 
                  ((request.SkipExisting) ? Restore.SessionFlags.SkipExisting : 0) | 
                  ((request.SkipReadOnly) ? Restore.SessionFlags.SkipReadOnly : 0) | 
                  ((request.VerifyResults) ? Restore.SessionFlags.VerifyResults : 0) | 
                  ((request.EnableDeletes) ? Restore.SessionFlags.EnableDeletes : 0)
            }
         );
         using (TransactionScope txn = new TransactionScope(TransactionScopeOption.Required, TimeSpan.MaxValue))
         {
            IEnumerable<Backup.Node> roots = this.archive.BackupIndex.ListNodes(null);
            foreach (KeyValuePair<String, String> pathMap in request.RootPathMap)
            {
               Backup.Node root = roots.FirstOrDefault(
                  n => String.Compare(n.Name, pathMap.Key, true) == 0
               );
               if (root != null)
                  this.archive.RestoreIndex.InsertPathMap(
                     new Restore.PathMap()
                     {
                        Session = session,
                        NodeID = root.ID,
                        Path = pathMap.Value
                     }
                  );
            }
            foreach (Int32 backupEntryID in request.Entries)
            {
               Backup.Entry backupEntry = this.archive.BackupIndex.FetchEntry(backupEntryID);
               switch (backupEntry.State)
               {
                  case Backup.EntryState.Completed:
                     Restore.Retrieval retrieval =
                        this.archive.RestoreIndex
                           .ListBlobRetrievals(session, backupEntry.Blob.Name)
                           .FirstOrDefault() ??
                        this.archive.RestoreIndex.InsertRetrieval(
                           new Restore.Retrieval()
                           {
                              Session = session,
                              Blob = backupEntry.Blob.Name,
                              Offset = 0,
                              Length = backupEntry.Blob.Length
                           }
                        );
                     this.archive.RestoreIndex.InsertEntry(
                        new Restore.Entry()
                        {
                           BackupEntryID = backupEntry.ID,
                           Retrieval = retrieval,
                           State = Restore.EntryState.Pending,
                           Offset = backupEntry.Offset,
                           Length = backupEntry.Length
                        }
                     );
                     session.TotalLength += backupEntry.Length;
                     break;
                  case Backup.EntryState.Deleted:
                     // TODO: implement
                     break;
               }
            }
            this.archive.RestoreIndex.UpdateSession(session);
            txn.Complete();
            return session;
         }
      }

      public void StartRestore (Restore.Session session)
      {
         // TODO: validate request
         if (this.archive == null)
            throw new InvalidOperationException("TODO: Not connected");
         if (session.State == Restore.SessionState.Completed)
            throw new InvalidOperationException("TODO: session completed");
         Store.IRestore restore = null;
         try
         {
            using (TransactionScope txn = new TransactionScope(TransactionScopeOption.Required, TimeSpan.MaxValue))
            {
               restore = this.archive.PrepareRestore(session);
               if (session.State == Restore.SessionState.Pending)
               {
                  session.State = Restore.SessionState.InProgress;
                  this.archive.RestoreIndex.UpdateSession(session);
               }
               txn.Complete();
            }
            for (; ; )
            {
               Restore.Entry restoreEntry = this.archive.RestoreIndex.LookupNextEntry(session);
               if (restoreEntry == null)
                  break;
               Backup.Entry backupEntry = this.archive.BackupIndex.FetchEntry(restoreEntry.BackupEntryID);
               Backup.Node rootNode = backupEntry.Node.GetRoot();
               Restore.PathMap rootMap = this.archive.RestoreIndex.LookupPathMap(session, rootNode.ID);
               String rootPath = (rootMap != null) ? rootMap.Path : rootNode.Name;
               String path = Path.Combine(rootPath, backupEntry.Node.GetRelativePath());
               Directory.CreateDirectory(Path.GetDirectoryName(path));
               FileInfo fileInfo = new FileInfo(path);
               Boolean restoreFile = true;
               if (fileInfo.Exists)
               {
                  if (session.SkipExisting && fileInfo.Length > 0)
                     restoreFile = false;
                  else if (fileInfo.Attributes.HasFlag(FileAttributes.ReadOnly))
                     if (session.SkipReadOnly)
                        restoreFile = false;
                     else
                        fileInfo.Attributes &= ~FileAttributes.ReadOnly;
               }
               if (this.OnProgress != null)
               {
                  ProgressEvent progress = new ProgressEvent()
                  {
                     Type = EventType.BeginRestoreEntry,
                     BackupSession = backupEntry.Session,
                     BackupEntry = backupEntry,
                     RestoreSession = session,
                     RestoreEntry = restoreEntry
                  };
                  this.OnProgress(progress);
                  if (progress.Cancel)
                     break;
               }
               if (restoreFile)
               {
                  try
                  {
                     // TODO: fault tolerance
                     using (Stream fileStream = IO.FileSystem.Truncate(path))
                     using (IO.Crc32Stream crcStream = new IO.Crc32Stream(fileStream, IO.StreamMode.Write))
                     using (Stream archiveStream = restore.Restore(restoreEntry))
                     using (Stream cryptoStream = new CryptoStream(archiveStream, this.aes.CreateDecryptor(), CryptoStreamMode.Read))
                     {
                        cryptoStream.CopyTo(crcStream);
                        if (session.VerifyResults && crcStream.Value != backupEntry.Crc32)
                           throw new InvalidOperationException("TODO: CRC does not match");
                     }
                  }
                  catch (Exception e)
                  {
                     try { File.Delete(path); }
                     catch { }
                     ErrorEvent error = new ErrorEvent()
                     {
                        Type = EventType.BeginRestoreEntry,
                        BackupSession = backupEntry.Session,
                        BackupEntry = backupEntry,
                        RestoreSession = session,
                        RestoreEntry = restoreEntry,
                        Exception = e,
                        Result = ErrorResult.Abort
                     };
                     try
                     {
                        if (this.OnError != null)
                           this.OnError(error);
                     }
                     catch { }
                     switch (error.Result)
                     {
                        case ErrorResult.Abort:
                           throw;
                        case ErrorResult.Retry:
                           continue;
                        case ErrorResult.Fail:
                           restoreEntry = this.archive.RestoreIndex.FetchEntry(restoreEntry.ID);
                           restoreEntry.State = Restore.EntryState.Failed;
                           this.archive.RestoreIndex.UpdateEntry(restoreEntry);
                           continue;
                     }
                  }
               }
               using (TransactionScope txn = new TransactionScope())
               {
                  restoreEntry.State = Restore.EntryState.Completed;
                  // TODO: don't set restore length for deletes + look for other cases
                  session.RestoreLength += restoreEntry.Length;
                  this.archive.RestoreIndex.UpdateEntry(restoreEntry);
                  this.archive.RestoreIndex.UpdateSession(session);
                  txn.Complete();
               }
               if (this.OnProgress != null)
               {
                  ProgressEvent progress = new ProgressEvent()
                  {
                     Type = EventType.EndRestoreEntry,
                     BackupSession = backupEntry.Session,
                     BackupEntry = backupEntry,
                     RestoreSession = session,
                     RestoreEntry = restoreEntry
                  };
                  this.OnProgress(progress);
                  if (progress.Cancel)
                     break;
               }
            }
            if (this.archive.RestoreIndex.LookupNextEntry(session) == null)
            {
               session.State = Restore.SessionState.Completed;
               this.archive.RestoreIndex.UpdateSession(session);
            }
         }
         finally
         {
            if (restore != null)
               restore.Dispose();
         }
      }

      public DiffResult Difference (DiffRequest request)
      {
         // TODO: validate request
         if (this.archive == null)
            throw new InvalidOperationException("TODO: Not connected");
         // load the archive/index
         Store.IStore store = this.Connection.Store;
         try
         {
            DiffResult result = new DiffResult()
            {
               Entries = new List<Differencer.Diff>()
            };
            foreach (Backup.Node root in this.archive.BackupIndex.ListNodes(null))
            {
               String path = root.Name;
               String mapPath = null;
               if (request.RootPathMap.TryGetValue(path, out mapPath))
                  path = mapPath;
               result.Entries.AddRange(
                  new Differencer()
                  {
                     Method = request.Method,
                     Index = this.archive.BackupIndex,
                     Root = root,
                     Path = path
                  }.Enumerate()
               );
            }
            return result;
         }
         finally
         {
            archive.Dispose();
         }
      }

      public enum EventType
      {
         BeginBackupEntry,
         EndBackupEntry,
         BeginBackupCheckpoint,
         EndBackupCheckpoint,
         BeginRestoreEntry,
         EndRestoreEntry
      }
      public enum ErrorResult
      {
         Abort = 1,
         Retry = 2,
         Fail = 3
      }
      public class ErrorEvent
      {
         public EventType Type { get; set; }
         public Backup.Session BackupSession { get; set; }
         public Backup.Entry BackupEntry { get; set; }
         public Restore.Session RestoreSession { get; set; }
         public Restore.Entry RestoreEntry { get; set; }
         public Exception Exception { get; set; }
         public ErrorResult Result { get; set; }
      }

      public class ProgressEvent
      {
         public EventType Type { get; set; }
         public Backup.Session BackupSession { get; set; }
         public Backup.Entry BackupEntry { get; set; }
         public Restore.Session RestoreSession { get; set; }
         public Restore.Entry RestoreEntry { get; set; }
         public Boolean Cancel { get; set; }
      }
   }
}
