using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using SkyFloe.Restore;

namespace SkyFloe.Store
{
   public interface IRestoreSession : IDisposable
   {
      // administrative operations
      Stream Serialize ();
      // header operations
      Header FetchHeader ();
      // retrieval operations
      IEnumerable<Retrieval> ListRetrievals ();
      IEnumerable<Retrieval> ListBlobRetrievals (Int32 blobID);
      Retrieval FetchRetrieval (Int32 id);
      Retrieval InsertRetrieval (Retrieval retrieval);
      Retrieval UpdateRetrieval (Retrieval retrieval);
      void DeleteRetrieval (Retrieval retrieval);
      // entry operations
      Entry LookupNextEntry ();
      Entry FetchEntry (Int32 id);
      Entry InsertEntry (Entry entry);
      Entry UpdateEntry (Entry entry);
      void DeleteEntry (Entry entry);
   }
}
