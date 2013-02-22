//===========================================================================
// MODULE:  PathMap.cs
// PURPOSE: restore index root path mapping record type
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
   /// The root path mapping record type
   /// </summary>
   /// <remarks>
   /// This class represents a path remapping from the source path of a
   /// root backup node to a target file system path. This provides the 
   /// ability to restore the contents of a backup to an location on disk
   /// that is different from the original backup location.
   /// </remarks>
   public class PathMap
   {
      /// <summary>
      /// Record primary key
      /// </summary>
      public Int32 ID { get; set; }
      /// <summary>
      /// Restore session containing the path mapping
      /// </summary>
      public Session Session { get; set; }
      /// <summary>
      /// The primary key of the root backup node being mapped
      /// </summary>
      public Int32 NodeID { get; set; }
      /// <summary>
      /// The mapped path
      /// </summary>
      public String Path { get; set; }
   }
}
