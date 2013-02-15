using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

namespace SkyFloe.Tasks
{
   public abstract class Task : IDisposable
   {
      public CancellationToken Canceler { get; set; }
      public Store.IArchive Archive { get; set; }
      public SymmetricAlgorithm Crypto { get; set; }
      public event EventHandler<Engine.ProgressEventArgs> OnProgress;
      public event EventHandler<Engine.ErrorEventArgs> OnError;

      public virtual void Dispose ()
      {
      }

      public abstract void Execute ();

      protected void ReportProgress (Engine.ProgressEventArgs args)
      {
         if (this.OnProgress != null)
            this.OnProgress(this, args);
      }
      protected Engine.ErrorResult ReportError (String action, Exception e)
      {
         Engine.ErrorEventArgs args = new Engine.ErrorEventArgs()
         {
            Action = action,
            Exception = e,
            Result = Engine.ErrorResult.Abort
         };
         if (this.OnError != null)
            this.OnError(this, args);
         return args.Result;
      }
   }
}
