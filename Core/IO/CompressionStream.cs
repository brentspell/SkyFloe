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
using System.Text;
// Project References
using Strings = SkyFloe.Resources.Strings;

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
         if (baseStream == null)
            throw new ArgumentNullException("baseStream");
         this.baseStream = baseStream;
         this.readBuffer = new Byte[DefaultBlockSize];
         switch (compressMode)
         {
            case CompressionMode.Compress:
               InitEncode();
               break;
            case CompressionMode.Decompress:
               InitDecode();
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
      /// <summary>
      /// Initializes the stream for compression
      /// </summary>
      private void InitEncode ()
      {
         this.encoder = new LZ4.Compressor();
         this.streamBuffer = new Byte[
            BlockHeader.Size +
            this.encoder.GetMaxCompressedLength(this.readBuffer.Length)
         ];
         this.streamLength = StreamHeader.Current.Encode(this.streamBuffer);
      }
      /// <summary>
      /// Initializes the stream for decompression
      /// </summary>
      private void InitDecode ()
      {
         this.decoder = new LZ4.Decompressor();
         this.streamBuffer = new Byte[DefaultBlockSize];
         var header = StreamHeader.Read(this.baseStream);
         if (!header.IsValid)
            throw new DataException();
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
         // read the uncompressed block into the read buffer
         var decoded = ReadBlock(this.readBuffer, this.readBuffer.Length);
         if (decoded > 0)
         {
            // compress the block into the stream buffer,
            // leaving room for the header
            var encoded = Encode(
               this.readBuffer,
               0,
               decoded,
               this.streamBuffer,
               BlockHeader.Size
            );
            // copy the length header into the stream buffer
            var header = new BlockHeader()
            {
               EncodedLength = encoded,
               DecodedLength = decoded
            };
            this.streamLength += header.Encode(this.streamBuffer);
            this.streamLength += encoded;
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
         var header = BlockHeader.Read(this.baseStream);
         if (!header.IsEmpty)
         {
            // read the compressed block into the read buffer
            if (header.EncodedLength > this.readBuffer.Length)
               Array.Resize(ref this.readBuffer, header.EncodedLength);
            var encoded = ReadBlock(this.readBuffer, header.EncodedLength);
            if (encoded != header.EncodedLength)
               throw new DataException();
            // decompress the block into the stream buffer
            if (header.DecodedLength > this.streamBuffer.Length)
               Array.Resize(ref this.streamBuffer, header.DecodedLength);
            var decoded = Decode(
               this.readBuffer,
               0,
               header.EncodedLength,
               this.streamBuffer,
               0
            );
            if (decoded != header.DecodedLength)
               throw new DataException();
            this.streamLength = header.DecodedLength;
            return true;
         }
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

      /// <summary>
      /// Compression stream header
      /// </summary>
      /// <remarks>
      /// This structure contains information for an entire
      /// compressed stream, including versioning.
      /// </remarks>
      public struct StreamHeader
      {
         public const Int32 Size = 4;
         public const Byte CurrentVersion = 1;
         public static readonly Byte[] CurrentSignature = Encoding.ASCII.GetBytes("LZ4");
         public static readonly StreamHeader Empty = new StreamHeader();
         public static readonly StreamHeader Current = new StreamHeader()
         {
            Signature = CurrentSignature,
            Version = CurrentVersion
         };

         /// <summary>
         /// The stream signature
         /// </summary>
         public Byte[] Signature { get; set; }
         /// <summary>
         /// The current stream version
         /// </summary>
         public Byte Version { get; set; }
         /// <summary>
         /// Indicates whether the header is empty
         /// </summary>
         public Boolean IsEmpty
         {
            get { return this.Signature == null; }
         }
         /// <summary>
         /// Validate the header
         /// </summary>
         public Boolean IsValid
         {
            get
            {
               if (this.Signature == null)
                  return false;
               if (!Enumerable.SequenceEqual(this.Signature, CurrentSignature))
                  return false;
               if (this.Version != CurrentVersion)
                  return false;
               return true;
            }
         }

         /// <summary>
         /// Serializes the stream header to a buffer
         /// </summary>
         /// <returns>
         /// The serialized stream header
         /// </returns>
         public Byte[] Encode ()
         {
            Byte[] buffer = new Byte[Size];
            Encode(buffer);
            return buffer;
         }
         /// <summary>
         /// Serializes the stream header to a buffer
         /// </summary>
         /// <param name="buffer">
         /// The buffer to write
         /// </param>
         /// <returns>
         /// The number of bytes written
         /// </returns>
         public Int32 Encode (Byte[] buffer)
         {
            if (buffer.Length < Size)
               throw new ArgumentException("buffer");
            buffer[0] = this.Signature[0];
            buffer[1] = this.Signature[1];
            buffer[2] = this.Signature[2];
            buffer[3] = this.Version;
            return Size;
         }
         /// <summary>
         /// Deserializes a stream header from a buffer
         /// </summary>
         /// <param name="buffer">
         /// The serialized stream header
         /// </param>
         /// <returns>
         /// The deserialized stream header
         /// </returns>
         public static StreamHeader Decode (Byte[] buffer)
         {
            if (buffer.Length < Size)
               throw new ArgumentException("buffer");
            return new StreamHeader()
            {
               Signature = new [] { buffer[0], buffer[1], buffer[2] },
               Version = buffer[3]
            };
         }
         /// <summary>
         /// Deserializes a stream header from a stream
         /// </summary>
         /// <param name="stream">
         /// The stream to read
         /// </param>
         /// <returns>
         /// A valid stream header if successful
         /// An empty header if the end of stream was reached
         /// </returns>
         public static StreamHeader Read (Stream stream)
         {
            var buffer = new Byte[Size];
            var read = stream.Read(buffer, 0, buffer.Length);
            if (read > 0)
            {
               if (read != Size)
                  throw new DataException();
               return Decode(buffer);
            }
            return StreamHeader.Empty;
         }
         /// <summary>
         /// Serializes the stream header to a stream
         /// </summary>
         /// <param name="stream">
         /// The stream to write
         /// </param>
         public void Write (Stream stream)
         {
            if (this.IsEmpty)
               throw new DataException();
            stream.Write(Encode(), 0, Size);
         }
      }

      /// <summary>
      /// Compression stream block header
      /// </summary>
      /// <remarks>
      /// This structure contains length information for each compressed 
      /// block within an encoded stream. It also provides for 
      /// platform-independent serialization of the lengths in big-endian
      /// format.
      /// </remarks>
      private struct BlockHeader
      {
         public const Int32 Size = 8;
         public static readonly BlockHeader Empty = new BlockHeader();

         /// <summary>
         /// The size of the compressed data block
         /// </summary>
         public Int32 EncodedLength { get; set; }
         /// <summary>
         /// The size of the original data block
         /// </summary>
         public Int32 DecodedLength { get; set; }
         /// <summary>
         /// Indicates whether the header is empty
         /// </summary>
         public Boolean IsEmpty
         { 
            get { return this.EncodedLength == 0 && this.DecodedLength == 0; }
         }

         /// <summary>
         /// Serializes the block header to a buffer
         /// </summary>
         /// <returns>
         /// The serialized block header
         /// </returns>
         public Byte[] Encode ()
         {
            Byte[] buffer = new Byte[Size];
            Encode(buffer);
            return buffer;
         }
         /// <summary>
         /// Serializes the block header to a buffer
         /// </summary>
         /// <param name="buffer">
         /// The buffer to write
         /// </param>
         /// <returns>
         /// The number of bytes written
         /// </returns>
         public Int32 Encode (Byte[] buffer)
         {
            if (buffer.Length < Size)
               throw new ArgumentException("buffer");
            buffer[0] = (Byte)(this.EncodedLength >> 24);
            buffer[1] = (Byte)(this.EncodedLength >> 16);
            buffer[2] = (Byte)(this.EncodedLength >> 8);
            buffer[3] = (Byte)(this.EncodedLength >> 0);
            buffer[4] = (Byte)(this.DecodedLength >> 24);
            buffer[5] = (Byte)(this.DecodedLength >> 16);
            buffer[6] = (Byte)(this.DecodedLength >> 8);
            buffer[7] = (Byte)(this.DecodedLength >> 0);
            return Size;
         }
         /// <summary>
         /// Deserializes a block header from a buffer
         /// </summary>
         /// <param name="buffer">
         /// The serialized block header
         /// </param>
         /// <returns>
         /// The deserialized block header
         /// </returns>
         public static BlockHeader Decode (Byte[] buffer)
         {
            if (buffer.Length < Size)
               throw new ArgumentException("buffer");
            return new BlockHeader()
            {
               EncodedLength =
                  ((Int32)buffer[0] << 24) |
                  ((Int32)buffer[1] << 16) |
                  ((Int32)buffer[2] << 8) |
                  ((Int32)buffer[3] << 0),
               DecodedLength =
                  ((Int32)buffer[4] << 24) |
                  ((Int32)buffer[5] << 16) |
                  ((Int32)buffer[6] << 8) |
                  ((Int32)buffer[7] << 0)
            };
         }
         /// <summary>
         /// Deserializes a block header from a stream
         /// </summary>
         /// <param name="stream">
         /// The stream to read
         /// </param>
         /// <returns>
         /// A valid block header if successful
         /// An empty header if the end of stream was reached
         /// </returns>
         public static BlockHeader Read (Stream stream)
         {
            var buffer = new Byte[Size];
            var read = stream.Read(buffer, 0, buffer.Length);
            if (read > 0)
            {
               if (read != Size)
                  throw new DataException();
               return Decode(buffer);
            }
            return BlockHeader.Empty;
         }
         /// <summary>
         /// Serializes the block header to a stream
         /// </summary>
         /// <param name="stream">
         /// The stream to write
         /// </param>
         public void Write (Stream stream)
         {
            if (this.IsEmpty)
               throw new DataException();
            stream.Write(Encode(), 0, Size);
         }
      }

      /// <summary>
      /// Invalid compressed data exception
      /// </summary>
      [Serializable]
      public class DataException : Exception
      {
         /// <summary>
         /// Initializes a new exception instance
         /// </summary>
         public DataException () 
            : base(Strings.CompressionInvalidData)
         {
         }
         /// <summary>
         /// Initializes a new exception instance
         /// </summary>
         /// <param name="message">
         /// Custom exception message
         /// </param>
         public DataException (String message)
            : base(message)
         {
         }
         /// <summary>
         /// Initializes a new exception instance
         /// </summary>
         /// <param name="message">
         /// Custom exception message
         /// </param>
         /// <param name="inner">
         /// Inner exception instance
         /// </param>
         public DataException (String message, Exception inner)
            : base(message, inner)
         {
         }
         /// <summary>
         /// Initializes a new exception instance
         /// </summary>
         /// <param name="info">
         /// Serialization properties
         /// </param>
         /// <param name="context">
         /// Serialization context
         /// </param>
         protected DataException (
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
         {
         }
      }
   }
}
