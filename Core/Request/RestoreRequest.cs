using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyFloe
{
   public class RestoreRequest
   {
      public RestoreRequest ()
      {
         this.RootPathMap = new Dictionary<IO.Path, IO.Path>();
         this.Entries = Enumerable.Empty<Int32>();
         this.Filter = new RegexFilter();
      }

      public IDictionary<IO.Path, IO.Path> RootPathMap { get; set; }
      public IEnumerable<Int32> Entries { get; set; }
      public RegexFilter Filter { get; set; }
      public Boolean SkipExisting { get; set; }
      public Boolean SkipReadOnly { get; set; }
      public Boolean VerifyResults { get; set; }
      public Boolean EnableDeletes { get; set; }
      public Int32 RateLimit { get; set; }
   }
}
