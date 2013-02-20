using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SkyFloe
{
   public class BackupRequest
   {
      public BackupRequest ()
      {
         this.Sources = Enumerable.Empty<String>();
         this.Filter = new RegexFilter();
      }

      public IEnumerable<String> Sources { get; set; }
      public RegexFilter Filter { get; set; }
      public DiffMethod DiffMethod { get; set; }
      public Int32 RateLimit { get; set; }
      public Int64 CheckpointLength { get; set; }
   }
}
