using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Tpl = System.Threading.Tasks;

namespace SkyFloe.App.Forms
{
   public partial class WaitForm : Form
   {
      Tpl.Task task;       // task to execute while displaying the wait
      Boolean completed;   // task is complete?

      #region Construction/Disposal
      /// <summary>
      /// Initializes a new form instance
      /// </summary>
      /// <param name="task">
      /// The running async task to wait for
      /// </param>
      /// <param name="message">
      /// The message to display on the wait form
      /// </param>
      public WaitForm (Tpl.Task task, String message = null)
      {
         InitializeComponent();
         // set the progress message and save the task instance
         if (!String.IsNullOrEmpty(message))
            lblMessage.Text = message;
         this.task = task;
      }
      /// <summary>
      /// Initializes a new form instance
      /// </summary>
      /// <param name="asyncAction">
      /// The action to execute and wait for
      /// </param>
      /// <param name="message">
      /// The message to display on the wait form
      /// </param>
      public WaitForm (Action asyncAction, String message = null)
         : this(Tpl.Task.Factory.StartNew(asyncAction, Tpl.TaskCreationOptions.LongRunning), message)
      {
      }
      #endregion

      #region Event Handlers
      private void WaitForm_Load (Object sender, EventArgs e)
      {
         // set up a continuation task to close the form
         // when the attached task completes
         SynchronizationContext sync = SynchronizationContext.Current;
         if (this.task != null)
            this.task.ContinueWith(
               t => sync.Post(
                  o =>
                  {
                     this.completed = true;
                     this.Close();
                  },
                  null
               )
            );
         this.task = null;
      }
      private void WaitForm_FormClosing (Object sender, FormClosingEventArgs e)
      {
         if (!this.completed)
            e.Cancel = true;
      }
      #endregion
   }
}
