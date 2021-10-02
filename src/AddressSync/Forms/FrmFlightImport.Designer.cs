using System.Diagnostics;
using System.Windows.Forms;

namespace FlsGliderSync
{
    [Microsoft.VisualBasic.CompilerServices.DesignerGenerated()]
    public partial class FrmFlightImport : Form
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
            Label2 = new Label();
            Label3 = new Label();
            btnFormerFlightImport = new Button();
            GroupBox1 = new GroupBox();
            btnCancel = new Button();
            dtpFlightVon = new DateTimePicker();
            dtpFlightBis = new DateTimePicker();
            GroupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // Label2
            // 
            Label2.AutoSize = true;
            Label2.Location = new System.Drawing.Point(11, 26);
            Label2.Name = "Label2";
            Label2.Size = new System.Drawing.Size(28, 13);
            Label2.TabIndex = 1;
            Label2.Text = "von:";
            // 
            // Label3
            // 
            Label3.AutoSize = true;
            Label3.Location = new System.Drawing.Point(16, 52);
            Label3.Name = "Label3";
            Label3.Size = new System.Drawing.Size(23, 13);
            Label3.TabIndex = 2;
            Label3.Text = "bis:";
            // 
            // btnFormerFlightImport
            // 
            btnFormerFlightImport.DialogResult = DialogResult.OK;
            btnFormerFlightImport.Location = new System.Drawing.Point(151, 79);
            btnFormerFlightImport.Name = "btnFormerFlightImport";
            btnFormerFlightImport.Size = new System.Drawing.Size(104, 23);
            btnFormerFlightImport.TabIndex = 5;
            btnFormerFlightImport.Text = "Flüge importieren";
            btnFormerFlightImport.UseVisualStyleBackColor = true;
            // 
            // GroupBox1
            // 
            GroupBox1.Controls.Add(dtpFlightBis);
            GroupBox1.Controls.Add(dtpFlightVon);
            GroupBox1.Controls.Add(btnCancel);
            GroupBox1.Controls.Add(btnFormerFlightImport);
            GroupBox1.Controls.Add(Label3);
            GroupBox1.Controls.Add(Label2);
            GroupBox1.Location = new System.Drawing.Point(12, 12);
            GroupBox1.Name = "GroupBox1";
            GroupBox1.Size = new System.Drawing.Size(273, 143);
            GroupBox1.TabIndex = 6;
            GroupBox1.TabStop = false;
            GroupBox1.Text = "Frühere Flüge importieren";
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new System.Drawing.Point(183, 110);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(72, 23);
            btnCancel.TabIndex = 6;
            btnCancel.Text = "Abbrechen";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // dtpFlightVon
            // 
            dtpFlightVon.Location = new System.Drawing.Point(55, 26);
            dtpFlightVon.Name = "dtpFlightVon";
            dtpFlightVon.Size = new System.Drawing.Size(200, 20);
            dtpFlightVon.TabIndex = 7;
            // 
            // dtpFlightBis
            // 
            dtpFlightBis.Location = new System.Drawing.Point(55, 53);
            dtpFlightBis.Name = "dtpFlightBis";
            dtpFlightBis.Size = new System.Drawing.Size(200, 20);
            dtpFlightBis.TabIndex = 8;
            // 
            // FrmFlightImport
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(6.0f, 13.0f);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(299, 166);
            Controls.Add(GroupBox1);
            Name = "FrmFlightImport";
            Text = "Frühere Flüge laden";
            GroupBox1.ResumeLayout(false);
            GroupBox1.PerformLayout();
            KeyDown += new KeyEventHandler(FrmSettings_KeyDown);
            ResumeLayout(false);
        }

        internal Label Label2;
        internal Label Label3;
        internal Button btnFormerFlightImport;
        internal GroupBox GroupBox1;
        internal Button btnCancel;
        internal DateTimePicker dtpFlightBis;
        internal DateTimePicker dtpFlightVon;
    }
}