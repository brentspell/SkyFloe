using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SkyFloe
{
   public enum DiffType
   {
      New,
      Changed,
      Deleted
   }

   public class DiffResult
   {
      public DiffType Type { get; set; }
      public Backup.Node Node { get; set; }
   }
}
