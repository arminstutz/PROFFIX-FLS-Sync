using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.VisualBasic.CompilerServices;

namespace FlsGliderSync
{
    [DesignerGenerated()]
    public partial class FrmSettings : Form
    {

        // Das Formular überschreibt den Löschvorgang, um die Komponentenliste zu bereinigen.
        [DebuggerNonUserCode()]
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && components is object)
                {
                    components.Dispose();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        // Wird vom Windows Form-Designer benötigt.
        private System.ComponentModel.IContainer components;

        // Hinweis: Die folgende Prozedur ist für den Windows Form-Designer erforderlich.
        // Das Bearbeiten ist mit dem Windows Form-Designer möglich.  
        // Das Bearbeiten mit dem Code-Editor ist nicht möglich.
        [DebuggerStepThrough()]
        private void InitializeComponent()
        {
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmSettings));
            tbUser = new TextBox();
            tbPassword = new TextBox();
            lblUser = new Label();
            lblPassword = new Label();
            btnSave = new Button();
            lblZugang = new Label();
            tbZugang = new TextBox();
            gbVerbindung = new GroupBox();
            btnCancel = new Button();
            gbVerbindung.SuspendLayout();
            SuspendLayout();
            // 
            // tbUser
            // 
            tbUser.Location = new System.Drawing.Point(75, 45);
            tbUser.Name = "tbUser";
            tbUser.Size = new System.Drawing.Size(188, 20);
            tbUser.TabIndex = 4;
            // 
            // tbPassword
            // 
            tbPassword.Location = new System.Drawing.Point(75, 71);
            tbPassword.Name = "tbPassword";
            tbPassword.PasswordChar = '*';
            tbPassword.Size = new System.Drawing.Size(188, 20);
            tbPassword.TabIndex = 5;
            // 
            // lblUser
            // 
            lblUser.AutoSize = true;
            lblUser.Location = new System.Drawing.Point(10, 52);
            lblUser.Name = "lblUser";
            lblUser.Size = new System.Drawing.Size(55, 13);
            lblUser.TabIndex = 6;
            lblUser.Text = "Username";
            // 
            // lblPassword
            // 
            lblPassword.AutoSize = true;
            lblPassword.Location = new System.Drawing.Point(10, 78);
            lblPassword.Name = "lblPassword";
            lblPassword.Size = new System.Drawing.Size(53, 13);
            lblPassword.TabIndex = 7;
            lblPassword.Text = "Password";
            // 
            // btnSave
            // 
            btnSave.DialogResult = DialogResult.OK;
            btnSave.Location = new System.Drawing.Point(12, 127);
            btnSave.Name = "btnSave";
            btnSave.Size = new System.Drawing.Size(75, 23);
            btnSave.TabIndex = 8;
            btnSave.Text = "Speichern";
            btnSave.UseVisualStyleBackColor = true;
            // 
            // lblZugang
            // 
            lblZugang.AutoSize = true;
            lblZugang.Location = new System.Drawing.Point(10, 26);
            lblZugang.Name = "lblZugang";
            lblZugang.Size = new System.Drawing.Size(44, 13);
            lblZugang.TabIndex = 9;
            lblZugang.Text = "Zugang";
            // 
            // tbZugang
            // 
            tbZugang.Enabled = false;
            tbZugang.Location = new System.Drawing.Point(75, 19);
            tbZugang.Name = "tbZugang";
            tbZugang.Size = new System.Drawing.Size(188, 20);
            tbZugang.TabIndex = 10;
            // 
            // gbVerbindung
            // 
            gbVerbindung.Controls.Add(tbZugang);
            gbVerbindung.Controls.Add(tbUser);
            gbVerbindung.Controls.Add(lblZugang);
            gbVerbindung.Controls.Add(tbPassword);
            gbVerbindung.Controls.Add(lblUser);
            gbVerbindung.Controls.Add(lblPassword);
            gbVerbindung.Location = new System.Drawing.Point(12, 12);
            gbVerbindung.Name = "gbVerbindung";
            gbVerbindung.Size = new System.Drawing.Size(286, 109);
            gbVerbindung.TabIndex = 11;
            gbVerbindung.TabStop = false;
            gbVerbindung.Text = "Verbindungseinstellungen";
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new System.Drawing.Point(106, 127);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(75, 23);
            btnCancel.TabIndex = 14;
            btnCancel.Text = "Abbrechen";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // FrmSettings
            // 
            AllowDrop = true;
            AutoScaleDimensions = new System.Drawing.SizeF(6.0f, 13.0f);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            CancelButton = btnCancel;
            ClientSize = new System.Drawing.Size(316, 165);
            Controls.Add(btnCancel);
            Controls.Add(gbVerbindung);
            Controls.Add(btnSave);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MaximumSize = new System.Drawing.Size(9999, 9999);
            MinimumSize = new System.Drawing.Size(320, 150);
            Name = "FrmSettings";
            SizeGripStyle = SizeGripStyle.Hide;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Einstellungen";
            gbVerbindung.ResumeLayout(false);
            gbVerbindung.PerformLayout();
            KeyDown += new KeyEventHandler(FrmSettings_KeyDown);
            ResumeLayout(false);
        }

        internal TextBox tbUser;
        internal TextBox tbPassword;
        internal Label lblUser;
        internal Label lblPassword;
        internal Button btnSave;
        internal Label lblZugang;
        internal TextBox tbZugang;
        internal GroupBox gbVerbindung;
        internal Button btnCancel;
    }
}