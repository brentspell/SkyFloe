//===========================================================================
// MODULE:  GlacierArchive.cs
// PURPOSE: AWS Glacier backup archive
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
// Project References
using SkyFloe.Store;

namespace SkyFloe.Aws
{
   /// <summary>
   /// Glacier backup archive
   /// </summary>
   /// <remarks>
   /// This class represents a named backup container within the Glacier 
   /// store. The archive consists of an S3 object containing the backup
   /// index and a Glacier vault containing the backup blobs.
   /// The backup index is a Sqlite database (see the Sqlite project)
   /// containing the metadata for the backup archive, including the blob
   /// references to the Glacier vault archives. Each Blob record in the
   /// backup index corresponds to a vault archive, with the blob name
   /// matching the vault archive ID.
   /// The backup index is maintained within a temporary path during backup
   /// operations and only copied up to S3 during checkpoint, to ensure the 
   /// consistency of the index when faults occur and minimize AWS traffic.
   /// The archive also provides access to the restore index, stored in the
   /// local file system per user. This restore index provides for pausing/
   /// resuming restore processes (similar to the backup index) and recovery
   /// from crashes during restore.
   /// </remarks>
   public class GlacierArchive : IArchive
   {
      public const String IndexS3KeyExtension = ".db.gz";
      private Amazon.S3.AmazonS3 s3;
      private Amazon.Glacier.AmazonGlacierClient glacier;
      private String vault;
      private String bucket;
      private String name;
      private IO.FileSystem.TempStream backupIndexFile;
      private IBackupIndex backupIndex;
      private IRestoreIndex restoreIndex;

      /// <summary>
      /// Initializes a new archive instance
      /// </summary>
      /// <param name="s3">
      /// The connected AWS S3 client
      /// </param>
      /// <param name="glacier">
      /// The connected AWS Glacier client
      /// </param>
      /// <param name="vault">
      /// The name of the Glacier vault for the archive
      /// </param>
      /// <param name="bucket">
      /// The name of the S3 bucket containing the backup index
      /// </param>
      /// <param name="name">
      /// The name of the archive
      /// </param>
      public GlacierArchive (
         Amazon.S3.AmazonS3 s3, 
         Amazon.Glacier.AmazonGlacierClient glacier,
         String vault,
         String bucket,
         String name)
      {
         this.s3 = s3;
         this.glacier = glacier;
         this.vault = vault;
         this.bucket = bucket;
         this.name = name;
         // connect to the restore index
         var restoreIndexPath = new IO.Path(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SkyFloe",
            "AwsGlacier",
            name,
            "restore.db"
         );
         IO.FileSystem.CreateDirectory(restoreIndexPath.Parent);
         this.restoreIndex = (IO.FileSystem.GetMetadata(restoreIndexPath).Exists) ?
            Sqlite.RestoreIndex.Open(restoreIndexPath) :
            Sqlite.RestoreIndex.Create(restoreIndexPath, new Restore.Header());
      }
      /// <summary>
      /// Releases the resources associated with the archive
      /// </summary>
      public void Dispose ()
      {
         if (this.backupIndex != null)
            this.backupIndex.Dispose();
         if (this.restoreIndex != null)
            this.restoreIndex.Dispose();
         if (this.backupIndexFile != null)
            this.backupIndexFile.Dispose();
         this.backupIndexFile = null;
         this.backupIndex = null;
         this.restoreIndex = null;
      }

      /// <summary>
      /// The AWS Glacier client
      /// </summary>
      public Amazon.Glacier.AmazonGlacierClient Glacier
      {
         get { return this.glacier; }
      }
      /// <summary>
      /// The Glacier vault name
      /// </summary>
      public String Vault
      {
         get { return this.vault; }
      }
      /// <summary>
      /// The S3 backup index key
      /// </summary>
      private String IndexS3Key
      {
         get { return String.Format("{0}{1}", this.name, IndexS3KeyExtension); }
      }
      
      /// <summary>
      /// Creates a new archive
      /// </summary>
      /// <param name="header">
      /// The backup index header to insert
      /// </param>
      public void Create (Backup.Header header)
      {
         // create the Glacier vault, fail if it exists
         this.glacier.CreateVault(
            new Amazon.Glacier.Model.CreateVaultRequest()
            {
               VaultName = this.vault
            }
         );
         // create the local backup index file
         this.backupIndexFile = IO.FileSystem.Temp();
         this.backupIndex = Sqlite.BackupIndex.Create(
            this.backupIndexFile.Path, 
            header
         );
         // sync the initialized archive to the S3 bucket
         Save();
      }
      /// <summary>
      /// Opens an existing archive
      /// </summary>
      public void Open ()
      {
         // download the existing backup index
         this.backupIndexFile = IO.FileSystem.Temp();
         using (var s3Stream = this.s3.GetObject(
               new Amazon.S3.Model.GetObjectRequest()
               {
                  BucketName = this.bucket,
                  Key = this.IndexS3Key
               }
            ).ResponseStream
         )
         // extract and open the index
         using (var gzip = new GZipStream(s3Stream, CompressionMode.Decompress))
            gzip.CopyTo(this.backupIndexFile);
         this.backupIndex = Sqlite.BackupIndex.Open(this.backupIndexFile.Path);
      }
      /// <summary>
      /// Checkpoints the archive
      /// </summary>
      public void Save ()
      {
         using (var checkpointStream = IO.FileSystem.Temp())
         {
            // compress the backup index to a temporary file
            using (var gzipStream = new GZipStream(
                  checkpointStream, 
                  CompressionMode.Compress, 
                  true
               )
            )
            using (var indexStream = this.BackupIndex.Serialize())
               indexStream.CopyTo(gzipStream);
            // upload the backup index to S3
            checkpointStream.Position = 0;
            this.s3.PutObject(
               new Amazon.S3.Model.PutObjectRequest()
               {
                  BucketName = this.bucket,
                  Key = this.IndexS3Key,
                  InputStream = checkpointStream
               }
            );
         }
      }

      #region IArchive Implementation
      /// <summary>
      /// The archive name
      /// </summary>
      public String Name
      {
         get { return this.name; }
      }
      /// <summary>
      /// The archive backup index
      /// </summary>
      public IBackupIndex BackupIndex
      {
         get { return this.backupIndex; }
      }
      /// <summary>
      /// The archive restore index
      /// </summary>
      public Store.IRestoreIndex RestoreIndex
      {
         get { return this.restoreIndex; } 
      }
      /// <summary>
      /// Prepares the archive for a new backup process and returns
      /// an object used to add entries to the archive
      /// </summary>
      /// <param name="session">
      /// The new backup session
      /// </param>
      /// <returns>
      /// The archive backup implementation
      /// </returns>
      public IBackup PrepareBackup (Backup.Session session)
      {
         return new GlacierBackup(this, session);
      }
      /// <summary>
      /// Prepares the archive for a new restore process and returns
      /// an object used to restore entries from the archive
      /// </summary>
      /// <param name="session">
      /// The new restore session
      /// </param>
      /// <returns>
      /// The archive restore implementation
      /// </returns>
      public IRestore PrepareRestore (Restore.Session session)
      {
         return new GlacierRestore(this, session);
      }
      #endregion
   }
}
