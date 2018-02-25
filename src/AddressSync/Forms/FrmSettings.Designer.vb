<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FrmSettings
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(FrmSettings))
        Me.tbUser = New System.Windows.Forms.TextBox()
        Me.tbPassword = New System.Windows.Forms.TextBox()
        Me.lblUser = New System.Windows.Forms.Label()
        Me.lblPassword = New System.Windows.Forms.Label()
        Me.btnSave = New System.Windows.Forms.Button()
        Me.lblZugang = New System.Windows.Forms.Label()
        Me.tbZugang = New System.Windows.Forms.TextBox()
        Me.gbVerbindung = New System.Windows.Forms.GroupBox()
        Me.btnCancel = New System.Windows.Forms.Button()
        Me.gbVerbindung.SuspendLayout()
        Me.SuspendLayout()
        '
        'tbUser
        '
        Me.tbUser.Location = New System.Drawing.Point(75, 45)
        Me.tbUser.Name = "tbUser"
        Me.tbUser.Size = New System.Drawing.Size(188, 20)
        Me.tbUser.TabIndex = 4
        '
        'tbPassword
        '
        Me.tbPassword.Location = New System.Drawing.Point(75, 71)
        Me.tbPassword.Name = "tbPassword"
        Me.tbPassword.PasswordChar = Global.Microsoft.VisualBasic.ChrW(42)
        Me.tbPassword.Size = New System.Drawing.Size(188, 20)
        Me.tbPassword.TabIndex = 5
        '
        'lblUser
        '
        Me.lblUser.AutoSize = True
        Me.lblUser.Location = New System.Drawing.Point(10, 52)
        Me.lblUser.Name = "lblUser"
        Me.lblUser.Size = New System.Drawing.Size(55, 13)
        Me.lblUser.TabIndex = 6
        Me.lblUser.Text = "Username"
        '
        'lblPassword
        '
        Me.lblPassword.AutoSize = True
        Me.lblPassword.Location = New System.Drawing.Point(10, 78)
        Me.lblPassword.Name = "lblPassword"
        Me.lblPassword.Size = New System.Drawing.Size(53, 13)
        Me.lblPassword.TabIndex = 7
        Me.lblPassword.Text = "Password"
        '
        'btnSave
        '
        Me.btnSave.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.btnSave.Location = New System.Drawing.Point(12, 127)
        Me.btnSave.Name = "btnSave"
        Me.btnSave.Size = New System.Drawing.Size(75, 23)
        Me.btnSave.TabIndex = 8
        Me.btnSave.Text = "Speichern"
        Me.btnSave.UseVisualStyleBackColor = True
        '
        'lblZugang
        '
        Me.lblZugang.AutoSize = True
        Me.lblZugang.Location = New System.Drawing.Point(10, 26)
        Me.lblZugang.Name = "lblZugang"
        Me.lblZugang.Size = New System.Drawing.Size(44, 13)
        Me.lblZugang.TabIndex = 9
        Me.lblZugang.Text = "Zugang"
        '
        'tbZugang
        '
        Me.tbZugang.Enabled = False
        Me.tbZugang.Location = New System.Drawing.Point(75, 19)
        Me.tbZugang.Name = "tbZugang"
        Me.tbZugang.Size = New System.Drawing.Size(188, 20)
        Me.tbZugang.TabIndex = 10
        '
        'gbVerbindung
        '
        Me.gbVerbindung.Controls.Add(Me.tbZugang)
        Me.gbVerbindung.Controls.Add(Me.tbUser)
        Me.gbVerbindung.Controls.Add(Me.lblZugang)
        Me.gbVerbindung.Controls.Add(Me.tbPassword)
        Me.gbVerbindung.Controls.Add(Me.lblUser)
        Me.gbVerbindung.Controls.Add(Me.lblPassword)
        Me.gbVerbindung.Location = New System.Drawing.Point(12, 12)
        Me.gbVerbindung.Name = "gbVerbindung"
        Me.gbVerbindung.Size = New System.Drawing.Size(286, 109)
        Me.gbVerbindung.TabIndex = 11
        Me.gbVerbindung.TabStop = False
        Me.gbVerbindung.Text = "Verbindungseinstellungen"
        '
        'btnCancel
        '
        Me.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.btnCancel.Location = New System.Drawing.Point(106, 127)
        Me.btnCancel.Name = "btnCancel"
        Me.btnCancel.Size = New System.Drawing.Size(75, 23)
        Me.btnCancel.TabIndex = 14
        Me.btnCancel.Text = "Abbrechen"
        Me.btnCancel.UseVisualStyleBackColor = True
        '
        'FrmSettings
        '
        Me.AllowDrop = True
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
        Me.CancelButton = Me.btnCancel
        Me.ClientSize = New System.Drawing.Size(316, 165)
        Me.Controls.Add(Me.btnCancel)
        Me.Controls.Add(Me.gbVerbindung)
        Me.Controls.Add(Me.btnSave)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximumSize = New System.Drawing.Size(9999, 9999)
        Me.MinimumSize = New System.Drawing.Size(320, 150)
        Me.Name = "FrmSettings"
        Me.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Einstellungen"
        Me.gbVerbindung.ResumeLayout(False)
        Me.gbVerbindung.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents tbUser As System.Windows.Forms.TextBox
    Friend WithEvents tbPassword As System.Windows.Forms.TextBox
    Friend WithEvents lblUser As System.Windows.Forms.Label
    Friend WithEvents lblPassword As System.Windows.Forms.Label
    Friend WithEvents btnSave As System.Windows.Forms.Button
    Friend WithEvents lblZugang As System.Windows.Forms.Label
    Friend WithEvents tbZugang As System.Windows.Forms.TextBox
    Friend WithEvents gbVerbindung As System.Windows.Forms.GroupBox
    Friend WithEvents btnCancel As System.Windows.Forms.Button
End Class
