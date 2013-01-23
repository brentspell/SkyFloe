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
      private Double maxRetrievalRate;

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
         String restoreIndexPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SkyFloe",
            "AwsGlacier",
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
         get { return this.restoreIndex; } 
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
            foreach (Restore.Retrieval oldRetrieval in this.restoreIndex.ListRetrievals(session))
            {
               Backup.Blob blob = this.backupIndex.LookupBlob(oldRetrieval.Blob);
               oldRetrieval.Length = 0;
               this.restoreIndex.UpdateRetrieval(oldRetrieval);
               Restore.Retrieval newRetrieval = oldRetrieval;
               foreach (Restore.Entry entry in this.restoreIndex.ListRetrievalEntries(newRetrieval))
               {
                  if (entry.Offset < newRetrieval.Offset + newRetrieval.Length + MinRetrievalSize)
                  {
                     newRetrieval.Length = entry.Offset - newRetrieval.Offset + entry.Length;
                     newRetrieval.Length += MinRetrievalSize - newRetrieval.Length % MinRetrievalSize;
                     this.restoreIndex.UpdateRetrieval(newRetrieval);
                  }
                  else
                  {
                     entry.Retrieval = newRetrieval = this.restoreIndex.InsertRetrieval(
                        new Restore.Retrieval()
                        {
                           Session = session,
                           Blob = newRetrieval.Blob,
                           Offset = entry.Offset - entry.Offset % MinRetrievalSize,
                           Length = entry.Length + (MinRetrievalSize - entry.Length % MinRetrievalSize)
                        }
                     );
                     entry.Offset -= entry.Retrieval.Offset;
                     this.restoreIndex.UpdateEntry(entry);
                  }
                  if (newRetrieval.Offset + newRetrieval.Length > blob.Length)
                  {
                     newRetrieval.Length = blob.Length - newRetrieval.Offset;
                     this.restoreIndex.UpdateRetrieval(newRetrieval);
                  }
               }
            }
            foreach (Restore.Retrieval orphan in this.restoreIndex.ListRetrievals(session))
               if (orphan.Length == 0)
                  this.restoreIndex.DeleteRetrieval(orphan);
         }
         this.maxRetrievalRate =
            0.05d * this.backupIndex.ListSessions().Sum(s => s.ActualLength) /
            TimeSpan.FromDays(30).TotalSeconds;
         this.downloader = new GlacierDownloader(this.glacier, this.vault);
         foreach (Restore.Retrieval retrieval in this.restoreIndex.ListRetrievals(session))
         {
            try
            {
               if (retrieval.Name != null)
                  this.downloader.QueryJob(retrieval.Name);
            }
            catch
            {
               retrieval.Name = null;
               this.restoreIndex.UpdateRetrieval(retrieval);
            }
         }
      }
      public Stream RestoreEntry (Restore.Entry entry)
      {
         Boolean ready = false;
         while (!ready)
         {
            try
            {
               if (entry.Retrieval.Name != null)
                  ready = this.downloader.QueryJob(entry.Retrieval.Name);
            }
            catch
            {
               entry.Retrieval.Name = null;
               this.restoreIndex.UpdateRetrieval(entry.Retrieval);
            }
            foreach (Restore.Retrieval retrieval in this.restoreIndex
               .ListRetrievals(entry.Retrieval.Session)
               .SkipWhile(r => r.ID != entry.Retrieval.ID)
            )
            {
               Double retrievalRate = 
                  (Double)entry.Retrieval.Session.Retrieved /
                  (DateTime.UtcNow - entry.Retrieval.Session.Created).TotalSeconds;
               if (retrievalRate > this.maxRetrievalRate)
                  if (retrieval.ID != entry.Retrieval.ID)
                     break;
               if (retrieval.Name == null)
               {
                  retrieval.Name = this.downloader.StartJob(
                     retrieval.Blob,
                     retrieval.Offset,
                     retrieval.Length
                  );
                  this.restoreIndex.UpdateRetrieval(retrieval);
                  entry.Retrieval.Session.Retrieved += retrieval.Length;
               }
            }
            foreach (Restore.Retrieval retrieval in this.restoreIndex
               .ListRetrievals(entry.Retrieval.Session)
               .TakeWhile(r => r.ID != entry.Retrieval.ID)
               .Where(r => r.Name != null)
            )
               this.downloader.DeleteJob(retrieval.Name);
            if (!ready)
               System.Threading.Thread.Sleep(
                  (Int32)TimeSpan.FromMinutes(20).TotalMilliseconds
               );
         }
         return this.downloader.GetJobStream(
            entry.Retrieval.Name, 
            entry.Offset, 
            entry.Length
         );
      }
      #endregion
   }
}
