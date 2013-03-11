//===========================================================================
// MODULE:  Clock.cs
// PURPOSE: laboratory timer
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
// Project References

namespace SkyFloe.Lab
{
   /// <summary>
   /// Test timer
   /// </summary>
   /// <remarks>
   /// This class wraps the CLR StopWatch class, adding functionality for 
   /// calculating running statistics.
   /// </remarks>
   public class Clock
   {
      private Stopwatch watch;
      public Int32 Runs { get; private set; }
      public Int64 MinTicks { get; private set; }
      public Int64 MaxTicks { get; private set; }
      public Int64 SumTicks { get; private set; }
      public Int64 Sum2Ticks { get; private set; }
      public Double MinTime
      {
         get { return (Double)this.MinTicks / Stopwatch.Frequency; }
      }
      public Double MaxTime
      {
         get { return (Double)this.MaxTicks / Stopwatch.Frequency; }
      }
      public Double MeanTime
      {
         get { return (Double)this.MinTicks / Runs / Stopwatch.Frequency; }
      }
      public Double StdDevTime
      {
         get
         { 
            return Math.Sqrt(
               this.Runs * this.Sum2Ticks - 
               this.SumTicks * this.SumTicks
            ) / this.Runs / Stopwatch.Frequency; 
         }
      }

      /// <summary>
      /// Initializes a new clock instance
      /// </summary>
      public Clock ()
      {
         this.watch = new Stopwatch();
         this.Runs = 0;
         this.MinTicks = Int64.MaxValue;
         this.MaxTicks = Int64.MinValue;
         this.SumTicks = 0;
         this.Sum2Ticks = 0;
      }
      /// <summary>
      /// Starts a new watch instance
      /// </summary>
      public void Start ()
      {
         this.watch.Start();
      }
      /// <summary>
      /// Stops the current watch instance, and records summary statistics
      /// </summary>
      public void Stop ()
      {
         this.watch.Stop();
         var ticks = this.watch.ElapsedTicks;
         this.Runs++;
         if (ticks < this.MinTicks)
            this.MinTicks = ticks;
         if (ticks > this.MaxTicks)
            this.MaxTicks = ticks;
         this.SumTicks += ticks;
         this.Sum2Ticks += ticks * ticks;
         this.watch.Reset();
      }
   }

   /// <summary>
   /// Clock list extension methods
   /// </summary>
   public static class IEnumerableClockExtensions
   {
      /// <summary>
      /// Totals up all runs across a set of clocks
      /// </summary>
      /// <param name="clocks">
      /// clock list
      /// </param>
      /// <returns>
      /// The total number of runs
      /// </returns>
      public static Int32 Runs (this IEnumerable<Clock> clocks)
      {
         return clocks.Sum(c => c.Runs);
      }
      /// <summary>
      /// Calculates the shortest run across a list of clocks
      /// </summary>
      /// <param name="clocks">
      /// clock list
      /// </param>
      /// <returns>
      /// The shortest run time
      /// </returns>
      public static Double MinTime (this IEnumerable<Clock> clocks)
      {
         return clocks.Min(c => c.MinTime);
      }
      /// <summary>
      /// Calculates the longest run across a list of clocks
      /// </summary>
      /// <param name="clocks">
      /// clock list
      /// </param>
      /// <returns>
      /// The longest run time
      /// </returns>
      public static Double MaxTime (this IEnumerable<Clock> clocks)
      {
         return clocks.Max(c => c.MaxTime);
      }
      /// <summary>
      /// Calculates the mean run across a list of clocks
      /// </summary>
      /// <param name="clocks">
      /// clock list
      /// </param>
      /// <returns>
      /// The mean run time
      /// </returns>
      public static Double MeanTime (this IEnumerable<Clock> clocks)
      {
         return (Double)clocks.Sum(c => c.SumTicks) / 
            clocks.Sum(c => c.Runs) / 
            Stopwatch.Frequency;
      }
      /// <summary>
      /// Calculates the population standard deviation across a list of clocks
      /// </summary>
      /// <param name="clocks">
      /// clock list
      /// </param>
      /// <returns>
      /// The standard deviation of the run times
      /// </returns>
      public static Double StdDevTime (this IEnumerable<Clock> clocks)
      {
         return Math.Sqrt(
            clocks.Sum(c => c.Runs) * clocks.Sum(c => c.Sum2Ticks) -
            clocks.Sum(c => c.SumTicks) * clocks.Sum(c => c.SumTicks)
         ) / clocks.Sum(c => c.Runs) / Stopwatch.Frequency;
      }
   }
}
