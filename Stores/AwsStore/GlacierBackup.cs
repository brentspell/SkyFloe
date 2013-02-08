using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

using SkyFloe.Store;

namespace SkyFloe.Aws
{
   public class GlacierBackup : IBackup
   {
      public const Int32 PartSize = 16 * 1024 * 1024;
      private IBackupIndex index;
      private Amazon.S3.AmazonS3 s3;
      private Amazon.Glacier.AmazonGlacierClient glacier;
      private String vault;
      private String indexS3Bucket;
      private String indexS3Key;
      private GlacierUploader uploader;

      public GlacierBackup (
         IBackupIndex index,
         Amazon.S3.AmazonS3 s3,
         Amazon.Glacier.AmazonGlacierClient glacier,
         String vault,
         String indexS3Bucket,
         String indexS3Key)
      {
         this.index = index;
         this.s3 = s3;
         this.glacier = glacier;
         this.vault = vault;
         this.indexS3Bucket = indexS3Bucket;
         this.indexS3Key = indexS3Key;
      }

      public void Dispose ()
      {
         if (this.uploader != null)
            this.uploader.Dispose();
         this.uploader = null;
      }

      #region IBackup Implementation
      public void Backup (Backup.Entry entry, Stream stream)
      {
         if (this.uploader == null)
         {
            this.uploader = new GlacierUploader(
               this.glacier,
               this.vault,
               PartSize
            );
            this.index.InsertBlob(
               new Backup.Blob()
               {
                  Name = this.uploader.UploadID
               }
            );
         }
         Backup.Blob blob = this.index.LookupBlob(this.uploader.UploadID);
         if (blob.Length != this.uploader.Length)
            blob.Length = this.uploader.Resync(blob.Length);
         Int64 offset = this.uploader.Length;
         Int64 length = this.uploader.Upload(stream);
         entry.Blob = blob;
         entry.Offset = offset;
         entry.Length = length;
      }
      public void Checkpoint ()
      {
         if (this.uploader != null)
         {
            Backup.Blob blob = this.index.LookupBlob(this.uploader.UploadID);
            this.uploader.Flush();
            blob.Length = this.uploader.Length;
            String archiveID = this.uploader.Complete();
            blob.Name = archiveID;
            this.index.UpdateBlob(blob);
            this.uploader.Dispose();
            this.uploader = null;
         }
         using (Stream checkpointStream = IO.FileSystem.Temp())
         {
            using (GZipStream gzipStream = new GZipStream(checkpointStream, CompressionMode.Compress, true))
            using (Stream indexStream = this.index.Serialize())
               indexStream.CopyTo(gzipStream);
            checkpointStream.Position = 0;
            this.s3.PutObject(
               new Amazon.S3.Model.PutObjectRequest()
               {
                  BucketName = this.indexS3Bucket,
                  Key = this.indexS3Key,
                  InputStream = checkpointStream
               }
            );
         }
      }
      #endregion
   }
}
