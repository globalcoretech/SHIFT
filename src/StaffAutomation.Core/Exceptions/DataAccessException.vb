Imports System
Imports Microsoft.Data.SqlClient

Namespace Exceptions
    ''' <summary>
    ''' Exception thrown by DAL when database connectivity or SQL queries fail.
    ''' Sanitizes user-facing messages while retaining structured technical details (CorrelationId, SqlErrorNumber, StackTrace).
    ''' </summary>
    Public Class DataAccessException
        Inherits StaffAutomationException

        Public Property CorrelationId As String = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()
        Public Property SqlErrorNumber As Integer = 0
        Public Property SqlClass As Byte = 0
        Public Property SqlState As String = String.Empty
        Public Property InternalMessage As String = String.Empty
        Public Property UserFriendlyMessage As String = "Database operation could not be completed. Please try again or contact the administrator."

        Public Sub New()
            MyBase.New("Database operation could not be completed. Please try again or contact the administrator.")
            InternalMessage = "Database operation failure."
        End Sub

        Public Sub New(message As String)
            MyBase.New(message)
            InternalMessage = message
        End Sub

        Public Sub New(message As String, innerException As Exception)
            MyBase.New(message, innerException)
            InternalMessage = message
            If innerException IsNot Nothing Then
                Dim sqlEx = TryCast(innerException, SqlException)
                If sqlEx IsNot Nothing Then
                    SqlErrorNumber = sqlEx.Number
                    SqlClass = sqlEx.Class
                    SqlState = sqlEx.State.ToString()
                End If
            End If
        End Sub

        Public Function GetDiagnosticTrace(repositoryMethod As String, taskId As Integer, userId As Integer, timeCategoryId As Integer) As String
            Dim innerChain As String = If(InnerException IsNot Nothing, $"{InnerException.GetType().FullName}: {InnerException.Message}", "None")
            If InnerException IsNot Nothing AndAlso InnerException.InnerException IsNot Nothing Then
                innerChain &= $" ---> {InnerException.InnerException.GetType().FullName}: {InnerException.InnerException.Message}"
            End If

            Return $"[TimerTrace]" & vbCrLf &
                   $"CorrelationId      : {CorrelationId}" & vbCrLf &
                   $"TaskId             : {taskId}" & vbCrLf &
                   $"CurrentUserId      : {userId}" & vbCrLf &
                   $"TimeCategoryId     : {timeCategoryId}" & vbCrLf &
                   $"RepositoryMethod   : {repositoryMethod}" & vbCrLf &
                   $"InternalMessage    : {InternalMessage}" & vbCrLf &
                   $"SqlErrorNumber     : {SqlErrorNumber}" & vbCrLf &
                   $"SqlClass           : {SqlClass}" & vbCrLf &
                   $"SqlState           : {SqlState}" & vbCrLf &
                   $"ExceptionType      : {Me.GetType().FullName}" & vbCrLf &
                   $"InnerExceptionChain: {innerChain}" & vbCrLf &
                   $"StackTrace         : {StackTrace}"
        End Function
    End Class
End Namespace
