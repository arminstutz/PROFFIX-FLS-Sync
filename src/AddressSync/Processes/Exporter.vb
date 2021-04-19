
Imports Newtonsoft.Json.Linq
Imports pxBook
Imports SMC.Lib

Imports System.Text.RegularExpressions
Imports System.Net.Http
Imports System.Threading
Imports System.Reflection
Imports System.ServiceModel

Public Class Exporter

    Public Enum ExporterCommitCommand
        Update
        Create
    End Enum

    ' Klassenvariablen
    Private flsConn As FlsConnection
    Private MyConn As ProffixConnection
    Private pxHelper As ProffixHelper
    Private articleMapper As ArticleMapper

    ' Actions
    Public DoProgress As Action
    Public Log As Action(Of String)

    ' Regex-Pattern für GUID
    Private pattern_GUID As Regex = New Regex("^(\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1}$", RegexOptions.Compiled)

    Private _lastExport As DateTime = Nothing
    Public Property LastExport As DateTime
        Get
            Return _lastExport
        End Get
        Set(ByVal value As DateTime)
            _lastExport = value
        End Set
    End Property

    Private _progress As Integer
    Public Property Progress As Integer
        Get
            Return _progress
        End Get
        Private Set(ByVal value As Integer)
            _progress = value
        End Set
    End Property

    Private _count As Integer
    Public Property Count As Integer
        Get
            Return _count
        End Get
        Private Set(ByVal value As Integer)
            _count = value
        End Set
    End Property

    Public Sub New(ByVal lastExport As DateTime, ByRef serviceClient As FlsConnection, ByRef pxHelper As ProffixHelper, ByRef Myconn As ProffixConnection)
        Me.LastExport = lastExport
        Me.flsConn = serviceClient
        Me.pxHelper = pxHelper
        Me.MyConn = Myconn
        articleMapper = New ArticleMapper
    End Sub


    Public Function Export() As Boolean
        Dim articleResult As Threading.Tasks.Task(Of JArray)
        Dim fehler As String = ""
        Dim response_FLS As String = ""
        Dim successful As Boolean = True
        Dim articleList = New List(Of pxBook.pxKommunikation.pxArtikel)
        Dim existsInFLS As Boolean = False
        Dim response As String = String.Empty
        Dim update_successful As Boolean = True
        Dim create_successful As Boolean = True

        Try
            logComplete("Artikelexport gestartet", LogLevel.Info)
            Progress = 0

            ' **************************************************************alle FLS-Artikel: Negativbestand = 1 setzen***********************************************************
            pxHelper.SetNegativBestand(response)
            If Not String.IsNullOrEmpty(response) Then
                logComplete(response, LogLevel.Exception)
            End If

            '******************************************************************************Artikel holen*********************************************************************************
            ' alle Artikel aus FLS holen
            articleResult = flsConn.CallAsyncAsJArray(My.Settings.ServiceAPIArticlesMethod)
            articleResult.Wait()

            ' alle Artikel aus Proffix holen, die erstellt/geaendert am > lastExport haben und in Gruppe FLS sind
            articleList = pxArtikelLaden()

            Count = articleList.Count

            '**********************************************************************Artikel vergleichen*********************************************************
            ' Artikel in Proffix + FLS vergleichen (ArticleNumber)
            For Each proffixArticle In articleList

                ' Defaultwert = false
                existsInFLS = False

                ' Alle Artikel aus FLS durchgehen
                For Each existingFLSarticle As JObject In articleResult.Result.Children

                    '************************************************************UPDATE IN FLS*******************************************************************
                    ' --> wenn in FLS vorhanden --> update PUT
                    If proffixArticle.ArtikelNr = existingFLSarticle("ArticleNumber").ToString Then
                        existsInFLS = True

                        ' updated den Artikel in FLS (Flag existsInFLS
                        If Not updateInFLS(proffixArticle, existingFLSarticle) Then

                            ' geklappt soll falsch sein (und bleiben), sobald 1 update/create nicht geklappt hat --> wenn updateInFLS true zurückgibt --> geklappt nicht verändern, da sonst vorheriger Fehler ignoriert wird
                            successful = False
                        End If
                    End If
                Next

                '*******************************************************************CREATE IN FLS******************************************************************
                '  wenn in FLS nicht vorhanden 
                If Not existsInFLS Then

                    ' ... und  in Proffix nicht bereits schon wieder gelöscht wurde
                    If proffixArticle.Geloescht = 0 Then

                        '... dann: create Article in FLS + neue Id in Proffix schreiben
                        If Not createInFLS(proffixArticle) Then
                            ' geklappt soll falsch sein, sobald 1 update/create nicht geklappt hat --> wenn updateInFLS true zurückgibt --> geklappt nicht verändern, da sonst vorheriger Fehler ignoriert wird
                            successful = False
                        End If
                    End If
                End If
                Progress += 1
                InvokeDoProgress()
            Next

            '**************************************************************LastSync updaten*****************************************************************************
            ' wenn bis herhin alles geklappt --> geklappt immer noch true
            If successful Then
                logComplete("Artikelexport erfolgreich beendet", LogLevel.Info)
                LastExport = DateTime.Now
            Else
                logComplete("Beim Artikelexport ist mindestens 1 Fehler aufgetreten. Deshalb wird das Datum des letzten Exports nicht angepasst.", LogLevel.Exception)
                Logger.GetInstance.Log(LogLevel.Exception, "Artikelexport beendet. Mindestens 1 Fehler bei Artikelexport")
            End If

            logComplete("", LogLevel.Info)
            Progress = Count
            InvokeDoProgress()

            Return successful
        Catch faultExce As FaultException
            Logger.GetInstance().Log(LogLevel.Exception, faultExce.Message)
            Throw faultExce
            'End If
        Catch exce As Exception
            Logger.GetInstance().Log(LogLevel.Exception, exce)
            Throw
        End Try
    End Function

    Private Function pxArtikelLaden() As List(Of pxKommunikation.pxArtikel)
        Dim sql As String = String.Empty
        Dim rs As New ADODB.Recordset
        Dim articleList As New List(Of pxKommunikation.pxArtikel)
        Dim fehler As String = String.Empty


        sql = "Select artikelNrLAG, bezeichnung1, bezeichnung2, bezeichnung3, geloescht from lag_artikel " + _
             "where gruppeLAG = 'FLS' and (erstelltam > '" + LastExport.ToString(pxHelper.dateformat + " HH:mm:ss") + "' or geaendertam > '" + LastExport.ToString(pxHelper.dateformat + " HH:mm:ss") + "')"
        If Not MyConn.getRecord(rs, sql, fehler) Then
            logComplete("Fehler beim Laden der geänderten Artikel" + fehler, LogLevel.Exception)
        Else

            ' geholte Artikel in einer Liste speichern
            Dim article As New pxKommunikation.pxArtikel
            While Not rs.EOF
                article.ArtikelNr = rs.Fields("artikelNrLAG").Value.ToString()
                article.Bezeichnung1 = rs.Fields("bezeichnung1").Value.ToString()
                article.Bezeichnung2 = rs.Fields("bezeichnung2").Value.ToString()
                article.Bezeichnung3 = rs.Fields("bezeichnung3").Value.ToString()
                article.Geloescht = CInt(rs.Fields("geloescht").Value)

                articleList.Add(article)
                rs.MoveNext()
            End While
        End If
        Return articleList
    End Function

    Private Function updateInFLS(ByVal proffixArticle As pxKommunikation.pxArtikel, ByVal existingFLSarticle As JObject) As Boolean
        Dim response_FLS As String = String.Empty
        Dim sql As String = String.Empty
        Dim rs As New ADODB.Recordset
        Dim fehler As String = String.Empty

        Try
            ' JSON verändern
            existingFLSarticle = articleMapper.Mapp(proffixArticle, existingFLSarticle)

            ' Artikel in FLS updaten 
            response_FLS = flsConn.ExportChanges(existingFLSarticle("ArticleId").ToString, existingFLSarticle, ExporterCommitCommand.Update)

            ' Ist response_FLS keine GUID? --> update in FLS hat nicht geklappt, response_FLS enthält Fehlermeldung
            If Not pattern_GUID.IsMatch(response_FLS) Then
                logComplete("Fehler beim Updaten in FLS ArtikelNr: " + proffixArticle.ArtikelNr.ToString + " " + proffixArticle.Bezeichnung1, LogLevel.Exception, response_FLS)
                Throw New Exception("Fehler beim Updaten des Artikels in FLS")
            End If

            ' response_FLS ist GUID = update in FLS hat geklappt --> ArticleId in Proffix updaten
            If Not updateArticleIdInProffix(response_FLS, proffixArticle.ArtikelNr, fehler) Then
                ' Update ArticleId in Proffix hat nicht geklappt
                Logger.GetInstance().Log(LogLevel.Exception, "... des bereits in FLS vorhandenen Artikels. " + " ArtikelNr:" + proffixArticle.ArtikelNr + " " + proffixArticle.Bezeichnung1)
                Throw New Exception("Fehler beim Updaten der ArticleId in Proffix ArtikelNr ")
            End If

            ' wenn bis hierher --> update (in Artikel in FLS und ArticleId in Proffix) hat geklappt
            logComplete("Aktualisiert in FLS: ArticleNr " + proffixArticle.ArtikelNr + " Bezeichnung " + proffixArticle.Bezeichnung1, LogLevel.Info)
            Return True

        Catch ex As Exception
            logComplete("Fehler beim Updaten des Artikels in FLS ArtikelNr: " + proffixArticle.ArtikelNr, LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod().Name + " " + ex.Message)
            Return False
        End Try
    End Function

    Private Function createInFLS(ByVal proffixArticle As pxKommunikation.pxArtikel) As Boolean
        Dim newFLSarticle = New JObject
        Dim response_FLS As String = String.Empty
        Dim fehler As String = String.Empty

        Try
            ' JSON für neuen Artikel erstellen
            newFLSarticle = articleMapper.Mapp(proffixArticle, newFLSarticle)

            If LogAusfuehrlich Then
                Logger.GetInstance.Log(LogLevel.Info, My.Settings.ServiceAPIArticlesMethod)
                Logger.GetInstance.Log(LogLevel.Info, newFLSarticle.ToString)
            End If
            ' Artikel in FLS erstellen
            response_FLS = flsConn.ExportChanges("", newFLSarticle, ExporterCommitCommand.Create)

            ' Wenn InternalServerError und ArtikelName in FLS bereits vorhanden (unique) --> Fehlermeldung
            If response_FLS.Contains("InternalServerError") And articleNameExistsAlreadyInFLS(newFLSarticle("ArticleName").ToString) Then

                ' Anweisungen an User
                logComplete("Fehler: In FLS besteht bereits ein Artikel mit dem Artikelnamen/Bezeichnung1 """ + proffixArticle.Bezeichnung1 + """ ArtikelNr: " + newFLSarticle("ArticleNumber").ToString +
                "Deshalb konnte der Artikel in FLS nicht neu erstellt werden." + vbCrLf +
                "Sie haben folgende Möglichkeiten:" + vbCrLf +
                "- Ändern Sie den Artikelnamen/Bezeichnung1 """ + proffixArticle.Bezeichnung1 + """des Artikels" + vbCrLf +
                "- Falls der Artikel mit Artikel-Nr. " + newFLSarticle("ArticleNumber").ToString + " Artikelname/Bezeichnung1 " + proffixArticle.Bezeichnung1 + " gelöscht noch vorhanden ist, entfernen Sie das Häckchen bei ""gelöscht""",
                LogLevel.Exception)
                ' logcomplete( "- Erstellen Sie einen neuen Artikel mit der Artikel-Nr: " + newFLSarticle("ArticleNumber").ToString + "Artikelname/Bezeichnung1: " + proffixArticle.Bezeichnung1 + " und löschen Sie den Artikel mit der Artikel-Nr: " + proffixArticle.ArtikelNr)
                Logger.GetInstance.Log(LogLevel.Exception, "In FLS besteht bereits ein Artikel mit dem Artikelnamen/Bezeichnung """ + proffixArticle.Bezeichnung1 + """ ArtikelNr: " + newFLSarticle("ArticleNumber").ToString + " Anweisungen an Kunde im Log")
                Return False
            End If

            ' Ist response_FLS GUID? --> create hat geklappt, ansonsten enthält response_FLS die Fehlermeldung
            If Not pattern_GUID.IsMatch(response_FLS) Then
                logComplete("Fehler beim Erstellen in FLS: " + proffixArticle.ArtikelNr + " " + proffixArticle.Bezeichnung1, LogLevel.Exception, response_FLS)
                Return False
            End If

            ' response_FLS ist GUID --> in Proffix updaten
            If Not updateArticleIdInProffix(response_FLS, proffixArticle.ArtikelNr, fehler) Then
                Logger.GetInstance().Log(LogLevel.Exception, "... des soeben in FLS neu erstellten Artikels. " + " ArtikelNr:" + proffixArticle.ArtikelNr + " " + proffixArticle.Bezeichnung1)
                Return False
            End If

            ' wenn bis hierher --> create Artikel in FLS und update ArticleId in Proffix hat geklappt
            logComplete("Erstellt in FLS: " + proffixArticle.ArtikelNr + " " + proffixArticle.Bezeichnung1, LogLevel.Info)
            Return True

        Catch ex As Exception
            logComplete("Fehler beim Erstellen des Artikels in FLS ArtikelNr: " + proffixArticle.ArtikelNr, LogLevel.Exception, response_FLS + " " + ex.Message)
            Return Nothing
        End Try
    End Function

    ' prüft, ob Artikelname/Bezeichnung1 bereits in FLS in Artikeltabelle vorhanden ist (Feld ist unique)
    Private Function articleNameExistsAlreadyInFLS(ByVal articlename As String) As Boolean
        Dim articleResult As Threading.Tasks.Task(Of JArray)

        ' alle Artikel aus Artikeltabelle in FLS holen
        articleResult = flsConn.CallAsyncAsJArray(My.Settings.ServiceAPIArticlesMethod)
        articleResult.Wait()

        ' ist der Artikelname bereits in FLS vorhanden?
        For Each article In articleResult.Result.Children

            ' Artikelname/Bezeichnung in FLS bereits vorhanden
            If articlename = article("ArticleName").ToString Then
                Return True
            End If
        Next

        ' wenn bis hierher --> Artikelname in FLS in Artikeltabelle noch nicht vorhanden
        Return False

    End Function

    Private Function updateArticleIdInProffix(ByVal FLSarticleId As String, ByVal pxArtikelNr As String, ByRef fehler As String) As Boolean
        Dim sql As String = String.Empty
        Dim rs As New ADODB.Recordset

        ' Artikel mit neuer ArtikelId in Proffix schreiben
        sql = "Update lag_artikel set Z_ArticleId = '" + FLSarticleId + "', " + _
                "geaendertVon = '" + Assembly.GetExecutingAssembly().GetName.Name + "', geaendertAm = '" + Now.ToString(pxHelper.dateformat + " HH:mm:ss") + "' " + _
                "where ArtikelNrLAG = '" + pxArtikelNr + "'"
        If Not MyConn.getRecord(rs, sql, fehler) Then
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler beim Updaten der ArticleId " + FLSarticleId + " in Proffix")
            Return False
        Else
            Return True
        End If

    End Function

    Private Sub InvokeDoProgress()
        If DoProgress IsNot Nothing Then DoProgress.Invoke()
    End Sub


    ' schreibt in Log und in Logger (File)
    Private Sub logComplete(ByVal logString As String, ByVal loglevel As LogLevel, Optional ByVal zusatzloggerString As String = "")
        If Log IsNot Nothing Then Log.Invoke(If(loglevel <> loglevel.Info, vbTab, "") + logString)
        Logger.GetInstance.Log(loglevel, logString + " " + zusatzloggerString)
    End Sub

    ''' <summary>
    ''' Anzeigen des Synchronisationsfortschritt
    ''' </summary>
    Private Sub DoExporterProgress()
        'ProgressBar aktualisieren
        FrmMain.pbMain.Maximum = Count
        FrmMain.pbMain.Value = Progress
    End Sub

End Class
