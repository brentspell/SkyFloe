//===========================================================================
// MODULE:  StreamCopier.cs
// PURPOSE: generic stream copier
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
using Stream = System.IO.Stream;
// Project References

namespace SkyFloe.IO
{
   /// <summary>
   /// Stream copier
   /// </summary>
   /// <remarks>
   /// This class provides the ability to perform repeated stream copy
   /// operations using a single buffer (as opposed to Stream.CopyTo, which
   /// allocates a buffer for each copy). The copier also runs the read/write
   /// operations async, which avoids waiting on the operations if they
   /// are synchronous or takes advantage of native async calls.
   /// </remarks>
   public class StreamCopier
   {
      public const Int32 DefaultBufferSize = 65536;
      private Byte[][] buffers;

      /// <summary>
      /// Initializes a new copier instance
      /// </summary>
      /// <param name="bufferSize">
      /// The size of the internal buffer to use for stream transfers
      /// </param>
      public StreamCopier (Int32 bufferSize = DefaultBufferSize)
      {
         if (bufferSize < 2)
            throw new ArgumentOutOfRangeException("bufferSize");
         // allocate the stream buffers
         // the buffer is split to support alternating async reads/writes
         this.buffers = new Byte[2][];
         this.buffers[0] = new Byte[bufferSize / 2];
         this.buffers[1] = new Byte[bufferSize / 2];
      }

      /// <summary>
      /// Transfers an entire source stream to a target
      /// </summary>
      /// <param name="source">
      /// The stream to read
      /// </param>
      /// <param name="target">
      /// The stream to write
      /// </param>
      /// <returns>
      /// The total number of bytes transferred
      /// </returns>
      public Int32 Copy (Stream source, Stream target)
      {
         var copied = 0;
         var bufferIdx = 0;
         // start an initial dummy write to avoid 
         // a null test within the copy loop
         var writer = target.BeginWrite(this.buffers[1], 0, 0, null, null);
         for (; ; )
         {
            // read into the current buffer
            var buffer = this.buffers[bufferIdx];
            var reader = source.BeginRead(buffer, 0, buffer.Length, null, null);
            // complete the previous write and the current read
            target.EndWrite(writer);
            var read = source.EndRead(reader);
            if (read == 0)
               break;
            copied += read;
            // start the next write for the completed read
            writer = target.BeginWrite(buffer, 0, read, null, null);
            // swap the buffer index for the next read
            bufferIdx = (bufferIdx + 1) % 2;
         }
         return copied;
      }
      /// <summary>
      /// Transfers a source stream to a target and flushes the target
      /// </summary>
      /// <param name="source">
      /// The stream to read
      /// </param>
      /// <param name="target">
      /// The stream to write
      /// </param>
      /// <returns>
      /// The total number of bytes transferred
      /// </returns>
      public Int32 CopyAndFlush (Stream source, Stream target)
      {
         var copied = Copy(source, target);
         target.Flush();
         return copied;
      }
   }
}
