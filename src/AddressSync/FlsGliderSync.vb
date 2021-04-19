Imports pxBook

Imports SMC

''' <summary>
''' Die Globalen Attribute der Applikation
''' </summary>
Public Module FlsGliderSync

    ''' <summary>
    ''' Die Verbindung zu PROFFIX
    ''' </summary>
    Friend Property Proffix As Proffix = New Proffix("25sdf-ds829-artea-32666", "FlsGliderSync", Application.StartupPath + "\")
    'Für Proffix V4.1007: Public Property Proffix As Proffix = New Proffix("23sdf-ds829-affea-32444", "FlsGliderSync", Application.StartupPath + "\")

    ''' <summary>
    ''' Die Helper klasse für die Verschlüsselung
    ''' </summary>
    Friend Property ProffixCrypto As ProffixCrypto = ProffixCrypto

    ' je nach Angabe in ini (1 = ausführlich, 0/"" = nicht ausführlich)
    Friend Property LogAusfuehrlich As Boolean = GeneralHelper.logAusfuehrlichSchreiben()

    ' dieser Postfix wird in FLS an MemberNr angehängt, wenn Adresse nur noch in FLS und in PX ganz gelöscht wurde
    Friend Property Postfix As String = "_delInPX"


End Module
