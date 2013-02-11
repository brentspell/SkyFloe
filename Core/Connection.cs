using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace SkyFloe
{
   public class Connection : IDisposable
   {
      private String connectionString;
      private Store.IStore store;
      private static Dictionary<String, String> knownStores =
         new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase)
         {
            { "FileSystem", "SkyFloe.FileSystemStore,SkyFloe.FileStore" },
            { "AwsGlacier", "SkyFloe.Aws.GlacierStore,SkyFloe.AwsStore" }
         };

      public Connection ()
      {
      }
      public Connection (String connect)
      {
         Open(connect);
      }
      public void Dispose ()
      {
         Close();
      }

      public static Dictionary<String, String> Parse (String connect)
      {
         Dictionary<String, String> paramMap =
            new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
         foreach (String param in connect.Split(';'))
         {
            Int32 sepIdx = param.IndexOf('=');
            if (sepIdx != -1)
               paramMap[param.Substring(0, sepIdx).Trim()] =
                  param.Substring(sepIdx + 1).Trim();
         }
         return paramMap;
      }

      public static void Bind (Dictionary<String, String> paramMap, Object props)
      {
         foreach (KeyValuePair<String, String> param in paramMap
            .Where(p => String.Compare(p.Key, "Store", true) != 0)
         )
         {
            PropertyDescriptor prop = TypeDescriptor.GetProperties(props)
               .Cast<PropertyDescriptor>()
               .FirstOrDefault(p => String.Compare(p.Name, param.Key, true) == 0);
            if (prop == null)
               throw new InvalidOperationException("TODO: connection string param not found");
            try
            {
               prop.SetValue(props, prop.Converter.ConvertFrom(param.Value));
            }
            catch (Exception e)
            {
               throw new InvalidOperationException("TODO: failed to bind connection string param", e);
            }
         }
      }

      public static Object GetStoreProperties (String store)
      {
         // TODO: refactor
         String knownStore = null;
         if (knownStores.TryGetValue(store, out knownStore))
            store = knownStore;
         // load and create the store type
         return Activator.CreateInstance(Type.GetType(store, true));
      }

      public static String GetConnectionString (String store, Object props)
      {
         return String.Join(
            ";",
            new[] { String.Format("Store={0}", store) }
            .Concat(
               TypeDescriptor.GetProperties(props)
               .Cast<PropertyDescriptor>()
               .Select(
                  p => String.Format(
                     "{0}={1}", 
                     p.Name, 
                     p.Converter.ConvertTo(p.GetValue(props), typeof(String))
                  )
               )
            )
         );
      }

      public String ConnectionString
      {
         get { return this.connectionString; }
      }
      internal Store.IStore Store
      {
         get { return this.store; }
      }

      public void Open (String connect)
      {
         if (this.store != null)
            throw new InvalidOperationException("TODO: already connected");
         // parse connection string parameters
         Dictionary<String, String> paramMap = Parse(connect);
         // determine the store name
         String storeName = null;
         if (!paramMap.TryGetValue("Store", out storeName))
            throw new InvalidOperationException("TODO: store not found");
         paramMap.Remove("Store");
         String knownStore = null;
         if (knownStores.TryGetValue(storeName, out knownStore))
            storeName = knownStore;
         // attempt to load the store type
         Type storeType = Type.GetType(storeName, true);
         Store.IStore store = (Store.IStore)Activator.CreateInstance(storeType);
         // bind the store properties and connect
         Bind(paramMap, store);
         store.Open();
         this.connectionString = connect;
         this.store = store;
      }
      public void Close ()
      {
         if (this.store != null)
            this.store.Dispose();
         this.store = null;
         this.connectionString = null;
      }

      public IEnumerable<String> ListArchives ()
      {
         if (this.store == null)
            throw new InvalidOperationException("TODO: Not connected");
         return this.store.ListArchives();
      }
      public Archive OpenArchive (String name)
      {
         if (this.store == null)
            throw new InvalidOperationException("TODO: Not connected");
         return new Archive(this.store.OpenArchive(name));
      }

      public class Archive : IDisposable
      {
         private Store.IArchive archive;

         internal Archive (Store.IArchive archive)
         {
            this.archive = archive;
         }

         public void Dispose ()
         {
            if (this.archive != null)
               this.archive.Dispose();
            this.archive = null;
         }

         public String Name
         { 
            get { return this.archive.Name; }
         }
         public IEnumerable<Backup.Blob> Blobs
         {
            get { return this.archive.BackupIndex.ListBlobs(); }
         }
         public IEnumerable<Backup.Session> Backups
         {
            get { return this.archive.BackupIndex.ListSessions(); }
         }
         public IEnumerable<Backup.Node> Roots
         {
            get { return this.archive.BackupIndex.ListNodes(null); }
         }
         public IEnumerable<Backup.Node> AllNodes
         {
            get { return GetSubtrees(this.Roots); }
         }
         public IEnumerable<Restore.Session> Restores
         {
            get { return this.archive.RestoreIndex.ListSessions(); }
         }
         public IEnumerable<Backup.Node> GetChildren (Backup.Node parent)
         {
            return this.archive.BackupIndex.ListNodes(parent);
         }
         public IEnumerable<Backup.Node> GetDescendants (Backup.Node parent)
         {
            return this.archive.BackupIndex.ListNodes(parent).SelectMany(GetSubtree);
         }
         public IEnumerable<Backup.Node> GetSubtree (Backup.Node node)
         {
            return new[] { node }.Concat(GetDescendants(node));
         }
         public IEnumerable<Backup.Node> GetSubtrees (IEnumerable<Backup.Node> nodes)
         {
            return nodes.SelectMany(GetSubtree);
         }
         public IEnumerable<Backup.Entry> GetEntries (Backup.Node node)
         {
            return this.archive.BackupIndex.ListNodeEntries(node);
         }
      }
   }
}
