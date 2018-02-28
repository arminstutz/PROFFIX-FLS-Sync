'Imports System.ServiceModel
Imports System.Threading
Imports System.Net.Http

Imports pxModul
Imports pxBook

Imports SMC.Lib

Imports Newtonsoft.Json.Linq
Imports System.Text.RegularExpressions
Imports System.ServiceModel
Imports System.Reflection


''' <summary>
''' Die Managerklasse zur Synchronisation der Adressen
''' </summary>
Public Class Syncer

    ''' <summary>
    ''' Das private HttpClient Objekt
    ''' </summary>
    Private Property _httpClient As HttpClient
    ''' <summary>
    ''' Den Client des FLS
    ''' </summary>
    Private _serviceClient As FlsConnection
    Private pxhelper As ProffixHelper
    Private myconn As ProffixConnection
    Private generalLoader As GeneralDataLoader

    Private loader As PersonLoader
    Private updater As PersonUpdater
    Private creator As PersonCreator
    Private deleter As PersonDeleter

    Private rs_adressdefault As New ADODB.Recordset
    ''' <summary>
    ''' Die letzte Adressynchronisation
    ''' </summary>
    Private _lastSync As DateTime = Nothing
    Public Property LastSync As DateTime
        Get
            Return _lastSync
        End Get
        Set(ByVal value As DateTime)
            _lastSync = value
        End Set
    End Property

    Public Property DoProgress As Action    ' Aktion, die ausgeführt wird, wenn die Synchronisation Fortschritte macht
    Private _progress As Integer    ' Fortschritt der Synchronisation anzeigen
    Public Property Progress As Integer
        Get
            Return _progress
        End Get
        Private Set(ByVal value As Integer)
            _progress = value
        End Set
    End Property

    Private _count As Integer
    Public Property Count As Integer
        Get
            Return _count
        End Get
        Private Set(ByVal value As Integer)
            _count = value
        End Set
    End Property

    Public Property Log As Action(Of String)    ' Aktion, die ausgeführt wird, wenn in Logfeld geschrieben wird

    ''' <summary>
    ''' Initialisiert ein neues Objekt
    ''' </summary>
    ''' <param name="lastSync">Der Zeitpunkt der letzten Synchronisation</param>
    ''' <param name="serviceClient">Der Client des FLS</param>
    Public Sub New(ByVal lastSync As DateTime, serviceClient As FlsConnection, ByRef pxHelper As ProffixHelper, ByRef MyConn As ProffixConnection, ByVal generalLoader As GeneralDataLoader)
        Me._serviceClient = serviceClient
        Me.LastSync = lastSync
        Me.pxhelper = pxHelper
        Me.myconn = MyConn
        Me.generalLoader = generalLoader
    End Sub

    ''' <summary>
    ''' Ausführen der Adressen-Synchronisation
    ''' </summary>
    ''' <returns>Ein Boolean der definiert ob die Synchronisation erfolgreich war</returns>
    Public Function Sync() As Boolean
        Dim successful As Boolean = True                        ' wird während Synchronisation auf false gesetzt, sobald für 1 Adresse ein Fehler auftritt
        Dim fehler As String = String.Empty
        Dim FLShardDeletedPersons As New List(Of JObject)       ' alle jemals in FLS hart gelöschten Personen
        Dim FLSPersons As New List(Of JObject)                  ' alle in FLS vorhandenen Personen (IsActive true oder false)
        Dim PXadressen As New List(Of pxBook.pxKommunikation.pxAdressen)    ' alle in Proffix vorhandenen Adressen (geloescht 0 oder 1)
        Dim addressWorkProgress = New Dictionary(Of Integer, Boolean)
        'Dim sinceDate As DateTime

        Try

            ' abfangen, wenn kein LastSync geladen wurde
            If LastSync = Nothing Then
                logComplete("Es wurde kein LastSync-Datum gefunden.", LogLevel.Exception)
                successful = False
                Return False
            End If

            ' Standardwerte für Adressen laden
            rs_adressdefault = pxhelper.GetPXAdressDefaultValues()
            If rs_adressdefault Is Nothing Then
                logComplete("Fehler beim Laden der Defaultwerte für Adressen", LogLevel.Exception)
                Return False
            End If

            ' Objekte für Synchronisation erstellen
            loader = New PersonLoader(_serviceClient, Log)
            updater = New PersonUpdater(myconn, rs_adressdefault, pxhelper, _serviceClient, Log)
            deleter = New PersonDeleter(_serviceClient, pxhelper, Log)
            creator = New PersonCreator(myconn, rs_adressdefault, pxhelper, _serviceClient, LastSync, Log, updater, deleter)

            'Benutzerrückmeldung
            logComplete("Adresssynchronisation gestartet", LogLevel.Info)
            Progress = 0
            InvokeDoProgress()

            ' alle geladenen Personen in die Zusatztabelle laden (damit man auch in Proffix die Verbindung zwischen PersonId und Name sieht)
            If Not generalLoader.importPersons() Then
                logComplete("Fehler beim Importieren der Daten in ZUS_FLSPersons", LogLevel.Exception)
            End If

            '*********************************************************************alle Daten laden***********************************************************************
            ' lädt alle in die Funktion übergegebene Listen mit Werten
            If Not Loader.datenLaden(FLShardDeletedPersons, FLSPersons, PXadressen) Then
                logComplete("Fehler beim Laden der Daten", LogLevel.Exception)
                Return False
            End If

            If logAusfuehrlich Then
                Logger.GetInstance.Log(LogLevel.Info, "Anzahl FLS Adressen, die geladen wurden: " + FLSPersons.Count.ToString)
                Logger.GetInstance.Log(LogLevel.Info, "Anzahl PX Adressen, die geladen wurden: " + PXadressen.Count.ToString)
            End If

            ' ******************************************************************Synchronisation********************************************************************************
            ' Anzahl Adressen aus FLS und Proffix zusammenzählen
            Count = FLSPersons.Count + PXadressen.Count
            ' jede Adresse aus FLS (person) durchgehen
            For Each person As JObject In FLSPersons

                Try

                    If logAusfuehrlich Then
                        Logger.GetInstance.Log(LogLevel.Info, "Geprüft wird FLS-Adresse Nachname: " + person("Lastname").ToString + " Vorname: " + person("Firstname").ToString)
                    End If

                    ' prüfen, ob club-abhängige Daten erhalten (wenn der User in FLS System-Admin-Rechte hat, enthalten die JSONS keine clubrelevanten Daten)
                    If person("ClubRelatedPersonDetails") Is Nothing Then
                        logComplete("Für die Personen wurden von FLS keine club-abhängigen Daten geliefert. Die Adresssynchronisation kann nicht ausgeführt werden. Kontaktieren Sie den Support", LogLevel.Exception, "FLS liefert keine clubrelatedPersonDetails. Möglicher Grund: User ist System-Admin. --> Kontrollieren in FLS unter Benutzer")
                        Return False
                    End If

                    Dim existsInProffix As Boolean = False          ' Default setzen (wird später nur auf True gesetzt, wenn in PX gefunden)

                    ' PersonId und Änderungsdatum auslesen
                    Dim flsPersonId As String = person("PersonId").ToString.ToLower.Trim

                    ' Adresse suchen, die dieselbe PersonId hat
                    Dim adressemitPersonId As IEnumerable(Of pxKommunikation.pxAdressen) = From address As pxKommunikation.pxAdressen In PXadressen Where flsPersonId = ProffixHelper.GetZusatzFelder(address, "Z_FLSPersonId").ToLower.Trim

                    ' nur noch die eine bereits gefundene pxadresse mit der relevanten Personid abarbeiten
                    For Each address As pxKommunikation.pxAdressen In adressemitPersonId

                        ' die FLS-Adresse mit jeder Adresse aus Proffix vergleichen
                        '  For Each address As pxKommunikation.pxAdressen In PXadressen


                        If logAusfuehrlich Then
                            Logger.GetInstance.Log(LogLevel.Info, "Verglichen werden FLS-Adresse Nachname: " + person("Lastname").ToString + " Vorname: " + person("Firstname").ToString + " mit PX-Adresse " + address.AdressNr.ToString + " Nachname: " + address.Name)
                        End If
                        Try

                            ' PersonId auslesen
                            Dim proffixFLSPersonId As String = ProffixHelper.GetZusatzFelder(address, "Z_FLSPersonId")

                            ' wenn PersonID (FLS) einen Wert hat --> Adresse in FLS neu erstellt, oder bereits synchronisiert 
                            '--> ist PersonID (FLS) = ZF_FLSPersonId (Proffix) --> Adresse aus FLS ist in Proffix bereits vorhanden
                            ' wenn die Werte PersonID (FLS) und Zusatzfeld FLSPersonId (Proffix) identisch und PersonID (FLS) hat einen Wert...
                            If ((proffixFLSPersonId = flsPersonId) And Not String.IsNullOrEmpty(flsPersonId)) Then

                                '*************************************************************in beiden vorhanden --> update*********************************************************************************************
                                existsInProffix = True

                                ' AdressNr der Proffixadresse zu _addressWorkProgress (enhält alle pxadressen, die bereits in FLS vorhanden sind) hinzufügen
                                addressWorkProgress.Add(address.AdressNr, True)

                                ' prüfen, ob die Adresse synchronisiert werden soll (erst nach existInPX = true prüfen, da sonst = false bleibt --> geht in NurInFLS()
                                Dim synchronizeOk As Integer
                                Dim Z_sync_dbvalue = ProffixHelper.GetZusatzFelder(address, "Z_Synchronisieren")
                                Try
                                    synchronizeOk = CInt(Z_sync_dbvalue)    ' 0 oder 1 auslesen (catch, falls null) --> gilt als zu synchroniseren
                                Catch ex As Exception
                                    synchronizeOk = 1
                                End Try
                                If CInt(synchronizeOk) = 1 Then

                                    '  prüfen, ob AdressNr der PXAdresse NICHT mit der AdressNr der FLSPerson übereinstimmt oder in FLS NICHT NULL ist (wenn Adresse IsActive = false)
                                    If Not GetValOrDef(person, "ClubRelatedPersonDetails.MemberNumber") = address.AdressNr.ToString Then
                                        If Not GetValOrDef(person, "ClubRelatedPersonDetails.MemberNumber") = "" Then
                                            logComplete("Die AdressNr in PX: " + address.AdressNr.ToString + " stimmt nicht überein mit der VereinsmitgliedNr in FLS: " + GetValOrDef(person, "ClubRelatedPersonDetails.MemberNumber") +
                                                        " gemeinsame PersonId: " + person("PersonId").ToString.ToLower.Trim + "FLS-person: " + person.ToString, LogLevel.Exception)
                                            Exit For
                                        End If
                                    End If


                                    ' die Adressen, welche in beiden Systemen bereits vorhanden sind, entsprechend updaten
                                    If Not updater.update(person, address, LastSync) Then
                                        successful = False
                                    End If

                                    Progress += 1
                                    InvokeDoProgress()
                                End If
                                Exit For
                            End If

                        Catch ex As Exception
                            logComplete("Fehler beim updaten " + ex.Message + " " + person.ToString, LogLevel.Exception)
                            successful = False
                            Continue For
                        End Try

                    Next            ' nächste Proffixadresse (address)

                    '***********************************************************************nur in FLS ****************************************************************
                    ' wenn PersonID leer oder PersonID (FLS)-Wert in Proffix unter ZF_FLSPersonId nicht gefunden wurde 
                    ' --> Adresse noch nicht in Proffix vorhanden
                    If existsInProffix = False Then
                        If logAusfuehrlich Then
                            Logger.GetInstance.Log(LogLevel.Info, "nur in FLS vorhanden: Nachname: " + person("Lastname").ToString + " Vorname: " + person("Firstname").ToString)
                        End If

                        Try

                            ' prüfen, ob die Adresse nicht bereits gelöscht vorhanden ist bzw. ob keine Duplikate entstehen
                            If Not creator.NurInFLS(person) Then
                                successful = False
                            End If

                        Catch ex As Exception
                            logComplete("Fehler beim Erstellen in Proffix " + ex.Message + " " + person.ToString, LogLevel.Exception)
                            successful = False
                        End Try
                    End If

                    Progress += 1
                    InvokeDoProgress()

                Catch ex As Exception
                    logComplete("Fehler beim Prüfen der Adresse " + ex.Message + " " + person.ToString, LogLevel.Exception)
                    successful = False
                    Continue For
                End Try

            Next        ' nächste FLS-Adresse (person)

         

            '**************************************************************************************CREATE IN FLS + PERSONID IN PROFFIX
            ' _AddressWorkProgress enthält die AdressNr (Proffix) der Adressen, die in beiden sind.
            ' --> Wenn anfangs weniger Adressen an beiden Orten vorhanden waren als in Proffix anfangs Adressen vorhanden waren 
            ' --> es gibt noch Adressen, die nur in Proffix sind --> in FLS hinzufügen
            If (addressWorkProgress.Count < PXadressen.Count) Then

                If logAusfuehrlich Then
                    Logger.GetInstance.Log(LogLevel.Info, "Verarbeitung von Adressen, die nur in PX vorhanden sind...")
                End If

                ' jede Adresse aus Proffix durchgehen
                For Each address As pxKommunikation.pxAdressen In PXadressen
                    Try

                        If logAusfuehrlich Then
                            Logger.GetInstance.Log(LogLevel.Info, "Nur in PX vorhanden: " + address.AdressNr.ToString + " Nachname: " + address.Name)
                        End If

                        ' wenn Adresse bisher nur in Proffix vorhanden + seit LastSync verändert wurde + Synchronisieren ok ist
                        Dim existsOnlyInPx As Boolean = Not addressWorkProgress.ContainsKey(address.AdressNr)
                        Dim Z_sync_dbvalue = ProffixHelper.GetZusatzFelder(address, "Z_Synchronisieren")
                        Dim synchronizeok As Integer
                        Try
                            synchronizeok = CInt(Z_sync_dbvalue)    ' 0 oder 1 auslesen (catch, falls null) --> gilt als zu synchroniseren
                        Catch ex As Exception
                            synchronizeok = 1
                        End Try

                        Try
                            ' wenn die Adresse nur in PX existiert und synchronisiert werden soll
                            If existsOnlyInPx And synchronizeok = 1 Then
                                If Not creator.NurInPX(address, FLShardDeletedPersons) Then
                                    successful = False
                                End If
                                Progress += 1
                                InvokeDoProgress()
                            End If
                        Catch ex As Exception
                            logComplete("Fehler beim Erstellen in FLS " + ex.Message + " PX-AdressNr: " + address.AdressNr.ToString, LogLevel.Exception)
                            successful = False
                        End Try
                    Catch ex As Exception
                        Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Verarbeiten von PX-Adresse (existiert nur in PX) AdressNr: " + address.AdressNr.ToString + " Name: " + address.Name)
                    End Try
                Next
              
            End If
            Progress = Count
            InvokeDoProgress()

            ' wenn bis herhin alles geklappt --> geklappt immer noch true
            If successful Then

                logComplete("Adresssynchronsation erfolgreich beendet", LogLevel.Info)
                LastSync = DateTime.Now
            Else
                logComplete("Bei der Adresssynchronisation ist mindestens 1 Fehler aufgetreten. Deshalb wird das Datum der letzten Synchronsiation nicht angepasst.", LogLevel.Exception)
            End If

            logComplete("", LogLevel.Info)

            Return successful
        Catch faultExce As FaultException
            Logger.GetInstance().Log(LogLevel.Exception, faultExce)
            Throw faultExce
            'End If
        Catch exce As Exception
            Logger.GetInstance().Log(LogLevel.Exception, exce)
            Throw exce
        End Try
    End Function

    ' schreibt in Log und in Logger (File)
    Private Sub logComplete(ByVal logString As String, ByVal loglevel As LogLevel, Optional ByVal zusatzloggerString As String = "")
        If Log IsNot Nothing Then Log.Invoke(If(loglevel <> loglevel.Info, vbTab, "") + logString)
        Logger.GetInstance.Log(loglevel, logString + " " + zusatzloggerString)
    End Sub

    ''' <summary>
    ''' Synchronisationsfortschritt anzeigen
    ''' </summary>
    Private Sub InvokeDoProgress()
        If DoProgress IsNot Nothing Then DoProgress.Invoke()
    End Sub

End Class


