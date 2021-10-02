using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json.Linq;
using pxBook;
using SMC.Lib;

namespace FlsGliderSync
{
    public class PersonCreator
    {
        public Action<string> Log { get; set; }    // Aktion, die ausgeführt wird, wenn in Logfeld geschrieben wird

        private PersonMapper personMapper = new PersonMapper();
        private ClubMapper clubMapper = new ClubMapper();
        private PersonDeleter deleter;
        private PersonUpdater updater;
        private ProffixHelper pxhelper;
        private ProffixConnection myconn;
        private FlsConnection _serviceClient;
        private DateTime lastSync;

        private ADODB.Recordset rs_adressdefault { get; set; }

        public PersonCreator(ProffixConnection myconn, ADODB.Recordset rs_adressdefault, ProffixHelper pxhelper, FlsConnection _serviceClient, DateTime lastSync, Action<string> Log, PersonUpdater updater, PersonDeleter deleter)
        {
            this.myconn = myconn;
            this.rs_adressdefault = rs_adressdefault;
            this.pxhelper = pxhelper;
            this._serviceClient = _serviceClient;
            this.lastSync = lastSync;
            this.Log = Log;
            this.updater = updater;
            this.deleter = deleter;
        }

        // *************************************************************************Nur in FLS vorhanden**********************************************************************
        // in Proffix gibt es keine Adresse (geloscht ist irrelevant) mit dieser PersonId
        public bool NurInFLS(JObject person)
        {
            string response_FLS = string.Empty;
            // wenn diese Person noch keine AdressNr hat --> noch nie synchronisiert
            if (string.IsNullOrEmpty(FlsHelper.GetValOrDef(person, "ClubRelatedPersonDetails.MemberNumber")))
            {

                // die Adresse wurde in FLS neu erstellt --> auch in PX erstellen (mit neu vergebener AdressNr) aus FLS sollen Adressen synchronisiert werden --> immer in PX erstellen und verknüpfen 
                if (!createInProffix(person, string.Empty))
                {
                    return false;
                }
            }

            // wenn diese Person bereits eine AdressNr hat --> bereits mal synchronisiert 
            // --> wurde in PX gelöscht, da nur noch in FLS vorhanden

            // PersonId existiert nur in FLS, hat aber eine MemberNr, Adresse wurde aber nicht verändert 
            // wenn in FLS NICHT seit letzter Synchronisation verändert --> nichts machen
            else if (Conversions.ToDate(FlsHelper.GetPersonChangeDate(person)) < lastSync)
            {
                Logger.GetInstance().Log(LogLevel.Info, "Info: Adresse Name: " + person["Lastname"].ToString() + " Vorname: " + person["Firstname"].ToString() + " hat die PersonId " + person["PersonId"].ToString().ToLower() + ", welche nur in FLS existiert aber eine MemberNr " + FlsHelper.GetValOrDef(person, "ClubRelatedPersonDetails.MemberNumber") + " hat. " + Constants.vbCrLf + "Die Adresse wurde seit der letzten Synchronisation nicht verändert. Mit dieser Adresse wird daher nichts gemacht.");
            }

            // ' wenn nicht bereits auf gelöscht gesetzt --> in FLS löschen
            // If GetValOrDef(person, "ClubRelatedPersonDetails.IsActive").ToLower <> "false" Then

            // ' Postfix an MemberNr anhängen
            // FlsHelper.SetPostfixToMemberNr(person)
            // ' in FLS Person löschen (IsActive = false)
            // If Not deleter.deleteInFLS(person) Then
            // Return False
            // End If
            // End If

            // in FLS nach letzter Synchronisation nochmals verändert --> in PX wieder erstellen
            else
            {
                // ist in PX gelöscht worden, aber nach lastSync in FLS noch verändert --> versuchen wiederzuerstellen in PX mit der alten AdressNr
                FlsHelper.RemovePostfixFromMemberNr(ref person);

                // prüfen, ob bereits vorhanden aber als nicht zu synchronisieren --> wenn ja wird gleich nur synchronisieren = 1 gesetzt
                if (pxhelper.DoesAddressExistsAsNichtZuSynchronisierend(person["PersonId"].ToString().ToLower().Trim()))
                {
                    if (!pxhelper.SetAsZuSynchroniseren(person["PersonId"].ToString().ToLower().Trim()))
                    {
                        logComplete("Die Adresse mit der PersonId " + person["PersonId"].ToString().ToLower().Trim() + "wurde in Proffix als zu synchronisieren gesetzt, da sie in FLS nach der letzten Synchronisation noch verändert wurde", LogLevel.Info);
                        return false;
                    }
                }

                // auch nicht als NichtZuSynchroniserend vorhanden --> neu erstellen
                else if (!createInProffix(person, FlsHelper.GetValOrDef(person, "ClubRelatedPersonDetails.MemberNumber")))
                {
                    // nachfragen, ob für neue Adressnr erstellen
                    if (!(DialogResult.OK == MessageBox.Show("Soll die Adresse für eine neue Adress-Nr. in Proffix erstellt werden?", "Adress-Nr. bereits vorhanden", MessageBoxButtons.OKCancel, MessageBoxIcon.Information)))
                    {
                        return false;
                    }

                    // wenn ok --> für neue AdressNr erstellen
                    if (!createInProffix(person, ""))
                    {
                        return false;
                    }
                }

                // in person wurde bei MemberNr Postfix entfernt --> an FLS senden
                response_FLS = _serviceClient.SubmitChanges(person["PersonId"].ToString().ToLower().Trim(), person, SyncerCommitCommand.Update);
                if (response_FLS != "OK")
                {
                    logComplete("Fehler beim Update von MemberNr ohne postfix. " + person.ToString(), LogLevel.Exception);
                    return false;
                }
            }

            // wenn bis hierher gekommen --> geklappt, wie gewünscht
            return true;
        }
        // ************************************************************************Nur in PX vorhanden************************************************************************
        // In FLS gibt es keine ungelöschte Adresse (IsActive ist irrelevant) mit dieser PersonId
        public bool NurInPX(pxKommunikation.pxAdressen address, List<JObject> flshardDeletedPersons)
        {
            var DeletedOn = DateTime.MinValue;
            var addressChangeDate = ProffixHelper.GetAddressChangeDate(address);

            // wenn diese Adresse noch keine PersonId hat --> noch nie synchronisiert
            if (string.IsNullOrEmpty(ProffixHelper.GetZusatzFelder(address, "Z_FLSPersonId")))
            {
                if (!createInFLS(address))
                {
                    return false;
                }
            }

            // wenn diese Adresse bereits eine PersonId hat --> bereits mal synchronisiert --> ist in FLS nicht mehr verfügbar, da hart gelöscht 
            else
            {
                // wenn Adresse nicht unter den hartgelöschten --> Fehler
                // die Adresse unter den gelöschten suchen
                foreach (JObject person in flshardDeletedPersons)
                {
                    if ((person["PersonId"].ToString().ToLower().Trim() ?? "") == (ProffixHelper.GetZusatzFelder(address, "Z_FLSPersonId") ?? ""))
                    {
                        DeletedOn = DateTime.Parse(FlsHelper.GetValOrDef(person, "DeletedOn"));
                    }
                }
                // unter den gelöschten in FLS wurde keine Person mit der entsprechenden PersonId gefunden, aber in PX mit PersonId vorhanden --> Fehler
                if (DeletedOn == DateTime.MinValue)
                {
                    logComplete("Fehler! Die Adresse mit der AdressNr " + address.AdressNr.ToString() + " ist in Proffix bereits mit einer PersonId vorhanden aber in FLS nicht unter den gelöschten.", LogLevel.Exception);
                    return false;
                }

                // unter den gelöschten wurde die Person in FLS gefunden
                // mögliche Reihenfolgen:
                // lastSync --> PX Veränderung --> deletedon oder PX Veränderung --> lastSync --> deletedon
                // --> in PX löschen (geloescht = 1)
                // lastSync --> deletedOn --> PX Veränderung oder deletedOn --> lastSync --> PX Veränderung
                // in FLS neu erstellen (konnte gelöscht werden --> noch keine Verknüpfungen --> neue PersonId egal)
                // deletedon --> PX Veränderung --> lastSync oder PX Veränderung --> deletedOn --> lastSync
                // nichts machen

                // wenn als letztes in FLS gelöscht wurde (egal ob in px vor oder nach lastSync verändert)
                else if (DeletedOn > lastSync & DeletedOn > addressChangeDate)
                {
                    // --> in PX auch löschen
                    if (!deleter.deleteInProffix(address))
                    {
                        return false;
                    }
                }
                else if (addressChangeDate > lastSync & addressChangeDate > DeletedOn)
                {
                    // wenn als letztes in PX verändert wurde (egal ob in fls vor oder nach lastSync verändert)
                    // --> in FLS neu erstellen (konnte in FLS hart gelöscht werden --> ist noch nicht mit Flug... verbunden --> egal wenn neue PersonId
                    if (!createInFLS(address))
                    {
                        return false;
                    }
                    // logComplete("Hinweis: Adresse wurde in FLS gelöscht und aber danach in Proffix noch verändert AdressNr: " + address.AdressNr.ToString, LogLevel.Exception)

                }
            }

            return true;
        }




        // erstellt Adresse in Proffix (Adressnr = "" --> ganz neue Adresse wird erstellt, AdressNr = Wert --> wurde früher bereits mal erstellt aber gelöscht --> mit gleicher AdressNr versuchen zu erstellen)
        public bool createInProffix(JObject person, string bisherigeAdressNr)
        {
            string fehler = string.Empty;
            string response_FLS = string.Empty;
            object add = new pxKommunikation.pxAdressen();
            if (FlsGliderSync.logAusfuehrlich)
            {
                Logger.GetInstance().Log(MethodBase.GetCurrentMethod().Name + person.ToString());
            }

            // Werte aus person in entsprechende Felder eines pxAdressen-Objektes (add) füllen
            add = personMapper.DeMapp(add, person);
            add = clubMapper.DeMapp((pxKommunikation.pxAdressen)add, person);

            // von Datentyp Objekt in pxAdresse umwandeln
            pxKommunikation.pxAdressen adresse = (pxKommunikation.pxAdressen)add;

            // falls Adresse keinen Ort/PLZ enthält --> Defaultwerte für Proffix setzen
            adresse = ProffixHelper.SetAdressDefault(adresse, rs_adressdefault);

            // wenn eine zu verwendende AdressNr mitgegeben wird (Adresse wurde bereits mal synchronisiert mit FLS) 
            if ((bisherigeAdressNr ?? "") != (string.Empty ?? ""))
            {
                // bisherige AdressNr verwenden (um zu verhindern, dass gleiche Personen mit neuer AdressNr wiedererstellt werden)
                adresse.AdressNr = Conversions.ToInteger(bisherigeAdressNr);
            }
            // wenn keine zu verwendende AdressNr mitgegeben wird (Adresse hat noch nie bestanden)
            else
            {
                // die bisherige Adresse hatte noch keine AdressNr --> nächste aus PX holen
                adresse.AdressNr = FlsGliderSync.Proffix.GoBook.GetAdresseNr(fehler);
            }

            // Adresse in Proffix
            if (!FlsGliderSync.Proffix.GoBook.AddAdresse(adresse, ref fehler, true, ProffixHelper.CreateZusatzFelderSql(adresse)))
            {
                logComplete("Fehler beim Erstellen in Proffix für Nachname:" + FlsHelper.GetValOrDef(person, "Lastname") + " Vorname: " + FlsHelper.GetValOrDef(person, "Firstname") + ".", LogLevel.Exception);
                // es wurde eine Adresse versucht zu erstellen, dessen AdressNr bereits vorhanden ist
                if (fehler == "Adresse ist bereits vorhanden")
                {
                    logComplete("Es existiert bereits eine Adresse mit der AdressNr " + adresse.AdressNr.ToString(), LogLevel.Exception);
                }
                else
                {
                    logComplete(fehler, LogLevel.Exception, person.ToString());
                }

                return false;
            }

            // Zusatzfelder mit Datum nachträglich hinzufügen, da über Gobook.AddAdresse nicht funktioniert
            if (!pxhelper.SetDatumsZusatzfelderToPXAdresse(adresse, person, ref fehler))
            {
                logComplete("Fehler beim Hinzufügen der Datums-Zusatzfelder AdressNr: " + adresse.AdressNr.ToString() + " " + fehler, LogLevel.Exception);
                return false;
            }

            if (!pxhelper.SetGeloeschtInPXAdresseDependingOnIsActive(person))
            {
                logComplete("Fehler in SetGeloeschtInPXAdresse()", LogLevel.Exception);
                return false;
            }

            // wenn bisher noch keine AdressNr geben war (= wurde in FLS seit lastSync neu erstellt)
            if (string.IsNullOrEmpty(bisherigeAdressNr))
            {
                // neue AdressNr in FLS-Adresse speichern und an FLS übergeben
                person["ClubRelatedPersonDetails"]["MemberNumber"] = adresse.AdressNr.ToString();
                response_FLS = _serviceClient.SubmitChanges(person["PersonId"].ToString().ToLower().Trim(), person, SyncerCommitCommand.Update);
                if (response_FLS != "OK")
                {
                    logComplete("Fehler beim updaten der AdressNr " + adresse.AdressNr.ToString() + " in FLS der soeben in Proffix erstellten Adresse. Name: " + adresse.Name, LogLevel.Exception, response_FLS);
                    return false;
                }
            }

            // Adresse in PROFFIX erstellt
            logComplete("Erstellt in Proffix. AdressNr: " + adresse.AdressNr.ToString() + " Nachname: " + adresse.Name + " Vorname: " + (adresse.Vorname is object ? adresse.Vorname : ""), LogLevel.Info);
            return true;
        }

        // erstellt Adresse in FLS
        private bool createInFLS(pxKommunikation.pxAdressen address)
        {
            var pers = new JObject();                                // Für Werte, die direkt in JSON auf der vordersten Ebene eingefüllt werden können
            var clubpers = new JObject();                             // JObject, indem alle ClubRel. Werte gespeichert werden --> als weiteres Objekt zu pers hinzufügen
            string newPersonId = string.Empty;
            string fehler = string.Empty;
            string response_FLS = string.Empty;

            // pxAdressenobjekt in ein neues JObject pers einfügen
            pers = personMapper.Mapp(address, pers);
            // eigenes JObject mit clubrel Daten erstellen
            clubpers = clubMapper.Mapp(address, clubpers);
            // clubpers in pers einfügen
            pers = clubMapper.completePersWithclubPers(pers, ref clubpers);
            if (!FlsHelper.validatePerson(pers, pxhelper, ref fehler))
            {
                logComplete("Fehler beim Validieren der Person. AdressNr: " + address.AdressNr + " Nachname: " + address.Name + " Vorname: " + address.Vorname + " " + fehler, LogLevel.Exception);
                return false;
            }

            if (FlsGliderSync.logAusfuehrlich)
            {
                Logger.GetInstance().Log(MethodBase.GetCurrentMethod().Name + pers.ToString());
            }

            if (!pxhelper.SetIsActiveInFLSPersonDependingOnGeloescht(ref pers, address.AdressNr.ToString()))
            {
                logComplete("Fehler in SetIsActiveInFLSPerson()", LogLevel.Exception);
                return false;
            }

            // Sicherstellen, dass keine MenberNumber mitgegeben wird (wenn in FLS gelöscht wurde + in PX noch verändert --> wird in FLS wieder erstellt, aber knallt, da MemberNumber schon mal vergeben war)
            pers["ClubRelatedPersonDetails"]["MemberNumber"] = null;
            if (FlsGliderSync.logAusfuehrlich)
            {
                Logger.GetInstance().Log(LogLevel.Info, "JSON um Adresse in FLS zu erstellen: " + pers.ToString());
            }

            // neue Adresse in FLS schreiben und neu erstellte PersonId auslesen
            // response_FLS enthält bei Erfolg newPersonId (da create) und bei Misserfolg Fehlermeldung
            response_FLS = _serviceClient.SubmitChanges("", pers, SyncerCommitCommand.Create);

            // Ist response_FLS GUID? --> create hat geklappt, ansonsten enthält response_FLS die Fehlermeldung
            if (!GeneralHelper.isGUID(response_FLS))
            {
                logComplete("Fehler beim Erstellen in FLS AdressNr: " + address.AdressNr.ToString() + " Nachname: " + address.Name + " Vorname: " + (address.Vorname is object ? address.Vorname : ""), LogLevel.Exception, response_FLS + " " + pers.ToString());
                return false;
            }
            else
            {
                // neuerstellte PersonId in pxAdresse --> Proffix schreiben
                address = ProffixHelper.SetZusatzFelder(address, "Z_FLSPersonId", "Z_FLSPersonId", "", "", response_FLS);
                FlsGliderSync.Proffix.GoBook.AddAdresse(address, ref fehler, false, ProffixHelper.CreateZusatzFelderSql(address));
                if (!string.IsNullOrEmpty(fehler))
                {
                    logComplete("Fehler beim updaten der FLSPersonId in Proffix der in FLS soeben erstellten Adresse. AdressNr: " + address.AdressNr.ToString() + " Nachname: " + address.Name + " Vorname: " + (address.Vorname is object ? address.Vorname : "") + " " + fehler, LogLevel.Exception);
                    return false;
                }
            }

            // Adresse im FLS erstellen
            logComplete("Erstellt in FLS. AdressNr: " + address.AdressNr.ToString() + " Nachname: " + address.Name + " Vorname: " + (address.Vorname is object ? address.Vorname : ""), LogLevel.Info);
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