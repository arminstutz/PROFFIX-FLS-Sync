using System;
using System.Collections.Generic;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json.Linq;
using pxBook;

namespace FlsGliderSync
{
    public class ClubMapper : Mapper<pxKommunikation.pxAdressen, JObject>
    {
        public static Func<object, object> BooleanToString = new Func<object, object>(obj => Conversions.ToBoolean(obj) ? "True" : "False");
        public static Func<object, object> StringToBoolean = new Func<object, object>(obj => Conversions.ToString(obj).ToLower() == "true" ? true : false);


        // **************************************************************************************MappingProperties für ClubRelated Werte (inkl. MemberKey = AdressNr in Proffix)
        public new static List<IMappingProperty> MappingProperies
        {
            get
            {
                return new List<IMappingProperty>(new[] { new MappingProperty("IsGliderPilot", "Z_Segelflugpilot", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONClubRel_ProffixZF(target, name, "IsGliderPilot", "Z_Segelflugpilot")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONClubRel_ProffixZF(target, name, value, "IsGliderPilot", "Z_Segelflugpilot"))), new MappingProperty("IsGliderInstructor", "Z_Segelfluglehrer", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONClubRel_ProffixZF(target, name, "IsGliderInstructor", "Z_Segelfluglehrer")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONClubRel_ProffixZF(target, name, value, "IsGliderInstructor", "Z_Segelfluglehrer"))), new MappingProperty("IsGliderTrainee", "Z_Segelflugschueler", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONClubRel_ProffixZF(target, name, "IsGliderTrainee", "Z_Segelflugschueler")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONClubRel_ProffixZF(target, name, value, "IsGliderTrainee", "Z_Segelflugschueler"))), new MappingProperty("IsMotorPilot", "Z_Motorflugpilot", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONClubRel_ProffixZF(target, name, "IsMotorPilot", "Z_Motorflugpilot")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONClubRel_ProffixZF(target, name, value, "IsMotorPilot", "Z_Motorflugpilot"))), new MappingProperty("IsPassenger", "Z_Passagier", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONClubRel_ProffixZF(target, name, "IsPassenger", "Z_Passagier")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONClubRel_ProffixZF(target, name, value, "IsPassenger", "Z_Passagier"))), new MappingProperty("IsTowPilot", "Z_Schleppilot", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONClubRel_ProffixZF(target, name, "IsTowPilot", "Z_Schleppilot")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONClubRel_ProffixZF(target, name, value, "IsTowPilot", "Z_Schleppilot"))), new MappingProperty("IsWinchOperator", "Z_Windenfuehrer", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONClubRel_ProffixZF(target, name, "IsWinchOperator", "Z_Windenfuehrer")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONClubRel_ProffixZF(target, name, value, "IsWinchOperator", "Z_Windenfuehrer"))), new MappingProperty("ReceiveFlightReports", "Z_erhaeltFlugreport", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONClubRel_ProffixZF(target, name, "ReceiveFlightReports", "Z_erhaeltFlugreport")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONClubRel_ProffixZF(target, name, value, "ReceiveFlightReports", "Z_erhaeltFlugreport"))), new MappingProperty("IsMotorInstructor", "Z_Motorfluglehrer", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONClubRel_ProffixZF(target, name, "IsMotorInstructor", "Z_Motorfluglehrer")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONClubRel_ProffixZF(target, name, value, "IsMotorInstructor", "Z_Motorfluglehrer"))), new MappingProperty("ReceiveAircraftReservationNotifications", "Z_erhaeltReservationsmeldung", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONClubRel_ProffixZF(target, name, "ReceiveAircraftReservationNotifications", "Z_erhaeltReservationsmeldung")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONClubRel_ProffixZF(target, name, value, "ReceiveAircraftReservationNotifications", "Z_erhaeltReservationsmeldung"))), new MappingProperty("ReceivePlanningDayRoleReminder", "Z_erhaeltPlanungserinnerung", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONClubRel_ProffixZF(target, name, "ReceivePlanningDayRoleReminder", "Z_erhaeltPlanungserinnerung")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONClubRel_ProffixZF(target, name, value, "ReceivePlanningDayRoleReminder", "Z_erhaeltPlanungserinnerung"))), new MappingProperty("MemberStateId", "Z_MemberStateId", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONClubRel_ProffixZF(target, name, "MemberStateId", "Z_MemberStateId")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONClubRel_ProffixZF(target, name, value, "MemberStateId", "Z_MemberStateId"))), new MappingProperty("MemberNumber", "AdressNr", null, null, null, null, new Func<object, string, object>((target, name) => GetValueFnc_JSONClubRel_Proffixdirekt(target, name, "MemberNumber", "AdressNr")), new Func<object, string, object, object>((target, name, value) => SetValueFnc_JSONClubRel_Proffixdirekt(target, name, value, "MemberNumber", "AdressNr"))) });
            }
        }

        // ***********************************************************************Werte holen und einfügen*********************************************************************************
        // für IsGliderPilot... (FLS ClubRelated <--> Proffix Zusatzfeld)
        // aus JSON ClubRel lesen
        // aus Proffix Zusatzfeld lesen
        public static Func<object, string, string, string, object> GetValueFnc_JSONClubRel_ProffixZF = new Func<object, string, string, string, object>((target, name, serviceField, proffixField) => { try { if (target.GetType() == typeof(JObject)) { return ((JObject)target)["ClubRelatedPersonDetails"][serviceField]; } else if (target.GetType() == typeof(pxKommunikation.pxAdressen)) { return ProffixHelper.GetZusatzFelder((pxKommunikation.pxAdressen)target, proffixField); } return target; } catch (Exception ex) { throw new Exception("Fehler beim Auslesen von " + name); return null; } });

        // für MemberKey (FLS ClubRelated <--> Proffix Feld direkt)
        // aus JSON ClubRel lesen
        // direkt aus Proffix-Feld lesen
        public static Func<object, string, string, string, object> GetValueFnc_JSONClubRel_Proffixdirekt = new Func<object, string, string, string, object>((target, name, serviceField, proffixField) => { try { if (target.GetType() == typeof(JObject)) { return ((JObject)target)["ClubRelatedPersonDetails"][serviceField]; } else if (target.GetType() == typeof(pxKommunikation.pxAdressen)) { return target.GetType().GetField(name).GetValue(target); } return target; } catch (Exception ex) { throw new Exception("Fehler beim Auslesen von " + name); return null; } });


        // für IsGliderPilot... (FLS ClubRelated <--> Proffix Zusatzfeld)
        public static Func<object, string, object, string, string, object> SetValueFnc_JSONClubRel_ProffixZF = new Func<object, string, object, string, string, object>((target, name, value, serviceField, proffixField) =>
            {
            // in JSON einfügen (update --> clubrel vorhaden) (create --> clubrel als eigenes JObject erstellen)
            if (target.GetType() == typeof(JObject))
                {
                // 1/0 in boolean umwandeln und als Boolean in JObject schreiben
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
                        }

                    // enthält das JObject den Text ClubRelatedPersonDetails?
                    if (target.ToString().Contains("ClubRelatedPersonDetails"))
                        {
                        // wenn ja (update von bestehender Adresse in FLS) --> unter clubrel einfügen
                        ((JObject)target)["ClubRelatedPersonDetails"][serviceField] = JValue;
                        }
                        else
                        {
                        // wenn nein (= create von neuer Adresse in FLS) --> clubrel als eigenes JObject erstellen und danach als ganzes in person einfügen
                        ((JObject)target)[serviceField] = JValue;
                        }

                        return target;
                    }

                // ein String soll eingefügt werden
                // enthält das JObject den Text ClubRelatedPersonDetails?
                if (target.ToString().Contains("ClubRelatedPersonDetails"))
                    {
                    // wenn ja (update von bestehender Adresse in FLS) --> unter clubrel einfügen
                    ((JObject)target)["ClubRelatedPersonDetails"][serviceField] = value.ToString();
                    }
                    else
                    {
                    // wenn nein (= create von neuer Adresse in FLS) --> clubrel als eigenes JObject erstellen und danach als ganzes in person einfügen
                    ((JObject)target)[serviceField] = value.ToString();
                    }
                }

            // in pxAdresse Zusatzfeld einfügen
            else if (target.GetType() == typeof(pxKommunikation.pxAdressen))
                {
                    return ProffixHelper.SetZusatzFelder((pxKommunikation.pxAdressen)target, proffixField, proffixField, "", string.Empty, value is object ? value.ToString() : string.Empty);
                }

                return target;
            });


        // für MemberNumber (FLS ClubRelated <--> Proffix Feld direkt)
        public static Func<object, string, object, string, string, object> SetValueFnc_JSONClubRel_Proffixdirekt = new Func<object, string, object, string, string, object>((target, name, value, serviceField, proffixField) =>
            {
            // MemberNumber bei Update und neu erstellen in FLS einfügen
            if (target.GetType() == typeof(JObject))
                {
                // wird update (clubrel bereits vorhanden) oder create (clubrel als eigenes JObject) gemacht
                if (target.ToString().Contains("ClubRelatedPersonDetails"))
                    {
                    // wenn update --> clubrel bereits in FLS vorhanden --> unter clubrel einfügen
                    ((JObject)target)["ClubRelatedPersonDetails"][serviceField] = value.ToString();
                    }
                    else
                    {
                    // wenn create --> clubrel als eigenes JObject erstellt --> direkt einfügen (wird später als ganzes in person eingefügt)
                    ((JObject)target)[serviceField] = value.ToString();
                    }
                }
                else if (target.GetType() == typeof(pxKommunikation.pxAdressen))
                {
                // AdressNr muss nie selber gesetzt werden, da sie in Proffix erstellt wird (create), oder bereits vorhanden ist (update)
            }

                return target;
            });


        // '**************************************************************************************JObject clubPers in JObject pers einfügen, gibt zusammengefügtes JSON zurück
        public JObject completePersWithclubPers(JObject target, ref JObject innerJObject)
        {
            target["ClubRelatedPersonDetails"] = innerJObject;
            return target;
        }

        // ********************************************************************************Mapp Demapp*************************************************************************************************
        // Public Overloads Function Mapp(ByRef source As pxKommunikation.pxAdressen, ByRef target As JObject) As JObject
        // 'SetMemberKey(target, source)
        // Return MyBase.Mapp(source, target)
        // End Function

        // Public Overloads Function DeMapp(ByRef source As JObject, ByRef target As pxKommunikation.pxAdressen) As pxKommunikation.pxAdressen
        // Return MyBase.DeMapp(target, source)
        // End Function

    }
}