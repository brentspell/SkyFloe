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
         Console.WriteLine("testing");
         foreach (var prop in Builder
            .Parse(@"Test1=val u e1 ;Test1=value2")
            .GroupBy(p => p.Key)
            .Select(g => new KeyValuePair<String, Object>(g.Key, g.Last()))
         )
            Console.WriteLine("|{0} = {1}|", prop.Key, prop.Value);
         Console.WriteLine("done");
      }
   }

   public class Builder : IEnumerable<KeyValuePair<String, Object>>
   {
      public static readonly Regex PropertyStringRegex = new Regex(
         @"(?<key>\w+) \s* = \s* (?<value>(\s*(\\;|[^;\s]))*) \s* ;?", 
         RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace
      );
      public static readonly Regex PropertyEscapeRegex = new Regex(
         @"\\(?<char>.)"
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
         foreach (var prop in Parse(properties))
            this.properties[prop.Key] = prop.Value;
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
      public static IEnumerable<KeyValuePair<String, String>> Parse (String properties)
      {
         return PropertyStringRegex.Matches(properties)
            .Cast<Match>()
            .Select(
               m => new KeyValuePair<String, String>(
                  m.Groups["key"].Value,
                  PropertyEscapeRegex.Replace(m.Groups["value"].Value, @"${char}")
               )
            );
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
