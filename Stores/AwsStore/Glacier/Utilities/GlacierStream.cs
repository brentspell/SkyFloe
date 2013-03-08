//===========================================================================
// MODULE:  GlacierStream.cs
// PURPOSE: AWS glacier retrieval stream
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
using System.IO;
using System.Linq;
// Project References

namespace SkyFloe.Aws
{
   /// <summary>
   /// Glacier retrieval job output stream
   /// </summary>
   /// <remarks>
   /// This class encapsulates the results of a single glacier retrieval job
   /// and converts those results into a reliable, read-only, random access 
   /// stream.
   /// The stream provides for sequential access (or downstream seeks of less
   /// than 1MB) to the job results by simply downloading the job output in 
   /// order with a single AWS GetJobOutput request.
   /// When a random access seek occurs, the stream makes a new GetJobOutput 
   /// request, specifying the offset requested.
   /// Doing so minimizes both the number of AWS requests, and more 
   /// importantly, the amount of data downloaded from AWS, which can get
   /// expensive when restoring large vault archives.
   /// If any errors occur on the stream, the job output is discarded
   /// and retrieved in the subsequent request. This means that the stream
   /// can be reused after a transient fault occurs.
   /// </remarks>
   public class GlacierStream : Stream
   {
      private const Int32 MaxDownstreamSeek = 1024 * 1024;
      private Amazon.Glacier.AmazonGlacierClient glacier;
      private String vault;
      private String jobID;
      private Stream stream;
      private Int64 offset;
      private Int64 length;
      private Byte[] seekBuffer;

      /// <summary>
      /// Initializes a new stream instance
      /// </summary>
      /// <param name="glacier">
      /// The Glacier client interface
      /// </param>
      /// <param name="vault">
      /// The current Glacier vault name
      /// </param>
      /// <param name="jobID">
      /// The retrieval job identifier
      /// </param>
      /// <param name="length">
      /// The size of the retrieval job, in bytes
      /// </param>
      public GlacierStream (
         Amazon.Glacier.AmazonGlacierClient glacier,
         String vault,
         String jobID,
         Int64 length)
      {
         this.glacier = glacier;
         this.vault = vault;
         this.jobID = jobID;
         this.stream = null;
         this.offset = 0;
         this.length = length;
         this.seekBuffer = new Byte[65536];
      }
      /// <summary>
      /// Releases the resources associated with the stream
      /// </summary>
      /// <param name="disposing">
      /// True to release managed and unmanaged resources
      /// False to release only unmanaged resources
      /// </param>
      protected override void Dispose (Boolean disposing)
      {
         base.Dispose(disposing);
         CloseJob();
      }

      /// <summary>
      /// The Glacier retrieval job ID
      /// </summary>
      public String JobID { get { return this.jobID; } }

      /// <summary>
      /// Issues a Glacier GetJobOutput request for the current
      /// byte range and attaches the result as a network stream
      /// </summary>
      private void OpenJob ()
      {
         if (this.stream != null)
            CloseJob();
         this.stream = this.glacier.GetJobOutput(
            new Amazon.Glacier.Model.GetJobOutputRequest()
            {
               VaultName = this.vault,
               JobId = this.jobID,
               Range = String.Format("bytes={0}-{1}", this.offset, this.length - 1)
            }
         ).GetJobOutputResult.Body;
      }
      /// <summary>
      /// Releases the Glacier job output stream
      /// </summary>
      private void CloseJob ()
      {
         if (this.stream != null)
            this.stream.Dispose();
         this.stream = null;
      }

      #region Stream Overrides
      /// <summary>
      /// Stream seek indicator
      /// </summary>
      public override Boolean CanSeek
      {
         get { return true; }
      }
      /// <summary>
      /// Stream read indicator
      /// </summary>
      public override Boolean CanRead
      {
         get { return true; }
      }
      /// <summary>
      /// Stream write indicator
      /// </summary>
      public override Boolean CanWrite
      {
         get { return false; }
      }
      /// <summary>
      /// Gets the current stream position or
      /// seeks the stream to a new absolute position
      /// </summary>
      public override Int64 Position
      {
         get { return this.offset; }
         set { Seek(value, SeekOrigin.Begin); }
      }
      /// <summary>
      /// Retrieves the length of the job stream
      /// </summary>
      public override Int64 Length
      {
         get { return this.length; }
      }
      /// <summary>
      /// Sets the stream length
      /// </summary>
      /// <param name="value">
      /// The new stream length
      /// </param>
      public override void SetLength (Int64 value)
      {
         throw new NotSupportedException();
      }
      /// <summary>
      /// Seeks the job output stream
      /// </summary>
      /// <param name="offset">
      /// The stream offset, in bytes
      /// </param>
      /// <param name="origin">
      /// The origin of the seek operation
      /// </param>
      /// <returns>
      /// The updated absolute stream position, in bytes
      /// </returns>
      public override Int64 Seek (Int64 offset, SeekOrigin origin)
      {
         // calculate the updated job stream position
         var newOffset = 0L;
         switch (origin)
         {
            case SeekOrigin.Begin:
               newOffset = offset;
               break;
            case SeekOrigin.End:
               newOffset = this.length + offset;
               break;
            case SeekOrigin.Current:
               newOffset = this.offset + offset;
               break;
         }
         newOffset = Math.Min(Math.Max(newOffset, 0), this.length);
         // if we are seeking upstream or more than 1MB downstream
         // close the existing stream and issue a new request at
         // the desired offset
         if (newOffset < this.offset || newOffset - this.offset > MaxDownstreamSeek)
            CloseJob();
         if (this.stream == null)
         {
            this.offset = newOffset;
            OpenJob();
         }
         else
         {
            try
            {
               // since we are now seeking within the current request,
               // we must read and discard any bytes until we get
               // to the desired offset
               // the number of discarded bytes is at most 1MB, due to
               // the calculation above
               while (this.offset < newOffset)
               {
                  var read = this.stream.Read(
                     this.seekBuffer,
                     0,
                     Math.Min((Int32)(newOffset - this.offset), this.seekBuffer.Length)
                  );
                  if (read == 0)
                     break;
                  this.offset += read;
               }
            }
            catch
            {
               CloseJob();
               throw;
            }
         }
         return this.offset;
      }
      /// <summary>
      /// Reads the job output
      /// </summary>
      /// <param name="buffer">
      /// The read buffer
      /// </param>
      /// <param name="offset">
      /// The read buffer offset, in bytes
      /// </param>
      /// <param name="count">
      /// The maximum number of bytes to read
      /// </param>
      /// <returns>
      /// The actual number of bytes read
      /// </returns>
      public override Int32 Read (Byte[] buffer, Int32 offset, Int32 count)
      {
         if (this.stream == null)
            OpenJob();
         try
         {
            var read = this.stream.Read(buffer, offset, count);
            this.offset += read;
            return read;
         }
         catch
         {
            CloseJob();
            throw;
         }
      }
      /// <summary>
      /// Stream write override
      /// </summary>
      /// <param name="buffer">
      /// The write buffer
      /// </param>
      /// <param name="offset">
      /// The write buffer offset, in bytes
      /// </param>
      /// <param name="count">
      /// The number of bytes to write
      /// </param>
      public override void Write (Byte[] buffer, Int32 offset, Int32 count)
      {
         throw new NotSupportedException();
      }
      /// <summary>
      /// Stream flush override
      /// </summary>
      public override void Flush ()
      {
      }
      #endregion
   }
}
