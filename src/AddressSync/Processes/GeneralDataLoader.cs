using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using SMC.Lib;

namespace FlsGliderSync
{
    public class GeneralDataLoader
    {
        private FlsConnection flsClient;
        private ProffixHelper pxhelper;
        private ProffixConnection myconn;
        private Importer importer;
        private static Dictionary<string, string> _laender_dict = new Dictionary<string, string>();   // enthält die in FLS verfügbaren Länder und ihre CountryIds

        public Action<string> Log { get; set; }    // Aktion, die ausgeführt wird, wenn in Logfeld geschrieben wird

        public static Dictionary<string, string> GetLaender
        {
            get
            {
                return _laender_dict;
            }
        }

        public GeneralDataLoader(ProffixHelper pxhelper, FlsConnection flsclient, ProffixConnection myconn, Importer importer)
        {
            flsClient = flsclient;
            this.myconn = myconn;
            this.pxhelper = pxhelper;
            this.importer = importer;
        }

        // allgemeine Daten laden
        public bool loadGeneralData()
        {
            try
            {
                if (loadCountries())     // wird nur in Liste in Programm geladen
                {
                    if (loadMemberStates()) // wird in ZUS_FLSMemberStates geladen
                    {
                        if (loadAircrafts()) // wird in ZUS_FLSAircrafts geladen
                        {
                            if (loadLocations()) // wird in ZUS_FLSLocations geladen
                            {
                                return true;
                            }
                            else
                            {
                                Logger.GetInstance().Log(LogLevel.Exception, "Fehler in loadLocations");
                            }
                        }
                        else
                        {
                            Logger.GetInstance().Log(LogLevel.Exception, "Fehler in loadAircrafts");
                        }
                    }
                    else
                    {
                        Logger.GetInstance().Log(LogLevel.Exception, "Fehler in loadMemberStates");
                    }
                }
                else
                {
                    Logger.GetInstance().Log(LogLevel.Exception, "Fehler in loadCountries");
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name);
                return false;
            }
        }

        // ***************************************************************************Data for Proffix***************************************************************************
        // lädt alle Orte mit ihren Ids in ZUS_FLSLocations
        private bool loadLocations()
        {
            string sql_delete = string.Empty;
            var rs_delete = new ADODB.Recordset();
            string sql_insert = string.Empty;
            var rs_insert = new ADODB.Recordset();
            System.Threading.Tasks.Task<JArray> locations;
            string fehler = string.Empty;
            try
            {
                // TODO testen, wenn noch nie etwas in Zusazttabelle stand. Hat in SGN erst funktioniert, wenn Datensatz in Laufnummern für Tabelle

                // LaufNr holen. Wird eigentlich für pxbook.AddZusatztabelleWerte() nicht benötigt, aber pxBook.AddZusatztabelleWerte() funktioniert nicht, wenn kein Eintrag für Tabelle in Tabelle Laufnummern
                pxhelper.GetNextLaufNr("ZUS_FLSLocations");

                // bisheriger Inhalt löschen
                sql_delete = "Delete from ZUS_FLSLocations";
                if (!myconn.getRecord(ref rs_delete, sql_delete, ref fehler))
                {
                    throw new Exception("Fehler beim Löschen des Inhalts aus ZUS_FLSLocations");
                }
                else
                {
                    if (!pxhelper.resetLaufNr("ZUS_FLSLocations"))
                    {
                        throw new Exception("Fehler in resetLaufNr");
                    }

                    // Infos zu Ländern aus FLS laden
                    locations = flsClient.CallAsyncAsJArray(My.MySettingsProperty.Settings.ServiceAPILocationsMethod);
                    locations.Wait();

                    // Infos in ZUS_FLSLocations schreiben
                    foreach (JObject location in locations.Result.Children())
                    {
                        if (!FlsGliderSync.Proffix.GoBook.AddZusatztabelleWerte("ZUS_FLSLocations", "LocationId, " + "LocationName, " + "IcaoCode, " + "CountryName", "'" + FlsHelper.GetValOrDef(location, "LocationId") + "', '" + FlsHelper.GetValOrDef(location, "LocationName") + "', '" + FlsHelper.GetValOrDef(location, "IcaoCode") + "', '" + FlsHelper.GetValOrDef(location, "CountryName") + "'", ref fehler))
                        {
                            Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Beschreiben von ZUS_FLSLocations " + fehler);
                            return false;
                        }
                        // If logAusfuehrlich Then
                        // Logger.GetInstance.Log(LogLevel.Info, "Location in ZUS_FLSAircrafts geschrieben: " + location.ToString)
                        // End If
                    }
                }

                Logger.GetInstance().Log(LogLevel.Info, locations.Result.Children().Count().ToString() + "Location-Informationen erfolgreich in ZUSLocations importiert");
                return true;
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return false;
            }
        }


        // lädt Memberstates
        private bool loadMemberStates()
        {
            System.Threading.Tasks.Task<JArray> memberStatesResult;
            string sql_delete = string.Empty;
            var rs_delete = new ADODB.Recordset();
            string sql_insert = string.Empty;
            var rs_insert = new ADODB.Recordset();
            string fehler = string.Empty;
            try
            {
                // LaufNr holen. Wird eigentlich für pxbook.AddZusatztabelleWerte() nicht benötigt, aber pxBook.AddZusatztabelleWerte() funktioniert nicht, wenn kein Eintrag für Tabelle in Tabelle Laufnummern
                pxhelper.GetNextLaufNr("ZUS_FLSMemberStates");


                // ZUS_FLSMemberStates leeren
                sql_delete = "Delete from ZUS_FLSMemberStates";
                if (!myconn.getRecord(ref rs_delete, sql_delete, ref fehler))
                {
                    Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Löschen des Inhalts der Tabelle ZUS_FLSMemberStates" + fehler);
                }

                if (!pxhelper.resetLaufNr("ZUS_FLSMemberStates"))
                {
                    throw new Exception("Fehler in resetLaufNr");
                    return false;
                }

                // MemberStatedaten holen
                memberStatesResult = flsClient.CallAsyncAsJArray(My.MySettingsProperty.Settings.ServiceAPIMemberStates);
                memberStatesResult.Wait();

                // alle Adressen aus FLS in ZUS_FLSMemberStates einfügen
                foreach (JObject memberState in memberStatesResult.Result.Children())
                {
                    if (!FlsGliderSync.Proffix.GoBook.AddZusatztabelleWerte("ZUS_FLSMemberstates", "MemberStateId, " + "MemberStateName", "'" + memberState["MemberStateId"].ToString() + "', '" + FlsHelper.GetValOrDef(memberState, "MemberStateName") + "'", ref fehler))
                    {
                        Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Beschreiben von ZUS_FLSMemberStates " + fehler);
                        return false;
                    }
                    // If logAusfuehrlich Then
                    // Logger.GetInstance.Log(LogLevel.Info, "memberstate in ZUS_Memberstates geschrieben: " + memberState.ToString)
                    // End If
                }

                Logger.GetInstance().Log(LogLevel.Info, memberStatesResult.Result.Children().Count().ToString() + "MemberStates erfolgreich geladen");
                return true;
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Log(LogLevel.Exception, ex.Message);
                return false;
            }
        }

        // füllt ZUS_FLSPersons 1 zu 1 anhand Personeninformationen aus FLS ab --> man sieht Verbindung von PersonId und Adressnr/Name auch in Proffix
        public bool importPersons()
        {
            string sql_delete = string.Empty;
            var rs_delete = new ADODB.Recordset();
            string sql_insert = string.Empty;
            var rs_insert = new ADODB.Recordset();
            string fehler = string.Empty;
            System.Threading.Tasks.Task<JArray> personResult;
            try
            {

                // ZUS_FLSPersons leeren
                sql_delete = "Delete from ZUS_FLSPersons";
                if (!myconn.getRecord(ref rs_delete, sql_delete, ref fehler))
                {
                    Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Löschen des Inhalts der Tabelle ZUS_FLSPersons" + fehler);
                    return false;
                }

                if (!pxhelper.resetLaufNr("ZUS_FLSPersons"))
                {
                    Logger.GetInstance().Log(LogLevel.Exception, "Fehler in resetLaufNr");
                    return false;
                }

                // Alle ungelöschten FLS Adressen holen, wenn nicht bereits in Fkt mitgegeben
                personResult = flsClient.CallAsyncAsJArray(My.MySettingsProperty.Settings.ServiceAPIModifiedPersonFullDetailsMethod + DateTime.MinValue.ToString("yyyy-MM-dd"));
                personResult.Wait();

                // alle Adressen aus FLS in ZUS_FLSPersons einfügen (damit Verknüpfung PersonId AdressNr Name auch in Proffix sichtbar
                foreach (JObject person in personResult.Result.Children())
                {

                    // nicht über pxBook da zu langsam bei so vielen DS
                    sql_insert = "insert into ZUS_FLSPersons (" + "PersonId, " + "Name, " + "Vorname, " + "Ort, " + "VereinsmitgliedNrAdressNr, " + "LaufNr, " + "ImportNr, " + "ErstelltAm, " + "ErstelltVon, " + "GeaendertAm, " + "GeaendertVon, " + "Geaendert, " + "Exportiert" + ") values (" + "'" + person["PersonId"].ToString().ToLower().Trim() + "', " + FlsHelper.GetValOrDefString(person, "Lastname") + ", " + FlsHelper.GetValOrDefString(person, "Firstname") + ", " + FlsHelper.GetValOrDefString(person, "City") + ", " + FlsHelper.GetValOrDefString(person, "ClubRelatedPersonDetails.MemberNumber") + ", " + pxhelper.GetNextLaufNr("ZUS_FLSPersons").ToString() + ", " + "0, " + "'" + DateAndTime.Now.ToString(pxhelper.dateTimeFormat) + "', " + "'" + Assembly.GetExecutingAssembly().GetName().Name + "', " + "'" + DateAndTime.Now.ToString(pxhelper.dateTimeFormat) + "', " + "'" + Assembly.GetExecutingAssembly().GetName().Name + "', " + "1, 0" + ")";
                    if (!myconn.getRecord(ref rs_insert, sql_insert, ref fehler))
                    {
                        Logger.GetInstance().Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + " " + fehler + sql_insert);
                        return false;
                    }
                    // If logAusfuehrlich Then
                    // Logger.GetInstance.Log(LogLevel.Info, "person in ZUS_FLSPersons geschrieben: " + person.ToString)
                    // End If
                }

                Logger.GetInstance().Log(LogLevel.Info, personResult.Result.Children().Count().ToString() + " FLS Adressen wurden in ZUS_Persons geladen");
                return true;
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Log(LogLevel.Exception, ex.Message);
                return false;
            }
        }

        // lädt Aircrafts
        private bool loadAircrafts()
        {
            System.Threading.Tasks.Task<JArray> aircraftsResult;
            string sql_delete = string.Empty;
            var rs_delete = new ADODB.Recordset();
            string sql_insert = string.Empty;
            var rs_insert = new ADODB.Recordset();
            string fehler = string.Empty;
            try
            {
                // LaufNr holen. Wird eigentlich für pxbook.AddZusatztabelleWerte() nicht benötigt, aber pxBook.AddZusatztabelleWerte() funktioniert nicht, wenn kein Eintrag für Tabelle in Tabelle Laufnummern
                if (pxhelper.GetNextLaufNr("ZUS_FLSAircrafts") == default)
                {
                    Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Laden der nächsten LaufNr für ZUS_FLSAircrafts");
                }

                // ZUS_FLSMemberStates leeren
                sql_delete = "Delete from ZUS_FLSAircrafts";
                if (!myconn.getRecord(ref rs_delete, sql_delete, ref fehler))
                {
                    Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Löschen des Inhalts der Tabelle ZUS_FLSAircrafts " + fehler);
                    return false;
                }

                // LaufNr auf 1 setzen, da Daten aus Tabelle gelöscht wurden
                if (!pxhelper.resetLaufNr("ZUS_FLSAircrafts"))
                {
                    throw new Exception("Fehler in resetLaufNr");
                    return false;
                }

                // MemberStatedaten holen
                aircraftsResult = flsClient.CallAsyncAsJArray(My.MySettingsProperty.Settings.ServiceAPIAircraftsMethod);
                aircraftsResult.Wait();


                // alle Adressen aus FLS in ZUS_FLSMemberStates einfügen
                foreach (JObject aircraft in aircraftsResult.Result.Children())
                {
                    if (!FlsGliderSync.Proffix.GoBook.AddZusatztabelleWerte("ZUS_FLSAircrafts", "AircraftId, " + "Immatriculation", "'" + aircraft["AircraftId"].ToString() + "', '" + FlsHelper.GetValOrDef(aircraft, "Immatriculation") + "'", ref fehler))
                    {
                        Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Beschreiben von ZUS_FLSAircrafts" + fehler);
                        return false;
                    }
                    else
                    {
                        // If logAusfuehrlich Then
                        // Logger.GetInstance.Log(LogLevel.Info, "Aircraft in ZUS_FLSAircrafts geschrieben: " + aircraft.ToString)
                        // End If
                    }
                }

                Logger.GetInstance().Log(LogLevel.Info, aircraftsResult.Result.Children().Count().ToString() + " Aircrafts erfolgreich geladen");
                return true;
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Log(LogLevel.Exception, ex.Message);
                return false;
            }
        }

        // ****************************************************************only stored in this application************************************************************************
        // lädt die in FLS vorhandenen Länder (FLS will CountryId, Proffix will Länderkürzel)
        private bool loadCountries()
        {
            System.Threading.Tasks.Task<JArray> laender;
            try
            {

                // Länder auslesen und in Dictionnary abspeichern
                laender = flsClient.CallAsyncAsJArray(My.MySettingsProperty.Settings.ServiceAPICountriesMethod);
                laender.Wait();
                foreach (var land in laender.Result.Children())
                {
                    // wenn Land noch nicht enthalten (damit kein Fehler beim 2. Synchronisieren)
                    if (!_laender_dict.ContainsKey(land["CountryCode"].ToString()))
                    {
                        _laender_dict.Add(land["CountryCode"].ToString(), land["CountryId"].ToString());
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Log(LogLevel.Critical, "Fehler beim Laden der Länder" + ex.Message);
                return false;
            }
        }



        // ruft Deliveries + Flights aus FLS erneut ab (werden beim nächsten Mal wieder geliefert werden) und löscht für diese DeliveryIds bzw. FlightIds alle bereits bestehenden Daten, damit nicht 2x ein LS erstellt wird bzw. Flugdaten 2x in ZUS_flight importiert werden
        public bool deleteIncompleteData()
        {
            try
            {
                if (!deleteIncompleteDeliveries())
                {
                    Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Löschen unvollständiger Lieferscheindaten. Möglicherweise sind Lieferscheine vorhanden, die unvollständig sind.");
                    throw new Exception("Fehler in " + MethodBase.GetCurrentMethod().Name);
                    return false;
                }

                // unvollständige Flugdaten löschen
                if (!deleteIncompleteFlights())
                {
                    Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Löschen unvollständiger Flugdaten.");
                    throw new Exception("Fehler in deleteIncompleteFlights()");
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

        private bool deleteIncompleteDeliveries()
        {
            System.Threading.Tasks.Task<JArray> notProcessedDeliveries; // noch zu erstellende Deliveries aus FLS
            try
            {
                // alle Deliveries die zu verrechnen sind aus FLS herunterladen
                notProcessedDeliveries = flsClient.CallAsyncAsJArray(My.MySettingsProperty.Settings.ServiceAPIDeliveriesNotProcessedMethod);
                notProcessedDeliveries.Wait();
                Logger.GetInstance().Log(LogLevel.Info, "Allfällige Daten für noch nicht vollständig verrechnete Lieferscheine werden gelöscht.");

                // für alle DeliveryIds, die beim nächsten Mal wieder geliefert werden
                foreach (JObject delivery in notProcessedDeliveries.Result.Children())
                {
                    // von abgebrochenen Lieferscheinimporten vorhandene Daten für die MasterFlightId löschen
                    if (!pxhelper.deleteIncompleteDeliveryData(delivery["DeliveryId"].ToString()))
                    {
                        return false;
                    }
                }

                Logger.GetInstance().Log("Allfällige Daten für noch nicht importierte " + notProcessedDeliveries.Result.Children().Count().ToString() + " Deliveries wurden gelöscht");
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        // löscht alle Daten zu FlightIds die beim nächsten Mal nochmals kommen werden
        private bool deleteIncompleteFlights()
        {
            List<JObject> modifiedFlights;
            var lastChangeDate = DateTime.MinValue;
            try
            {
                Logger.GetInstance().Log(LogLevel.Info, "Allfällige Daten für noch nicht vollständig importierte Flugdaten werden gelöscht.");

                // Flüge laden, die seit letztem Flugdatenimport verändert/erstellt wurden
                modifiedFlights = importer.loadModifiedFlights();
                foreach (JObject flight in modifiedFlights)
                {

                    // Daten für diesen Flug löschen
                    if (!pxhelper.deleteIncompleteFlightData(flight["FlightId"].ToString()))
                    {
                        Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Löschen der Daten für FlightId " + flight["FlightId"].ToString());
                        return false;
                    }
                    // End If
                    // End If
                }
                // Logger.GetInstance.Log(LogLevel.Info, "flugdaten werden gelöscht")
                Logger.GetInstance().Log("Allfällige Daten für noch zu importierte " + modifiedFlights.Children().Count().ToString() + " Flüge wurden gelöscht");
                return true;
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler beim Löschen der Flugdaten seit " + importer.lastFlightImport.ToString("yyyy-MM-dd") + " " + ex.Message);
                return false;
            }
        }
    }
}