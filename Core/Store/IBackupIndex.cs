//===========================================================================
// MODULE:  IBackupIndex.cs
// PURPOSE: store backup index interface
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
using SkyFloe.Backup;

namespace SkyFloe.Store
{
   /// <summary>
   /// The store backup index interface
   /// </summary>
   /// <remarks>
   /// This is the interface to a store's implementation of the backup index,
   /// which provides efficient access to archive metadata. The interface
   /// includes the CRUD and query/lookup operations needed by the backup
   /// engine and other components for efficient archive processing.
   /// </remarks>
   public interface IBackupIndex : IDisposable
   {
      // administrative operations
      Stream Serialize ();
      // header operations
      Header FetchHeader ();
      // blob operations
      IEnumerable<Blob> ListBlobs ();
      Blob LookupBlob (String name);
      Blob FetchBlob (Int32 id);
      Blob InsertBlob (Blob blob);
      Blob UpdateBlob (Blob blob);
      void DeleteBlob (Blob blob);
      // session operations
      IEnumerable<Session> ListSessions ();
      Session FetchSession (Int32 id);
      Session InsertSession (Session session);
      Session UpdateSession (Session session);
      void DeleteSession (Session session);
      // node operations
      IEnumerable<Node> ListNodes (Node parent);
      Node FetchNode (Int32 id);
      Node InsertNode (Node node);
      Node UpdateNode (Node node);
      void DeleteNode (Node node);
      // entry operations
      IEnumerable<Entry> ListNodeEntries (Node node);
      Entry LookupNextEntry (Session session);
      Entry FetchEntry (Int32 id);
      Entry InsertEntry (Entry entry);
      Entry UpdateEntry (Entry entry);
      void DeleteEntry (Entry entry);
   }
}
