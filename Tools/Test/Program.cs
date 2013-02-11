using System;
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
         new Options.OptionSet()
         {
         }.Parse(args);
         Stopwatch watch = new Stopwatch();
         watch.Start();
         //------------------------------------------------------------------
         //------------------------------------------------------------------
         watch.Stop();
         Console.WriteLine("Duration: {0:0.000}secs", (Double)watch.ElapsedMilliseconds / 1000);
         Console.Write("Press enter to exit.");
         Console.ReadLine();
      }
   }
}
