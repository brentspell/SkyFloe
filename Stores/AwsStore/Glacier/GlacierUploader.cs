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
      public const Int32 PartSize = 16 * 1024 * 1024;
      private AmazonGlacierClient glacier;
      private String vault;
      private Int32 partOffset;
      private Stream partStream;
      private Byte[] readBuffer;
      private List<String> partChecksums;
      private String uploadID;
      private Int64 archiveOffset;

      public String UploadID { get { return this.uploadID; } }
      public Int64 Length { get { return this.archiveOffset + this.partOffset; } }
      
      public GlacierUploader (AmazonGlacierClient glacier, String vault)
      {
         this.glacier = glacier;
         this.vault = vault;
         this.partOffset = 0;
         this.partStream = IO.FileSystem.Temp();
         this.partStream.SetLength(PartSize);
         this.readBuffer = new Byte[65536];
         this.partChecksums = new List<String>();
         this.uploadID = this.glacier.InitiateMultipartUpload(
            new InitiateMultipartUploadRequest()
            {
               VaultName = this.vault,
               PartSize = PartSize
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
         var streamLength = 0L;
         for (; ; )
         {
            if (this.partOffset == PartSize)
               Flush();
            var read = stream.Read(
               this.readBuffer,
               0,
               Math.Min(
                  this.readBuffer.Length,
                  PartSize - this.partOffset
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
         var partLength = this.partOffset;
         if (partLength > 0)
         {
            // TODO: comment about not setting any state variables before upload is successful, for retry
            this.partStream.SetLength(partLength);
            this.partStream.Position = 0;
            var checksum = TreeHashGenerator.CalculateTreeHash(this.partStream);
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
            var commitPartLength = (Int32)(commitLength % PartSize);
            var commitPartOffset = commitLength - commitPartLength;
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
                  this.archiveOffset += PartSize;
               // remove any checksums already calculated for 
               // uncommitted parts
               var partIdx = (Int32)(this.archiveOffset / PartSize);
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
         var archiveID = this.glacier.CompleteMultipartUpload(
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
