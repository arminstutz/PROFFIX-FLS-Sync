﻿'------------------------------------------------------------------------------
' <auto-generated>
'     Dieser Code wurde von einem Tool generiert.
'     Laufzeitversion:4.0.30319.42000
'
'     Änderungen an dieser Datei können falsches Verhalten verursachen und gehen verloren, wenn
'     der Code erneut generiert wird.
' </auto-generated>
'------------------------------------------------------------------------------

Option Strict On
Option Explicit On


Namespace My
    
    <Global.System.Runtime.CompilerServices.CompilerGeneratedAttribute(),  _
     Global.System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "15.7.0.0"),  _
     Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Advanced)>  _
    Partial Friend NotInheritable Class MySettings
        Inherits Global.System.Configuration.ApplicationSettingsBase
        
        Private Shared defaultInstance As MySettings = CType(Global.System.Configuration.ApplicationSettingsBase.Synchronized(New MySettings()),MySettings)
        
#Region "Automatische My.Settings-Speicherfunktion"
#If _MyType = "WindowsForms" Then
    Private Shared addedHandler As Boolean

    Private Shared addedHandlerLockObject As New Object

    <Global.System.Diagnostics.DebuggerNonUserCodeAttribute(), Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Advanced)> _
    Private Shared Sub AutoSaveSettings(sender As Global.System.Object, e As Global.System.EventArgs)
        If My.Application.SaveMySettingsOnExit Then
            My.Settings.Save()
        End If
    End Sub
#End If
#End Region
        
        Public Shared ReadOnly Property [Default]() As MySettings
            Get
                
#If _MyType = "WindowsForms" Then
               If Not addedHandler Then
                    SyncLock addedHandlerLockObject
                        If Not addedHandler Then
                            AddHandler My.Application.Shutdown, AddressOf AutoSaveSettings
                            addedHandler = True
                        End If
                    End SyncLock
                End If
#End If
                Return defaultInstance
            End Get
        End Property
        
        <Global.System.Configuration.UserScopedSettingAttribute(),  _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
         Global.System.Configuration.DefaultSettingValueAttribute("szik5fzsEWA=")>  _
        Public Property Password() As String
            Get
                Return CType(Me("Password"),String)
            End Get
            Set
                Me("Password") = value
            End Set
        End Property
        
        <Global.System.Configuration.UserScopedSettingAttribute(),  _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
         Global.System.Configuration.DefaultSettingValueAttribute("")>  _
        Public Property Username() As String
            Get
                Return CType(Me("Username"),String)
            End Get
            Set
                Me("Username") = value
            End Set
        End Property
        
        <Global.System.Configuration.UserScopedSettingAttribute(),  _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
         Global.System.Configuration.DefaultSettingValueAttribute("smc%espo$")>  _
        Public Property Crypto() As String
            Get
                Return CType(Me("Crypto"),String)
            End Get
            Set
                Me("Crypto") = value
            End Set
        End Property
        
        <Global.System.Configuration.UserScopedSettingAttribute(),  _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
         Global.System.Configuration.DefaultSettingValueAttribute("https://test.glider-fls.ch/Token")>  _
        Public Property ServiceAPITokenMethod() As String
            Get
                Return CType(Me("ServiceAPITokenMethod"),String)
            End Get
            Set
                Me("ServiceAPITokenMethod") = value
            End Set
        End Property
        
        <Global.System.Configuration.UserScopedSettingAttribute(),  _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
         Global.System.Configuration.DefaultSettingValueAttribute("https://test.glider-fls.ch/api/v1/persons/")>  _
        Public Property ServiceAPIPersonMethod() As String
            Get
                Return CType(Me("ServiceAPIPersonMethod"),String)
            End Get
            Set
                Me("ServiceAPIPersonMethod") = value
            End Set
        End Property
        
        <Global.System.Configuration.UserScopedSettingAttribute(),  _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
         Global.System.Configuration.DefaultSettingValueAttribute("https://test.glider-fls.ch/api/v1/deliveries/notprocessed")>  _
        Public Property ServiceAPIDeliveriesNotProcessedMethod() As String
            Get
                Return CType(Me("ServiceAPIDeliveriesNotProcessedMethod"),String)
            End Get
            Set
                Me("ServiceAPIDeliveriesNotProcessedMethod") = value
            End Set
        End Property
        
        <Global.System.Configuration.UserScopedSettingAttribute(),  _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
         Global.System.Configuration.DefaultSettingValueAttribute("https://test.glider-fls.ch/api/v1/persons/fulldetails/deleted/")>  _
        Public Property ServiceAPIDeletedPersonFulldetailsMethod() As String
            Get
                Return CType(Me("ServiceAPIDeletedPersonFulldetailsMethod"),String)
            End Get
            Set
                Me("ServiceAPIDeletedPersonFulldetailsMethod") = value
            End Set
        End Property
        
        <Global.System.Configuration.UserScopedSettingAttribute(),  _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
         Global.System.Configuration.DefaultSettingValueAttribute("https://test.glider-fls.ch/api/v1/persons/fulldetails/modified/")>  _
        Public Property ServiceAPIModifiedPersonFullDetailsMethod() As String
            Get
                Return CType(Me("ServiceAPIModifiedPersonFullDetailsMethod"),String)
            End Get
            Set
                Me("ServiceAPIModifiedPersonFullDetailsMethod") = value
            End Set
        End Property
        
        <Global.System.Configuration.UserScopedSettingAttribute(),  _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
         Global.System.Configuration.DefaultSettingValueAttribute("False")>  _
        Public Property ShowSameFields() As Boolean
            Get
                Return CType(Me("ShowSameFields"),Boolean)
            End Get
            Set
                Me("ShowSameFields") = value
            End Set
        End Property
        
        <Global.System.Configuration.UserScopedSettingAttribute(),  _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
         Global.System.Configuration.DefaultSettingValueAttribute("https://test.glider-fls.ch/api/v1/countries/overview")>  _
        Public Property ServiceAPICountriesMethod() As String
            Get
                Return CType(Me("ServiceAPICountriesMethod"),String)
            End Get
            Set
                Me("ServiceAPICountriesMethod") = value
            End Set
        End Property
        
        <Global.System.Configuration.UserScopedSettingAttribute(),  _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
         Global.System.Configuration.DefaultSettingValueAttribute("https://test.glider-fls.ch/api/v1/articles/")>  _
        Public Property ServiceAPIArticlesMethod() As String
            Get
                Return CType(Me("ServiceAPIArticlesMethod"),String)
            End Get
            Set
                Me("ServiceAPIArticlesMethod") = value
            End Set
        End Property
        
        <Global.System.Configuration.UserScopedSettingAttribute(),  _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
         Global.System.Configuration.DefaultSettingValueAttribute("https://test.glider-fls.ch/api/v1/persons/membernumber/")>  _
        Public Property ServiceAPIPersonsMemberNrMethod() As String
            Get
                Return CType(Me("ServiceAPIPersonsMemberNrMethod"),String)
            End Get
            Set
                Me("ServiceAPIPersonsMemberNrMethod") = value
            End Set
        End Property
        
        <Global.System.Configuration.UserScopedSettingAttribute(),  _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
         Global.System.Configuration.DefaultSettingValueAttribute("https://test.glider-fls.ch/api/v1/memberstates")>  _
        Public Property ServiceAPIMemberStates() As String
            Get
                Return CType(Me("ServiceAPIMemberStates"),String)
            End Get
            Set
                Me("ServiceAPIMemberStates") = value
            End Set
        End Property
        
        <Global.System.Configuration.UserScopedSettingAttribute(),  _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
         Global.System.Configuration.DefaultSettingValueAttribute("https://test.glider-fls.ch/api/v1/deliveries/delivered")>  _
        Public Property ServiceAPIDeliveredMethod() As String
            Get
                Return CType(Me("ServiceAPIDeliveredMethod"),String)
            End Get
            Set
                Me("ServiceAPIDeliveredMethod") = value
            End Set
        End Property
        
        <Global.System.Configuration.UserScopedSettingAttribute(),  _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
         Global.System.Configuration.DefaultSettingValueAttribute("https://test.glider-fls.ch/api/v1/flights/exchange/modified/")>  _
        Public Property ServiceAPIModifiedFlightsMethod() As String
            Get
                Return CType(Me("ServiceAPIModifiedFlightsMethod"),String)
            End Get
            Set
                Me("ServiceAPIModifiedFlightsMethod") = value
            End Set
        End Property
        
        <Global.System.Configuration.UserScopedSettingAttribute(),  _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
         Global.System.Configuration.DefaultSettingValueAttribute("https://test.glider-fls.ch/api/v1/aircrafts/overview")>  _
        Public Property ServiceAPIAircraftsMethod() As String
            Get
                Return CType(Me("ServiceAPIAircraftsMethod"),String)
            End Get
            Set
                Me("ServiceAPIAircraftsMethod") = value
            End Set
        End Property
    End Class
End Namespace

Namespace My
    
    <Global.Microsoft.VisualBasic.HideModuleNameAttribute(),  _
     Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
     Global.System.Runtime.CompilerServices.CompilerGeneratedAttribute()>  _
    Friend Module MySettingsProperty
        
        <Global.System.ComponentModel.Design.HelpKeywordAttribute("My.Settings")>  _
        Friend ReadOnly Property Settings() As Global.FlsGliderSync.My.MySettings
            Get
                Return Global.FlsGliderSync.My.MySettings.Default
            End Get
        End Property
    End Module
End Namespace
