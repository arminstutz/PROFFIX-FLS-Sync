using System;
using System.Linq;
using System.Windows.Forms;
using SMC.Lib;

namespace FlsGliderSync
{
    public partial class FrmDBAuswahl
    {
        public FrmDBAuswahl()
        {
            InitializeComponent();
            _Button1.Name = "Button1";
        }

        /// <summary>
    /// Der Dialog wird geladen und eine Auswahl von Mandanten angezeigt
    /// </summary>
    /// <param name="sender">Die Form</param>
    /// <param name="e">Informationen zum "Load" Event</param>
        private void FrmMain_Load(object sender, EventArgs e)
        {
            try
            {
                // abfangen, ob FLSGliderSync bereits geöffnet ist --> nicht nochmals öffnen
                GeneralHelper.startProgrammOnlyOnce();

                // LogDatei nach X Tagen in Unterordner verschieben (X = in ini definiert unter LogLoeschenNachTagen
                // GeneralHelper.logArchivieren()
                // auskommentiert, da es immer Fehlermeldung gibt, dass File bereits verwendet wird

                // prüfen, ob LogFile existiert
                // Dim path As String = Application.StartupPath() + "\" + Assembly.GetExecutingAssembly().GetName.Name + ".log"
                // If Not File.Exists(path) Then
                // File.Create(path)
                // Logger.GetInstance.Log("Log File erstellt")
                // End If


                // Try
                // Dim a As JObject = JObject.Parse(bspDelivery1Menge0)
                // Catch ex As Exception
                // MsgBox(ex.Message)
                // End Try

                // Lesen der Konfigurationsdateien
                if (!FlsGliderSync.Proffix.Settings.IsLoaded)
                {
                    FlsGliderSync.Proffix.Settings.Load();
                }
                // Datenbanken in das Dropdown füllen
                cbMain.DataSource = FlsGliderSync.Proffix.Settings.ProffixDataBases.Values.ToArray();
                Controls.Add(cbMain);
                cbMain.SelectedItem = cbMain.Items[0];

                // Klick simulieren da in dieser Applikation keine Auswahl getroffen werden darf
                if (cbMain.Items.Count == 1)
                {
                    Application.DoEvents();
                    btnSelect_Click(null, null);
                }
            }
            catch (Exception exce)
            {
                MessageBox.Show("Fehler beim Laden des Programms.");
                Logger.GetInstance().Log(LogLevel.Exception, exce.Message);
                // Falls ein Fehler auftritt wird die Konfigurationshilfe angezeigt
                handleExeception(exce);
                Environment.Exit(0);
            }
        }

        /// <summary>
    /// Die Datenbank wurde ausgewählt und "OK" geklickt
    /// </summary>
        private void btnSelect_Click(object sender, EventArgs e)
        {
            try
            {

                // Die Verbindung zu PROFFIX wird aufgebaut
                FlsGliderSync.Proffix.Settings.DefaultDataBase = cbMain.SelectedItem.ToString();
                FlsGliderSync.Proffix.LoadConnection = true;

                // Der Dialog wird versteckt 
                Hide();
                try
                {
                    var form = new FrmMain();
                    form.ShowDialog();
                }
                catch (Exception ex)
                {
                    // Falls ein Fehler auftrit wird der Benutzer informiert und details in die Logdatei geschrieben
                    MessageBox.Show("Beim Starten des Hauptfensters ist ein Fehler aufgetreten. Kontaktieren Sie den Support.");
                    Logger.GetInstance().Log(LogLevel.Critical, ex);
                }

                Close();
            }
            catch (Exception exce)
            {
                handleExeception(exce);
            }
        }

        /// <summary>
    /// Wenn ein Fehler beim Aufbau der Verbindung auftrit wird die Konfigurationshilfe angezeigt
    /// </summary>
    /// <param name="exce">Die Exception die geloggt wird</param>
        private void handleExeception(Exception exce)
        {
            Logger.GetInstance().Log(LogLevel.Critical, exce);
            Hide();
            MessageBox.Show("Beim Starten des Programms ist ein Fehler aufgetreten. Überprüfen sie ihre Konfigurationen.");
            // Dim frmHelp As New FrmHelp()
            // frmHelp.ShowDialog()
            Close();
        }
    }
}