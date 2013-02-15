using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyFloe
{
   public enum DiffMethod
   {
      Timestamp = 1,
      Digest = 2
   }

   public class DiffRequest
   {
      public Dictionary<IO.Path, IO.Path> RootPathMap { get; set; }
      public DiffMethod Method { get; set; }
   }
}
