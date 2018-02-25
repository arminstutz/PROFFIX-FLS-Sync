Imports System.Text

Imports Microsoft.VisualStudio.TestTools.UnitTesting

Imports SMC
Imports SMC.Lib

<TestClass()> Public Class UnitTest

    Private Property Proffix As Proffix

    ''' <summary>
    ''' Testen des Lesen der Konfigurationsdateien und der Verbindung zu PROFFIX
    ''' </summary>
    ''' <remarks></remarks>
    <TestMethod()> Public Sub TestConnectToProffix()
        'PROFFIX Initialisieren
        Me.Proffix = New Proffix("23sdf-ds829-affea-32444", "FlsGliderSync", Environment.CurrentDirectory + "\")
        'Konfigurationsdateien lesen
        If Not Proffix.Settings.IsLoaded Then
            Proffix.Settings.Load()
        End If

        'Die Verbindung zu PROFFIX wird aufgebaut
        Proffix.Settings.DefaultDataBase = "PXSGN01"
        Proffix.LoadConnection = True

        Assert.IsTrue(Proffix.Open())
    End Sub

End Class