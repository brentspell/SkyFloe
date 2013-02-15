using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace SkyFloe.IO
{
   public class CancelFilter : FilterStream
   {
      private CancellationToken cancel;

      public CancelFilter (Stream stream, CancellationToken cancel) : base(stream)
      {
         this.cancel = cancel;
      }
      protected override void Filter (Byte[] buffer, Int32 offset, Int32 count)
      {
         this.cancel.ThrowIfCancellationRequested();
      }
   }
}
