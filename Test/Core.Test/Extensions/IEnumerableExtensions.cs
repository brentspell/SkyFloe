//===========================================================================
// MODULE:  IEnumerableExtensions.cs
// PURPOSE: extension methods for the IEnumerable interface
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
// Project References

namespace SkyFloe.Core.Test
{
   public static class IEnumerableExtensions
   {
      public static IDictionary<TKey, TValue> ToDictionary<TKey, TValue> (
         this IEnumerable<KeyValuePair<TKey, TValue>> e)
      {
         return e.ToDictionary(p => p.Key, p => p.Value);
      }
   }
}
