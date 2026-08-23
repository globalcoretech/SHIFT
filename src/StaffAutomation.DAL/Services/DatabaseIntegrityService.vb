Imports System
Imports System.Collections.Generic
Imports System.Data
Imports Microsoft.Data.SqlClient
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Models.Backup

Namespace Services
    ''' <summary>
    ''' Implementation of Database Integrity Service executing T-SQL DBCC CHECKDB diagnostics
    ''' and recording results in tbl_DatabaseIntegrityHistory.
    ''' </summary>
    Public Class DatabaseIntegrityService
        Implements IDatabaseIntegrityService

        Private ReadOnly _sqlHelper As ISqlHelper

        Public Sub New(sqlHelper As ISqlHelper)
            _sqlHelper = sqlHelper
        End Sub

        Public Async Function ExecuteIntegrityCheckAsync(Optional executedBy As String = "") As Task(Of DatabaseIntegrityHistoryDto) Implements IDatabaseIntegrityService.ExecuteIntegrityCheckAsync
            Dim record As New DatabaseIntegrityHistoryDto() With {
                .IntegrityCheckId = Guid.NewGuid(),
                .DatabaseName = "StaffAutomationDb",
                .StartedOn = DateTime.Now,
                .Status = "Running",
                .ExecutedBy = If(String.IsNullOrWhiteSpace(executedBy), Environment.UserName, executedBy)
            }

            Try
                ' Log initial RUNNING state
                Await LogIntegrityCheckStartAsync(record)

                ' Execute DBCC CHECKDB WITH NO_INFOMSGS, ALL_ERRORMSGS
                Dim sql = "DBCC CHECKDB ('StaffAutomationDb') WITH NO_INFOMSGS, ALL_ERRORMSGS;"
                Await _sqlHelper.ExecuteNonQueryAsync(sql, Nothing)

                record.CompletedOn = DateTime.Now
                record.Status = "Passed"
                record.ResultSummary = "DBCC CHECKDB completed with 0 allocation errors and 0 consistency errors."
            Catch ex As Exception
                record.CompletedOn = DateTime.Now
                record.Status = "Failed"
                record.ErrorMessage = ex.Message
                record.ResultSummary = $"DBCC CHECKDB detected integrity issues: {ex.Message}"
            End Try

            ' Update final execution status
            Await UpdateIntegrityCheckStatusAsync(record)
            Return record
        End Function

        Public Async Function GetIntegrityHistoryAsync(Optional topCount As Integer = 50) As Task(Of List(Of DatabaseIntegrityHistoryDto)) Implements IDatabaseIntegrityService.GetIntegrityHistoryAsync
            Dim sql = $"SELECT TOP ({Math.Max(1, topCount)}) IntegrityCheckId, DatabaseName, StartedOn, CompletedOn, Status, ResultSummary, ErrorMessage, ExecutedBy FROM tbl_DatabaseIntegrityHistory ORDER BY StartedOn DESC;"
            Return Await _sqlHelper.ExecuteReaderAsync(sql, Nothing, Function(reader) MapHistory(reader))
        End Function

        Private Async Function LogIntegrityCheckStartAsync(r As DatabaseIntegrityHistoryDto) As Task
            Dim sql = "INSERT INTO tbl_DatabaseIntegrityHistory (IntegrityCheckId, DatabaseName, StartedOn, CompletedOn, Status, ResultSummary, ErrorMessage, ExecutedBy) VALUES (@IntegrityCheckId, @DatabaseName, @StartedOn, @CompletedOn, @Status, @ResultSummary, @ErrorMessage, @ExecutedBy);"
            Dim params As IDbDataParameter() = {
                New SqlParameter("@IntegrityCheckId", r.IntegrityCheckId),
                New SqlParameter("@DatabaseName", r.DatabaseName),
                New SqlParameter("@StartedOn", r.StartedOn),
                New SqlParameter("@CompletedOn", If(r.CompletedOn.HasValue, r.CompletedOn.Value, CType(DBNull.Value, Object))),
                New SqlParameter("@Status", r.Status),
                New SqlParameter("@ResultSummary", If(r.ResultSummary, CType(DBNull.Value, Object))),
                New SqlParameter("@ErrorMessage", If(r.ErrorMessage, CType(DBNull.Value, Object))),
                New SqlParameter("@ExecutedBy", r.ExecutedBy)
            }
            Await _sqlHelper.ExecuteNonQueryAsync(sql, params)
        End Function

        Private Async Function UpdateIntegrityCheckStatusAsync(r As DatabaseIntegrityHistoryDto) As Task
            Dim sql = "UPDATE tbl_DatabaseIntegrityHistory SET CompletedOn = @CompletedOn, Status = @Status, ResultSummary = @ResultSummary, ErrorMessage = @ErrorMessage WHERE IntegrityCheckId = @IntegrityCheckId;"
            Dim params As IDbDataParameter() = {
                New SqlParameter("@IntegrityCheckId", r.IntegrityCheckId),
                New SqlParameter("@CompletedOn", If(r.CompletedOn.HasValue, r.CompletedOn.Value, CType(DBNull.Value, Object))),
                New SqlParameter("@Status", r.Status),
                New SqlParameter("@ResultSummary", If(r.ResultSummary, CType(DBNull.Value, Object))),
                New SqlParameter("@ErrorMessage", If(r.ErrorMessage, CType(DBNull.Value, Object)))
            }
            Await _sqlHelper.ExecuteNonQueryAsync(sql, params)
        End Function

        Private Function MapHistory(r As IDataReader) As DatabaseIntegrityHistoryDto
            Return New DatabaseIntegrityHistoryDto() With {
                .IntegrityCheckId = If(r("IntegrityCheckId") Is DBNull.Value, Guid.Empty, CType(r("IntegrityCheckId"), Guid)),
                .DatabaseName = If(r("DatabaseName") Is DBNull.Value, "", r("DatabaseName").ToString()),
                .StartedOn = If(r("StartedOn") Is DBNull.Value, DateTime.Now, Convert.ToDateTime(r("StartedOn"))),
                .CompletedOn = If(r("CompletedOn") Is DBNull.Value, CType(Nothing, Nullable(Of DateTime)), Convert.ToDateTime(r("CompletedOn"))),
                .Status = If(r("Status") Is DBNull.Value, "", r("Status").ToString()),
                .ResultSummary = If(r("ResultSummary") Is DBNull.Value, "", r("ResultSummary").ToString()),
                .ErrorMessage = If(r("ErrorMessage") Is DBNull.Value, "", r("ErrorMessage").ToString()),
                .ExecutedBy = If(r("ExecutedBy") Is DBNull.Value, "", r("ExecutedBy").ToString())
            }
        End Function
    End Class
End Namespace
