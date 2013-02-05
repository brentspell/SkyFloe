using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

using SkyFloe.Restore;

namespace SkyFloe.Sqlite
{
   public class RestoreIndex : Database, Store.IRestoreIndex
   {
      private const Int32 CurrentVersion = 1;

      private RestoreIndex (String path) : base(path)
      {
      }

      public static RestoreIndex Create (String path, Header header)
      {
         Database.Create(path, "SkyFloe.Sqlite.Resources.RestoreIndex.sql");
         RestoreIndex index = null;
         try
         {
            index = new RestoreIndex(path);
            index.Execute(
               "INSERT INTO Header (Version) " + 
               "VALUES (@p0);", 
               header.Version = CurrentVersion
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
      public static RestoreIndex Open (String path)
      {
         RestoreIndex index = new RestoreIndex(path);
         if (index.FetchHeader().Version != CurrentVersion)
            throw new InvalidOperationException("TODO: invalid version number");
         return index;
      }

      #region Administrative Operations
      public Header FetchHeader ()
      {
         using (IDataReader reader = ExecuteReader("SELECT Version FROM Header;"))
            if (reader.Read())
               return new Header()
               {
                  Version = Convert.ToInt32(reader[0])
               };
         return null;
      }
      #endregion

      #region Session Operations
      public IEnumerable<Session> ListSessions ()
      {
         using (IDataReader reader = ExecuteReader("SELECT ID, Archive, State, Flags, Retrieved, Created FROM Session;"))
            while (reader.Read())
               yield return new Session()
               {
                  ID = Convert.ToInt32(reader[0]),
                  Archive = Convert.ToString(reader[1]),
                  State = (SessionState)Convert.ToInt32(reader[2]),
                  Flags = (SessionFlags)Convert.ToInt32(reader[3]),
                  Retrieved = Convert.ToInt64(reader[4]),
                  Created = DateTime.SpecifyKind(Convert.ToDateTime(reader[5]), DateTimeKind.Utc)
               };
      }
      public Session FetchSession (Int32 id)
      {
         using (IDataReader reader = ExecuteReader("SELECT Archive, State, Flags, Retrieved, Created FROM Session WHERE ID = @p0;", id))
            if (reader.Read())
               return new Session()
               {
                  ID = id,
                  Archive = Convert.ToString(reader[0]),
                  State = (SessionState)Convert.ToInt32(reader[1]),
                  Flags = (SessionFlags)Convert.ToInt32(reader[2]),
                  Retrieved = Convert.ToInt64(reader[3]),
                  Created = DateTime.SpecifyKind(Convert.ToDateTime(reader[4]), DateTimeKind.Utc)
               };
         return null;
      }
      public Session InsertSession (Session session)
      {
         Execute(
            "INSERT INTO Session (Archive, State, Flags, Retrieved, Created) VALUES (@p0, @p1, @p2, @p3, @p4);",
            session.Archive,
            session.State,
            session.Flags,
            session.Retrieved,
            session.Created = DateTime.UtcNow
         );
         session.ID = QueryRowID();
         return session;
      }
      public Session UpdateSession (Session session)
      {
         Execute(
            "UPDATE Session SET Archive = @p1, State = @p2, Flags = @p3, Retrieved = @p4 WHERE ID = @p0;",
            session.ID,
            session.Archive,
            session.State,
            session.Flags,
            session.Retrieved
         );
         return session;
      }
      public void DeleteSession (Session session)
      {
         Execute(
            "DELETE FROM Session WHERE ID = @p0;",
            session.ID
         );
      }
      #endregion

      #region Path Map Operations
      public PathMap LookupPathMap (Session session, Int32 nodeID)
      {
         using (IDataReader reader = ExecuteReader("SELECT ID, Path FROM PathMap WHERE SessionID = @p0 AND NodeID = @p1;", session.ID, nodeID))
            if (reader.Read())
               return new PathMap()
               {
                  ID = Convert.ToInt32(reader[0]),
                  Session = session,
                  NodeID = nodeID,
                  Path = Convert.ToString(reader[1])
               };
         return null;
      }
      public PathMap FetchPathMap (Int32 id)
      {
         using (IDataReader reader = ExecuteReader("SELECT SessionID, NodeID, Path FROM PathMap WHERE ID = @p0;", id))
            if (reader.Read())
               return new PathMap()
               {
                  ID = id,
                  Session = FetchSession(Convert.ToInt32(reader[0])),
                  NodeID = Convert.ToInt32(reader[1]),
                  Path = Convert.ToString(reader[2])
               };
         return null;
      }
      public PathMap InsertPathMap (PathMap PathMap)
      {
         Execute(
            "INSERT INTO PathMap (SessionID, NodeID, Path) VALUES (@p0, @p1, @p2);",
            PathMap.Session.ID,
            PathMap.NodeID,
            PathMap.Path
         );
         PathMap.ID = QueryRowID();
         return PathMap;
      }
      public PathMap UpdatePathMap (PathMap PathMap)
      {
         Execute(
            "UPDATE PathMap SET SessionID = @p1, NodeID = @p2, Path = @p3 WHERE ID = @p0;",
            PathMap.ID,
            PathMap.Session.ID,
            PathMap.NodeID,
            PathMap.Path
         );
         return PathMap;
      }
      public void DeletePathMap (PathMap PathMap)
      {
         Execute(
            "DELETE FROM PathMap WHERE ID = @p0;",
            PathMap.ID
         );
      }
      #endregion

      #region Retrieval Operations
      public IEnumerable<Retrieval> ListRetrievals (Session session)
      {
         using (IDataReader reader = 
               ExecuteReader(
                  "SELECT ID, Blob, Name, Offset, Length FROM Retrieval WHERE SessionID = @p0 ORDER BY ID;",
                  session.ID
               )
            )
            while (reader.Read())
               yield return new Retrieval()
               {
                  ID = Convert.ToInt32(reader[0]),
                  Session = session,
                  Blob = Convert.ToString(reader[1]),
                  Name = (!reader.IsDBNull(2)) ? Convert.ToString(reader[2]) : null,
                  Offset = Convert.ToInt64(reader[3]),
                  Length = Convert.ToInt64(reader[4])
               };
      }
      public IEnumerable<Retrieval> ListBlobRetrievals (Session session, String blob)
      {
         using (IDataReader reader =
               ExecuteReader(
                  "SELECT ID, Name, Offset, Length FROM Retrieval WHERE SessionID = @p0 AND Blob = @p1 ORDER BY ID;",
                  session.ID,
                  blob
               )
            )
            while (reader.Read())
               yield return new Retrieval()
               {
                  ID = Convert.ToInt32(reader[0]),
                  Session = session,
                  Blob = blob,
                  Name = (!reader.IsDBNull(1)) ? Convert.ToString(reader[1]) : null,
                  Offset = Convert.ToInt64(reader[2]),
                  Length = Convert.ToInt64(reader[3])
               };
      }
      public Retrieval FetchRetrieval (Int32 id)
      {
         using (IDataReader reader = ExecuteReader("SELECT SessionID, Blob, Name, Offset, Length FROM Retrieval WHERE ID = @p0;", id))
            if (reader.Read())
               return new Retrieval()
               {
                  ID = id,
                  Session = FetchSession(Convert.ToInt32(reader[0])),
                  Blob = Convert.ToString(reader[1]),
                  Name = (!reader.IsDBNull(2)) ? Convert.ToString(reader[2]) : null,
                  Offset = Convert.ToInt64(reader[3]),
                  Length = Convert.ToInt64(reader[4])
               };
         return null;
      }
      public Retrieval InsertRetrieval (Retrieval retrieval)
      {
         Execute(
            "INSERT INTO Retrieval (SessionID, Blob, Name, Offset, Length) VALUES (@p0, @p1, @p2, @p3, @p4);",
            retrieval.Session.ID,
            retrieval.Blob,
            retrieval.Name,
            retrieval.Offset,
            retrieval.Length
         );
         retrieval.ID = QueryRowID();
         return retrieval;
      }
      public Retrieval UpdateRetrieval (Retrieval retrieval)
      {
         Execute(
            "UPDATE Retrieval SET SessionID = @p1, Blob = @p2, Name = @p3, Offset = @p4, Length = @p5 WHERE ID = @p0;",
            retrieval.ID,
            retrieval.Session.ID,
            retrieval.Blob,
            retrieval.Name,
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
      public IEnumerable<Entry> ListRetrievalEntries (Retrieval retrieval)
      {
         using (IDataReader reader = 
               ExecuteReader(
                  "SELECT ID, BackupEntryID, State, Offset, Length FROM Entry WHERE RetrievalID = @p0 ORDER BY Offset;",
                  retrieval.ID
               )
            )
            while (reader.Read())
               yield return new Entry()
               {
                  ID = Convert.ToInt32(reader[0]),
                  BackupEntryID = Convert.ToInt32(reader[1]),
                  Retrieval = retrieval,
                  State = (EntryState)Convert.ToInt32(reader[2]),
                  Offset = Convert.ToInt64(reader[3]),
                  Length = Convert.ToInt64(reader[4])
               };
      }
      public Entry LookupNextEntry (Session session)
      {
         using (IDataReader reader =
               ExecuteReader(
                  "SELECT ID, BackupEntryID, RetrievalID, State, Offset, Length " + 
                  "FROM Entry " + 
                  "WHERE SessionID = @p0 AND " + 
                  "      State = @p1 " + 
                  "ORDER BY RetrievalID, Offset " + 
                  "LIMIT 1;",
                  session.ID,
                  EntryState.Pending
               )
            )
            if (reader.Read())
            {
               Entry entry = new Entry()
               {
                  ID = Convert.ToInt32(reader[0]),
                  BackupEntryID = Convert.ToInt32(reader[1]),
                  Retrieval = FetchRetrieval(Convert.ToInt32(reader[2])),
                  State = (EntryState)Convert.ToInt32(reader[3]),
                  Offset = Convert.ToInt64(reader[4]),
                  Length = Convert.ToInt64(reader[5])
               };
               entry.Retrieval.Session = session;
               return entry;
            }
         return null;
      }
      public Entry FetchEntry (Int32 id)
      {
         using (IDataReader reader = 
               ExecuteReader(
                  "SELECT BackupEntryID, RetrievalID, State, Offset, Length FROM Entry WHERE ID = @p0;", 
                  id
               )
            )
            if (reader.Read())
               return new Entry()
               {
                  ID = id,
                  BackupEntryID = Convert.ToInt32(reader[0]),
                  Retrieval = FetchRetrieval(Convert.ToInt32(reader[1])),
                  State = (EntryState)Convert.ToInt32(reader[2]),
                  Offset = Convert.ToInt64(reader[3]),
                  Length = Convert.ToInt64(reader[4])
               };
         return null;
      }
      public Entry InsertEntry (Entry entry)
      {
         Execute(
            "INSERT INTO Entry (BackupEntryID, SessionID, RetrievalID, State, Offset, Length) VALUES (@p0, @p1, @p2, @p3, @p4, @p5);",
            entry.BackupEntryID,
            entry.Retrieval.Session.ID,
            entry.Retrieval.ID,
            Convert.ToInt32(entry.State),
            entry.Offset,
            entry.Length
         );
         return entry;
      }
      public Entry UpdateEntry (Entry entry)
      {
         Execute(
            "UPDATE Entry SET BackupEntryID = @p1, SessionID = @p2, RetrievalID = @p3, State = @p4, Offset = @p5, Length = @p6 WHERE ID = @p0;",
            entry.ID,
            entry.BackupEntryID,
            entry.Retrieval.Session.ID,
            entry.Retrieval.ID,
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
