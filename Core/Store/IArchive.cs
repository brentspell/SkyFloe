using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SkyFloe.Store
{
   public interface IArchive : IDisposable
   {
      String Name { get; }
      IBackupIndex Index { get; }
      // backup operations
      void PrepareBackup ();
      void BackupEntry (Backup.Entry entry, Stream stream);
      void Checkpoint ();
      // restore operations
      IRestoreSession PrepareRestore (IEnumerable<Int32> entries);
      IRestoreSession AttachRestore (Stream stream);
      Stream RestoreEntry (Backup.Entry entry);
   }
}
