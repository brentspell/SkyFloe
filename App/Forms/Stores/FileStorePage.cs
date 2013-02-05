using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SkyFloe.App.Forms.Stores
{
   public partial class FileStorePage : UserControl, IBaseStorePage
   {
      public FileStorePage ()
      {
         InitializeComponent();
         this.txtPath.DataBindings.Add(new Binding("Text", this.binding, "Path"));
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

      private void txtPath_TextChanged (Object o, EventArgs a)
      {
         this.Validate();
      }

      private void btnChoosePath_Click (Object o, EventArgs a)
      {
         if (this.folderBrowser.ShowDialog(this) == DialogResult.OK)
            this.txtPath.Text = this.folderBrowser.SelectedPath;
      }
   }
}
