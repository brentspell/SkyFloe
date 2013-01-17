using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SkyFloe.Aws
{
   public class GlacierUploader
   {
      private Amazon.Glacier.AmazonGlacierClient glacier;
      private String vault;
      private Int32 partSize;
      private Byte[] partBuffer;
      private Int32 partLength;
      private Stream partStream;
      private List<String> partChecksums;
      private String uploadID;
      private Int64 archiveOffset;

      public String UploadID { get { return this.uploadID; } }
      public Int64 Length { get { return this.archiveOffset + this.partLength; } }
      
      public GlacierUploader (
         Amazon.Glacier.AmazonGlacierClient glacier,
         String vault,
         Int32 partSize)
      {
         this.glacier = glacier;
         this.vault = vault;
         this.partSize = partSize;
         this.partBuffer = new Byte[this.partSize];
         this.partStream = new MemoryStream(this.partBuffer);
         this.partLength = 0;
         this.partChecksums = new List<String>();
         this.uploadID = this.glacier.InitiateMultipartUpload(
            new Amazon.Glacier.Model.InitiateMultipartUploadRequest()
            {
               VaultName = this.vault,
               PartSize = this.partSize
            }
         ).InitiateMultipartUploadResult.UploadId;
         this.archiveOffset = 0;
      }

      public Int64 Upload (Stream stream)
      {
         var streamLength = 0L;
         for (; ; )
         {
            if (this.partLength == this.partSize)
               Flush();
            var read = stream.Read(
               this.partBuffer,
               this.partLength,
               this.partSize - this.partLength);
            if (read == 0)
               break;
            this.partLength += read;
            streamLength += read;
         }
         return streamLength;
      }

      public void Flush ()
      {
         if (this.partLength > 0)
         {
            this.partStream.SetLength(this.partLength);
            this.partStream.Seek(0, SeekOrigin.Begin);
            var checksum = Amazon.Glacier.TreeHashGenerator.CalculateTreeHash(
               this.partStream
            );
            this.partStream.Seek(0, SeekOrigin.Begin);
            this.glacier.UploadMultipartPart(
               new Amazon.Glacier.Model.UploadMultipartPartRequest()
               {
                  VaultName = this.vault,
                  UploadId = this.uploadID,
                  Body = this.partStream,
                  Range = String.Format(
                     "bytes {0}-{1}/*",
                     this.archiveOffset,
                     this.archiveOffset + this.partLength - 1
                  ),
                  Checksum = checksum
               }
            );
            var partIdx = (Int32)(this.archiveOffset / this.partSize);
            while (partIdx >= this.partChecksums.Count)
               this.partChecksums.Add(null);
            this.partChecksums[partIdx] = checksum;
            this.archiveOffset += this.partLength;
            this.partLength = 0;
         }
      }

      public void Resync (Int64 commitLength)
      {
         if (commitLength > this.Length)
            throw new ArgumentException("commitLength");
         if (commitLength < this.Length)
         {
            // if there are outstanding buffered changes
            // for other archive entries, flush them to Glacier
            if (this.partLength > 0)
            {
               this.partLength = this.partSize;
               Flush();
            }
            // resync the archive offset with the known commit
            // length, and align on the next part boundary,
            // to avoid wasting archive parts for large files
            var commitOffset = commitLength % this.partSize;
            if (commitOffset > 0)
               this.archiveOffset = commitLength + this.partSize - commitOffset;
         }
      }

      public String Complete ()
      {
         if (this.partLength > 0)
            Flush();
         var partCount = (Int32)(this.Length / this.partSize);
         if (this.Length % this.partSize > 0)
            partCount++;
         var archiveID = this.glacier.CompleteMultipartUpload(
            new Amazon.Glacier.Model.CompleteMultipartUploadRequest()
            {
               VaultName = this.vault,
               UploadId = this.uploadID,
               ArchiveSize = this.Length.ToString(),
               Checksum = Amazon.Glacier.TreeHashGenerator.CalculateTreeHash(
                  this.partChecksums.Take(partCount)
               )
            }
         ).CompleteMultipartUploadResult.ArchiveId;
         this.glacier = null;
         this.vault = null;
         this.partBuffer = null;
         this.partLength = 0;
         this.partStream = null;
         this.partChecksums = null;
         this.uploadID = null;
         this.archiveOffset = 0;
         return archiveID;
      }
   }
}
