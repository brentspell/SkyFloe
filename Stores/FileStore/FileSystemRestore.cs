using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using SkyFloe.Store;

namespace SkyFloe
{
   public class FileSystemRestore : IRestore
   {
      Stream blobFile;
      
      public FileSystemRestore (String blobPath)
      {
         this.blobFile = IO.FileSystem.Open(blobPath);
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
         return new IO.SubStream(this.blobFile, entry.Offset, entry.Length);
      }
      #endregion
   }
}
