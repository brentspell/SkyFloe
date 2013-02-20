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
      public EventHandler<Engine.ProgressEventArgs> OnProgress { get; set; }
      public EventHandler<Engine.ErrorEventArgs> OnError { get; set; }

      public virtual void Dispose ()
      {
      }

      public void Validate ()
      {
         if (this.Canceler == null)
            throw new ArgumentException("Canceler");
         if (this.Archive == null)
            throw new ArgumentException("Archive");
         if (this.Crypto == null)
            throw new ArgumentException("Crypto");
         DoValidate();
      }
      public void Execute ()
      {
         Validate();
         DoExecute();
      }

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
      protected Boolean TryExecute (String actionName, Action action)
      {
         return TryExecute(actionName, () => { action(); return true; });
      }
      protected T TryExecute<T> (String actionName, Func<T> action)
      {
         return TryExecute(actionName, action, default(T));
      }
      protected T TryExecute<T> (String actionName, Func<T> action, T failValue)
      {
         for ( ; ; )
         {
            try
            {
               return action();
            }
            catch (Exception e)
            {
               switch (ReportError(actionName, e))
               {
                  case Engine.ErrorResult.Fail:
                     return failValue;
                  case Engine.ErrorResult.Abort:
                     throw;
               }
            }
         }
      }
      protected void Execute (String actionName, Action action)
      {
         for (; ; )
         {
            try
            {
               action();
               return;
            }
            catch (Exception e)
            {
               if (ReportError(actionName, e) != Engine.ErrorResult.Retry)
                  throw;
            }
         }
      }

      protected abstract void DoValidate ();
      protected abstract void DoExecute ();
   }
}
