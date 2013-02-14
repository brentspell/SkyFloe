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
      public Int32 Thread;
      public Int32 Iteration;
      
      static Test ()
      {
      }
      
      public Test ()
      {
      }

      public void Run ()
      {
         CancellationTokenSource cts = new CancellationTokenSource();
         cts.Cancel();
         Task.Factory.StartNew(() => { }, cts.Token).Wait();
      }
   }
}
