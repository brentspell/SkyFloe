using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Transactions;
using System.Xml.Linq;

namespace SkyFloe.Test
{
   class Program
   {
      static void Main (String[] args)
      {
         var crypto = Aes.Create();
         crypto.Key = 
            new Rfc2898DeriveBytes("", BitConverter.GetBytes(0xDEADBEEF600DF00D))
            .GetBytes(crypto.KeySize / 8);
         using (var input = File.OpenRead(@"c:\test.txt"))
         using (var output = File.OpenWrite(@"c:\test.txt.aes"))
         using (var encoder = new CryptoStream(input, crypto.CreateEncryptor(), CryptoStreamMode.Read))
            encoder.CopyTo(output);
         using (var input = File.OpenRead(@"c:\test.txt.aes"))
         using (var output = File.OpenWrite(@"c:\test.txt.aes.txt"))
         using (var decoder = new CryptoStream(input, crypto.CreateDecryptor(), CryptoStreamMode.Read))
            decoder.CopyTo(output);
      }
      static void DumpArchive (Connection.Archive arch)
      {
         var doc = new XDocument();
         Func<Model.Node, XElement> nodeSelector = null;
         nodeSelector = n => new XElement(
            "Node",
            new XAttribute("Name", n.Name),
            arch.GetEntries(n).Select(
               e => new XElement(
                  "Entry",
                  new XAttribute("Created", e.Session.Created),
                  (e.Blob != null) ? new XAttribute("BlobID", e.Blob.ID) : null,
                  new XAttribute("State", e.State),
                  new XAttribute("Offset", e.Offset),
                  new XAttribute("Length", e.Length),
                  new XAttribute("Crc32", e.Crc32)
               )
            ),
            arch.GetChildren(n).Select(nodeSelector)
         );
         doc.Add(
            new XElement(
               "Index",
               new XAttribute("Name", arch.Name),
               new XAttribute("IndexSize", arch.IndexSize),
               new XElement(
                  "Sessions",
                  arch.Sessions.Select(
                     s => new XElement(
                        "Session",
                        new XAttribute("Created", s.Created),
                        new XAttribute("State", s.State)
                     )
                  ).ToArray()
               ),
               new XElement(
                  "Blobs",
                  arch.Blobs.Select(
                     b => new XElement(
                        "Blob",
                        new XAttribute("ID", b.ID),
                        new XAttribute("Name", b.Name),
                        new XAttribute("Length", b.Length)
                     )
                  ).ToArray()
               ),
               new XElement(
                  "Nodes",
                  arch.Roots.Select(nodeSelector).ToArray()
               )
            )
         );
         doc.Save(@"c:\temp\test.xml");
      }
   }
}
