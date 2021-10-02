using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Microsoft.VisualBasic.CompilerServices;

namespace FlsGliderSync
{
    // Imports wyDay.Controls

    [DesignerGenerated()]
    public partial class FrmMain : Form
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmMain));
            this._lbLog = new System.Windows.Forms.ListBox();
            this.msMain = new System.Windows.Forms.MenuStrip();
            this.tsmiFile = new System.Windows.Forms.ToolStripMenuItem();
            this._tsmiSettings = new System.Windows.Forms.ToolStripMenuItem();
            this._tsmiClearLogView = new System.Windows.Forms.ToolStripMenuItem();
            this._tsmiCheckAdressLink = new System.Windows.Forms.ToolStripMenuItem();
            this._tsmiClearLink = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiLSManage = new System.Windows.Forms.ToolStripMenuItem();
            this._tsmiClose = new System.Windows.Forms.ToolStripMenuItem();
            this.lblLastSyncDate = new System.Windows.Forms.Label();
            this.pbMain = new System.Windows.Forms.ProgressBar();
            this.lblProffixMandant = new System.Windows.Forms.Label();
            this.lblMandant = new System.Windows.Forms.Label();
            this.pbLoading = new System.Windows.Forms.PictureBox();
            this.gbMain = new System.Windows.Forms.GroupBox();
            this.gbAccount = new System.Windows.Forms.GroupBox();
            this.lblFLSAccount = new System.Windows.Forms.Label();
            this.lblAccount = new System.Windows.Forms.Label();
            this.gbFortschritt = new System.Windows.Forms.GroupBox();
            this._BtnCancel = new System.Windows.Forms.Button();
            this.groupSync = new System.Windows.Forms.GroupBox();
            this.lblLastFlightImportDate = new System.Windows.Forms.Label();
            this.lblLastSuccess = new System.Windows.Forms.Label();
            this._cbAdressen = new System.Windows.Forms.CheckBox();
            this._btnSync = new System.Windows.Forms.Button();
            this.cbArtikel = new System.Windows.Forms.CheckBox();
            this.lblLastDeliveryImportDate = new System.Windows.Forms.Label();
            this._cbFluege = new System.Windows.Forms.CheckBox();
            this.lblLastExportDate = new System.Windows.Forms.Label();
            this._cbLieferscheine = new System.Windows.Forms.CheckBox();
            this.gbLog = new System.Windows.Forms.GroupBox();
            this._lblHelp = new System.Windows.Forms.Label();
            this.msMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbLoading)).BeginInit();
            this.gbMain.SuspendLayout();
            this.gbAccount.SuspendLayout();
            this.gbFortschritt.SuspendLayout();
            this.groupSync.SuspendLayout();
            this.gbLog.SuspendLayout();
            this.SuspendLayout();
            // 
            // _lbLog
            // 
            this._lbLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._lbLog.FormattingEnabled = true;
            this._lbLog.ItemHeight = 31;
            this._lbLog.Location = new System.Drawing.Point(8, 38);
            this._lbLog.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this._lbLog.MaximumSize = new System.Drawing.Size(5327, 4764);
            this._lbLog.Name = "_lbLog";
            this._lbLog.Size = new System.Drawing.Size(1428, 841);
            this._lbLog.TabIndex = 0;
            this._lbLog.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.FrmMain_KeyDown);
            // 
            // msMain
            // 
            this.msMain.GripMargin = new System.Windows.Forms.Padding(2, 2, 0, 2);
            this.msMain.ImageScalingSize = new System.Drawing.Size(40, 40);
            this.msMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiFile});
            this.msMain.Location = new System.Drawing.Point(0, 0);
            this.msMain.Name = "msMain";
            this.msMain.Padding = new System.Windows.Forms.Padding(16, 5, 0, 5);
            this.msMain.Size = new System.Drawing.Size(1443, 55);
            this.msMain.TabIndex = 10;
            this.msMain.Text = "MenuStrip1";
            // 
            // tsmiFile
            // 
            this.tsmiFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._tsmiSettings,
            this._tsmiClearLogView,
            this._tsmiCheckAdressLink,
            this._tsmiClearLink,
            this.tsmiLSManage,
            this._tsmiClose});
            this.tsmiFile.Name = "tsmiFile";
            this.tsmiFile.Size = new System.Drawing.Size(119, 45);
            this.tsmiFile.Text = "Menu";
            // 
            // _tsmiSettings
            // 
            this._tsmiSettings.Name = "_tsmiSettings";
            this._tsmiSettings.Size = new System.Drawing.Size(574, 54);
            this._tsmiSettings.Text = "Einstellungen";
            this._tsmiSettings.Click += new System.EventHandler(this.tsmiSettings_Click);
            // 
            // _tsmiClearLogView
            // 
            this._tsmiClearLogView.Name = "_tsmiClearLogView";
            this._tsmiClearLogView.Size = new System.Drawing.Size(574, 54);
            this._tsmiClearLogView.Text = "Log-Datei archivieren";
            this._tsmiClearLogView.Click += new System.EventHandler(this.ClearLogToolStripMenuItem_Click);
            // 
            // _tsmiCheckAdressLink
            // 
            this._tsmiCheckAdressLink.Name = "_tsmiCheckAdressLink";
            this._tsmiCheckAdressLink.Size = new System.Drawing.Size(574, 54);
            this._tsmiCheckAdressLink.Text = "Adressverknüpfung prüfen";
            this._tsmiCheckAdressLink.Click += new System.EventHandler(this.tsmiCheckAdressLink_Click);
            // 
            // _tsmiClearLink
            // 
            this._tsmiClearLink.Name = "_tsmiClearLink";
            this._tsmiClearLink.Size = new System.Drawing.Size(574, 54);
            this._tsmiClearLink.Text = "Adressverknüpfung aufheben";
            this._tsmiClearLink.Click += new System.EventHandler(this.tsmiClearLink_Click);
            // 
            // tsmiLSManage
            // 
            this.tsmiLSManage.Name = "tsmiLSManage";
            this.tsmiLSManage.Size = new System.Drawing.Size(574, 54);
            this.tsmiLSManage.Text = "Lieferscheine verwalten";
            // 
            // _tsmiClose
            // 
            this._tsmiClose.Name = "_tsmiClose";
            this._tsmiClose.Size = new System.Drawing.Size(574, 54);
            this._tsmiClose.Text = "Beenden";
            this._tsmiClose.Click += new System.EventHandler(this.tsmiClose_Click);
            // 
            // lblLastSyncDate
            // 
            this.lblLastSyncDate.AutoSize = true;
            this.lblLastSyncDate.Location = new System.Drawing.Point(419, 95);
            this.lblLastSyncDate.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.lblLastSyncDate.Name = "lblLastSyncDate";
            this.lblLastSyncDate.Size = new System.Drawing.Size(149, 32);
            this.lblLastSyncDate.TabIndex = 6;
            this.lblLastSyncDate.Text = "unbekannt";
            // 
            // pbMain
            // 
            this.pbMain.BackColor = System.Drawing.Color.White;
            this.pbMain.Location = new System.Drawing.Point(24, 103);
            this.pbMain.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.pbMain.Name = "pbMain";
            this.pbMain.Size = new System.Drawing.Size(571, 55);
            this.pbMain.TabIndex = 7;
            // 
            // lblProffixMandant
            // 
            this.lblProffixMandant.AutoSize = true;
            this.lblProffixMandant.Location = new System.Drawing.Point(16, 79);
            this.lblProffixMandant.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.lblProffixMandant.Name = "lblProffixMandant";
            this.lblProffixMandant.Size = new System.Drawing.Size(214, 32);
            this.lblProffixMandant.TabIndex = 8;
            this.lblProffixMandant.Text = "Proffix Mandant";
            // 
            // lblMandant
            // 
            this.lblMandant.AutoSize = true;
            this.lblMandant.Location = new System.Drawing.Point(384, 86);
            this.lblMandant.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.lblMandant.Name = "lblMandant";
            this.lblMandant.Size = new System.Drawing.Size(149, 32);
            this.lblMandant.TabIndex = 9;
            this.lblMandant.Text = "unbekannt";
            // 
            // pbLoading
            // 
            this.pbLoading.BackColor = System.Drawing.SystemColors.Control;
            this.pbLoading.Image = global::FlsGliderSync.My.Resources.Resources.load;
            this.pbLoading.Location = new System.Drawing.Point(192, 31);
            this.pbLoading.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.pbLoading.Name = "pbLoading";
            this.pbLoading.Size = new System.Drawing.Size(64, 57);
            this.pbLoading.TabIndex = 12;
            this.pbLoading.TabStop = false;
            this.pbLoading.Visible = false;
            // 
            // gbMain
            // 
            this.gbMain.Controls.Add(this.gbAccount);
            this.gbMain.Controls.Add(this.gbFortschritt);
            this.gbMain.Controls.Add(this.groupSync);
            this.gbMain.Dock = System.Windows.Forms.DockStyle.Top;
            this.gbMain.Location = new System.Drawing.Point(0, 55);
            this.gbMain.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.gbMain.Name = "gbMain";
            this.gbMain.Padding = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.gbMain.Size = new System.Drawing.Size(1443, 458);
            this.gbMain.TabIndex = 9;
            this.gbMain.TabStop = false;
            // 
            // gbAccount
            // 
            this.gbAccount.Controls.Add(this.lblProffixMandant);
            this.gbAccount.Controls.Add(this.lblFLSAccount);
            this.gbAccount.Controls.Add(this.lblMandant);
            this.gbAccount.Controls.Add(this.lblAccount);
            this.gbAccount.Location = new System.Drawing.Point(789, 45);
            this.gbAccount.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.gbAccount.Name = "gbAccount";
            this.gbAccount.Padding = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.gbAccount.Size = new System.Drawing.Size(621, 191);
            this.gbAccount.TabIndex = 32;
            this.gbAccount.TabStop = false;
            this.gbAccount.Text = "Account";
            // 
            // lblFLSAccount
            // 
            this.lblFLSAccount.AutoSize = true;
            this.lblFLSAccount.Location = new System.Drawing.Point(16, 141);
            this.lblFLSAccount.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.lblFLSAccount.Name = "lblFLSAccount";
            this.lblFLSAccount.Size = new System.Drawing.Size(177, 32);
            this.lblFLSAccount.TabIndex = 17;
            this.lblFLSAccount.Text = "FLS Account";
            // 
            // lblAccount
            // 
            this.lblAccount.AutoSize = true;
            this.lblAccount.Location = new System.Drawing.Point(384, 148);
            this.lblAccount.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.lblAccount.Name = "lblAccount";
            this.lblAccount.Size = new System.Drawing.Size(149, 32);
            this.lblAccount.TabIndex = 18;
            this.lblAccount.Text = "unbekannt";
            // 
            // gbFortschritt
            // 
            this.gbFortschritt.Controls.Add(this.pbMain);
            this.gbFortschritt.Controls.Add(this._BtnCancel);
            this.gbFortschritt.Controls.Add(this.pbLoading);
            this.gbFortschritt.Location = new System.Drawing.Point(789, 250);
            this.gbFortschritt.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.gbFortschritt.Name = "gbFortschritt";
            this.gbFortschritt.Padding = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.gbFortschritt.Size = new System.Drawing.Size(621, 181);
            this.gbFortschritt.TabIndex = 31;
            this.gbFortschritt.TabStop = false;
            this.gbFortschritt.Text = "Fortschritt";
            // 
            // _BtnCancel
            // 
            this._BtnCancel.Location = new System.Drawing.Point(392, 26);
            this._BtnCancel.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this._BtnCancel.Name = "_BtnCancel";
            this._BtnCancel.Size = new System.Drawing.Size(200, 62);
            this._BtnCancel.TabIndex = 22;
            this._BtnCancel.Text = "Beenden";
            this._BtnCancel.UseVisualStyleBackColor = true;
            this._BtnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // groupSync
            // 
            this.groupSync.Controls.Add(this.lblLastFlightImportDate);
            this.groupSync.Controls.Add(this.lblLastSuccess);
            this.groupSync.Controls.Add(this._cbAdressen);
            this.groupSync.Controls.Add(this._btnSync);
            this.groupSync.Controls.Add(this.cbArtikel);
            this.groupSync.Controls.Add(this.lblLastDeliveryImportDate);
            this.groupSync.Controls.Add(this._cbFluege);
            this.groupSync.Controls.Add(this.lblLastExportDate);
            this.groupSync.Controls.Add(this._cbLieferscheine);
            this.groupSync.Controls.Add(this.lblLastSyncDate);
            this.groupSync.Location = new System.Drawing.Point(16, 43);
            this.groupSync.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.groupSync.Name = "groupSync";
            this.groupSync.Padding = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.groupSync.Size = new System.Drawing.Size(741, 389);
            this.groupSync.TabIndex = 30;
            this.groupSync.TabStop = false;
            this.groupSync.Text = "Synchronisieren";
            // 
            // lblLastFlightImportDate
            // 
            this.lblLastFlightImportDate.AutoSize = true;
            this.lblLastFlightImportDate.Location = new System.Drawing.Point(419, 260);
            this.lblLastFlightImportDate.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.lblLastFlightImportDate.Name = "lblLastFlightImportDate";
            this.lblLastFlightImportDate.Size = new System.Drawing.Size(149, 32);
            this.lblLastFlightImportDate.TabIndex = 32;
            this.lblLastFlightImportDate.Text = "unbekannt";
            // 
            // lblLastSuccess
            // 
            this.lblLastSuccess.AutoSize = true;
            this.lblLastSuccess.Location = new System.Drawing.Point(320, 31);
            this.lblLastSuccess.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.lblLastSuccess.Name = "lblLastSuccess";
            this.lblLastSuccess.Size = new System.Drawing.Size(411, 32);
            this.lblLastSuccess.TabIndex = 30;
            this.lblLastSuccess.Text = "letztmals erfolgreich ausgeführt";
            // 
            // _cbAdressen
            // 
            this._cbAdressen.AutoSize = true;
            this._cbAdressen.Checked = true;
            this._cbAdressen.CheckState = System.Windows.Forms.CheckState.Checked;
            this._cbAdressen.Location = new System.Drawing.Point(45, 86);
            this._cbAdressen.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this._cbAdressen.Name = "_cbAdressen";
            this._cbAdressen.Size = new System.Drawing.Size(173, 36);
            this._cbAdressen.TabIndex = 25;
            this._cbAdressen.Text = "Adressen";
            this._cbAdressen.UseVisualStyleBackColor = true;
            this._cbAdressen.CheckedChanged += new System.EventHandler(this.cbAdressen_CheckedChanged);
            // 
            // _btnSync
            // 
            this._btnSync.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this._btnSync.Location = new System.Drawing.Point(45, 305);
            this._btnSync.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this._btnSync.Name = "_btnSync";
            this._btnSync.Size = new System.Drawing.Size(280, 55);
            this._btnSync.TabIndex = 29;
            this._btnSync.Text = "Synchronisieren";
            this._btnSync.UseVisualStyleBackColor = false;
            this._btnSync.Click += new System.EventHandler(this.btnSync_Click);
            // 
            // cbArtikel
            // 
            this.cbArtikel.AutoSize = true;
            this.cbArtikel.Checked = true;
            this.cbArtikel.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbArtikel.Location = new System.Drawing.Point(45, 141);
            this.cbArtikel.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.cbArtikel.Name = "cbArtikel";
            this.cbArtikel.Size = new System.Drawing.Size(133, 36);
            this.cbArtikel.TabIndex = 26;
            this.cbArtikel.Text = "Artikel";
            this.cbArtikel.UseVisualStyleBackColor = true;
            this.cbArtikel.CheckedChanged += new System.EventHandler(this.cbArtikel_CheckedChanged);
            // 
            // lblLastDeliveryImportDate
            // 
            this.lblLastDeliveryImportDate.AutoSize = true;
            this.lblLastDeliveryImportDate.Location = new System.Drawing.Point(419, 205);
            this.lblLastDeliveryImportDate.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.lblLastDeliveryImportDate.Name = "lblLastDeliveryImportDate";
            this.lblLastDeliveryImportDate.Size = new System.Drawing.Size(149, 32);
            this.lblLastDeliveryImportDate.TabIndex = 24;
            this.lblLastDeliveryImportDate.Text = "unbekannt";
            // 
            // _cbFluege
            // 
            this._cbFluege.AutoSize = true;
            this._cbFluege.Checked = true;
            this._cbFluege.CheckState = System.Windows.Forms.CheckState.Checked;
            this._cbFluege.Location = new System.Drawing.Point(45, 250);
            this._cbFluege.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this._cbFluege.Name = "_cbFluege";
            this._cbFluege.Size = new System.Drawing.Size(125, 36);
            this._cbFluege.TabIndex = 28;
            this._cbFluege.Text = "Flüge";
            this._cbFluege.UseVisualStyleBackColor = true;
            this._cbFluege.CheckedChanged += new System.EventHandler(this.cbFluege_CheckedChanged);
            // 
            // lblLastExportDate
            // 
            this.lblLastExportDate.AutoSize = true;
            this.lblLastExportDate.Location = new System.Drawing.Point(419, 150);
            this.lblLastExportDate.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.lblLastExportDate.Name = "lblLastExportDate";
            this.lblLastExportDate.Size = new System.Drawing.Size(149, 32);
            this.lblLastExportDate.TabIndex = 20;
            this.lblLastExportDate.Text = "unbekannt";
            // 
            // _cbLieferscheine
            // 
            this._cbLieferscheine.AutoSize = true;
            this._cbLieferscheine.Checked = true;
            this._cbLieferscheine.CheckState = System.Windows.Forms.CheckState.Checked;
            this._cbLieferscheine.Location = new System.Drawing.Point(45, 196);
            this._cbLieferscheine.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this._cbLieferscheine.Name = "_cbLieferscheine";
            this._cbLieferscheine.Size = new System.Drawing.Size(224, 36);
            this._cbLieferscheine.TabIndex = 27;
            this._cbLieferscheine.Text = "Lieferscheine";
            this._cbLieferscheine.UseVisualStyleBackColor = true;
            this._cbLieferscheine.CheckedChanged += new System.EventHandler(this.cbLieferscheine_CheckedChanged);
            // 
            // gbLog
            // 
            this.gbLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbLog.Controls.Add(this._lbLog);
            this.gbLog.Location = new System.Drawing.Point(0, 529);
            this.gbLog.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.gbLog.MaximumSize = new System.Drawing.Size(5333, 4769);
            this.gbLog.Name = "gbLog";
            this.gbLog.Padding = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.gbLog.Size = new System.Drawing.Size(1443, 920);
            this.gbLog.TabIndex = 8;
            this.gbLog.TabStop = false;
            this.gbLog.Text = "Log";
            // 
            // _lblHelp
            // 
            this._lblHelp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._lblHelp.AutoSize = true;
            this._lblHelp.BackColor = System.Drawing.Color.Transparent;
            this._lblHelp.Location = new System.Drawing.Point(1323, 7);
            this._lblHelp.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this._lblHelp.Name = "_lblHelp";
            this._lblHelp.Size = new System.Drawing.Size(73, 32);
            this._lblHelp.TabIndex = 11;
            this._lblHelp.Text = "Hilfe";
            this._lblHelp.Click += new System.EventHandler(this.lblHelp_Click);
            // 
            // FrmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(16F, 31F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(1443, 1450);
            this.Controls.Add(this._lblHelp);
            this.Controls.Add(this.gbMain);
            this.Controls.Add(this.gbLog);
            this.Controls.Add(this.msMain);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(8, 7, 8, 7);
            this.MinimumSize = new System.Drawing.Size(941, 749);
            this.Name = "FrmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "PROFFIX - FLS Glider Sync";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmMain_FormClosing);
            this.msMain.ResumeLayout(false);
            this.msMain.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbLoading)).EndInit();
            this.gbMain.ResumeLayout(false);
            this.gbAccount.ResumeLayout(false);
            this.gbAccount.PerformLayout();
            this.gbFortschritt.ResumeLayout(false);
            this.groupSync.ResumeLayout(false);
            this.groupSync.PerformLayout();
            this.gbLog.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private ListBox _lbLog;

        internal ListBox lbLog
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lbLog;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lbLog != null)
                {
                    _lbLog.KeyPress -= FrmMain_KeyDown;
                }

                _lbLog = value;
                if (_lbLog != null)
                {
                    _lbLog.KeyPress += FrmMain_KeyDown;
                }
            }
        }

        internal MenuStrip msMain;
        internal ToolStripMenuItem tsmiFile;
        private ToolStripMenuItem _tsmiSettings;

        internal ToolStripMenuItem tsmiSettings
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _tsmiSettings;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_tsmiSettings != null)
                {
                    _tsmiSettings.Click -= tsmiSettings_Click;
                }

                _tsmiSettings = value;
                if (_tsmiSettings != null)
                {
                    _tsmiSettings.Click += tsmiSettings_Click;
                }
            }
        }

        private ToolStripMenuItem _tsmiClearLogView;

        internal ToolStripMenuItem tsmiClearLogView
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _tsmiClearLogView;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_tsmiClearLogView != null)
                {
                    _tsmiClearLogView.Click -= ClearLogToolStripMenuItem_Click;
                }

                _tsmiClearLogView = value;
                if (_tsmiClearLogView != null)
                {
                    _tsmiClearLogView.Click += ClearLogToolStripMenuItem_Click;
                }
            }
        }

        private ToolStripMenuItem _tsmiClose;

        internal ToolStripMenuItem tsmiClose
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _tsmiClose;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_tsmiClose != null)
                {
                    _tsmiClose.Click -= tsmiClose_Click;
                }

                _tsmiClose = value;
                if (_tsmiClose != null)
                {
                    _tsmiClose.Click += tsmiClose_Click;
                }
            }
        }

        internal Label lblLastSyncDate;
        internal ProgressBar pbMain;
        internal Label lblProffixMandant;
        internal Label lblMandant;
        internal PictureBox pbLoading;
        internal GroupBox gbMain;
        internal GroupBox gbLog;
        internal Label lblAccount;
        internal Label lblFLSAccount;
        internal Label lblLastExportDate;
        private ToolStripMenuItem _tsmiCheckAdressLink;

        internal ToolStripMenuItem tsmiCheckAdressLink
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _tsmiCheckAdressLink;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_tsmiCheckAdressLink != null)
                {
                    _tsmiCheckAdressLink.Click -= tsmiCheckAdressLink_Click;
                }

                _tsmiCheckAdressLink = value;
                if (_tsmiCheckAdressLink != null)
                {
                    _tsmiCheckAdressLink.Click += tsmiCheckAdressLink_Click;
                }
            }
        }

        private Label _lblHelp;

        internal Label lblHelp
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _lblHelp;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_lblHelp != null)
                {
                    _lblHelp.Click -= lblHelp_Click;
                }

                _lblHelp = value;
                if (_lblHelp != null)
                {
                    _lblHelp.Click += lblHelp_Click;
                }
            }
        }

        private Button _BtnCancel;

        internal Button BtnCancel
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _BtnCancel;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_BtnCancel != null)
                {
                    _BtnCancel.Click -= BtnCancel_Click;
                }

                _BtnCancel = value;
                if (_BtnCancel != null)
                {
                    _BtnCancel.Click += BtnCancel_Click;
                }
            }
        }

        internal GroupBox groupSync;
        private CheckBox _cbAdressen;

        internal CheckBox cbAdressen
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _cbAdressen;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_cbAdressen != null)
                {
                    _cbAdressen.CheckedChanged -= cbAdressen_CheckedChanged;
                }

                _cbAdressen = value;
                if (_cbAdressen != null)
                {
                    _cbAdressen.CheckedChanged += cbAdressen_CheckedChanged;
                }
            }
        }

        private Button _btnSync;

        internal Button btnSync
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _btnSync;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_btnSync != null)
                {
                    _btnSync.Click -= btnSync_Click;
                }

                _btnSync = value;
                if (_btnSync != null)
                {
                    _btnSync.Click += btnSync_Click;
                }
            }
        }

        internal CheckBox cbArtikel;
        private CheckBox _cbFluege;

        internal CheckBox cbFluege
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _cbFluege;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_cbFluege != null)
                {
                    _cbFluege.CheckedChanged -= cbFluege_CheckedChanged;
                }

                _cbFluege = value;
                if (_cbFluege != null)
                {
                    _cbFluege.CheckedChanged += cbFluege_CheckedChanged;
                }
            }
        }

        private CheckBox _cbLieferscheine;

        internal CheckBox cbLieferscheine
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _cbLieferscheine;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_cbLieferscheine != null)
                {
                    _cbLieferscheine.CheckedChanged -= cbLieferscheine_CheckedChanged;
                }

                _cbLieferscheine = value;
                if (_cbLieferscheine != null)
                {
                    _cbLieferscheine.CheckedChanged += cbLieferscheine_CheckedChanged;
                }
            }
        }

        internal Label lblLastDeliveryImportDate;
        internal Label lblLastFlightImportDate;
        internal GroupBox gbAccount;
        internal GroupBox gbFortschritt;
        internal Label lblLastSuccess;
        private ToolStripMenuItem _tsmiClearLink;

        internal ToolStripMenuItem tsmiClearLink
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _tsmiClearLink;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_tsmiClearLink != null)
                {
                    _tsmiClearLink.Click -= tsmiClearLink_Click;
                }

                _tsmiClearLink = value;
                if (_tsmiClearLink != null)
                {
                    _tsmiClearLink.Click += tsmiClearLink_Click;
                }
            }
        }

        internal ToolStripMenuItem tsmiLSManage;
    }
}