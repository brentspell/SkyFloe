//===========================================================================
// MODULE:  IBackup.cs
// PURPOSE: store backup interface
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
using System.IO;
using System.Linq;
// Project References

namespace SkyFloe.Store
{
   /// <summary>
   /// The store backup interface
   /// </summary>
   /// <remarks>
   /// This is the interface to an active backup process running on an 
   /// archive within a connected backup store.
   /// </remarks>
   public interface IBackup : IDisposable
   {
      /// <summary>
      /// Writes the contents of a backup entry to the archive
      /// </summary>
      /// <param name="entry">
      /// The entry to backup
      /// </param>
      /// <param name="stream">
      /// The stream for the source file to backup
      /// </param>
      void Backup (Backup.Entry entry, Stream stream);
      /// <summary>
      /// Commits any outstanding changes to the backup, making all changes 
      /// since the previous checkpoint durable
      /// </summary>
      void Checkpoint ();
   }
}
