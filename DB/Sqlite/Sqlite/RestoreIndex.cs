//===========================================================================
// MODULE:  RestoreIndex.cs
// PURPOSE: Sqlite restore index implementation
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
using System.Data;
using System.Linq;
// Project References
using SkyFloe.Restore;
using SkyFloe.Store;

namespace SkyFloe.Sqlite
{
   /// <summary>
   /// The Sqlite restore index
   /// </summary>
   /// <remarks>
   /// This class implements the IRestoreIndex interface using an embedded
   /// Sqlite database for storage and retrieval.
   /// </remarks>
   public class RestoreIndex : Database, IRestoreIndex
   {
      private const Int32 CurrentVersion = 1;

      /// <summary>
      /// Connects to an existing restore index
      /// </summary>
      /// <param name="path">
      /// The path to the restore index to open
      /// </param>
      private RestoreIndex (IO.Path path)
         : base(path)
      {
      }

      #region Administrative Operations
      /// <summary>
      /// Creates a new restore index
      /// </summary>
      /// <param name="path">
      /// The path to the restore index file to create
      /// </param>
      /// <param name="header">
      /// The restore index header to insert
      /// </param>
      /// <returns>
      /// A new restore index implementation
      /// </returns>
      public static IRestoreIndex Create (IO.Path path, Header header)
      {
         // create the restore index file and schema
         Database.Create(path, "SkyFloe.Sqlite.Resources.RestoreIndex.sql");
         var index = (RestoreIndex)null;
         try
         {
            // connect to the database and add the header row
            index = new RestoreIndex(path);
            index.Execute(
               "INSERT INTO Header (" +
                  "Version) " + 
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
      /// <summary>
      /// Connects to an existing restore index database
      /// </summary>
      /// <param name="path">
      /// The path to the restore index to open
      /// </param>
      /// <returns>
      /// A new restore index implementation
      /// </returns>
      public static IRestoreIndex Open (IO.Path path)
      {
         var index = new RestoreIndex(path);
         if (index.FetchHeader().Version != CurrentVersion)
            throw new InvalidOperationException("Invalid database version");
         return index;
      }
      /// <summary>
      /// Retrieves the database header
      /// </summary>
      /// <returns></returns>
      public Header FetchHeader ()
      {
         return Fetch(
            "SELECT Version " +
            "FROM Header;",
            reader => new Header()
            {
               Version = Convert.ToInt32(reader[0])
            }
         );
      }
      #endregion

      #region Session Operations
      /// <summary>
      /// Retrieves all session records
      /// </summary>
      /// <returns>
      /// The enumeration of sessionss
      /// </returns>
      public IEnumerable<Session> ListSessions ()
      {
         return Enumerate(
            "SELECT " +
               "ID, " +
               "TotalLength, " +
               "RestoreLength, " +
               "State, " +
               "Flags, " +
               "RateLimit, " +
               "Created " +
            "FROM Session;",
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
      /// <summary>
      /// Fetches a session record by primary key
      /// </summary>
      /// <param name="id">
      /// The record primary key
      /// </param>
      /// <returns>
      /// The requested session if found
      /// Null otherwise
      /// </returns>
      public Session FetchSession (Int32 id)
      {
         return Fetch(
            "SELECT " +
               "TotalLength, " +
               "RestoreLength, " +
               "State, " +
               "Flags, " +
               "RateLimit, " +
               "Created " +
            "FROM Session " +
            "WHERE ID = @p0;",
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
      /// <summary>
      /// Inserts a new session record
      /// </summary>
      /// <param name="session">
      /// The session to insert
      /// </param>
      /// <returns>
      /// The inserted session, including the generated primary key
      /// </returns>
      public Session InsertSession (Session session)
      {
         Execute(
            "INSERT INTO Session (" +
               "TotalLength, " +
               "RestoreLength, " +
               "State, " +
               "Flags, " +
               "RateLimit, " +
               "Created) " +
            "VALUES (@p0, @p1, @p2, @p3, @p4, @p5);",
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
      /// <summary>
      /// Updates an existing session record
      /// </summary>
      /// <param name="session">
      /// The session to update
      /// </param>
      /// <returns>
      /// The updated session record
      /// </returns>
      public Session UpdateSession (Session session)
      {
         Execute(
            "UPDATE Session SET " +
               "TotalLength = @p1, " +
               "RestoreLength = @p2, " +
               "State = @p3, " +
               "Flags = @p4, " +
               "RateLimit = @p5 " +
            "WHERE ID = @p0;",
            session.ID,
            session.TotalLength,
            session.RestoreLength,
            session.State,
            session.Flags,
            session.RateLimit
         );
         return session;
      }
      /// <summary>
      /// Deletes an existing session
      /// </summary>
      /// <param name="session">
      /// The session to delete
      /// </param>
      public void DeleteSession (Session session)
      {
         Execute(
            "DELETE FROM Session WHERE ID = @p0;",
            session.ID
         );
      }
      #endregion

      #region Path Map Operations
      /// <summary>
      /// Searches for a path map record for a source node
      /// </summary>
      /// <param name="session">
      /// The restore session containing the path map
      /// </param>
      /// <param name="nodeID">
      /// The source backup node to map
      /// </param>
      /// <returns>
      /// The requested path map if found
      /// Null otherwise
      /// </returns>
      public PathMap LookupPathMap (Session session, Int32 nodeID)
      {
         return Fetch(
            "SELECT " +
               "ID, " +
               "Path " +
            "FROM PathMap " +
            "WHERE SessionID = @p0 " +
                  "AND NodeID = @p1;",
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
      /// <summary>
      /// Fetches a path map record by primary key
      /// </summary>
      /// <param name="id">
      /// The record primary key
      /// </param>
      /// <returns>
      /// The requested path map if found
      /// Null otherwise
      /// </returns>
      public PathMap FetchPathMap (Int32 id)
      {
         return Fetch(
            "SELECT " +
               "SessionID, " +
               "NodeID, " +
               "Path " +
            "FROM PathMap " +
            "WHERE ID = @p0;",
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
      /// <summary>
      /// Inserts a new path map record
      /// </summary>
      /// <param name="pathMap">
      /// The path map to insert
      /// </param>
      /// <returns>
      /// The inserted path map, including the generated primary key
      /// </returns>
      public PathMap InsertPathMap (PathMap pathMap)
      {
         Execute(
            "INSERT INTO PathMap (" +
               "SessionID, " +
               "NodeID, " +
               "Path) " +
            "VALUES (@p0, @p1, @p2);",
            pathMap.Session.ID,
            pathMap.NodeID,
            pathMap.Path
         );
         pathMap.ID = GetLastRowID();
         return pathMap;
      }
      /// <summary>
      /// Updates an existing path map record
      /// </summary>
      /// <param name="pathMap">
      /// The path map to update
      /// </param>
      /// <returns>
      /// The updated path map record
      /// </returns>
      public PathMap UpdatePathMap (PathMap pathMap)
      {
         Execute(
            "UPDATE PathMap SET " +
               "SessionID = @p1, " +
               "NodeID = @p2, " +
               "Path = @p3 " +
            "WHERE ID = @p0;",
            pathMap.ID,
            pathMap.Session.ID,
            pathMap.NodeID,
            pathMap.Path
         );
         return pathMap;
      }
      /// <summary>
      /// Deletes an existing path map
      /// </summary>
      /// <param name="pathMap">
      /// The path map to delete
      /// </param>
      public void DeletePathMap (PathMap pathMap)
      {
         Execute(
            "DELETE FROM PathMap WHERE ID = @p0;",
            pathMap.ID
         );
      }
      #endregion

      #region Retrieval Operations
      /// <summary>
      /// Retrieves all retrieval records associated with a restore session
      /// </summary>
      /// <param name="session">
      /// The restore session to query
      /// </param>
      /// <returns>
      /// The enumeration of retrievals
      /// </returns>
      public IEnumerable<Retrieval> ListRetrievals (Session session)
      {
         return Enumerate(
            "SELECT " +
               "ID, " +
               "Blob, " +
               "Name, " +
               "Offset, " +
               "Length " +
            "FROM Retrieval " +
            "WHERE SessionID = @p0 " +
            "ORDER BY ID;",
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
      /// <summary>
      /// Retrieves all retrieval records associated with a source blob
      /// </summary>
      /// <param name="session">
      /// The restore session to query
      /// </param>
      /// <param name="blob">
      /// The name of the source blob to query
      /// </param>
      /// <returns>
      /// The enumeration of retrievals
      /// </returns>
      public IEnumerable<Retrieval> ListBlobRetrievals (Session session, String blob)
      {
         return Enumerate(
            "SELECT " +
               "ID, " +
               "Name, " +
               "Offset, " +
               "Length " +
            "FROM Retrieval " +
            "WHERE SessionID = @p0 " +
                  "AND Blob = @p1 " +
            "ORDER BY ID;",
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
      /// <summary>
      /// Fetches a retrieval record by primary key
      /// </summary>
      /// <param name="id">
      /// The record primary key
      /// </param>
      /// <returns>
      /// The requested retrieval if found
      /// Null otherwise
      /// </returns>
      public Retrieval FetchRetrieval (Int32 id)
      {
         return Fetch(
            "SELECT " +
               "SessionID, " +
               "Blob, " +
               "Name, " +
               "Offset, " +
               "Length " +
            "FROM Retrieval " +
            "WHERE ID = @p0;",
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
      /// <summary>
      /// Inserts a new retrieval record
      /// </summary>
      /// <param name="retrieval">
      /// The retrieval to insert
      /// </param>
      /// <returns>
      /// The inserted retrieval, including the generated primary key
      /// </returns>
      public Retrieval InsertRetrieval (Retrieval retrieval)
      {
         Execute(
            "INSERT INTO Retrieval (" +
               "SessionID, " +
               "Blob, " +
               "Name, " +
               "Offset, " +
               "Length) " +
            "VALUES (@p0, @p1, @p2, @p3, @p4);",
            retrieval.Session.ID,
            retrieval.Blob,
            retrieval.Name,
            retrieval.Offset,
            retrieval.Length
         );
         retrieval.ID = GetLastRowID();
         return retrieval;
      }
      /// <summary>
      /// Updates an existing retrieval record
      /// </summary>
      /// <param name="retrieval">
      /// The retrieval to update
      /// </param>
      /// <returns>
      /// The updated retrieval record
      /// </returns>
      public Retrieval UpdateRetrieval (Retrieval retrieval)
      {
         Execute(
            "UPDATE Retrieval SET " +
               "SessionID = @p1, " +
               "Blob = @p2, " +
               "Name = @p3, " +
               "Offset = @p4, " +
               "Length = @p5 " +
            "WHERE ID = @p0;",
            retrieval.ID,
            retrieval.Session.ID,
            retrieval.Blob,
            retrieval.Name,
            retrieval.Offset,
            retrieval.Length
         );
         return retrieval;
      }
      /// <summary>
      /// Deletes an existing retrieval
      /// </summary>
      /// <param name="retrieval">
      /// The retrieval to delete
      /// </param>
      public void DeleteRetrieval (Retrieval retrieval)
      {
         Execute(
            "DELETE FROM Retrieval WHERE ID = @p0;",
            retrieval.ID
         );
      }
      #endregion

      #region Entry Operations
      /// <summary>
      /// Retrieves the entry records associated with a retrieval
      /// </summary>
      /// <param name="retrieval">
      /// The retrieval to query
      /// </param>
      /// <returns>
      /// The enumeration of entries
      /// </returns>
      public IEnumerable<Entry> ListRetrievalEntries (Retrieval retrieval)
      {
         return Enumerate(
            "SELECT " +
               "ID, " +
               "BackupEntryID, " +
               "State, " +
               "Offset, " +
               "Length " +
            "FROM Entry " +
            "WHERE RetrievalID = @p0 " +
            "ORDER BY Offset;",
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
      /// <summary>
      /// Searches for the next pending entry record
      /// </summary>
      /// <param name="session">
      /// The session to query
      /// </param>
      /// <returns>
      /// The next entry record with a status of pending, if any
      /// Null otherwise
      /// </returns>
      public Entry LookupNextEntry (Session session)
      {
         return Fetch(
            "SELECT " +
               "ID, " +
               "BackupEntryID, " +
               "RetrievalID, " +
               "State, " +
               "Offset, " +
               "Length " +
            "FROM Entry " +
            "WHERE SessionID = @p0 " +
                  "AND State = @p1 " +
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
      /// <summary>
      /// Fetches an entry record by primary key
      /// </summary>
      /// <param name="id">
      /// The record primary key
      /// </param>
      /// <returns>
      /// The requested entry if found
      /// Null otherwise
      /// </returns>
      public Entry FetchEntry (Int32 id)
      {
         return Fetch(
            "SELECT " +
               "BackupEntryID, " +
               "SessionID, " +
               "RetrievalID, " +
               "State, " +
               "Offset, " +
               "Length " +
            "FROM Entry WHERE ID = @p0;",
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
      /// <summary>
      /// Inserts a new entry record
      /// </summary>
      /// <param name="entry">
      /// The entry to insert
      /// </param>
      /// <returns>
      /// The inserted entry, including the generated primary key
      /// </returns>
      public Entry InsertEntry (Entry entry)
      {
         Execute(
            "INSERT INTO Entry (" +
               "BackupEntryID, " +
               "SessionID, " +
               "RetrievalID, " +
               "State, " +
               "Offset, " +
               "Length) " +
            "VALUES (@p0, @p1, @p2, @p3, @p4, @p5);",
            entry.BackupEntryID,
            entry.Session.ID,
            (entry.Retrieval != null) ? entry.Retrieval.ID : (Object)null,
            Convert.ToInt32(entry.State),
            entry.Offset,
            entry.Length
         );
         return entry;
      }
      /// <summary>
      /// Updates an existing entry record
      /// </summary>
      /// <param name="entry">
      /// The entry to update
      /// </param>
      /// <returns>
      /// The updated entry record
      /// </returns>
      public Entry UpdateEntry (Entry entry)
      {
         Execute(
            "UPDATE Entry SET " +
               "BackupEntryID = @p1, " +
               "SessionID = @p2, " +
               "RetrievalID = @p3, " +
               "State = @p4, " +
               "Offset = @p5, " +
               "Length = @p6 " +
            "WHERE ID = @p0;",
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
      /// <summary>
      /// Deletes an existing entry
      /// </summary>
      /// <param name="entry">
      /// The entry to delete
      /// </param>
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
