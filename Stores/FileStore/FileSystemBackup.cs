using System;
using System.Collections.Generic;
using System.Linq;
using Stream = System.IO.Stream;

using SkyFloe.Store;

namespace SkyFloe
{
   public class FileSystemBackup : IBackup
   {
      FileSystemArchive archive;
      Stream blobFile;

      public FileSystemBackup (FileSystemArchive archive, Backup.Session session)
      {
         this.archive = archive;
         this.blobFile = IO.FileSystem.Append(archive.BlobPath);
      }

      public void Dispose ()
      {
         if (this.blobFile != null)
            this.blobFile.Dispose();
         this.archive = null;
         this.blobFile = null;
      }

      #region IBackup Implementation
      public void Backup (Backup.Entry entry, Stream stream)
      {
         var blob = this.archive.BackupIndex.FetchBlob(1);
         this.blobFile.Position = blob.Length;
         stream.CopyTo(this.blobFile);
         entry.Blob = blob;
         entry.Offset = blob.Length;
         entry.Length = this.blobFile.Position - entry.Offset;
      }
      public void Checkpoint ()
      {
         this.blobFile.Flush();
         this.archive.Save();
      }
      #endregion
   }
}
