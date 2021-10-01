Imports System.Drawing
Imports System.Reflection
Imports Newtonsoft.Json.Linq
Imports System.Text.RegularExpressions

''' <summary>
''' Ein Helper Modul (man kann direkt zugreifen, ohne Objekt erstellen zu müssen, entspricht "static" in Java) für allgemeine Aktionen
''' </summary>
Module GeneralHelper

    ' prüft, ob Programm bereits schonmal geöffnet wurde
    Public Sub startProgrammOnlyOnce()

        ' welche Prozesse sind am laufen?
        Dim processes As Process() = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName)
        Dim relevantProcesses As New List(Of Process)

        ' alle laufenden Prozesse durchgehen
        For Each process As Process In processes

            ' wenn der Prozess wie die Assembly heisst
            If process.ProcessName.ToLower = Assembly.GetExecutingAssembly().GetName.Name.ToLower Then

                ' in Array mit relevanten Prozessen einfügen
                relevantProcesses.Add(process)

                ' wenn 2 Mal das Programm gefunden worden ist
                If relevantProcesses.Count > 1 Then

                    ' ältere Ausführung stoppen
                    If processes(0).StartTime < processes(1).StartTime Then
                        processes(0).Kill()
                    Else
                        processes(1).Kill()
                    End If

                    ' wenn mehr als 2 Mal ausgeführt --> alle stoppen
                ElseIf relevantProcesses.Count > 2 Then
                    MessageBox.Show("Das Programm wird mehr als 2 ausgeführt. Alle Ausführungen werden beendet. Das Programm muss neu gestartet werden.")
                    For Each relprocess In relevantProcesses
                        relprocess.Kill()
                    Next
                End If
            End If
        Next
    End Sub

    ' archiviert LogFile nach X Tagen in Unterordner FLSGliderSyncLogs
    Public Function logArchivieren() As Boolean

        Try
            ' Dim logArchivierenNachTagen As String
            'Dim logArchivierenNachSeconds As Integer
            'Dim creationSinceSeconds As Double
            Dim appname As String = Assembly.GetExecutingAssembly().GetName.Name

            ' logArtivhierenNachTagen aus ini auslesen
            'logArchivierenNachTagen = readFromIni("logArchivierenNachTagen".ToLower)

            '' wurde eine brauchbare Zahl angegeben für logArchivierenNachTagen
            'If IsNumeric(logArchivierenNachTagen) Then
            '    ' wenn richtige Zahl --> fortfahren
            '    logArchivierenNachSeconds = CInt(logArchivierenNachTagen) * 24 * 60 * 60
            '    ' wenn für logArchivierenNachTagen in ini nichts angegeben wurde --> nicht archivieren
            'ElseIf logArchivierenNachTagen = "" Or logArchivierenNachTagen = " " Then
            '    Exit Function
            '    ' wenn andere Zeichen als Zahlen angegeben wurden --> Meldung
            'Else
            '    MessageBox.Show("Für logArchivierenNachTagen wurde in " + appname + ".ini ein falscher Wert angegeben: " + logArchivierenNachTagen + " Es muss sich um eine Zahl handeln", "Ungültiger Wert", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            'End If

            ' wenn noch kein Log-File existiert
            If Not System.IO.File.Exists(appname + ".log") Then
                ' hat nichts zu archivieren --> Sub beenden
                Return True
            End If

            '' vor wievielen Sekunden wurde das LogFile erstellt
            'creationSinceSeconds = (Now() - (New IO.FileInfo(appname + ".log")).CreationTime).TotalSeconds

            '' wenn Erstellungsdatum der Log-Datei länger her als in logArchivierenNachTagen definiert
            'If creationSinceSeconds > logArchivierenNachSeconds Then

            Try
                ' wenn in Prog der Ordner FLSGliderSyncLogs noch nicht existiert --> Unterordner FLSGliderSyncLogs für alte Logs erstellen
                If Not System.IO.Directory.Exists(appname + "Logs") Then
                    System.IO.Directory.CreateDirectory(appname + "Logs")
                End If
                '   dann LogFile in Archivordner FLSGliderLogs verschieben
                File.Move(appname + ".log", appname + "Logs\" + appname + "_" & Now.Day.ToString("00") & Now.Month.ToString("00") & Now.Year & "_" & Now.Hour & Now.Minute & Now.Second & ".log")

                ' neues File erstellen
                ' File.Create(appname + ".log")

                Return True
            Catch exce As Exception
                MessageBox.Show("Das Log-File konnte nicht archiviert werden.", "Log archivieren fehlgeschlagen", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                Return False
            End Try
            ' End If
            'Return True

        Catch ex As Exception
            MessageBox.Show("Fehler beim Überprüfen der Log-Datei", "Log archivieren fehlgeschlagen", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Return False
        End Try
    End Function

    ''' <summary>
    ''' liest Werte aus Ini
    ''' </summary>
    ''' <param name="key">welcher Wert soll ausgelesen werden</param>
    ''' <param name="mustexist">wenn true (default) --> Fehlermeldung, wenn key in ini nicht vorhanden, wenn false --> wird ignoriert, wenn key in ini nicht vorhanden</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function readFromIni(ByVal key As String, Optional mustexist As Boolean = True) As String
        Dim reader As StreamReader
        Dim strIniUserDef As String = String.Empty
        Dim appname As String = Assembly.GetExecutingAssembly().GetName.Name
        Try
            strIniUserDef = CStr(Registry.GetValue("HKEY_CURRENT_USER\Software\PROFFIX.NET\smcAnwendungen\" + appname, "iniUserDef", ""))

            reader = New StreamReader(strIniUserDef)
        Catch ex As Exception
            MsgBox("Datei " + striniuserdef + " wurde nicht gefunden")
            Return String.Empty
        End Try

        Dim sLine As String = ""
        Dim iniLine As String = ""

        ' Zeile zu key aus Ini auslesen
        Do
            sLine = reader.ReadLine()
            If Not sLine Is Nothing Then
                If sLine.ToLower.Contains(key.ToLower + "=") Then
                    iniLine = sLine
                    Exit Do
                End If
            End If
        Loop Until sLine Is Nothing
        reader.Close()

        ' wenn keine Zeile mit key gefunden wurde
        If iniLine = "" Then
            If mustexist Then
                MessageBox.Show(key + " in " + appname + ".ini nicht definiert")
                Throw New Exception(key + " in " + appname + ".ini nicht vorhanden")
            Else
                Return String.Empty
            End If
        End If

        ' Value zurückgeben
        Return iniLine.Substring(iniLine.IndexOf("=") + 1)
    End Function


    Public Function logAusfuehrlichSchreiben() As Boolean
        Dim value As String = readFromIni("LogAusfuehrlich")

        If value = "1" Then
            Return True
        ElseIf value = "0" Then 'Or value = ""
            Return False
        Else
            MessageBox.Show("Für den Wert LogAusfuehrlich muss in der Konfigurationsdatei 0 oder 1 angegeben werden") 'nichts,
            Return False
        End If

    End Function


    ' gibt Datum zurück, ab dem die geänderten/gelöschten Adressen geholt werden sollen
    Public Function GetSinceDate(ByVal lastSync As DateTime) As DateTime
        If lastSync = Nothing Then
            Return DateTime.MinValue
        Else
            Return lastSync
        End If
    End Function

    ''' <summary>
    ''' Gibt das neuste der mitgegebenen DateTime Objekte zurück
    ''' </summary>
    ''' <param name="dates">Die DateTime Objekte die sortiert werden sollen</param>
    ''' <returns>Das neuste DateTime Objekt</returns>
    Public Function GetNewestDate(ByVal dates As DateTime()) As DateTime
        Dim d As DateTime = DateTime.MinValue
        'Für jedes DateTime Objekt prüfen ob es neuer als das bis jetzt neuste ist
        Array.ForEach(dates, New Action(Of Date)(Sub(dt)
                                                     d = If(dt > d, dt, d)
                                                 End Sub))
        Return d
    End Function

    ''' <summary>
    ''' Rotiert das Bild
    ''' </summary>
    ''' <param name="image">Das Bild das rotiert werden soll</param>
    ''' <param name="offset">Punkt um den rotiert wird</param>
    ''' <param name="angle">Der Winkel der definiert wie viel rotiert wird</param>
    ''' <returns>Das neugezeichnete Bild</returns>
    Public Function RotateImage(ByVal image As Image, ByVal offset As PointF, ByVal angle As Single) As Bitmap
        'Wirf einen Fehler wenn das Bild leer ist
        If image Is Nothing Then
            Throw New ArgumentNullException("image")
        End If

        'Eine neue Bitmap erstellen
        Dim rotatedBmp As New Bitmap(image.Width, image.Height)
        rotatedBmp.SetResolution(image.HorizontalResolution, image.VerticalResolution)

        'Die Graphik laden
        Dim g As Graphics = Graphics.FromImage(rotatedBmp)

        'Die Rotation durchführen
        g.TranslateTransform(offset.X, offset.Y)
        g.RotateTransform(angle)
        g.TranslateTransform(-offset.X, -offset.Y)
        g.DrawImage(image, New PointF(0, 0))

        Return rotatedBmp
    End Function

    ' prüft auf email-pattern
    Public Function isEmail(ByVal email As String) As Boolean
        Dim pattern_email As Regex = New Regex("^.+@.+\.[^\s]+$")
        Return pattern_email.IsMatch(email)
    End Function

    ' prüft auf guid-pattern
    Public Function isGUID(ByVal guid As String) As Boolean
        Dim pattern_GUID As Regex = New Regex("^(\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1}$", RegexOptions.Compiled)
        Return pattern_GUID.IsMatch(guid) 
    End Function
















    Public Function fktumgetestetzuwerden(ByVal int As Integer) As Integer
        Return int + 5
    End Function

End Module
