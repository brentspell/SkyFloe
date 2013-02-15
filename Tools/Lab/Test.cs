using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SkyFloe.Lab
{
   public class Test
   {
      public Int32 ThreadID;
      public Int32 Iteration;
      
      static Test ()
      {
      }
      
      public Test ()
      {
      }

      public void Run ()
      {
         var cts = new CancellationTokenSource();
         var ct = cts.Token;
         Task t = Task.Factory.StartNew(() => { Thread.Sleep(1000); ct.ThrowIfCancellationRequested(); }, ct).ContinueWith(t1 => { Thread.Sleep(2000); }, ct);
         t.Wait();
      }
   }
}
