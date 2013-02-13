using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SkyFloe.Lab
{
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

      public Clock ()
      {
         this.watch = new Stopwatch();
         this.Runs = 0;
         this.MinTicks = Int64.MaxValue;
         this.MaxTicks = Int64.MinValue;
         this.SumTicks = 0;
         this.Sum2Ticks = 0;
      }
      public void Start ()
      {
         this.watch.Start();
      }
      public void Stop ()
      {
         this.watch.Stop();
         Int64 ticks = this.watch.ElapsedTicks;
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

   public static class IEnumerableClockExtensions
   {
      public static Int32 Runs (this IEnumerable<Clock> clocks)
      {
         return clocks.Sum(c => c.Runs);
      }
      public static Double MinTime (this IEnumerable<Clock> clocks)
      {
         return clocks.Min(c => c.MinTime);
      }
      public static Double MaxTime (this IEnumerable<Clock> clocks)
      {
         return clocks.Max(c => c.MaxTime);
      }
      public static Double MeanTime (this IEnumerable<Clock> clocks)
      {
         return (Double)clocks.Sum(c => c.SumTicks) / 
            clocks.Sum(c => c.Runs) / 
            Stopwatch.Frequency;
      }
      public static Double StdDevTime (this IEnumerable<Clock> clocks)
      {
         return Math.Sqrt(
            clocks.Sum(c => c.Runs) * clocks.Sum(c => c.Sum2Ticks) -
            clocks.Sum(c => c.SumTicks) * clocks.Sum(c => c.SumTicks)
         ) / clocks.Sum(c => c.Runs) / Stopwatch.Frequency;
      }
   }
}
