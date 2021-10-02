using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using pxBook;
using SMC.Lib;

namespace FlsGliderSync
{
    public class PersonLoader
    {
        public Action<string> Log { get; set; }    // Aktion, die ausgeführt wird, wenn in Logfeld geschrieben wird

        private FlsConnection _serviceClient;

        public PersonLoader(FlsConnection _serviceclient, Action<string> Log)
        {
            _serviceClient = _serviceclient;
            this.Log = Log;
        }

        public bool datenLaden(ref List<JObject> FLSharddeletedpersons, ref List<JObject> FLSPersons, ref List<pxKommunikation.pxAdressen> PXAdressen)
        {
            try
            {
                logComplete("Adressdaten aus FLS werden geladen", LogLevel.Info);
                // alle in FLS seit sinceDate gelöschten Personen laden
                FLSharddeletedpersons = loadDeletedPersons(DateTime.MinValue);
                // Alle FLS Adressen holen egal ob IsActive true/false oder nicht
                FLSPersons = loadFLSPersons(DateTime.MinValue);
                logComplete("Adressdaten aus Proffix werden geladen", LogLevel.Info);
                // Alle  PROFFIX Adressen holen egal ob geloescht = 0/1
                PXAdressen = loadPXAdressen();

                // prüfen, ob Laden geklappt hat
                if (FLSharddeletedpersons is null | FLSPersons is null | PXAdressen is null)
                {
                    logComplete("", LogLevel.Exception);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }


        // lädt die in FLS gelöschten Personen
        private List<JObject> loadDeletedPersons(DateTime sinceDate)
        {
            try
            {
                var deletedPersons = new List<JObject>();
                System.Threading.Tasks.Task<JArray> personDeletedResult;
                personDeletedResult = _serviceClient.CallAsyncAsJArray(My.MySettingsProperty.Settings.ServiceAPIDeletedPersonFulldetailsMethod + sinceDate.ToString("yyyy-MM-dd"));
                personDeletedResult.Wait();
                foreach (JObject person in personDeletedResult.Result.Children())
                    deletedPersons.Add(person);
                return deletedPersons;
            }
            catch (Exception ex)
            {
                logComplete("Fehler beim Laden der in FLS gelöschten Adressen. " + ex.Message, LogLevel.Exception);
                return null;
            }
        }

        // gibt alle FLS Persons zurück, (egal ob IsActive true oder false)
        private List<JObject> loadFLSPersons(DateTime sinceDate)
        {
            System.Threading.Tasks.Task<JArray> personResult;
            var FLSPersons = new List<JObject>();
            try
            {
                // alle Adressen aus FLS laden (egal ob IsActive = true oder nicht)
                personResult = _serviceClient.CallAsyncAsJArray(My.MySettingsProperty.Settings.ServiceAPIModifiedPersonFullDetailsMethod + sinceDate.ToString("yyyy-MM-dd"));
                personResult.Wait();
                foreach (JObject person in personResult.Result.Children())
                    FLSPersons.Add(person);
                return FLSPersons;
            }
            catch (Exception ex)
            {
                logComplete("Fehler in " + MethodBase.GetCurrentMethod().Name + " " + ex.Message, LogLevel.Exception);
                return null;
            }
        }

        // alle Adressen aus PX laden, egal ob geloeshct = 0 oder 1
        private List<pxKommunikation.pxAdressen> loadPXAdressen()
        {
            var adressList = new List<pxKommunikation.pxAdressen>();
            var ungeloeschteAdressen = new pxKommunikation.pxAdressen[] { };
            var geloeschteAdressen = new pxKommunikation.pxAdressen[] { };
            string fehler = string.Empty;
            try
            {
                // alle ungelöschten Adressen laden
                if (!FlsGliderSync.Proffix.GoBook.GetAdresse(pxKommunikation.pxAdressSuchTyp.Alle, "%", ref ungeloeschteAdressen, ref fehler))
                {
                    if (!fehler.Contains("Keine Adressen gefunden!"))
                    {
                        logComplete("Fehler in " + MethodBase.GetCurrentMethod().Name + " Laden der ungelöschten Adressen aus PX " + fehler, LogLevel.Exception);
                        return null;
                    }
                }

                foreach (pxKommunikation.pxAdressen address in ungeloeschteAdressen)
                {
                    if (address.AdressNr != 0)
                    {
                        adressList.Add(address);
                    }
                }

                // alle gelöschten Adressen laden
                if (!FlsGliderSync.Proffix.GoBook.GetAdresse(pxKommunikation.pxAdressSuchTyp.Alle, "%", ref geloeschteAdressen, ref fehler, pxKommunikation.pxGeloeschte.Geloeschte))
                {
                    if (!fehler.Contains("Keine Adressen gefunden!"))
                    {
                        logComplete("Fehler in " + MethodBase.GetCurrentMethod().Name + " Laden der gelöschten Adressen aus PX " + fehler, LogLevel.Exception);
                        return null;
                    }
                }

                foreach (pxKommunikation.pxAdressen address in geloeschteAdressen)
                {
                    if (address.AdressNr != 0)
                    {
                        adressList.Add(address);
                    }
                }

                return adressList;
            }
            catch (Exception ex)
            {
                logComplete("Fehler in " + MethodBase.GetCurrentMethod().Name + " " + ex.Message, LogLevel.Exception);
                return null;
            }
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