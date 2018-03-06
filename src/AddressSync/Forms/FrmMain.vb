Imports FlsGliderSync.My.Resources
Imports ADODB
Imports pxBook
Imports pxTools
Imports SMC
Imports SMC.Lib
Imports Newtonsoft.Json.Linq
Imports System.Net.Security
Imports System.Threading
Imports System.Runtime.Remoting
Imports System.Security.Cryptography.X509Certificates
Imports System.Threading.Tasks
Imports System.Drawing
Imports System.Reflection

' Tipps:
'- Fehlermeldungen/Warnungen wegbringen, indem "Release" anstatt "Debug"-Modus
' wenn im ini "Testumgebung=1" eingetragen ist, werden die URLs auf das Testsystem geleitet, sonst auf das scharfe FLS! (Kontrolle in Einstellungen möglich, welche URL gilt)
' --> für Installation beim Kunden muss "Testumgebung=1" aus dem ini entfernt werden
' --> bei Installation: URLs prüfen (werden in log angegeben, sofern in .ini logAusfuehrlich = 1)

' im Ini kann angegeben werden, falls alle Adressen vom einen zum anderen System geupdatet werden sollen (letzte Änderung wird dann ignoriert) Dies ist bei der 1. Synchronisation nötig, da dann nur FLS die Werte der Zusatzfelder kennt
' im ini muss immer master= stehen. Falls master=fls steht, werden alle Adressen von fls in PX geschrieben. Umgekehrt mit master=proffix
' Bei Installtion des Programms muss somit im ini master=fls gesetzt werden, und nach der 1. Synchronisation master= gesetzt werden

'- relevante Webseiten + Anmeldung: 
'   - https://test.glider-fls.ch bzw. https://fls.glider-fls.ch --> API --> welche Methoden könen aufgerufen werden
'   - https://test.glider-fls.ch/client --> Anmeldung Testumgebung (Userdaten: bei Patrick Schuler anfragen)
'   - https://fls.glider-fls.ch/client --> Anmeldung FLS (Userdaten: bei Patrick Schuler anfragen)


'!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! TODO !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
' 2. JSON wird mir neben Artikel Rabatt geben (Bsp. 50%, da er nur die Hälfte bezahlt) --> implementieren, dass dies berücksichtigt wird
' 1. Patrick Schuler wird noch implementieren, dass Fehlermeldungen von FLS mehr Infos enthalten --> dementsprechendes Fehlerhandling anpassen
' 2. AdressSynchronisation synchronisiert folgende Felder noch nicht: Midname, CompanyName (nicht in FLS: Tel direkt)


'!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!Anleitung, um Feld für Person hinzuzufügen!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
' - in Proffix entsprechendes Zusatzfeld erstellen
' - in Klasse PersonMapper bzw. ClubMapper (wenn in JSON unter clubrelated) entsprechende 3 Zeilen für dieses Feld mit den Namen aus JSON und Zusatzfeld hinzufügen
' - in Klasse ProffixHelper in Funktion GetZusatzFelderSql() entsprechende Zeile hinzufügen. (wenn Datum --> muss separat in datumsZusatzfelderInProffixEinfuegen() hinzugefügt werden)

' !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!Anleitung, um Zusatzfeld für Flug hinzuzufügen!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
' - in Proffix entsprechendes Zusatzfeld erstellen
' - in ProffixHelper in allen fill_ZUS (bzw. doInsertIntoTable) hinzufügen


'*********************************************************************************************************************************************************

'!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!ANPASSUNG TEST <-> SCHARFE VERSION während Implementation!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
' dazu fuerScharfeVersionAnpassen bzw. fuerTestVersionAnpassen in FrmMain.New auskommentieren
' die Funktion testFluegeFreigeben() (Aufruf in Importer) ist nur für den Testbetrieb gedacht


'!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! Kurzanleitung Installation Programm:
'- Zip-Ordner mit exe, ini, reg, ztxs..., zfx, dlls (SMC.lib.dll und SMC.Proffix.dll)
'- Zusatztabellen erstellen
'- alle Zusatzfelder in ADR_Adressen einfügen
'- Zusatzfeld ArticleId in LAG_Artikel
'- Dokumenttyp (LS) mit Kürzel FLS erstellen
'- In Proffix Bezeichnung1 = Artikelname, Bezeichnung2 = ARtikelinfo, Bezeichnung3 = Artikelbeschreibung, Bezeichnung5 = InvoiceLineText


''' <summary>
''' Das Hauptfenster
''' </summary>
Public Class FrmMain

    '***********************************************************Hilfsfunktionen während Implementation****************************************************************
    ' nur während Implementation!! Macht Anpassungen in Einstellungen, um das Programm auf der scharfen Version laufen zu lassen
    ' dazu in New() des FrmMains zum Ausführen nicht mehr auskommentieren
    Private Sub fuerScharfeVersionAnpassen()

        ' User + Passwort auf "" setzen, damit man nicht ein fremdes Login übernehmen kann
        My.Settings.Username = ""
        My.Settings.Password = ProffixCrypto.Encrypt("", My.Settings.Crypto)

        ' URLs ersetzen
        My.Settings.ServiceAPITokenMethod = My.Settings.ServiceAPITokenMethod.Replace("test.glider-fls.ch/", "fls.glider-fls.ch/")
        My.Settings.ServiceAPIPersonMethod = My.Settings.ServiceAPIPersonMethod.Replace("test.glider-fls.ch/", "fls.glider-fls.ch/")
        My.Settings.ServiceAPIDeliveriesNotProcessedMethod = My.Settings.ServiceAPIDeliveriesNotProcessedMethod.Replace("test.glider-fls.ch/", "fls.glider-fls.ch/")
        My.Settings.ServiceAPIDeletedPersonFulldetailsMethod = My.Settings.ServiceAPIDeletedPersonFulldetailsMethod.Replace("test.glider-fls.ch/", "fls.glider-fls.ch/")
        My.Settings.ServiceAPIModifiedPersonFullDetailsMethod = My.Settings.ServiceAPIModifiedPersonFullDetailsMethod.Replace("test.glider-fls.ch/", "fls.glider-fls.ch/")
        My.Settings.ServiceAPICountriesMethod = My.Settings.ServiceAPICountriesMethod.Replace("test.glider-fls.ch/", "fls.glider-fls.ch/")
        My.Settings.ServiceAPIArticlesMethod = My.Settings.ServiceAPIArticlesMethod.Replace("test.glider-fls.ch/", "fls.glider-fls.ch/")
        My.Settings.ServiceAPIPersonsMemberNrMethod = My.Settings.ServiceAPIPersonsMemberNrMethod.Replace("test.glider-fls.ch/", "fls.glider-fls.ch/")
        My.Settings.ServiceAPIMemberStates = My.Settings.ServiceAPIMemberStates.Replace("test.glider-fls.ch/", "fls.glider-fls.ch/")
        My.Settings.ServiceAPIDeliveredMethod = My.Settings.ServiceAPIDeliveredMethod.Replace("test.glider-fls.ch/", "fls.glider-fls.ch/")
        My.Settings.ServiceAPIModifiedFlightsMethod = My.Settings.ServiceAPIModifiedFlightsMethod.Replace("test.glider-fls.ch/", "fls.glider-fls.ch/")
        My.Settings.ServiceAPIAircraftsMethod = My.Settings.ServiceAPIAircraftsMethod.Replace("test.glider-fls.ch/", "fls.glider-fls.ch/")

        ' wenn weitere Methoden für den FLS Zugriff dazu kommen, müssen sie hier entsprechend ergänzt werden, damit bei Umstellung von Test auf scharf alle angepasst werden
        ' Idee als TODO: in For each durch alle My.Settings Eigenschaften gehen (funktioniert nicht)

        ' angepasste Einstellungen speichern
        My.Settings.Save()
    End Sub

    ' nur während Implementation!! Macht Anpassungen in Einstellungen, um das Programm auf den Testdaten laufen zu lassen
    ' dazu in New() des FrmMains zum Ausführen nicht mehr auskommentieren
    Private Sub fuerTestVersionAnpassen()
        ' die im Programm ursprünglich angegebenen Einstellungen wiederherstellen
        My.Settings.Reset()
    End Sub

    '*******************************************************************Klasse FrmMain***********************************************************************************
    ' Private GoBook As New pxBook.pxKommunikation
    Private Property frmSettings As FrmSettings
    Private Property frmLS As FrmLSManage

    ' Klassen für die Verbindung zum FLS bzw. zur Proffix-DB
    Private Property FlsConn As New FlsConnection()
    Friend Property MyConn As New ProffixConnection
    Private Property pxHelper As New ProffixHelper(MyConn)

    ' Die Klassen für die möglichen Prozesse
    Private generalLoader As GeneralDataLoader
    Private Property Linker As LinkAdressen
    Private Property Syncer As Syncer
    Private Property Exporter As Exporter
    Private Property Importer As Importer

    ' Threads
    Private Property LinkerThread As Thread
    Private Property SyncerThread As Thread
    Private Property ExporterThread As Thread
    Private Property DeliveryImporterThread As Thread
    Private Property FlightImporterThread As Thread
    Private Property LoadingImageThread As Thread   ' für Drehen des Lade-Bildes

    ' Exceptions
    Private Property LinkerException As Exception
    Private Property SyncerException As Exception
    Private Property ExporterException As Exception
    Private Property DeliveryImporterException As Exception
    Private Property FlightImporterException As Exception

    ' Hilfsvariablen
    ' Private Property IsAllInOne As Boolean = False        ' Flag, ob alle 3 Prozesse ausgeführt werden sollen (Import bei fehlender AdressNr ergibt Fehler --> zuerst Adresssynchronisation)
    Private Property RotateValue As Integer = 2     ' definiert, wie schnell das Lade-Bild rotiert
    Private Property linkersuccessful As Boolean = False
    Private Property syncsuccessful As Boolean = False    ' definiert, ob die Adresssynchronisation geklappt hat --> SaveLastDate
    Private Property exportsuccessful As Boolean = False  ' definiert, ob der Artikelexport geklappt hat --> SaveLastDate
    Private Property deliveryimportsuccessful As Boolean = False
    Private Property flightimportsuccessful As Boolean = False
    Private Property linkProcessFinished As Boolean = False
    Private Property syncProcessFinished As Boolean = False
    Private Property exportProcessFinished As Boolean = False
    Private Property deliveryImportProcessFinished As Boolean = False
    Private Property flightImportProcessFinished As Boolean = False
    Private Property flsLoginFinished As Boolean = False

    'Private waitHandlerSyncerThread As New System.Threading.AutoResetEvent(False)

    ''' <summary>
    ''' Initialisiert den Dialog
    ''' </summary>
    Public Sub New()

        ' wenn aus irgend einem Grund die Settings auf die Testumgebung umgestellt haben --> wieder auf scharfe Version umstellen (Anmeldedaten fallen aber raus)
        If My.Settings.ServiceAPITokenMethod.Contains("test.glider-fls.ch") Then
            fuerScharfeVersionAnpassen()
            If logAusfuehrlich Then
                Logger.GetInstance.Log(LogLevel.Info, "fuerScharfeVersionAnpassen() wurde ausgeführt")
            End If
        End If

        ' für debuggen auf TestURLs umstellen (wenn auf Testumgebung gestellt werden soll, muss in ini "Testumgebung=1" angegeben werden)
        ' wenn der key "Testumgebung" in ini fehlt, --> scharfe Version
        If readFromIni("Testumgebung", False) = "1" Then
            fuerTestVersionAnpassen()
            If logAusfuehrlich Then
                Logger.GetInstance.Log(LogLevel.Info, "fuerTestversionAnpassen() wurde ausgeführt")
            End If
        End If

        ' auslesen, ob ein Master definiert ist
        ' wenn master=fls --> Beim Sync wird im Fall eines Updates immer PX mit FLS überschrieben (benötigt bei 1. Synchronisation)
        ' wenn master=proffix --> Beim sync wird im Fall eines Updates immer FLS mit PX überschrieben
        ' in allen anderen Fällen "master= ,master=irgendwasanderes --> Beim Sync wird im Fall eiens updates die zuletzt veränderte Adresse verwendet
        Dim masterdb As String = readFromIni("master").ToLower
        If masterdb = "fls" Then
            Master = UseAsMaster.fls
        ElseIf masterdb = "proffix" Then
            Master = UseAsMaster.proffix
        Else
            Master = UseAsMaster.undefined
        End If

        If logAusfuehrlich Then
            Logger.GetInstance.Log(LogLevel.Info, My.Settings.ServiceAPITokenMethod)
            Logger.GetInstance.Log(LogLevel.Info, My.Settings.ServiceAPIPersonMethod)
            Logger.GetInstance.Log(LogLevel.Info, My.Settings.ServiceAPIDeliveriesNotProcessedMethod)
            Logger.GetInstance.Log(LogLevel.Info, My.Settings.ServiceAPIDeletedPersonFulldetailsMethod)
            Logger.GetInstance.Log(LogLevel.Info, My.Settings.ServiceAPIModifiedPersonFullDetailsMethod)
            Logger.GetInstance.Log(LogLevel.Info, My.Settings.ServiceAPICountriesMethod)
            Logger.GetInstance.Log(LogLevel.Info, My.Settings.ServiceAPIArticlesMethod)
            Logger.GetInstance.Log(LogLevel.Info, My.Settings.ServiceAPIPersonsMemberNrMethod)
            Logger.GetInstance.Log(LogLevel.Info, My.Settings.ServiceAPIMemberStates)
            Logger.GetInstance.Log(LogLevel.Info, My.Settings.ServiceAPIDeliveredMethod)
            Logger.GetInstance.Log(LogLevel.Info, My.Settings.ServiceAPIModifiedFlightsMethod)
            Logger.GetInstance.Log(LogLevel.Info, My.Settings.ServiceAPIAircraftsMethod)
        End If

        'Controls Initialisieren
        InitializeComponent()

        Logger.GetInstance.Log(LogLevel.Info, "Starting main form...")
        CreateToolTips()

        ' anklickbare Elemente auf enabled = false setzen, bis Anmeldungsversuch abgeschlossen
        enableFormElements(False)

        '*********************************************************************bei FLS anmelden******************************************************************
        ' bei FLS anmelden und anzeigen, ob erfolgreich

        ' bei FLS anmelden
        Dim openConnectionThread As New Thread(
            New ThreadStart(
                Sub()
                    Try
                        OpenFLSConnection()
                    Catch exce As Exception
                        'Die Verbindung konnte nicht aufgebaut werden
                        Logger.GetInstance.Log(LogLevel.Exception, New Exception("Verbindung zu FLS mit den Standardwerten fehlgeschlagen.", exce))
                    End Try
                End Sub))
        openConnectionThread.Start()

        'verwendeter Account anzeigen
        lblAccount.Text = My.Settings.Username
        '*****************************************************************bei Proffix anmelden*****************************************************************
        'Dim loginUser As String
        'loginUser = readFromIni()

        Proffix.GoBook.LoginUser = "FlsGliderSync"
        ' mit Proffix verbinden und anzeigen, ob erfolgreich
        If Not Proffix.Open() Then
            Log("Proffix-Anmeldung fehlgeschlagen.")
            If DialogResult.OK = MessageBox.Show("Das Programm konnte sich nicht korrekt in Proffix anmelden. Kontrollieren Sie die Konfigurationsdateien oder kontaktieren Sie den Support. Das Programm muss neu gestartet werden", "Proffix-Anmeldung", MessageBoxButtons.OK, MessageBoxIcon.Error) Then
                ' wenn Proffixanmeldung fehlgeschlagen funktioniert Programm nicht --> Programm beenden
                End
            End If
        End If

        ' DEBUG
        Dim fehler As String
        Dim adressen() As pxKommunikation.pxAdressen = {}
        Proffix.GoBook.GetAdresse(pxKommunikation.pxAdressSuchTyp.AdressNr, "3", adressen, fehler)
        adressen(1).Strasse = "Teeststrasse"
        Proffix.GoBook.AddAdresse(adressen(1), fehler)



        ' Proffix-Anemdlung erfolgreich
        Log("Proffix-Anmeldung erfolgreich")
        ' verwendeter Mandant anzeigen
        lblMandant.Text = Proffix.GoBook.Mandant


        '***********************************************************Klassen initialisieren******************************************************************
        initializeProcessClasses()

        ' FrmSettings initialisieren, um start/enddate für Flugdatenimport richtig zu handeln, falls bei FrmSettings Cancel geklickt wird
        frmSettings = New FrmSettings
        frmLS = New FrmLSManage(FlsConn)

        'Die letzte Synchonisation anzeigen
        Dim action As New Action(
            Sub()
                If ((Syncer.LastSync = Nothing) = False) Then
                    lblLastSyncDate.Text = Syncer.LastSync.ToShortDateString() + " " +
                        Syncer.LastSync.ToShortTimeString()
                End If

                If ((Exporter.LastExport = Nothing) = False) Then
                    lblLastExportDate.Text = Exporter.LastExport.ToShortDateString() + " " +
                        Exporter.LastExport.ToShortTimeString()
                End If

                If ((Importer.lastDeliveryImport = Nothing) = False) Then
                    lblLastDeliveryImportDate.Text = Importer.lastDeliveryImport.ToShortDateString() + " " +
                        Importer.lastDeliveryImport.ToShortTimeString()
                End If
                If ((Importer.lastFlightImport = Nothing) = False) Then
                    lblLastFlightImportDate.Text = Importer.lastFlightImport.ToShortDateString() + " " +
                        Importer.lastFlightImport.ToShortTimeString()
                End If
            End Sub)

        If (lblLastSyncDate.InvokeRequired) Then
            Invoke(action)
        Else
            action.Invoke()
        End If

        ' warten bis FLS angemeldet ist, bevor FrmMain anklickbar werden soll
        While Not flsLoginFinished

        End While
        enableFormElements(True)

    End Sub

    '***********************************************************************Connection*************************************************************************************
    ''' <summary>
    ''' Öffnen der Verbindung  und Initialisieren der Manager Klassen
    ''' </summary>
    Private Async Sub OpenFLSConnection()
        flsLoginFinished = False
        ' Login in FLS
        If Await FlsConn.Login(My.Settings.Username, ProffixCrypto.Decrypt(My.Settings.Password, My.Settings.Crypto), My.Settings.ServiceAPITokenMethod) Then
            'lblFLSVerbunden.Text = "erfolgreich"
            Log("FLS-Anmeldung erfolgreich")
        Else
            'lblFLSVerbunden.Text = "fehlgeschlagen"
            Log("FLS-Anmeldung fehlgeschlagen. Überprüfen Sie unter Menu --> Einstellungen den Usernamen und das Passwort")
        End If
        flsLoginFinished = True

    End Sub

    ' initialisiert die Klassen für die verschiedenen Prozesse, welche das Programm ausführen kann
    Private Sub initializeProcessClasses()

        ' Importerklasse initialisieren
        Importer = New Importer(LoadLastDate("DeliveryImport"), LoadLastDate("FlightImport"), FlsConn, pxHelper, MyConn) With {
                                                .Log = New Action(Of String)(Sub(message)
                                                                                 Invoke(New Action(Sub()
                                                                                                       Log(message)
                                                                                                   End Sub))
                                                                             End Sub),
                                                .DoProgressDelivery = New Action(Sub()
                                                                                     Invoke(New Action(AddressOf DoImporterProgressDelivery))
                                                                                 End Sub),
                                                .DoProgressFlight = New Action(Sub()
                                                                                   Invoke(New Action(AddressOf DoImporterProgressFlight))
                                                                               End Sub)
                                            }

        ' GeneralLoader initialisieren
        generalLoader = New GeneralDataLoader(pxHelper, FlsConn, MyConn, Importer)

        'Linker Klasse Initialisieren und aktionen verknüpfen
        Linker = New LinkAdressen(LoadLastDate("AddressSync"), FlsConn, pxHelper, MyConn, generalLoader) With {
                                                        .Log = New Action(Of String)(Sub(message)
                                                                                         Invoke(New Action(Sub()
                                                                                                               Log(message)
                                                                                                           End Sub))
                                                                                     End Sub),
                                                        .DoProgress = New Action(Sub()
                                                                                     Invoke(New Action(AddressOf DoLinkerProgress))
                                                                                 End Sub)
                                                    }

        'Syncer Klasse Initialisieren und aktionen verknüpfen
        Syncer = New Syncer(LoadLastDate("AddressSync"), FlsConn, pxHelper, MyConn, generalLoader) With {
                                                        .Log = New Action(Of String)(Sub(message)
                                                                                         Invoke(New Action(Sub()
                                                                                                               Log(message)
                                                                                                           End Sub))
                                                                                     End Sub),
                                                        .DoProgress = New Action(Sub()
                                                                                     Invoke(New Action(AddressOf DoSyncerProgress))
                                                                                 End Sub)
                                                    }

        ' Exporterklasse initialisieren
        Exporter = New Exporter(LoadLastDate("ArticleExport"), FlsConn, pxHelper, MyConn) With {
                                                        .Log = New Action(Of String)(Sub(message)
                                                                                         Invoke(New Action(Sub()
                                                                                                               Log(message)
                                                                                                           End Sub))
                                                                                     End Sub),
                                                        .DoProgress = New Action(Sub()
                                                                                     Invoke(New Action(AddressOf DoExporterProgress))
                                                                                 End Sub)
                                                    }
    End Sub











    '*********************************************************************************Prozesse**********************************************************************************
    Private Sub DoLoadGeneralData()
       
        Log("Laden allgemeiner Daten gestartet")
        Logger.GetInstance.Log(LogLevel.Info, "Allgemeine Daten werden geladen")
        If Not generalLoader.loadGeneralData() Then
            Log(vbTab + "Fehler beim Laden der allgemeinen Daten")
            Logger.GetInstance.Log(LogLevel.Info, "Fehler beim Allgemeine Daten werden geladen")
        Else
            Log("Laden allgemeiner Daten erfolgreich abgeschlossen")
        End If

        Log("")
    End Sub
    '************************************************************************************Adressen verknüpfen**********************************************************************
    Private Sub DoLinkAdresses()

        If cbAdressen.Checked Then
            'Den Synchronisationsthread starten
            LinkerThread = New Thread(New ThreadStart(AddressOf LinkerWork))
            LinkerThread.Start()
        Else
            ' Sync wird zwar nicht ausgeführt, aber die nächsten Prozesse sollen gestartet werden können
            LinkerWorkEnd()
        End If
    End Sub

    Private Sub LinkerWork()
        Try
            'MessageBox.Show("Debug: Linker wird übersprungen. Ist in LinkerWork() auskommentiert", "")
            linkersuccessful = Linker.Link()
        Catch exce As Exception
            'Den Fehler ausgeben und zurücksetzen
            Logger.GetInstance().Log(LogLevel.Critical, exce)
            Invoke(New Action(Sub()
                                  Log("Adressen verknüpfen fehlgeschlagen")
                              End Sub))
            LinkerException = exce
        Finally
            Invoke(New Action(AddressOf LinkerWorkEnd))
        End Try
    End Sub

    Private Sub LinkerWorkEnd()
        ' gab es bei der Adress-Synchronisation einen Fehler?
        If LinkerException IsNot Nothing Then
            logException(SyncerException)
        End If

        ' wenn nur die Verknüpfung (über Menü) ausgeführt werden soll  --> nur noch beenden
        linkProcessFinished = True

        startNextProcess(If(LinkerException Is Nothing And linkersuccessful, False, True))

    End Sub

    ''' <summary>
    ''' Anzeigen des Synchronisationsfortschritt
    ''' </summary>
    Private Sub DoLinkerProgress()
        'ProgressBar aktualisieren
        pbMain.Maximum = Linker.Count
        pbMain.Value = Linker.Progress
    End Sub

    '***********************************************************************************AdressSync*********************************************************************************
    ''' <summary>
    ''' Alle Adressen Synchronisieren
    ''' </summary>
    Public Sub DoSyncAdresses()
        If cbAdressen.Checked Then
            'Den Synchronisationsthread starten
            SyncerThread = New Thread(New ThreadStart(AddressOf SyncerWork))
            SyncerThread.Start()
        Else
            ' Sync wird zwar nicht ausgeführt, aber die nächsten Prozesse sollen gestartet werden können
            SyncerWorkEnd()
        End If
    End Sub

    ''' <summary>
    ''' Die Synchronisation durchführen
    ''' </summary>
    Private Sub SyncerWork()
        Try
            ' MsgBox("Debug: Adresssync wird übersprungen. Syncer.Sync in FrmMain auskommentiert.")
            syncsuccessful = Syncer.Sync()
        Catch exce As Exception
            'Den Fehler ausgeben und zurücksetzen
            Logger.GetInstance().Log(LogLevel.Critical, exce)
            Invoke(New Action(Sub()
                                  Log("Adresssynchronisation fehlgeschlagen...")
                              End Sub))
            SyncerException = exce
        Finally
            Invoke(New Action(AddressOf SyncerWorkEnd))
        End Try
    End Sub

    ''' <summary>
    ''' Gibt Fehlermeldungen aus + ruft Beendigungsfunktion oder Artikelexport auf
    ''' </summary>
    Private Sub SyncerWorkEnd()
        ' gab es bei der Adress-Synchronisation einen Fehler?
        If SyncerException Is Nothing Then
            'Letzte Synchronisation setzen
            If Not Syncer.LastSync = Nothing Then
                ' wenn Synchronisation der Adressen geklappt hat
                If syncsuccessful Then
                    ' ... und LastSync abgespeichert werden konnte
                    If SaveLastDate(Syncer.LastSync, "AddressSync") Then
                        ' ... Datum Now anzeigen
                        lblLastSyncDate.Text = Syncer.LastSync.ToShortDateString() + " " + Syncer.LastSync.ToShortTimeString()
                    End If
                End If
            End If
        Else
            logException(SyncerException)
            'EndWork()
        End If
       
        syncProcessFinished = True
        startNextProcess(If(SyncerException Is Nothing, False, True))
    End Sub

    ''' <summary>
    ''' Anzeigen des Synchronisationsfortschritt
    ''' </summary>
    Private Sub DoSyncerProgress()
        'ProgressBar aktualisieren
        pbMain.Maximum = Syncer.Count
        pbMain.Value = Syncer.Progress
    End Sub


    '**********************************************************************************ArticleExport******************************************************************************
    ''' <summary>
    ''' Alle Adressen Exporthronisieren
    ''' </summary>
    Public Sub DoExportArticles()
        If cbArtikel.Checked Then
            'Den Exporthronisationsthread starten
            ExporterThread = New Thread(New ThreadStart(AddressOf ExporterWork))
            ExporterThread.Start()
        Else
            ExporterWorkEnd()
        End If
    End Sub

    ''' <summary>
    ''' Die Exporthronisation durchführen
    ''' </summary>
    Private Sub ExporterWork()
        ' waitHandlerSyncerThread.WaitOne()
        Try
            'If syncsuccessful Then
            exportsuccessful = Exporter.Export()
            'End If
        Catch exce As Exception
            'Den Fehler ausgeben und zurücksetzen
            Logger.GetInstance().Log(LogLevel.Critical, exce)
            Invoke(New Action(Sub()
                                  Log("Artikelexport fehlgeschlagen ...")
                              End Sub))
            ExporterException = exce
        Finally
            Invoke(New Action(AddressOf ExporterWorkEnd))
        End Try
    End Sub

    ''' <summary>
    ''' Gibt Fehlermeldungen aus + ruft Beendigungsfunktion oder FlightImport auf
    ''' </summary>
    Private Sub ExporterWorkEnd()
        ' gab es beim Artikel-Export einen Fehler?
        If ExporterException Is Nothing Then
            'Letzte Exporthronisation setzen
            If Not Exporter.LastExport = Nothing Then
                If exportsuccessful Then
                    If SaveLastDate(Exporter.LastExport, "ArticleExport") Then
                        lblLastExportDate.Text = Exporter.LastExport.ToShortDateString() + " " + Exporter.LastExport.ToShortTimeString()
                    End If
                End If
            End If
        Else
            logException(ExporterException)
        End If
        exportProcessFinished = True
        startNextProcess(If(ExporterException Is Nothing, False, True))
    End Sub

    ''' <summary>
    ''' Anzeigen des Synchronisationsfortschritt
    ''' </summary>
    Private Sub DoExporterProgress()
        'ProgressBar aktualisieren
        pbMain.Maximum = Exporter.Count
        pbMain.Value = Exporter.Progress
    End Sub


    '************************************************************************************Import************************************************************************************
    ''' <summary>
    ''' Den Import der Lieferscheine starten
    ''' </summary>
    Public Sub DoDeliveryImport()

        If cbLieferscheine.Checked Then
            'Den Importthread starten
            DeliveryImporterThread = New Thread(New ThreadStart(AddressOf DeliveryImporterWork))
            DeliveryImporterThread.Start()
        Else
            DeliveryImporterWorkEnd()
        End If
    End Sub

    ''' <summary>
    ''' Import der Flugdaten durchführen
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub DeliveryImporterWork()
        'waitHandlerExporterThread.WaitOne()
        Try
            'If exportsuccessful Then
            If Not generalLoader.deleteIncompleteData() Then
                Log("Fehler beim Löschen von Daten")
            Else
                deliveryimportsuccessful = Importer.DeliveryImport()
            End If
        Catch ex As Exception
            'Wenn ein Fehler auftaucht wird dieser ausgegeben
            Logger.GetInstance().Log(LogLevel.Critical, ex)
            Invoke(New Action(Sub()
                                  Log("Lieferscheinimport fehlgeschlagen")
                              End Sub))
            DeliveryImporterException = ex
        Finally
            ' unkomplette Daten für DeliveryIds + FlightIds löschen
            generalLoader.deleteIncompleteData()
            Invoke(New Action(AddressOf DeliveryImporterWorkEnd))
        End Try
    End Sub

    ''' <summary>
    ''' Gibt Fehlermeldungen aus + ruft Beendigungsfunktion auf
    ''' </summary>
    Private Sub DeliveryImporterWorkEnd()

        ' gab es beim FlightImport einen Fehler?
        If DeliveryImporterException Is Nothing Then
            If Not Importer.lastDeliveryImport = Nothing Then
                If deliveryimportsuccessful Then
                    If SaveLastDate(Importer.lastDeliveryImport, "DeliveryImport") Then
                        lblLastDeliveryImportDate.Text = Importer.lastDeliveryImport.ToShortDateString() + " " + Importer.lastDeliveryImport.ToShortTimeString()
                    End If
                End If
            End If
        Else
            logException(DeliveryImporterException)
        End If
        'waitHandlerDeliveryImporterThread.Set()
        deliveryImportProcessFinished = True
        startNextProcess(If(DeliveryImporterException Is Nothing, False, True))
    End Sub


    ' ''' <summary>
    ' ''' Anzeigen des Synchronisationsfortschritt
    ' ''' </summary>
    'Private Sub DoImporterProgress()
    '    'ProgressBar aktualisieren
    '    pbMain.Maximum = Importer.DeliveryCount
    '    pbMain.Value = Importer.Progress
    'End Sub

    ''' <summary>
    ''' Anzeigen des Synchronisationsfortschritt
    ''' </summary>
    Private Sub DoImporterProgressDelivery()
        'ProgressBar aktualisieren
        pbMain.Maximum = Importer.DeliveryCount
        pbMain.Value = Importer.ProgressDelivery
    End Sub

    ''' <summary>
    ''' Anzeigen des Synchronisationsfortschritt
    ''' </summary>
    Private Sub DoImporterProgressFlight()
        'ProgressBar aktualisieren
        pbMain.Maximum = Importer.FlightCount
        pbMain.Value = Importer.ProgressFlight
    End Sub



    ''' <summary>
    ''' Den Import der Flugdaten starten
    ''' </summary>
    Public Sub DoFlightImport()
        If cbFluege.Checked Then
            'Den Importthread starten
            FlightImporterThread = New Thread(New ThreadStart(AddressOf FlightImporterWork))
            FlightImporterThread.Start()

        Else
            FlightImporterWorkEnd()
        End If
    End Sub

    ''' <summary>
    ''' Import der Flugdaten durchführen
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub FlightImporterWork()
        'waitHandlerDeliveryImporterThread.WaitOne()
        Try
            'If deliveryimportsuccessful Then
            If Not generalLoader.deleteIncompleteData() Then
                Log("Fehler beim Löschen von Daten")
            Else
                flightimportsuccessful = Importer.FlightImport()
            End If
        Catch ex As Exception
            'Wenn ein Fehler auftaucht wird dieser ausgegeben
            Logger.GetInstance().Log(LogLevel.Critical, ex)
            Invoke(New Action(Sub()
                                  Log("Flugdatenimport fehlgeschlagen")
                              End Sub))
            FlightImporterException = ex
        Finally
            ' unkomplette Daten für DeliveryIds + FlightIds löschen
            generalLoader.deleteIncompleteData()
            Invoke(New Action(AddressOf FlightImporterWorkEnd))
        End Try
    End Sub

    ''' <summary>
    ''' Gibt Fehlermeldungen aus + ruft Beendigungsfunktion auf
    ''' </summary>
    Private Sub FlightImporterWorkEnd()

        ' gab es beim FlightImport einen Fehler?
        If FlightImporterException Is Nothing Then
            If Not Importer.lastFlightImport = Nothing Then
                If flightimportsuccessful Then
                    If SaveLastDate(Importer.lastFlightImport, "FlightImport") Then
                        lblLastFlightImportDate.Text = Importer.lastFlightImport.ToShortDateString() + " " + Importer.lastFlightImport.ToShortTimeString()
                    End If
                End If
            End If
        Else
            logException(FlightImporterException)
        End If

        'waitHandlerFlightImporterThread.Set()
        flightImportProcessFinished = True
        startNextProcess(If(FlightImporterException Is Nothing, False, True))
    End Sub

    ' ''' <summary>
    ' ''' Anzeigen des Synchronisationsfortschritt
    ' ''' </summary>
    'Private Sub DoFlightImporterProgress()
    '    'ProgressBar aktualisieren
    '    pbMain.Maximum = Importer.FlightCount
    '    pbMain.Value = Importer.Progress
    'End Sub

    ' ''' <summary>
    ' ''' Anzeigen des Synchronisationsfortschritt
    ' ''' </summary>
    'Private Sub DoDeliveryImporterProgress()
    '    'ProgressBar aktualisieren
    '    pbMain.Maximum = Importer.DeliveryCount
    '    pbMain.Value = Importer.Progress
    'End Sub


    '************************************************************************Button Clicks***********************************************************************************

    ' wenn Lieferscheine und Flüge synchronisert werden sollen, müssen die Adressen aktuell sein --> müssen ebenfalls synchronisiert werden
     Private Sub cbLieferscheine_CheckedChanged(sender As Object, e As EventArgs) Handles cbLieferscheine.CheckedChanged
        If cbLieferscheine.Checked Then
            cbAdressen.Checked = True
        End If
    End Sub

    Private Sub cbFluege_CheckedChanged(sender As Object, e As EventArgs) Handles cbFluege.CheckedChanged
        If cbFluege.Checked Then
            cbAdressen.Checked = True
          End If
    End Sub

    ' Adressen müssen auch synchronisiert werden, wenn Lieferscheine erstellt bzw Flugdaten importiert werden sollen, da sonst Verknüpfung möglicherweise fehlt
    Private Sub cbAdressen_CheckedChanged(sender As Object, e As EventArgs) Handles cbAdressen.CheckedChanged
        If Not cbAdressen.Checked Then
            cbLieferscheine.Checked = False
            cbFluege.Checked = False
        End If
    End Sub

    Public Sub FrmMain_KeyDown(sender As Object, e As KeyPressEventArgs) Handles lbLog.KeyPress
        If e.KeyChar = "" And ModifierKeys.HasFlag(Keys.Control) And lbLog.SelectedItem IsNot Nothing Then
            Clipboard.SetText(lbLog.SelectedItem.ToString)
        End If
    End Sub

    '******************************************************************************Menu Klicks*************************************************************************************

    'Private Sub tsmiLSManage_Click(sender As Object, e As EventArgs) Handles tsmiLSManage.Click
    '    MsgBox("Diese Funktion steht momentan noch nicht zur Verfügung")



    '    ' enableFormElements(False)

    '    'frmLS.ShowDialog()

    'End Sub


        ''' <summary>
        ''' Es wurde auf "Einstellungen" geklickt
        ''' </summary>
        ''' <param name="sender">Der Button</param>
        ''' <param name="e">Informationen zum Event</param>
    Private Async Sub tsmiSettings_Click(ByVal sender As Object, ByVal e As EventArgs) Handles tsmiSettings.Click
        ' Form auf nicht anklickbar stellen
        enableFormElements(False)

        ' Speichern der alten Werte, um sie wieder hervorzuholen, falls Cancel geklickt wird
        'Dim old_startdate As DateTime = _startDate
        'Dim old_enddate As DateTime = _endDate

        ' Werte für Username + Passwort neu aus Settings laden
        frmSettings.tbUser.Text = My.Settings.Username
        frmSettings.tbPassword.Text = ProffixCrypto.Decrypt(My.Settings.Password, My.Settings.Crypto)

        Dim dialogres As DialogResult = frmSettings.ShowDialog()

        ' AWenn auf FormSettings Ok geklicht wurde
        If dialogres = DialogResult.OK Then

            ' Zeitspanne speichern, in der die Flugdaten importiert werden sollen
            '_startDate = frmSettings.dtpFrom.Value
            '_endDate = frmSettings.dtpTo.Value

            lbLog.Items.Clear()
            rotateLoadingImage(True)


            ' wenn Username oder Passwort geändert wurde --> neu anmelden
            If Not frmSettings.tbUser.Text = My.Settings.Username Or Not frmSettings.tbPassword.Text = My.Settings.Password Then

                ' neue Angaben in Settings speichern
                My.Settings.Password = ProffixCrypto.Encrypt(frmSettings.tbPassword.Text, My.Settings.Crypto)
                My.Settings.Username = frmSettings.tbUser.Text
                My.Settings.Save()

                If Not Await FlsConn.Login(My.Settings.Username, ProffixCrypto.Decrypt(My.Settings.Password, My.Settings.Crypto), My.Settings.ServiceAPITokenMethod) Then
                    ' lblFLSVerbunden.Text = "fehlgeschlagen"
                    Log("FLS-Anmeldung mit neuen Daten fehlgeschlagen, überprüfen Sie den Usernamen und das Passwort")
                Else
                    Log("FLS-Anmeldung mit neuen Daten erfolgreich")
                    ' lblFLSVerbunden.Text = "erfolgreich"
                End If

                ' neu eingegebenen FLS-Account auf FrmMain anzeigen
                lblAccount.Text = My.Settings.Username

            End If

            rotateLoadingImage(False)
        ElseIf dialogres = Windows.Forms.DialogResult.Cancel Then
            '_startDate = old_startdate
            '_endDate = old_enddate
            'frmSettings.dtpFrom.Value = old_startdate
            'frmSettings.dtpTo.Value = old_enddate
            My.Settings.Save()
        End If

        ' FrmMain wieder auf anklickbar stellen
        enableFormElements(True)
    End Sub

    ' wenn Flüge ab früherem Datum geladen werden sollen
    'Private Sub tsmiFormerFlights_Click(sender As Object, e As EventArgs)

    'MsgBox("Wird momentan nicht ausgeführt")

    'Dim frmFlights As New FrmFlightImport

    'Dim dialogres As DialogResult = frmFlights.ShowDialog()
    '' wenn Ok geklickt wurde
    'If dialogres = DialogResult.OK Then

    '    ' Datum, ab wann importiert werden soll, setzen.
    '    Importer.lastFlightImport = frmFlights.dtpFlightVon.Value
    '    '  Importer.BisFlightImport = frmFlights.dtpFlightBis.Value

    '    ' Form vorbereiten
    '    PrepareWork()

    '    ' damit nur FlightImport ausgeführt wird
    '    linkProcessFinished = True
    '    syncProcessFinished = True
    '    exportProcessFinished = True
    '    deliveryImportProcessFinished = True
    '    cbFluege.Checked = True

    '    startNextProcess(False)

    'End If
    'End Sub


    ' prüft, ob Verknüpfung der Adressen stimmt
    Private Sub tsmiCheckAdressLink_Click(sender As Object, e As EventArgs) Handles tsmiCheckAdressLink.Click
        PrepareWork()
        Linker.checkAddressLink()
        EndWork()
    End Sub

    ''' <summary>
    ''' leert Konsole
    ''' </summary>
    ''' <param name="sender">Der Button</param>
    ''' <param name="e">Informationen zum Event</param>
    Private Sub ClearLogToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tsmiClearLogView.Click
        If Not GeneralHelper.logArchivieren() Then
            MessageBox.Show("Archivieren der Log-Datei fehlgeschlagen", "Archivieren fehlgeschlagen", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        End If
        lbLog.Items.Clear()
    End Sub

    ' Programm schliessen
    Private Sub tsmiClose_Click(sender As Object, e As EventArgs) Handles tsmiClose.Click
        Me.Close()
        Proffix.Close()
        End
    End Sub
    '****************************************************************************Hilfe****************************************************************************************
    Private Sub lblHelp_Click(sender As Object, e As EventArgs) Handles lblHelp.Click
        Try
            Process.Start("FLSHelp.pdf")
        Catch
            MessageBox.Show("Fehler beim Öffnen der Datei FLSHelp.pdf. Kontrollieren Sie, ob die Datei im Prog-Verzeichnis vorhanden ist und ob ein Programm zum Öffnen von PDFs installiert ist.", "Fehler beim Öffnen der Datei", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        End Try
    End Sub

    '*************************************************************************Abbruch***************************************************************************
    ' es wurde Abbrechen geklickt
    Private Sub BtnCancel_Click(sender As Object, e As EventArgs) Handles BtnCancel.Click
        '    Log("Es wurde Abbrechen geklickt")
        '    Logger.GetInstance.Log("Es wurde Abbrechen geklickt. Prozess wird abgebrochen")
        Me.Close()
    End Sub

    ' FrmMain wird geschlossen (Kreuz oben rechts geklickt, aus Abbrechen durch Me.Close() aufgerufen, oder andere Gründe)
    Private Sub FrmMain_FormClosing(ByVal sender As Object, ByVal e As FormClosingEventArgs) Handles MyBase.FormClosing
        enableFormElements(False)
        Me.Close()
        Log("Programm wird beendet")
        Logger.GetInstance.Log(LogLevel.Info, "FrmMain wird geschlossen.")
        generalLoader.deleteIncompleteData()
        Proffix.Close()
        End
    End Sub


    '***************************************************************************GUI Hilfsfunktionen**************************************************************************

    ''' <summary>
    ''' Vorbereitung auf die Prozessausführung
    ''' </summary>
    Private Sub PrepareWork()

        ' nicht anklickbar + Ladenbild
        enableFormElements(False)
        rotateLoadingImage(True)

        ' Exception auf Nothing setzen
        SyncerException = Nothing
        ExporterException = Nothing
        DeliveryImporterException = Nothing
        FlightImporterException = Nothing

        ' Progressbar
        pbMain.Value = 0
        pbLoading.Visible = True

        lbLog.Items.Clear()
        Log("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++")
    End Sub

    ''' <summary>
    ''' Gibt aufgetretene Fehler aus
    ''' </summary>
    ''' <param name="exce"></param>
    Private Sub logException(ByVal exce As Exception)
        If logAusfuehrlich Then

            Log("---------------------------------------------------------------------------------------------------")
            Log("Folgender Fehler ist aufgetreten")

            'Den Fehler detailiert anzeigen
            While (exce IsNot Nothing)
                Log(exce.Message)
                Log(String.Empty)
                If (exce.Source IsNot Nothing) Then
                    Log(exce.Source)
                End If
                If (exce.TargetSite IsNot Nothing) Then
                    Log(exce.TargetSite.GetType().FullName + " - " + exce.TargetSite.Name)
                End If
                If (exce.StackTrace IsNot Nothing) Then
                    For Each val As String In _
                        exce.StackTrace.Split(New String() {Environment.NewLine}, StringSplitOptions.None)
                        Log(val)
                    Next
                End If
                If (exce.Data IsNot Nothing) Then
                    For Each keyVal As KeyValuePair(Of Object, Object) In exce.Data
                        Log(keyVal.Key.ToString() + " : " + keyVal.Value.ToString())
                    Next
                End If
                Log("---------------------------------------------------------------------------------------------------")
                exce = exce.InnerException
            End While
        End If
    End Sub

    ''' <summary>
    ''' Macht Form für nächsten Prozess bereit
    ''' </summary>
    Private Sub EndWork()

        Log("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++")

        enableFormElements(True)
        rotateLoadingImage(False)
        pbLoading.Visible = False
    End Sub

    ' Buttons + Menu anklickbar, bzw. nicht anklickbar stellen
    Private Sub enableFormElements(ByVal bool As Boolean)
        msMain.Enabled = bool
        cbAdressen.Enabled = bool
        cbArtikel.Enabled = bool
        cbLieferscheine.Enabled = bool
        cbFluege.Enabled = bool
        btnSync.Enabled = bool
    End Sub

    ''' <summary>
    ''' Die Tooltips der Controls erstellen
    ''' </summary>
    Private Sub CreateToolTips()
        Dim syncButtonToolTip As ToolTip = CreateToolTip(),
            mandantToolTip As ToolTip = CreateToolTip(),
            flsaccountToolTip As ToolTip = CreateToolTip(),
            btnCancelToolTip As ToolTip = CreateToolTip(),
            addressSyncToolTip As ToolTip = CreateToolTip(),
            articleExportToolTip As ToolTip = CreateToolTip(),
            deliveryImportToolTip As ToolTip = CreateToolTip(),
            flightImportToolTip As ToolTip = CreateToolTip(),
            lastSuccessToolTip As ToolTip = CreateToolTip(),
            syncBtnToolTip As ToolTip = CreateToolTip()


        mandantToolTip.ToolTipTitle = "PROFFIX Mandant"
        flsaccountToolTip.ToolTipTitle = "FLS Account"
        mandantToolTip.SetToolTip(lblMandant, "Auf diese Datenbank wird durch das Programm zugegriffen")
        flsaccountToolTip.SetToolTip(lblAccount, "Benutzernamen, mit dem auf die FLS-Daten zugegriffen wird")
        btnCancelToolTip.SetToolTip(BtnCancel, "Den laufenden Prozess beenden")


        mandantToolTip.ToolTipTitle = Nothing
        addressSyncToolTip.SetToolTip(cbAdressen, "Anklicken, wenn die Adressen synchronisiert werden sollen." + vbCrLf + "Adressen müssen bei Lieferschein- bzw. Flugdatenimporten immer auch synchronisiert werden.")
        articleExportToolTip.SetToolTip(cbArtikel, "Anklicken, wenn die Artikel aus Proffix in FLS exportiert werden sollen.")
        deliveryImportToolTip.SetToolTip(cbLieferscheine, "Anklicken, wenn die Lieferscheine aus FLS in Proffix importiert werden sollen." + vbCrLf + "(Adressen werden automatisch auch synchronisiert)")
        flightImportToolTip.SetToolTip(cbFluege, "Anklicken, wenn die Flugdaten aus FLS in Proffix importiert werden sollen." + vbCrLf + "(Adressen werden automatisch auch synchronisiert)")
        lastSuccessToolTip.SetToolTip(lblLastSuccess, "Zu diesem Zeitpunkt wurde welcher Prozess das letzte Mal vollständig, erfolgreich ausgeführt.")
        syncBtnToolTip.SetToolTip(btnSync, "Die angeklickten Prozesse werden ausgeführt")


        tsmiSettings.ToolTipText = "Anmeldedaten und Datumsbereich für Flugdatenimport setzen"
        tsmiClearLogView.ToolTipText = "Leert die Log-Konsole"
        ' tsmiInstallation.ToolTipText = "Installationshilfe anzeigen"
        tsmiCheckAdressLink.ToolTipText = "Prüft, ob alle Adressen in FLS und PROFFIX richtig verknüpft sind"
        tsmiClose.ToolTipText = "Schliesst das Programm FLSGliderSync"

    End Sub

    ''' <summary>
    ''' Ein Tooltip Objekt einheitlich initialisieren
    ''' </summary>
    Private Function CreateToolTip() As ToolTip
        Return New ToolTip() With {.IsBalloon = True,
                                    .UseAnimation = True,
                                    .UseFading = True,
                                    .AutomaticDelay = 1000}
    End Function


    'startet bzw. beendet Rotation des Bildes
    Private Sub rotateLoadingImage(ByVal rotate As Boolean)
        RotateValue = 2

        ' wenn Prozess am Laufen --> LadenBild rotieren lassen
        If rotate Then
            'Die Rotation des "Laden" Bild starten
            LoadingImageThread = New Thread(New ThreadStart(Sub()
                                                                Dim defaultImage = pbLoading.Image
                                                                Dim index = 0

                                                                While rotate
                                                                    Thread.Sleep(25)
                                                                    Invoke(New Action(Sub()
                                                                                          pbLoading.Image = GeneralHelper.RotateImage(defaultImage, New PointF(12, 12), RotateValue * index)
                                                                                      End Sub))
                                                                    index += 1
                                                                End While
                                                            End Sub))
            LoadingImageThread.Start()
            'wenn Prozess beendet --> Ladenbild ausblenden)
        Else
            LoadingImageThread.Abort()
        End If
    End Sub



    '********************************************************************************Log**************************************************************
    ''' <summary>
    ''' Eine Nachricht im Log anzeigen
    ''' </summary>
    ''' <param name="message">Die Nachricht</param>
    Friend Sub Log(ByVal message As String)
        If lbLog.InvokeRequired Then
            lbLog.Invoke(Sub() Log(message))
            Return
        End If
        lbLog.Items.Add(message)
        If (lbLog.SelectedIndex = lbLog.Items.Count - 2) Then
            lbLog.SelectedIndex = lbLog.Items.Count - 1
        End If
    End Sub

    ''' <summary>
    ''' Laden des letzten Synchronisationsdatums
    ''' </summary>
    ''' <returns>Das letzte Synchronisationsdatum</returns>
    Private Function LoadLastDate(ByVal synctype As String) As DateTime
        Try
            Return pxHelper.GetDate(synctype)
        Catch exce As Exception
            'Der Fehler wird geloggt und der Benutzer gefragt ob er die Zusaztabelle erstellt hat und ob er die Installationshilfe angezeigt braucht
            Logger.GetInstance().Log(LogLevel.Info, exce)
            MessageBox.Show("Eine Zusatztabelle names 'ZUS_FLSSyncDate' muss zuerst erzeugt werden!", "Warnung", MessageBoxButtons.OK)
        End Try
    End Function

    ''' <summary>
    ''' Speichern des letzen Synchronisationsdatums
    ''' </summary>
    ''' <param name="lastDate">Das Datum</param>
    Private Function SaveLastDate(ByVal lastDate As DateTime, ByVal synctype As String) As Boolean
        Dim fehler As String = String.Empty
        If Not pxHelper.SetDate(lastDate, synctype, fehler) Then
            Log(vbTab + "Fehler: LastDate konnte für " + synctype + " nicht in der DB gespeichert werden.")
            Logger.GetInstance.Log(LogLevel.Exception, "LastDate konnte für " + synctype + " in ZUS_FLSSyncDate nicht gespeichert werden. " + fehler)
        End If
        Return True
    End Function


    Private Sub btnSync_Click(sender As Object, e As EventArgs) Handles btnSync.Click
        ' es wurde noch kein Prozess ausgeführt (hier weil für syncProcessFinished in PrepareWork() zu spät, da sonst bei 2. Ausführen gar nicht dorthin kommt, da noch true
        linkProcessFinished = False
        syncProcessFinished = False
        exportProcessFinished = False
        deliveryImportProcessFinished = False
        flightImportProcessFinished = False
        startNextProcess(False)
    End Sub

    ' handelt den Ablauf der Synchronisation (welcher Prozess wann und ob er ausgeführt wird)
    Private Sub startNextProcess(ByVal fehlerAufgetreten As Boolean)
        ' ist ein Fehler aufgetreten --> beenden
        If fehlerAufgetreten Then
            EndWork()
            ' ist kein Fehler aufgetreten --> nächster Prozess
        Else

            ' wenn die Adresssynchronisation noch nicht erfolgreich ausgeführt wurde --> ausführen, wenn angeklickt
            If Not linkProcessFinished Then
                PrepareWork()
                DoLoadGeneralData()
                DoLinkAdresses()
            ElseIf linkProcessFinished Then
                If Not syncProcessFinished Then
                    DoSyncAdresses()
                ElseIf syncProcessFinished Then
                    If Not exportProcessFinished Then
                        DoExportArticles()
                    ElseIf exportProcessFinished Then
                        If Not deliveryImportProcessFinished Then
                            DoDeliveryImport()
                        ElseIf deliveryImportProcessFinished Then
                            If Not flightImportProcessFinished Then
                                DoFlightImport()
                            ElseIf flightImportProcessFinished Then
                                EndWork()
                            End If
                        End If
                    End If
                End If
            End If
        End If
    End Sub

    Private Sub tsmiClearLink_Click(sender As Object, e As EventArgs) Handles tsmiClearLink.Click
        PrepareWork()
        If DialogResult.OK = MessageBox.Show("Sind Sie sicher, dass Sie die Verknüpfung der Adressen aus FLS und Proffix aufheben wollen? " + vbCrLf + vbCrLf +
                                             "Wenn sie danach die nächste Adresssynchronisation ausführen, werden die Adressen neu verknüpft." + vbCrLf + vbCrLf +
                                             "Danach werden alle Adressen in FLS als aktueller gelten und in Proffix aktualisiert!!!", "Verknüpfung wirklich löschen?", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) Then
            If Not Linker.verknuepfungenAufheben() Then
                Log("Fehler beim Aufheben der Verknüpfungen der Adresse")
            Else
                Log("Verknüpfungen erfolgreich aufgehoben")
            End If
        End If
        EndWork()
    End Sub

End Class

