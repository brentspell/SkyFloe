//===========================================================================
// MODULE:  RateLimiter.cs
// PURPOSE: generic synchronous processing rate limiter
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
using System.IO;
using System.Linq;
using System.Threading;
// Project References

namespace SkyFloe.IO
{
   /// <summary>
   /// Synchronous processing rate limiter
   /// </summary>
   /// <remarks>
   /// This class provides support for enforcing a custom data transfer
   /// or processing rate limit using synchronous delay (sleep). Processing
   /// may be specified manually using the Register/RegisterAndThrottle
   /// methods or implicitly through a synchronous stream using 
   /// CreateStreamFilter. The InControl/OutOfControl properties indicate
   /// whether current processing has exceeded the rate limit.
   /// </remarks>
   public class RateLimiter
   {
      private Int32 rateLimit;
      private DateTime started;
      private Int64 currentBytes;
      private Int64 totalBytes;

      /// <summary>
      /// Initializes a new limiter
      /// </summary>
      /// <param name="rateLimit">
      /// The rate limit to set, in bytes/second
      /// </param>
      public RateLimiter (Int32 rateLimit)
      {
         if (rateLimit < 1)
            throw new ArgumentOutOfRangeException("rateLimit");
         this.rateLimit = rateLimit;
         this.started = DateTime.UtcNow;
         this.totalBytes = 0;
      }

      /// <summary>
      /// Indicates whether the current rate is within the limit
      /// </summary>
      public Boolean InControl { get { return GetDelay() == 0; } }
      /// <summary>
      /// Indicates whether the current is not within the limit
      /// </summary>
      public Boolean OutOfControl { get { return GetDelay() > 0; } }

      /// <summary>
      /// Records a data processing/transfer event
      /// </summary>
      /// <param name="bytes">
      /// The number of bytes processed
      /// </param>
      public void Register (Int64 bytes)
      {
         this.currentBytes += bytes;
      }
      /// <summary>
      /// Delays processing if necessary until the limiter is in control
      /// </summary>
      public void Throttle ()
      {
         var delay = GetDelay();
         if (delay > 0)
            Thread.Sleep(delay * 1000);
      }
      /// <summary>
      /// Records a processing event and throttles if necessary
      /// </summary>
      /// <param name="bytes">
      /// The number of bytes processed
      /// </param>
      public void RegisterAndThrottle (Int64 bytes)
      {
         Register(bytes);
         Throttle();
      }
      /// <summary>
      /// Creates a stream that can be used to throttle a
      /// synchronous data transfer
      /// </summary>
      /// <param name="baseStream">
      /// The underlying stream to attach
      /// </param>
      /// <returns>
      /// The rate limiting stream
      /// </returns>
      public Stream CreateStreamFilter (Stream baseStream)
      {
         return new LimiterFilter(baseStream, this);
      }
      /// <summary>
      /// Determines the delay needed bring the processing
      /// operation back into control
      /// </summary>
      /// <returns>
      /// The number of seconds to wait
      /// </returns>
      private Int32 GetDelay ()
      {
         // in order to avoid thrashing the kernel for fine-grained calls, 
         // only retrieve the current time once we have processed at
         // least one second's bytes (or if no throttling has been done yet)
         if (this.currentBytes == 0 || this.currentBytes > this.rateLimit)
         {
            this.totalBytes += this.currentBytes;
            this.currentBytes = 0;
            // calculate the expected number of bytes and
            // return a delay if the actual number is greater
            var duration = (Int32)(DateTime.UtcNow - this.started).TotalSeconds;
            var limitBytes = (Int64)this.rateLimit * duration;
            if (this.totalBytes > limitBytes)
               return (Int32)(this.totalBytes - limitBytes) / this.rateLimit;
         }
         return 0;
      }

      /// <summary>
      /// Rate limiting stream filter
      /// </summary>
      /// <remarks>
      /// This class filters an attached stream, and throttles
      /// stream reads/writes using the attached limiter
      /// </remarks>
      private class LimiterFilter : FilterStream
      {
         private RateLimiter limiter;

         /// <summary>
         /// Initializes a new filter instance
         /// </summary>
         /// <param name="baseStream">
         /// The base stream to filter
         /// </param>
         /// <param name="limiter">
         /// The rate limiter to attach
         /// </param>
         public LimiterFilter (Stream baseStream, RateLimiter limiter)
            : base(baseStream)
         {
            this.limiter = limiter;
         }
         /// <summary>
         /// Processes stream reads/writes, throttling via the rate limiter
         /// </summary>
         /// <param name="buffer">
         /// Read/write buffer
         /// </param>
         /// <param name="offset">
         /// Read/write buffer offset
         /// </param>
         /// <param name="count">
         /// Number of bytes read/written
         /// </param>
         protected override void Filter (Byte [] buffer, Int32 offset, Int32 count)
         {
            this.limiter.RegisterAndThrottle(count);
         }
      }
   }
}
