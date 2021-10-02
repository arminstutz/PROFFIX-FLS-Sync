using System.Windows.Forms;

namespace FlsGliderSync
{

    /// <summary>
/// Der Dialog der zum bearbeiten der Einstellungen verwendet wird
/// </summary>
    public partial class FrmSettings
    {

        // beim Start des Programms werden wird FrmSettings geladen aber nicht angezeigt 
        // --> Werte für DateTimePicker werden übernommen von anfangs geladenen Daten bzw. die Änderung, die das letzte Mal gemacht wurde, als das Frm angezeigt wurde
        public FrmSettings()
        {
            InitializeComponent();
            KeyPreview = true;

            // Logindaten anzeigen
            tbZugang.Text = My.MySettingsProperty.Settings.ServiceAPITokenMethod.ToString().Substring(0, My.MySettingsProperty.Settings.ServiceAPITokenMethod.Length - 5);

            // ' Default Daten setzen, aus welchem Bereich die Flugdaten importiert werden sollen
            // If Now.Month > 2 Then
            // dtpFrom.Value = New DateTime(Now.Year, 1, 1)
            // Else
            // dtpFrom.Value = New DateTime(Now.Year - 1, 1, 1)
            // End If
            // dtpTo.Value = DateSerial(Now.Year, Now.Month, 0)

        }

        // Wenn Enter bzw. ESC geklickt wird
        private void FrmSettings_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Escape)
            {
                DialogResult = DialogResult.OK;
            }
            else if (e.KeyData == Keys.Enter)
            {
                DialogResult = DialogResult.Cancel;
            }
        }
    }
}