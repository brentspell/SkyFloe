using System;
using System.Collections.Generic;
using System.Linq;
using Stream = System.IO.Stream;

using SkyFloe.Store;

namespace SkyFloe
{
   public class FileSystemRestore : IRestore
   {
      Stream blobFile;

      public FileSystemRestore (FileSystemArchive archive, Restore.Session session)
      {
         this.blobFile = IO.FileSystem.Open(archive.BlobPath);
      }

      public void Dispose ()
      {
         if (this.blobFile != null)
            this.blobFile.Dispose();
         this.blobFile = null;
      }

      #region IRestore Implementation
      public Stream Restore (Restore.Entry entry)
      {
         return new IO.Substream(this.blobFile, entry.Offset, entry.Length);
      }
      #endregion
   }
}
