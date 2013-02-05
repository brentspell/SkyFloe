namespace SkyFloe.App.Forms.Stores
{
   partial class AwsGlacierStorePage
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

      #region Component Designer generated code

      /// <summary> 
      /// Required method for Designer support - do not modify 
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent ()
      {
         this.components = new System.ComponentModel.Container();
         this.binding = new System.Windows.Forms.BindingSource(this.components);
         this.lblAccessKey = new System.Windows.Forms.Label();
         this.txtAccessKey = new System.Windows.Forms.TextBox();
         this.lblSecretKey = new System.Windows.Forms.Label();
         this.txtSecretKey = new System.Windows.Forms.TextBox();
         this.lblBucket = new System.Windows.Forms.Label();
         this.txtBucket = new System.Windows.Forms.TextBox();
         ((System.ComponentModel.ISupportInitialize)(this.binding)).BeginInit();
         this.SuspendLayout();
         // 
         // binding
         // 
         this.binding.CurrentItemChanged += new System.EventHandler(this.binding_CurrentItemChanged);
         // 
         // lblAccessKey
         // 
         this.lblAccessKey.AutoSize = true;
         this.lblAccessKey.Location = new System.Drawing.Point(0, 7);
         this.lblAccessKey.Name = "lblAccessKey";
         this.lblAccessKey.Size = new System.Drawing.Size(62, 13);
         this.lblAccessKey.TabIndex = 0;
         this.lblAccessKey.Text = "Access key";
         // 
         // txtAccessKey
         // 
         this.txtAccessKey.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
         this.txtAccessKey.Location = new System.Drawing.Point(119, 4);
         this.txtAccessKey.MaxLength = 20;
         this.txtAccessKey.Name = "txtAccessKey";
         this.txtAccessKey.Size = new System.Drawing.Size(252, 20);
         this.txtAccessKey.TabIndex = 1;
         this.txtAccessKey.Validating += new System.ComponentModel.CancelEventHandler(this.txtAccessKey_Validating);
         // 
         // lblSecretKey
         // 
         this.lblSecretKey.AutoSize = true;
         this.lblSecretKey.Location = new System.Drawing.Point(0, 33);
         this.lblSecretKey.Name = "lblSecretKey";
         this.lblSecretKey.Size = new System.Drawing.Size(58, 13);
         this.lblSecretKey.TabIndex = 2;
         this.lblSecretKey.Text = "Secret key";
         // 
         // txtSecretKey
         // 
         this.txtSecretKey.Location = new System.Drawing.Point(119, 30);
         this.txtSecretKey.MaxLength = 40;
         this.txtSecretKey.Name = "txtSecretKey";
         this.txtSecretKey.Size = new System.Drawing.Size(252, 20);
         this.txtSecretKey.TabIndex = 3;
         this.txtSecretKey.UseSystemPasswordChar = true;
         this.txtSecretKey.Validating += new System.ComponentModel.CancelEventHandler(this.txtSecretKey_Validating);
         // 
         // lblBucket
         // 
         this.lblBucket.AutoSize = true;
         this.lblBucket.Location = new System.Drawing.Point(0, 59);
         this.lblBucket.Name = "lblBucket";
         this.lblBucket.Size = new System.Drawing.Size(113, 13);
         this.lblBucket.TabIndex = 4;
         this.lblBucket.Text = "Vault prefix/S3 bucket";
         // 
         // txtBucket
         // 
         this.txtBucket.Location = new System.Drawing.Point(119, 56);
         this.txtBucket.MaxLength = 255;
         this.txtBucket.Name = "txtBucket";
         this.txtBucket.Size = new System.Drawing.Size(140, 20);
         this.txtBucket.TabIndex = 5;
         this.txtBucket.Validating += new System.ComponentModel.CancelEventHandler(this.txtBucket_Validating);
         // 
         // AwsGlacierStorePage
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.Controls.Add(this.txtBucket);
         this.Controls.Add(this.lblBucket);
         this.Controls.Add(this.txtSecretKey);
         this.Controls.Add(this.lblSecretKey);
         this.Controls.Add(this.txtAccessKey);
         this.Controls.Add(this.lblAccessKey);
         this.Name = "AwsGlacierStorePage";
         this.Size = new System.Drawing.Size(371, 110);
         ((System.ComponentModel.ISupportInitialize)(this.binding)).EndInit();
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion

      private System.Windows.Forms.BindingSource binding;
      private System.Windows.Forms.Label lblAccessKey;
      private System.Windows.Forms.TextBox txtAccessKey;
      private System.Windows.Forms.Label lblSecretKey;
      private System.Windows.Forms.TextBox txtSecretKey;
      private System.Windows.Forms.Label lblBucket;
      private System.Windows.Forms.TextBox txtBucket;
   }
}
