//===========================================================================
// MODULE:  Database.cs
// PURPOSE: Sqlite relation-based ORM base class
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
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Transactions;
using Mono.Data.Sqlite;
// Project References

namespace SkyFloe.Sqlite
{
   /// <summary>
   /// Database wrapper base class
   /// </summary>
   /// <remarks>
   /// This class encapsulates a connection to an embedded Sqlite database. 
   /// It provides helper methods for creating and accessing the database 
   /// file.
   /// </remarks>
   public class Database : IDisposable
   {
      private IO.Path path;
      private DbConnection connection;
      private Transaction transaction;

      /// <summary>
      /// Initializes a new database instance
      /// </summary>
      /// <param name="path">
      /// The path to the database file to open
      /// </param>
      public Database (IO.Path path)
      {
         this.path = path;
         this.connection = new SqliteConnection();
         this.connection.ConnectionString = String.Format("URI=file:{0}", path);
         this.connection.Open();
         Execute("PRAGMA foreign_keys = ON;");
         Execute("PRAGMA journal_mode = PERSIST;");
      }
      /// <summary>
      /// Releases the database connection
      /// </summary>
      public void Dispose ()
      {
         if (this.connection != null)
            this.connection.Close();
         this.connection = null;
      }

      /// <summary>
      /// Creates a new Sqlite database
      /// </summary>
      /// <param name="path">
      /// The path to the database file to create
      /// </param>
      /// <param name="resource">
      /// The name of the embedded resource within the caller's assembly
      /// containing the SQL script to execute
      /// </param>
      protected static void Create (IO.Path path, String resource)
      {
         try
         {
            // load the SQL script from the derived class assembly
            var asm = Assembly.GetCallingAssembly();
            var ddl = (String)null;
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
            try { IO.FileSystem.Delete(path); }
            catch { }
            throw;
         }
      }
      /// <summary>
      /// Deletes a Sqlite database
      /// </summary>
      /// <param name="path">
      /// The path to the database file to delete
      /// </param>
      public static void Delete (IO.Path path)
      {
         IO.FileSystem.Delete(path);
         IO.FileSystem.Delete((IO.Path)String.Format("{0}-journal", path));
      }
      /// <summary>
      /// Opens a stream used to serialize the database file
      /// </summary>
      /// <returns>
      /// The database stream
      /// The caller is responsible for disposing it
      /// </returns>
      public Stream Serialize ()
      {
         Execute("VACUUM;");
         return IO.FileSystem.Open(this.path, FileShare.ReadWrite);
      }
      /// <summary>
      /// Creates a new database command object connected to the
      /// attached database file
      /// </summary>
      /// <param name="command">
      /// The SQL command to bind to the object
      /// </param>
      /// <param name="parameters">
      /// The command parameters to bind to the object
      /// </param>
      /// <returns>
      /// The new database command
      /// </returns>
      protected IDbCommand CreateCommand (
         String command, 
         params Object[] parameters)
      {
         if (this.connection == null)
            throw new ObjectDisposedException("SqliteIndex");
         // enlist in the ambient transaction
         if (Transaction.Current != this.transaction)
         {
            this.transaction = Transaction.Current;
            if (this.transaction != null)
               ((DbConnection)this.connection).EnlistTransaction(this.transaction);
         }
         // create the command object and bind the parameter values
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
      /// <summary>
      /// Executes a database command
      /// </summary>
      /// <param name="command">
      /// The SQL command to execute
      /// </param>
      /// <param name="parameters">
      /// The parameters to pass to the command
      /// </param>
      protected void Execute (
         String command, 
         params Object[] parameters)
      {
         using (var dbcmd = CreateCommand(command, parameters))
            dbcmd.ExecuteNonQuery();
      }
      /// <summary>
      /// Executes a single valued command
      /// </summary>
      /// <param name="command">
      /// The SQL command to execute
      /// </param>
      /// <param name="parameters">
      /// The parameters to pass to the command
      /// </param>
      /// <returns>
      /// Returns the first row and first column of the result set
      /// Throws if there is not exactly one row and one column
      /// </returns>
      protected Object ExecuteScalar (
         String command, 
         params Object[] parameters)
      {
         using (var dbcmd = CreateCommand(command, parameters))
            return dbcmd.ExecuteScalar();
      }
      /// <summary>
      /// Executes a query command
      /// </summary>
      /// <param name="command">
      /// The SQL command to execute
      /// </param>
      /// <param name="parameters">
      /// The parameters to pass to the command
      /// </param>
      /// <returns>
      /// A data reader that can be used to retrieve the result set
      /// </returns>
      protected IDataReader ExecuteReader (
         String command, 
         params Object[] parameters)
      {
         using (var dbcmd = CreateCommand(command, parameters))
            return dbcmd.ExecuteReader();
      }
      /// <summary>
      /// Fetches a single record from the database, useful
      /// for fetching singleton records
      /// </summary>
      /// <typeparam name="T">
      /// The record type
      /// </typeparam>
      /// <param name="command">
      /// The SQL fetch command to execute
      /// </param>
      /// <param name="factory">
      /// A delegate used to convert the row to a record
      /// </param>
      /// <returns>
      /// The fetched row if found
      /// Null otherwise
      /// </returns>
      protected T Fetch<T> (
         String command, 
         Func<IDataReader, T> factory)
      {
         using (var reader = ExecuteReader(command))
            if (reader.Read())
               return factory(reader);
         return default(T);
      }
      /// <summary>
      /// Fetches a single record from the database
      /// </summary>
      /// <typeparam name="T">
      /// The record type
      /// </typeparam>
      /// <param name="command">
      /// The SQL fetch command to execute
      /// </param>
      /// <param name="parameters">
      /// The parameters to pass to the command
      /// </param>
      /// <param name="factory">
      /// A delegate used to convert the row to a record
      /// </param>
      /// <returns>
      /// The fetched row if found
      /// Null otherwise
      /// </returns>
      protected T Fetch<T> (
         String command, 
         Object[] parameters, 
         Func<IDataReader, T> factory)
      {
         using (var reader = ExecuteReader(command, parameters))
            if (reader.Read())
               return factory(reader);
         return default(T);
      }
      /// <summary>
      /// Reads a list of records from the database
      /// </summary>
      /// <typeparam name="T">
      /// The record type
      /// </typeparam>
      /// <param name="command">
      /// The SQL query command to execute
      /// </param>
      /// <param name="factory">
      /// A delegate used to convert each row to a record
      /// </param>
      /// <returns>
      /// An enumeration of the records matching the query command
      /// </returns>
      protected IEnumerable<T> Enumerate<T> (
         String command, 
         Func<IDataReader, T> factory)
      {
         using (var reader = ExecuteReader(command))
            while (reader.Read())
               yield return factory(reader);
      }
      /// <summary>
      /// Reads a list of records from the database
      /// </summary>
      /// <typeparam name="T">
      /// The record type
      /// </typeparam>
      /// <param name="command">
      /// The SQL query command to execute
      /// </param>
      /// <param name="parameters">
      /// The parameters to pass to the command
      /// </param>
      /// <param name="factory">
      /// A delegate used to convert each row to a record
      /// </param>
      /// <returns>
      /// An enumeration of the records matching the query command
      /// </returns>
      protected IEnumerable<T> Enumerate<T> (
         String command, 
         Object[] parameters, 
         Func<IDataReader, T> factory)
      {
         using (var reader = ExecuteReader(command, parameters))
            while (reader.Read())
               yield return factory(reader);
      }
      /// <summary>
      /// Fetches the last generated Sqlite rowid value on this connection
      /// </summary>
      /// <returns>
      /// The last generated rowid value
      /// </returns>
      protected Int32 GetLastRowID ()
      {
         return Convert.ToInt32(ExecuteScalar("SELECT last_insert_rowid();"));
      }
   }
}
