using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SkyFloe.Store
{
   public interface IArchive : IDisposable
   {
      String Name { get; }
      IIndex Index { get; }
      // backup operations
      void PrepareBackup ();
      void StoreEntry (Model.Entry entry, Stream stream);
      void Checkpoint ();
      // restore operations
      void PrepareRestore (IList<BlobRestore> blobs);
      Stream LoadEntry (Model.Entry entry);
   }
}
