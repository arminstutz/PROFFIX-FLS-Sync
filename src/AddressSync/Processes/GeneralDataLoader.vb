Imports Newtonsoft.Json.Linq
Imports SMC.Lib
Imports System.Reflection

Public Class GeneralDataLoader

    Private flsClient As FlsConnection
    Private pxhelper As ProffixHelper
    Private myconn As ProffixConnection
    Private importer As Importer

    Private Shared _laender_dict As New Dictionary(Of String, String)   ' enthält die in FLS verfügbaren Länder und ihre CountryIds
    Public Property Log As Action(Of String)    ' Aktion, die ausgeführt wird, wenn in Logfeld geschrieben wird

    Public Shared ReadOnly Property GetLaender() As Dictionary(Of String, String)
        Get
            Return _laender_dict
        End Get
    End Property

    Public Sub New(ByVal pxhelper As ProffixHelper, ByVal flsclient As FlsConnection, ByVal myconn As ProffixConnection, ByVal importer As Importer)
        Me.flsClient = flsclient
        Me.myconn = myconn
        Me.pxhelper = pxhelper
        Me.importer = importer
    End Sub

    ' allgemeine Daten laden
    Public Function loadGeneralData() As Boolean
        Try
            If loadCountries() Then     ' wird nur in Liste in Programm geladen
                If loadMemberStates() Then ' wird in ZUS_FLSMemberStates geladen
                    If loadAircrafts() Then ' wird in ZUS_FLSAircrafts geladen
                        If loadLocations() Then ' wird in ZUS_FLSLocations geladen
                            Return True
                        Else
                            Logger.GetInstance.Log(LogLevel.Exception, "Fehler in loadLocations")
                        End If
                    Else
                        Logger.GetInstance.Log(LogLevel.Exception, "Fehler in loadAircrafts")
                    End If
                Else
                    Logger.GetInstance.Log(LogLevel.Exception, "Fehler in loadMemberStates")
                End If
            Else
                Logger.GetInstance.Log(LogLevel.Exception, "Fehler in loadCountries")
            End If

            Return False

        Catch ex As Exception
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod.Name)
            Return False
        End Try
    End Function

    '***************************************************************************Data for Proffix***************************************************************************
    ' lädt alle Orte mit ihren Ids in ZUS_FLSLocations
    Private Function loadLocations() As Boolean
        Dim sql_delete As String = String.Empty
        Dim rs_delete As New ADODB.Recordset
        Dim sql_insert As String = String.Empty
        Dim rs_insert As New ADODB.Recordset
        Dim locations As Threading.Tasks.Task(Of JArray)
        Dim fehler As String = String.Empty

        Try
            ' TODO testen, wenn noch nie etwas in Zusazttabelle stand. Hat in SGN erst funktioniert, wenn Datensatz in Laufnummern für Tabelle

            ' LaufNr holen. Wird eigentlich für pxbook.AddZusatztabelleWerte() nicht benötigt, aber pxBook.AddZusatztabelleWerte() funktioniert nicht, wenn kein Eintrag für Tabelle in Tabelle Laufnummern
            pxhelper.GetNextLaufNr("ZUS_FLSLocations")

            ' bisheriger Inhalt löschen
            sql_delete = "Delete from ZUS_FLSLocations"
            If Not myconn.getRecord(rs_delete, sql_delete, fehler) Then
                Throw New Exception("Fehler beim Löschen des Inhalts aus ZUS_FLSLocations")
            Else

                If Not pxhelper.resetLaufNr("ZUS_FLSLocations") Then
                    Throw New Exception("Fehler in resetLaufNr")
                End If

                ' Infos zu Ländern aus FLS laden
                locations = flsClient.CallAsyncAsJArray("https://test.glider-fls.ch/api/v1/locations/overview")
                locations.Wait()

                ' Infos in ZUS_FLSLocations schreiben
                For Each location As JObject In locations.Result.Children

                    If Not Proffix.GoBook.AddZusatztabelleWerte("ZUS_FLSLocations",
                        "LocationId, " +
                        "LocationName, " +
                        "IcaoCode, " +
                        "CountryName",
                            "'" + FlsHelper.GetValOrDef(location, "LocationId") + "', '" +
                            FlsHelper.GetValOrDef(location, "LocationName") + "', '" +
                            FlsHelper.GetValOrDef(location, "IcaoCode") + "', '" +
                            FlsHelper.GetValOrDef(location, "CountryName") + "'", fehler) Then
                        Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Beschreiben von ZUS_FLSLocations " + fehler)
                        Return False
                    End If
                    'If logAusfuehrlich Then
                    '    Logger.GetInstance.Log(LogLevel.Info, "Location in ZUS_FLSAircrafts geschrieben: " + location.ToString)
                    'End If
                Next
            End If

            Logger.GetInstance.Log(LogLevel.Info, locations.Result.Children.Count.ToString + "Location-Informationen erfolgreich in ZUSLocations importiert")
            Return True

        Catch ex As Exception
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + " " + ex.Message)
            Return False
        End Try
    End Function


    ' lädt Memberstates
    Private Function loadMemberStates() As Boolean
        Dim memberStatesResult As Threading.Tasks.Task(Of JArray)
        Dim sql_delete As String = String.Empty
        Dim rs_delete As New ADODB.Recordset
        Dim sql_insert As String = String.Empty
        Dim rs_insert As New ADODB.Recordset

        Dim fehler As String = String.Empty

        Try
            ' LaufNr holen. Wird eigentlich für pxbook.AddZusatztabelleWerte() nicht benötigt, aber pxBook.AddZusatztabelleWerte() funktioniert nicht, wenn kein Eintrag für Tabelle in Tabelle Laufnummern
            pxhelper.GetNextLaufNr("ZUS_FLSMemberStates")


            ' ZUS_FLSMemberStates leeren
            sql_delete = "Delete from ZUS_FLSMemberStates"
            If Not myconn.getRecord(rs_delete, sql_delete, fehler) Then
                Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Löschen des Inhalts der Tabelle ZUS_FLSMemberStates" + fehler)
            End If

            If Not pxhelper.resetLaufNr("ZUS_FLSMemberStates") Then
                Throw New Exception("Fehler in resetLaufNr")
                Return False
            End If

            ' MemberStatedaten holen
            memberStatesResult = flsClient.CallAsyncAsJArray(My.Settings.ServiceAPIMemberStates)
            memberStatesResult.Wait()

            ' alle Adressen aus FLS in ZUS_FLSMemberStates einfügen
            For Each memberState As JObject In memberStatesResult.Result.Children()

                If Not Proffix.GoBook.AddZusatztabelleWerte("ZUS_FLSMemberstates",
                                            "MemberStateId, " +
                                            "MemberStateName",
                                        "'" + memberState("MemberStateId").ToString + "', '" + _
                                        FlsHelper.GetValOrDef(memberState, "MemberStateName") + "'", fehler) Then
                    Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Beschreiben von ZUS_FLSMemberStates " + fehler)
                    Return False
                End If
                'If logAusfuehrlich Then
                '    Logger.GetInstance.Log(LogLevel.Info, "memberstate in ZUS_Memberstates geschrieben: " + memberState.ToString)
                'End If
            Next

            Logger.GetInstance.Log(LogLevel.Info, memberStatesResult.Result.Children.Count.ToString + "MemberStates erfolgreich geladen")
            Return True

        Catch ex As Exception
            Logger.GetInstance.Log(LogLevel.Exception, ex.Message)
            Return False
        End Try

    End Function

    ' füllt ZUS_FLSPersons 1 zu 1 anhand Personeninformationen aus FLS ab --> man sieht Verbindung von PersonId und Adressnr/Name auch in Proffix
    Public Function importPersons() As Boolean
        Dim sql_delete As String = String.Empty
        Dim rs_delete As New ADODB.Recordset
        Dim sql_insert As String = String.Empty
        Dim rs_insert As New ADODB.Recordset
        Dim fehler As String = String.Empty
        Dim personResult As Threading.Tasks.Task(Of JArray)
        Try

            ' ZUS_FLSPersons leeren
            sql_delete = "Delete from ZUS_FLSPersons"
            If Not myconn.getRecord(rs_delete, sql_delete, fehler) Then
                Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Löschen des Inhalts der Tabelle ZUS_FLSPersons" + fehler)
                Return False
            End If

            If Not pxhelper.resetLaufNr("ZUS_FLSPersons") Then
                Logger.GetInstance.Log(LogLevel.Exception, "Fehler in resetLaufNr")
                Return False
            End If

            ' Alle ungelöschten FLS Adressen holen, wenn nicht bereits in Fkt mitgegeben
            personResult = flsClient.CallAsyncAsJArray(My.Settings.ServiceAPIModifiedPersonFullDetailsMethod + DateTime.MinValue.ToString("yyyy-MM-dd"))
            personResult.Wait()

            ' alle Adressen aus FLS in ZUS_FLSPersons einfügen (damit Verknüpfung PersonId AdressNr Name auch in Proffix sichtbar
            For Each person As JObject In personResult.Result.Children()

                ' nicht über pxBook da zu langsam bei so vielen DS
                sql_insert = "insert into ZUS_FLSPersons (" +
                    "PersonId, " +
                    "Name, " +
                    "Vorname, " +
                    "Ort, " +
                    "VereinsmitgliedNrAdressNr, " +
                    "LaufNr, " +
                    "ImportNr, " +
                    "ErstelltAm, " +
                    "ErstelltVon, " +
                    "GeaendertAm, " +
                    "GeaendertVon, " +
                    "Geaendert, " +
                    "Exportiert" +
                            ") values (" +
                                "'" + person("PersonId").ToString + "', " +
                                FlsHelper.GetValOrDefString(person, "Lastname") + ", " +
                                FlsHelper.GetValOrDefString(person, "Firstname") + ", " +
                                FlsHelper.GetValOrDefString(person, "City") + ", " +
                                FlsHelper.GetValOrDefString(person, "ClubRelatedPersonDetails.MemberNumber") + ", " +
                                pxhelper.GetNextLaufNr("ZUS_FLSPersons").ToString + ", " +
                                "0, " +
                                "'" + Now.ToString(pxhelper.dateTimeFormat) + "', " +
                                "'" + Assembly.GetExecutingAssembly().GetName.Name + "', " +
                                "'" + Now.ToString(pxhelper.dateTimeFormat) + "', " +
                                "'" + Assembly.GetExecutingAssembly().GetName.Name + "', " +
                                "1, 0" +
                        ")"
                If Not myconn.getRecord(rs_insert, sql_insert, fehler) Then
                    Logger.GetInstance.Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod.Name + " " + fehler + sql_insert)
                    Return False
                End If
                'If logAusfuehrlich Then
                '    Logger.GetInstance.Log(LogLevel.Info, "person in ZUS_FLSPersons geschrieben: " + person.ToString)
                'End If
            Next

            Logger.GetInstance.Log(LogLevel.Info, personResult.Result.Children.Count.ToString + " FLS Adressen wurden in ZUS_Persons geladen")

            Return True

        Catch ex As Exception
            Logger.GetInstance.Log(LogLevel.Exception, ex.Message)
            Return False
        End Try
    End Function

    ' lädt Aircrafts
    Private Function loadAircrafts() As Boolean
        Dim aircraftsResult As Threading.Tasks.Task(Of JArray)
        Dim sql_delete As String = String.Empty
        Dim rs_delete As New ADODB.Recordset
        Dim sql_insert As String = String.Empty
        Dim rs_insert As New ADODB.Recordset

        Dim fehler As String = String.Empty

        Try
            ' LaufNr holen. Wird eigentlich für pxbook.AddZusatztabelleWerte() nicht benötigt, aber pxBook.AddZusatztabelleWerte() funktioniert nicht, wenn kein Eintrag für Tabelle in Tabelle Laufnummern
            If pxhelper.GetNextLaufNr("ZUS_FLSAircrafts") = Nothing Then
                Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Laden der nächsten LaufNr für ZUS_FLSAircrafts")
            End If

            ' ZUS_FLSMemberStates leeren
            sql_delete = "Delete from ZUS_FLSAircrafts"
            If Not myconn.getRecord(rs_delete, sql_delete, fehler) Then
                Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Löschen des Inhalts der Tabelle ZUS_FLSAircrafts " + fehler)
                Return False
            End If

            ' LaufNr auf 1 setzen, da Daten aus Tabelle gelöscht wurden
            If Not pxhelper.resetLaufNr("ZUS_FLSAircrafts") Then
                Throw New Exception("Fehler in resetLaufNr")
                Return False
            End If

            ' MemberStatedaten holen
            aircraftsResult = flsClient.CallAsyncAsJArray(My.Settings.ServiceAPIAircraftsMethod)
            aircraftsResult.Wait()


            ' alle Adressen aus FLS in ZUS_FLSMemberStates einfügen
            For Each aircraft As JObject In aircraftsResult.Result.Children()

                If Not Proffix.GoBook.AddZusatztabelleWerte("ZUS_FLSAircrafts",
                                            "AircraftId, " +
                                            "Immatriculation",
                                        "'" + aircraft("AircraftId").ToString + "', '" + _
                                        FlsHelper.GetValOrDef(aircraft, "Immatriculation") + "'", fehler) Then
                    Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Beschreiben von ZUS_FLSAircrafts" + fehler)
                    Return False
                Else
                    'If logAusfuehrlich Then
                    '    Logger.GetInstance.Log(LogLevel.Info, "Aircraft in ZUS_FLSAircrafts geschrieben: " + aircraft.ToString)
                    'End If
                End If
            Next

            Logger.GetInstance.Log(LogLevel.Info, aircraftsResult.Result.Children.Count.ToString + " Aircrafts erfolgreich geladen")
            Return True

        Catch ex As Exception
            Logger.GetInstance.Log(LogLevel.Exception, "1111 " & ex.Message)
            Return False
        End Try

    End Function

    '****************************************************************only stored in this application************************************************************************
    ' lädt die in FLS vorhandenen Länder (FLS will CountryId, Proffix will Länderkürzel)
    Private Function loadCountries() As Boolean
        Dim laender As Threading.Tasks.Task(Of JArray)
        Try

            ' Länder auslesen und in Dictionnary abspeichern
            laender = flsClient.CallAsyncAsJArray(My.Settings.ServiceAPICountriesMethod)
            laender.Wait()
            For Each land In laender.Result.Children()
                ' wenn Land noch nicht enthalten (damit kein Fehler beim 2. Synchronisieren)
                If Not _laender_dict.ContainsKey(land("CountryCode").ToString) Then
                    _laender_dict.Add(land("CountryCode").ToString, land("CountryId").ToString)
                End If
            Next

            Return True

        Catch ex As Exception
            Logger.GetInstance.Log(LogLevel.Critical, "Fehler beim Laden der Länder" + ex.Message)
            Return False
        End Try
    End Function



    ' ruft Deliveries + Flights aus FLS erneut ab (werden beim nächsten Mal wieder geliefert werden) und löscht für diese DeliveryIds bzw. FlightIds alle bereits bestehenden Daten, damit nicht 2x ein LS erstellt wird bzw. Flugdaten 2x in ZUS_flight importiert werden
    Public Function deleteIncompleteData() As Boolean
        Try

            If Not deleteIncompleteDeliveries() Then
                Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Löschen unvollständiger Lieferscheindaten. Möglicherweise sind Lieferscheine vorhanden, die unvollständig sind.")
                Throw New Exception("Fehler in " + MethodBase.GetCurrentMethod.Name)
                Return False
            End If

            ' unvollständige Flugdaten löschen
            If Not deleteIncompleteFlights() Then
                Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Löschen unvollständiger Flugdaten.")
                Throw New Exception("Fehler in deleteIncompleteFlights()")
                Return False
            End If

            Return True

        Catch ex As Exception
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod.Name + " " + ex.Message)
            Return False
        End Try
    End Function

    Private Function deleteIncompleteDeliveries() As Boolean
        Dim notProcessedDeliveries As Threading.Tasks.Task(Of JArray) ' noch zu erstellende Deliveries aus FLS
        Try
            ' alle Deliveries die zu verrechnen sind aus FLS herunterladen
            notProcessedDeliveries = flsClient.CallAsyncAsJArray(My.Settings.ServiceAPIDeliveriesNotProcessedMethod)
            notProcessedDeliveries.Wait()

            Logger.GetInstance.Log(LogLevel.Info, "Allfällige Daten für noch nicht vollständig verrechnete Lieferscheine werden gelöscht.")

            ' für alle DeliveryIds, die beim nächsten Mal wieder geliefert werden
            For Each delivery As JObject In notProcessedDeliveries.Result.Children()
                ' von abgebrochenen Lieferscheinimporten vorhandene Daten für die MasterFlightId löschen
                If Not pxhelper.deleteIncompleteDeliveryData(delivery("DeliveryId").ToString) Then
                    Return False
                End If
            Next
        Catch ex As Exception
            Return False
        End Try
        Return True
    End Function

    ' löscht alle Daten zu FlightIds die beim nächsten Mal nochmals kommen werden
    Private Function deleteIncompleteFlights() As Boolean
        Dim modifiedFlights As List(Of JObject)
        Dim lastChangeDate As Date = DateTime.MinValue
        Try
            Logger.GetInstance.Log(LogLevel.Info, "Allfällige Daten für noch nicht vollständig importierte Flugdaten werden gelöscht.")

            ' Flüge laden, die seit letztem Flugdatenimport verändert/erstellt wurden
            modifiedFlights = importer.loadModifiedFlights()

             For Each flight As JObject In modifiedFlights

                ' Daten für diesen Flug löschen
                If Not pxhelper.deleteIncompleteFlightData(flight("FlightId").ToString) Then
                    Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Löschen der Daten für FlightId " + flight("FlightId").ToString)
                    Return False
                End If
                'End If
                'End If
            Next
            Return True

        Catch ex As Exception
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Löschen der Flugdaten seit " + importer.lastFlightImport.ToString("yyyy-MM-dd") + " " + ex.Message)
            Return False
        End Try
    End Function



End Class

