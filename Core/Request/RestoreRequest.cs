using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyFloe
{
   public class RestoreRequest
   {
      public String Archive { get; set; }
      public String Password { get; set; }
      public Dictionary<String, String> RootPathMap { get; set; }
      public IEnumerable<Int32> Entries { get; set; }
      public Boolean OverwriteReadOnly { get; set; }
      public Boolean VerifyResults { get; set; }
   }
}
