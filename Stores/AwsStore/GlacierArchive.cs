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
      Amazon.S3.AmazonS3 s3;
      Amazon.Glacier.AmazonGlacierClient glacier;
      String vault;
      String bucket;
      String name;
      String indexPath;
      Sqlite.SqliteIndex index;
      Byte[] partBuffer;
      Int32 partLength;
      Stream partStream;
      List<String> partChecksums;
      Model.Blob currentBlob;
      Int64 blobOffset;

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
      public Stream LoadEntry (Model.Entry entry)
      {
         throw new NotImplementedException();
      }
      public void StoreEntry (Model.Entry entry, Stream stream)
      {
         if (this.currentBlob == null)
         {
            if (this.partBuffer == null)
               this.partBuffer = new Byte[16 * 1024 * 1024];
            this.partStream = new MemoryStream(this.partBuffer);
            this.partLength = 0;
            this.partChecksums = new List<String>();
            var partUploadID = this.glacier.InitiateMultipartUpload(
               new Amazon.Glacier.Model.InitiateMultipartUploadRequest()
               {
                  VaultName = this.vault,
                  PartSize = this.partBuffer.Length
               }
            ).InitiateMultipartUploadResult.UploadId;
            this.currentBlob = this.index.InsertBlob(
               new Model.Blob()
               {
                  Name = partUploadID
               }
            );
         }
         var entryOffset = this.currentBlob.Length;
         var entryLength = 0;
         for (; ; )
         {
            if (this.partLength == this.partBuffer.Length)
            {
               this.partStream.Position = 0;
               var checksum = Amazon.Glacier.TreeHashGenerator.CalculateTreeHash(this.partStream);
               this.partChecksums.Add(checksum);
               this.partStream.Position = 0;
               this.glacier.UploadMultipartPart(
                  new Amazon.Glacier.Model.UploadMultipartPartRequest()
                  {
                     VaultName = this.vault,
                     UploadId = this.currentBlob.Name,
                     Body = this.partStream,
                     Range = String.Format(
                        "bytes {0}-{1}/*",
                        this.blobOffset,
                        this.blobOffset + this.partLength - 1
                     ),
                     Checksum = checksum
                  }
               );
               this.blobOffset += this.partLength;
               this.partLength = 0;
            }
            var read = stream.Read(
               this.partBuffer,
               this.partLength,
               this.partBuffer.Length - this.partLength);
            if (read == 0)
               break;
            this.partLength += read;
            entryLength += read;
         }
         entry.Blob = this.currentBlob;
         entry.Offset = entryOffset;
         entry.Length = entryLength;
      }
      public void Checkpoint ()
      {
         if (this.currentBlob != null)
         {
            if (this.partLength > 0)
            {
               this.partStream.SetLength(this.partLength);
               this.partStream.Position = 0;
               var checksum = Amazon.Glacier.TreeHashGenerator.CalculateTreeHash(this.partStream);
               this.partChecksums.Add(checksum);
               this.partStream.Position = 0;
               this.glacier.UploadMultipartPart(
                  new Amazon.Glacier.Model.UploadMultipartPartRequest()
                  {
                     VaultName = this.vault,
                     UploadId = this.currentBlob.Name,
                     Body = this.partStream,
                     Range = String.Format(
                        "bytes {0}-{1}/*",
                        this.blobOffset,
                        this.blobOffset + this.partLength - 1
                     ),
                     Checksum = checksum
                  }
               );
            }
            this.currentBlob.Name = this.glacier.CompleteMultipartUpload(
               new Amazon.Glacier.Model.CompleteMultipartUploadRequest()
               {
                  VaultName = this.vault,
                  UploadId = this.currentBlob.Name,
                  ArchiveSize = this.currentBlob.Length.ToString(),
                  Checksum = Amazon.Glacier.TreeHashGenerator.CalculateTreeHash(this.partChecksums)
               }
            ).CompleteMultipartUploadResult.ArchiveId;
            this.index.UpdateBlob(this.currentBlob);
            this.partStream = null;
            this.partLength = 0;
            this.partChecksums = null;
            this.currentBlob = null;
            this.blobOffset = 0;
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
      #endregion
   }
}
