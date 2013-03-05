//===========================================================================
// MODULE:  IRestore.cs
// PURPOSE: store restore interface
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
using Stream = System.IO.Stream;
// Project References

namespace SkyFloe.Store
{
   /// <summary>
   /// The store restore interface
   /// </summary>
   /// <remarks>
   /// This is the interface to an active restore process running on an 
   /// archive within a connected backup store.
   /// </remarks>
   public interface IRestore : IDisposable
   {
      /// <summary>
      /// Retrieves a backed-up entry from the archive
      /// </summary>
      /// <param name="entry">
      /// The entry to retrieve
      /// </param>
      /// <returns>
      /// A stream that can be used to retrieve the entry's contents
      /// </returns>
      Stream Restore (Restore.Entry entry);
   }
}
