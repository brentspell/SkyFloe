//===========================================================================
// MODULE:  Engine.cs
// PURPOSE: backup runtime engine
// 
// Copyright © 2013
// Brent M. Spell. All rights reserved.
//
// This library is free software; you can redistribute it and/or modify it 
// under the terms of the GNU Lesser General Public License as published 
// by the Free Software Foundation; either version 3 of the License, or 
// (at your option) any later version. This library is distributed in the 
// hope that it will be useful, but WITHOUT ANY WARRANTY; without even the 
// implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
// See the GNU Lesser General Public License for more details. You should 
// have received a copy of the GNU Lesser General Public License along with 
// this library; if not, write to 
//    Free Software Foundation, Inc. 
//    51 Franklin Street, Fifth Floor 
//    Boston, MA 02110-1301 USA
//===========================================================================
// System References
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Transactions;
using Stream = System.IO.Stream;
// Project References

namespace SkyFloe
{
   /// <summary>
   /// The backup engine
   /// </summary>
   /// <remarks>
   /// This class is responsible for coordinating all backup/restore 
   /// processing in the SkyFloe system. It connects to a store and archive
   /// and dispatches backup and restore tasks.
   /// These tasks execute synchronously for simplicity - clients are
   /// responsible for running the engine asynchronously from the UI. The
   /// dispatches notifications to the client for responsiveness and supports
   /// cancellation through the CancellationToken mechanism.
   /// </remarks>
   public class Engine : IDisposable
   {
      private const Int32 DefaultCryptoHashLength = 256;
      private const Int32 DefaultCryptoSaltLength = 128;
      private const Int32 DefaultCryptoIterations = 1000;
      private Store.IArchive archive;
      private SymmetricAlgorithm crypto;

      public event EventHandler<ProgressEventArgs> OnProgress;
      public event EventHandler<ErrorEventArgs> OnError;

      /// <summary>
      /// Initializes a new engine instance
      /// </summary>
      public Engine ()
      {
      }
      /// <summary>
      /// Releases resources associated with the engine
      /// </summary>
      public void Dispose ()
      {
         CloseArchive();
      }

      /// <summary>
      /// The connection to a backup store
      /// </summary>
      public Connection Connection { get; set; }
      /// <summary>
      /// The task cancellation token
      /// </summary>
      public CancellationToken Canceler { get; set; }
      /// <summary>
      /// The current archive connection
      /// </summary>
      public Connection.Archive Archive
      {
         get
         {
            return (this.archive != null) ?
               new Connection.Archive(this.archive) :
               null;
         }
      }

      #region Archive Operations
      /// <summary>
      /// Creates and connects to a new backup archive
      /// </summary>
      /// <param name="name">
      /// The name of the archive to create
      /// </param>
      /// <param name="password">
      /// The security password for the archive
      /// </param>
      public void CreateArchive (String name, String password)
      {
         // validate parameters
         if (this.Connection == null)
            throw new InvalidOperationException("TODO: not connected to a store");
         if (this.archive != null)
            throw new InvalidOperationException("TODO: already connected to an archive");
         var store = this.Connection.Store;
         try
         {
            // verify that the archive does not already exist at the store
            if (store.ListArchives().Contains(name, StringComparer.OrdinalIgnoreCase))
               throw new InvalidOperationException("TODO: archive exists");
            // create the archive header and encryption parameters
            var random = RandomNumberGenerator.Create();
            var header = new Backup.Header()
            {
               CryptoIterations = DefaultCryptoIterations,
               ArchiveSalt = new Byte[DefaultCryptoSaltLength],
               PasswordSalt = new Byte[DefaultCryptoSaltLength]
            };
            random.GetBytes(header.ArchiveSalt);
            random.GetBytes(header.PasswordSalt);
            using (var hash = CreateHasher(password, header))
               header.PasswordHash = hash.GetBytes(DefaultCryptoHashLength);
            // attach the encryption algorithm and
            // delegate to the store implementation to create the archive
            this.crypto = CreateCrypto(password, header);
            this.archive = store.CreateArchive(name, header);
         }
         catch
         {
            CloseArchive();
            throw;
         }
      }
      /// <summary>
      /// Connects to an existing backup archive
      /// </summary>
      /// <param name="name">
      /// The name of the archive to open
      /// </param>
      /// <param name="password">
      /// The security password of the archive
      /// </param>
      public void OpenArchive (String name, String password)
      {
         // validate parameters
         if (this.Connection == null)
            throw new InvalidOperationException("TODO: not connected to a store");
         if (this.archive != null)
            throw new InvalidOperationException("TODO: already connected to an archive");
         var store = this.Connection.Store;
         try
         {
            // verify that the archive exists at the store and
            // open the archive implementation
            if (!store.ListArchives().Contains(name, StringComparer.OrdinalIgnoreCase))
               throw new InvalidOperationException("TODO: archive not found");
            this.archive = store.OpenArchive(name);
            // authenticate the request and attach the encryption algorithm
            var header = this.archive.BackupIndex.FetchHeader();
            using (var hash = CreateHasher(password, header))
               if (!hash.GetBytes(header.PasswordHash.Length).SequenceEqual(header.PasswordHash))
                  throw new InvalidOperationException("TODO: authentication failed");
            this.crypto = CreateCrypto(password, header);
         }
         catch
         {
            CloseArchive();
            throw;
         }
      }
      /// <summary>
      /// Disconnects from the attached archive
      /// </summary>
      public void CloseArchive ()
      {
         if (this.archive != null)
            this.archive.Dispose();
         if (this.crypto != null)
            this.crypto.Dispose();
         this.archive = null;
         this.crypto = null;
      }
      /// <summary>
      /// Permanently deletes an archive
      /// </summary>
      /// <param name="name">
      /// The name of the archive to delete
      /// </param>
      public void DeleteArchive (String name)
      {
         this.Connection.Store.DeleteArchive(name);
      }
      /// <summary>
      /// Creates a secure hash generator
      /// </summary>
      /// <param name="password">
      /// The password to hash
      /// </param>
      /// <param name="header">
      /// The backup header containing crypto configuration
      /// </param>
      /// <returns>
      /// The requested hash generator
      /// </returns>
      private DeriveBytes CreateHasher (String password, Backup.Header header)
      {
         return new Rfc2898DeriveBytes(
            password,
            header.PasswordSalt,
            header.CryptoIterations
         );
      }
      /// <summary>
      /// Creates an encryption algorithm implementation
      /// </summary>
      /// <param name="password">
      /// The password to use to derive the encryption key
      /// </param>
      /// <param name="header">
      /// The backup header containing crypto configuration
      /// </param>
      /// <returns>
      /// The symmetric encryption algorithm implementation
      /// </returns>
      private SymmetricAlgorithm CreateCrypto (String password, Backup.Header header)
      {
         var aes = new AesCryptoServiceProvider();
         try
         {
            var keygen = new Rfc2898DeriveBytes(
               password, 
               header.ArchiveSalt, 
               header.CryptoIterations
            );
            using (keygen)
            {
               aes.Key = keygen.GetBytes(aes.KeySize / 8);
               aes.IV = header.ArchiveSalt.Take(aes.BlockSize / 8).ToArray();
            }
            return aes;
         }
         catch
         {
            aes.Dispose();
            throw;
         }
      }
      #endregion

      #region Backup/Restore Operations
      /// <summary>
      /// Creates a new backup session
      /// </summary>
      /// <param name="request">
      /// The backup creation request
      /// </param>
      /// <returns>
      /// The new backup session
      /// </returns>
      public Backup.Session CreateBackup (BackupRequest request)
      {
         var task = new Tasks.CreateBackup() { Request = request };
         Execute(task);
         return task.Session;
      }
      /// <summary>
      /// Starts or resumes a backup session
      /// </summary>
      /// <param name="session">
      /// The backup session to execute
      /// </param>
      public void ExecuteBackup (Backup.Session session)
      {
         Execute(new Tasks.ExecuteBackup() { Session = session });
      }
      /// <summary>
      /// Creates a new restore session
      /// </summary>
      /// <param name="request">
      /// The restore creation request
      /// </param>
      /// <returns>
      /// The new restore session
      /// </returns>
      public Restore.Session CreateRestore (RestoreRequest request)
      {
         var task = new Tasks.CreateRestore() { Request = request };
         Execute(task);
         return task.Session;
      }
      /// <summary>
      /// Starts or resumes a restore session
      /// </summary>
      /// <param name="session">
      /// The restore session to execute
      /// </param>
      public void ExecuteRestore (Restore.Session session)
      {
         Execute(new Tasks.ExecuteRestore() { Session = session });
      }
      /// <summary>
      /// Deletes a restore session
      /// </summary>
      /// <param name="session">
      /// The restore session to delete
      /// </param>
      public void DeleteRestore (Restore.Session session)
      {
         this.archive.RestoreIndex.DeleteSession(session);
      }
      /// <summary>
      /// Executes a backup differencing operation
      /// </summary>
      /// <param name="request">
      /// The differencing request
      /// </param>
      public void Difference (DiffRequest request)
      {
         Execute(new Tasks.Difference() { Request = request });
      }
      #endregion

      #region Task Operations
      /// <summary>
      /// Executes an engine task
      /// </summary>
      /// <param name="task">
      /// The task to execute
      /// </param>
      public void Execute (Tasks.Task task)
      {
         if (this.archive == null)
            throw new InvalidOperationException("TODO: not connected to an archive");
         task.Archive = this.archive;
         task.Crypto = this.crypto;
         task.Canceler = this.Canceler;
         using (task)
         {
            if (this.OnProgress != null)
               task.OnProgress = (o, a) => this.OnProgress(o, a);
            if (this.OnError != null)
               task.OnError = (o, a) => this.OnError(o, a);
            task.Execute();
         }
      }
      #endregion
   }

   /// <summary>
   /// Indicates the action that the engine should
   /// take to recover from a backup/restore operation fault
   /// </summary>
   public enum ErrorResult
   {
      Abort = 1,     // terminate the entire backup/restore task
      Retry = 2,     // retry the current operation
      Fail = 3       // fail the current operation but attempt to continue
   }

   /// <summary>
   /// Backup error event parameters
   /// </summary>
   public class ErrorEventArgs : EventArgs
   {
      public String Action { get; set; }
      public Exception Exception { get; set; }
      public ErrorResult Result { get; set; }
   }

   /// <summary>
   /// Backup progress event parameters
   /// </summary>
   public class ProgressEventArgs : EventArgs
   {
      public String Operation { get; set; }
      public Backup.Session BackupSession { get; set; }
      public Backup.Entry BackupEntry { get; set; }
      public Restore.Session RestoreSession { get; set; }
      public Restore.Entry RestoreEntry { get; set; }
      public DiffResult DiffEntry { get; set; }
   }
}
