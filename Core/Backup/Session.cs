//===========================================================================
// MODULE:  Session.cs
// PURPOSE: backup index session record type
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
   /// Backup session state
   /// </summary>
   public enum SessionState
   {
      Pending = 1,         // session created, can still add source files
      InProgress = 2,      // session started, file backup can proceed
      Completed = 3        // session complete and committed
   }

   /// <summary>
   /// The backup session record type
   /// </summary>
   /// <remarks>
   /// This class represents a single backup process performed on a source 
   /// file system into an archive. There can be any number of historical 
   /// completed backup sessions, but at most one session in either of the 
   /// pending or in-progress states. Each backup session consists of a set
   /// of backup entries, at most one per file node. The entries track the
   /// state of each file at the time of the session.
   /// </remarks>
   public class Session
   {
      /// <summary>
      /// Record primary key
      /// </summary>
      public Int32 ID { get; set; }
      /// <summary>
      /// State of the backup session
      /// </summary>
      public SessionState State { get; set; }
      /// <summary>
      /// Requested rate limit for the session, in bytes/second
      /// </summary>
      public Int32 RateLimit { get; set; }
      /// <summary>
      /// Requested session checkpoint interval, in bytes backed up
      /// </summary>
      public Int64 CheckpointLength { get; set; }
      /// <summary>
      /// Estimated total length of the backup session, in bytes
      /// </summary>
      public Int64 EstimatedLength { get; set; }
      /// <summary>
      /// Number of bytes committed to the archive for this session
      /// </summary>
      public Int64 ActualLength { get; set; }
      /// <summary>
      /// Record creation stamp
      /// </summary>
      public DateTime Created { get; set; }
   }
}
