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
      public Connection Connection { get; private set; }

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

      private void Disconnect ()
      {
         // TODO: clear form state
         this.tree.Nodes.Clear();
         if (this.Connection != null)
            this.Connection.Dispose();
         this.Connection = null;
         this.btnRefresh.Enabled = false;
      }

      private void Connect ()
      {
         var connectionForm = new ConnectionForm();
         if (connectionForm.ShowDialog(this) == DialogResult.OK)
         {
            Disconnect();
            this.Connection = connectionForm.Connection;
            BuildTree();
            this.btnRefresh.Enabled = true;
         }
      }
      private void Reconnect ()
      {
         var connect = this.Connection.ConnectionString;
         Disconnect();
         // TODO: refactor
         new WaitForm(
            () => this.Connection = new Connection(connect),
            "Connecting..."
         ).ShowDialog(this);
         BuildTree();
         // TODO: refactor
         this.btnRefresh.Enabled = true;
      }
      private void BuildTree ()
      {
         this.tree.BeginUpdate();
         try
         {
            foreach (var archiveName in this.Connection
               .ListArchives()
               .OrderBy(a => a)
            )
            {
               var treeArchive = new TreeNode()
               {
                  Name = archiveName,
                  Text = archiveName,
                  ImageKey = "Archive",
                  SelectedImageKey = "Archive"
               };
               this.tree.Nodes.Add(treeArchive);
            }
         }
         finally
         {
            this.tree.EndUpdate();
         }
      }

      private void ExpandArchive (TreeNode treeArchive)
      {
         if (!treeArchive.IsExpanded)
         {
            try
            {
               this.Cursor = Cursors.WaitCursor;
               foreach (var archiveNode in this.tree.Nodes.Cast<TreeNode>())
                  archiveNode.Collapse();
               var archive = treeArchive.Tag as Connection.Archive;
               if (archive == null)
               {
                  new WaitForm(
                     () => archive = this.Connection.OpenArchive(treeArchive.Name),
                     String.Format("Opening archive {0}...", treeArchive.Name)
                  ).ShowDialog(this);
                  treeArchive.Tag = archive;
               }
               this.Cursor = Cursors.WaitCursor;
               foreach (var root in archive.Roots.OrderBy(r => r.Name))
                  AddNode(treeArchive, root);
               AddDescendents(treeArchive);
               treeArchive.Expand();
            }
            finally
            {
               this.Cursor = Cursors.Default;
            }
         }
      }
      private void AddNode (TreeNode treeParent, Backup.Node node)
      {
         var imageKey = node.Type.ToString();
         var treeChild = new TreeNode()
         {
            Name = node.Name,
            Text = node.Name,
            ImageKey = imageKey,
            SelectedImageKey = imageKey,
            Tag = node
         };
         treeParent.Nodes.Add(treeChild);
      }
      private void AddDescendents (TreeNode treeNode)
      {
         this.Cursor = Cursors.WaitCursor;
         try
         {
            var archiveNode = treeNode;
            while (archiveNode.ImageKey != "Archive")
               archiveNode = archiveNode.Parent;
            var archive = (Connection.Archive)archiveNode.Tag;
            foreach (var treeChild in treeNode.Nodes.Cast<TreeNode>())
            {
               if (treeChild.Nodes.Count == 0)
               {
                  var child = (Backup.Node)treeChild.Tag;
                  foreach (var desc in archive
                     .GetChildren(child)
                     .OrderBy(n => n.Type)
                     .ThenBy(n => n.Name)
                  )
                     AddNode(treeChild, desc);
               }
            }
         }
         finally
         {
            this.Cursor = Cursors.Default;
         }
      }

      private void MainForm_Load (Object o, EventArgs a)
      {
         Show();
         Connect();
      }
      private void MainForm_FormClosed (Object o, FormClosedEventArgs a)
      {
         Disconnect();
      }
      private void MainForm_Move (Object o, EventArgs a)
      {
         if (this.WindowState == FormWindowState.Normal)
            Program.Settings.WindowLocation = this.Location;
      }

      private void MainForm_Resize (Object o, EventArgs a)
      {
         Program.Settings.WindowState = this.WindowState;
         if (this.WindowState == FormWindowState.Normal)
            Program.Settings.WindowSize = this.Size;
      }

      private void tree_Click (Object o, EventArgs a)
      {
         var selected = this.tree.SelectedNode;
         if (selected != null && selected.Parent == null)
            ExpandArchive(selected);
      }

      private void tree_AfterSelect (Object o, TreeViewEventArgs a)
      {
         var selected = a.Node;
         if (selected.Parent == null)
            ExpandArchive(selected);
      }

      private void tree_AfterExpand (Object o, TreeViewEventArgs a)
      {
         var expanded = a.Node;
         if (expanded.ImageKey == "Root" || expanded.ImageKey == "Directory")
            AddDescendents(expanded);
      }
      private void tree_AfterCollapse (Object o, TreeViewEventArgs a)
      {
         var collapsed = a.Node;
         if (collapsed.Parent == null)
            collapsed.Nodes.Clear();
      }

      private void btnConnect_Click (object sender, EventArgs e)
      {
         Connect();
      }

      private void btnRefresh_Click (object sender, EventArgs e)
      {
         Reconnect();
      }

      protected override Boolean ProcessCmdKey (ref Message msg, Keys keyData)
      {
         switch (keyData)
         {
            case Keys.F5:
               Reconnect();
               return true;
            default:
               return base.ProcessCmdKey(ref msg, keyData);
         }
      }
   }
}
