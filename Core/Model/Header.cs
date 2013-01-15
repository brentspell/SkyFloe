using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyFloe.Model
{
   public class Header
   {
      public Int32 Version { get; set; }
      public Int32 CryptoIterations { get; set; }
      public Byte[] ArchiveSalt { get; set; }
      public Byte[] PasswordHash { get; set; }
      public Byte[] PasswordSalt { get; set; }
   }
}
