//===========================================================================
// MODULE:  BackupIndex.cs
// PURPOSE: Sqlite backup index implementation
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
using SkyFloe.Backup;
using SkyFloe.Store;

namespace SkyFloe.Sqlite
{
   /// <summary>
   /// The Sqlite backup index
   /// </summary>
   /// <remarks>
   /// This class implements the IBackupIndex interface using an embedded
   /// Sqlite database for storage and retrieval.
   /// </remarks>
   public class BackupIndex : Database, IBackupIndex
   {
      private const Int32 CurrentVersion = 1;

      /// <summary>
      /// Connects to an existing backup index
      /// </summary>
      /// <param name="path">
      /// The path to the backup index to open
      /// </param>
      private BackupIndex (IO.Path path)
         : base(path)
      {
      }

      #region Administrative Operations
      /// <summary>
      /// Creates a new backup index
      /// </summary>
      /// <param name="path">
      /// The path to the backup index file to create
      /// </param>
      /// <param name="header">
      /// The backup index header to insert
      /// </param>
      /// <returns>
      /// A new backup index implementation
      /// </returns>
      public static IBackupIndex Create (IO.Path path, Header header)
      {
         // create the backup index file and schema
         Database.Create(path, "SkyFloe.Sqlite.Resources.BackupIndex.sql");
         var index = (BackupIndex)null;
         try
         {
            // connect to the database and add the header row
            index = new BackupIndex(path);
            index.Execute(
               "INSERT INTO Header (" + 
               "   Version, " +
               "   CryptoIterations, " +
               "   ArchiveSalt, " +
               "   PasswordHash, " + 
               "   PasswordSalt) " + 
               "VALUES (@p0, @p1, @p2, @p3, @p4);", 
               header.Version = CurrentVersion,
               header.CryptoIterations,
               header.ArchiveSalt,
               header.PasswordHash,
               header.PasswordSalt
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
      /// Connects to an existing backup index database
      /// </summary>
      /// <param name="path">
      /// The path to the backup index to open
      /// </param>
      /// <returns>
      /// A new backup index implementation
      /// </returns>
      public static IBackupIndex Open (IO.Path path)
      {
         BackupIndex index = new BackupIndex(path);
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
            "SELECT " + 
               "Version, " +
               "CryptoIterations, " + 
               "ArchiveSalt, " + 
               "PasswordHash, " + 
               "PasswordSalt " + 
            "FROM Header;",
            reader => new Header()
            {
               Version = Convert.ToInt32(reader[0]),
               CryptoIterations = Convert.ToInt32(reader[1]),
               ArchiveSalt = (Byte[])reader[2],
               PasswordHash = (Byte[])reader[3],
               PasswordSalt = (Byte[])reader[4]
            }
         );
      }
      #endregion

      #region Blob Operations
      /// <summary>
      /// Retrieves all blob records
      /// </summary>
      /// <returns>
      /// The enumeration of blobs
      /// </returns>
      public IEnumerable<Blob> ListBlobs ()
      {
         return Enumerate(
            "SELECT " + 
               "ID, " + 
               "Name, " +
               "Length, " +
               "Created, " +
               "Updated " + 
            "FROM Blob;",
            reader => new Blob()
            {
               ID = Convert.ToInt32(reader[0]),
               Name = Convert.ToString(reader[1]),
               Length = Convert.ToInt64(reader[2]),
               Created = DateTime.SpecifyKind(Convert.ToDateTime(reader[3]), DateTimeKind.Utc),
               Updated = DateTime.SpecifyKind(Convert.ToDateTime(reader[4]), DateTimeKind.Utc),
            }
         );
      }
      /// <summary>
      /// Searches for a blob record by name
      /// </summary>
      /// <param name="name">
      /// The name of the blob to lookup
      /// </param>
      /// <returns>
      /// The requested blob if found
      /// Null otherwise
      /// </returns>
      public Blob LookupBlob (String name)
      {
         return Fetch(
            "SELECT " +
               "ID, " +
               "Length, " +
               "Created, " +
               "Updated " +
            "FROM Blob " +
            "WHERE Name = @p0;",
            new Object[] { name },
            reader => new Blob()
            {
               ID = Convert.ToInt32(reader[0]),
               Name = name,
               Length = Convert.ToInt64(reader[1]),
               Created = DateTime.SpecifyKind(Convert.ToDateTime(reader[2]), DateTimeKind.Utc),
               Updated = DateTime.SpecifyKind(Convert.ToDateTime(reader[3]), DateTimeKind.Utc),
            }
         );
      }
      /// <summary>
      /// Fetches a blob record by primary key
      /// </summary>
      /// <param name="id">
      /// The record primary key
      /// </param>
      /// <returns>
      /// The requested blob if found
      /// Null otherwise
      /// </returns>
      public Blob FetchBlob (Int32 id)
      {
         return Fetch(
            "SELECT " +
               "Name, " +
               "Length, " +
               "Created, " +
               "Updated " +
            "FROM Blob " +
            "WHERE ID = @p0;",
            new Object[] { id },
            reader => new Blob()
            {
               ID = id,
               Name = Convert.ToString(reader[0]),
               Length = Convert.ToInt64(reader[1]),
               Created = DateTime.SpecifyKind(Convert.ToDateTime(reader[2]), DateTimeKind.Utc),
               Updated = DateTime.SpecifyKind(Convert.ToDateTime(reader[3]), DateTimeKind.Utc),
            }
         );
      }
      /// <summary>
      /// Inserts a new blob record
      /// </summary>
      /// <param name="blob">
      /// The blob to insert
      /// </param>
      /// <returns>
      /// The inserted blob, including the generated primary key
      /// </returns>
      public Blob InsertBlob (Blob blob)
      {
         Execute(
            "INSERT INTO Blob (" +
               "Name, " +
               "Length, " +
               "Created, " +
               "Updated) " +
            "VALUES (@p0, @p1, @p2, @p2);",
            blob.Name,
            blob.Length,
            blob.Updated = blob.Created = DateTime.UtcNow
         );
         blob.ID = GetLastRowID();
         return blob;
      }
      /// <summary>
      /// Updates an existing blob record
      /// </summary>
      /// <param name="blob">
      /// The blob to update
      /// </param>
      /// <returns>
      /// The updated blob record
      /// </returns>
      public Blob UpdateBlob (Blob blob)
      {
         Execute(
            "UPDATE Blob SET " +
               "Name = @p1, " +
               "Length = @p2, " +
               "Updated = @p3 " +
            "WHERE ID = @p0;",
            blob.ID,
            blob.Name,
            blob.Length,
            blob.Updated = DateTime.UtcNow
         );
         return blob;
      }
      /// <summary>
      /// Deletes an existing blob
      /// </summary>
      /// <param name="blob">
      /// The blob to delete
      /// </param>
      public void DeleteBlob (Blob blob)
      {
         Execute(
            "DELETE FROM Blob WHERE ID = @p0;",
            blob.ID
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
               "State, " +
               "Flags, " +
               "RateLimit, " +
               "CheckpointLength, " +
               "EstimatedLength, " +
               "ActualLength, " +
               "Created " +
            "FROM Session;",
            reader => new Session()
            {
               ID = Convert.ToInt32(reader[0]),
               State = (SessionState)Convert.ToInt32(reader[1]),
               Flags = (SessionFlags)Convert.ToInt32(reader[2]),
               RateLimit = Convert.ToInt32(reader[3]),
               CheckpointLength = Convert.ToInt64(reader[4]),
               EstimatedLength = Convert.ToInt64(reader[5]),
               ActualLength = Convert.ToInt64(reader[6]),
               Created = DateTime.SpecifyKind(Convert.ToDateTime(reader[7]), DateTimeKind.Utc)
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
               "State, " +
               "Flags, " +
               "RateLimit, " +
               "CheckpointLength, " +
               "EstimatedLength, " +
               "ActualLength, " +
               "Created " +
            "FROM Session " +
            "WHERE ID = @p0;",
            new Object[] { id },
            reader => new Session()
            {
               ID = id,
               State = (SessionState)Convert.ToInt32(reader[0]),
               Flags = (SessionFlags)Convert.ToInt32(reader[1]),
               RateLimit = Convert.ToInt32(reader[2]),
               CheckpointLength = Convert.ToInt64(reader[3]),
               EstimatedLength = Convert.ToInt64(reader[4]),
               ActualLength = Convert.ToInt64(reader[5]),
               Created = DateTime.SpecifyKind(Convert.ToDateTime(reader[6]), DateTimeKind.Utc)
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
               "State, " +
               "Flags, " +
               "RateLimit, " +
               "CheckpointLength, " +
               "EstimatedLength, " +
               "ActualLength, " +
               "Created) " +
            "VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6);",
            session.State,
            session.Flags,
            session.RateLimit,
            session.CheckpointLength,
            session.EstimatedLength,
            session.ActualLength,
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
               "State = @p1, " +
               "Flags = @p2, " +
               "RateLimit = @p3, " +
               "CheckpointLength = @p4, " +
               "EstimatedLength = @p5, " +
               "ActualLength = @p6 " +
            "WHERE ID = @p0;",
            session.ID,
            session.State,
            session.Flags,
            session.RateLimit,
            session.CheckpointLength,
            session.EstimatedLength,
            session.ActualLength
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

      #region Node Operations
      /// <summary>
      /// Retrieves a list of child or root node records
      /// </summary>
      /// <param name="parent">
      /// The parent node to query
      /// Retrieve all root nodes if null
      /// </param>
      /// <returns>
      /// The enumeration of nodes
      /// </returns>
      public IEnumerable<Node> ListNodes (Node parent = null)
      {
         return Enumerate(
            (parent == null) ?
               "SELECT " +
                  "ID, " +
                  "Type, " +
                  "Name " +
               "FROM Node " +
               "WHERE ParentID IS NULL;" :
               "SELECT " +
                  "ID, " +
                  "Type, " +
                  "Name " +
               "FROM Node " +
               "WHERE ParentID = @p0;",
            new Object[] { (parent != null) ? (Object)parent.ID : null },
            reader => new Node()
            {
               ID = Convert.ToInt32(reader[0]),
               Parent = parent,
               Type = (NodeType)Convert.ToInt32(reader[1]),
               Name = Convert.ToString(reader[2])
            }
         );
      }
      /// <summary>
      /// Fetches a node record by primary key
      /// </summary>
      /// <param name="id">
      /// The record primary key
      /// </param>
      /// <returns>
      /// The requested node if found
      /// Null otherwise
      /// </returns>
      public Node FetchNode (Int32 id)
      {
         return Fetch(
            "SELECT " +
               "ParentID, " +
               "Type, " +
               "Name " +
            "FROM Node " +
            "WHERE ID = @p0;",
            new Object[] { id },
            reader => new Node()
            {
               ID = id,
               Parent = (!reader.IsDBNull(0)) ? FetchNode(Convert.ToInt32(reader[0])) : null,
               Type = (NodeType)Convert.ToInt32(reader[1]),
               Name = Convert.ToString(reader[2])
            }
         );
      }
      /// <summary>
      /// Inserts a new node record
      /// </summary>
      /// <param name="node">
      /// The node to insert
      /// </param>
      /// <returns>
      /// The inserted node, including the generated primary key
      /// </returns>
      public Node InsertNode (Node node)
      {
         Execute(
            "INSERT INTO Node (" +
               "ParentID, " +
               "Type, " +
               "Name) " +
            "VALUES (@p0, @p1, @p2);",
            (node.Parent != null) ? (Object)node.Parent.ID : null,
            Convert.ToInt32(node.Type),
            node.Name
         );
         node.ID = GetLastRowID();
         return node;
      }
      /// <summary>
      /// Updates an existing node record
      /// </summary>
      /// <param name="node">
      /// The node to update
      /// </param>
      /// <returns>
      /// The updated node record
      /// </returns>
      public Node UpdateNode (Node node)
      {
         Execute(
            "UPDATE Node SET " +
               "ParentID = @p1, " +
               "Type = @p2, " +
               "Name = @p3 " +
            "WHERE ID = @p0;",
            node.ID,
            (node.Parent != null) ? (Object)node.Parent.ID : null,
            Convert.ToInt32(node.Type),
            node.Name
         );
         return node;
      }
      /// <summary>
      /// Deletes an existing node
      /// </summary>
      /// <param name="node">
      /// The node to delete
      /// </param>
      public void DeleteNode (Node node)
      {
         Execute(
            "DELETE FROM Node WHERE ID = @p0;",
            node.ID
         );
      }
      #endregion

      #region Entry Operations
      /// <summary>
      /// Retrieves the entry records associated with a node
      /// </summary>
      /// <param name="node">
      /// The node to query
      /// </param>
      /// <returns>
      /// The enumeration of entries
      /// </returns>
      public IEnumerable<Entry> ListNodeEntries (Node node)
      {
         return Enumerate(
            "SELECT " +
               "ID, " +
               "SessionID, " +
               "BlobID, " +
               "State, " +
               "Offset, " +
               "Length, " +
               "Crc32 " +
            "FROM Entry " +
            "WHERE NodeID = @p0;",
            new Object[] { node.ID },
            reader => new Entry()
            {
               ID = Convert.ToInt32(reader[0]),
               Session = FetchSession(Convert.ToInt32(reader[1])),
               Node = node,
               Blob = (!reader.IsDBNull(2)) ? FetchBlob(Convert.ToInt32(reader[2])) : null,
               State = (EntryState)Convert.ToInt32(reader[3]),
               Offset = Convert.ToInt64(reader[4]),
               Length = Convert.ToInt64(reader[5]),
               Crc32 = BitConverter.ToUInt32((Byte[])reader[6], 0)
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
               "NodeID, " +
               "BlobID, " +
               "State, " +
               "Offset, " +
               "Length, " +
               "Crc32 " +
            "FROM Entry " +
            "WHERE SessionID = @p0 " +
                  "AND State = @p1 " +
            "LIMIT 1;",
            new Object[] { session.ID, EntryState.Pending },
            reader => new Entry()
            {
               ID = Convert.ToInt32(reader[0]),
               Session = session,
               Node = FetchNode(Convert.ToInt32(reader[1])),
               Blob = (!reader.IsDBNull(2)) ? FetchBlob(Convert.ToInt32(reader[2])) : null,
               State = (EntryState)Convert.ToInt32(reader[3]),
               Offset = Convert.ToInt64(reader[4]),
               Length = Convert.ToInt64(reader[5]),
               Crc32 = BitConverter.ToUInt32((Byte[])reader[6], 0)
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
               "SessionID, " +
               "NodeID, " +
               "BlobID, " +
               "State, " +
               "Offset, " +
               "Length, " +
               "Crc32 " +
            "FROM Entry " +
            "WHERE ID = @p0;",
            new Object[] { id },
            reader => new Entry()
            {
               ID = id,
               Session = FetchSession(Convert.ToInt32(reader[0])),
               Node = FetchNode(Convert.ToInt32(reader[1])),
               Blob = (!reader.IsDBNull(2)) ? FetchBlob(Convert.ToInt32(reader[2])) : null,
               State = (EntryState)Convert.ToInt32(reader[3]),
               Offset = Convert.ToInt64(reader[4]),
               Length = Convert.ToInt64(reader[5]),
               Crc32 = BitConverter.ToUInt32((Byte[])reader[6], 0)
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
               "SessionID, " +
               "NodeID, " +
               "BlobID, " +
               "State, " +
               "Offset, " +
               "Length, " +
               "Crc32) " +
            "VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6);",
            entry.Session.ID,
            entry.Node.ID,
            (entry.Blob != null) ? (Object)entry.Blob.ID : null,
            Convert.ToInt32(entry.State),
            entry.Offset,
            entry.Length,
            BitConverter.GetBytes(entry.Crc32)
         );
         entry.ID = GetLastRowID();
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
               "SessionID = @p1, " +
               "NodeID = @p2, " +
               "BlobID = @p3, " +
               "State = @p4, " +
               "Offset = @p5, " +
               "Length = @p6, " +
               "Crc32 = @p7 " +
            "WHERE ID = @p0;",
            entry.ID,
            entry.Session.ID,
            entry.Node.ID,
            (entry.Blob != null) ? (Object)entry.Blob.ID : null,
            Convert.ToInt32(entry.State),
            entry.Offset,
            entry.Length,
            BitConverter.GetBytes(entry.Crc32)
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
