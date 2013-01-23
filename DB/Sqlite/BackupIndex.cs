using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

using SkyFloe.Backup;

namespace SkyFloe.Sqlite
{
   public class BackupIndex : Database, Store.IBackupIndex
   {
      private const Int32 CurrentVersion = 1;

      private BackupIndex (String path) : base(path)
      {
      }

      public static BackupIndex Create (String path, Header header)
      {
         Database.Create(path, "SkyFloe.Sqlite.Resources.BackupIndex.sql");
         BackupIndex index = null;
         try
         {
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
      public static BackupIndex Open (String path)
      {
         BackupIndex index = new BackupIndex(path);
         if (index.FetchHeader().Version != CurrentVersion)
            throw new InvalidOperationException("TODO: invalid version number");
         return index;
      }

      #region IIndex Implementation
      public Header FetchHeader ()
      {
         using (IDataReader reader = ExecuteReader("SELECT Version, CryptoIterations, ArchiveSalt, PasswordHash, PasswordSalt FROM Header;"))
            if (reader.Read())
               return new Header()
               {
                  Version = Convert.ToInt32(reader[0]),
                  CryptoIterations = Convert.ToInt32(reader[1]),
                  ArchiveSalt = (Byte[])reader[2],
                  PasswordHash = (Byte[])reader[3],
                  PasswordSalt = (Byte[])reader[4]
               };
         return null;
      }
      public IEnumerable<Blob> ListBlobs ()
      {
         using (IDataReader reader = ExecuteReader("SELECT ID, Name, Length, Created, Updated FROM Blob;"))
            while (reader.Read())
               yield return new Blob()
               {
                  ID = Convert.ToInt32(reader[0]),
                  Name = Convert.ToString(reader[1]),
                  Length = Convert.ToInt64(reader[2]),
                  Created = DateTime.SpecifyKind(Convert.ToDateTime(reader[3]), DateTimeKind.Utc),
                  Updated = DateTime.SpecifyKind(Convert.ToDateTime(reader[4]), DateTimeKind.Utc),
               };
      }
      public Blob LookupBlob (String name)
      {
         using (IDataReader reader = ExecuteReader("SELECT ID, Length, Created, Updated FROM Blob WHERE Name = @p0;", name))
            if (reader.Read())
               return new Blob()
               {
                  ID = Convert.ToInt32(reader[0]),
                  Name = name,
                  Length = Convert.ToInt64(reader[1]),
                  Created = DateTime.SpecifyKind(Convert.ToDateTime(reader[2]), DateTimeKind.Utc),
                  Updated = DateTime.SpecifyKind(Convert.ToDateTime(reader[3]), DateTimeKind.Utc),
               };
         return null;
      }
      public Blob FetchBlob (Int32 id)
      {
         using (IDataReader reader = ExecuteReader("SELECT Name, Length, Created, Updated FROM Blob WHERE ID = @p0;", id))
            if (reader.Read())
               return new Blob()
               {
                  ID = id,
                  Name = Convert.ToString(reader[0]),
                  Length = Convert.ToInt64(reader[1]),
                  Created = DateTime.SpecifyKind(Convert.ToDateTime(reader[2]), DateTimeKind.Utc),
                  Updated = DateTime.SpecifyKind(Convert.ToDateTime(reader[3]), DateTimeKind.Utc),
               };
         return null;
      }
      public Blob InsertBlob (Blob blob)
      {
         Execute(
            "INSERT INTO Blob (Name, Length, Created, Updated) VALUES (@p0, @p1, @p2, @p2);",
            blob.Name,
            blob.Length,
            blob.Updated = blob.Created = DateTime.UtcNow
         );
         blob.ID = QueryRowID();
         return blob;
      }
      public Blob UpdateBlob (Blob blob)
      {
         Execute(
            "UPDATE Blob SET Name = @p1, Length = @p2, Updated = @p3 WHERE ID = @p0;",
            blob.ID,
            blob.Name,
            blob.Length,
            blob.Updated = DateTime.UtcNow
         );
         return blob;
      }
      public void DeleteBlob (Blob blob)
      {
         Execute(
            "DELETE FROM Blob WHERE ID = @p0;",
            blob.ID
         );
      }
      #endregion

      #region Session Operations
      public IEnumerable<Session> ListSessions ()
      {
         using (IDataReader reader = ExecuteReader("SELECT ID, State, Created, EstimatedLength, ActualLength FROM Session;"))
            while (reader.Read())
               yield return new Session()
               {
                  ID = Convert.ToInt32(reader[0]),
                  State = (SessionState)Convert.ToInt32(reader[1]),
                  Created = DateTime.SpecifyKind(Convert.ToDateTime(reader[2]), DateTimeKind.Utc),
                  EstimatedLength = Convert.ToInt64(reader[3]),
                  ActualLength = Convert.ToInt64(reader[4])
               };
      }
      public Session FetchSession (Int32 id)
      {
         using (IDataReader reader = ExecuteReader("SELECT State, Created, EstimatedLength, ActualLength FROM Session WHERE ID = @p0;", id))
            if (reader.Read())
               return new Session()
               {
                  ID = id,
                  State = (SessionState)Convert.ToInt32(reader[0]),
                  Created = DateTime.SpecifyKind(Convert.ToDateTime(reader[1]), DateTimeKind.Utc),
                  EstimatedLength = Convert.ToInt64(reader[2]),
                  ActualLength = Convert.ToInt64(reader[3])
               };
         return null;
      }
      public Session InsertSession (Session session)
      {
         Execute(
            "INSERT INTO Session (State, Created, EstimatedLength, ActualLength) VALUES (@p0, @p1, @p2, @p3);",
            session.State,
            session.Created = DateTime.UtcNow,
            session.EstimatedLength,
            session.ActualLength
         );
         session.ID = QueryRowID();
         return session;
      }
      public Session UpdateSession (Session session)
      {
         Execute(
            "UPDATE Session SET State = @p1, EstimatedLength = @p2, ActualLength = @p3 WHERE ID = @p0;",
            session.ID,
            session.State,
            session.EstimatedLength,
            session.ActualLength
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

      #region Node Operations
      public IEnumerable<Node> ListNodes (Node parent = null)
      {
         using (IDataReader reader = (parent == null) ?
               ExecuteReader(
                  "SELECT ID, Type, Name FROM Node WHERE ParentID IS NULL;"
               ) :
               ExecuteReader(
                  "SELECT ID, Type, Name FROM Node WHERE ParentID = @p0;",
                  parent.ID
               )
            )
            while (reader.Read())
               yield return new Node()
               {
                  ID = Convert.ToInt32(reader[0]),
                  Parent = parent,
                  Type = (NodeType)Convert.ToInt32(reader[1]),
                  Name = Convert.ToString(reader[2])
               };
      }
      public Node FetchNode (Int32 id)
      {
         using (IDataReader reader = ExecuteReader("SELECT ParentID, Type, Name FROM Node WHERE ID = @p0;", id))
            if (reader.Read())
               return new Node()
               {
                  ID = id,
                  Parent = (!reader.IsDBNull(0)) ? FetchNode(Convert.ToInt32(reader[0])) : null,
                  Type = (NodeType)Convert.ToInt32(reader[1]),
                  Name = Convert.ToString(reader[2])
               };
         return null;
      }
      public Node InsertNode (Node node)
      {
         Execute(
            "INSERT INTO Node (ParentID, Type, Name) VALUES (@p0, @p1, @p2);",
            (node.Parent != null) ? (Object)node.Parent.ID : null,
            Convert.ToInt32(node.Type),
            node.Name
         );
         node.ID = QueryRowID();
         return node;
      }
      public Node UpdateNode (Node node)
      {
         Execute(
            "UPDATE Node SET ParentID = @p1, Type = @p2, Name = @p3 WHERE ID = @p0;",
            node.ID,
            (node.Parent != null) ? (Object)node.Parent.ID : null,
            Convert.ToInt32(node.Type),
            node.Name
         );
         return node;
      }
      public void DeleteNode (Node node)
      {
         Execute(
            "DELETE FROM Node WHERE ID = @p0;",
            node.ID
         );
      }
      #endregion

      #region Entry Operations
      public IEnumerable<Entry> ListNodeEntries (Node node)
      {
         using (IDataReader reader =
               ExecuteReader(
                  "SELECT ID, SessionID, BlobID, State, Offset, Length, Crc32 FROM Entry WHERE NodeID = @p0;",
                  node.ID
               )
            )
            while (reader.Read())
               yield return new Entry()
               {
                  ID = Convert.ToInt32(reader[0]),
                  Session = FetchSession(Convert.ToInt32(reader[1])),
                  Node = node,
                  Blob = (!reader.IsDBNull(2)) ? FetchBlob(Convert.ToInt32(reader[2])) : null,
                  State = (EntryState)Convert.ToInt32(reader[3]),
                  Offset = Convert.ToInt64(reader[4]),
                  Length = Convert.ToInt64(reader[5]),
                  Crc32 = BitConverter.ToUInt32((Byte[])reader[6], 0)
               };
      }
      public Entry LookupNextEntry (Session session)
      {
         using (IDataReader reader =
               ExecuteReader(
                  "SELECT ID, NodeID, BlobID, State, Offset, Length, Crc32 FROM Entry WHERE SessionID = @p0 AND State = @p1 LIMIT 1;",
                  session.ID,
                  EntryState.Pending
               )
            )
            if (reader.Read())
               return new Entry()
               {
                  ID = Convert.ToInt32(reader[0]),
                  Session = session,
                  Node = FetchNode(Convert.ToInt32(reader[1])),
                  Blob = (!reader.IsDBNull(2)) ? FetchBlob(Convert.ToInt32(reader[2])) : null,
                  State = (EntryState)Convert.ToInt32(reader[3]),
                  Offset = Convert.ToInt64(reader[4]),
                  Length = Convert.ToInt64(reader[5]),
                  Crc32 = BitConverter.ToUInt32((Byte[])reader[6], 0)
               };
         return null;
      }
      public Entry FetchEntry (Int32 id)
      {
         using (IDataReader reader = 
               ExecuteReader(
                  "SELECT SessionID, NodeID, BlobID, State, Offset, Length, Crc32 FROM Entry WHERE ID = @p0;", 
                  id
               )
            )
            if (reader.Read())
               return new Entry()
               {
                  ID = id,
                  Session = FetchSession(Convert.ToInt32(reader[0])),
                  Node = FetchNode(Convert.ToInt32(reader[1])),
                  Blob = (!reader.IsDBNull(2)) ? FetchBlob(Convert.ToInt32(reader[2])) : null,
                  State = (EntryState)Convert.ToInt32(reader[3]),
                  Offset = Convert.ToInt64(reader[4]),
                  Length = Convert.ToInt64(reader[5]),
                  Crc32 = BitConverter.ToUInt32((Byte[])reader[6], 0)
               };
         return null;
      }
      public Entry InsertEntry (Entry entry)
      {
         Execute(
            "INSERT INTO Entry (SessionID, NodeID, BlobID, State, Offset, Length, Crc32) VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6);",
            entry.Session.ID,
            entry.Node.ID,
            (entry.Blob != null) ? (Object)entry.Blob.ID : null,
            Convert.ToInt32(entry.State),
            entry.Offset,
            entry.Length,
            BitConverter.GetBytes(entry.Crc32)
         );
         entry.ID = QueryRowID();
         return entry;
      }
      public Entry UpdateEntry (Entry entry)
      {
         Execute(
            "UPDATE Entry SET SessionID = @p1, NodeID = @p2, BlobID = @p3, State = @p4, Offset = @p5, Length = @p6, Crc32 = @p7 WHERE ID = @p0;",
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
