using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyFloe
{
   public enum DiffType
   {
      New = 1,
      Changed = 2,
      Deleted = 3
   }

   public class DiffResult
   {
      public List<Differencer.Diff> Entries { get; set; }
   }
}
