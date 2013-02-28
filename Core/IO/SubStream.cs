//===========================================================================
// MODULE:  SubStream.cs
// PURPOSE: stream subset wrapper
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

namespace SkyFloe.IO
{
   /// <summary>
   /// Stream byte subset stream
   /// </summary>
   /// <remarks>
   /// This stream encapsulates a read-only random access base stream, 
   /// exposing a contiguous sub-region of the stream's bytes as a new 
   /// stream.
   /// </remarks>
   public class Substream : Stream
   {
      private Stream baseStream;
      private Int64 offset;
      private Int64 length;

      /// <summary>
      /// Initializes a new stream instance
      /// </summary>
      /// <param name="baseStream">
      /// The underlying stream to attach
      /// </param>
      /// <param name="offset"></param>
      /// <param name="length"></param>
      public Substream (Stream baseStream, Int64 offset, Int64 length)
      {
         if (baseStream == null)
            throw new ArgumentNullException("baseStream");
         if (!baseStream.CanRead || !baseStream.CanSeek)
            throw new ArgumentException("stream");
         if (offset < 0)
            throw new ArgumentOutOfRangeException("offset");
         if (length < 0)
            throw new ArgumentOutOfRangeException("length");
         var streamLen = baseStream.Length;
         if (offset > streamLen)
            throw new ArgumentOutOfRangeException("offset");
         if (offset + length > streamLen)
            throw new ArgumentOutOfRangeException("length");
         this.baseStream = baseStream;
         this.offset = offset;
         this.length = length;
         this.baseStream.Position = this.offset;
      }
      /// <summary>
      /// Disposes the underlying stream
      /// </summary>
      /// <param name="disposing">
      /// True to release both managed and unmanaged resources
      /// False to release only unmanaged resources
      /// </param>
      protected override void Dispose (Boolean disposing)
      {
         // do not dispose the base stream - substreams only attach shared
         base.Dispose(disposing);
         this.baseStream = null;
      }
      /// <summary>
      /// Indicates whether the stream supports random access
      /// </summary>
      public override Boolean CanSeek
      {
         get { return true; }
      }
      /// <summary>
      /// Indicates whether the stream is open for reading
      /// </summary>
      public override Boolean CanRead
      {
         get { return true; }
      }
      /// <summary>
      /// Indicates whether the stream is open for writing
      /// </summary>
      public override Boolean CanWrite
      {
         get { return false; }
      }
      /// <summary>
      /// Gets/sets the current stream absolute position
      /// </summary>
      public override Int64 Position
      {
         get { return this.baseStream.Position - this.offset; }
         set { Seek(value, SeekOrigin.Begin); }
      }
      /// <summary>
      /// Gets the length of the stream
      /// </summary>
      public override Int64 Length
      {
         get { return this.length; }
      }
      /// <summary>
      /// Sets the length of the stream
      /// </summary>
      /// <param name="value">
      /// The new stream length
      /// </param>
      public override void SetLength (Int64 value)
      {
         throw new NotSupportedException();
      }
      /// <summary>
      /// Seeks the stream to a new position
      /// </summary>
      /// <param name="offset">
      /// The seek offset, in bytes
      /// </param>
      /// <param name="origin">
      /// The seek origin position
      /// </param>
      /// <returns>
      /// The updated absolute position of the stream
      /// </returns>
      public override Int64 Seek (Int64 offset, SeekOrigin origin)
      {
         switch (origin)
         {
            case SeekOrigin.Begin:
               return this.baseStream.Seek(this.offset + offset, origin) - this.offset;
            case SeekOrigin.End:
               return this.baseStream.Seek(this.length + this.offset + offset, SeekOrigin.Begin) - this.offset;
            case SeekOrigin.Current:
               return this.baseStream.Seek(offset, origin) - this.offset;
            default:
               throw new ArgumentException("origin");
         }
      }
      /// <summary>
      /// Reads from the underlying stream sub-region
      /// </summary>
      /// <param name="buffer">
      /// Read buffer
      /// </param>
      /// <param name="offset">
      /// Read buffer offset
      /// </param>
      /// <param name="count">
      /// Maximum number of bytes to read
      /// </param>
      /// <returns>
      /// Actual number of bytes read
      /// </returns>
      public override Int32 Read (Byte[] buffer, Int32 offset, Int32 count)
      {
         var read = (Int32)Math.Min(
            count,
            this.length - (this.baseStream.Position - this.offset)
         );
         return read > 0 ? this.baseStream.Read(buffer, offset, read) : 0;
      }
      /// <summary>
      /// Writes to the underlying stream
      /// </summary>
      /// <param name="buffer">
      /// Write buffer
      /// </param>
      /// <param name="offset">
      /// Write buffer offset
      /// </param>
      /// <param name="count">
      /// Number of bytes to write
      /// </param>
      public override void Write (Byte[] buffer, Int32 offset, Int32 count)
      {
         throw new NotSupportedException();
      }
      /// <summary>
      /// Flushes any outstanding changes to the underlying stream
      /// </summary>
      public override void Flush ()
      {
      }
   }
}
