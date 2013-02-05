namespace SkyFloe.App.Forms
{
   partial class ConnectionForm
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
         System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConnectionForm));
         this.lblStoreType = new System.Windows.Forms.Label();
         this.cboStoreType = new System.Windows.Forms.ComboBox();
         this.pnlProperties = new System.Windows.Forms.Panel();
         this.btnOK = new System.Windows.Forms.Button();
         this.btnCancel = new System.Windows.Forms.Button();
         this.lblRecent = new System.Windows.Forms.Label();
         this.cboRecent = new System.Windows.Forms.ComboBox();
         this.grpStoreProps = new System.Windows.Forms.GroupBox();
         this.grpStoreProps.SuspendLayout();
         this.SuspendLayout();
         // 
         // lblStoreType
         // 
         this.lblStoreType.AutoSize = true;
         this.lblStoreType.Location = new System.Drawing.Point(6, 22);
         this.lblStoreType.Name = "lblStoreType";
         this.lblStoreType.Size = new System.Drawing.Size(55, 13);
         this.lblStoreType.TabIndex = 1;
         this.lblStoreType.Text = "Store type";
         // 
         // cboStoreType
         // 
         this.cboStoreType.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
         this.cboStoreType.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
         this.cboStoreType.DisplayMember = "Value";
         this.cboStoreType.FormattingEnabled = true;
         this.cboStoreType.Location = new System.Drawing.Point(125, 19);
         this.cboStoreType.MaxDropDownItems = 4;
         this.cboStoreType.Name = "cboStoreType";
         this.cboStoreType.Size = new System.Drawing.Size(252, 21);
         this.cboStoreType.TabIndex = 2;
         this.cboStoreType.ValueMember = "Key";
         this.cboStoreType.SelectedIndexChanged += new System.EventHandler(this.cboStoreType_SelectedIndexChanged);
         this.cboStoreType.Validating += new System.ComponentModel.CancelEventHandler(this.cboStoreType_Validating);
         // 
         // pnlProperties
         // 
         this.pnlProperties.AutoScroll = true;
         this.pnlProperties.Location = new System.Drawing.Point(6, 46);
         this.pnlProperties.Name = "pnlProperties";
         this.pnlProperties.Size = new System.Drawing.Size(371, 110);
         this.pnlProperties.TabIndex = 5;
         // 
         // btnOK
         // 
         this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
         this.btnOK.Enabled = false;
         this.btnOK.Location = new System.Drawing.Point(130, 208);
         this.btnOK.Name = "btnOK";
         this.btnOK.Size = new System.Drawing.Size(75, 23);
         this.btnOK.TabIndex = 4;
         this.btnOK.Text = "Connect";
         this.btnOK.UseVisualStyleBackColor = true;
         // 
         // btnCancel
         // 
         this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
         this.btnCancel.Location = new System.Drawing.Point(211, 208);
         this.btnCancel.Name = "btnCancel";
         this.btnCancel.Size = new System.Drawing.Size(75, 23);
         this.btnCancel.TabIndex = 5;
         this.btnCancel.Text = "Cancel";
         this.btnCancel.UseVisualStyleBackColor = true;
         // 
         // lblRecent
         // 
         this.lblRecent.AutoSize = true;
         this.lblRecent.Location = new System.Drawing.Point(12, 15);
         this.lblRecent.Name = "lblRecent";
         this.lblRecent.Size = new System.Drawing.Size(98, 13);
         this.lblRecent.TabIndex = 1;
         this.lblRecent.Text = "Recent connection";
         // 
         // cboRecent
         // 
         this.cboRecent.DisplayMember = "Value";
         this.cboRecent.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.cboRecent.FormattingEnabled = true;
         this.cboRecent.Location = new System.Drawing.Point(140, 12);
         this.cboRecent.Name = "cboRecent";
         this.cboRecent.Size = new System.Drawing.Size(258, 21);
         this.cboRecent.TabIndex = 2;
         this.cboRecent.ValueMember = "Key";
         this.cboRecent.SelectedIndexChanged += new System.EventHandler(this.cboRecent_SelectedIndexChanged);
         // 
         // grpStoreProps
         // 
         this.grpStoreProps.Controls.Add(this.cboStoreType);
         this.grpStoreProps.Controls.Add(this.lblStoreType);
         this.grpStoreProps.Controls.Add(this.pnlProperties);
         this.grpStoreProps.Location = new System.Drawing.Point(12, 39);
         this.grpStoreProps.Name = "grpStoreProps";
         this.grpStoreProps.Size = new System.Drawing.Size(386, 163);
         this.grpStoreProps.TabIndex = 3;
         this.grpStoreProps.TabStop = false;
         // 
         // ConnectionForm
         // 
         this.AcceptButton = this.btnOK;
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.CancelButton = this.btnCancel;
         this.ClientSize = new System.Drawing.Size(410, 238);
         this.Controls.Add(this.grpStoreProps);
         this.Controls.Add(this.cboRecent);
         this.Controls.Add(this.lblRecent);
         this.Controls.Add(this.btnCancel);
         this.Controls.Add(this.btnOK);
         this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
         this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.Name = "ConnectionForm";
         this.ShowInTaskbar = false;
         this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
         this.Text = "Connect to Store";
         this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ConnectionForm_FormClosing);
         this.grpStoreProps.ResumeLayout(false);
         this.grpStoreProps.PerformLayout();
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion

      private System.Windows.Forms.Label lblStoreType;
      private System.Windows.Forms.ComboBox cboStoreType;
      private System.Windows.Forms.Panel pnlProperties;
      private System.Windows.Forms.Button btnOK;
      private System.Windows.Forms.Button btnCancel;
      private System.Windows.Forms.Label lblRecent;
      private System.Windows.Forms.ComboBox cboRecent;
      private System.Windows.Forms.GroupBox grpStoreProps;
   }
}