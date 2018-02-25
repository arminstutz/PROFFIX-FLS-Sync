Imports SMC.Lib

Public Class ProffixConnection
    Friend strConnectString As String = Proffix.Settings.ConnectionString

    Private conn As New ADODB.Connection

    Public Sub New(Optional ByVal connectString As String = "")

        Try
            If String.IsNullOrEmpty(connectString) Then
                conn.ConnectionString = strConnectString
            Else
                conn.ConnectionString = connectString
            End If
            conn.Open()
            If Not conn.State = ADODB.ObjectStateEnum.adStateOpen Then
                Logger.GetInstance.Log(LogLevel.Exception, "Es konnte keine Verbindung hergestellt werdens")

                Exit Sub
            End If
        Catch ex As Exception
            Logger.GetInstance.Log(LogLevel.Exception, ex.Message & ex.StackTrace)
        End Try

    End Sub

    Public Sub close()
        conn.Close()
    End Sub


    Public Function getRecord(ByRef rs As ADODB.Recordset, ByVal sql As String, ByRef Fehler As String, _
                              Optional ByVal looped As Boolean = False) As Boolean
        If conn.State = ADODB.ObjectStateEnum.adStateOpen Then
            Try
                rs.Open(sql, conn)
            Catch ex As Exception
                If Not looped Then
                    Logger.GetInstance.Log(LogLevel.Exception, ex.Message & ex.StackTrace & sql)
                End If
                Fehler = ex.Message
                Return False
            End Try
        Else
            Fehler = "Keine Verbindung vorhanden"
            Return False
        End If

        Return True
    End Function

End Class
