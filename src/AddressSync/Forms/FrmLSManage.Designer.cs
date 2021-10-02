using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace FlsGliderSync
{
    [Microsoft.VisualBasic.CompilerServices.DesignerGenerated()]
    public partial class FrmLSManage : Form
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
            Label1 = new Label();
            _btnLSLoad = new Button();
            _btnLSLoad.Click += new EventHandler(btnLSLoad_Click);
            SuspendLayout();
            // 
            // Label1
            // 
            Label1.AutoSize = true;
            Label1.Location = new System.Drawing.Point(24, 13);
            Label1.Name = "Label1";
            Label1.Size = new System.Drawing.Size(252, 39);
            Label1.TabIndex = 0;
            Label1.Text = "Kann ein Lieferschein aus irgendwelchen Gründen " + '\r' + '\n' + "nicht aus FLS in PROFFIX import" + "iert werden, " + '\r' + '\n' + "kann hier entschieden werden, was mit ihm passiert.";
            // 
            // btnLSLoad
            // 
            _btnLSLoad.Location = new System.Drawing.Point(85, 74);
            _btnLSLoad.Name = "_btnLSLoad";
            _btnLSLoad.Size = new System.Drawing.Size(111, 23);
            _btnLSLoad.TabIndex = 1;
            _btnLSLoad.Text = "Lieferscheine laden";
            _btnLSLoad.UseVisualStyleBackColor = true;
            // 
            // FrmLSManage
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(6.0f, 13.0f);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(284, 108);
            Controls.Add(_btnLSLoad);
            Controls.Add(Label1);
            Name = "FrmLSManage";
            Text = "Lieferscheine verwalten";
            ResumeLayout(false);
            PerformLayout();
        }

        internal Label Label1;
        private Button _btnLSLoad;

        internal Button btnLSLoad
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _btnLSLoad;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_btnLSLoad != null)
                {
                    _btnLSLoad.Click -= btnLSLoad_Click;
                }

                _btnLSLoad = value;
                if (_btnLSLoad != null)
                {
                    _btnLSLoad.Click += btnLSLoad_Click;
                }
            }
        }
    }
}