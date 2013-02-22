//===========================================================================
// MODULE:  IStore.cs
// PURPOSE: store interface
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
   /// The store interface
   /// </summary>
   /// <remarks>
   /// This interface represents the entry point to a custom component that
   /// provides storage of backup archives and restore metadata.
   /// In addition to the explicit interface described below, there is an 
   /// implicit property contract used to map connection string parameters to 
   /// the store implementation prior to calling the Open() method. For
   /// more information on store property binding, see the Connection class.
   /// </remarks>
   public interface IStore : IDisposable
   {
      /// <summary>
      /// A user-friendly description of the store connection
      /// </summary>
      String Caption { get; }
      /// <summary>
      /// Connects to the store, using the bound properties
      /// </summary>
      void Open ();
      /// <summary>
      /// Retrieves the list of names of archives within the connected store
      /// </summary>
      /// <returns>
      /// The list of archives within the store
      /// </returns>
      IEnumerable<String> ListArchives ();
      /// <summary>
      /// Creates a new archive
      /// </summary>
      /// <param name="name">
      /// The unique name of the new archive
      /// </param>
      /// <param name="header">
      /// The archive header
      /// </param>
      /// <returns>
      /// An interface to the created archive
      /// </returns>
      IArchive CreateArchive (String name, Backup.Header header);
      /// <summary>
      /// Connects to an existing archive
      /// </summary>
      /// <param name="name">
      /// The name of the archive to open
      /// </param>
      /// <returns>
      /// An interface to the connected archive
      /// </returns>
      IArchive OpenArchive (String name);
      /// <summary>
      /// Removes an archive from the store, irrecoverably
      /// </summary>
      /// <param name="name">
      /// The archive to delete
      /// </param>
      void DeleteArchive (String name);
   }
}
