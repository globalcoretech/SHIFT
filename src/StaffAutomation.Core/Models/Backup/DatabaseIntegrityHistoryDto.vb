Imports System

Namespace Models.Backup
    ''' <summary>
    ''' Data Transfer Object representing a database integrity check execution record in tbl_DatabaseIntegrityHistory.
    ''' </summary>
    Public Class DatabaseIntegrityHistoryDto
        Public Property IntegrityCheckId As Guid = Guid.NewGuid()
        Public Property DatabaseName As String = "StaffAutomationDb"
        Public Property StartedOn As DateTime = DateTime.Now
        Public Property CompletedOn As Nullable(Of DateTime)
        Public Property Status As String = "Running" ' Running, Passed, Failed
        Public Property ResultSummary As String = String.Empty
        Public Property ErrorMessage As String = String.Empty
        Public Property ExecutedBy As String = Environment.UserName
    End Class
End Namespace
