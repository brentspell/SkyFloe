﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using SkyFloe.Restore;

namespace SkyFloe.Sqlite
{
   public class RestoreIndex : Database, Store.IRestoreIndex
   {
      private const Int32 CurrentVersion = 1;

      private RestoreIndex (IO.Path path)
         : base(path)
      {
      }

      public static RestoreIndex Create (IO.Path path, Header header)
      {
         Database.Create(path, "SkyFloe.Sqlite.Resources.RestoreIndex.sql");
         var index = (RestoreIndex)null;
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
      public static RestoreIndex Open (IO.Path path)
      {
         var index = new RestoreIndex(path);
         if (index.FetchHeader().Version != CurrentVersion)
            throw new InvalidOperationException("TODO: invalid version number");
         return index;
      }

      #region Administrative Operations
      public Header FetchHeader ()
      {
         return Fetch(
            "SELECT Version FROM Header;",
            reader => new Header()
            {
               Version = Convert.ToInt32(reader[0])
            }
         );
      }
      #endregion

      #region Session Operations
      public IEnumerable<Session> ListSessions ()
      {
         return Query(
            "SELECT ID, TotalLength, RestoreLength, State, Flags, RateLimit, Created FROM Session;",
            reader => new Session()
            {
               ID = Convert.ToInt32(reader[0]),
               TotalLength = Convert.ToInt64(reader[1]),
               RestoreLength = Convert.ToInt64(reader[2]),
               State = (SessionState)Convert.ToInt32(reader[3]),
               Flags = (SessionFlags)Convert.ToInt32(reader[4]),
               RateLimit = Convert.ToInt32(reader[5]),
               Created = DateTime.SpecifyKind(Convert.ToDateTime(reader[6]), DateTimeKind.Utc)
            }
         );
      }
      public Session FetchSession (Int32 id)
      {
         return Fetch(
            "SELECT TotalLength, RestoreLength, State, Flags, RateLimit, Created FROM Session WHERE ID = @p0;",
            new Object[] { id },
            reader => new Session()
            {
               ID = id,
               TotalLength = Convert.ToInt64(reader[0]),
               RestoreLength = Convert.ToInt64(reader[1]),
               State = (SessionState)Convert.ToInt32(reader[2]),
               Flags = (SessionFlags)Convert.ToInt32(reader[3]),
               RateLimit = Convert.ToInt32(reader[4]),
               Created = DateTime.SpecifyKind(Convert.ToDateTime(reader[5]), DateTimeKind.Utc)
            }
         );
      }
      public Session InsertSession (Session session)
      {
         Execute(
            "INSERT INTO Session (TotalLength, RestoreLength, State, Flags, RateLimit, Created) VALUES (@p0, @p1, @p2, @p3, @p4, @p5);",
            session.TotalLength,
            session.RestoreLength,
            session.State,
            session.Flags,
            session.RateLimit,
            session.Created = DateTime.UtcNow
         );
         session.ID = GetLastRowID();
         return session;
      }
      public Session UpdateSession (Session session)
      {
         Execute(
            "UPDATE Session SET TotalLength = @p1, RestoreLength = @p2, State = @p3, Flags = @p4, RateLimit = @p5 WHERE ID = @p0;",
            session.ID,
            session.TotalLength,
            session.RestoreLength,
            session.State,
            session.Flags,
            session.RateLimit
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
         return Fetch(
            "SELECT ID, Path FROM PathMap WHERE SessionID = @p0 AND NodeID = @p1;",
            new Object[] { session.ID, nodeID },
            reader => new PathMap()
            {
               ID = Convert.ToInt32(reader[0]),
               Session = session,
               NodeID = nodeID,
               Path = Convert.ToString(reader[1])
            }
         );
      }
      public PathMap FetchPathMap (Int32 id)
      {
         return Fetch(
            "SELECT SessionID, NodeID, Path FROM PathMap WHERE ID = @p0;",
            new Object[] { id },
            reader => new PathMap()
            {
               ID = id,
               Session = FetchSession(Convert.ToInt32(reader[0])),
               NodeID = Convert.ToInt32(reader[1]),
               Path = Convert.ToString(reader[2])
            }
         );
      }
      public PathMap InsertPathMap (PathMap PathMap)
      {
         Execute(
            "INSERT INTO PathMap (SessionID, NodeID, Path) VALUES (@p0, @p1, @p2);",
            PathMap.Session.ID,
            PathMap.NodeID,
            PathMap.Path
         );
         PathMap.ID = GetLastRowID();
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
         return Query(
            "SELECT ID, Blob, Name, Offset, Length FROM Retrieval WHERE SessionID = @p0 ORDER BY ID;",
            new Object[] { session.ID },
            reader => new Retrieval()
            {
               ID = Convert.ToInt32(reader[0]),
               Session = session,
               Blob = Convert.ToString(reader[1]),
               Name = (!reader.IsDBNull(2)) ? Convert.ToString(reader[2]) : null,
               Offset = Convert.ToInt64(reader[3]),
               Length = Convert.ToInt64(reader[4])
            }
         );
      }
      public IEnumerable<Retrieval> ListBlobRetrievals (Session session, String blob)
      {
         return Query(
            "SELECT ID, Name, Offset, Length FROM Retrieval WHERE SessionID = @p0 AND Blob = @p1 ORDER BY ID;",
            new Object[] { session.ID, blob },
            reader => new Retrieval()
            {
               ID = Convert.ToInt32(reader[0]),
               Session = session,
               Blob = blob,
               Name = (!reader.IsDBNull(1)) ? Convert.ToString(reader[1]) : null,
               Offset = Convert.ToInt64(reader[2]),
               Length = Convert.ToInt64(reader[3])
            }
         );
      }
      public Retrieval FetchRetrieval (Int32 id)
      {
         return Fetch(
            "SELECT SessionID, Blob, Name, Offset, Length FROM Retrieval WHERE ID = @p0;",
            new Object[] { id },
            reader => new Retrieval()
            {
               ID = id,
               Session = FetchSession(Convert.ToInt32(reader[0])),
               Blob = Convert.ToString(reader[1]),
               Name = (!reader.IsDBNull(2)) ? Convert.ToString(reader[2]) : null,
               Offset = Convert.ToInt64(reader[3]),
               Length = Convert.ToInt64(reader[4])
            }
         );
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
         retrieval.ID = GetLastRowID();
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
         return Query(
            "SELECT ID, BackupEntryID, State, Offset, Length FROM Entry WHERE RetrievalID = @p0 ORDER BY Offset;",
            new Object[] { retrieval.ID },
            reader => new Entry()
            {
               ID = Convert.ToInt32(reader[0]),
               BackupEntryID = Convert.ToInt32(reader[1]),
               Session = retrieval.Session,
               Retrieval = retrieval,
               State = (EntryState)Convert.ToInt32(reader[2]),
               Offset = Convert.ToInt64(reader[3]),
               Length = Convert.ToInt64(reader[4])
            }
         );
      }
      public Entry LookupNextEntry (Session session)
      {
         return Fetch(
            "SELECT ID, BackupEntryID, RetrievalID, State, Offset, Length " +
            "FROM Entry " +
            "WHERE SessionID = @p0 AND " +
            "      State = @p1 " +
            "ORDER BY RetrievalID, Offset " +
            "LIMIT 1;",
            new Object[] { session.ID, EntryState.Pending },
            reader =>
            {
               var entry = new Entry()
               {
                  ID = Convert.ToInt32(reader[0]),
                  BackupEntryID = Convert.ToInt32(reader[1]),
                  Session = session,
                  Retrieval = (!reader.IsDBNull(2)) ? FetchRetrieval(Convert.ToInt32(reader[2])) : null,
                  State = (EntryState)Convert.ToInt32(reader[3]),
                  Offset = Convert.ToInt64(reader[4]),
                  Length = Convert.ToInt64(reader[5])
               };
               if (entry.Retrieval != null)
                  entry.Retrieval.Session = session;
               return entry;
            }
         );
      }
      public Entry FetchEntry (Int32 id)
      {
         return Fetch(
            "SELECT BackupEntryID, SessionID, RetrievalID, State, Offset, Length FROM Entry WHERE ID = @p0;",
            new Object[] { id },
            reader => new Entry()
            {
               ID = id,
               BackupEntryID = Convert.ToInt32(reader[0]),
               Session = FetchSession(Convert.ToInt32(reader[1])),
               Retrieval = (!reader.IsDBNull(2)) ? FetchRetrieval(Convert.ToInt32(reader[2])) : null,
               State = (EntryState)Convert.ToInt32(reader[3]),
               Offset = Convert.ToInt64(reader[4]),
               Length = Convert.ToInt64(reader[5])
            }
         );
      }
      public Entry InsertEntry (Entry entry)
      {
         Execute(
            "INSERT INTO Entry (BackupEntryID, SessionID, RetrievalID, State, Offset, Length) VALUES (@p0, @p1, @p2, @p3, @p4, @p5);",
            entry.BackupEntryID,
            entry.Session.ID,
            (entry.Retrieval != null) ? entry.Retrieval.ID : (Object)null,
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
            entry.Session.ID,
            (entry.Retrieval != null) ? entry.Retrieval.ID : (Object)null,
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
