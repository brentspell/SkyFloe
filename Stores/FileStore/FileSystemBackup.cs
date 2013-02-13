﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
         Backup.Blob blob = this.archive.BackupIndex.FetchBlob(1);
         this.blobFile.Seek(blob.Length, SeekOrigin.Begin);
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