using System.Windows.Forms;

namespace FlsGliderSync
{
    public partial class FrmFlightImport
    {
        public FrmFlightImport()
        {
            InitializeComponent();
            KeyPreview = true;
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