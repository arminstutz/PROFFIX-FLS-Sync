Imports System.Net.Http
Imports System.Net.Http.Headers
Imports Newtonsoft.Json.Linq
Imports FlsGliderSync.Exporter
Imports System.Reflection

''' <summary>
''' Den HTTP Request Typ
''' </summary>
Public Enum HttpRequestType
    POST
    PUT
    DELETE
End Enum

''' <summary>
''' Der Client des FLS Services
''' </summary>
Public Class FlsConnection

    Private Const WebMethodOverride As String = "X-HTTP-Method-Override"

    ''' <summary>
    ''' Das private HttpClient Objekt
    ''' </summary>
    Private Property pClient As HttpClient
    ''' <summary>
    ''' Boolean der angibt ob der Benutzer Authentifiziert ist
    ''' </summary>
    Private Property pIsAuthenticated As Boolean

    ''' <summary>
    ''' Boolean der angibt ob der Benutzer Authentifiziert ist und von ausserhalb zugreifbar ist
    ''' </summary>
    Public ReadOnly Property IsAuthenticated As Boolean
        Get
            Return pIsAuthenticated
        End Get
    End Property

    ''' <summary>
    ''' Initialisiert den FLS Client
    ''' </summary>
    Public Sub New()
        'Initialisiert den Http Client
        pClient = New HttpClient()
    End Sub


    '************************************************************nur für Testbetrieb********************************************************************************
    ' gibt die erfassten Flüge aus der Testdatenbank für die Verrechnung frei
    Public Sub testDeliveriesErstellen()
        Dim response As Threading.Tasks.Task(Of HttpResponseMessage) = pClient.GetAsync("https://test.glider-fls.ch/api/v1/flights/validate")
        response.Wait()

        Dim respons2 As Threading.Tasks.Task(Of HttpResponseMessage) = pClient.GetAsync("https://test.glider-fls.ch/api/v1/flights/lock/force")
        respons2.Wait()

        Dim response3 As Threading.Tasks.Task(Of HttpResponseMessage) = pClient.GetAsync("https://test.glider-fls.ch/api/v1/deliveries/create")
        response3.Wait()
    End Sub
    '*************************************************************************************************************************************************************

    ''' <summary>
    ''' Einloggen auf dem FLS Service
    ''' </summary>
    ''' <param name="userName">Der Benutzername</param>
    ''' <param name="password">Das Passwort</param>
    ''' <param name="tokenAddress">Den Pfad zur Webmehthode</param>
    ''' <returns>Ein Boolean der angibt ob die Authentifizierung erfolgreich war</returns>
    Public Async Function Login(userName As String, password As String, tokenAddress As String) As Threading.Tasks.Task(Of Boolean)

        ' Header leeren, damit bei Änderung der Logindaten nicht weiterhin Zugang
        pClient.DefaultRequestHeaders.Authorization = Nothing

        Try
            'Aufruf der Loggin Mehtode auf dem Service
            Dim token As String = Await GetToken(userName, password, tokenAddress),
                json = JObject.Parse(token),
            accessToken = json("access_token").ToString()

            'Den Authorization Header setzten
            pClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", accessToken)
            Return True

        Catch
            pIsAuthenticated = False
        End Try
        Return IsAuthenticated
    End Function

    'Public Function loadPerson(ByVal adressnr As Integer) As JObject
    '    Dim response As Threading.Tasks.Task(Of HttpResponseMessage) = pClient.GetAsync(My.Settings.ServiceAPIPersonsMemberNrMethod + adressnr.ToString)
    '    response.Wait()
    'End Function

    ''' <summary>
    ''' Laden des Token für die Authentifikation
    ''' </summary>
    ''' <param name="userName">Der Benutzername</param>
    ''' <param name="password">Das Passwort</param>
    ''' <param name="tokenAddress">Die Adresse der Webmethode</param>
    ''' <returns>Der geladene Token</returns>
    Private Function GetToken(userName As String, password As String, tokenAddress As String) As System.Threading.Tasks.Task(Of String)

        'Definiert den Parameter der Webmethode
        Dim pairs As New List(Of KeyValuePair(Of String, String))(
        {
            New KeyValuePair(Of String, String)("grant_type", "password"),
            New KeyValuePair(Of String, String)("username", userName),
            New KeyValuePair(Of String, String)("Password", password)
        })

        Dim content As New FormUrlEncodedContent(pairs)

        'Aufruf der Webmethode
        Using client As New HttpClient

            Dim response As HttpResponseMessage
            response = client.PostAsync(tokenAddress, content).Result

            Return response.Content.ReadAsStringAsync()
        End Using
    End Function

    ''' <summary>
    ''' Eine Webmehtode Asynchron laden
    ''' </summary>
    ''' <param name="path">Der Pfad der Methode</param>
    ''' <returns>Das Resultat des Aufrufs</returns>
    Public Async Function CallAsyncAsJArray(path As String) As Threading.Tasks.Task(Of JArray)
        'Ruft die Webmethode auf
        Dim response As Threading.Tasks.Task(Of HttpResponseMessage) = pClient.GetAsync(path)
        response.Wait()

        Dim result As String = Await response.Result.Content.ReadAsStringAsync()
        'Parst das resultat und gibt es zurück
        Return JArray.Parse(result)
    End Function

    ''' <summary>
    ''' Eine Webmehtode Asynchron laden
    ''' </summary>
    ''' <param name="path">Der Pfad der Methode</param>
    ''' <returns>Das Resultat des Aufrufs</returns>
    Public Async Function CallAsyncAsJObject(path As String) As Threading.Tasks.Task(Of JObject)
        'Ruft die Webmethode auf
        Dim response As Threading.Tasks.Task(Of HttpResponseMessage) = pClient.GetAsync(path)
        response.Wait()

        Dim result As String = Await response.Result.Content.ReadAsStringAsync()
        'Parst das resultat und gibt es zurück
        Return JObject.Parse(result)
    End Function


    ''' <summary>
    ''' Macht WebRequest um Daten in FLS zu schreiben (delete, update und create)
    ''' </summary>
    ''' <param name="id"></param>
    ''' <param name="obj"></param>
    ''' <param name="operation"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function SubmitChanges(ByVal id As String, ByVal obj As JObject, ByVal operation As SyncerCommitCommand) As String
        Try

            Dim newPersonId As String = ""
            Dim result As HttpResponseMessage = Nothing

            ' Header leeren
            If (pClient.DefaultRequestHeaders.Contains(WebMethodOverride)) Then
                pClient.DefaultRequestHeaders.Remove(WebMethodOverride)
            End If

            ' delete (in Proffix gelöschte Adressen)
            If operation = SyncerCommitCommand.Delete Then
                pClient.DefaultRequestHeaders.Add(WebMethodOverride, HttpRequestType.DELETE.ToString())
                result = pClient.PostAsync(My.Settings.ServiceAPIPersonMethod + id, New StringContent(obj.ToString(), Encoding.UTF8, "application/json")).Result

                ' update (Änderungen + neu erstellte AdressNr in Proffix --> MemberNumber)
            ElseIf operation = SyncerCommitCommand.Update Then
                pClient.DefaultRequestHeaders.Add(WebMethodOverride, HttpRequestType.PUT.ToString())
                result = pClient.PostAsync(My.Settings.ServiceAPIPersonMethod + id, New StringContent(obj.ToString(), Encoding.UTF8, "application/json")).Result

                ' create (in Proffix neu erstellte Adresse in FLS erstellen)
            ElseIf operation = SyncerCommitCommand.Create Then
                pClient.DefaultRequestHeaders.Add(WebMethodOverride, HttpRequestType.POST.ToString())
                result = pClient.PostAsync(My.Settings.ServiceAPIPersonMethod + id, New StringContent(obj.ToString(), Encoding.UTF8, "application/json")).Result
            End If

            '***********************************************************RESPONSE AUSWERTEN + JE NACH RESULTAT ANDERER RÜCKGABEWERT****************************************
            ' Response auswerten (gibt Fehler-bzw. Erfolgsstring zurück oder newPersonId wenn create)
            If result.StatusCode <> 200 Then '200 = erfolgreich --> alles andere = Fehler
                Return result.StatusCode.ToString + " " + result.ReasonPhrase
            Else

                ' neu erstellte PersonId bei create auslesen, um in Proffix schreiben zu können
                If operation = SyncerCommitCommand.Create Then
                    Dim jobj As JObject = JObject.Parse(result.Content.ReadAsStringAsync().Result)
                    newPersonId = jobj("PersonId").ToString
                    Return newPersonId
                    ' wenn update (NICHT create) --> Statuscode 200 zurückgeben
                Else
                    Return result.StatusCode.ToString
                End If
            End If
        Catch ex As Exception
            Return "Fehler in " + MethodBase.GetCurrentMethod.Name + " " + obj.ToString
        End Try
    End Function

    ' exportiert die Artikel aus Proffix in FLS
    Public Function ExportChanges(ByVal id As String, ByVal obj As JObject, ByVal operation As ExporterCommitCommand) As String

        Dim newarticleId As String = ""
        Dim result As HttpResponseMessage = Nothing

        Try

            ' Header leeren
            If (pClient.DefaultRequestHeaders.Contains(WebMethodOverride)) Then
                pClient.DefaultRequestHeaders.Remove(WebMethodOverride)
            End If

            ' update Artikel in FLS (auch InActive = 1 ist ein update, entspricht dem Löschen des Artikels)
            If operation = ExporterCommitCommand.Update Then
                pClient.DefaultRequestHeaders.Add(WebMethodOverride, HttpRequestType.PUT.ToString())
                result = pClient.PostAsync(My.Settings.ServiceAPIArticlesMethod + id, New StringContent(obj.ToString(), Encoding.UTF8, "application/json")).Result

                ' create Artikel in FLS
            ElseIf operation = ExporterCommitCommand.Create Then
                pClient.DefaultRequestHeaders.Add(WebMethodOverride, HttpRequestType.POST.ToString())

                '  MsgBox("vor")
                result = pClient.PostAsync(My.Settings.ServiceAPIArticlesMethod, New StringContent(obj.ToString(), Encoding.UTF8, "application/json")).Result
                ' MsgBox("nach")

            End If

            '***********************************************************RESPONSE AUSWERTEN + JE NACH RESULTAT ANDERER RÜCKGABEWERT****************************************
            ' Response auswerten (gibt Fehler bzw. ArticleId
            If result.StatusCode <> 200 Then '200 = erfolgreich --> alles andere = Fehler
                Return result.StatusCode.ToString + " " + result.ReasonPhrase
            Else
                ' ArticleId auslesen, um in Proffix schreiben/updaten zu können
                Dim jobj As JObject = JObject.Parse(result.Content.ReadAsStringAsync().Result)
                newarticleId = jobj("ArticleId").ToString
                Return newarticleId
            End If

        Catch ex As Exception
             Return "Fehler in " + MethodBase.GetCurrentMethod.Name + " " + ex.Message + " " + result.ReasonPhrase
        End Try
    End Function

    ' exportiert die Artikel aus Proffix in FLS
    Public Function submitFlag(ByVal path As String, ByVal obj As JObject) As String

        Dim result As HttpResponseMessage = Nothing

        ' Header leeren
        If (pClient.DefaultRequestHeaders.Contains(WebMethodOverride)) Then
            pClient.DefaultRequestHeaders.Remove(WebMethodOverride)
        End If

        pClient.DefaultRequestHeaders.Add(WebMethodOverride, HttpRequestType.POST.ToString())
        result = pClient.PostAsync(path, New StringContent(obj.ToString(), Encoding.UTF8, "application/json")).Result

        '***********************************************************RESPONSE AUSWERTEN + JE NACH RESULTAT ANDERER RÜCKGABEWERT****************************************
        ' Response auswerten (gibt Fehler-bzw. Erfolgsstring zurück oder newPersonId wenn create)
        If result.StatusCode <> 200 Then '200 = erfolgreich --> alles andere = Fehler
            Return result.StatusCode.ToString + " " + result.ReasonPhrase
        Else
            Return result.StatusCode.ToString
        End If
    End Function
End Class
