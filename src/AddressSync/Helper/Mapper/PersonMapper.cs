using System;
using System.Collections.Generic;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json.Linq;
using pxBook;

namespace FlsGliderSync
{
    public class PersonMapper : Mapper<object, JObject>
    {
        /// <summary>
    /// Die Parse Funktion von String zu DateTime
    /// </summary>
        public static Func<string, DateTime> ToDateTime
        {
            get
            {
                return new Func<string, DateTime>(value => string.IsNullOrEmpty(value) | (value ?? "") == (string.Empty ?? "") ? default : DateTime.Parse(value));
            }
        }

        /// <summary>
    /// Die Parse Funktion von DateTime zu String
    /// </summary>
        public static Func<DateTime, string> FromDateTime
        {
            get
            {
                return new Func<DateTime, string>(value => value == default ? null : value.ToString());
            }
        }


        // ************************************************************************************MappingProperties, welches FLS-Feld entspricht welchem Proffixfeld und wie muss geholt/eingefügt werden*******************************
        /// <summary>
    /// Die Mappingproperties für direkte Eigenschaften (bis MobilePhoneNumber) bzw. FLS direkt und Proffix Zusatzfeld (ab HasGliderInstructorLicence)
    /// Die Mappingproperties für die ClubRelated Eigenschaften sind in ClubMapper definiert
    /// </summary>
        public new static List<IMappingProperty> MappingProperies
        {
            get
            {
                return new List<IMappingProperty>(){ new MappingProperty("AddressLine1", "Strasse"), new MappingProperty("AddressLine2", "Adresszeile1"), new MappingProperty("BusinessPhoneNumber", "TelZentrale"), new MappingProperty("City", "Ort"), new MappingProperty("FaxNumber", "Fax"), new MappingProperty("Firstname", "Vorname"), new MappingProperty("Lastname", "Name"), new MappingProperty("PrivateEmail", "EMail"), new MappingProperty("PrivatePhoneNumber", "TelPrivat"), new MappingProperty("Region", "Region"), new MappingProperty("ZipCode", "Plz"), new MappingProperty("MobilePhoneNumber", "Natel"), new MappingProperty("CountryId", "Land"), new MappProperty<DateTime, string>("Birthday", "Geburtsdatum", FromDateTime, ToDateTime), new MappingProperty("HasGliderInstructorLicence", "Z_Segelfluglehrer_Lizenz", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONdirekt_ProffixZF(target, name, "HasGliderInstructorLicence", "Z_Segelfluglehrer_Lizenz")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "HasGliderInstructorLicence", "Z_Segelfluglehrer_Lizenz"))), new MappingProperty("HasGliderPilotLicence", "Z_Segelflugpilot_Lizenz", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONdirekt_ProffixZF(target, name, "HasGliderPilotLicence", "Z_Segelflugpilot_Lizenz")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "HasGliderPilotLicence", "Z_Segelflugpilot_Lizenz"))), new MappingProperty("HasGliderTraineeLicence", "Z_Segelflugschueler_Lizenz", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONdirekt_ProffixZF(target, name, "HasGliderTraineeLicence", "Z_Segelflugschueler_Lizenz")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "HasGliderTraineeLicence", "Z_Segelflugschueler_Lizenz"))), new MappingProperty("HasMotorPilotLicence", "Z_Motorflugpilot_Lizenz", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONdirekt_ProffixZF(target, name, "HasMotorPilotLicence", "Z_Motorflugpilot_Lizenz")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "HasMotorPilotLicence", "Z_Motorflugpilot_Lizenz"))), new MappingProperty("HasTowPilotLicence", "Z_Schleppilot_Lizenz", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONdirekt_ProffixZF(target, name, "HasTowPilotLicence", "Z_Schleppilot_Lizenz")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "HasTowPilotLicence", "Z_Schleppilot_Lizenz"))), new MappingProperty("HasGliderPassengerLicence", "Z_Segelflugpassagier_Lizenz", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONdirekt_ProffixZF(target, name, "HasGliderPassengerLicence", "Z_Segelflugpassagier_Lizenz")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "HasGliderPassengerLicence", "Z_Segelflugpassagier_Lizenz"))), new MappingProperty("HasTMGLicence", "Z_TMG_Lizenz", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONdirekt_ProffixZF(target, name, "HasTMGLicence", "Z_TMG_Lizenz")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "HasTMGLicence", "Z_TMG_Lizenz"))), new MappingProperty("HasWinchOperatorLicence", "Z_Windenfuehrer_Lizenz", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONdirekt_ProffixZF(target, name, "HasWinchOperatorLicence", "Z_Windenfuehrer_Lizenz")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "HasWinchOperatorLicence", "Z_Windenfuehrer_Lizenz"))), new MappingProperty("HasMotorInstructorLicence", "Z_Motorfluglehrer_Lizenz", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONdirekt_ProffixZF(target, name, "HasMotorInstructorLicence", "Z_Motorfluglehrer_Lizenz")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "HasMotorInstructorLicence", "Z_Motorfluglehrer_Lizenz"))), new MappingProperty("HasGliderTowingStartPermission", "Z_Schleppstart_Zulassung", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONdirekt_ProffixZF(target, name, "HasGliderTowingStartPermission", "Z_Schleppstart_Zulassung")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "HasGliderTowingStartPermission", "Z_Schleppstart_Zulassung"))), new MappingProperty("HasGliderSelfStartPermission", "Z_Eigenstart_Zulassung", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONdirekt_ProffixZF(target, name, "HasGliderSelfStartPermission", "Z_Eigenstart_Zulassung")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "HasGliderSelfStartPermission", "Z_Eigenstart_Zulassung"))), new MappingProperty("HasGliderWinchStartPermission", "Z_Windenstart_Zulassung", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONdirekt_ProffixZF(target, name, "HasGliderWinchStartPermission", "Z_Windenstart_Zulassung")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "HasGliderWinchStartPermission", "Z_Windenstart_Zulassung"))), new MappingProperty("SpotLink", "Z_SpotURL", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONdirekt_ProffixZF(target, name, "SpotLink", "Z_SpotURL")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "SpotLink", "Z_SpotURL"))), new MappingProperty("MedicalClass1ExpireDate", "Z_Medical1GueltigBis", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONdirekt_ProffixZF(target, name, "MedicalClass1ExpireDate", "Z_Medical1GueltigBis")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "MedicalClass1ExpireDate", "Z_Medical1GueltigBis"))), new MappingProperty("MedicalClass2ExpireDate", "Z_Medical1GueltigBis", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONdirekt_ProffixZF(target, name, "MedicalClass2ExpireDate", "Z_Medical2GueltigBis")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "MedicalClass2ExpireDate", "Z_Medical2GueltigBis"))), new MappingProperty("MedicalLaplExpireDate", "Z_MedicalLAPLGueltigBis", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONdirekt_ProffixZF(target, name, "MedicalLaplExpireDate", "Z_MedicalLAPLGueltigBis")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "MedicalLaplExpireDate", "Z_MedicalLAPLGueltigBis"))), new MappingProperty("GliderInstructorLicenceExpireDate", "Z_SegelfluglehrerlizenzGueltigBis", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONdirekt_ProffixZF(target, name, "GliderInstructorLicenceExpireDate", "Z_SegelfluglehrerlizenzGueltigBis")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "GliderInstructorLicenceExpireDate", "Z_SegelfluglehrerlizenzGueltigBis"))), new MappingProperty("BusinessEmail", "Z_Email_Geschaeft", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONdirekt_ProffixZF(target, name, "BusinessEmail", "Z_Email_Geschaeft")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "BusinessEmail", "Z_Email_Geschaeft"))), new MappingProperty("ReceiveOwnedAircraftStatisticReports", "Z_erhaeltFlugStatistikenZuEigenen", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONdirekt_ProffixZF(target, name, "ReceiveOwnedAircraftStatisticReports", "Z_erhaeltFlugStatistikenZuEigenen")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "ReceiveOwnedAircraftStatisticReports", "Z_erhaeltFlugStatistikenZuEigenen"))), new MappingProperty("LicenceNumber", "Z_Lizenznummer", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONdirekt_ProffixZF(target, name, "LicenceNumber", "Z_Lizenznummer")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "LicenceNumber", "Z_Lizenznummer"))), new MappingProperty("PersonId", "Z_FLSPersonId", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONdirekt_ProffixZF(target, name, "PersonId", "Z_FLSPersonId")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "PersonId", "Z_FLSPersonId"))) };
            }
        }



        // Noch einzufügende Felder
        // - Midname (gehört zum Namen, kann aber in PX nicht eingegeben werden --> müsste als Zusatzfeld implementiert werden)
        // - CompanyName (Achtung: Adressen aus FLS wären in PX eigentlich Kontakte und die Firma wäre die Adresse. Da aber in den Flugclubs nur Personen ohne Firma sind, wird die Person als Adresse genommen --> CompanyName höchstens als Zusatzfeld (aber kann Verwirrung geben, wenn die Firma doch wieder als Adresse)


        // ***************************************************************************************Werte holen und setzen*********************************************************************
        // Holt Wert direkt aus JSON (FLS) bzw. aus Zusatzfeld (Proffix)
        // aus JSON direkt lesen

        // aus pxAdresse Zusatzfeld lesen
        public static Func<object, string, string, string, object> GetValueFnc_JSONdirekt_ProffixZF = new Func<object, string, string, string, object>((target, name, serviceField, proffixField) => { try { if (target.GetType() == typeof(JObject)) { return ((JObject)target)[serviceField]; } else if (target.GetType() == typeof(pxKommunikation.pxAdressen)) { return ProffixHelper.GetZusatzFelder((pxKommunikation.pxAdressen)target, proffixField); } return target; } catch (Exception ex) { throw new Exception("Fehler beim Auslesen von " + name); return null; } });

        /// <summary>
    /// Speicher des PersonID
    /// </summary>
        public static Func<object, string, object, string, string, object> SetValueFnc_JSONdirekt_ProffixZF = new Func<object, string, object, string, string, object>((target, name, value, serviceField, proffixField) =>
            {

            // in ein JSON direkt einfügen
            if (target.GetType() == typeof(JObject))
                {
                    if (value is null | name.ToString() == "PersonId" & string.IsNullOrEmpty(value.ToString()))
                    {
                    }
                    else
                    {
                    // wenn es ein Boolean ist
                    if (value.ToString() == "1" | value.ToString() == "0")
                        {
                            JValue JValue = (JValue)false;
                            if (value.ToString() == "1")
                            {
                                JValue = (JValue)true;
                            }
                            else if (value.ToString() == "0")
                            {
                                JValue = (JValue)false;
                            } ((JObject)target)[serviceField] = JValue;
                            return target;
                        }


                    // wenn es ein Datum ist --> in UTC-Format umwandeln
                    // If value.GetType() Is GetType(Date) Or value.GetType() Is GetType(DateTime) Then
                    if (Information.IsDate(value) & name.ToString().ToLower().Contains("date"))
                        {
                        // In einer als Datetime definierten Variable speichern (erst dann können datetimefunktionen aufgerufen werden)
                        DateTime datum_datetime = Conversions.ToDate(value);

                        // wenn kein Datum eingefügt ist --> nichts hineinschreiben (bei keinem Datum ist day, month und year = 1 und hour, minute and second = 0)
                        if (datum_datetime.Day + datum_datetime.Month + datum_datetime.Year == 3)
                            {
                                ((JObject)target)[serviceField] = null;
                            }

                        // es soll ein gültiges Date/Datetime hineingeschrieben werden
                        else
                            {
                                ((JObject)target)[name] = datum_datetime.ToString("o");
                            }

                            return target;

                        // wenn es ein String ist
                    } ((JObject)target)[serviceField] = value.ToString();
                    }
                }

            // in ein pxAdresse einfügen
            else if (target.GetType() == typeof(pxKommunikation.pxAdressen))
                {
                    return ProffixHelper.SetZusatzFelder((pxKommunikation.pxAdressen)target, proffixField, proffixField, "", string.Empty, value is object ? value.ToString() : string.Empty);
                }

                return target;
            });
    }
}