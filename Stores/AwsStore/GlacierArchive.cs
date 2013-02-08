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
      private Amazon.S3.AmazonS3 s3;
      private Amazon.Glacier.AmazonGlacierClient glacier;
      private String vault;
      private String bucket;
      private String name;
      private IO.FileSystem.TempStream backupIndexFile;
      private IBackupIndex backupIndex;
      private IRestoreIndex restoreIndex;

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
         String restoreIndexPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SkyFloe",
            "AwsGlacier",
            name,
            "restore.db"
         );
         Directory.CreateDirectory(System.IO.Path.GetDirectoryName(restoreIndexPath));
         this.restoreIndex = (File.Exists(restoreIndexPath)) ?
            Sqlite.RestoreIndex.Open(restoreIndexPath) :
            Sqlite.RestoreIndex.Create(restoreIndexPath, new Restore.Header());
      }

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

      public void Create (Backup.Header header)
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
         this.backupIndexFile = IO.FileSystem.Temp();
         this.backupIndex = Sqlite.BackupIndex.Create(this.backupIndexFile.Path, header);
         // TODO: remove duplication between here and the backup object
         using (Stream checkpointStream = IO.FileSystem.Temp())
         {
            using (GZipStream gzipStream = new GZipStream(checkpointStream, CompressionMode.Compress, true))
            using (Stream indexStream = this.backupIndex.Serialize())
               indexStream.CopyTo(gzipStream);
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
      public void Open ()
      {
         this.backupIndexFile = IO.FileSystem.Temp();
         Stream s3Stream = this.s3.GetObject(
            new Amazon.S3.Model.GetObjectRequest()
            {
               BucketName = this.bucket,
               Key = this.IndexS3Key
            }
         ).ResponseStream;
         using (s3Stream)
         using (GZipStream gzip = new GZipStream(s3Stream, CompressionMode.Decompress))
            gzip.CopyTo(this.backupIndexFile);
         this.backupIndex = Sqlite.BackupIndex.Open(this.backupIndexFile.Path);
      }

      #region IArchive Implementation
      public String Name
      {
         get { return this.name; }
      }
      public IBackupIndex BackupIndex
      {
         get { return this.backupIndex; }
      }
      public Store.IRestoreIndex RestoreIndex
      {
         get { return this.restoreIndex; } 
      }
      public IBackup PrepareBackup (Backup.Session session)
      {
         return new GlacierBackup(
            this.backupIndex,
            this.s3,
            this.glacier,
            this.vault,
            this.bucket,
            this.IndexS3Key
         );
      }
      public IRestore PrepareRestore (Restore.Session session)
      {
         return new GlacierRestore(
            this.backupIndex,
            this.restoreIndex,
            session,
            this.glacier,
            this.vault
         );
      }
      #endregion
   }
}
