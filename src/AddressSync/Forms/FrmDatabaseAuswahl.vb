Imports Newtonsoft.Json.Linq
Imports SMC.Lib
Imports System.Reflection

Public Class FrmDBAuswahl

    ''' <summary>
    ''' Der Dialog wird geladen und eine Auswahl von Mandanten angezeigt
    ''' </summary>
    ''' <param name="sender">Die Form</param>
    ''' <param name="e">Informationen zum "Load" Event</param>
    Private Sub FrmMain_Load(ByVal sender As Object, ByVal e As EventArgs) Handles MyBase.Load
        Try
            ' abfangen, ob FLSGliderSync bereits geöffnet ist --> nicht nochmals öffnen
            GeneralHelper.startProgrammOnlyOnce()

            ' LogDatei nach X Tagen in Unterordner verschieben (X = in ini definiert unter LogLoeschenNachTagen
            ' GeneralHelper.logArchivieren()
            ' auskommentiert, da es immer Fehlermeldung gibt, dass File bereits verwendet wird

            ' prüfen, ob LogFile existiert
            'Dim path As String = Application.StartupPath() + "\" + Assembly.GetExecutingAssembly().GetName.Name + ".log"
            'If Not File.Exists(path) Then
            '    File.Create(path)
            '    Logger.GetInstance.Log("Log File erstellt")
            'End If


            'Try
            '    Dim a As JObject = JObject.Parse(bspDelivery1Menge0)
            'Catch ex As Exception
            '    MsgBox(ex.Message)
            'End Try

            'Lesen der Konfigurationsdateien
            If Not Proffix.Settings.IsLoaded Then
                Proffix.Settings.Load()
            End If
            'Datenbanken in das Dropdown füllen
            cbMain.DataSource = Proffix.Settings.ProffixDataBases.Values.ToArray()

            Controls.Add(cbMain)

            cbMain.SelectedItem = cbMain.Items(0)

            'Klick simulieren da in dieser Applikation keine Auswahl getroffen werden darf
            If cbMain.Items.Count = 1 Then
                Application.DoEvents()

                btnSelect_Click(Nothing, Nothing)
            End If

        Catch exce As Exception
            MessageBox.Show("Fehler beim Laden des Programms.")
            Logger.GetInstance.Log(LogLevel.Exception, exce.Message)
            'Falls ein Fehler auftritt wird die Konfigurationshilfe angezeigt
            handleExeception(exce)
            End
        End Try
    End Sub

    ''' <summary>
    ''' Die Datenbank wurde ausgewählt und "OK" geklickt
    ''' </summary>
    Private Sub btnSelect_Click(ByVal sender As Object, ByVal e As EventArgs) Handles Button1.Click
        Try

            'Die Verbindung zu PROFFIX wird aufgebaut
            Proffix.Settings.DefaultDataBase = cbMain.SelectedItem.ToString()
            Proffix.LoadConnection = True

            'Der Dialog wird versteckt 
            Hide()

            Try
                Dim form As FrmMain = New FrmMain()
                form.ShowDialog()
            Catch ex As Exception
                'Falls ein Fehler auftrit wird der Benutzer informiert und details in die Logdatei geschrieben
                MessageBox.Show("Beim Starten des Hauptfensters ist ein Fehler aufgetreten. Kontaktieren Sie den Support.")
                Logger.GetInstance().Log(LogLevel.Critical, ex)
            End Try
            Close()
        Catch exce As Exception
            handleExeception(exce)
        End Try
    End Sub

    ''' <summary>
    ''' Wenn ein Fehler beim Aufbau der Verbindung auftrit wird die Konfigurationshilfe angezeigt
    ''' </summary>
    ''' <param name="exce">Die Exception die geloggt wird</param>
    Private Sub handleExeception(exce As Exception)
        Logger.GetInstance().Log(LogLevel.Critical, exce)
        Hide()
        MessageBox.Show("Beim Starten des Programms ist ein Fehler aufgetreten. Überprüfen sie ihre Konfigurationen.")
        ' Dim frmHelp As New FrmHelp()
        ' frmHelp.ShowDialog()
        Close()
    End Sub

End Class