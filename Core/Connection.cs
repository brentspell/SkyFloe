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
            { "File", "SkyFloe.FileStore,SkyFloe.FileStore" },
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
         Dictionary<String, String> paramMap = 
            new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
         foreach (String param in connect.Split(';'))
         {
            Int32 sepIdx = param.IndexOf('=');
            if (sepIdx != -1)
               paramMap[param.Substring(0, sepIdx).Trim()] =
                  param.Substring(sepIdx + 1).Trim();
         }
         // determine the store name
         String storeName = null;
         if (!paramMap.TryGetValue("Store", out storeName))
            throw new InvalidOperationException("TODO: store not found");
         paramMap.Remove("Store");
         String knownStore = "";
         if (knownStores.TryGetValue(storeName, out knownStore))
            storeName = knownStore;
         // attempt to load the store type
         Type storeType = Type.GetType(storeName, true);
         Store.IStore store = (Store.IStore)Activator.CreateInstance(storeType);
         // bind the store properties
         foreach (KeyValuePair<String, String> param in paramMap)
         {
            PropertyDescriptor prop = TypeDescriptor.GetProperties(storeType)
               .Cast<PropertyDescriptor>()
               .FirstOrDefault(p => String.Compare(p.Name, param.Key, true) == 0);
            if (prop == null)
               throw new InvalidOperationException("TODO: connection string param not found");
            try
            {
               prop.SetValue(store, prop.Converter.ConvertFrom(param.Value));
            }
            catch (Exception e)
            {
               throw new InvalidOperationException("TODO: failed to bind connection string param", e);
            }
         }
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
         public IEnumerable<Restore.Session> Restores
         {
            get 
            {
               return this.archive.RestoreIndex
                  .ListSessions()
                  .Where(s => String.Compare(s.Archive, this.Name, true) == 0);
            }
         }
         public IEnumerable<Backup.Node> GetChildren (Backup.Node parent)
         {
            return this.archive.BackupIndex.ListNodes(parent);
         }
         public IEnumerable<Backup.Entry> GetEntries (Backup.Node node)
         {
            return this.archive.BackupIndex.ListNodeEntries(node);
         }
      }
   }
}
