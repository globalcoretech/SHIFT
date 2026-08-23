Imports System
Imports System.Collections.Generic
Imports System.Data
Imports Microsoft.Data.SqlClient
Imports System.Threading.Tasks
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Interfaces

Namespace Repositories
    ''' <summary>
    ''' Data Access Repository querying persisted system audit logs from dbo.tbl_AuditLogs.
    ''' </summary>
    Public Class SecurityAuditRepository
        Private ReadOnly _sqlHelper As ISqlHelper

        Public Sub New(sqlHelper As ISqlHelper)
            _sqlHelper = sqlHelper
        End Sub

        Public Async Function GetAuditLogsAsync(Optional fromDate As Nullable(Of DateTime) = Nothing, Optional toDate As Nullable(Of DateTime) = Nothing, Optional userId As Nullable(Of Integer) = Nothing, Optional moduleName As String = "", Optional actionFilter As String = "", Optional topCount As Integer = 200) As Task(Of List(Of AuditLogEntryDto))
            Dim sqlBuilder As New System.Text.StringBuilder()
            sqlBuilder.Append($"SELECT TOP ({Math.Max(1, topCount)}) a.AuditId, a.UserId, ISNULL(u.Username, 'System') AS Username, ISNULL(u.FullName, 'System User') AS FullName, a.Action, a.ModuleName, a.Details, a.IPAddress, a.Timestamp ")
            sqlBuilder.Append("FROM dbo.tbl_AuditLogs a ")
            sqlBuilder.Append("LEFT JOIN dbo.tbl_Users u ON a.UserId = u.UserId ")
            sqlBuilder.Append("WHERE 1=1 ")

            Dim params As New List(Of SqlParameter)()

            If fromDate.HasValue Then
                sqlBuilder.Append("AND a.Timestamp >= @FromDate ")
                params.Add(New SqlParameter("@FromDate", fromDate.Value))
            End If

            If toDate.HasValue Then
                sqlBuilder.Append("AND a.Timestamp <= @ToDate ")
                params.Add(New SqlParameter("@ToDate", toDate.Value))
            End If

            If userId.HasValue AndAlso userId.Value > 0 Then
                sqlBuilder.Append("AND a.UserId = @UserId ")
                params.Add(New SqlParameter("@UserId", userId.Value))
            End If

            If Not String.IsNullOrWhiteSpace(moduleName) Then
                sqlBuilder.Append("AND a.ModuleName = @ModuleName ")
                params.Add(New SqlParameter("@ModuleName", moduleName.Trim()))
            End If

            If Not String.IsNullOrWhiteSpace(actionFilter) Then
                sqlBuilder.Append("AND a.Action LIKE @ActionFilter ")
                params.Add(New SqlParameter("@ActionFilter", $"%{actionFilter.Trim()}%"))
            End If

            sqlBuilder.Append("ORDER BY a.Timestamp DESC;")

            Return Await _sqlHelper.ExecuteReaderAsync(sqlBuilder.ToString(), params.ToArray(), AddressOf MapAuditEntry)
        End Function

        Public Async Function GetAuditSummaryMetricsAsync() As Task(Of AuditSummaryMetricsDto)
            Dim metrics As New AuditSummaryMetricsDto()
            Try
                Dim todayUtc = DateTime.UtcNow.Date
                Dim countToday = Await _sqlHelper.ExecuteScalarAsync(Of Integer)("SELECT COUNT(1) FROM dbo.tbl_AuditLogs WHERE Timestamp >= @TodayUtc;", New SqlParameter() {New SqlParameter("@TodayUtc", todayUtc)})
                metrics.TodayActivityCount = countToday

                Dim countSecurity = Await _sqlHelper.ExecuteScalarAsync(Of Integer)("SELECT COUNT(1) FROM dbo.tbl_AuditLogs WHERE ModuleName IN ('RolePermissionService', 'UserPermissionOverrideService', 'Security', 'Role Security');", Nothing)
                metrics.SecurityChangesCount = countSecurity

                Dim countFailed = Await _sqlHelper.ExecuteScalarAsync(Of Integer)("SELECT COUNT(1) FROM dbo.tbl_AuditLogs WHERE Action LIKE '%FAIL%' OR Details LIKE '%denied%' OR Details LIKE '%failed%';", Nothing)
                metrics.FailedActionsCount = countFailed

                Dim lastDateObj = Await _sqlHelper.ExecuteScalarAsync(Of Object)("SELECT MAX(Timestamp) FROM dbo.tbl_AuditLogs WHERE ModuleName IN ('RolePermissionService', 'UserPermissionOverrideService', 'Security', 'Role Security');", Nothing)
                If lastDateObj IsNot Nothing AndAlso Not Convert.IsDBNull(lastDateObj) Then
                    metrics.LastSecurityEventOn = Convert.ToDateTime(lastDateObj)
                End If
            Catch ex As Exception
                ' Fail closed gracefully
            End Try
            Return metrics
        End Function

        Private Function MapAuditEntry(r As IDataReader) As AuditLogEntryDto
            Return New AuditLogEntryDto() With {
                .AuditId = Convert.ToInt32(r("AuditId")),
                .UserId = Convert.ToInt32(r("UserId")),
                .Username = Convert.ToString(r("Username")),
                .FullName = Convert.ToString(r("FullName")),
                .Action = Convert.ToString(r("Action")),
                .ModuleName = Convert.ToString(r("ModuleName")),
                .Details = Convert.ToString(r("Details")),
                .IPAddress = If(r("IPAddress") Is DBNull.Value, "", Convert.ToString(r("IPAddress"))),
                .Timestamp = Convert.ToDateTime(r("Timestamp")),
                .Status = If(Convert.ToString(r("Details")).Contains("FAILED") OrElse Convert.ToString(r("Action")).Contains("DENY"), "Denied", "Success")
            }
        End Function
    End Class
End Namespace
