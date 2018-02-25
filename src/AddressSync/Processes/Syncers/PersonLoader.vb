Imports pxBook
Imports SMC.Lib
Imports System.Reflection
Imports Newtonsoft.Json.Linq
Imports System.Windows.Forms.Control


Public Class PersonLoader
    Public Property Log As Action(Of String)    ' Aktion, die ausgeführt wird, wenn in Logfeld geschrieben wird

    Private _serviceClient As FlsConnection

    Public Sub New(ByVal _serviceclient As FlsConnection, ByVal Log As System.Action(Of String))
        Me._serviceClient = _serviceclient
        Me.Log = Log
    End Sub

    Public Function datenLaden(ByRef FLSharddeletedpersons As List(Of JObject),
                               ByRef FLSPersons As List(Of JObject),
                               ByRef PXAdressen As List(Of pxKommunikation.pxAdressen)) As Boolean

        Try
            logComplete("Adressdaten aus FLS werden geladen", LogLevel.Info)
            ' alle in FLS seit sinceDate gelöschten Personen laden
            FLSharddeletedpersons = loadDeletedPersons(DateTime.MinValue)
            ' Alle FLS Adressen holen egal ob IsActive true/false oder nicht
            FLSPersons = loadFLSPersons(DateTime.MinValue)

            logComplete("Adressdaten aus Proffix werden geladen", LogLevel.Info)
            'Alle  PROFFIX Adressen holen egal ob geloescht = 0/1
            PXAdressen = loadPXAdressen()

            ' prüfen, ob Laden geklappt hat
            If FLSharddeletedpersons Is Nothing Or FLSPersons Is Nothing Or PXAdressen Is Nothing Then
                logComplete("", LogLevel.Exception)
                Return False
            End If

            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function


    ' lädt die in FLS gelöschten Personen
    Private Function loadDeletedPersons(ByVal sinceDate As DateTime) As List(Of JObject)
        Try

            Dim deletedPersons As New List(Of JObject)
            Dim personDeletedResult As Threading.Tasks.Task(Of JArray)

            personDeletedResult = _serviceClient.CallAsyncAsJArray(My.Settings.ServiceAPIDeletedPersonFulldetailsMethod + sinceDate.ToString("yyyy-MM-dd"))
            personDeletedResult.Wait()

            For Each person As JObject In personDeletedResult.Result.Children
                deletedPersons.Add(person)
            Next
            Return deletedPersons

        Catch ex As Exception
            logComplete("Fehler beim Laden der in FLS gelöschten Adressen. " + ex.Message, LogLevel.Exception)
            Return Nothing
        End Try
    End Function

    ' gibt alle FLS Persons zurück, (egal ob IsActive true oder false)
    Private Function loadFLSPersons(ByVal sinceDate As DateTime) As List(Of JObject)
        Dim personResult As Threading.Tasks.Task(Of JArray)
        Dim FLSPersons As New List(Of JObject)
        Try
            ' alle Adressen aus FLS laden (egal ob IsActive = true oder nicht)
            personResult = _serviceClient.CallAsyncAsJArray(My.Settings.ServiceAPIModifiedPersonFullDetailsMethod + sinceDate.ToString("yyyy-MM-dd"))
            personResult.Wait()

            For Each person As JObject In personResult.Result.Children()
                FLSPersons.Add(person)
            Next

            Return FLSPersons
        Catch ex As Exception
            logComplete("Fehler in " + MethodBase.GetCurrentMethod.Name + " " + ex.Message, LogLevel.Exception)
            Return Nothing
        End Try
    End Function

    ' alle Adressen aus PX laden, egal ob geloeshct = 0 oder 1
    Private Function loadPXAdressen() As List(Of pxKommunikation.pxAdressen)
        Dim adressList As New List(Of pxKommunikation.pxAdressen)
        Dim ungeloeschteAdressen As pxKommunikation.pxAdressen() = New pxKommunikation.pxAdressen() {}
        Dim geloeschteAdressen As pxKommunikation.pxAdressen() = New pxKommunikation.pxAdressen() {}
        Dim fehler As String = String.Empty

        Try
            ' alle ungelöschten Adressen laden
            If Not Proffix.GoBook.GetAdresse(pxKommunikation.pxAdressSuchTyp.Alle, "%", ungeloeschteAdressen, fehler) Then
                If Not fehler.Contains("Keine Adressen gefunden!") Then
                    logComplete("Fehler in " + MethodBase.GetCurrentMethod.Name + " Laden der ungelöschten Adressen aus PX " + fehler, LogLevel.Exception)
                    Return Nothing
                End If
            End If

            For Each address As pxKommunikation.pxAdressen In ungeloeschteAdressen
                If address.AdressNr <> 0 Then
                    adressList.Add(address)
                End If
            Next

            ' alle gelöschten Adressen laden
            If Not Proffix.GoBook.GetAdresse(pxKommunikation.pxAdressSuchTyp.Alle, "%", geloeschteAdressen, fehler, pxKommunikation.pxGeloeschte.Geloeschte) Then
                If Not fehler.Contains("Keine Adressen gefunden!") Then
                    logComplete("Fehler in " + MethodBase.GetCurrentMethod.Name + " Laden der gelöschten Adressen aus PX " + fehler, LogLevel.Exception)
                    Return Nothing
                End If
            End If

            For Each address As pxKommunikation.pxAdressen In geloeschteAdressen
                If address.AdressNr <> 0 Then
                    adressList.Add(address)
                End If
            Next
            Return adressList
        Catch ex As Exception
            logComplete("Fehler in " + MethodBase.GetCurrentMethod.Name + " " + ex.Message, LogLevel.Exception)
            Return Nothing
        End Try
    End Function


    ' schreibt in Log und in Logger (File)
    Private Sub logComplete(ByVal logString As String, ByVal loglevel As LogLevel, Optional ByVal zusatzloggerString As String = "")
        If Log IsNot Nothing Then Log.Invoke(If(loglevel <> loglevel.Info, vbTab, "") + logString)
        Logger.GetInstance.Log(loglevel, logString + " " + zusatzloggerString)
    End Sub

End Class