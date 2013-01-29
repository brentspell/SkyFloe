namespace SkyFloe.App.Forms
{
   partial class WaitForm
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
         this.lblMessage = new System.Windows.Forms.Label();
         this.progressBar = new System.Windows.Forms.ProgressBar();
         this.SuspendLayout();
         // 
         // lblMessage
         // 
         this.lblMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.lblMessage.AutoSize = true;
         this.lblMessage.Location = new System.Drawing.Point(3, 8);
         this.lblMessage.MinimumSize = new System.Drawing.Size(200, 0);
         this.lblMessage.Name = "lblMessage";
         this.lblMessage.Size = new System.Drawing.Size(200, 13);
         this.lblMessage.TabIndex = 0;
         this.lblMessage.Text = "Please wait...";
         this.lblMessage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
         this.lblMessage.UseWaitCursor = true;
         // 
         // progressBar
         // 
         this.progressBar.Dock = System.Windows.Forms.DockStyle.Bottom;
         this.progressBar.Location = new System.Drawing.Point(3, 27);
         this.progressBar.Margin = new System.Windows.Forms.Padding(3, 10, 3, 10);
         this.progressBar.MarqueeAnimationSpeed = 80;
         this.progressBar.Name = "progressBar";
         this.progressBar.Size = new System.Drawing.Size(200, 12);
         this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
         this.progressBar.TabIndex = 1;
         this.progressBar.UseWaitCursor = true;
         // 
         // WaitForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.AutoSize = true;
         this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
         this.ClientSize = new System.Drawing.Size(206, 47);
         this.ControlBox = false;
         this.Controls.Add(this.progressBar);
         this.Controls.Add(this.lblMessage);
         this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.MinimumSize = new System.Drawing.Size(6, 50);
         this.Name = "WaitForm";
         this.Padding = new System.Windows.Forms.Padding(3, 8, 3, 8);
         this.ShowIcon = false;
         this.ShowInTaskbar = false;
         this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
         this.UseWaitCursor = true;
         this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.WaitForm_FormClosing);
         this.Load += new System.EventHandler(this.WaitForm_Load);
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion

      private System.Windows.Forms.Label lblMessage;
      private System.Windows.Forms.ProgressBar progressBar;
   }
}
