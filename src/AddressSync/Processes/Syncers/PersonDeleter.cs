using System;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using pxBook;
using SMC.Lib;

namespace FlsGliderSync
{
    public class PersonDeleter
    {
        public Action<string> Log { get; set; }    // Aktion, die ausgeführt wird, wenn in Logfeld geschrieben wird

        private ProffixHelper pxhelper;
        private FlsConnection _serviceClient;

        public PersonDeleter(FlsConnection _serviceClient, ProffixHelper pxhelper, Action<string> Log)
        {
            this._serviceClient = _serviceClient;
            this.pxhelper = pxhelper;
            this.Log = Log;
        }

        // setzt in FLS Adresse auf IsActive = false (Dieses Programm löscht keine Adressen ganz)
        public bool deleteInFLS(JObject person)
        {
            string response_FLS = string.Empty;
            person["ClubRelatedPersonDetails"]["IsActive"] = false;

            // Adresse in FLS updaten (mit Änderungen, die in Proffix gemacht wurden
            response_FLS = _serviceClient.SubmitChanges(person["PersonId"].ToString().ToLower().Trim(), person, SyncerCommitCommand.Update);
            if (response_FLS != "OK")
            {
                logComplete("Fehler beim Löschen In FLS: AdressNr: " + FlsHelper.GetValOrDef(person, "ClubRelatedPersonDetails.MemberNumber") + "Nachname: " + FlsHelper.GetValOrDef(person, "Lastname") + " Vorname: " + FlsHelper.GetValOrDef(person, "Firstname"), LogLevel.Exception, response_FLS + " " + person.ToString());
                return false;
            }

            logComplete("Gelöscht in FLS Nachname: " + FlsHelper.GetValOrDef(person, "Lastname") + " Vorname: " + FlsHelper.GetValOrDef(person, "Firstname"), LogLevel.Info);
            return true;
        }

        public bool deleteInProffix(pxKommunikation.pxAdressen address)
        {
            if (!pxhelper.SetPXAddressAsGeloescht(ProffixHelper.GetZusatzFelder(address, "Z_FLSPersonId")))
            {
                return false;
            }

            logComplete("Gelöscht in Proffix Nachname: " + address.Name + " Vorname: " + address.Vorname, LogLevel.Info);
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