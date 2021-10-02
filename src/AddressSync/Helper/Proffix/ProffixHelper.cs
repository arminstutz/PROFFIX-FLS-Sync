using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json.Linq;
using pxBook;
using SMC.Lib;

namespace FlsGliderSync
{

    /// <summary>
/// Eine Helper Klasse die sich um Aktionen rund um PROFFIX kümmert
/// Hauptsächlich Funktionalitäten die die pxBook Schnitstelle nicht zur verfügung stellt
/// </summary>
    public class ProffixHelper
    {
        private ProffixConnection MyConn { get; set; }
        // Die Liste wird einmal pro Programmausführung gefüllt und als Zwischenspeicher benutzt
        private static Dictionary<string, bool> ExistingFields = new Dictionary<string, bool>();

        public string dateformat { get; set; }
        public string dateTimeFormat { get; set; }

        public ProffixHelper()
        {
        }

        public ProffixHelper(ProffixConnection myconn)
        {
            MyConn = myconn;
            dateformat = DateServerFormat();
            dateTimeFormat = dateformat + " HH:mm:ss.fff";
        }


        // *************************************************************************DATEN AUS PROFFIX LADEN***********************************************************************
        /// <summary>
    /// Laden des letzten Datums des synctypes (AdressSync, ArticleExport...)
    /// </summary>
    /// <returns>Das letzte Synchronisationsdatum</returns>
        public DateTime GetDate(string synctype)
        {
            string sql = string.Empty;
            var rs = new ADODB.Recordset();
            string fehler = string.Empty;

            // Wert aus DB holen
            sql = "Select SyncDate from ZUS_FLSSyncDate where SyncType = '" + synctype + "' order by SyncDate desc";
            if (!MyConn.getRecord(ref rs, sql, ref fehler))
            {
                Logger.GetInstance().Log(LogLevel.Exception, fehler);
            }
            else if (rs.EOF)
            {
                // es konnte kein LastSync geladen werden, da für den angegebenen synctype noch kein Datensatz in ZUS_FLSSync erstellt wurde
                // --> Defaultdate
                return DateTime.Parse("2013-02-02 08:00:00.000");
                // Return DateTime.MinValue
            }

            // gibt lastSync des entsprechenden syncType zurück
            return DateTime.Parse(rs.Fields["SyncDate"].Value.ToString());
        }

        // lädt die in der DB definierten Standardwerte für eine Adresse
        public ADODB.Recordset GetPXAdressDefaultValues()
        {
            var rs = new ADODB.Recordset();
            string sql = string.Empty;
            string fehler = string.Empty;
            var MyConn = new ProffixConnection();
            sql = "Select * from adr_adressdef";
            MyConn.getRecord(ref rs, sql, ref fehler);
            if (!string.IsNullOrEmpty(fehler) | rs.EOF)
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Laden der Defaultwerte für Adressen");
                return null;
            }
            else
            {
                return rs;
            }
        }

        // gibt Liste mit personIds der in Proffix gelöschten Adressen zurück
        public List<string> GetDeletedAddresses(DateTime sinceDate)
        {
            var rs = new ADODB.Recordset();
            string sql;
            string fehler = "";
            var personId_list = new List<string>();
            try
            {
                sql = "Select Z_FLSPersonId from adr_adressen where geloescht = 1 and geaendertAm > '" + sinceDate.ToString(dateTimeFormat) + "'";
                if (!MyConn.getRecord(ref rs, sql, ref fehler))
                {
                    Logger.GetInstance().Log(LogLevel.Exception, fehler);
                }

                while (!rs.EOF)
                {

                    // es interessieren nur Adressen, die bereits PersonId haben (wenn noch keine = noch nie mit FLS synchronisiert --> msus auch nicht in FLS gelöscht werden)
                    if (!string.IsNullOrEmpty(rs.Fields["Z_FLSPersonId"].ToString()))
                    {
                        personId_list.Add(rs.Fields["Z_FLSPersonId"].ToString());
                    }

                    rs.MoveNext();
                }

                return personId_list;
            }
            catch
            {
                Logger.GetInstance().Log("Fehler bei GetGeloeschteAddresses aus Proffix. Möglicher Grund: LastSync = Nothing");
                return null;
            }
        }

        // gibt die PersonId zurück, welche für eine AdressNr in PX gilt
        public string GetPersonId(string adressnr)
        {
            string sql = string.Empty;
            var rs = new ADODB.Recordset();
            string fehler = string.Empty;
            sql = "select Z_FLSPersonId from adr_adressen where adressnradr = " + adressnr;
            if (!MyConn.getRecord(ref rs, sql, ref fehler))
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + " " + fehler);
                return null;
            }

            if (rs.EOF)
            {
                return "";
            }
            else
            {
                return rs.Fields["Z_FLSPersonId"].ToString();
            }
        }

        // sucht anhand einer PersonId die dazugehörige AdressNr
        public string GetAdressNr(string personId)
        {
            string sql = string.Empty;
            var rs = new ADODB.Recordset();
            string fehler = string.Empty;
            try
            {
                sql = "select adressNrADR from adr_adressen where Z_FLSPersonId = '" + personId + "'";
                if (!MyConn.getRecord(ref rs, sql, ref fehler))
                {
                    throw new Exception("Fehler in " + MethodBase.GetCurrentMethod().Name + " " + fehler);
                }

                if (rs.EOF)
                {
                    throw new Exception("Für die PersonId " + personId + "  wurde keine Adresse gefunden");
                }

                return rs.Fields["adressNrADR"].ToString();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
                return null;
            }
        }

        // holt die nächste LaufNr der angegebenen Tabelle und erhöht den Wert in Proffix
        public int GetNextLaufNr(string table)
        {
            string sql = string.Empty;
            var rs_select = new ADODB.Recordset();
            var rs_update = new ADODB.Recordset();
            string fehler = string.Empty;
            int nextNr;
            try
            {

                // momentane LaufNr holen
                sql = "select laufnr from laufnummern where tabelle = '" + table + "'";
                if (!MyConn.getRecord(ref rs_select, sql, ref fehler))
                {
                    Logger.GetInstance().Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name);
                    return default;
                }

                // es wurde keine Zahl geliefert, da, wenn noch nichts in Zusatztabelle geschrieben worden ist, gibt es in Tabelle Laufnummern noch keinen Eintrag für die Tabelle --> 1 nehmen
                if (rs_select.EOF)
                {
                    nextNr = 1;
                    sql = "insert into laufnummern (laufnr, tabelle) values (1, '" + table + "')";
                    if (!MyConn.getRecord(ref rs_update, sql, ref fehler))
                    {
                        Logger.GetInstance().Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + " " + fehler);
                        return default;
                    }
                }

                // es wurde eine Zahl geliefert
                else
                {
                    // nächste LaufNr= + 1
                    nextNr = Conversions.ToInteger(rs_select.Fields["LaufNr"]) + 1;

                    // updatet die LaufNr in Proffix
                    sql = "update laufnummern set laufnr = " + nextNr.ToString() + "where tabelle = '" + table + "'";
                    if (!MyConn.getRecord(ref rs_update, sql, ref fehler))
                    {
                        Logger.GetInstance().Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + " " + fehler);
                        return default;
                    }
                }

                return nextNr;
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + " " + fehler + " " + ex.Message);
                return default;
            }
        }



        // *************************************************************************DATEN IN PROFFIX schreiben***************************************************************
        /// <summary>
    /// Speichern des letzen Synchronisationsdatums
    /// </summary>
    /// <param name="lastDate">Das Datum</param>
        public bool SetDate(DateTime lastDate, string synctype, ref string fehler)
        {
            var rs_lastSync = new ADODB.Recordset();
            string sql = "";
            var rs_id = new ADODB.Recordset();
            string sql_id;
            var id = default(int);

            // nächst mögliche id laden
            sql_id = "select case (select count(*) from zus_FLSSyncDate) when 0 then 1 else (select max(syncid) from ZUS_FLSSyncDate) + 1 end as id";
            if (!MyConn.getRecord(ref rs_id, sql_id, ref fehler))
            {
                Logger.GetInstance().Log("Fehler beim Laden der nächsten Id in ZUS_FLSSyncDate " + fehler);
                return false;
            }

            if (rs_id.EOF)
            {
                Logger.GetInstance().Log("kein Record geladen für " + sql_id);
                return false;
            }

            while (!rs_id.EOF)
            {
                id = Conversions.ToInteger(rs_id.Fields["id"]);
                rs_id.MoveNext();
            }

            try
            {
                // LastDate updaten (nicht über pxBook AddZusatztabelleWerte(), da es Datum nicht richtig frisst)
                sql = "insert into ZUS_FLSSyncDate " + "(SyncId, " + "SyncDate, " + "SyncType, " + "LaufNr, " + "ImportNr, " + "erstelltAm, " + "erstelltVon, " + "geaendertAm, " + "geaendertVon, " + "geaendert, " + "exportiert" + ") values (" + "" + id.ToString() + ", '" + lastDate.ToString(dateTimeFormat) + "', '" + synctype + "', " + GetNextLaufNr("ZUS_FLSSyncDate").ToString() + ", " + "0, '" + DateAndTime.Now.ToString(dateTimeFormat) + "', '" + Assembly.GetExecutingAssembly().GetName().Name + "', '" + DateAndTime.Now.ToString(dateTimeFormat) + "', '" + Assembly.GetExecutingAssembly().GetName().Name + "', " + "1, 0)";

                // sql = "insert into ZUS_FLSSyncDate " + _
                // "(SyncId, " +
                // "SyncDate, " +
                // "SyncType, " +
                // "LaufNr, " +
                // "ImportNr, " +
                // "erstelltAm, " +
                // "erstelltVon, " +
                // "geaendertAm, " +
                // "geaendertVon, " +
                // "geaendert, " +
                // "exportiert" +
                // ") values (" +
                // "(select case (select count(*) from zus_FLSSyncDate) when 0 then 1 else (select max(syncid) from ZUS_FLSSyncDate) + 1 end), '" +
                // lastDate.ToString(dateTimeFormat) + "', '" +
                // synctype + "', " +
                // GetNextLaufNr("ZUS_FLSSyncDate").ToString + ", " +
                // "0, '" +
                // Now.ToString(dateTimeFormat) + "', '" +
                // Assembly.GetExecutingAssembly().GetName.Name + "', '" +
                // Now.ToString(dateTimeFormat) + "', '" +
                // Assembly.GetExecutingAssembly().GetName.Name + "', " +
                // "1, 0)"

                if (!MyConn.getRecord(ref rs_lastSync, sql, ref fehler))
                {
                    Logger.GetInstance().Log("Fehler in " + MethodBase.GetCurrentMethod().Name + " " + fehler);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + " " + ex.Message);
            }

            return default;
        }

        // löscht bereits vorhandene Datensätze für die zu importierende DeliveryId (Überreste von vorgängigen unvollständigen Importen)
        public bool deleteIncompleteDeliveryData(string DeliveryId)
        {
            var rs_pos = new ADODB.Recordset();
            string sql_pos = string.Empty;
            var rs_doknr = new ADODB.Recordset();
            string sql_doknr = string.Empty;
            var rs_delete = new ADODB.Recordset();
            string sql_delete = string.Empty;
            var rs_dokflightlink = new ADODB.Recordset();
            string sql_dokflightlink = string.Empty;
            string fehler = string.Empty;
            if (FlsGliderSync.logAusfuehrlich)
            {
                Logger.GetInstance().Log(LogLevel.Info, "Die Daten für die DeliveryId " + DeliveryId + " werden aus Proffix gelöscht.");
            }
            // ****************************************************************DocPos löschen************************************************************************
            // ' die docNr + PosNr für die zu löschenden DocPos zwischenspeichern um in LAG_Statistik die richtigen Positionen zu finden
            // sql_pos = "select dokumentnrauf, artikelnr, mengeaus from AUF_DokumentPos where Z_DeliveryId = '" + DeliveryId + "'"
            // If Not MyConn.getRecord(rs_pos_sel, sql_pos_sel, fehler) Then
            // Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Ermitteln der zu löschenden Positionen. DeliveryId: " + DeliveryId + fehler)
            // Return False
            // End If

            // TODO Lagerabtrag rückgängig machen, falls in AUF_Doktypen für "Lieferschein" lagerabtrag = 1 gilt. Problem: Derselbe Artikel kann in 1 Dok in mehreren Positionen vorkommen 
            // --> in LAG_Statistik kann man den zugehörigen Datensatz nciht identifizieren --> es würde jedesmal beim Funktionsaufruf die Menge abgezogen, solange es diesen Artikel auf dem Dok hat

            sql_pos = "Delete from AUF_DokumentPos where Z_DeliveryId = '" + DeliveryId + "'";
            if (!MyConn.getRecord(ref rs_pos, sql_pos, ref fehler))
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Löschen der Positionen. DeliveryId: " + DeliveryId + fehler);
                return false;
            }

            // **********************************************************************vervaiste Docs löschen***********************************************************************
            // DokNr ermitteln, für die keine DocPos mehr existieren 
            sql_doknr = "select auf_dokumente.dokumentnrauf as doknr " + "from auf_dokumente left join auf_dokumentpos on auf_dokumente.dokumentnrauf = auf_dokumentpos.dokumentnrauf " + "where auf_dokumente.doktypAUF = 'flsls' and auf_dokumentpos.artikelnrLAG is null and auf_dokumentpos.Z_DeliveryId is null";
            if (!MyConn.getRecord(ref rs_doknr, sql_doknr, ref fehler))
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Laden der verwaisten Doks" + fehler);
                return false;
            }

            // für jede der verwaisten DocNr...
            while (!rs_doknr.EOF)
            {
                int dokNr = Conversions.ToInteger(rs_doknr.Fields["doknr"]);

                // Doc mit DokNr löschen
                sql_delete = "Delete from auf_dokumente where dokumentnrauf = " + rs_doknr.Fields["doknr"].ToString();
                if (!MyConn.getRecord(ref rs_delete, sql_delete, ref fehler))
                {
                    Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Löschen verwaister Doks" + fehler);
                    return false;
                }

                // Verknüpfung Dok Flight löschen
                sql_dokflightlink = "delete from zus_dokflightlink where dokumentnr = " + rs_doknr.Fields["doknr"].ToString();
                if (!MyConn.getRecord(ref rs_dokflightlink, sql_dokflightlink, ref fehler))
                {
                    Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Löschen auf ZUS_DokFlightLink anhand DokNr:" + rs_doknr.Fields["doknr"].ToString() + fehler);
                    return false;
                }

                rs_doknr.MoveNext();
            }

            // Verknüpfung Dok Flight löschen
            sql_dokflightlink = "delete from zus_dokflightlink where deliveryId = '" + DeliveryId + "'";
            if (!MyConn.getRecord(ref rs_dokflightlink, sql_dokflightlink, ref fehler))
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Löschen auf ZUS_DokFlightLink anhand DeliveryId:" + DeliveryId + fehler);
                return false;
            }

            return true;
        }

        // löscht bereits vorhandene Datensätze für die zu importierende FlightId (Überreste von vorgängigen unvollständigen Importen)
        public bool deleteIncompleteFlightData(string flightid)
        {
            var rs = new ADODB.Recordset();
            string sql = string.Empty;
            // Dim rs_dokflightlink As New ADODB.Recordset
            // Dim sql_dokflightlink As String = String.Empty
            string fehler = string.Empty;
            if (FlsGliderSync.logAusfuehrlich)
            {
                Logger.GetInstance().Log(LogLevel.Info, "Die Daten für die FlightId " + flightid + " werden aus Proffix gelöscht.");
            }

            sql = "Delete from ZUS_Flights where FlightId = '" + flightid + "'";
            if (!MyConn.getRecord(ref rs, sql, ref fehler))
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + " FlightId: " + flightid);
                return false;
            }

            return true;
        }

        // setzt alle Artikel die zur FLS Gruppe gehören den Negativbestand auf 1, damit bei AddDokument kein Rückstand verwaltet werden muss
        public bool SetNegativBestand(ref string response)
        {
            var rs_negativbestand = new ADODB.Recordset();
            string fehler = string.Empty;
            string sql = "Update LAG_Artikel set negativbestand = 1 where gruppelag = 'FLS'";
            MyConn.getRecord(ref rs_negativbestand, sql, ref fehler);
            if (!string.IsNullOrEmpty(fehler))
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Negativbestand setzen: " + fehler);
                response = "Negativbestand für FLS-Artikel konnte in Proffix nicht gesetzt werden";
                return false;
            }

            return true;
        }

        // setzt bei der Adresse, wo das Zusatzfeld FLSPersonId = personId ist, gelöscht = 1
        public bool SetPXAddressAsGeloescht(string personId)
        {
            var rs = new ADODB.Recordset();
            string sql;
            string fehler = "";
            sql = "Update adr_adressen set " + "geloescht = 1, " + "geaendertVon = '" + Assembly.GetExecutingAssembly().GetName().Name + "', " + "geaendertAm = '" + DateAndTime.Now.ToString(dateformat + " HH:mm:ss") + "' " + "where Z_FLSPersonId = '" + personId + "'";

            if (!MyConn.getRecord(ref rs, sql, ref fehler))
            {
                Logger.GetInstance().Log(LogLevel.Exception, fehler);
                return false;
            }
            else
            {
                Logger.GetInstance().Log(LogLevel.Info, "deleted: " + personId);
                return true;
            }
        }



        /// <summary>
    /// Setzt ein den Wert eines Zusatzfelds einer Adresse
    /// </summary>
    /// <param name="source">Die Adresse auf der die Zusatzfelder gesetzt werden</param>
    /// <param name="name">Der Name des Zusatzfeldes</param>
    /// <param name="value">Der Wert des Zusatzfeldes</param>
    /// <returns>Die geänderte Adresse</returns>
        public static pxKommunikation.pxAdressen SetZusatzFelder(pxKommunikation.pxAdressen source, string name, string listenName, string typ, string defaultValue, string value)
        {
            if (string.IsNullOrEmpty(source.ZusatzfelderListe))
                source.ZusatzfelderListe = string.Empty;
            if (string.IsNullOrEmpty(source.ZusatzfelderWerte))
                source.ZusatzfelderWerte = string.Empty;

            // Wert umwandeln
            if (value == "True")
            {
                value = "1";
            }
            else if (value == "False")
            {
                value = "0";
            }

            // ************************************************************************* wenn die Eigenschaft noch nicht vorhanden ist --> anhängen *************************************************
            if (Array.IndexOf(source.ZusatzfelderListe.Split("¿".ToCharArray()), name) < 0)
            {
                // wenn bereits etwas im String steht --> "¿" anhängen
                if (source.ZusatzfelderListe.Length > 0)
                {
                    source.ZusatzfelderListe += "¿";
                    source.ZusatzfelderWerte += "¿";
                }

                // Eigenschaftenname + Defaultwert einfügen
                source.ZusatzfelderListe += listenName;
                source.ZusatzfelderWerte += value;
            }

            // ************************************************************* wenn Eigenschaft im String bereits vorhanden --> Wert überschreiben **************************************************
            else
            {
                // an welcher Position soll Wert eingefügt werden?
                int insertAt = Array.IndexOf(source.ZusatzfelderListe.Split("¿".ToCharArray()), name);

                // Werte aus String in Array speichern
                var arr_ZFWerte = source.ZusatzfelderWerte.Split("¿".ToCharArray());

                // An Position insertAt Wert ersetzen
                arr_ZFWerte[insertAt] = value;

                // Werte aus Array in String speichern
                source.ZusatzfelderWerte = string.Join("¿", arr_ZFWerte);
            }

            return source;
        }

        // fügt die Zusatzfelder im JSON hinzu, die ein Datum enthalten (funktioniert nicht über Gobook.AddAdresse)
        public bool SetDatumsZusatzfelderToPXAdresse(pxKommunikation.pxAdressen adresse, JObject person, ref string fehler)
        {
            string sql = string.Empty;
            var rs = new ADODB.Recordset();
            sql = "update ADR_Adressen set " + "Z_Medical1GueltigBis = " + (person["MedicalClass1ExpireDate"] is object ? "'" + DateTime.Parse(person["MedicalClass1ExpireDate"].ToString()).ToString(dateformat) + "'" : "null") + ", " + "Z_Medical2GueltigBis = " + (person["MedicalClass2ExpireDate"] is object ? "'" + DateTime.Parse(person["MedicalClass2ExpireDate"].ToString()).ToString(dateformat) + "'" : "null") + ", " + "Z_MedicalLAPLGueltigBis = " + (person["MedicalLaplExpireDate"] is object ? "'" + DateTime.Parse(person["MedicalLaplExpireDate"].ToString()).ToString(dateformat) + "'" : "null") + ", " + "Z_SegelfluglehrerlizenzGueltigBis = " + (person["GliderInstructorLicenceExpireDate"] is object ? "'" + DateTime.Parse(person["GliderInstructorLicenceExpireDate"].ToString()).ToString(dateformat) + "'" : "null") + " where adressNrADR = " + adresse.AdressNr.ToString();




            if (!MyConn.getRecord(ref rs, sql, ref fehler))
            {
                return false;
            }

            return true;
        }

        // befüllt ZUS_DokFlightLink
        public bool SetDokFlightLink(JObject delivery, int dokNr)
        {
            string sql = string.Empty;
            var rs = new ADODB.Recordset();
            string fehler = string.Empty;

            // prüfen, ob für diese DeliveryId bereits ein DS besteht
            sql = "Select FlightId from ZUS_DokFlightLink where DeliveryId = '" + delivery["DeliveryId"].ToString() + "'";
            if (!MyConn.getRecord(ref rs, sql, ref fehler))
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + " " + fehler);
                return false;
            }

            // nur wenn noch kein DS besteht, einen erstellen
            if (rs.EOF)
            {
                if (!FlsGliderSync.Proffix.GoBook.AddZusatztabelleWerte("ZUS_DokFlightLink", "DokumentNr, " + "DeliveryId, " + "FlightId", dokNr.ToString() + "," + "'" + delivery["DeliveryId"].ToString() + "', " + "'" + FlsHelper.GetValOrDef(delivery, "FlightInformation.FlightId"), ref fehler))
                {
                    Logger.GetInstance().Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + fehler);
                    return false;
                }
            }

            return true;
        }

        // insert the masterflight (Glider or Motorflight) in ZUS_flights
        public bool insertMasterFlight(JObject flight)
        {
            var rs = new ADODB.Recordset();
            string sql = string.Empty;
            string fehler = string.Empty;
            string passengersPersonIds = string.Empty;
            string passengersName = string.Empty;
            string laufnr = string.Empty;
            if (!CreateRecord(flight["FlightId"].ToString(), ref laufnr))
            {
                return false;
            }

            // String für Values für Passengers zusammensetzen
            if (!string.IsNullOrEmpty(FlsHelper.GetValOrDef(flight, "Passengers")))
            {
                foreach (JObject passenger in flight["Passengers"])
                {
                    passengersPersonIds += FlsHelper.GetValOrDef(passenger, "PersonId");
                    passengersPersonIds += "¿";
                    passengersName += FlsHelper.GetValOrDef(passenger, "Firstname");
                    passengersName += " ";
                    passengersName += FlsHelper.GetValOrDef(passenger, "Lastname");
                    passengersName += "; ";
                }
                // leztes Zeichen ¿ abschneiden
                passengersPersonIds = passengersPersonIds.Substring(0, passengersPersonIds.Length - 2);
                passengersName = passengersName.Substring(0, passengersName.Length - 2);
            }

            sql = "update ZUS_flights set " + (!string.IsNullOrEmpty(passengersPersonIds) ? "PassengersPersonIds = '" + passengersPersonIds + "'," : "") + (!string.IsNullOrEmpty(passengersName) ? "PassengersNameString = '" + passengersName + "', " : "") + "StartType = " + FlsHelper.GetValOrDefString(flight, "StartType") + ", " + "CoPilotPersonId = " + FlsHelper.GetValOrDefString(flight, "CoPilot.PersonId") + ", " + "ObserverPersonId = " + FlsHelper.GetValOrDefString(flight, "Observer.PersonId") + ", " + "InvoiceRecipientPersonId = " + FlsHelper.GetValOrDefString(flight, "InvoiceRecipient.PersonId") + ", " + "NrOfLdgsOnStartLocation = " + FlsHelper.GetValOrDefInteger(flight, "NrOfLdgsOnStartLocation") + ", " + getGeneralFlightString(flight, "") + " where laufnr = " + laufnr;
            if (!MyConn.getRecord(ref rs, sql, ref fehler))
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Updaten in ZUS_Flights flightId = " + flight["FlightId"].ToString());
                return false;
            }

            return true;
        }

        // insert the Towflight in ZUS_Flights
        public bool insertTowFlight(JObject flight)
        {
            var rs = new ADODB.Recordset();
            string sql = string.Empty;
            string fehler = string.Empty;
            string laufnr = string.Empty;
            Logger.GetInstance().Log("Flugdaten zum Schleppflug werden importiert TowFlightId: " + FlsHelper.GetValOrDef(flight, "TowFlightFlightId") + " FlightId: " + flight["FlightId"].ToString());
            if (!CreateRecord(flight["FlightId"].ToString(), ref laufnr))
            {
                return false;
            }

            sql = "update ZUS_flights set " + "TowFlightFlightId = " + FlsHelper.GetValOrDefString(flight, "TowFlightFlightId") + ", " + getGeneralFlightString(flight, "TowFlight") + " where laufnr = " + laufnr;
            if (!MyConn.getRecord(ref rs, sql, ref fehler))
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Updaten in ZUS_Flights flightId = " + flight["FlightId"].ToString());
                return false;
            }

            return true;
        }

        // ' fügt der ZUS_DokFlightLink die TowFlightFlightId hinzu (ZUS_DokFlightLink wird mit Delivery abgefüllt, Delivery kennt aber die TowFlightFlightId nicht --> hier erst abfüllen)
        // Public Function updateDokFlightLink(ByVal flightid As String, ByVal towflightflightid As String) As Boolean
        // Dim sql As String = String.Empty
        // Dim rs As New ADODB.Recordset
        // Dim fehler As String = String.Empty

        // sql = "update zus_dokflightlink set towflightflightid = ' " & towflightflightid & "' where flightid = '" & flightid & "'"
        // If Not MyConn.getRecord(rs, sql, fehler) Then
        // Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Updaten in ZUS_DokFlightLink FlightId: " & flightid & " TowFlightFlightId: " & towflightflightid)
        // Return False
        // End If
        // Return True
        // End Function

        // creates a record for a flight in ZUS_Flights
        private bool CreateRecord(string flightid, ref string laufnr)
        {
            try
            {
                var rs = new ADODB.Recordset();
                string sql = string.Empty;
                string fehler = string.Empty;
                // LaufNr ermitteln --> abfangen wenn nicht ermittelt werden konnte
                laufnr = GetNextLaufNr("ZUS_Flights").ToString();
                if (string.IsNullOrEmpty(laufnr))
                {
                    Logger.GetInstance().Log(LogLevel.Exception, "Es konnte keine LaufNr ermittelt werden für ZUS_Flights");
                }

                sql = "insert into zus_Flights (" + "ID, " + "FlightId, " + "LaufNr, " + "ImportNr, " + "geaendert, " + "exportiert, " + "erstelltam, " + "erstelltvon, " + "geaendertam, " + "geaendertvon" + ") values " + "(" + laufnr + ", '" + flightid + "', " + laufnr.ToString() + ", " + "0, " + "1, " + "0, " + "'" + DateAndTime.Now.ToString(dateformat + " HH:mm:ss") + "', " + "'" + Assembly.GetExecutingAssembly().GetName().Name + "', " + "'" + DateAndTime.Now.ToString(dateformat + " HH:mm:ss") + "', " + "'" + Assembly.GetExecutingAssembly().GetName().Name + "')";
                if (!MyConn.getRecord(ref rs, sql, ref fehler))
                {
                    Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Insert in ZUS_Flights flightId = " + flightid);
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

        // erstellt einen String für die Felder, die für Glider sowie TowFlight gleich sind
        private string getGeneralFlightString(JObject flight, string prefix)
        {
            try
            {
                return "" + "AircraftImmatriculation = " + FlsHelper.GetValOrDefString(flight, prefix + "AircraftImmatriculation") + ", " + "PilotPersonId = " + FlsHelper.GetValOrDefString(flight, prefix + "Pilot.PersonId") + ", " + "InstructorPersonId = " + FlsHelper.GetValOrDefString(flight, prefix + "Instructor.PersonId") + ", " + "FlightComment = " + FlsHelper.GetValOrDefString(flight, prefix + "FlightComment") + ", " + "AirState = " + FlsHelper.GetValOrDefString(flight, prefix + "AirState") + ", " + "FlightTypeName = " + FlsHelper.GetValOrDefString(flight, prefix + "FlightTypeName") + ", " + "FlightTypeCode = " + FlsHelper.GetValOrDefString(flight, prefix + "FlightTypeCode") + ", " + "StartLocationIcaoCode = " + FlsHelper.GetValOrDefString(flight, prefix + "StartLocation.IcaoCode") + ", " + "LdgLocationIcaoCode = " + FlsHelper.GetValOrDefString(flight, prefix + "LdgLocation.IcaoCode") + ", " + "OutboundRoute = " + FlsHelper.GetValOrDefString(flight, prefix + "OutboundRoute") + ", " + "InboundRoute = " + FlsHelper.GetValOrDefString(flight, prefix + "InboundRoute") + ", " + "NrOfLdgs = " + FlsHelper.GetValOrDefInteger(flight, prefix + "NrOfLdgs") + ", " + "NoStartTimeInformation = " + FlsHelper.GetValOrDefBoolean(flight, prefix + "NoStartTimeInformation") + ", " + "NoLdgTimeInformation = " + FlsHelper.GetValOrDefBoolean(flight, prefix + "NoLdgTimeInformation") + ", " + "LdgDateTime = " + FlsHelper.GetValOrDefDateTime(flight, prefix + "LdgDateTime", dateformat) + ", " + "StartDateTime = " + FlsHelper.GetValOrDefDateTime(flight, prefix + "StartDateTime", dateformat) + ", " + "FlightDuration = " + (string.IsNullOrEmpty(FlsHelper.GetValOrDef(flight, prefix + "FlightDuration")) ? "NULL" : "'" + DateTime.Parse(flight[prefix + "FlightDuration"].ToString()).ToString("HH:mm:ss") + "'");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        internal bool InFehlertabelleSchreiben(JObject delivery, string fehler)
        {
            var rs_vorhanden = new ADODB.Recordset();
            var rs = new ADODB.Recordset();
            string sql = string.Empty;
            int laufnr;
            // Dim artikelnummern As String = String.Empty

            try
            {

                // prüfen, ob der Datensatz für diese deliveryId noch nicht eingetragen ist
                sql = "select * from zus_err_deliveries where deliveryId = '" + delivery["DeliveryId"].ToString() + "'";
                if (!MyConn.getRecord(ref rs_vorhanden, sql, ref fehler))
                {
                    throw new Exception("Fehler beim Abfragen, ob für DeliveryId bereits ein Datensatz besteht " + delivery.ToString());
                }

                // wenn noch kein Datensatz mit dieser DeliveryId vorhanden
                if (rs_vorhanden.EOF)
                {

                    // Datensatz eintragen
                    laufnr = GetNextLaufNr("ZUS_err_deliveries");
                    if (laufnr == 0)
                    {
                        throw new Exception("Fehler beim Laden der LaufNr");
                    }

                    sql = "insert into zus_err_deliveries (" + "deliveryid, " + "flightid, " + "message, " + "LaufNr, " + "ImportNr, " + "Erstelltam, " + "erstelltvon, " + "geaendertam, " + "geaendertvon, " + "geaendert, " + "exportiert" + ") values (" + FlsHelper.GetValOrDefString(delivery, "DeliveryId") + ", " + FlsHelper.GetValOrDefString(delivery, "FlightInformation.FlightId") + ", " + "'" + fehler + "', " + laufnr.ToString() + ", " + "0, " + "'" + DateAndTime.Now.ToString(dateformat + " HH:mm:ss") + "', " + "'" + Assembly.GetExecutingAssembly().GetName().Name + "', " + "'" + DateAndTime.Now.ToString(dateformat + " HH:mm:ss") + "', " + "'" + Assembly.GetExecutingAssembly().GetName().Name + "'" + ", 1, 0)";
                    if (!MyConn.getRecord(ref rs, sql, ref fehler))
                    {
                        throw new Exception(fehler);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Log(LogLevel.Exception, "fehlerhafter Delivery konnte nicht in Fehlertabelle geschrieben werden. Im JSON ist RecipientDetails leer. " + delivery.ToString());
                return false;
            }
        }
















        // setzt eine Adresse in PX als zu synchronieren
        public bool SetAsZuSynchroniseren(string personId)
        {
            string sql = string.Empty;
            var rs = new ADODB.Recordset();
            string fehler = string.Empty;
            // Synchroniseren für die Adresse 1 setzen
            sql = "update adr_adressen set Z_synchronisieren = 1 where Z_FLSPersonId = '" + personId + "'";
            if (!MyConn.getRecord(ref rs, sql, ref fehler))
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + " " + fehler);
                return false;
            }

            return true;
        }

        // setzt die LaufNr für eine Tabelle wieder auf 0 (aufrufen, wenn Inhalt der Tabelle gelöscht wird)
        public bool resetLaufNr(string table)
        {
            string sql = string.Empty;
            var rs = new ADODB.Recordset();
            string fehler = string.Empty;
            try
            {
                sql = "update laufnummern set laufnr = 0 where tabelle = '" + table + "'";
                if (!MyConn.getRecord(ref rs, sql, ref fehler))
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
                return false;
            }
        }



        // *************************************************************************PRÜFFUNKTIONEN***********************************************************************
        // wurde als MemberStateId eine gültige Id angegeben?
        public bool isValidMemberStateId(string memberStateId, ref string fehler)
        {
            string sql = string.Empty;
            var rs = new ADODB.Recordset();

            // ist ein DS mit der MemberSTateId vorhanden?
            sql = "Select memberStateId from ZUS_FLSMemberStates where memberstateId = '" + memberStateId + "'";
            if (!MyConn.getRecord(ref rs, sql, ref fehler))
            {
                fehler = "Fehler beim Laden der MemberStateId " + memberStateId + fehler;
                return false;
            }

            // wenn kein DS gefunden --> falsche MemberStateId angegeben
            if (rs.EOF)
            {
                fehler = "Kontrollieren Sie die MemberStateId in Proffix. Der Inhalt entspricht keiner vorhandenen MemberStateId.";
                return false;
            }

            return true;
        }

        /// <summary>
    /// Püft ob ein Feld in der PROFFIX Datenbank existiert
    /// </summary>
    /// <param name="field">Der Name des Feldes</param>
    /// <param name="table">Der Name der Tabelle</param>
    /// <returns>Ein Boolen der aussagt ob das Feld existiert</returns>
        private static bool DoesFieldExist(string field, string table)
        {
            // Prüfen ob das feld im zwischenspeicher abgelegt ist
            if (!ExistingFields.ContainsKey(field))
            {
                var rs = new ADODB.Recordset();
                bool doExist;
                // Suchen des Feldes in der Datenbank
                rs.Open("select * from sys.columns where Name = N'" + field + "' and Object_ID = Object_ID(N'" + table + "')", FlsGliderSync.Proffix.DataBaseConnection);
                doExist = !rs.EOF;
                // Resulatat in den Zwischenspeicher schreiben
                ExistingFields.Add(field, doExist);
                rs.Close();
            }
            // Resultat zurückgeben
            return ExistingFields[field];
        }

        // prüft anhand PersonId, ob eine Adresse gelöscht vorhanden ist
        public bool DoesAddressExistsAsGeloescht(string personId, string adressnr)
        {
            string sql = string.Empty;
            var rs = new ADODB.Recordset();
            string fehler = string.Empty;
            sql = "select * from adr_adressen where " + "geloescht = 1 and " + "AdressNrADR = " + adressnr + "FLSPersonId = '" + personId + "'";
            if (!MyConn.getRecord(ref rs, sql, ref fehler))
            {
                throw new Exception("Fehler in " + MethodBase.GetCurrentMethod().Name + " " + fehler);
            }
            else if (rs.EOF)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        // prüft, ob eine Adresse anhand PersonId bereits vorhanden ist in PX, aber Synchroniseren = 0 ist. 
        public bool DoesAddressExistsAsNichtZuSynchronisierend(string personId)
        {
            string sql = string.Empty;
            var rs = new ADODB.Recordset();
            string fehler = string.Empty;
            sql = "select * from adr_adressen where Z_Synchronisieren = 0 and Z_FLSPersonId = '" + personId + "'";
            if (!MyConn.getRecord(ref rs, sql, ref fehler))
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + " " + fehler);
                return default;
            }

            // es wurde kein Datensatz mit der PersonId gefunden
            if (rs.EOF)
            {
                return false;
            }
            // es wurde eine entsprechene Adresse mit Synchroniseren = 0 gefunden
            else
            {
                return true;
            }
        }




        // *************************************************************Daten aus DB laden**************************************************************************
        // gibt ServerFormat für Datum zurück
        public string DateServerFormat()
        {
            string defaultFormat = "yyyy-MM-dd";
            string sql;
            var rsformat = new ADODB.Recordset();
            string fehler = "";
            try
            {
                sql = "SELECT dateformat FROM master..syslanguages WHERE name = @@LANGUAGE";
                MyConn.getRecord(ref rsformat, sql, ref fehler);
                switch (rsformat.Fields["dateformat"].ToString() ?? "")
                {
                    case "dmy":
                        {
                            return "dd-MM-yyyy";
                        }

                    case "mdy":
                        {
                            return "MM-dd-yyyy";
                        }

                    case "ymd":
                        {
                            return "yyyy-MM-dd";
                        }

                    default:
                        {
                            return defaultFormat;
                        }
                }
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler in DateServerFormat()");
                return defaultFormat;
            }
        }



        // *********************************************************pxAdresse-Objekt auslesen**********************************************************************************
        /// <summary>
    /// Gibt den Wert eines Zusatzfeldes einer Adresse zurück
    /// </summary>
    /// <param name="source">Die Adresse von der die Zusatzfelder gelesen werden</param>
    /// <param name="name">Der Name des Zusatzfeldes</param>
    /// <returns>Der Wert des Zusatzfeldes</returns>
        public static string GetZusatzFelder(pxKommunikation.pxAdressen source, string name)
        {
            if (string.IsNullOrEmpty(source.ZusatzfelderListe))
                return null;
            int getFrom = Array.IndexOf(source.ZusatzfelderListe.Split("¿".ToCharArray()), name);
            try
            {
                return source.ZusatzfelderWerte.Split("¿".ToCharArray())[getFrom];
            }
            catch
            {
                return null;
            }
        }

        // Holt des Änderungsdatum der Adresse. Dieses ist das neuste der Felder "GeändertAm" und "ErstelltAm"
        public static DateTime GetAddressChangeDate(pxKommunikation.pxAdressen adress)
        {
            // Ruft die Helper Funktion des GeneralHelpers auf
            return GeneralHelper.GetNewestDate(new[] { !string.IsNullOrEmpty(adress.ErstelltAm) ? DateTime.Parse(adress.ErstelltAm) : DateTime.MinValue, !string.IsNullOrEmpty(adress.GeaendertAm) ? DateTime.Parse(adress.GeaendertAm) : DateTime.MinValue });
        }

        // Holt des Änderungsdatum der Adresse. Dieses ist das neuste der Felder "GeändertAm" und "ErstelltAm"
        public static DateTime GetArticleChangeDate(pxKommunikation.pxArtikel article)
        {
            // Ruft die Helper Funktion des GeneralHelpers auf
            return GeneralHelper.GetNewestDate(new[] { !string.IsNullOrEmpty(article.ErstelltAm) ? DateTime.Parse(article.ErstelltAm) : DateTime.MinValue, !string.IsNullOrEmpty(article.GeaendertAm) ? DateTime.Parse(article.GeaendertAm) : DateTime.MinValue });
        }



        // ***********************************************************pxAdressen-Objekt bearbeiten*******************************************************************
        // wenn aus FLS eine Adresse importiert werden soll, für die kein Ort angegeben ist (Pflichtfeld in Proffix) --> Default setzen
        public static pxKommunikation.pxAdressen SetAdressDefault(pxKommunikation.pxAdressen adress, ADODB.Recordset rs_adressdefault)
        {

            // Ort + PLZ sind Pflichtfelder für PX
            if (string.IsNullOrEmpty(adress.Plz))
            {
                adress.Plz = "9999";
            }

            if (string.IsNullOrEmpty(adress.Ort))
            {
                adress.Ort = "Ort unbekannt";
            }

            // Felder, für die aus FLS keine Werte kommen können, aber für die Dokumenterstellung nötig sind:
            // aus recordset aus ADR_Adressdef (anfangs Sync 1 Mal geladen) lesen
            if (adress.Kondition == 0)
            {
                try
                {
                    adress.Kondition = Conversions.ToInteger(rs_adressdefault.Fields["DKondition"]);
                }
                catch (Exception ex)
                {
                    // wenn kein Adressdefault für Kondition definiert ist --> 1
                    adress.Kondition = 1;
                }
            }

            if (string.IsNullOrEmpty(adress.Sammelkonto))
            {
                try
                {
                    adress.Sammelkonto = rs_adressdefault.Fields["DSammelKto"].ToString();
                }
                catch (Exception ex)
                {
                    // wenn kein Adressdefault für Sammelkonto definiert ist --> 1100
                    adress.Sammelkonto = "1100";
                }
            }

            if (string.IsNullOrEmpty(adress.Waehrung))
            {
                try
                {
                    adress.Waehrung = rs_adressdefault.Fields["DWaehrung"].ToString();
                }
                catch (Exception ex)
                {
                    // wenn kein Adressdefault für Währung definiert ist --> CHF
                    adress.Waehrung = "CHF";
                }
            }

            // FLS gibt es nur auf Deutsch --> Standardsprache wird fix aud Deutsch gesetzt
            if (string.IsNullOrEmpty(adress.Sprache))
            {
                adress.Sprache = "D";
            }

            return adress;
        }



        // ************************************************************Hilfsfunktionen*******************************************************************************

        /// <summary>
    /// Die Zusatzfelder werden der PROFFIX Schnitstelle als SQL mitgegeben
    /// Hier werden sie in SQL geparst
    /// </summary>
    /// <param name="source">Die Adresse deren Zusatzfelder gelesen werden</param>
    /// <returns>Der SQL String</returns>
        public static string CreateZusatzFelderSql(pxKommunikation.pxAdressen source)
        {
            // Definieren der Zusatzfelder liste
            var values = new Dictionary<string, string>();
            values.Add("Z_Segelfluglehrer_Lizenz", GetZusatzFelder(source, "Z_Segelfluglehrer_Lizenz"));
            values.Add("Z_Segelflugpilot_Lizenz", GetZusatzFelder(source, "Z_Segelflugpilot_Lizenz"));
            values.Add("Z_Segelflugschueler_Lizenz", GetZusatzFelder(source, "Z_Segelflugschueler_Lizenz"));
            values.Add("Z_Motorflugpilot_Lizenz", GetZusatzFelder(source, "Z_Motorflugpilot_Lizenz"));
            values.Add("Z_Schleppilot_Lizenz", GetZusatzFelder(source, "Z_Schleppilot_Lizenz"));
            values.Add("Z_Segelflugpassagier_Lizenz", GetZusatzFelder(source, "Z_Segelflugpassagier_Lizenz"));
            values.Add("Z_TMG_Lizenz", GetZusatzFelder(source, "Z_TMG_Lizenz"));
            values.Add("Z_Windenfuehrer_Lizenz", GetZusatzFelder(source, "Z_Windenfuehrer_Lizenz"));
            values.Add("Z_Motorfluglehrer_Lizenz", GetZusatzFelder(source, "Z_Motorfluglehrer_Lizenz"));
            values.Add("Z_Schleppstart_Zulassung", GetZusatzFelder(source, "Z_Schleppstart_Zulassung"));
            values.Add("Z_Eigenstart_Zulassung", GetZusatzFelder(source, "Z_Eigenstart_Zulassung"));
            values.Add("Z_Windenstart_Zulassung", GetZusatzFelder(source, "Z_Windenstart_Zulassung"));
            values.Add("Z_SpotURL", "'" + GetZusatzFelder(source, "Z_SpotURL") + "'");
            values.Add("Z_Email_Geschaeft", "'" + GetZusatzFelder(source, "Z_Email_Geschaeft") + "'");
            values.Add("Z_Lizenznummer", "'" + GetZusatzFelder(source, "Z_Lizenznummer") + "'");
            values.Add("Z_FLSPersonId", "'" + GetZusatzFelder(source, "Z_FLSPersonId") + "'");
            values.Add("Z_Segelflugpilot", GetZusatzFelder(source, "Z_Segelflugpilot"));
            values.Add("Z_Segelfluglehrer", GetZusatzFelder(source, "Z_Segelfluglehrer"));
            values.Add("Z_Segelflugschueler", GetZusatzFelder(source, "Z_Segelflugschueler"));
            values.Add("Z_Motorflugpilot", GetZusatzFelder(source, "Z_Motorflugpilot"));
            values.Add("Z_Motorfluglehrer", GetZusatzFelder(source, "Z_Motorfluglehrer"));
            values.Add("Z_Passagier", GetZusatzFelder(source, "Z_Passagier"));
            values.Add("Z_Schleppilot", GetZusatzFelder(source, "Z_Schleppilot"));
            values.Add("Z_Windenfuehrer", GetZusatzFelder(source, "Z_Windenfuehrer"));
            values.Add("Z_erhaeltFlugreport", GetZusatzFelder(source, "Z_erhaeltFlugreport"));
            values.Add("Z_erhaeltReservationsmeldung", GetZusatzFelder(source, "Z_erhaeltReservationsmeldung"));
            values.Add("Z_erhaeltPlanungserinnerung", GetZusatzFelder(source, "Z_erhaeltPlanungserinnerung"));
            values.Add("Z_erhaeltFlugStatistikenZuEigenen", GetZusatzFelder(source, "Z_erhaeltFlugStatistikenZuEigenen"));
            values.Add("Z_MemberStateId", "'" + GetZusatzFelder(source, "Z_MemberStateId") + "'");

            // Umwandeln des Dictionary in ein SQL String
            string sql = string.Empty;
            foreach (var value in values)
            {
                if (!string.IsNullOrEmpty(sql))
                {
                    sql += ", ";
                }

                if (DoesFieldExist(value.Key, "ADR_Adressen"))
                {
                    sql += value.Key + " = " + value.Value;
                }
            }

            // Zusatzfeld Synchronisieren anhängen
            sql += ", Z_Synchronisieren = 1";
            return sql;
        }


        // updated das IsActive Feld (geloescht = 1 entspricht IsActive = False)
        public bool SetIsActiveInFLSPersonDependingOnGeloescht(ref JObject person, string adressnr)
        {
            string sql = string.Empty;
            var rs = new ADODB.Recordset();
            string fehler = string.Empty;
            bool IsActive;
            try
            {
                sql = "select geloescht from adr_adressen where adressnradr = " + adressnr;
                if (!MyConn.getRecord(ref rs, sql, ref fehler))
                {
                    throw new Exception(fehler);
                    return false;
                }

                if (rs.Fields["geloescht"].ToString() == "1")
                {
                    IsActive = false;
                }
                else
                {
                    IsActive = true;
                }

                person["ClubRelatedPersonDetails"]["IsActive"] = IsActive;
                return true;
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return false;
            }
        }

        // updatet in PX das geloeschtFeld
        public bool SetGeloeschtInPXAdresseDependingOnIsActive(JObject person)
        {
            string sql = string.Empty;
            var rs = new ADODB.Recordset();
            string fehler = string.Empty;
            try
            {

                // IsActive umgekehrt auf Geloescht updaten (IsActive = false --> geloescht = 1)
                string geloescht = string.Empty;
                if (FlsHelper.GetValOrDef(person, "ClubRelatedPersonDetails.IsActive") == "false")
                {
                    geloescht = "1";
                }
                else
                {
                    geloescht = "0";
                }

                sql = "update adr_adressen set geloescht = " + geloescht + " where Z_FLSPersonId = '" + person["PersonId"].ToString().ToLower().Trim() + "'";
                if (!MyConn.getRecord(ref rs, sql, ref fehler))
                {
                    throw new Exception(fehler);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return false;
            }
        }
    }
}


// ''' <summary>
// ''' Sucht die nächste Adressnummer und reserviert diese
// ''' </summary>
// ''' <returns>Die nächste Adressnummer</returns>
// Public Shared Function GetNextAdressNr() As String
// Return NextNrCircle(Proffix.GoBook.Mandant & ".dbo.ADR_Adressen", "AdressNrADR")
// End Function

// ''' <summary>
// ''' Gibt die nächste Nummer eines PROFFIX Nummernkreises zurück und reserviert diese
// ''' </summary>
// ''' <param name="Tabelle">Die Tabelle des Nummernkreises</param>
// ''' <param name="Key">Der Key des Nummernkreises</param>
// ''' <returns>Die nächste Nummer</returns>
// Public Shared Function NextNrCircle(ByVal Tabelle As String, ByVal Key As String) As String

// Try
// Dim rs As New ADODB.Recordset,
// teile() As String = Tabelle.Split("."c),
// fehler As String,
// sql As String,
// nr As String = String.Empty
// If teile.Length = 3 Then
// Dim Mandant As String = teile(0)
// Dim Schema As String = teile(1)
// Tabelle = teile(2)
// fehler = ""
// sql = "select NrAktuell from " & Mandant & "." & Schema & ".NrKreis where NrKreis = '" & Tabelle & Key & "' "
// rs.Open(sql, Proffix.DataBaseConnection)
// If String.IsNullOrEmpty(fehler) Then
// If Not rs.EOF Then
// nr = getFeld(rs.Fields("NrAktuell"), "-2")
// nr = CStr(CInt(nr) + 1)
// End If
// rs.Close()
// sql = "update NrKreis set NrAktuell=" & nr & " where NrKreis = '" & Tabelle & Key & "' "
// rs.Open(sql, Proffix.DataBaseConnection)
// If Not String.IsNullOrEmpty(fehler) Then
// 'setLog(Fehler)
// End If
// Return nr
// Else
// 'setLog(Fehler)
// End If
// Else
// 'setLog("Tabelle ohne Mandant und Schema übergeben", "naechsteLaufnummer(" & Tabelle & ")")
// End If
// Catch ex As Exception
// 'setLog(ex.Message, ex.StackTrace)
// End Try
// Return String.Empty
// End Function

// ''' <summary>
// ''' Gibt den Wert eines SQL Feldes zurück und löst ihn auf
// ''' </summary>
// Private Shared Function getFeld(ByVal feld As ADODB.Field, ByVal strInit As String, Optional ByVal prefix As String = "", Optional ByVal postfix As String = "", Optional ByVal maxlenght As Integer = 0, Optional ByVal sql As Boolean = False, Optional ByVal UmbruchTabs As Boolean = True) As String
// Dim Str As String
// Try
// Str = CStr(feld.Value)
// Catch ex As Exception
// Str = strInit
// End Try
// If String.IsNullOrEmpty(Str) Then
// getFeld = prefix & postfix
// Else
// If sql Then Str = Str.Replace("'", "''")
// If Not UmbruchTabs Then
// While Str.IndexOf(Chr(13)) >= 0
// Str = Str.Replace(Chr(13), "¬")
// End While
// While Str.IndexOf(Chr(10)) >= 0
// Str = Str.Replace(Chr(10), "¬")
// End While
// While Str.IndexOf(";;") >= 0
// Str = Str.Replace("¬¬", "¬")
// End While
// ' Tab durch Leerzeichen ersetzen
// While Str.IndexOf(Chr(9)) >= 0
// Str = Str.Replace(Chr(9), " ")
// End While
// ' Leerzeichen vor :;,. entfernen
// While Str.IndexOf(" :") >= 0
// Str = Str.Replace(" :", ": ")
// End While
// While Str.IndexOf(" ;") >= 0
// Str = Str.Replace(" ;", "; ")
// End While
// While Str.IndexOf(" ,") >= 0
// Str = Str.Replace(" ,", ", ")
// End While
// While Str.IndexOf(" .") >= 0
// Str = Str.Replace(" .", ". ")
// End While
// ' Leerzeichen nach ;, einfügen
// Str = Str.Replace(";", "; ")
// Str = Str.Replace(",", ", ")
// ' doppelte Leerzeichen entfernen
// While Str.IndexOf("  ") >= 0
// Str = Str.Replace("  ", " ")
// End While
// While Str.IndexOf("--") >= 0
// Str = Str.Replace("--", "-")
// End While
// ' Leerzeichen am Anfang und am Ende entfernen
// Str = Str.Trim
// ' ; am Anfang und am Ende entfernen
// If Not String.IsNullOrEmpty(Str) Then If Str.Substring(0, 1) = ";" Then Str = Str.Substring(1)
// If Not String.IsNullOrEmpty(Str) Then If Str.Substring(Str.Length - 1, 1) = ";" Then Str = Str.Substring(0, Str.Length - 1)
// Str = Str.Trim
// End If
// If maxlenght > 0 And Str.Length > maxlenght Then Str = Str.Substring(0, maxlenght)
// getFeld = prefix & Str & postfix
// End If
// End Function
