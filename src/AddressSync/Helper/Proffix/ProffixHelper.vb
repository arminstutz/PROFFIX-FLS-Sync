Imports pxBook
Imports SMC.Lib
Imports Newtonsoft.Json.Linq
Imports System.Reflection

''' <summary>
''' Eine Helper Klasse die sich um Aktionen rund um PROFFIX kümmert
''' Hauptsächlich Funktionalitäten die die pxBook Schnitstelle nicht zur verfügung stellt
''' </summary>
Public Class ProffixHelper

    Private Property MyConn As ProffixConnection
    ' Die Liste wird einmal pro Programmausführung gefüllt und als Zwischenspeicher benutzt
    Private Shared ExistingFields As New Dictionary(Of String, Boolean)
    Public Property dateformat As String
    Public Property dateTimeFormat As String

    Public Sub New()
    End Sub

    Public Sub New(ByVal myconn As ProffixConnection)
        Me.MyConn = myconn
        dateformat = DateServerFormat()
        dateTimeFormat = dateformat + " HH:mm:ss.fff"
    End Sub


    '*************************************************************************DATEN AUS PROFFIX LADEN***********************************************************************
    ''' <summary>
    ''' Laden des letzten Datums des synctypes (AdressSync, ArticleExport...)
    ''' </summary>
    ''' <returns>Das letzte Synchronisationsdatum</returns>
    Public Function GetDate(ByVal synctype As String) As DateTime
        Dim sql As String = String.Empty
        Dim rs As New ADODB.Recordset
        Dim fehler As String = String.Empty

        ' Wert aus DB holen
        sql = "Select SyncDate from ZUS_FLSSyncDate where SyncType = '" + synctype + "' order by SyncDate desc"
        If Not MyConn.getRecord(rs, sql, fehler) Then
            Logger.GetInstance().Log(LogLevel.Exception, fehler)
        Else
            If rs.EOF Then
                ' es konnte kein LastSync geladen werden, da für den angegebenen synctype noch kein Datensatz in ZUS_FLSSync erstellt wurde
                '--> Defaultdate
                Return DateTime.Parse("2017-02-02 08:00:00.000")
                'Return DateTime.MinValue
            End If
        End If

        ' gibt lastSync des entsprechenden syncType zurück
        Return DateTime.Parse(rs.Fields("SyncDate").Value.ToString())
    End Function

    ' lädt die in der DB definierten Standardwerte für eine Adresse
    Public Function GetPXAdressDefaultValues() As ADODB.Recordset
        Dim rs As New ADODB.Recordset
        Dim sql As String = String.Empty
        Dim fehler As String = String.Empty
        Dim MyConn = New ProffixConnection

        sql = "Select * from adr_adressdef"
        MyConn.getRecord(rs, sql, fehler)
        If Not String.IsNullOrEmpty(fehler) Or rs.EOF Then
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Laden der Defaultwerte für Adressen")
            Return Nothing
        Else
            Return rs
        End If
    End Function

    ' gibt Liste mit personIds der in Proffix gelöschten Adressen zurück
    Public Function GetDeletedAddresses(ByVal sinceDate As DateTime) As List(Of String)
        Dim rs As New ADODB.Recordset
        Dim sql As String
        Dim fehler As String = ""
        Dim personId_list As New List(Of String)
        Try
            sql = "Select Z_FLSPersonId from adr_adressen where geloescht = 1 and geaendertAm > '" + sinceDate.ToString(dateTimeFormat) + "'"
            If Not MyConn.getRecord(rs, sql, fehler) Then
                Logger.GetInstance().Log(LogLevel.Exception, fehler)
            End If

            While Not rs.EOF

                ' es interessieren nur Adressen, die bereits PersonId haben (wenn noch keine = noch nie mit FLS synchronisiert --> msus auch nicht in FLS gelöscht werden)
                If rs.Fields("Z_FLSPersonId").Value.ToString <> "" Then

                    personId_list.Add(rs.Fields("Z_FLSPersonId").Value.ToString)
                End If
                rs.MoveNext()
            End While
            Return personId_list
        Catch
            Logger.GetInstance().Log("Fehler bei GetGeloeschteAddresses aus Proffix. Möglicher Grund: LastSync = Nothing")
            Return Nothing
        End Try
    End Function

    ' gibt die PersonId zurück, welche für eine AdressNr in PX gilt
    Public Function GetPersonId(ByVal adressnr As String) As String
        Dim sql As String = String.Empty
        Dim rs As New ADODB.Recordset
        Dim fehler As String = String.Empty

        sql = "select Z_FLSPersonId from adr_adressen where adressnradr = " + adressnr
        If Not MyConn.getRecord(rs, sql, fehler) Then
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod.Name + " " + fehler)
            Return Nothing
        End If

        If rs.EOF Then
            Return ""
        Else
            Return rs.Fields("Z_FLSPersonId").Value.ToString
        End If
    End Function

    'sucht anhand einer PersonId die dazugehörige AdressNr
    Public Function GetAdressNr(ByVal personId As String) As String
        Dim sql As String = String.Empty
        Dim rs As New ADODB.Recordset
        Dim fehler As String = String.Empty
        Try
            sql = "select adressNrADR from adr_adressen where Z_FLSPersonId = '" + personId + "'"
            If Not MyConn.getRecord(rs, sql, fehler) Then
                Throw New Exception("Fehler in " + MethodBase.GetCurrentMethod.Name + " " + fehler)
            End If

            If rs.EOF Then
                Throw New Exception("Für die PersonId " + personId + "  wurde keine Adresse gefunden")
            End If

            Return rs.Fields("adressNrADR").Value.ToString

        Catch ex As Exception
            Throw New Exception(ex.Message)
            Return Nothing
        End Try
    End Function

    ' holt die nächste LaufNr der angegebenen Tabelle und erhöht den Wert in Proffix
    Public Function GetNextLaufNr(ByVal table As String) As Integer
        Dim sql As String = String.Empty
        Dim rs_select As New ADODB.Recordset
        Dim rs_update As New ADODB.Recordset
        Dim fehler As String = String.Empty
        Dim nextNr As Integer

        Try

            ' momentane LaufNr holen
            sql = "select laufnr from laufnummern where tabelle = '" + table + "'"
            If Not MyConn.getRecord(rs_select, sql, fehler) Then
                Logger.GetInstance.Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod.Name)
                Return Nothing
            End If

            ' es wurde keine Zahl geliefert, da, wenn noch nichts in Zusatztabelle geschrieben worden ist, gibt es in Tabelle Laufnummern noch keinen Eintrag für die Tabelle --> 1 nehmen
            If rs_select.EOF Then
                nextNr = 1
                sql = "insert into laufnummern (laufnr, tabelle) values (1, '" + table + "')"
                If Not MyConn.getRecord(rs_update, sql, fehler) Then
                    Logger.GetInstance.Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod.Name + " " + fehler)
                    Return Nothing
                End If

                'es wurde eine Zahl geliefert
            Else
                ' nächste LaufNr= + 1
                nextNr = CInt(rs_select.Fields("LaufNr").Value) + 1

                ' updatet die LaufNr in Proffix
                sql = "update laufnummern set laufnr = " + nextNr.ToString + "where tabelle = '" + table + "'"
                If Not MyConn.getRecord(rs_update, sql, fehler) Then
                    Logger.GetInstance.Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod.Name + " " + fehler)
                    Return Nothing
                End If
            End If


            '  Logger.GetInstance.Log(LogLevel.Info, "LaufNr:" & nextNr)

            Return nextNr

        Catch ex As Exception
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod.Name + " " + fehler + " " + ex.Message)
            Return Nothing
        End Try
    End Function



    '*************************************************************************DATEN IN PROFFIX schreiben***************************************************************
    ''' <summary>
    ''' Speichern des letzen Synchronisationsdatums
    ''' </summary>
    ''' <param name="lastDate">Das Datum</param>
    Public Function SetDate(ByVal lastDate As DateTime, ByVal synctype As String, ByRef fehler As String) As Boolean

        Dim rs_lastSync As New ADODB.Recordset
        Dim sql As String = ""
        Try
            ' LastDate updaten (nicht über pxBook AddZusatztabelleWerte(), da es Datum nicht richtig frisst)
            sql = "insert into ZUS_FLSSyncDate " + _
                "(SyncId, " +
                "SyncDate, " +
                "SyncType, " +
                "LaufNr, " +
                "ImportNr, " +
                "erstelltAm, " +
                "erstelltVon, " +
                "geaendertAm, " +
                "geaendertVon, " +
                "geaendert, " +
                "exportiert" +
                ") values (" +
            "(select case (select count(*) from zus_FLSSyncDate) when 0 then 1 else (select max(syncid) from ZUS_FLSSyncDate) + 1 end), '" +
            lastDate.ToString(dateTimeFormat) + "', '" +
            synctype + "', " +
            GetNextLaufNr("ZUS_FLSSyncDate").ToString + ", " +
            "0, '" +
            Now.ToString(dateTimeFormat) + "', '" +
            Assembly.GetExecutingAssembly().GetName.Name + "', '" +
            Now.ToString(dateTimeFormat) + "', '" +
            Assembly.GetExecutingAssembly().GetName.Name + "', " +
            "1, 0)"

            If Not MyConn.getRecord(rs_lastSync, sql, fehler) Then
                Logger.GetInstance.Log("Fehler in " + MethodBase.GetCurrentMethod.Name + " " + fehler)
                Return False
            End If
            Return True
        Catch ex As Exception
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod.Name + " " + ex.Message)
        End Try
    End Function

    ' löscht bereits vorhandene Datensätze für die zu importierende DeliveryId (Überreste von vorgängigen unvollständigen Importen)
    Public Function deleteIncompleteDeliveryData(ByVal DeliveryId As String) As Boolean
        Dim rs_pos As New ADODB.Recordset
        Dim sql_pos As String = String.Empty
        Dim rs_doknr As New ADODB.Recordset
        Dim sql_doknr As String = String.Empty
        Dim rs_delete As New ADODB.Recordset
        Dim sql_delete As String = String.Empty
        Dim rs_dokflightlink As New ADODB.Recordset
        Dim sql_dokflightlink As String = String.Empty
        Dim fehler As String = String.Empty

        Logger.GetInstance.Log(LogLevel.Info, "Die Daten für die DeliveryId " + DeliveryId + " werden aus Proffix gelöscht.")

        '****************************************************************DocPos löschen************************************************************************
        sql_pos = "Delete from AUF_DokumentPos where Z_DeliveryId = '" + DeliveryId + "'"
        If Not MyConn.getRecord(rs_pos, sql_pos, fehler) Then
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Löschen der Positionen. DeliveryId: " + DeliveryId + fehler)
            Return False
        End If

        '**********************************************************************vervaiste Docs löschen***********************************************************************
        ' DokNr ermitteln, für die keine DocPos mehr existieren 
        sql_doknr = "select auf_dokumente.dokumentnrauf as doknr " +
                    "from auf_dokumente left join auf_dokumentpos on auf_dokumente.dokumentnrauf = auf_dokumentpos.dokumentnrauf " +
                    "where auf_dokumente.doktypAUF = 'flsls' and auf_dokumentpos.artikelnrLAG is null and auf_dokumentpos.Z_DeliveryId is null"
        If Not MyConn.getRecord(rs_doknr, sql_doknr, fehler) Then
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Laden der verwaisten Doks" + fehler)
            Return False
        End If

        ' für jede der verwaisten DocNr...
        While Not rs_doknr.EOF

            ' Doc mit DokNr löschen
            sql_delete = "Delete from auf_dokumente where dokumentnrauf = " + rs_doknr.Fields("doknr").Value.ToString
            If Not MyConn.getRecord(rs_delete, sql_delete, fehler) Then
                Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Löschen verwaister Doks" + fehler)
                Return False
            End If

            ' Verknüpfung Dok Flight löschen
            sql_dokflightlink = "delete from zus_dokflightlink where dokumentnr = " + rs_doknr.Fields("doknr").Value.ToString
            If Not MyConn.getRecord(rs_dokflightlink, sql_dokflightlink, fehler) Then
                Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Löschen auf ZUS_DokFlightLink anhand DokNr:" + rs_doknr.Fields("doknr").Value.ToString + fehler)
                Return False
            End If

            rs_doknr.MoveNext()
        End While

        ' Verknüpfung Dok Flight löschen
        sql_dokflightlink = "delete from zus_dokflightlink where deliveryId = '" + DeliveryId + "'"
        If Not MyConn.getRecord(rs_dokflightlink, sql_dokflightlink, fehler) Then
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Löschen auf ZUS_DokFlightLink anhand DeliveryId:" + DeliveryId + fehler)
            Return False
        End If

        Return True
    End Function

    ' löscht bereits vorhandene Datensätze für die zu importierende FlightId (Überreste von vorgängigen unvollständigen Importen)
    Public Function deleteIncompleteFlightData(ByVal flightid As String) As Boolean
        Dim rs As New ADODB.Recordset
        Dim sql As String = String.Empty
        'Dim rs_dokflightlink As New ADODB.Recordset
        'Dim sql_dokflightlink As String = String.Empty
        Dim fehler As String = String.Empty

        Logger.GetInstance.Log(LogLevel.Info, "Die Daten für die FlightId " + flightid + " werden aus Proffix gelöscht.")

        sql = "Delete from ZUS_Flights where FlightId = '" + flightid + "'"
        If Not MyConn.getRecord(rs, sql, fehler) Then
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + " FlightId: " + flightid)
            Return False
        End If
        Return True
    End Function

    ' setzt alle Artikel die zur FLS Gruppe gehören den Negativbestand auf 1, damit bei AddDokument kein Rückstand verwaltet werden muss
    Public Function SetNegativBestand(ByRef response As String) As Boolean
        Dim rs_negativbestand As New ADODB.Recordset
        Dim fehler As String = String.Empty


        Dim sql As String = "Update LAG_Artikel set negativbestand = 1 where gruppelag = 'FLS'"
        MyConn.getRecord(rs_negativbestand, sql, fehler)
        If Not String.IsNullOrEmpty(fehler) Then
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Negativbestand setzen: " + fehler)
            response = "Negativbestand für FLS-Artikel konnte in Proffix nicht gesetzt werden"
            Return False
        End If

        Return True
    End Function

    ' setzt bei der Adresse, wo das Zusatzfeld FLSPersonId = personId ist, gelöscht = 1
    Public Function SetPXAddressAsGeloescht(ByVal personId As String) As Boolean
        Dim rs As New ADODB.Recordset
        Dim sql As String
        Dim fehler As String = ""

        sql = "Update adr_adressen set " +
            "geloescht = 1, " + _
             "geaendertVon = '" + Assembly.GetExecutingAssembly().GetName.Name + "', " +
             "geaendertAm = '" + Now.ToString(dateformat + " HH:mm:ss") + "' " + _
           "where Z_FLSPersonId = '" & personId & "'"

        If Not MyConn.getRecord(rs, sql, fehler) Then
            Logger.GetInstance().Log(LogLevel.Exception, fehler)
            Return False
        Else
            Logger.GetInstance.Log(LogLevel.Info, "deleted: " & personId)
            Return True
        End If
    End Function



    ''' <summary>
    ''' Setzt ein den Wert eines Zusatzfelds einer Adresse
    ''' </summary>
    ''' <param name="source">Die Adresse auf der die Zusatzfelder gesetzt werden</param>
    ''' <param name="name">Der Name des Zusatzfeldes</param>
    ''' <param name="value">Der Wert des Zusatzfeldes</param>
    ''' <returns>Die geänderte Adresse</returns>
    Public Shared Function SetZusatzFelder(ByVal source As pxKommunikation.pxAdressen, ByVal name As String, ByVal listenName As String, ByVal typ As String, ByVal defaultValue As String, ByVal value As String) As pxKommunikation.pxAdressen

        If String.IsNullOrEmpty(source.ZusatzfelderListe) Then source.ZusatzfelderListe = String.Empty
        If String.IsNullOrEmpty(source.ZusatzfelderWerte) Then source.ZusatzfelderWerte = String.Empty

        ' Wert umwandeln
        If value = "True" Then
            value = "1"
        ElseIf value = "False" Then
            value = "0"
        End If

        '************************************************************************* wenn die Eigenschaft noch nicht vorhanden ist --> anhängen *************************************************
        If (Array.IndexOf(source.ZusatzfelderListe.Split("¿".ToCharArray), name) < 0) Then
            ' wenn bereits etwas im String steht --> "¿" anhängen
            If source.ZusatzfelderListe.Length > 0 Then
                source.ZusatzfelderListe += "¿"
                source.ZusatzfelderWerte += "¿"
            End If

            ' Eigenschaftenname + Defaultwert einfügen
            source.ZusatzfelderListe += listenName
            source.ZusatzfelderWerte += value

            '************************************************************* wenn Eigenschaft im String bereits vorhanden --> Wert überschreiben **************************************************
        Else
            ' an welcher Position soll Wert eingefügt werden?
            Dim insertAt As Integer = Array.IndexOf(source.ZusatzfelderListe.Split("¿".ToCharArray()), name)

            ' Werte aus String in Array speichern
            Dim arr_ZFWerte = source.ZusatzfelderWerte.Split("¿".ToCharArray())

            ' An Position insertAt Wert ersetzen
            arr_ZFWerte(insertAt) = value

            ' Werte aus Array in String speichern
            source.ZusatzfelderWerte = String.Join("¿", arr_ZFWerte)
        End If

        Return source
    End Function

    ' fügt die Zusatzfelder im JSON hinzu, die ein Datum enthalten (funktioniert nicht über Gobook.AddAdresse)
    Public Function SetDatumsZusatzfelderToPXAdresse(ByVal adresse As pxBook.pxKommunikation.pxAdressen, ByVal person As JObject, ByRef fehler As String) As Boolean
        Dim sql As String = String.Empty
        Dim rs As New ADODB.Recordset

        sql = "update ADR_Adressen set " + _
            "Z_Medical1GueltigBis = " + If(person("MedicalClass1ExpireDate") IsNot Nothing, "'" + DateTime.Parse(person("MedicalClass1ExpireDate").ToString).ToString(dateformat) + "'", "null") + ", " + _
            "Z_Medical2GueltigBis = " + If(person("MedicalClass2ExpireDate") IsNot Nothing, "'" + DateTime.Parse(person("MedicalClass2ExpireDate").ToString).ToString(dateformat) + "'", "null") + ", " + _
            "Z_MedicalLAPLGueltigBis = " + If(person("MedicalLaplExpireDate") IsNot Nothing, "'" + DateTime.Parse(person("MedicalLaplExpireDate").ToString).ToString(dateformat) + "'", "null") + ", " + _
           "Z_SegelfluglehrerlizenzGueltigBis = " + If(person("GliderInstructorLicenceExpireDate") IsNot Nothing, "'" + DateTime.Parse(person("GliderInstructorLicenceExpireDate").ToString).ToString(dateformat) + "'", "null") + _
           " where adressNrADR = " + adresse.AdressNr.ToString

        If Not MyConn.getRecord(rs, sql, fehler) Then
            Return False
        End If
        Return True
    End Function

    ' befüllt ZUS_DokFlightLink
    Public Function SetDokFlightLink(ByVal delivery As JObject, ByVal dokNr As Integer) As Boolean
        Dim sql As String = String.Empty
        Dim rs As New ADODB.Recordset
        Dim fehler As String = String.Empty

        'prüfen, ob für diese DeliveryId bereits ein DS besteht
        sql = "Select FlightId from ZUS_DokFlightLink where DeliveryId = '" + delivery("DeliveryId").ToString + "'"
        If Not MyConn.getRecord(rs, sql, fehler) Then
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod.Name + " " + fehler)
            Return False
        End If

        ' nur wenn noch kein DS besteht, einen erstellen
        If rs.EOF Then
            If Not Proffix.GoBook.AddZusatztabelleWerte("ZUS_DokFlightLink",
                                                 "DokumentNr, " +
                                                 "DeliveryId, " +
                                                 "FlightId",
                                                     dokNr.ToString + "," +
                                                     "'" + delivery("DeliveryId").ToString + "', " +
                                                     "'" + GetValOrDef(delivery, "FlightInformation.FlightId"),
                                                fehler) Then
                Logger.GetInstance.Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod.Name + fehler)
                Return False
            End If
        End If
        Return True

    End Function

    ' insert the masterflight (Glider or Motorflight) in ZUS_flights
    Public Function insertMasterFlight(ByVal flight As JObject) As Boolean
        Dim rs As New ADODB.Recordset
        Dim sql As String = String.Empty
        Dim fehler As String = String.Empty
        Dim passengersPersonIds As String = String.Empty
        Dim passengersName As String = String.Empty
        Dim laufnr As String = String.Empty

        If Not CreateRecord(flight("FlightId").ToString, laufnr) Then
            Return False
        End If

        ' String für Values für Passengers zusammensetzen
        If GetValOrDef(flight, "Passengers") <> "" Then
            For Each passenger As JObject In flight("Passengers")
                passengersPersonIds += GetValOrDef(passenger, "PersonId")
                passengersPersonIds += "¿"
                passengersName += GetValOrDef(passenger, "Firstname")
                passengersName += " "
                passengersName += GetValOrDef(passenger, "Lastname")
                passengersName += "; "
            Next
            ' leztes Zeichen ¿ abschneiden
            passengersPersonIds = passengersPersonIds.Substring(0, passengersPersonIds.Length - 2)
            passengersName = passengersName.Substring(0, passengersName.Length - 2)
        End If

        sql = "update ZUS_flights set " +
         If(passengersPersonIds <> "", "PassengersPersonIds = '" + passengersPersonIds + "',", "") +
         If(passengersName <> "", "PassengersNameString = '" + passengersName + "', ", "") +
         "StartType = " + GetValOrDefString(flight, "StartType") + ", " +
         "CoPilotPersonId = " + GetValOrDefString(flight, "CoPilot.PersonId") + ", " +
         "ObserverPersonId = " + GetValOrDefString(flight, "Observer.PersonId") + ", " +
         "InvoiceRecipientPersonId = " + GetValOrDefString(flight, "InvoiceRecipient.PersonId") + ", " +
         "NrOfLdgsOnStartLocation = " + GetValOrDefInteger(flight, "NrOfLdgsOnStartLocation") + ", " +
         getGeneralFlightString(flight, "") +
         " where laufnr = " + laufnr
        If Not MyConn.getRecord(rs, sql, fehler) Then
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Updaten in ZUS_Flights flightId = " + flight("FlightId").ToString)
            Return False
        End If

        Return True
    End Function

    ' insert the Towflight in ZUS_Flights
    Public Function insertTowFlight(ByVal flight As JObject) As Boolean
        Dim rs As New ADODB.Recordset
        Dim sql As String = String.Empty
        Dim fehler As String = String.Empty
        Dim laufnr As String = String.Empty

        Logger.GetInstance.Log("Flugdaten zum Schleppflug werden importiert TowFlightId: " + GetValOrDef(flight, "TowFlightFlightId") + " FlightId: " + flight("FlightId").ToString)

        If Not CreateRecord(flight("FlightId").ToString, laufnr) Then
            Return False
        End If

        sql = "update ZUS_flights set " +
        "TowFlightFlightId = " + GetValOrDefString(flight, "TowFlightFlightId") + ", " +
        getGeneralFlightString(flight, "TowFlight") +
        " where laufnr = " + laufnr
        If Not MyConn.getRecord(rs, sql, fehler) Then
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Updaten in ZUS_Flights flightId = " + flight("FlightId").ToString)
            Return False
        End If

        Return True
    End Function

    '' fügt der ZUS_DokFlightLink die TowFlightFlightId hinzu (ZUS_DokFlightLink wird mit Delivery abgefüllt, Delivery kennt aber die TowFlightFlightId nicht --> hier erst abfüllen)
    'Public Function updateDokFlightLink(ByVal flightid As String, ByVal towflightflightid As String) As Boolean
    '    Dim sql As String = String.Empty
    '    Dim rs As New ADODB.Recordset
    '    Dim fehler As String = String.Empty

    '    sql = "update zus_dokflightlink set towflightflightid = ' " & towflightflightid & "' where flightid = '" & flightid & "'"
    '    If Not MyConn.getRecord(rs, sql, fehler) Then
    '        Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Updaten in ZUS_DokFlightLink FlightId: " & flightid & " TowFlightFlightId: " & towflightflightid)
    '        Return False
    '    End If
    '    Return True
    'End Function

    ' creates a record for a flight in ZUS_Flights
    Private Function CreateRecord(ByVal flightid As String, ByRef laufnr As String) As Boolean
        Try

            Dim rs As New ADODB.Recordset
            Dim sql As String = String.Empty
            Dim fehler As String = String.Empty
            ' LaufNr ermitteln --> abfangen wenn nicht ermittelt werden konnte
            laufnr = GetNextLaufNr("ZUS_Flights").ToString
            If laufnr = "" Then
                Logger.GetInstance.Log(LogLevel.Exception, "Es konnte keine LaufNr ermittelt werden für ZUS_Flights")
            End If

            sql = "insert into zus_Flights (" +
                                  "ID, " +
                                  "FlightId, " +
                                  "LaufNr, " +
                                  "ImportNr, " +
                                  "geaendert, " +
                                  "exportiert, " +
                                  "erstelltam, " +
                                  "erstelltvon, " +
                                  "geaendertam, " +
                                  "geaendertvon" +
                              ") values " +
                                      "(" + laufnr + ", '" + flightid + "', " +
                                      laufnr.ToString + ", " +
                                      "0, " +
                                      "1, " +
                                      "0, " +
                                      "'" + Now.ToString(dateformat + " HH:mm:ss") + "', " +
                                      "'" + Assembly.GetExecutingAssembly().GetName.Name + "', " +
                                      "'" + Now.ToString(dateformat + " HH:mm:ss") + "', " +
                                      "'" + Assembly.GetExecutingAssembly().GetName.Name + "')"
            If Not MyConn.getRecord(rs, sql, fehler) Then
                Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Insert in ZUS_Flights flightId = " + flightid)
                Return False
            End If
            Return True

        Catch ex As Exception
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod.Name + " " + ex.Message)
            Return False
        End Try

    End Function

    ' erstellt einen String für die Felder, die für Glider sowie TowFlight gleich sind
    Private Function getGeneralFlightString(ByVal flight As JObject, ByVal prefix As String) As String
        Try
            Return "" +
                "AircraftImmatriculation = " + GetValOrDefString(flight, prefix + "AircraftImmatriculation") + ", " +
                "PilotPersonId = " + GetValOrDefString(flight, prefix + "Pilot.PersonId") + ", " +
                "InstructorPersonId = " + GetValOrDefString(flight, prefix + "Instructor.PersonId") + ", " +
                "FlightComment = " + GetValOrDefString(flight, prefix + "FlightComment") + ", " +
                "AirState = " + GetValOrDefString(flight, prefix + "AirState") + ", " +
                "FlightTypeName = " + GetValOrDefString(flight, prefix + "FlightTypeName") + ", " +
                "FlightTypeCode = " + GetValOrDefString(flight, prefix + "FlightTypeCode") + ", " +
                "StartLocationIcaoCode = " + GetValOrDefString(flight, prefix + "StartLocation.IcaoCode") + ", " +
                "LdgLocationIcaoCode = " + GetValOrDefString(flight, prefix + "LdgLocation.IcaoCode") + ", " +
                "OutboundRoute = " + GetValOrDefString(flight, prefix + "OutboundRoute") + ", " +
                "InboundRoute = " + GetValOrDefString(flight, prefix + "InboundRoute") + ", " +
                "NrOfLdgs = " + GetValOrDefInteger(flight, prefix + "NrOfLdgs") + ", " +
                "NoStartTimeInformation = " + GetValOrDefBoolean(flight, prefix + "NoStartTimeInformation") + ", " +
                "NoLdgTimeInformation = " + GetValOrDefBoolean(flight, prefix + "NoLdgTimeInformation") + ", " +
                "LdgDateTime = " + GetValOrDefDateTime(flight, prefix + "LdgDateTime", dateformat) + ", " +
                "StartDateTime = " + GetValOrDefDateTime(flight, prefix + "StartDateTime", dateformat) + ", " +
                "FlightDuration = " + If(GetValOrDef(flight, prefix + "FlightDuration") = "", "NULL", "'" + DateTime.Parse(flight(prefix + "FlightDuration").ToString).ToString("HH:mm:ss") + "'")

        Catch ex As Exception
            Throw New Exception(ex.Message)
        End Try

    End Function







    Friend Function InFehlertabelleSchreiben(ByVal delivery As JObject, ByVal fehler As String) As Boolean
        Dim rs_vorhanden As New ADODB.Recordset
        Dim rs As New ADODB.Recordset
        Dim sql As String = String.Empty
        Dim laufnr As Integer
        '   Dim artikelnummern As String = String.Empty

        Try

            ' prüfen, ob der Datensatz für diese deliveryId noch nicht eingetragen ist
            sql = "select * from zus_err_deliveries where deliveryId = '" + delivery("DeliveryId").ToString + "'"
            If Not MyConn.getRecord(rs_vorhanden, sql, fehler) Then
                Throw New Exception("Fehler beim Abfragen, ob für DeliveryId bereits ein Datensatz besteht " + delivery.ToString)
            End If

            ' wenn noch kein Datensatz mit dieser DeliveryId vorhanden
            If rs_vorhanden.EOF Then

                ' Datensatz eintragen
                laufnr = GetNextLaufNr("ZUS_err_deliveries")
                If laufnr = 0 Then
                    Throw New Exception("Fehler beim Laden der LaufNr")
                End If

                sql = "insert into zus_err_deliveries (" +
                    "deliveryid, " +
                    "flightid, " +
                    "message, " +
                    "LaufNr, " +
                    "ImportNr, " +
                    "Erstelltam, " +
                    "erstelltvon, " +
                    "geaendertam, " +
                    "geaendertvon, " +
                    "geaendert, " +
                    "exportiert" +
                    ") values (" +
                    GetValOrDefString(delivery, "DeliveryId") + ", " +
                    GetValOrDefString(delivery, "FlightInformation.FlightId") + ", " +
                    "'" + fehler + "', " +
                    laufnr.ToString + ", " +
                    "0, " +
                    "'" + Now.ToString(dateformat + " HH:mm:ss") + "', " +
                    "'" + Assembly.GetExecutingAssembly().GetName.Name + "', " +
                    "'" + Now.ToString(dateformat + " HH:mm:ss") + "', " +
                    "'" + Assembly.GetExecutingAssembly().GetName.Name + "'" +
                    ", 1, 0)"


                If Not MyConn.getRecord(rs, sql, fehler) Then
                    Throw New Exception(fehler)
                End If
            End If
            Return True
        Catch ex As Exception
            Logger.GetInstance.Log(LogLevel.Exception, "fehlerhafter Delivery konnte nicht in Fehlertabelle geschrieben werden. Im JSON ist RecipientDetails leer. " + delivery.ToString)
            Return False
        End Try
    End Function
















    ' setzt eine Adresse in PX als zu synchronieren
    Public Function SetAsZuSynchroniseren(ByVal personId As String) As Boolean
        Dim sql As String = String.Empty
        Dim rs As New ADODB.Recordset
        Dim fehler As String = String.Empty
        ' Synchroniseren für die Adresse 1 setzen
        sql = "update adr_adressen set Z_synchronisieren = 1 where Z_FLSPersonId = '" + personId + "'"
        If Not MyConn.getRecord(rs, sql, fehler) Then
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod.Name + " " + fehler)
            Return False
        End If
        Return True
    End Function

    ' setzt die LaufNr für eine Tabelle wieder auf 0 (aufrufen, wenn Inhalt der Tabelle gelöscht wird)
    Public Function resetLaufNr(ByVal table As String) As Boolean
        Dim sql As String = String.Empty
        Dim rs As New ADODB.Recordset
        Dim fehler As String = String.Empty
        Try

            sql = "update laufnummern set laufnr = 0 where tabelle = '" + table + "'"
            If Not MyConn.getRecord(rs, sql, fehler) Then
                Return False
            End If

            Return True
        Catch ex As Exception
            Throw New Exception(ex.Message)
            Return False
        End Try
    End Function



    '*************************************************************************PRÜFFUNKTIONEN***********************************************************************
    ' wurde als MemberStateId eine gültige Id angegeben?
    Public Function isValidMemberStateId(ByVal memberStateId As String, ByRef fehler As String) As Boolean
        Dim sql As String = String.Empty
        Dim rs As New ADODB.Recordset

        ' ist ein DS mit der MemberSTateId vorhanden?
        sql = "Select memberStateId from ZUS_FLSMemberStates where memberstateId = '" + memberStateId + "'"
        If Not MyConn.getRecord(rs, sql, fehler) Then
            fehler = "Fehler beim Laden der MemberStateId " + memberStateId + fehler
            Return False
        End If

        'wenn kein DS gefunden --> falsche MemberStateId angegeben
        If rs.EOF Then
            fehler = "Kontrollieren Sie die MemberStateId in Proffix. Der Inhalt entspricht keiner vorhandenen MemberStateId."
            Return False
        End If
        Return True
    End Function

    ''' <summary>
    ''' Püft ob ein Feld in der PROFFIX Datenbank existiert
    ''' </summary>
    ''' <param name="field">Der Name des Feldes</param>
    ''' <param name="table">Der Name der Tabelle</param>
    ''' <returns>Ein Boolen der aussagt ob das Feld existiert</returns>
    Private Shared Function DoesFieldExist(field As String, table As String) As Boolean
        'Prüfen ob das feld im zwischenspeicher abgelegt ist
        If Not ExistingFields.ContainsKey(field) Then
            Dim rs As New ADODB.Recordset,
            doExist As Boolean
            'Suchen des Feldes in der Datenbank
            rs.Open("select * from sys.columns where Name = N'" + field + "' and Object_ID = Object_ID(N'" + table + "')", Proffix.DataBaseConnection)
            doExist = Not rs.EOF
            'Resulatat in den Zwischenspeicher schreiben
            ExistingFields.Add(field, doExist)
            rs.Close()
        End If
        'Resultat zurückgeben
        Return ExistingFields(field)
    End Function

    ' prüft anhand PersonId, ob eine Adresse gelöscht vorhanden ist
    Public Function DoesAddressExistsAsGeloescht(ByVal personId As String, ByVal adressnr As String) As Boolean
        Dim sql As String = String.Empty
        Dim rs As New ADODB.Recordset
        Dim fehler As String = String.Empty

        sql = "select * from adr_adressen where " +
            "geloescht = 1 and " +
            "AdressNrADR = " + adressnr +
            "FLSPersonId = '" + personId + "'"
        If Not MyConn.getRecord(rs, sql, fehler) Then
            Throw New Exception("Fehler in " + MethodBase.GetCurrentMethod.Name + " " + fehler)
        Else
            If rs.EOF Then
                Return False
            Else
                Return True
            End If
        End If
    End Function

    ' prüft, ob eine Adresse anhand PersonId bereits vorhanden ist in PX, aber Synchroniseren = 0 ist. 
    Public Function DoesAddressExistsAsNichtZuSynchronisierend(ByVal personId As String) As Boolean
        Dim sql As String = String.Empty
        Dim rs As New ADODB.Recordset
        Dim fehler As String = String.Empty

        sql = "select * from adr_adressen where Z_Synchronisieren = 0 and Z_FLSPersonId = '" + personId + "'"
        If Not MyConn.getRecord(rs, sql, fehler) Then
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod.Name + " " + fehler)
            Return Nothing
        End If

        ' es wurde kein Datensatz mit der PersonId gefunden
        If rs.EOF Then
            Return False
            ' es wurde eine entsprechene Adresse mit Synchroniseren = 0 gefunden
        Else
            Return True
        End If
    End Function




    ' *************************************************************Daten aus DB laden**************************************************************************
    ' gibt ServerFormat für Datum zurück
    Public Function DateServerFormat() As String
        Dim defaultFormat As String = "yyyy-MM-dd"
        Dim sql As String
        Dim rsformat As New ADODB.Recordset
        Dim fehler As String = ""

        Try
            sql = "SELECT dateformat FROM master..syslanguages WHERE name = @@LANGUAGE"
            MyConn.getRecord(rsformat, sql, fehler)

            Select Case rsformat.Fields("dateformat").Value.ToString
                Case "dmy"
                    Return "dd-MM-yyyy"

                Case "mdy"
                    Return "MM-dd-yyyy"

                Case "ymd"
                    Return "yyyy-MM-dd"
                Case Else
                    Return defaultFormat
            End Select

        Catch ex As Exception
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler in DateServerFormat()")
            Return defaultFormat
        End Try
    End Function



    '*********************************************************pxAdresse-Objekt auslesen**********************************************************************************
    ''' <summary>
    ''' Gibt den Wert eines Zusatzfeldes einer Adresse zurück
    ''' </summary>
    ''' <param name="source">Die Adresse von der die Zusatzfelder gelesen werden</param>
    ''' <param name="name">Der Name des Zusatzfeldes</param>
    ''' <returns>Der Wert des Zusatzfeldes</returns>
    Public Shared Function GetZusatzFelder(ByVal source As pxKommunikation.pxAdressen, ByVal name As String) As String
        If (String.IsNullOrEmpty(source.ZusatzfelderListe)) Then Return Nothing
        Dim getFrom As Integer = Array.IndexOf(source.ZusatzfelderListe.Split("¿".ToCharArray()), name)

        Try
            Return source.ZusatzfelderWerte.Split("¿".ToCharArray())(getFrom)
        Catch
            Return Nothing
        End Try
    End Function

    ' Holt des Änderungsdatum der Adresse. Dieses ist das neuste der Felder "GeändertAm" und "ErstelltAm"
    Public Shared Function GetAddressChangeDate(ByVal adress As pxKommunikation.pxAdressen) As DateTime
        'Ruft die Helper Funktion des GeneralHelpers auf
        Return GeneralHelper.GetNewestDate({
            If(Not String.IsNullOrEmpty(adress.ErstelltAm), DateTime.Parse(adress.ErstelltAm), DateTime.MinValue),
            If(Not String.IsNullOrEmpty(adress.GeaendertAm), DateTime.Parse(adress.GeaendertAm), DateTime.MinValue)
        })
    End Function

    ' Holt des Änderungsdatum der Adresse. Dieses ist das neuste der Felder "GeändertAm" und "ErstelltAm"
    Public Shared Function GetArticleChangeDate(ByVal article As pxKommunikation.pxArtikel) As DateTime
        'Ruft die Helper Funktion des GeneralHelpers auf
        Return GeneralHelper.GetNewestDate({
            If(Not String.IsNullOrEmpty(article.ErstelltAm), DateTime.Parse(article.ErstelltAm), DateTime.MinValue),
            If(Not String.IsNullOrEmpty(article.GeaendertAm), DateTime.Parse(article.GeaendertAm), DateTime.MinValue)
        })
    End Function



    '***********************************************************pxAdressen-Objekt bearbeiten*******************************************************************
    ' wenn aus FLS eine Adresse importiert werden soll, für die kein Ort angegeben ist (Pflichtfeld in Proffix) --> Default setzen
    Public Shared Function SetAdressDefault(ByVal adress As pxBook.pxKommunikation.pxAdressen, ByVal rs_adressdefault As ADODB.Recordset) As pxKommunikation.pxAdressen

        ' Ort + PLZ sind Pflichtfelder für PX
        If String.IsNullOrEmpty(adress.Plz) Then
            adress.Plz = "9999"
        End If

        If String.IsNullOrEmpty(adress.Ort) Then
            adress.Ort = "Ort unbekannt"
        End If

        ' Felder, für die aus FLS keine Werte kommen können, aber für die Dokumenterstellung nötig sind:
        ' aus recordset aus ADR_Adressdef (anfangs Sync 1 Mal geladen) lesen
        If adress.Kondition = 0 Then
            Try

                adress.Kondition = CInt(rs_adressdefault.Fields("DKondition").Value)
            Catch ex As Exception
                ' wenn kein Adressdefault für Kondition definiert ist --> 1
                adress.Kondition = 1
            End Try
        End If

        If String.IsNullOrEmpty(adress.Sammelkonto) Then
            Try
                adress.Sammelkonto = rs_adressdefault.Fields("DSammelKto").Value.ToString
            Catch ex As Exception
                ' wenn kein Adressdefault für Sammelkonto definiert ist --> 1100
                adress.Sammelkonto = "1100"
            End Try
        End If

        If String.IsNullOrEmpty(adress.Waehrung) Then
            Try
                adress.Waehrung = rs_adressdefault.Fields("DWaehrung").Value.ToString
            Catch ex As Exception
                ' wenn kein Adressdefault für Währung definiert ist --> CHF
                adress.Waehrung = "CHF"
            End Try
        End If

        ' FLS gibt es nur auf Deutsch --> Standardsprache wird fix aud Deutsch gesetzt
        If String.IsNullOrEmpty(adress.Sprache) Then
            adress.Sprache = "D"
        End If

        Return adress
    End Function



    '************************************************************Hilfsfunktionen*******************************************************************************

    ''' <summary>
    ''' Die Zusatzfelder werden der PROFFIX Schnitstelle als SQL mitgegeben
    ''' Hier werden sie in SQL geparst
    ''' </summary>
    ''' <param name="source">Die Adresse deren Zusatzfelder gelesen werden</param>
    ''' <returns>Der SQL String</returns>
    Public Shared Function CreateZusatzFelderSql(ByVal source As pxKommunikation.pxAdressen) As String
        'Definieren der Zusatzfelder liste
        Dim values As New Dictionary(Of String, String) From {
            {"Z_Segelfluglehrer_Lizenz", GetZusatzFelder(source, "Z_Segelfluglehrer_Lizenz")},
            {"Z_Segelflugpilot_Lizenz", GetZusatzFelder(source, "Z_Segelflugpilot_Lizenz")},
            {"Z_Segelflugschueler_Lizenz", GetZusatzFelder(source, "Z_Segelflugschueler_Lizenz")},
            {"Z_Motorflugpilot_Lizenz", GetZusatzFelder(source, "Z_Motorflugpilot_Lizenz")},
            {"Z_Schleppilot_Lizenz", GetZusatzFelder(source, "Z_Schleppilot_Lizenz")},
            {"Z_Segelflugpassagier_Lizenz", GetZusatzFelder(source, "Z_Segelflugpassagier_Lizenz")},
            {"Z_TMG_Lizenz", GetZusatzFelder(source, "Z_TMG_Lizenz")},
            {"Z_Windenfuehrer_Lizenz", GetZusatzFelder(source, "Z_Windenfuehrer_Lizenz")},
            {"Z_Motorfluglehrer_Lizenz", GetZusatzFelder(source, "Z_Motorfluglehrer_Lizenz")},
            {"Z_Schleppstart_Zulassung", GetZusatzFelder(source, "Z_Schleppstart_Zulassung")},
            {"Z_Eigenstart_Zulassung", GetZusatzFelder(source, "Z_Eigenstart_Zulassung")},
            {"Z_Windenstart_Zulassung", GetZusatzFelder(source, "Z_Windenstart_Zulassung")},
            {"Z_SpotURL", "'" + GetZusatzFelder(source, "Z_SpotURL") + "'"},
            {"Z_Email_Geschaeft", "'" + GetZusatzFelder(source, "Z_Email_Geschaeft") + "'"},
            {"Z_Lizenznummer", "'" + GetZusatzFelder(source, "Z_Lizenznummer") + "'"},
            {"Z_FLSPersonId", "'" + GetZusatzFelder(source, "Z_FLSPersonId") + "'"},
            {"Z_Segelflugpilot", GetZusatzFelder(source, "Z_Segelflugpilot")},
            {"Z_Segelfluglehrer", GetZusatzFelder(source, "Z_Segelfluglehrer")},
            {"Z_Segelflugschueler", GetZusatzFelder(source, "Z_Segelflugschueler")},
            {"Z_Motorflugpilot", GetZusatzFelder(source, "Z_Motorflugpilot")},
            {"Z_Motorfluglehrer", GetZusatzFelder(source, "Z_Motorfluglehrer")},
            {"Z_Passagier", GetZusatzFelder(source, "Z_Passagier")},
            {"Z_Schleppilot", GetZusatzFelder(source, "Z_Schleppilot")},
            {"Z_Windenfuehrer", GetZusatzFelder(source, "Z_Windenfuehrer")},
            {"Z_erhaeltFlugreport", GetZusatzFelder(source, "Z_erhaeltFlugreport")},
            {"Z_erhaeltReservationsmeldung", GetZusatzFelder(source, "Z_erhaeltReservationsmeldung")},
            {"Z_erhaeltPlanungserinnerung", GetZusatzFelder(source, "Z_erhaeltPlanungserinnerung")},
            {"Z_erhaeltFlugStatistikenZuEigenen", GetZusatzFelder(source, "Z_erhaeltFlugStatistikenZuEigenen")},
            {"Z_MemberStateId", "'" + GetZusatzFelder(source, "Z_MemberStateId") + "'"}
        }

        'Umwandeln des Dictionary in ein SQL String
        Dim sql As String = String.Empty
        For Each value In values
            If Not String.IsNullOrEmpty(sql) Then
                sql += ", "
            End If
            If DoesFieldExist(value.Key, "ADR_Adressen") Then
                sql += value.Key + " = " + value.Value
            End If
        Next

        ' Zusatzfeld Synchronisieren anhängen
        sql += ", Z_Synchronisieren = 1"

        Return sql
    End Function


    ' updated das IsActive Feld (geloescht = 1 entspricht IsActive = False)
    Public Function SetIsActiveInFLSPersonDependingOnGeloescht(ByRef person As JObject, ByVal adressnr As String) As Boolean
        Dim sql As String = String.Empty
        Dim rs As New ADODB.Recordset
        Dim fehler As String = String.Empty
        Dim IsActive As Boolean
        Try
            sql = "select geloescht from adr_adressen where adressnradr = " + adressnr
            If Not MyConn.getRecord(rs, sql, fehler) Then
                Throw New Exception(fehler)
                Return False
            End If
            If rs.Fields("geloescht").Value.ToString = "1" Then
                IsActive = False
            Else
                IsActive = True
            End If

            person("ClubRelatedPersonDetails")("IsActive") = IsActive

            Return True
        Catch ex As Exception
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod.Name + " " + ex.Message)
            Return False
        End Try
    End Function

    ' updatet in PX das geloeschtFeld
    Public Function SetGeloeschtInPXAdresseDependingOnIsActive(ByVal person As JObject) As Boolean
        Dim sql As String = String.Empty
        Dim rs As New ADODB.Recordset
        Dim fehler As String = String.Empty
        Try

            ' IsActive umgekehrt auf Geloescht updaten (IsActive = false --> geloescht = 1)
            Dim geloescht As String = String.Empty
            If GetValOrDef(person, "ClubRelatedPersonDetails.IsActive") = "false" Then
                geloescht = "1"
            Else
                geloescht = "0"
            End If

            sql = "update adr_adressen set geloescht = " + geloescht + " where Z_FLSPersonId = '" + person("PersonId").ToString + "'"
            If Not MyConn.getRecord(rs, sql, fehler) Then
                Throw New Exception(fehler)
            End If
            Return True

        Catch ex As Exception
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod.Name + " " + ex.Message)
            Return False
        End Try
    End Function

End Class


' ''' <summary>
' ''' Sucht die nächste Adressnummer und reserviert diese
' ''' </summary>
' ''' <returns>Die nächste Adressnummer</returns>
'Public Shared Function GetNextAdressNr() As String
'    Return NextNrCircle(Proffix.GoBook.Mandant & ".dbo.ADR_Adressen", "AdressNrADR")
'End Function

' ''' <summary>
' ''' Gibt die nächste Nummer eines PROFFIX Nummernkreises zurück und reserviert diese
' ''' </summary>
' ''' <param name="Tabelle">Die Tabelle des Nummernkreises</param>
' ''' <param name="Key">Der Key des Nummernkreises</param>
' ''' <returns>Die nächste Nummer</returns>
'Public Shared Function NextNrCircle(ByVal Tabelle As String, ByVal Key As String) As String

'    Try
'        Dim rs As New ADODB.Recordset,
'            teile() As String = Tabelle.Split("."c),
'            fehler As String,
'            sql As String,
'            nr As String = String.Empty
'        If teile.Length = 3 Then
'            Dim Mandant As String = teile(0)
'            Dim Schema As String = teile(1)
'            Tabelle = teile(2)
'            fehler = ""
'            sql = "select NrAktuell from " & Mandant & "." & Schema & ".NrKreis where NrKreis = '" & Tabelle & Key & "' "
'            rs.Open(sql, Proffix.DataBaseConnection)
'            If String.IsNullOrEmpty(fehler) Then
'                If Not rs.EOF Then
'                    nr = getFeld(rs.Fields("NrAktuell"), "-2")
'                    nr = CStr(CInt(nr) + 1)
'                End If
'                rs.Close()
'                sql = "update NrKreis set NrAktuell=" & nr & " where NrKreis = '" & Tabelle & Key & "' "
'                rs.Open(sql, Proffix.DataBaseConnection)
'                If Not String.IsNullOrEmpty(fehler) Then
'                    'setLog(Fehler)
'                End If
'                Return nr
'            Else
'                'setLog(Fehler)
'            End If
'        Else
'            'setLog("Tabelle ohne Mandant und Schema übergeben", "naechsteLaufnummer(" & Tabelle & ")")
'        End If
'    Catch ex As Exception
'        'setLog(ex.Message, ex.StackTrace)
'    End Try
'    Return String.Empty
'End Function

' ''' <summary>
' ''' Gibt den Wert eines SQL Feldes zurück und löst ihn auf
' ''' </summary>
'Private Shared Function getFeld(ByVal feld As ADODB.Field, ByVal strInit As String, Optional ByVal prefix As String = "", Optional ByVal postfix As String = "", Optional ByVal maxlenght As Integer = 0, Optional ByVal sql As Boolean = False, Optional ByVal UmbruchTabs As Boolean = True) As String
'    Dim Str As String
'    Try
'        Str = CStr(feld.Value)
'    Catch ex As Exception
'        Str = strInit
'    End Try
'    If String.IsNullOrEmpty(Str) Then
'        getFeld = prefix & postfix
'    Else
'        If sql Then Str = Str.Replace("'", "''")
'        If Not UmbruchTabs Then
'            While Str.IndexOf(Chr(13)) >= 0
'                Str = Str.Replace(Chr(13), "¬")
'            End While
'            While Str.IndexOf(Chr(10)) >= 0
'                Str = Str.Replace(Chr(10), "¬")
'            End While
'            While Str.IndexOf(";;") >= 0
'                Str = Str.Replace("¬¬", "¬")
'            End While
'            ' Tab durch Leerzeichen ersetzen
'            While Str.IndexOf(Chr(9)) >= 0
'                Str = Str.Replace(Chr(9), " ")
'            End While
'            ' Leerzeichen vor :;,. entfernen
'            While Str.IndexOf(" :") >= 0
'                Str = Str.Replace(" :", ": ")
'            End While
'            While Str.IndexOf(" ;") >= 0
'                Str = Str.Replace(" ;", "; ")
'            End While
'            While Str.IndexOf(" ,") >= 0
'                Str = Str.Replace(" ,", ", ")
'            End While
'            While Str.IndexOf(" .") >= 0
'                Str = Str.Replace(" .", ". ")
'            End While
'            ' Leerzeichen nach ;, einfügen
'            Str = Str.Replace(";", "; ")
'            Str = Str.Replace(",", ", ")
'            ' doppelte Leerzeichen entfernen
'            While Str.IndexOf("  ") >= 0
'                Str = Str.Replace("  ", " ")
'            End While
'            While Str.IndexOf("--") >= 0
'                Str = Str.Replace("--", "-")
'            End While
'            ' Leerzeichen am Anfang und am Ende entfernen
'            Str = Str.Trim
'            ' ; am Anfang und am Ende entfernen
'            If Not String.IsNullOrEmpty(Str) Then If Str.Substring(0, 1) = ";" Then Str = Str.Substring(1)
'            If Not String.IsNullOrEmpty(Str) Then If Str.Substring(Str.Length - 1, 1) = ";" Then Str = Str.Substring(0, Str.Length - 1)
'            Str = Str.Trim
'        End If
'        If maxlenght > 0 And Str.Length > maxlenght Then Str = Str.Substring(0, maxlenght)
'        getFeld = prefix & Str & postfix
'    End If
'End Function
