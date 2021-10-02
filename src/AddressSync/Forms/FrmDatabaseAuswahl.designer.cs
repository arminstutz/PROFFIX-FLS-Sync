using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Microsoft.VisualBasic.CompilerServices;

namespace FlsGliderSync
{
    [DesignerGenerated()]
    public partial class FrmDBAuswahl : Form
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
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmDBAuswahl));
            cbMain = new ComboBox();
            _Button1 = new Button();
            _Button1.Click += new EventHandler(btnSelect_Click);
            SuspendLayout();
            // 
            // cbMain
            // 
            cbMain.DropDownStyle = ComboBoxStyle.DropDownList;
            cbMain.FormattingEnabled = true;
            cbMain.Location = new System.Drawing.Point(12, 12);
            cbMain.Name = "cbMain";
            cbMain.Size = new System.Drawing.Size(230, 21);
            cbMain.TabIndex = 37;
            // 
            // Button1
            // 
            _Button1.Location = new System.Drawing.Point(12, 39);
            _Button1.Name = "_Button1";
            _Button1.Size = new System.Drawing.Size(230, 23);
            _Button1.TabIndex = 38;
            _Button1.Text = "Weiter";
            _Button1.UseVisualStyleBackColor = true;
            // 
            // FrmDatabaseAuswahl
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(6.0f, 13.0f);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(254, 71);
            Controls.Add(_Button1);
            Controls.Add(cbMain);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MaximumSize = new System.Drawing.Size(270, 110);
            MinimizeBox = false;
            MinimumSize = new System.Drawing.Size(270, 110);
            Name = "FrmDatabaseAuswahl";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Datenbank";
            Load += new EventHandler(FrmMain_Load);
            ResumeLayout(false);
        }

        internal ComboBox cbMain;
        private Button _Button1;

        internal Button Button1
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _Button1;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_Button1 != null)
                {
                    _Button1.Click -= btnSelect_Click;
                }

                _Button1 = value;
                if (_Button1 != null)
                {
                    _Button1.Click += btnSelect_Click;
                }
            }
        }
    }
}