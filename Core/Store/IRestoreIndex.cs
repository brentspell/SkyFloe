//===========================================================================
// MODULE:  IRestoreIndex.cs
// PURPOSE: store restore index interface
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
using Stream = System.IO.Stream;
// Project References
using SkyFloe.Restore;

namespace SkyFloe.Store
{
   /// <summary>
   /// The store restore index interface
   /// </summary>
   /// <remarks>
   /// This is the interface to a store's implementation of the restore 
   /// index, which provides efficient access to persistent restore metadata. 
   /// The interface includes the CRUD and query/lookup operations needed by 
   /// the backup engine for efficient restore processing.
   /// </remarks>
   public interface IRestoreIndex : IDisposable
   {
      // administrative operations
      Stream Serialize ();
      // header operations
      Header FetchHeader ();
      // session operations
      IEnumerable<Session> ListSessions ();
      Session FetchSession (Int32 id);
      Session InsertSession (Session session);
      Session UpdateSession (Session session);
      void DeleteSession (Session session);
      // path map operations
      PathMap LookupPathMap (Session session, Int32 nodeID);
      PathMap FetchPathMap (Int32 id);
      PathMap InsertPathMap (PathMap PathMap);
      PathMap UpdatePathMap (PathMap PathMap);
      void DeletePathMap (PathMap PathMap);
      // retrieval operations
      IEnumerable<Retrieval> ListRetrievals (Session session);
      IEnumerable<Retrieval> ListBlobRetrievals (Session session, String blob);
      Retrieval FetchRetrieval (Int32 id);
      Retrieval InsertRetrieval (Retrieval retrieval);
      Retrieval UpdateRetrieval (Retrieval retrieval);
      void DeleteRetrieval (Retrieval retrieval);
      // entry operations
      IEnumerable<Entry> ListRetrievalEntries (Retrieval retrieval);
      Entry LookupNextEntry (Session session);
      Entry FetchEntry (Int32 id);
      Entry InsertEntry (Entry entry);
      Entry UpdateEntry (Entry entry);
      void DeleteEntry (Entry entry);
   }
}
