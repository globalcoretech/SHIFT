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
        Public Property InternalMessage As String = String.Empty
        Public Property UserFriendlyMessage As String = "Database operation could not be completed. Please try again or contact the administrator."

        Public Sub New()
            MyBase.New("Database operation could not be completed. Please try again or contact the administrator.")
            InternalMessage = "Database operation failure."
        End Sub

        Public Sub New(message As String)
            MyBase.New("Database operation could not be completed. Please try again or contact the administrator.")
            InternalMessage = message
        End Sub

        Public Sub New(message As String, innerException As Exception)
            MyBase.New("Database operation could not be completed. Please try again or contact the administrator.", innerException)
            InternalMessage = message
            If innerException IsNot Nothing Then
                Dim sqlEx = TryCast(innerException, SqlException)
                If sqlEx IsNot Nothing Then
                    SqlErrorNumber = sqlEx.Number
                End If
            End If
        End Sub
    End Class
End Namespace
