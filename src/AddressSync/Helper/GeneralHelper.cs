using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Microsoft.Win32;

namespace FlsGliderSync
{

    /// <summary>
/// Ein Helper Modul (man kann direkt zugreifen, ohne Objekt erstellen zu müssen, entspricht "static" in Java) für allgemeine Aktionen
/// </summary>
    static class GeneralHelper
    {

        // prüft, ob Programm bereits schonmal geöffnet wurde
        public static void startProgrammOnlyOnce()
        {

            // welche Prozesse sind am laufen?
            var processes = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
            var relevantProcesses = new List<Process>();

            // alle laufenden Prozesse durchgehen
            foreach (Process process in processes)
            {

                // wenn der Prozess wie die Assembly heisst
                if ((process.ProcessName.ToLower() ?? "") == (Assembly.GetExecutingAssembly().GetName().Name.ToLower() ?? ""))
                {

                    // in Array mit relevanten Prozessen einfügen
                    relevantProcesses.Add(process);

                    // wenn 2 Mal das Programm gefunden worden ist
                    if (relevantProcesses.Count > 1)
                    {

                        // ältere Ausführung stoppen
                        if (processes[0].StartTime < processes[1].StartTime)
                        {
                            processes[0].Kill();
                        }
                        else
                        {
                            processes[1].Kill();
                        }
                    }

                    // wenn mehr als 2 Mal ausgeführt --> alle stoppen
                    else if (relevantProcesses.Count > 2)
                    {
                        MessageBox.Show("Das Programm wird mehr als 2 ausgeführt. Alle Ausführungen werden beendet. Das Programm muss neu gestartet werden.");
                        foreach (var relprocess in relevantProcesses)
                            relprocess.Kill();
                    }
                }
            }
        }

        // archiviert LogFile nach X Tagen in Unterordner FLSGliderSyncLogs
        public static bool logArchivieren()
        {
            try
            {
                // Dim logArchivierenNachTagen As String
                // Dim logArchivierenNachSeconds As Integer
                // Dim creationSinceSeconds As Double
                string appname = Assembly.GetExecutingAssembly().GetName().Name;

                // logArtivhierenNachTagen aus ini auslesen
                // logArchivierenNachTagen = readFromIni("logArchivierenNachTagen".ToLower)

                // ' wurde eine brauchbare Zahl angegeben für logArchivierenNachTagen
                // If IsNumeric(logArchivierenNachTagen) Then
                // ' wenn richtige Zahl --> fortfahren
                // logArchivierenNachSeconds = CInt(logArchivierenNachTagen) * 24 * 60 * 60
                // ' wenn für logArchivierenNachTagen in ini nichts angegeben wurde --> nicht archivieren
                // ElseIf logArchivierenNachTagen = "" Or logArchivierenNachTagen = " " Then
                // Exit Function
                // ' wenn andere Zeichen als Zahlen angegeben wurden --> Meldung
                // Else
                // MessageBox.Show("Für logArchivierenNachTagen wurde in " + appname + ".ini ein falscher Wert angegeben: " + logArchivierenNachTagen + " Es muss sich um eine Zahl handeln", "Ungültiger Wert", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                // End If

                // wenn noch kein Log-File existiert
                if (!File.Exists(appname + ".log"))
                {
                    // hat nichts zu archivieren --> Sub beenden
                    return true;
                }

                // ' vor wievielen Sekunden wurde das LogFile erstellt
                // creationSinceSeconds = (Now() - (New IO.FileInfo(appname + ".log")).CreationTime).TotalSeconds

                // ' wenn Erstellungsdatum der Log-Datei länger her als in logArchivierenNachTagen definiert
                // If creationSinceSeconds > logArchivierenNachSeconds Then

                try
                {
                    // wenn in Prog der Ordner FLSGliderSyncLogs noch nicht existiert --> Unterordner FLSGliderSyncLogs für alte Logs erstellen
                    if (!Directory.Exists(appname + "Logs"))
                    {
                        Directory.CreateDirectory(appname + "Logs");
                    }
                    // dann LogFile in Archivordner FLSGliderLogs verschieben
                    File.Move(appname + ".log", appname + @"Logs\" + appname + "_" + DateAndTime.Now.Day.ToString("00") + DateAndTime.Now.Month.ToString("00") + DateAndTime.Now.Year + "_" + DateAndTime.Now.Hour + DateAndTime.Now.Minute + DateAndTime.Now.Second + ".log");

                    // neues File erstellen
                    // File.Create(appname + ".log")

                    return true;
                }
                catch (Exception exce)
                {
                    MessageBox.Show("Das Log-File konnte nicht archiviert werden.", "Log archivieren fehlgeschlagen", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return false;
                }
            }
            // End If
            // Return True

            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Überprüfen der Log-Datei", "Log archivieren fehlgeschlagen", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }
        }

        /// <summary>
    /// liest Werte aus Ini
    /// </summary>
    /// <param name="key">welcher Wert soll ausgelesen werden</param>
    /// <param name="mustexist">wenn true (default) --> Fehlermeldung, wenn key in ini nicht vorhanden, wenn false --> wird ignoriert, wenn key in ini nicht vorhanden</param>
    /// <returns></returns>
    /// <remarks></remarks>
        public static string readFromIni(string key, bool mustexist = true)
        {
            StreamReader reader;
            string strIniUserDef = string.Empty;
            string appname = Assembly.GetExecutingAssembly().GetName().Name;
            try
            {
                strIniUserDef = Conversions.ToString(Registry.GetValue(@"HKEY_CURRENT_USER\Software\PROFFIX.NET\smcAnwendungen\" + appname, "iniUserDef", ""));
                reader = new StreamReader(strIniUserDef);
            }
            catch (Exception ex)
            {
                Interaction.MsgBox("Datei " + strIniUserDef + " wurde nicht gefunden");
                return string.Empty;
            }

            string sLine = "";
            string iniLine = "";

            // Zeile zu key aus Ini auslesen
            do
            {
                sLine = reader.ReadLine();
                if (sLine is object)
                {
                    if (sLine.ToLower().Contains(key.ToLower() + "="))
                    {
                        iniLine = sLine;
                        break;
                    }
                }
            }
            while (!(sLine is null));
            reader.Close();

            // wenn keine Zeile mit key gefunden wurde
            if (string.IsNullOrEmpty(iniLine))
            {
                if (mustexist)
                {
                    MessageBox.Show(key + " in " + appname + ".ini nicht definiert");
                    throw new Exception(key + " in " + appname + ".ini nicht vorhanden");
                }
                else
                {
                    return string.Empty;
                }
            }

            // Value zurückgeben
            return iniLine.Substring(iniLine.IndexOf("=") + 1);
        }

        public static bool logAusfuehrlichSchreiben()
        {
            string value = readFromIni("LogAusfuehrlich");
            if (value == "1")
            {
                return true;
            }
            else if (value == "0") // Or value = ""
            {
                return false;
            }
            else
            {
                MessageBox.Show("Für den Wert LogAusfuehrlich muss in der Konfigurationsdatei 0 oder 1 angegeben werden"); // nichts,
                return false;
            }
        }


        // gibt Datum zurück, ab dem die geänderten/gelöschten Adressen geholt werden sollen
        public static DateTime GetSinceDate(DateTime lastSync)
        {
            if (lastSync == default)
            {
                return DateTime.MinValue;
            }
            else
            {
                return lastSync;
            }
        }

        /// <summary>
    /// Gibt das neuste der mitgegebenen DateTime Objekte zurück
    /// </summary>
    /// <param name="dates">Die DateTime Objekte die sortiert werden sollen</param>
    /// <returns>Das neuste DateTime Objekt</returns>
        public static DateTime GetNewestDate(DateTime[] dates)
        {
            var d = DateTime.MinValue;
            // Für jedes DateTime Objekt prüfen ob es neuer als das bis jetzt neuste ist
            Array.ForEach(dates, new Action<DateTime>(dt => d = dt > d ? dt : d));
            return d;
        }

        /// <summary>
    /// Rotiert das Bild
    /// </summary>
    /// <param name="image">Das Bild das rotiert werden soll</param>
    /// <param name="offset">Punkt um den rotiert wird</param>
    /// <param name="angle">Der Winkel der definiert wie viel rotiert wird</param>
    /// <returns>Das neugezeichnete Bild</returns>
        public static Bitmap RotateImage(Image image, PointF offset, float angle)
        {
            // Wirf einen Fehler wenn das Bild leer ist
            if (image is null)
            {
                throw new ArgumentNullException("image");
            }

            // Eine neue Bitmap erstellen
            var rotatedBmp = new Bitmap(image.Width, image.Height);
            rotatedBmp.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            // Die Graphik laden
            var g = Graphics.FromImage(rotatedBmp);

            // Die Rotation durchführen
            g.TranslateTransform(offset.X, offset.Y);
            g.RotateTransform(angle);
            g.TranslateTransform(-offset.X, -offset.Y);
            g.DrawImage(image, new PointF(0f, 0f));
            return rotatedBmp;
        }

        // prüft auf email-pattern
        public static bool isEmail(string email)
        {
            var pattern_email = new Regex(@"^.+@.+\.[^\s]+$");
            return pattern_email.IsMatch(email);
        }

        // prüft auf guid-pattern
        public static bool isGUID(string guid)
        {
            var pattern_GUID = new Regex(@"^(\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1}$", RegexOptions.Compiled);
            return pattern_GUID.IsMatch(guid);
        }

        public static int fktumgetestetzuwerden(int @int)
        {
            return @int + 5;
        }
    }
}