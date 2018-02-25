<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FrmFlightImport
    Inherits System.Windows.Forms.Form

    'Das Formular überschreibt den Löschvorgang, um die Komponentenliste zu bereinigen.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Wird vom Windows Form-Designer benötigt.
    Private components As System.ComponentModel.IContainer

    'Hinweis: Die folgende Prozedur ist für den Windows Form-Designer erforderlich.
    'Das Bearbeiten ist mit dem Windows Form-Designer möglich.  
    'Das Bearbeiten mit dem Code-Editor ist nicht möglich.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.btnFormerFlightImport = New System.Windows.Forms.Button()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.btnCancel = New System.Windows.Forms.Button()
        Me.dtpFlightVon = New System.Windows.Forms.DateTimePicker()
        Me.dtpFlightBis = New System.Windows.Forms.DateTimePicker()
        Me.GroupBox1.SuspendLayout()
        Me.SuspendLayout()
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(11, 26)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(28, 13)
        Me.Label2.TabIndex = 1
        Me.Label2.Text = "von:"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(16, 52)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(23, 13)
        Me.Label3.TabIndex = 2
        Me.Label3.Text = "bis:"
        '
        'btnFormerFlightImport
        '
        Me.btnFormerFlightImport.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.btnFormerFlightImport.Location = New System.Drawing.Point(151, 79)
        Me.btnFormerFlightImport.Name = "btnFormerFlightImport"
        Me.btnFormerFlightImport.Size = New System.Drawing.Size(104, 23)
        Me.btnFormerFlightImport.TabIndex = 5
        Me.btnFormerFlightImport.Text = "Flüge importieren"
        Me.btnFormerFlightImport.UseVisualStyleBackColor = True
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.dtpFlightBis)
        Me.GroupBox1.Controls.Add(Me.dtpFlightVon)
        Me.GroupBox1.Controls.Add(Me.btnCancel)
        Me.GroupBox1.Controls.Add(Me.btnFormerFlightImport)
        Me.GroupBox1.Controls.Add(Me.Label3)
        Me.GroupBox1.Controls.Add(Me.Label2)
        Me.GroupBox1.Location = New System.Drawing.Point(12, 12)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(273, 143)
        Me.GroupBox1.TabIndex = 6
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "Frühere Flüge importieren"
        '
        'btnCancel
        '
        Me.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.btnCancel.Location = New System.Drawing.Point(183, 110)
        Me.btnCancel.Name = "btnCancel"
        Me.btnCancel.Size = New System.Drawing.Size(72, 23)
        Me.btnCancel.TabIndex = 6
        Me.btnCancel.Text = "Abbrechen"
        Me.btnCancel.UseVisualStyleBackColor = True
        '
        'dtpFlightVon
        '
        Me.dtpFlightVon.Location = New System.Drawing.Point(55, 26)
        Me.dtpFlightVon.Name = "dtpFlightVon"
        Me.dtpFlightVon.Size = New System.Drawing.Size(200, 20)
        Me.dtpFlightVon.TabIndex = 7
        '
        'dtpFlightBis
        '
        Me.dtpFlightBis.Location = New System.Drawing.Point(55, 53)
        Me.dtpFlightBis.Name = "dtpFlightBis"
        Me.dtpFlightBis.Size = New System.Drawing.Size(200, 20)
        Me.dtpFlightBis.TabIndex = 8
        '
        'FrmFlightImport
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(299, 166)
        Me.Controls.Add(Me.GroupBox1)
        Me.Name = "FrmFlightImport"
        Me.Text = "Frühere Flüge laden"
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents btnFormerFlightImport As System.Windows.Forms.Button
    Friend WithEvents GroupBox1 As System.Windows.Forms.GroupBox
    Friend WithEvents btnCancel As System.Windows.Forms.Button
    Friend WithEvents dtpFlightBis As System.Windows.Forms.DateTimePicker
    Friend WithEvents dtpFlightVon As System.Windows.Forms.DateTimePicker
End Class
