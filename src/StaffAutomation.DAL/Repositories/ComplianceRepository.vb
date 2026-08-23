Imports System
Imports System.Collections.Generic
Imports System.Data
Imports Microsoft.Data.SqlClient
Imports System.Threading.Tasks
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Interfaces

Namespace Repositories
    ''' <summary>
    ''' Data Access Repository for Compliance Automation rules, alert schedules, and history.
    ''' </summary>
    Public Class ComplianceRepository
        Private ReadOnly _sqlHelper As ISqlHelper

        Public Sub New(sqlHelper As ISqlHelper)
            _sqlHelper = sqlHelper
        End Sub

        Public Async Function GetAllRulesAsync(Optional includeInactive As Boolean = False) As Task(Of List(Of ComplianceRuleDto))
            Dim sql = "SELECT ComplianceRuleId, ComplianceName, Category, Frequency, DueDateRuleDay, DueDateRuleMonth, Priority, IsActive, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy " &
                      "FROM dbo.tbl_ComplianceRules " &
                      If(includeInactive, "", "WHERE IsActive = 1 ") &
                      "ORDER BY ComplianceName ASC;"

            Dim rules = Await _sqlHelper.ExecuteReaderAsync(sql, Nothing, AddressOf MapRule)
            For Each r In rules
                r.AlertSchedules = Await GetSchedulesForRuleAsync(r.ComplianceRuleId)
            Next
            Return rules
        End Function

        Public Async Function GetRuleByIdAsync(ruleId As Integer) As Task(Of ComplianceRuleDto)
            Dim sql = "SELECT ComplianceRuleId, ComplianceName, Category, Frequency, DueDateRuleDay, DueDateRuleMonth, Priority, IsActive, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy " &
                      "FROM dbo.tbl_ComplianceRules WHERE ComplianceRuleId = @RuleId;"
            Dim params As SqlParameter() = {New SqlParameter("@RuleId", ruleId)}
            Dim list = Await _sqlHelper.ExecuteReaderAsync(sql, params, AddressOf MapRule)
            If list.Count = 0 Then Return Nothing

            Dim rule = list(0)
            rule.AlertSchedules = Await GetSchedulesForRuleAsync(rule.ComplianceRuleId)
            Return rule
        End Function

        Public Async Function SaveRuleAsync(rule As ComplianceRuleDto, modifiedBy As Integer) As Task(Of Boolean)
            If rule.ComplianceRuleId <= 0 Then
                ' Insert Rule
                Const insertSql As String = "INSERT INTO dbo.tbl_ComplianceRules (ComplianceName, Category, Frequency, DueDateRuleDay, DueDateRuleMonth, Priority, IsActive, CreatedBy) " &
                                           "VALUES (@Name, @Category, @Frequency, @Day, @Month, @Priority, @IsActive, @CreatedBy); SELECT SCOPE_IDENTITY();"
                Dim params As SqlParameter() = {
                    New SqlParameter("@Name", rule.ComplianceName.Trim()),
                    New SqlParameter("@Category", rule.Category),
                    New SqlParameter("@Frequency", rule.Frequency),
                    New SqlParameter("@Day", rule.DueDateRuleDay),
                    New SqlParameter("@Month", If(rule.DueDateRuleMonth.HasValue, rule.DueDateRuleMonth.Value, CObj(DBNull.Value))),
                    New SqlParameter("@Priority", rule.Priority),
                    New SqlParameter("@IsActive", rule.IsActive),
                    New SqlParameter("@CreatedBy", modifiedBy)
                }

                Dim newIdObj = Await _sqlHelper.ExecuteScalarAsync(Of Object)(insertSql, params)
                If newIdObj IsNot Nothing AndAlso Not Convert.IsDBNull(newIdObj) Then
                    rule.ComplianceRuleId = Convert.ToInt32(newIdObj)
                    Await SaveSchedulesAsync(rule.ComplianceRuleId, rule.AlertSchedules)
                    Return True
                End If
                Return False
            Else
                ' Update Rule
                Const updateSql As String = "UPDATE dbo.tbl_ComplianceRules SET ComplianceName = @Name, Category = @Category, Frequency = @Frequency, DueDateRuleDay = @Day, DueDateRuleMonth = @Month, Priority = @Priority, IsActive = @IsActive, ModifiedOn = GETUTCDATE(), ModifiedBy = @ModifiedBy WHERE ComplianceRuleId = @RuleId;"
                Dim params As SqlParameter() = {
                    New SqlParameter("@RuleId", rule.ComplianceRuleId),
                    New SqlParameter("@Name", rule.ComplianceName.Trim()),
                    New SqlParameter("@Category", rule.Category),
                    New SqlParameter("@Frequency", rule.Frequency),
                    New SqlParameter("@Day", rule.DueDateRuleDay),
                    New SqlParameter("@Month", If(rule.DueDateRuleMonth.HasValue, rule.DueDateRuleMonth.Value, CObj(DBNull.Value))),
                    New SqlParameter("@Priority", rule.Priority),
                    New SqlParameter("@IsActive", rule.IsActive),
                    New SqlParameter("@ModifiedBy", modifiedBy)
                }

                Dim rows = Await _sqlHelper.ExecuteNonQueryAsync(updateSql, params)
                If rows > 0 Then
                    Await SaveSchedulesAsync(rule.ComplianceRuleId, rule.AlertSchedules)
                    Return True
                End If
                Return False
            End If
        End Function

        Public Async Function ToggleRuleStateAsync(ruleId As Integer, isActive As Boolean, modifiedBy As Integer) As Task(Of Boolean)
            Const sql As String = "UPDATE dbo.tbl_ComplianceRules SET IsActive = @IsActive, ModifiedOn = GETUTCDATE(), ModifiedBy = @ModifiedBy WHERE ComplianceRuleId = @RuleId;"
            Dim params As SqlParameter() = {
                New SqlParameter("@RuleId", ruleId),
                New SqlParameter("@IsActive", isActive),
                New SqlParameter("@ModifiedBy", modifiedBy)
            }
            Return (Await _sqlHelper.ExecuteNonQueryAsync(sql, params)) > 0
        End Function

        Public Async Function GetSchedulesForRuleAsync(ruleId As Integer) As Task(Of List(Of ComplianceAlertScheduleDto))
            Dim sql = "SELECT AlertScheduleId, ComplianceRuleId, DaysBeforeDeadline, AlertType, IsEnabled FROM dbo.tbl_ComplianceAlertSchedules WHERE ComplianceRuleId = @RuleId ORDER BY DaysBeforeDeadline DESC;"
            Dim params As SqlParameter() = {New SqlParameter("@RuleId", ruleId)}
            Return Await _sqlHelper.ExecuteReaderAsync(sql, params, AddressOf MapSchedule)
        End Function

        Private Async Function SaveSchedulesAsync(ruleId As Integer, schedules As List(Of ComplianceAlertScheduleDto)) As Task
            Const delSql As String = "DELETE FROM dbo.tbl_ComplianceAlertSchedules WHERE ComplianceRuleId = @RuleId;"
            Dim delParams As SqlParameter() = {New SqlParameter("@RuleId", ruleId)}
            Await _sqlHelper.ExecuteNonQueryAsync(delSql, delParams)

            If schedules IsNot Nothing Then
                For Each s In schedules
                    Const insSql As String = "INSERT INTO dbo.tbl_ComplianceAlertSchedules (ComplianceRuleId, DaysBeforeDeadline, AlertType, IsEnabled) VALUES (@RuleId, @Days, @Type, @IsEnabled);"
                    Dim insParams As SqlParameter() = {
                        New SqlParameter("@RuleId", ruleId),
                        New SqlParameter("@Days", s.DaysBeforeDeadline),
                        New SqlParameter("@Type", s.AlertType),
                        New SqlParameter("@IsEnabled", s.IsEnabled)
                    }
                    Await _sqlHelper.ExecuteNonQueryAsync(insSql, insParams)
                Next
            End If
        End Function

        Public Async Function HasAutomationExecutedAsync(ruleId As Integer, periodName As String) As Task(Of Boolean)
            Const sql As String = "SELECT COUNT(1) FROM dbo.tbl_ComplianceAutomationHistory WHERE ComplianceRuleId = @RuleId AND CompliancePeriod = @Period;"
            Dim params As SqlParameter() = {
                New SqlParameter("@RuleId", ruleId),
                New SqlParameter("@Period", periodName)
            }
            Dim count = Await _sqlHelper.ExecuteScalarAsync(Of Integer)(sql, params)
            Return count > 0
        End Function

        Public Async Function RecordAutomationHistoryAsync(hist As ComplianceAutomationHistoryDto) As Task(Of Integer)
            Const sql As String = "INSERT INTO dbo.tbl_ComplianceAutomationHistory (ComplianceRuleId, CompliancePeriod, DeadlineDate, TriggerDate, TaskId, AlertGenerated, TaskGenerated, Status) " &
                                 "VALUES (@RuleId, @Period, @Deadline, @Trigger, @TaskId, @AlertGen, @TaskGen, @Status); SELECT SCOPE_IDENTITY();"
            Dim params As SqlParameter() = {
                New SqlParameter("@RuleId", hist.ComplianceRuleId),
                New SqlParameter("@Period", hist.CompliancePeriod),
                New SqlParameter("@Deadline", hist.DeadlineDate),
                New SqlParameter("@Trigger", hist.TriggerDate),
                New SqlParameter("@TaskId", If(hist.TaskId.HasValue, hist.TaskId.Value, CObj(DBNull.Value))),
                New SqlParameter("@AlertGen", hist.AlertGenerated),
                New SqlParameter("@TaskGen", hist.TaskGenerated),
                New SqlParameter("@Status", hist.Status)
            }

            Dim res = Await _sqlHelper.ExecuteScalarAsync(Of Object)(sql, params)
            If res IsNot Nothing AndAlso Not Convert.IsDBNull(res) Then
                Return Convert.ToInt32(res)
            End If
            Return 0
        End Function

        Public Async Function GetHistoryAsync(Optional topCount As Integer = 50) As Task(Of List(Of ComplianceAutomationHistoryDto))
            Dim sql = $"SELECT TOP ({Math.Max(1, topCount)}) h.HistoryId, h.ComplianceRuleId, r.ComplianceName, h.CompliancePeriod, h.DeadlineDate, h.TriggerDate, h.TaskId, h.AlertGenerated, h.TaskGenerated, h.Status, h.CreatedOn " &
                      "FROM dbo.tbl_ComplianceAutomationHistory h " &
                      "INNER JOIN dbo.tbl_ComplianceRules r ON h.ComplianceRuleId = r.ComplianceRuleId " &
                      "ORDER BY h.CreatedOn DESC;"
            Return Await _sqlHelper.ExecuteReaderAsync(sql, Nothing, AddressOf MapHistory)
        End Function

        Private Function MapRule(r As IDataReader) As ComplianceRuleDto
            Return New ComplianceRuleDto() With {
                .ComplianceRuleId = Convert.ToInt32(r("ComplianceRuleId")),
                .ComplianceName = Convert.ToString(r("ComplianceName")),
                .Category = Convert.ToString(r("Category")),
                .Frequency = Convert.ToString(r("Frequency")),
                .DueDateRuleDay = Convert.ToInt32(r("DueDateRuleDay")),
                .DueDateRuleMonth = If(r("DueDateRuleMonth") Is DBNull.Value, CType(Nothing, Nullable(Of Integer)), Convert.ToInt32(r("DueDateRuleMonth"))),
                .Priority = Convert.ToString(r("Priority")),
                .IsActive = Convert.ToBoolean(r("IsActive")),
                .CreatedOn = Convert.ToDateTime(r("CreatedOn"))
            }
        End Function

        Private Function MapSchedule(r As IDataReader) As ComplianceAlertScheduleDto
            Return New ComplianceAlertScheduleDto() With {
                .AlertScheduleId = Convert.ToInt32(r("AlertScheduleId")),
                .ComplianceRuleId = Convert.ToInt32(r("ComplianceRuleId")),
                .DaysBeforeDeadline = Convert.ToInt32(r("DaysBeforeDeadline")),
                .AlertType = Convert.ToString(r("AlertType")),
                .IsEnabled = Convert.ToBoolean(r("IsEnabled"))
            }
        End Function

        Private Function MapHistory(r As IDataReader) As ComplianceAutomationHistoryDto
            Return New ComplianceAutomationHistoryDto() With {
                .HistoryId = Convert.ToInt32(r("HistoryId")),
                .ComplianceRuleId = Convert.ToInt32(r("ComplianceRuleId")),
                .ComplianceName = Convert.ToString(r("ComplianceName")),
                .CompliancePeriod = Convert.ToString(r("CompliancePeriod")),
                .DeadlineDate = Convert.ToDateTime(r("DeadlineDate")),
                .TriggerDate = Convert.ToDateTime(r("TriggerDate")),
                .TaskId = If(r("TaskId") Is DBNull.Value, CType(Nothing, Nullable(Of Integer)), Convert.ToInt32(r("TaskId"))),
                .AlertGenerated = Convert.ToBoolean(r("AlertGenerated")),
                .TaskGenerated = Convert.ToBoolean(r("TaskGenerated")),
                .Status = Convert.ToString(r("Status")),
                .CreatedOn = Convert.ToDateTime(r("CreatedOn"))
            }
        End Function
    End Class
End Namespace
