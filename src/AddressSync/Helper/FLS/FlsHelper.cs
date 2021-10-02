using System;
using System.Reflection;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json.Linq;

namespace FlsGliderSync
{

    /// <summary>
/// Eine Helper Klasse für aktionen die mit dem FLS Service zu tun haben
/// </summary>
/// <remarks></remarks>
    static class FlsHelper
    {

        /// <summary>
    /// Gibt das Änderungsdatum der Adresse zurück
    /// </summary>
    /// <param name="person">Die Adresse als JSON Object</param>
    /// <returns>Das Änderungsdatum</returns>
    /// <remarks></remarks>
        public static DateTime GetPersonChangeDate(JObject person)
        {
            DateTime newestDate;
            newestDate = GeneralHelper.GetNewestDate(new[] { DateTime.Parse(person["CreatedOn"].ToString()), person["ModifiedOn"] is object ? DateTime.Parse(person["ModifiedOn"].ToString()) : DateTime.MinValue, person["ClubRelatedPersonDetails"]["CreatedOn"] is object ? DateTime.Parse(person["ClubRelatedPersonDetails"]["CreatedOn"].ToString()) : DateTime.MinValue, person["ClubRelatedPersonDetails"]["ModifiedOn"] is object ? DateTime.Parse(person["ClubRelatedPersonDetails"]["ModifiedOn"].ToString()) : DateTime.MinValue });

            // FLS hat UTC Zeit, Proffix hat lokale Zeit --> in lokale Zeit umwandeln (Hinweis: UTC-Zeitformat = Datetime.Tostring("o") anstatt "yyyy-MM...
            newestDate = newestDate.ToLocalTime();
            return newestDate;
        }


        /// <summary>
    /// Gibt das Änderungsdatum des flightszurück
    /// </summary>
    /// <param name="flight">Der Flug als JSON Object</param>
    /// <returns>Das Änderungsdatum</returns>
    /// <remarks></remarks>
        public static DateTime GetFlightChangeDate(JObject flight)
        {
            DateTime newestDate;
            newestDate = GeneralHelper.GetNewestDate(new[] { DateTime.Parse(flight["CreatedOn"].ToString()), flight["ModifiedOn"] is object ? DateTime.Parse(flight["ModifiedOn"].ToString()) : DateTime.MinValue });

            // FLS hat UTC Zeit, Proffix hat lokale Zeit --> in lokale Zeit umwandeln (Hinweis: UTC-Zeitformat = Datetime.Tostring("o") anstatt "yyyy-MM...
            newestDate = newestDate.ToLocalTime();
            return newestDate;
        }

        // da Adresse mit fulldetails geholt wird, sollten die Metadaten für update in FLS aus JSON entfernt werden
        public static object removeMetadata(object target)
        {
            ((JObject)target).Remove("CreatedOn");
            // CType(target, JObject).Remove("CreatedByUserId")
            // CType(target, JObject).Remove("RecordState")
            ((JObject)target).Remove("ModifiedOn");
            // CType(target, JObject).Remove("ModifiedByUserId")
            ((JObject)target).Remove("OwnerId");
            ((JObject)target).Remove("OwnershipType");
            return target;
        }

        // prüft einige Felder auf richtigen Inhalt
        public static bool validatePerson(JObject person, ProffixHelper proffixhelper, ref string fehler)
        {
            var pxhelper = new ProffixHelper();
            pxhelper = proffixhelper;

            // ist PrivatEmail richtig angegeben?
            if (!string.IsNullOrEmpty(GetValOrDef(person, "PrivateEmail").ToString()))
            {
                if (!GeneralHelper.isEmail(person["PrivateEmail"].ToString()))
                {
                    fehler = "Kontrollieren Sie die private Emailadresse in Proffix. Der Inhalt entspricht nicht dem Muster einer E-Mailadresse.";
                    return false;
                }
            }

            // ist GeschäftsEmail richtig angegeben?
            if (!string.IsNullOrEmpty(GetValOrDef(person, "BusinessEmail").ToString()))
            {
                if (!GeneralHelper.isEmail(person["BusinessEmail"].ToString()))
                {
                    fehler = "Kontrollieren Sie die Geschäfts-Emailadresse in Proffix. Der Inhalt entspricht nicht dem Muster einer E-Mailadresse.";
                    return false;
                }
            }

            // ist MemberStateId eine gültige Id (muss in ZUS_FLSMemberStates vorhanden sein)
            if (!string.IsNullOrEmpty(GetValOrDef(person, "ClubRelatedPersonDetails.MemberStateId").ToString()))
            {
                if (!proffixhelper.isValidMemberStateId(person["ClubRelatedPersonDetails"]["MemberStateId"].ToString(), ref fehler))
                {
                    return false;
                }
            }

            return true;
        }


        /// <summary>
    /// Gibt den Wert des JObject Felds als String zurück oder ein leeren String falls das Feld nicht existiert
    /// </summary>
    /// <param name="obj">Das JSON Object</param>
    /// <param name="name">Der Name des Felds</param>
    /// <returns>Den Wert des Felds als String oder ein leeren Text</returns>
        public static string GetValOrDef(JObject obj, string name)
        {
            JToken jObj = obj;
            var values = name.Split('.');
            int index = 0;
            foreach (string nameValue in values)
            {

                // wenn es das letzte Wort ist
                if (index == values.Length - 1)
                {

                    // Lesen des Feldes
                    var value = jObj[nameValue];

                    // Prüfen ob das Feld einen Wert entählt und wenn ja den Wert zurückgeben
                    if (value is object)
                    {
                        return value.ToString();
                    }

                    // Das Feld existiert nicht oder ist leer
                    return string.Empty;
                }

                // Ende erreicht
                else
                {
                    // geht in das UnterJObject
                    jObj = jObj[nameValue];
                    if (jObj is null)
                        break;
                }

                index += 1;
            }

            return string.Empty;
        }

        public static string GetValOrDefBoolean(JObject obj, string name)
        {
            return GetValOrNULL(obj, name, "boolean");
        }

        public static string GetValOrDefString(JObject obj, string name)
        {
            return GetValOrNULL(obj, name, "string");
        }

        public static string GetValOrDefInteger(JObject obj, string name)
        {
            return GetValOrNULL(obj, name, "integer");
        }

        public static string GetValOrDefDateTime(JObject obj, string name, string dateFormat)
        {
            return GetValOrNULL(obj, name, "datetime", dateFormat);
        }

        public static string GetValOrDefDate(JObject obj, string name, string dateformat)
        {
            return GetValOrNULL(obj, name, "date", dateformat);
        }

        private static string GetValOrNULL(JObject obj, string name, string type, string dateformat = null)
        {
            JToken jObj = obj;
            var values = name.Split('.');
            int index = 0;
            foreach (string nameValue in values)
            {

                // wenn es das letzte Wort ist
                if (index == values.Length - 1)
                {

                    // Lesen des Feldes
                    var value = jObj[nameValue];

                    // Prüfen ob das Feld einen Wert entählt und wenn ja den Wert zurückgeben
                    if (value is object)
                    {

                        // wenn es boolean ist
                        switch (type ?? "")
                        {
                            case "string":
                                {
                                    return "'" + FormatTextForSQL(value.ToString()) + "'";
                                }

                            case "integer":
                                {
                                    return value.ToString();
                                }

                            case "datetime":
                                {
                                    return "'" + DateTime.Parse(value.ToString()).ToLocalTime().ToString(dateformat + " " + "HH:mm:ss:fff") + "'";
                                }

                            case "date":
                                {
                                    return "'" + DateTime.Parse(value.ToString()).ToString(dateformat) + "'";
                                }

                            case "boolean":
                                {
                                    if (value.ToString().ToLower() == "true")
                                    {
                                        return "1";
                                    }
                                    else if (value.ToString().ToLower() == "false")
                                    {
                                        return "0";
                                    }
                                    else
                                    {
                                        throw new Exception("Fehler in " + MethodBase.GetCurrentMethod().Name + " nicht true oder false property: " + name + "value: " + value.ToString() + "Obj: " + obj.ToString());
                                    }

                                    break;
                                }

                            default:
                                {
                                    return "NULL";
                                }
                        }
                    }

                    // Das Feld existiert nicht oder ist leer
                    return "NULL";
                }

                // Ende erreicht
                else
                {
                    // geht in das UnterJObject
                    jObj = jObj[nameValue];
                    if (jObj is null)
                        break;
                }

                index += 1;
            }

            return "NULL";
        }

        // passt SQL string an (maskiert ' und " in Text, 
        public static string FormatTextForSQL(string str)
        {
            string FormatTextForSQLRet = default;
            string sLeft;
            string sRight;
            sLeft = Constants.vbNullString;
            sRight = str;
            while (!(Strings.Len(sRight) == 0 | Strings.InStr(1, sRight, "|") == 0))
            {
                sLeft = sLeft + Strings.Left(sRight, Strings.InStr(1, sRight, "|") - 1); // & "|"
                sRight = Strings.Right(sRight, Strings.Len(sRight) - Strings.InStr(1, sRight, "|"));
            }

            str = sLeft + sRight;
            sLeft = Constants.vbNullString;
            sRight = str;
            while (!(Strings.Len(sRight) == 0 | Strings.InStr(1, sRight, "\"") == 0))
            {
                sLeft = sLeft + Strings.Left(sRight, Strings.InStr(1, sRight, "\"")) + "\"";
                sRight = Strings.Right(sRight, Strings.Len(sRight) - Strings.InStr(1, sRight, "\""));
            }

            str = sLeft + sRight;
            sLeft = Constants.vbNullString;
            sRight = str;
            while (!(Strings.Len(sRight) == 0 | Strings.InStr(1, sRight, "'") == 0))
            {
                sLeft = sLeft + Strings.Left(sRight, Strings.InStr(1, sRight, "'")) + "'";
                sRight = Strings.Right(sRight, Strings.Len(sRight) - Strings.InStr(1, sRight, "'"));
            }

            str = sLeft + sRight;
            FormatTextForSQLRet = str;
            return FormatTextForSQLRet;
        }

        // fügt der MemberNr den Postfix hinzu (dass Adresse in PX nicht mehr existiert, da ganz gelöscht)
        public static void SetPostfixToMemberNr(ref JObject person)
        {
            // wenn nicht bereits postfix angehängt
            if (!GetValOrDef(person, "ClubRelatedPersonDetails.MemberNumber").Contains(FlsGliderSync.postfix))
            {
                // postfix anhängen
                person["ClubRelatedPersonDetails"]["MemberNumber"] = GetValOrDef(person, "ClubRelatedPersonDetails.MemberNumber") + FlsGliderSync.postfix;
            }
        }

        // entfernt den Postfix der MemberNr
        public static void RemovePostfixFromMemberNr(ref JObject person)
        {
            // enthält am Schluss nur noch MemberNr
            string memberNr = string.Empty;
            // jedes Zeichen des Strings durchgehen
            foreach (var zeichen in GetValOrDef(person, "ClubRelatedPersonDetails.MemberNumber"))
            {
                // wenn es eine Zahl ist
                if (Information.IsNumeric(zeichen))
                {
                    // an MemberNr anhängen (nicht, wenn es Buchstabe ist)
                    memberNr += Conversions.ToString(zeichen);
                }
            }

            person["ClubRelatedPersonDetails"]["MemberNumber"] = memberNr;
        }
    }
}