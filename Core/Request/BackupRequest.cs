//===========================================================================
// MODULE:  BackupRequest.cs
// PURPOSE: backup request type
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
using System.Text.RegularExpressions;
// Project References

namespace SkyFloe
{
   /// <summary>
   /// The backup request
   /// </summary>
   /// <remarks>
   /// This class encapsulates the parameters used to create a new backup
   /// session in the backup engine.
   /// </remarks>
   public class BackupRequest
   {
      /// <summary>
      /// Initializes a new request instance
      /// </summary>
      public BackupRequest ()
      {
         this.Sources = Enumerable.Empty<IO.Path>();
         this.Filter = new RegexFilter();
      }

      /// <summary>
      /// List of source directory paths to include in the backup
      /// </summary>
      public IEnumerable<IO.Path> Sources { get; set; }
      /// <summary>
      /// The backup source file filter
      /// </summary>
      public RegexFilter Filter { get; set; }
      /// <summary>
      /// The file differencing method, for incremental backups
      /// </summary>
      public DiffMethod DiffMethod { get; set; }
      /// <summary>
      /// The backup rate limit, in bytes/second
      /// </summary>
      public Int32 RateLimit { get; set; }
      /// <summary>
      /// The backup checkpoint interval, in bytes
      /// </summary>
      public Int64 CheckpointLength { get; set; }
   }
}
