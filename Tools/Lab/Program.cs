//===========================================================================
// MODULE:  Program.cs
// PURPOSE: skyfloe laboratory program
// 
// Copyright © 2013
// Brent M. Spell. All rights reserved.
//
// This library is free software; you can redistribute it and/or modify it 
// under the terms of the GNU Lesser General Public License as published 
// by the Free Software Foundation; either version 3 of the License, or 
// (at your option) any later version. This library is distributed in the 
// hope that it will be useful, but WITHOUT ANY WARRANTY; without even the 
// implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
// See the GNU Lesser General Public License for more details. You should 
// have received a copy of the GNU Lesser General Public License along with 
// this library; if not, write to 
//    Free Software Foundation, Inc. 
//    51 Franklin Street, Fifth Floor 
//    Boston, MA 02110-1301 USA
//===========================================================================
// System References
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
// Project References

namespace SkyFloe.Lab
{
   /// <summary>
   /// SkyFloe laboratory program
   /// </summary>
   /// <remarks>
   /// This program is a simple test scaffold for running experiments and
   /// collecting execution timings.
   /// </remarks>
   class Program
   {
      private static List<Test> tests = new List<Test>();
      private static List<Clock> clocks = new List<Clock>();
      private static Clock realClock = new Clock();

      /// <summary>
      /// Program entry point
      /// </summary>
      /// <param name="options">
      /// Program options
      /// </param>
      /// <returns>
      /// 0 if successful
      /// > 0 otherwise
      /// </returns>
      static Int32 Main (String[] options)
      {
         Console.WriteLine("SkyFloe Laboratory");
         if (!ParseOptions(options))
         {
            ReportUsage();
            return 1;
         }
         ExecuteTests();
         return 0;
      }
      /// <summary>
      /// Parses the command-line options
      /// </summary>
      /// <param name="options">
      /// The program options array
      /// </param>
      /// <returns>
      /// True if the arguments are valid
      /// False otherwise
      /// </returns>
      static Boolean ParseOptions (String[] options)
      {
         // parse options
         try
         {
            new Options.OptionSet()
            {
               { "t|threads=", (Int32 v) => Test.Threads = v },
               { "i|iterations=", (Int32 v) => Test.Iterations = v },
               { "p|Test.Param=", v => Test.Param = v }
            }.Parse(options);
         }
         catch { return false; }
         // validate options
         if (Test.Threads <= 0)
            return false;
         if (Test.Iterations <= 0)
            return false;
         return true;
      }
      /// <summary>
      /// Displays a program usage message
      /// </summary>
      static void ReportUsage ()
      {
         Console.WriteLine("   Usage: SkyFloe-Lab {options}");
         Console.WriteLine("      -t|-threads {count}        number of threads to run");
         Console.WriteLine("      -i|-iterations {count}     number of iterations per thread to run");
         Console.WriteLine("      -p|-Test.Param {value}          custom test paramter");
      }
      /// <summary>
      /// Executes the test methods defined in the Test class
      /// </summary>
      static void ExecuteTests ()
      {
         Console.WriteLine(
            "Running {0} threads, {1} iterations.",
            Test.Threads,
            Test.Iterations
         );
         Console.WriteLine();
         // create the test clocks and threads
         var threads = new List<Thread>();
         for (var i = 0; i < Test.Threads; i++)
         {
            tests.Add(new Test() { ThreadID = i });
            threads.Add(new Thread(ExecuteThread));
            clocks.Add(new Clock());
         }
         // start and wait for the threads
         realClock.Start();
         for (var i = 0; i < Test.Threads; i++)
            threads[i].Start(i);
         for (var i = 0; i < Test.Threads; i++)
            threads[i].Join();
         realClock.Stop();
         // report timing information
         Console.WriteLine();
         Console.WriteLine("Performance:");
         Console.WriteLine("   Runs:  {0,12}", clocks.Runs());
         Console.WriteLine("   Real:  {0,12:0.000} s", realClock.MeanTime);
         Console.WriteLine("   Min:   {0,12:0.000} ms", clocks.MinTime() * 1000);
         Console.WriteLine("   Max:   {0,12:0.000} ms", clocks.MaxTime() * 1000);
         Console.WriteLine("   Mean:  {0,12:0.000} ms", clocks.MeanTime() * 1000);
         Console.WriteLine("   StdDev:{0,12:0.000} ms", clocks.StdDevTime() * 1000);
         if (Debugger.IsAttached)
         {
            Console.WriteLine();
            Console.Write("Press enter to exit.");
            Console.ReadLine();
         }
      }
      /// <summary>
      /// Executes test iterations for a single thread
      /// </summary>
      /// <param name="param">
      /// The local thread identifier
      /// </param>
      static void ExecuteThread (Object param)
      {
         var t = (Int32)param;
         var clock = clocks[t];
         var test = tests[t];
         for (test.Iteration = 0; test.Iteration < Test.Iterations; test.Iteration++)
         {
            clock.Start();
            test.Run();
            clock.Stop();
         }
      }
   }
}
