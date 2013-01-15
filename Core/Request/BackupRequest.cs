using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyFloe
{
   public class BackupRequest
   {
      public String Archive { get; set; }
      public String Password { get; set; }
      public IEnumerable<String> Sources { get; set; }
      public DiffMethod DiffMethod { get; set; }
   }
}
