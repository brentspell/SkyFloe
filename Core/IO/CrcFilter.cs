//===========================================================================
// MODULE:  CrcFilter.cs
// PURPOSE: CRC calculating stream filter
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
   /// CRC stream filter
   /// </summary>
   /// <remarks>
   /// This class calculates a 32-bit CRC value from an underlying stream as 
   /// data is read from/written to it. It also provides static operations 
   /// for calculating CRC values without filtering a stream. The CRC 
   /// algorithm uses the  ANSI X3.66/ITU-T V.42/ethernet polynomial 
   /// (0xEDB88320 in little endian).
   /// </remarks>
   [CLSCompliant(false)]
   public class CrcFilter : FilterStream
   {
      public const UInt32 InitialValue = 0xFFFFFFFF;
      private const UInt32 Polynomial = 0xEDB88320;
      private static UInt32[] table = new UInt32[256];
      private UInt32 value;

      /// <summary>
      /// Initializes the CRC calculation tables
      /// </summary>
      static CrcFilter ()
      {
         for (var i = 0u; i < table.Length; i++)
         {
            var temp = i;
            for (var j = 8; j > 0; j--)
            {
               if ((temp & 1) == 1)
                  temp = (temp >> 1) ^ Polynomial;
               else
                  temp >>= 1;
            }
            table[i] = temp;
         }
      }

      /// <summary>
      /// Initializes the CRC filter
      /// </summary>
      /// <param name="baseStream">
      /// The base stream to attach
      /// </param>
      public CrcFilter (Stream baseStream) : base(baseStream)
      {
         this.value = InitialValue;
      }
      
      /// <summary>
      /// The current value calculated from the underlying stream
      /// </summary>
      public UInt32 Value
      {
         get { return CalculateFinal(this.value); }
      }

      #region CRC-32 Operations
      /// <summary>
      /// Calculates a CRC checksum over a buffer.
      /// </summary>
      /// <param name="buffer">
      /// The buffer to process
      /// </param>
      /// <param name="offset">
      /// The offset into the buffer
      /// </param>
      /// <param name="length">
      /// The number of bytes to process
      /// </param>
      /// <returns>
      /// The CRC value for the buffer
      /// </returns>
      public static UInt32 Calculate (Byte[] buffer, Int32 offset = 0, Int32 length = -1)
      {
         if (length == -1)
            length = buffer.Length;
         return CalculateFinal(CalculateIncremental(InitialValue, buffer, offset, length));
      }
      /// <summary>
      /// Calculates a CRC checksum over a stream.
      /// </summary>
      /// <param name="stream">
      /// The stream to process
      /// </param>
      /// <returns>
      /// The CRC value for the stream
      /// </returns>
      public static UInt32 Calculate (Stream stream)
      {
         var crc = InitialValue;
         var buffer = new Byte[8192];
         for (; ; )
         {
            var read = stream.Read(buffer, 0, buffer.Length);
            if (read == 0)
               break;
            crc = CalculateIncremental(crc, buffer, 0, read);
         }
         return CalculateFinal(crc);
      }
      /// <summary>
      /// Calculates a CRC checksum over a file.
      /// </summary>
      /// <param name="path">
      /// The path to the file to process
      /// </param>
      /// <returns>
      /// The CRC value for the file
      /// </returns>
      public static UInt32 Calculate (String path)
      {
         using (var stream = new FileStream(
               path,
               FileMode.Open,
               FileAccess.Read,
               FileShare.Read,
               65536,
               FileOptions.SequentialScan))
            return Calculate(stream);
      }
      /// <summary>
      /// Calculates an incremental CRC checksum
      /// </summary>
      /// <param name="crc">
      /// The current CRC value
      /// </param>
      /// <param name="buffer">
      /// The buffer to process
      /// </param>
      /// <param name="offset">
      /// The offset into the buffer
      /// </param>
      /// <param name="length">
      /// The number of bytes to process
      /// </param>
      /// <returns>
      /// The updated CRC value for the stream
      /// </returns>
      public static UInt32 CalculateIncremental (
         UInt32 crc,
         Byte[] buffer,
         Int32 offset,
         Int32 length)
      {
         for (var i = offset; i < offset + length; i++)
            crc = (crc >> 8) ^ table[(crc & 0xff) ^ buffer[i]];
         return crc;
      }
      /// <summary>
      /// Finalizes an incremental CRC checksum
      /// </summary>
      /// <param name="crc">
      /// The incremental CRC value to finalize
      /// </param>
      /// <returns>
      /// The finalized CRC checksum
      /// </returns>
      public static UInt32 CalculateFinal (UInt32 crc)
      {
         return ~crc;
      }
      #endregion

      #region FilterStream Overrides
      /// <summary>
      /// Calculates an incremental CRC for a buffer read from/written to
      /// the underlying stream
      /// </summary>
      /// <param name="buffer">
      /// The buffer read/written
      /// </param>
      /// <param name="offset">
      /// The read/write offset
      /// </param>
      /// <param name="count">
      /// The number of bytes read/written
      /// </param>
      protected override void Filter (Byte [] buffer, Int32 offset, Int32 count)
      {
         this.value = CalculateIncremental(this.value, buffer, offset, count);
      }
      #endregion
   }
}
