using System.Windows.Forms;
using SMC;

namespace FlsGliderSync
{

    /// <summary>
/// Die Globalen Attribute der Applikation
/// </summary>
    public static class FlsGliderSync
    {

        /// <summary>
    /// Die Verbindung zu PROFFIX
    /// </summary>
        internal static Proffix Proffix { get; set; } = new Proffix(ProffixCrypto.Decrypt(GeneralHelper.readFromIni("licenseKey"), My.MySettingsProperty.Settings.Crypto), "FlsGliderSync", Application.StartupPath + @"\");

        /// <summary>
    /// Die Helper klasse für die Verschlüsselung
    /// </summary>
        internal static ProffixCrypto ProffixCrypto { get; set; } = ProffixCrypto;

        // je nach Angabe in ini (1 = ausführlich, 0/"" = nicht ausführlich)
        internal static bool logAusfuehrlich { get; set; } = GeneralHelper.logAusfuehrlichSchreiben();

        // dieser Postfix wird in FLS an MemberNr angehängt, wenn Adresse nur noch in FLS und in PX ganz gelöscht wurde
        internal static string postfix { get; set; } = "_delInPX";

        // definiert, welche DB bei der 1. Synchronisation als Master gelten soll.
        // wenn im FlsGliderSync.ini master=fls oder master=proffix steht, ist die entsprechende DB master. ansonsten wird je nach Änderungsdatum geupdatet
        internal static UseAsMaster Master { get; set; }
    }

    public enum UseAsMaster
    {
        fls,
        proffix,
        undefined
    }
}