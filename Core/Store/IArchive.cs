using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SkyFloe.Store
{
   public interface IArchive : IDisposable
   {
      String Name { get; }
      IBackupIndex BackupIndex { get; }
      IRestoreIndex RestoreIndex { get; }
      // backup operations
      void PrepareBackup ();
      void BackupEntry (Backup.Entry entry, Stream stream);
      void Checkpoint ();
      // restore operations
      void PrepareRestore (Restore.Session session);
      Stream RestoreEntry (Restore.Entry entry);
   }
}
