using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SkyFloe
{
   internal class FileArchive : Store.IArchive
   {
      private Sqlite.BackupIndex index;
      private Stream blobFile;
      private String tempIndexPath;
      private String tempSessionPath;

      public FileArchive ()
      {
         this.tempIndexPath = System.IO.Path.GetTempFileName();
      }

      public void Dispose ()
      {
         if (this.index != null)
            this.index.Dispose();
         if (this.blobFile != null)
            this.blobFile.Dispose();
         if (this.tempIndexPath != null)
            Sqlite.BackupIndex.Delete(this.tempIndexPath);
         if (this.tempSessionPath != null)
            Sqlite.RestoreSession.Delete(this.tempSessionPath);
         this.index = null;
         this.blobFile = null;
         this.tempIndexPath = null;
      }

      public String Path
      { 
         get; set;
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
            this.index = Sqlite.BackupIndex.Create(this.tempIndexPath, header);
            this.index.InsertBlob(
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
            File.Copy(this.IndexPath, this.tempIndexPath, true);
            this.index = Sqlite.BackupIndex.Open(this.tempIndexPath);
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
      public Store.IBackupIndex Index
      {
         get { return this.index; }
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
         var blob = this.index.FetchBlob(1);
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
         using (var ckptIndex = new FileStream(this.IndexPath, FileMode.Create, FileAccess.Write, FileShare.Read))
         using (var tempIndex = this.index.Serialize())
            tempIndex.CopyTo(ckptIndex);
      }
      public Store.IRestoreSession PrepareRestore (IEnumerable<Int32> entries)
      {
         this.blobFile = new FileStream(
            this.BlobPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read
         );
         var session = Sqlite.RestoreSession.Create(
            this.tempSessionPath = System.IO.Path.GetTempFileName(),
            new Restore.Header()
            {
               Archive = this.Name
            }
         );
         try
         {
            foreach (var entryID in entries)
            {
               var backupEntry = this.index.FetchEntry(entryID);
               var retrieval = 
                  session
                     .ListBlobRetrievals(backupEntry.Blob.ID)
                     .FirstOrDefault() ??
                  session.InsertRetrieval(
                     new Restore.Retrieval()
                     {
                        BlobID = backupEntry.Blob.ID,
                        Offset = 0,
                        Length = backupEntry.Blob.Length
                     }
                  );
               session.InsertEntry(
                  new Restore.Entry()
                  {
                     Retrieval = retrieval,
                     State = Restore.EntryState.Pending,
                     Offset = backupEntry.Offset,
                     Length = backupEntry.Length
                  }
               );
            }
            return session;
         }
         catch
         {
            session.Dispose();
            throw;
         }
      }
      public Store.IRestoreSession AttachRestore (Stream stream)
      {
         this.tempSessionPath = System.IO.Path.GetTempFileName();
         using (var tempStream =
            new FileStream(
               this.tempSessionPath,
               FileMode.Open,
               FileAccess.Write,
               FileShare.Read
            )
         )
            stream.CopyTo(tempStream);
         var session = Sqlite.RestoreSession.Open(this.tempSessionPath);
         try
         {
            if (session.FetchHeader().Archive != this.Name)
               throw new InvalidOperationException("TODO: invalid archive name");
            return session;
         }
         catch
         {
            session.Dispose();
            throw;
         }
      }
      public Stream RestoreEntry (Backup.Entry entry)
      {
         return new IO.SubStream(this.blobFile, entry.Offset, entry.Length);
      }
      #endregion
   }
}
