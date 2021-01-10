Imports System.Runtime.Remoting.Messaging

Imports Newtonsoft.Json.Linq

Imports pxBook



Public Class ClubMapper : Inherits Mapper(Of pxKommunikation.pxAdressen, JObject)
    Public Shared BooleanToString As New Func(Of Object, Object)(Function(obj)
                                                                     Return If(CType(obj, Boolean), "True", "False")
                                                                 End Function)
    Public Shared StringToBoolean As New Func(Of Object, Object)(Function(obj)
                                                                     Return If(CType(obj, String).ToLower = "true", True, False)
                                                                 End Function)


    '**************************************************************************************MappingProperties für ClubRelated Werte (inkl. MemberKey = AdressNr in Proffix)
    Public Overloads Shared ReadOnly Property MappingProperies As List(Of IMappingProperty)
        Get
            Return New List(Of IMappingProperty)({
                                              New MappingProperty("IsGliderPilot", "Z_Segelflugpilot", Nothing, Nothing, Nothing, Nothing,
                                                                    New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONClubRel_ProffixZF(target, name, "IsGliderPilot", "Z_Segelflugpilot")),
                                                                    New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONClubRel_ProffixZF(target, name, value, "IsGliderPilot", "Z_Segelflugpilot"))),
                                              New MappingProperty("IsGliderInstructor", "Z_Segelfluglehrer", Nothing, Nothing, Nothing, Nothing,
                                                                    New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONClubRel_ProffixZF(target, name, "IsGliderInstructor", "Z_Segelfluglehrer")),
                                                                    New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONClubRel_ProffixZF(target, name, value, "IsGliderInstructor", "Z_Segelfluglehrer"))),
                                              New MappingProperty("IsGliderTrainee", "Z_Segelflugschueler", Nothing, Nothing, Nothing, Nothing,
                                                                    New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONClubRel_ProffixZF(target, name, "IsGliderTrainee", "Z_Segelflugschueler")),
                                                                    New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONClubRel_ProffixZF(target, name, value, "IsGliderTrainee", "Z_Segelflugschueler"))),
                                              New MappingProperty("IsMotorPilot", "Z_Motorflugpilot", Nothing, Nothing, Nothing, Nothing,
                                                                    New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONClubRel_ProffixZF(target, name, "IsMotorPilot", "Z_Motorflugpilot")),
                                                                    New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONClubRel_ProffixZF(target, name, value, "IsMotorPilot", "Z_Motorflugpilot"))),
                                              New MappingProperty("IsPassenger", "Z_Passagier", Nothing, Nothing, Nothing, Nothing,
                                                                    New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONClubRel_ProffixZF(target, name, "IsPassenger", "Z_Passagier")),
                                                                    New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONClubRel_ProffixZF(target, name, value, "IsPassenger", "Z_Passagier"))),
                                              New MappingProperty("IsTowPilot", "Z_Schleppilot", Nothing, Nothing, Nothing, Nothing,
                                                                    New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONClubRel_ProffixZF(target, name, "IsTowPilot", "Z_Schleppilot")),
                                                                    New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONClubRel_ProffixZF(target, name, value, "IsTowPilot", "Z_Schleppilot"))),
                                              New MappingProperty("IsWinchOperator", "Z_Windenfuehrer", Nothing, Nothing, Nothing, Nothing,
                                                                    New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONClubRel_ProffixZF(target, name, "IsWinchOperator", "Z_Windenfuehrer")),
                                                                    New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONClubRel_ProffixZF(target, name, value, "IsWinchOperator", "Z_Windenfuehrer"))),
                                              New MappingProperty("ReceiveFlightReports", "Z_erhaeltFlugreport", Nothing, Nothing, Nothing, Nothing,
                                                                    New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONClubRel_ProffixZF(target, name, "ReceiveFlightReports", "Z_erhaeltFlugreport")),
                                                                    New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONClubRel_ProffixZF(target, name, value, "ReceiveFlightReports", "Z_erhaeltFlugreport"))),
                                              New MappingProperty("IsMotorInstructor", "Z_Motorfluglehrer", Nothing, Nothing, Nothing, Nothing,
                                                                    New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONClubRel_ProffixZF(target, name, "IsMotorInstructor", "Z_Motorfluglehrer")),
                                                                    New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONClubRel_ProffixZF(target, name, value, "IsMotorInstructor", "Z_Motorfluglehrer"))),
                                              New MappingProperty("ReceiveAircraftReservationNotifications", "Z_erhaeltReservationsmeldung", Nothing, Nothing, Nothing, Nothing,
                                                                    New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONClubRel_ProffixZF(target, name, "ReceiveAircraftReservationNotifications", "Z_erhaeltReservationsmeldung")),
                                                                    New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONClubRel_ProffixZF(target, name, value, "ReceiveAircraftReservationNotifications", "Z_erhaeltReservationsmeldung"))),
                                              New MappingProperty("ReceivePlanningDayRoleReminder", "Z_erhaeltPlanungserinnerung", Nothing, Nothing, Nothing, Nothing,
                                                                    New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONClubRel_ProffixZF(target, name, "ReceivePlanningDayRoleReminder", "Z_erhaeltPlanungserinnerung")),
                                                                    New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONClubRel_ProffixZF(target, name, value, "ReceivePlanningDayRoleReminder", "Z_erhaeltPlanungserinnerung"))),
                                              New MappingProperty("MemberStateId", "Z_MemberStateId", Nothing, Nothing, Nothing, Nothing,
                                                                   New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONClubRel_ProffixZF(target, name, "MemberStateId", "Z_MemberStateId")),
                                                                    New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONClubRel_ProffixZF(target, name, value, "MemberStateId", "Z_MemberStateId"))),
                                              New MappingProperty("MemberNumber", "AdressNr", Nothing, Nothing, Nothing, Nothing,
                                                                   New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONClubRel_Proffixdirekt(target, name, "MemberNumber", "AdressNr")),
                                                                    New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONClubRel_Proffixdirekt(target, name, value, "MemberNumber", "AdressNr")))
 })

        End Get
    End Property

    '***********************************************************************Werte holen und einfügen*********************************************************************************
    ' für IsGliderPilot... (FLS ClubRelated <--> Proffix Zusatzfeld)
    Public Shared GetValueFnc_JSONClubRel_ProffixZF As New Func(Of Object, String, String, String, Object)(Function(target, name, serviceField, proffixField)
                                                                                                               Try
                                                                                                                   ' aus JSON ClubRel lesen
                                                                                                                   If target.GetType = GetType(JObject) Then
                                                                                                                       Return CType(target, JObject)("ClubRelatedPersonDetails")(serviceField)
                                                                                                                       ' aus Proffix Zusatzfeld lesen
                                                                                                                   ElseIf target.GetType = GetType(pxKommunikation.pxAdressen) Then
                                                                                                                       Return ProffixHelper.GetZusatzFelder(CType(target, pxKommunikation.pxAdressen), proffixField)
                                                                                                                   End If
                                                                                                                   Return target
                                                                                                               Catch ex As Exception
                                                                                                                   Throw New Exception("Fehler beim Auslesen von " + name)
                                                                                                                   Return Nothing
                                                                                                               End Try
                                                                                                           End Function)

    ' für MemberKey (FLS ClubRelated <--> Proffix Feld direkt)
    Public Shared GetValueFnc_JSONClubRel_Proffixdirekt As New Func(Of Object, String, String, String, Object)(Function(target, name, serviceField, proffixField)
                                                                                                                   Try
                                                                                                                       ' aus JSON ClubRel lesen
                                                                                                                       If target.GetType = GetType(JObject) Then
                                                                                                                           Return CType(target, JObject)("ClubRelatedPersonDetails")(serviceField)
                                                                                                                           ' direkt aus Proffix-Feld lesen
                                                                                                                       ElseIf target.GetType = GetType(pxKommunikation.pxAdressen) Then
                                                                                                                           Return target.GetType().GetField(name).GetValue(target)
                                                                                                                       End If
                                                                                                                       Return target
                                                                                                                   Catch ex As Exception
                                                                                                                       Throw New Exception("Fehler beim Auslesen von " + name)
                                                                                                                       Return Nothing
                                                                                                                   End Try
                                                                                                               End Function)


    ' für IsGliderPilot... (FLS ClubRelated <--> Proffix Zusatzfeld)
    Public Shared SetValueFnc_JSONClubRel_ProffixZF As New Func(Of Object, String, Object, String, String, Object)(Function(target, name, value, serviceField, proffixField)
                                                                                                                       ' in JSON einfügen (update --> clubrel vorhaden) (create --> clubrel als eigenes JObject erstellen)
                                                                                                                       If target.GetType = GetType(JObject) Then
                                                                                                                           ' 1/0 in boolean umwandeln und als Boolean in JObject schreiben
                                                                                                                           If value.ToString = "1" Or value.ToString = "0" Then
                                                                                                                               Dim JValue As JValue = CType(False, JValue)
                                                                                                                               If value.ToString = "1" Then
                                                                                                                                   JValue = CType(True, JValue)
                                                                                                                               ElseIf value.ToString = "0" Then
                                                                                                                                   JValue = CType(False, JValue)
                                                                                                                               End If

                                                                                                                               ' enthält das JObject den Text ClubRelatedPersonDetails?
                                                                                                                               If target.ToString.Contains("ClubRelatedPersonDetails") Then
                                                                                                                                   ' wenn ja (update von bestehender Adresse in FLS) --> unter clubrel einfügen
                                                                                                                                   CType(target, JObject)("ClubRelatedPersonDetails")(serviceField) = JValue
                                                                                                                               Else
                                                                                                                                   ' wenn nein (= create von neuer Adresse in FLS) --> clubrel als eigenes JObject erstellen und danach als ganzes in person einfügen
                                                                                                                                   CType(target, JObject)(serviceField) = JValue
                                                                                                                               End If
                                                                                                                               Return target
                                                                                                                           End If

                                                                                                                           ' ein String soll eingefügt werden
                                                                                                                           ' enthält das JObject den Text ClubRelatedPersonDetails?
                                                                                                                           If target.ToString.Contains("ClubRelatedPersonDetails") Then
                                                                                                                               ' wenn ja (update von bestehender Adresse in FLS) --> unter clubrel einfügen
                                                                                                                               CType(target, JObject)("ClubRelatedPersonDetails")(serviceField) = value.ToString
                                                                                                                           Else
                                                                                                                               ' wenn nein (= create von neuer Adresse in FLS) --> clubrel als eigenes JObject erstellen und danach als ganzes in person einfügen
                                                                                                                               CType(target, JObject)(serviceField) = value.ToString
                                                                                                                           End If

                                                                                                                           ' in pxAdresse Zusatzfeld einfügen
                                                                                                                       ElseIf target.GetType = GetType(pxKommunikation.pxAdressen) Then
                                                                                                                           Return ProffixHelper.SetZusatzFelder(
                                                                                                                                                                CType(target, pxKommunikation.pxAdressen),
                                                                                                                                                                proffixField,
                                                                                                                                                                proffixField,
                                                                                                                                                                "",
                                                                                                                                                                String.Empty,
                                                                                                                                                                If(value IsNot Nothing, value.ToString, String.Empty))
                                                                                                                       End If
                                                                                                                       Return target
                                                                                                                   End Function)


    ' für MemberNumber (FLS ClubRelated <--> Proffix Feld direkt)
    Public Shared SetValueFnc_JSONClubRel_Proffixdirekt As New Func(Of Object, String, Object, String, String, Object)(Function(target, name, value, serviceField, proffixField)
                                                                                                                           'MemberNumber bei Update und neu erstellen in FLS einfügen
                                                                                                                           If target.GetType = GetType(JObject) Then
                                                                                                                               ' wird update (clubrel bereits vorhanden) oder create (clubrel als eigenes JObject) gemacht
                                                                                                                               If target.ToString.Contains("ClubRelatedPersonDetails") Then
                                                                                                                                   ' wenn update --> clubrel bereits in FLS vorhanden --> unter clubrel einfügen
                                                                                                                                   CType(target, JObject)("ClubRelatedPersonDetails")(serviceField) = value.ToString
                                                                                                                               Else
                                                                                                                                   ' wenn create --> clubrel als eigenes JObject erstellt --> direkt einfügen (wird später als ganzes in person eingefügt)
                                                                                                                                   CType(target, JObject)(serviceField) = value.ToString
                                                                                                                               End If
                                                                                                                           ElseIf target.GetType = GetType(pxKommunikation.pxAdressen) Then
                                                                                                                               ' AdressNr muss nie selber gesetzt werden, da sie in Proffix erstellt wird (create), oder bereits vorhanden ist (update)
                                                                                                                           End If
                                                                                                                           Return target
                                                                                                                       End Function)


    ' '**************************************************************************************JObject clubPers in JObject pers einfügen, gibt zusammengefügtes JSON zurück
    Public Function completePersWithclubPers(ByVal target As JObject, ByRef innerJObject As JObject) As JObject
        CType(target, JObject)("ClubRelatedPersonDetails") = innerJObject
        Return target
    End Function

    '********************************************************************************Mapp Demapp*************************************************************************************************
    'Public Overloads Function Mapp(ByRef source As pxKommunikation.pxAdressen, ByRef target As JObject) As JObject
    '    'SetMemberKey(target, source)
    '    Return MyBase.Mapp(source, target)
    'End Function

    'Public Overloads Function DeMapp(ByRef source As JObject, ByRef target As pxKommunikation.pxAdressen) As pxKommunikation.pxAdressen
    '    Return MyBase.DeMapp(target, source)
    'End Function

End Class
