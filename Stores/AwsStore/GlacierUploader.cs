using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.Glacier;
using Amazon.Glacier.Model;

namespace SkyFloe.Aws
{
   public class GlacierUploader
   {
      static Int32 toggle = 0;
      private AmazonGlacierClient glacier;
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
         AmazonGlacierClient glacier,
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
            new InitiateMultipartUploadRequest()
            {
               VaultName = this.vault,
               PartSize = this.partSize
            }
         ).InitiateMultipartUploadResult.UploadId;
         this.archiveOffset = 0;
      }

      public Int64 Upload (Stream stream)
      {
         Int64 streamLength = 0;
         for (; ; )
         {
            if (this.partLength == this.partSize)
               Flush();
            Int32 read = stream.Read(
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
            String checksum = TreeHashGenerator.CalculateTreeHash(this.partStream);
            this.partStream.Seek(0, SeekOrigin.Begin);
            this.glacier.UploadMultipartPart(
               new UploadMultipartPartRequest()
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
            this.partChecksums.Add(checksum);
            this.archiveOffset += this.partLength;
            this.partLength = 0;
         }
      }

      public Int64 Resync (Int64 commitLength)
      {
         if (commitLength > this.Length)
            throw new ArgumentException("commitLength");
         if (commitLength < this.Length)
         {
            Int32 commitPartLength = (Int32)(commitLength % this.partSize);
            Int64 commitPartOffset = commitLength - commitPartLength;
            // if we haven't committed any parts since the failure,
            // simply reset the current part length and continue
            if (commitPartOffset == this.archiveOffset)
               this.partLength = commitPartLength;
            else
            {
               // otherwise, resync the archive offset with the 
               // known commit length and align on the next part 
               // boundary, if there were commits already uploaded
               this.partLength = 0;
               this.archiveOffset = commitPartOffset;
               if (commitPartLength > 0)
                  this.archiveOffset += this.partSize;
               // remove any checksums already calculated for 
               // uncommitted parts
               Int32 partIdx = (Int32)(this.archiveOffset / this.partSize);
               if (partIdx < this.partChecksums.Count)
                  this.partChecksums.RemoveRange(partIdx, this.partChecksums.Count - partIdx);
            }
         }
         return this.Length;
      }

      public String Complete ()
      {
         if (this.partLength > 0)
            Flush();
         String archiveID = this.glacier.CompleteMultipartUpload(
            new CompleteMultipartUploadRequest()
            {
               VaultName = this.vault,
               UploadId = this.uploadID,
               ArchiveSize = this.Length.ToString(),
               Checksum = TreeHashGenerator.CalculateTreeHash(this.partChecksums)
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
