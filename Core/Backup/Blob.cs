//===========================================================================
// MODULE:  Blob.cs
// PURPOSE: backup index blob record type
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
   /// The blob record type
   /// </summary>
   /// <remarks>
   /// This class contains the metadata for a chunk of a backup archive, as
   /// maintained in a store-specific location. Backup archives may be stored
   /// in multiple chunks when working with write-once storage (tape, 
   /// Amazon Glacier, etc.) and a backup session is interrupted/resumed.
   /// For appendable storage (i.e. files), a backup session may contain as 
   /// few as one blob.
   /// </remarks>
   public class Blob
   {
      /// <summary>
      /// Record primary key
      /// </summary>
      public Int32 ID { get; set; }
      /// <summary>
      /// Blob symbolic name, unique within the archive
      /// </summary>
      public String Name { get; set; }
      /// <summary>
      /// Blob length, in bytes
      /// </summary>
      public Int64 Length { get; set; }
      /// <summary>
      /// Record create stamp
      /// </summary>
      public DateTime Created { get; set; }
      /// <summary>
      /// Record update stamp
      /// </summary>
      public DateTime Updated { get; set; }
   }
}
