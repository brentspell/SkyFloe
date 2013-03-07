//===========================================================================
// MODULE:  StreamStack.cs
// PURPOSE: composite stream stack
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
using Strings = SkyFloe.Resources.Strings;

namespace SkyFloe.IO
{
   /// <summary>
   /// composite stream stack stream
   /// </summary>
   /// <remarks>
   /// This class encapsulates a stack of stream instances used to provide 
   /// sequential layered stream processing (compression, encryption, etc.). 
   /// It simplfies composite stream usage (avoiding long lists of usings) 
   /// and ensures consistent disposal semantics.
   /// The stack can be either read-only or write-only, dependening on the
   /// semantics of the stream on top of the stack.
   /// </remarks>
   public class StreamStack : Stream
   {
      private Stack<Stream> streams = new Stack<Stream>();

      #region Stack Operations
      /// <summary>
      /// Retrieves the last element added to the stream stack
      /// </summary>
      public Stream Top
      {
         get { return this.streams.Any() ? this.streams.Peek() : null; }
      }
      /// <summary>
      /// Retrieves a stream in the stack by type
      /// </summary>
      /// <typeparam name="T">
      /// The type of stream to retrieve
      /// </typeparam>
      /// <returns>
      /// The requested stream, if found
      /// Null otherwise
      /// </returns>
      public T GetStream<T> () where T : Stream
      {
         return this.streams.OfType<T>().FirstOrDefault();
      }
      /// <summary>
      /// Adds a stream to the stack
      /// </summary>
      /// <param name="stream">
      /// The stream to add
      /// </param>
      public void Push (Stream stream)
      {
         if (stream == null)
            throw new ArgumentNullException("stream");
         this.streams.Push(stream);
      }
      #endregion

      #region Stream Overrides
      /// <summary>
      /// Disposes the underlying streams
      /// </summary>
      /// <param name="disposing">
      /// True to release both managed and unmanaged resources
      /// False to release only unmanaged resources
      /// </param>
      protected override void Dispose (Boolean disposing)
      {
         base.Dispose(disposing);
         while (this.streams.Any())
            this.streams.Pop().Dispose();
      }
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
         get { return this.streams.Any() ? this.streams.Peek().CanRead : false; }
      }
      /// <summary>
      /// Indicates whether the stream is open for writing
      /// </summary>
      public override Boolean CanWrite
      {
         get { return this.streams.Any() ? this.streams.Peek().CanWrite : false; }
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
         if (!this.streams.Any())
            throw new InvalidOperationException(Strings.StreamStackEmpty);
         return this.streams.Peek().Read(buffer, offset, count);
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
         if (!this.streams.Any())
            throw new InvalidOperationException(Strings.StreamStackEmpty);
         this.streams.Peek().Write(buffer, offset, count);
      }
      /// <summary>
      /// Flushes any outstanding changes to the underlying stream
      /// </summary>
      public override void Flush ()
      {
         if (this.streams.Any())
            this.streams.Peek().Flush();
      }
      #endregion
   }
}
