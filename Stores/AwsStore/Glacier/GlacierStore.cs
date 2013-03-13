//===========================================================================
// MODULE:  GlacierStore.cs
// PURPOSE: AWS Glacier backup store
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
using System.ComponentModel.DataAnnotations;
using System.Linq;
// AWS References
using Amazon.IdentityManagement;
using Amazon.Glacier;
using Amazon.Glacier.Model;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
// Project References
using SkyFloe.Store;

namespace SkyFloe.Aws
{
   /// <summary>
   /// Glacier backup store
   /// </summary>
   /// <remarks>
   /// This class is the entry point to the SkyFloe AWS Glacier backup
   /// plugin. The store manages a list of SkyFloe archives, each represented
   /// by a Glacier vault containing backup blobs and an S3 file containing
   /// the backup index. The S3 index file is stored within a bucket named
   /// by the Bucket property (default = SkyFloe). The Glacier vault is named 
   /// {Bucket}-{Archive}.
   /// Due to the ambiguity of the SkyFloe archive and the Glacier archive
   /// terms, Glacier archives are always referred to within this 
   /// documentation as "vault archives."
   /// </remarks>
   public class GlacierStore : IStore
   {
      private AmazonS3 s3;
      private AmazonGlacierClient glacier;
      private String arn;

      /// <summary>
      /// Initializes a new store instance
      /// </summary>
      public GlacierStore ()
      {
         this.Bucket = "SkyFloe";
      }
      /// <summary>
      /// Releases the resources associated with the store
      /// </summary>
      public void Dispose ()
      {
         if (this.s3 != null)
            this.s3.Dispose();
         if (this.glacier != null)
            this.glacier.Dispose();
         this.s3 = null;
         this.glacier = null;
      }

      #region Connection Properties
      /// <summary>
      /// The AWS user access key
      /// </summary>
      [Required]
      [RegularExpression(@"^[0-9A-Za-z]{20}$")]
      public String AccessKey { get; set; }
      /// <summary>
      /// The AWS user secret key
      /// </summary>
      [Required]
      [RegularExpression(@"^[0-9A-Za-z/+]{40}$")]
      public String SecretKey { get; set; }
      /// <summary>
      /// The S3 bucket name/vault prefix (default: SkyFloe)
      /// </summary>
      [Required]
      [RegularExpression(@"^[0-9A-Za-z_\-.]{1,255}$")]
      public String Bucket { get; set; }
      #endregion

      #region IStore Implementation
      /// <summary>
      /// The store friendly name
      /// </summary>
      public String Caption
      {
         get { return String.Format("Amazon Glacier - {0}", this.arn); }
      }
      /// <summary>
      /// Opens the connection to the store
      /// </summary>
      public void Open ()
      {
         // connect to AWS S3 and Glacier
         var credentials = new BasicAWSCredentials(
            this.AccessKey,
            this.SecretKey
         );
         this.arn = new AmazonIdentityManagementServiceClient(credentials)
            .GetUser().GetUserResult.User.Arn;
         this.s3 = Amazon.AWSClientFactory.CreateAmazonS3Client(credentials);
         this.glacier = new AmazonGlacierClient(credentials);
         // ensure the bucket for the store exists
         this.s3.PutBucket(
            new PutBucketRequest()
            {
               BucketName = this.Bucket
            }
         );
      }
      /// <summary>
      /// Retrieves the list of named archives within the store
      /// </summary>
      /// <returns>
      /// The archive name enumeration
      /// </returns>
      public IEnumerable<String> ListArchives ()
      {
         // iterate over the S3 objects within the SkyFloe bucket
         foreach (var obj in this.s3.ListObjects(
               new ListObjectsRequest()
               {
                  BucketName = this.Bucket
               }
            ).S3Objects
         )
         {
            // each archive index file is named <archive>.db.gz
            // strip off the extension to retrieve the archive name
            var extIdx = obj.Key.IndexOf(
               GlacierArchive.IndexS3KeyExtension,
               StringComparison.OrdinalIgnoreCase
            );
            if (extIdx != -1)
               yield return obj.Key.Substring(0, extIdx);
         }
      }
      /// <summary>
      /// Creates a new store archive
      /// </summary>
      /// <param name="name">
      /// The name of the archive to create
      /// </param>
      /// <param name="header">
      /// The backup header for the archive index
      /// </param>
      /// <returns>
      /// The connected archive implementation
      /// </returns>
      public IArchive CreateArchive (String name, Backup.Header header)
      {
         var archive = new GlacierArchive(
            this.s3,
            this.glacier,
            GetVaultName(name),
            this.Bucket,
            name
         );
         archive.Create(header);
         return archive;
      }
      /// <summary>
      /// Connects to an existing store archive
      /// </summary>
      /// <param name="name">
      /// The name of the archive to open
      /// </param>
      /// <returns>
      /// The connected archive implementation
      /// </returns>
      public IArchive OpenArchive (String name)
      {
         var archive = new GlacierArchive(
            this.s3,
            this.glacier,
            GetVaultName(name),
            this.Bucket,
            name
         );
         archive.Open();
         return archive;
      }
      /// <summary>
      /// Permanently removes an archive from the store
      /// </summary>
      /// <param name="name">
      /// The name of the archive to delete
      /// </param>
      public void DeleteArchive (String name)
      {
         // delete the vault archives
         List<String> blobs = new List<String>();
         try
         { 
            using (var archive = OpenArchive(name))
               blobs.AddRange(archive.BackupIndex.ListBlobs().Select(b => b.Name));
         }
         catch { }
         foreach (var blob in blobs)
            try
            {
               this.glacier.DeleteArchive(
                  new DeleteArchiveRequest()
                  {
                     VaultName = GetVaultName(name),
                     ArchiveId = blob
                  }
               );
            }
            catch { }
         // delete the archive vault
         try
         {
            this.glacier.DeleteVault(
               new DeleteVaultRequest()
               {
                  VaultName = GetVaultName(name)
               }
            );
         }
         catch { }
         // delete the S3 backup index blob
         this.s3.DeleteObject(
            new DeleteObjectRequest()
            {
               BucketName = this.Bucket,
               Key = String.Format("{0}{1}", name, GlacierArchive.IndexS3KeyExtension)
            }
         );
      }
      #endregion

      #region Utilities
      /// <summary>
      /// Constructs the Glacier vault name for a SkyFloe archive
      /// </summary>
      /// <param name="archive">
      /// The archive name
      /// </param>
      /// <returns>
      /// The AWS Glacier vault name
      /// </returns>
      private String GetVaultName (String archive)
      {
         return this.Bucket + "-" + archive;
      }
      #endregion
   }
}
