using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using SkyFloe.Store;

namespace SkyFloe
{
   public class FileSystemArchive : IArchive
   {
      private IBackupIndex backupIndex;
      private IRestoreIndex restoreIndex;
      private IO.FileSystem.TempStream tempIndex;

      public FileSystemArchive (String path)
      {
         this.Path = path;
         String restoreIndexPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SkyFloe",
            "FileStore",
            this.Name,
            "restore.db"
         );
         Directory.CreateDirectory(System.IO.Path.GetDirectoryName(restoreIndexPath));
         this.restoreIndex = (File.Exists(restoreIndexPath)) ?
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

      public String Path
      { 
         get; private set;
      }
      public String IndexPath
      {
         get { return System.IO.Path.Combine(this.Path, "index.db"); } 
      }
      public String BlobPath
      {
         get { return System.IO.Path.Combine(this.Path, "blob.dat"); }
      }

      #region Operations
      public void Create (Backup.Header header)
      {
         try
         {
            Directory.CreateDirectory(this.Path);
            this.tempIndex = IO.FileSystem.Temp();
            this.backupIndex = Sqlite.BackupIndex.Create(this.tempIndex.Path, header);
            this.backupIndex.InsertBlob(
               new Backup.Blob()
               {
                  Name = System.IO.Path.GetFileName(this.BlobPath)
               }
            );
            // copy the initial version of the index to the archive path
            Save();
         }
         catch
         {
            Dispose();
            try { Directory.Delete(this.Path, true); } catch { }
            throw;
         }
      }
      public void Open ()
      {
         try
         {
            this.tempIndex = IO.FileSystem.Temp();
            File.Copy(this.IndexPath, this.tempIndex.Path, true);
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
         using (Stream indexStream = IO.FileSystem.Truncate(this.IndexPath))
         using (Stream tempStream = this.backupIndex.Serialize())
            tempStream.CopyTo(indexStream);
      }
      #endregion

      #region IArchive Implementation
      public String Name
      {
         get { return System.IO.Path.GetFileName(this.Path); }
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
