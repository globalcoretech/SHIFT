Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.DAL.Repositories

Namespace Services
    ''' <summary>
    ''' Business Logic Service for Security Audit reporting and summary metrics.
    ''' </summary>
    Public Class SecurityAuditService
        Implements ISecurityAuditService

        Private ReadOnly _auditRepo As SecurityAuditRepository

        Public Sub New(auditRepo As SecurityAuditRepository)
            _auditRepo = auditRepo
        End Sub

        Public Async Function GetAuditLogsAsync(Optional fromDate As Nullable(Of DateTime) = Nothing, Optional toDate As Nullable(Of DateTime) = Nothing, Optional userId As Nullable(Of Integer) = Nothing, Optional moduleName As String = "", Optional actionFilter As String = "", Optional topCount As Integer = 200) As Task(Of List(Of AuditLogEntryDto)) Implements ISecurityAuditService.GetAuditLogsAsync
            Return Await _auditRepo.GetAuditLogsAsync(fromDate, toDate, userId, moduleName, actionFilter, topCount)
        End Function

        Public Async Function GetAuditSummaryMetricsAsync() As Task(Of AuditSummaryMetricsDto) Implements ISecurityAuditService.GetAuditSummaryMetricsAsync
            Return Await _auditRepo.GetAuditSummaryMetricsAsync()
        End Function
    End Class
End Namespace
