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
      static IEnumerable<String> AllFiles (String path)
      {
         foreach (String entry in Directory.EnumerateFileSystemEntries(path))
            if (Directory.Exists(entry))
               foreach (String descendant in AllFiles(entry))
                  yield return descendant;
            else
               yield return entry;
      }

      static void Main (String[] args)
      {
         DateTime started = DateTime.UtcNow;
         /*
         foreach (String child in Directory.GetFileSystemEntries(@"l:\"))
            CompressPath(child);
         Console.WriteLine(
            "Compressed {0:0}MB to {1:0}MB.",
            ((Double)uncompressedSize) / 1048576,
            ((Double)compressedSize) / 1048576
         );
         */
         using (Connection connect = new Connection(@"Store=AwsGlacier;AccessKey=1V0BC55SS0SF3V9SRG02;SecretKey=h0QC/K4JGcx6MkJ/I7ZpEDTbMX49Eja0S+HOEpDS;"))
         using (Connection.Archive archive = connect.OpenArchive("Test"))
         {
            Func<Backup.Node, IEnumerable<Backup.Node>> nodeSelector = null;
            nodeSelector =
               node => new[] { node }.Concat(archive.GetChildren(node).SelectMany(nodeSelector));
            Engine engine = new Engine() { Connection = connect };
            engine.OpenArchive("Test", "secret");
            foreach (Restore.Session session in archive.Restores.ToList())
               engine.DeleteRestore(session);
            engine.StartRestore(
               engine.CreateRestore(
                  new RestoreRequest()
                  {
                     SkipExisting = false,
                     SkipReadOnly = false,
                     VerifyResults = true,
                     Entries = archive.Roots
                        .SelectMany(nodeSelector)
                        .Where(n => n.Type == Backup.NodeType.File)
                        .Select(
                           n => archive.GetEntries(n)
                              .OrderBy(e => e.Session.Created)
                              .Where(e => e.State == Backup.EntryState.Completed)
                              .Select(e => e.ID)
                              .DefaultIfEmpty(0)
                              .Last()
                        ).Where(id => id != 0),
                     RootPathMap = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase)
                     {
                        { @"c:\temp\source", @"c:\temp\Result" },
                        { @"l:\", @"c:\temp\Liono" }
                     }
                  }
               )
            );
         }

         /*
         using (Connection connect = new Connection("Store=AwsGlacier;AccessKey=1V0BC55SS0SF3V9SRG02;SecretKey=h0QC/K4JGcx6MkJ/I7ZpEDTbMX49Eja0S+HOEpDS;"))
         using (Connection.Archive archive = connect.OpenArchive("Liono"))
         {
            Func<Model.Node, IEnumerable<Model.Node>> nodeSelector = null;
            nodeSelector =
               node => new [] { node }.Concat(archive.GetChildren(node).SelectMany(nodeSelector));
            Backup.Entry entry = archive.Roots
               .SelectMany(nodeSelector)
               .Where(n => n.Name == "04 - Like a Virgin.mp3")
               .SelectMany(n => archive.GetEntries(n))
               .Single(e => e.State == Model.EntryState.Completed);
            Console.WriteLine("{0}: {1} ({2})  {3}", entry.Offset, entry.Length, entry.Crc32, entry.Blob.Name);
         }
         */

         /*
         using (Connection connect = new Connection("Store=AwsGlacier;AccessKey=1V0BC55SS0SF3V9SRG02;SecretKey=h0QC/K4JGcx6MkJ/I7ZpEDTbMX49Eja0S+HOEpDS;"))
         using (Connection.Archive archive = connect.OpenArchive("Liono"))
            Console.WriteLine(
               "{0:0.00}MB",
               ((Double)archive.Sessions.OrderBy(s => s.Created).Last().ActualLength) / 1048576
            );
         */
         /*
         using (Connection connect = new Connection("Store=AwsGlacier;AccessKey=1V0BC55SS0SF3V9SRG02;SecretKey=h0QC/K4JGcx6MkJ/I7ZpEDTbMX49Eja0S+HOEpDS;"))
         using (Connection.Archive archive = connect.OpenArchive("Test"))
            foreach (Backup.Blob blob in archive.Blobs)
               Console.WriteLine("{0} bytes, {1}", blob.Length, blob.Name);
         */

         Console.WriteLine("Duration: {0}", DateTime.UtcNow - started);
         Console.Write("Press enter to exit.");
         Console.ReadLine();
      }
      static void DumpArchive (Connection.Archive arch)
      {
         XDocument doc = new XDocument();
         Func<Backup.Node, XElement> nodeSelector = null;
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
               new XElement(
                  "Sessions",
                  arch.Backups.Select(
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
