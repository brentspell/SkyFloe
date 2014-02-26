//===========================================================================
// MODULE:  GlacierUploader.cs
// PURPOSE: AWS glacier upload utility
// 
// Copyright © 2013
// Brent M. Spell. All rights reserved.
//
// This library is free software; you can redistribute it and/or modify it 
// under the terms of the GNU Lesser General Public License as published 
// by the Free Software Foundation; either version 3 of the License, or 
// (at your option) any later version. This library is distributed in the 
// hope that it will be useful, but WITHOUT ANY WARRANTY; without even the 
// implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
// See the GNU Lesser General Public License for more details. You should 
// have received a copy of the GNU Lesser General Public License along with 
// this library; if not, write to 
//    Free Software Foundation, Inc. 
//    51 Franklin Street, Fifth Floor 
//    Boston, MA 02110-1301 USA
//===========================================================================
// System References
using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.Glacier;
using Amazon.Glacier.Model;
using Stream = System.IO.Stream;
// Project References

namespace SkyFloe.Aws
{
   /// <summary>
   /// Glacier vault archive uploader
   /// </summary>
   /// <remarks>
   /// This class encapsulates a single Glacier multi-part upload operation
   /// for a vault archive. It reads each incoming backup entry stream, buffers
   /// the results into a temporary file of size PartSize, and then uploads
   /// each full part to Glacier using the multi-part AWS interface, 
   /// maintaining and submitting the appropriate checksums.
   /// A checkpoint forces the completion of the vault archive after 
   /// uploading any buffered data.
   /// The uploader supports fault-tolerant uploads through the Resync
   /// method, which determines the point at which the last successful part 
   /// was uploaded and resumes copying there.
   /// </remarks>
   public class GlacierUploader : IDisposable
   {
      public const Int32 PartAttemptCount = 5;
      public const Int32 PartSize = 16 * 1024 * 1024;
      private AmazonGlacierClient glacier;
      private String vault;
      private Int32 partOffset;
      private Stream partStream;
      private Byte[] readBuffer;
      private List<String> partChecksums;
      private Int64 archiveOffset;
      private String uploadID;

      /// <summary>
      /// The multi-part upload identifier, which identifies the
      /// upload operation to Glacier
      /// </summary>
      public String UploadID { get { return this.uploadID; } }
      /// <summary>
      /// The current uncommitted length of the vault archive
      /// </summary>
      public Int64 Length { get { return this.archiveOffset + this.partOffset; } }
      
      /// <summary>
      /// Initializes a new vault archive uploader
      /// </summary>
      /// <param name="glacier">
      /// The Glacier client interface
      /// </param>
      /// <param name="vault">
      /// The current Glacier vault name
      /// </param>
      public GlacierUploader (AmazonGlacierClient glacier, String vault)
      {
         this.glacier = glacier;
         this.vault = vault;
         this.partOffset = 0;
         this.partStream = IO.FileSystem.Temp();
         this.partStream.SetLength(PartSize);
         this.readBuffer = new Byte[65536];
         this.partChecksums = new List<String>();
         this.archiveOffset = 0;
         this.uploadID = this.glacier.InitiateMultipartUpload(
            new InitiateMultipartUploadRequest()
            {
               VaultName = this.vault,
               PartSize = PartSize
            }
         ).InitiateMultipartUploadResult.UploadId;
      }
      /// <summary>
      /// Releases the resources associated with the uploader
      /// </summary>
      public void Dispose ()
      {
         if (this.partStream != null)
            this.partStream.Dispose();
         this.partStream = null;
      }
      /// <summary>
      /// Copies a stream to a Glacier vault archive
      /// </summary>
      /// <param name="stream">
      /// The stream to read
      /// </param>
      /// <returns>
      /// The total number of bytes read
      /// </returns>
      public Int64 Upload (Stream stream)
      {
         var streamLength = 0L;
         for (; ; )
         {
            // if we have filled the current part, then upload it
            if (this.partOffset == PartSize)
               Flush();
            // read the next block from the input stream
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
            // write the results to the part buffer stream
            this.partStream.Write(this.readBuffer, 0, read);
            this.partOffset += read;
            streamLength += read;
         }
         return streamLength;
      }
      /// <summary>
      /// Uploads the current vault archive part to Glacier
      /// </summary>
      public void Flush ()
      {
         // calculate the current part length
         // this value must be fixed (PartSize) for all uploads
         // to Glacier except for the last one
         var partLength = this.partOffset;
         if (partLength > 0)
         {
            // reset the part stream, calculate its hash, and upload it
            // it is important here not to modify any internal state
            // variables before we submit the upload request, so that
            // we can successfully retry the request if a fault occurs
            this.partStream.SetLength(partLength);
            this.partStream.Position = 0;
            var checksum = TreeHashGenerator.CalculateTreeHash(this.partStream);
            for (var attempt = 1; ; attempt++)
            {
               this.partStream.Position = 0;
               try
               {
                  // attempt to upload the current part
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
                  break;
               }
               catch
               {
                  // if we have reached the maximum attempt count, throw
                  if (attempt == PartAttemptCount)
                     throw;
               }
            }
            // now that the upload was successful, we can modify
            // the internal vault archive offset and checksum list 
            // and reset the part buffer stream
            this.partChecksums.Add(checksum);
            this.archiveOffset += partLength;
            this.partStream.Position = this.partOffset = 0;
         }
      }
      /// <summary>
      /// Resynchronizes the uploader with the externally-known
      /// committed vault archive length, which will be less than 
      /// the current archive length in the case of a fault within
      /// an input file that straddles a part boundary
      /// </summary>
      /// <param name="commitLength">
      /// The current committed vault archive length
      /// </param>
      /// <returns>
      /// The updated vault archive length
      /// </returns>
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
               // otherwise, resync the vault archive offset with the 
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
      /// <summary>
      /// Commits the current vault archive
      /// </summary>
      /// <returns>
      /// The completed vault archive identifier
      /// </returns>
      public String Complete ()
      {
         // upload any oustatanding buffered part data
         if (this.partOffset > 0)
            Flush();
         // complete the vault archive and clean up
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
         this.partStream.Dispose();
         this.partStream = null;
         this.readBuffer = null;
         this.partChecksums = null;
         this.uploadID = null;
         this.archiveOffset = 0;
         return archiveID;
      }
   }
}
