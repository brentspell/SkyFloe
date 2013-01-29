using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SkyFloe.App.Forms.Stores
{
   public partial class AwsGlacierStorePage : UserControl, IBaseStorePage
   {
      private static readonly Regex AccessKeyRegex = new Regex(@"^$|^[0-9A-Za-z]{20}$");
      private static readonly Regex SecretKeyRegex = new Regex(@"^$|^[0-9A-Za-z/+]{40}$");
      private static readonly Regex BucketRegex = new Regex(@"^$|^[0-9A-Za-z_\-.]{1,255}$");

      public AwsGlacierStorePage ()
      {
         InitializeComponent();
         this.txtAccessKey.DataBindings.Add(new Binding("Text", this.binding, "AccessKey"));
         this.txtSecretKey.DataBindings.Add(new Binding("Text", this.binding, "SecretKey"));
         this.txtBucket.DataBindings.Add(new Binding("Text", this.binding, "Bucket"));
      }

      public event EventHandler PropertyChanged;

      public Object StoreProperties
      {
         get { return this.binding.DataSource; }
         set { this.binding.DataSource = value; }
      }

      private void binding_CurrentItemChanged (Object o, EventArgs a)
      {
         if (this.PropertyChanged != null)
            this.PropertyChanged(this, new EventArgs());
      }

      private void txtAccessKey_Validating (Object o, CancelEventArgs a)
      {
         if (!AccessKeyRegex.IsMatch(txtAccessKey.Text))
            a.Cancel = true;
      }

      private void txtSecretKey_Validating (Object o, CancelEventArgs a)
      {
         if (!SecretKeyRegex.IsMatch(txtSecretKey.Text))
            a.Cancel = true;
      }

      private void txtBucket_Validating (Object o, CancelEventArgs a)
      {
         if (!BucketRegex.IsMatch(txtBucket.Text))
            a.Cancel = true;
      }
   }
}
