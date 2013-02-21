using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace SkyFloe.App.Forms
{
   public partial class ConnectionForm : Form
   {
      public Connection Connection { get; private set; }

      private String SelectedStoreType
      {
         get { return (String)this.cboStoreType.SelectedValue; }
      }
      private Object StoreProperties
      {
         get; set; 
      }

      private static Dictionary<String, String> knownStores =
         new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase)
         {
            { "FileSystem", "File System" },
            { "AwsGlacier", "Amazon Glacier" }
         };

      public ConnectionForm ()
      {
         InitializeComponent();
         Control tabNextCtrl = new Control()
         {
            TabIndex = this.cboStoreType.TabIndex + 1,
            TabStop = true
         };
         Control tabPrevCtrl = new Control()
         {
            TabIndex = this.pnlProperties.TabIndex - 1,
            TabStop = true
         };
         tabNextCtrl.GotFocus += (o, a) =>
         {
            if (this.pnlProperties.Controls.Count > 0)
               this.pnlProperties.Controls[0].Focus();
         };
         tabPrevCtrl.GotFocus += (o, a) =>
         {
            this.cboStoreType.Focus();
         };
         this.grpStoreProps.Controls.Add(tabNextCtrl);
         this.grpStoreProps.Controls.Add(tabPrevCtrl);
         // load the connection strings
         if (Program.Settings.ConnectionStrings == null)
            Program.Settings.ConnectionStrings = new Data.ConnectionStringList();
         // bind the store types
         BindingList<KeyValuePair<String, String>> stores =
            new BindingList<KeyValuePair<String, String>>();
         foreach (KeyValuePair<String, String> known in knownStores)
            stores.Add(known);
         foreach (Data.ConnectionString connect in Program.Settings.ConnectionStrings.Items)
         {
            String store = null;
            if (Connection.Parse(connect.Value).TryGetValue("Store", out store))
               if (!String.IsNullOrWhiteSpace(store))
                  if (!knownStores.ContainsKey(store))
                     stores.Add(new KeyValuePair<String, String>(store, store));
         }
         this.cboStoreType.DataSource = 
            new BindingList<KeyValuePair<String, String>>(stores);
         // bind the recent connection strings
         this.cboRecent.DataSource = new BindingList<KeyValuePair<String, String>>(
            Program.Settings.ConnectionStrings
               .Items
               .Select(s => new KeyValuePair<String, String>(s.Value, s.Caption))
               .Concat(new[] { new KeyValuePair<String, String>(null, "<New>") })
               .ToList()
         );
         if (this.cboRecent.Items.Count == 0)
            this.cboStoreType.Focus();
      }

      private void ConnectionForm_FormClosing (Object sender, FormClosingEventArgs a)
      {
         this.Connection = null;
         if (DialogResult == DialogResult.OK)
         {
            this.DialogResult = DialogResult.None;
            if (ValidateChildren())
            {
               try
               {
                  String storeType = this.SelectedStoreType;
                  Object storeProps = this.StoreProperties;
                  new WaitForm(
                     () => this.Connection = new Connection(
                        Connection.GetConnectionString(storeType, storeProps)
                     ),
                     "Connecting..."
                  ).ShowDialog(this);
                  Program.Settings.ConnectionStrings.Items.Remove(
                     Program.Settings.ConnectionStrings.Items.FirstOrDefault(
                        s => StringComparer.OrdinalIgnoreCase.Equals(s.Value, this.Connection.ConnectionString)
                     )
                  );
                  Program.Settings.ConnectionStrings.Items.Insert(
                     0, 
                     new Data.ConnectionString()
                     {
                        Caption = this.Connection.Caption,
                        Value = this.Connection.ConnectionString
                     }
                  );
                  while (Program.Settings.ConnectionStrings.Items.Count > 10)
                     Program.Settings.ConnectionStrings.Items.RemoveAt(
                        Program.Settings.ConnectionStrings.Items.Count - 1
                     );
                  this.DialogResult = DialogResult.OK;
               }
               catch (Exception e)
               {
                  new ErrorForm(e).ShowDialog(this);
               }
            }
         }
      }

      private void cboRecent_SelectedIndexChanged (Object o, EventArgs a)
      {
         this.cboStoreType.SelectedItem = null;
         if (this.cboRecent.SelectedValue != null)
         {
            Dictionary<String, String> paramMap = Connection.Parse(
               (String)this.cboRecent.SelectedValue
            );
            this.cboStoreType.SelectedValue = paramMap["Store"];
            Connection.Bind(paramMap, this.StoreProperties);
         }
         HandleConnectionValueChanged(o, a);
      }
      private void cboStoreType_SelectedIndexChanged (Object o, EventArgs a)
      {
         Boolean focusProps = this.pnlProperties.Contains(this.ActiveControl);
         this.pnlProperties.Controls.Clear();
         if (this.cboStoreType.SelectedValue != null)
         {
            // TODO: custom panel + default panel
            Stores.IBaseStorePage page = null;
            if (this.SelectedStoreType == "FileSystem")
               page = new Stores.FileStorePage();
            else if (this.SelectedStoreType == "AwsGlacier")
               page = new Stores.AwsGlacierStorePage();
            else
               page = new Stores.GenericStorePage();
            page.StoreProperties = this.StoreProperties = Connection.GetStoreProperties(this.SelectedStoreType);
            ((Control)page).Dock = DockStyle.Fill;
            page.PropertyChanged += HandleConnectionValueChanged;
            this.pnlProperties.Controls.Add((Control)page);
            if (focusProps)
               this.pnlProperties.Controls[0].Focus();
         }
         HandleConnectionValueChanged(o, a);
      }

      private void cboStoreType_Validating (Object sender, CancelEventArgs a)
      {
         Boolean focusProps = this.pnlProperties.Contains(this.ActiveControl);
         if (this.cboStoreType.SelectedValue == null)
         {
            if (!String.IsNullOrWhiteSpace(this.cboStoreType.Text))
            {
               String connect = this.cboStoreType.Text;
               try
               {
                  // TODO: refactor
                  Connection.GetStoreProperties(connect);
                  ((BindingList<KeyValuePair<String, String>>)this.cboStoreType.DataSource)
                     .Add(
                        new KeyValuePair<String, String>(connect, connect)
                     );
                  this.cboStoreType.SelectedValue = connect;
               }
               catch
               {
                  a.Cancel = true;
               }
            }
         }
      }
      private void HandleConnectionValueChanged (Object o, EventArgs a)
      {
         Boolean isValid = true;
         if (this.cboStoreType.SelectedItem == null)
            isValid = false;
         else
         {
            List<ValidationResult> results = new List<ValidationResult>();
            isValid = Validator.TryValidateObject(
               this.StoreProperties,
               new ValidationContext(this.StoreProperties, null, null),
               results,
               true
            );
         }
         this.btnOK.Enabled = isValid;
      }
   }
}
