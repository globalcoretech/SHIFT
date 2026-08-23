Imports System
Imports Microsoft.Data.SqlClient
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Interfaces

Namespace Core
    ''' <summary>
    ''' Implementation of SQL Transient Retry Policy executing operations up to 3 times
    ''' with exponential backoff and jitter on transient SQL network, deadlock, or service restart errors.
    ''' </summary>
    Public Class SqlTransientRetryPolicy
        Implements ISqlTransientRetryPolicy

        Private Shared ReadOnly _random As New Random()

        Public Async Function ExecuteAsync(Of T)(operation As Func(Of Task(Of T))) As Task(Of T) Implements ISqlTransientRetryPolicy.ExecuteAsync
            Dim maxAttempts As Integer = 3
            Dim lastException As Exception = Nothing

            For attempt As Integer = 1 To maxAttempts
                Dim shouldDelay As Boolean = False
                Dim delayMs As Integer = 0

                Try
                    Return Await operation()
                Catch ex As Exception
                    lastException = ex
                    If Not IsTransientException(ex) OrElse attempt >= maxAttempts Then
                        Throw
                    End If

                    ' Exponential backoff with jitter (100ms * 2^(attempt-1) + random 10-50ms)
                    delayMs = CInt(Math.Pow(2, attempt - 1) * 100) + _random.Next(10, 50)
                    shouldDelay = True
                End Try

                If shouldDelay Then
                    Await Task.Delay(delayMs)
                End If
            Next

            If lastException IsNot Nothing Then
                Throw lastException
            End If
            Return CType(Nothing, T)
        End Function

        Public Function IsTransientException(ex As Exception) As Boolean Implements ISqlTransientRetryPolicy.IsTransientException
            If ex Is Nothing Then Return False

            Dim sqlEx As SqlException = TryCast(ex, SqlException)
            If sqlEx Is Nothing AndAlso ex.InnerException IsNot Nothing Then
                sqlEx = TryCast(ex.InnerException, SqlException)
            End If

            If sqlEx IsNot Nothing Then
                For Each err As SqlError In sqlEx.Errors
                    Select Case err.Number
                        Case 1205 ' Deadlock victim
                            Return True
                        Case 40613 ' Database unavailable / restarting
                            Return True
                        Case 40197, 40501 ' Service processing request / throttling
                            Return True
                        Case 10054, 10060, 233, 64 ' Transport level connection reset / network drop
                            Return True
                        Case -2 ' Timeout
                            Return True
                    End Select
                Next
            End If

            Return False
        End Function
    End Class
End Namespace
