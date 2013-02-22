//===========================================================================
// MODULE:  FilterStream.cs
// PURPOSE: stream filter base class
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
   /// A stream filter base class
   /// </summary>
   /// <remarks>
   /// This class implements the tedious required Stream overrides and
   /// provides the ability for derivatives to implement pass-through
   /// processing of all data read from/written to an underlying stream.
   /// Simply override the Filter method for custom processing.
   /// </remarks>
   public class FilterStream : Stream
   {
      Stream baseStream;

      /// <summary>
      /// Initializes a new stream instance
      /// </summary>
      /// <param name="baseStream">
      /// The underlying stream to attach
      /// </param>
      public FilterStream (Stream baseStream)
      {
         this.baseStream = baseStream;
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
         if (this.baseStream != null)
            this.baseStream.Dispose();
         this.baseStream = null;
         base.Dispose(disposing);
      }

      #region Stream Overrides
      /// <summary>
      /// Indicates whether the stream supports random access
      /// </summary>
      public override Boolean CanSeek
      {
         get { return false; }
      }
      /// <summary>
      /// Indicates whether the stream is open for reading
      /// </summary>
      public override Boolean CanRead
      {
         get { return this.baseStream.CanRead; }
      }
      /// <summary>
      /// Indicates whether the stream is open for writing
      /// </summary>
      public override Boolean CanWrite
      {
         get { return this.baseStream.CanWrite; }
      }
      /// <summary>
      /// Gets/sets the current stream absolute position
      /// </summary>
      public override Int64 Position
      {
         get { throw new NotSupportedException(); }
         set { throw new NotSupportedException(); }
      }
      /// <summary>
      /// Gets the length of the stream
      /// </summary>
      public override Int64 Length
      {
         get { throw new NotSupportedException(); }
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
         throw new NotSupportedException();
      }
      /// <summary>
      /// Reads from the underlying stream and filters the results
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
         if (!this.CanRead)
            throw new InvalidOperationException();
         var read = this.baseStream.Read(buffer, offset, count);
         Filter(buffer, offset, read);
         return read;
      }
      /// <summary>
      /// Filters a write buffer and writes it to the underlying stream
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
         if (!this.CanWrite)
            throw new InvalidOperationException();
         Filter(buffer, offset, count);
         this.baseStream.Write(buffer, offset, count);
      }
      /// <summary>
      /// Flushes any outstanding changes to the underlying stream
      /// </summary>
      public override void Flush ()
      {
         this.baseStream.Flush();
      }
      #endregion

      #region FilterStream Overrides
      /// <summary>
      /// Stream filtering override
      /// </summary>
      /// <param name="buffer">
      /// Read/write buffer
      /// </param>
      /// <param name="offset">
      /// Read/write buffer offset
      /// </param>
      /// <param name="count">
      /// Number of bytes read/written
      /// </param>
      protected virtual void Filter (Byte[] buffer, Int32 offset, Int32 count)
      {
      }
      #endregion
   }
}
