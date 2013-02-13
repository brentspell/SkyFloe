﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace SkyFloe.IO
{
   public class RateLimiter
   {
      private Int32 rateLimit;
      private DateTime started;
      private Int64 currentBytes;
      private Int64 totalBytes;

      public RateLimiter (Int32 rateLimit)
      {
         if (rateLimit < 1)
            throw new ArgumentOutOfRangeException("rateLimit");
         this.rateLimit = rateLimit;
         this.started = DateTime.UtcNow;
         this.totalBytes = 0;
      }

      public Boolean InControl { get { return GetDelay() == 0; } }
      public Boolean OutOfControl { get { return GetDelay() > 0; } }

      public void Register (Int64 bytes)
      {
         this.currentBytes += bytes;
      }
      
      public void Throttle ()
      {
         Int32 delay = GetDelay();
         if (delay > 0)
            Thread.Sleep(delay * 1000);
      }

      public void RegisterAndThrottle (Int64 bytes)
      {
         Register(bytes);
         Throttle();
      }

      public Stream CreateStream (Stream baseStream, StreamMode mode)
      {
         return new LimiterStream(baseStream, mode, this);
      }

      private Int32 GetDelay ()
      {
         if (this.currentBytes == 0 || this.currentBytes > this.rateLimit)
         {
            this.totalBytes += this.currentBytes;
            this.currentBytes = 0;
            Int32 duration = (Int32)(DateTime.UtcNow - this.started).TotalSeconds;
            Int64 limitBytes = (Int64)this.rateLimit * duration;
            if (totalBytes > limitBytes)
               return (Int32)((totalBytes - limitBytes) / this.rateLimit);
         }
         return 0;
      }

      private class LimiterStream : SequentialStream
      {
         private Stream stream;
         private RateLimiter limiter;

         public LimiterStream (Stream stream, StreamMode mode, RateLimiter limiter)
            : base(mode)
         {
            this.stream = stream;
            this.limiter = limiter;
         }
         protected override void Dispose (Boolean disposing)
         {
            base.Dispose(disposing);
            if (this.stream != null)
               this.stream.Dispose();
            this.stream = null;
         }
         public override Int32 Read (Byte[] buffer, Int32 offset, Int32 count)
         {
            if (!this.CanRead)
               throw new InvalidOperationException();
            Int32 read = this.stream.Read(buffer, offset, count);
            this.limiter.RegisterAndThrottle(read);
            return read;
         }
         public override void Write (Byte[] buffer, Int32 offset, Int32 count)
         {
            if (!this.CanWrite)
               throw new InvalidOperationException();
            this.stream.Write(buffer, offset, count);
            this.limiter.RegisterAndThrottle(count);
         }
         public override void Flush ()
         {
            this.stream.Flush();
         }
      }
   }
}