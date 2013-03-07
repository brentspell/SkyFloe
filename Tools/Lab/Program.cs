using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace SkyFloe.Lab
{
   class Program
   {
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
               { "t|threads=", (Int32 v) => Test.Threads = v },
               { "i|iterations=", (Int32 v) => Test.Iterations = v },
               { "p|Test.Param=", v => Test.Param = v }
            }.Parse(options);
            return true;
         }
         catch { return false; }
      }

      static void ReportUsage ()
      {
         Console.WriteLine("   Usage: SkyFloe.Lab {options}");
         Console.WriteLine("      -t|-threads {count}        number of threads to run");
         Console.WriteLine("      -i|-iterations {count}     number of iterations per thread to run");
         Console.WriteLine("      -p|-Test.Param {value}          custom test paramter");
      }

      static void ExecuteTests ()
      {
         Console.WriteLine(
            "Running {0} threads, {1} iterations.",
            Test.Threads,
            Test.Iterations
         );
         Console.WriteLine();
         realClock.Start();
         var threads = new List<Thread>();
         for (var i = 0; i < Test.Threads; i++)
         {
            threads.Add(new Thread(ExecuteThread));
            testClocks.Add(new Clock());
         }
         for (var i = 0; i < Test.Threads; i++)
            threads[i].Start(i);
         for (var i = 0; i < Test.Threads; i++)
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
         var t = (Int32)param;
         var clock = testClocks[t];
         var test = new Test() { ThreadID = t };
         for (test.Iteration = 0; test.Iteration < Test.Iterations; test.Iteration++)
         {
            clock.Start();
            test.Run();
            clock.Stop();
         }
      }
   }
}
