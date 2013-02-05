using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SkyFloe.App.Data
{
   [SettingsSerializeAs(SettingsSerializeAs.String)]
   [TypeConverter(typeof(ConnectionStringList.Converter))]
   public class ConnectionStringList : List<String>
   {
      public class Converter : TypeConverter
      {
         private const String Separator = "|";

         #region TypeConverter Overrides
         public override Boolean CanConvertFrom (
            ITypeDescriptorContext context, 
            Type type)
         {
            if (type == typeof(String))
               return true;
            return base.CanConvertFrom(context, type);
         }
         public override Boolean CanConvertTo (
            ITypeDescriptorContext context, 
            Type type)
         {
            if (type == typeof(String))
               return true;
            return base.CanConvertTo(context, type);
         }
         public override Object ConvertFrom (
            ITypeDescriptorContext context, 
            System.Globalization.CultureInfo culture, 
            Object value)
         {
            if (value is String)
            {
               ConnectionStringList list = new ConnectionStringList();
               list.AddRange(
                  Encoding.UTF8.GetString(
                     ProtectedData.Unprotect(
                        Convert.FromBase64String((String)value),
                        null,
                        DataProtectionScope.CurrentUser
                     )
                  ).Split(new[] { Separator[0] }, StringSplitOptions.RemoveEmptyEntries)
               );
               return list;
            }
            return base.ConvertFrom(context, culture, value);
         }
         public override Object ConvertTo (
            ITypeDescriptorContext context, 
            System.Globalization.CultureInfo culture, 
            Object value, 
            Type type)
         {
            if (type == typeof(String))
               return Convert.ToBase64String(
                  ProtectedData.Protect(
                     Encoding.UTF8.GetBytes(
                        String.Join(Separator, (ConnectionStringList)value)
                     ),
                     null,
                     DataProtectionScope.CurrentUser
                  )
               );
            return base.ConvertTo(context, culture, value, type);
         }
         #endregion
      }
   }
}
