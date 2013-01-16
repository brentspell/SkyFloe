using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SkyFloe
{
   internal class FileArchive : Store.IArchive
   {
      private Sqlite.SqliteIndex index;
      private Stream blobFile;
      private String tempIndexPath;

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
            Sqlite.SqliteIndex.Delete(this.tempIndexPath);
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
      public void Create (Model.Header header)
      {
         try
         {
            Directory.CreateDirectory(this.Path);
            this.index = Sqlite.SqliteIndex.Create(this.tempIndexPath, header);
            this.index.InsertBlob(
               new Model.Blob()
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
            this.index = Sqlite.SqliteIndex.Open(this.tempIndexPath);
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
      public Store.IIndex Index
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
      public void StoreEntry (Model.Entry entry, Stream stream)
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
      public void PrepareRestore (IList<Store.BlobRestore> blobs)
      {
         this.blobFile = new FileStream(
            this.BlobPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read
         );
      }
      public Stream LoadEntry (Model.Entry entry)
      {
         return new IO.SubStream(this.blobFile, entry.Offset, entry.Length);
      }
      #endregion
   }
}
