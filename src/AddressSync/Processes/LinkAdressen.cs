

// Imports System.ServiceModel
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.ServiceModel;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json.Linq;
using pxBook;
using SMC.Lib;

namespace FlsGliderSync
{
    public enum SyncerCommitCommand
    {
        Update,
        Create,
        Delete
    }

    // verknüpft die Adressen aus FLS und Proffix anhand der Vor- und Nachnamen
    // muss vor der 1. Synchronisation ausgeführt werden

    public class LinkAdressen
    {
        private int _progress;
        private int _count;

        private HttpClient client { get; set; }

        private FlsConnection _serviceClient;
        private ProffixHelper pxhelper;
        private ProffixConnection myconn;
        private GeneralDataLoader _generalLoader;

        private DateTime lastSync { get; set; }

        private Dictionary<int, bool> _addressWorkProgress;
        private static Dictionary<string, string> _laender_dict = new Dictionary<string, string>();

        public Action DoProgress { get; set; }
        public Action<string> Log { get; set; }

        public Dictionary<int, bool> AddressWorkProgress
        {
            get
            {
                return _addressWorkProgress;
            }
        }

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

        public LinkAdressen(DateTime lastSync, FlsConnection serviceClient, ref ProffixHelper pxHelper, ref ProffixConnection MyConn, GeneralDataLoader generalLoader)
        {
            _serviceClient = serviceClient;
            pxhelper = pxHelper;
            myconn = MyConn;
            _generalLoader = generalLoader;
            this.lastSync = lastSync;
        }


        // **************************************************************************************Verlinken
        // Synchronisiert die PersonId und Adressnr/VereinsmitgliedNr anhand der Vor und Nachnamen
        public bool Link()
        {
            string sql = string.Empty;
            string fehler = string.Empty;
            string response_FLS = string.Empty;
            bool geklappt = true;
            List<pxKommunikation.pxAdressen> adressList;
            bool IsSamePerson = false;
            try
            {
                InvokeLog("Adressverknüpfung gestartet");
                Logger.GetInstance().Log(LogLevel.Info, "Adressverknüpfung gestartet. Sich nach Name und Adresse entsprechende Adressen aus FLS und Proffix werden verknüpft");
                Progress = 0;
                InvokeDoProgress();

                // ---------------------------------------------------------------------Daten holen-------------------------------------------------------
                string errorMessage = string.Empty;
                bool existsInProffix = false;
                _addressWorkProgress = new Dictionary<int, bool>();

                // Alle ungelöschten FLS Adressen holen, die seit lastSync verändert wurden
                // Dim personResult As Threading.Tasks.Task(Of JArray) = _serviceClient.CallAsyncAsJArray(My.Settings.ServiceAPIModifiedPersonFullDetailsMethod + DateTime.MinValue.ToString("yyyy-MM-dd"))
                var personResult = _serviceClient.CallAsyncAsJArray(My.MySettingsProperty.Settings.ServiceAPIModifiedPersonFullDetailsMethod + lastSync.ToString("yyyy-MM-dd"));
                personResult.Wait();

                // um Zeit zu sparen werden nicht pber pxBook sondern über adodb gekürzte (nur Name, Adresse, PersonId, AdressNr in pxAdressen ausgefüllt) geladen 
                // --> nur wenn wirklich verknüpft werden soll, wird für diese eine Adresse die ganze Adresse über pxBook geladen
                adressList = gekuerztePXAdressenLaden();

                // Anzahl Adressen aus FLS und Proffix zusammenzählen
                Count = adressList.Count + personResult.Result.Count;

                // ---------------------------------------------------------Daten vergleichen und synchronisieren-------------------------------------------------------------------
                // jede Adresse aus FLS (person) durchgehen
                foreach (JObject person in personResult.Result.Children())
                {

                    // es interessieren nur Adressen, die nach lastSync erstellt wurden
                    // If CDate(FlsHelper.GetPersonChangeDate(person)) > lastSync Then

                    string personname = FlsHelper.GetValOrDef(person, "Lastname").Trim();
                    string personvorname = FlsHelper.GetValOrDef(person, "Firstname").Trim();


                    // Linq testen. Vorselektion. Gehe in for each nur die Adressen durch, bei denen der Name und Vorname stimmt
                    var gleichnamigeAdressen = from address in adressList
                                               where (address.Name.Trim() ?? "") == (personname.Trim() ?? "") & (address.Vorname.Trim() ?? "") == (personvorname.Trim() ?? "")
                                               select address;
                    if (gleichnamigeAdressen.Count() == 0)
                    {
                        if (FlsGliderSync.logAusfuehrlich)
                        {
                            Logger.GetInstance().Log(LogLevel.Info, "Keine Adressen mit Name " + personname + " Vorname " + personvorname + " in PX vorhanden");
                        }

                        continue;
                    }

                    if (FlsGliderSync.logAusfuehrlich)
                    {
                        Logger.GetInstance().Log(LogLevel.Info, "Es wird mit " + gleichnamigeAdressen.Count().ToString() + " Adressen aus PX mit dem Namen " + personname + " Vornamen " + personvorname + " verglichen");
                    }

                    // die FLS-Adresse mit jeder Adresse aus Proffix vergleichen, (mit linq: nur noch die durchgehen, welche gleichen Name/Vorname haben)
                    foreach (pxKommunikation.pxAdressen address in gleichnamigeAdressen)
                    {
                        IsSamePerson = false;
                        var addressChangeDate = ProffixHelper.GetAddressChangeDate(address);
                        // es interessieren nur Adressen, die nach lastSync bearbeitet/erstellt wurden
                        if (Conversions.ToDate(addressChangeDate) > lastSync)
                        {

                            // Dim addressname As String = address.Name
                            // Dim addressvorname As String = address.Vorname

                            // wenn es laut Name/Vorname die gleichen Adressen sind
                            // If personname.Trim() = addressname.Trim() And personvorname.Trim() = addressvorname.Trim() Then

                            string personIdAusFLS = person["PersonId"].ToString().ToLower().Trim();
                            string adressNrAusFLS = FlsHelper.GetValOrDef(person, "ClubRelatedPersonDetails.MemberNumber").ToLower().Trim();
                            string personIdAusPX = pxhelper.GetPersonId(address.AdressNr.ToString()).ToLower().Trim();
                            string adressNrAusPX = address.AdressNr.ToString().ToLower().Trim();

                            // wenn FLS bereits eine AdressNr eingetragen hat --> prüfen, ob die Adressen bereits richtig verknüpft sind
                            if ((adressNrAusFLS ?? "") == (adressNrAusPX ?? ""))
                            {
                                if ((personIdAusFLS ?? "") == (personIdAusPX ?? ""))
                                {
                                    // wenn bereits richtig verknüpft --> nichts machen + unnötig zu prüfen, ob es gleiche Person ist
                                    break;
                                }

                                // FLS hat zwar bereits die PX-Adressnr, aber PX noch nicht die PersonId
                                else if ((personIdAusPX ?? "") == (string.Empty ?? ""))
                                {

                                    // adressNrAusFLS (MemberNumber) ist gleich wie AddressNrAusPX und Vorname und Nachname stimmt auch überein, PersonId ist aber noch leer in PX
                                    // --> schreibt PersonId in PX (+ schreibt bereits vorhandene AdressNr nochmals in FLS. Dies ist nötig, damit FLS als zu letzt geändert gilt, da zu Beginn nur FLS die Zusatzfeldinfos enthält)
                                    if (!verknuepfen(person, adressNrAusPX))
                                    {
                                        geklappt = false;
                                    }
                                    else
                                    {
                                        // Meldung dass erfolgreich
                                        InvokeLog("Name: " + address.Name + " Vorname: " + address.Vorname + " AdressNr: " + address.AdressNr.ToString() + " PersonId: " + person["PersonId"].ToString().ToLower().Trim() + " wurde verknüpft");
                                        Logger.GetInstance().Log(LogLevel.Info, "Name: " + address.Name + " Vorname: " + address.Vorname + " AdressNr: " + address.AdressNr.ToString() + " PersonId: " + person["PersonId"].ToString().ToLower().Trim() + "wurde verknüpft");
                                    }

                                    break;
                                }

                                // in PX hat PersonId einen Wert, aber nicht denselben wie aus FLS --> hat Wert drin, aber nicht denselben wie von FLS geliefert wird --> Fehler
                                else
                                {
                                    InvokeLog(Constants.vbTab + "Die PX-Adresse mit der AdressNr " + adressNrAusPX + " enthält die PersonId " + personIdAusPX + ". In FLS ist diese AdressNr aber für Person mit Id " + personIdAusFLS + " eingetragen. Löschen Sie in PX die PersonId, damit die AdressNr " + adressNrAusPX + " mit PersonId " + personIdAusFLS + " verknüpft werden kann.");
                                    Logger.GetInstance().Log(LogLevel.Exception, "Die PX-Adresse mit der AdressNr " + adressNrAusPX + " enthält die PersonId " + personIdAusPX + ". In FLS ist diese AdressNr aber für Person mit Id " + personIdAusFLS + " eingetragen. Löschen Sie in PX die PersonId, damit die AdressNr " + adressNrAusPX + " mit PersonId " + personIdAusFLS + " verknüpft werden kann.");
                                    geklappt = false;
                                    break;
                                }
                            }

                            // die Adressen sind noch nicht verknüpft

                            // es interessieren nur Adressen, die noch nie verknüpft wurden
                            if ((adressNrAusFLS ?? "") == (string.Empty ?? "") & (personIdAusPX ?? "") == (string.Empty ?? ""))
                            {

                                // prüfen ob gleiche Person (Strasse + Ort)
                                if (((FlsHelper.GetValOrDef(person, "AddressLine1").Trim() ?? "") == (address.Strasse.Trim() ?? "") | (FlsHelper.GetValOrDef(person, "AddressLine1").Trim() ?? "") == (address.Strasse.Trim().Replace("str.", "strasse") ?? "")) & (FlsHelper.GetValOrDef(person, "City").Trim() ?? "") == (address.Ort.Trim() ?? ""))

                                {

                                    // es ist die gleiche Person
                                    IsSamePerson = true;
                                }
                                // Vor- + Nachname + PLZ stimmen überein. Wenn auch noch PLZ übereinstimmt --> User fragen ob gleiche Person
                                else if ((FlsHelper.GetValOrDef(person, "ZipCode") ?? "") == (address.Plz ?? ""))
                                {
                                    var dialogres = MessageBox.Show("Handelt es sich bei folgenden Adressen um dieselbe Person?" + Constants.vbCrLf + Constants.vbCrLf + "Adresse aus FLS:" + Constants.vbCrLf + "PersonId: " + personIdAusFLS + Constants.vbCrLf + "AdressNr: " + adressNrAusFLS + Constants.vbCrLf + "Name: " + personname + Constants.vbCrLf + "Vorname: " + personvorname + Constants.vbCrLf + "Strasse: " + FlsHelper.GetValOrDef(person, "AddressLine1") + Constants.vbCrLf + "PLZ: " + FlsHelper.GetValOrDef(person, "ZipCode") + Constants.vbCrLf + "Ort: " + FlsHelper.GetValOrDef(person, "City") + Constants.vbCrLf + Constants.vbCrLf + "Adresse aus Proffix:" + Constants.vbCrLf + "AdressNr: " + adressNrAusPX + Constants.vbCrLf + "PersonId: " + personIdAusPX + Constants.vbCrLf + "Name: " + address.Name + Constants.vbCrLf + "Vorname: " + address.Vorname + Constants.vbCrLf + "Strasse: " + address.Strasse + Constants.vbCrLf + "PLZ: " + address.Plz + Constants.vbCrLf + "Ort: " + address.Ort + Constants.vbCrLf + Constants.vbCrLf + Constants.vbCrLf + "Klicken Sie ja, wenn es sich um dieselbe Person handelt, und die Adressen verknüpft werden sollen. " + Constants.vbCrLf + "Klicken Sie nein, wenn es sich nicht um dieselbe Person handelt. Sie werden dann jeweils im anderen System neu erstellt. " + Constants.vbCrLf + "Klicken Sie Abbrechen, um den Vorgang abzubrechen", "Dieselbe Person?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);

                                    // wenn User angibt, dass selbe Person --> verknüpfen
                                    if (dialogres == DialogResult.Yes)
                                    {
                                        IsSamePerson = true;
                                    }
                                    // wenn User angibt, dass nicht selbe Person (dialogresult.no) --> nichts machen --> Personen werden bei AdressSynchronisation im anderen System erstellt
                                    else if (dialogres == DialogResult.No)
                                    {
                                    }
                                    // wenn User abbricht oder Fenster schliesst --> Verknüpfung abbrechen
                                    else
                                    {
                                        return false;
                                    }
                                }

                                // wenn es sich um gleiche Person handelt (laut Programm, oder laut User)
                                if (IsSamePerson)
                                {
                                    if (!verknuepfen(person, adressNrAusPX))
                                    {
                                        geklappt = false;
                                    }
                                    else
                                    {
                                        // Meldung dass erfolgreich
                                        InvokeLog("Name: " + address.Name + " Vorname: " + address.Vorname + " AdressNr: " + address.AdressNr.ToString() + " PersonId: " + person["PersonId"].ToString().ToLower().Trim() + " wurde verknüpft");
                                        Logger.GetInstance().Log(LogLevel.Info, "Name: " + address.Name + " Vorname: " + address.Vorname + " AdressNr: " + address.AdressNr.ToString() + " PersonId: " + person["PersonId"].ToString().ToLower().Trim() + "wurde verknüpft");
                                    }
                                }
                            }
                            // End If
                        }
                    }
                    // End If
                }

                // Adressen, die nur in einem System vorhanden sind, werden hier ignoriert --> werden bei Adresssynchronisation im anderen System erstellt

                // wenn bis herhin alles geklappt --> geklappt immer noch true
                if (geklappt == true)
                {
                    InvokeLog("Adressverknüpfung erfolgreich abgeschlossen");
                }
                else
                {
                    InvokeLog("Mindestens 1 Fehler beim Verknüpfen der Adressen");
                }

                InvokeLog("");
                Progress = Count;
                InvokeDoProgress();
                Logger.GetInstance().Log(LogLevel.Info, "Adressverknüpfung beendet");
                return geklappt;
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
                throw;
            }
        }

        // verküpft 2 Adressen
        private bool verknuepfen(JObject person, string adressNr)
        {
            string fehler = string.Empty;
            try
            {

                // Dim vollstaendigeAdresse As pxKommunikation.pxAdressen() = New pxKommunikation.pxAdressen() {}

                // ' ganze Adresse aus PX holen
                // If Not Proffix.GoBook.GetAdresse(pxKommunikation.pxAdressSuchTyp.AdressNr, address.AdressNr.ToString, vollstaendigeAdresse, fehler) Then
                // ' die Adresse ist nicht unter den ungelöschten
                // If Not Proffix.GoBook.GetAdresse(pxKommunikation.pxAdressSuchTyp.AdressNr, address.AdressNr.ToString, vollstaendigeAdresse, fehler, pxKommunikation.pxGeloeschte.Geloeschte) Then
                // ' die Adresse ist nicht unter den ungelöschten oder gelöschten --> gar nicht vorhanden
                // Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Laden der Adresse für AdressNr " + address.AdressNr.ToString + " " + fehler)
                // Return False
                // End If
                // End If

                // ' wenn mehr als eine Adresse (+ die leere an Position 0) geladen wurde --> mehrere Adressen haben dieselbe PersonId --> Fehler
                // If vollstaendigeAdresse.Count > 2 Then
                // Logger.GetInstance.Log(LogLevel.Exception, "Es wurde mehr als 1 Adresse geladen für die AdressNr: " + address.AdressNr.ToString)
                // End If

                // PersonId aus FLS in PX schreiben
                if (!pxAktualisieren(person["PersonId"].ToString().ToLower().Trim(), adressNr))
                {
                    throw new Exception("Fehler in pxAktualisieren");
                }

                // AdressNr aus PX in FLS schreiben
                if (!flsAktualisieren(person, adressNr))
                {
                    throw new Exception("Fehler in flsAktualisieren");
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                InvokeLog(Constants.vbTab + "Fehler beim Verknüpfen der Adressen. PersonId:" + person["PersonId"].ToString().ToLower().Trim() + " AdressNr: " + adressNr);
                return false;
            }
        }


        // alle Adressen aus PX laden, egal ob geloeshct = 0 oder 1
        private List<pxKommunikation.pxAdressen> gekuerztePXAdressenLaden()
        {
            var adressList = new List<pxKommunikation.pxAdressen>();
            string sql = string.Empty;
            var rs = new ADODB.Recordset();
            string fehler = string.Empty;
            // Dim syncOk As Integer   ' ist die Adresse in PX als zu synchronisieren markiert (Zuatzfeld Z_Synchronisieren = 1)

            try
            {
                sql = "select * from adr_adressen where Z_synchronisieren = 1 order by name";
                if (!myconn.getRecord(ref rs, sql, ref fehler))
                {
                    Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Laden der Adressen aus Proffix");
                    return null;
                }

                while (!rs.EOF)
                {
                    var adresse = new pxKommunikation.pxAdressen();
                    adresse.AdressNr = Conversions.ToInteger(rs.Fields["AdressNrADR"].ToString());
                    adresse.Name = rs.Fields["Name"].ToString();
                    adresse.Vorname = rs.Fields["Vorname"].ToString();
                    adresse.Strasse = rs.Fields["Strasse"].ToString();
                    adresse.Plz = rs.Fields["PLZ"].ToString();
                    adresse.Ort = rs.Fields["Ort"].ToString();
                    adresse.ErstelltAm = rs.Fields["erstelltAm"].ToString();
                    adresse.GeaendertAm = rs.Fields["geaendertAm"].ToString();
                    adressList.Add(adresse);
                    rs.MoveNext();
                }

                return adressList;
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name);
                return null;
            }
        }

        // prüft, ob der Name in FLS oder Proffix mehr als 1x vorkommt
        public bool checkIfIsSamePerson(string nachname, string vorname, string strasseFLS, string ortFLS, string strassePX, string ortPX, System.Threading.Tasks.Task<JArray> personResult, List<pxKommunikation.pxAdressen> adressList)
        {
            if ((strasseFLS ?? "") == (strassePX ?? "") & (ortFLS ?? "") == (ortPX ?? ""))
            {
                return true;
            }

            string sql = string.Empty;
            var rs = new ADODB.Recordset();
            string fehler = string.Empty;
            int vorhandenFLS = 0;
            int vorhandenPX = 0;
            try
            {
                // ************************************************************************alle Adressen prüfen***********************************************************
                // zählen, wie oft eine Person mit diesem Vor- und Nachnamen in FLS vorkommt
                foreach (JObject person in personResult.Result.Children())
                {
                    if ((nachname ?? "") == (person["Lastname"].ToString() ?? "") & (vorname ?? "") == (person["Firstname"].ToString() ?? ""))
                    {
                        vorhandenFLS += 1;
                        if (vorhandenFLS > 1)
                        {
                            break;
                        }
                    }
                }

                // zählen, wie oft eine Person mit diesem Vor- und Nachnamen in Proffix vorkommt
                sql = "select count(*) as anzahl from adr_adressen where name = '" + nachname + "' and vorname = '" + vorname + "'";
                if (!myconn.getRecord(ref rs, sql, ref fehler))
                {
                    InvokeLog(Constants.vbTab + "Fehler beim Laden der Adressen für die Verlinkung");
                    Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Laden der Adressen für die Verlinkung");
                    throw new Exception("Fehler in " + MethodBase.GetCurrentMethod().Name);
                }

                vorhandenPX = Conversions.ToInteger(rs.Fields["anzahl"]);

                // *****************************************************************************Name nur 1x vorhanden*******************************************************
                // kommt in FLS und px nur je 1x vor
                if (vorhandenFLS == 1 & vorhandenPX == 1)
                {
                    return true;
                }

                // ****************************************************************************mehr als 1 mit gleichem Namen vorhanden************************************************
                // wenn mehrmals vorhanden --> stimmt Strasse + Ort überein?
                if ((strasseFLS ?? "") == (strassePX ?? "") & (ortFLS ?? "") == (ortPX ?? ""))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Fehler in " + MethodBase.GetCurrentMethod().Name + " " + ex.Message);
            }

            return default;
        }


        // AdressNr in FLS schreiben
        private bool flsAktualisieren(JObject person, string adressnr)
        {
            string response_FLS = string.Empty;

            // AdressNr in JSON schreiben
            person["ClubRelatedPersonDetails"]["MemberNumber"] = adressnr;
            person = (JObject)FlsHelper.removeMetadata(person);

            // in FLS updaten
            response_FLS = _serviceClient.SubmitChanges(person["PersonId"].ToString().ToLower().Trim(), person, SyncerCommitCommand.Update);
            if (response_FLS != "OK")
            {
                InvokeLog(Constants.vbTab + "Fehler: Die AdressNr(VereinsmitgliedNr)" + adressnr + " konnte in FLS nicht aktualisiert werden." + response_FLS);
                Logger.GetInstance().Log(LogLevel.Critical, "Die AdressNr(VereinsmitgliedNr) konnte in FLS nicht aktualisiert werden. AdressNr:" + adressnr + " " + response_FLS);
                return false;
            }

            return true;
        }


        // PersonId in PX schreiben
        private bool pxAktualisieren(string personid, string adressnr)
        {
            string fehler = string.Empty;
            var rs = new ADODB.Recordset();

            // nur PersonId über SQL updaten, damit Adresse in PX danach nicht als neuer gilt
            string Sql = "update ADR_Adressen set Z_FLSPersonId = '" + personid + "' where adressnradr = " + adressnr;
            if (!myconn.getRecord(ref rs, Sql, ref fehler))
            {
                InvokeLog(Constants.vbTab + "Fehler beim updaten der PersonId " + personid + " in Proffix für AdressNr " + adressnr);
                Logger.GetInstance().Log(LogLevel.Exception, "error in updating the PersonId " + personid + " in PX for AdressNr " + adressnr);
                return false;
            }

            // ' PersonId in pxAdresse schreiben
            // address = ProffixHelper.SetZusatzFelder(address, "Z_FLSPersonId", "Z_FLSPersonId", "", "", person("PersonId").ToString.ToLower.Trim)

            // ' in Proffix updaten
            // Proffix.GoBook.AddAdresse(address, fehler, False, ProffixHelper.CreateZusatzFelderSql(address))
            // If Not String.IsNullOrEmpty(fehler) Then
            // InvokeLog(vbTab + "Fehler: FLSPersonId konnte in Proffix nicht aktualisiert werden." + fehler)
            // Logger.GetInstance.Log(LogLevel.Critical, "FLSPersonId konnte in Proffix nicht geupdatet werden. AdressNr: " + CStr(address.AdressNr) + " " + fehler)
            // Return False
            // End If
            return true;
        }

        // ***********************************************************************************************Check Verlinkung*************************************************
        // vergleichen ob in ZUS_FLSPersons (Daten direkt aus FLS) und ADR_Adressen die PersonIds und Adressnr/VereinsmitgliedNr gleich zueinander vernknüpft sind.
        public void checkAddressLink()
        {
            string sql_ADRAdressen = string.Empty;
            string sql_FLSPersons = string.Empty;
            var rs_ADRAdressen = new ADODB.Recordset();
            var rs_FLSPersons = new ADODB.Recordset();
            string fehler = string.Empty;
            bool falscheVerknuepfungVorhanden = false;
            bool inBeidenVorhandenAnhandFLS = false;
            // Dim personResult As Threading.Tasks.Task(Of JArray)

            try
            {
                InvokeLog("Prüfung der Adressverknüpfung gestartet");
                Logger.GetInstance().Log(LogLevel.Info, "Prüfung der Adressverknüpfung gestartet");

                // ZUS_FLSPersons neu füllen
                if (!_generalLoader.importPersons())
                {
                    InvokeLog("Fehler beim Importieren der Daten in ZUS_FLSPersons");
                }

                // Alle ungelöschten FLS Adressen holen
                // personResult = _serviceClient.CallAsyncAsJArray(My.Settings.ServiceAPIModifiedPersonFullDetailsMethod + DateTime.MinValue.ToString("yyyy-MM-dd"))
                // personResult.Wait()

                // alle Adressen aus ADR_Adressen holen
                sql_ADRAdressen = "Select adressnradr, Z_FLSPersonId, name, vorname from ADR_Adressen where geloescht = 0";
                if (!myconn.getRecord(ref rs_ADRAdressen, sql_ADRAdressen, ref fehler))
                {
                    throw new Exception("Fehler beim Holen der Adressen aus ADR_Adressen");
                }

                // alle Adressen aus FLSPersons holen
                sql_FLSPersons = "Select * from ZUS_FLSPersons";
                if (!myconn.getRecord(ref rs_FLSPersons, sql_FLSPersons, ref fehler))
                {
                    throw new Exception("Fehler beim Holen der Personen aus ZUS_FLSPersons");
                }

                // miteinander vergleichen
                while (!rs_FLSPersons.EOF)
                {
                    while (!rs_ADRAdressen.EOF)
                    {

                        // wemm PersonId identisch --> prüfen, ob AdressNr  identisch
                        if ((rs_FLSPersons.Fields["PersonId"].ToString() ?? "") == (rs_ADRAdressen.Fields["Z_FLSPersonId"].ToString() ?? ""))
                        {

                            // Flag setzen
                            inBeidenVorhandenAnhandFLS = true;
                            // wenn die AdressNr nicht übereinstimmen --> Meldung
                            if (!((rs_ADRAdressen.Fields["AdressNrADR"].ToString() ?? "") == (rs_FLSPersons.Fields["VereinsmitgliedNrAdressNr"].ToString() ?? "")))
                            {
                                this.InvokeLog(Constants.vbTab + "Die Adresse mit der PersonId " + rs_ADRAdressen.Fields["Z_FLSPersonId"].ToString() + " hat in Proffix (" + rs_ADRAdressen.Fields["AdressNrADR"].ToString() + ") und FLS (" + rs_FLSPersons.Fields["VereinsmitgliedNrAdressNr"].ToString() + ") unterschiedliche AdressNr/VereinsmitgliedNr Nachname: " + rs_ADRAdressen.Fields["Name"].ToString() + " Vorname: " + rs_ADRAdressen.Fields["Vorname"].ToString());
                                Logger.GetInstance().Log(LogLevel.Exception, "Die Adresse mit der PersonId " + rs_ADRAdressen.Fields["Z_FLSPersonId"].ToString() + " hat in Proffix (" + rs_ADRAdressen.Fields["AdressNrADR"].ToString() + ") und FLS (" + rs_FLSPersons.Fields["VereinsmitgliedNrAdressNr"].ToString() + ") unterschiedliche AdressNr/VereinsmitgliedNr Nachname: " + rs_ADRAdressen.Fields["Name"].ToString() + " Vorname: " + rs_ADRAdressen.Fields["Vorname"].ToString());
                                falscheVerknuepfungVorhanden = true;
                            }

                            // entsprechende Adresse wurde gefunden (gleiche PersonId) --> weitere Adressen aus ADR_Adressen durchsuchen nicht mehr nötig
                            break;
                        }

                        rs_ADRAdressen.MoveNext();
                    }

                    rs_ADRAdressen.MoveFirst();
                    rs_FLSPersons.MoveNext();
                }

                // Schlussmeldung
                if (falscheVerknuepfungVorhanden)
                {
                    InvokeLog("Prüfung der Adressverknüpfung beendet. Es wurde mindestens 1 Fehler entdeckt, der bereits aufgeführt wurde.");
                    Logger.GetInstance().Log(LogLevel.Critical, "Prüfung der Adressverknüpfung beendet. Es wurde mindestens 1 Fehler entdeckt, der bereits aufgeführt wurde.");
                }
                else
                {
                    InvokeLog("Prüfung der Adressverknüpfung beendet. Alle Verknüpfungen sind korrekt.");
                    Logger.GetInstance().Log(LogLevel.Info, "Prüfung der Adressverknüpfung beendet. Alle Verknüpfungen sind korrekt.");
                }
            }
            catch (Exception ex)
            {
                InvokeLog(Constants.vbTab + "Fehler beim Überprüfen der Verknüpfungen der Adressen");
                Logger.GetInstance().Log(ex.Message);
            }
        }


        // !!!!!!!!!!!!!!!!!!¨Nur für Debuggen! gut überlegen ob wirklich ausführen!!
        public bool verknuepfungenAufheben()
        {
            string sql = string.Empty;
            var rs = new ADODB.Recordset();
            string fehler = string.Empty;
            System.Threading.Tasks.Task<JArray> personResult;
            try
            {
                if (FlsGliderSync.logAusfuehrlich)
                {
                    Logger.GetInstance().Log(LogLevel.Info, "in allen PX-Adressen wird die FLS-PersonId gelöscht");
                }
                // Für alle PX Adressen die PersonId löschen
                sql = "update adr_adressen set geaendertAm = '" + DateAndTime.Now.ToString(pxhelper.dateTimeFormat) + "', Z_FLSPersonId = null where Z_FLSPersonId is not null";
                if (!myconn.getRecord(ref rs, sql, ref fehler))
                {
                    InvokeLog("Fehler beim Löschen der FLSPersonId in Proffix");
                    Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Löschen der FLSPersonId in Proffix");
                    return false;
                }
                else
                {
                    InvokeLog("In Proffix wurden für alle Adressen die FLSPersonId erfolgreich gelöscht");
                    Logger.GetInstance().Log(LogLevel.Info, "In Proffix wurden für alle Adressen die FLSPersonId erfolgreich gelöscht");
                }

                // alle Adressen aus FLS laden (egal ob IsActive = true oder nicht)
                personResult = _serviceClient.CallAsyncAsJArray(My.MySettingsProperty.Settings.ServiceAPIModifiedPersonFullDetailsMethod + DateTime.MinValue.ToString("yyyy-MM-dd"));
                personResult.Wait();
                if (FlsGliderSync.logAusfuehrlich)
                {
                    Logger.GetInstance().Log(LogLevel.Info, "alle FLS Adressen geladen");
                }

                // für jede FLS Person
                foreach (JObject person in personResult.Result.Children())
                {

                    // MemberNumber löschen
                    if (!string.IsNullOrEmpty(FlsHelper.GetValOrDef(person, "ClubRelatedPersonDetails.MemberNumber")))
                    {
                        person["ClubRelatedPersonDetails"]["MemberNumber"] = null;
                        if (!(_serviceClient.SubmitChanges(person["PersonId"].ToString().ToLower().Trim(), person, SyncerCommitCommand.Update) == "OK"))
                        {
                            InvokeLog("Fehler beim Löschen der MemberNumber in FLS");
                            Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Löschen der MemberNumber in FLS" + person.ToString());
                            return false;
                        }
                        else
                        {
                            Logger.GetInstance().Log(LogLevel.Exception, "MemberNumber in FLS gelöscht für " + FlsHelper.GetValOrDef(person, "LastName") + " " + FlsHelper.GetValOrDef(person, "Firstname"));
                        }
                    }
                }

                InvokeLog("In FLS wurden für alle Adressne die Vereinsmitglied-Nr. erfolgreich gelöscht");
                return true;
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return false;
            }
        }


        /// <summary>
    /// Benutzerrückmeldung anzeigen
    /// </summary>
    /// <param name="message">Die Nachricht</param>
        private void InvokeLog(string message)
        {
            if (Log is object)
                Log.Invoke(message);
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