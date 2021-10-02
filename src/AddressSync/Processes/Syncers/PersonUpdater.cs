using System;
using System.Reflection;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json.Linq;
using pxBook;
using SMC.Lib;

namespace FlsGliderSync
{
    public class PersonUpdater
    {
        public Action<string> Log { get; set; }    // Aktion, die ausgeführt wird, wenn in Logfeld geschrieben wird

        private PersonMapper personMapper = new PersonMapper();
        private ClubMapper clubMapper = new ClubMapper();
        private ProffixHelper pxhelper;
        private ProffixConnection myconn;
        private FlsConnection _serviceClient;

        private ADODB.Recordset rs_adressdefault { get; set; }

        public PersonUpdater(ProffixConnection myconn, ADODB.Recordset rs_adressdefault, ProffixHelper pxhelper, FlsConnection _serviceClient, Action<string> Log)
        {
            this.myconn = myconn;
            this.rs_adressdefault = rs_adressdefault;
            this.pxhelper = pxhelper;
            this._serviceClient = _serviceClient;
            this.Log = Log;
        }

        // updates the address
        public bool updateAccordingMaster(JObject person, pxKommunikation.pxAdressen address, UseAsMaster master)
        {
            if (FlsGliderSync.logAusfuehrlich)
            {
                Logger.GetInstance().Log(LogLevel.Info, "In beiden vorhanden PersonId: " + person["PersonId"].ToString().ToLower().Trim() + " AdressNr: " + address.AdressNr.ToString());
            }


            // ***************************************************************************************UPDATE IN PROFFIX***********************************************************
            if (master == UseAsMaster.fls)
            {

                // Die Adresse wird in PROFFIX aktualisiert
                if (!updateInProffix(person, address, rs_adressdefault))
                {
                    return false;
                }
            }

            // ******************************************************************************UPDATE IN FLS******************************************************************
            else if (master == UseAsMaster.proffix)
            {

                // wenn die Adresse synchronisiert werden soll
                if (Conversions.ToInteger(ProffixHelper.GetZusatzFelder(address, "Z_Synchronisieren")) == 1)
                {

                    // in FLS updaten
                    if (!updateInFLS(person, address))
                    {
                        return false;
                    }
                }
            }

            // keine DB wurde als Master definiert --> das hier ist für diesen Fall die falsche Funktion
            else
            {
                Logger.GetInstance().Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + " master hat nicht den Wert fls oder proffix, sondern " + master.ToString());
                return false;
            }

            return true;
        }

        public bool updateAccordingDate(JObject person, pxKommunikation.pxAdressen address, DateTime lastSync)
        {
            // Flag auf Default false setzen
            bool newerInFLS = false;
            bool newerInProffix = false;
            DateTime adressChangeDate;
            var personChangeDate = Conversions.ToDate(FlsHelper.GetPersonChangeDate(person));
            if (FlsGliderSync.logAusfuehrlich)
            {
                Logger.GetInstance().Log(LogLevel.Info, "In beiden vorhanden PersonId: " + person["PersonId"].ToString().ToLower().Trim() + " AdressNr: " + address.AdressNr.ToString());
            }

            // Änderungsdatum auslesen
            adressChangeDate = ProffixHelper.GetAddressChangeDate(address);

            // wenn beide nach LastSync verändert wurden --> welche ist neuer?
            if (personChangeDate > lastSync & adressChangeDate > lastSync)
            {
                if (personChangeDate > adressChangeDate)
                {
                    newerInFLS = true;
                }
                else if (adressChangeDate > personChangeDate)
                {
                    newerInProffix = true;
                }
            }

            // ***************************************************************************************UPDATE IN PROFFIX***********************************************************
            // Adresse an beiden Orten vorhanden, nur in FLS nach LastSync verändert oder in FLS neuere Änderung
            // --> Änderungen in Proffix updaten
            if (personChangeDate > lastSync & adressChangeDate < lastSync | newerInFLS)
            {

                // Die Adresse wird in PROFFIX aktualisiert
                if (!updateInProffix(person, address, rs_adressdefault))
                {
                    return false;
                }
            }


            // ******************************************************************************UPDATE IN FLS******************************************************************
            // Adresse an beiden vorhanden, nur in Proffix nach LastSync verändert, oder in Proffix neuere Änderung
            // --> Änderungen in FLS updaten
            else if (adressChangeDate > lastSync & personChangeDate < lastSync | newerInProffix)
            {

                // wenn die Adresse synchronisiert werden soll
                if (Conversions.ToInteger(ProffixHelper.GetZusatzFelder(address, "Z_Synchronisieren")) == 1)
                {

                    // in FLS updaten
                    if (!updateInFLS(person, address))
                    {
                        return false;
                    }
                }
            }

            return true;
        }


        // updated Adressen, die in beiden vorhanden, aber in FLS neuer sind, in Proffix
        public bool updateInProffix(JObject person, pxKommunikation.pxAdressen address, ADODB.Recordset rs_adressdefault)
        {
            string fehler = string.Empty;
            bool successful = true;
            if (FlsGliderSync.logAusfuehrlich)
            {
                Logger.GetInstance().Log(LogLevel.Info, "Adresse zum updaten: " + person.ToString());
            }

            // FLS-Daten in "veraltetes" pxAdressen übertragen
            address = (pxKommunikation.pxAdressen)personMapper.DeMapp(address, person);
            address = clubMapper.DeMapp(address, person);

            // Proffix braucht PLZ+Ort --> wenn nicht angegeben --> Defaultwerte setzen
            address = ProffixHelper.SetAdressDefault(address, rs_adressdefault);

            // Speichern der veränderten Adresse in Proffix
            if (!FlsGliderSync.Proffix.GoBook.AddAdresse(address, ref fehler, false, ProffixHelper.CreateZusatzFelderSql(address)))
            {
                logComplete("Fehler beim Updaten in Proffix. AdressNr: " + address.AdressNr.ToString() + " Nachname: " + address.Name + " Vorname: " + (address.Vorname is object ? address.Vorname : "") + " " + fehler, LogLevel.Exception);
                return false;
            }

            // Zusatzfelder mit Datum über Gobook.AddAdresse hinzuzufügen, funktioniert nicht --> nachträglich mit ADODB
            if (!pxhelper.SetDatumsZusatzfelderToPXAdresse(address, person, ref fehler))
            {
                logComplete("Fehler beim Hinzufügen der Datums-Zusatzfelder in ProffixAdressNr: " + address.AdressNr.ToString() + " " + fehler, LogLevel.Exception);
                return false;
            }

            // IsActive/Geloescht synchronisieren
            if (!pxhelper.SetGeloeschtInPXAdresseDependingOnIsActive(person))
            {
                logComplete("Fehler beim Updaten des Geloescht Feldes in Proffix. PersonId: " + person["PersonId"].ToString().ToLower().Trim(), LogLevel.Exception);
            }

            logComplete("Aktualisiert in Proffix: AdressNr: " + address.AdressNr.ToString() + " Nachname: " + address.Name + " Vorname: " + (address.Vorname is object ? address.Vorname : ""), LogLevel.Info);
            return true;
        }


        // updated Adressen, die in beiden vorhanden, aber in Proffix neuer sind, in FLS
        // wird auch verwendet, wenn bei Flugdatenimport Adresse mittels AdressNr nicht gefunden wird --> AdressNr aus Proffix in FLS updaten
        public bool updateInFLS(JObject person, pxKommunikation.pxAdressen address)
        {
            string response_FLS = string.Empty;
            string fehler = string.Empty;

            // bisherige Person zwischenspeichern
            string personVorUpdate = person.ToString();

            // Proffix-Daten in "veraltetes" JObject übertragen
            person = personMapper.Mapp(address, person);

            // ' clubRel Werte updaten
            person = clubMapper.Mapp(address, person);

            // möglicherweise falsche MemberNr/AdressNr in FLS wieder synchronisieren
            person["ClubRelatedPersonDetails"]["MemberNumber"] = address.AdressNr;

            // testen, ob sich Person verändert hat 
            // --> wenn ja --> in FLS updaten, wenn nein --> es hat sich nichts verändert 
            // --> nicht updaten
            if (person.ToString().CompareTo(personVorUpdate) != 0)
            {

                // Metadaten aus JSON entfernen
                FlsHelper.removeMetadata(person);

                // prüft, ob Emails eine Email enthalten bzw. GUIDs GUIDs sind
                if (!FlsHelper.validatePerson(person, pxhelper, ref fehler))
                {
                    logComplete("Fehler beim Validieren der Adresse. AdressNr: " + address.AdressNr + " Nachname: " + address.Name + " Vorname: " + address.Vorname + " " + fehler, LogLevel.Exception);
                    return false;
                }

                if (FlsGliderSync.logAusfuehrlich)
                {
                    Logger.GetInstance().Log(MethodBase.GetCurrentMethod().Name + person.ToString());
                }

                // Geloescht/IsActive in FLS updaten
                if (!pxhelper.SetIsActiveInFLSPersonDependingOnGeloescht(ref person, address.AdressNr.ToString()))
                {
                    logComplete("Fehler beim Updaten des Geloescht Feldes in Proffix. PersonId: " + person["PersonId"].ToString().ToLower().Trim(), LogLevel.Exception);
                    return false;
                }

                // If logAusfuehrlich Then
                // Logger.GetInstance.Log(LogLevel.Info, "JSON um Adresse in FLS zu aktualisieren: " + person.ToString)
                // End If

                // Adresse in FLS updaten (mit Änderungen, die in Proffix gemacht wurden
                response_FLS = _serviceClient.SubmitChanges(person["PersonId"].ToString().ToLower().Trim(), person, SyncerCommitCommand.Update);
                if (response_FLS != "OK")
                {
                    logComplete("Fehler beim Updaten In FLS: AdressNr: " + address.AdressNr.ToString() + "Nachname: " + address.Name + " Vorname: " + (address.Vorname is object ? address.Vorname : "") + address.Name, LogLevel.Exception, response_FLS + " " + person.ToString());
                    return false;
                }

                logComplete("Aktualisiert in FLS: AdressNr: " + address.AdressNr.ToString() + " Nachname: " + address.Name + " Vorname: " + FlsHelper.GetValOrDef(person, "Firstname"), LogLevel.Info);
            }

            return true;
        }


        // schreibt in Log und in Logger (File)
        private void logComplete(string logString, LogLevel loglevel, string zusatzloggerString = "")
        {
            if (Log is object)
                Log.Invoke((loglevel != LogLevel.Info ? Constants.vbTab : "") + logString);
            Logger.GetInstance().Log(loglevel, logString + " " + zusatzloggerString);
        }
    }
}