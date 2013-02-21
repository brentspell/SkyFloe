using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyFloe.Store
{
   public interface IStore : IDisposable
   {
      String Caption { get; }
      void Open ();
      IEnumerable<String> ListArchives ();
      IArchive CreateArchive (String name, Backup.Header header);
      IArchive OpenArchive (String name);
      void DeleteArchive (String name);
   }
}
