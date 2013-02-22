//===========================================================================
// MODULE:  DiffRequest.cs
// PURPOSE: difference request type
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
   /// File differencing method
   /// </summary>
   public enum DiffMethod
   {
      Timestamp = 1,    // diff using file last write times
      Digest = 2        // diff using file digest (CRC)
   }

   /// <summary>
   /// The difference request
   /// </summary>
   /// <remarks>
   /// This class encapsulates the parameters passed to the backup engine
   /// to start a new differencing operation. A differencing operation
   /// compres the contents of a backup index with a source file system,
   /// to find files that have been added, removed, changed, or possibly 
   /// corrupted.
   /// </remarks>
   public class DiffRequest
   {
      /// <summary>
      /// Initializes a new request instance
      /// </summary>
      public DiffRequest ()
      {
         this.RootPathMap = new Dictionary<IO.Path, IO.Path>();
         this.Filter = new RegexFilter();
      }

      /// <summary>
      /// The source root path mapping, used to map root backup node
      /// paths to target directories
      /// </summary>
      public IDictionary<IO.Path, IO.Path> RootPathMap { get; set; }
      /// <summary>
      /// The differencing method to use
      /// </summary>
      public DiffMethod Method { get; set; }
      /// <summary>
      /// The target file filter for the diff operation
      /// </summary>
      public RegexFilter Filter { get; set; }
   }
}
