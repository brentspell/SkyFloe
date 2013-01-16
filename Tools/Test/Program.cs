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
         using (var connect = new Connection(@"Store=File;Path=c:\temp\store;"))
         using (var archive = connect.OpenArchive("Test"))
         {
            Func<Model.Node, IEnumerable<Model.Node>> nodeSelector = null;
            nodeSelector =
               node => new[] { node }.Concat(archive.GetChildren(node).SelectMany(nodeSelector));
            var engine = new Engine() { Connection = connect };
            engine.Restore(
               new RestoreRequest()
               {
                  Archive = archive.Name,
                  Password = "secret",
                  OverwriteReadOnly = true,
                  VerifyResults = true,
                  Entries = archive.Roots
                     .SelectMany(nodeSelector)
                     .Where(n => n.Type == Model.NodeType.File)
                     .Select(
                        n => archive.GetEntries(n)
                           .OrderBy(e => e.Session.Created)
                           .Where(e => e.State == Model.EntryState.Completed)
                           .Select(e => e.ID)
                           .DefaultIfEmpty(0)
                           .Last()
                     ).Where(id => id != 0),
                  RootPathMap = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase)
                  {
                     { @"c:\temp\source", @"c:\temp\Result" }
                  }
               }
            );
         }

         /*
         using (var connect = new Connection("Store=AwsGlacier;AccessKey=1V0BC55SS0SF3V9SRG02;SecretKey=h0QC/K4JGcx6MkJ/I7ZpEDTbMX49Eja0S+HOEpDS;"))
         using (var archive = connect.OpenArchive("Liono"))
         {
            Func<Model.Node, IEnumerable<Model.Node>> nodeSelector = null;
            nodeSelector =
               node => new [] { node }.Concat(archive.GetChildren(node).SelectMany(nodeSelector));
            var entry = archive.Roots
               .SelectMany(nodeSelector)
               .Where(n => n.Name == "13 - Express Yourself.mp3")
               .SelectMany(n => archive.GetEntries(n))
               .Single(e => e.State == Model.EntryState.Completed);
            Console.WriteLine("{0}: {1} ({2})  {3}", entry.Offset, entry.Length, entry.Crc32, entry.Blob.Name);
         }
         */
         /*
         using (var connect = new Connection("Store=AwsGlacier;AccessKey=1V0BC55SS0SF3V9SRG02;SecretKey=h0QC/K4JGcx6MkJ/I7ZpEDTbMX49Eja0S+HOEpDS;"))
         using (var archive = connect.OpenArchive("Liono"))
            Console.WriteLine(
               "{0:0.00}MB",
               ((Double)archive.Sessions.OrderBy(s => s.Created).Last().ActualLength) / 1048576
            );
         */
         /*
         using (var connect = new Connection("Store=AwsGlacier;AccessKey=1V0BC55SS0SF3V9SRG02;SecretKey=h0QC/K4JGcx6MkJ/I7ZpEDTbMX49Eja0S+HOEpDS;"))
         using (var archive = connect.OpenArchive("Test"))
            foreach (var blob in archive.Blobs)
               Console.WriteLine("{0} bytes, {1}", blob.Length, blob.Name);
         */
         Console.Write("Press enter to exit.");
         Console.ReadLine();
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
