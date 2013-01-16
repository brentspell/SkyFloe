using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SkyFloe.Store;

namespace SkyFloe.Aws
{
   public class GlacierStore : IStore
   {
      private Amazon.S3.AmazonS3 s3;
      private Amazon.Glacier.AmazonGlacierClient glacier;

      #region Connection Properties
      public String AccessKey { get; set; }
      public String SecretKey { get; set; }
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
      public void Open ()
      {
         var credentials = new Amazon.Runtime.BasicAWSCredentials(
            this.AccessKey,
            this.SecretKey
         );
         if (this.Bucket == null)
            this.Bucket = "SkyFloe";
         this.s3 = Amazon.AWSClientFactory.CreateAmazonS3Client(credentials);
         this.glacier = new Amazon.Glacier.AmazonGlacierClient(credentials);
         this.s3.PutBucket(
            new Amazon.S3.Model.PutBucketRequest()
            {
               BucketName = this.Bucket
            }
         );
      }
      public IEnumerable<String> ListArchives ()
      {
         return this.s3
            .ListObjects(
               new Amazon.S3.Model.ListObjectsRequest()
               {
                  BucketName = this.Bucket
               }
            ).S3Objects
            .Where(o => o.Key.EndsWith(GlacierArchive.IndexS3KeyExtension))
            .Select(o => o.Key.Substring(0, o.Key.LastIndexOf(GlacierArchive.IndexS3KeyExtension)));
      }
      public IArchive CreateArchive (String name, Model.Header header)
      {
         var archive = new GlacierArchive(
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
         var archive = new GlacierArchive(
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
         var vault = this.VaultPrefix + name;
         var blobs = new List<String>();
         try
         {
            using (var archive = OpenArchive(name))
               blobs.AddRange(archive.Index.ListBlobs().Select(b => b.Name));
         }
         catch { }
         foreach (var blob in blobs)
            this.glacier.DeleteArchive(
               new Amazon.Glacier.Model.DeleteArchiveRequest()
               {
                  VaultName = vault,
                  ArchiveId = blob
               }
            );
         try
         {
            this.glacier.DeleteVault(
               new Amazon.Glacier.Model.DeleteVaultRequest()
               {
                  VaultName = vault
               }
            );
         }
         catch { }
         this.s3.DeleteObject(
            new Amazon.S3.Model.DeleteObjectRequest()
            {
               BucketName = this.Bucket,
               Key = String.Format("{0}{1}", name, GlacierArchive.IndexS3KeyExtension)
            }
         );
      }
      #endregion
   }
}
