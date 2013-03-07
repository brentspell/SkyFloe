//===========================================================================
// MODULE:  TestRateLimiter.cs
// PURPOSE: rate limiter unit test
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
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
// Project References
using SkyFloe.IO;

namespace SkyFloe.Core.Test.IO
{
   [TestClass]
   public class TestRateLimiter
   {
      [TestMethod]
      public void TestConstruction ()
      {
         var timer = RateLimiter.Timer = new TestTimer();
         // invalid construction
         AssertException(() => new RateLimiter(0));
         AssertException(() => new RateLimiter(-1));
         AssertException(() => new RateLimiter(Int32.MinValue));
         // valid construction
         new RateLimiter(1);
         new RateLimiter(Int32.MaxValue);
      }

      [TestMethod]
      public void TestProperties ()
      {
         var timer = RateLimiter.Timer = new TestTimer();
         var limiter = (RateLimiter)null;
         // control flags
         Assert.IsTrue(new RateLimiter(1).InControl);
         Assert.IsTrue(new RateLimiter(Int32.MaxValue).InControl);
         Assert.IsFalse(new RateLimiter(1).OutOfControl);
         Assert.IsFalse(new RateLimiter(Int32.MaxValue).OutOfControl);
         limiter = new RateLimiter(1);
         Assert.IsTrue(limiter.InControl);
         Assert.IsFalse(limiter.OutOfControl);
         limiter.Process(1);
         Assert.IsFalse(limiter.InControl);
         Assert.IsTrue(limiter.OutOfControl);
      }

      [TestMethod]
      public void TestOperations ()
      {
         var timer = RateLimiter.Timer = new TestTimer();
         var limiter = (RateLimiter)null;
         // no throttling needed
         {
            timer = RateLimiter.Timer = new TestTimer();
            limiter = new RateLimiter(1);
            limiter.Throttle();
            Assert.AreEqual(timer.Seconds, 0);
            for (var i = 0; i < 1000; i++)
            {
               timer.Sleep(1);
               limiter.Process(1);
               limiter.Throttle();
               Assert.AreEqual(timer.Seconds, i + 1);
               Assert.IsTrue(limiter.InControl);
               Assert.IsFalse(limiter.OutOfControl);
            }
         }
         // manual throttling
         {
            timer = RateLimiter.Timer = new TestTimer();
            limiter = new RateLimiter(1);
            for (var i = 0; i < 1000; i++)
            {
               limiter.Process(1);
               Assert.IsFalse(limiter.InControl);
               Assert.IsTrue(limiter.OutOfControl);
               limiter.Throttle();
               Assert.AreEqual(timer.Seconds, i + 1);
               Assert.IsTrue(limiter.InControl);
               Assert.IsFalse(limiter.OutOfControl);
            }
         }
         // automatic throttling
         {
            timer = RateLimiter.Timer = new TestTimer();
            limiter = new RateLimiter(1);
            for (var i = 0; i < 1000; i++)
            {
               limiter.ProcessAndThrottle(1);
               Assert.AreEqual(timer.Seconds, i + 1);
               Assert.IsTrue(limiter.InControl);
               Assert.IsFalse(limiter.OutOfControl);
            }
         }
         // multi-bps throttling
         {
            timer = RateLimiter.Timer = new TestTimer();
            limiter = new RateLimiter(3);
            for (var i = 0; i < 1000; i++)
            {
               limiter.ProcessAndThrottle(1);
               Assert.AreEqual(timer.Seconds, (i + 1) / 3);
               Assert.IsTrue(limiter.InControl);
               Assert.IsFalse(limiter.OutOfControl);
            }
         }
         // multi-byte throttling
         {
            timer = RateLimiter.Timer = new TestTimer();
            limiter = new RateLimiter(3);
            for (var i = 0; i < 1000; i++)
            {
               limiter.ProcessAndThrottle(5);
               Assert.AreEqual(timer.Seconds, 5 * (i + 1) / 3);
               Assert.IsTrue(limiter.InControl);
               Assert.IsFalse(limiter.OutOfControl);
            }
         }
         // stream throttling
         {
            timer = RateLimiter.Timer = new TestTimer();
            limiter = new RateLimiter(3);
            using (var stream = limiter.CreateStreamFilter(new System.IO.MemoryStream()))
            for (var i = 0; i < 1000; i++)
            {
               stream.Write(new Byte[5], 0, 5);
               Assert.AreEqual(timer.Seconds, 5 * (i + 1) / 3);
               Assert.IsTrue(limiter.InControl);
               Assert.IsFalse(limiter.OutOfControl);
            }
         }
      }

      private void AssertException (Action a)
      {
         try { a(); }
         catch { return; }
         Assert.Fail("Expected: exception");
      }

      private class TestTimer : RateLimiter.ITimer
      {
         private Int32 seconds = 0;

         public Int32 Seconds
         {
            get { return this.seconds; }
         }
         public void Sleep (Int32 seconds)
         {
            this.seconds += seconds;
         }
      }
   }
}
