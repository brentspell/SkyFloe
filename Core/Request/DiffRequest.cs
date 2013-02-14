using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyFloe
{
   public class DiffRequest
   {
      public Dictionary<IO.Path, IO.Path> RootPathMap { get; set; }
      public DiffMethod Method { get; set; }
   }
}
