using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Transactions;
using Mono.Data.Sqlite;

namespace SkyFloe.Sqlite
{
   public class SqliteIndex : Store.IIndex
   {
      private const Int32 CurrentVersion = 1;

      private String path;
      private DbConnection connection;
      private Transaction transaction;

      private SqliteIndex (String path)
      {
         this.path = path;
         this.connection = new SqliteConnection();
         this.connection.ConnectionString = String.Format("URI=file:{0}", path);
         this.connection.Open();
         Execute("PRAGMA foreign_keys = ON;");
         Execute("PRAGMA journal_mode = PERSIST;");
      }

      public void Dispose ()
      {
         if (this.connection != null)
            this.connection.Close();
         this.connection = null;
      }

      public static SqliteIndex Create (String path, Model.Header header)
      {
         SqliteIndex index = null;
         SqliteConnection.CreateFile(path);
         try
         {
            index = new SqliteIndex(path);
            index.CreateSchema();
            header.Version = CurrentVersion;
            index.Execute(
               "INSERT INTO Header (" + 
               "   Version, " +
               "   CryptoIterations, " +
               "   ArchiveSalt, " +
               "   PasswordHash, " + 
               "   PasswordSalt) " + 
               "VALUES (@p0, @p1, @p2, @p3, @p4);", 
               header.Version,
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
            try { File.Delete(path); } catch { }
            throw;
         }
      }
      public static SqliteIndex Open (String path)
      {
         var index = new SqliteIndex(path);
         if (index.FetchHeader().Version != CurrentVersion)
            throw new InvalidOperationException("TODO: invalid version number");
         return index;
      }
      public static void Delete (String path)
      {
         File.Delete(path);
         File.Delete(String.Format("{0}-journal", path));
      }

      private void CreateSchema ()
      {
         // load the SQL script
         var asm = Assembly.GetExecutingAssembly();
         var res = "SkyFloe.Sqlite.Resources.index.sql";
         var sql = "";
         using (var stream = asm.GetManifestResourceStream(res))
         using (var reader = new StreamReader(stream))
            sql = reader.ReadToEnd();
         // execute the statements in the script
         foreach (var stmt in sql.Split(';'))
            Execute(stmt);
      }

      #region IIndex Implementation
      public Int64 Size
      {
         get { return new FileInfo(this.path).Length; }
      }
      public Stream Serialize ()
      {
         Execute("VACUUM;");
         return new FileStream(
            this.path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite
         );
      }
      public Model.Header FetchHeader ()
      {
         using (var reader = ExecuteReader("SELECT Version, CryptoIterations, ArchiveSalt, PasswordHash, PasswordSalt FROM Header;"))
            if (reader.Read())
               return new Model.Header()
               {
                  Version = Convert.ToInt32(reader[0]),
                  CryptoIterations = Convert.ToInt32(reader[1]),
                  ArchiveSalt = (Byte[])reader[2],
                  PasswordHash = (Byte[])reader[3],
                  PasswordSalt = (Byte[])reader[4]
               };
         return null;
      }
      public IEnumerable<Model.Blob> ListBlobs ()
      {
         using (var reader = ExecuteReader("SELECT ID, Name, Length, Created, Updated FROM Blob;"))
            while (reader.Read())
               yield return new Model.Blob()
               {
                  ID = Convert.ToInt32(reader[0]),
                  Name = Convert.ToString(reader[1]),
                  Length = Convert.ToInt64(reader[2]),
                  Created = DateTime.SpecifyKind(Convert.ToDateTime(reader[3]), DateTimeKind.Utc),
                  Updated = DateTime.SpecifyKind(Convert.ToDateTime(reader[4]), DateTimeKind.Utc),
               };
      }
      public Model.Blob LookupBlob (String name)
      {
         using (var reader = ExecuteReader("SELECT ID, Length, Created, Updated FROM Blob WHERE Name = @p0;", name))
            if (reader.Read())
               return new Model.Blob()
               {
                  ID = Convert.ToInt32(reader[0]),
                  Name = name,
                  Length = Convert.ToInt64(reader[1]),
                  Created = DateTime.SpecifyKind(Convert.ToDateTime(reader[2]), DateTimeKind.Utc),
                  Updated = DateTime.SpecifyKind(Convert.ToDateTime(reader[3]), DateTimeKind.Utc),
               };
         return null;
      }
      public Model.Blob FetchBlob (Int32 id)
      {
         using (var reader = ExecuteReader("SELECT Name, Length, Created, Updated FROM Blob WHERE ID = @p0;", id))
            if (reader.Read())
               return new Model.Blob()
               {
                  ID = id,
                  Name = Convert.ToString(reader[0]),
                  Length = Convert.ToInt64(reader[1]),
                  Created = DateTime.SpecifyKind(Convert.ToDateTime(reader[2]), DateTimeKind.Utc),
                  Updated = DateTime.SpecifyKind(Convert.ToDateTime(reader[3]), DateTimeKind.Utc),
               };
         return null;
      }
      public Model.Blob InsertBlob (Model.Blob blob)
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
      public Model.Blob UpdateBlob (Model.Blob blob)
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
      public void DeleteBlob (Model.Blob blob)
      {
         Execute(
            "DELETE FROM Blob WHERE ID = @p0;",
            blob.ID
         );
      }
      #endregion

      #region Session Operations
      public IEnumerable<Model.Session> ListSessions ()
      {
         using (var reader = ExecuteReader("SELECT ID, State, Created, EstimatedLength, ActualLength FROM Session;"))
            while (reader.Read())
               yield return new Model.Session()
               {
                  ID = Convert.ToInt32(reader[0]),
                  State = (Model.SessionState)Convert.ToInt32(reader[1]),
                  Created = DateTime.SpecifyKind(Convert.ToDateTime(reader[2]), DateTimeKind.Utc),
                  EstimatedLength = Convert.ToInt64(reader[3]),
                  ActualLength = Convert.ToInt64(reader[4])
               };
      }
      public Model.Session FetchSession (Int32 id)
      {
         using (var reader = ExecuteReader("SELECT State, Created, EstimatedLength, ActualLength FROM Session WHERE ID = @p0;", id))
            if (reader.Read())
               return new Model.Session()
               {
                  ID = id,
                  State = (Model.SessionState)Convert.ToInt32(reader[0]),
                  Created = DateTime.SpecifyKind(Convert.ToDateTime(reader[1]), DateTimeKind.Utc),
                  EstimatedLength = Convert.ToInt64(reader[2]),
                  ActualLength = Convert.ToInt64(reader[3])
               };
         return null;
      }
      public Model.Session InsertSession (Model.Session session)
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
      public Model.Session UpdateSession (Model.Session session)
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
      public void DeleteSession (Model.Session session)
      {
         Execute(
            "DELETE FROM Session WHERE ID = @p0;",
            session.ID
         );
      }
      #endregion

      #region Node Operations
      public IEnumerable<Model.Node> ListNodes (Model.Node parent = null)
      {
         using (var reader = (parent == null) ?
               ExecuteReader(
                  "SELECT ID, Type, Name FROM Node WHERE ParentID IS NULL;"
               ) :
               ExecuteReader(
                  "SELECT ID, Type, Name FROM Node WHERE ParentID = @p0;",
                  parent.ID
               )
            )
            while (reader.Read())
               yield return new Model.Node()
               {
                  ID = Convert.ToInt32(reader[0]),
                  Parent = parent,
                  Type = (Model.NodeType)Convert.ToInt32(reader[1]),
                  Name = Convert.ToString(reader[2])
               };
      }
      public Model.Node FetchNode (Int32 id)
      {
         using (var reader = ExecuteReader("SELECT ParentID, Type, Name FROM Node WHERE ID = @p0;", id))
            if (reader.Read())
               return new Model.Node()
               {
                  ID = id,
                  Parent = (!reader.IsDBNull(0)) ? FetchNode(Convert.ToInt32(reader[0])) : null,
                  Type = (Model.NodeType)Convert.ToInt32(reader[1]),
                  Name = Convert.ToString(reader[2])
               };
         return null;
      }
      public Model.Node InsertNode (Model.Node node)
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
      public Model.Node UpdateNode (Model.Node node)
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
      public void DeleteNode (Model.Node node)
      {
         Execute(
            "DELETE FROM Node WHERE ID = @p0;",
            node.ID
         );
      }
      #endregion

      #region Entry Operations
      public IEnumerable<Model.Entry> ListNodeEntries (Model.Node node)
      {
         using (var reader =
               ExecuteReader(
                  "SELECT ID, SessionID, BlobID, State, Offset, Length, Crc32 FROM Entry WHERE NodeID = @p0;",
                  node.ID
               )
            )
            while (reader.Read())
               yield return new Model.Entry()
               {
                  ID = Convert.ToInt32(reader[0]),
                  Session = FetchSession(Convert.ToInt32(reader[1])),
                  Node = node,
                  Blob = (!reader.IsDBNull(2)) ? FetchBlob(Convert.ToInt32(reader[2])) : null,
                  State = (Model.EntryState)Convert.ToInt32(reader[3]),
                  Offset = Convert.ToInt64(reader[4]),
                  Length = Convert.ToInt64(reader[5]),
                  Crc32 = BitConverter.ToUInt32((Byte[])reader[6], 0)
               };
      }
      public Model.Entry LookupNextPendingEntry (Model.Session session)
      {
         using (var reader =
               ExecuteReader(
                  "SELECT ID, NodeID, BlobID, State, Offset, Length, Crc32 FROM Entry WHERE SessionID = @p0 AND State = @p1 LIMIT 1;",
                  session.ID,
                  Model.EntryState.Pending
               )
            )
            if (reader.Read())
               return new Model.Entry()
               {
                  ID = Convert.ToInt32(reader[0]),
                  Session = session,
                  Node = FetchNode(Convert.ToInt32(reader[1])),
                  Blob = (!reader.IsDBNull(2)) ? FetchBlob(Convert.ToInt32(reader[2])) : null,
                  State = (Model.EntryState)Convert.ToInt32(reader[3]),
                  Offset = Convert.ToInt64(reader[4]),
                  Length = Convert.ToInt64(reader[5]),
                  Crc32 = BitConverter.ToUInt32((Byte[])reader[6], 0)
               };
         return null;
      }
      public Model.Entry FetchEntry (Int32 id)
      {
         using (var reader = 
               ExecuteReader(
                  "SELECT SessionID, NodeID, BlobID, State, Offset, Length, Crc32 FROM Entry WHERE ID = @p0;", 
                  id
               )
            )
            if (reader.Read())
               return new Model.Entry()
               {
                  ID = id,
                  Session = FetchSession(Convert.ToInt32(reader[0])),
                  Node = FetchNode(Convert.ToInt32(reader[1])),
                  Blob = (!reader.IsDBNull(2)) ? FetchBlob(Convert.ToInt32(reader[2])) : null,
                  State = (Model.EntryState)Convert.ToInt32(reader[3]),
                  Offset = Convert.ToInt64(reader[4]),
                  Length = Convert.ToInt64(reader[5]),
                  Crc32 = BitConverter.ToUInt32((Byte[])reader[6], 0)
               };
         return null;
      }
      public Model.Entry InsertEntry (Model.Entry entry)
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
      public Model.Entry UpdateEntry (Model.Entry entry)
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
      public void DeleteEntry (Model.Entry entry)
      {
         Execute(
            "DELETE FROM Entry WHERE ID = @p0;",
            entry.ID
         );
      }
      #endregion

      private IDbCommand CreateCommand (String command, params Object[] parameters)
      {
         if (this.connection == null)
            throw new ObjectDisposedException("SqliteIndex");
         if (Transaction.Current != this.transaction)
         {
            this.transaction = Transaction.Current;
            if (this.transaction != null)
               ((DbConnection)this.connection).EnlistTransaction(this.transaction);
         }
         var dbcmd = this.connection.CreateCommand();
         dbcmd.CommandText = command;
         var paramIdx = 0;
         foreach (var param in parameters)
         {
            var dbparam = dbcmd.CreateParameter();
            dbparam.ParameterName = String.Format("@p{0}", paramIdx++);
            dbparam.Value = param ?? DBNull.Value;
            dbcmd.Parameters.Add(dbparam);
         }
         return dbcmd;
      }
      private void Execute (String command, params Object[] parameters)
      {
         using (var dbcmd = CreateCommand(command, parameters))
            dbcmd.ExecuteNonQuery();
      }
      private Object ExecuteScalar (String command, params Object[] parameters)
      {
         using (var dbcmd = CreateCommand(command, parameters))
            return dbcmd.ExecuteScalar();
      }
      private IDataReader ExecuteReader (String command, params Object[] parameters)
      {
         using (var dbcmd = CreateCommand(command, parameters))
            return dbcmd.ExecuteReader();
      }
      private Int32 QueryRowID ()
      {
         return Convert.ToInt32(ExecuteScalar("SELECT last_insert_rowid();"));
      }
   }
}
