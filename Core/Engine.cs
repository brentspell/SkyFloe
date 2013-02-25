using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Transactions;

namespace SkyFloe
{
   public class Engine : IDisposable
   {
      private const Int32 CryptoHashLength = 256;
      private const Int32 CryptoSaltLength = 128;
      private const Int32 CryptoIterations = 1000;
      private Store.IArchive archive;
      private SymmetricAlgorithm crypto;

      public event EventHandler<ProgressEventArgs> OnProgress;
      public event EventHandler<ErrorEventArgs> OnError;

      public void Dispose ()
      {
         if (this.archive != null)
            this.archive.Dispose();
         if (this.crypto != null)
            this.crypto.Dispose();
         this.archive = null;
         this.crypto = null;
      }

      public Connection Connection
      {
         get; set; 
      }

      public CancellationToken Canceler
      {
         get; set;
      }

      public Connection.Archive Archive
      {
         get { return new Connection.Archive(this.archive);  } // TODO: verify connected + archive open
      }

      #region Archive Management
      public void CreateArchive (String name, String password)
      {
         var store = this.Connection.Store;
         try
         {
            if (store.ListArchives().Contains(name, StringComparer.OrdinalIgnoreCase))
               throw new InvalidOperationException("TODO: archive exists");
            var rng = RandomNumberGenerator.Create();
            var header = new Backup.Header()
            {
               CryptoIterations = Engine.CryptoIterations,
               ArchiveSalt = new Byte[CryptoSaltLength],
               PasswordSalt = new Byte[CryptoSaltLength]
            };
            rng.GetBytes(header.ArchiveSalt);
            rng.GetBytes(header.PasswordSalt);
            using (var crypto =
               new Rfc2898DeriveBytes(
                  password,
                  header.PasswordSalt,
                  header.CryptoIterations
               )
            )
               header.PasswordHash = crypto.GetBytes(CryptoHashLength);
            this.crypto = new AesCryptoServiceProvider();
            using (var crypto =
               new Rfc2898DeriveBytes(
                  password,
                  header.ArchiveSalt,
                  header.CryptoIterations
               )
            )
            {
               this.crypto.Key = crypto.GetBytes(this.crypto.KeySize / 8);
               this.crypto.IV = header.ArchiveSalt.Take(this.crypto.BlockSize / 8).ToArray();
            }
            this.archive = store.CreateArchive(name, header);
         }
         catch
         {
            if (this.archive != null)
               this.archive.Dispose();
            if (this.crypto != null)
               this.crypto.Dispose();
            this.archive = null;
            this.crypto = null;
            throw;
         }
      }
      public void OpenArchive (String name, String password)
      {
         var store = this.Connection.Store;
         try
         {
            if (!store.ListArchives().Contains(name, StringComparer.OrdinalIgnoreCase))
               throw new InvalidOperationException("TODO: archive not found");
            this.archive = store.OpenArchive(name);
            var header = this.archive.BackupIndex.FetchHeader();
            using (var crypto =
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
            this.crypto = new AesCryptoServiceProvider();
            using (var crypto =
               new Rfc2898DeriveBytes(
                  password,
                  header.ArchiveSalt,
                  header.CryptoIterations
               )
            )
            {
               this.crypto.Key = crypto.GetBytes(this.crypto.KeySize / 8);
               this.crypto.IV = header.ArchiveSalt.Take(this.crypto.BlockSize / 8).ToArray();
            }
         }
         catch
         {
            if (this.archive != null)
               this.archive.Dispose();
            if (this.crypto != null)
               this.crypto.Dispose();
            this.archive = null;
            this.crypto = null;
            throw;
         }
      }
      public void DeleteArchive (String name)
      {
         this.Connection.Store.DeleteArchive(name);
      }
      #endregion

      public Backup.Session CreateBackup (BackupRequest request)
      {
         var task = new Tasks.CreateBackup()
         {
            Archive = this.archive,
            Crypto = this.crypto,
            Canceler = this.Canceler,
            Request = request
         };
         Execute(task);
         return task.Session;
      }

      public void ExecuteBackup (Backup.Session session)
      {
         Execute(
            new Tasks.ExecuteBackup()
            {
               Archive = this.archive,
               Crypto = this.crypto,
               Canceler = this.Canceler,
               Session = session
            }
         );
      }
      public Restore.Session CreateRestore (RestoreRequest request)
      {
         var task = new Tasks.CreateRestore()
         {
            Archive = this.archive,
            Crypto = this.crypto,
            Canceler = this.Canceler,
            Request = request
         };
         Execute(task);
         return task.Session;
      }

      public void ExecuteRestore (Restore.Session session)
      {
         Execute(
            new Tasks.ExecuteRestore()
            {
               Archive = this.archive,
               Crypto = this.crypto,
               Canceler = this.Canceler,
               Session = session
            }
         );
      }

      public void DeleteRestore (Restore.Session session)
      {
         this.archive.RestoreIndex.DeleteSession(session);
      }

      public void Difference (DiffRequest request)
      {
         Execute(
            new Tasks.Difference()
            {
               Archive = this.archive,
               Crypto = this.crypto,
               Canceler = this.Canceler,
               Request = request
            }
         );
      }

      public void Execute (Tasks.Task task)
      {
         using (task)
         {
            if (this.OnProgress != null)
               task.OnProgress = (o, a) => this.OnProgress(o, a);
            if (this.OnError != null)
               task.OnError = (o, a) => this.OnError(o, a);
            task.Execute();
         }
      }

      public enum ErrorResult
      {
         Abort = 1,
         Retry = 2,
         Fail = 3
      }
      public class ErrorEventArgs : EventArgs
      {
         public String Action { get; set; }
         public Exception Exception { get; set; }
         public ErrorResult Result { get; set; }
      }
      public class ProgressEventArgs : EventArgs
      {
         public String Action { get; set; }
         public Backup.Session BackupSession { get; set; }
         public Backup.Entry BackupEntry { get; set; }
         public Restore.Session RestoreSession { get; set; }
         public Restore.Entry RestoreEntry { get; set; }
         public DiffResult DiffEntry { get; set; }
      }
   }
}
