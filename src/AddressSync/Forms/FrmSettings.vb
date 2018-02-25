Imports SMC
Imports System.Windows.Forms.VisualStyles

''' <summary>
''' Der Dialog der zum bearbeiten der Einstellungen verwendet wird
''' </summary>
Public Class FrmSettings

    ' beim Start des Programms werden wird FrmSettings geladen aber nicht angezeigt 
    '--> Werte für DateTimePicker werden übernommen von anfangs geladenen Daten bzw. die Änderung, die das letzte Mal gemacht wurde, als das Frm angezeigt wurde
    Public Sub New()
        InitializeComponent()
        KeyPreview = True

        ' Logindaten anzeigen
        tbZugang.Text = My.Settings.ServiceAPITokenMethod.ToString.Substring(0, My.Settings.ServiceAPITokenMethod.Length - 5)

        '' Default Daten setzen, aus welchem Bereich die Flugdaten importiert werden sollen
        'If Now.Month > 2 Then
        '    dtpFrom.Value = New DateTime(Now.Year, 1, 1)
        'Else
        '    dtpFrom.Value = New DateTime(Now.Year - 1, 1, 1)
        'End If
        'dtpTo.Value = DateSerial(Now.Year, Now.Month, 0)

    End Sub

    ' Wenn Enter bzw. ESC geklickt wird
    Private Sub FrmSettings_KeyDown(sender As Object, e As KeyEventArgs) Handles MyBase.KeyDown
        If e.KeyData = Keys.Escape Then
            DialogResult = DialogResult.OK
        ElseIf e.KeyData = Keys.Enter Then
            DialogResult = DialogResult.Cancel
        End If
    End Sub

End Class
