using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using SkyFloe.Model;

namespace SkyFloe.Store
{
   public interface IIndex : IDisposable
   {
      // administrative operations
      Int64 Size { get; }
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
      Entry LookupNextPendingEntry (Session session);
      Entry FetchEntry (Int32 id);
      Entry InsertEntry (Entry entry);
      Entry UpdateEntry (Entry entry);
      void DeleteEntry (Entry entry);
   }
}
