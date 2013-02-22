//===========================================================================
// MODULE:  DiffResult.cs
// PURPOSE: difference result type
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

namespace SkyFloe
{
   /// <summary>
   /// File difference type
   /// </summary>
   public enum DiffType
   {
      New,           // file was added
      Changed,       // file changed, according to diff method
      Deleted        // file was deleted
   }

   /// <summary>
   /// The difference result
   /// </summary>
   /// <remarks>
   /// This class represents a single change in the file system as compared
   /// to the contents of a backup archive.
   /// </remarks>
   public class DiffResult
   {
      /// <summary>
      /// The type of difference
      /// </summary>
      public DiffType Type { get; set; }
      /// <summary>
      /// The backup archive node, or a new node if Type=New
      /// </summary>
      public Backup.Node Node { get; set; }
   }
}
