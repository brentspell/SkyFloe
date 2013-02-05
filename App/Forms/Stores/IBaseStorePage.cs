using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SkyFloe.App.Forms.Stores
{
   public interface IBaseStorePage
   {
      event EventHandler PropertyChanged;
      Object StoreProperties { get; set; }
   }
}
