Imports System.Media
Imports System.Security.AccessControl
Imports System.Security.Principal
'Imports wyDay.Controls

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FrmMain
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(FrmMain))
        Me.lbLog = New System.Windows.Forms.ListBox()
        Me.msMain = New System.Windows.Forms.MenuStrip()
        Me.tsmiFile = New System.Windows.Forms.ToolStripMenuItem()
        Me.tsmiSettings = New System.Windows.Forms.ToolStripMenuItem()
        Me.tsmiClearLogView = New System.Windows.Forms.ToolStripMenuItem()
        Me.tsmiCheckAdressLink = New System.Windows.Forms.ToolStripMenuItem()
        Me.tsmiClearLink = New System.Windows.Forms.ToolStripMenuItem()
        Me.tsmiClose = New System.Windows.Forms.ToolStripMenuItem()
        Me.lblLastSyncDate = New System.Windows.Forms.Label()
        Me.pbMain = New System.Windows.Forms.ProgressBar()
        Me.lblProffixMandant = New System.Windows.Forms.Label()
        Me.lblMandant = New System.Windows.Forms.Label()
        Me.pbLoading = New System.Windows.Forms.PictureBox()
        Me.gbMain = New System.Windows.Forms.GroupBox()
        Me.gbAccount = New System.Windows.Forms.GroupBox()
        Me.lblFLSAccount = New System.Windows.Forms.Label()
        Me.lblAccount = New System.Windows.Forms.Label()
        Me.gbFortschritt = New System.Windows.Forms.GroupBox()
        Me.BtnCancel = New System.Windows.Forms.Button()
        Me.groupSync = New System.Windows.Forms.GroupBox()
        Me.lblLastFlightImportDate = New System.Windows.Forms.Label()
        Me.lblLastSuccess = New System.Windows.Forms.Label()
        Me.cbAdressen = New System.Windows.Forms.CheckBox()
        Me.btnSync = New System.Windows.Forms.Button()
        Me.cbArtikel = New System.Windows.Forms.CheckBox()
        Me.lblLastDeliveryImportDate = New System.Windows.Forms.Label()
        Me.cbFluege = New System.Windows.Forms.CheckBox()
        Me.lblLastExportDate = New System.Windows.Forms.Label()
        Me.cbLieferscheine = New System.Windows.Forms.CheckBox()
        Me.gbLog = New System.Windows.Forms.GroupBox()
        Me.lblHelp = New System.Windows.Forms.Label()
        Me.tsmiLSManage = New System.Windows.Forms.ToolStripMenuItem()
        Me.msMain.SuspendLayout()
        CType(Me.pbLoading, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.gbMain.SuspendLayout()
        Me.gbAccount.SuspendLayout()
        Me.gbFortschritt.SuspendLayout()
        Me.groupSync.SuspendLayout()
        Me.gbLog.SuspendLayout()
        Me.SuspendLayout()
        '
        'lbLog
        '
        Me.lbLog.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lbLog.FormattingEnabled = True
        Me.lbLog.Location = New System.Drawing.Point(3, 16)
        Me.lbLog.MaximumSize = New System.Drawing.Size(2000, 2000)
        Me.lbLog.Name = "lbLog"
        Me.lbLog.Size = New System.Drawing.Size(538, 355)
        Me.lbLog.TabIndex = 0
        '
        'msMain
        '
        Me.msMain.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.tsmiFile})
        Me.msMain.Location = New System.Drawing.Point(0, 0)
        Me.msMain.Name = "msMain"
        Me.msMain.Size = New System.Drawing.Size(541, 24)
        Me.msMain.TabIndex = 10
        Me.msMain.Text = "MenuStrip1"
        '
        'tsmiFile
        '
        Me.tsmiFile.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.tsmiSettings, Me.tsmiClearLogView, Me.tsmiCheckAdressLink, Me.tsmiClearLink, Me.tsmiLSManage, Me.tsmiClose})
        Me.tsmiFile.Name = "tsmiFile"
        Me.tsmiFile.Size = New System.Drawing.Size(50, 20)
        Me.tsmiFile.Text = "Menu"
        '
        'tsmiSettings
        '
        Me.tsmiSettings.Name = "tsmiSettings"
        Me.tsmiSettings.Size = New System.Drawing.Size(230, 22)
        Me.tsmiSettings.Text = "Einstellungen"
        '
        'tsmiClearLogView
        '
        Me.tsmiClearLogView.Name = "tsmiClearLogView"
        Me.tsmiClearLogView.Size = New System.Drawing.Size(230, 22)
        Me.tsmiClearLogView.Text = "Log-Datei archivieren"
        '
        'tsmiCheckAdressLink
        '
        Me.tsmiCheckAdressLink.Name = "tsmiCheckAdressLink"
        Me.tsmiCheckAdressLink.Size = New System.Drawing.Size(230, 22)
        Me.tsmiCheckAdressLink.Text = "Adressverknüpfung prüfen"
        '
        'tsmiClearLink
        '
        Me.tsmiClearLink.Name = "tsmiClearLink"
        Me.tsmiClearLink.Size = New System.Drawing.Size(230, 22)
        Me.tsmiClearLink.Text = "Adressverknüpfung aufheben"
        '
        'tsmiClose
        '
        Me.tsmiClose.Name = "tsmiClose"
        Me.tsmiClose.Size = New System.Drawing.Size(230, 22)
        Me.tsmiClose.Text = "Beenden"
        '
        'lblLastSyncDate
        '
        Me.lblLastSyncDate.AutoSize = True
        Me.lblLastSyncDate.Location = New System.Drawing.Point(157, 40)
        Me.lblLastSyncDate.Name = "lblLastSyncDate"
        Me.lblLastSyncDate.Size = New System.Drawing.Size(58, 13)
        Me.lblLastSyncDate.TabIndex = 6
        Me.lblLastSyncDate.Text = "unbekannt"
        '
        'pbMain
        '
        Me.pbMain.BackColor = System.Drawing.Color.White
        Me.pbMain.Location = New System.Drawing.Point(9, 43)
        Me.pbMain.Name = "pbMain"
        Me.pbMain.Size = New System.Drawing.Size(214, 23)
        Me.pbMain.TabIndex = 7
        '
        'lblProffixMandant
        '
        Me.lblProffixMandant.AutoSize = True
        Me.lblProffixMandant.Location = New System.Drawing.Point(6, 33)
        Me.lblProffixMandant.Name = "lblProffixMandant"
        Me.lblProffixMandant.Size = New System.Drawing.Size(81, 13)
        Me.lblProffixMandant.TabIndex = 8
        Me.lblProffixMandant.Text = "Proffix Mandant"
        '
        'lblMandant
        '
        Me.lblMandant.AutoSize = True
        Me.lblMandant.Location = New System.Drawing.Point(144, 36)
        Me.lblMandant.Name = "lblMandant"
        Me.lblMandant.Size = New System.Drawing.Size(58, 13)
        Me.lblMandant.TabIndex = 9
        Me.lblMandant.Text = "unbekannt"
        '
        'pbLoading
        '
        Me.pbLoading.BackColor = System.Drawing.SystemColors.Control
        Me.pbLoading.Image = Global.FlsGliderSync.My.Resources.Resources.load
        Me.pbLoading.Location = New System.Drawing.Point(72, 13)
        Me.pbLoading.Name = "pbLoading"
        Me.pbLoading.Size = New System.Drawing.Size(24, 24)
        Me.pbLoading.TabIndex = 12
        Me.pbLoading.TabStop = False
        Me.pbLoading.Visible = False
        '
        'gbMain
        '
        Me.gbMain.Controls.Add(Me.gbAccount)
        Me.gbMain.Controls.Add(Me.gbFortschritt)
        Me.gbMain.Controls.Add(Me.groupSync)
        Me.gbMain.Dock = System.Windows.Forms.DockStyle.Top
        Me.gbMain.Location = New System.Drawing.Point(0, 24)
        Me.gbMain.Name = "gbMain"
        Me.gbMain.Size = New System.Drawing.Size(541, 192)
        Me.gbMain.TabIndex = 9
        Me.gbMain.TabStop = False
        '
        'gbAccount
        '
        Me.gbAccount.Controls.Add(Me.lblProffixMandant)
        Me.gbAccount.Controls.Add(Me.lblFLSAccount)
        Me.gbAccount.Controls.Add(Me.lblMandant)
        Me.gbAccount.Controls.Add(Me.lblAccount)
        Me.gbAccount.Location = New System.Drawing.Point(296, 19)
        Me.gbAccount.Name = "gbAccount"
        Me.gbAccount.Size = New System.Drawing.Size(233, 80)
        Me.gbAccount.TabIndex = 32
        Me.gbAccount.TabStop = False
        Me.gbAccount.Text = "Account"
        '
        'lblFLSAccount
        '
        Me.lblFLSAccount.AutoSize = True
        Me.lblFLSAccount.Location = New System.Drawing.Point(6, 59)
        Me.lblFLSAccount.Name = "lblFLSAccount"
        Me.lblFLSAccount.Size = New System.Drawing.Size(69, 13)
        Me.lblFLSAccount.TabIndex = 17
        Me.lblFLSAccount.Text = "FLS Account"
        '
        'lblAccount
        '
        Me.lblAccount.AutoSize = True
        Me.lblAccount.Location = New System.Drawing.Point(144, 62)
        Me.lblAccount.Name = "lblAccount"
        Me.lblAccount.Size = New System.Drawing.Size(58, 13)
        Me.lblAccount.TabIndex = 18
        Me.lblAccount.Text = "unbekannt"
        '
        'gbFortschritt
        '
        Me.gbFortschritt.Controls.Add(Me.pbMain)
        Me.gbFortschritt.Controls.Add(Me.BtnCancel)
        Me.gbFortschritt.Controls.Add(Me.pbLoading)
        Me.gbFortschritt.Location = New System.Drawing.Point(296, 105)
        Me.gbFortschritt.Name = "gbFortschritt"
        Me.gbFortschritt.Size = New System.Drawing.Size(233, 76)
        Me.gbFortschritt.TabIndex = 31
        Me.gbFortschritt.TabStop = False
        Me.gbFortschritt.Text = "Fortschritt"
        '
        'BtnCancel
        '
        Me.BtnCancel.Location = New System.Drawing.Point(147, 11)
        Me.BtnCancel.Name = "BtnCancel"
        Me.BtnCancel.Size = New System.Drawing.Size(75, 26)
        Me.BtnCancel.TabIndex = 22
        Me.BtnCancel.Text = "Beenden"
        Me.BtnCancel.UseVisualStyleBackColor = True
        '
        'groupSync
        '
        Me.groupSync.Controls.Add(Me.lblLastFlightImportDate)
        Me.groupSync.Controls.Add(Me.lblLastSuccess)
        Me.groupSync.Controls.Add(Me.cbAdressen)
        Me.groupSync.Controls.Add(Me.btnSync)
        Me.groupSync.Controls.Add(Me.cbArtikel)
        Me.groupSync.Controls.Add(Me.lblLastDeliveryImportDate)
        Me.groupSync.Controls.Add(Me.cbFluege)
        Me.groupSync.Controls.Add(Me.lblLastExportDate)
        Me.groupSync.Controls.Add(Me.cbLieferscheine)
        Me.groupSync.Controls.Add(Me.lblLastSyncDate)
        Me.groupSync.Location = New System.Drawing.Point(6, 18)
        Me.groupSync.Name = "groupSync"
        Me.groupSync.Size = New System.Drawing.Size(278, 163)
        Me.groupSync.TabIndex = 30
        Me.groupSync.TabStop = False
        Me.groupSync.Text = "Synchronisieren"
        '
        'lblLastFlightImportDate
        '
        Me.lblLastFlightImportDate.AutoSize = True
        Me.lblLastFlightImportDate.Location = New System.Drawing.Point(157, 109)
        Me.lblLastFlightImportDate.Name = "lblLastFlightImportDate"
        Me.lblLastFlightImportDate.Size = New System.Drawing.Size(58, 13)
        Me.lblLastFlightImportDate.TabIndex = 32
        Me.lblLastFlightImportDate.Text = "unbekannt"
        '
        'lblLastSuccess
        '
        Me.lblLastSuccess.AutoSize = True
        Me.lblLastSuccess.Location = New System.Drawing.Point(120, 13)
        Me.lblLastSuccess.Name = "lblLastSuccess"
        Me.lblLastSuccess.Size = New System.Drawing.Size(152, 13)
        Me.lblLastSuccess.TabIndex = 30
        Me.lblLastSuccess.Text = "letztmals erfolgreich ausgeführt"
        '
        'cbAdressen
        '
        Me.cbAdressen.AutoSize = True
        Me.cbAdressen.Checked = True
        Me.cbAdressen.CheckState = System.Windows.Forms.CheckState.Checked
        Me.cbAdressen.Location = New System.Drawing.Point(17, 36)
        Me.cbAdressen.Name = "cbAdressen"
        Me.cbAdressen.Size = New System.Drawing.Size(70, 17)
        Me.cbAdressen.TabIndex = 25
        Me.cbAdressen.Text = "Adressen"
        Me.cbAdressen.UseVisualStyleBackColor = True
        '
        'btnSync
        '
        Me.btnSync.BackColor = System.Drawing.SystemColors.ActiveCaption
        Me.btnSync.Location = New System.Drawing.Point(17, 128)
        Me.btnSync.Name = "btnSync"
        Me.btnSync.Size = New System.Drawing.Size(105, 23)
        Me.btnSync.TabIndex = 29
        Me.btnSync.Text = "Synchronisieren"
        Me.btnSync.UseVisualStyleBackColor = False
        '
        'cbArtikel
        '
        Me.cbArtikel.AutoSize = True
        Me.cbArtikel.Checked = True
        Me.cbArtikel.CheckState = System.Windows.Forms.CheckState.Checked
        Me.cbArtikel.Location = New System.Drawing.Point(17, 59)
        Me.cbArtikel.Name = "cbArtikel"
        Me.cbArtikel.Size = New System.Drawing.Size(55, 17)
        Me.cbArtikel.TabIndex = 26
        Me.cbArtikel.Text = "Artikel"
        Me.cbArtikel.UseVisualStyleBackColor = True
        '
        'lblLastDeliveryImportDate
        '
        Me.lblLastDeliveryImportDate.AutoSize = True
        Me.lblLastDeliveryImportDate.Location = New System.Drawing.Point(157, 86)
        Me.lblLastDeliveryImportDate.Name = "lblLastDeliveryImportDate"
        Me.lblLastDeliveryImportDate.Size = New System.Drawing.Size(58, 13)
        Me.lblLastDeliveryImportDate.TabIndex = 24
        Me.lblLastDeliveryImportDate.Text = "unbekannt"
        '
        'cbFluege
        '
        Me.cbFluege.AutoSize = True
        Me.cbFluege.Checked = True
        Me.cbFluege.CheckState = System.Windows.Forms.CheckState.Checked
        Me.cbFluege.Location = New System.Drawing.Point(17, 105)
        Me.cbFluege.Name = "cbFluege"
        Me.cbFluege.Size = New System.Drawing.Size(52, 17)
        Me.cbFluege.TabIndex = 28
        Me.cbFluege.Text = "Flüge"
        Me.cbFluege.UseVisualStyleBackColor = True
        '
        'lblLastExportDate
        '
        Me.lblLastExportDate.AutoSize = True
        Me.lblLastExportDate.Location = New System.Drawing.Point(157, 63)
        Me.lblLastExportDate.Name = "lblLastExportDate"
        Me.lblLastExportDate.Size = New System.Drawing.Size(58, 13)
        Me.lblLastExportDate.TabIndex = 20
        Me.lblLastExportDate.Text = "unbekannt"
        '
        'cbLieferscheine
        '
        Me.cbLieferscheine.AutoSize = True
        Me.cbLieferscheine.Checked = True
        Me.cbLieferscheine.CheckState = System.Windows.Forms.CheckState.Checked
        Me.cbLieferscheine.Location = New System.Drawing.Point(17, 82)
        Me.cbLieferscheine.Name = "cbLieferscheine"
        Me.cbLieferscheine.Size = New System.Drawing.Size(89, 17)
        Me.cbLieferscheine.TabIndex = 27
        Me.cbLieferscheine.Text = "Lieferscheine"
        Me.cbLieferscheine.UseVisualStyleBackColor = True
        '
        'gbLog
        '
        Me.gbLog.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.gbLog.Controls.Add(Me.lbLog)
        Me.gbLog.Location = New System.Drawing.Point(0, 222)
        Me.gbLog.MaximumSize = New System.Drawing.Size(2000, 2000)
        Me.gbLog.Name = "gbLog"
        Me.gbLog.Size = New System.Drawing.Size(541, 386)
        Me.gbLog.TabIndex = 8
        Me.gbLog.TabStop = False
        Me.gbLog.Text = "Log"
        '
        'lblHelp
        '
        Me.lblHelp.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblHelp.AutoSize = True
        Me.lblHelp.BackColor = System.Drawing.Color.Transparent
        Me.lblHelp.Location = New System.Drawing.Point(496, 3)
        Me.lblHelp.Name = "lblHelp"
        Me.lblHelp.Size = New System.Drawing.Size(28, 13)
        Me.lblHelp.TabIndex = 11
        Me.lblHelp.Text = "Hilfe"
        '
        'tsmiLSManage
        '
        Me.tsmiLSManage.Name = "tsmiLSManage"
        Me.tsmiLSManage.Size = New System.Drawing.Size(230, 22)
        Me.tsmiLSManage.Text = "Lieferscheine verwalten"
        '
        'FrmMain
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.SystemColors.Control
        Me.ClientSize = New System.Drawing.Size(541, 608)
        Me.Controls.Add(Me.lblHelp)
        Me.Controls.Add(Me.gbMain)
        Me.Controls.Add(Me.gbLog)
        Me.Controls.Add(Me.msMain)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MinimumSize = New System.Drawing.Size(373, 365)
        Me.Name = "FrmMain"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "PROFFIX - FLS Glider Sync"
        Me.msMain.ResumeLayout(False)
        Me.msMain.PerformLayout()
        CType(Me.pbLoading, System.ComponentModel.ISupportInitialize).EndInit()
        Me.gbMain.ResumeLayout(False)
        Me.gbAccount.ResumeLayout(False)
        Me.gbAccount.PerformLayout()
        Me.gbFortschritt.ResumeLayout(False)
        Me.groupSync.ResumeLayout(False)
        Me.groupSync.PerformLayout()
        Me.gbLog.ResumeLayout(False)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents lbLog As System.Windows.Forms.ListBox
    Friend WithEvents msMain As System.Windows.Forms.MenuStrip
    Friend WithEvents tsmiFile As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents tsmiSettings As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents tsmiClearLogView As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents tsmiClose As ToolStripMenuItem
    Friend WithEvents lblLastSyncDate As System.Windows.Forms.Label
    Friend WithEvents pbMain As System.Windows.Forms.ProgressBar
    Friend WithEvents lblProffixMandant As System.Windows.Forms.Label
    Friend WithEvents lblMandant As System.Windows.Forms.Label
    Friend WithEvents pbLoading As System.Windows.Forms.PictureBox
    Friend WithEvents gbMain As System.Windows.Forms.GroupBox
    Friend WithEvents gbLog As System.Windows.Forms.GroupBox
    Friend WithEvents lblAccount As System.Windows.Forms.Label
    Friend WithEvents lblFLSAccount As System.Windows.Forms.Label
    Friend WithEvents lblLastExportDate As System.Windows.Forms.Label
    Friend WithEvents tsmiCheckAdressLink As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents lblHelp As System.Windows.Forms.Label
    Friend WithEvents BtnCancel As System.Windows.Forms.Button
    Friend WithEvents groupSync As System.Windows.Forms.GroupBox
    Friend WithEvents cbAdressen As System.Windows.Forms.CheckBox
    Friend WithEvents btnSync As System.Windows.Forms.Button
    Friend WithEvents cbArtikel As System.Windows.Forms.CheckBox
    Friend WithEvents cbFluege As System.Windows.Forms.CheckBox
    Friend WithEvents cbLieferscheine As System.Windows.Forms.CheckBox
    Friend WithEvents lblLastDeliveryImportDate As System.Windows.Forms.Label
    Friend WithEvents lblLastFlightImportDate As System.Windows.Forms.Label
    Friend WithEvents gbAccount As System.Windows.Forms.GroupBox
    Friend WithEvents gbFortschritt As System.Windows.Forms.GroupBox
    Friend WithEvents lblLastSuccess As System.Windows.Forms.Label
    Friend WithEvents tsmiClearLink As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents tsmiLSManage As System.Windows.Forms.ToolStripMenuItem
End Class
