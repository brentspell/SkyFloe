//===========================================================================
// MODULE:  ConnectionException.cs
// PURPOSE: store connection exception
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
using Strings = SkyFloe.Resources.Strings;

namespace SkyFloe
{
   /// <summary>
   /// The connection exception
   /// </summary>
   [Serializable]
   public class ConnectionException : Exception
   {
      /// <summary>
      /// Initializes a new exception instance
      /// </summary>
      public ConnectionException () 
         : base(Strings.ConnectionConnectFailed)
      {
      }
      /// <summary>
      /// Initializes a new exception instance
      /// </summary>
      /// <param name="message">
      /// Custom exception message
      /// </param>
      public ConnectionException (Exception inner) 
         : base(Strings.ConnectionConnectFailed, inner)
      {
      }
      /// <summary>
      /// Initializes a new exception instance
      /// </summary>
      /// <param name="message">
      /// Custom exception message
      /// </param>
      public ConnectionException (String message)
         : base(message)
      {
      }
      /// <summary>
      /// Initializes a new exception instance
      /// </summary>
      /// <param name="message">
      /// Custom exception message
      /// </param>
      /// <param name="inner">
      /// Inner exception instance
      /// </param>
      public ConnectionException (String message, Exception inner)
         : base(message, inner)
      {
      }
      /// <summary>
      /// Initializes a new exception instance
      /// </summary>
      /// <param name="info">
      /// Serialization properties
      /// </param>
      /// <param name="context">
      /// Serialization context
      /// </param>
      protected ConnectionException (
         System.Runtime.Serialization.SerializationInfo info,
         System.Runtime.Serialization.StreamingContext context)
         : base(info, context)
      {
      }
   }
}
