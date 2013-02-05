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
   public partial class GenericStorePage : UserControl, IBaseStorePage
   {
      public GenericStorePage ()
      {
         InitializeComponent();
      }

      #region IStorePage Members
      public event EventHandler PropertyChanged;

      public Object StoreProperties
      {
         get { return this.propertyGrid.SelectedObject; }
         set { this.propertyGrid.SelectedObject = value; }
      }
      #endregion

      private void propertyGrid_PropertyValueChanged (Object s, PropertyValueChangedEventArgs e)
      {
         if (this.PropertyChanged != null)
            this.PropertyChanged(this, new EventArgs());
      }
   }
}
