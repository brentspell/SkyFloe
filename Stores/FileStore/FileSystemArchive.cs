using System;
using System.Collections.Generic;
using System.Linq;

using SkyFloe.Store;

namespace SkyFloe
{
   public class FileSystemArchive : IArchive
   {
      private IBackupIndex backupIndex;
      private IRestoreIndex restoreIndex;
      private IO.FileSystem.TempStream tempIndex;

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

      public IO.Path Path
      { 
         get; private set;
      }
      public IO.Path IndexPath
      {
         get { return this.Path + "index.db"; } 
      }
      public IO.Path BlobPath
      {
         get { return this.Path + "blob.dat"; }
      }

      #region Operations
      public void Create (Backup.Header header)
      {
         try
         {
            IO.FileSystem.CreateDirectory(this.Path);
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
      public void Open ()
      {
         try
         {
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
      public void Save ()
      {
         using (var indexStream = IO.FileSystem.Truncate(this.IndexPath))
         using (var tempStream = this.backupIndex.Serialize())
            tempStream.CopyTo(indexStream);
      }
      #endregion

      #region IArchive Implementation
      public String Name
      {
         get { return this.Path.FileName; }
      }
      public IBackupIndex BackupIndex
      {
         get { return this.backupIndex; }
      }
      public IRestoreIndex RestoreIndex
      {
         get { return this.restoreIndex; }
      }
      public IBackup PrepareBackup (Backup.Session session)
      {
         return new FileSystemBackup(this, session);
      }
      public IRestore PrepareRestore (Restore.Session session)
      {
         return new FileSystemRestore(this, session);
      }
      #endregion
   }
}
