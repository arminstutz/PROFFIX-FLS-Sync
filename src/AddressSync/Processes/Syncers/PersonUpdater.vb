Imports Newtonsoft.Json.Linq
Imports pxBook
Imports SMC.Lib
Imports System.Reflection

Public Class PersonUpdater
    Public Property Log As Action(Of String)    ' Aktion, die ausgeführt wird, wenn in Logfeld geschrieben wird

    Private personMapper As New PersonMapper
    Private clubMapper As New ClubMapper

    Private pxhelper As ProffixHelper
    Private myconn As ProffixConnection
    Private _serviceClient As FlsConnection
    Private Property rs_adressdefault As ADODB.Recordset

    Public Sub New(ByVal myconn As ProffixConnection, ByVal rs_adressdefault As ADODB.Recordset, ByVal pxhelper As ProffixHelper, ByVal _serviceClient As FlsConnection, ByVal Log As System.Action(Of String))
        Me.myconn = myconn
        Me.rs_adressdefault = rs_adressdefault
        Me.pxhelper = pxhelper
        Me._serviceClient = _serviceClient
        Me.Log = Log
    End Sub

    ' updates the address
    Public Function updateAccordingMaster(ByVal person As JObject, ByVal address As pxKommunikation.pxAdressen, ByVal master As UseAsMaster) As Boolean

        If logAusfuehrlich Then
            Logger.GetInstance.Log(LogLevel.Info, "In beiden vorhanden PersonId: " + person("PersonId").ToString.ToLower.Trim + " AdressNr: " + address.AdressNr.ToString)
        End If


        '***************************************************************************************UPDATE IN PROFFIX***********************************************************
        If master = UseAsMaster.fls Then

            'Die Adresse wird in PROFFIX aktualisiert
            If Not updateInProffix(person, address, rs_adressdefault) Then
                Return False
            End If

            '******************************************************************************UPDATE IN FLS******************************************************************
        ElseIf master = UseAsMaster.proffix Then

            ' wenn die Adresse synchronisiert werden soll
            If CInt(ProffixHelper.GetZusatzFelder(address, "Z_Synchronisieren")) = 1 Then

                ' in FLS updaten
                If Not updateInFLS(person, address) Then
                    Return False
                End If

            End If

            ' keine DB wurde als Master definiert --> das hier ist für diesen Fall die falsche Funktion
        Else
            Logger.GetInstance.Log(LogLevel.Exception, "Fehler in " + MethodBase.GetCurrentMethod.Name + " master hat nicht den Wert fls oder proffix, sondern " + master.ToString)
            Return False
        End If

        Return True
    End Function




    Public Function updateAccordingDate(ByVal person As JObject, ByVal address As pxKommunikation.pxAdressen, ByVal lastSync As DateTime) As Boolean
        'Flag auf Default false setzen
        Dim newerInFLS As Boolean = False
        Dim newerInProffix As Boolean = False
        Dim adressChangeDate As DateTime
        Dim personChangeDate As DateTime = CDate(FlsHelper.GetPersonChangeDate(person))

        If logAusfuehrlich Then
            Logger.GetInstance.Log(LogLevel.Info, "In beiden vorhanden PersonId: " + person("PersonId").ToString.ToLower.Trim + " AdressNr: " + address.AdressNr.ToString)
        End If

        ' Änderungsdatum auslesen
        adressChangeDate = ProffixHelper.GetAddressChangeDate(address)

        ' wenn beide nach LastSync verändert wurden --> welche ist neuer?
        If personChangeDate > lastSync And adressChangeDate > lastSync Then
            If personChangeDate > adressChangeDate Then
                newerInFLS = True
            ElseIf adressChangeDate > personChangeDate Then
                newerInProffix = True
            End If
        End If

        '***************************************************************************************UPDATE IN PROFFIX***********************************************************
        ' Adresse an beiden Orten vorhanden, nur in FLS nach LastSync verändert oder in FLS neuere Änderung
        ' --> Änderungen in Proffix updaten
        If (personChangeDate > lastSync And adressChangeDate < lastSync) Or newerInFLS Then

            'Die Adresse wird in PROFFIX aktualisiert
            If Not updateInProffix(person, address, rs_adressdefault) Then
                Return False
            End If


            '******************************************************************************UPDATE IN FLS******************************************************************
            ' Adresse an beiden vorhanden, nur in Proffix nach LastSync verändert, oder in Proffix neuere Änderung
            ' --> Änderungen in FLS updaten
        ElseIf (adressChangeDate > lastSync And personChangeDate < lastSync) Or newerInProffix Then

            ' wenn die Adresse synchronisiert werden soll
            If CInt(ProffixHelper.GetZusatzFelder(address, "Z_Synchronisieren")) = 1 Then

                ' in FLS updaten
                If Not updateInFLS(person, address) Then
                    Return False
                End If

            End If
        End If

        Return True
    End Function


    ' updated Adressen, die in beiden vorhanden, aber in FLS neuer sind, in Proffix
    Public Function updateInProffix(ByVal person As JObject, ByVal address As pxBook.pxKommunikation.pxAdressen, ByVal rs_adressdefault As ADODB.Recordset) As Boolean
        Dim fehler As String = String.Empty
        Dim successful As Boolean = True

        If logAusfuehrlich Then
            Logger.GetInstance.Log(LogLevel.Info, "Adresse zum updaten: " + person.ToString)
        End If

        'FLS-Daten in "veraltetes" pxAdressen übertragen
        address = CType(personMapper.DeMapp(CType(address, pxBook.pxKommunikation.pxAdressen), person), pxKommunikation.pxAdressen)
        address = clubMapper.DeMapp(address, person)

        ' Proffix braucht PLZ+Ort --> wenn nicht angegeben --> Defaultwerte setzen
        address = ProffixHelper.SetAdressDefault(CType(address, pxKommunikation.pxAdressen), rs_adressdefault)

        'Speichern der veränderten Adresse in Proffix
        If Not Proffix.GoBook.AddAdresse(CType(address, pxKommunikation.pxAdressen), fehler, False, ProffixHelper.CreateZusatzFelderSql(CType(address, pxKommunikation.pxAdressen))) Then
            logComplete("Fehler beim Updaten in Proffix. AdressNr: " + address.AdressNr.ToString + " Nachname: " + address.Name + " Vorname: " + If(address.Vorname IsNot Nothing, address.Vorname, "") + " " + fehler, LogLevel.Exception)
            Return False
        End If

        ' Zusatzfelder mit Datum über Gobook.AddAdresse hinzuzufügen, funktioniert nicht --> nachträglich mit ADODB
        If Not pxhelper.SetDatumsZusatzfelderToPXAdresse(address, person, fehler) Then
            logComplete("Fehler beim Hinzufügen der Datums-Zusatzfelder in ProffixAdressNr: " + address.AdressNr.ToString + " " + fehler, LogLevel.Exception)
            Return False
        End If

        ' IsActive/Geloescht synchronisieren
        If Not pxhelper.SetGeloeschtInPXAdresseDependingOnIsActive(person) Then
            logComplete("Fehler beim Updaten des Geloescht Feldes in Proffix. PersonId: " + person("PersonId").ToString.ToLower.Trim, LogLevel.Exception)
        End If

        logComplete("Aktualisiert in Proffix: AdressNr: " + address.AdressNr.ToString + " Nachname: " + address.Name + " Vorname: " + If(address.Vorname IsNot Nothing, address.Vorname, ""), LogLevel.Info)
        Return True

    End Function


    ' updated Adressen, die in beiden vorhanden, aber in Proffix neuer sind, in FLS
    ' wird auch verwendet, wenn bei Flugdatenimport Adresse mittels AdressNr nicht gefunden wird --> AdressNr aus Proffix in FLS updaten
    Public Function updateInFLS(ByVal person As JObject, ByVal address As pxBook.pxKommunikation.pxAdressen) As Boolean
        Dim response_FLS As String = String.Empty
        Dim fehler As String = String.Empty

        ' bisherige Person zwischenspeichern
        Dim personVorUpdate As String = person.ToString

        'Proffix-Daten in "veraltetes" JObject übertragen
        person = personMapper.Mapp(address, person)

        '' clubRel Werte updaten
        person = clubMapper.Mapp(address, person)

        ' möglicherweise falsche MemberNr/AdressNr in FLS wieder synchronisieren
        person("ClubRelatedPersonDetails")("MemberNumber") = address.AdressNr

        ' testen, ob sich Person verändert hat 
        ' --> wenn ja --> in FLS updaten, wenn nein --> es hat sich nichts verändert 
        ' --> nicht updaten
        If person.ToString.CompareTo(personVorUpdate) <> 0 Then

            ' Metadaten aus JSON entfernen
            FlsHelper.removeMetadata(person)

            ' prüft, ob Emails eine Email enthalten bzw. GUIDs GUIDs sind
            If Not FlsHelper.validatePerson(person, pxhelper, fehler) Then
                logComplete("Fehler beim Validieren der Adresse. AdressNr: " & address.AdressNr & " Nachname: " & address.Name & " Vorname: " & address.Vorname & " " & fehler, LogLevel.Exception)
                Return False
            End If

            If logAusfuehrlich Then
                Logger.GetInstance.Log(MethodBase.GetCurrentMethod().Name + person.ToString)
            End If

            ' Geloescht/IsActive in FLS updaten
            If Not pxhelper.SetIsActiveInFLSPersonDependingOnGeloescht(person, address.AdressNr.ToString) Then
                logComplete("Fehler beim Updaten des Geloescht Feldes in Proffix. PersonId: " + person("PersonId").ToString.ToLower.Trim, LogLevel.Exception)
                Return False
            End If

            ' If logAusfuehrlich Then
            '    Logger.GetInstance.Log(LogLevel.Info, "JSON um Adresse in FLS zu aktualisieren: " + person.ToString)
            ' End If

            'Adresse in FLS updaten (mit Änderungen, die in Proffix gemacht wurden
            response_FLS = _serviceClient.SubmitChanges(person("PersonId").ToString.ToLower.Trim, person, SyncerCommitCommand.Update)
            If response_FLS <> "OK" Then
                logComplete("Fehler beim Updaten In FLS: AdressNr: " + address.AdressNr.ToString + "Nachname: " + address.Name + " Vorname: " + If(address.Vorname IsNot Nothing, address.Vorname, "") + address.Name, LogLevel.Exception, response_FLS + " " + person.ToString)
                Return False
            End If

            logComplete("Aktualisiert in FLS: AdressNr: " + address.AdressNr.ToString + " Nachname: " + address.Name + " Vorname: " + GetValOrDef(person, "Firstname"), LogLevel.Info)
        End If
        Return True

    End Function


    ' schreibt in Log und in Logger (File)
    Private Sub logComplete(ByVal logString As String, ByVal loglevel As LogLevel, Optional ByVal zusatzloggerString As String = "")
        If Log IsNot Nothing Then Log.Invoke(If(loglevel <> LogLevel.Info, vbTab, "") + logString)
        Logger.GetInstance.Log(loglevel, logString + " " + zusatzloggerString)
    End Sub

End Class