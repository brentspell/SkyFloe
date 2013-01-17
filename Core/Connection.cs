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
         var paramMap = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
         foreach (var param in connect.Split(';'))
         {
            var sepIdx = param.IndexOf('=');
            if (sepIdx != -1)
               paramMap[param.Substring(0, sepIdx).Trim()] =
                  param.Substring(sepIdx + 1).Trim();
         }
         // determine the store name
         var storeName = "";
         if (!paramMap.TryGetValue("Store", out storeName))
            throw new InvalidOperationException("TODO: store not found");
         paramMap.Remove("Store");
         var knownStore = "";
         if (knownStores.TryGetValue(storeName, out knownStore))
            storeName = knownStore;
         // attempt to load the store type
         var storeType = Type.GetType(storeName, true);
         var store = (Store.IStore)Activator.CreateInstance(storeType);
         // bind the store properties
         foreach (var param in paramMap)
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
         public IEnumerable<Model.Blob> Blobs
         {
            get { return this.archive.Index.ListBlobs(); }
         }
         public IEnumerable<Model.Session> Sessions
         {
            get { return this.archive.Index.ListSessions(); }
         }
         public IEnumerable<Model.Node> Roots
         {
            get { return this.archive.Index.ListNodes(null); }
         }
         public IEnumerable<Model.Node> GetChildren (Model.Node parent)
         {
            return this.archive.Index.ListNodes(parent);
         }
         public IEnumerable<Model.Entry> GetEntries (Model.Node node)
         {
            return this.archive.Index.ListNodeEntries(node);
         }
      }
   }
}
