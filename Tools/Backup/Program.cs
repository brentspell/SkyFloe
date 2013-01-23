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

      static void Main (String[] args)
      {
         Console.WriteLine("SkyFloe Backup");
         if (ParseArguments(args))
            ExecuteBackup();
         else
            ReportUsage();
      }

      static void ExecuteBackup ()
      {
         retries = 0;
         failures = 0;
         try
         {
            Console.Write("   Connecting to archive {0}...", archiveName);
            Engine engine = new Engine()
            {
               Connection = new Connection(connectionString)
            };
            engine.OnProgress += ReportProgress;
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
               Console.WriteLine("   Creating a new backup session.");
               session = engine.CreateBackup(
                  new BackupRequest()
                  {
                     DiffMethod = diffMethod,
                     Sources = sourcePaths
                  }
               );
            }
            engine.StartBackup(session);
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
         }
      }

      static Boolean ParseArguments (String[] args)
      {
         // initialize arguments
         password = "";
         diffMethod = DiffMethod.Timestamp;
         sourcePaths = new List<String>();
         deleteArchive = false;
         maxRetries = 5;
         maxFailures = 5;
         // parse arguments
         for (Int32 i = 0; i < args.Length; i++)
         {
            String arg = args[i].ToLower();
            String val = (i < args.Length - 1) ? args[++i] : null;
            if (arg[0] != '/' && arg[0] != '-')
               return false;
            switch (Char.ToLower(arg[1]))
            {
               case 'a':
                  archiveName = val;
                  break;
               case 'c':
                  connectionString = val;
                  break;
               case 'd':
                  if (!Enum.TryParse<DiffMethod>(val, true, out diffMethod))
                     return false;
                  break;
               case 'f':
                  if (!Int32.TryParse(val, out maxFailures))
                     return false;
                  break;
               case 'k':
                  deleteArchive = true;
                  // TODO: refactor
                  if (i < args.Length - 1)
                     i--;
                  break;
               case 'p':
                  password = val;
                  break;
               case 'r':
                  if (!Int32.TryParse(val, out maxRetries))
                     return false;
                  break;
               case 's':
                  sourcePaths.Add(val);
                  break;
               default:
                  return false;
            }
         }
         // validate arguments
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

      static void HandleError (Engine.ErrorEvent evt)
      {
         if (++retries <= maxRetries)
         {
            System.Threading.Thread.Sleep(retries * 1000);
            evt.Result = Engine.ErrorResult.Retry;
            Console.WriteLine("      Retrying...");
         }
         else if (++failures <= maxFailures && evt.Entry != null)
         {
            evt.Result = Engine.ErrorResult.Fail;
            retries = 0;
            Console.WriteLine("      Skipping {0} due to error.", evt.Entry.Node.Name);
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
         Console.WriteLine("      -a {archive}     archive name");
         Console.WriteLine("      -c {connect}     backup store connection string");
         Console.WriteLine("      -d {diff}        file diff method (Timestamp or Digest) default: Timestamp");
         Console.WriteLine("      -f {failures}    maximum file failures before aborting (default = 5)");
         Console.WriteLine("      -k               delete the archive before backing up");
         Console.WriteLine("      -p {password}    archive password");
         Console.WriteLine("      -r {retries}     maximum file retries before skipping (default = 5)");
         Console.WriteLine("      -s {source}      backup source directory (zero or more, default: current)");
      }

      static void ReportProgress (Engine.ProgressEvent evt)
      {
         retries = failures = 0;
         Console.WriteLine(
            String.Format(
               "   {0:MM/dd hh:mm}: Total: {1:0} MB, Current: {2} KB - {3}",
               DateTime.Now,
               evt.Entry.Session.ActualLength / 1048576,
               evt.Entry.Length / 1024,
               evt.Entry.Node.Name
            )
         );
      }
   }
}
