Imports System.Reflection
Imports Newtonsoft.Json.Linq
Imports pxBook
Imports SMC.Lib

Public Class PersonCreator
    Public Property Log As Action(Of String)    ' Aktion, die ausgeführt wird, wenn in Logfeld geschrieben wird

    Private personMapper As New PersonMapper
    Private clubMapper As New ClubMapper

    Private deleter As PersonDeleter
    Private updater As PersonUpdater
    Private pxhelper As ProffixHelper
    Private myconn As ProffixConnection
    Private _serviceClient As FlsConnection
    Private lastSync As DateTime
    Private Property rs_adressdefault As ADODB.Recordset


    Public Sub New(ByVal myconn As ProffixConnection, ByVal rs_adressdefault As ADODB.Recordset, ByVal pxhelper As ProffixHelper, ByVal _serviceClient As FlsConnection, ByVal lastSync As DateTime, ByVal Log As System.Action(Of String), ByVal updater As PersonUpdater, ByVal deleter As PersonDeleter)
        Me.myconn = myconn
        Me.rs_adressdefault = rs_adressdefault
        Me.pxhelper = pxhelper
        Me._serviceClient = _serviceClient
        Me.lastSync = lastSync
        Me.Log = Log
        Me.updater = updater
        Me.deleter = deleter
    End Sub

    '*************************************************************************Nur in FLS vorhanden**********************************************************************
    ' in Proffix gibt es keine Adresse (geloscht ist irrelevant) mit dieser PersonId
    Public Function NurInFLS(ByVal person As JObject) As Boolean
        Dim response_FLS As String = String.Empty
        ' wenn diese Person noch keine AdressNr hat --> noch nie synchronisiert
        If GetValOrDef(person, "ClubRelatedPersonDetails.MemberNumber") = "" Then

             ' die Adresse wurde in FLS neu erstellt --> auch in PX erstellen (mit neu vergebener AdressNr) aus FLS sollen Adressen synchronisiert werden --> immer in PX erstellen und verknüpfen 
            If Not createInProffix(person, String.Empty) Then
                Return False
            End If

            ' wenn diese Person bereits eine AdressNr hat --> bereits mal synchronisiert 
            '--> wurde in PX gelöscht, da nur noch in FLS vorhanden
        Else

            ' PersonId existiert nur in FLS, hat aber eine MemberNr, Adresse wurde aber nicht verändert 
            ' wenn in FLS NICHT seit letzter Synchronisation verändert --> nichts machen
            If CDate(FlsHelper.GetPersonChangeDate(person)) < lastSync Then

                Logger.GetInstance.Log(LogLevel.Info, "Info: Adresse Name: " + person("Lastname").ToString + " Vorname: " + person("Firstname").ToString + " hat die PersonId " + person("PersonId").ToString.ToLower +
                                       ", welche nur in FLS existiert aber eine MemberNr " + GetValOrDef(person, "ClubRelatedPersonDetails.MemberNumber") + " hat. " + vbCrLf +
                                       "Die Adresse wurde seit der letzten Synchronisation nicht verändert. Mit dieser Adresse wird daher nichts gemacht.")

                '' wenn nicht bereits auf gelöscht gesetzt --> in FLS löschen
                'If GetValOrDef(person, "ClubRelatedPersonDetails.IsActive").ToLower <> "false" Then

                '    ' Postfix an MemberNr anhängen
                '    FlsHelper.SetPostfixToMemberNr(person)
                '    ' in FLS Person löschen (IsActive = false)
                '    If Not deleter.deleteInFLS(person) Then
                '        Return False
                '    End If
                'End If

                'in FLS nach letzter Synchronisation nochmals verändert --> in PX wieder erstellen
            Else
                ' ist in PX gelöscht worden, aber nach lastSync in FLS noch verändert --> versuchen wiederzuerstellen in PX mit der alten AdressNr
                FlsHelper.RemovePostfixFromMemberNr(person)

                ' prüfen, ob bereits vorhanden aber als nicht zu synchronisieren --> wenn ja wird gleich nur synchronisieren = 1 gesetzt
                If pxhelper.DoesAddressExistsAsNichtZuSynchronisierend(person("PersonId").ToString.ToLower.Trim) Then
                    If Not pxhelper.SetAsZuSynchroniseren(person("PersonId").ToString.ToLower.Trim) Then
                        logComplete("Die Adresse mit der PersonId " + person("PersonId").ToString.ToLower.Trim + "wurde in Proffix als zu synchronisieren gesetzt, da sie in FLS nach der letzten Synchronisation noch verändert wurde", LogLevel.Info)
                        Return False
                    End If

                    ' auch nicht als NichtZuSynchroniserend vorhanden --> neu erstellen
                Else

                    If Not createInProffix(person, GetValOrDef(person, "ClubRelatedPersonDetails.MemberNumber")) Then
                        ' nachfragen, ob für neue Adressnr erstellen
                        If Not DialogResult.OK = MessageBox.Show("Soll die Adresse für eine neue Adress-Nr. in Proffix erstellt werden?", "Adress-Nr. bereits vorhanden", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) Then
                            Return False
                        End If

                        ' wenn ok --> für neue AdressNr erstellen
                        If Not createInProffix(person, "") Then
                            Return False
                        End If
                    End If
                End If

                    ' in person wurde bei MemberNr Postfix entfernt --> an FLS senden
                response_FLS = _serviceClient.SubmitChanges(person("PersonId").ToString.ToLower.Trim, person, SyncerCommitCommand.Update)
                    If response_FLS <> "OK" Then
                        logComplete("Fehler beim Update von MemberNr ohne postfix. " + person.ToString, LogLevel.Exception)
                        Return False
                    End If
            End If
        End If

        'wenn bis hierher gekommen --> geklappt, wie gewünscht
        Return True

    End Function
    '************************************************************************Nur in PX vorhanden************************************************************************
    ' In FLS gibt es keine ungelöschte Adresse (IsActive ist irrelevant) mit dieser PersonId
    Public Function NurInPX(ByVal address As pxKommunikation.pxAdressen, ByVal flshardDeletedPersons As List(Of JObject)) As Boolean
        Dim DeletedOn As DateTime = DateTime.MinValue
        Dim addressChangeDate = ProffixHelper.GetAddressChangeDate(address)

        ' wenn diese Adresse noch keine PersonId hat --> noch nie synchronisiert
        If ProffixHelper.GetZusatzFelder(address, "Z_FLSPersonId") = "" Then

            If Not createInFLS(address) Then
                Return False
            End If

            ' wenn diese Adresse bereits eine PersonId hat --> bereits mal synchronisiert --> ist in FLS nicht mehr verfügbar, da hart gelöscht 
        Else
            ' wenn Adresse nicht unter den hartgelöschten --> Fehler
            ' die Adresse unter den gelöschten suchen
            For Each person As JObject In flshardDeletedPersons
                If person("PersonId").ToString.ToLower.Trim = ProffixHelper.GetZusatzFelder(address, "Z_FLSPersonId") Then
                    DeletedOn = DateTime.Parse(GetValOrDef(person, "DeletedOn"))
                End If
            Next
            ' unter den gelöschten in FLS wurde keine Person mit der entsprechenden PersonId gefunden, aber in PX mit PersonId vorhanden --> Fehler
            If DeletedOn = DateTime.MinValue Then
                logComplete("Fehler! Die Adresse mit der AdressNr " + address.AdressNr.ToString + " ist in Proffix bereits mit einer PersonId vorhanden aber in FLS nicht unter den gelöschten.", LogLevel.Exception)
                Return False

                ' unter den gelöschten wurde die Person in FLS gefunden
            Else
                ' mögliche Reihenfolgen:
                ' lastSync --> PX Veränderung --> deletedon oder PX Veränderung --> lastSync --> deletedon
                '       --> in PX löschen (geloescht = 1)
                ' lastSync --> deletedOn --> PX Veränderung oder deletedOn --> lastSync --> PX Veränderung
                '       in FLS neu erstellen (konnte gelöscht werden --> noch keine Verknüpfungen --> neue PersonId egal)
                ' deletedon --> PX Veränderung --> lastSync oder PX Veränderung --> deletedOn --> lastSync
                '       nichts machen

                ' wenn als letztes in FLS gelöscht wurde (egal ob in px vor oder nach lastSync verändert)
                If DeletedOn > lastSync And DeletedOn > addressChangeDate Then
                    ' --> in PX auch löschen
                    If Not deleter.deleteInProffix(address) Then
                        Return False
                    End If
                ElseIf addressChangeDate > lastSync And addressChangeDate > DeletedOn Then
                    ' wenn als letztes in PX verändert wurde (egal ob in fls vor oder nach lastSync verändert)
                    ' --> in FLS neu erstellen (konnte in FLS hart gelöscht werden --> ist noch nicht mit Flug... verbunden --> egal wenn neue PersonId
                    If Not createInFLS(address) Then
                        Return False
                    End If
                    '  logComplete("Hinweis: Adresse wurde in FLS gelöscht und aber danach in Proffix noch verändert AdressNr: " + address.AdressNr.ToString, LogLevel.Exception)

                End If
            End If
        End If
            Return True
    End Function




    ' erstellt Adresse in Proffix (Adressnr = "" --> ganz neue Adresse wird erstellt, AdressNr = Wert --> wurde früher bereits mal erstellt aber gelöscht --> mit gleicher AdressNr versuchen zu erstellen)
    Public Function createInProffix(ByVal person As JObject, ByVal bisherigeAdressNr As String) As Boolean
        Dim fehler As String = String.Empty
        Dim response_FLS As String = String.Empty
        Dim add As Object = CType(New pxKommunikation.pxAdressen, ValueType)

        If logAusfuehrlich Then
            Logger.GetInstance.Log(MethodBase.GetCurrentMethod().Name + person.ToString)
        End If

        ' Werte aus person in entsprechende Felder eines pxAdressen-Objektes (add) füllen
        add = personMapper.DeMapp(add, person)
        add = clubMapper.DeMapp(CType(add, pxKommunikation.pxAdressen), person)

        ' von Datentyp Objekt in pxAdresse umwandeln
        Dim adresse As pxKommunikation.pxAdressen = CType(add, pxKommunikation.pxAdressen)

        ' falls Adresse keinen Ort/PLZ enthält --> Defaultwerte für Proffix setzen
        adresse = ProffixHelper.SetAdressDefault(adresse, rs_adressdefault)

        ' wenn eine zu verwendende AdressNr mitgegeben wird (Adresse wurde bereits mal synchronisiert mit FLS) 
        If bisherigeAdressNr <> String.Empty Then
            'bisherige AdressNr verwenden (um zu verhindern, dass gleiche Personen mit neuer AdressNr wiedererstellt werden)
            adresse.AdressNr = CInt(bisherigeAdressNr)
            ' wenn keine zu verwendende AdressNr mitgegeben wird (Adresse hat noch nie bestanden)
        Else
            ' die bisherige Adresse hatte noch keine AdressNr --> nächste aus PX holen
            adresse.AdressNr = Proffix.GoBook.GetAdresseNr(fehler)
        End If

        ' Adresse in Proffix
        If Not Proffix.GoBook.AddAdresse(adresse, fehler, True, ProffixHelper.CreateZusatzFelderSql(adresse)) Then
            logComplete("Fehler beim Erstellen in Proffix für Nachname:" + GetValOrDef(person, "Lastname") + " Vorname: " + GetValOrDef(person, "Firstname") + ".", LogLevel.Exception)
            ' es wurde eine Adresse versucht zu erstellen, dessen AdressNr bereits vorhanden ist
            If fehler = "Adresse ist bereits vorhanden" Then
                logComplete("Es existiert bereits eine Adresse mit der AdressNr " + adresse.AdressNr.ToString, LogLevel.Exception)
            Else
                logComplete(fehler, LogLevel.Exception, person.ToString)
            End If
            Return False
        End If

        ' Zusatzfelder mit Datum nachträglich hinzufügen, da über Gobook.AddAdresse nicht funktioniert
        If Not pxhelper.SetDatumsZusatzfelderToPXAdresse(adresse, person, fehler) Then
            logComplete("Fehler beim Hinzufügen der Datums-Zusatzfelder AdressNr: " + adresse.AdressNr.ToString + " " + fehler, LogLevel.Exception)
            Return False
        End If

        If Not pxhelper.SetGeloeschtInPXAdresseDependingOnIsActive(person) Then
            logComplete("Fehler in SetGeloeschtInPXAdresse()", LogLevel.Exception)
            Return False
        End If

        ' wenn bisher noch keine AdressNr geben war (= wurde in FLS seit lastSync neu erstellt)
        If bisherigeAdressNr = "" Then
            ' neue AdressNr in FLS-Adresse speichern und an FLS übergeben
            person("ClubRelatedPersonDetails")("MemberNumber") = adresse.AdressNr.ToString
            response_FLS = _serviceClient.SubmitChanges(person("PersonId").ToString.ToLower.Trim, person, SyncerCommitCommand.Update)
            If response_FLS <> "OK" Then
                logComplete("Fehler beim updaten der AdressNr " + adresse.AdressNr.ToString + " in FLS der soeben in Proffix erstellten Adresse. Name: " + adresse.Name, LogLevel.Exception, response_FLS)
                Return False
            End If
        End If

        'Adresse in PROFFIX erstellt
        logComplete("Erstellt in Proffix. AdressNr: " + adresse.AdressNr.ToString + " Nachname: " + adresse.Name + " Vorname: " + If(adresse.Vorname IsNot Nothing, adresse.Vorname, ""), LogLevel.Info)
        Return True

    End Function

    ' erstellt Adresse in FLS
    Private Function createInFLS(ByVal address As pxBook.pxKommunikation.pxAdressen) As Boolean
        Dim pers As New JObject                                ' Für Werte, die direkt in JSON auf der vordersten Ebene eingefüllt werden können
        Dim clubpers As New JObject                             ' JObject, indem alle ClubRel. Werte gespeichert werden --> als weiteres Objekt zu pers hinzufügen
        Dim newPersonId As String = String.Empty
        Dim fehler As String = String.Empty
        Dim response_FLS As String = String.Empty

        ' pxAdressenobjekt in ein neues JObject pers einfügen
        pers = personMapper.Mapp(address, pers)
        ' eigenes JObject mit clubrel Daten erstellen
        clubpers = clubMapper.Mapp(address, clubpers)
        ' clubpers in pers einfügen
        pers = clubMapper.completePersWithclubPers(pers, clubpers)

        If Not FlsHelper.validatePerson(pers, pxhelper, fehler) Then
            logComplete("Fehler beim Validieren der Person. AdressNr: " & address.AdressNr & " Nachname: " & address.Name & " Vorname: " & address.Vorname & " " & fehler, LogLevel.Exception)
            Return False
        End If

        If logAusfuehrlich Then
            Logger.GetInstance.Log(MethodBase.GetCurrentMethod().Name + pers.ToString)
        End If

        If Not pxhelper.SetIsActiveInFLSPersonDependingOnGeloescht(pers, address.AdressNr.ToString) Then
            logComplete("Fehler in SetIsActiveInFLSPerson()", LogLevel.Exception)
            Return False
        End If

        ' Sicherstellen, dass keine MenberNumber mitgegeben wird (wenn in FLS gelöscht wurde + in PX noch verändert --> wird in FLS wieder erstellt, aber knallt, da MemberNumber schon mal vergeben war)
        pers("ClubRelatedPersonDetails")("MemberNumber") = Nothing

        If logAusfuehrlich Then
            Logger.GetInstance.Log(LogLevel.Info, "JSON um Adresse in FLS zu erstellen: " + pers.ToString)
        End If

        'neue Adresse in FLS schreiben und neu erstellte PersonId auslesen
        'response_FLS enthält bei Erfolg newPersonId (da create) und bei Misserfolg Fehlermeldung
        response_FLS = _serviceClient.SubmitChanges("", pers, SyncerCommitCommand.Create)

        ' Ist response_FLS GUID? --> create hat geklappt, ansonsten enthält response_FLS die Fehlermeldung
        If Not isGUID(response_FLS) Then
            logComplete("Fehler beim Erstellen in FLS AdressNr: " + address.AdressNr.ToString + " Nachname: " + address.Name + " Vorname: " + If(address.Vorname IsNot Nothing, address.Vorname, ""), LogLevel.Exception, response_FLS + " " + pers.ToString)
            Return False
        Else
            ' neuerstellte PersonId in pxAdresse --> Proffix schreiben
            address = ProffixHelper.SetZusatzFelder(address, "Z_FLSPersonId", "Z_FLSPersonId", "", "", response_FLS)

            Proffix.GoBook.AddAdresse(address, fehler, False, ProffixHelper.CreateZusatzFelderSql(address))
            If Not String.IsNullOrEmpty(fehler) Then
                logComplete("Fehler beim updaten der FLSPersonId in Proffix der in FLS soeben erstellten Adresse. AdressNr: " + address.AdressNr.ToString + " Nachname: " + address.Name + " Vorname: " + If(address.Vorname IsNot Nothing, address.Vorname, "") + " " + fehler, LogLevel.Exception)
                Return False
            End If
        End If

        'Adresse im FLS erstellen
        logComplete("Erstellt in FLS. AdressNr: " + address.AdressNr.ToString() + " Nachname: " + address.Name + " Vorname: " + If(address.Vorname IsNot Nothing, address.Vorname, ""), LogLevel.Info)
        Return True
    End Function

    ' schreibt in Log und in Logger (File)
    Private Sub logComplete(ByVal logString As String, ByVal loglevel As LogLevel, Optional ByVal zusatzloggerString As String = "")
        If Log IsNot Nothing Then Log.Invoke(If(loglevel <> loglevel.Info, vbTab, "") + logString)
        Logger.GetInstance.Log(loglevel, logString + " " + zusatzloggerString)
    End Sub
End Class

