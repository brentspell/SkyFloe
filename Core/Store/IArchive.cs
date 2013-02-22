//===========================================================================
// MODULE:  IArchive.cs
// PURPOSE: store archive interface
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

namespace SkyFloe.Store
{
   /// <summary>
   /// The store archive interface
   /// </summary>
   /// <remarks>
   /// This is the interface to a connected named archive implemented by a 
   /// pluggable backup storage component. Each archive exposes the backup
   /// index containing all backup sessions, as well as the restore index,
   /// which tracks local restore sessions.
   /// </remarks>
   public interface IArchive : IDisposable
   {
      /// <summary>
      /// The archive name, unique within the store connection
      /// </summary>
      String Name { get; }
      /// <summary>
      /// The archive backup index, containing metadata for all backup
      /// sessions performed within the archive
      /// </summary>
      IBackupIndex BackupIndex { get; }
      /// <summary>
      /// The local restore index, containing metadata for all restores
      /// performed from this archive on the local computer by the
      /// current user
      /// </summary>
      IRestoreIndex RestoreIndex { get; }
      /// <summary>
      /// Prepares the archive for starting/resuming a backup session
      /// </summary>
      /// <param name="session">
      /// The backup session to process
      /// </param>
      /// <returns>
      /// An object that can be used to control the backup process
      /// </returns>
      IBackup PrepareBackup (Backup.Session session);
      /// <summary>
      /// Prepares the archive for starting/resuming a restore session
      /// </summary>
      /// <param name="session">
      /// The restore session to process
      /// </param>
      /// <returns>
      /// An object that can be used to control the restore process
      /// </returns>
      IRestore PrepareRestore (Restore.Session session);
   }
}
