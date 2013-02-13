using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SkyFloe.Restore
{
   class Program
   {
      private static String connectionString;
      private static String archiveName;
      private static String password;
      private static Dictionary<String, String> rootPathMap;
      private static Boolean skipExisting;
      private static Boolean skipReadOnly;
      private static Boolean verifyResults;
      private static Boolean enableDeletes;
      private static List<String> restoreFiles;
      private static Int32 maxRetries;
      private static Int32 maxFailures;
      private static Int32 rateLimit;
      private static Int32 retries;
      private static Int32 failures;

      static Int32 Main (String[] args)
      {
         Console.SetBufferSize(Math.Max(1000, Console.BufferWidth), Console.BufferHeight);
         Console.WriteLine("SkyFloe Restore");
         if (!ParseOptions(args))
         {
            ReportUsage();
            return 1;
         }
         if (!ExecuteRestore())
            return 1;
         return 0;
      }

      static Boolean ParseOptions (String[] args)
      {
         // initialize options
         password = "";
         rootPathMap = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
         maxRetries = 5;
         maxFailures = 5;
         restoreFiles = new List<String>();
         rateLimit = Int32.MaxValue / 1024;
         // parse options
         try
         {
            new Options.OptionSet()
            {
               { "c|connect=", v => connectionString = v },
               { "a|archive=", v => archiveName = v },
               { "p|password=", v => password = v },
               { "r|max-retry=", (Int32 v) => maxRetries = v },
               { "f|max-fail=", (Int32 v) => maxFailures = v },
               { "m|map-path=", v => rootPathMap.Add(v.Split('=')[0], v.Split('=')[1]) },
               { "e|skip-existing", v => skipExisting = (v != null) },
               { "o|skip-readonly", v => skipReadOnly = (v != null) },
               { "v|verify", v => verifyResults = (v != null) },
               { "k|delete", v => enableDeletes = (v != null) },
               { "i|file=", v => restoreFiles.Add(v) },
               { "l|rate=", (Int32 v) => rateLimit = v },
            }.Parse(args);
         }
         catch (Options.OptionException) { return false; }
         // validate options
         if (String.IsNullOrWhiteSpace(connectionString))
            return false;
         if (String.IsNullOrWhiteSpace(archiveName))
            return false;
         if (maxRetries < 0)
            return false;
         if (maxFailures < 0)
            return false;
         if (rootPathMap.Any(p => String.IsNullOrWhiteSpace(p.Key)))
            return false;
         if (rootPathMap.Any(p => String.IsNullOrWhiteSpace(p.Value)))
            return false;
         if (restoreFiles.Any(f => String.IsNullOrWhiteSpace(f)))
            return false;
         return true;
      }

      static void ReportUsage ()
      {
         Console.WriteLine("   Usage: SkyFloe.Restore {options}");
         Console.WriteLine("      -c|-connect {connect}      backup store connection string");
         Console.WriteLine("      -a|-archive {archive}      archive name");
         Console.WriteLine("      -p|-password {password}    archive password");
         Console.WriteLine("      -r|-max-retry {retries}    maximum file retries before skipping (default = 5)");
         Console.WriteLine("      -f|-max-fail {failures}    maximum file failures before aborting (default = 5)");
         Console.WriteLine("      -m|-map-path {src}={dst}   map a root backup path to a restore path");
         Console.WriteLine("      -e|-skip-existing[+/-]     ignore existing files (no overwrite)");
         Console.WriteLine("      -o|-skip-readonly[+/-]     ignore read-only files");
         Console.WriteLine("      -v|-verify[+/-]            verify CRCs of restored files");
         Console.WriteLine("      -k|-delete[+/-]            delete files marked as deleted in the archive");
         Console.WriteLine("      -i|-file {path}            specify an individual file to restore (source absolute path)");
         Console.WriteLine("      -l|-rate {limit}           restore rate limit, in KB/sec, default: unlimited");
      }

      static Boolean ExecuteRestore ()
      {
         try
         {
            Console.Write("   Connecting to archive {0}...", archiveName);
            Engine engine = new Engine()
            {
               Connection = new Connection(connectionString)
            };
            using (engine)
            {
               engine.OnProgress += HandleProgress;
               engine.OnError += HandleError;
               engine.OpenArchive(archiveName, password);
               Console.WriteLine("done.");
               Restore.Session session = null;
               using (Connection.Archive archive = engine.Connection.OpenArchive(archiveName))
               {
                  session = archive.Restores.FirstOrDefault(s => s.State != SessionState.Completed);
                  if (session != null)
                     Console.WriteLine(
                        "   Resuming restore session started {0:MMM d, yyyy h:m tt}.",
                        session.Created
                     );
                  else
                  {
                     Console.Write("   Creating a new restore session...");
                     IEnumerable<Backup.Node> nodes;
                     if (!restoreFiles.Any())
                        nodes = archive.Roots;
                     else
                     {
                        List<Backup.Node> nodeList = new List<Backup.Node>();
                        foreach (String file in restoreFiles)
                        {
                           Backup.Node node = archive.Roots.FirstOrDefault(
                              r => file.StartsWith(r.Name, StringComparison.OrdinalIgnoreCase)
                           );
                           String path = file.Substring(node.Name.Length);
                           while (node != null && path != String.Empty)
                           {
                              node = archive.GetChildren(node).FirstOrDefault(n => path.StartsWith(n.Name, StringComparison.OrdinalIgnoreCase));
                              path = (node != null) ?
                                 path = path.Substring(node.Name.Length + 1) : 
                                 String.Empty;
                           }
                           if (node == null)
                              throw new InvalidOperationException(String.Format("Path not found in the archive: {0}.", file));
                           nodeList.Add(node);
                        }
                        nodes = nodeList;
                     }
                     nodes = archive.GetSubtrees(nodes);
                     session = engine.CreateRestore(
                        new RestoreRequest()
                        {
                           RootPathMap = rootPathMap,
                           SkipExisting = skipExisting,
                           SkipReadOnly = skipReadOnly,
                           VerifyResults = verifyResults,
                           EnableDeletes = enableDeletes,
                           RateLimit = rateLimit * 1024,
                           Entries = nodes.Select(
                              n => archive.GetEntries(n)
                                 .OrderBy(e => e.Session.Created)
                                 .Where(e => e.State == Backup.EntryState.Completed)
                                 .Select(e => e.ID)
                                 .DefaultIfEmpty(0)
                                 .Last()
                           ).Where(id => id != 0),
                        }
                     );
                     Console.WriteLine("done.");
                  }
               }
               engine.StartRestore(session);
            }
            Console.WriteLine();
            Console.WriteLine("   Restore complete.");
         }
         catch (Exception e)
         {
            Console.WriteLine();
            Console.WriteLine(
               "   Restore failed: {0}", 
               e.ToString().Replace("\n", "\n      ")
            );
            return false;
         }
         return true;
      }

      static void HandleProgress (Engine.ProgressEvent evt)
      {
         switch (evt.Type)
         {
            case Engine.EventType.BeginRestoreEntry:
               String[] units = { "KB", "MB", "GB", "TB" };
               Int64 totalSize = evt.RestoreSession.RestoreLength / 1024;
               Int64 entrySize = evt.RestoreEntry.Length / 1024;
               Int32 totalUnit = 0;
               Int32 entryUnit = 0;
               while (totalSize > 999)
               {
                  totalSize /= 1024;
                  totalUnit++;
               }
               while (entrySize > 999)
               {
                  entrySize /= 1024;
                  entryUnit++;
               }
               Console.Write(
                  "   {0:MM/dd hh:mm}: Total: {1,3:#,0} {2}, Current: {3,3:#,0} {4} - {5}...",
                  DateTime.Now,
                  totalSize,
                  units[totalUnit],
                  entrySize,
                  units[entryUnit],
                  evt.BackupEntry.Node.GetRelativePath()
               );
               break;
            case Engine.EventType.EndRestoreEntry:
               Console.WriteLine("done.");
               break;
         }
         retries = failures = 0;
      }

      static void HandleError (Engine.ErrorEvent evt)
      {
         if (++retries <= maxRetries)
         {
            System.Threading.Thread.Sleep(retries * 1000);
            evt.Result = Engine.ErrorResult.Retry;
            Console.WriteLine("      Retrying...");
         }
         else if (++failures <= maxFailures && evt.RestoreEntry != null)
         {
            evt.Result = Engine.ErrorResult.Fail;
            retries = 0;
            Console.WriteLine("      Skipping {0} due to error.", evt.BackupEntry.Node.GetRelativePath());
            Console.WriteLine(
               "         {0}",
               evt.Exception.ToString().Replace("\n", "\n         ")
            );
         }
         else
            evt.Result = Engine.ErrorResult.Abort;
      }
   }
}
