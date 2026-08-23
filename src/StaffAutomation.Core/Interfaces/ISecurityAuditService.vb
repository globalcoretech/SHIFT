Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.Core.DTOs

Namespace Interfaces
    ''' <summary>
    ''' Service interface for Security and System Audit Trail reporting and filtering.
    ''' </summary>
    Public Interface ISecurityAuditService
        Function GetAuditLogsAsync(Optional fromDate As Nullable(Of DateTime) = Nothing, Optional toDate As Nullable(Of DateTime) = Nothing, Optional userId As Nullable(Of Integer) = Nothing, Optional moduleName As String = "", Optional actionFilter As String = "", Optional topCount As Integer = 200) As Task(Of List(Of AuditLogEntryDto))
        Function GetAuditSummaryMetricsAsync() As Task(Of AuditSummaryMetricsDto)
    End Interface
End Namespace
