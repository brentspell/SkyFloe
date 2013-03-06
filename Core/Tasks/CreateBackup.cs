//===========================================================================
// MODULE:  CreateBackup.cs
// PURPOSE: backup session creation task
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
using System.Transactions;
// Project References

namespace SkyFloe.Tasks
{
   /// <summary>
   /// Create backup task
   /// </summary>
   /// <remarks>
   /// This class creates a new persistent backup session within the attached 
   /// archive, performs a diff against the source file system and adds any 
   /// new, changed, or deleted files to the session. The new session can
   /// be accessed after execution from the Session property.
   /// </remarks>
   public class CreateBackup : Task
   {
      /// <summary>
      /// The backup creation request
      /// </summary>
      public BackupRequest Request { get; set; }
      /// <summary>
      /// Contains the new backup session after successful execution
      /// </summary>
      public Backup.Session Session { get; private set; }

      /// <summary>
      /// Task validation override
      /// </summary>
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
      /// <summary>
      /// Task execution override
      /// </summary>
      protected override void DoExecute ()
      {
         using (var txn = new TransactionScope(TransactionScopeOption.Required, TimeSpan.MaxValue))
         {
            // create the new restore session
            var session = this.Archive.BackupIndex.InsertSession(
               new Backup.Session()
               {
                  State = Backup.SessionState.Pending,
                  CheckpointLength = this.Request.CheckpointLength,
                  RateLimit = this.Request.RateLimit,
                  Flags = (this.Request.Compress ? Backup.SessionFlags.Compress : 0)
               }
            );
            // add the requested backup source directores to the session
            foreach (var source in this.Request.Sources)
               AddSource(session, source);
            // commit and attach the session
            this.Archive.BackupIndex.UpdateSession(session);
            txn.Complete();
            this.Session = session;
         }
      }
      /// <summary>
      /// Adds a source directory to a backup session
      /// </summary>
      /// <param name="session">
      /// The backup session being constructed
      /// </param>
      /// <param name="source">
      /// The source directory path
      /// </param>
      private void AddSource (Backup.Session session, IO.Path source)
      {
         // fetch/insert the root node for the source
         var root =
            this.Archive.BackupIndex.ListNodes(null)
               .FirstOrDefault(n => n.NameEquals(source)) ??
            this.Archive.BackupIndex.InsertNode(
               new Backup.Node()
               {
                  Type = Backup.NodeType.Root,
                  Name = source
               }
            );
         // create a differencer and difference the archive
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
         foreach (var diff in differenceTask.Enumerate(root))
            AddEntry(session, diff);
      }
      /// <summary>
      /// Adds a file difference to a backup session
      /// </summary>
      /// <param name="session">
      /// The backup session being constructed
      /// </param>
      /// <param name="diff">
      /// The file difference to add
      /// </param>
      private void AddEntry (Backup.Session session, DiffResult diff)
      {
         // insert the node if it does not already exist
         if (diff.Node.ID == 0)
            this.Archive.BackupIndex.InsertNode(diff.Node);
         // only create backup entries for file nodes
         if (diff.Node.Type == Backup.NodeType.File)
         {
            // add the differenced backup entry to the session
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
                  Crc32 = IO.CrcFilter.InitialValue
               }
            );
            ReportProgress(
               new ProgressEventArgs()
               {
                  Operation = "CreateBackupEntry",
                  BackupSession = session,
                  BackupEntry = entry
               }
            );
            session.EstimatedLength += entry.Length;
         }
      }
   }
}
