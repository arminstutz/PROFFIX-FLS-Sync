using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using SMC;
using SMC.Lib;

namespace FlsGliderSync
{

    // Tipps:
    // - Fehlermeldungen/Warnungen wegbringen, indem "Release" anstatt "Debug"-Modus
    // wenn im ini "Testumgebung=1" eingetragen ist, werden die URLs auf das Testsystem geleitet, sonst auf das scharfe FLS! (Kontrolle in Einstellungen möglich, welche URL gilt)
    // --> für Installation beim Kunden muss "Testumgebung=1" aus dem ini entfernt werden
    // --> bei Installation: URLs prüfen (werden in log angegeben, sofern in .ini logAusfuehrlich = 1)

    // im Ini kann angegeben werden, falls alle Adressen vom einen zum anderen System geupdatet werden sollen (letzte Änderung wird dann ignoriert) Dies ist bei der 1. Synchronisation nötig, da dann nur FLS die Werte der Zusatzfelder kennt
    // im ini muss immer master= stehen. Falls master=fls steht, werden alle Adressen von fls in PX geschrieben. Umgekehrt mit master=proffix
    // Bei Installtion des Programms muss somit im ini master=fls gesetzt werden, und nach der 1. Synchronisation master= gesetzt werden

    // - relevante Webseiten + Anmeldung: 
    // - https://test.glider-fls.ch bzw. https://fls.glider-fls.ch --> API --> welche Methoden könen aufgerufen werden
    // - https://test.glider-fls.ch/client --> Anmeldung Testumgebung (Userdaten: bei Patrick Schuler anfragen)
    // - https://fls.glider-fls.ch/client --> Anmeldung FLS (Userdaten: bei Patrick Schuler anfragen)


    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! TODO !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    // 2. JSON wird mir neben Artikel Rabatt geben (Bsp. 50%, da er nur die Hälfte bezahlt) --> implementieren, dass dies berücksichtigt wird
    // 1. Patrick Schuler wird noch implementieren, dass Fehlermeldungen von FLS mehr Infos enthalten --> dementsprechendes Fehlerhandling anpassen
    // 2. AdressSynchronisation synchronisiert folgende Felder noch nicht: Midname, CompanyName (nicht in FLS: Tel direkt)


    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!Anleitung, um Feld für Person hinzuzufügen!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    // - in Proffix entsprechendes Zusatzfeld erstellen
    // - in Klasse PersonMapper bzw. ClubMapper (wenn in JSON unter clubrelated) entsprechende 3 Zeilen für dieses Feld mit den Namen aus JSON und Zusatzfeld hinzufügen
    // - in Klasse ProffixHelper in Funktion GetZusatzFelderSql() entsprechende Zeile hinzufügen. (wenn Datum --> muss separat in datumsZusatzfelderInProffixEinfuegen() hinzugefügt werden)

    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!Anleitung, um Zusatzfeld für Flug hinzuzufügen!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    // - in Proffix entsprechendes Zusatzfeld erstellen
    // - in ProffixHelper in allen fill_ZUS (bzw. doInsertIntoTable) hinzufügen


    // *********************************************************************************************************************************************************

    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!ANPASSUNG TEST <-> SCHARFE VERSION während Implementation!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    // dazu fuerScharfeVersionAnpassen bzw. fuerTestVersionAnpassen in FrmMain.New auskommentieren
    // die Funktion testFluegeFreigeben() (Aufruf in Importer) ist nur für den Testbetrieb gedacht


    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! Kurzanleitung Installation Programm:
    // - Zip-Ordner mit exe, ini, reg, ztxs..., zfx, dlls (SMC.lib.dll und SMC.Proffix.dll)
    // - Zusatztabellen erstellen
    // - alle Zusatzfelder in ADR_Adressen einfügen
    // - Zusatzfeld ArticleId in LAG_Artikel
    // - Dokumenttyp (LS) mit Kürzel FLS erstellen
    // - In Proffix Bezeichnung1 = Artikelname, Bezeichnung2 = ARtikelinfo, Bezeichnung3 = Artikelbeschreibung, Bezeichnung5 = InvoiceLineText


    /// <summary>
/// Das Hauptfenster
/// </summary>
    public partial class FrmMain
    {

        // Private waitHandlerSyncerThread As New System.Threading.AutoResetEvent(False)

        /// <summary>
    /// Initialisiert den Dialog
    /// </summary>
        public FrmMain()
        {

            // wenn aus irgend einem Grund die Settings auf die Testumgebung umgestellt haben --> wieder auf scharfe Version umstellen (Anmeldedaten fallen aber raus)
            if (My.MySettingsProperty.Settings.ServiceAPITokenMethod.Contains("test.glider-fls.ch"))
            {
                fuerScharfeVersionAnpassen();
                if (FlsGliderSync.logAusfuehrlich)
                {
                    Logger.GetInstance().Log(LogLevel.Info, "fuerScharfeVersionAnpassen() wurde ausgeführt");
                }
            }

            // für debuggen auf TestURLs umstellen (wenn auf Testumgebung gestellt werden soll, muss in ini "Testumgebung=1" angegeben werden)
            // wenn der key "Testumgebung" in ini fehlt, --> scharfe Version
            if (GeneralHelper.readFromIni("Testumgebung", false) == "1")
            {
                fuerTestVersionAnpassen();
                if (FlsGliderSync.logAusfuehrlich)
                {
                    Logger.GetInstance().Log(LogLevel.Info, "fuerTestversionAnpassen() wurde ausgeführt");
                }
            }

            // auslesen, ob ein Master definiert ist
            // wenn master=fls --> Beim Sync wird im Fall eines Updates immer PX mit FLS überschrieben (benötigt bei 1. Synchronisation)
            // wenn master=proffix --> Beim sync wird im Fall eines Updates immer FLS mit PX überschrieben
            // in allen anderen Fällen "master= ,master=irgendwasanderes --> Beim Sync wird im Fall eiens updates die zuletzt veränderte Adresse verwendet
            string masterdb = GeneralHelper.readFromIni("master").ToLower();
            if (masterdb == "fls")
            {
                FlsGliderSync.Master = UseAsMaster.fls;
            }
            else if (masterdb == "proffix")
            {
                FlsGliderSync.Master = UseAsMaster.proffix;
            }
            else
            {
                FlsGliderSync.Master = UseAsMaster.undefined;
            }

            if (FlsGliderSync.logAusfuehrlich)
            {
                Logger.GetInstance().Log(LogLevel.Info, My.MySettingsProperty.Settings.ServiceAPITokenMethod);
                Logger.GetInstance().Log(LogLevel.Info, My.MySettingsProperty.Settings.ServiceAPIPersonMethod);
                Logger.GetInstance().Log(LogLevel.Info, My.MySettingsProperty.Settings.ServiceAPIDeliveriesNotProcessedMethod);
                Logger.GetInstance().Log(LogLevel.Info, My.MySettingsProperty.Settings.ServiceAPIDeletedPersonFulldetailsMethod);
                Logger.GetInstance().Log(LogLevel.Info, My.MySettingsProperty.Settings.ServiceAPIModifiedPersonFullDetailsMethod);
                Logger.GetInstance().Log(LogLevel.Info, My.MySettingsProperty.Settings.ServiceAPICountriesMethod);
                Logger.GetInstance().Log(LogLevel.Info, My.MySettingsProperty.Settings.ServiceAPIArticlesMethod);
                Logger.GetInstance().Log(LogLevel.Info, My.MySettingsProperty.Settings.ServiceAPIPersonsMemberNrMethod);
                Logger.GetInstance().Log(LogLevel.Info, My.MySettingsProperty.Settings.ServiceAPIMemberStates);
                Logger.GetInstance().Log(LogLevel.Info, My.MySettingsProperty.Settings.ServiceAPIDeliveredMethod);
                Logger.GetInstance().Log(LogLevel.Info, My.MySettingsProperty.Settings.ServiceAPIModifiedFlightsMethod);
                Logger.GetInstance().Log(LogLevel.Info, My.MySettingsProperty.Settings.ServiceAPIAircraftsMethod);
                Logger.GetInstance().Log(LogLevel.Info, My.MySettingsProperty.Settings.ServiceAPILocationsMethod);
            }

            // Controls Initialisieren
            InitializeComponent();
            Logger.GetInstance().Log(LogLevel.Info, "Starting main form...");
            CreateToolTips();

            // anklickbare Elemente auf enabled = false setzen, bis Anmeldungsversuch abgeschlossen
            enableFormElements(false);

            // *********************************************************************bei FLS anmelden******************************************************************
            // bei FLS anmelden und anzeigen, ob erfolgreich

            // bei FLS anmelden
            // Die Verbindung konnte nicht aufgebaut werden
            var openConnectionThread = new Thread(new ThreadStart(() => { try { OpenFLSConnection(); } catch (Exception exce) { Logger.GetInstance().Log(LogLevel.Exception, new Exception("Verbindung zu FLS mit den Standardwerten fehlgeschlagen.", exce)); } }));
            openConnectionThread.Start();

            // verwendeter Account anzeigen
            lblAccount.Text = My.MySettingsProperty.Settings.Username;
            // *****************************************************************bei Proffix anmelden*****************************************************************
            FlsGliderSync.Proffix.GoBook.LoginUser = Assembly.GetExecutingAssembly().GetName().Name;
            // mit Proffix verbinden und anzeigen, ob erfolgreich
            if (!FlsGliderSync.Proffix.Open())
            {
                Log("Proffix-Anmeldung fehlgeschlagen.");
                if (DialogResult.OK == MessageBox.Show("Das Programm konnte sich nicht korrekt in Proffix anmelden. Kontrollieren Sie die Konfigurationsdateien oder kontaktieren Sie den Support. Das Programm muss neu gestartet werden", "Proffix-Anmeldung", MessageBoxButtons.OK, MessageBoxIcon.Error))
                {
                    // wenn Proffixanmeldung fehlgeschlagen funktioniert Programm nicht --> Programm beenden
                    Environment.Exit(0);
                }
            }

            // Proffix-Anemdlung erfolgreich
            Log("Proffix-Anmeldung erfolgreich");
            // verwendeter Mandant anzeigen
            lblMandant.Text = FlsGliderSync.Proffix.GoBook.Mandant;


            // ***********************************************************Klassen initialisieren******************************************************************
            initializeProcessClasses();

            // FrmSettings initialisieren, um start/enddate für Flugdatenimport richtig zu handeln, falls bei FrmSettings Cancel geklickt wird
            frmSettings = new FrmSettings();
            frmLS = new FrmLSManage(FlsConn);

            // Die letzte Synchonisation anzeigen
            var action = new Action(() =>
                {
                    if (Syncer.LastSync == default == false)
                    {
                        lblLastSyncDate.Text = Syncer.LastSync.ToShortDateString() + " " + Syncer.LastSync.ToShortTimeString();
                    }

                    if (Exporter.LastExport == default == false)
                    {
                        lblLastExportDate.Text = Exporter.LastExport.ToShortDateString() + " " + Exporter.LastExport.ToShortTimeString();
                    }

                    if (Importer.lastDeliveryImport == default == false)
                    {
                        lblLastDeliveryImportDate.Text = Importer.lastDeliveryImport.ToShortDateString() + " " + Importer.lastDeliveryImport.ToShortTimeString();
                    }

                    if (Importer.lastFlightImport == default == false)
                    {
                        lblLastFlightImportDate.Text = Importer.lastFlightImport.ToShortDateString() + " " + Importer.lastFlightImport.ToShortTimeString();
                    }
                });
            if (lblLastSyncDate.InvokeRequired)
            {
                Invoke(action);
            }
            else
            {
                action.Invoke();
            }

            // warten bis FLS angemeldet ist, bevor FrmMain anklickbar werden soll
            while (!flsLoginFinished)
            {
            }

            enableFormElements(true);
            _lbLog.Name = "lbLog";
            _tsmiSettings.Name = "tsmiSettings";
            _tsmiClearLogView.Name = "tsmiClearLogView";
            _tsmiCheckAdressLink.Name = "tsmiCheckAdressLink";
            _tsmiClearLink.Name = "tsmiClearLink";
            _tsmiClose.Name = "tsmiClose";
            _BtnCancel.Name = "BtnCancel";
            _cbAdressen.Name = "cbAdressen";
            _btnSync.Name = "btnSync";
            _cbFluege.Name = "cbFluege";
            _cbLieferscheine.Name = "cbLieferscheine";
            _lblHelp.Name = "lblHelp";
        }

        // ***********************************************************Hilfsfunktionen während Implementation****************************************************************
        // nur während Implementation!! Macht Anpassungen in Einstellungen, um das Programm auf der scharfen Version laufen zu lassen
        // dazu in New() des FrmMains zum Ausführen nicht mehr auskommentieren
        private void fuerScharfeVersionAnpassen()
        {

            // User + Passwort auf "" setzen, damit man nicht ein fremdes Login übernehmen kann
            My.MySettingsProperty.Settings.Username = "";
            My.MySettingsProperty.Settings.Password = ProffixCrypto.Encrypt("", My.MySettingsProperty.Settings.Crypto);

            // URLs ersetzen
            My.MySettingsProperty.Settings.ServiceAPITokenMethod = My.MySettingsProperty.Settings.ServiceAPITokenMethod.Replace("test.glider-fls.ch/", "fls.glider-fls.ch/");
            My.MySettingsProperty.Settings.ServiceAPIPersonMethod = My.MySettingsProperty.Settings.ServiceAPIPersonMethod.Replace("test.glider-fls.ch/", "fls.glider-fls.ch/");
            My.MySettingsProperty.Settings.ServiceAPIDeliveriesNotProcessedMethod = My.MySettingsProperty.Settings.ServiceAPIDeliveriesNotProcessedMethod.Replace("test.glider-fls.ch/", "fls.glider-fls.ch/");
            My.MySettingsProperty.Settings.ServiceAPIDeletedPersonFulldetailsMethod = My.MySettingsProperty.Settings.ServiceAPIDeletedPersonFulldetailsMethod.Replace("test.glider-fls.ch/", "fls.glider-fls.ch/");
            My.MySettingsProperty.Settings.ServiceAPIModifiedPersonFullDetailsMethod = My.MySettingsProperty.Settings.ServiceAPIModifiedPersonFullDetailsMethod.Replace("test.glider-fls.ch/", "fls.glider-fls.ch/");
            My.MySettingsProperty.Settings.ServiceAPICountriesMethod = My.MySettingsProperty.Settings.ServiceAPICountriesMethod.Replace("test.glider-fls.ch/", "fls.glider-fls.ch/");
            My.MySettingsProperty.Settings.ServiceAPIArticlesMethod = My.MySettingsProperty.Settings.ServiceAPIArticlesMethod.Replace("test.glider-fls.ch/", "fls.glider-fls.ch/");
            My.MySettingsProperty.Settings.ServiceAPIPersonsMemberNrMethod = My.MySettingsProperty.Settings.ServiceAPIPersonsMemberNrMethod.Replace("test.glider-fls.ch/", "fls.glider-fls.ch/");
            My.MySettingsProperty.Settings.ServiceAPIMemberStates = My.MySettingsProperty.Settings.ServiceAPIMemberStates.Replace("test.glider-fls.ch/", "fls.glider-fls.ch/");
            My.MySettingsProperty.Settings.ServiceAPIDeliveredMethod = My.MySettingsProperty.Settings.ServiceAPIDeliveredMethod.Replace("test.glider-fls.ch/", "fls.glider-fls.ch/");
            My.MySettingsProperty.Settings.ServiceAPIModifiedFlightsMethod = My.MySettingsProperty.Settings.ServiceAPIModifiedFlightsMethod.Replace("test.glider-fls.ch/", "fls.glider-fls.ch/");
            My.MySettingsProperty.Settings.ServiceAPIAircraftsMethod = My.MySettingsProperty.Settings.ServiceAPIAircraftsMethod.Replace("test.glider-fls.ch/", "fls.glider-fls.ch/");
            My.MySettingsProperty.Settings.ServiceAPILocationsMethod = My.MySettingsProperty.Settings.ServiceAPILocationsMethod.Replace("test.glider-fls.ch/", "fls.glider-fls.ch/");

            // wenn weitere Methoden für den FLS Zugriff dazu kommen, müssen sie hier entsprechend ergänzt werden, damit bei Umstellung von Test auf scharf alle angepasst werden
            // Idee als TODO: in For each durch alle My.Settings Eigenschaften gehen (funktioniert nicht)

            // angepasste Einstellungen speichern
            My.MySettingsProperty.Settings.Save();
        }

        // nur während Implementation!! Macht Anpassungen in Einstellungen, um das Programm auf den Testdaten laufen zu lassen
        // dazu in New() des FrmMains zum Ausführen nicht mehr auskommentieren
        private void fuerTestVersionAnpassen()
        {
            // die im Programm ursprünglich angegebenen Einstellungen wiederherstellen
            My.MySettingsProperty.Settings.Reset();
        }

        // *******************************************************************Klasse FrmMain***********************************************************************************
        // Private GoBook As New pxBook.pxKommunikation
        private FrmSettings frmSettings { get; set; }
        private FrmLSManage frmLS { get; set; }

        // Klassen für die Verbindung zum FLS bzw. zur Proffix-DB
        private FlsConnection FlsConn { get; set; } = new FlsConnection();
        internal ProffixConnection MyConn { get; set; } = new ProffixConnection();
        private ProffixHelper pxHelper { get; set; } = new ProffixHelper(new ProffixConnection());

        // Die Klassen für die möglichen Prozesse
        private GeneralDataLoader generalLoader;

        private LinkAdressen Linker { get; set; }
        private Syncer Syncer { get; set; }
        private Exporter Exporter { get; set; }
        private Importer Importer { get; set; }

        // Threads
        private Thread LinkerThread { get; set; }
        private Thread SyncerThread { get; set; }
        private Thread ExporterThread { get; set; }
        private Thread DeliveryImporterThread { get; set; }
        private Thread FlightImporterThread { get; set; }
        private Thread LoadingImageThread { get; set; }   // für Drehen des Lade-Bildes

        // Exceptions
        private Exception LinkerException { get; set; }
        private Exception SyncerException { get; set; }
        private Exception ExporterException { get; set; }
        private Exception DeliveryImporterException { get; set; }
        private Exception FlightImporterException { get; set; }

        // Hilfsvariablen
        // Private Property IsAllInOne As Boolean = False        ' Flag, ob alle 3 Prozesse ausgeführt werden sollen (Import bei fehlender AdressNr ergibt Fehler --> zuerst Adresssynchronisation)
        private int RotateValue { get; set; } = 2;     // definiert, wie schnell das Lade-Bild rotiert
        private bool linkersuccessful { get; set; } = false;
        private bool syncsuccessful { get; set; } = false;    // definiert, ob die Adresssynchronisation geklappt hat --> SaveLastDate
        private bool exportsuccessful { get; set; } = false;  // definiert, ob der Artikelexport geklappt hat --> SaveLastDate
        private bool deliveryimportsuccessful { get; set; } = false;
        private bool flightimportsuccessful { get; set; } = false;
        private bool linkProcessFinished { get; set; } = false;
        private bool syncProcessFinished { get; set; } = false;
        private bool exportProcessFinished { get; set; } = false;
        private bool deliveryImportProcessFinished { get; set; } = false;
        private bool flightImportProcessFinished { get; set; } = false;
        private bool flsLoginFinished { get; set; } = false;

        // ***********************************************************************Connection*************************************************************************************
        /// <summary>
    /// Öffnen der Verbindung  und Initialisieren der Manager Klassen
    /// </summary>
        private async void OpenFLSConnection()
        {
            flsLoginFinished = false;
            // Login in FLS
            if (await FlsConn.Login(My.MySettingsProperty.Settings.Username, ProffixCrypto.Decrypt(My.MySettingsProperty.Settings.Password, My.MySettingsProperty.Settings.Crypto), My.MySettingsProperty.Settings.ServiceAPITokenMethod))
            {
                // lblFLSVerbunden.Text = "erfolgreich"
                Log("FLS-Anmeldung erfolgreich");
            }
            else
            {
                // lblFLSVerbunden.Text = "fehlgeschlagen"
                Log("FLS-Anmeldung fehlgeschlagen. Überprüfen Sie unter Menu --> Einstellungen den Usernamen und das Passwort");
            }

            flsLoginFinished = true;
        }

        // initialisiert die Klassen für die verschiedenen Prozesse, welche das Programm ausführen kann
        private void initializeProcessClasses()
        {

            // Importerklasse initialisieren
            var arglastDeliveryImport = LoadLastDate("DeliveryImport");
            var arglastFlightImport = LoadLastDate("FlightImport");
            var argclient = FlsConn;
            var argpxhelper = pxHelper;
            var argMyConn = MyConn;
            Importer = new Importer(ref arglastDeliveryImport, ref arglastFlightImport, ref argclient, ref argpxhelper, ref argMyConn)
            {
                Log = new Action<string>(message => Invoke(new Action(() => Log(message)))),
                DoProgressDelivery = new Action(() => Invoke(new Action(DoImporterProgressDelivery))),
                DoProgressFlight = new Action(() => Invoke(new Action(DoImporterProgressFlight)))
            };
            FlsConn = argclient;
            pxHelper = argpxhelper;
            MyConn = argMyConn;

            // GeneralLoader initialisieren
            generalLoader = new GeneralDataLoader(pxHelper, FlsConn, MyConn, Importer);

            // Linker Klasse Initialisieren und aktionen verknüpfen
            var argpxHelper = pxHelper;
            var argMyConn1 = MyConn;
            Linker = new LinkAdressen(LoadLastDate("AddressSync"), FlsConn, ref argpxHelper, ref argMyConn1, generalLoader)
            {
                Log = new Action<string>(message => Invoke(new Action(() => Log(message)))),
                DoProgress = new Action(() => Invoke(new Action(DoLinkerProgress)))
            };
            pxHelper = argpxHelper;
            MyConn = argMyConn1;

            // Syncer Klasse Initialisieren und aktionen verknüpfen
            var argpxHelper1 = pxHelper;
            var argMyConn2 = MyConn;
            Syncer = new Syncer(LoadLastDate("AddressSync"), FlsConn, ref argpxHelper1, ref argMyConn2, generalLoader)
            {
                Log = new Action<string>(message => Invoke(new Action(() => Log(message)))),
                DoProgress = new Action(() => Invoke(new Action(DoSyncerProgress)))
            };
            pxHelper = argpxHelper1;
            MyConn = argMyConn2;

            // Exporterklasse initialisieren
            var argserviceClient = FlsConn;
            var argpxHelper2 = pxHelper;
            var argMyconn = MyConn;
            Exporter = new Exporter(LoadLastDate("ArticleExport"), ref argserviceClient, ref argpxHelper2, ref argMyconn)
            {
                Log = new Action<string>(message => Invoke(new Action(() => Log(message)))),
                DoProgress = new Action(() => Invoke(new Action(DoExporterProgress)))
            };
            FlsConn = argserviceClient;
            pxHelper = argpxHelper2;
            MyConn = argMyconn;
        }











        // *********************************************************************************Prozesse**********************************************************************************
        private void DoLoadGeneralData()
        {
            Log("Laden allgemeiner Daten gestartet");
            Logger.GetInstance().Log(LogLevel.Info, "Allgemeine Daten werden geladen");
            if (!generalLoader.loadGeneralData())
            {
                Log(Constants.vbTab + "Fehler beim Laden der allgemeinen Daten");
                Logger.GetInstance().Log(LogLevel.Info, "Fehler beim Allgemeine Daten werden geladen");
            }
            else
            {
                Log("Laden allgemeiner Daten erfolgreich abgeschlossen");
            }

            Log("");
        }
        // ************************************************************************************Adressen verknüpfen**********************************************************************
        private void DoLinkAdresses()
        {
            if (cbAdressen.Checked)
            {
                // Den Synchronisationsthread starten
                LinkerThread = new Thread(new ThreadStart(LinkerWork));
                LinkerThread.Start();
            }
            else
            {
                // Sync wird zwar nicht ausgeführt, aber die nächsten Prozesse sollen gestartet werden können
                LinkerWorkEnd();
            }
        }

        private void LinkerWork()
        {
            try
            {
                // MessageBox.Show("Debug: Linker wird übersprungen. Ist in LinkerWork() auskommentiert", "")
                linkersuccessful = Linker.Link();
            }
            catch (Exception exce)
            {
                // Den Fehler ausgeben und zurücksetzen
                Logger.GetInstance().Log(LogLevel.Critical, exce);
                Invoke(new Action(() => Log("Adressen verknüpfen fehlgeschlagen")));
                LinkerException = exce;
            }
            finally
            {
                Invoke(new Action(LinkerWorkEnd));
            }
        }

        private void LinkerWorkEnd()
        {
            // gab es bei der Adress-Synchronisation einen Fehler?
            if (LinkerException is object)
            {
                logException(SyncerException);
            }

            // wenn nur die Verknüpfung (über Menü) ausgeführt werden soll  --> nur noch beenden
            linkProcessFinished = true;
            startNextProcess(LinkerException is null & linkersuccessful ? false : true);
        }

        /// <summary>
    /// Anzeigen des Synchronisationsfortschritt
    /// </summary>
        private void DoLinkerProgress()
        {
            // ProgressBar aktualisieren
            pbMain.Maximum = Linker.Count;
            pbMain.Value = Linker.Progress;
        }

        // ***********************************************************************************AdressSync*********************************************************************************
        /// <summary>
    /// Alle Adressen Synchronisieren
    /// </summary>
        public void DoSyncAdresses()
        {
            if (cbAdressen.Checked)
            {
                // Den Synchronisationsthread starten
                SyncerThread = new Thread(new ThreadStart(SyncerWork));
                SyncerThread.Start();
            }
            else
            {
                // Sync wird zwar nicht ausgeführt, aber die nächsten Prozesse sollen gestartet werden können
                SyncerWorkEnd();
            }
        }

        /// <summary>
    /// Die Synchronisation durchführen
    /// </summary>
        private void SyncerWork()
        {
            try
            {
                // MsgBox("Debug: Adresssync wird übersprungen. Syncer.Sync in FrmMain auskommentiert.")
                syncsuccessful = Syncer.Sync();
            }
            catch (Exception exce)
            {
                // Den Fehler ausgeben und zurücksetzen
                Logger.GetInstance().Log(LogLevel.Critical, exce);
                Invoke(new Action(() => Log("Adresssynchronisation fehlgeschlagen...")));
                SyncerException = exce;
            }
            finally
            {
                Invoke(new Action(SyncerWorkEnd));
            }
        }

        /// <summary>
    /// Gibt Fehlermeldungen aus + ruft Beendigungsfunktion oder Artikelexport auf
    /// </summary>
        private void SyncerWorkEnd()
        {
            // gab es bei der Adress-Synchronisation einen Fehler?
            if (SyncerException is null)
            {
                // Letzte Synchronisation setzen
                if (Syncer.LastSync != default)
                {
                    // wenn Synchronisation der Adressen geklappt hat
                    if (syncsuccessful)
                    {
                        // ... und LastSync abgespeichert werden konnte
                        if (SaveLastDate(Syncer.LastSync, "AddressSync"))
                        {
                            // ... Datum Now anzeigen
                            lblLastSyncDate.Text = Syncer.LastSync.ToShortDateString() + " " + Syncer.LastSync.ToShortTimeString();
                        }
                    }
                }
            }
            else
            {
                logException(SyncerException);
                // EndWork()
            }

            syncProcessFinished = true;
            startNextProcess(SyncerException is null ? false : true);
        }

        /// <summary>
    /// Anzeigen des Synchronisationsfortschritt
    /// </summary>
        private void DoSyncerProgress()
        {
            // ProgressBar aktualisieren
            pbMain.Maximum = Syncer.Count;
            pbMain.Value = Syncer.Progress;
        }


        // **********************************************************************************ArticleExport******************************************************************************
        /// <summary>
    /// Alle Adressen Exporthronisieren
    /// </summary>
        public void DoExportArticles()
        {
            if (cbArtikel.Checked)
            {
                // Den Exporthronisationsthread starten
                ExporterThread = new Thread(new ThreadStart(ExporterWork));
                ExporterThread.Start();
            }
            else
            {
                ExporterWorkEnd();
            }
        }

        /// <summary>
    /// Die Exporthronisation durchführen
    /// </summary>
        private void ExporterWork()
        {
            // waitHandlerSyncerThread.WaitOne()
            try
            {
                // If syncsuccessful Then
                exportsuccessful = Exporter.Export();
            }
            // End If
            catch (Exception exce)
            {
                // Den Fehler ausgeben und zurücksetzen
                Logger.GetInstance().Log(LogLevel.Critical, exce);
                Invoke(new Action(() => Log("Artikelexport fehlgeschlagen ...")));
                ExporterException = exce;
            }
            finally
            {
                Invoke(new Action(ExporterWorkEnd));
            }
        }

        /// <summary>
    /// Gibt Fehlermeldungen aus + ruft Beendigungsfunktion oder FlightImport auf
    /// </summary>
        private void ExporterWorkEnd()
        {
            // gab es beim Artikel-Export einen Fehler?
            if (ExporterException is null)
            {
                // Letzte Exporthronisation setzen
                if (Exporter.LastExport != default)
                {
                    if (exportsuccessful)
                    {
                        if (SaveLastDate(Exporter.LastExport, "ArticleExport"))
                        {
                            lblLastExportDate.Text = Exporter.LastExport.ToShortDateString() + " " + Exporter.LastExport.ToShortTimeString();
                        }
                    }
                }
            }
            else
            {
                logException(ExporterException);
            }

            exportProcessFinished = true;
            startNextProcess(ExporterException is null ? false : true);
        }

        /// <summary>
    /// Anzeigen des Synchronisationsfortschritt
    /// </summary>
        private void DoExporterProgress()
        {
            // ProgressBar aktualisieren
            pbMain.Maximum = Exporter.Count;
            pbMain.Value = Exporter.Progress;
        }


        // ************************************************************************************Import************************************************************************************
        /// <summary>
    /// Den Import der Lieferscheine starten
    /// </summary>
        public void DoDeliveryImport()
        {
            if (cbLieferscheine.Checked)
            {
                // Den Importthread starten
                DeliveryImporterThread = new Thread(new ThreadStart(DeliveryImporterWork));
                DeliveryImporterThread.Start();
            }
            else
            {
                DeliveryImporterWorkEnd();
            }
        }

        /// <summary>
    /// Import der Flugdaten durchführen
    /// </summary>
    /// <remarks></remarks>
        private void DeliveryImporterWork()
        {
            // waitHandlerExporterThread.WaitOne()
            try
            {
                // If exportsuccessful Then
                if (!generalLoader.deleteIncompleteData())
                {
                    Log("Fehler beim Löschen von Daten");
                }
                else
                {
                    deliveryimportsuccessful = Importer.DeliveryImport();
                }
            }
            catch (Exception ex)
            {
                // Wenn ein Fehler auftaucht wird dieser ausgegeben
                Logger.GetInstance().Log(LogLevel.Critical, ex);
                Invoke(new Action(() => Log("Lieferscheinimport fehlgeschlagen")));
                DeliveryImporterException = ex;
            }
            finally
            {
                // unkomplette Daten für DeliveryIds + FlightIds löschen
                generalLoader.deleteIncompleteData();
                Invoke(new Action(DeliveryImporterWorkEnd));
            }
        }

        /// <summary>
    /// Gibt Fehlermeldungen aus + ruft Beendigungsfunktion auf
    /// </summary>
        private void DeliveryImporterWorkEnd()
        {

            // gab es beim FlightImport einen Fehler?
            if (DeliveryImporterException is null)
            {
                if (Importer.lastDeliveryImport != default)
                {
                    if (deliveryimportsuccessful)
                    {
                        if (SaveLastDate(Importer.lastDeliveryImport, "DeliveryImport"))
                        {
                            lblLastDeliveryImportDate.Text = Importer.lastDeliveryImport.ToShortDateString() + " " + Importer.lastDeliveryImport.ToShortTimeString();
                        }
                    }
                }
            }
            else
            {
                logException(DeliveryImporterException);
            }
            // waitHandlerDeliveryImporterThread.Set()
            deliveryImportProcessFinished = true;
            startNextProcess(DeliveryImporterException is null ? false : true);
        }


        // ''' <summary>
        // ''' Anzeigen des Synchronisationsfortschritt
        // ''' </summary>
        // Private Sub DoImporterProgress()
        // 'ProgressBar aktualisieren
        // pbMain.Maximum = Importer.DeliveryCount
        // pbMain.Value = Importer.Progress
        // End Sub

        /// <summary>
    /// Anzeigen des Synchronisationsfortschritt
    /// </summary>
        private void DoImporterProgressDelivery()
        {
            // ProgressBar aktualisieren
            pbMain.Maximum = Importer.DeliveryCount;
            pbMain.Value = Importer.ProgressDelivery;
        }

        /// <summary>
    /// Anzeigen des Synchronisationsfortschritt
    /// </summary>
        private void DoImporterProgressFlight()
        {
            // ProgressBar aktualisieren
            pbMain.Maximum = Importer.FlightCount;
            pbMain.Value = Importer.ProgressFlight;
        }



        /// <summary>
    /// Den Import der Flugdaten starten
    /// </summary>
        public void DoFlightImport()
        {
            if (cbFluege.Checked)
            {
                // Den Importthread starten
                FlightImporterThread = new Thread(new ThreadStart(FlightImporterWork));
                FlightImporterThread.Start();
            }
            else
            {
                FlightImporterWorkEnd();
            }
        }

        /// <summary>
    /// Import der Flugdaten durchführen
    /// </summary>
    /// <remarks></remarks>
        private void FlightImporterWork()
        {
            // waitHandlerDeliveryImporterThread.WaitOne()
            try
            {
                // If deliveryimportsuccessful Then
                if (!generalLoader.deleteIncompleteData())
                {
                    Log("Fehler beim Löschen von Daten");
                }
                else
                {
                    flightimportsuccessful = Importer.FlightImport();
                }
            }
            catch (Exception ex)
            {
                // Wenn ein Fehler auftaucht wird dieser ausgegeben
                Logger.GetInstance().Log(LogLevel.Critical, ex);
                Invoke(new Action(() => Log("Flugdatenimport fehlgeschlagen")));
                FlightImporterException = ex;
            }
            finally
            {
                // unkomplette Daten für DeliveryIds + FlightIds löschen
                generalLoader.deleteIncompleteData();
                Invoke(new Action(FlightImporterWorkEnd));
            }
        }

        /// <summary>
    /// Gibt Fehlermeldungen aus + ruft Beendigungsfunktion auf
    /// </summary>
        private void FlightImporterWorkEnd()
        {

            // gab es beim FlightImport einen Fehler?
            if (FlightImporterException is null)
            {
                if (Importer.lastFlightImport != default)
                {
                    if (flightimportsuccessful)
                    {
                        if (SaveLastDate(Importer.lastFlightImport, "FlightImport"))
                        {
                            lblLastFlightImportDate.Text = Importer.lastFlightImport.ToShortDateString() + " " + Importer.lastFlightImport.ToShortTimeString();
                        }
                    }
                }
            }
            else
            {
                logException(FlightImporterException);
            }

            // waitHandlerFlightImporterThread.Set()
            flightImportProcessFinished = true;
            startNextProcess(FlightImporterException is null ? false : true);
        }

        // ''' <summary>
        // ''' Anzeigen des Synchronisationsfortschritt
        // ''' </summary>
        // Private Sub DoFlightImporterProgress()
        // 'ProgressBar aktualisieren
        // pbMain.Maximum = Importer.FlightCount
        // pbMain.Value = Importer.Progress
        // End Sub

        // ''' <summary>
        // ''' Anzeigen des Synchronisationsfortschritt
        // ''' </summary>
        // Private Sub DoDeliveryImporterProgress()
        // 'ProgressBar aktualisieren
        // pbMain.Maximum = Importer.DeliveryCount
        // pbMain.Value = Importer.Progress
        // End Sub


        // ************************************************************************Button Clicks***********************************************************************************

        // wenn Lieferscheine und Flüge synchronisert werden sollen, müssen die Adressen aktuell sein --> müssen ebenfalls synchronisiert werden
        private void cbLieferscheine_CheckedChanged(object sender, EventArgs e)
        {
            if (cbLieferscheine.Checked)
            {
                cbAdressen.Checked = true;
            }
        }

        private void cbFluege_CheckedChanged(object sender, EventArgs e)
        {
            if (cbFluege.Checked)
            {
                cbAdressen.Checked = true;
            }
        }

        // Adressen müssen auch synchronisiert werden, wenn Lieferscheine erstellt bzw Flugdaten importiert werden sollen, da sonst Verknüpfung möglicherweise fehlt
        private void cbAdressen_CheckedChanged(object sender, EventArgs e)
        {
            if (!cbAdressen.Checked)
            {
                cbLieferscheine.Checked = false;
                cbFluege.Checked = false;
            }
        }

        public void FrmMain_KeyDown(object sender, KeyPressEventArgs e)
        {
            if (Conversions.ToString(e.KeyChar) == "" & ModifierKeys.HasFlag(Keys.Control) & lbLog.SelectedItem is object)
            {
                Clipboard.SetText(lbLog.SelectedItem.ToString());
            }
        }

        // ******************************************************************************Menu Klicks*************************************************************************************

        // Private Sub tsmiLSManage_Click(sender As Object, e As EventArgs) Handles tsmiLSManage.Click
        // MsgBox("Diese Funktion steht momentan noch nicht zur Verfügung")



        // ' enableFormElements(False)

        // 'frmLS.ShowDialog()

        // End Sub


        /// <summary>
    /// Es wurde auf "Einstellungen" geklickt
    /// </summary>
    /// <param name="sender">Der Button</param>
    /// <param name="e">Informationen zum Event</param>
        private async void tsmiSettings_Click(object sender, EventArgs e)
        {
            // Form auf nicht anklickbar stellen
            enableFormElements(false);

            // Speichern der alten Werte, um sie wieder hervorzuholen, falls Cancel geklickt wird
            // Dim old_startdate As DateTime = _startDate
            // Dim old_enddate As DateTime = _endDate

            // Werte für Username + Passwort neu aus Settings laden
            frmSettings.tbUser.Text = My.MySettingsProperty.Settings.Username;
            frmSettings.tbPassword.Text = ProffixCrypto.Decrypt(My.MySettingsProperty.Settings.Password, My.MySettingsProperty.Settings.Crypto);
            var dialogres = frmSettings.ShowDialog();

            // AWenn auf FormSettings Ok geklicht wurde
            if (dialogres == DialogResult.OK)
            {

                // Zeitspanne speichern, in der die Flugdaten importiert werden sollen
                // _startDate = frmSettings.dtpFrom.Value
                // _endDate = frmSettings.dtpTo.Value

                lbLog.Items.Clear();
                rotateLoadingImage(true);


                // wenn Username oder Passwort geändert wurde --> neu anmelden
                if (!((frmSettings.tbUser.Text ?? "") == (My.MySettingsProperty.Settings.Username ?? "")) | !((frmSettings.tbPassword.Text ?? "") == (My.MySettingsProperty.Settings.Password ?? "")))
                {

                    // neue Angaben in Settings speichern
                    My.MySettingsProperty.Settings.Password = ProffixCrypto.Encrypt(frmSettings.tbPassword.Text, My.MySettingsProperty.Settings.Crypto);
                    My.MySettingsProperty.Settings.Username = frmSettings.tbUser.Text;
                    My.MySettingsProperty.Settings.Save();
                    if (!await FlsConn.Login(My.MySettingsProperty.Settings.Username, ProffixCrypto.Decrypt(My.MySettingsProperty.Settings.Password, My.MySettingsProperty.Settings.Crypto), My.MySettingsProperty.Settings.ServiceAPITokenMethod))
                    {
                        // lblFLSVerbunden.Text = "fehlgeschlagen"
                        Log("FLS-Anmeldung mit neuen Daten fehlgeschlagen, überprüfen Sie den Usernamen und das Passwort");
                    }
                    else
                    {
                        Log("FLS-Anmeldung mit neuen Daten erfolgreich");
                        // lblFLSVerbunden.Text = "erfolgreich"
                    }

                    // neu eingegebenen FLS-Account auf FrmMain anzeigen
                    lblAccount.Text = My.MySettingsProperty.Settings.Username;
                }

                rotateLoadingImage(false);
            }
            else if (dialogres == DialogResult.Cancel)
            {
                // _startDate = old_startdate
                // _endDate = old_enddate
                // frmSettings.dtpFrom.Value = old_startdate
                // frmSettings.dtpTo.Value = old_enddate
                My.MySettingsProperty.Settings.Save();
            }

            // FrmMain wieder auf anklickbar stellen
            enableFormElements(true);
        }

        // wenn Flüge ab früherem Datum geladen werden sollen
        // Private Sub tsmiFormerFlights_Click(sender As Object, e As EventArgs)

        // MsgBox("Wird momentan nicht ausgeführt")

        // Dim frmFlights As New FrmFlightImport

        // Dim dialogres As DialogResult = frmFlights.ShowDialog()
        // ' wenn Ok geklickt wurde
        // If dialogres = DialogResult.OK Then

        // ' Datum, ab wann importiert werden soll, setzen.
        // Importer.lastFlightImport = frmFlights.dtpFlightVon.Value
        // '  Importer.BisFlightImport = frmFlights.dtpFlightBis.Value

        // ' Form vorbereiten
        // PrepareWork()

        // ' damit nur FlightImport ausgeführt wird
        // linkProcessFinished = True
        // syncProcessFinished = True
        // exportProcessFinished = True
        // deliveryImportProcessFinished = True
        // cbFluege.Checked = True

        // startNextProcess(False)

        // End If
        // End Sub


        // prüft, ob Verknüpfung der Adressen stimmt
        private void tsmiCheckAdressLink_Click(object sender, EventArgs e)
        {
            PrepareWork();
            Linker.checkAddressLink();
            EndWork();
        }

        /// <summary>
    /// leert Konsole
    /// </summary>
    /// <param name="sender">Der Button</param>
    /// <param name="e">Informationen zum Event</param>
        private void ClearLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!GeneralHelper.logArchivieren())
            {
                MessageBox.Show("Archivieren der Log-Datei fehlgeschlagen", "Archivieren fehlgeschlagen", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

            lbLog.Items.Clear();
        }

        // Programm schliessen
        private void tsmiClose_Click(object sender, EventArgs e)
        {
            Close();
            FlsGliderSync.Proffix.Close();
            Environment.Exit(0);
        }
        // ****************************************************************************Hilfe****************************************************************************************
        private void lblHelp_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("FLSHelp.pdf");
            }
            catch
            {
                MessageBox.Show("Fehler beim Öffnen der Datei FLSHelp.pdf. Kontrollieren Sie, ob die Datei im Prog-Verzeichnis vorhanden ist und ob ein Programm zum Öffnen von PDFs installiert ist.", "Fehler beim Öffnen der Datei", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        // *************************************************************************Abbruch***************************************************************************
        // es wurde Abbrechen geklickt
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            // Log("Es wurde Abbrechen geklickt")
            // Logger.GetInstance.Log("Es wurde Abbrechen geklickt. Prozess wird abgebrochen")
            Close();
        }

        // FrmMain wird geschlossen (Kreuz oben rechts geklickt, aus Abbrechen durch Me.Close() aufgerufen, oder andere Gründe)
        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            enableFormElements(false);
            Close();
            Log("Programm wird beendet");
            Logger.GetInstance().Log(LogLevel.Info, "FrmMain wird geschlossen.");
            generalLoader.deleteIncompleteData();
            FlsGliderSync.Proffix.Close();
            Environment.Exit(0);
        }


        // ***************************************************************************GUI Hilfsfunktionen**************************************************************************

        /// <summary>
    /// Vorbereitung auf die Prozessausführung
    /// </summary>
        private void PrepareWork()
        {

            // nicht anklickbar + Ladenbild
            enableFormElements(false);
            rotateLoadingImage(true);

            // Exception auf Nothing setzen
            SyncerException = null;
            ExporterException = null;
            DeliveryImporterException = null;
            FlightImporterException = null;

            // Progressbar
            pbMain.Value = 0;
            pbLoading.Visible = true;
            lbLog.Items.Clear();
            Log("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
        }

        /// <summary>
    /// Gibt aufgetretene Fehler aus
    /// </summary>
    /// <param name="exce"></param>
        private void logException(Exception exce)
        {
            if (FlsGliderSync.logAusfuehrlich)
            {
                Log("---------------------------------------------------------------------------------------------------");
                Log("Folgender Fehler ist aufgetreten");

                // Den Fehler detailiert anzeigen
                while (exce is object)
                {
                    Log(exce.Message);
                    Log(string.Empty);
                    if (exce.Source is object)
                    {
                        Log(exce.Source);
                    }

                    if (exce.TargetSite is object)
                    {
                        Log(exce.TargetSite.GetType().FullName + " - " + exce.TargetSite.Name);
                    }

                    if (exce.StackTrace is object)
                    {
                        foreach (string val in exce.StackTrace.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
                            Log(val);
                    }

                    if (exce.Data is object)
                    {
                        foreach (KeyValuePair<object, object> keyVal in exce.Data)
                            Log(keyVal.Key.ToString() + " : " + keyVal.Value.ToString());
                    }

                    Log("---------------------------------------------------------------------------------------------------");
                    exce = exce.InnerException;
                }
            }
        }

        /// <summary>
    /// Macht Form für nächsten Prozess bereit
    /// </summary>
        private void EndWork()
        {
            Log("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            enableFormElements(true);
            rotateLoadingImage(false);
            pbLoading.Visible = false;
        }

        // Buttons + Menu anklickbar, bzw. nicht anklickbar stellen
        private void enableFormElements(bool @bool)
        {
            msMain.Enabled = @bool;
            cbAdressen.Enabled = @bool;
            cbArtikel.Enabled = @bool;
            cbLieferscheine.Enabled = @bool;
            cbFluege.Enabled = @bool;
            btnSync.Enabled = @bool;
        }

        /// <summary>
    /// Die Tooltips der Controls erstellen
    /// </summary>
        private void CreateToolTips()
        {
            var syncButtonToolTip = CreateToolTip();
            var mandantToolTip = CreateToolTip();
            var flsaccountToolTip = CreateToolTip();
            var btnCancelToolTip = CreateToolTip();
            var addressSyncToolTip = CreateToolTip();
            var articleExportToolTip = CreateToolTip();
            var deliveryImportToolTip = CreateToolTip();
            var flightImportToolTip = CreateToolTip();
            var lastSuccessToolTip = CreateToolTip();
            var syncBtnToolTip = CreateToolTip();
            mandantToolTip.ToolTipTitle = "PROFFIX Mandant";
            flsaccountToolTip.ToolTipTitle = "FLS Account";
            mandantToolTip.SetToolTip(lblMandant, "Auf diese Datenbank wird durch das Programm zugegriffen");
            flsaccountToolTip.SetToolTip(lblAccount, "Benutzernamen, mit dem auf die FLS-Daten zugegriffen wird");
            btnCancelToolTip.SetToolTip(BtnCancel, "Den laufenden Prozess beenden");
            mandantToolTip.ToolTipTitle = null;
            addressSyncToolTip.SetToolTip(cbAdressen, "Anklicken, wenn die Adressen synchronisiert werden sollen." + Constants.vbCrLf + "Adressen müssen bei Lieferschein- bzw. Flugdatenimporten immer auch synchronisiert werden.");
            articleExportToolTip.SetToolTip(cbArtikel, "Anklicken, wenn die Artikel aus Proffix in FLS exportiert werden sollen.");
            deliveryImportToolTip.SetToolTip(cbLieferscheine, "Anklicken, wenn die Lieferscheine aus FLS in Proffix importiert werden sollen." + Constants.vbCrLf + "(Adressen werden automatisch auch synchronisiert)");
            flightImportToolTip.SetToolTip(cbFluege, "Anklicken, wenn die Flugdaten aus FLS in Proffix importiert werden sollen." + Constants.vbCrLf + "(Adressen werden automatisch auch synchronisiert)");
            lastSuccessToolTip.SetToolTip(lblLastSuccess, "Zu diesem Zeitpunkt wurde welcher Prozess das letzte Mal vollständig, erfolgreich ausgeführt.");
            syncBtnToolTip.SetToolTip(btnSync, "Die angeklickten Prozesse werden ausgeführt");
            tsmiSettings.ToolTipText = "Anmeldedaten und Datumsbereich für Flugdatenimport setzen";
            tsmiClearLogView.ToolTipText = "Leert die Log-Konsole";
            // tsmiInstallation.ToolTipText = "Installationshilfe anzeigen"
            tsmiCheckAdressLink.ToolTipText = "Prüft, ob alle Adressen in FLS und PROFFIX richtig verknüpft sind";
            tsmiClose.ToolTipText = "Schliesst das Programm FLSGliderSync";
        }

        /// <summary>
    /// Ein Tooltip Objekt einheitlich initialisieren
    /// </summary>
        private ToolTip CreateToolTip()
        {
            return new ToolTip()
            {
                IsBalloon = true,
                UseAnimation = true,
                UseFading = true,
                AutomaticDelay = 1000
            };
        }


        // startet bzw. beendet Rotation des Bildes
        private void rotateLoadingImage(bool rotate)
        {
            RotateValue = 2;

            // wenn Prozess am Laufen --> LadenBild rotieren lassen
            if (rotate)
            {
                // Die Rotation des "Laden" Bild starten
                LoadingImageThread = new Thread(new ThreadStart(() =>
                    {
                        var defaultImage = pbLoading.Image;
                        int index = 0;
                        while (rotate)
                        {
                            Thread.Sleep(25);
                            Invoke(new Action(() => pbLoading.Image = GeneralHelper.RotateImage(defaultImage, new PointF(12f, 12f), RotateValue * index)));
                            index += 1;
                        }
                    }));
                LoadingImageThread.Start();
            }
            // wenn Prozess beendet --> Ladenbild ausblenden)
            else
            {
                LoadingImageThread.Abort();
            }
        }



        // ********************************************************************************Log**************************************************************
        /// <summary>
    /// Eine Nachricht im Log anzeigen
    /// </summary>
    /// <param name="message">Die Nachricht</param>
        internal void Log(string message)
        {
            if (lbLog.InvokeRequired)
            {
                lbLog.Invoke(new Action(() => Log(message)));
                return;
            }

            lbLog.Items.Add(message);
            if (lbLog.SelectedIndex == lbLog.Items.Count - 2)
            {
                lbLog.SelectedIndex = lbLog.Items.Count - 1;
            }
        }

        /// <summary>
    /// Laden des letzten Synchronisationsdatums
    /// </summary>
    /// <returns>Das letzte Synchronisationsdatum</returns>
        private DateTime LoadLastDate(string synctype)
        {
            try
            {
                return pxHelper.GetDate(synctype);
            }
            catch (Exception exce)
            {
                // Der Fehler wird geloggt und der Benutzer gefragt ob er die Zusaztabelle erstellt hat und ob er die Installationshilfe angezeigt braucht
                Logger.GetInstance().Log(LogLevel.Info, exce);
                MessageBox.Show("Eine Zusatztabelle names 'ZUS_FLSSyncDate' muss zuerst erzeugt werden!", "Warnung", MessageBoxButtons.OK);
            }

            return default;
        }

        /// <summary>
    /// Speichern des letzen Synchronisationsdatums
    /// </summary>
    /// <param name="lastDate">Das Datum</param>
        private bool SaveLastDate(DateTime lastDate, string synctype)
        {
            string fehler = string.Empty;
            if (!pxHelper.SetDate(lastDate, synctype, ref fehler))
            {
                Log(Constants.vbTab + "Fehler: LastDate konnte für " + synctype + " nicht in der DB gespeichert werden.");
                Logger.GetInstance().Log(LogLevel.Exception, "LastDate konnte für " + synctype + " in ZUS_FLSSyncDate nicht gespeichert werden. " + fehler);
            }

            return true;
        }

        private void btnSync_Click(object sender, EventArgs e)
        {
            // es wurde noch kein Prozess ausgeführt (hier weil für syncProcessFinished in PrepareWork() zu spät, da sonst bei 2. Ausführen gar nicht dorthin kommt, da noch true
            linkProcessFinished = false;
            syncProcessFinished = false;
            exportProcessFinished = false;
            deliveryImportProcessFinished = false;
            flightImportProcessFinished = false;
            startNextProcess(false);
        }

        // handelt den Ablauf der Synchronisation (welcher Prozess wann und ob er ausgeführt wird)
        private void startNextProcess(bool fehlerAufgetreten)
        {
            // ist ein Fehler aufgetreten --> beenden
            if (fehlerAufgetreten)
            {
                EndWork();
            }
            // ist kein Fehler aufgetreten --> nächster Prozess

            // wenn die Adresssynchronisation noch nicht erfolgreich ausgeführt wurde --> ausführen, wenn angeklickt
            else if (!linkProcessFinished)
            {
                PrepareWork();
                DoLoadGeneralData();
                DoLinkAdresses();
            }
            else if (linkProcessFinished)
            {
                if (!syncProcessFinished)
                {
                    DoSyncAdresses();
                }
                else if (syncProcessFinished)
                {
                    if (!exportProcessFinished)
                    {
                        DoExportArticles();
                    }
                    else if (exportProcessFinished)
                    {
                        if (!deliveryImportProcessFinished)
                        {
                            DoDeliveryImport();
                        }
                        else if (deliveryImportProcessFinished)
                        {
                            if (!flightImportProcessFinished)
                            {
                                DoFlightImport();
                            }
                            else if (flightImportProcessFinished)
                            {
                                EndWork();
                            }
                        }
                    }
                }
            }
        }

        private void tsmiClearLink_Click(object sender, EventArgs e)
        {
            PrepareWork();
            Interaction.MsgBox("Steht momentan nicht zur Verfügung. Wenden Sie sich an den Support von FLS oder SMC Computer AG wenn Sie wirklich alle Adressen neu verknüpfen wollen.", Constants.vbCritical);

            // steht dem User momemtan nicht zur Verfügung, da eine Neuverknüpfung Fehlerpotential besitzt

            // If DialogResult.OK = MessageBox.Show("Sind Sie sicher, dass Sie die Verknüpfung der Adressen aus FLS und Proffix aufheben wollen? " + vbCrLf + vbCrLf +
            // "Wenn sie danach die nächste Adresssynchronisation ausführen, werden die Adressen neu verknüpft." + vbCrLf + vbCrLf +
            // "Danach werden alle Adressen in FLS als aktueller gelten und in Proffix aktualisiert!!!", "Verknüpfung wirklich löschen?", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) Then
            // If Not Linker.verknuepfungenAufheben() Then
            // Log("Fehler beim Aufheben der Verknüpfungen der Adresse")
            // Else
            // Log("Verknüpfungen erfolgreich aufgehoben")
            // End If
            // End If
            EndWork();
        }

        private void cbArtikel_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}