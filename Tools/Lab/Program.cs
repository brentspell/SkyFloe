using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace SkyFloe.Lab
{
   class Program
   {
      static Int32 threadCount = 1;
      static Int32 iterationCount = 1;
      static String param = "";
      static List<Clock> testClocks = new List<Clock>();
      static Clock realClock = new Clock();

      static Int32 Main (String[] args)
      {
         Console.WriteLine("SkyFloe Laboratory");
         if (!ParseOptions(args))
         {
            ReportUsage();
            return 1;
         }
         ExecuteTests();
         return 0;
      }

      static Boolean ParseOptions (String[] options)
      {
         try
         {
            new Options.OptionSet()
            {
               { "t|threads=", (Int32 v) => threadCount = v },
               { "i|iterations=", (Int32 v) => iterationCount = v },
               { "p|param=", v => param = v }
            }.Parse(options);
         }
         catch (Options.OptionException)
         {
            return false;
         }
         return true;
      }

      static void ReportUsage ()
      {
         Console.WriteLine("   Usage: SkyFloe.Test {options}");
         Console.WriteLine("      -t|-threads {count}        number of threads to run");
         Console.WriteLine("      -i|-iterations {count}     number of iterations per thread to run");
         Console.WriteLine("      -p|-param {value}          custom test paramter");
      }

      static void ExecuteTests ()
      {
         Console.WriteLine(
            "Running {0} threads, {1} iterations.",
            threadCount,
            iterationCount
         );
         Console.WriteLine();
         realClock.Start();
         List<Thread> threads = new List<Thread>();
         for (Int32 i = 0; i < threadCount; i++)
         {
            threads.Add(new Thread(ExecuteThread));
            testClocks.Add(new Clock());
         }
         for (Int32 i = 0; i < threadCount; i++)
            threads[i].Start(i);
         for (Int32 i = 0; i < threadCount; i++)
            threads[i].Join();
         realClock.Stop();
         Console.WriteLine();
         Console.WriteLine("Performance:");
         Console.WriteLine("   Runs:  {0,12}", testClocks.Runs());
         Console.WriteLine("   Real:  {0,12:0.000} s", realClock.MeanTime);
         Console.WriteLine("   Min:   {0,12:0.000} ms", testClocks.MinTime() * 1000);
         Console.WriteLine("   Max:   {0,12:0.000} ms", testClocks.MaxTime() * 1000);
         Console.WriteLine("   Mean:  {0,12:0.000} ms", testClocks.MeanTime() * 1000);
         Console.WriteLine("   StdDev:{0,12:0.000} ms", testClocks.StdDevTime() * 1000);
         if (Debugger.IsAttached)
         {
            Console.WriteLine();
            Console.Write("Press enter to exit.");
            Console.ReadLine();
         }
      }

      static void ExecuteThread (Object param)
      {
         Int32 t = (Int32)param;
         Clock clock = testClocks[t];
         Test test = new Test() { Thread = t };
         for (test.Iteration = 0; test.Iteration < iterationCount; test.Iteration++)
         {
            clock.Start();
            test.Run();
            clock.Stop();
         }
      }
   }
}
