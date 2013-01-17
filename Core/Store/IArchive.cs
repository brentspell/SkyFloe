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
      void BackupEntry (Model.Entry entry, Stream stream);
      void Checkpoint ();
      // restore operations
      void PrepareRestore (IEnumerable<Int32> entries);
      Stream RestoreEntry (Model.Entry entry);
   }
}
