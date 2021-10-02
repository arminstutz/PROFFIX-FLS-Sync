using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json.Linq;
using SMC.Lib;

namespace FlsGliderSync
{

    /// <summary>
/// Die Managerklasse die für den Download und die Verrechnung der Flugdaten benutzt wird
/// </summary>
    public class Importer
    {
        public FlsConnection client { get; set; }
        public ProffixHelper pxhelper { get; set; }
        public ProffixConnection MyConn { get; set; }
        public Action<string> Log { get; set; }
        public DateTime lastDeliveryImport { get; set; }
        public DateTime lastFlightImport { get; set; }
        // Public Property bisFlightImport As DateTime

        // Public Property DoProgress As Action    ' Aktion, die ausgeführt wird, wenn der Import Fortschritte macht
        public Action DoProgressDelivery { get; set; }    // Aktion, die ausgeführt wird, wenn der Import Fortschritte macht
        public Action DoProgressFlight { get; set; }    // Aktion, die ausgeführt wird, wenn der Import Fortschritte macht

        private int _progressDelivery;    // Fortschritt der Synchronisation anzeigen

        public int ProgressDelivery
        {
            get
            {
                return _progressDelivery;
            }

            private set
            {
                _progressDelivery = value;
            }
        }

        private int _progressFlight;    // Fortschritt der Synchronisation anzeigen

        public int ProgressFlight
        {
            get
            {
                return _progressFlight;
            }

            private set
            {
                _progressFlight = value;
            }
        }

        private int _deliverycount;

        public int DeliveryCount
        {
            get
            {
                return _deliverycount;
            }

            private set
            {
                _deliverycount = value;
            }
        }

        private int _flightcount;

        public int FlightCount
        {
            get
            {
                return _flightcount;
            }

            private set
            {
                _flightcount = value;
            }
        }

        public Importer(ref DateTime lastDeliveryImport, ref DateTime lastFlightImport, ref FlsConnection client, ref ProffixHelper pxhelper, ref ProffixConnection MyConn)
        {
            this.lastDeliveryImport = lastDeliveryImport;
            this.lastFlightImport = lastFlightImport;
            this.client = client;
            this.pxhelper = pxhelper;
            this.MyConn = MyConn;
        }

        // **************************************************************************FlightImport*****************************************************************************************
        /// <summary>
    /// Donwload der Flugdaten in die ZUS_flight
    /// </summary>
    /// <returns>Ein Boolean ob der Import erfolgreich war</returns>
        public bool FlightImport()
        {
            List<JObject> modifiedFlights; // Flugdaten aus FLS (zur Verrechnung freigegeben)
            int dokNr = 0;
            string fehler = string.Empty;
            var lastChangeDate = DateTime.MinValue;
            bool successful = true;
            try
            {
                InvokeLog("Flugdatenimport gestartet");
                Logger.GetInstance().Log(LogLevel.Info, "Flugdatenimport gestartet");

                // Flüge laden, die seit letztem Flugimport erstellt/verändert wurden
                modifiedFlights = loadModifiedFlights();
                FlightCount = modifiedFlights.Count;
                ProgressFlight = 0;
                InvokeDoProgressFlight();

                // Iteration durch die Flüge, die zu verrechnen sind
                foreach (JObject flight in modifiedFlights)
                {

                    // von abgebrochenen Flugimporten vorhandene Daten für die MasterFlightId löschen
                    if (!pxhelper.deleteIncompleteFlightData(flight["FlightId"].ToString()))
                    {
                        InvokeLog("Fehler beim Löschen der Daten für FlugId " + flight["FlightId"].ToString());
                        successful = false;
                    }

                    // Daten für die MasterFlightId in Zusatztabelle importieren
                    if (!SetFlightData(flight))
                    {
                        InvokeLog(Constants.vbTab + "Fehler beim Importieren der Flugdaten oder Erstellen der Dokumente für FlightId: " + flight["FlightId"].ToString());
                        if (!pxhelper.deleteIncompleteFlightData(flight["FlightId"].ToString()))
                        {
                            InvokeLog("Fehler beim Löschen der Daten für FlugId " + flight["FlightId"].ToString());
                        }

                        successful = false;
                    }
                    else
                    {
                        InvokeLog("Der Flug mit der FlightId " + flight["FlightId"].ToString() + " wurde importiert");
                    }

                    ProgressFlight += 1;
                    InvokeDoProgressFlight();
                }

                // Logger.GetInstance.Log(LogLevel.Info, "flugdaten werden gelöscht")
                Logger.GetInstance().Log("Allfällige Daten für noch zu importierte " + modifiedFlights.Children().Count().ToString() + " Flüge wurden gelöscht");


                // wenn bis hierher gekommen --> Flugdatenimport hat für alle FlightIds geklappt
                ProgressFlight = FlightCount;
                InvokeDoProgressFlight();

                // nur wenn kein Fehler aufgetreten ist, Now() als letzten erfolgreichen Import setzen
                if (successful)
                {
                    lastFlightImport = DateTime.Now;
                    InvokeLog("Flugdatenimport erfolgreich beendet");
                    Logger.GetInstance().Log(LogLevel.Info, "Flugdatenimport erfolgreich beendet");
                }
                else
                {
                    Logger.GetInstance().Log(LogLevel.Exception, "Beim Flugdatenimport ist mindestens 1 Fehler aufgetreten.");
                    InvokeLog("Beim Flugdatenimport ist mindestens 1 Fehler aufgetreten. Das Datum des letzten erfolgreichen Flugdatenimports wird nicht aktualisiert.");
                    InvokeLog("Deshalb werden alle Flüge, die nach " + lastFlightImport.ToString("yyyy-MM-dd hh:mm:ss") + " verändert und importiert wurden wieder gelöscht");
                }

                return successful;
            }
            catch (Exception exce)
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + " " + exce.Message);
                InvokeLog(Constants.vbTab + "Fehler beim Flugdatenimport");
                return false;
            }
        }

        // gibt alle Flüge zurück, die seit letztem Flugdatenimport erstellt/verändert wurden
        public List<JObject> loadModifiedFlights()
        {
            try
            {
                System.Threading.Tasks.Task<JArray> modifiedFlightsByDate;
                var modifiedFlights = new List<JObject>();
                DateTime lastChangeDate;
                if (FlsGliderSync.logAusfuehrlich)
                {
                    Logger.GetInstance().Log(LogLevel.Info, My.MySettingsProperty.Settings.ServiceAPIModifiedFlightsMethod + lastFlightImport.ToString("yyyy-MM-dd"));
                }

                // alle Flüge, die seit letzem erfolgreichem ImportDATUM verändert/erstellt wurden, herunterladen
                modifiedFlightsByDate = client.CallAsyncAsJArray(My.MySettingsProperty.Settings.ServiceAPIModifiedFlightsMethod + lastFlightImport.ToString("yyyy-MM-dd"));
                modifiedFlightsByDate.Wait();

                // aus FLS kann nur anhand Datum geladen werden 
                // --> hier auch noch auf Zeit prüfen, ob nach letztem Flugdatenimport
                foreach (JObject flight in modifiedFlightsByDate.Result.Children())
                {
                    // die Flüge werden anhand Datum geholt --> hier auch noch auf die Zeit prüfen (nur neu importieren, wenn wirklich nach lastFlightImport
                    lastChangeDate = FlsHelper.GetFlightChangeDate(flight);

                    // ' falls nur bis zu Datum importiert werden soll
                    // If bisFlightImport > DateTime.MinValue Then

                    // ' Flüge, die später noch verändert wurden, abfangen und nicht laden
                    // If bisFlightImport > lastChangeDate Then
                    // Continue For
                    // End If
                    // End If

                    // alle Flüge, die nach letztem Import verändert wurden, laden
                    if (lastChangeDate != DateTime.MinValue)
                    {
                        if (lastChangeDate > lastFlightImport)
                        {
                            modifiedFlights.Add(flight);
                        }
                    }
                }

                return modifiedFlights;
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return null;
            }
        }

        // Tabelle enthält allgemeine Infos zum Flug, Primärschlüssel: FlightId
        public bool SetFlightData(JObject flight)
        {
            try
            {
                if (FlsGliderSync.logAusfuehrlich)
                {
                    Logger.GetInstance().Log(LogLevel.Info, flight.ToString());
                }

                // insert Gliderflight or Motorflight
                if (!pxhelper.insertMasterFlight(flight))
                {
                    return false;
                }
                // if exists, insert Towflight
                if (!(string.IsNullOrEmpty(FlsHelper.GetValOrDef(flight, "TowFlightFlightId")) | FlsHelper.GetValOrDef(flight, "TowFlightFlightId") == "00000000-0000-0000-0000-000000000000"))
                {
                    if (!pxhelper.insertTowFlight(flight))
                    {
                        return false;
                    }

                    // ' ergänzt in ZUS_DokflightLink die FlightId mit der TowFlightFlightId (insert in DokFlightLink wird mit Delivery gemacht, Delivery kennt aber TowFlightFlightId nicht
                    // If Not pxhelper.UpdateDokFlightLink(GetValOrDef(flight, "FlightId"), GetValOrDef(flight, "TowFlightFlightId")) Then
                    // Return False
                    // End If
                }

                Logger.GetInstance().Log(LogLevel.Info, "Der Flug mit der FlightId " + flight["FlightId"].ToString() + " wurde importiert");
                return true;
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Importieren der Flugdaten" + ex.Message);
                return false;
            }
        }

        private System.Threading.Tasks.Task<JArray> TestFunktionGetBeispielDeliveries()
        {
            var filetext = new List<string>();
            string path = @"C:\Workspace\Projekte_inArbeit\FLS\AddressSync\BeispielDeliveries.txt";
            filetext = ReadFile(path);
            return default;

            // Dim str As String
            // str = String.Join(Char(13), filetext)

        }

        private List<string> ReadFile(string path)
        {
            var filetext = new List<string>();
            StreamReader reader;
            try
            {
                reader = new StreamReader(path);
            }
            catch (Exception ex)
            {
                Interaction.MsgBox("Datei " + path + " wurde nicht gefunden");
                return null;
            }

            string sLine = "";
            // Dim iniLine As String = ""

            // Zeile zu key aus Ini auslesen
            do
            {
                sLine = reader.ReadLine();
                filetext.Add(sLine);
            }
            while (!(sLine is null));
            reader.Close();
            return filetext;
        }




        // *********************************************************************************DeliveryImport*************************************************************************
        /// <summary>
    /// Donwload der Deliveries + Erstellung der Dokumente
    /// </summary>
    /// <returns>Ein Boolean ob der Import erfolgreich war</returns>
        public bool DeliveryImport()
        {
            System.Threading.Tasks.Task<JArray> deliveries; // Lieferscheindaten aus FLS (zur Verrechnung freigegeben)
            int dokNr = 0;
            string fehler = string.Empty;
            bool successful = true;
            try
            {

                // DEBUG für Test: Validieren + Sperren von erfassten Flügen --> werden als zu verrechnende Flüge geliefert (im Normalbetrieb werden die Flüge durch FLS freigegeben = "gesperrt" für Bearbeitung in FLS)
                if (My.MySettingsProperty.Settings.ServiceAPITokenMethod.Contains("test.glider-fls.ch"))
                {
                    client.testDeliveriesErstellen();
                }

                InvokeLog("Lieferscheinimport gestartet");
                Logger.GetInstance().Log(LogLevel.Info, "Lieferscheinimport gestartet");

                // alle Flüge, die zu verrechnen sind aus FLS herunterladen
                if (FlsGliderSync.logAusfuehrlich)
                {
                    Logger.GetInstance().Log(LogLevel.Info, My.MySettingsProperty.Settings.ServiceAPIDeliveriesNotProcessedMethod);
                }

                deliveries = client.CallAsyncAsJArray(My.MySettingsProperty.Settings.ServiceAPIDeliveriesNotProcessedMethod);
                deliveries.Wait();

                // Debug
                // deliveries = TestFunktionGetBeispielDeliveries()

                DeliveryCount = deliveries.Result.Children().Count();
                ProgressDelivery = 0;
                InvokeDoProgressDelivery();
                if (FlsGliderSync.logAusfuehrlich)
                {
                    Logger.GetInstance().Log(LogLevel.Info, "Anzahl geladener Lieferscheine " + deliveries.Result.Children().Count());
                    Logger.GetInstance().Log(LogLevel.Info, deliveries.Result.ToString());
                }

                if (deliveries.Result.Count == 0)
                {
                    InvokeLog("Es wurden keine Lieferscheine geladen");
                }

                // Iteration durch die Flüge, die zu verrechnen sind
                // TODO: OrderBy PersonId and Startdate of flight
                foreach (JObject delivery in deliveries.Result.Children())
                {
                    dokNr = 0;
                    // von abgebrochenen Lieferscheinimporten vorhandene Daten für die DeliveryId löschen
                    if (!pxhelper.deleteIncompleteDeliveryData(delivery["DeliveryId"].ToString()))
                    {
                        InvokeLog("Fehler beim Löschen der Daten für DeliveryId " + delivery["DeliveryId"].ToString());
                        throw new Exception("Fehler beim Löschen der Daten für DeliveryId " + delivery["DeliveryId"].ToString());
                        return false;
                    }

                    // prüfen, ob der Delivery die nötigen Daten enthält. Wenn nicht, kann dieser Delivery nicht importiert werden
                    if (!checkForCompleteDelivery(delivery))
                    {
                        InvokeLog(Constants.vbTab + "Fehler: Im zu importierender Lieferschein fehlen Daten. Er kann nicht importiert werden. DeliveryId: " + delivery["DeliveryId"].ToString());

                        // Daten des fehlerhaften Lieferscheins in Zusatztabelle schreiben
                        if (!FehlerhafterDeliveryVerarbeiten(delivery))
                        {
                            InvokeLog(Constants.vbTab + "Fehler: fehlerhafter Delivery mit DeliveryId " + delivery["DeliveryId"].ToString() + " konnte nicht verarbeitet werden");
                        }

                        successful = false;
                        continue;

                        // ' wenn nötige Daten fehlen --> nachfragen, ob überspringen + mit nächstem weiterfahren (OK) oder Deliveryimport abbrechen (Cancel)
                        // Dim dialogres As DialogResult = MessageBox.Show("Der Lieferschein mit der DeliveryId " + GetValOrDef(delivery, "DeliveryId") + " kann nicht importiert werden, da benötigte Daten fehlen." + vbCrLf + vbCrLf +
                        // "Wenn Sie diesen Lieferschein überspringen und den nächsten importieren wollen, klicken Sie ""OK""" + vbCrLf + vbCrLf +
                        // "Mit Abbrechen beenden Sie den Lieferscheinimport", "Keine PersonId vorhanden", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2)
                        // ' OK --> diesen Delivery überspringen --> mit nächstem weiterfahren
                        // If dialogres = DialogResult.OK Then
                        // successful = False

                        // Continue For
                        // ' Cancel --> Deliveryimport abbrechen
                        // ElseIf dialogres = DialogResult.Cancel Then
                        // Return False
                        // End If

                    }

                    // 1 Lieferschein importieren
                    if (!importDelivery(delivery, ref dokNr))
                    {

                        // Lieferscheinimport fehlgeschlagen
                        if (!FehlerhafterDeliveryVerarbeiten(delivery))
                        {
                            InvokeLog(Constants.vbTab + "Fehler: Delivery mit DeliveryId " + delivery["DeliveryId"].ToString() + " konnte nicht verarbeitet werden");
                        }

                        InvokeLog(Constants.vbTab + "Fehler beim Importieren der Lieferscheine oder Erstellen der Dokumente für DeliveryId: " + delivery["DeliveryId"].ToString());
                        if (!pxhelper.deleteIncompleteDeliveryData(delivery["DeliveryId"].ToString()))
                        {
                            InvokeLog("Fehler beim Löschen der Daten für DeliveryId " + delivery["DeliveryId"].ToString());
                        }

                        continue;
                    }

                    // wenn dokNr noch 0 ist --> Fehler = es wurde kein Dok erstellt
                    if (dokNr == 0)
                    {
                        InvokeLog(Constants.vbTab + "Fehler beim Auslesen der neuen DokumentNr des in Proffix neu erstellten Dokuments AdressNr: " + (string.IsNullOrEmpty(FlsHelper.GetValOrDef(delivery, "RecipientDetails.PersonId")) ? FlsHelper.GetValOrDef(delivery, "RecipientDetails.PersonClubMemberNumber") : pxhelper.GetAdressNr(FlsHelper.GetValOrDef(delivery, "RecipientDetails.PersonId"))) + " Flugdatum: " + FlsHelper.GetValOrDef(delivery, "FlightInformation.FlightDate"));
                        Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Auslesen der neuen DokumentNr des in Proffix neu erstellten Dokuments AdressNr: " + (string.IsNullOrEmpty(FlsHelper.GetValOrDef(delivery, "RecipientDetails.PersonId")) ? FlsHelper.GetValOrDef(delivery, "RecipientDetails.PersonClubMemberNumber") : pxhelper.GetAdressNr(FlsHelper.GetValOrDef(delivery, "RecipientDetails.PersonId"))) + " Flugdatum: " + FlsHelper.GetValOrDef(delivery, "FlightInformation.FlightDate"));
                        return false;
                    }

                    // wenn dieser Delivery eine FlightId enthält --> Datensatz in ZUS_DokFlightLink schreiben
                    if (!string.IsNullOrEmpty(FlsHelper.GetValOrDef(delivery, "FlightInformation.FlightId")))
                    {

                        // DokDeliveryLink füllen
                        if (!pxhelper.SetDokFlightLink(delivery, dokNr))
                        {
                            InvokeLog(Constants.vbTab + "Fehler beim Befüllen der Zusatztabelle ZUS_DokFlightLink");
                            Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Befüllen der Zusatztabelle ZUS_DokFlightLink");
                            return false;
                        }
                    }

                    // in FLS schreiben, dass diese Delivery verrechnet (LS erstellt) wurde
                    if (!flagAsDelivered(delivery, dokNr))
                    {
                        InvokeLog(Constants.vbTab + "Fehler beim Rückmelden an FLS, dass das FLS-Dokument erstellt worden ist");
                        pxhelper.deleteIncompleteDeliveryData(delivery["DeliveryId"].ToString());
                        InvokeLog("Fehler beim Löschen der Daten für DeliveryId " + delivery["DeliveryId"].ToString());
                        throw new Exception("Fehler in " + MethodBase.GetCurrentMethod().Name);
                        return false;
                    }

                    ProgressDelivery += 1;
                    InvokeDoProgressDelivery();
                }

                ProgressDelivery = DeliveryCount;
                InvokeDoProgressDelivery();
                if (successful)
                {
                    InvokeLog("Lieferscheinimport erfolgreich beendet");
                    Logger.GetInstance().Log(LogLevel.Info, "Lieferscheinimport erfolgreich beendet");
                    lastDeliveryImport = DateAndTime.Now;
                }
                else
                {
                    InvokeLog("Fehler beim Lieferscheinimport");
                    Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Lieferscheinimport");
                }

                InvokeLog("");
                return successful;
            }
            catch (Exception exce)
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + " " + exce.Message);
                InvokeLog(Constants.vbTab + "Fehler beim Importieren der Lieferscheine");
                return false;
            }
        }

        // IDs der Lieferscheine, die in PX nicht verarbeitet werden können, in Fehlertabelle schreiben
        private bool FehlerhafterDeliveryVerarbeiten(JObject delivery)
        {
            bool fehlerBekannt = false;

            // abfangen ob RecipientDetails leer --> unklar, wer bezahlen muss 
            if (delivery["RecipientDetails"].ToString() == "{}")
            {
                fehlerBekannt = true;
                if (!pxhelper.InFehlertabelleSchreiben(delivery, "Lieferschein aus FLS enthält keine Daten zum Rechnungsempfänger"))
                {
                    return false;
                }
            }

            // prüfen, ob eine PersonId für Rechnungsempfänger vorhanden ist
            if (string.IsNullOrEmpty(FlsHelper.GetValOrDef(delivery, "RecipientDetails.PersonId")))
            {
                if (string.IsNullOrEmpty(FlsHelper.GetValOrDef(delivery, "RecipientDetails.PersonClubMemberNumber")))
                {
                    fehlerBekannt = true;
                    if (!pxhelper.InFehlertabelleSchreiben(delivery, "Lieferschein aus FLS enthält weder eine PersonId noch eine MemberNumberals Rechnungsempfänger"))
                    {
                        return false;
                    }
                }
            }

            // prüfen, ob delivery überhaupt Artikel enthält (wenn nicht, ist nicht definiert, welche Artikel mit diesem Flug verknüpft sind
            if (delivery["DeliveryItems"].Count() == 0)
            {
                fehlerBekannt = true;
                if (!pxhelper.InFehlertabelleSchreiben(delivery, "Artikel fehlen"))
                {
                    return false;
                }
            }

            // wenn es keiner der bekannten Fehler war
            if (!fehlerBekannt)
            {
                if (!pxhelper.InFehlertabelleSchreiben(delivery, "unbekannter Fehler"))
                {
                    return false;
                }
            }

            // nachfragen, ob der fehlerhafte Lieferschein als erledigt verbucht werden soll.
            var dialogres = MessageBox.Show("Der Lieferschein mit der DeliveryId " + FlsHelper.GetValOrDef(delivery, "DeliveryId") + " kann nicht verarbeitet werden. " + "Die DeliveryId und FlightId wurden in die Fehlertabelle \"err_deliveries\" geschrieben. " + Constants.vbCrLf + Constants.vbCrLf + "Soll der Lieferschein an FLS als erledigt markiert werden? " + "ACHTUNG: Wenn sie JA klicken, kann der Lieferschein nicht mehr über dieses Programm importiert werden, sondern muss in Proffix manuell erstellt werden!!" + Constants.vbCrLf + Constants.vbCrLf + "Wenn Sie NEIN klicken, wird der fehlerhafte Lieferschein beim nächsten Import wieder als zu Importieren erscheinen", "Lieferscheinimport fehlgeschlagen", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
            if (dialogres == DialogResult.Yes)
            {
                // fehlerhaften Lieferschein als Verarbeitet an FLS melden, damit er nicht immer wieder kommt
                if (!flagAsDelivered(delivery, 0))
                {
                    InvokeLog(Constants.vbTab + "Fehler beim Rückmelden an FLS, dass das FLS-Dokument erstellt worden ist");
                    return false;
                }
            }

            return true;
        }

        // verarbeitet die Daten für eine DeliveryId
        private bool importDelivery(JObject delivery, ref int dokNr)
        {
            string deliveryId = delivery["DeliveryId"].ToString();
            var docToEdit = new pxBook.pxKommunikation.pxDokument();  // Dokument (aus Proffix, aus erstellten, oder neu erstelltes) welches bearbeitet werden soll
            bool docExistInProffix = false;                        // Flag, ob das Dok in PX existiert (entweder bereits seit vor Programmausführung, oder durch ein bereits neu erstelltes Dok, welches vorher bereits in PX geladen wurde
            var newDocPositions = new List<pxBook.pxKommunikation.pxDokumentPos>();         // enthält alle DocPos, die für das Dok neu erstellt werden
            var newDocPosition = new pxBook.pxKommunikation.pxDokumentPos();  // einzelne DocPos, die erstellt wird
            var newTextPosition = new pxBook.pxKommunikation.pxDokumentPos();
            string fehler = string.Empty;
            string adressNr = string.Empty;
            string recipientPersonId = string.Empty;
            try
            {
                if (FlsGliderSync.logAusfuehrlich)
                {
                    Logger.GetInstance().Log(delivery.ToString());
                }

                if (string.IsNullOrEmpty(FlsHelper.GetValOrDef(delivery, "RecipientDetails.PersonId")))
                {
                    if (string.IsNullOrEmpty(FlsHelper.GetValOrDef(delivery, "RecipientDetails.PersonClubMemberNumber")))
                    {
                        Logger.GetInstance().Log(LogLevel.Exception, "Weder die PeronId noch die MemberNumber für den Rechnungsempfänger konnte geladen werden" + delivery.ToString());
                        InvokeLog(Constants.vbTab + "Fehler: Weder die PersonId noch die MemberNumber für den Rechnungsempfänger konnte geladen werden");
                        return false;
                    }
                }

                // eine leere DokPos in Liste laden, damit 1. leer --> damit alle eigentlichen Positionen bei AddDokument in Proffix geladen werden (pxBook verlangt, dass 1. leer ist)
                newDocPositions.Add(new pxBook.pxKommunikation.pxDokumentPos());

                // 'AdressNr des Rechnungsempfängers auslesen
                // If delivery("RecipientDetails")("PersonId") Is Nothing Then
                // Throw New Exception("Kein Rechnungsempfänger definiert für DeliveryId: " + delivery("DeliveryId").ToString)
                // End If

                // existiert bereits ein Dok für diesen Tag für diesen Kunden?
                // gibt zu bearbeitendes Dok zurück (aus Proffix, oder neu erstellt) + setzt Flag docExistInProffix entsprechend
                docToEdit = selectDocToEdit(delivery, ref docExistInProffix);
                if (docToEdit.AdressNr == 0)
                {
                    InvokeLog(Constants.vbTab + "Fehler beim Erstellen eines neuen Dokumentes AdressNr: " + adressNr + " Flugdatum: " + (delivery["FlightDate"] is object ? DateTime.Parse(delivery["FlightDate"].ToString()).ToShortDateString() : ""));
                    return false;
                }

                // heutiges Datum beim Dok als Referenztext anhängen
                // docToEdit.Referenztext += delivery("FlightDate").ToString

                // als erste DocPos eine Textpos hinzufügen mit DeliveryInfos (AircraftImmatriculation, FlightType) + AdditionalInfo
                newTextPosition = createTextPos(delivery);
                newDocPositions.Add(newTextPosition);

                // jeden Artikel des Deliveries durchgehen
                // TODO: OrderBy DeliveryItems.Position to sort the items as in the rules
                foreach (JObject lineItem in delivery["DeliveryItems"].Children())        // .OrderBy(delivery("DeliveryItems")("Position"))
                {

                    // aus LineItem ein DocPos erstellen und zu Liste der bereits neu erstellten hinzufügen
                    newDocPosition = createDocPos(docToEdit.DokumentNr, lineItem, deliveryId);
                    if ((newDocPosition.ArtikelNr ?? "") == (string.Empty ?? ""))
                    {
                        InvokeLog(Constants.vbTab + "Fehler beim Erstellen der Dokumentposition");
                        Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Erstellen der Dokumentposition aus " + lineItem.ToString());
                        return false;
                    }

                    newDocPositions.Add(newDocPosition);
                }

                // Daten in Proffix laden und Dokument bearbeiten/erstellen
                if (!loadDataInProffix(docExistInProffix, ref docToEdit, newDocPositions, deliveryId))
                {
                    InvokeLog(Constants.vbTab + "Fehler beim Verarbeiten des Dokuments AdressNr: " + docToEdit.AdressNr.ToString() + " Flugdatum: " + docToEdit.Datum);
                    return false;
                }

                // die ByRef Variable dokNr setzen (nötig für flagAsDelivered())
                dokNr = docToEdit.DokumentNr;
                return true;
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Log("Fehler in " + MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return false;
            }
        }

        // **************************************************************************Hilfsfunktionen*******************************************************************************

        /// <summary>
    /// gibtzu bearbeitendes Doc zurück (existierendes aus Proffix, oder neu erstelltes) + passt Flag docExistInProffix an
    /// </summary>
    /// <param name="delivery"></param>
    /// <param name="docExistInProffix"></param>
    /// <returns></returns>
    /// <remarks></remarks>
        private pxBook.pxKommunikation.pxDokument selectDocToEdit(JObject delivery, ref bool docExistInProffix)
        {
            var docs = Array.Empty<pxBook.pxKommunikation.pxDokument>();
            var rs = new ADODB.Recordset();
            string sql = string.Empty;
            string fehler = string.Empty;
            string adressNr = string.Empty;
            DateTime flightDate;
            try
            {
                // AdressNr auslesen
                if (!string.IsNullOrEmpty(FlsHelper.GetValOrDef(delivery, "RecipientDetails.PersonId")))
                {
                    // PersonId auslesen und die dazugehörige AdressNr aus PX laden
                    adressNr = pxhelper.GetAdressNr(delivery["RecipientDetails"]["PersonId"].ToString());
                }
                else
                {
                    // AdressNr direkt auslesen, da PersonId fehlt
                    adressNr = FlsHelper.GetValOrDef(delivery, "RecipientDetails.PersonClubMemberNumber");
                }

                // wenn kein FlightDatum enthalten, da z.B. Jahrespauschale --> heute als Datum
                if (string.IsNullOrEmpty(FlsHelper.GetValOrDef(delivery, "FlightInformation.FlightDate")))
                {
                    flightDate = DateAndTime.Now;
                }
                // sonst das FlightDate aus dem JSON auslesen
                else
                {
                    flightDate = DateTime.Parse(delivery["FlightInformation"]["FlightDate"].ToString());
                }

                // ist bereits ein FLS-Dok vorhanden für den Tag/Kunden, welches noch nicht weiterverarbeitet (RG) wurde
                sql = "select auf_dokumente.dokumentnrauf from auf_dokumente left join auf_doklink on auf_dokumente.dokumentnrauf = auf_doklink.dokumentnrauf where " + "auf_dokumente.doktypauf = 'FLSLS' and " + "auf_dokumente.adressNradr = " + adressNr + " and " + "auf_dokumente.datum = '" + flightDate.ToString(pxhelper.dateformat) + "' and " + "auf_doklink.dokumentnrauf is null";
                if (!MyConn.getRecord(ref rs, sql, ref fehler))
                {
                    throw new Exception("Fehler beim Abfragen, ob bereits Dok vorhanden für AdressNr " + (string.IsNullOrEmpty(FlsHelper.GetValOrDef(delivery, "RecipientDetails.PersonId")) ? FlsHelper.GetValOrDef(delivery, "RecipientDetails.PersonClubMemberNumber") : pxhelper.GetAdressNr(delivery["RecipientDetails"]["PersonId"].ToString())) + " und Datum " + DateTime.Parse(delivery["FlightDate"].ToString()).ToString(pxhelper.dateformat) + " " + fehler);
                }

                // für alle gefundenen Dokumente
                while (!rs.EOF)
                {

                    // das Dokument mit der ermittelten DokNr holen
                    if (!FlsGliderSync.Proffix.GoBook.GetDokument(ref docs, ref fehler, Conversions.ToInteger(rs.Fields["dokumentnrauf"].ToString())))
                    {
                        throw new Exception("Fehler beim Laden des Doks mit der DokNr " + rs.Fields["dokumentnrauf"].ToString() + fehler);
                    }
                    else
                    {
                        // Flag, dass Dok existiert = true setzen + return vorhandenes Dokument
                        docExistInProffix = true;
                        return docs[1];
                    }

                    rs.MoveNext();
                }

                // wenn hierher gekommen = es wurde kein Dok für den Tag/Kunden gefunden, welches nicht bereits weiterverarbeitet wurde
                // --> Flag auf false setuen + return ein neuerstelltes, leeres Dokument
                docExistInProffix = false;
                return createDoc(delivery);
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return new pxBook.pxKommunikation.pxDokument();
                // Throw New Exception(ex.Message)
            }
        }

        /// <summary>
    /// Ein Dokument erstellen
    /// </summary>
    /// <returns>Das erstellte PROFFIX Dokument</returns>
        private pxBook.pxKommunikation.pxDokument createDoc(JObject delivery)
        {
            var doc = new pxBook.pxKommunikation.pxDokument();
            var adresse = new pxBook.pxKommunikation.pxAdressen();
            try
            {
                // Werte für Dokument setzen:
                if (!string.IsNullOrEmpty(FlsHelper.GetValOrDef(delivery, "RecipientDetails.PersonId")))
                {
                    doc.AdressNr = Conversions.ToInteger(pxhelper.GetAdressNr(delivery["RecipientDetails"]["PersonId"].ToString()));
                }
                else
                {
                    doc.AdressNr = Conversions.ToInteger(FlsHelper.GetValOrDef(delivery, "RecipientDetails.PersonClubMemberNumber"));
                }

                doc.DokumentTyp = "FLSLS";
                if (string.IsNullOrEmpty(FlsHelper.GetValOrDef(delivery, "FlightInformation.FlightDate")))
                {
                    doc.Datum = DateAndTime.Now.ToString(pxhelper.dateformat);
                    doc.RDatum = DateAndTime.Now.ToString(pxhelper.dateformat);
                    doc.Referenztext = "Verrechnung: " + DateAndTime.Now.ToString(pxhelper.dateformat);
                }
                else
                {
                    doc.Datum = FlsHelper.GetValOrDef(delivery, "FlightInformation.FlightDate");
                    doc.RDatum = FlsHelper.GetValOrDef(delivery, "FlightInformation.FlightDate");
                    doc.Referenztext = "Flugdatum: " + FlsHelper.GetValOrDef(delivery, "FlightInformation.FlightDate").Substring(0, 10);
                }

                return doc;
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + " " + ex.ToString());
                return default;
            }
        }

        // erstellt eine DocPos
        private pxBook.pxKommunikation.pxDokumentPos createDocPos(int dokNr, JObject lineItem, string deliveryId)
        {
            string artikelNr = lineItem["ArticleNumber"].ToString();
            decimal menge = Conversions.ToDecimal(lineItem["Quantity"].ToString());
            decimal rabatt = (decimal)(string.IsNullOrEmpty(FlsHelper.GetValOrDef(lineItem, "DiscountInPercent")) ? 0 : lineItem["DiscountInPercent"]);
            string zusatzfelder = "Z_DeliveryId = '" + deliveryId + "', Z_DeliveryItemId = '" + FlsHelper.GetValOrDef(lineItem, "DeliveryItemId") + "'";
            var docpos = new pxBook.pxKommunikation.pxDokumentPos();

            // Dim einheitpro As String = String.Empty
            // Dim rgeinheit As String = String.Empty
            // If Not GetEinheiten(artikelNr, einheitpro, rgeinheit) Then
            // Return Nothing
            // End If

            // dim preis as decimal = GetVerkauf1(lineItem("ArticleNumber").ToString)

            docpos.DokumentNr = dokNr;
            docpos.ArtikelNr = artikelNr;
            docpos.Menge = menge;    // wenn in JSON Menge = 0 --> 1, anosnten die im JSON angegebene Menge
            docpos.Rabatt = rabatt;
            docpos.Zusatzfelder = zusatzfelder;
            // bisher funktionierte es nur, wenn auch folgende Werte geladen wurden
            // .MengeVerr = menge 'If(menge > 0, menge, 1)
            // .Lagereinheit = einheitpro
            // .Rechnungseinheit = rgeinheit
            // .PreisSw = preis
            // .PreisFw = preis

            // ' Variante, wie am wenigsten Werte für Pos geladen werden müssen, die Preise aber korrekt übernommen werden
            // funktioniert nicht
            // With docpos
            // .Positionsart = pxBook.pxKommunikation.pxPositionsart.Artikel
            // .DokumentNr = dokNr
            // .ArtikelNr = artikelNr
            // .Menge = menge
            // End With

            // hat früher funktioniert
            // With docpos
            // .DokumentNr = dokNr
            // .ArtikelNr = artikelNr
            // .Menge = menge
            // .Zusatzfelder = zusatzfelder
            // .Rabatt = rabatt
            // End With

            // ' Debug Tests für Preis = 0 (JSON gibt mir in dem Fall Menge = 0)
            // ' für alle Pos ausser 1 werden Testeshalber Menge = 0 gesetzt
            // If CInt(GetValOrDef(lineItem, "Position")) > 1 Then
            // menge = 0
            // With docpos
            // .Menge = 0
            // .MengeVerr = 0
            // .PreisSw = 0
            // .PreisFw = 0
            // .TotalSw = 0
            // .TotalFw = 0
            // End With
            // '    If CInt(GetValOrDef(lineItem, "Position")) > 2 Then
            // '        With docpos
            // '            .Menge = 1
            // '            .MengeVerr = 1
            // '            .PreisSw = CDec(0.0)
            // '            .PreisFw = CDec(0.0)
            // '            .TotalSw = 0
            // '            .TotalFw = 0
            // '        End With

            // '    End If
            // End If

            return docpos;
        }

        // Einheiten des Artikels laden
        private bool GetEinheiten(string artikelnr, ref string lageinheit, ref string rgeinheit)
        {
            string sql = string.Empty;
            var rs = new ADODB.Recordset();
            string fehler = string.Empty;
            sql = "Select einheitpro, einheitrechnung from lag_artikel where artikelnrlag = '" + artikelnr + "'";
            if (!MyConn.getRecord(ref rs, sql, ref fehler))
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Laden der Einheiten");
                return false;
            }

            // prüfen, ob ein DS gefunden wurde
            if (rs.EOF)
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Kein Datensatz für Artikel " + artikelnr + " gefunden");
                return false;
            }

            while (!rs.EOF)
            {
                try
                {
                    lageinheit = rs.Fields["einheitPRO"].ToString();
                    rgeinheit = rs.Fields["einheitrechnung"].ToString();
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Auslesen der Einheiten. ArtikelNr: " + artikelnr);
                    return false;
                }

                rs.MoveNext();
            }

            return default;
        }

        private decimal GetVerkauf1(string artikelNr)
        {
            string sql = "select verkauf1 from lag_artikel where artikelnrlag = '" + artikelNr + "'";
            var rs = new ADODB.Recordset();
            string fehler = string.Empty;
            var verkauf1 = default(decimal);
            if (!MyConn.getRecord(ref rs, sql, ref fehler))
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Laden des Verkaufspreis 1 von Artikel " + artikelNr);
                return 0m;
            }

            while (!rs.EOF)
            {
                verkauf1 = Conversions.ToDecimal(rs.Fields["verkauf1"]);
                rs.MoveNext();
            }

            return verkauf1;
        }

        // erstellt eine TextPos, die einem Dok als TextPosition hinzugefügt werden kann
        private pxBook.pxKommunikation.pxDokumentPos createTextPos(JObject delivery)
        {
            return new pxBook.pxKommunikation.pxDokumentPos()
            {
                Bezeichnung1 = FlsHelper.GetValOrDef(delivery, "DeliveryInformation") + " " + (FlsHelper.GetValOrDef(delivery, "AdditionalInformation") != "0" ? FlsHelper.GetValOrDef(delivery, "AdditionalInformation") : FlsHelper.GetValOrDef(delivery, "FlightInformation.AircraftImmatriculation")),
                NotizenExtern = FlsHelper.GetValOrDef(delivery, "DeliveryInformation") + " " + (FlsHelper.GetValOrDef(delivery, "AdditionalInformation") != "0" ? FlsHelper.GetValOrDef(delivery, "AdditionalInformation") : FlsHelper.GetValOrDef(delivery, "FlightInformation.AircraftImmatriculation")),
                Positionsart = pxBook.pxKommunikation.pxPositionsart.Text,
                Zusatzfelder = "Z_DeliveryId = '" + delivery["DeliveryId"].ToString() + "'"
            };
        }

        // **********************************************************************************************************************************************************************
        // lädt Daten in Proffix (entweder nur zusätzlich DocPos in ein existierendes Doc, oder ein neues Doc mit seinen DocPos, in dem Fall wird in docToEdit die neu erstellte DokumentNr eingetragen)
        private bool loadDataInProffix(bool docExistInProffix, ref pxBook.pxKommunikation.pxDokument docToEdit, List<pxBook.pxKommunikation.pxDokumentPos> newDocPositions, string deliveryId)
        {
            var rs = new ADODB.Recordset();
            string sql = string.Empty;
            string fehler = string.Empty;
            var newDocPositionsArray = newDocPositions.ToArray();

            // wenn es sich um ein Dok handelt, welches bereits in PX existiert (bereits vor Programmausführung, oder ein bereits neu erstelltes, welches bereits in PX hochgeladen wurde)
            if (docExistInProffix)
            {

                // jede neue DocPos in PX laden
                foreach (var docpos in newDocPositions)
                {
                    if (!(docpos.ArtikelNr is null & docpos.Bezeichnung1 is null))
                    {
                        var docPosRef = docpos;
                        if (!FlsGliderSync.Proffix.GoBook.AddDokumentPos(docToEdit.DokumentNr, ref docPosRef, ref fehler))
                        {
                            InvokeLog(Constants.vbTab + "Beim Bearbeiten des existierenden Dokumentes " + docPosRef.DokumentNr.ToString() + " ist ein Fehler aufgetreten. Artikel: " + docPosRef.ArtikelNr);
                            InvokeLog(Constants.vbTab + "Fehlermeldung: " + fehler);
                            Logger.GetInstance().Log(LogLevel.Exception, "Fehler in pxbook.AddDokumentPos() AdressNr: " + docToEdit.AdressNr.ToString() + " Artikel: " + docPosRef.ArtikelNr + "für Dokument: " + docToEdit.DokumentNr.ToString() + " pxBook Fehlermeldung: " + fehler);
                            return false;
                        }
                        else
                        {
                            // AddDokumentPos() gab return true
                            InvokeLog("Der Artikel " + docPosRef.ArtikelNr + " " + (docPosRef.Bezeichnung1 is object ? docPosRef.Bezeichnung1 : "") + " wurde als Dokumentposition zum Dokument " + docToEdit.DokumentNr.ToString() + " hinzugefügt.");
                            Logger.GetInstance().Log(LogLevel.Info, "Der Artikel " + docPosRef.ArtikelNr + " " + (docPosRef.Bezeichnung1 is object ? docPosRef.Bezeichnung1 : "") + " wurde als Dokumentposition zum Dokument " + docToEdit.DokumentNr.ToString() + " hinzugefügt.");
                            if (!string.IsNullOrEmpty(fehler))
                            {
                                // diese Fehlermeldung kann ignoriert werden, wenn AddDokument() true zurückgegeben hat
                                Logger.GetInstance().Log(LogLevel.Exception, "pxBook Fehlermeldung wurde ignoriert, da true zurückgegeben wurde. Fehlermeldung: " + fehler);
                            }
                        }
                    }
                }
            }

            // wenn Dokument in Proffix noch nicht existiert
            // das neu erstellte Dokument mit den erstellten Positionen in Proffix laden
            else if (!FlsGliderSync.Proffix.GoBook.AddDokument(ref docToEdit, ref newDocPositionsArray, Array.Empty<pxBook.pxKommunikation.pxZahlungen>(), ref fehler))
            {
                if (fehler.Contains("Kein Dokumenttyp gefunden!"))
                {
                    InvokeLog(Constants.vbTab + "Es existiert kein Dokumenttyp \"FLSLS\" in Proffix. Dieser muss erstellt werden, bevor neue Dokumente erstellt werden können");
                }
                else
                {
                    InvokeLog(Constants.vbTab + "Beim Bearbeiten des neu erstellten Dokumentes ist ein Fehler aufgetreten. AdressNr: " + docToEdit.AdressNr.ToString() + " DokumentNr: " + docToEdit.DokumentNr.ToString() + " pxBook Fehlermeldung: " + fehler);
                    InvokeLog(Constants.vbTab + "Fehlermeldung: " + fehler);
                }

                Logger.GetInstance().Log(LogLevel.Exception, "Fehler in pxbook.AddDokument() AdressNr: " + docToEdit.AdressNr.ToString() + " pxBook Fehlermeldung: " + fehler);
                return false;
            }
            else
            {
                Logger.GetInstance().Log(LogLevel.Info, "Für die AdressNr: " + docToEdit.AdressNr.ToString() + " wurde für das Flugdatum: " + docToEdit.Datum.ToString().Substring(0, 10) + " ein Dokument " + docToEdit.DokumentNr.ToString() + " erstellt.");
                InvokeLog("Für die AdressNr: " + docToEdit.AdressNr.ToString() + " wurde für das Flugdatum: " + docToEdit.Datum.ToString().Substring(0, 10) + " ein Dokument " + docToEdit.DokumentNr.ToString() + " erstellt.");

                // Doktotal kontrollieren (wenn 0, ob dies so ok ist)
                if (CheckDokTotal(docToEdit.DokumentNr) < 0)
                {
                    InvokeLog(Constants.vbTab + "Fehler beim Prüfen, ob für das erstellte Dokument die Preise gesetzt werden konnten.");
                    return false;
                }
            }

            return true;
        }

        // check if the total is > 0 and if = 0 if it is ok (as all articles have verkauf1 = 0)
        // return: -1 = error, 0 = ok, 1 = false
        private int CheckDokTotal(int dokNr)
        {
            string sql;
            var rs_total = new ADODB.Recordset();
            var rs_verkauf = new ADODB.Recordset();
            string fehler = string.Empty;
            decimal totalsw = 0m;
            decimal verkauf = 0m;
            try
            {

                // prüfen ob das Total des Docs > 0 ist
                sql = "select totalsw from auf_dokumente where dokumentnrauf = " + dokNr.ToString();
                if (!MyConn.getRecord(ref rs_total, sql, ref fehler))
                {
                    Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Prüfen des Totals für Dokument " + dokNr.ToString());
                    return -1;
                }

                while (!rs_total.EOF)
                {
                    totalsw = Conversions.ToDecimal(rs_total.Fields["TotalSW"]);
                    rs_total.MoveNext();
                }

                // das Dok hat ein Total > 0 = ok
                if (totalsw > 0m)
                {
                    return 0;
                }

                // Das Dok hat ein Total = 0 --> prüfen, ob das so ok ist (wenn alle Artikel Verkauf1 = 0 haben)
                else
                {
                    // check if there is at least 1 article in the doc, which has a verkauf1 > 0
                    sql = "select sum(verkauf1) as verkauf from lag_artikel where artikelnrlag in (select distinct artikelnrlag from auf_dokumentpos where dokumentnrauf = " + dokNr.ToString() + ")";
                    if (!MyConn.getRecord(ref rs_verkauf, sql, ref fehler))
                    {
                        Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Bestimmen des Verkaufspreises der Artikel im Dok " + dokNr.ToString());
                        return -1;
                    }

                    while (!rs_verkauf.EOF)
                    {
                        verkauf = Conversions.ToInteger(rs_verkauf.Fields["verkauf"]);
                        rs_verkauf.MoveNext();
                    }

                    // wenn keiner der Artikel einen Preis hat --> Total = 0 ist ok
                    if (!(verkauf > 0m))
                    {
                        return 0;
                    }

                    // wenn mind. 1 der Artikel einen Verkauf1-Preis hat, ist Total = 0 falsch
                    else
                    {
                        Interaction.MsgBox("Achtung: Das Dokument " + dokNr.ToString() + " wurde zwar erstellt, die Preise der Artikel konnten jedoch nicht richtig gesetzt werden!", Constants.vbCritical);
                        InvokeLog(Constants.vbTab + "Achtung: Das Dokument " + dokNr.ToString() + " wurde zwar erstellt, die Preise der Artikel konnten jedoch nicht richtig gesetzt werden!");
                        Logger.GetInstance().Log(LogLevel.Exception, "Dokument " + dokNr.ToString() + " wurde zwar erstellt, die Preise konnten aber nicht richtig gesetzt werden");
                        return 1;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return -1;
            }
        }


        // erstellt ein JSON und setzt in FLS den Delivery mit der DeliveryId und betroffener DocNr als Delivered (= in Proffix erfasst, aber noch nicht bezahlt)
        private bool flagAsDelivered(JObject delivery, int dokNr)
        {
            string sql = string.Empty;
            var rs = new ADODB.Recordset();
            string fehler = string.Empty;
            var json = new JObject();
            string response = string.Empty;
            string deliveryDateTime = string.Empty;
            try
            {

                // wenn der Lieferschein korrekt eingebucht wurde --> Erstellungsdatum des Lieferscheins laden
                if (dokNr != 0)
                {

                    // das Erstellungsdatum für das Dokument in Proffix holen
                    sql = "select erstelltAm from auf_dokumente where dokumentnrauf = " + dokNr.ToString();
                    if (!MyConn.getRecord(ref rs, sql, ref fehler))
                    {
                        Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Laden des Erstellungsdatum für die DokNr " + dokNr.ToString() + " in " + MethodBase.GetCurrentMethod().Name);
                        return false;
                    }

                    deliveryDateTime = Conversions.ToDate(rs.Fields["erstelltAm"]).ToUniversalTime().ToString("o");
                }

                // der Lieferschein ist fehlerhaft --> mit DokNr = 0 als erledigt an FLS melden
                else
                {
                    deliveryDateTime = DateAndTime.Now.ToUniversalTime().ToString("o");
                }

                // JSON erstellen
                json["DeliveryId"] = delivery["DeliveryId"].ToString();
                json["DeliveryDateTime"] = deliveryDateTime;
                json["DeliveryNumber"] = dokNr.ToString();

                // JSON an FLS schicken
                response = client.submitFlag(My.MySettingsProperty.Settings.ServiceAPIDeliveredMethod, json);
                if (response != "OK")
                {
                    Logger.GetInstance().Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + response);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return false;
            }
        }



        // prüft, ob JSON Rechnungsinfos, PersonId bzw. Artikelinfos enthält. Wenn nicht --> dieser Delivery ist nicht verrechenbar
        internal bool checkForCompleteDelivery(JObject delivery)
        {

            // abfangen ob RecipientDetails leer --> unklar, wer bezahlen muss 
            if (delivery["RecipientDetails"].ToString() == "{}")
            {
                InvokeLog(Constants.vbTab + "Fehler: Der Lieferschein aus FLS enthält keine Daten zum Rechnungsempfänger. DeliveryId: " + delivery["DeliveryId"].ToString());
                Logger.GetInstance().Log(LogLevel.Exception, "Im JSON ist RecipientDetails leer " + delivery.ToString());
                return false;
            }

            // prüfen, ob eine PersonId oder MemberNumber für Rechnungsempfänger vorhanden ist
            if (string.IsNullOrEmpty(FlsHelper.GetValOrDef(delivery, "RecipientDetails.PersonId")))
            {
                if (string.IsNullOrEmpty(FlsHelper.GetValOrDef(delivery, "RecipientDetails.PersonClubMemberNumber")))
                {
                    InvokeLog(Constants.vbTab + "Fehler: Der Lieferschein aus FLS enthält weder eine PersonId noch eine MenberNumber für den Rechnungsempfänger. DeliveryId: " + delivery["DeliveryId"].ToString());
                    Logger.GetInstance().Log(LogLevel.Exception, "Lieferschein ohne Rechnungesmpfänger: PersonId und MemberNumber. " + delivery.ToString());
                    return false;
                }
            }

            // prüfen, ob delivery überhaupt Artikel enthält (wenn nicht, ist nicht definiert, welche Artikel mit diesem Flug verknüpft sind
            if (delivery["DeliveryItems"].Count() == 0)
            {
                InvokeLog(Constants.vbTab + "Fehler: Für diesen Lieferschein " + delivery["DeliveryId"].ToString() + " ist nicht definiert, welche Artikel in Proffix verwendet werden sollen. In FLS muss durch den Administrator zuerst festgelegt werden, welche Artikel für diesen Flug verrechnet werden sollen.");
                Logger.GetInstance().Log(LogLevel.Exception, Constants.vbTab + "Für diesen Lieferschein ist nicht definiert, welche Artikel in Proffix verwendet werden sollen. JSON enthält keine Artikel " + delivery.ToString());
                return false;
            }

            return true;
        }

        /// <summary>
    /// Schreibt den mitgegebenen Text in das Log in der Benutzeroberfläche und alenfalls in die Logdatei
    /// </summary>
    /// <param name="message">Den auszugebenden Text</param>
    /// <param name="doWriteLogFile">Definiert ob der Text auch in das Logfile geschrieben wird</param>
    /// <param name="logLevel">Das Loglevel mit dem die Nachricht geloggt wird</param>
        private void InvokeLog(string message, bool doWriteLogFile = false, LogLevel logLevel = LogLevel.Info)
        {
            if (Log is object)
                Log.Invoke(message);
            if (doWriteLogFile)
                Logger.GetInstance().Log(logLevel, message);
        }

        private void InvokeDoProgressDelivery()
        {
            if (DoProgressDelivery is object)
                DoProgressDelivery.Invoke();
        }

        private void InvokeDoProgressFlight()
        {
            if (DoProgressFlight is object)
                DoProgressFlight.Invoke();
        }
    }
}

// using System;
// using System.Collections.Generic;
// using System.ComponentModel.DataAnnotations;

// Namespace FLS.Data.WebApi.Accounting
// {
// public class DeliveryDetails : FLSBaseData
// {
// Public DeliveryDetails()
// {
// RecipientDetails = new RecipientDetails();
// FlightInformation = new FlightInformation();
// DeliveryItems = new List<DeliveryItemDetails>();
// }

// public Guid DeliveryId { get; set; }

// public FlightInformation FlightInformation { get; set; }

// public RecipientDetails RecipientDetails { get; set; }

// [StringLength(250)]
// public string DeliveryInformation { get; set; }

// [StringLength(250)]
// public string AdditionalInformation { get; set; }

// public List<DeliveryItemDetails> DeliveryItems { get; set; }

// public string DeliveryNumber { get; set; }

// public DateTime? DeliveredOn { get; set; }

// public bool IsFurtherProcessed { get; set; }

// public override Guid Id
// {
// get { return DeliveryId; }
// set { DeliveryId = value; }
// }
// }
// }
