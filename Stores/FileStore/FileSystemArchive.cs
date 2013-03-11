//===========================================================================
// MODULE:  FileSystemArchive.cs
// PURPOSE: file-based backup archive
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
// Project References
using SkyFloe.Store;

namespace SkyFloe
{
   /// <summary>
   /// File system backup archive
   /// </summary>
   /// <remarks>
   /// This class represents a named backup container within the file store. 
   /// A file archive is a subdirectory within the store containing two 
   /// files: the backup index (index.db) and the backup blob (blob.dat).
   /// The backup index is a Sqlite database (see the Sqlite project), and
   /// the blob is a sequential binary file containing only backup data,
   /// with backup file offsets/lengths stored within the index.
   /// The backup index is maintained within a temporary path during backup
   /// operations and only copied up to the archive path during checkpoint,
   /// to ensure the consistency of the index when faults occur. This also
   /// provides better performance when the archive is a network path.
   /// The archive also provides access to the restore index, stored in the
   /// local file system per user. This restore index provides for pausing/
   /// resuming restore processes (similar to the backup index) and recovery
   /// from crashes during restore.
   /// </remarks>
   public class FileSystemArchive : IArchive
   {
      private IBackupIndex backupIndex;
      private IRestoreIndex restoreIndex;
      private IO.FileSystem.TempStream tempIndex;

      /// <summary>
      /// Initializes a new archive instance
      /// </summary>
      /// <param name="path">
      /// The path to the archive subdirectory
      /// </param>
      public FileSystemArchive (IO.Path path)
      {
         this.Path = path;
         var restoreIndexPath = new IO.Path(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SkyFloe",
            "FileStore",
            this.Name,
            "restore.db"
         );
         IO.FileSystem.CreateDirectory(restoreIndexPath.Parent);
         this.restoreIndex = (IO.FileSystem.GetMetadata(restoreIndexPath).Exists) ?
            Sqlite.RestoreIndex.Open(restoreIndexPath) :
            Sqlite.RestoreIndex.Create(restoreIndexPath, new Restore.Header());
      }
      /// <summary>
      /// Releases the resources associated with the archive
      /// </summary>
      public void Dispose ()
      {
         if (this.backupIndex != null)
            this.backupIndex.Dispose();
         if (this.restoreIndex != null)
            this.restoreIndex.Dispose();
         if (this.tempIndex != null)
            this.tempIndex.Dispose();
         this.backupIndex = null;
         this.restoreIndex = null;
         this.tempIndex = null;
      }

      /// <summary>
      /// The archive file system path
      /// </summary>
      public IO.Path Path
      { 
         get; private set;
      }
      /// <summary>
      /// The path to the archive backup index
      /// </summary>
      public IO.Path IndexPath
      {
         get { return this.Path + "index.db"; } 
      }
      /// <summary>
      /// The path to the archive blob file
      /// </summary>
      public IO.Path BlobPath
      {
         get { return this.Path + "blob.dat"; }
      }

      #region Operations
      /// <summary>
      /// Creates a new backup archive
      /// </summary>
      /// <param name="header">
      /// The backup index header to insert
      /// </param>
      public void Create (Backup.Header header)
      {
         try
         {
            // create the archive directory
            IO.FileSystem.CreateDirectory(this.Path);
            // create the backup index within the temp path
            this.tempIndex = IO.FileSystem.Temp();
            this.backupIndex = Sqlite.BackupIndex.Create(this.tempIndex.Path, header);
            this.backupIndex.InsertBlob(
               new Backup.Blob()
               {
                  Name = this.BlobPath.FileName
               }
            );
            // copy the initial version of the index to the archive path
            Save();
         }
         catch
         {
            Dispose();
            try { IO.FileSystem.Delete(this.Path); } catch { }
            throw;
         }
      }
      /// <summary>
      /// Opens an existing backup archive
      /// </summary>
      public void Open ()
      {
         try
         {
            // copy the backup index to a temporary location and open it
            this.tempIndex = IO.FileSystem.Temp();
            IO.FileSystem.Copy(this.IndexPath, this.tempIndex.Path);
            this.backupIndex = Sqlite.BackupIndex.Open(this.tempIndex.Path);
         }
         catch
         {
            Dispose();
            throw;
         }
      }
      /// <summary>
      /// Commits the temporary backup index to the archive location
      /// </summary>
      public void Save ()
      {
         IO.FileSystem.Copy(this.tempIndex.Path, this.IndexPath);
      }
      #endregion

      #region IArchive Implementation
      /// <summary>
      /// The archive name
      /// </summary>
      public String Name
      {
         get { return this.Path.FileName; }
      }
      /// <summary>
      /// The archive backup index
      /// </summary>
      public IBackupIndex BackupIndex
      {
         get { return this.backupIndex; }
      }
      /// <summary>
      /// The archive restore index
      /// </summary>
      public IRestoreIndex RestoreIndex
      {
         get { return this.restoreIndex; }
      }
      /// <summary>
      /// Prepares the archive for a new backup process and returns
      /// an object used to add entries to the archive
      /// </summary>
      /// <param name="session">
      /// The new backup session
      /// </param>
      /// <returns>
      /// The archive backup implementation
      /// </returns>
      public IBackup PrepareBackup (Backup.Session session)
      {
         return new FileSystemBackup(this, session);
      }
      /// <summary>
      /// Prepares the archive for a new restore process and returns
      /// an object used to restore entries from the archive
      /// </summary>
      /// <param name="session">
      /// The new restore session
      /// </param>
      /// <returns>
      /// The archive restore implementation
      /// </returns>
      public IRestore PrepareRestore (Restore.Session session)
      {
         return new FileSystemRestore(this, session);
      }
      #endregion
   }
}
