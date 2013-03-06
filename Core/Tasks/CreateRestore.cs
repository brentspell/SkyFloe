//===========================================================================
// MODULE:  CreateRestore.cs
// PURPOSE: restore session creation task
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
   /// Create restore task
   /// </summary>
   /// <remarks>
   /// This class creates a new persistent restore session for the attached
   /// archive. It creates restore entries within the session for each
   /// requested backup entry. The new session can be accessed after 
   /// execution from the Session property.
   /// </remarks>
   public class CreateRestore : Task
   {
      /// <summary>
      /// The restore creation request
      /// </summary>
      public RestoreRequest Request { get; set; }
      /// <summary>
      /// Contains the new restore session after successful execution
      /// </summary>
      public Restore.Session Session { get; private set; }

      /// <summary>
      /// Task validation override
      /// </summary>
      protected override void DoValidate ()
      {
         if (this.Request == null)
            throw new ArgumentException("Request");
         foreach (var map in this.Request.RootPathMap)
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
      /// <summary>
      /// Task execution override
      /// </summary>
      protected override void DoExecute ()
      {
         using (var txn = new TransactionScope(TransactionScopeOption.Required, TimeSpan.MaxValue))
         {
            // create the new restore session
            var session = this.Archive.RestoreIndex.InsertSession(
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
            // set up the backup source path mappings in the restore session
            var roots = this.Archive.BackupIndex.ListNodes(null);
            foreach (var pathMap in this.Request.RootPathMap)
            {
               var root = roots.FirstOrDefault(n => n.Name == pathMap.Key);
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
            // add the requested backup entries to the restore session
            foreach (var backupEntryID in this.Request.Entries)
               AddBackupEntry(
                  session, 
                  this.Archive.BackupIndex.FetchEntry(backupEntryID)
               );
            // commit and attach the session
            this.Archive.RestoreIndex.UpdateSession(session);
            txn.Complete();
            this.Session = session;
         }
      }
      /// <summary>
      /// Creates a restore entry for a backup entry
      /// </summary>
      /// <param name="session">
      /// The restore session being constructed
      /// </param>
      /// <param name="backupEntry">
      /// The backup entry to restore
      /// </param>
      private void AddBackupEntry (Restore.Session session, Backup.Entry backupEntry)
      {
         // ignore the backup entry if it does not match the filter
         if (this.Request.Filter.Evaluate(backupEntry.Node.GetAbsolutePath()))
         {
            var restoreEntry = (Restore.Entry)null;
            switch (backupEntry.State)
            {
               case Backup.EntryState.Completed:
                  // fetch/create the restore retrieval for the entry's blob
                  var retrieval =
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
                  // create the new restore entry
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
                  // create the delete restore entry
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
                  new ProgressEventArgs()
                  {
                     Operation = "CreateRestoreEntry",
                     BackupSession = backupEntry.Session,
                     BackupEntry = backupEntry,
                     RestoreSession = session,
                     RestoreEntry = restoreEntry
                  }
               );
         }
      }
   }
}
