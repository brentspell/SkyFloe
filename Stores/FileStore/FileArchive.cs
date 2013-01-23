using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SkyFloe
{
   internal class FileArchive : Store.IArchive
   {
      private Sqlite.BackupIndex backupIndex;
      private Sqlite.RestoreIndex restoreIndex;
      private Stream blobFile;
      private String tempBackupIndexPath;

      public FileArchive ()
      {
         this.tempBackupIndexPath = System.IO.Path.GetTempFileName();
      }

      public void Dispose ()
      {
         if (this.backupIndex != null)
            this.backupIndex.Dispose();
         if (this.restoreIndex != null)
            this.restoreIndex.Dispose();
         if (this.blobFile != null)
            this.blobFile.Dispose();
         if (this.tempBackupIndexPath != null)
            Sqlite.BackupIndex.Delete(this.tempBackupIndexPath);
         this.backupIndex = null;
         this.restoreIndex = null;
         this.blobFile = null;
         this.tempBackupIndexPath = null;
         String restoreIndexPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SkyFloe",
            "FileStore",
            "restore.db"
         );
         Directory.CreateDirectory(System.IO.Path.GetDirectoryName(restoreIndexPath));
         this.restoreIndex = (File.Exists(restoreIndexPath)) ?
            Sqlite.RestoreIndex.Open(restoreIndexPath) :
            Sqlite.RestoreIndex.Create(restoreIndexPath, new Restore.Header());
      }

      public String Path
      { 
         get; set;
      }
      public String BackupIndexPath
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
            this.backupIndex = Sqlite.BackupIndex.Create(this.tempBackupIndexPath, header);
            this.backupIndex.InsertBlob(
               new Backup.Blob()
               {
                  Name = "blob.dat"
               }
            );
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
            File.Copy(this.BackupIndexPath, this.tempBackupIndexPath, true);
            this.backupIndex = Sqlite.BackupIndex.Open(this.tempBackupIndexPath);
         }
         catch
         {
            Dispose();
            throw;
         }
      }
      #endregion

      #region IArchive Implementation
      public String Name
      {
         get { return System.IO.Path.GetFileName(this.Path); }
      }
      public Store.IBackupIndex BackupIndex
      {
         get { return this.backupIndex; }
      }
      public Store.IRestoreIndex RestoreIndex
      {
         get { return this.restoreIndex; }
      }
      public void PrepareBackup ()
      {
         this.blobFile = new FileStream(
            this.BlobPath,
            FileMode.OpenOrCreate,
            FileAccess.Write,
            FileShare.Read
         );
      }
      public void BackupEntry (Backup.Entry entry, Stream stream)
      {
         Backup.Blob blob = this.backupIndex.FetchBlob(1);
         this.blobFile.Seek(blob.Length, SeekOrigin.Begin);
         stream.CopyTo(this.blobFile);
         entry.Blob = blob;
         entry.Offset = blob.Length;
         entry.Length = this.blobFile.Position - entry.Offset;
      }
      public void Checkpoint ()
      {
         if (this.blobFile != null)
            this.blobFile.Flush();
         using (FileStream ckptIndex = new FileStream(this.BackupIndexPath, FileMode.Create, FileAccess.Write, FileShare.Read))
         using (Stream tempIndex = this.backupIndex.Serialize())
            tempIndex.CopyTo(ckptIndex);
      }
      public void PrepareRestore (Restore.Session session)
      {
         this.blobFile = new FileStream(
            this.BlobPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read
         );
      }
      public Stream RestoreEntry (Restore.Entry entry)
      {
         return new IO.SubStream(this.blobFile, entry.Offset, entry.Length);
      }
      #endregion
   }
}
