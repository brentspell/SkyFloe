using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using Tpl = System.Threading.Tasks;

namespace SkyFloe.Tasks
{
   public abstract class Task : IDisposable
   {
      public CancellationToken Cancel { get; set; }
      public Store.IArchive Archive { get; set; }
      public SymmetricAlgorithm Crypto { get; set; }
      public event Action<Event> OnProgress;
      public event Func<Event, ErrorResult> OnError;

      public virtual void Dispose ()
      {
      }

      public Tpl.Task Start ()
      {
         return Tpl.Task.Factory.StartNew(Execute);
      }

      protected abstract void Execute ();

      protected void ReportProgress (String name, Object context)
      {
         if (this.OnProgress != null)
            this.OnProgress(
               new Event()
               {
                  Name = name,
                  Context = context
               }
            );
      }
      protected ErrorResult ReportError (Exception e)
      {
         ErrorResult result = ErrorResult.Abort;
         if (!this.Cancel.IsCancellationRequested)
            if (this.OnError != null)
               result = this.OnError(
                  new Event()
                  {
                     Name = "Error",
                     Context = e
                  }
               );
         return result;
      }

      public enum ErrorResult
      {
         Abort = 1,
         Retry = 2,
         Fail = 3
      }
      public class Event
      {
         public String Name { get; set; }
         public Object Context { get; set; }
      }
   }
}
