using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SkyFloe.App.Forms
{
   public partial class MainForm : Form
   {
      public MainForm ()
      {
         InitializeComponent();
         this.WindowState = Program.Settings.WindowState;
         if (this.WindowState != FormWindowState.Maximized)
         {
            this.Location = Program.Settings.WindowLocation;
            this.Size = Program.Settings.WindowSize;
         }
      }

      #region Form Events
      private void MainForm_Move (object sender, EventArgs e)
      {
         if (this.WindowState == FormWindowState.Normal)
            Program.Settings.WindowLocation = this.Location;
      }

      private void MainForm_Resize (object sender, EventArgs e)
      {
         Program.Settings.WindowState = this.WindowState;
         if (this.WindowState == FormWindowState.Normal)
            Program.Settings.WindowSize = this.Size;
      }
      #endregion
   }
}
