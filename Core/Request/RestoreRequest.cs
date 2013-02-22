//===========================================================================
// MODULE:  RestoreRequest.cs
// PURPOSE: restore request type
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
   /// The restore request
   /// </summary>
   /// <remarks>
   /// This class encapsulates the parameters used to create a new restore
   /// session in the backup engine.
   /// </remarks>
   public class RestoreRequest
   {
      /// <summary>
      /// Initializes a new request instance
      /// </summary>
      public RestoreRequest ()
      {
         this.RootPathMap = new Dictionary<IO.Path, IO.Path>();
         this.Entries = Enumerable.Empty<Int32>();
         this.Filter = new RegexFilter();
      }

      /// <summary>
      /// The source root path mapping, used to map root backup node
      /// paths to restore directories
      /// </summary>
      public IDictionary<IO.Path, IO.Path> RootPathMap { get; set; }
      /// <summary>
      /// The list of primary keys of the backup entries to restore
      /// </summary>
      public IEnumerable<Int32> Entries { get; set; }
      /// <summary>
      /// The filter to use to determine which files to restore, applied
      /// to the full source path of each backup entry
      /// </summary>
      public RegexFilter Filter { get; set; }
      /// <summary>
      /// Specifies whether to ignore existing files at the target
      /// </summary>
      public Boolean SkipExisting { get; set; }
      /// <summary>
      /// Specifies whether to ignore read-only files at the target
      /// </summary>
      public Boolean SkipReadOnly { get; set; }
      /// <summary>
      /// Specifies whether to verify the restored file CRCs
      /// </summary>
      public Boolean VerifyResults { get; set; }
      /// <summary>
      /// Specifies whether to delete files at the target that were
      /// specified as deleted in the backup archive
      /// </summary>
      public Boolean EnableDeletes { get; set; }
      /// <summary>
      /// The restore retrieval rate limit, in bytes/second
      /// </summary>
      public Int32 RateLimit { get; set; }
   }
}
