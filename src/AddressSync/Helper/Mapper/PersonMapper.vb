Imports System.Runtime.Remoting.Messaging
Imports Newtonsoft.Json.Linq

Imports pxBook


Public Class PersonMapper : Inherits Mapper(Of Object, JObject)
    ''' <summary>
    ''' Die Parse Funktion von String zu DateTime
    ''' </summary>
    Public Shared ReadOnly Property ToDateTime As Func(Of String, DateTime)
        Get
            Return New Func(Of String, DateTime)(Function(value)
                                                     Return If(String.IsNullOrEmpty(value) Or value = String.Empty, Nothing, DateTime.Parse(value))
                                                 End Function)
        End Get
    End Property

    ''' <summary>
    ''' Die Parse Funktion von DateTime zu String
    ''' </summary>
    Public Shared ReadOnly Property FromDateTime As Func(Of DateTime, String)
        Get
            Return New Func(Of DateTime, String)(Function(value)
                                                     Return If(value = Nothing, Nothing, value.ToString())
                                                 End Function)
        End Get
    End Property


    '************************************************************************************MappingProperties, welches FLS-Feld entspricht welchem Proffixfeld und wie muss geholt/eingefügt werden*******************************
    ''' <summary>
    ''' Die Mappingproperties für direkte Eigenschaften (bis MobilePhoneNumber) bzw. FLS direkt und Proffix Zusatzfeld (ab HasGliderInstructorLicence)
    ''' Die Mappingproperties für die ClubRelated Eigenschaften sind in ClubMapper definiert
    ''' </summary>
    Public Overloads Shared ReadOnly Property MappingProperies As List(Of IMappingProperty)
        Get
            Return New List(Of IMappingProperty)({ _
                                                New MappingProperty("AddressLine1", "Strasse"),
                                                New MappingProperty("AddressLine2", "Adresszeile1"),
                                                New MappingProperty("BusinessPhoneNumber", "TelZentrale"),
                                                New MappingProperty("City", "Ort"),
                                                New MappingProperty("FaxNumber", "Fax"),
                                                New MappingProperty("Firstname", "Vorname"),
                                                New MappingProperty("Lastname", "Name"),
                                                New MappingProperty("PrivateEmail", "EMail"),
                                                New MappingProperty("PrivatePhoneNumber", "TelPrivat"),
                                                New MappingProperty("Region", "Region"),
                                                New MappingProperty("ZipCode", "Plz"),
                                                New MappingProperty("MobilePhoneNumber", "Natel"),
                                                New MappingProperty("CountryId", "Land"),
                                                New MappProperty(Of DateTime, String)("Birthday", "Geburtsdatum", FromDateTime, ToDateTime),
                                                New MappingProperty("HasGliderInstructorLicence", "Z_Segelfluglehrer_Lizenz", Nothing, Nothing, Nothing, Nothing,
                                                                    New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONdirekt_ProffixZF(target, name, "HasGliderInstructorLicence", "Z_Segelfluglehrer_Lizenz")),
                                                                      New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "HasGliderInstructorLicence", "Z_Segelfluglehrer_Lizenz"))),
                                                New MappingProperty("HasGliderPilotLicence", "Z_Segelflugpilot_Lizenz", Nothing, Nothing, Nothing, Nothing,
                                                                    New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONdirekt_ProffixZF(target, name, "HasGliderPilotLicence", "Z_Segelflugpilot_Lizenz")),
                                                                      New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "HasGliderPilotLicence", "Z_Segelflugpilot_Lizenz"))),
                                                New MappingProperty("HasGliderTraineeLicence", "Z_Segelflugschueler_Lizenz", Nothing, Nothing, Nothing, Nothing,
                                                                    New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONdirekt_ProffixZF(target, name, "HasGliderTraineeLicence", "Z_Segelflugschueler_Lizenz")),
                                                                      New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "HasGliderTraineeLicence", "Z_Segelflugschueler_Lizenz"))),
                                                New MappingProperty("HasMotorPilotLicence", "Z_Motorflugpilot_Lizenz", Nothing, Nothing, Nothing, Nothing,
                                                                    New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONdirekt_ProffixZF(target, name, "HasMotorPilotLicence", "Z_Motorflugpilot_Lizenz")),
                                                                      New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "HasMotorPilotLicence", "Z_Motorflugpilot_Lizenz"))),
                                                New MappingProperty("HasTowPilotLicence", "Z_Schleppilot_Lizenz", Nothing, Nothing, Nothing, Nothing,
                                                                    New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONdirekt_ProffixZF(target, name, "HasTowPilotLicence", "Z_Schleppilot_Lizenz")),
                                                                      New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "HasTowPilotLicence", "Z_Schleppilot_Lizenz"))),
                                                New MappingProperty("HasGliderPassengerLicence", "Z_Segelflugpassagier_Lizenz", Nothing, Nothing, Nothing, Nothing,
                                                                    New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONdirekt_ProffixZF(target, name, "HasGliderPassengerLicence", "Z_Segelflugpassagier_Lizenz")),
                                                                      New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "HasGliderPassengerLicence", "Z_Segelflugpassagier_Lizenz"))),
                                                New MappingProperty("HasTMGLicence", "Z_TMG_Lizenz", Nothing, Nothing, Nothing, Nothing,
                                                                    New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONdirekt_ProffixZF(target, name, "HasTMGLicence", "Z_TMG_Lizenz")),
                                                                      New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "HasTMGLicence", "Z_TMG_Lizenz"))),
                                                New MappingProperty("HasWinchOperatorLicence", "Z_Windenfuehrer_Lizenz", Nothing, Nothing, Nothing, Nothing,
                                                                    New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONdirekt_ProffixZF(target, name, "HasWinchOperatorLicence", "Z_Windenfuehrer_Lizenz")),
                                                                      New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "HasWinchOperatorLicence", "Z_Windenfuehrer_Lizenz"))),
                                                New MappingProperty("HasMotorInstructorLicence", "Z_Motorfluglehrer_Lizenz", Nothing, Nothing, Nothing, Nothing,
                                                                    New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONdirekt_ProffixZF(target, name, "HasMotorInstructorLicence", "Z_Motorfluglehrer_Lizenz")),
                                                                      New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "HasMotorInstructorLicence", "Z_Motorfluglehrer_Lizenz"))),
                                                New MappingProperty("HasGliderTowingStartPermission", "Z_Schleppstart_Zulassung", Nothing, Nothing, Nothing, Nothing,
                                                                    New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONdirekt_ProffixZF(target, name, "HasGliderTowingStartPermission", "Z_Schleppstart_Zulassung")),
                                                                      New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "HasGliderTowingStartPermission", "Z_Schleppstart_Zulassung"))),
                                                New MappingProperty("HasGliderSelfStartPermission", "Z_Eigenstart_Zulassung", Nothing, Nothing, Nothing, Nothing,
                                                                    New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONdirekt_ProffixZF(target, name, "HasGliderSelfStartPermission", "Z_Eigenstart_Zulassung")),
                                                                      New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "HasGliderSelfStartPermission", "Z_Eigenstart_Zulassung"))),
                                                New MappingProperty("HasGliderWinchStartPermission", "Z_Windenstart_Zulassung", Nothing, Nothing, Nothing, Nothing,
                                                                    New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONdirekt_ProffixZF(target, name, "HasGliderWinchStartPermission", "Z_Windenstart_Zulassung")),
                                                                      New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "HasGliderWinchStartPermission", "Z_Windenstart_Zulassung"))),
                                                 New MappingProperty("SpotLink", "Z_SpotURL", Nothing, Nothing, Nothing, Nothing,
                                                                    New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONdirekt_ProffixZF(target, name, "SpotLink", "Z_SpotURL")),
                                                                      New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "SpotLink", "Z_SpotURL"))),
                                                 New MappingProperty("MedicalClass1ExpireDate", "Z_Medical1GueltigBis", Nothing, Nothing, Nothing, Nothing,
                                                                   New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONdirekt_ProffixZF(target, name, "MedicalClass1ExpireDate", "Z_Medical1GueltigBis")),
                                                                     New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "MedicalClass1ExpireDate", "Z_Medical1GueltigBis"))),
                                                New MappingProperty("MedicalClass2ExpireDate", "Z_Medical1GueltigBis", Nothing, Nothing, Nothing, Nothing,
                                                                    New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONdirekt_ProffixZF(target, name, "MedicalClass2ExpireDate", "Z_Medical2GueltigBis")),
                                                                        New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "MedicalClass2ExpireDate", "Z_Medical2GueltigBis"))),
                                                New MappingProperty("MedicalLaplExpireDate", "Z_MedicalLAPLGueltigBis", Nothing, Nothing, Nothing, Nothing,
                                                                    New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONdirekt_ProffixZF(target, name, "MedicalLaplExpireDate", "Z_MedicalLAPLGueltigBis")),
                                                                        New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "MedicalLaplExpireDate", "Z_MedicalLAPLGueltigBis"))),
                                                 New MappingProperty("GliderInstructorLicenceExpireDate", "Z_SegelfluglehrerlizenzGueltigBis", Nothing, Nothing, Nothing, Nothing,
                                                                    New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONdirekt_ProffixZF(target, name, "GliderInstructorLicenceExpireDate", "Z_SegelfluglehrerlizenzGueltigBis")),
                                                                        New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "GliderInstructorLicenceExpireDate", "Z_SegelfluglehrerlizenzGueltigBis"))),
                                               New MappingProperty("BusinessEmail", "Z_Email_Geschaeft", Nothing, Nothing, Nothing, Nothing,
                                                                    New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONdirekt_ProffixZF(target, name, "BusinessEmail", "Z_Email_Geschaeft")),
                                                                      New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "BusinessEmail", "Z_Email_Geschaeft"))),
                                                New MappingProperty("ReceiveOwnedAircraftStatisticReports", "Z_erhaeltFlugStatistikenZuEigenen", Nothing, Nothing, Nothing, Nothing,
                                                                    New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONdirekt_ProffixZF(target, name, "ReceiveOwnedAircraftStatisticReports", "Z_erhaeltFlugStatistikenZuEigenen")),
                                                                      New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "ReceiveOwnedAircraftStatisticReports", "Z_erhaeltFlugStatistikenZuEigenen"))),
                                                New MappingProperty("LicenceNumber", "Z_Lizenznummer", Nothing, Nothing, Nothing, Nothing,
                                                                    New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONdirekt_ProffixZF(target, name, "LicenceNumber", "Z_Lizenznummer")),
                                                                      New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "LicenceNumber", "Z_Lizenznummer"))),
                                                New MappingProperty("PersonId", "Z_FLSPersonId", Nothing, Nothing, Nothing, Nothing,
                                                                   New Func(Of Object, String, Object)(Function(target, name) GetValueFnc_JSONdirekt_ProffixZF(target, name, "PersonId", "Z_FLSPersonId")),
                                                                     New Func(Of Object, String, Object, Object)(Function(target, name, value) SetValueFnc_JSONdirekt_ProffixZF(target, name, value, "PersonId", "Z_FLSPersonId")))
   })
        End Get
    End Property



    ' Noch einzufügende Felder
    '- Midname (gehört zum Namen, kann aber in PX nicht eingegeben werden --> müsste als Zusatzfeld implementiert werden)
    '- CompanyName (Achtung: Adressen aus FLS wären in PX eigentlich Kontakte und die Firma wäre die Adresse. Da aber in den Flugclubs nur Personen ohne Firma sind, wird die Person als Adresse genommen --> CompanyName höchstens als Zusatzfeld (aber kann Verwirrung geben, wenn die Firma doch wieder als Adresse)

 
    '***************************************************************************************Werte holen und setzen*********************************************************************
    ' Holt Wert direkt aus JSON (FLS) bzw. aus Zusatzfeld (Proffix)
    Public Shared GetValueFnc_JSONdirekt_ProffixZF As New Func(Of Object, String, String, String, Object)(Function(target, name, serviceField, proffixField)

                                                                                                              Try
                                                                                                                  ' aus JSON direkt lesen
                                                                                                                  If target.GetType = GetType(JObject) Then
                                                                                                                      Return CType(target, JObject)(serviceField)

                                                                                                                      ' aus pxAdresse Zusatzfeld lesen
                                                                                                                  ElseIf target.GetType = GetType(pxKommunikation.pxAdressen) Then
                                                                                                                      Return ProffixHelper.GetZusatzFelder(CType(target, pxKommunikation.pxAdressen), proffixField)
                                                                                                                  End If
                                                                                                                  Return target

                                                                                                              Catch ex As Exception
                                                                                                                  Throw New Exception("Fehler beim Auslesen von " + name)
                                                                                                                  Return Nothing
                                                                                                              End Try
                                                                                                          End Function)

    ''' <summary>
    ''' Speicher des PersonID
    ''' </summary>
    Public Shared SetValueFnc_JSONdirekt_ProffixZF As New Func(Of Object, String, Object, String, String, Object)(Function(target, name, value, serviceField, proffixField)

                                                                                                                      ' in ein JSON direkt einfügen
                                                                                                                      If target.GetType = GetType(JObject) Then
                                                                                                                          If (value Is Nothing Or (name.ToString = "PersonId" And value.ToString = "")) Then
                                                                                                                          Else
                                                                                                                              ' wenn es ein Boolean ist
                                                                                                                              If value.ToString = "1" Or value.ToString = "0" Then
                                                                                                                                  Dim JValue As JValue = CType(False, JValue)
                                                                                                                                  If value.ToString = "1" Then
                                                                                                                                      JValue = CType(True, JValue)
                                                                                                                                  ElseIf value.ToString = "0" Then
                                                                                                                                      JValue = CType(False, JValue)
                                                                                                                                  End If
                                                                                                                                  CType(target, JObject)(serviceField) = JValue
                                                                                                                                  Return target
                                                                                                                              End If


                                                                                                                              ' wenn es ein Datum ist --> in UTC-Format umwandeln
                                                                                                                              ' If value.GetType() Is GetType(Date) Or value.GetType() Is GetType(DateTime) Then
                                                                                                                              If IsDate(value) And name.ToString.ToLower.Contains("date") Then
                                                                                                                                  ' In einer als Datetime definierten Variable speichern (erst dann können datetimefunktionen aufgerufen werden)
                                                                                                                                  Dim datum_datetime As DateTime = CType(value, DateTime)

                                                                                                                                  ' wenn kein Datum eingefügt ist --> nichts hineinschreiben (bei keinem Datum ist day, month und year = 1 und hour, minute and second = 0)
                                                                                                                                  If datum_datetime.Day + datum_datetime.Month + datum_datetime.Year = 3 Then
                                                                                                                                      CType(target, JObject)(serviceField) = Nothing

                                                                                                                                      ' es soll ein gültiges Date/Datetime hineingeschrieben werden
                                                                                                                                  Else
                                                                                                                                      CType(target, JObject)(name) = datum_datetime.ToString("o")
                                                                                                                                  End If
                                                                                                                                  Return target
                                                                                                                              End If

                                                                                                                              ' wenn es ein String ist
                                                                                                                              CType(target, JObject)(serviceField) = value.ToString

                                                                                                                          End If

                                                                                                                          ' in ein pxAdresse einfügen
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

End Class
