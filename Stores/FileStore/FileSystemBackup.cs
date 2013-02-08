using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using SkyFloe.Store;

namespace SkyFloe
{
   public class FileSystemBackup : IBackup
   {
      IBackupIndex index;
      String indexPath;
      Stream blobFile;
      
      public FileSystemBackup (
         IBackupIndex index, 
         String indexPath,
         String blobPath)
      {
         this.index = index;
         this.indexPath = indexPath;
         this.blobFile = IO.FileSystem.Append(blobPath);
      }

      public void Dispose ()
      {
         if (this.blobFile != null)
            this.blobFile.Dispose();
         this.index = null;
         this.blobFile = null;
      }

      #region IBackup Implementation
      public void Backup (Backup.Entry entry, Stream stream)
      {
         Backup.Blob blob = this.index.FetchBlob(1);
         this.blobFile.Seek(blob.Length, SeekOrigin.Begin);
         stream.CopyTo(this.blobFile);
         entry.Blob = blob;
         entry.Offset = blob.Length;
         entry.Length = this.blobFile.Position - entry.Offset;
      }
      public void Checkpoint ()
      {
         this.blobFile.Flush();
         using (Stream indexStream = IO.FileSystem.Truncate(this.indexPath))
         using (Stream tempStream = this.index.Serialize())
            tempStream.CopyTo(indexStream);
      }
      #endregion
   }
}
