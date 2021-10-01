
Imports System.Threading
Imports Newtonsoft.Json.Linq
Imports SMC.Lib

Imports System.Net.Http
Imports System.Reflection

''' <summary>
''' Die Managerklasse die für den Download und die Verrechnung der Flugdaten benutzt wird
''' </summary>
Public Class Importer

    Public Property client As FlsConnection
    Public Property pxhelper As ProffixHelper
    Public Property MyConn As ProffixConnection
    Public Property Log As Action(Of String)
    Public Property lastDeliveryImport As DateTime
    Public Property lastFlightImport As DateTime
    ' Public Property bisFlightImport As DateTime

    'Public Property DoProgress As Action    ' Aktion, die ausgeführt wird, wenn der Import Fortschritte macht
    Public Property DoProgressDelivery As Action    ' Aktion, die ausgeführt wird, wenn der Import Fortschritte macht
    Public Property DoProgressFlight As Action    ' Aktion, die ausgeführt wird, wenn der Import Fortschritte macht

    Private _progressDelivery As Integer    ' Fortschritt der Synchronisation anzeigen
    Public Property ProgressDelivery As Integer
        Get
            Return _progressDelivery
        End Get
        Private Set(ByVal value As Integer)
            _progressDelivery = value
        End Set
    End Property

    Private _progressFlight As Integer    ' Fortschritt der Synchronisation anzeigen
    Public Property ProgressFlight As Integer
        Get
            Return _progressFlight
        End Get
        Private Set(ByVal value As Integer)
            _progressFlight = value
        End Set
    End Property

    Private _deliverycount As Integer
    Public Property DeliveryCount As Integer
        Get
            Return _deliverycount
        End Get
        Private Set(ByVal value As Integer)
            _deliverycount = value
        End Set
    End Property

    Private _flightcount As Integer
    Public Property FlightCount As Integer
        Get
            Return _flightcount
        End Get
        Private Set(ByVal value As Integer)
            _flightcount = value
        End Set
    End Property

    Sub New(ByRef lastDeliveryImport As DateTime, ByRef lastFlightImport As DateTime, ByRef client As FlsConnection, ByRef pxhelper As ProffixHelper, ByRef MyConn As ProffixConnection)
        Me.lastDeliveryImport = lastDeliveryImport
        Me.lastFlightImport = lastFlightImport
        Me.client = client
        Me.pxhelper = pxhelper
        Me.MyConn = MyConn
    End Sub

    '**************************************************************************FlightImport*****************************************************************************************
    ''' <summary>
    ''' Donwload der Flugdaten in die ZUS_flight
    ''' </summary>
    ''' <returns>Ein Boolean ob der Import erfolgreich war</returns>
    Public Function FlightImport() As Boolean
        Dim modifiedFlights As List(Of JObject) ' Flugdaten aus FLS (zur Verrechnung freigegeben)
        Dim dokNr As Integer = 0
        Dim fehler As String = String.Empty
        Dim lastChangeDate As Date = DateTime.MinValue
        Dim successful As Boolean = True

        Try
            InvokeLog("Flugdatenimport gestartet")
            Logger.GetInstance.Log(LogLevel.Info, "Flugdatenimport gestartet")

            ' Flüge laden, die seit letztem Flugimport erstellt/verändert wurden
            modifiedFlights = loadModifiedFlights()

            FlightCount = modifiedFlights.Count
            ProgressFlight = 0
            InvokeDoProgressFlight()

            'Iteration durch die Flüge, die zu verrechnen sind
            For Each flight As JObject In modifiedFlights

                ' von abgebrochenen Flugimporten vorhandene Daten für die MasterFlightId löschen
                If Not pxhelper.deleteIncompleteFlightData(flight("FlightId").ToString) Then
                    InvokeLog("Fehler beim Löschen der Daten für FlugId " + flight("FlightId").ToString)
                    successful = False
                End If

                ' Daten für die MasterFlightId in Zusatztabelle importieren
                If Not SetFlightData(flight) Then
                    InvokeLog(vbTab + "Fehler beim Importieren der Flugdaten oder Erstellen der Dokumente für FlightId: " + flight("FlightId").ToString)
                    If Not pxhelper.deleteIncompleteFlightData(flight("FlightId").ToString) Then
                        InvokeLog("Fehler beim Löschen der Daten für FlugId " + flight("FlightId").ToString)
                    End If
                    successful = False
                Else
                    InvokeLog("Der Flug mit der FlightId " + flight("FlightId").ToString + " wurde importiert")
                End If

                ProgressFlight += 1
                InvokeDoProgressFlight()
            Next

            '  Logger.GetInstance.Log(LogLevel.Info, "flugdaten werden gelöscht")
            Logger.GetInstance.Log("Allfällige Daten für noch zu importierte " + modifiedFlights.Children.Count.ToString + " Flüge wurden gelöscht")


            ' wenn bis hierher gekommen --> Flugdatenimport hat für alle FlightIds geklappt
            ProgressFlight = FlightCount
            InvokeDoProgressFlight()

            ' nur wenn kein Fehler aufgetreten ist, Now() als letzten erfolgreichen Import setzen
            If successful Then
                lastFlightImport = DateTime.Now
                InvokeLog("Flugdatenimport erfolgreich beendet")
                Logger.GetInstance.Log(LogLevel.Info, "Flugdatenimport erfolgreich beendet")
            Else
                Logger.GetInstance.Log(LogLevel.Exception, "Beim Flugdatenimport ist mindestens 1 Fehler aufgetreten.")
                InvokeLog("Beim Flugdatenimport ist mindestens 1 Fehler aufgetreten. Das Datum des letzten erfolgreichen Flugdatenimports wird nicht aktualisiert.")
                InvokeLog("Deshalb werden alle Flüge, die nach " + lastFlightImport.ToString("yyyy-MM-dd hh:mm:ss") + " verändert und importiert wurden wieder gelöscht")
            End If

            Return successful

        Catch exce As Exception
            Logger.GetInstance().Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod.Name + " " + exce.Message)
            InvokeLog(vbTab + "Fehler beim Flugdatenimport")
            Return False
        End Try
    End Function

    ' gibt alle Flüge zurück, die seit letztem Flugdatenimport erstellt/verändert wurden
    Public Function loadModifiedFlights() As List(Of JObject)
        Try
            Dim modifiedFlightsByDate As Threading.Tasks.Task(Of JArray)
            Dim modifiedFlights As New List(Of JObject)
            Dim lastChangeDate As DateTime

            If logAusfuehrlich Then
                Logger.GetInstance.Log(LogLevel.Info, My.Settings.ServiceAPIModifiedFlightsMethod + lastFlightImport.ToString("yyyy-MM-dd"))
            End If

            ' alle Flüge, die seit letzem erfolgreichem ImportDATUM verändert/erstellt wurden, herunterladen
            modifiedFlightsByDate = client.CallAsyncAsJArray(My.Settings.ServiceAPIModifiedFlightsMethod + lastFlightImport.ToString("yyyy-MM-dd"))
            modifiedFlightsByDate.Wait()

            ' aus FLS kann nur anhand Datum geladen werden 
            '--> hier auch noch auf Zeit prüfen, ob nach letztem Flugdatenimport
            For Each flight As JObject In modifiedFlightsByDate.Result.Children()
                ' die Flüge werden anhand Datum geholt --> hier auch noch auf die Zeit prüfen (nur neu importieren, wenn wirklich nach lastFlightImport
                lastChangeDate = FlsHelper.GetFlightChangeDate(flight)

                '' falls nur bis zu Datum importiert werden soll
                'If bisFlightImport > DateTime.MinValue Then

                '    ' Flüge, die später noch verändert wurden, abfangen und nicht laden
                '    If bisFlightImport > lastChangeDate Then
                '        Continue For
                '    End If
                'End If

                ' alle Flüge, die nach letztem Import verändert wurden, laden
                If lastChangeDate <> DateTime.MinValue Then
                    If lastChangeDate > lastFlightImport Then
                        modifiedFlights.Add(flight)
                    End If
                End If
            Next

            Return modifiedFlights
        Catch ex As Exception
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod.Name + " " + ex.Message)
            Return Nothing
        End Try
    End Function

    ' Tabelle enthält allgemeine Infos zum Flug, Primärschlüssel: FlightId
    Public Function SetFlightData(ByVal flight As JObject) As Boolean

        Try

            If logAusfuehrlich Then
                Logger.GetInstance.Log(LogLevel.Info, flight.ToString)
            End If

            ' insert Gliderflight or Motorflight
            If Not pxhelper.insertMasterFlight(flight) Then
                Return False
            End If
            ' if exists, insert Towflight
            If Not (GetValOrDef(flight, "TowFlightFlightId") = "" Or GetValOrDef(flight, "TowFlightFlightId") = "00000000-0000-0000-0000-000000000000") Then
                If Not pxhelper.insertTowFlight(flight) Then
                    Return False
                End If

                '' ergänzt in ZUS_DokflightLink die FlightId mit der TowFlightFlightId (insert in DokFlightLink wird mit Delivery gemacht, Delivery kennt aber TowFlightFlightId nicht
                'If Not pxhelper.UpdateDokFlightLink(GetValOrDef(flight, "FlightId"), GetValOrDef(flight, "TowFlightFlightId")) Then
                '    Return False
                'End If
            End If

            Logger.GetInstance.Log(LogLevel.Info, "Der Flug mit der FlightId " + flight("FlightId").ToString + " wurde importiert")
            Return True
        Catch ex As Exception
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Importieren der Flugdaten" + ex.Message)
            Return False
        End Try
    End Function



    Private Function TestFunktionGetBeispielDeliveries() As Threading.Tasks.Task(Of JArray)
        Dim filetext As New List(Of String)
        Dim path As String = "C:\Workspace\Projekte_inArbeit\FLS\AddressSync\BeispielDeliveries.txt"
        filetext = ReadFile(path)

        '  Dim str As String
        ' str = String.Join(Char(13), filetext)
       
    End Function
    Private Function ReadFile(ByVal path As String) As List(Of String)
        Dim filetext As New List(Of String)
        Dim reader As StreamReader
        Try
            reader = New StreamReader(path)
        Catch ex As Exception
            MsgBox("Datei " + path + " wurde nicht gefunden")
            Return Nothing
        End Try

        Dim sLine As String = ""
        ' Dim iniLine As String = ""

        ' Zeile zu key aus Ini auslesen
        Do
            sLine = reader.ReadLine()
            filetext.Add(sLine)
        Loop Until sLine Is Nothing
        reader.Close()

       Return filetext

    End Function




    '*********************************************************************************DeliveryImport*************************************************************************
    ''' <summary>
    ''' Donwload der Deliveries + Erstellung der Dokumente
    ''' </summary>
    ''' <returns>Ein Boolean ob der Import erfolgreich war</returns>
    Public Function DeliveryImport() As Boolean
        Dim deliveries As Threading.Tasks.Task(Of JArray) ' Lieferscheindaten aus FLS (zur Verrechnung freigegeben)
        Dim dokNr As Integer = 0
        Dim fehler As String = String.Empty
        Dim successful As Boolean = True

        Try

            'DEBUG für Test: Validieren + Sperren von erfassten Flügen --> werden als zu verrechnende Flüge geliefert (im Normalbetrieb werden die Flüge durch FLS freigegeben = "gesperrt" für Bearbeitung in FLS)
            If My.Settings.ServiceAPITokenMethod.Contains("test.glider-fls.ch") Then
                client.testDeliveriesErstellen()
            End If

            InvokeLog("Lieferscheinimport gestartet")
            Logger.GetInstance.Log(LogLevel.Info, "Lieferscheinimport gestartet")

            ' alle Flüge, die zu verrechnen sind aus FLS herunterladen
            If logAusfuehrlich Then
                Logger.GetInstance.Log(LogLevel.Info, My.Settings.ServiceAPIDeliveriesNotProcessedMethod)
            End If
            deliveries = client.CallAsyncAsJArray(My.Settings.ServiceAPIDeliveriesNotProcessedMethod)
            deliveries.Wait()

            ' Debug
            ' deliveries = TestFunktionGetBeispielDeliveries()

            DeliveryCount = deliveries.Result.Children.Count
            ProgressDelivery = 0
            InvokeDoProgressDelivery()

            If logAusfuehrlich Then
                Logger.GetInstance.Log(LogLevel.Info, "Anzahl geladener Lieferscheine " & deliveries.Result.Children.Count)
                Logger.GetInstance.Log(LogLevel.Info, deliveries.Result.ToString)
            End If

            If deliveries.Result.Count = 0 Then
                InvokeLog("Es wurden keine Lieferscheine geladen")
            End If

            'Iteration durch die Flüge, die zu verrechnen sind
            'TODO: OrderBy PersonId and Startdate of flight
            For Each delivery As JObject In deliveries.Result.Children()

                dokNr = 0
                ' von abgebrochenen Lieferscheinimporten vorhandene Daten für die DeliveryId löschen
                If Not pxhelper.deleteIncompleteDeliveryData(delivery("DeliveryId").ToString) Then
                    InvokeLog("Fehler beim Löschen der Daten für DeliveryId " + delivery("DeliveryId").ToString)
                    Throw New Exception("Fehler beim Löschen der Daten für DeliveryId " + delivery("DeliveryId").ToString)
                    Return False
                End If

                ' prüfen, ob der Delivery die nötigen Daten enthält. Wenn nicht, kann dieser Delivery nicht importiert werden
                If Not checkForCompleteDelivery(delivery) Then


                    InvokeLog(vbTab + "Fehler: Im zu importierender Lieferschein fehlen Daten. Er kann nicht importiert werden. DeliveryId: " + delivery("DeliveryId").ToString)

                    ' Daten des fehlerhaften Lieferscheins in Zusatztabelle schreiben
                    If Not FehlerhafterDeliveryVerarbeiten(delivery) Then
                        InvokeLog(vbTab + "Fehler: fehlerhafter Delivery mit DeliveryId " + delivery("DeliveryId").ToString + " konnte nicht verarbeitet werden")
                    End If

                    successful = False
                    Continue For

                    '' wenn nötige Daten fehlen --> nachfragen, ob überspringen + mit nächstem weiterfahren (OK) oder Deliveryimport abbrechen (Cancel)
                    'Dim dialogres As DialogResult = MessageBox.Show("Der Lieferschein mit der DeliveryId " + GetValOrDef(delivery, "DeliveryId") + " kann nicht importiert werden, da benötigte Daten fehlen." + vbCrLf + vbCrLf +
                    '                                     "Wenn Sie diesen Lieferschein überspringen und den nächsten importieren wollen, klicken Sie ""OK""" + vbCrLf + vbCrLf +
                    '                                     "Mit Abbrechen beenden Sie den Lieferscheinimport", "Keine PersonId vorhanden", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2)
                    '' OK --> diesen Delivery überspringen --> mit nächstem weiterfahren
                    'If dialogres = DialogResult.OK Then
                    '    successful = False

                    '    Continue For
                    '    ' Cancel --> Deliveryimport abbrechen
                    'ElseIf dialogres = DialogResult.Cancel Then
                    '    Return False
                    'End If

                End If

                ' 1 Lieferschein importieren
                If Not importDelivery(delivery, dokNr) Then

                    ' Lieferscheinimport fehlgeschlagen
                    If Not FehlerhafterDeliveryVerarbeiten(delivery) Then
                        InvokeLog(vbTab + "Fehler: Delivery mit DeliveryId " + delivery("DeliveryId").ToString + " konnte nicht verarbeitet werden")
                    End If

                    InvokeLog(vbTab + "Fehler beim Importieren der Lieferscheine oder Erstellen der Dokumente für DeliveryId: " + delivery("DeliveryId").ToString)
                    If Not pxhelper.deleteIncompleteDeliveryData(delivery("DeliveryId").ToString) Then
                        InvokeLog("Fehler beim Löschen der Daten für DeliveryId " + delivery("DeliveryId").ToString)
                    End If
                    Continue For
                End If

                ' wenn dokNr noch 0 ist --> Fehler = es wurde kein Dok erstellt
                If dokNr = 0 Then
                    InvokeLog(vbTab + "Fehler beim Auslesen der neuen DokumentNr des in Proffix neu erstellten Dokuments AdressNr: " + If(GetValOrDef(delivery, "RecipientDetails.PersonId") = "", GetValOrDef(delivery, "RecipientDetails.PersonClubMemberNumber"), pxhelper.GetAdressNr(GetValOrDef(delivery, "RecipientDetails.PersonId"))) + " Flugdatum: " + GetValOrDef(delivery, "FlightInformation.FlightDate"))
                    Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Auslesen der neuen DokumentNr des in Proffix neu erstellten Dokuments AdressNr: " + If(GetValOrDef(delivery, "RecipientDetails.PersonId") = "", GetValOrDef(delivery, "RecipientDetails.PersonClubMemberNumber"), pxhelper.GetAdressNr(GetValOrDef(delivery, "RecipientDetails.PersonId"))) + " Flugdatum: " + GetValOrDef(delivery, "FlightInformation.FlightDate"))
                    Return False
                End If

                ' wenn dieser Delivery eine FlightId enthält --> Datensatz in ZUS_DokFlightLink schreiben
                If GetValOrDef(delivery, "FlightInformation.FlightId") <> "" Then

                    ' DokDeliveryLink füllen
                    If Not pxhelper.SetDokFlightLink(delivery, dokNr) Then
                        InvokeLog(vbTab + "Fehler beim Befüllen der Zusatztabelle ZUS_DokFlightLink")
                        Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Befüllen der Zusatztabelle ZUS_DokFlightLink")
                        Return False
                    End If
                End If

                'in FLS schreiben, dass diese Delivery verrechnet (LS erstellt) wurde
                If Not flagAsDelivered(delivery, dokNr) Then
                    InvokeLog(vbTab + "Fehler beim Rückmelden an FLS, dass das FLS-Dokument erstellt worden ist")
                    pxhelper.deleteIncompleteDeliveryData(delivery("DeliveryId").ToString)
                    InvokeLog("Fehler beim Löschen der Daten für DeliveryId " + delivery("DeliveryId").ToString)
                    Throw New Exception("Fehler in " + MethodBase.GetCurrentMethod.Name)
                    Return False
                End If

                ProgressDelivery += 1
                InvokeDoProgressDelivery()

            Next

            ProgressDelivery = DeliveryCount
            InvokeDoProgressDelivery()

            If successful Then
                InvokeLog("Lieferscheinimport erfolgreich beendet")
                Logger.GetInstance.Log(LogLevel.Info, "Lieferscheinimport erfolgreich beendet")
                lastDeliveryImport = Now()
            Else
                InvokeLog("Fehler beim Lieferscheinimport")
                Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Lieferscheinimport")
            End If

            InvokeLog("")
            Return successful

        Catch exce As Exception
            Logger.GetInstance().Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod.Name + " " + exce.Message)
            InvokeLog(vbTab + "Fehler beim Importieren der Lieferscheine")
            Return False

        End Try
    End Function

    ' IDs der Lieferscheine, die in PX nicht verarbeitet werden können, in Fehlertabelle schreiben
    Private Function FehlerhafterDeliveryVerarbeiten(ByVal delivery As JObject) As Boolean
        Dim fehlerBekannt As Boolean = False

        ' abfangen ob RecipientDetails leer --> unklar, wer bezahlen muss 
        If delivery("RecipientDetails").ToString = "{}" Then
            fehlerBekannt = True
            If Not pxhelper.InFehlertabelleSchreiben(delivery, "Lieferschein aus FLS enthält keine Daten zum Rechnungsempfänger") Then
                Return False
            End If
        End If

        ' prüfen, ob eine PersonId für Rechnungsempfänger vorhanden ist
        If GetValOrDef(delivery, "RecipientDetails.PersonId") = "" Then
            If GetValOrDef(delivery, "RecipientDetails.PersonClubMemberNumber") = "" Then
                fehlerBekannt = True
                If Not pxhelper.InFehlertabelleSchreiben(delivery, "Lieferschein aus FLS enthält weder eine PersonId noch eine MemberNumberals Rechnungsempfänger") Then
                    Return False
                End If
            End If
        End If

        ' prüfen, ob delivery überhaupt Artikel enthält (wenn nicht, ist nicht definiert, welche Artikel mit diesem Flug verknüpft sind
        If delivery("DeliveryItems").Count = 0 Then
            fehlerBekannt = True
            If Not pxhelper.InFehlertabelleSchreiben(delivery, "Artikel fehlen") Then
                Return False
            End If
        End If

        ' wenn es keiner der bekannten Fehler war
        If Not fehlerBekannt Then
            If Not pxhelper.InFehlertabelleSchreiben(delivery, "unbekannter Fehler") Then
                Return False
            End If
        End If

        ' nachfragen, ob der fehlerhafte Lieferschein als erledigt verbucht werden soll.
        Dim dialogres As DialogResult = MessageBox.Show("Der Lieferschein mit der DeliveryId " + GetValOrDef(delivery, "DeliveryId") + " kann nicht verarbeitet werden. " +
                                                       "Die DeliveryId und FlightId wurden in die Fehlertabelle ""err_deliveries"" geschrieben. " + vbCrLf + vbCrLf +
                                                        "Soll der Lieferschein an FLS als erledigt markiert werden? " +
                                                        "ACHTUNG: Wenn sie JA klicken, kann der Lieferschein nicht mehr über dieses Programm importiert werden, sondern muss in Proffix manuell erstellt werden!!" + vbCrLf + vbCrLf +
                                                        "Wenn Sie NEIN klicken, wird der fehlerhafte Lieferschein beim nächsten Import wieder als zu Importieren erscheinen", "Lieferscheinimport fehlgeschlagen", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation)
        If dialogres = DialogResult.Yes Then
            ' fehlerhaften Lieferschein als Verarbeitet an FLS melden, damit er nicht immer wieder kommt
            If Not flagAsDelivered(delivery, 0) Then
                InvokeLog(vbTab + "Fehler beim Rückmelden an FLS, dass das FLS-Dokument erstellt worden ist")
                Return False
            End If
        End If

        Return True

    End Function

    ' verarbeitet die Daten für eine DeliveryId
    Private Function importDelivery(ByVal delivery As JObject, ByRef dokNr As Integer) As Boolean
        Dim deliveryId As String = delivery("DeliveryId").ToString
        Dim docToEdit As New pxBook.pxKommunikation.pxDokument  ' Dokument (aus Proffix, aus erstellten, oder neu erstelltes) welches bearbeitet werden soll
        Dim docExistInProffix As Boolean = False                        ' Flag, ob das Dok in PX existiert (entweder bereits seit vor Programmausführung, oder durch ein bereits neu erstelltes Dok, welches vorher bereits in PX geladen wurde
        Dim newDocPositions As New List(Of pxBook.pxKommunikation.pxDokumentPos)         ' enthält alle DocPos, die für das Dok neu erstellt werden
        Dim newDocPosition As New pxBook.pxKommunikation.pxDokumentPos  ' einzelne DocPos, die erstellt wird
        Dim newTextPosition As New pxBook.pxKommunikation.pxDokumentPos
        Dim fehler As String = String.Empty
        Dim adressNr As String = String.Empty
        Dim recipientPersonId As String = String.Empty

        Try
            If logAusfuehrlich Then
                Logger.GetInstance.Log(delivery.ToString)
            End If

            If GetValOrDef(delivery, "RecipientDetails.PersonId") = "" Then
                If GetValOrDef(delivery, "RecipientDetails.PersonClubMemberNumber") = "" Then

                    Logger.GetInstance.Log(LogLevel.Exception, "Weder die PeronId noch die MemberNumber für den Rechnungsempfänger konnte geladen werden" + delivery.ToString)
                    InvokeLog(vbTab + "Fehler: Weder die PersonId noch die MemberNumber für den Rechnungsempfänger konnte geladen werden")
                    Return False
                End If
            End If

            ' eine leere DokPos in Liste laden, damit 1. leer --> damit alle eigentlichen Positionen bei AddDokument in Proffix geladen werden (pxBook verlangt, dass 1. leer ist)
            newDocPositions.Add(New pxBook.pxKommunikation.pxDokumentPos)

            ''AdressNr des Rechnungsempfängers auslesen
            'If delivery("RecipientDetails")("PersonId") Is Nothing Then
            '    Throw New Exception("Kein Rechnungsempfänger definiert für DeliveryId: " + delivery("DeliveryId").ToString)
            'End If

            ' existiert bereits ein Dok für diesen Tag für diesen Kunden?
            ' gibt zu bearbeitendes Dok zurück (aus Proffix, oder neu erstellt) + setzt Flag docExistInProffix entsprechend
            docToEdit = selectDocToEdit(delivery, docExistInProffix)
            If docToEdit.AdressNr = 0 Then
                InvokeLog(vbTab + "Fehler beim Erstellen eines neuen Dokumentes AdressNr: " + adressNr + " Flugdatum: " + If(delivery("FlightDate") IsNot Nothing, DateTime.Parse(delivery("FlightDate").ToString).ToShortDateString, ""))
                Return False
            End If

            ' heutiges Datum beim Dok als Referenztext anhängen
            'docToEdit.Referenztext += delivery("FlightDate").ToString

            ' als erste DocPos eine Textpos hinzufügen mit DeliveryInfos (AircraftImmatriculation, FlightType) + AdditionalInfo
            newTextPosition = createTextPos(delivery)
            newDocPositions.Add(newTextPosition)

            ' jeden Artikel des Deliveries durchgehen
            ' TODO: OrderBy DeliveryItems.Position to sort the items as in the rules
            For Each lineItem As JObject In delivery("DeliveryItems").Children()        '.OrderBy(delivery("DeliveryItems")("Position"))

                ' aus LineItem ein DocPos erstellen und zu Liste der bereits neu erstellten hinzufügen
                newDocPosition = createDocPos(docToEdit.DokumentNr, lineItem, deliveryId)
                If newDocPosition.ArtikelNr = String.Empty Then
                    InvokeLog(vbTab + "Fehler beim Erstellen der Dokumentposition")
                    Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Erstellen der Dokumentposition aus " + lineItem.ToString)
                    Return False
                End If
                newDocPositions.Add(newDocPosition)
            Next

            ' Daten in Proffix laden und Dokument bearbeiten/erstellen
            If Not loadDataInProffix(docExistInProffix, docToEdit, newDocPositions, deliveryId) Then
                InvokeLog(vbTab + "Fehler beim Verarbeiten des Dokuments AdressNr: " + docToEdit.AdressNr.ToString + " Flugdatum: " + docToEdit.Datum)
                Return False
            End If

            ' die ByRef Variable dokNr setzen (nötig für flagAsDelivered())
            dokNr = docToEdit.DokumentNr
            Return True
        Catch ex As Exception
            Logger.GetInstance.Log("Fehler in " + MethodBase.GetCurrentMethod.Name + " " + ex.Message)
            Return False
        End Try
    End Function

    '**************************************************************************Hilfsfunktionen*******************************************************************************

    ''' <summary>
    ''' gibtzu bearbeitendes Doc zurück (existierendes aus Proffix, oder neu erstelltes) + passt Flag docExistInProffix an
    ''' </summary>
    ''' <param name="delivery"></param>
    ''' <param name="docExistInProffix"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function selectDocToEdit(ByVal delivery As JObject, ByRef docExistInProffix As Boolean) As pxBook.pxKommunikation.pxDokument

        Dim docs() As pxBook.pxKommunikation.pxDokument = {}
        Dim rs As New ADODB.Recordset
        Dim sql As String = String.Empty
        Dim fehler As String = String.Empty
        Dim adressNr As String = String.Empty
        Dim flightDate As Date

        Try
            ' AdressNr auslesen
            If GetValOrDef(delivery, "RecipientDetails.PersonId") <> "" Then
                ' PersonId auslesen und die dazugehörige AdressNr aus PX laden
                adressNr = pxhelper.GetAdressNr(delivery("RecipientDetails")("PersonId").ToString)
            Else
                ' AdressNr direkt auslesen, da PersonId fehlt
                adressNr = GetValOrDef(delivery, "RecipientDetails.PersonClubMemberNumber")
            End If

            ' wenn kein FlightDatum enthalten, da z.B. Jahrespauschale --> heute als Datum
            If GetValOrDef(delivery, "FlightInformation.FlightDate") = "" Then
                flightDate = Now()
                ' sonst das FlightDate aus dem JSON auslesen
            Else
                flightDate = DateTime.Parse(delivery("FlightInformation")("FlightDate").ToString)
            End If

            ' ist bereits ein FLS-Dok vorhanden für den Tag/Kunden, welches noch nicht weiterverarbeitet (RG) wurde
            sql = "select auf_dokumente.dokumentnrauf from auf_dokumente left join auf_doklink on auf_dokumente.dokumentnrauf = auf_doklink.dokumentnrauf where " +
                            "auf_dokumente.doktypauf = 'FLSLS' and " +
                            "auf_dokumente.adressNradr = " + adressNr + " and " +
                            "auf_dokumente.datum = '" + flightDate.ToString(pxhelper.dateformat) + "' and " +
                            "auf_doklink.dokumentnrauf is null"
            If Not MyConn.getRecord(rs, sql, fehler) Then
                Throw New Exception("Fehler beim Abfragen, ob bereits Dok vorhanden für AdressNr " + If(GetValOrDef(delivery, "RecipientDetails.PersonId") = "", GetValOrDef(delivery, "RecipientDetails.PersonClubMemberNumber"), pxhelper.GetAdressNr(delivery("RecipientDetails")("PersonId").ToString)) + " und Datum " + DateTime.Parse(delivery("FlightDate").ToString).ToString(pxhelper.dateformat) + " " + fehler)
            End If

            ' für alle gefundenen Dokumente
            While Not rs.EOF

                ' das Dokument mit der ermittelten DokNr holen
                If Not Proffix.GoBook.GetDokument(docs, fehler, CInt(rs.Fields("dokumentnrauf").Value.ToString)) Then
                    Throw New Exception("Fehler beim Laden des Doks mit der DokNr " + rs.Fields("dokumentnrauf").Value.ToString + fehler)
                Else
                    ' Flag, dass Dok existiert = true setzen + return vorhandenes Dokument
                    docExistInProffix = True
                    Return docs(1)
                End If

                rs.MoveNext()
            End While

            ' wenn hierher gekommen = es wurde kein Dok für den Tag/Kunden gefunden, welches nicht bereits weiterverarbeitet wurde
            ' --> Flag auf false setuen + return ein neuerstelltes, leeres Dokument
            docExistInProffix = False
            Return createDoc(delivery)

        Catch ex As Exception
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + " " + ex.Message)
            Return New pxBook.pxKommunikation.pxDokument
            '            Throw New Exception(ex.Message)
        End Try
    End Function

    ''' <summary>
    ''' Ein Dokument erstellen
    ''' </summary>
    ''' <returns>Das erstellte PROFFIX Dokument</returns>
    Private Function createDoc(ByVal delivery As JObject) As pxBook.pxKommunikation.pxDokument
        Dim doc As New pxBook.pxKommunikation.pxDokument
        Dim adresse As New pxBook.pxKommunikation.pxAdressen

        Try
            ' Werte für Dokument setzen:
            If GetValOrDef(delivery, "RecipientDetails.PersonId") <> "" Then
                doc.AdressNr = CInt(pxhelper.GetAdressNr(delivery("RecipientDetails")("PersonId").ToString))
            Else
                doc.AdressNr = CInt(GetValOrDef(delivery, "RecipientDetails.PersonClubMemberNumber"))
            End If
            doc.DokumentTyp = "FLSLS"

            If GetValOrDef(delivery, "FlightInformation.FlightDate") = "" Then
                doc.Datum = Now().ToString(pxhelper.dateformat)
                doc.RDatum = Now().ToString(pxhelper.dateformat)
                doc.Referenztext = "Verrechnung: " + Now().ToString(pxhelper.dateformat)
            Else
                doc.Datum = GetValOrDef(delivery, "FlightInformation.FlightDate")
                doc.RDatum = GetValOrDef(delivery, "FlightInformation.FlightDate")
                doc.Referenztext = "Flugdatum: " + GetValOrDef(delivery, "FlightInformation.FlightDate").Substring(0, 10)
            End If
            Return doc

        Catch ex As Exception
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + " " + ex.ToString)
            Return Nothing
        End Try
    End Function

    ' erstellt eine DocPos
    Private Function createDocPos(ByVal dokNr As Integer, ByVal lineItem As JObject, ByVal deliveryId As String) As pxBook.pxKommunikation.pxDokumentPos
        Dim artikelNr As String = lineItem("ArticleNumber").ToString
        Dim menge As Decimal = CDec(lineItem("Quantity").ToString)
        Dim rabatt As Decimal = CDec(If(GetValOrDef(lineItem, "DiscountInPercent") = "", 0, lineItem("DiscountInPercent")))
        Dim zusatzfelder As String = "Z_DeliveryId = '" + deliveryId + "', Z_DeliveryItemId = '" + GetValOrDef(lineItem, "DeliveryItemId") + "'"

        Dim docpos As New pxBook.pxKommunikation.pxDokumentPos

        'Dim einheitpro As String = String.Empty
        'Dim rgeinheit As String = String.Empty
        'If Not GetEinheiten(artikelNr, einheitpro, rgeinheit) Then
        '    Return Nothing
        'End If

        '  dim preis as decimal = GetVerkauf1(lineItem("ArticleNumber").ToString)

        With docpos
            .DokumentNr = dokNr
            .ArtikelNr = artikelNr
            .Menge = menge    ' wenn in JSON Menge = 0 --> 1, anosnten die im JSON angegebene Menge
            .Rabatt = rabatt
            .Zusatzfelder = zusatzfelder
        End With
        ' bisher funktionierte es nur, wenn auch folgende Werte geladen wurden
        '  .MengeVerr = menge 'If(menge > 0, menge, 1)
        '   .Lagereinheit = einheitpro
        '  .Rechnungseinheit = rgeinheit
        '    .PreisSw = preis
        '    .PreisFw = preis

        '' Variante, wie am wenigsten Werte für Pos geladen werden müssen, die Preise aber korrekt übernommen werden
        ' funktioniert nicht
        'With docpos
        '    .Positionsart = pxBook.pxKommunikation.pxPositionsart.Artikel
        '    .DokumentNr = dokNr
        '    .ArtikelNr = artikelNr
        '    .Menge = menge
        'End With

        ' hat früher funktioniert
        'With docpos
        '    .DokumentNr = dokNr
        '    .ArtikelNr = artikelNr
        '    .Menge = menge
        '    .Zusatzfelder = zusatzfelder
        '    .Rabatt = rabatt
        'End With

        ' ' Debug Tests für Preis = 0 (JSON gibt mir in dem Fall Menge = 0)
        ' ' für alle Pos ausser 1 werden Testeshalber Menge = 0 gesetzt
        'If CInt(GetValOrDef(lineItem, "Position")) > 1 Then
        '    menge = 0
        '    With docpos
        '        .Menge = 0
        '        .MengeVerr = 0
        '        .PreisSw = 0
        '        .PreisFw = 0
        '        .TotalSw = 0
        '        .TotalFw = 0
        '    End With
        '    '    If CInt(GetValOrDef(lineItem, "Position")) > 2 Then
        '    '        With docpos
        '    '            .Menge = 1
        '    '            .MengeVerr = 1
        '    '            .PreisSw = CDec(0.0)
        '    '            .PreisFw = CDec(0.0)
        '    '            .TotalSw = 0
        '    '            .TotalFw = 0
        '    '        End With

        '    '    End If
        'End If

        Return docpos

    End Function

    ' Einheiten des Artikels laden
    Private Function GetEinheiten(ByVal artikelnr As String, ByRef lageinheit As String, ByRef rgeinheit As String) As Boolean
        Dim sql As String = String.Empty
        Dim rs As New ADODB.Recordset
        Dim fehler As String = String.Empty

        sql = "Select einheitpro, einheitrechnung from lag_artikel where artikelnrlag = '" + artikelnr + "'"
        If Not MyConn.getRecord(rs, sql, fehler) Then
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Laden der Einheiten")
            Return False
        End If

        ' prüfen, ob ein DS gefunden wurde
        If rs.EOF Then
            Logger.GetInstance.Log(LogLevel.Exception, "Kein Datensatz für Artikel " + artikelnr + " gefunden")
            Return False
        End If

        While Not rs.EOF
            Try
                lageinheit = rs.Fields("einheitPRO").Value.ToString
                rgeinheit = rs.Fields("einheitrechnung").Value.ToString
                Return True
            Catch ex As Exception
                Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Auslesen der Einheiten. ArtikelNr: " + artikelnr)
                Return False
            End Try
            rs.MoveNext()
        End While
    End Function

    Private Function GetVerkauf1(ByVal artikelNr As String) As Decimal
        Dim sql As String = "select verkauf1 from lag_artikel where artikelnrlag = '" + artikelNr + "'"
        Dim rs As New ADODB.Recordset
        Dim fehler As String = String.Empty
        Dim verkauf1 As Decimal

        If Not MyConn.getRecord(rs, sql, fehler) Then
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Laden des Verkaufspreis 1 von Artikel " + artikelNr)
            Return 0
        End If

        While Not rs.EOF
            verkauf1 = CDec(rs.Fields("verkauf1").Value)
            rs.MoveNext()
        End While

        Return verkauf1
    End Function

    ' erstellt eine TextPos, die einem Dok als TextPosition hinzugefügt werden kann
    Private Function createTextPos(ByVal delivery As JObject) As pxBook.pxKommunikation.pxDokumentPos
        Return New pxBook.pxKommunikation.pxDokumentPos With {
            .bezeichnung1 = GetValOrDef(delivery, "DeliveryInformation") + " " + If(GetValOrDef(delivery, "AdditionalInformation") <> "0", GetValOrDef(delivery, "AdditionalInformation"), GetValOrDef(delivery, "FlightInformation.AircraftImmatriculation")),
            .NotizenExtern = GetValOrDef(delivery, "DeliveryInformation") + " " + If(GetValOrDef(delivery, "AdditionalInformation") <> "0", GetValOrDef(delivery, "AdditionalInformation"), GetValOrDef(delivery, "FlightInformation.AircraftImmatriculation")),
            .Positionsart = pxBook.pxKommunikation.pxPositionsart.Text,
            .Zusatzfelder = "Z_DeliveryId = '" + delivery("DeliveryId").ToString + "'"
            }
    End Function

    '**********************************************************************************************************************************************************************
    ' lädt Daten in Proffix (entweder nur zusätzlich DocPos in ein existierendes Doc, oder ein neues Doc mit seinen DocPos, in dem Fall wird in docToEdit die neu erstellte DokumentNr eingetragen)
    Private Function loadDataInProffix(ByVal docExistInProffix As Boolean,
                                       ByRef docToEdit As pxBook.pxKommunikation.pxDokument,
                                       ByVal newDocPositions As List(Of pxBook.pxKommunikation.pxDokumentPos),
                                       ByVal deliveryId As String) As Boolean

        Dim rs As New ADODB.Recordset
        Dim sql As String = String.Empty
        Dim fehler As String = String.Empty

        ' wenn es sich um ein Dok handelt, welches bereits in PX existiert (bereits vor Programmausführung, oder ein bereits neu erstelltes, welches bereits in PX hochgeladen wurde)
        If docExistInProffix Then

            ' jede neue DocPos in PX laden
            For Each docpos In newDocPositions
                If Not (docpos.ArtikelNr Is Nothing And docpos.Bezeichnung1 Is Nothing) Then
                    If Not Proffix.GoBook.AddDokumentPos(docToEdit.DokumentNr, docpos, fehler) Then
                        InvokeLog(vbTab + "Beim Bearbeiten des existierenden Dokumentes " + docpos.DokumentNr.ToString + " ist ein Fehler aufgetreten. Artikel: " + docpos.ArtikelNr)
                        InvokeLog(vbTab + "Fehlermeldung: " + fehler)
                        Logger.GetInstance.Log(LogLevel.Exception, "Fehler in pxbook.AddDokumentPos() AdressNr: " + docToEdit.AdressNr.ToString + " Artikel: " + docpos.ArtikelNr + "für Dokument: " + docToEdit.DokumentNr.ToString + " pxBook Fehlermeldung: " + fehler)
                        Return False
                    Else
                        ' AddDokumentPos() gab return true
                        InvokeLog("Der Artikel " + docpos.ArtikelNr + " " + If(docpos.Bezeichnung1 IsNot Nothing, docpos.Bezeichnung1, "") + " wurde als Dokumentposition zum Dokument " + docToEdit.DokumentNr.ToString + " hinzugefügt.")
                        Logger.GetInstance.Log(LogLevel.Info, "Der Artikel " + docpos.ArtikelNr + " " + If(docpos.Bezeichnung1 IsNot Nothing, docpos.Bezeichnung1, "") + " wurde als Dokumentposition zum Dokument " + docToEdit.DokumentNr.ToString + " hinzugefügt.")
                        If Not String.IsNullOrEmpty(fehler) Then
                            ' diese Fehlermeldung kann ignoriert werden, wenn AddDokument() true zurückgegeben hat
                            Logger.GetInstance.Log(LogLevel.Exception, "pxBook Fehlermeldung wurde ignoriert, da true zurückgegeben wurde. Fehlermeldung: " + fehler)
                        End If
                    End If
                End If
            Next

            ' wenn Dokument in Proffix noch nicht existiert
        Else
            ' das neu erstellte Dokument mit den erstellten Positionen in Proffix laden
            If Not Proffix.GoBook.AddDokument(docToEdit, newDocPositions.ToArray, {}, fehler) Then
                If fehler.Contains("Kein Dokumenttyp gefunden!") Then
                    InvokeLog(vbTab + "Es existiert kein Dokumenttyp ""FLSLS"" in Proffix. Dieser muss erstellt werden, bevor neue Dokumente erstellt werden können")
                Else
                    InvokeLog(vbTab + "Beim Bearbeiten des neu erstellten Dokumentes ist ein Fehler aufgetreten. AdressNr: " + docToEdit.AdressNr.ToString + " DokumentNr: " + docToEdit.DokumentNr.ToString + " pxBook Fehlermeldung: " + fehler)
                    InvokeLog(vbTab + "Fehlermeldung: " + fehler)
                End If
                Logger.GetInstance.Log(LogLevel.Exception, "Fehler in pxbook.AddDokument() AdressNr: " + docToEdit.AdressNr.ToString + " pxBook Fehlermeldung: " + fehler)
                Return False

            Else
                Logger.GetInstance.Log(LogLevel.Info, "Für die AdressNr: " + docToEdit.AdressNr.ToString + " wurde für das Flugdatum: " + docToEdit.Datum.ToString().Substring(0, 10) + " ein Dokument " + docToEdit.DokumentNr.ToString + " erstellt.")
                InvokeLog("Für die AdressNr: " + docToEdit.AdressNr.ToString + " wurde für das Flugdatum: " + docToEdit.Datum.ToString().Substring(0, 10) + " ein Dokument " + docToEdit.DokumentNr.ToString + " erstellt.")

                ' Doktotal kontrollieren (wenn 0, ob dies so ok ist)
                If CheckDokTotal(docToEdit.DokumentNr) < 0 Then
                    InvokeLog(vbTab + "Fehler beim Prüfen, ob für das erstellte Dokument die Preise gesetzt werden konnten.")
                    Return False
                End If
            End If
        End If

        Return True
    End Function

    ' check if the total is > 0 and if = 0 if it is ok (as all articles have verkauf1 = 0)
    ' return: -1 = error, 0 = ok, 1 = false
    Private Function CheckDokTotal(ByVal dokNr As Integer) As Integer
        Dim sql As String
        Dim rs_total As New ADODB.Recordset
        Dim rs_verkauf As New ADODB.Recordset
        Dim fehler As String = String.Empty
        Dim totalsw As Decimal = 0
        Dim verkauf As Decimal = 0

        Try

            ' prüfen ob das Total des Docs > 0 ist
            sql = "select totalsw from auf_dokumente where dokumentnrauf = " + dokNr.ToString
            If Not MyConn.getRecord(rs_total, sql, fehler) Then
                Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Prüfen des Totals für Dokument " + dokNr.ToString)
                Return -1
            End If
            While Not rs_total.EOF
                totalsw = CDec(rs_total.Fields("TotalSW").Value)
                rs_total.MoveNext()
            End While

            ' das Dok hat ein Total > 0 = ok
            If totalsw > 0 Then
                Return 0

                ' Das Dok hat ein Total = 0 --> prüfen, ob das so ok ist (wenn alle Artikel Verkauf1 = 0 haben)
            Else
                ' check if there is at least 1 article in the doc, which has a verkauf1 > 0
                sql = "select sum(verkauf1) as verkauf from lag_artikel where artikelnrlag in (select distinct artikelnrlag from auf_dokumentpos where dokumentnrauf = " + dokNr.ToString + ")"
                If Not MyConn.getRecord(rs_verkauf, sql, fehler) Then
                    Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Bestimmen des Verkaufspreises der Artikel im Dok " + dokNr.ToString)
                    Return -1
                End If

                While Not rs_verkauf.EOF
                    verkauf = CInt(rs_verkauf.Fields("verkauf").Value)
                    rs_verkauf.MoveNext()
                End While

                ' wenn keiner der Artikel einen Preis hat --> Total = 0 ist ok
                If Not verkauf > 0 Then
                    Return 0

                    ' wenn mind. 1 der Artikel einen Verkauf1-Preis hat, ist Total = 0 falsch
                Else
                    MsgBox("Achtung: Das Dokument " + dokNr.ToString + " wurde zwar erstellt, die Preise der Artikel konnten jedoch nicht richtig gesetzt werden!", vbCritical)
                    InvokeLog(vbTab + "Achtung: Das Dokument " + dokNr.ToString + " wurde zwar erstellt, die Preise der Artikel konnten jedoch nicht richtig gesetzt werden!")
                    Logger.GetInstance.Log(LogLevel.Exception, "Dokument " + dokNr.ToString + " wurde zwar erstellt, die Preise konnten aber nicht richtig gesetzt werden")
                    Return 1
                End If
            End If

        Catch ex As Exception
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod.Name + " " + ex.Message)
            Return -1
        End Try
    End Function


    ' erstellt ein JSON und setzt in FLS den Delivery mit der DeliveryId und betroffener DocNr als Delivered (= in Proffix erfasst, aber noch nicht bezahlt)
    Private Function flagAsDelivered(ByVal delivery As JObject, ByVal dokNr As Integer) As Boolean
        Dim sql As String = String.Empty
        Dim rs As New ADODB.Recordset
        Dim fehler As String = String.Empty
        Dim json As New JObject
        Dim response As String = String.Empty

        Dim deliveryDateTime As String = String.Empty
        Try

            ' wenn der Lieferschein korrekt eingebucht wurde --> Erstellungsdatum des Lieferscheins laden
            If dokNr <> 0 Then

                ' das Erstellungsdatum für das Dokument in Proffix holen
                sql = "select erstelltAm from auf_dokumente where dokumentnrauf = " + dokNr.ToString
                If Not MyConn.getRecord(rs, sql, fehler) Then
                    Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Laden des Erstellungsdatum für die DokNr " + dokNr.ToString + " in " + MethodBase.GetCurrentMethod.Name)
                    Return False
                End If

                deliveryDateTime = (CDate(rs.Fields("erstelltAm").Value).ToUniversalTime.ToString("o"))

                ' der Lieferschein ist fehlerhaft --> mit DokNr = 0 als erledigt an FLS melden
            Else
                deliveryDateTime = Now.ToUniversalTime.ToString("o")
            End If

            ' JSON erstellen
            json("DeliveryId") = delivery("DeliveryId").ToString
            json("DeliveryDateTime") = deliveryDateTime
            json("DeliveryNumber") = dokNr.ToString

            ' JSON an FLS schicken
            response = client.submitFlag(My.Settings.ServiceAPIDeliveredMethod, json)
            If response <> "OK" Then
                Logger.GetInstance.Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod.Name + response)
                Return False
            End If
            Return True

        Catch ex As Exception
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod.Name + " " + ex.Message)
            Return False
        End Try
    End Function



    ' prüft, ob JSON Rechnungsinfos, PersonId bzw. Artikelinfos enthält. Wenn nicht --> dieser Delivery ist nicht verrechenbar
    Friend Function checkForCompleteDelivery(ByVal delivery As JObject) As Boolean

        ' abfangen ob RecipientDetails leer --> unklar, wer bezahlen muss 
        If delivery("RecipientDetails").ToString = "{}" Then
            InvokeLog(vbTab + "Fehler: Der Lieferschein aus FLS enthält keine Daten zum Rechnungsempfänger. DeliveryId: " + delivery("DeliveryId").ToString)
            Logger.GetInstance.Log(LogLevel.Exception, "Im JSON ist RecipientDetails leer " + delivery.ToString)
            Return False
        End If

        ' prüfen, ob eine PersonId oder MemberNumber für Rechnungsempfänger vorhanden ist
        If GetValOrDef(delivery, "RecipientDetails.PersonId") = "" Then
            If GetValOrDef(delivery, "RecipientDetails.PersonClubMemberNumber") = "" Then
                InvokeLog(vbTab + "Fehler: Der Lieferschein aus FLS enthält weder eine PersonId noch eine MenberNumber für den Rechnungsempfänger. DeliveryId: " + delivery("DeliveryId").ToString)
                Logger.GetInstance.Log(LogLevel.Exception, "Lieferschein ohne Rechnungesmpfänger: PersonId und MemberNumber. " + delivery.ToString)
                Return False
            End If
        End If

        ' prüfen, ob delivery überhaupt Artikel enthält (wenn nicht, ist nicht definiert, welche Artikel mit diesem Flug verknüpft sind
        If delivery("DeliveryItems").Count = 0 Then
            InvokeLog(vbTab + "Fehler: Für diesen Lieferschein " + delivery("DeliveryId").ToString + " ist nicht definiert, welche Artikel in Proffix verwendet werden sollen. In FLS muss durch den Administrator zuerst festgelegt werden, welche Artikel für diesen Flug verrechnet werden sollen.")
            Logger.GetInstance.Log(LogLevel.Exception, vbTab + "Für diesen Lieferschein ist nicht definiert, welche Artikel in Proffix verwendet werden sollen. JSON enthält keine Artikel " + delivery.ToString)
            Return False
        End If

        Return True
    End Function

    ''' <summary>
    ''' Schreibt den mitgegebenen Text in das Log in der Benutzeroberfläche und alenfalls in die Logdatei
    ''' </summary>
    ''' <param name="message">Den auszugebenden Text</param>
    ''' <param name="doWriteLogFile">Definiert ob der Text auch in das Logfile geschrieben wird</param>
    ''' <param name="logLevel">Das Loglevel mit dem die Nachricht geloggt wird</param>
    Private Sub InvokeLog(ByVal message As String, Optional ByVal doWriteLogFile As Boolean = False, Optional ByVal logLevel As LogLevel = LogLevel.Info)
        If Log IsNot Nothing Then Log.Invoke(message)
        If doWriteLogFile Then Logger.GetInstance.Log(logLevel, message)
    End Sub

    Private Sub InvokeDoProgressDelivery()
        If DoProgressDelivery IsNot Nothing Then DoProgressDelivery.Invoke()
    End Sub

    Private Sub InvokeDoProgressFlight()
        If DoProgressFlight IsNot Nothing Then DoProgressFlight.Invoke()
    End Sub

End Class

'using System;
'using System.Collections.Generic;
'using System.ComponentModel.DataAnnotations;

'Namespace FLS.Data.WebApi.Accounting
'{
'    public class DeliveryDetails : FLSBaseData
'    {
'        Public DeliveryDetails()
'        {
'            RecipientDetails = new RecipientDetails();
'            FlightInformation = new FlightInformation();
'            DeliveryItems = new List<DeliveryItemDetails>();
'        }

'        public Guid DeliveryId { get; set; }

'        public FlightInformation FlightInformation { get; set; }

'        public RecipientDetails RecipientDetails { get; set; }

'        [StringLength(250)]
'        public string DeliveryInformation { get; set; }

'        [StringLength(250)]
'        public string AdditionalInformation { get; set; }

'        public List<DeliveryItemDetails> DeliveryItems { get; set; }

'        public string DeliveryNumber { get; set; }

'        public DateTime? DeliveredOn { get; set; }

'        public bool IsFurtherProcessed { get; set; }

'        public override Guid Id
'        {
'            get { return DeliveryId; }
'            set { DeliveryId = value; }
'        }
'    }
'}
