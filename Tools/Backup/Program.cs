using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
      private static IList<IO.Path> sourcePaths;
      private static IList<Regex> includeFilter;
      private static IList<Regex> excludeFilter;
      private static Boolean deleteArchive;
      private static DiffMethod diffMethod;
      private static Int32 checkpointLength;
      private static Int32 rateLimit;
      private static Boolean compress;
      private static Int32 retries;
      private static Int32 failures;
      private static Engine engine;
      private static CancellationTokenSource canceler;

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
         sourcePaths = new List<IO.Path>();
         includeFilter = new List<Regex>();
         excludeFilter = new List<Regex>();
         deleteArchive = false;
         maxRetries = 5;
         maxFailures = 5;
         checkpointLength = 1024;
         rateLimit = Int32.MaxValue / 1024;
         compress = false;
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
               { "s|source=", v => sourcePaths.Add((IO.Path)v) },
               { "n|include=", v => includeFilter.Add(new Regex(v, RegexOptions.IgnoreCase)) },
               { "x|exclude=", v => excludeFilter.Add(new Regex(v, RegexOptions.IgnoreCase)) },
               { "k|delete", v => deleteArchive = (v != null) },
               { "d|diff=", (DiffMethod v) => diffMethod = v },
               { "t|checkpoint=", (Int32 v) => checkpointLength = v },
               { "l|rate=", (Int32 v) => rateLimit = v },
               { "z|compress", v => compress = (v != null) },
            }.Parse(args);
         }
         catch { return false; }
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
            sourcePaths.Add(IO.Path.Current);
         if (sourcePaths.Any(p => String.IsNullOrWhiteSpace(p)))
            return false;
         if (diffMethod == 0)
            return false;
         if (checkpointLength <= 0)
            return false;
         if (rateLimit <= 0)
            return false;
         return true;
      }

      static void ReportUsage ()
      {
         Console.WriteLine("   Usage: SkyFloe-Backup {options}");
         Console.WriteLine("      -c|-connect {connect}      backup store connection string");
         Console.WriteLine("      -a|-archive {archive}      archive name");
         Console.WriteLine("      -p|-password {password}    archive password");
         Console.WriteLine("      -r|-max-retry {retries}    maximum file retries before skipping (default = 5)");
         Console.WriteLine("      -f|-max-fail {failures}    maximum file failures before aborting (default = 5)");
         Console.WriteLine("      -s|-source {source}        backup source directory (zero or more, default: current)");
         Console.WriteLine("      -n|-include {regex}        source path inclusion filter regular expression");
         Console.WriteLine("      -x|-exclude {regex}        source path exclusion filter regular expression");
         Console.WriteLine("      -k|-delete[+/-]            delete the archive before backing up (default: false)");
         Console.WriteLine("      -d|-diff {diff}            file diff method (Timestamp or Digest) default: Timestamp");
         Console.WriteLine("      -t|-checkpoint {size}      backup checkpoint interval, in megabytes, default: 1024");
         Console.WriteLine("      -l|-rate {limit}           backup rate limit, in KB/sec, default: unlimited");
         Console.WriteLine("      -z|-compress[+/-]          enable backup compression (default: false)");
      }

      static Boolean ExecuteBackup ()
      {
         var backupOk = false;
         canceler = new CancellationTokenSource();
         if (Debugger.IsAttached)
            Console.SetBufferSize(1000, 1000);
         try
         {
            var tasks = Tpl.Task.Factory.StartNew(
               () =>
               {
                  Connect();
                  Backup.Session session = CreateSession();
                  ExecuteBackup(session);
               }
            );
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
         var session = engine.Archive.Sessions
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
                  Filter = new RegexFilter()
                  {
                     Include = includeFilter,
                     Exclude = excludeFilter
                  },
                  CheckpointLength = checkpointLength * 1048576,
                  RateLimit = rateLimit * 1024,
                  Compress = compress
               }
            );
         }
      }

      static void ExecuteBackup (Backup.Session session)
      {
         engine.ExecuteBackup(session);
         Console.WriteLine("   Backup complete.");
      }

      static void HandleProgress (Object sender, ProgressEventArgs args)
      {
         if (args.Operation == "CreateBackupEntry")
         {
            Console.WriteLine("   Adding {0}.", args.BackupEntry.Node.GetAbsolutePath());
         }
         if (args.Operation == "BeginBackupEntry")
         {
            Console.Write(
               "   {0:MM/dd hh:mm}: Total: {1}, Current: {2} - {3}...",
               DateTime.Now,
               FormatLength(args.BackupSession.ActualLength),
               FormatLength(args.BackupEntry.Length),
               args.BackupEntry.Node.GetAbsolutePath()
            );
         }
         else if (args.Operation == "EndBackupEntry")
         {
            Console.WriteLine("done.");
         }
         else if (args.Operation == "BeginCheckpoint")
         {
            Console.Write("   Checkpointing...");
         }
         else if (args.Operation == "EndCheckpoint")
         {
            Console.WriteLine("done.");
         }
         retries = failures = 0;
      }

      static void HandleError (Object sender, ErrorEventArgs args)
      {
         if (++retries <= maxRetries)
         {
            System.Threading.Thread.Sleep(retries * 1000);
            Console.WriteLine();
            Console.WriteLine("      Retrying...");
            args.Result = ErrorResult.Retry;
         }
         else if (++failures <= maxFailures)
         {
            retries = 0;
            Console.WriteLine();
            Console.WriteLine("      Skipping due to error.");
            Console.WriteLine(
               "         {0}",
               args.Exception.ToString().Replace("\n", "\n         ")
            );
            args.Result = ErrorResult.Fail;
         }
         else
            args.Result = ErrorResult.Abort;
      }

      static String FormatLength (Int64 bytes)
      {
         var units = new[] { "B", "KB", "MB", "GB", "TB" };
         var unit = 0;
         var norm = (Double)bytes;
         while (norm >= 1000 & unit < units.Length)
         {
            norm /= 1024;
            unit++;
         }
         var format = new StringBuilder();
         format.Append("{0:#,0");
         if (unit > 0 && norm < 10)
            format.Append(".00");
         else if (unit > 0 && norm < 100)
            format.Append(".0");
         format.Append(" ");
         format.Append(units[unit]);
         format.Append("}");
         return String.Format(format.ToString(), norm);
      }
   }
}
