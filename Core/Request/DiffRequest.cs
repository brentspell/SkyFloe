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
      public DiffRequest ()
      {
         this.RootPathMap = new Dictionary<IO.Path, IO.Path>();
         this.Filter = new RegexFilter();
      }

      public IDictionary<IO.Path, IO.Path> RootPathMap { get; set; }
      public DiffMethod Method { get; set; }
      public RegexFilter Filter { get; set; }
   }
}
