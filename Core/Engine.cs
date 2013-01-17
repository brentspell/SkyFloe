using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Transactions;

namespace SkyFloe
{
   public class Engine
   {
      private const Int32 CryptoHashLength = 256;
      private const Int32 CryptoSaltLength = 128;
      private const Int32 CryptoIterations = 1000;

      public event Action<ProgressEvent> OnProgress;
      public event Action<ErrorEvent> OnError;

      public Connection Connection
      {
         get; set; 
      }

      public void DeleteArchive (String archive)
      {
         this.Connection.Store.DeleteArchive(archive);
      }

      public void Backup (BackupRequest request)
      {
         // TODO: validate request
         if (this.Connection == null)
            throw new InvalidOperationException("TODO: Not connected");
         // load the archive/index
         var store = this.Connection.Store;
         var archive = (Store.IArchive)null;
         if (store.ListArchives().Contains(request.Archive, StringComparer.OrdinalIgnoreCase))
         {
            archive = store.OpenArchive(request.Archive);
            try
            {
               var header = archive.Index.FetchHeader();
               using (var crypto =
                  new Rfc2898DeriveBytes(
                     request.Password,
                     header.PasswordSalt,
                     header.CryptoIterations
                  )
               )
               {
                  var hash = crypto.GetBytes(header.PasswordHash.Length);
                  if (!hash.SequenceEqual(header.PasswordHash))
                     throw new InvalidOperationException("TODO: authentication failed");
               }
            }
            catch
            {
               archive.Dispose();
               throw;
            }
         }
         else
         {
            var rng = RandomNumberGenerator.Create();
            var header = new Model.Header()
            {
               CryptoIterations = Engine.CryptoIterations,
               ArchiveSalt = new Byte[CryptoSaltLength],
               PasswordSalt = new Byte[CryptoSaltLength]
            };
            rng.GetBytes(header.ArchiveSalt);
            rng.GetBytes(header.PasswordSalt);
            using (var crypto = 
               new Rfc2898DeriveBytes(
                  request.Password,
                  header.PasswordSalt,
                  header.CryptoIterations
               )
            )
               header.PasswordHash = crypto.GetBytes(CryptoHashLength);
            archive = store.CreateArchive(request.Archive, header);
            try
            {
               archive.Checkpoint();
            }
            catch
            {
               archive.Dispose();
               throw;
            }
         }
         try
         {
            archive.PrepareBackup();
            var session = archive.Index
               .ListSessions()
               .SingleOrDefault(s => s.State != Model.SessionState.Completed);
            if (session == null)
               session = archive.Index.InsertSession(
                  new Model.Session()
                  {
                     State = Model.SessionState.Pending
                  }
               );
            if (session.State == Model.SessionState.Pending)
            {
               foreach (var source in request.Sources)
               {
                  // TODO: validate source is directory
                  using (var txn = new TransactionScope(TransactionScopeOption.Required, TimeSpan.MaxValue))
                  {
                     var root = archive.Index
                        .ListNodes(null)
                        .FirstOrDefault(n => String.Compare(n.Name, source, true) == 0);
                     if (root == null)
                        root = archive.Index.InsertNode(
                           new Model.Node()
                           {
                              Type = Model.NodeType.Root,
                              Name = source
                           }
                        );
                     var differencer = new Differencer()
                     {
                        Method = request.DiffMethod,
                        Index = archive.Index,
                        Root = root,
                        Path = source
                     };
                     foreach (var diff in differencer.Enumerate())
                     {
                        if (diff.Node.ID == 0)
                           archive.Index.InsertNode(diff.Node);
                        if (diff.Node.Type == Model.NodeType.File)
                        {
                           archive.Index.InsertEntry(
                              new Model.Entry()
                              {
                                 Session = session,
                                 Node = diff.Node,
                                 State = (diff.Type != DiffType.Deleted) ?
                                    Model.EntryState.Pending :
                                    Model.EntryState.Deleted,
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
               session.State = Model.SessionState.InProgress;
               archive.Index.UpdateSession(session);
               archive.Checkpoint();
            }
            var header = archive.Index.FetchHeader();
            var aes = new AesCryptoServiceProvider();
            using (var crypto =
               new Rfc2898DeriveBytes(
                  request.Password,
                  header.ArchiveSalt,
                  header.CryptoIterations
               )
            )
            {
               aes.Key = crypto.GetBytes(aes.KeySize / 8);
               aes.IV = header.ArchiveSalt.Take(aes.BlockSize / 8).ToArray();
            }
            var checkpointSize = 0L;
            for (; ; )
            {
               var entry = archive.Index.LookupEntry(session, Model.EntryState.Pending);
               if (entry == null)
                  break;
               try
               {
                  using (var fileStream = new FileStream(entry.Node.GetAbsolutePath(), FileMode.Open, FileAccess.Read, FileShare.Read))
                  using (var crcStream = new IO.Crc32Stream(fileStream, IO.StreamMode.Read))
                  using (var cryptoStream = new CryptoStream(crcStream, aes.CreateEncryptor(), CryptoStreamMode.Read))
                  {
                     archive.BackupEntry(entry, cryptoStream);
                     entry.Crc32 = crcStream.Value;
                  }
               }
               catch (Exception e)
               {
                  ErrorEvent error = new ErrorEvent()
                  {
                     Entry = entry,
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
                        entry = archive.Index.FetchEntry(entry.ID);
                        entry.State = Model.EntryState.Failed;
                        archive.Index.UpdateEntry(entry);
                        continue;
                  }
               }
               entry.State = Model.EntryState.Completed;
               entry.Blob.Length += entry.Length;
               session.ActualLength += entry.Length;
               using (var txn = new TransactionScope())
               {
                  archive.Index.UpdateEntry(entry);
                  archive.Index.UpdateBlob(entry.Blob);
                  archive.Index.UpdateSession(session);
                  txn.Complete();
               }
               if (this.OnProgress != null)
               {
                  var progress = new ProgressEvent()
                  {
                     Entry = entry
                  };
                  this.OnProgress(progress);
                  if (progress.Cancel)
                  {
                     archive.Checkpoint();
                     break;
                  }
               }
               // TODO: set checkpoint size based on configuration/request
               checkpointSize += entry.Length;
               if (checkpointSize > 1024 * 1024 * 1024)
               {
                  checkpointSize = 0;
                  try
                  {
                     archive.Checkpoint();
                  }
                  catch (Exception e)
                  {
                     ErrorEvent error = new ErrorEvent()
                     {
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
                           var name = archive.Name;
                           try { archive.Dispose(); }
                           catch { }
                           archive = store.OpenArchive(name);
                           archive.PrepareBackup();
                           session = archive.Index.FetchSession(session.ID);
                           continue;
                        default:
                           throw;
                     }
                  }
               }
            }
            session.State = Model.SessionState.Completed;
            archive.Index.UpdateSession(session);
            archive.Checkpoint();
         }
         finally
         {
            archive.Dispose();
         }
      }

      public void Restore (RestoreRequest request)
      {
         // TODO: validate request
         if (this.Connection == null)
            throw new InvalidOperationException("TODO: Not connected");
         // load the archive/index
         var store = this.Connection.Store;
         var archive = (Store.IArchive)null;
         if (!store.ListArchives().Contains(request.Archive, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("TODO: archive not found");
         archive = store.OpenArchive(request.Archive);
         try
         {
            archive.PrepareRestore(request.Entries);
            var header = archive.Index.FetchHeader();
            using (var crypto =
               new Rfc2898DeriveBytes(
                  request.Password,
                  header.PasswordSalt,
                  header.CryptoIterations
               )
            )
            {
               var hash = crypto.GetBytes(header.PasswordHash.Length);
               if (!hash.SequenceEqual(header.PasswordHash))
                  throw new InvalidOperationException("TODO: authentication failed");
            }
            var aes = new AesCryptoServiceProvider();
            using (var crypto =
               new Rfc2898DeriveBytes(
                  request.Password,
                  header.ArchiveSalt,
                  header.CryptoIterations
               )
            )
            {
               aes.Key = crypto.GetBytes(aes.KeySize / 8);
               aes.IV = header.ArchiveSalt.Take(aes.BlockSize / 8).ToArray();
            }
            foreach (var entryID in request.Entries)
            {
               var entry = archive.Index.FetchEntry(entryID);
               var path = entry.Node.GetRelativePath();
               var root = entry.Node.GetRoot();
               var rootPath = "";
               if (!request.RootPathMap.TryGetValue(root.Name, out rootPath))
                  rootPath = root.Name;
               path = Path.Combine(rootPath, path);
               Directory.CreateDirectory(Path.GetDirectoryName(path));
               var fileInfo = new FileInfo(path);
               if (fileInfo.Exists)
               {
                  if (fileInfo.Attributes.HasFlag(FileAttributes.ReadOnly))
                     if (!request.OverwriteReadOnly)
                        throw new InvalidOperationException("TODO: cannot overwrite read-only file");
                     else
                        fileInfo.Attributes &= ~FileAttributes.ReadOnly;
               }
               try
               {
                  // TODO: fault tolerance
                  using (var archiveStream = archive.RestoreEntry(entry))
                  using (var cryptoStream = new CryptoStream(archiveStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                  using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
                     cryptoStream.CopyTo(fileStream);
                  if (request.VerifyResults)
                     if (IO.Crc32Stream.Calculate(fileInfo) != entry.Crc32)
                        throw new InvalidOperationException("TODO: CRC does not match");
               }
               catch
               {
                  try { File.Delete(path); }
                  catch { }
                  throw;
               }
            }
         }
         finally
         {
            if (archive != null)
               archive.Dispose();
         }
      }

      public DiffResult Difference (DiffRequest request)
      {
         // TODO: validate request
         if (this.Connection == null)
            throw new InvalidOperationException("TODO: Not connected");
         // load the archive/index
         var store = this.Connection.Store;
         var archive = store.OpenArchive(request.Archive);
         var index = archive.Index;
         try
         {
            DiffResult result = new DiffResult()
            {
               Entries = new List<Differencer.Diff>()
            };
            foreach (var root in index.ListNodes(null))
            {
               var path = root.Name;
               var mapPath = "";
               if (request.RootPathMap.TryGetValue(path, out mapPath))
                  path = mapPath;
               result.Entries.AddRange(
                  new Differencer()
                  {
                     Method = request.Method,
                     Index = index,
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

      public enum ErrorResult
      {
         Abort = 1,
         Retry = 2,
         Fail = 3
      }

      public class ErrorEvent
      {
         public Model.Entry Entry { get; set; }
         public Exception Exception { get; set; }
         public ErrorResult Result { get; set; }
      }

      public class ProgressEvent
      {
         public Model.Entry Entry { get; set; }
         public Boolean Cancel { get; set; }
      }
   }
}
