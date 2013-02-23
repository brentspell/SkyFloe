using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SkyFloe.Lab
{
   public class Test
   {
      public Int32 ThreadID;
      public Int32 Iteration;
      
      static Test ()
      {
      }
      
      public Test ()
      {
      }

      public void Run ()
      {
      }
   }

   public class Builder : IEnumerable<KeyValuePair<String, Object>>
   {
      public static readonly Regex PropertyStringRegex = new Regex(
         @"(?<key>\w+) \s* = \s* (?<value>((\\;)|(\s*[^;\s]))*) \s* (;|$)", 
         RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace
      );
      Type t;
      IDictionary<String, Object> properties;

      public Builder (Type t)
      {
         this.t = t;
         this.properties = new Dictionary<String, Object>(
            StringComparer.OrdinalIgnoreCase
         );
      }
      public Builder (Type t, IDictionary<String, Object> properties) :
         this(t)
      {
         SetProperties(properties);
      }
      public Builder (Type t, String properties) : 
         this(t)
      {
         SetProperties(properties);
      }
      public void SetProperties (IDictionary<String, Object> properties)
      {
         this.properties.Clear();
         foreach (var prop in properties)
            this.properties.Add(prop);
      }
      public void SetProperties (String properties)
      {
         this.properties.Clear();
         foreach (var match in PropertyStringRegex.Matches(properties).Cast<Match>())
            this.properties[match.Groups["key"].Value] = match.Groups["value"].Value;
      }

      public void Add (String key, Object value)
      {
         this.properties[key] = value;
      }
      public void Remove (String key)
      {
         this.properties.Remove(key);
      }
      public void Clear ()
      {
         this.properties.Clear();
      }

      #region IEnumerable Implementation
      public IEnumerator<KeyValuePair<String, Object>> GetEnumerator ()
      {
         return this.properties.GetEnumerator();
      }

      System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
      {
         return this.properties.GetEnumerator();
      }
      #endregion
   }
}
