using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.IdentityManagement;
using Amazon.Glacier;
using Amazon.Glacier.Model;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

using SkyFloe.Store;

namespace SkyFloe.Aws
{
   public class GlacierStore : IStore
   {
      private AmazonS3 s3;
      private AmazonGlacierClient glacier;
      private String arn;

      public GlacierStore ()
      {
         this.Bucket = "SkyFloe";
      }

      #region Connection Properties
      [Required]
      [RegularExpression(@"^[0-9A-Za-z]{20}$")]
      public String AccessKey { get; set; }
      [Required]
      [RegularExpression(@"^[0-9A-Za-z/+]{40}$")]
      public String SecretKey { get; set; }
      [Required]
      [RegularExpression(@"^[0-9A-Za-z_\-.]{1,255}$")]
      public String Bucket { get; set; }
      #endregion

      private String VaultPrefix
      {
         get { return this.Bucket + "-"; }
      }

      public void Dispose ()
      {
         if (this.s3 != null)
            this.s3.Dispose();
         if (this.glacier != null)
            this.glacier.Dispose();
         this.s3 = null;
         this.glacier = null;
      }

      #region IStore Implementation
      public String Caption
      {
         get { return String.Format("Amazon Glacier - {0}", this.arn); }
      }

      public void Open ()
      {
         BasicAWSCredentials credentials = new BasicAWSCredentials(
            this.AccessKey,
            this.SecretKey
         );
         this.arn = new AmazonIdentityManagementServiceClient(credentials)
            .GetUser().GetUserResult.User.Arn;
         this.s3 = Amazon.AWSClientFactory.CreateAmazonS3Client(credentials);
         this.glacier = new AmazonGlacierClient(credentials);
         this.s3.PutBucket(
            new PutBucketRequest()
            {
               BucketName = this.Bucket
            }
         );
      }
      public IEnumerable<String> ListArchives ()
      {
         return this.s3
            .ListObjects(
               new ListObjectsRequest()
               {
                  BucketName = this.Bucket
               }
            ).S3Objects
            .Where(o => o.Key.EndsWith(GlacierArchive.IndexS3KeyExtension))
            .Select(o => o.Key.Substring(0, o.Key.LastIndexOf(GlacierArchive.IndexS3KeyExtension)));
      }
      public IArchive CreateArchive (String name, Backup.Header header)
      {
         GlacierArchive archive = new GlacierArchive(
            this.s3,
            this.glacier,
            this.VaultPrefix + name,
            this.Bucket,
            name
         );
         archive.Create(header);
         return archive;
      }
      public IArchive OpenArchive (String name)
      {
         GlacierArchive archive = new GlacierArchive(
            this.s3,
            this.glacier,
            this.VaultPrefix + name,
            this.Bucket,
            name
         );
         archive.Open();
         return archive;
      }
      public void DeleteArchive (String name)
      {
         // TODO: consider not using archive implementation here
         // TODO: or consider moving all delete code into archive
         String vault = this.VaultPrefix + name;
         List<String> blobs = new List<String>();
         try
         {
            using (Store.IArchive archive = OpenArchive(name))
               blobs.AddRange(archive.BackupIndex.ListBlobs().Select(b => b.Name));
         }
         catch { }
         foreach (String blob in blobs)
            this.glacier.DeleteArchive(
               new DeleteArchiveRequest()
               {
                  VaultName = vault,
                  ArchiveId = blob
               }
            );
         try
         {
            this.glacier.DeleteVault(
               new DeleteVaultRequest()
               {
                  VaultName = vault
               }
            );
         }
         catch { }
         this.s3.DeleteObject(
            new DeleteObjectRequest()
            {
               BucketName = this.Bucket,
               Key = String.Format("{0}{1}", name, GlacierArchive.IndexS3KeyExtension)
            }
         );
      }
      #endregion
   }
}
