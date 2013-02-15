using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Tpl = System.Threading.Tasks;

namespace SkyFloe.Backup
{
   class Program
   {
      private static String connectionString;
      private static String archiveName;
      private static String password;
      private static Int32 maxRetries;
      private static Int32 maxFailures;
      private static IList<String> sourcePaths;
      private static Boolean deleteArchive;
      private static DiffMethod diffMethod;
      private static Int32 checkpointLength;
      private static Int32 rateLimit;
      private static Int32 retries;
      private static Int32 failures;
      private static Engine engine;
      private static CancellationTokenSource canceler;

      static Int32 Main (String[] args)
      {
         Console.SetBufferSize(Math.Max(1000, Console.BufferWidth), Console.BufferHeight);
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
         checkpointLength = 1024;
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
               { "s|source=", v => sourcePaths.Add(v) },
               { "k|delete", v => deleteArchive = (v != null) },
               { "d|diff=", (DiffMethod v) => diffMethod = v },
               { "t|checkpoint=", (Int32 v) => checkpointLength = v },
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
         if (!sourcePaths.Any())
            sourcePaths.Add(Environment.CurrentDirectory);
         if (sourcePaths.Any(p => String.IsNullOrWhiteSpace(p)))
            return false;
         if (diffMethod == 0)
            return false;
         if (checkpointLength < 1)
            return false;
         if (rateLimit < 1)
            return false;
         return true;
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
         Console.WriteLine("      -k|-delete[+/-]            delete the archive before backing up");
         Console.WriteLine("      -d|-diff {diff}            file diff method (Timestamp or Digest) default: Timestamp");
         Console.WriteLine("      -t|-checkpoint {size}      backup checkpoint interval, in megabytes, default: 1024");
         Console.WriteLine("      -l|-rate {limit}           backup rate limit, in KB/sec, default: unlimited");
      }

      static Boolean ExecuteBackup ()
      {
         Boolean backupOk = false;
         canceler = new CancellationTokenSource();
         try
         {
            Tpl.Task tasks = Tpl.Task.Factory
               .StartNew(() => Connect())
               .ContinueWith<Backup.Session>(t => CreateSession())
               .ContinueWith(t => ExecuteBackup(t.Result));
            while (!tasks.Wait(1000))
            {
               while (Console.KeyAvailable)
               {
                  if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                  {
                     Console.WriteLine();
                     Console.Write("   Canceling...");
                     canceler.Cancel();
                  }
               }
            }
            backupOk = true;
         }
         catch (Exception e)
         {
            Console.WriteLine();
            if (canceler.IsCancellationRequested)
               Console.WriteLine("   Backup canceled.");
            else
               Console.WriteLine(
                  "   Backup failed: {0}",
                  e.ToString().Replace("\n", "\n      ")
               );
         }
         if (Debugger.IsAttached)
         {
            Console.WriteLine();
            Console.Write("Press enter to exit.");
            Console.ReadLine();
         }
         return backupOk;
      }

      static void Connect ()
      {
         Console.Write("   Connecting to archive {0}...", archiveName);
         engine = new Engine()
         {
            Connection = new Connection(connectionString),
            Canceler = canceler.Token
         };
         try
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
         }
         catch
         {
            engine.Dispose();
            throw;
         }
      }

      static Backup.Session CreateSession ()
      {
         Console.WriteLine("   Starting the backup. Press escape to cancel/pause.");
         Backup.Session session = engine.Archive.Sessions
            .FirstOrDefault(s => s.State != SessionState.Completed);
         if (session != null)
         {
            Console.WriteLine(
               "   Resuming backup session started {0:MMM d, yyyy h:m tt}.",
               session.Created
            );
            return session;
         }
         else
         {
            Console.WriteLine("   Creating a new backup session.");
            return engine.CreateBackup(
               new BackupRequest()
               {
                  DiffMethod = diffMethod,
                  Sources = sourcePaths,
                  CheckpointLength = checkpointLength * 1048576,
                  RateLimit = rateLimit * 1024
               }
            );
         }
      }

      static void ExecuteBackup (Backup.Session session)
      {
         engine.ExecuteBackup(session);
         Console.WriteLine("   Backup complete.");
      }

      static void HandleProgress (Object sender, Engine.ProgressEventArgs args)
      {
         if (args.Action == "CreateBackupEntry")
         {
            Console.WriteLine("   Registering {0}.", args.BackupEntry.Node.GetAbsolutePath());
         }
         if (args.Action == "BeginBackupEntry")
         {
            String[] units = { "KB", "MB", "GB", "TB" };
            Int64 totalSize = args.BackupSession.ActualLength / 1024;
            Int64 entrySize = args.BackupEntry.Length / 1024;
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
               args.BackupEntry.Node.GetAbsolutePath()
            );
         }
         else if (args.Action == "EndBackupEntry")
         {
            Console.WriteLine("done.");
         }
         else if (args.Action == "BeginCheckpoint")
         {
            Console.Write("   Checkpointing...");
         }
         else if (args.Action == "EndCheckpoint")
         {
            Console.WriteLine("done.");
         }
         retries = failures = 0;
      }

      static void HandleError (Object sender, Engine.ErrorEventArgs args)
      {
         if (++retries <= maxRetries)
         {
            System.Threading.Thread.Sleep(retries * 1000);
            Console.WriteLine();
            Console.WriteLine("      Retrying...");
            args.Result = Engine.ErrorResult.Retry;
         }
         else if (++failures <= maxFailures && args.Action == "BackupEntry")
         {
            retries = 0;
            Console.WriteLine();
            Console.WriteLine("      Skipping due to error.");
            Console.WriteLine(
               "         {0}",
               args.Exception.ToString().Replace("\n", "\n         ")
            );
            args.Result = Engine.ErrorResult.Fail;
         }
         else
            args.Result = Engine.ErrorResult.Abort;
      }
   }
}
