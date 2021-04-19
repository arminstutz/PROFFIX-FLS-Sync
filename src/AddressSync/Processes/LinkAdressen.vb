

'Imports System.ServiceModel
Imports System.Threading
Imports System.Net
Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Net.Http.HttpResponseMessage

Imports pxModul
Imports pxBook

Imports SMC.Lib

Imports Newtonsoft.Json.Linq
Imports System.Text.RegularExpressions
Imports System.ServiceModel
Imports System.Reflection

Public Enum SyncerCommitCommand
    Update
    Create
    Delete
End Enum

' verknüpft die Adressen aus FLS und Proffix anhand der Vor- und Nachnamen
' muss vor der 1. Synchronisation ausgeführt werden

Public Class LinkAdressen
    Private _progress As Integer
    Private _count As Integer
    Private Property client As HttpClient
    Private _serviceClient As FlsConnection
    Private pxhelper As ProffixHelper
    Private myconn As ProffixConnection
    Private _generalLoader As GeneralDataLoader

    Private Property lastSync As DateTime
    Private _addressWorkProgress As Dictionary(Of Integer, Boolean)
    Private Shared _laender_dict As New Dictionary(Of String, String)
    Public Property DoProgress As Action
    Public Property Log As Action(Of String)

    Public ReadOnly Property AddressWorkProgress() As Dictionary(Of Integer, Boolean)
        Get
            Return _addressWorkProgress
        End Get
    End Property

    Public Property Progress As Integer
        Get
            Return _progress
        End Get
        Private Set(ByVal value As Integer)
            _progress = value
        End Set
    End Property

    Public Property Count As Integer
        Get
            Return _count
        End Get
        Private Set(ByVal value As Integer)
            _count = value
        End Set
    End Property

    Public Sub New(ByVal lastSync As DateTime, serviceClient As FlsConnection, ByRef pxHelper As ProffixHelper, ByRef MyConn As ProffixConnection, ByVal generalLoader As GeneralDataLoader)
        _serviceClient = serviceClient
        Me.pxhelper = pxHelper
        Me.myconn = MyConn
        _generalLoader = generalLoader
        Me.lastSync = lastSync
    End Sub


    '**************************************************************************************Verlinken
    ' Synchronisiert die PersonId und Adressnr/VereinsmitgliedNr anhand der Vor und Nachnamen
    Public Function Link() As Boolean
        Dim sql As String = String.Empty
        Dim fehler As String = String.Empty
        Dim response_FLS As String = String.Empty
        Dim geklappt As Boolean = True
        Dim adressList As List(Of pxBook.pxKommunikation.pxAdressen)
        Dim IsSamePerson As Boolean = False

        Try

            InvokeLog("Adressverknüpfung gestartet")
            Logger.GetInstance.Log(LogLevel.Info, "Adressverknüpfung gestartet. Sich nach Name und Adresse entsprechende Adressen aus FLS und Proffix werden verknüpft")
            Progress = 0
            InvokeDoProgress()

            '---------------------------------------------------------------------Daten holen-------------------------------------------------------
            Dim errorMessage As String = String.Empty
            Dim existsInProffix As Boolean = False

            _addressWorkProgress = New Dictionary(Of Integer, Boolean)

            ' Alle ungelöschten FLS Adressen holen
            Dim personResult As Threading.Tasks.Task(Of JArray) = _serviceClient.CallAsyncAsJArray(My.Settings.ServiceAPIModifiedPersonFullDetailsMethod + DateTime.MinValue.ToString("yyyy-MM-dd"))
            personResult.Wait()

            ' um Zeit zu sparen werden nicht pber pxBook sondern über adodb gekürzte (nur Name, Adresse, PersonId, AdressNr in pxAdressen ausgefüllt) geladen 
            ' --> nur wenn wirklich verknüpft werden soll, wird für diese eine Adresse die ganze Adresse über pxBook geladen
            adressList = gekuerztePXAdressenLaden()

            ' Anzahl Adressen aus FLS und Proffix zusammenzählen
            Count = adressList.Count + personResult.Result.Count

            '---------------------------------------------------------Daten vergleichen und synchronisieren-------------------------------------------------------------------
            ' jede Adresse aus FLS (person) durchgehen
            For Each person As JObject In personResult.Result.Children()

                ' es interessieren nur Adressen, die nach lastSync erstellt wurden
                If CDate(FlsHelper.GetPersonChangeDate(person)) > lastSync Then

                    ' die FLS-Adresse mit jeder Adresse aus Proffix vergleichen
                    For Each address As pxKommunikation.pxAdressen In adressList
                        IsSamePerson = False
                        Dim addressChangeDate As DateTime = ProffixHelper.GetAddressChangeDate(address)
                        ' es interessieren nur Adressen, die nach lastSync bearbeitet/erstellt wurden
                        If CDate(addressChangeDate) > lastSync Then

                            Dim personname As String = GetValOrDef(person, "Lastname")
                            Dim personvorname As String = GetValOrDef(person, "Firstname")
                            Dim addressname As String = address.Name
                            Dim addressvorname As String = address.Vorname

                            ' wenn es laut Name/Vorname die gleichen Adressen sind
                            If personname.Trim() = addressname.Trim() And personvorname.Trim() = addressvorname.Trim() Then

                                Dim personIdAusFLS As String = person("PersonId").ToString
                                Dim adressNrAusFLS As String = GetValOrDef(person, "ClubRelatedPersonDetails.MemberNumber")
                                Dim personIdAusPX As String = pxhelper.GetPersonId(address.AdressNr.ToString)
                                Dim adressNrAusPX As String = address.AdressNr.ToString

                                ' wenn bereits richtig verknüpft --> nichts machen + unnötig zu prüfen, ob es gleiche Person ist
                                If (adressNrAusFLS = adressNrAusPX) And personIdAusFLS = personIdAusPX Then
                                    Exit For
                                End If

                                ' die Adressen sind noch nicht verknüpft

                                ' es interessieren nur Adressen, die noch nie verknüpft wurden
                                If adressNrAusFLS = String.Empty And personIdAusPX = String.Empty Then

                                    ' prüfen ob gleiche Person (Strasse + Ort)
                                    If (GetValOrDef(person, "AddressLine1").Trim() = address.Strasse.Trim() _
                                        Or GetValOrDef(person, "AddressLine1").Trim() = address.Strasse.Trim().Replace("str.", "strasse")) _
                                        And GetValOrDef(person, "City").Trim() = address.Ort.Trim() Then

                                        ' es ist die gleiche Person
                                        IsSamePerson = True
                                    Else
                                        ' Vor- + Nachname + PLZ stimmen überein. Wenn auch noch PLZ übereinstimmt --> User fragen ob gleiche Person
                                        If GetValOrDef(person, "ZipCode") = address.Plz Then

                                            Dim dialogres As DialogResult = MessageBox.Show("Handelt es sich bei folgenden Adressen um dieselbe Person?" + vbCrLf +
                                                             vbCrLf +
                                                                "Adresse aus FLS:" + vbCrLf +
                                                                "PersonId: " + personIdAusFLS + vbCrLf +
                                                                "AdressNr: " + adressNrAusFLS + vbCrLf +
                                                                "Name: " + personname + vbCrLf +
                                                                "Vorname: " + personvorname + vbCrLf +
                                                                "Strasse: " + GetValOrDef(person, "AddressLine1") + vbCrLf +
                                                                "PLZ: " + GetValOrDef(person, "ZipCode") + vbCrLf +
                                                                "Ort: " + GetValOrDef(person, "City") + vbCrLf +
                                                                vbCrLf +
                                                                "Adresse aus Proffix:" + vbCrLf +
                                                                "AdressNr: " + adressNrAusPX + vbCrLf +
                                                                "PersonId: " + personIdAusPX + vbCrLf +
                                                                "Name: " + addressname + vbCrLf +
                                                                "Vorname: " + addressvorname + vbCrLf +
                                                                "Strasse: " + address.Strasse + vbCrLf +
                                                                "PLZ: " + address.Plz + vbCrLf +
                                                                "Ort: " + address.Ort + vbCrLf + vbCrLf + vbCrLf +
                                                                "Klicken Sie ja, wenn es sich um dieselbe Person handelt, und die Adressen verknüpft werden sollen. " + vbCrLf +
                                                                "Klicken Sie nein, wenn es sich nicht um dieselbe Person handelt. Sie werden dann jeweils im anderen System neu erstellt. " + vbCrLf +
                                                                "Klicken Sie Abbrechen, um den Vorgang abzubrechen", "Dieselbe Person?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2)

                                            ' wenn User angibt, dass selbe Person --> verknüpfen
                                            If dialogres = DialogResult.Yes Then
                                                IsSamePerson = True
                                                ' wenn User angibt, dass nicht selbe Person (dialogresult.no) --> nichts machen --> Personen werden bei AdressSynchronisation im anderen System erstellt
                                            ElseIf dialogres = DialogResult.No Then
                                                ' wenn User abbricht oder Fenster schliesst --> Verknüpfung abbrechen
                                            Else
                                                Return False
                                            End If
                                        End If
                                    End If

                                    ' wenn es sich um gleiche Person handelt (laut Programm, oder laut User)
                                    If IsSamePerson Then
                                        If Not verknuepfen(person, address) Then
                                            geklappt = False
                                        End If
                                    End If
                                End If
                            End If
                            End If
                    Next
                End If
            Next

            ' Adressen, die nur in einem System vorhanden sind, werden hier ignoriert --> werden bei Adresssynchronisation im anderen System erstellt

            ' wenn bis herhin alles geklappt --> geklappt immer noch true
            If geklappt = True Then
                InvokeLog("Adressverknüpfung erfolgreich abgeschlossen")
            Else
                InvokeLog("Mindestens 1 Fehler beim Verknüpfen der Adressen")
            End If
            InvokeLog("")

            Progress = Count
            InvokeDoProgress()

            Logger.GetInstance.Log(LogLevel.Info, "Adressverknüpfung beendet")

            Return geklappt
        Catch faultExce As FaultException
            Logger.GetInstance().Log(LogLevel.Exception, faultExce)
            Throw faultExce
            'End If
        Catch exce As Exception
            Logger.GetInstance().Log(LogLevel.Exception, exce)
            Throw
        End Try
    End Function

    ' verküpft 2 Adressen
    Private Function verknuepfen(ByVal person As JObject, ByVal address As pxKommunikation.pxAdressen) As Boolean
        Dim fehler As String = String.Empty

        Try

            Dim vollstaendigeAdresse As pxKommunikation.pxAdressen() = New pxKommunikation.pxAdressen() {}

            ' ganze Adresse aus PX holen
            If Not Proffix.GoBook.GetAdresse(pxKommunikation.pxAdressSuchTyp.AdressNr, address.AdressNr.ToString, vollstaendigeAdresse, fehler) Then
                ' die Adresse ist nicht unter den ungelöschten
                If Not Proffix.GoBook.GetAdresse(pxKommunikation.pxAdressSuchTyp.AdressNr, address.AdressNr.ToString, vollstaendigeAdresse, fehler, pxKommunikation.pxGeloeschte.Geloeschte) Then
                    ' die Adresse ist nicht unter den ungelöschten oder gelöschten --> gar nicht vorhanden
                    Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Laden der Adresse für AdressNr " + address.AdressNr.ToString + " " + fehler)
                    Return False
                End If
            End If

            ' wenn mehr als eine Adresse (+ die leere an Position 0) geladen wurde --> mehrere Adressen haben dieselbe PersonId --> Fehler
            If vollstaendigeAdresse.Count > 2 Then
                Logger.GetInstance.Log(LogLevel.Exception, "Es wurde mehr als 1 Adresse geladen für die AdressNr: " + address.AdressNr.ToString)
            End If

            ' PersonId aus FLS in PX schreiben
            If Not pxAktualisieren(person, vollstaendigeAdresse(1)) Then
                Throw New Exception("Fehler in pxAktualisieren")
            End If

            ' AdressNr aus PX in FLS schreiben
            If Not flsAktualisieren(person, vollstaendigeAdresse(1)) Then
                Throw New Exception("Fehler in flsAktualisieren")
            End If

            'Meldung dass erfolgreich
            InvokeLog("Name: " + address.Name + " Vorname: " + address.Vorname + " AdressNr: " + address.AdressNr.ToString + " PersonId: " + person("PersonId").ToString + " wurde verknüpft")
            Logger.GetInstance.Log(LogLevel.Info, "Name: " + address.Name + " Vorname: " + address.Vorname + " AdressNr: " + address.AdressNr.ToString + " PersonId: " + person("PersonId").ToString + "wurde verknüpft")
            Return True

        Catch ex As Exception
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod.Name + " " + ex.Message)
            InvokeLog(vbTab + "Fehler beim Verknüpfen der Adressen. PersonId:" + person("PersonId").ToString + " AdressNr: " + address.AdressNr.ToString + " Name: " + address.Name + " Vorname: " + address.Vorname)

            Return False
        End Try
    End Function


    ' alle Adressen aus PX laden, egal ob geloeshct = 0 oder 1
    Private Function gekuerztePXAdressenLaden() As List(Of pxKommunikation.pxAdressen)
        Dim adressList As New List(Of pxKommunikation.pxAdressen)
        Dim sql As String = String.Empty
        Dim rs As New ADODB.Recordset
        Dim fehler As String = String.Empty
        'Dim syncOk As Integer   ' ist die Adresse in PX als zu synchronisieren markiert (Zuatzfeld Z_Synchronisieren = 1)

        Try
            sql = "select * from adr_adressen where Z_synchronisieren = 1"
            If Not myconn.getRecord(rs, sql, fehler) Then
                Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Laden der Adressen aus Proffix")
                Return Nothing
            End If

            While Not rs.EOF
                Dim adresse As New pxKommunikation.pxAdressen
                adresse.AdressNr = CInt(rs.Fields("AdressNrADR").Value.ToString)
                adresse.Name = rs.Fields("Name").Value.ToString
                adresse.Vorname = rs.Fields("Vorname").Value.ToString
                adresse.Strasse = rs.Fields("Strasse").Value.ToString
                adresse.Plz = rs.Fields("PLZ").Value.ToString
                adresse.Ort = rs.Fields("Ort").Value.ToString
                adresse.ErstelltAm = rs.Fields("erstelltAm").Value.ToString
                adresse.GeaendertAm = rs.Fields("geaendertAm").Value.ToString
                adressList.Add(adresse)

                rs.MoveNext()
            End While

            Return adressList
        Catch ex As Exception
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod.Name)
            Return Nothing
        End Try
    End Function

    ' prüft, ob der Name in FLS oder Proffix mehr als 1x vorkommt
    Public Function checkIfIsSamePerson(
                                      ByVal nachname As String,
                                      ByVal vorname As String,
                                      ByVal strasseFLS As String,
                                      ByVal ortFLS As String,
                                      ByVal strassePX As String,
                                      ByVal ortPX As String,
                                      ByVal personResult As Threading.Tasks.Task(Of JArray),
                                      ByVal adressList As List(Of pxKommunikation.pxAdressen)) As Boolean



        If strasseFLS = strassePX And ortFLS = ortPX Then
            Return True
        End If

        Dim sql As String = String.Empty
        Dim rs As New ADODB.Recordset
        Dim fehler As String = String.Empty
        Dim vorhandenFLS As Integer = 0
        Dim vorhandenPX As Integer = 0
        Try
            '************************************************************************alle Adressen prüfen***********************************************************
            ' zählen, wie oft eine Person mit diesem Vor- und Nachnamen in FLS vorkommt
            For Each person As JObject In personResult.Result.Children()
                If nachname = person("Lastname").ToString And vorname = person("Firstname").ToString Then
                    vorhandenFLS += 1
                    If vorhandenFLS > 1 Then
                        Exit For
                    End If
                End If
            Next

            ' zählen, wie oft eine Person mit diesem Vor- und Nachnamen in Proffix vorkommt
            sql = "select count(*) as anzahl from adr_adressen where name = '" + nachname + "' and vorname = '" + vorname + "'"
            If Not myconn.getRecord(rs, sql, fehler) Then
                InvokeLog(vbTab + "Fehler beim Laden der Adressen für die Verlinkung")
                Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Laden der Adressen für die Verlinkung")
                Throw New Exception("Fehler in " + MethodBase.GetCurrentMethod().Name)
            End If

            vorhandenPX = CInt(rs.Fields("anzahl").Value)

            '*****************************************************************************Name nur 1x vorhanden*******************************************************
            ' kommt in FLS und px nur je 1x vor
            If vorhandenFLS = 1 And vorhandenPX = 1 Then
                Return True
            End If

            '****************************************************************************mehr als 1 mit gleichem Namen vorhanden************************************************
            ' wenn mehrmals vorhanden --> stimmt Strasse + Ort überein?
            If strasseFLS = strassePX And ortFLS = ortPX Then
                Return True
            End If

        Catch ex As Exception
            Throw New Exception("Fehler in " + MethodBase.GetCurrentMethod.Name + " " + ex.Message)
        End Try
    End Function


    ' AdressNr in FLS schreiben
    Private Function flsAktualisieren(ByVal person As JObject, ByVal address As pxBook.pxKommunikation.pxAdressen) As Boolean
        Dim response_FLS As String = String.Empty

        ' AdressNr in JSON schreiben
        person("ClubRelatedPersonDetails")("MemberNumber") = address.AdressNr.ToString
        person = CType(FlsHelper.removeMetadata(person), JObject)

        ' in FLS updaten
        response_FLS = _serviceClient.SubmitChanges(person("PersonId").ToString(), person, SyncerCommitCommand.Update)

        If response_FLS <> "OK" Then
            InvokeLog(vbTab + "Fehler: Die AdressNr(VereinsmitgliedNr)" & address.AdressNr & " konnte in FLS nicht aktualisiert werden." + response_FLS)
            Logger.GetInstance.Log(LogLevel.Critical, "Die AdressNr(VereinsmitgliedNr) konnte in FLS nicht aktualisiert werden. AdressNr:" + CStr(address.AdressNr) + " " + response_FLS)
            Return False
        End If
        Return True
    End Function


    ' PersonId in PX schreiben
    Private Function pxAktualisieren(ByVal person As JObject, ByVal address As pxBook.pxKommunikation.pxAdressen) As Boolean
        Dim fehler As String = String.Empty
        ' PersonId in pxAdresse schreiben
        address = ProffixHelper.SetZusatzFelder(address, "Z_FLSPersonId", "Z_FLSPersonId", "", "", person("PersonId").ToString)

        ' in Proffix updaten
        Proffix.GoBook.AddAdresse(address, fehler, False, ProffixHelper.CreateZusatzFelderSql(address))
        If Not String.IsNullOrEmpty(fehler) Then
            InvokeLog(vbTab + "Fehler: FLSPersonId konnte in Proffix nicht aktualisiert werden." + fehler)
            Logger.GetInstance.Log(LogLevel.Critical, "FLSPersonId konnte in Proffix nicht geupdatet werden. AdressNr: " + CStr(address.AdressNr) + " " + fehler)
            Return False
        End If
        Return True
    End Function

    '***********************************************************************************************Check Verlinkung*************************************************
    ' vergleichen ob in ZUS_FLSPersons (Daten direkt aus FLS) und ADR_Adressen die PersonIds und Adressnr/VereinsmitgliedNr gleich zueinander vernknüpft sind.
    Public Sub checkAddressLink()
        Dim sql_ADRAdressen As String = String.Empty
        Dim sql_FLSPersons As String = String.Empty
        Dim rs_ADRAdressen As New ADODB.Recordset
        Dim rs_FLSPersons As New ADODB.Recordset
        Dim fehler As String = String.Empty
        Dim falscheVerknuepfungVorhanden As Boolean = False
        Dim inBeidenVorhandenAnhandFLS As Boolean = False
        'Dim personResult As Threading.Tasks.Task(Of JArray)

        Try

            InvokeLog("Prüfung der Adressverknüpfung gestartet")
            Logger.GetInstance.Log(LogLevel.Info, "Prüfung der Adressverknüpfung gestartet")

            ' ZUS_FLSPersons neu füllen
            If Not _generalLoader.importPersons() Then
                InvokeLog("Fehler beim Importieren der Daten in ZUS_FLSPersons")
            End If

            ' Alle ungelöschten FLS Adressen holen
            'personResult = _serviceClient.CallAsyncAsJArray(My.Settings.ServiceAPIModifiedPersonFullDetailsMethod + DateTime.MinValue.ToString("yyyy-MM-dd"))
            'personResult.Wait()

            ' alle Adressen aus ADR_Adressen holen
            sql_ADRAdressen = "Select adressnradr, Z_FLSPersonId, name, vorname from ADR_Adressen where geloescht = 0"
            If Not myconn.getRecord(rs_ADRAdressen, sql_ADRAdressen, fehler) Then
                Throw New Exception("Fehler beim Holen der Adressen aus ADR_Adressen")
            End If

            ' alle Adressen aus FLSPersons holen
            sql_FLSPersons = "Select * from ZUS_FLSPersons"
            If Not myconn.getRecord(rs_FLSPersons, sql_FLSPersons, fehler) Then
                Throw New Exception("Fehler beim Holen der Personen aus ZUS_FLSPersons")
            End If

            ' miteinander vergleichen
            While Not rs_FLSPersons.EOF
                While Not rs_ADRAdressen.EOF

                    ' wemm PersonId identisch --> prüfen, ob AdressNr  identisch
                    If rs_FLSPersons.Fields("PersonId").Value.ToString = rs_ADRAdressen.Fields("Z_FLSPersonId").Value.ToString Then

                        ' Flag setzen
                        inBeidenVorhandenAnhandFLS = True
                        ' wenn die AdressNr nicht übereinstimmen --> Meldung
                        If Not rs_ADRAdressen.Fields("AdressNrADR").Value.ToString = rs_FLSPersons.Fields("VereinsmitgliedNrAdressNr").Value.ToString Then
                            InvokeLog(vbTab + "Die Adresse mit der PersonId " + rs_ADRAdressen.Fields("Z_FLSPersonId").Value.ToString + " hat in Proffix (" + rs_ADRAdressen.Fields("AdressNrADR").Value.ToString + ") und FLS (" + rs_FLSPersons.Fields("VereinsmitgliedNrAdressNr").Value.ToString + ") unterschiedliche AdressNr/VereinsmitgliedNr Nachname: " + rs_ADRAdressen.Fields("Name").Value.ToString + " Vorname: " + rs_ADRAdressen.Fields("Vorname").Value.ToString)
                            Logger.GetInstance.Log(LogLevel.Exception, "Die Adresse mit der PersonId " + rs_ADRAdressen.Fields("Z_FLSPersonId").Value.ToString + " hat in Proffix (" + rs_ADRAdressen.Fields("AdressNrADR").Value.ToString + ") und FLS (" + rs_FLSPersons.Fields("VereinsmitgliedNrAdressNr").Value.ToString + ") unterschiedliche AdressNr/VereinsmitgliedNr Nachname: " + rs_ADRAdressen.Fields("Name").Value.ToString + " Vorname: " + rs_ADRAdressen.Fields("Vorname").Value.ToString)
                            falscheVerknuepfungVorhanden = True
                        End If

                        ' entsprechende Adresse wurde gefunden (gleiche PersonId) --> weitere Adressen aus ADR_Adressen durchsuchen nicht mehr nötig
                        Exit While
                    End If

                    rs_ADRAdressen.MoveNext()
                End While

                rs_ADRAdressen.MoveFirst()
                rs_FLSPersons.MoveNext()

            End While

            ' Schlussmeldung
            If falscheVerknuepfungVorhanden Then
                InvokeLog("Prüfung der Adressverknüpfung beendet. Es wurde mindestens 1 Fehler entdeckt, der bereits aufgeführt wurde.")
                Logger.GetInstance.Log(LogLevel.Critical, "Prüfung der Adressverknüpfung beendet. Es wurde mindestens 1 Fehler entdeckt, der bereits aufgeführt wurde.")
            Else
                InvokeLog("Prüfung der Adressverknüpfung beendet. Alle Verknüpfungen sind korrekt.")
                Logger.GetInstance.Log(LogLevel.Info, "Prüfung der Adressverknüpfung beendet. Alle Verknüpfungen sind korrekt.")
            End If

        Catch ex As Exception
            InvokeLog(vbTab + "Fehler beim Überprüfen der Verknüpfungen der Adressen")
            Logger.GetInstance.Log(ex.Message)
        End Try

    End Sub


    ' !!!!!!!!!!!!!!!!!!¨Nur für Debuggen! gut überlegen ob wirklich ausführen!!
    Public Function verknuepfungenAufheben() As Boolean
        Dim sql As String = String.Empty
        Dim rs As New ADODB.Recordset
        Dim fehler As String = String.Empty
        Dim personResult As Threading.Tasks.Task(Of JArray)
        Try

            If LogAusfuehrlich Then
                Logger.GetInstance.Log(LogLevel.Info, "in allen PX-Adressen wird die FLS-PersonId gelöscht")
            End If
            ' Für alle PX Adressen die PersonId löschen
            sql = "update adr_adressen set geaendertAm = '" + Now().ToString(pxhelper.dateTimeFormat) + "', Z_FLSPersonId = null where Z_FLSPersonId is not null"
            If Not myconn.getRecord(rs, sql, fehler) Then
                InvokeLog("Fehler beim Löschen der FLSPersonId in Proffix")
                Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Löschen der FLSPersonId in Proffix")
                Return False
            Else
                InvokeLog("In Proffix wurden für alle Adressen die FLSPersonId erfolgreich gelöscht")
                Logger.GetInstance.Log(LogLevel.Info, "In Proffix wurden für alle Adressen die FLSPersonId erfolgreich gelöscht")

            End If

            ' alle Adressen aus FLS laden (egal ob IsActive = true oder nicht)
            personResult = _serviceClient.CallAsyncAsJArray(My.Settings.ServiceAPIModifiedPersonFullDetailsMethod + DateTime.MinValue.ToString("yyyy-MM-dd"))
            personResult.Wait()

            If LogAusfuehrlich Then
                Logger.GetInstance.Log(LogLevel.Info, "alle FLS Adressen geladen")
            End If

            ' für jede FLS Person
            For Each person As JObject In personResult.Result.Children

                ' MemberNumber löschen
                If GetValOrDef(person, "ClubRelatedPersonDetails.MemberNumber") <> "" Then
                    person("ClubRelatedPersonDetails")("MemberNumber") = Nothing
                    If Not _serviceClient.SubmitChanges(person("PersonId").ToString(), person, SyncerCommitCommand.Update) = "OK" Then
                        InvokeLog("Fehler beim Löschen der MemberNumber in FLS")
                        Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Löschen der MemberNumber in FLS" + person.ToString)
                        Return False
                    Else
                        Logger.GetInstance.Log(LogLevel.Exception, "MemberNumber in FLS gelöscht für " + GetValOrDef(person, "LastName") + " " + GetValOrDef(person, "Firstname"))
                    End If
                End If
            Next
            InvokeLog("In FLS wurden für alle Adressne die Vereinsmitglied-Nr. erfolgreich gelöscht")
            Return True

        Catch ex As Exception
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod.Name + " " + ex.Message)
            Return False
        End Try
    End Function


    ''' <summary>
    ''' Benutzerrückmeldung anzeigen
    ''' </summary>
    ''' <param name="message">Die Nachricht</param>
    Private Sub InvokeLog(ByVal message As String)
        If Log IsNot Nothing Then Log.Invoke(message)
    End Sub

    ''' <summary>
    ''' Synchronisationsfortschritt anzeigen
    ''' </summary>
    Private Sub InvokeDoProgress()
        If DoProgress IsNot Nothing Then DoProgress.Invoke()
    End Sub
End Class

