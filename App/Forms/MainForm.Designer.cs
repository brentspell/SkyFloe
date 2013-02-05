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
         this.components = new System.ComponentModel.Container();
         System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
         this.statusBar = new System.Windows.Forms.StatusStrip();
         this.split = new System.Windows.Forms.SplitContainer();
         this.tree = new System.Windows.Forms.TreeView();
         this.treeImages = new System.Windows.Forms.ImageList(this.components);
         this.toolBar = new System.Windows.Forms.ToolStrip();
         this.btnConnect = new System.Windows.Forms.ToolStripButton();
         this.btnRefresh = new System.Windows.Forms.ToolStripButton();
         ((System.ComponentModel.ISupportInitialize)(this.split)).BeginInit();
         this.split.Panel1.SuspendLayout();
         this.split.SuspendLayout();
         this.toolBar.SuspendLayout();
         this.SuspendLayout();
         // 
         // statusBar
         // 
         this.statusBar.Location = new System.Drawing.Point(0, 551);
         this.statusBar.Name = "statusBar";
         this.statusBar.Size = new System.Drawing.Size(792, 22);
         this.statusBar.TabIndex = 0;
         // 
         // split
         // 
         this.split.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.split.DataBindings.Add(new System.Windows.Forms.Binding("SplitterDistance", global::SkyFloe.App.Properties.Settings.Default, "MainSplitterDistance", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
         this.split.Location = new System.Drawing.Point(0, 24);
         this.split.Name = "split";
         // 
         // split.Panel1
         // 
         this.split.Panel1.Controls.Add(this.tree);
         this.split.Size = new System.Drawing.Size(792, 527);
         this.split.SplitterDistance = global::SkyFloe.App.Properties.Settings.Default.MainSplitterDistance;
         this.split.TabIndex = 1;
         this.split.TabStop = false;
         // 
         // tree
         // 
         this.tree.Dock = System.Windows.Forms.DockStyle.Fill;
         this.tree.ImageIndex = 0;
         this.tree.ImageList = this.treeImages;
         this.tree.ItemHeight = 18;
         this.tree.Location = new System.Drawing.Point(0, 0);
         this.tree.Name = "tree";
         this.tree.SelectedImageIndex = 0;
         this.tree.ShowRootLines = false;
         this.tree.Size = new System.Drawing.Size(320, 527);
         this.tree.TabIndex = 0;
         this.tree.AfterCollapse += new System.Windows.Forms.TreeViewEventHandler(this.tree_AfterCollapse);
         this.tree.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(this.tree_AfterExpand);
         this.tree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tree_AfterSelect);
         this.tree.Click += new System.EventHandler(this.tree_Click);
         // 
         // treeImages
         // 
         this.treeImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("treeImages.ImageStream")));
         this.treeImages.TransparentColor = System.Drawing.Color.Transparent;
         this.treeImages.Images.SetKeyName(0, "Archive");
         this.treeImages.Images.SetKeyName(1, "Root");
         this.treeImages.Images.SetKeyName(2, "Directory");
         this.treeImages.Images.SetKeyName(3, "File");
         // 
         // toolBar
         // 
         this.toolBar.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
         this.toolBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnConnect,
            this.btnRefresh});
         this.toolBar.Location = new System.Drawing.Point(0, 0);
         this.toolBar.Name = "toolBar";
         this.toolBar.Size = new System.Drawing.Size(792, 25);
         this.toolBar.TabIndex = 2;
         // 
         // btnConnect
         // 
         this.btnConnect.AutoToolTip = false;
         this.btnConnect.Image = global::SkyFloe.App.Properties.Resources.connect;
         this.btnConnect.ImageTransparentColor = System.Drawing.Color.Magenta;
         this.btnConnect.Name = "btnConnect";
         this.btnConnect.Size = new System.Drawing.Size(67, 22);
         this.btnConnect.Text = "&Connect";
         this.btnConnect.ToolTipText = "Connect to a new store";
         this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
         // 
         // btnRefresh
         // 
         this.btnRefresh.AutoToolTip = false;
         this.btnRefresh.Enabled = false;
         this.btnRefresh.Image = ((System.Drawing.Image)(resources.GetObject("btnRefresh.Image")));
         this.btnRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
         this.btnRefresh.Name = "btnRefresh";
         this.btnRefresh.Size = new System.Drawing.Size(65, 22);
         this.btnRefresh.Text = "&Refresh";
         this.btnRefresh.ToolTipText = "Refresh the current connection";
         this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
         // 
         // MainForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(792, 573);
         this.Controls.Add(this.toolBar);
         this.Controls.Add(this.split);
         this.Controls.Add(this.statusBar);
         this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
         this.Location = new System.Drawing.Point(112, 84);
         this.MinimumSize = new System.Drawing.Size(800, 600);
         this.Name = "MainForm";
         this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
         this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
         this.Text = "SkyFloe Backup/Restore";
         this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
         this.Load += new System.EventHandler(this.MainForm_Load);
         this.Move += new System.EventHandler(this.MainForm_Move);
         this.Resize += new System.EventHandler(this.MainForm_Resize);
         this.split.Panel1.ResumeLayout(false);
         ((System.ComponentModel.ISupportInitialize)(this.split)).EndInit();
         this.split.ResumeLayout(false);
         this.toolBar.ResumeLayout(false);
         this.toolBar.PerformLayout();
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion

      private System.Windows.Forms.StatusStrip statusBar;
      private System.Windows.Forms.SplitContainer split;
      private System.Windows.Forms.TreeView tree;
      private System.Windows.Forms.ToolStrip toolBar;
      private System.Windows.Forms.ImageList treeImages;
      private System.Windows.Forms.ToolStripButton btnConnect;
      private System.Windows.Forms.ToolStripButton btnRefresh;
   }
}

