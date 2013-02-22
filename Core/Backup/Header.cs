//===========================================================================
// MODULE:  Header.cs
// PURPOSE: backup index header record type
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

namespace SkyFloe.Backup
{
   /// <summary>
   /// The header record type
   /// </summary>
   /// <remarks>
   /// There is exactly one header record in each backup index, which
   /// contains global information about the archive.
   /// </remarks>
   public class Header
   {
      /// <summary>
      /// The archive version, to support archive upgrade or
      /// backward compatibility
      /// </summary>
      public Int32 Version { get; set; }
      /// <summary>
      /// The number of iterations used to derive the archive's
      /// encryption keys and calculate the password hash
      /// </summary>
      public Int32 CryptoIterations { get; set; }
      /// <summary>
      /// The archive encryption key initialization vector
      /// </summary>
      public Byte[] ArchiveSalt { get; set; }
      /// <summary>
      /// The archive password hash, for password verification
      /// </summary>
      public Byte[] PasswordHash { get; set; }
      /// <summary>
      /// The archive password hash salt
      /// </summary>
      public Byte[] PasswordSalt { get; set; }
   }
}
