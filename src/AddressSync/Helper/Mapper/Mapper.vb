Imports Newtonsoft.Json.Linq

''' <summary>
''' Die Basisklasse des Mappers der generisch mehere Felder von zwei verschiedenen Typen verbindet
''' </summary>
''' <typeparam name="TS">Der erste Typ</typeparam>
''' <typeparam name="TT">Der zweite Typ</typeparam>
Public MustInherit Class Mapper(Of TS, TT)

    ''' <summary>
    ''' Die Einstellungen der Mappings
    ''' </summary>
    Shared ReadOnly Property MappingProperies As List(Of IMappingProperty)
        Get
            Return Nothing
        End Get
    End Property

    ''' <summary>
    ''' Die Werte der Felder des 'TS' Objekts werden in die Felder des 'TT' Objekts geschrieben
    ''' </summary>
    ''' <param name="source">Das Objekt aus dem die Felder gelesen werden</param>
    ''' <param name="target">Das Objekt in das die Felder gespeichert werden</param>
    ''' <returns>Das neu gemappte TT Objekt</returns>
    Public Function Mapp(ByVal source As TS,
                    ByVal target As TT) As TT
        'Lesen der Mappingproperties
        Dim objTarget As Object = CType(target, Object),
            mp As List(Of IMappingProperty) = CType(Me.GetType.GetProperty("MappingProperies").GetValue(Me, Nothing), List(Of IMappingProperty))

        'Iteration durch die Properties (--> PersonMapper bzw. ClubMapper) und ausführung deren 'Mapp' funktion
        target = CType(mp.Aggregate(objTarget, Function(current, mapping) mapping.Mapp(source, current)), TT)

        Return target
    End Function

    ''' <summary>
    ''' Die Werte der Felder des 'TT' Objekts werden in die Felder des 'TS' Objekts geschrieben
    ''' </summary>
    ''' <param name="target">Das Objekt in das die Felder gespeichert werden</param>
    ''' <param name="source">Das Objekt aus dem die Felder gelesen werden</param>
    ''' <returns>Das neu gemappte TS Objekt</returns>
    Public Function DeMapp(ByVal target As TS, ByVal source As TT) As TS
        'Lesen der Mappingproperties
        Dim objTarget As Object = CType(target, Object),
            objSource As TT = source

        ' Holt MappingPropertys
        Dim mp As List(Of IMappingProperty) = CType(Me.GetType.GetProperty("MappingProperies").GetValue(Me, Nothing), List(Of IMappingProperty))

        'Iteration durch die Properties (--> PersonMapper bzw. ClubMapper) und ausführung deren 'DeMapp' funktion
        target = CType(mp.Aggregate(objTarget, Function(current, mapping)
                                                   Return mapping.DeMapp(objSource, current)
                                               End Function), TS)
        Return target
    End Function


End Class
