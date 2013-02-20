using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SkyFloe
{
   public class RegexFilter
   {
      public RegexFilter ()
      {
         this.Include = Enumerable.Empty<Regex>();
         this.Exclude = Enumerable.Empty<Regex>();
      }

      public IEnumerable<Regex> Include { get; set; }
      public IEnumerable<Regex> Exclude { get; set; }

      public Boolean IsValid
      {
         get
         {
            if (this.Include.Any(i => i == null))
               return false;
            if (this.Exclude.Any(i => i == null))
               return false;
            if (this.Include.Intersect(this.Exclude).Any())
               return false;
            return true;
         }
      }

      public Boolean Evaluate (String value)
      {
         if (this.Exclude.Any(r => r.IsMatch(value)))
            return false;
         if (this.Include.Any() && !this.Include.Any(r => r.IsMatch(value)))
            return false;
         return true;
      }
   }
}
