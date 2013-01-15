using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyFloe
{
   public class DiffRequest
   {
      public String Archive { get; set; }
      public Dictionary<String, String> RootPathMap { get; set; }
      public DiffMethod Method { get; set; }
   }
}
