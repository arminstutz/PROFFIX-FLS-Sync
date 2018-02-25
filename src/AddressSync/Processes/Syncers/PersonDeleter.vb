Imports Newtonsoft.Json.Linq
Imports SMC.Lib
Imports pxBook

Public Class PersonDeleter
  
    Public Property Log As Action(Of String)    ' Aktion, die ausgeführt wird, wenn in Logfeld geschrieben wird
    Private pxhelper As ProffixHelper
    Private _serviceClient As FlsConnection
    Public Sub New(ByVal _serviceClient As FlsConnection, ByVal pxhelper As ProffixHelper, ByVal Log As System.Action(Of String))
        Me._serviceClient = _serviceClient
        Me.pxhelper = pxhelper
        Me.Log = Log
    End Sub

    ' setzt in FLS Adresse auf IsActive = false (Dieses Programm löscht keine Adressen ganz)
    Public Function deleteInFLS(ByVal person As JObject) As Boolean
        Dim response_FLS As String = String.Empty
        person("ClubRelatedPersonDetails")("IsActive") = False
  
        'Adresse in FLS updaten (mit Änderungen, die in Proffix gemacht wurden
        response_FLS = _serviceClient.SubmitChanges(person("PersonId").ToString(), person, SyncerCommitCommand.Update)
        If response_FLS <> "OK" Then
            logComplete("Fehler beim Löschen In FLS: AdressNr: " + GetValOrDef(person, "ClubRelatedPersonDetails.MemberNumber") + "Nachname: " + GetValOrDef(person, "Lastname") + " Vorname: " + GetValOrDef(person, "Firstname"), LogLevel.Exception, response_FLS + " " + person.ToString)
            Return False
        End If
        logComplete("Gelöscht in FLS Nachname: " + GetValOrDef(person, "Lastname") + " Vorname: " + GetValOrDef(person, "Firstname"), LogLevel.Info)
        Return True
    End Function

    Public Function deleteInProffix(ByVal address As pxKommunikation.pxAdressen) As Boolean

        If Not pxhelper.SetPXAddressAsGeloescht(ProffixHelper.GetZusatzFelder(address, "Z_FLSPersonId")) Then
            Return False
        End If

        logComplete("Gelöscht in Proffix Nachname: " + address.Name + " Vorname: " + address.Vorname, LogLevel.Info)
        Return True
    End Function


    ' schreibt in Log und in Logger (File)
    Private Sub logComplete(ByVal logString As String, ByVal loglevel As LogLevel, Optional ByVal zusatzloggerString As String = "")
        If Log IsNot Nothing Then Log.Invoke(If(loglevel <> loglevel.Info, vbTab, "") + logString)
        Logger.GetInstance.Log(loglevel, logString + " " + zusatzloggerString)
    End Sub

End Class