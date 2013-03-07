//===========================================================================
// MODULE:  Connection.cs
// PURPOSE: store connection
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
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
// Project References
using Strings = SkyFloe.Resources.Strings;

namespace SkyFloe
{
   /// <summary>
   /// The connection
   /// </summary>
   /// <remarks>
   /// This class encapsulates a connection to a SkyFloe backup store. The
   /// store is identified and initialized using a connection string
   /// mechanism, to avoid coupling clients directly to store 
   /// implementations. The connection class also supports operations for
   /// browsing archives and their contents in a read-only manner.
   /// </remarks>
   public class Connection : IDisposable
   {
      private static readonly Dictionary<String, String> knownStores =
         new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase)
         {
            { "FileSystem", "SkyFloe.FileSystemStore,SkyFloe.FileStore" },
            { "AwsGlacier", "SkyFloe.Aws.GlacierStore,SkyFloe.AwsStore" }
         };
      private ConnectionString connectionString;
      private Store.IStore store;

      /// <summary>
      /// Initializes a new connection instance
      /// </summary>
      public Connection ()
      {
      }
      /// <summary>
      /// Opens a new connection
      /// </summary>
      /// <param name="connect">
      /// The connection string to open
      /// </param>
      public Connection (String connect)
      {
         Open(connect);
      }
      /// <summary>
      /// Releases the connection
      /// </summary>
      public void Dispose ()
      {
         Close();
      }

      /// <summary>
      /// The current connection string, or null if not open
      /// </summary>
      public ConnectionString ConnectionString
      {
         get { return this.connectionString; }
      }
      /// <summary>
      /// The display caption for the store
      /// </summary>
      public String Caption
      {
         get {  return this.store != null ? this.store.Caption : String.Empty; }
      }
      /// <summary>
      /// The internal store implementation
      /// </summary>
      internal Store.IStore Store
      {
         get { return this.store; }
      }

      /// <summary>
      /// Connects to a store
      /// </summary>
      /// <param name="connect">
      /// The store connection string
      /// </param>
      public void Open (String connect)
      {
         if (this.store != null)
            throw new ConnectionException(Strings.ConnectionAlreadyConnected);
         Store.IStore store = null;
         try
         {
            // parse connection string parameters
            var connectionString = ConnectionString.Parse(connect);
            // determine the store name
            var storeName = connectionString.Store;
            var knownStore = (String)null;
            if (knownStores.TryGetValue(storeName, out knownStore))
               storeName = knownStore;
            // attempt to load the store type
            var storeType = Type.GetType(storeName, true);
            store = (Store.IStore)Activator.CreateInstance(storeType);
            // bind the store properties and connect
            connectionString.Bind(store);
            // connect to the store
            store.Open();
            this.connectionString = connectionString;
            this.store = store;
         }
         catch (Exception e)
         {
            if (store != null)
               store.Dispose();
            throw new ConnectionException(e);
         }
      }
      /// <summary>
      /// Closes the open connection
      /// </summary>
      public void Close ()
      {
         if (this.store != null)
            this.store.Dispose();
         this.store = null;
         this.connectionString = null;
      }
      /// <summary>
      /// Retrieves the list of archives within the store
      /// </summary>
      /// <returns>
      /// The archive name list
      /// </returns>
      public IEnumerable<String> ListArchives ()
      {
         if (this.store == null)
            throw new ConnectionException(Strings.ConnectionNotConnected);
         try
         {
            return this.store.ListArchives();
         }
         catch (Exception e)
         {
            throw new ConnectionException(Strings.ConnectionNotConnected, e);
         }
      }
      /// <summary>
      /// Connects to an archive in the store
      /// </summary>
      /// <param name="name">
      /// The name of the archive to connect
      /// </param>
      /// <returns>
      /// The opened archive
      /// </returns>
      public Archive OpenArchive (String name)
      {
         if (this.store == null)
            throw new ConnectionException(Strings.ConnectionNotConnected);
         try
         {
            return new Archive(this.store.OpenArchive(name));
         }
         catch (Exception e)
         {
            throw new ConnectionException(Strings.ConnectionNotConnected, e);
         }
      }

      /// <summary>
      /// Connected archive
      /// </summary>
      /// <remarks>
      /// This class wraps an underlying archive implementation for
      /// read-only browsing purposes.
      /// </remarks>
      public class Archive : IDisposable
      {
         private Store.IArchive archive;

         /// <summary>
         /// Initializes a new archive instance
         /// </summary>
         /// <param name="archive">
         /// The archive implementation to attach
         /// </param>
         internal Archive (Store.IArchive archive)
         {
            this.archive = archive;
         }
         /// <summary>
         /// Releases the resources associated with the archive
         /// </summary>
         public void Dispose ()
         {
            if (this.archive != null)
               this.archive.Dispose();
            this.archive = null;
         }
         /// <summary>
         /// The archive name
         /// </summary>
         public String Name
         { 
            get { return this.archive.Name; }
         }
         /// <summary>
         /// The list if storage blobs in the archive
         /// </summary>
         public IEnumerable<Backup.Blob> Blobs
         {
            get { return this.archive.BackupIndex.ListBlobs(); }
         }
         /// <summary>
         /// The list of backup sessions in the archive
         /// </summary>
         public IEnumerable<Backup.Session> Sessions
         {
            get { return this.archive.BackupIndex.ListSessions(); }
         }
         /// <summary>
         /// The list of root nodes in the archive
         /// </summary>
         public IEnumerable<Backup.Node> Roots
         {
            get { return this.archive.BackupIndex.ListNodes(null); }
         }
         /// <summary>
         /// The list of restore sessions in the archive
         /// </summary>
         public IEnumerable<Restore.Session> Restores
         {
            get { return this.archive.RestoreIndex.ListSessions(); }
         }
         /// <summary>
         /// Enumerates all nodes in the archive
         /// </summary>
         /// <returns>
         /// An enumeration of all backup nodes
         /// </returns>
         public IEnumerable<Backup.Node> GetAllNodes ()
         {
            return GetSubtrees(this.Roots);
         }
         /// <summary>
         /// Searches for a backup node by its source path
         /// </summary>
         /// <param name="path">
         /// The original source path of the backup node
         /// </param>
         /// <returns>
         /// The requested backup node if found
         /// Null otherwise
         /// </returns>
         public Backup.Node LookupNode (IO.Path path)
         {
            if (path.IsEmpty)
               throw new ArgumentException("path");
            var node = this.Roots.FirstOrDefault(
               r => new IO.Path(r.Name).IsAncestor(path)
            );
            if (node != null)
            {
               var rootPath = (IO.Path)node.Name;
               foreach (var pathElem in path.Skip(rootPath.Count()))
               {
                  node = GetChildren(node).FirstOrDefault(n => n.NameEquals(pathElem));
                  if (node == null)
                     break;
               }
            }
            return node;
         }
         /// <summary>
         /// Enumerates the child nodes of a node in the archive
         /// </summary>
         /// <param name="parent">
         /// The parent node to query
         /// </param>
         /// <returns>
         /// An enumeration of the requested child nodes
         /// </returns>
         public IEnumerable<Backup.Node> GetChildren (Backup.Node parent)
         {
            if (parent == null)
               throw new ArgumentNullException("parent");
            return this.archive.BackupIndex.ListNodes(parent);
         }
         /// <summary>
         /// Enumerates the descendant nodes of a node in the archive
         /// </summary>
         /// <param name="parent">
         /// The parent node to query
         /// </param>
         /// <returns>
         /// An enumeration of the requested descendant nodes
         /// </returns>
         public IEnumerable<Backup.Node> GetDescendants (Backup.Node parent)
         {
            return this.archive.BackupIndex.ListNodes(parent).SelectMany(GetSubtree);
         }
         /// <summary>
         /// Enumerates a subtree rooted at a given node
         /// </summary>
         /// <param name="node">
         /// The node to query
         /// </param>
         /// <returns>
         /// An enumeration of the node and its descendants
         /// </returns>
         public IEnumerable<Backup.Node> GetSubtree (Backup.Node node)
         {
            return new[] { node }.Concat(GetDescendants(node));
         }
         /// <summary>
         /// Enumerates the subtrees rooted at a set of given nodes
         /// </summary>
         /// <param name="nodes">
         /// The nodes to query
         /// </param>
         /// <returns>
         /// An enumeration of the nodes and their descendants
         /// </returns>
         public IEnumerable<Backup.Node> GetSubtrees (IEnumerable<Backup.Node> nodes)
         {
            return nodes.SelectMany(GetSubtree);
         }
         /// <summary>
         /// Enumerates the backup entries associated with a node
         /// </summary>
         /// <param name="node">
         /// The node to query
         /// </param>
         /// <returns>
         /// An enumeration of the backup entries for the node
         /// </returns>
         public IEnumerable<Backup.Entry> GetEntries (Backup.Node node)
         {
            return this.archive.BackupIndex.ListNodeEntries(node);
         }
      }
   }
}
