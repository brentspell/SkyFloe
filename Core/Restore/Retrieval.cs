//===========================================================================
// MODULE:  Retrieval.cs
// PURPOSE: restore index blob retrieval record type
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
   /// The blob retrieval record type
   /// </summary>
   /// <remarks>
   /// This class represents a retrieval operation to be performed within
   /// a restore session. By default, all blob contents are retrieved
   /// during the restore session and there is a single retrieval record
   /// for each blob referenced. This class makes it possible for a store
   /// to more intelligently schedule retrievals, in order to minimize
   /// the amount of data retrieved (cloud storage) or ensure that it is 
   /// retrieved in sequential order (tape).
   /// </remarks>
   public class Retrieval
   {
      /// <summary>
      /// Record primary key
      /// </summary>
      public Int32 ID { get; set; }
      /// <summary>
      /// Restore session including the retrieval
      /// </summary>
      public Session Session { get; set; }
      /// <summary>
      /// Symbolic name of the backup blob to retrieve
      /// </summary>
      public String Blob { get; set; }
      /// <summary>
      /// Symbolic name of the retrieval, for store-specific references
      /// </summary>
      public String Name { get; set; }
      /// <summary>
      /// Offset of the retrieval, in bytes, within the blob
      /// </summary>
      public Int64 Offset { get; set; }
      /// <summary>
      /// Length of the retrieval, in bytes
      /// </summary>
      public Int64 Length { get; set; }
   }
}
