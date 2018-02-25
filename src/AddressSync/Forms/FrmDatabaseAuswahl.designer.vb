<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FrmDBAuswahl
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(FrmDBAuswahl))
        Me.cbMain = New System.Windows.Forms.ComboBox()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'cbMain
        '
        Me.cbMain.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cbMain.FormattingEnabled = True
        Me.cbMain.Location = New System.Drawing.Point(12, 12)
        Me.cbMain.Name = "cbMain"
        Me.cbMain.Size = New System.Drawing.Size(230, 21)
        Me.cbMain.TabIndex = 37
        '
        'Button1
        '
        Me.Button1.Location = New System.Drawing.Point(12, 39)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(230, 23)
        Me.Button1.TabIndex = 38
        Me.Button1.Text = "Weiter"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'FrmDatabaseAuswahl
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(254, 71)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.cbMain)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximizeBox = False
        Me.MaximumSize = New System.Drawing.Size(270, 110)
        Me.MinimizeBox = False
        Me.MinimumSize = New System.Drawing.Size(270, 110)
        Me.Name = "FrmDatabaseAuswahl"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Datenbank"
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents cbMain As System.Windows.Forms.ComboBox
    Friend WithEvents Button1 As System.Windows.Forms.Button
End Class
