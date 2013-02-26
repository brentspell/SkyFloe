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
      /// The stream compression mode (compress/decompress)
      /// </param>
      public CompressionStream (Stream baseStream, CompressionMode compressMode)
      {
         this.baseStream = baseStream;
         this.readBuffer = new Byte[DefaultBlockSize];
         switch (compressMode)
         {
            case CompressionMode.Compress:
               this.encoder = new LZ4.Compressor();
               this.streamBuffer = new Byte[
                  this.encoder.GetMaxCompressedLength(this.readBuffer.Length)
               ];
               break;
            case CompressionMode.Decompress:
               this.decoder = new LZ4.Decompressor();
               this.streamBuffer = new Byte[DefaultBlockSize];
               break;
            default:
               throw new ArgumentException("mode");
         }
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
         base.Dispose(disposing);
         if (this.baseStream != null)
            this.baseStream.Dispose();
         this.baseStream = null;
      }
      #endregion

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
      /// Reads from the underlying stream and 
      /// compresses or decompresses the results
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
         var total = 0;
         do
         {
            // process the current block
            // . if there is data available in the stream buffer,
            //   attempt to satisfy the read from there
            // . otherwise, read and compress/decompress a block
            if (this.streamLength > 0)
               total += ReadFromBuffer(buffer, offset + total, count - total);
            else if (this.encoder != null && !ReadAndCompress())
               break;
            else if (this.decoder != null && !ReadAndDecompress())
               break;
         } while (total < count);
         return total;
      }
      /// <summary>
      /// Stream write override
      /// </summary>
      /// <param name="buffer">
      /// Write buffer
      /// </param>
      /// <param name="offset">
      /// Write buffer offset, in bytes
      /// </param>
      /// <param name="count">
      /// Number of bytes to write
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
         this.baseStream.Flush();
      }
      #endregion

      #region Stream Reading Utilities
      /// <summary>
      /// Satisfies a stream read from the internal stream buffer
      /// </summary>
      /// <param name="buffer">
      /// The output buffer
      /// </param>
      /// <param name="offset">
      /// Output buffer offset
      /// </param>
      /// <param name="count">
      /// Maximum number of bytes to copy to the output buffer
      /// </param>
      /// <returns>
      /// The number of bytes transferred
      /// </returns>
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
      /// <summary>
      /// Reads a block from the underlying stream and compresses it
      /// </summary>
      /// <returns>
      /// True if data was read and compressed
      /// False if the end of the underlying stream was reached
      /// </returns>
      private Boolean ReadAndCompress ()
      {
         this.streamOffset = this.streamLength = 0;
         // attempt to fill a complete block of uncompressed data
         var blockRead = ReadBlock(this.readBuffer, this.readBuffer.Length);
         if (blockRead > 0)
         {
            // compress the block, leaving room for the length header
            var encoded = Encode(
               this.readBuffer,
               0,
               blockRead,
               this.streamBuffer,
               sizeof(Int32)
            );
            // copy the length header into the stream buffer
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
      /// <summary>
      /// Reads a block from the underlying stream and decompresses it
      /// </summary>
      /// <returns>
      /// True if data was read and decompressed
      /// False if the end of the underlying stream was reached
      /// </returns>
      private Boolean ReadAndDecompress ()
      {
         this.streamOffset = this.streamLength = 0;
         // read the encoded block length
         var blockLength = 0;
         if (ReadBlockLength(out blockLength))
         {
            // read the whole compressed block
            if (blockLength > this.readBuffer.Length)
               Array.Resize(ref this.readBuffer, blockLength);
            var blockRead = ReadBlock(this.readBuffer, blockLength);
            if (blockRead != blockLength)
               throw new InvalidOperationException("TODO: invalid block");
            // decompress the block into the stream buffer
            // . the decoder will return -1 to indicate an overrun,
            //   so resize the stream buffer when this happens
            // . repeat until the decoder returns success
            var decoded = 0;
            for (; ; )
            {
               decoded = Decode(
                  this.readBuffer,
                  0,
                  blockLength,
                  this.streamBuffer,
                  0
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

      #region Compression/Decompression
      /// <summary>
      /// Compresses a block of data
      /// </summary>
      /// <param name="srcBuffer">
      /// Source data buffer
      /// </param>
      /// <param name="srcOffset">
      /// Source buffer offset
      /// </param>
      /// <param name="srcLength">
      /// Source buffer length
      /// </param>
      /// <param name="dstBuffer">
      /// Destination data buffer
      /// </param>
      /// <param name="dstOffset">
      /// Destination buffer offset
      /// </param>
      /// <returns>
      /// The compressed size of the block
      /// </returns>
      private Int32 Encode (
         Byte[] srcBuffer,
         Int32 srcOffset,
         Int32 srcLength,
         Byte[] dstBuffer,
         Int32 dstOffset)
      {
         return this.encoder.Compress(
            srcBuffer,
            srcOffset,
            srcLength,
            dstBuffer,
            dstOffset
         );
      }
      /// <summary>
      /// Decompresses a block of data
      /// </summary>
      /// <param name="srcBuffer">
      /// Source data buffer
      /// </param>
      /// <param name="srcOffset">
      /// Source buffer offset
      /// </param>
      /// <param name="srcLength">
      /// Source buffer length
      /// </param>
      /// <param name="dstBuffer">
      /// Destination data buffer
      /// </param>
      /// <param name="dstOffset">
      /// Destination buffer offset
      /// </param>
      /// <returns>
      /// The decompressed size of the block
      /// </returns>
      private Int32 Decode (
         Byte[] srcBuffer,
         Int32 srcOffset,
         Int32 srcLength,
         Byte[] dstBuffer,
         Int32 dstOffset)
      {
         return this.decoder.Decompress(
            srcBuffer,
            srcOffset,
            dstBuffer,
            dstOffset,
            srcLength
         );
      }
      #endregion

      #region Encoding Utilities
      /// <summary>
      /// Attempts to read a 32-bit big endian block length value
      /// from the underlying stream
      /// </summary>
      /// <param name="value">
      /// Return the block length via here
      /// </param>
      /// <returns>
      /// True if the value was read
      /// False if the end of the underlying stream has been read
      /// </returns>
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
      /// <summary>
      /// Reads a data block from the underlying stream
      /// </summary>
      /// <remarks>
      /// Some streams (network, etc.) return any data that is available 
      /// without blocking if there is data to be read. The compression
      /// stream needs full blocks, so this method repeatedly reads from
      /// the underlying stream until the buffer is filled or the end
      /// of the stream is reached.
      /// </remarks>
      /// <param name="buffer">
      /// The read buffer
      /// </param>
      /// <param name="maxRead">
      /// The maximum number of bytes to read
      /// </param>
      /// <returns>
      /// The number of bytes read from the stream
      /// </returns>
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
      /// <summary>
      /// Converts a block length value to storage format (big endian)
      /// </summary>
      /// <param name="value">
      /// The block length
      /// </param>
      /// <returns>
      /// The encoded block length
      /// </returns>
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
      /// <summary>
      /// Converts a block length from storage format to runtime format
      /// </summary>
      /// <param name="bytes">
      /// The encoded block length
      /// </param>
      /// <returns>
      /// The block length value
      /// </returns>
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
