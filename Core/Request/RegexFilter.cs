//===========================================================================
// MODULE:  RegexFilter.cs
// PURPOSE: backup/restore/differencing file filter
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
using System.Linq;
using System.Text.RegularExpressions;
// Project References

namespace SkyFloe
{
   /// <summary>
   /// The file filter
   /// </summary>
   /// <remarks>
   /// This class represents a filter to apply to incoming file paths
   /// when performing a backup, restore, or differencing operation. Filters
   /// are specified as regular expressions applied to the full file path,
   /// using inclusion/exclusion semantics.
   ///   If an exclude regex matches the file
   ///      path does not pass
   ///   Else If there are include regexes, and one matches the file
   ///      path passes
   ///   Else
   ///      path passes
   /// Based on these rules, in a conflict scenario (include+exclude match),
   /// the exclusion takes priority.
   /// </remarks>
   public class RegexFilter
   {
      /// <summary>
      /// Initializes a new filter instance
      /// </summary>
      public RegexFilter ()
      {
         this.Include = Enumerable.Empty<Regex>();
         this.Exclude = Enumerable.Empty<Regex>();
      }

      /// <summary>
      /// The list of inclusion filter expressions
      /// </summary>
      public IEnumerable<Regex> Include { get; set; }
      /// <summary>
      /// The list of exclusion filter expressions
      /// </summary>
      public IEnumerable<Regex> Exclude { get; set; }

      /// <summary>
      /// Validates the filter expressions
      /// </summary>
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
      /// <summary>
      /// Evaluates the filter against a path value
      /// </summary>
      /// <param name="value">
      /// The value to test
      /// </param>
      /// <returns>
      /// True if the path value passes through the filter
      /// False otherwise
      /// </returns>
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
