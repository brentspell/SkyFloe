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
      Stream LoadEntry (Model.Entry entry);
      void StoreEntry (Model.Entry entry, Stream stream);
      void Checkpoint ();
   }
}
