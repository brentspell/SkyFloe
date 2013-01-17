using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SkyFloe.Store;

namespace SkyFloe.Aws
{
   public class GlacierArchive : IArchive
   {
      public const String IndexS3KeyExtension = ".db.gz";
      public const Int32 PartSize = 16 * 1024 * 1024;
      private Amazon.S3.AmazonS3 s3;
      private Amazon.Glacier.AmazonGlacierClient glacier;
      private String vault;
      private String bucket;
      private String name;
      private String indexPath;
      private Sqlite.SqliteIndex index;
      private GlacierUploader uploader;

      private String IndexS3Key
      {
         get { return String.Format("{0}{1}", this.name, IndexS3KeyExtension); }
      }

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
         this.indexPath = Path.GetTempFileName();
      }

      public void Dispose ()
      {
         if (this.index != null)
            this.index.Dispose();
         if (this.indexPath != null)
            Sqlite.SqliteIndex.Delete(this.indexPath);
         this.index = null;
         this.indexPath = null;
      }

      public void Create (Model.Header header)
      {
         // create the Glacier vault, fail if it exists
         // create the S3 bucket, if it does not exist
         // create the local index file
         // sync the initialized archive
         this.glacier.CreateVault(
            new Amazon.Glacier.Model.CreateVaultRequest()
            {
               VaultName = this.vault
            }
         );
         this.s3.PutBucket(
            new Amazon.S3.Model.PutBucketRequest()
            {
               BucketName = this.bucket
            }
         );
         this.index = Sqlite.SqliteIndex.Create(this.indexPath, header);
      }
      public void Open ()
      {
         using (var indexStream = new FileStream(this.indexPath, FileMode.Open, FileAccess.Write, FileShare.None))
         using (var s3Object = 
               this.s3.GetObject(
                  new Amazon.S3.Model.GetObjectRequest()
                  {
                     BucketName = this.bucket,
                     Key = this.IndexS3Key
                  }
               ).ResponseStream
            )
         using (var gzip = new GZipStream(s3Object, CompressionMode.Decompress))
            gzip.CopyTo(indexStream);
         this.index = Sqlite.SqliteIndex.Open(this.indexPath);
      }

      #region IArchive Implementation
      public String Name
      {
         get { return this.name; }
      }
      public IIndex Index
      {
         get { return this.index; }
      }
      public void PrepareBackup ()
      {
      }
      public void BackupEntry (Model.Entry entry, Stream stream)
      {
         if (this.uploader == null)
         {
            this.uploader = new GlacierUploader(
               this.glacier, 
               this.vault,
               PartSize
            );
            this.index.InsertBlob(
               new Model.Blob()
               {
                  Name = this.uploader.UploadID
               }
            );
         }
         var blob = this.index.LookupBlob(this.uploader.UploadID);
         if (blob.Length != this.uploader.Length)
            this.uploader.Resync(blob.Length);
         var offset = blob.Length = this.uploader.Length;
         var length = this.uploader.Upload(stream);
         entry.Blob = blob;
         entry.Offset = offset;
         entry.Length = length;
      }
      public void Checkpoint ()
      {
         if (this.uploader != null)
         {
            var blob = this.index.LookupBlob(this.uploader.UploadID);
            this.uploader.Flush();
            blob.Length = this.uploader.Length;
            var archiveID = this.uploader.Complete();
            blob.Name = archiveID;
            this.index.UpdateBlob(blob);
            this.uploader = null;
         }
         using (var s3File = new FileStream(Path.GetTempFileName(), FileMode.Open, FileAccess.ReadWrite, FileShare.None, 65536, FileOptions.DeleteOnClose))
         {
            using (var gzip = new GZipStream(s3File, CompressionMode.Compress, true))
            using (var idx = this.index.Serialize())
               idx.CopyTo(gzip);
            s3File.Position = 0;
            this.s3.PutObject(
               new Amazon.S3.Model.PutObjectRequest()
               {
                  BucketName = this.bucket,
                  Key = this.IndexS3Key,
                  InputStream = s3File
               }
            );
         }
      }
      public void PrepareRestore (IEnumerable<Int32> entries)
      {
         throw new NotImplementedException();
      }
      public Stream RestoreEntry (Model.Entry entry)
      {
         throw new NotImplementedException();
      }
      #endregion
   }
}
