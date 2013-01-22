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
      public const Int32 MinRetrievalSize = 1024 * 1024;
      private Amazon.S3.AmazonS3 s3;
      private Amazon.Glacier.AmazonGlacierClient glacier;
      private String vault;
      private String bucket;
      private String name;
      private String backupIndexPath;
      private Sqlite.BackupIndex backupIndex;
      private Sqlite.RestoreIndex restoreIndex;
      private GlacierUploader uploader;
      private GlacierDownloader downloader;

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
         this.backupIndexPath = Path.GetTempFileName();
      }

      public void Dispose ()
      {
         if (this.backupIndex != null)
            this.backupIndex.Dispose();
         if (this.restoreIndex != null)
            this.restoreIndex.Dispose();
         if (this.backupIndexPath != null)
            Sqlite.BackupIndex.Delete(this.backupIndexPath);
         this.backupIndex = null;
         this.restoreIndex = null;
         this.backupIndexPath = null;
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
         this.backupIndex = Sqlite.BackupIndex.Create(this.backupIndexPath, header);
      }
      public void Open ()
      {
         using (FileStream indexStream = new FileStream(this.backupIndexPath, FileMode.Open, FileAccess.Write, FileShare.None))
         using (Stream s3Stream = 
               this.s3.GetObject(
                  new Amazon.S3.Model.GetObjectRequest()
                  {
                     BucketName = this.bucket,
                     Key = this.IndexS3Key
                  }
               ).ResponseStream
            )
         using (GZipStream gzip = new GZipStream(s3Stream, CompressionMode.Decompress))
            gzip.CopyTo(indexStream);
         this.backupIndex = Sqlite.BackupIndex.Open(this.backupIndexPath);
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
         get
         {
            if (this.restoreIndex == null)
            {
               String path = System.IO.Path.Combine(
                  Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                  "SkyFloe",
                  "AwsGlacier",
                  "restore.db"
               );
               Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
               this.restoreIndex = (File.Exists(path)) ?
                  Sqlite.RestoreIndex.Open(path) :
                  Sqlite.RestoreIndex.Create(path, new Restore.Header());
            }
            return this.restoreIndex;
         }
      }
      public void PrepareBackup ()
      {
      }
      public void BackupEntry (Backup.Entry entry, Stream stream)
      {
         if (this.uploader == null)
         {
            this.uploader = new GlacierUploader(
               this.glacier, 
               this.vault,
               PartSize
            );
            this.backupIndex.InsertBlob(
               new Backup.Blob()
               {
                  Name = this.uploader.UploadID
               }
            );
         }
         Backup.Blob blob = this.backupIndex.LookupBlob(this.uploader.UploadID);
         if (blob.Length != this.uploader.Length)
            this.uploader.Resync(blob.Length);
         Int64 offset = blob.Length = this.uploader.Length;
         Int64 length = this.uploader.Upload(stream);
         entry.Blob = blob;
         entry.Offset = offset;
         entry.Length = length;
      }
      public void Checkpoint ()
      {
         if (this.uploader != null)
         {
            Backup.Blob blob = this.backupIndex.LookupBlob(this.uploader.UploadID);
            this.uploader.Flush();
            blob.Length = this.uploader.Length;
            String archiveID = this.uploader.Complete();
            blob.Name = archiveID;
            this.backupIndex.UpdateBlob(blob);
            this.uploader = null;
         }
         using (FileStream s3File = new FileStream(Path.GetTempFileName(), FileMode.Open, FileAccess.ReadWrite, FileShare.None, 65536, FileOptions.DeleteOnClose))
         {
            using (GZipStream gzip = new GZipStream(s3File, CompressionMode.Compress, true))
            using (Stream idx = this.backupIndex.Serialize())
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
      public void PrepareRestore (Restore.Session session)
      {
         if (session.State == Restore.SessionState.Pending)
         {
            List<Restore.Retrieval> orphanRetrievals = new List<Restore.Retrieval>();
            foreach (Restore.Retrieval oldRetrieval in this.RestoreIndex.ListRetrievals(session))
            {
               Restore.Retrieval newRetrieval = oldRetrieval;
               newRetrieval.Length = 0;
               foreach (Restore.Entry entry in this.restoreIndex.ListRetrievalEntries(newRetrieval))
               {
                  if (entry.Offset < newRetrieval.Offset + newRetrieval.Length + MinRetrievalSize)
                  {
                     newRetrieval.Length = entry.Offset - newRetrieval.Offset + entry.Length;
                     newRetrieval.Length += MinRetrievalSize - newRetrieval.Length % MinRetrievalSize;
                     this.RestoreIndex.UpdateRetrieval(newRetrieval);
                  }
                  else
                  {
                     newRetrieval = entry.Retrieval = this.RestoreIndex.InsertRetrieval(
                        new Restore.Retrieval()
                        {
                           Session = session,
                           BlobID = newRetrieval.BlobID,
                           Name = newRetrieval.Name,
                           Offset = entry.Offset - entry.Offset % MinRetrievalSize,
                           Length = entry.Length + (MinRetrievalSize - entry.Length % MinRetrievalSize)
                        }
                     );
                     entry.Offset -= entry.Retrieval.Offset;
                     this.RestoreIndex.UpdateEntry(entry);
                  }
               }
               if (!this.restoreIndex.ListRetrievalEntries(oldRetrieval).Any())
                  orphanRetrievals.Add(oldRetrieval);
            }
            foreach (Restore.Retrieval orphan in orphanRetrievals)
               this.RestoreIndex.DeleteRetrieval(orphan);
            foreach (Restore.Retrieval retrieval in this.RestoreIndex.ListRetrievals(session))
            {
               Backup.Blob blob = this.BackupIndex.FetchBlob(retrieval.BlobID);
               if (retrieval.Offset + retrieval.Length > blob.Length)
               {
                  retrieval.Length = blob.Length - retrieval.Offset;
                  this.RestoreIndex.UpdateRetrieval(retrieval);
               }
            }
         }
         Double maxRetrievalRate =
            0.05d * this.BackupIndex.ListSessions().Sum(s => s.ActualLength) /
            TimeSpan.FromDays(1).TotalSeconds;
         Restore.Entry firstEntry = this.RestoreIndex.LookupNextEntry(session);
         if (firstEntry != null)
         {
            this.downloader = new GlacierDownloader(
               this.glacier,
               this.vault,
               maxRetrievalRate,
               this.RestoreIndex
                  .ListRetrievals(session)
                  .SkipWhile(r => r.ID != firstEntry.Retrieval.ID)
                  .ToList()
            );
         }
      }
      public Stream RestoreEntry (Restore.Entry entry)
      {
         return this.downloader.Download(
            entry.Retrieval, 
            entry.Offset, 
            entry.Length
         );
      }
      #endregion
   }
}
