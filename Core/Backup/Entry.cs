//===========================================================================
// MODULE:  Entry.cs
// PURPOSE: backup index file entry record type
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
// Project References

namespace SkyFloe.Backup
{
   /// <summary>
   /// Backup entry state
   /// </summary>
   public enum EntryState
   {
      Pending = 1,      // created, but not backed up
      Completed = 2,    // backed up successfuly
      Deleted = 3,      // indicates a deleted file
      Failed = 4        // backup failed for this file and was skipped
   }

   /// <summary>
   /// The file entry record type
   /// </summary>
   /// <remarks>
   /// This class represents the backup metadata for a given file node within 
   /// a single backup session. A file node may contain more than one backup
   /// entry, with one for each session containing the file. This maintains
   /// the file's history within the archive for point-in-time recovery.
   /// Files that have not changed since the most recent backup session
   /// will not have corresponding entry records in the subsequent session.
   /// </remarks>
   public class Entry
   {
      /// <summary>
      /// Record primary key
      /// </summary>
      public Int32 ID { get; set; }
      /// <summary>
      /// Backup session that includes the entry
      /// </summary>
      public Session Session { get; set; }
      /// <summary>
      /// File node being backed up
      /// </summary>
      public Node Node { get; set; }
      /// <summary>
      /// Backup blob containing the file's data
      /// </summary>
      public Blob Blob { get; set; }
      /// <summary>
      /// State of the backup entry
      /// </summary>
      public EntryState State { get; set; }
      /// <summary>
      /// Offset of the entry in the blob, in bytes
      /// </summary>
      public Int64 Offset { get; set; }
      /// <summary>
      /// Length of the file in this backup, in bytes
      /// </summary>
      public Int64 Length { get; set; }
      /// <summary>
      /// CRC for the file in this backup
      /// </summary>
      [CLSCompliant(false)]
      public UInt32 Crc32 { get; set; }
   }
}
