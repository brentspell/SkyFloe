using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SkyFloe.App
{
   static class Program
   {
      public static Properties.Settings Settings
      {
         get { return Properties.Settings.Default; }
      }

      [STAThread]
      static void Main ()
      {
         Application.EnableVisualStyles();
         Application.SetCompatibleTextRenderingDefault(false);
         Application.Run(new Forms.MainForm());
         Settings.Save();
      }
   }
}
