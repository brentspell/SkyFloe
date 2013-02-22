//===========================================================================
// MODULE:  Session.cs
// PURPOSE: restore index session record type
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
   /// Restore session state
   /// </summary>
   public enum SessionState
   {
      Pending = 1,         // session created, can still add backup files
      InProgress = 2,      // session started, restore can proceed
      Completed = 3        // session complete
   }

   /// <summary>
   /// Restore session request flags
   /// </summary>
   [Flags]
   public enum SessionFlags
   {
      SkipExisting = 1,    // don't overwrite any existing files at the target
      SkipReadOnly = 2,    // don't overwrite any read-only files at the target
      VerifyResults = 4,   // verify the CRC of restored files
      EnableDeletes = 8    // delete files at the target marked as deleted in the backup
   }

   /// <summary>
   /// The restore session record type
   /// </summary>
   /// <remarks>
   /// This class tracks a persistent restore process for a backup archive,
   /// allowing a restore to resume from pause or a crash.
   /// </remarks>
   public class Session
   {
      /// <summary>
      /// Record primary key
      /// </summary>
      public Int32 ID { get; set; }
      /// <summary>
      /// Total number of bytes to restore
      /// </summary>
      public Int64 TotalLength { get; set; }
      /// <summary>
      /// Number of bytes restored so far
      /// </summary>
      public Int64 RestoreLength { get; set; }  
      /// <summary>
      /// State of the restore session
      /// </summary>
      public SessionState State { get; set; }
      /// <summary>
      /// Session request flags
      /// </summary>
      public SessionFlags Flags { get; set; }
      /// <summary>
      /// Requested rate limit for the session, in bytes/second
      /// </summary>
      public Int32 RateLimit { get; set; }
      /// <summary>
      /// Record creation stamp
      /// </summary>
      public DateTime Created { get; set; }
      /// <summary>
      /// Retrieves the flag for skipping existing files at the target
      /// </summary>
      public Boolean SkipExisting
      { 
         get { return this.Flags.HasFlag(SessionFlags.SkipExisting); }
      }
      /// <summary>
      /// Retrieves the flag for skipping read-only files at the target
      /// </summary>
      public Boolean SkipReadOnly
      {
         get { return this.Flags.HasFlag(SessionFlags.SkipReadOnly); }
      }
      /// <summary>
      /// Retrieves the flag for verifying restored file CRC values
      /// </summary>
      public Boolean VerifyResults
      {
         get { return this.Flags.HasFlag(SessionFlags.VerifyResults); }
      }
      /// <summary>
      /// Retrieves the flag for deleting files at the target
      /// </summary>
      public Boolean EnableDeletes
      {
         get { return this.Flags.HasFlag(SessionFlags.EnableDeletes); }
      }
   }
}
