﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SkyFloe.IO
{
   [CLSCompliant(false)]
   public class Crc32Filter : FilterStream
   {
      public const UInt32 InitialValue = 0xFFFFFFFF;
      private static UInt32[] table = new UInt32[256];
      private UInt32 value;

      static Crc32Filter ()
      {
         const UInt32 poly = 0xEDB88320;
         for (UInt32 i = 0; i < table.Length; i++)
         {
            UInt32 temp = i;
            for (Int32 j = 8; j > 0; j--)
            {
               if ((temp & 1) == 1)
                  temp = (temp >> 1) ^ poly;
               else
                  temp >>= 1;
            }
            table[i] = temp;
         }
      }

      public Crc32Filter (Stream stream) : base(stream)
      {
         this.value = InitialValue;
      }

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
         UInt32 crc = InitialValue;
         Byte[] buffer = new Byte[8192];
         for (; ; )
         {
            Int32 actual = stream.Read(buffer, 0, buffer.Length);
            if (actual == 0)
               break;
            crc = CalculateIncremental(crc, buffer, 0, actual);
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
         using (FileStream stream = new FileStream(
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
         for (Int32 i = offset; i < offset + length; i++)
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
      protected override void Filter (Byte [] buffer, Int32 offset, Int32 count)
      {
         this.value = CalculateIncremental(this.value, buffer, offset, count);
      }
      #endregion
   }
}