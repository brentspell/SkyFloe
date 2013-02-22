//===========================================================================
// MODULE:  Entry.cs
// PURPOSE: restore index file entry record type
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

namespace SkyFloe.Restore
{
   /// <summary>
   /// Restore entry state
   /// </summary>
   public enum EntryState
   {
      Pending = 1,      // entry selected, but not restored
      Completed = 2,    // restore completed for the entry
      Failed = 3        // restore failed and skipped for the entry
   }

   /// <summary>
   /// The file entry record type
   /// </summary>
   /// <remarks>
   /// This class maintains the restore state for an entry from the backup
   /// index. It provides the ability to persistently pause/resume the
   /// restore process.
   /// </remarks>
   public class Entry
   {
      /// <summary>
      /// Record primary key
      /// </summary>
      public Int32 ID { get; set; }
      /// <summary>
      /// Primary key of the corresponding backup entry record
      /// </summary>
      public Int32 BackupEntryID { get; set; }
      /// <summary>
      /// Restore session that includes this entry
      /// </summary>
      public Session Session { get; set; }
      /// <summary>
      /// Blob retrieval metadata for this restore entry
      /// </summary>
      public Retrieval Retrieval { get; set; }
      /// <summary>
      /// State of the restore entry
      /// </summary>
      public EntryState State { get; set; }
      /// <summary>
      /// Offset of the entry, in bytes, from the start of the retrieval
      /// </summary>
      public Int64 Offset { get; set; }
      /// <summary>
      /// Length of the entry in the retrieval, in bytes
      /// </summary>
      public Int64 Length { get; set; }
   }
}
