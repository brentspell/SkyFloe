using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SkyFloe.Backup
{
   class Program
   {
      private static String connectionString;
      private static String archiveName;
      private static String password;
      private static DiffMethod diffMethod;
      private static IList<String> sourcePaths;
      private static Boolean deleteArchive;
      private static Int32 maxRetries;
      private static Int32 maxFailures;
      private static Int32 retries;
      private static Int32 failures;

      static Int32 Main (String[] args)
      {
         Console.WriteLine("SkyFloe Backup");
         if (!ParseOptions(args))
         {
            ReportUsage();
            return 1;
         }
         if (!ExecuteBackup())
            return 1;
         return 0;
      }

      static Boolean ParseOptions (String[] args)
      {
         // initialize options
         password = "";
         diffMethod = DiffMethod.Timestamp;
         sourcePaths = new List<String>();
         deleteArchive = false;
         maxRetries = 5;
         maxFailures = 5;
         // parse options
         try
         {
            new Options.OptionSet()
            {
               { "c|connect=", v => connectionString = v },
               { "a|archive=", v => archiveName = v },
               { "p|password=", v => password = v },
               { "r|max-retry=", v => maxRetries = Int32.Parse(v) },
               { "f|max-fail=", v => maxFailures = Int32.Parse(v) },
               { "s|source=", v => sourcePaths.Add(v) },
               { "k|delete", v => deleteArchive = true },
               { "d|diff=", v => diffMethod = (DiffMethod)Enum.Parse(typeof(DiffMethod), v) },
            }.Parse(args);
         }
         catch (Options.OptionException) { return false; }
         // validate options
         if (String.IsNullOrWhiteSpace(connectionString))
            return false;
         if (String.IsNullOrWhiteSpace(archiveName))
            return false;
         if (diffMethod == 0)
            return false;
         if (maxRetries < 0)
            return false;
         if (maxFailures < 0)
            return false;
         if (!sourcePaths.Any())
            sourcePaths.Add(Environment.CurrentDirectory);
         if (sourcePaths.Any(p => String.IsNullOrWhiteSpace(p)))
            return false;
         return true;
      }

      static Boolean ExecuteBackup ()
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
               if (deleteArchive)
                  engine.DeleteArchive(archiveName);
               if (engine.Connection.ListArchives().Contains(archiveName, StringComparer.OrdinalIgnoreCase))
                  engine.OpenArchive(archiveName, password);
               else
                  engine.CreateArchive(archiveName, password);
               Console.WriteLine("done.");
               Backup.Session session = null;
               using (Connection.Archive archive = engine.Connection.OpenArchive(archiveName))
                  session = archive.Backups.FirstOrDefault(s => s.State != SessionState.Completed);
               if (session != null)
                  Console.WriteLine(
                     "   Resuming backup session started {0:MMM d, yyyy h:m tt}.",
                     session.Created
                  );
               else
               {
                  Console.Write("   Creating a new backup session...");
                  session = engine.CreateBackup(
                     new BackupRequest()
                     {
                        DiffMethod = diffMethod,
                        Sources = sourcePaths
                     }
                  );
                  Console.WriteLine("done.");
               }
               engine.StartBackup(session);
            }
            Console.WriteLine();
            Console.WriteLine("   Backup complete.");
         }
         catch (Exception e)
         {
            Console.WriteLine();
            Console.WriteLine(
               "   Backup failed: {0}", 
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
            case Engine.EventType.BeginBackupEntry:
               String[] units = { "KB", "MB", "GB", "TB" };
               Int64 totalSize = evt.BackupSession.ActualLength / 1024;
               Int64 entrySize = evt.BackupEntry.Length / 1024;
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
            case Engine.EventType.EndBackupEntry:
               Console.WriteLine("done.");
               break;
            case Engine.EventType.BeginBackupCheckpoint:
               Console.Write("Checkpointing...");
               break;
            case Engine.EventType.EndBackupCheckpoint:
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
         else if (++failures <= maxFailures && evt.BackupEntry != null)
         {
            evt.Result = Engine.ErrorResult.Fail;
            retries = 0;
            Console.WriteLine("      Skipping {0} due to error.", evt.BackupEntry.Node.Name);
            Console.WriteLine(
               "         {0}",
               evt.Exception.ToString().Replace("\n", "\n         ")
            );
         }
         else
            evt.Result = Engine.ErrorResult.Abort;
      }

      static void ReportUsage ()
      {
         Console.WriteLine("   Usage: SkyFloe.Backup {options}");
         Console.WriteLine("      -c|-connect {connect}      backup store connection string");
         Console.WriteLine("      -a|-archive {archive}      archive name");
         Console.WriteLine("      -p|-password {password}    archive password");
         Console.WriteLine("      -r|-max-retry {retries}    maximum file retries before skipping (default = 5)");
         Console.WriteLine("      -f|-max-fail {failures}    maximum file failures before aborting (default = 5)");
         Console.WriteLine("      -s|-source {source}        backup source directory (zero or more, default: current)");
         Console.WriteLine("      -k|-delete                 delete the archive before backing up");
         Console.WriteLine("      -d|-diff {diff}            file diff method (Timestamp or Digest) default: Timestamp");
      }
   }
}
