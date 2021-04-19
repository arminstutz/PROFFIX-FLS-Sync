Imports Newtonsoft.Json.Linq
Imports System.Reflection
Imports SMC.Lib

''' <summary>
''' Eine Helper Klasse für aktionen die mit dem FLS Service zu tun haben
''' </summary>
''' <remarks></remarks>
Module FlsHelper

    ''' <summary>
    ''' Gibt das Änderungsdatum der Adresse zurück
    ''' </summary>
    ''' <param name="person">Die Adresse als JSON Object</param>
    ''' <returns>Das Änderungsdatum</returns>
    ''' <remarks></remarks>
    Public Function GetPersonChangeDate(ByVal person As JObject) As DateTime
        Dim newestDate As DateTime

        newestDate = GeneralHelper.GetNewestDate({
            DateTime.Parse(person("CreatedOn").ToString()),
            If(person("ModifiedOn") IsNot Nothing, DateTime.Parse(person("ModifiedOn").ToString()), DateTime.MinValue),
            If(person("ClubRelatedPersonDetails")("CreatedOn") IsNot Nothing, DateTime.Parse(person("ClubRelatedPersonDetails")("CreatedOn").ToString()), DateTime.MinValue),
            If(person("ClubRelatedPersonDetails")("ModifiedOn") IsNot Nothing, DateTime.Parse(person("ClubRelatedPersonDetails")("ModifiedOn").ToString()), DateTime.MinValue)})

        ' FLS hat UTC Zeit, Proffix hat lokale Zeit --> in lokale Zeit umwandeln (Hinweis: UTC-Zeitformat = Datetime.Tostring("o") anstatt "yyyy-MM...
        newestDate = newestDate.ToLocalTime()

        Return newestDate

    End Function


    ''' <summary>
    ''' Gibt das Änderungsdatum des flightszurück
    ''' </summary>
    ''' <param name="flight">Der Flug als JSON Object</param>
    ''' <returns>Das Änderungsdatum</returns>
    ''' <remarks></remarks>
    Public Function GetFlightChangeDate(ByVal flight As JObject) As DateTime
        Dim newestDate As DateTime

        newestDate = GeneralHelper.GetNewestDate({
            DateTime.Parse(flight("CreatedOn").ToString()),
            If(flight("ModifiedOn") IsNot Nothing, DateTime.Parse(flight("ModifiedOn").ToString()), DateTime.MinValue)})

        ' FLS hat UTC Zeit, Proffix hat lokale Zeit --> in lokale Zeit umwandeln (Hinweis: UTC-Zeitformat = Datetime.Tostring("o") anstatt "yyyy-MM...
        newestDate = newestDate.ToLocalTime()

        Return newestDate

    End Function

    ' da Adresse mit fulldetails geholt wird, sollten die Metadaten für update in FLS aus JSON entfernt werden
    Public Function removeMetadata(ByVal target As Object) As Object
        CType(target, JObject).Remove("CreatedOn")
        'CType(target, JObject).Remove("CreatedByUserId")
        'CType(target, JObject).Remove("RecordState")
        CType(target, JObject).Remove("ModifiedOn")
        'CType(target, JObject).Remove("ModifiedByUserId")
        CType(target, JObject).Remove("OwnerId")
        CType(target, JObject).Remove("OwnershipType")

        Return target
    End Function

    ' prüft einige Felder auf richtigen Inhalt
    Public Function validatePerson(ByVal person As JObject, ByVal proffixhelper As ProffixHelper, ByRef fehler As String) As Boolean
        Dim pxhelper = New ProffixHelper()
        pxhelper = proffixhelper

        ' ist PrivatEmail richtig angegeben?
        If Not String.IsNullOrEmpty(GetValOrDef(person, "PrivateEmail").ToString) Then
            If Not GeneralHelper.isEmail(person("PrivateEmail").ToString) Then
                fehler = "Kontrollieren Sie die private Emailadresse in Proffix. Der Inhalt entspricht nicht dem Muster einer E-Mailadresse."
                Return False
            End If
        End If

        ' ist GeschäftsEmail richtig angegeben?
        If Not String.IsNullOrEmpty(GetValOrDef(person, "BusinessEmail").ToString) Then
            If Not GeneralHelper.isEmail(person("BusinessEmail").ToString) Then
                fehler = "Kontrollieren Sie die Geschäfts-Emailadresse in Proffix. Der Inhalt entspricht nicht dem Muster einer E-Mailadresse."
                Return False
            End If
        End If

        ' ist MemberStateId eine gültige Id (muss in ZUS_FLSMemberStates vorhanden sein)
        If Not String.IsNullOrEmpty(GetValOrDef(person, "ClubRelatedPersonDetails.MemberStateId").ToString) Then
            If Not proffixhelper.isValidMemberStateId(person("ClubRelatedPersonDetails")("MemberStateId").ToString, fehler) Then
                Return False
            End If
        End If

        Return True
    End Function


    ''' <summary>
    ''' Gibt den Wert des JObject Felds als String zurück oder ein leeren String falls das Feld nicht existiert
    ''' </summary>
    ''' <param name="obj">Das JSON Object</param>
    ''' <param name="name">Der Name des Felds</param>
    ''' <returns>Den Wert des Felds als String oder ein leeren Text</returns>
    Public Function GetValOrDef(ByVal obj As JObject, ByVal name As String) As String
        Dim jObj As JToken = obj,
            values() As String = name.Split("."c),
            index As Integer = 0

        For Each nameValue As String In values

            ' wenn es das letzte Wort ist
            If (index = values.Length - 1) Then

                'Lesen des Feldes
                Dim value = jObj(nameValue)

                'Prüfen ob das Feld einen Wert entählt und wenn ja den Wert zurückgeben
                If value IsNot Nothing Then
                    Return value.ToString
                End If

                'Das Feld existiert nicht oder ist leer
                Return String.Empty

                ' Ende erreicht
            Else
                ' geht in das UnterJObject
                jObj = jObj(nameValue)
                If jObj Is Nothing Then Exit For
            End If
            index += 1
        Next
        Return String.Empty
    End Function

    Public Function GetValOrDefBoolean(ByVal obj As JObject, ByVal name As String) As String
        Return GetValOrNULL(obj, name, "boolean")
    End Function

    Function GetValOrDefString(obj As JObject, name As String) As String
        Return GetValOrNULL(obj, name, "string")
    End Function

    Function GetValOrDefInteger(obj As JObject, name As String) As String
        Return GetValOrNULL(obj, name, "integer")
    End Function

    Public Function GetValOrDefDateTime(ByVal obj As JObject, ByVal name As String, ByVal dateFormat As String) As String
        Return GetValOrNULL(obj, name, "datetime", dateFormat)
    End Function

    Public Function GetValOrDefDate(ByVal obj As JObject, ByVal name As String, ByVal dateformat As String) As String
        Return GetValOrNULL(obj, name, "date", dateformat)
    End Function

    Private Function GetValOrNULL(ByVal obj As JObject, ByVal name As String, ByVal type As String, Optional ByVal dateformat As String = Nothing) As String
        Dim jObj As JToken = obj,
        values() As String = name.Split("."c),
        index As Integer = 0

        For Each nameValue As String In values

            ' wenn es das letzte Wort ist
            If (index = values.Length - 1) Then

                'Lesen des Feldes
                Dim value = jObj(nameValue)

                'Prüfen ob das Feld einen Wert entählt und wenn ja den Wert zurückgeben
                If value IsNot Nothing Then

                    ' wenn es boolean ist
                    Select Case type
                        Case "string"
                            Return "'" + FormatTextForSQL(value.ToString) + "'"
                        Case "integer"
                            Return value.ToString
                        Case "datetime"
                            Return "'" + DateTime.Parse(value.ToString).ToLocalTime().ToString(dateformat + " " + "HH:mm:ss:fff") + "'"
                        Case "date"
                            Return "'" + DateTime.Parse(value.ToString).ToString(dateformat) + "'"
                        Case "boolean"
                            If value.ToString.ToLower = "true" Then
                                Return "1"
                            ElseIf value.ToString.ToLower = "false" Then
                                Return "0"
                            Else
                                Throw New Exception("Fehler in " + MethodBase.GetCurrentMethod.Name + " nicht true oder false property: " + name + "value: " + value.ToString + "Obj: " + obj.ToString)
                            End If
                        Case Else
                            Return "NULL"
                    End Select
                End If

                'Das Feld existiert nicht oder ist leer
                Return "NULL"

                ' Ende erreicht
            Else
                ' geht in das UnterJObject
                jObj = jObj(nameValue)
                If jObj Is Nothing Then Exit For
            End If
            index += 1
        Next
        Return "NULL"
    End Function

    ' passt SQL string an (maskiert ' und " in Text, 
    Function FormatTextForSQL(str As String) As String
        Dim sLeft As String
        Dim sRight As String

        sLeft = vbNullString
        sRight = str
        Do Until Len(sRight) = 0 Or InStr(1, sRight, "|") = 0
            sLeft = sLeft & Left(sRight, InStr(1, sRight, "|") - 1) '& "|"
            sRight = right(sRight, Len(sRight) - InStr(1, sRight, "|"))
        Loop
        str = sLeft & sRight

        sLeft = vbNullString
        sRight = str

        Do Until Len(sRight) = 0 Or InStr(1, sRight, """") = 0
            sLeft = sLeft & Left(sRight, InStr(1, sRight, """")) & """"
            sRight = right(sRight, Len(sRight) - InStr(1, sRight, """"))
        Loop
        str = sLeft & sRight

        sLeft = vbNullString
        sRight = str

        Do Until Len(sRight) = 0 Or InStr(1, sRight, "'") = 0
            sLeft = sLeft & Left(sRight, InStr(1, sRight, "'")) & "'"
            sRight = right(sRight, Len(sRight) - InStr(1, sRight, "'"))
        Loop
        str = sLeft & sRight

        FormatTextForSQL = str

    End Function

    ' fügt der MemberNr den Postfix hinzu (dass Adresse in PX nicht mehr existiert, da ganz gelöscht)
    Public Sub SetPostfixToMemberNr(ByRef person As JObject)
        ' wenn nicht bereits postfix angehängt
        If Not GetValOrDef(person, "ClubRelatedPersonDetails.MemberNumber").Contains(Postfix) Then
            ' postfix anhängen
            person("ClubRelatedPersonDetails")("MemberNumber") = GetValOrDef(person, "ClubRelatedPersonDetails.MemberNumber") + Postfix
        End If
    End Sub

    ' entfernt den Postfix der MemberNr
    Public Sub RemovePostfixFromMemberNr(ByRef person As JObject)
        ' enthält am Schluss nur noch MemberNr
        Dim memberNr As String = String.Empty
        ' jedes Zeichen des Strings durchgehen
        For Each zeichen In GetValOrDef(person, "ClubRelatedPersonDetails.MemberNumber")
            ' wenn es eine Zahl ist
            If IsNumeric(zeichen) Then
                ' an MemberNr anhängen (nicht, wenn es Buchstabe ist)
                memberNr += zeichen
            End If
        Next

        person("ClubRelatedPersonDetails")("MemberNumber") = memberNr
    End Sub

End Module
