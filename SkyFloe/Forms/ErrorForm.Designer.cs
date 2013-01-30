namespace SkyFloe.App.Forms
{
   partial class ErrorForm
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
         this.components = new System.ComponentModel.Container();
         System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ErrorForm));
         this.lblName = new System.Windows.Forms.Label();
         this.txtMessage = new System.Windows.Forms.TextBox();
         this.lblType = new System.Windows.Forms.Label();
         this.txtType = new System.Windows.Forms.TextBox();
         this.lblSource = new System.Windows.Forms.Label();
         this.txtSource = new System.Windows.Forms.TextBox();
         this.lblStackTrace = new System.Windows.Forms.Label();
         this.txtStackTrace = new System.Windows.Forms.TextBox();
         this.btnClose = new System.Windows.Forms.Button();
         this.btnNext = new System.Windows.Forms.Button();
         this.btnPrev = new System.Windows.Forms.Button();
         this.btnCopy = new System.Windows.Forms.Button();
         this.binding = new System.Windows.Forms.BindingSource(this.components);
         ((System.ComponentModel.ISupportInitialize)(this.binding)).BeginInit();
         this.SuspendLayout();
         // 
         // lblName
         // 
         this.lblName.AutoSize = true;
         this.lblName.Location = new System.Drawing.Point(9, 81);
         this.lblName.Name = "lblName";
         this.lblName.Size = new System.Drawing.Size(50, 13);
         this.lblName.TabIndex = 5;
         this.lblName.Text = "Message";
         // 
         // txtMessage
         // 
         this.txtMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.txtMessage.Location = new System.Drawing.Point(12, 100);
         this.txtMessage.Multiline = true;
         this.txtMessage.Name = "txtMessage";
         this.txtMessage.ReadOnly = true;
         this.txtMessage.Size = new System.Drawing.Size(480, 86);
         this.txtMessage.TabIndex = 6;
         this.txtMessage.WordWrap = false;
         // 
         // lblType
         // 
         this.lblType.AutoSize = true;
         this.lblType.Location = new System.Drawing.Point(9, 15);
         this.lblType.Name = "lblType";
         this.lblType.Size = new System.Drawing.Size(31, 13);
         this.lblType.TabIndex = 1;
         this.lblType.Text = "Type";
         // 
         // txtType
         // 
         this.txtType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.txtType.Location = new System.Drawing.Point(65, 12);
         this.txtType.Name = "txtType";
         this.txtType.ReadOnly = true;
         this.txtType.Size = new System.Drawing.Size(329, 20);
         this.txtType.TabIndex = 2;
         // 
         // lblSource
         // 
         this.lblSource.AutoSize = true;
         this.lblSource.Location = new System.Drawing.Point(9, 41);
         this.lblSource.Name = "lblSource";
         this.lblSource.Size = new System.Drawing.Size(41, 13);
         this.lblSource.TabIndex = 3;
         this.lblSource.Text = "Source";
         // 
         // txtSource
         // 
         this.txtSource.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.txtSource.Location = new System.Drawing.Point(65, 38);
         this.txtSource.Name = "txtSource";
         this.txtSource.ReadOnly = true;
         this.txtSource.Size = new System.Drawing.Size(329, 20);
         this.txtSource.TabIndex = 4;
         // 
         // lblStackTrace
         // 
         this.lblStackTrace.AutoSize = true;
         this.lblStackTrace.Location = new System.Drawing.Point(9, 205);
         this.lblStackTrace.Name = "lblStackTrace";
         this.lblStackTrace.Size = new System.Drawing.Size(62, 13);
         this.lblStackTrace.TabIndex = 7;
         this.lblStackTrace.Text = "Stack trace";
         // 
         // txtStackTrace
         // 
         this.txtStackTrace.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.txtStackTrace.Location = new System.Drawing.Point(12, 221);
         this.txtStackTrace.Multiline = true;
         this.txtStackTrace.Name = "txtStackTrace";
         this.txtStackTrace.ReadOnly = true;
         this.txtStackTrace.Size = new System.Drawing.Size(480, 124);
         this.txtStackTrace.TabIndex = 8;
         this.txtStackTrace.WordWrap = false;
         // 
         // btnClose
         // 
         this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
         this.btnClose.Location = new System.Drawing.Point(417, 71);
         this.btnClose.Name = "btnClose";
         this.btnClose.Size = new System.Drawing.Size(75, 23);
         this.btnClose.TabIndex = 11;
         this.btnClose.Text = "Close";
         this.btnClose.UseVisualStyleBackColor = true;
         this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
         // 
         // btnNext
         // 
         this.btnNext.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.btnNext.Location = new System.Drawing.Point(457, 12);
         this.btnNext.Name = "btnNext";
         this.btnNext.Size = new System.Drawing.Size(35, 23);
         this.btnNext.TabIndex = 10;
         this.btnNext.Text = ">>";
         this.btnNext.UseVisualStyleBackColor = true;
         this.btnNext.Click += new System.EventHandler(this.btnNext_Click);
         // 
         // btnPrev
         // 
         this.btnPrev.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.btnPrev.Location = new System.Drawing.Point(417, 12);
         this.btnPrev.Name = "btnPrev";
         this.btnPrev.Size = new System.Drawing.Size(35, 23);
         this.btnPrev.TabIndex = 9;
         this.btnPrev.Text = "<<";
         this.btnPrev.UseVisualStyleBackColor = true;
         this.btnPrev.Click += new System.EventHandler(this.btnPrev_Click);
         // 
         // btnCopy
         // 
         this.btnCopy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.btnCopy.Location = new System.Drawing.Point(417, 42);
         this.btnCopy.Name = "btnCopy";
         this.btnCopy.Size = new System.Drawing.Size(75, 23);
         this.btnCopy.TabIndex = 12;
         this.btnCopy.Text = "Copy";
         this.btnCopy.UseVisualStyleBackColor = true;
         this.btnCopy.Click += new System.EventHandler(this.btnCopy_Click);
         // 
         // binding
         // 
         this.binding.AllowNew = false;
         this.binding.PositionChanged += new System.EventHandler(this.binding_PositionChanged);
         // 
         // ErrorForm
         // 
         this.AcceptButton = this.btnClose;
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.CancelButton = this.btnClose;
         this.ClientSize = new System.Drawing.Size(504, 357);
         this.Controls.Add(this.btnCopy);
         this.Controls.Add(this.btnPrev);
         this.Controls.Add(this.btnNext);
         this.Controls.Add(this.btnClose);
         this.Controls.Add(this.txtStackTrace);
         this.Controls.Add(this.lblStackTrace);
         this.Controls.Add(this.txtSource);
         this.Controls.Add(this.lblSource);
         this.Controls.Add(this.txtType);
         this.Controls.Add(this.lblType);
         this.Controls.Add(this.txtMessage);
         this.Controls.Add(this.lblName);
         this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
         this.Name = "ErrorForm";
         this.ShowInTaskbar = false;
         this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
         this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
         this.Text = "Error";
         ((System.ComponentModel.ISupportInitialize)(this.binding)).EndInit();
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion

      private System.Windows.Forms.Label lblName;
      private System.Windows.Forms.TextBox txtMessage;
      private System.Windows.Forms.Label lblType;
      private System.Windows.Forms.TextBox txtType;
      private System.Windows.Forms.Label lblSource;
      private System.Windows.Forms.TextBox txtSource;
      private System.Windows.Forms.Label lblStackTrace;
      private System.Windows.Forms.TextBox txtStackTrace;
      private System.Windows.Forms.Button btnClose;
      private System.Windows.Forms.Button btnNext;
      private System.Windows.Forms.Button btnPrev;
      private System.Windows.Forms.Button btnCopy;
      private System.Windows.Forms.BindingSource binding;
   }
}