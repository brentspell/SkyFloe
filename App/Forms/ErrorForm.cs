using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SkyFloe.App.Forms
{
   public partial class ErrorForm : Form
   {
      public ErrorForm (Exception exception)
      {
         InitializeComponent();
         BindingList<Exception> exceptions = new BindingList<Exception>();
         for (Exception e = exception; e != null; e = e.InnerException)
            exceptions.Add(e);
         this.binding.DataSource = exceptions;
         this.txtSource.DataBindings.Add(new Binding("Text", this.binding, "Source"));
         this.txtMessage.DataBindings.Add(new Binding("Text", this.binding, "Message"));
         this.txtStackTrace.DataBindings.Add(new Binding("Text", this.binding, "StackTrace"));
      }

      private void binding_PositionChanged (Object sender, EventArgs e)
      {
         this.btnPrev.Enabled = (this.binding.Position > 0);
         this.btnNext.Enabled = (this.binding.Position < this.binding.Count - 1);
         this.txtType.Text = this.binding.Current.GetType().FullName;
      }

      private void btnPrev_Click (object sender, EventArgs e)
      {
         this.binding.MovePrevious();
      }

      private void btnNext_Click (object sender, EventArgs e)
      {
         this.binding.MoveNext();
      }

      private void btnCopy_Click (object sender, EventArgs e)
      {
         Clipboard.SetText(this.binding.Current.ToString());
      }

      private void btnClose_Click (object sender, EventArgs e)
      {
         this.Close();
      }
   }
}
