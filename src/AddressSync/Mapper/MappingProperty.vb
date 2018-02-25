Imports pxBook

Imports Newtonsoft.Json.Linq

''' <summary>
''' Das Interface der MappingProperty
''' </summary>
Public Interface IMappingProperty
    ''' <summary>
    ''' Der Name des Feldes in der Quell-Klasse
    ''' </summary>
    Property SourcePropertyName As String
    ''' <summary>
    ''' Der Name des Feldes in der Ziel-Klasse
    ''' </summary>
    Property TargetPropertyName As String

    ''' <summary>
    ''' Definition der Mapp Methode
    ''' </summary>
    ''' <param name="source">Das Quell-Objekt</param>
    ''' <param name="target">Das Ziel-Objekt</param>
    ''' <returns>Das neu gemappte Ziel-Objekt</returns>
    Function Mapp(ByVal source As Object, ByRef target As Object) As Object

    ''' <summary>
    ''' Definition der DeMapp Methode
    ''' </summary>
    ''' <param name="source">Das Quell-Objekt</param>
    ''' <param name="target">Das Ziel-Objekt</param>
    ''' <returns>Das neu gemappte Ziel-Objekt</returns>
    Function DeMapp(ByVal source As Object, ByRef target As Object) As Object

    ''' <summary>
    ''' Gibt den Wert des Source-Objekts zurück
    ''' </summary>
    ''' <param name="source">Das Objekt</param>
    ''' <param name="name">Der Name des Feldes</param>
    ''' <returns>Den Wert des Feldes</returns>
    Function GetValue(ByVal source As Object, ByVal name As String) As Object

    ''' <summary>
    ''' Setzt den Wert des Target-Objekts
    ''' </summary>
    ''' <param name="target">Das Objekts</param>
    ''' <param name="name">Der Name des Feldes</param>
    ''' <param name="value">Den Wert der gesetzt wird</param>
    Sub SetValue(ByRef target As Object, ByVal name As String, ByVal value As Object)
End Interface

''' <summary>
''' Die Generische Implementation des IMappingProperty Interfaces
''' </summary>
''' <typeparam name="TP">Der erste Typ</typeparam>
''' <typeparam name="TA">Der zweite Typ</typeparam>
Public Class MappProperty(Of TP, TA)
    Implements IMappingProperty

    Private laender_dict As Dictionary(Of String, String) = Nothing

    ''' <summary>
    ''' Der Name des Feldes in der Quell-Klasse
    ''' </summary>
    Public Property SourcePropertyName As String Implements IMappingProperty.SourcePropertyName
    ''' <summary>
    ''' Der Name des Feldes in der Ziel-Klasse
    ''' </summary>
    Public Property TargetPropertyName As String Implements IMappingProperty.TargetPropertyName

    ''' <summary>
    ''' Parst das Objekt vom Typ TP zum Typ TA
    ''' </summary>
    ''' <value>Das Objekt vom Typ TP</value>
    ''' <returns>Das Objekt vom Typ TA</returns>
    Public Property ToTargetProperty As Func(Of TP, TA)
    ''' <summary>
    ''' Parst das Objekt vom Typ TA zum Typ TP
    ''' </summary>
    ''' <value>Das Objekt vom Typ TA</value>
    ''' <returns>Das Objekt vom Typ TP</returns>
    Public Property ToSourceProperty As Func(Of TA, TP)

    ''' <summary>
    ''' Setzt alle Werte des ersten Objekts gleich wie die des zweiten Objekts
    ''' </summary>
    ''' <value>Das Source-Objekt und das Ziel-Objekt</value>
    ''' <returns>Das neue Ziel-Objekt</returns>
    Public Property MappFunc As Func(Of Object, Object, Object)
    ''' <summary>
    ''' Setzt alle Werte des ersten Objekts gleich wie die des zweiten Objekts
    ''' </summary>
    ''' <value>Das Source-Objekt und das Ziel-Objekt</value>
    ''' <returns>Das neue Ziel-Objekt</returns>
    Public Property DeMappFunc As Func(Of Object, Object, Object)

    ''' <summary>
    ''' Gibt den Wert des Felds des Objekts zurück
    ''' </summary>
    ''' <value>Das Objekt und der Name des Feldes</value>
    ''' <returns>Der Wert des Felds</returns>
    Public Property GetValueFunc As Func(Of Object, String, Object)
    ''' <summary>
    ''' Setzt den Wert des Felds des Objekts
    ''' </summary>
    ''' <value>Das Objekt, der Name des Fels und der neue Wert</value>
    ''' <returns>Das neue Objekt</returns>
    Public Property SetValueFunc As Func(Of Object, String, Object, Object)

    ''' <summary>
    ''' Initialisiert die Klasse
    ''' </summary>
    ''' <param name="targetPropertyName">Der Name des Ziel-Felds</param>
    ''' <param name="sourcePropertyName">Der Name des Quell-Felds</param>
    Public Sub New(ByVal targetPropertyName As String, ByVal sourcePropertyName As String)
        Me.New(sourcePropertyName, targetPropertyName, Nothing, Nothing)
    End Sub

    ''' <summary>
    ''' Initialisiert die Klasse
    ''' </summary>
    ''' <param name="targetPropertyName">Der Name des Ziel-Felds</param>
    ''' <param name="sourcePropertyName">Der Name des Quell-Felds</param>
    ''' <param name="toTargetProperty">Die Parse Funktion des Ziel Objekts</param>
    ''' <param name="toSourceProperty">Die Parse Funktion des Quell Objekts</param>
    Public Sub New(ByVal targetPropertyName As String, ByVal sourcePropertyName As String,
                   ByVal toTargetProperty As Func(Of TP, TA), ByVal toSourceProperty As Func(Of TA, TP))
        Me.SourcePropertyName = sourcePropertyName
        Me.TargetPropertyName = targetPropertyName
        Me.ToTargetProperty = toTargetProperty
        Me.ToSourceProperty = toSourceProperty
    End Sub

    ''' <summary>
    ''' Initialisiert die Klasse
    ''' </summary>
    ''' <param name="targetPropertyName">Der Name des Ziel-Felds</param>
    ''' <param name="sourcePropertyName">Der Name des Quell-Felds</param>
    ''' <param name="toTargetProperty">Die Parse Funktion des Ziel Objekts</param>
    ''' <param name="toSourceProperty">Die Parse Funktion des Quell Objekts</param>
    ''' <param name="mappFunc">Die Mapping Funktion</param>
    ''' <param name="demappFunc">Die DeMapping Funktion</param>
    Public Sub New(ByVal targetPropertyName As String, ByVal sourcePropertyName As String,
                   ByVal toTargetProperty As Func(Of TP, TA), ByVal toSourceProperty As Func(Of TA, TP),
                   ByVal mappFunc As Func(Of Object, Object, Object), ByVal demappFunc As Func(Of Object, Object, Object))
        Me.SourcePropertyName = sourcePropertyName
        Me.TargetPropertyName = targetPropertyName
        Me.ToTargetProperty = toTargetProperty
        Me.ToSourceProperty = toSourceProperty
        Me.MappFunc = mappFunc
        Me.DeMappFunc = demappFunc
    End Sub

    ''' <summary>
    ''' Initialisiert die Klasse
    ''' </summary>
    ''' <param name="targetPropertyName">Der Name des Ziel-Felds</param>
    ''' <param name="sourcePropertyName">Der Name des Quell-Felds</param>
    ''' <param name="toTargetProperty">Die Parse Funktion des Ziel Objekts</param>
    ''' <param name="toSourceProperty">Die Parse Funktion des Quell Objekts</param>
    ''' <param name="mappFunc">Die Mapping Funktion</param>
    ''' <param name="demappFunc">Die DeMapping Funktion</param>
    ''' <param name="getValueFunc">Die Funtkion um den Wert eines Felds zu lesen</param>
    ''' <param name="setValueFunc">Die Funktion um den Wert eines Felds zu schreiben</param>
    Public Sub New(ByVal targetPropertyName As String, ByVal sourcePropertyName As String,
                   ByVal toTargetProperty As Func(Of TP, TA), ByVal toSourceProperty As Func(Of TA, TP),
                   ByVal mappFunc As Func(Of Object, Object, Object), ByVal demappFunc As Func(Of Object, Object, Object),
                   ByVal getValueFunc As Func(Of Object, String, Object), ByVal setValueFunc As Func(Of Object, String, Object, Object))
        Me.SourcePropertyName = sourcePropertyName
        Me.TargetPropertyName = targetPropertyName
        Me.ToTargetProperty = toTargetProperty
        Me.ToSourceProperty = toSourceProperty
        Me.MappFunc = mappFunc
        Me.DeMappFunc = demappFunc
        Me.GetValueFunc = getValueFunc
        Me.SetValueFunc = setValueFunc
    End Sub

    ''' <summary>
    ''' Mappt die Werte des Source-Objekts zum Target-Objekt
    ''' </summary>
    ''' <param name="source">Das Quell-Objekt</param>
    ''' <param name="target">Das Ziel-Objekt</param>
    ''' <returns>Das neue Objekt</returns>
    Public Function Mapp(ByVal source As Object, ByRef target As Object) _
        As Object Implements IMappingProperty.Mapp
        'Wenn eine spezielle Mapping Funtkion übergeben wurde wird diese verwendet
        If MappFunc IsNot Nothing Then
            Return MappFunc(source, target)
        Else
            'Anderenfalls wird die "GetValue" Mehtode aufgerufen
            Dim data As Object = GetValue(source, SourcePropertyName)


            'Wenn der Wert in einen anderen Typ geparst werden muss wird das hier gemacht
            If ToSourceProperty IsNot Nothing Then
                data = ToSourceProperty.Invoke(CType(data, TA))

            End If

            'Als letztes wird der Wert auf das Ziel-Objekt gesetzt ()
            SetValue(target, TargetPropertyName, data)

            Return target
        End If
    End Function

    ''' <summary>
    ''' DeMappt die Werte des Source-Objekt zum Target-Objekt
    ''' </summary>
    ''' <param name="source">Das Quell-Objekt</param>
    ''' <param name="target">Das Ziel-Objekt</param>
    ''' <returns>Das neue Objekt</returns>
    Public Function DeMapp(ByVal source As Object, ByRef target As Object) As Object _
        Implements IMappingProperty.DeMapp
        'Wenn eine spezielle DeMapping Funtkion übergeben wurde wird diese verwendet
        If DeMappFunc IsNot Nothing Then
            Return DeMappFunc(source, target)
        Else
            'Anderenfalls wird die "GetValue" Mehtode aufgerufen
            Dim data = GetValue(source, TargetPropertyName)
            'Wenn der Wert in einen anderen Typ geparst werden muss wird das hier gemacht
            If ToTargetProperty IsNot Nothing Then
                data = ToTargetProperty.Invoke(CType(data, TP))
            End If
            'Als letztes wird der Wert auf das Ziel-Objekt gesetzt
            SetValue(target, SourcePropertyName, data)
            Return target
        End If
    End Function

    ''' <summary>
    ''' Gibt den Wert des Feldes zurück
    ''' </summary>
    ''' <param name="source">Das Quell-Objekt</param>
    ''' <param name="name">Der Name des Fels</param>
    ''' <returns>Der Wert des Fels</returns>
    Public Function GetValue(ByVal source As Object, ByVal name As String) As Object Implements IMappingProperty.GetValue
        'Wenn eine spezielle 'GetValue' Funktion definiert wurde wird diese verwendet
        If GetValueFunc IsNot Nothing Then Return GetValueFunc(source, name)

        'Anderenfalls wird geprüft ob es ein JSON Objekt ist sich dieses anders verhält als eine gewönliche Klasse
        Dim data As Object
        If source.GetType() Is GetType(JObject) Then
            'Lesen der Daten wenn es ein JSON Objekt ist
            data = CType(source, JObject)(name)
            If data IsNot Nothing Then
                data = data.ToString
            End If
        Else
            'Es ist kein JSON Objekt
            'Der Wert wird als Property oder Feld gelesen
            data = source.GetType().GetProperty(name)
            If data Is Nothing Then
                data = source.GetType().GetField(name).GetValue(source)
            Else
                data = CType(data, Reflection.PropertyInfo).GetValue(source, Nothing)
            End If
        End If

        Return data
    End Function


    ''' <summary>
    ''' Setzt den Wert des Feldes
    ''' </summary>
    ''' <param name="target">Das Ziel-Objekt</param>
    ''' <param name="name">Der Name des Felds</param>
    ''' <param name="value">Der Wert des Felds</param>
    Public Sub SetValue(ByRef target As Object, ByVal name As String, ByVal value As Object) Implements IMappingProperty.SetValue
        Dim isNull As Boolean = False

        'Wenn eine spezielle 'SetValue' Funktion definiert wurde wird diese verwendet
        If SetValueFunc IsNot Nothing Then
            target = SetValueFunc.Invoke(target, name, value)
            Exit Sub
        End If

        'Wenn das auszufüllende Objekt ein JSON Objekt ist
        If target.GetType() Is GetType(JObject) Then

            ' wenn kein value keinen Wert hat
            If value Is Nothing Then
                ' FLS verlangt einen Vornamen
                If name = "Firstname" Then
                    value = "."
                    CType(target, JObject)(name) = value.ToString()
                Else
                    isNull = True
                End If

                ' wenn value einen Wert hat
            Else
                ' ist es ein Datum?
                If value.GetType() Is GetType(Date) Or value.GetType() Is GetType(DateTime) Then
                    ' In einer als Datetime definierten Variable speichern (erst dann können datetimefunktionen aufgerufen werden)
                    Dim datum_datetime As DateTime = CType(value, DateTime)

                    ' wenn kein Datum eingefügt ist --> nichts hineinschreiben (bei keinem Datum ist day, month und year = 1 und hour, minute and second = 0)
                    If datum_datetime.Day + datum_datetime.Month + datum_datetime.Year = 3 Then
                        isNull = True

                        ' es soll ein gültiges Date/Datetime hineingeschrieben werden
                    Else
                        CType(target, JObject)(name) = datum_datetime.ToString("o")
                    End If

                    ' es ist kein Datum einzufügen sondern String oder Boolean
                Else

                    ' wenn in Proffix Defaultwerte für Ort + PLZ --> in FLS nichts übertragen
                    If (name = "ZipCode" And value.ToString = "9999") Or (name = "City" And value.ToString = "Ort unbekannt") Then
                        isNull = True
                    Else

                        ' wenn Land --> CountryId einfügen (auch in MappingProperties einfügen
                        If name = "CountryId" Then
                            value = landToCountryId(value.ToString)
                        End If

                        ' wenn bis hierher gekommen --> value für name normal einfügen
                        CType(target, JObject)(name) = value.ToString()

                    End If
                End If
            End If

            ' null, damit vorher ausgefüllte Felder leerbar
            If isNull Then
                CType(target, JObject)(name) = Nothing
            End If

        Else
            ' CountryId in Landeskürzel umwandeln 
            If name = "Land" Then
                If value IsNot Nothing Then
                    value = CountryIdToLand(value.ToString)
                End If
            End If

            'Es ist kein JSON Objekt
            Dim data As Object = target.GetType().GetProperty(name)
            If data Is Nothing Then
                Dim objTarget As ValueType = CType(target, ValueType)
                target.GetType().GetField(name).SetValue(objTarget, value)
                target = CType(objTarget, Object)
            Else

                ' wenn bis hierher gekommen --> value für name normal einfügen
                CType(target, JObject)(name) = value.ToString()

                CType(data, Reflection.PropertyInfo).SetValue(target, value, Nothing)
            End If
        End If
    End Sub

    ' gibt für einen Ländernamen die entsprechende CountryId zurück
    Private Function landToCountryId(ByVal value As String) As String
        laender_dict = GeneralDataLoader.GetLaender()
        ' entsprechende Werte aus Dict auslesen (muss in
        If laender_dict.ContainsKey(value.ToString) Then
            value = laender_dict(value.ToString)
        Else
            value = Nothing
        End If
        Return value
    End Function

    '  CountryId in Land umwandeln 
    Private Function CountryIdToLand(ByVal value As String) As String
        For Each pair In GeneralDataLoader.GetLaender()
            If pair.Value = value Then
                value = pair.Key
            End If
        Next
        Return value
    End Function


    '' Spezielle Werte abfangen, die bei der Synchronisation nicht richtig übertragen würden
    'Private Function isSpecialValue(ByVal name As String, ByVal value As Object) As Boolean

    '    ' wenn ein Wert leer ist --> nichts in JSON schreiben
    '    If value Is Nothing Then
    '        Return True
    '    ElseIf value.ToString = "" Then
    '        Return True
    '        ' wenn in Proffix kein Datum --> würde 1.1.01 in FLS schreiben --> abfangen --> nichts in JSON schreiben
    '        ' ElseIf value.ToString.Contains("01.01.0001") Or value.ToString.Contains("0001-01-01") Then
    '        '    Return True
    '        ' wenn in Proffix Defaultwerte für Ort + PLZ --> in FLS nichts übertragen
    '    ElseIf (name = "ZipCode" And value.ToString = "9999") Or (name = "City" And value.ToString = "Ort unbekannt") Then
    '        Return True
    '    End If

    '    ' wenn bis hierher gekommen --> kein Spezialfall --> false zurückgeben = kein Spezialfall
    '    Return False
    'End Function

End Class

''' <summary>
''' Nicht generische Implementation des IMappingProperty Interface
''' </summary>
Public Class MappingProperty
    Inherits MappProperty(Of Object, Object)

    ''' <summary>
    ''' Initialisiert die Klasse
    ''' </summary>
    ''' <param name="personPropertyName">Der Name des Ziel-Felds</param>
    ''' <param name="addressPropertyName">Der Name des Quell-Felds</param>
    Public Sub New(ByVal personPropertyName As String, ByVal addressPropertyName As String)
        MyBase.New(personPropertyName, addressPropertyName, Nothing, Nothing)
    End Sub

    ''' <summary>
    ''' Initialisiert die Klasse
    ''' </summary>
    ''' <param name="personPropertyName">Der Name des Ziel-Felds</param>
    ''' <param name="addressPropertyName">Der Name des Quell-Felds</param>
    ''' <param name="toAdressProperty">Die Parse Funktion des Ziel Objekts</param>
    ''' <param name="toPersonProperty">Die Parse Funktion des Quell Objekts</param>
    Public Sub New(ByVal personPropertyName As String, ByVal addressPropertyName As String,
                   ByVal toAdressProperty As Func(Of Object, Object), ByVal toPersonProperty As Func(Of Object, Object))
        MyBase.New(personPropertyName, addressPropertyName, toAdressProperty, toPersonProperty)
    End Sub

    ''' <summary>
    ''' Initialisiert die Klasse
    ''' </summary>
    ''' <param name="personPropertyName">Der Name des Ziel-Felds</param>
    ''' <param name="addressPropertyName">Der Name des Quell-Felds</param>
    ''' <param name="toAdressProperty">Die Parse Funktion des Ziel Objekts</param>
    ''' <param name="toPersonProperty">Die Parse Funktion des Quell Objekts</param>
    ''' <param name="mappFunc">Die Mapping Funktion</param>
    ''' <param name="demappFunc">Die DeMapping Funktion</param>
    Public Sub New(ByVal personPropertyName As String, ByVal addressPropertyName As String,
                   ByVal toAdressProperty As Func(Of Object, Object), ByVal toPersonProperty As Func(Of Object, Object),
                   ByVal mappFunc As Func(Of Object, Object, Object), ByVal demappFunc As Func(Of Object, Object, Object))
        MyBase.New(personPropertyName, addressPropertyName, toAdressProperty, toPersonProperty, mappFunc, demappFunc)
    End Sub

    ''' <summary>
    ''' Initialisiert die Klasse
    ''' </summary>
    ''' <param name="personPropertyName">Der Name des Ziel-Felds</param>
    ''' <param name="addressPropertyName">Der Name des Quell-Felds</param>
    ''' <param name="toAdressProperty">Die Parse Funktion des Ziel Objekts</param>
    ''' <param name="toPersonProperty">Die Parse Funktion des Quell Objekts</param>
    ''' <param name="mappFunc">Die Mapping Funktion</param>
    ''' <param name="demappFunc">Die DeMapping Funktion</param>
    ''' <param name="getValueFunc">Die Funtkion um den Wert eines Felds zu lesen</param>
    ''' <param name="setValueFunc">Die Funktion um den Wert eines Felds zu schreiben</param>
    Public Sub New(ByVal personPropertyName As String, ByVal addressPropertyName As String,
                   ByVal toAdressProperty As Func(Of Object, Object), ByVal toPersonProperty As Func(Of Object, Object),
                   ByVal mappFunc As Func(Of Object, Object, Object), ByVal demappFunc As Func(Of Object, Object, Object),
                   ByVal getValueFunc As Func(Of Object, String, Object), ByVal setValueFunc As Func(Of Object, String, Object, Object))
        MyBase.New(personPropertyName, addressPropertyName, toAdressProperty, toPersonProperty, mappFunc, demappFunc, getValueFunc, setValueFunc)
    End Sub
End Class

' alt:
' wenn das Datum keine Zeit enthält --> + 2h, damit bei Umwandlung in UTC (rechnet in Sommerzeit -2h) nicht in Vortag rutscht
'If datum_datetime.Hour + datum_datetime.Minute + datum_datetime.Second = 0 Then
'    datum_datetime.AddHours(3)
'End If

' in UTC-Format in FLS schreiben
'CType(target, JObject)(name) = datum_datetime.ToUniversalTime.ToString("o")
