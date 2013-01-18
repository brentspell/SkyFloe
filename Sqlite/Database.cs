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
   public class Database : IDisposable
   {
      private String path;
      private DbConnection connection;
      private Transaction transaction;

      public Database (String path)
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

      protected static void Create (String path, String resource)
      {
         SqliteConnection.CreateFile(path);
         try
         {
            // load the SQL script
            var asm = Assembly.GetExecutingAssembly();
            var ddl = "";
            using (var stream = asm.GetManifestResourceStream(resource))
            using (var reader = new StreamReader(stream))
               ddl = reader.ReadToEnd();
            // execute the statements in the script
            using (var db = new Database(path))
               foreach (var stmt in ddl.Split(';'))
                  db.Execute(stmt);
         }
         catch
         {
            try { File.Delete(path); }
            catch { }
            throw;
         }
      }
      public static void Delete (String path)
      {
         File.Delete(path);
         File.Delete(String.Format("{0}-journal", path));
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
      protected IDbCommand CreateCommand (String command, params Object[] parameters)
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
      protected void Execute (String command, params Object[] parameters)
      {
         using (var dbcmd = CreateCommand(command, parameters))
            dbcmd.ExecuteNonQuery();
      }
      protected Object ExecuteScalar (String command, params Object[] parameters)
      {
         using (var dbcmd = CreateCommand(command, parameters))
            return dbcmd.ExecuteScalar();
      }
      protected IDataReader ExecuteReader (String command, params Object[] parameters)
      {
         using (var dbcmd = CreateCommand(command, parameters))
            return dbcmd.ExecuteReader();
      }
      protected Int32 QueryRowID ()
      {
         return Convert.ToInt32(ExecuteScalar("SELECT last_insert_rowid();"));
      }
   }
}
