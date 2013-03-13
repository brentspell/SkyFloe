//===========================================================================
// MODULE:  FileSystemBackup.cs
// PURPOSE: file-based backup implementation
// 
// Copyright © 2013
// Brent M. Spell. All rights reserved.
//
// This library is free software; you can redistribute it and/or modify it 
// under the terms of the GNU Lesser General Public License as published 
// by the Free Software Foundation; either version 3 of the License, or 
// (at your option) any later version. This library is distributed in the 
// hope that it will be useful, but WITHOUT ANY WARRANTY; without even the 
// implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
// See the GNU Lesser General Public License for more details. You should 
// have received a copy of the GNU Lesser General Public License along with 
// this library; if not, write to 
//    Free Software Foundation, Inc. 
//    51 Franklin Street, Fifth Floor 
//    Boston, MA 02110-1301 USA
//===========================================================================
// System References
using System;
using System.Collections.Generic;
using System.Linq;
using Stream = System.IO.Stream;
// Project References
using SkyFloe.Store;

namespace SkyFloe
{
   /// <summary>
   /// File system backup
   /// </summary>
   /// <remarks>
   /// This class implements the IBackup interface for adding new entries
   /// to a backup archive. New files are written to the blob.dat file
   /// in sequential order, with offsets/lengths being recorded in the
   /// backup entry.
   /// </remarks>
   public class FileSystemBackup : IBackup
   {
      private FileSystemArchive archive;
      private Stream blobFile;
      private IO.StreamCopier copier;

      /// <summary>
      /// Initializes a new backup instance
      /// </summary>
      /// <param name="archive">
      /// The file system archive for the backup
      /// </param>
      /// <param name="session">
      /// The backup session being processed
      /// </param>
      public FileSystemBackup (FileSystemArchive archive, Backup.Session session)
      {
         this.archive = archive;
         this.blobFile = IO.FileSystem.Append(archive.BlobPath);
         this.copier = new IO.StreamCopier();
      }
      /// <summary>
      /// Releases the resources associated with the backup
      /// </summary>
      public void Dispose ()
      {
         if (this.blobFile != null)
            this.blobFile.Dispose();
         this.archive = null;
         this.blobFile = null;
      }

      #region IBackup Implementation
      /// <summary>
      /// Adds a file to the archive
      /// </summary>
      /// <param name="entry">
      /// The backup metadata for the file
      /// </param>
      /// <param name="stream">
      /// The file stream to add to the archive
      /// </param>
      public void Backup (Backup.Entry entry, Stream stream)
      {
         // sync the blob file position with the blob length,
         // to avoid wasting storage when recovering from faults
         var blob = this.archive.BackupIndex.FetchBlob(1);
         this.blobFile.Position = blob.Length;
         // transfer the file data to the blob
         this.copier.Copy(stream, this.blobFile);
         // update the backup entry metadata
         entry.Blob = blob;
         entry.Offset = blob.Length;
         entry.Length = this.blobFile.Position - entry.Offset;
      }
      /// <summary>
      /// Commits all outstanding changes to the archive
      /// </summary>
      public void Checkpoint ()
      {
         this.blobFile.Flush();
         this.archive.Save();
      }
      #endregion
   }
}
