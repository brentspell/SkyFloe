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
            var header = archive.Index.FetchHeader();
            var crypto = new Rfc2898DeriveBytes(
               request.Password,
               header.PasswordSalt,
               header.CryptoIterations
            );
            var passwordHash = crypto.GetBytes(header.PasswordHash.Length);
            if (!passwordHash.SequenceEqual(header.PasswordHash))
               throw new InvalidOperationException("TODO: authentication failed");
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
            header.PasswordHash = new Rfc2898DeriveBytes(
               request.Password,
               header.PasswordSalt,
               header.CryptoIterations
            ).GetBytes(CryptoHashLength);
            archive = store.CreateArchive(request.Archive, header);
            archive.Checkpoint();
         }
         // initialize the new backup session
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
         // start the backup task
         try
         {
            if (session.State == Model.SessionState.Pending)
               foreach (var source in request.Sources)
                  AddSource(archive.Index, session, source, request.DiffMethod);
            BackupSession(request, store, archive, session);
         }
         finally
         {
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

      public void AddSource (Store.IIndex index, Model.Session session, String path, DiffMethod diffMethod)
      {
         // TODO: validate source is directory
         using (var txn = new TransactionScope(TransactionScopeOption.Required, TimeSpan.MaxValue))
         {
            var root = index
               .ListNodes(null)
               .FirstOrDefault(n => String.Compare(n.Name, path, true) == 0);
            if (root == null)
               root = index.InsertNode(
                  new Model.Node()
                  {
                     Type = Model.NodeType.Root,
                     Name = path
                  }
               );
            var differencer = new Differencer()
            {
               Method = diffMethod,
               Index = index,
               Root = root,
               Path = path
            };
            foreach (var diff in differencer.Enumerate())
            {
               if (diff.Node.ID == 0)
                  index.InsertNode(diff.Node);
               if (diff.Node.Type == Model.NodeType.File)
               {
                  index.InsertEntry(
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
      private void BackupSession (BackupRequest request, Store.IStore store, Store.IArchive archive, Model.Session session)
      {
         if (session.State == Model.SessionState.Pending)
         {
            session.State = Model.SessionState.InProgress;
            archive.Index.UpdateSession(session);
            archive.Checkpoint();
         }
         var header = archive.Index.FetchHeader();
         var crypto = AesManaged.Create();
         crypto.Key = new Rfc2898DeriveBytes(request.Password, header.ArchiveSalt, header.CryptoIterations)
            .GetBytes(crypto.KeySize / 8);
         var checkpointSize = 0L;
         for ( ; ; )
         {
            var entry = archive.Index.FetchNextPendingEntry(session);
            if (entry == null)
               break;
            try
            {
               using (var fileStream = new FileStream(entry.Node.GetAbsolutePath(), FileMode.Open, FileAccess.Read, FileShare.Read))
               using (var crcStream = new IO.Crc32Stream(fileStream, IO.StreamMode.Read))
               using (var cryptoStream = new CryptoStream(crcStream, crypto.CreateEncryptor(), CryptoStreamMode.Read))
               {
                  archive.StoreEntry(entry, cryptoStream);
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
               this.OnProgress(
                  new ProgressEvent()
                  {
                     Entry = entry
                  }
               );
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
      }
   }
}
