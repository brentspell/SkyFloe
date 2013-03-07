//===========================================================================
// MODULE:  TestTask.cs
// PURPOSE: backup task base class unit test
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
// Project References

namespace SkyFloe.Core.Test.Tasks
{
   [TestClass]
   public class TestTask
   {
      [TestMethod]
      public void TestValidation ()
      {
         // invalid validation
         AssertException(new Task().Validate);
         AssertException(
            new Task()
            {
               Archive = new Archive(),
               Crypto = new AesCryptoServiceProvider()
            }.Validate
         );
         AssertException(
            new Task()
            {
               Canceler = new CancellationTokenSource().Token,
               Crypto = new AesCryptoServiceProvider()
            }.Validate
         );
         AssertException(
            new Task()
            {
               Canceler = new CancellationTokenSource().Token,
               Archive = new Archive(),
            }.Validate
         );
         AssertException(
            new Task()
            {
               Canceler = new CancellationTokenSource().Token,
               Archive = new Archive(),
               Crypto = new AesCryptoServiceProvider(),
               FailValidation = true
            }.Validate
         );
         // valid validation
         new Task()
         {
            Canceler = new CancellationTokenSource().Token,
            Archive = new Archive(),
            Crypto = new AesCryptoServiceProvider(),
            FailValidation = false
         }.Validate();
      }

      [TestMethod]
      public void TestExecution ()
      {
         var task = (Task)null;
         var notifyBegin = false;
         var notifyEnd = false;
         var notifyError = false;
         // invalid execution
         AssertException(
            new Task()
            {
               Canceler = new CancellationTokenSource().Token,
               Archive = new Archive(),
               Crypto = new AesCryptoServiceProvider(),
               FailValidation = true
            }.Execute
         );
         // valid execution
         notifyBegin = notifyEnd = false;
         task = new Task()
         {
            Canceler = new CancellationTokenSource().Token,
            Archive = new Archive(),
            Crypto = new AesCryptoServiceProvider()
         };
         task.OnProgress += (o, a) =>
         {
            if (a.Operation == "BeginExecute")
               notifyBegin = true;
            else if (a.Operation == "EndExecute")
               notifyEnd = true;
         };
         task.Execute();
         Assert.IsTrue(task.Result);
         Assert.IsTrue(notifyBegin);
         Assert.IsTrue(notifyEnd);
         // duplicate execution
         notifyBegin = notifyEnd = false;
         AssertException(task.Execute);
         Assert.IsFalse(notifyBegin);
         Assert.IsFalse(notifyEnd);
         // failed execution (default)
         task = new Task()
         {
            Canceler = new CancellationTokenSource().Token,
            Archive = new Archive(),
            Crypto = new AesCryptoServiceProvider(),
            FailExecution = true
         };
         AssertException(task.Execute);
         task.FailExecution = false;
         AssertException(task.Execute);
         Assert.IsFalse(task.Result);
         // failed execution (abort)
         notifyError = false;
         task = new Task()
         {
            Canceler = new CancellationTokenSource().Token,
            Archive = new Archive(),
            Crypto = new AesCryptoServiceProvider(),
            FailExecution = true
         };
         task.OnError += (o, a) => 
         {
            notifyError = true;
            a.Result = ErrorResult.Abort;
         };
         AssertException(task.Execute);
         Assert.IsFalse(task.Result);
         Assert.IsTrue(notifyError);
         // failed execution (retry)
         notifyError = false;
         task = new Task()
         {
            Canceler = new CancellationTokenSource().Token,
            Archive = new Archive(),
            Crypto = new AesCryptoServiceProvider(),
            FailExecution = true
         };
         task.OnError += (o, a) =>
         {
            notifyError = true;
            task.FailExecution = false;
            a.Result = ErrorResult.Retry;
         };
         task.Execute();
         Assert.IsTrue(task.Result);
         Assert.IsTrue(notifyError);
         // failed execution (fail)
         notifyError = false;
         task = new Task()
         {
            Canceler = new CancellationTokenSource().Token,
            Archive = new Archive(),
            Crypto = new AesCryptoServiceProvider(),
            FailExecution = true
         };
         task.OnError += (o, a) =>
         {
            notifyError = true;
            a.Result = ErrorResult.Fail;
         };
         task.Execute();
         Assert.IsFalse(task.Result);
         Assert.IsTrue(notifyError);
         // cancellation
         var canceler = new CancellationTokenSource();
         notifyBegin = notifyEnd = false;
         task = new Task()
         {
            Canceler = canceler.Token,
            Archive = new Archive(),
            Crypto = new AesCryptoServiceProvider()
         };
         task.OnProgress += (o, a) =>
         {
            if (a.Operation == "BeginExecute")
               notifyBegin = true;
            else if (a.Operation == "EndExecute")
               notifyEnd = true;
         };
         canceler.Cancel();
         try
         {
            task.Execute();
            Assert.Fail("Expected: OperationCanceledException");
         }
         catch (OperationCanceledException) { }
         Assert.IsFalse(task.Result);
         Assert.IsTrue(notifyBegin);
         Assert.IsFalse(notifyEnd);
      }

      [TestMethod]
      public void TestRetryable ()
      {
         var opName = "test";
         var result = ErrorResult.Retry;
         var attempt = 0;
         var task = new Task()
         {
            Canceler = new CancellationTokenSource().Token,
            Archive = new Archive(),
            Crypto = new AesCryptoServiceProvider(),
            FailExecution = true
         };
         task.OnError += (o, a) => 
         {
            Assert.AreEqual(opName, a.Action);
            a.Result = result;
         };
         // retry-only
         result = ErrorResult.Abort;
         AssertException(
            () => task.WithRetry(
               opName, 
               () => { throw new InvalidOperationException("test"); }
            )
         );
         result = ErrorResult.Fail;
         AssertException(
            () => task.WithRetry(
               opName,
               () => { throw new InvalidOperationException("test"); }
            )
         );
         result = ErrorResult.Retry;
         attempt = 0;
         task.WithRetry(
            opName,
            () =>
            {
               if (attempt++ < 5)
                  throw new InvalidOperationException("test");
            }
         );
         // boolean retry
         result = ErrorResult.Abort;
         AssertException(
            () => task.TryWithRetry(
               opName,
               () => { throw new InvalidOperationException("test"); }
            )
         );
         result = ErrorResult.Fail;
         Assert.IsFalse(
            task.TryWithRetry(
               opName, 
               () => { throw new InvalidOperationException("test"); }
            )
         );
         result = ErrorResult.Retry;
         attempt = 0;
         Assert.IsTrue(
            task.TryWithRetry(
               opName,
               () =>
               {
                  if (attempt++ < 5)
                     throw new InvalidOperationException("test");
               }
            )
         );
         // typed retry
         result = ErrorResult.Abort;
         AssertException(
            () => Assert.IsNull(
               task.WithRetry<Object>(
                  opName,
                  () => { throw new InvalidOperationException("test"); }
               )
            )
         );
         result = ErrorResult.Fail;
         Assert.IsNull(
            task.WithRetry<Object>(
               opName,
               () => { throw new InvalidOperationException("test"); }
            )
         );
         result = ErrorResult.Retry;
         attempt = 0;
         Assert.IsNotNull(
            task.WithRetry(
               opName,
               () =>
               {
                  if (attempt++ < 5)
                     throw new InvalidOperationException("test");
                  return new Object();
               }
            )
         );
      }

      private void AssertException (Action a)
      {
         try { a(); }
         catch { return; }
         Assert.Fail("Expected: exception");
      }

      private class Task : SkyFloe.Tasks.Task
      {
         public Boolean FailValidation { get; set; }
         public Boolean FailExecution { get; set; }
         public Boolean Result { get; private set; }

         protected override void DoValidate ()
         {
            if (this.FailValidation)
               throw new InvalidOperationException();
         }
         protected override void DoExecute ()
         {
            ReportProgress(
               new ProgressEventArgs()
               {
                  Operation = "BeginExecute"
               }
            );
            this.Canceler.ThrowIfCancellationRequested();
            this.Result = WithRetry(
               "Execution",
               () => 
               {
                  if (this.FailExecution)
                     throw new InvalidOperationException();
                  return true;
               }
            );
            ReportProgress(
               new ProgressEventArgs()
               {
                  Operation = "EndExecute"
               }
            );
         }
         public new Boolean TryWithRetry (String actionName, Action action)
         {
            return base.TryWithRetry(actionName, action);
         }
         public new T WithRetry<T> (String actionName, Func<T> action)
         {
            return base.WithRetry(actionName, action);
         }
         public new void WithRetry (String actionName, Action action)
         {
            base.WithRetry(actionName, action);
         }
      }

      private class Archive : Store.IArchive
      {
         #region IDisposable Implementation
         public void Dispose ()
         {
         }
         #endregion

         #region IArchive Members
         public string Name
         {
            get { throw new NotSupportedException(); }
         }
         public Store.IBackupIndex BackupIndex
         {
            get { throw new NotSupportedException(); }
         }
         public Store.IRestoreIndex RestoreIndex
         {
            get { throw new NotSupportedException(); }
         }
         public Store.IBackup PrepareBackup (Backup.Session session)
         {
            throw new NotSupportedException();
         }
         public Store.IRestore PrepareRestore (Restore.Session session)
         {
            throw new NotSupportedException();
         }
         #endregion
      }
   }
}
