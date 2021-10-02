// Imports System.ServiceModel
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json.Linq;
using pxBook;
using SMC.Lib;

namespace FlsGliderSync
{


    /// <summary>
/// Die Managerklasse zur Synchronisation der Adressen
/// </summary>
    public class Syncer
    {

        /// <summary>
    /// Das private HttpClient Objekt
    /// </summary>
        private HttpClient _httpClient { get; set; }
        /// <summary>
    /// Den Client des FLS
    /// </summary>
        private FlsConnection _serviceClient;
        private ProffixHelper pxhelper;
        private ProffixConnection myconn;
        private GeneralDataLoader generalLoader;
        private PersonLoader loader;
        private PersonUpdater updater;
        private PersonCreator creator;
        private PersonDeleter deleter;
        private ADODB.Recordset rs_adressdefault = new ADODB.Recordset();
        /// <summary>
    /// Die letzte Adressynchronisation
    /// </summary>
        private DateTime _lastSync = default;

        public DateTime LastSync
        {
            get
            {
                return _lastSync;
            }

            set
            {
                _lastSync = value;
            }
        }

        public Action DoProgress { get; set; }    // Aktion, die ausgeführt wird, wenn die Synchronisation Fortschritte macht

        private int _progress;    // Fortschritt der Synchronisation anzeigen

        public int Progress
        {
            get
            {
                return _progress;
            }

            private set
            {
                _progress = value;
            }
        }

        private int _count;

        public int Count
        {
            get
            {
                return _count;
            }

            private set
            {
                _count = value;
            }
        }

        public Action<string> Log { get; set; }    // Aktion, die ausgeführt wird, wenn in Logfeld geschrieben wird

        /// <summary>
    /// Initialisiert ein neues Objekt
    /// </summary>
    /// <param name="lastSync">Der Zeitpunkt der letzten Synchronisation</param>
    /// <param name="serviceClient">Der Client des FLS</param>
        public Syncer(DateTime lastSync, FlsConnection serviceClient, ref ProffixHelper pxHelper, ref ProffixConnection MyConn, GeneralDataLoader generalLoader)
        {
            _serviceClient = serviceClient;
            LastSync = lastSync;
            pxhelper = pxHelper;
            myconn = MyConn;
            this.generalLoader = generalLoader;
        }

        /// <summary>
    /// Ausführen der Adressen-Synchronisation
    /// </summary>
    /// <returns>Ein Boolean der definiert ob die Synchronisation erfolgreich war</returns>
        public bool Sync()
        {
            bool successful = true;                        // wird während Synchronisation auf false gesetzt, sobald für 1 Adresse ein Fehler auftritt
            string fehler = string.Empty;
            var FLShardDeletedPersons = new List<JObject>();       // alle jemals in FLS hart gelöschten Personen
            var FLSPersons = new List<JObject>();                  // alle in FLS vorhandenen Personen (IsActive true oder false)
            var PXadressen = new List<pxKommunikation.pxAdressen>();    // alle in Proffix vorhandenen Adressen (geloescht 0 oder 1)
            var addressWorkProgress = new Dictionary<int, bool>();
            // Dim sinceDate As DateTime

            try
            {

                // prüfen, ob ein Master gesetzt ist und nachfragen, ob wirklich so gewünscht
                if (FlsGliderSync.Master == UseAsMaster.fls | FlsGliderSync.Master == UseAsMaster.proffix)
                {
                    string mast;
                    string slave;
                    if (FlsGliderSync.Master == UseAsMaster.fls)
                    {
                        mast = "FLS";
                        slave = "Proffix";
                    }
                    else
                    {
                        mast = "Proffix";
                        slave = "FLS";
                    }

                    if (DialogResult.OK != MessageBox.Show("In der ini-Datei ist definiert, dass alle Adressen beim Update von " + mast + " zu " + slave + " übertragen werden sollen.", "Master ist gesetzt", MessageBoxButtons.OKCancel))
                    {
                        logComplete("Die Synchronisation wurde manuell abgebrochen", LogLevel.Exception);
                        return false;
                    }
                }

                // abfangen, wenn kein LastSync geladen wurde
                if (LastSync == default)
                {
                    logComplete("Es wurde kein LastSync-Datum gefunden.", LogLevel.Exception);
                    successful = false;
                    return false;
                }

                // Standardwerte für Adressen laden
                rs_adressdefault = pxhelper.GetPXAdressDefaultValues();
                if (rs_adressdefault is null)
                {
                    logComplete("Fehler beim Laden der Defaultwerte für Adressen", LogLevel.Exception);
                    return false;
                }

                // Objekte für Synchronisation erstellen
                loader = new PersonLoader(_serviceClient, Log);
                updater = new PersonUpdater(myconn, rs_adressdefault, pxhelper, _serviceClient, Log);
                deleter = new PersonDeleter(_serviceClient, pxhelper, Log);
                creator = new PersonCreator(myconn, rs_adressdefault, pxhelper, _serviceClient, LastSync, Log, updater, deleter);

                // Benutzerrückmeldung
                logComplete("Adresssynchronisation gestartet", LogLevel.Info);
                Progress = 0;
                InvokeDoProgress();

                // alle geladenen Personen in die Zusatztabelle laden (damit man auch in Proffix die Verbindung zwischen PersonId und Name sieht)
                if (!generalLoader.importPersons())
                {
                    logComplete("Fehler beim Importieren der Daten in ZUS_FLSPersons", LogLevel.Exception);
                }

                // *********************************************************************alle Daten laden***********************************************************************
                // lädt alle in die Funktion übergegebene Listen mit Werten
                if (!loader.datenLaden(ref FLShardDeletedPersons, ref FLSPersons, ref PXadressen))
                {
                    logComplete("Fehler beim Laden der Daten", LogLevel.Exception);
                    return false;
                }

                if (FlsGliderSync.logAusfuehrlich)
                {
                    Logger.GetInstance().Log(LogLevel.Info, "Anzahl FLS Adressen, die geladen wurden: " + FLSPersons.Count.ToString());
                    Logger.GetInstance().Log(LogLevel.Info, "Anzahl PX Adressen, die geladen wurden: " + PXadressen.Count.ToString());
                }

                // ******************************************************************Synchronisation********************************************************************************
                // Anzahl Adressen aus FLS und Proffix zusammenzählen
                Count = FLSPersons.Count + PXadressen.Count;
                // jede Adresse aus FLS (person) durchgehen
                foreach (JObject person in FLSPersons)
                {
                    try
                    {
                        if (FlsGliderSync.logAusfuehrlich)
                        {
                            Logger.GetInstance().Log(LogLevel.Info, "Geprüft wird FLS-Adresse Nachname: " + person["Lastname"].ToString() + " Vorname: " + person["Firstname"].ToString());
                        }

                        // prüfen, ob club-abhängige Daten erhalten (wenn der User in FLS System-Admin-Rechte hat, enthalten die JSONS keine clubrelevanten Daten)
                        if (person["ClubRelatedPersonDetails"] is null)
                        {
                            logComplete("Für die Personen wurden von FLS keine club-abhängigen Daten geliefert. Die Adresssynchronisation kann nicht ausgeführt werden. Kontaktieren Sie den Support", LogLevel.Exception, "FLS liefert keine clubrelatedPersonDetails. Möglicher Grund: User ist System-Admin. --> Kontrollieren in FLS unter Benutzer");
                            return false;
                        }

                        bool existsInProffix = false;          // Default setzen (wird später nur auf True gesetzt, wenn in PX gefunden)

                        // PersonId und Änderungsdatum auslesen
                        string flsPersonId = person["PersonId"].ToString().ToLower().Trim();

                        // Adresse suchen, die dieselbe PersonId hat
                        var adressemitPersonId = from address in PXadressen
                                                 where (flsPersonId ?? "") == (ProffixHelper.GetZusatzFelder(address, "Z_FLSPersonId").ToLower().Trim() ?? "")
                                                 select address;

                        // nur noch die eine bereits gefundene pxadresse mit der relevanten Personid abarbeiten
                        foreach (pxKommunikation.pxAdressen address in adressemitPersonId)
                        {

                            // die FLS-Adresse mit jeder Adresse aus Proffix vergleichen
                            // For Each address As pxKommunikation.pxAdressen In PXadressen


                            if (FlsGliderSync.logAusfuehrlich)
                            {
                                Logger.GetInstance().Log(LogLevel.Info, "Verglichen werden FLS-Adresse Nachname: " + person["Lastname"].ToString() + " Vorname: " + person["Firstname"].ToString() + " mit PX-Adresse " + address.AdressNr.ToString() + " Nachname: " + address.Name);
                            }

                            try
                            {

                                // PersonId auslesen
                                string proffixFLSPersonId = ProffixHelper.GetZusatzFelder(address, "Z_FLSPersonId");

                                // wenn PersonID (FLS) einen Wert hat --> Adresse in FLS neu erstellt, oder bereits synchronisiert 
                                // --> ist PersonID (FLS) = ZF_FLSPersonId (Proffix) --> Adresse aus FLS ist in Proffix bereits vorhanden
                                // wenn die Werte PersonID (FLS) und Zusatzfeld FLSPersonId (Proffix) identisch und PersonID (FLS) hat einen Wert...
                                if ((proffixFLSPersonId ?? "") == (flsPersonId ?? "") & !string.IsNullOrEmpty(flsPersonId))
                                {

                                    // *************************************************************in beiden vorhanden --> update*********************************************************************************************
                                    existsInProffix = true;

                                    // AdressNr der Proffixadresse zu _addressWorkProgress (enhält alle pxadressen, die bereits in FLS vorhanden sind) hinzufügen
                                    addressWorkProgress.Add(address.AdressNr, true);

                                    // prüfen, ob die Adresse synchronisiert werden soll (erst nach existInPX = true prüfen, da sonst = false bleibt --> geht in NurInFLS()
                                    int synchronizeOk;
                                    string Z_sync_dbvalue = ProffixHelper.GetZusatzFelder(address, "Z_Synchronisieren");
                                    try
                                    {
                                        synchronizeOk = Conversions.ToInteger(Z_sync_dbvalue);    // 0 oder 1 auslesen (catch, falls null) --> gilt als zu synchroniseren
                                    }
                                    catch (Exception ex)
                                    {
                                        synchronizeOk = 1;
                                    }

                                    if (synchronizeOk == 1)
                                    {

                                        // prüfen, ob AdressNr der PXAdresse NICHT mit der AdressNr der FLSPerson übereinstimmt oder in FLS NICHT NULL ist (wenn Adresse IsActive = false)
                                        if (!((FlsHelper.GetValOrDef(person, "ClubRelatedPersonDetails.MemberNumber") ?? "") == (address.AdressNr.ToString() ?? "")))
                                        {
                                            if (!string.IsNullOrEmpty(FlsHelper.GetValOrDef(person, "ClubRelatedPersonDetails.MemberNumber")))
                                            {
                                                logComplete("Die AdressNr in PX: " + address.AdressNr.ToString() + " stimmt nicht überein mit der VereinsmitgliedNr in FLS: " + FlsHelper.GetValOrDef(person, "ClubRelatedPersonDetails.MemberNumber") + " gemeinsame PersonId: " + person["PersonId"].ToString().ToLower().Trim() + "FLS-person: " + person.ToString(), LogLevel.Exception);
                                                break;
                                            }
                                        }

                                        // es ist ein Master definiert --> alle updates erfolgen von jener Seite her
                                        if (FlsGliderSync.Master == UseAsMaster.fls | FlsGliderSync.Master == UseAsMaster.proffix)
                                        {
                                            if (!updater.updateAccordingMaster(person, address, FlsGliderSync.Master))
                                            {
                                                successful = false;
                                            }
                                        }

                                        // es ist kein Master definiert --> updates erfolgen von der Seite her, wo die letzte Änderung gemacht wurde
                                        // die Adressen, welche in beiden Systemen bereits vorhanden sind, entsprechend updaten
                                        else if (!updater.updateAccordingDate(person, address, LastSync))
                                        {
                                            successful = false;
                                        }

                                        Progress += 1;
                                        InvokeDoProgress();
                                    }

                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                logComplete("Fehler beim updaten " + ex.Message + " " + person.ToString(), LogLevel.Exception);
                                successful = false;
                                continue;
                            }
                        }            // nächste Proffixadresse (address)

                        // ***********************************************************************nur in FLS ****************************************************************
                        // wenn PersonID leer oder PersonID (FLS)-Wert in Proffix unter ZF_FLSPersonId nicht gefunden wurde 
                        // --> Adresse noch nicht in Proffix vorhanden
                        if (existsInProffix == false)
                        {
                            if (FlsGliderSync.logAusfuehrlich)
                            {
                                Logger.GetInstance().Log(LogLevel.Info, "nur in FLS vorhanden: Nachname: " + person["Lastname"].ToString() + " Vorname: " + person["Firstname"].ToString());
                            }

                            try
                            {

                                // prüfen, ob die Adresse nicht bereits gelöscht vorhanden ist bzw. ob keine Duplikate entstehen
                                if (!creator.NurInFLS(person))
                                {
                                    successful = false;
                                }
                            }
                            catch (Exception ex)
                            {
                                logComplete("Fehler beim Erstellen in Proffix " + ex.Message + " " + person.ToString(), LogLevel.Exception);
                                successful = false;
                            }
                        }

                        Progress += 1;
                        InvokeDoProgress();
                    }
                    catch (Exception ex)
                    {
                        logComplete("Fehler beim Prüfen der Adresse " + ex.Message + " " + person.ToString(), LogLevel.Exception);
                        successful = false;
                        continue;
                    }
                }        // nächste FLS-Adresse (person)



                // **************************************************************************************CREATE IN FLS + PERSONID IN PROFFIX
                // _AddressWorkProgress enthält die AdressNr (Proffix) der Adressen, die in beiden sind.
                // --> Wenn anfangs weniger Adressen an beiden Orten vorhanden waren als in Proffix anfangs Adressen vorhanden waren 
                // --> es gibt noch Adressen, die nur in Proffix sind --> in FLS hinzufügen
                if (addressWorkProgress.Count < PXadressen.Count)
                {
                    if (FlsGliderSync.logAusfuehrlich)
                    {
                        Logger.GetInstance().Log(LogLevel.Info, "Verarbeitung von Adressen, die nur in PX vorhanden sind...");
                    }

                    // jede Adresse aus Proffix durchgehen
                    foreach (pxKommunikation.pxAdressen address in PXadressen)
                    {
                        try
                        {
                            if (FlsGliderSync.logAusfuehrlich)
                            {
                                Logger.GetInstance().Log(LogLevel.Info, "Nur in PX vorhanden: " + address.AdressNr.ToString() + " Nachname: " + address.Name);
                            }

                            // wenn Adresse bisher nur in Proffix vorhanden + seit LastSync verändert wurde + Synchronisieren ok ist
                            bool existsOnlyInPx = !addressWorkProgress.ContainsKey(address.AdressNr);
                            string Z_sync_dbvalue = ProffixHelper.GetZusatzFelder(address, "Z_Synchronisieren");
                            int synchronizeok;
                            try
                            {
                                synchronizeok = Conversions.ToInteger(Z_sync_dbvalue);    // 0 oder 1 auslesen (catch, falls null) --> gilt als zu synchroniseren
                            }
                            catch (Exception ex)
                            {
                                synchronizeok = 1;
                            }

                            try
                            {
                                // wenn die Adresse nur in PX existiert und synchronisiert werden soll
                                if (existsOnlyInPx & synchronizeok == 1)
                                {
                                    if (!creator.NurInPX(address, FLShardDeletedPersons))
                                    {
                                        successful = false;
                                    }

                                    Progress += 1;
                                    InvokeDoProgress();
                                }
                            }
                            catch (Exception ex)
                            {
                                logComplete("Fehler beim Erstellen in FLS " + ex.Message + " PX-AdressNr: " + address.AdressNr.ToString(), LogLevel.Exception);
                                successful = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Verarbeiten von PX-Adresse (existiert nur in PX) AdressNr: " + address.AdressNr.ToString() + " Name: " + address.Name);
                        }
                    }
                }

                Progress = Count;
                InvokeDoProgress();

                // wenn bis herhin alles geklappt --> geklappt immer noch true
                if (successful)
                {
                    logComplete("Adresssynchronsation erfolgreich beendet", LogLevel.Info);
                    LastSync = DateTime.Now;
                }
                else
                {
                    logComplete("Bei der Adresssynchronisation ist mindestens 1 Fehler aufgetreten. Deshalb wird das Datum der letzten Synchronsiation nicht angepasst.", LogLevel.Exception);
                }

                logComplete("", LogLevel.Info);
                return successful;
            }
            catch (FaultException faultExce)
            {
                Logger.GetInstance().Log(LogLevel.Exception, faultExce);
                throw faultExce;
            }
            // End If
            catch (Exception exce)
            {
                Logger.GetInstance().Log(LogLevel.Exception, exce);
                throw exce;
            }
        }

        // schreibt in Log und in Logger (File)
        private void logComplete(string logString, LogLevel loglevel, string zusatzloggerString = "")
        {
            if (Log is object)
                Log.Invoke((loglevel != LogLevel.Info ? Constants.vbTab : "") + logString);
            Logger.GetInstance().Log(loglevel, logString + " " + zusatzloggerString);
        }

        /// <summary>
    /// Synchronisationsfortschritt anzeigen
    /// </summary>
        private void InvokeDoProgress()
        {
            if (DoProgress is object)
                DoProgress.Invoke();
        }
    }
}