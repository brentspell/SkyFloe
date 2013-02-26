//===========================================================================
// MODULE:  CompressionStream.cs
// PURPOSE: data read-only compression stream
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
   /// Compression stream mode
   /// </summary>
   public enum CompressionMode
   {
      Compress = 1,        // compress underlying stream
      Decompress = 2       // descompress underlying stream
   }

   /// <summary>
   /// Compression reader stream
   /// </summary>
   /// <remarks>
   /// This class compresses or decompresses an underlying stream, in a 
   /// forward-only, read-only fashion. It uses the fast LZ4 block
   /// compression algorithm. During compression, uncompressed blocks of
   /// DefaultBlockSize are buffered, compressed and returned with a 32-bit
   /// prefix indicating the block length. During decompression, each
   /// block header is read, followed by the associated compressed data,
   /// which is then decompressed and buffered for read.
   /// Quad-plex read/write and compress/decompress functionality is not
   /// provided due to the complexity of the decompress-write algorithm,
   /// which would have some failure cases for Flush() calls that would
   /// be inconsistent with the Stream model.
   /// </remarks>
   public class CompressionStream : Stream
   {
      private const Int32 DefaultBlockSize = 65536;
      private Stream baseStream;
      private LZ4.Compressor encoder;
      private LZ4.Decompressor decoder;
      private Byte[] streamBuffer;
      private Int32 streamOffset;
      private Int32 streamLength;
      private Byte[] readBuffer;

      #region Construction/Disposal
      /// <summary>
      /// Initializes a new stream instance
      /// </summary>
      /// <param name="baseStream">
      /// The underlying stream to read
      /// </param>
      /// <param name="compressMode">
      /// The stream compression mode
      /// </param>
      public CompressionStream (Stream baseStream, CompressionMode compressMode)
      {
         this.baseStream = baseStream;
         this.readBuffer = new Byte[DefaultBlockSize];
         switch (compressMode)
         {
            case CompressionMode.Compress:
               this.encoder = new LZ4.Compressor();
               this.streamBuffer = new Byte[this.encoder.GetMaxCompressedLength(this.readBuffer.Length)];
               break;
            case CompressionMode.Decompress:
               this.decoder = new LZ4.Decompressor();
               this.streamBuffer = new Byte[DefaultBlockSize];
               break;
            default:
               throw new ArgumentException("mode");
         }
      }

      protected override void Dispose (Boolean disposing)
      {
         base.Dispose(disposing);
         if (this.baseStream != null)
            this.baseStream.Dispose();
         this.baseStream = null;
      }
      #endregion

      #region Stream Overrides
      public override Boolean CanSeek
      {
         get { return false; }
      }
      public override Boolean CanRead
      {
         get { return true; }
      }
      public override Boolean CanWrite
      {
         get { return false; }
      }
      public override Int64 Position
      {
         get { throw new NotSupportedException(); }
         set { throw new NotSupportedException(); }
      }
      public override Int64 Length
      {
         get { throw new NotSupportedException(); }
      }
      public override void SetLength (Int64 value)
      {
         throw new NotSupportedException();
      }
      public override Int64 Seek (Int64 offset, SeekOrigin origin)
      {
         throw new NotSupportedException();
      }
      public override Int32 Read (Byte[] buffer, Int32 offset, Int32 count)
      {
         var total = 0;
         do
         {
            if (this.streamLength > 0)
               total += ReadFromBuffer(buffer, offset + total, count - total);
            else if (this.encoder != null && !ReadAndCompress())
               break;
            else if (this.decoder != null && !ReadAndDecompress())
               break;
         } while (total < count);
         return total;
      }
      public override void Write (Byte[] buffer, Int32 offset, Int32 count)
      {
         throw new NotSupportedException();
      }
      public override void Flush ()
      {
         this.baseStream.Flush();
      }
      #endregion

      #region Stream Reading Utilities
      private Int32 ReadFromBuffer (Byte[] buffer, Int32 offset, Int32 count)
      {
         var read = Math.Min(this.streamLength, count);
         Buffer.BlockCopy(
            this.streamBuffer,
            this.streamOffset,
            buffer,
            offset,
            read
         );
         this.streamOffset += read;
         this.streamLength -= read;
         return read;
      }
      private Boolean ReadAndCompress ()
      {
         this.streamOffset = this.streamLength = 0;
         var blockRead = ReadBlock(this.readBuffer, this.readBuffer.Length);
         if (blockRead > 0)
         {
            var encoded = this.encoder.Compress(
               this.readBuffer,
               0,
               blockRead,
               this.streamBuffer,
               sizeof(Int32)
            );
            var lengthData = EncodeBlockLength(encoded);
            Buffer.BlockCopy(
               lengthData,
               0,
               this.streamBuffer,
               0,
               lengthData.Length
            );
            this.streamLength += lengthData.Length + encoded;
            return true;
         }
         return false;
      }
      private Boolean ReadAndDecompress ()
      {
         this.streamOffset = this.streamLength = 0;
         var blockLength = 0;
         if (ReadBlockLength(out blockLength))
         {
            if (blockLength > this.readBuffer.Length)
               Array.Resize(ref this.readBuffer, blockLength);
            var blockRead = ReadBlock(this.readBuffer, blockLength);
            if (blockRead != blockLength)
               throw new InvalidOperationException("TODO: invalid block");
            var decoded = 0;
            for (; ; )
            {
               decoded = this.decoder.Decompress(
                  this.readBuffer,
                  this.streamBuffer,
                  blockLength
               );
               if (decoded >= 0)
                  break;
               Array.Resize(ref this.streamBuffer, this.streamBuffer.Length * 2);
            }
            this.streamLength = decoded;
            return true;
         }
         return false;
      }
      #endregion

      #region Encoding Utilities
      private Boolean ReadBlockLength (out Int32 value)
      {
         Byte[] bytes = new Byte[4];
         Int32 read = this.baseStream.Read(bytes, 0, bytes.Length);
         if (read != 0)
         {
            if (read != bytes.Length)
               throw new InvalidOperationException("TODO: data truncation");
            value = DecodeBlockLength(bytes);
            return true;
         }
         value = 0;
         return false;
      }
      private Int32 ReadBlock (Byte[] buffer, Int32 maxRead)
      {
         var blockRead = 0;
         do
         {
            var read = this.baseStream.Read(
               buffer,
               blockRead,
               maxRead - blockRead
            );
            if (read == 0)
               break;
            blockRead += read;
         } while (blockRead < maxRead);
         return blockRead;
      }
      private Byte[] EncodeBlockLength (Int32 value)
      {
         return new[]
         {
            (Byte)(value >> 24),
            (Byte)(value >> 16),
            (Byte)(value >> 8),
            (Byte)(value >> 0)
         };
      }
      private Int32 DecodeBlockLength (Byte[] bytes)
      {
         return
            ((Int32)bytes[0] << 24) |
            ((Int32)bytes[1] << 16) |
            ((Int32)bytes[2] << 8) |
            ((Int32)bytes[3] << 0);
      }
      #endregion
   }
}
