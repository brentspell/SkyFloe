//===========================================================================
// MODULE:  GlacierBackup.cs
// PURPOSE: AWS glacier backup implementation
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
using System.IO.Compression;
using System.Linq;
using Stream = System.IO.Stream;
// Project References
using SkyFloe.Store;

namespace SkyFloe.Aws
{
   /// <summary>
   /// Glacier backup
   /// </summary>
   /// <remarks>
   /// This class implements the IBackup interface for AWS Glacier. It uses
   /// the GlacierUploader class to transfer incoming backup entries to a
   /// Glacier vault archive. It maintains the metadata for each Glacier
   /// vault archive as a blob within the backup index.
   /// </remarks>
   public class GlacierBackup : IBackup
   {
      private GlacierArchive archive;
      private GlacierUploader uploader;

      /// <summary>
      /// Initializes a new backup instance
      /// </summary>
      /// <param name="archive">
      /// The SkyFloe archive for the backup
      /// </param>
      /// <param name="session">
      /// The backup session being processed
      /// </param>
      public GlacierBackup (GlacierArchive archive, Backup.Session session)
      {
         this.archive = archive;
      }
      /// <summary>
      /// Releases the resources associated with the backup
      /// </summary>
      public void Dispose ()
      {
         if (this.uploader != null)
            this.uploader.Dispose();
         this.uploader = null;
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
         // if we are not currently uploading a vault archive,
         // then attach a new uploader and create a blob for it
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
         // fetch the upload blob and sync it with the uploader's offset
         var blob = this.archive.BackupIndex.LookupBlob(this.uploader.UploadID);
         if (blob.Length != this.uploader.Length)
            blob.Length = this.uploader.Resync(blob.Length);
         // upload the incoming stream and update the backup entry
         var offset = this.uploader.Length;
         var length = this.uploader.Upload(stream);
         entry.Blob = blob;
         entry.Offset = offset;
         entry.Length = length;
      }
      /// <summary>
      /// Commits the current vault archive to Glacier
      /// </summary>
      public void Checkpoint ()
      {
         if (this.uploader != null)
         {
            // complete the vault archive and update the blob
            var blob = this.archive.BackupIndex.LookupBlob(this.uploader.UploadID);
            this.uploader.Flush();
            blob.Length = this.uploader.Length;
            var archiveID = this.uploader.Complete();
            blob.Name = archiveID;
            this.archive.BackupIndex.UpdateBlob(blob);
            // detach the uploader for the current vault archive
            this.uploader.Dispose();
            this.uploader = null;
         }
         this.archive.Save();
      }
      #endregion
   }
}
