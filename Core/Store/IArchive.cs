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
      IBackup PrepareBackup (Backup.Session session);
      IRestore PrepareRestore (Restore.Session session);
   }
}
