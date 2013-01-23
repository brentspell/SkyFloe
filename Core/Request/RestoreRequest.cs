using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyFloe
{
   public class RestoreRequest
   {
      public Dictionary<String, String> RootPathMap { get; set; }
      public IEnumerable<Int32> Entries { get; set; }
      public Boolean SkipExisting { get; set; }
      public Boolean SkipReadOnly { get; set; }
      public Boolean VerifyResults { get; set; }
      public Boolean EnableDeletes { get; set; }
   }
}
