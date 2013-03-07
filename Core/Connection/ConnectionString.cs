//===========================================================================
// MODULE:  ConnectionString.cs
// PURPOSE: store connection string
// 
// Copyright © 2013
// Brent M. Spell. All rights reserved.
//
// This library is free software; you can redistribute it and/or modify it 
// under the terms of the GNU Lesser General Public License as published 
// by the Free Software Foundation; either version 3 of the License, or 
// (at your option) any later version. This library is distributed in the 
// hope that it will be useful, but WITHOUT ANY WARRANTY; without even the 
// implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
// See the GNU Lesser General Public License for more details. You should 
// have received a copy of the GNU Lesser General Public License along with 
// this library; if not, write to 
//    Free Software Foundation, Inc. 
//    51 Franklin Street, Fifth Floor 
//    Boston, MA 02110-1301 USA
//===========================================================================
// System References
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
// Project References
using Strings = SkyFloe.Resources.Strings;

namespace SkyFloe
{
   /// <summary>
   /// The store connection string
   /// </summary>
   /// <remarks>
   /// This class encapsulates a connection string to a SkyFloe store. The
   /// connection string format is 
   ///   Store={store};{prop1}=[value1];{prop2}=[value2];...
   /// The format is the same used for connection strings in System.Data
   /// The store name is required
   /// Property names must include only letters, numbers, and underscores
   /// Property values must be quoted using double quotes if they contain
   /// a double quote or semicolon
   /// Double quotes in values must be escaped by repeating them " => ""
   /// Invalid property values are silently ignored
   /// The following are valid sample connection strings
   ///   Store=MyNamespace.MyStore,MyAssembly;
   ///   Store=MyStore
   ///   Store=MyStore;MyProperty1=value1;MyProperty2=value2;
   ///   Store=MyStore;MyProperty=value
   ///   Store=MyStore;MyProperty="semicolon included in value;";
   ///   Store=MyStore;MyProperty="only ""two"" quotes";
   /// </remarks>
   public class ConnectionString : 
      IEquatable<ConnectionString>,
      IEquatable<String>
   {
      public const String StorePropertyName = "Store";
      public static readonly Regex PropertyRegex = new Regex(
         @" (?<name>\w+)                           " +   // match property name
         @" \s*=\s*                                " +   // match equals sign
         @" (                                      " +
         @"    (?<value>) |                        " +   // match empty value
         @"    (?<value>[^\"";](\s*[^;\s])*) |     " +   // match unquoted value, trim and stop before ;
         @"    (\""(?<value>(\""\""|[^\""])*)\"")  " +   // match quoted value, "" for quote literal
         @" )                                      " +
         @" \s*(;|$)                               ",    // end property with ; or EOF
         RegexOptions.IgnorePatternWhitespace
      );
      private static readonly Regex QuotePropertyRegex = new Regex(@"["";]|^\s.*$|^.*\s$");
      private Dictionary<String, String> props;

      /// <summary>
      /// The name of the store to connect to
      /// </summary>
      public String Store
      { 
         get; private set;
      }
      /// <summary>
      /// The list of properties associated with the store
      /// </summary>
      public IEnumerable<KeyValuePair<String, String>> Properties
      {
         get { return this.props; }
      }

      /// <summary>
      /// Initializes a new connection string instance
      /// </summary>
      /// <param name="store">
      /// The store to connect to
      /// </param>
      /// <param name="props">
      /// The list of store properties to assign
      /// </param>
      public ConnectionString (
         String store, 
         IEnumerable<KeyValuePair<String, String>> props)
      {
         if (String.IsNullOrWhiteSpace(store))
            throw new ArgumentNullException("store");
         if (props == null)
            throw new ArgumentException("props");
         if (props.Any(p => String.IsNullOrWhiteSpace(p.Key)))
            throw new ArgumentException("props");
         this.Store = store;
         this.props = new Dictionary<String, String>(
            StringComparer.OrdinalIgnoreCase
         );
         foreach (var prop in props)
            this.props[prop.Key] = prop.Value;
      }
      /// <summary>
      /// Parses a connection string
      /// </summary>
      /// <param name="str">
      /// The string to parse
      /// </param>
      /// <returns>
      /// The parsed connection string
      /// </returns>
      public static ConnectionString Parse (String str)
      {
         var store = String.Empty;
         var props = new List<KeyValuePair<String, String>>();
         foreach (Match match in PropertyRegex.Matches(str))
         {
            var name = match.Groups["name"].Value;
            var value = match.Groups["value"].Value.Replace("\"\"", "\"");
            if (!StringComparer.OrdinalIgnoreCase.Equals(name, StorePropertyName))
               props.Add(new KeyValuePair<String, String>(name, value));
            else
               store = value;
         }
         return new ConnectionString(store, props);
      }
      /// <summary>
      /// Converts a runtime object to a connection string
      /// </summary>
      /// <param name="store">
      /// The store to read
      /// </param>
      /// <returns>
      /// The connection string for the store
      /// </returns>
      public static ConnectionString Extract (Object store)
      {
         if (store == null)
            throw new ArgumentNullException("store");
         var name = store.GetType().AssemblyQualifiedName;
         var props = new List<KeyValuePair<String, String>>();
         foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(store))
         {
            var value = prop.GetValue(store);
            if (value != null)
               props.Add(
                  new KeyValuePair<String, String>(
                     prop.Name,
                     (String)prop.Converter.ConvertTo(value, typeof(String))
                  )
               );
         }
         return new ConnectionString(name, props);
      }
      /// <summary>
      /// Applies the connection string properties to an object
      /// </summary>
      /// <param name="store">
      /// The store to bind to
      /// </param>
      public void Bind (Object store)
      {
         if (store == null)
            throw new ArgumentNullException("store");
         foreach (var connectProp in this.props)
         {
            var storeProp = TypeDescriptor.GetProperties(store)
               .Find(connectProp.Key, true);
            if (storeProp == null)
               throw new KeyNotFoundException(
                  String.Format(
                     Strings.ConnectionInvalidProperty, 
                     connectProp.Key
                  )
               );
            storeProp.SetValue(
               store,
               storeProp.Converter.ConvertFrom(connectProp.Value)
            );
         }
      }

      #region Object Overrides
      /// <summary>
      /// Gets the connection string value
      /// </summary>
      /// <returns>
      /// The formatted connection string
      /// </returns>
      public override String ToString ()
      {
         StringBuilder str = new StringBuilder();
         str.AppendFormat("{0}={1};", StorePropertyName, this.Store);
         foreach (var prop in this.Properties)
            if (!QuotePropertyRegex.IsMatch(prop.Value))
               str.AppendFormat("{0}={1};", prop.Key, prop.Value);
            else
               str.AppendFormat("{0}=\"{1}\";", prop.Key, prop.Value.Replace("\"", "\"\""));
         return str.ToString();
      }
      /// <summary>
      /// Object equality comparison
      /// </summary>
      /// <param name="other">
      /// The object to compare
      /// </param>
      /// <returns>
      /// True if the objects are equal
      /// False otherwise
      /// </returns>
      public override Boolean Equals (Object other)
      {
         if (other is ConnectionString)
            return Equals((ConnectionString)other);
         if (other is String)
            return Equals((String)other);
         return false;
      }
      /// <summary>
      /// Calculates a hash code for the connection string
      /// </summary>
      /// <returns>
      /// The hash code value
      /// </returns>
      public override Int32 GetHashCode ()
      {
         var hash = this.Store.GetHashCode();
         foreach (var prop in this.props)
         {
            hash ^= StringComparer.OrdinalIgnoreCase.GetHashCode(prop.Key);
            if (prop.Value != null)
               hash ^= prop.Value.GetHashCode();
         }
         return hash;
      }
      #endregion

      #region IEquatable Implementation
      /// <summary>
      /// Connection string equality comparison
      /// </summary>
      /// <param name="cstr">
      /// The connection string to compare
      /// </param>
      /// <returns>
      /// True if the objects are equal
      /// False otherwise
      /// </returns>
      public Boolean Equals (ConnectionString cstr)
      {
         if (cstr == null)
            return false;
         if (!StringComparer.Ordinal.Equals(this.Store, cstr.Store))
            return false;
         if (this.props.Count != cstr.props.Count)
            return false;
         foreach (var prop in this.props)
         {
            var value = (String)null;
            if (!cstr.props.TryGetValue(prop.Key, out value))
               return false;
            if (!StringComparer.Ordinal.Equals(prop.Value, value))
               return false;
         }
         return true;
      }
      /// <summary>
      /// String equality comparison
      /// </summary>
      /// <param name="str">
      /// The string to compare
      /// </param>
      /// <returns>
      /// True if the objects are equal
      /// False otherwise
      /// </returns>
      public Boolean Equals (String str)
      {
         if (String.IsNullOrWhiteSpace(str))
            return false;
         return Equals(Parse(str));
      }
      #endregion

      #region Operators
      public static implicit operator String (ConnectionString cstr)
      {
         return cstr != null ? cstr.ToString() : null;
      }
      public static explicit operator ConnectionString (String str)
      {
         return !String.IsNullOrWhiteSpace(str) ? Parse(str) : null;
      }
      public static Boolean operator == (ConnectionString cstr1, ConnectionString cstr2)
      {
         if (Object.ReferenceEquals(cstr1, null))
            return Object.ReferenceEquals(cstr2, null);
         return cstr1.Equals(cstr2);
      }
      public static Boolean operator == (ConnectionString cstr, String str)
      {
         if (Object.ReferenceEquals(cstr, null))
            return String.IsNullOrWhiteSpace(str);
         return cstr.Equals(str);
      }
      public static Boolean operator == (String str, ConnectionString cstr)
      {
         if (Object.ReferenceEquals(cstr, null))
            return String.IsNullOrWhiteSpace(str);
         return cstr.Equals(str);
      }
      public static Boolean operator != (ConnectionString cstr1, ConnectionString cstr2)
      {
         if (Object.ReferenceEquals(cstr1, null))
            return !Object.ReferenceEquals(cstr2, null);
         return !cstr1.Equals(cstr2);
      }
      public static Boolean operator != (ConnectionString cstr, String str)
      {
         if (Object.ReferenceEquals(cstr, null))
            return !String.IsNullOrWhiteSpace(str);
         return !cstr.Equals(str);
      }
      public static Boolean operator != (String str, ConnectionString cstr)
      {
         if (Object.ReferenceEquals(cstr, null))
            return !String.IsNullOrWhiteSpace(str);
         return !cstr.Equals(str);
      }
      #endregion
   }
}
