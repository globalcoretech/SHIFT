Imports System

Namespace DTOs
    ''' <summary>
    ''' Data Transfer Object representing a system security audit log record.
    ''' </summary>
    Public Class AuditLogEntryDto
        Public Property AuditId As Integer
        Public Property UserId As Integer
        Public Property Username As String = String.Empty
        Public Property FullName As String = String.Empty
        Public Property Action As String = String.Empty
        Public Property ModuleName As String = String.Empty
        Public Property Details As String = String.Empty
        Public Property IPAddress As String = String.Empty
        Public Property Timestamp As DateTime = DateTime.UtcNow
        Public Property Status As String = "Success"
    End Class

    Public Class AuditSummaryMetricsDto
        Public Property TodayActivityCount As Integer = 0
        Public Property SecurityChangesCount As Integer = 0
        Public Property FailedActionsCount As Integer = 0
        Public Property LastSecurityEventOn As Nullable(Of DateTime)
    End Class
End Namespace
