using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using SkyFloe.Restore;

namespace SkyFloe.Sqlite
{
   public class RestoreSession : Database, Store.IRestoreSession
   {
      private const Int32 CurrentVersion = 1;

      private RestoreSession (String path) : base(path)
      {
      }

      public static RestoreSession Create (String path, Header header)
      {
         Database.Create(path, "SkyFloe.Sqlite.Resources.RestoreSession.sql");
         RestoreSession index = null;
         try
         {
            index = new RestoreSession(path);
            index.Execute(
               "INSERT INTO Header (Version, Archive) " + 
               "VALUES (@p0, @p1);", 
               header.Version = CurrentVersion,
               header.Archive
            );
            return index;
         }
         catch
         {
            if (index != null)
               index.Dispose();
            try { Database.Delete(path); } catch { }
            throw;
         }
      }
      public static RestoreSession Open (String path)
      {
         var index = new RestoreSession(path);
         if (index.FetchHeader().Version != CurrentVersion)
            throw new InvalidOperationException("TODO: invalid version number");
         return index;
      }

      #region Administrative Operations
      public Header FetchHeader ()
      {
         using (var reader = ExecuteReader("SELECT Version, Archive FROM Header;"))
            if (reader.Read())
               return new Header()
               {
                  Version = Convert.ToInt32(reader[0]),
                  Archive = Convert.ToString(reader[1])
               };
         return null;
      }
      #endregion

      #region Retrieval Operations
      public IEnumerable<Retrieval> ListRetrievals ()
      {
         using (var reader = ExecuteReader("SELECT ID, Name, BlobID, Offset, Length FROM Retrieval ORDER BY ID;"))
            while (reader.Read())
               yield return new Retrieval()
               {
                  ID = Convert.ToInt32(reader[0]),
                  Name = Convert.ToString(reader[1]),
                  BlobID = Convert.ToInt32(reader[2]),
                  Offset = Convert.ToInt64(reader[3]),
                  Length = Convert.ToInt64(reader[4])
               };
      }
      public IEnumerable<Retrieval> ListBlobRetrievals (Int32 blobID)
      {
         using (var reader = ExecuteReader("SELECT ID, Name, Offset, Length FROM Retrieval WHERE BlobID = @p0;", blobID))
            while (reader.Read())
               yield return new Retrieval()
               {
                  ID = Convert.ToInt32(reader[0]),
                  Name = Convert.ToString(reader[1]),
                  BlobID = blobID,
                  Offset = Convert.ToInt64(reader[2]),
                  Length = Convert.ToInt64(reader[3])
               };
      }
      public Retrieval FetchRetrieval (Int32 id)
      {
         using (var reader = ExecuteReader("SELECT Name, BlobID, Offset, Length FROM Retrieval WHERE ID = @p0;", id))
            if (reader.Read())
               return new Retrieval()
               {
                  ID = id,
                  Name = Convert.ToString(reader[0]),
                  BlobID = Convert.ToInt32(reader[1]),
                  Offset = Convert.ToInt64(reader[2]),
                  Length = Convert.ToInt64(reader[3])
               };
         return null;
      }
      public Retrieval InsertRetrieval (Retrieval retrieval)
      {
         Execute(
            "INSERT INTO Retrieval (Name, BlobID, Offset, Length) VALUES (@p0, @p1, @p2);",
            retrieval.Name,
            retrieval.BlobID,
            retrieval.Offset,
            retrieval.Length
         );
         retrieval.ID = QueryRowID();
         return retrieval;
      }
      public Retrieval UpdateRetrieval (Retrieval retrieval)
      {
         Execute(
            "UPDATE Retrieval SET Name = @p1, BlobID = @p2, Offset = @p3, Length = @p4 WHERE ID = @p0;",
            retrieval.ID,
            retrieval.Name,
            retrieval.BlobID,
            retrieval.Offset,
            retrieval.Length
         );
         return retrieval;
      }
      public void DeleteRetrieval (Retrieval retrieval)
      {
         Execute(
            "DELETE FROM Retrieval WHERE ID = @p0;",
            retrieval.ID
         );
      }
      #endregion

      #region Entry Operations
      public Entry LookupNextEntry ()
      {
         using (var reader =
               ExecuteReader(
                  "SELECT ID, RetrievalID, State, Offset, Length FROM Entry WHERE State = @p1 ORDER BY RetrievalID, Offset LIMIT 1;",
                  EntryState.Pending
               )
            )
            if (reader.Read())
               return new Entry()
               {
                  ID = Convert.ToInt32(reader[0]),
                  Retrieval = (!reader.IsDBNull(1)) ? FetchRetrieval(Convert.ToInt32(reader[1])) : null,
                  State = (EntryState)Convert.ToInt32(reader[2]),
                  Offset = Convert.ToInt64(reader[3]),
                  Length = Convert.ToInt64(reader[4])
               };
         return null;
      }
      public Entry FetchEntry (Int32 id)
      {
         using (var reader = 
               ExecuteReader(
                  "SELECT RetrievalID, State, Offset, Length FROM Entry WHERE ID = @p0;", 
                  id
               )
            )
            if (reader.Read())
               return new Entry()
               {
                  ID = id,
                  Retrieval = (!reader.IsDBNull(0)) ? FetchRetrieval(Convert.ToInt32(reader[0])) : null,
                  State = (EntryState)Convert.ToInt32(reader[1]),
                  Offset = Convert.ToInt64(reader[2]),
                  Length = Convert.ToInt64(reader[3])
               };
         return null;
      }
      public Entry InsertEntry (Entry entry)
      {
         Execute(
            "INSERT INTO Entry (ID, RetrievalID, State, Offset, Length) VALUES (@p0, @p1, @p2, @p3, @p4);",
            entry.ID,
            (entry.Retrieval != null) ? (Object)entry.Retrieval.ID : null,
            Convert.ToInt32(entry.State),
            entry.Offset,
            entry.Length
         );
         return entry;
      }
      public Entry UpdateEntry (Entry entry)
      {
         Execute(
            "UPDATE Entry SET RetrievalID = @p1, State = @p2, Offset = @p3, Length = @p4 WHERE ID = @p0;",
            entry.ID,
            (entry.Retrieval != null) ? (Object)entry.Retrieval.ID : null,
            Convert.ToInt32(entry.State),
            entry.Offset,
            entry.Length
         );
         return entry;
      }
      public void DeleteEntry (Entry entry)
      {
         Execute(
            "DELETE FROM Entry WHERE ID = @p0;",
            entry.ID
         );
      }
      #endregion
   }
}
