Imports System
Imports System.Collections.Generic
Imports System.Data
Imports Microsoft.Data.SqlClient
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.DAL.Interfaces

Imports StaffAutomation.Core.DTOs

Namespace Repositories
    ''' <summary>
    ''' Data Access Repository for Task Activity records and management reporting SQL aggregations.
    ''' </summary>
    Public Class TaskActivityRepository
        Implements ITaskActivityRepository

        Private ReadOnly _sqlHelper As ISqlHelper

        Public Sub New(sqlHelper As ISqlHelper)
            _sqlHelper = sqlHelper
        End Sub

        Public Async Function GetByIdAsync(activityId As Integer) As Task(Of TaskActivityEntity) Implements ITaskActivityRepository.GetByIdAsync
            Const query As String = "SELECT ActivityId, TaskId, UserId, TimeCategoryId AS CategoryId, ActivityDescription, StartTime, EndTime, DurationMinutes, ActivityDate, CreatedOn, CreatedBy " &
                                   "FROM dbo.tbl_TaskActivities WHERE ActivityId = @ActivityId;"
            Dim params As SqlParameter() = {New SqlParameter("@ActivityId", activityId)}
            Dim list = Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapActivityEntity)
            Return If(list.Count > 0, list(0), Nothing)
        End Function

        Public Async Function GetActivitiesByTaskAsync(taskId As Integer) As Task(Of List(Of TaskActivityEntity)) Implements ITaskActivityRepository.GetActivitiesByTaskAsync
            Const query As String = "SELECT ActivityId, TaskId, UserId, TimeCategoryId AS CategoryId, ActivityDescription, StartTime, EndTime, DurationMinutes, ActivityDate, CreatedOn, CreatedBy " &
                                   "FROM dbo.tbl_TaskActivities WHERE TaskId = @TaskId ORDER BY StartTime ASC;"
            Dim params As SqlParameter() = {New SqlParameter("@TaskId", taskId)}
            Return Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapActivityEntity)
        End Function

        Public Async Function GetActiveActivityForUserAsync(userId As Integer) As Task(Of TaskActivityEntity) Implements ITaskActivityRepository.GetActiveActivityForUserAsync
            Const query As String = "SELECT ActivityId, TaskId, UserId, TimeCategoryId AS CategoryId, ActivityDescription, StartTime, EndTime, DurationMinutes, ActivityDate, CreatedOn, CreatedBy " &
                                   "FROM dbo.tbl_TaskActivities WHERE UserId = @UserId AND EndTime IS NULL;"
            Dim params As SqlParameter() = {New SqlParameter("@UserId", userId)}
            Dim list = Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapActivityEntity)
            Return If(list.Count > 0, list(0), Nothing)
        End Function

        Public Async Function GetDailyTimelineByUserAsync(userId As Integer, activityDate As DateTime) As Task(Of List(Of TaskActivityEntity)) Implements ITaskActivityRepository.GetDailyTimelineByUserAsync
            Const query As String = "SELECT ActivityId, TaskId, UserId, TimeCategoryId AS CategoryId, ActivityDescription, StartTime, EndTime, DurationMinutes, ActivityDate, CreatedOn, CreatedBy " &
                                   "FROM dbo.tbl_TaskActivities WHERE UserId = @UserId AND CAST(ActivityDate AS DATE) = CAST(@ActivityDate AS DATE) ORDER BY StartTime ASC;"
            Dim params As SqlParameter() = {
                New SqlParameter("@UserId", userId),
                New SqlParameter("@ActivityDate", activityDate.Date)
            }
            Return Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapActivityEntity)
        End Function

        Public Async Function AddAsync(activity As TaskActivityEntity, Optional transaction As IDbTransaction = Nothing) As Task(Of Integer) Implements ITaskActivityRepository.AddAsync
            Const query As String = "INSERT INTO dbo.tbl_TaskActivities (TaskId, UserId, TimeCategoryId, ActivityDescription, StartTime, EndTime, DurationMinutes, ActivityDate, CreatedOn, CreatedBy) " &
                                   "VALUES (@TaskId, @UserId, @TimeCategoryId, @ActivityDescription, @StartTime, @EndTime, @DurationMinutes, @ActivityDate, GETUTCDATE(), @CreatedBy); " &
                                   "SELECT SCOPE_IDENTITY();"
            Dim params As SqlParameter() = {
                New SqlParameter("@TaskId", activity.TaskId),
                New SqlParameter("@UserId", activity.UserId),
                New SqlParameter("@TimeCategoryId", CInt(activity.Category)),
                New SqlParameter("@ActivityDescription", activity.ActivityDescription),
                New SqlParameter("@StartTime", activity.StartTime),
                New SqlParameter("@EndTime", If(activity.EndTime.HasValue, CObj(activity.EndTime.Value), DBNull.Value)),
                New SqlParameter("@DurationMinutes", activity.DurationMinutes),
                New SqlParameter("@ActivityDate", activity.ActivityDate),
                New SqlParameter("@CreatedBy", activity.CreatedBy)
            }
            Return Await _sqlHelper.ExecuteScalarAsync(Of Integer)(query, params, transaction)
        End Function

        Public Async Function AddChecklistCompletionIdempotentAsync(activity As TaskActivityEntity) As Task(Of Integer) Implements ITaskActivityRepository.AddChecklistCompletionIdempotentAsync
            ' Atomic SQL Server query with IF NOT EXISTS to prevent duplicate key race conditions
            Const query As String = "IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TaskActivities WHERE TaskId = @TaskId AND ActivityDescription = @ActivityDescription) " &
                                   "BEGIN " &
                                   "    INSERT INTO dbo.tbl_TaskActivities (TaskId, UserId, TimeCategoryId, ActivityDescription, StartTime, EndTime, DurationMinutes, ActivityDate, CreatedOn, CreatedBy) " &
                                   "    VALUES (@TaskId, @UserId, @TimeCategoryId, @ActivityDescription, @StartTime, @EndTime, @DurationMinutes, @ActivityDate, GETUTCDATE(), @CreatedBy); " &
                                   "    SELECT SCOPE_IDENTITY(); " &
                                   "END " &
                                   "ELSE " &
                                   "BEGIN " &
                                   "    SELECT COALESCE((SELECT TOP 1 ActivityId FROM dbo.tbl_TaskActivities WHERE TaskId = @TaskId AND ActivityDescription = @ActivityDescription), 0); " &
                                   "END"
            Dim params As SqlParameter() = {
                New SqlParameter("@TaskId", activity.TaskId),
                New SqlParameter("@UserId", activity.UserId),
                New SqlParameter("@TimeCategoryId", CInt(activity.Category)),
                New SqlParameter("@ActivityDescription", activity.ActivityDescription),
                New SqlParameter("@StartTime", activity.StartTime),
                New SqlParameter("@EndTime", If(activity.EndTime.HasValue, CObj(activity.EndTime.Value), DBNull.Value)),
                New SqlParameter("@DurationMinutes", activity.DurationMinutes),
                New SqlParameter("@ActivityDate", activity.ActivityDate),
                New SqlParameter("@CreatedBy", activity.CreatedBy)
            }

            Try
                Dim resultObj = Await _sqlHelper.ExecuteScalarAsync(Of Object)(query, params)
                If resultObj IsNot Nothing AndAlso Not Convert.IsDBNull(resultObj) Then
                    Return Convert.ToInt32(resultObj)
                End If
                Return 0
            Catch ex As SqlException When ex.Number = 2627 OrElse ex.Number = 2601
                ' Gracefully handle database duplicate key / unique index violation as a successful idempotent operation
                Return 0
            End Try
        End Function

        Public Async Function RemoveChecklistCompletionAsync(taskId As Integer, stepText As String) As Task(Of Boolean) Implements ITaskActivityRepository.RemoveChecklistCompletionAsync
            Const query As String = "DELETE FROM dbo.tbl_TaskActivities WHERE TaskId = @TaskId AND ActivityDescription = @ActivityDescription;"
            Dim stepDesc As String = "[CHECKLIST_COMPLETE] " & stepText.Trim()
            Dim params As SqlParameter() = {
                New SqlParameter("@TaskId", taskId),
                New SqlParameter("@ActivityDescription", stepDesc)
            }
            Dim rows = Await _sqlHelper.ExecuteNonQueryAsync(query, params)
            Return rows > 0
        End Function

        Public Async Function CompleteActiveTimerAsync(activityId As Integer, endTime As DateTime, durationMinutes As Integer, Optional updatedDescription As String = Nothing) As Task(Of Boolean) Implements ITaskActivityRepository.CompleteActiveTimerAsync
            Dim query As String
            Dim params As SqlParameter()

            If Not String.IsNullOrEmpty(updatedDescription) Then
                query = "UPDATE dbo.tbl_TaskActivities SET EndTime = @EndTime, DurationMinutes = @DurationMinutes, ActivityDescription = @ActivityDescription WHERE ActivityId = @ActivityId AND EndTime IS NULL;"
                params = {
                    New SqlParameter("@EndTime", endTime),
                    New SqlParameter("@DurationMinutes", durationMinutes),
                    New SqlParameter("@ActivityDescription", updatedDescription),
                    New SqlParameter("@ActivityId", activityId)
                }
            Else
                query = "UPDATE dbo.tbl_TaskActivities SET EndTime = @EndTime, DurationMinutes = @DurationMinutes WHERE ActivityId = @ActivityId AND EndTime IS NULL;"
                params = {
                    New SqlParameter("@EndTime", endTime),
                    New SqlParameter("@DurationMinutes", durationMinutes),
                    New SqlParameter("@ActivityId", activityId)
                }
            End If

            Dim rows = Await _sqlHelper.ExecuteNonQueryAsync(query, params)
            Return rows > 0
        End Function

        Public Async Function GetChronologicalDailyTimelineAsync(startDate As DateTime, endDate As DateTime, Optional userId As Nullable(Of Integer) = Nothing) As Task(Of List(Of DailyTimelineItemDto)) Implements ITaskActivityRepository.GetChronologicalDailyTimelineAsync
            Const query As String = "SELECT " &
                                   "    act.ActivityId, " &
                                   "    u.FullName AS StaffName, " &
                                   "    act.ActivityDate AS WorkDate, " &
                                   "    act.StartTime, " &
                                   "    act.EndTime, " &
                                   "    c.ClientName, " &
                                   "    t.TaskCode, " &
                                   "    t.Title AS TaskTitle, " &
                                   "    cat.CategoryName AS TaskCategory, " &
                                   "    act.ActivityDescription, " &
                                   "    CASE WHEN act.EndTime IS NOT NULL THEN act.DurationMinutes ELSE DATEDIFF(MINUTE, act.StartTime, GETUTCDATE()) END AS DurationMinutes, " &
                                   "    CASE WHEN act.EndTime IS NOT NULL THEN 'Completed' ELSE 'Running (Provisional)' END AS ActivityStatus " &
                                   "FROM dbo.tbl_TaskActivities act " &
                                   "INNER JOIN dbo.tbl_Users u ON act.UserId = u.UserId " &
                                   "INNER JOIN dbo.tbl_Tasks t ON act.TaskId = t.TaskId " &
                                   "INNER JOIN dbo.tbl_Clients c ON t.ClientId = c.ClientId " &
                                   "INNER JOIN dbo.tbl_TaskCategories cat ON t.CategoryId = cat.CategoryId " &
                                   "WHERE act.ActivityDate >= @StartDate AND act.ActivityDate < DATEADD(DAY, 1, @EndDate) " &
                                   "  AND (@UserId IS NULL OR @UserId = 0 OR act.UserId = @UserId) " &
                                   "  AND (act.ActivityDescription IS NULL OR act.ActivityDescription NOT LIKE '[[]TASK_%') " &
                                   "ORDER BY act.StartTime ASC;"

            Dim params As SqlParameter() = {
                New SqlParameter("@StartDate", startDate.Date),
                New SqlParameter("@EndDate", endDate.Date),
                New SqlParameter("@UserId", If(userId.HasValue, CObj(userId.Value), DBNull.Value))
            }

            Return Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapDailyTimelineItemDto)
        End Function

        Public Async Function GetClientWorkSummaryReportAsync(startDate As DateTime, endDate As DateTime, Optional clientId As Nullable(Of Integer) = Nothing) As Task(Of List(Of ClientWorkSummaryDto)) Implements ITaskActivityRepository.GetClientWorkSummaryReportAsync
            Const query As String = "SELECT " &
                                   "    c.ClientId, " &
                                   "    c.ClientCode, " &
                                   "    c.ClientName, " &
                                   "    COUNT(DISTINCT t.TaskId) AS TotalTasksCount, " &
                                   "    COUNT(DISTINCT CASE WHEN t.StatusId = 7 THEN t.TaskId END) AS CompletedTasksCount, " &
                                   "    COUNT(DISTINCT t.AssignedToUserId) AS AssignedStaffCount, " &
                                   "    ISNULL(SUM(act.DurationMinutes), 0) AS TotalProductiveMinutes " &
                                   "FROM dbo.tbl_Clients c " &
                                   "LEFT JOIN dbo.tbl_Tasks t ON c.ClientId = t.ClientId AND t.IsDeleted = 0 " &
                                   "LEFT JOIN dbo.tbl_TaskActivities act ON t.TaskId = act.TaskId AND act.EndTime IS NOT NULL " &
                                   "    AND act.ActivityDate >= @StartDate AND act.ActivityDate < DATEADD(DAY, 1, @EndDate) " &
                                   "    AND (act.ActivityDescription IS NULL OR act.ActivityDescription NOT LIKE '[[]TASK_%') " &
                                   "WHERE (@ClientId IS NULL OR @ClientId = 0 OR c.ClientId = @ClientId) AND c.IsDeleted = 0 " &
                                   "GROUP BY c.ClientId, c.ClientCode, c.ClientName " &
                                   "ORDER BY c.ClientName ASC;"

            Dim params As SqlParameter() = {
                New SqlParameter("@StartDate", startDate.Date),
                New SqlParameter("@EndDate", endDate.Date),
                New SqlParameter("@ClientId", If(clientId.HasValue, CObj(clientId.Value), DBNull.Value))
            }

            Return Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapClientWorkSummaryDto)
        End Function

        Public Async Function GetStaffProductivitySummaryReportAsync(startDate As DateTime, endDate As DateTime, Optional userId As Nullable(Of Integer) = Nothing) As Task(Of List(Of StaffProductivitySummaryDto)) Implements ITaskActivityRepository.GetStaffProductivitySummaryReportAsync
            Const query As String = "SELECT " &
                                   "    u.UserId, " &
                                   "    u.FullName AS UserName, " &
                                   "    ISNULL(d.DepartmentName, 'General') AS DepartmentName, " &
                                   "    COUNT(DISTINCT att.AttendanceDate) AS WorkingDaysCount, " &
                                   "    ISNULL(SUM(att.TotalWorkingMinutes), 0) AS AttendanceWorkingMinutes, " &
                                   "    ISNULL(SUM(att.TotalBreakMinutes), 0) AS AttendanceBreakMinutes, " &
                                   "    ISNULL(taskAgg.TotalProductiveMinutes, 0) AS ProductiveTaskMinutes, " &
                                   "    ISNULL(taskAgg.CompletedTasksCount, 0) AS CompletedTasksCount " &
                                   "FROM dbo.tbl_Users u " &
                                   "LEFT JOIN dbo.tbl_Departments d ON u.DepartmentId = d.DepartmentId " &
                                   "LEFT JOIN dbo.tbl_Attendance att ON u.UserId = att.UserId " &
                                   "    AND att.AttendanceDate >= @StartDate AND att.AttendanceDate < DATEADD(DAY, 1, @EndDate) " &
                                   "LEFT JOIN ( " &
                                   "    SELECT " &
                                   "        act.UserId, " &
                                   "        SUM(ISNULL(act.DurationMinutes, 0)) AS TotalProductiveMinutes, " &
                                   "        COUNT(DISTINCT CASE WHEN t.StatusId = 7 THEN t.TaskId END) AS CompletedTasksCount " &
                                   "    FROM dbo.tbl_TaskActivities act " &
                                   "    INNER JOIN dbo.tbl_Tasks t ON act.TaskId = t.TaskId AND t.IsDeleted = 0 " &
                                   "    WHERE act.EndTime IS NOT NULL " &
                                   "      AND act.ActivityDate >= @StartDate AND act.ActivityDate < DATEADD(DAY, 1, @EndDate) " &
                                   "      AND (act.ActivityDescription IS NULL OR act.ActivityDescription NOT LIKE '[[]TASK_%') " &
                                   "    GROUP BY act.UserId " &
                                   ") taskAgg ON u.UserId = taskAgg.UserId " &
                                   "WHERE (@UserId IS NULL OR @UserId = 0 OR u.UserId = @UserId) AND u.IsActive = 1 " &
                                   "GROUP BY u.UserId, u.FullName, d.DepartmentName, taskAgg.TotalProductiveMinutes, taskAgg.CompletedTasksCount " &
                                   "ORDER BY u.FullName ASC;"

            Dim params As SqlParameter() = {
                New SqlParameter("@StartDate", startDate.Date),
                New SqlParameter("@EndDate", endDate.Date),
                New SqlParameter("@UserId", If(userId.HasValue, CObj(userId.Value), DBNull.Value))
            }

            Return Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapStaffProductivitySummaryDto)
        End Function

        Public Async Function GetTotalWorkMinutesByTaskAsync(taskId As Integer) As Task(Of Integer) Implements ITaskActivityRepository.GetTotalWorkMinutesByTaskAsync
            Const query As String = "SELECT ISNULL(SUM(DurationMinutes), 0) FROM dbo.tbl_TaskActivities WHERE TaskId = @TaskId AND EndTime IS NOT NULL AND (ActivityDescription IS NULL OR ActivityDescription NOT LIKE '[[]TASK_%');"
            Dim params As SqlParameter() = {New SqlParameter("@TaskId", taskId)}
            Dim res = Await _sqlHelper.ExecuteScalarAsync(Of Object)(query, params)
            If res IsNot Nothing AndAlso Not Convert.IsDBNull(res) Then
                Return Convert.ToInt32(res)
            End If
            Return 0
        End Function

        Private Function MapDailyTimelineItemDto(reader As IDataReader) As DailyTimelineItemDto
            Return New DailyTimelineItemDto() With {
                .ActivityId = Convert.ToInt32(reader("ActivityId")),
                .StaffName = Convert.ToString(reader("StaffName")),
                .WorkDate = Convert.ToDateTime(reader("WorkDate")),
                .StartTime = Convert.ToDateTime(reader("StartTime")),
                .EndTime = If(reader("EndTime") Is DBNull.Value, CType(Nothing, Nullable(Of DateTime)), Convert.ToDateTime(reader("EndTime"))),
                .ClientName = Convert.ToString(reader("ClientName")),
                .TaskCode = Convert.ToString(reader("TaskCode")),
                .TaskTitle = Convert.ToString(reader("TaskTitle")),
                .TaskCategory = Convert.ToString(reader("TaskCategory")),
                .ActivityDescription = Convert.ToString(reader("ActivityDescription")),
                .DurationMinutes = Convert.ToInt32(reader("DurationMinutes")),
                .ActivityStatus = Convert.ToString(reader("ActivityStatus"))
            }
        End Function

        Private Function MapClientWorkSummaryDto(reader As IDataReader) As ClientWorkSummaryDto
            Return New ClientWorkSummaryDto() With {
                .ClientId = Convert.ToInt32(reader("ClientId")),
                .ClientCode = Convert.ToString(reader("ClientCode")),
                .ClientName = Convert.ToString(reader("ClientName")),
                .TotalTasksCount = Convert.ToInt32(reader("TotalTasksCount")),
                .CompletedTasksCount = Convert.ToInt32(reader("CompletedTasksCount")),
                .AssignedStaffCount = Convert.ToInt32(reader("AssignedStaffCount")),
                .TotalProductiveMinutes = Convert.ToInt32(reader("TotalProductiveMinutes"))
            }
        End Function

        Private Function MapStaffProductivitySummaryDto(reader As IDataReader) As StaffProductivitySummaryDto
            Return New StaffProductivitySummaryDto() With {
                .UserId = Convert.ToInt32(reader("UserId")),
                .UserName = Convert.ToString(reader("UserName")),
                .DepartmentName = Convert.ToString(reader("DepartmentName")),
                .WorkingDaysCount = Convert.ToInt32(reader("WorkingDaysCount")),
                .AttendanceWorkingMinutes = Convert.ToInt32(reader("AttendanceWorkingMinutes")),
                .AttendanceBreakMinutes = Convert.ToInt32(reader("AttendanceBreakMinutes")),
                .ProductiveTaskMinutes = Convert.ToInt32(reader("ProductiveTaskMinutes")),
                .CompletedTasksCount = Convert.ToInt32(reader("CompletedTasksCount"))
            }
        End Function

        Private Function MapActivityEntity(reader As IDataReader) As TaskActivityEntity
            Return New TaskActivityEntity() With {
                .ActivityId = Convert.ToInt32(reader("ActivityId")),
                .TaskId = Convert.ToInt32(reader("TaskId")),
                .UserId = Convert.ToInt32(reader("UserId")),
                .Category = CType(Convert.ToInt32(reader("CategoryId")), TimeCategory),
                .ActivityDescription = Convert.ToString(reader("ActivityDescription")),
                .StartTime = Convert.ToDateTime(reader("StartTime")),
                .EndTime = If(reader("EndTime") Is DBNull.Value, CType(Nothing, Nullable(Of DateTime)), Convert.ToDateTime(reader("EndTime"))),
                .DurationMinutes = Convert.ToInt32(reader("DurationMinutes")),
                .ActivityDate = Convert.ToDateTime(reader("ActivityDate")),
                .CreatedOn = Convert.ToDateTime(reader("CreatedOn")),
                .CreatedBy = Convert.ToInt32(reader("CreatedBy"))
            }
        End Function
    End Class
End Namespace
