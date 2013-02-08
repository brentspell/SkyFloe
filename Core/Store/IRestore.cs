using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SkyFloe.Store
{
   public interface IRestore : IDisposable
   {
      Stream Restore (Restore.Entry entry);
   }
}
