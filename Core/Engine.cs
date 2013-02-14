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
      private Store.IBackup backup;
      private Backup.Session backupSession;
      private IO.RateLimiter limiter;

      public event Action<ProgressEvent> OnProgress;
      public event Func<ErrorEvent, ErrorResult> OnError;

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

      #region Archive Management
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
      public void DeleteArchive (String name)
      {
         this.Connection.Store.DeleteArchive(name);
      }
      #endregion

      #region Backup
      public Backup.Session CreateBackup (BackupRequest request)
      {
         // TODO: validate request
         if (this.archive == null)
            throw new InvalidOperationException("TODO: Not connected");
         Backup.Header header = this.archive.BackupIndex.FetchHeader();
         if (this.archive.BackupIndex.ListSessions().Any(s => s.State != Backup.SessionState.Completed))
            throw new InvalidOperationException("TODO: session already exists");
         using (TransactionScope txn = new TransactionScope(TransactionScopeOption.Required, TimeSpan.MaxValue))
         {
            Backup.Session session = this.archive.BackupIndex.InsertSession(
               new Backup.Session()
               {
                  State = Backup.SessionState.Pending,
                  CheckpointLength = request.CheckpointLength,
                  RateLimit = request.RateLimit
               }
            );
            foreach (String source in request.Sources)
            {
               // TODO: validate source is directory
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
                     Backup.Entry entry = this.archive.BackupIndex.InsertEntry(
                        new Backup.Entry()
                        {
                           Session = session,
                           Node = diff.Node,
                           State = (diff.Type != DiffType.Deleted) ?
                              Backup.EntryState.Pending :
                              Backup.EntryState.Deleted,
                           Offset = -1,
                           Length = IO.FileSystem.GetMetadata(diff.Node.GetAbsolutePath()).Length,
                           Crc32 = IO.Crc32Stream.InitialValue
                        }
                     );
                     session.EstimatedLength += entry.Length;
                  }
               }
            }
            txn.Complete();
            return session;
         }
      }

      public void StartBackup (Backup.Session session)
      {
         // TODO: validate request
         if (this.archive == null)
            throw new InvalidOperationException("TODO: Not connected");
         if (session.State == Backup.SessionState.Completed)
            throw new InvalidOperationException("TODO: session already completed");
         try
         {
            this.backup = this.archive.PrepareBackup(session);
            this.backupSession = session;
            if (this.backupSession.State == Backup.SessionState.Pending)
            {
               this.backupSession.State = Backup.SessionState.InProgress;
               this.archive.BackupIndex.UpdateSession(this.backupSession);
               Checkpoint();
            }
            this.limiter = new IO.RateLimiter(this.backupSession.RateLimit);
            Backup.Header header = this.archive.BackupIndex.FetchHeader();
            Int64 checkpointSize = 0;
            for (; ; )
            {
               Backup.Entry entry = this.archive.BackupIndex.LookupNextEntry(this.backupSession);
               if (entry == null)
                  break;
               BackupEntry(entry);
               checkpointSize += entry.Length;
               if (checkpointSize > this.backupSession.CheckpointLength)
               {
                  checkpointSize = 0;
                  Checkpoint();
               }
            }
            if (this.archive.BackupIndex.LookupNextEntry(this.backupSession) == null)
            {
               this.backupSession.State = Backup.SessionState.Completed;
               this.archive.BackupIndex.UpdateSession(this.backupSession);
            }
            Checkpoint();
         }
         finally
         {
            if (backup != null)
               backup.Dispose();
            this.backup = null;
            this.backupSession = null;
            this.limiter = null;
         }
      }
      private void BackupEntry (Backup.Entry entry)
      {
         if (this.OnProgress != null)
            this.OnProgress(
               new ProgressEvent()
               {
                  Type = EventType.BeginBackupEntry,
                  BackupSession = this.backupSession,
                  BackupEntry = entry
               }
            );
         try
         {
            using (Stream fileStream = IO.FileSystem.Open(entry.Node.GetAbsolutePath()))
            using (IO.Crc32Stream crcStream = new IO.Crc32Stream(fileStream, IO.StreamMode.Read))
            using (Stream cryptoStream = new CryptoStream(crcStream, this.aes.CreateEncryptor(), CryptoStreamMode.Read))
            using (Stream limiterStream = this.limiter.CreateStream(cryptoStream, IO.StreamMode.Read))
            {
               this.backup.Backup(entry, limiterStream);
               entry.Crc32 = crcStream.Value;
            }
            entry.State = Backup.EntryState.Completed;
            entry.Blob.Length += entry.Length;
            this.backupSession.ActualLength += entry.Length;
            using (TransactionScope txn = new TransactionScope())
            {
               this.archive.BackupIndex.UpdateEntry(entry);
               this.archive.BackupIndex.UpdateBlob(entry.Blob);
               this.archive.BackupIndex.UpdateSession(this.backupSession);
               txn.Complete();
            }
         }
         catch (Exception e)
         {
            ErrorResult result = ErrorResult.Abort;
            if (this.OnError != null)
               result = this.OnError(
                  new ErrorEvent()
                  {
                     Type = EventType.BeginBackupEntry,
                     BackupSession = this.backupSession,
                     BackupEntry = entry,
                     Exception = e
                  }
               );
            switch (result)
            {
               case ErrorResult.Retry:
                  break;
               case ErrorResult.Fail:
                  entry = this.archive.BackupIndex.FetchEntry(entry.ID);
                  entry.State = Backup.EntryState.Failed;
                  this.archive.BackupIndex.UpdateEntry(entry);
                  break;
               default:
                  throw;
            }
         }
         if (this.OnProgress != null && entry.State == Backup.EntryState.Completed)
            this.OnProgress(
               new ProgressEvent()
               {
                  Type = EventType.EndBackupEntry,
                  BackupSession = this.backupSession,
                  BackupEntry = entry
               }
            );
      }
      private void Checkpoint ()
      {
         if (this.OnProgress != null)
            this.OnProgress(
               new ProgressEvent()
               {
                  Type = EventType.BeginBackupCheckpoint,
                  BackupSession = this.backupSession
               }
            );
         for (; ; )
         {
            try
            {
               this.backup.Checkpoint();
               break;
            }
            catch (Exception e)
            {
               ErrorResult result = ErrorResult.Abort;
               if (this.OnError != null)
                  result = this.OnError(
                     new ErrorEvent()
                     {
                        Type = EventType.BeginBackupCheckpoint,
                        BackupSession = this.backupSession,
                        Exception = e
                     }
                  );
               switch (result)
               {
                  case ErrorResult.Retry:
                     String name = this.archive.Name;
                     try { this.archive.Dispose(); } catch { }
                     try { this.backup.Dispose(); } catch { }
                     this.archive = this.Connection.Store.OpenArchive(name);
                     this.backup = this.archive.PrepareBackup(this.backupSession);
                     this.backupSession = this.archive.BackupIndex.FetchSession(this.backupSession.ID);
                     break;
                  default:
                     throw;
               }
            }
         }
         if (this.OnProgress != null)
            this.OnProgress(
               new ProgressEvent()
               {
                  Type = EventType.EndBackupCheckpoint,
                  BackupSession = this.backupSession
               }
            );
      }
      #endregion

      #region Restore
      public Restore.Session CreateRestore (RestoreRequest request)
      {
         // TODO: validate request
         if (this.archive == null)
            throw new InvalidOperationException("TODO: Not connected");
         using (TransactionScope txn = new TransactionScope(TransactionScopeOption.Required, TimeSpan.MaxValue))
         {
            Restore.Session session = this.archive.RestoreIndex.InsertSession(
               new Restore.Session()
               {
                  State = Restore.SessionState.Pending,
                  Flags =
                     ((request.SkipExisting) ? Restore.SessionFlags.SkipExisting : 0) |
                     ((request.SkipReadOnly) ? Restore.SessionFlags.SkipReadOnly : 0) |
                     ((request.VerifyResults) ? Restore.SessionFlags.VerifyResults : 0) |
                     ((request.EnableDeletes) ? Restore.SessionFlags.EnableDeletes : 0),
                  RateLimit = request.RateLimit
               }
            );
            IEnumerable<Backup.Node> roots = this.archive.BackupIndex.ListNodes(null);
            foreach (KeyValuePair<IO.Path, IO.Path> pathMap in request.RootPathMap)
            {
               Backup.Node root = roots.FirstOrDefault(
                  n => n.Name == pathMap.Key
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
                     this.archive.RestoreIndex.InsertEntry(
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
            this.limiter = new IO.RateLimiter(session.RateLimit);
            for (; ; )
            {
               Restore.Entry restoreEntry = this.archive.RestoreIndex.LookupNextEntry(session);
               if (restoreEntry == null)
                  break;
               Backup.Entry backupEntry = this.archive.BackupIndex.FetchEntry(restoreEntry.BackupEntryID);
               Backup.Node rootNode = backupEntry.Node.GetRoot();
               Restore.PathMap rootMap = this.archive.RestoreIndex.LookupPathMap(session, rootNode.ID);
               IO.Path rootPath = (rootMap != null) ? rootMap.Path : rootNode.Name;
               IO.Path path = rootPath + backupEntry.Node.GetRelativePath();
               IO.FileSystem.CreateDirectory(path.Parent);
               IO.FileSystem.Metadata metadata = IO.FileSystem.GetMetadata(path);
               // TODO: cleanup restore/delete logic
               Boolean restoreFile = true;
               if (metadata.Exists)
               {
                  if (session.SkipExisting && metadata.Length > 0)
                     restoreFile = false;
                  else if (metadata.IsReadOnly)
                     if (session.SkipReadOnly)
                        restoreFile = false;
                     else
                        IO.FileSystem.MakeWritable(path);
               }
               else if (backupEntry.State == Backup.EntryState.Deleted)
                  restoreFile = false;
               if (this.OnProgress != null)
                  this.OnProgress(
                     new ProgressEvent()
                     {
                        Type = EventType.BeginRestoreEntry,
                        BackupSession = backupEntry.Session,
                        BackupEntry = backupEntry,
                        RestoreSession = session,
                        RestoreEntry = restoreEntry
                     }
                  );
               if (restoreFile)
               {
                  try
                  {
                     if (backupEntry.State != Backup.EntryState.Deleted)
                     {
                        using (Stream fileStream = IO.FileSystem.Truncate(path))
                        using (IO.Crc32Stream crcStream = new IO.Crc32Stream(fileStream, IO.StreamMode.Write))
                        using (Stream limiterStream = this.limiter.CreateStream(crcStream, IO.StreamMode.Write))
                        using (Stream archiveStream = restore.Restore(restoreEntry))
                        using (Stream cryptoStream = new CryptoStream(archiveStream, this.aes.CreateDecryptor(), CryptoStreamMode.Read))
                        {
                           cryptoStream.CopyTo(limiterStream);
                           limiterStream.Flush();
                           if (session.VerifyResults && crcStream.Value != backupEntry.Crc32)
                              throw new InvalidOperationException("TODO: CRC does not match");
                        }
                     }
                     else if (session.EnableDeletes)
                     {
                        IO.FileSystem.Delete(path);
                     }
                  }
                  catch (Exception e)
                  {
                     try { IO.FileSystem.Delete(path); } catch { }
                     ErrorResult result = ErrorResult.Abort;
                     if (this.OnError != null)
                        result = this.OnError(
                           new ErrorEvent()
                           {
                              Type = EventType.BeginRestoreEntry,
                              BackupSession = backupEntry.Session,
                              BackupEntry = backupEntry,
                              RestoreSession = session,
                              RestoreEntry = restoreEntry,
                              Exception = e
                           }
                        );
                     switch (result)
                     {
                        case ErrorResult.Retry:
                           continue;
                        case ErrorResult.Fail:
                           restoreEntry = this.archive.RestoreIndex.FetchEntry(restoreEntry.ID);
                           restoreEntry.State = Restore.EntryState.Failed;
                           this.archive.RestoreIndex.UpdateEntry(restoreEntry);
                           continue;
                        default:
                           throw;
                     }
                  }
               }
               using (TransactionScope txn = new TransactionScope())
               {
                  restoreEntry.State = Restore.EntryState.Completed;
                  session.RestoreLength += restoreEntry.Length;
                  this.archive.RestoreIndex.UpdateEntry(restoreEntry);
                  this.archive.RestoreIndex.UpdateSession(session);
                  txn.Complete();
               }
               if (this.OnProgress != null)
                  this.OnProgress(
                     new ProgressEvent()
                     {
                        Type = EventType.EndRestoreEntry,
                        BackupSession = backupEntry.Session,
                        BackupEntry = backupEntry,
                        RestoreSession = session,
                        RestoreEntry = restoreEntry
                     }
                  );
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
            this.limiter = null;
         }
      }

      public void DeleteRestore (Restore.Session session)
      {
         this.archive.RestoreIndex.DeleteSession(session);
      }
      #endregion

      #region Difference
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
               IO.Path path = root.Name;
               IO.Path mapPath = null;
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
      #endregion
      
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
      }

      public class ProgressEvent
      {
         public EventType Type { get; set; }
         public Backup.Session BackupSession { get; set; }
         public Backup.Entry BackupEntry { get; set; }
         public Restore.Session RestoreSession { get; set; }
         public Restore.Entry RestoreEntry { get; set; }
      }
   }
}
