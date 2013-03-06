//===========================================================================
// MODULE:  ExecuteRestore.cs
// PURPOSE: restore session execution task
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
using System.Transactions;
// Project References

namespace SkyFloe.Tasks
{
   /// <summary>
   /// Execute restore task
   /// </summary>
   /// <remarks>
   /// This task starts or resumes a backup session prepared by the 
   /// CreateRestore task. It enumerates over all restore entries in the
   /// session, retrieves a restore stream from the store plugin, and
   /// copies the stream to the target path.
   /// </remarks>
   public class ExecuteRestore : Task
   {
      private Store.IRestore restore;
      private IO.RateLimiter limiter;

      /// <summary>
      /// The restore session to start/resume
      /// </summary>
      public SkyFloe.Restore.Session Session { get; set; }

      /// <summary>
      /// Releases resources associated with the task
      /// </summary>
      public override void Dispose ()
      {
         base.Dispose();
         if (this.restore != null)
            this.restore.Dispose();
         this.restore = null;
      }
      /// <summary>
      /// Task validation override
      /// </summary>
      protected override void DoValidate ()
      {
         if (this.Session == null)
            throw new ArgumentException("Session");
         if (this.Session.State == Restore.SessionState.Completed)
            throw new ArgumentException("Session.State");
      }
      /// <summary>
      /// Task execution override
      /// </summary>
      protected override void DoExecute ()
      {
         // prepare the restore plugin for the session
         // and mark it as in-progress
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
            // fetch and restore the next pending entry
            var entry = this.Archive.RestoreIndex.LookupNextEntry(this.Session);
            if (entry == null)
               break;
            RestoreEntry(entry);
         }
         // there are no more pending restore entries, so complete the session
         this.Session.State = SkyFloe.Restore.SessionState.Completed;
         this.Archive.RestoreIndex.UpdateSession(this.Session);
      }
      /// <summary>
      /// Restores a single backup entry to the file system
      /// </summary>
      /// <param name="restoreEntry">
      /// The entry to restore
      /// </param>
      void RestoreEntry (SkyFloe.Restore.Entry restoreEntry)
      {
         // fetch the corresponding backup entry
         var backupEntry = this.Archive.BackupIndex.FetchEntry(restoreEntry.BackupEntryID);
         ReportProgress(
            new ProgressEventArgs()
            {
               Operation = "BeginRestoreEntry",
               BackupSession = backupEntry.Session,
               BackupEntry = backupEntry,
               RestoreSession = this.Session,
               RestoreEntry = restoreEntry
            }
         );
         var path = IO.Path.Empty;
         try
         {
            // determine whether to restore the entry
            path = PrepareFile(backupEntry);
            if (!path.IsEmpty)
            {
               // restore or delete the entry
               if (backupEntry.State != SkyFloe.Backup.EntryState.Deleted)
                  RestoreFile(path, backupEntry, restoreEntry);
               else if (this.Session.EnableDeletes)
                  IO.FileSystem.Delete(path);
               this.Session.RestoreLength += restoreEntry.Length;
            }
            // the restore was successful or skipped, so commit it to the index
            restoreEntry.State = SkyFloe.Restore.EntryState.Completed;
            using (var txn = new TransactionScope())
            {
               this.Archive.RestoreIndex.UpdateEntry(restoreEntry);
               this.Archive.RestoreIndex.UpdateSession(this.Session);
               txn.Complete();
            }
         }
         catch (Exception e)
         {
            if (!path.IsEmpty)
               try { IO.FileSystem.Delete(path); }
               catch { }
            switch (ReportError("RestoreEntry", e))
            {
               case ErrorResult.Retry:
                  // the client chose to retry, so leave the entry pending
                  break;
               case ErrorResult.Fail:
                  // the client chose to ignore the failed entry,
                  // so mark it as failed and continue
                  restoreEntry = this.Archive.RestoreIndex.FetchEntry(restoreEntry.ID);
                  restoreEntry.State = SkyFloe.Restore.EntryState.Failed;
                  this.Archive.RestoreIndex.UpdateEntry(restoreEntry);
                  break;
               default:
                  // the client chose to abort, so propagate the exception
                  throw;
            }
         }
         if (restoreEntry.State == SkyFloe.Restore.EntryState.Completed)
            ReportProgress(
               new ProgressEventArgs()
               {
                  Operation = "EndRestoreEntry",
                  BackupSession = backupEntry.Session,
                  BackupEntry = backupEntry,
                  RestoreSession = this.Session,
                  RestoreEntry = restoreEntry
               }
            );
      }
      /// <summary>
      /// Prepares a file system file for restore
      /// </summary>
      /// <param name="backupEntry">
      /// The backup entry to restore
      /// </param>
      /// <returns>
      /// The file system path if it should be restored
      /// Path.Empty otherwise
      /// </returns>
      private IO.Path PrepareFile (Backup.Entry backupEntry)
      {
         // construct the full path to the node
         // map the root node through the restore path mapping
         // concatenate the relative path to the node
         var node = backupEntry.Node;
         var rootNode = node.GetRoot();
         var rootMap = this.Archive.RestoreIndex.LookupPathMap(this.Session, rootNode.ID);
         var rootPath = (IO.Path)(rootMap != null ? rootMap.Path : rootNode.Name);
         var file = IO.FileSystem.GetMetadata(rootPath + node.GetRelativePath());
         // determine whether to restore the existing file
         // . if it exists and we are not overwriting existing files, ignore
         // . if it is read-only and we are not overwriting read-only, ignore
         // . if it does not exist and we are deleting, ignore
         if (this.Session.SkipExisting && file.Length > 0)
            return IO.Path.Empty;
         if (this.Session.SkipReadOnly && file.IsReadOnly)
            return IO.Path.Empty;
         if (!file.Exists && backupEntry.State == SkyFloe.Backup.EntryState.Deleted)
            return IO.Path.Empty;
         // prepare the path
         // . create the containing directory if it does not exist
         // . set read-only files to writable
         IO.FileSystem.CreateDirectory(file.Path.Parent);
         if (file.IsReadOnly)
            IO.FileSystem.SetReadOnly(file.Path, false);
         return file.Path;
      }
      /// <summary>
      /// Restores a single backup entry to the file system
      /// </summary>
      /// <param name="path">
      /// The file system path to restore to
      /// </param>
      /// <param name="backupEntry">
      /// The backup entry to restore
      /// </param>
      /// <param name="restoreEntry">
      /// The restore entry to restore
      /// </param>
      private void RestoreFile (
         IO.Path path, 
         Backup.Entry backupEntry, 
         Restore.Entry restoreEntry)
      {
         using (var input = new IO.StreamStack())
         using (var output = IO.FileSystem.Truncate(path))
         {
            // create the input stream stack, consisting of the following
            // . restore stream from the archive
            // . read rate limiter
            // . decryptor
            // . optional decompressor
            input.Push(
               this.restore.Restore(restoreEntry)
            );
            input.Push(
               this.limiter.CreateStreamFilter(input.Top)
            );
            input.Push(
               new CryptoStream(
                  input.Top, 
                  this.Crypto.CreateDecryptor(), 
                  CryptoStreamMode.Read
               )
            );
            if (backupEntry.Session.Compress)
               input.Push(
                  new IO.CompressionStream(
                     input.Top, 
                     IO.CompressionMode.Decompress
                  )
               );
            input.CopyTo(output);
         }
         // verify the backup entry's CRC if requested
         if (this.Session.VerifyResults)
            if (IO.CrcFilter.Calculate(path) != backupEntry.Crc32)
               throw new InvalidOperationException("TODO: CRC does not match");
      }
   }
}
