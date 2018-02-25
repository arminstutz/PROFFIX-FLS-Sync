Public Class FrmFlightImport

    Public Sub New()
        InitializeComponent()
        KeyPreview = True

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