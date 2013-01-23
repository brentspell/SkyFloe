namespace SkyFloe.App.Forms
{
   partial class MainForm
   {
      /// <summary>
      /// Required designer variable.
      /// </summary>
      private System.ComponentModel.IContainer components = null;

      /// <summary>
      /// Clean up any resources being used.
      /// </summary>
      /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
      protected override void Dispose (bool disposing)
      {
         if (disposing && (components != null))
         {
            components.Dispose();
         }
         base.Dispose(disposing);
      }

      #region Windows Form Designer generated code

      /// <summary>
      /// Required method for Designer support - do not modify
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent ()
      {
         this.menuBar = new System.Windows.Forms.MenuStrip();
         this.statusBar = new System.Windows.Forms.StatusStrip();
         this.SuspendLayout();
         // 
         // menuBar
         // 
         this.menuBar.Location = new System.Drawing.Point(0, 0);
         this.menuBar.Name = "menuBar";
         this.menuBar.Size = new System.Drawing.Size(792, 24);
         this.menuBar.TabIndex = 0;
         // 
         // statusBar
         // 
         this.statusBar.Location = new System.Drawing.Point(0, 551);
         this.statusBar.Name = "statusBar";
         this.statusBar.Size = new System.Drawing.Size(792, 22);
         this.statusBar.TabIndex = 0;
         // 
         // MainForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(792, 573);
         this.Controls.Add(this.statusBar);
         this.Controls.Add(this.menuBar);
         this.Location = new System.Drawing.Point(112, 84);
         this.MainMenuStrip = this.menuBar;
         this.MinimumSize = new System.Drawing.Size(800, 600);
         this.Name = "MainForm";
         this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
         this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
         this.Text = "SkyFloe Backup/Restore";
         this.Move += new System.EventHandler(this.MainForm_Move);
         this.Resize += new System.EventHandler(this.MainForm_Resize);
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion

      private System.Windows.Forms.MenuStrip menuBar;
      private System.Windows.Forms.StatusStrip statusBar;
   }
}

