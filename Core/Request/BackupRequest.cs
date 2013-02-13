using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyFloe
{
   public class BackupRequest
   {
      public IEnumerable<String> Sources { get; set; }
      public DiffMethod DiffMethod { get; set; }
      public Int32 RateLimit { get; set; }
      public Int64 CheckpointLength { get; set; }
   }
}
