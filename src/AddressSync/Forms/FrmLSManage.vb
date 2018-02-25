Imports Newtonsoft.Json.Linq

Public Class FrmLSManage
    Private _client As FlsConnection



    Public Sub New(ByVal client As FlsConnection)
        InitializeComponent()

        _client = client
    End Sub

    Private Sub btnLSLoad_Click(sender As Object, e As EventArgs) Handles btnLSLoad.Click
       
        
       


        ' Dim deliveries As Threading.Tasks.Task(Of JArray) ' Lieferscheindaten aus FLS (zur Verrechnung freigegeben)
        ' lädt alle zu importierenden Lieferscheine
        '   deliveries = _client.CallAsyncAsJArray(My.Settings.ServiceAPIDeliveriesNotProcessedMethod)
        '  deliveries.Wait()


        ''Iteration durch die Flüge, die zu verrechnen sind
        'For Each delivery As JObject In deliveries.Result.Children()

        '    ' prüfen, ob der Delivery die nötigen Daten enthält. Wenn nicht, kann dieser Delivery nicht importiert werden
        '    If Not checkForCompleteDelivery(delivery) Then

        '        ' wenn nötige Daten fehlen --> nachfragen, ob überspringen + mit nächstem weiterfahren (OK) oder Deliveryimport abbrechen (Cancel)
        '        Dim dialogres As DialogResult = MessageBox.Show("Der Lieferschein mit der DeliveryId " + GetValOrDef(delivery, "DeliveryId") + " kann nicht importiert werden, da benötigte Daten fehlen." + vbCrLf + vbCrLf +
        '                                          "Soll der Lieferschein verworfen werden?", "Lieferschein nicht importierbar", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2)
        '        ' OK --> diesen Delivery überspringen --> mit nächstem weiterfahren
        '        If dialogres = DialogResult.Yes Then


        '            'in FLS schreiben, dass diese Delivery verrechnet wurde
        '            If Not flagAsDelivered(delivery, dokNr) Then
        '            End If
        '        End If
        '    End If
        'Next
    End Sub

End Class