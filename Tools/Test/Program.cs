﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
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
         foreach (String dir in Directory.EnumerateDirectories(path))
            if (!File.GetAttributes(dir).HasFlag(FileAttributes.System))
               foreach (String descendant in AllFiles(dir))
                  yield return descendant;
         foreach (String file in Directory.EnumerateFiles(path))
            if (!File.GetAttributes(file).HasFlag(FileAttributes.System))
               yield return file;
      }

      static void Main (String[] args)
      {
         DateTime started = DateTime.UtcNow;

#if false
         var dupBytes = 0L;
         var hashSet = new HashSet<String>();
         foreach (var file in AllFiles(@"l:\"))
            using (var stream = File.OpenRead(file))
               if (!hashSet.Add(Convert.ToBase64String(SHA1.Create().ComputeHash(stream))))
                  dupBytes += stream.Length;
         Console.WriteLine("Duplicate MB: {0}", dupBytes / 1048576);
#endif

#if false
         using (var idx = Sqlite.BackupIndex.Open(@"c:\liono.db"))
         {
            var node = 
               idx.ListNodes(
               idx.ListNodes(
               idx.ListNodes(
               idx.ListNodes(
               idx.ListNodes(
               idx.ListNodes(
               idx.ListNodes(
               idx.ListNodes(
               idx.ListNodes(null)
               .Single(n => n.Name == "l:\\"))
               .Single(n => n.Name == "Kelly"))
               .Single(n => n.Name == "Pictures"))
               .Single(n => n.Name == "2008"))
               .Single(n => n.Name == "Chelsea's Wedding Celebration 2008"))
               .Single(n => n.Name == "Kate's pics"))
               .Single(n => n.Name == "Fourth of July 7.4.08"))
               .Single(n => n.Name == "Originals"))
               .Single(n => n.Name == "IMG_2701.jpg");
            var entry = idx.ListNodeEntries(node).Single();
         }
#endif

#if false
         using (var idx = Sqlite.RestoreIndex.Open(@"c:\restore.db"))
         {
            Restore.Session session = idx.ListSessions().Single();
            for (Int32 i = 0; i < 100; i++)
               idx.LookupNextEntry(session);
            for (; ; )
            {
               Restore.Entry entry = idx.LookupNextEntry(session);
               if (entry == null)
                  break;
               entry.State = Restore.EntryState.Completed;
               idx.UpdateEntry(entry);
            }
         }
#endif

#if true
         using (Connection connect = new Connection(String.Format(@"Store=AwsGlacier;AccessKey={0};SecretKey={1};", args[0], args[1])))
         using (Connection.Archive archive = connect.OpenArchive("Liono"))
         {
            Func<Backup.Node, IEnumerable<Backup.Node>> nodeSelector = null;
            nodeSelector =
               node => new[] { node }.Concat(archive.GetChildren(node).SelectMany(nodeSelector));
            Engine engine = new Engine() { Connection = connect };
            using (engine)
            {
               Int32 retries = 0;
               engine.OnError += e =>
               {
                  if (retries++ > 30)
                     e.Result = Engine.ErrorResult.Abort;
                  else
                  {
                     Console.WriteLine("   Retrying...");
                     System.Threading.Thread.Sleep(retries * 1000);
                     e.Result = Engine.ErrorResult.Retry;
                  }
               };
               engine.OnProgress += e =>
               {
                  retries = 0;
                  if (e.Entry != null)
                     Console.WriteLine("   Restored {0}", e.Entry.Node.GetRelativePath());
               };
               engine.OpenArchive("Liono", "y7df3bn#");
               foreach (Restore.Session existing in archive.Restores.ToList())
                  if (existing.State == Restore.SessionState.Completed)
                     engine.DeleteRestore(existing);
               Restore.Session session = archive.Restores.FirstOrDefault();
               if (session == null)
                  session = engine.CreateRestore(
                     new RestoreRequest()
                     {
                        SkipExisting = true,
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
                           { @"l:\", @"e:\" }
                        }
                     }
                  );
               engine.StartRestore(session);
            }
         }

         /*
         using (Connection connect = new Connection(String.Format(@"Store=AwsGlacier;AccessKey={0};SecretKey={1};", args[0], args[1])))
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
         using (Connection connect = new Connection(String.Format(@"Store=AwsGlacier;AccessKey={0};SecretKey={1};", args[0], args[1])))
         using (Connection.Archive archive = connect.OpenArchive("Liono"))
            Console.WriteLine(
               "{0:0.00}MB",
               ((Double)archive.Sessions.OrderBy(s => s.Created).Last().ActualLength) / 1048576
            );
         */
         /*
         using (Connection connect = new Connection(String.Format(@"Store=AwsGlacier;AccessKey={0};SecretKey={1};", args[0], args[1])))
         using (Connection.Archive archive = connect.OpenArchive("Test"))
            foreach (Backup.Blob blob in archive.Blobs)
               Console.WriteLine("{0} bytes, {1}", blob.Length, blob.Name);
         */
#endif

         Console.WriteLine("Duration: {0}", DateTime.UtcNow - started);
         Console.Write("Press enter to exit.");
         Console.ReadLine();
      }
   }
}
