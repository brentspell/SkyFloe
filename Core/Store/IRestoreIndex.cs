using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using SkyFloe.Restore;

namespace SkyFloe.Store
{
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
