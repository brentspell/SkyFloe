using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SkyFloe.Store
{
   public interface IBackup : IDisposable
   {
      void Backup (Backup.Entry entry, Stream stream);
      void Checkpoint ();
   }
}
