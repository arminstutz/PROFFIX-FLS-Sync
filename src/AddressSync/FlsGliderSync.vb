Imports pxBook

Imports SMC

''' <summary>
''' Die Globalen Attribute der Applikation
''' </summary>
Public Module FlsGliderSync

    ''' <summary>
    ''' Die Verbindung zu PROFFIX
    ''' </summary>
    Friend Property Proffix As Proffix = New Proffix(ProffixCrypto.Decrypt(readFromIni("licenseKey"), My.Settings.Crypto), "FlsGliderSync", Application.StartupPath + "\")

    ''' <summary>
    ''' Die Helper klasse für die Verschlüsselung
    ''' </summary>
    Friend Property ProffixCrypto As ProffixCrypto = ProffixCrypto

    ' je nach Angabe in ini (1 = ausführlich, 0/"" = nicht ausführlich)
    Friend Property logAusfuehrlich As Boolean = GeneralHelper.logAusfuehrlichSchreiben()

    ' dieser Postfix wird in FLS an MemberNr angehängt, wenn Adresse nur noch in FLS und in PX ganz gelöscht wurde
    Friend Property postfix As String = "_delInPX"

    ' definiert, welche DB bei der 1. Synchronisation als Master gelten soll.
    ' wenn im FlsGliderSync.ini master=fls oder master=proffix steht, ist die entsprechende DB master. ansonsten wird je nach Änderungsdatum geupdatet
    Friend Property Master As UseAsMaster

End Module

Public Enum UseAsMaster
    fls
    proffix
    undefined
End Enum
