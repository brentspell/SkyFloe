using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

using SkyFloe.Store;

namespace SkyFloe.Aws
{
   public class GlacierBackup : IBackup
   {
      private GlacierArchive archive;
      private GlacierUploader uploader;

      public GlacierBackup (GlacierArchive archive, Backup.Session session)
      {
         this.archive = archive;
      }

      public void Dispose ()
      {
         if (this.uploader != null)
            this.uploader.Dispose();
         this.uploader = null;
      }

      #region IBackup Implementation
      public void Backup (Backup.Entry entry, Stream stream)
      {
         if (this.uploader == null)
         {
            this.uploader = new GlacierUploader(
               this.archive.Glacier,
               this.archive.Vault
            );
            this.archive.BackupIndex.InsertBlob(
               new Backup.Blob()
               {
                  Name = this.uploader.UploadID
               }
            );
         }
         var blob = this.archive.BackupIndex.LookupBlob(this.uploader.UploadID);
         if (blob.Length != this.uploader.Length)
            blob.Length = this.uploader.Resync(blob.Length);
         var offset = this.uploader.Length;
         var length = this.uploader.Upload(stream);
         entry.Blob = blob;
         entry.Offset = offset;
         entry.Length = length;
      }
      public void Checkpoint ()
      {
         if (this.uploader != null)
         {
            var blob = this.archive.BackupIndex.LookupBlob(this.uploader.UploadID);
            this.uploader.Flush();
            blob.Length = this.uploader.Length;
            var archiveID = this.uploader.Complete();
            blob.Name = archiveID;
            this.archive.BackupIndex.UpdateBlob(blob);
            this.uploader.Dispose();
            this.uploader = null;
         }
         this.archive.Save();
      }
      #endregion
   }
}
