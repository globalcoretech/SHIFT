Imports System
Imports System.Diagnostics
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Common
Imports StaffAutomation.Core.Interfaces

Namespace Core
    ''' <summary>
    ''' Health Checker service verifying SQL Server Express LAN connectivity and latency.
    ''' </summary>
    Public Class DatabaseHealthChecker
        Private ReadOnly _sqlHelper As ISqlHelper

        Public Sub New(sqlHelper As ISqlHelper)
            _sqlHelper = sqlHelper
        End Sub

        Public Async Function CheckHealthAsync() As Task(Of OperationResult(Of Double))
            Dim sw As Stopwatch = Stopwatch.StartNew()
            Try
                Dim testResult = Await _sqlHelper.ExecuteScalarAsync(Of Integer)("SELECT 1;", Nothing)
                sw.Stop()
                If testResult = 1 Then
                    Return OperationResult(Of Double).Success(sw.Elapsed.TotalMilliseconds, $"SQL Server connectivity verified. Latency: {sw.Elapsed.TotalMilliseconds:F2} ms.")
                Else
                    Return OperationResult(Of Double).Failure("SQL Server health check returned unexpected result.")
                End If
            Catch ex As Exception
                sw.Stop()
                Return OperationResult(Of Double).Failure("SQL Server health check failed: " & ex.Message, ex)
            End Try
        End Function
    End Class
End Namespace
