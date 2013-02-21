using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace SkyFloe.App.Data
{
   public class ConnectionString
   {
      public String Caption { get; set; }
      public String Encrypted { get; set; }

      internal String Value
      {
         get
         {
            return Encoding.UTF8.GetString(
               ProtectedData.Unprotect(
                  Convert.FromBase64String(this.Encrypted),
                  null,
                  DataProtectionScope.CurrentUser
               )
            );
         }
         set
         {
            this.Encrypted = Convert.ToBase64String(
               ProtectedData.Protect(
                  Encoding.UTF8.GetBytes(value),
                  null,
                  DataProtectionScope.CurrentUser
               )
            );
         }
      }
   }

   public class ConnectionStringList
   {
      public ConnectionStringList ()
      {
         this.Items = new List<ConnectionString>();
      }
      public List<ConnectionString> Items
      {
         get; set;
      }
   }
}
