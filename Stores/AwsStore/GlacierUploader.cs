using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.Glacier;
using Amazon.Glacier.Model;

namespace SkyFloe.Aws
{
   public class GlacierUploader : IDisposable
   {
      private AmazonGlacierClient glacier;
      private String vault;
      private Int32 partSize;
      private Int32 partOffset;
      private Stream partStream;
      private Byte[] readBuffer;
      private List<String> partChecksums;
      private String uploadID;
      private Int64 archiveOffset;

      public String UploadID { get { return this.uploadID; } }
      public Int64 Length { get { return this.archiveOffset + this.partOffset; } }
      
      public GlacierUploader (
         AmazonGlacierClient glacier,
         String vault,
         Int32 partSize)
      {
         FileInfo partFile = new FileInfo(Path.GetTempFileName());
         partFile.Attributes |= FileAttributes.Temporary;
         this.glacier = glacier;
         this.vault = vault;
         this.partSize = partSize;
         this.partOffset = 0;
         this.partStream = new FileStream(
            partFile.FullName,
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.None,
            65536,
            FileOptions.DeleteOnClose
         );
         this.partStream.Position = 0;
         this.partStream.SetLength(this.partSize);
         this.readBuffer = new Byte[65536];
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

      public void Dispose ()
      {
         if (this.partStream != null)
            this.partStream.Dispose();
         this.partStream = null;
      }

      public Int64 Upload (Stream stream)
      {
         Int64 streamLength = 0;
         for (; ; )
         {
            if (this.partOffset == this.partSize)
               Flush();
            Int32 read = stream.Read(
               this.readBuffer,
               0,
               Math.Min(
                  this.readBuffer.Length, 
                  this.partSize - this.partOffset
               )
            );
            if (read == 0)
               break;
            this.partStream.Write(this.readBuffer, 0, read);
            this.partOffset += read;
            streamLength += read;
         }
         return streamLength;
      }

      public void Flush ()
      {
         Int32 partLength = this.partOffset;
         if (partLength > 0)
         {
            this.partStream.SetLength(partLength);
            this.partStream.Position = 0;
            String checksum = TreeHashGenerator.CalculateTreeHash(this.partStream);
            this.partStream.Position = 0;
            this.glacier.UploadMultipartPart(
               new UploadMultipartPartRequest()
               {
                  VaultName = this.vault,
                  UploadId = this.uploadID,
                  Body = this.partStream,
                  Range = String.Format(
                     "bytes {0}-{1}/*",
                     this.archiveOffset,
                     this.archiveOffset + partLength - 1
                  ),
                  Checksum = checksum
               }
            );
            this.partChecksums.Add(checksum);
            this.archiveOffset += partLength;
            this.partStream.Position = this.partOffset = 0;
         }
      }

      public Int64 Resync (Int64 commitLength)
      {
         if (commitLength > this.Length)
            throw new ArgumentException("commitLength");
         this.partStream.Position = this.partOffset;
         if (commitLength < this.Length)
         {
            Int32 commitPartLength = (Int32)(commitLength % this.partSize);
            Int64 commitPartOffset = commitLength - commitPartLength;
            // if we haven't committed any parts since the failure,
            // simply reset the current part length and continue
            if (commitPartOffset == this.archiveOffset)
               this.partStream.Position = this.partOffset = commitPartLength;
            else
            {
               // otherwise, resync the archive offset with the 
               // known commit length and align on the next part 
               // boundary, if there were commits already uploaded
               this.partStream.Position = this.partOffset = 0;
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
         if (this.partOffset > 0)
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
         this.partStream = null;
         this.readBuffer = null;
         this.partChecksums = null;
         this.uploadID = null;
         this.archiveOffset = 0;
         return archiveID;
      }
   }
}
