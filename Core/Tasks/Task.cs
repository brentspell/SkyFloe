//===========================================================================
// MODULE:  Task.cs
// PURPOSE: backup/restore task base class
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
using System.Security.Cryptography;
using System.Threading;
// Project References

namespace SkyFloe.Tasks
{
   /// <summary>
   /// Backup engine task base class
   /// </summary>
   /// <remarks>
   /// This class encapsulates the parameters, processing, and error handling 
   /// of a backup operation within the SkyFloe engine. Derivative tasks
   /// need only override the DoValidate and DoExecute methods for custom
   /// task processing. A task may be executed at most one time per instance.
   /// For simplicity, all processing within the task is synchronous. The
   /// client/engine is responsible for running tasks asynchronously for
   /// responsiveness. Tasks may be cancelled at any time using the
   /// attached cancellation token.
   /// </remarks>
   public abstract class Task : IDisposable
   {
      private Boolean executed = false;

      #region Construction/Disposal
      /// <summary>
      /// Initializes a new task instance
      /// </summary>
      public Task ()
      {
      }
      /// <summary>
      /// Releases resources associated with the task
      /// </summary>
      public virtual void Dispose ()
      {
      }
      #endregion

      #region Properties
      /// <summary>
      /// The task cancellation token, for cancelling synchronous
      /// operations on the client
      /// </summary>
      public CancellationToken Canceler { get; set; }
      /// <summary>
      /// The backup archive being processed
      /// </summary>
      public Store.IArchive Archive { get; set; }
      /// <summary>
      /// The encryption algorithm used for archive processing
      /// </summary>
      public SymmetricAlgorithm Crypto { get; set; }
      /// <summary>
      /// Task progress event handler
      /// </summary>
      public EventHandler<Engine.ProgressEventArgs> OnProgress { get; set; }
      /// <summary>
      /// Task error handler
      /// </summary>
      public EventHandler<Engine.ErrorEventArgs> OnError { get; set; }
      #endregion

      #region Operations
      /// <summary>
      /// Task validation
      /// </summary>
      public void Validate ()
      {
         if (!this.Canceler.CanBeCanceled)
            throw new ArgumentException("Canceler");
         if (this.Archive == null)
            throw new ArgumentException("Archive");
         if (this.Crypto == null)
            throw new ArgumentException("Crypto");
         DoValidate();
      }
      /// <summary>
      /// Task execution
      /// </summary>
      public void Execute ()
      {
         if (this.executed)
            throw new InvalidOperationException("TODO: task already executed");
         Validate();
         this.executed = true;
         DoExecute();
      }
      #endregion

      #region Events
      /// <summary>
      /// Dispatches a progress event to the client
      /// </summary>
      /// <param name="args">
      /// Progress event parameters
      /// </param>
      protected void ReportProgress (Engine.ProgressEventArgs args)
      {
         if (this.OnProgress != null)
            this.OnProgress(this, args);
      }
      /// <summary>
      /// Dispatches an error event to the client
      /// </summary>
      /// <param name="action">
      /// The name of the action that faulted
      /// </param>
      /// <param name="e">
      /// The exception associated with the fault
      /// </param>
      /// <returns>
      /// An error code indicating the action that the task should take
      /// (retry the operation, fail the operation, or abort the task)
      /// </returns>
      protected Engine.ErrorResult ReportError (String action, Exception e)
      {
         var args = new Engine.ErrorEventArgs()
         {
            Action = action,
            Exception = e,
            Result = Engine.ErrorResult.Abort
         };
         if (this.OnError != null)
            this.OnError(this, args);
         return args.Result;
      }
      #endregion

      #region Retry Helpers
      /// <summary>
      /// Attempts a retryable operation, notifying the client 
      /// of any failures that occur
      /// </summary>
      /// <param name="opName">
      /// The name of the action (for notifications)
      /// </param>
      /// <param name="op">
      /// The action to execute
      /// </param>
      /// <returns>
      /// True if the action eventually executed successfully
      /// False if the user chose to fail the operation
      /// Throws if the user chose to abort the task
      /// </returns>
      protected Boolean TryWithRetry (String opName, Action op)
      {
         return WithRetry(opName, () => { op(); return true; });
      }
      /// <summary>
      /// Attempts a retryable operation with a typed result, 
      /// notifying the client of any failures that occur
      /// </summary>
      /// <param name="opName">
      /// The name of the action (for notifications)
      /// </param>
      /// <param name="op">
      /// The action to execute
      /// </param>
      /// <returns>
      /// The result of the operation if successful
      /// default(T) if the user chose to fail the operation
      /// Throws if the user chose to abort the task
      /// </returns>
      protected T WithRetry<T> (String opName, Func<T> op)
      {
         return WithRetry(opName, op, default(T));
      }
      /// <summary>
      /// Attempts a retryable operation with a typed result, 
      /// notifying the client of any failures that occur
      /// </summary>
      /// <param name="opName">
      /// The name of the action (for notifications)
      /// </param>
      /// <param name="op">
      /// The action to execute
      /// </param>
      /// <param name="failValue">
      /// The value to return in case the user chooses to fail the op
      /// </param>
      /// <returns>
      /// The result of the operation if successful
      /// failValue if the user chose to fail the operation
      /// Throws if the user chose to abort the task
      /// </returns>
      protected T WithRetry<T> (String opName, Func<T> op, T failValue)
      {
         for ( ; ; )
         {
            try
            {
               return op();
            }
            catch (Exception e)
            {
               switch (ReportError(opName, e))
               {
                  case Engine.ErrorResult.Retry:
                     break;
                  case Engine.ErrorResult.Fail:
                     return failValue;
                  default:
                     throw;
               }
            }
         }
      }
      /// <summary>
      /// Attempts a retryable operation, notifying the client
      /// of any failures that occur; fails the task if the user 
      /// chooses to fail the operation
      /// </summary>
      /// <param name="opName">
      /// The name of the action (for notifications)
      /// </param>
      /// <param name="op">
      /// The action to execute
      /// </param>
      protected void WithRetry (String opName, Action op)
      {
         for (; ; )
         {
            try
            {
               op();
               break;
            }
            catch (Exception e)
            {
               if (ReportError(opName, e) != Engine.ErrorResult.Retry)
                  throw;
            }
         }
      }
      #endregion

      #region Task Overrides
      /// <summary>
      /// Task validation override
      /// </summary>
      protected abstract void DoValidate ();
      /// <summary>
      /// Task execution override
      /// </summary>
      protected abstract void DoExecute ();
      #endregion
   }
}
