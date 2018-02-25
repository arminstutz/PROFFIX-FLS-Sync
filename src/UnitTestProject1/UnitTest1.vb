Imports System.Text
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports SMC
'Imports FlsGliderSync.Helper

<TestClass()> Public Class UnitTest1

    Private Property Proffix As Proffix

    ''' <summary>
    ''' Testen des Lesen der Konfigurationsdateien und der Verbindung zu PROFFIX
    ''' </summary>
    ''' <remarks></remarks>
    <TestMethod()> Public Sub TestConnectToProffix()
        'PROFFIX Initialisieren
        Me.Proffix = New Proffix("25sdf-ds829-artea-32666", "FlsGliderSync", Environment.CurrentDirectory + "\")
        ' Application.StartupPath + "\")
        'Konfigurationsdateien lesen
        If Not Proffix.Settings.IsLoaded Then
            Proffix.Settings.Load()
        End If

        'Die Verbindung zu PROFFIX wird aufgebaut
        Proffix.Settings.DefaultDataBase = "TESTFLS"
        Proffix.LoadConnection = True

        Assert.IsTrue(Proffix.Open())
    End Sub

    <TestMethod()>
    Public Sub Test1()
        Assert.AreEqual("hall", "hallo")
    End Sub

    <TestMethod()>
    Public Sub test2()
        ' Dim generalHelper As New Helper.GeneralHelper

        'Assert.Equals(Now, generalhelper.GetSinceDate(Now))

        'Assert.IsTrue(generalhelper.GetSinceDate(Now) = DateTime.minvalue
    End Sub
End Class