Imports Newtonsoft.Json.Linq
Imports pxBook

Public Class ArticleMapper

    ' Dictionary der Felder, die im FLS <-> Proffix entsprechen
    Private ReadOnly Property MappingProperties As Dictionary(Of String, String)
        Get
            Dim articleProperties_dict = New Dictionary(Of String, String) From {
          {"ArticleNumber", "ArtikelNr"},
           {"ArticleName", "Bezeichnung1"},
           {"ArticleInfo", "Bezeichnung2"},
           {"Description", "Bezeichnung3"},
           {"IsActive", "Geloescht"}}

            Return articleProperties_dict
        End Get
    End Property

    Private Property Exporter As Exporter



    ' updatet target mit Werten aus source
    Public Function Mapp(ByVal source As pxBook.pxKommunikation.pxArtikel, ByVal target As JObject) As JObject
        Dim data As Object

        ' holt für jede Artikeleigenschaft den Wert aus pxArtikel und setzt sie in JSON ein
        For Each item In MappingProperties
            data = GetValue(source, item.Value.ToString)
            SetValue(CType(target, Object), item.Key.ToString, data)
        Next

        ' gibt geupdatetes JSON zurück
        Return target
    End Function

    ''' <summary>
    ''' Liest Wert aus pxArtikel
    ''' </summary>
    ''' <param name="source"></param>
    ''' <param name="name"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function GetValue(ByVal source As Object, ByVal name As String) As Object

        Dim data As Object

        ' Feld wird aus pxArtikel gelesen
        data = source.GetType().GetProperty(name)
        If data Is Nothing Then
            data = source.GetType().GetField(name).GetValue(source)
        Else
            data = CType(data, Reflection.PropertyInfo).GetValue(source, Nothing)
        End If

        Return data
    End Function


    ''' <summary>
    ''' Schreibt wert in ein JSON
    ''' </summary>
    ''' <param name="target"></param>
    ''' <param name="name"></param>
    ''' <param name="value"></param>
    ''' <remarks></remarks>
    Public Sub SetValue(ByRef target As Object, ByVal name As String, ByVal value As Object)

        ' wenn value keinen Wert hat --> null in JSON
        If value Is Nothing Then
            CType(target, JObject)(name) = Nothing

            ' wenn value einen Wert hat --> Wert für name einfügen
        Else

            If name = "IsActive" Then
                Dim jValue As JValue = CType(False, JValue)
                If value.ToString = "0" Then
                    jValue = CType(True, JValue)
                Else
                    jValue = CType(False, JValue)
                End If
                CType(target, JObject)(name) = jValue
            Else
                CType(target, JObject)(name) = value.ToString()
            End If
        End If
    End Sub
End Class
