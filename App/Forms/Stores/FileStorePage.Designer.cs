namespace SkyFloe.App.Forms.Stores
{
   partial class FileStorePage
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
         this.folderBrowser = new System.Windows.Forms.FolderBrowserDialog();
         this.binding = new System.Windows.Forms.BindingSource(this.components);
         this.btnChoosePath = new System.Windows.Forms.Button();
         this.txtPath = new System.Windows.Forms.TextBox();
         this.lblPath = new System.Windows.Forms.Label();
         ((System.ComponentModel.ISupportInitialize)(this.binding)).BeginInit();
         this.SuspendLayout();
         // 
         // folderBrowser
         // 
         this.folderBrowser.RootFolder = System.Environment.SpecialFolder.MyComputer;
         // 
         // binding
         // 
         this.binding.CurrentItemChanged += new System.EventHandler(this.binding_CurrentItemChanged);
         // 
         // btnChoosePath
         // 
         this.btnChoosePath.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.btnChoosePath.Location = new System.Drawing.Point(346, 3);
         this.btnChoosePath.Margin = new System.Windows.Forms.Padding(0);
         this.btnChoosePath.Name = "btnChoosePath";
         this.btnChoosePath.Size = new System.Drawing.Size(25, 21);
         this.btnChoosePath.TabIndex = 2;
         this.btnChoosePath.Text = "...";
         this.btnChoosePath.UseVisualStyleBackColor = true;
         this.btnChoosePath.Click += new System.EventHandler(this.btnChoosePath_Click);
         // 
         // txtPath
         // 
         this.txtPath.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
         this.txtPath.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystemDirectories;
         this.txtPath.Location = new System.Drawing.Point(119, 4);
         this.txtPath.Name = "txtPath";
         this.txtPath.Size = new System.Drawing.Size(224, 20);
         this.txtPath.TabIndex = 1;
         this.txtPath.TextChanged += new System.EventHandler(this.txtPath_TextChanged);
         // 
         // lblPath
         // 
         this.lblPath.AutoSize = true;
         this.lblPath.Location = new System.Drawing.Point(0, 7);
         this.lblPath.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
         this.lblPath.Name = "lblPath";
         this.lblPath.Size = new System.Drawing.Size(29, 13);
         this.lblPath.TabIndex = 0;
         this.lblPath.Text = "Path";
         // 
         // FileStorePage
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.Controls.Add(this.btnChoosePath);
         this.Controls.Add(this.txtPath);
         this.Controls.Add(this.lblPath);
         this.Name = "FileStorePage";
         this.Size = new System.Drawing.Size(371, 110);
         ((System.ComponentModel.ISupportInitialize)(this.binding)).EndInit();
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion

      private System.Windows.Forms.Label lblPath;
      private System.Windows.Forms.TextBox txtPath;
      private System.Windows.Forms.Button btnChoosePath;
      private System.Windows.Forms.FolderBrowserDialog folderBrowser;
      private System.Windows.Forms.BindingSource binding;
   }
}
