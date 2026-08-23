Imports System
Imports System.Collections.Generic
Imports System.Data
Imports Microsoft.Data.SqlClient
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.DAL.Interfaces

Namespace Repositories
    ''' <summary>
    ''' Data Access Repository for Task Activity records conforming strictly to TaskActivityEntity properties.
    ''' </summary>
    Public Class TaskActivityRepository
        Implements ITaskActivityRepository

        Private ReadOnly _sqlHelper As ISqlHelper

        Public Sub New(sqlHelper As ISqlHelper)
            _sqlHelper = sqlHelper
        End Sub

        Public Async Function GetByIdAsync(activityId As Integer) As Task(Of TaskActivityEntity) Implements ITaskActivityRepository.GetByIdAsync
            Const query As String = "SELECT ActivityId, TaskId, UserId, CategoryId, ActivityDescription, StartTime, EndTime, DurationMinutes, ActivityDate, CreatedOn, CreatedBy " &
                                   "FROM dbo.tbl_TaskActivities WHERE ActivityId = @ActivityId;"
            Dim params As SqlParameter() = {New SqlParameter("@ActivityId", activityId)}
            Dim list = Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapActivityEntity)
            Return If(list.Count > 0, list(0), Nothing)
        End Function

        Public Async Function GetActivitiesByTaskAsync(taskId As Integer) As Task(Of List(Of TaskActivityEntity)) Implements ITaskActivityRepository.GetActivitiesByTaskAsync
            Const query As String = "SELECT ActivityId, TaskId, UserId, CategoryId, ActivityDescription, StartTime, EndTime, DurationMinutes, ActivityDate, CreatedOn, CreatedBy " &
                                   "FROM dbo.tbl_TaskActivities WHERE TaskId = @TaskId ORDER BY StartTime ASC;"
            Dim params As SqlParameter() = {New SqlParameter("@TaskId", taskId)}
            Return Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapActivityEntity)
        End Function

        Public Async Function GetActiveActivityForUserAsync(userId As Integer) As Task(Of TaskActivityEntity) Implements ITaskActivityRepository.GetActiveActivityForUserAsync
            Const query As String = "SELECT ActivityId, TaskId, UserId, CategoryId, ActivityDescription, StartTime, EndTime, DurationMinutes, ActivityDate, CreatedOn, CreatedBy " &
                                   "FROM dbo.tbl_TaskActivities WHERE UserId = @UserId AND EndTime IS NULL;"
            Dim params As SqlParameter() = {New SqlParameter("@UserId", userId)}
            Dim list = Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapActivityEntity)
            Return If(list.Count > 0, list(0), Nothing)
        End Function

        Public Async Function GetDailyTimelineByUserAsync(userId As Integer, activityDate As DateTime) As Task(Of List(Of TaskActivityEntity)) Implements ITaskActivityRepository.GetDailyTimelineByUserAsync
            Const query As String = "SELECT ActivityId, TaskId, UserId, CategoryId, ActivityDescription, StartTime, EndTime, DurationMinutes, ActivityDate, CreatedOn, CreatedBy " &
                                   "FROM dbo.tbl_TaskActivities WHERE UserId = @UserId AND CAST(ActivityDate AS DATE) = CAST(@ActivityDate AS DATE) ORDER BY StartTime ASC;"
            Dim params As SqlParameter() = {
                New SqlParameter("@UserId", userId),
                New SqlParameter("@ActivityDate", activityDate.Date)
            }
            Return Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapActivityEntity)
        End Function

        Public Async Function AddAsync(activity As TaskActivityEntity, Optional transaction As IDbTransaction = Nothing) As Task(Of Integer) Implements ITaskActivityRepository.AddAsync
            Const query As String = "INSERT INTO dbo.tbl_TaskActivities (TaskId, UserId, CategoryId, ActivityDescription, StartTime, EndTime, DurationMinutes, ActivityDate, CreatedOn, CreatedBy) " &
                                   "VALUES (@TaskId, @UserId, @CategoryId, @ActivityDescription, @StartTime, @EndTime, @DurationMinutes, @ActivityDate, GETUTCDATE(), @CreatedBy); " &
                                   "SELECT SCOPE_IDENTITY();"
            Dim params As SqlParameter() = {
                New SqlParameter("@TaskId", activity.TaskId),
                New SqlParameter("@UserId", activity.UserId),
                New SqlParameter("@CategoryId", CInt(activity.Category)),
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
                                   "    INSERT INTO dbo.tbl_TaskActivities (TaskId, UserId, CategoryId, ActivityDescription, StartTime, EndTime, DurationMinutes, ActivityDate, CreatedOn, CreatedBy) " &
                                   "    VALUES (@TaskId, @UserId, @CategoryId, @ActivityDescription, @StartTime, @EndTime, @DurationMinutes, @ActivityDate, GETUTCDATE(), @CreatedBy); " &
                                   "    SELECT SCOPE_IDENTITY(); " &
                                   "END " &
                                   "ELSE " &
                                   "BEGIN " &
                                   "    SELECT COALESCE((SELECT TOP 1 ActivityId FROM dbo.tbl_TaskActivities WHERE TaskId = @TaskId AND ActivityDescription = @ActivityDescription), 0); " &
                                   "END"
            Dim params As SqlParameter() = {
                New SqlParameter("@TaskId", activity.TaskId),
                New SqlParameter("@UserId", activity.UserId),
                New SqlParameter("@CategoryId", CInt(activity.Category)),
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

        Public Async Function CompleteActiveTimerAsync(activityId As Integer, endTime As DateTime, durationMinutes As Integer) As Task(Of Boolean) Implements ITaskActivityRepository.CompleteActiveTimerAsync
            Const query As String = "UPDATE dbo.tbl_TaskActivities SET EndTime = @EndTime, DurationMinutes = @DurationMinutes WHERE ActivityId = @ActivityId;"
            Dim params As SqlParameter() = {
                New SqlParameter("@EndTime", endTime),
                New SqlParameter("@DurationMinutes", durationMinutes),
                New SqlParameter("@ActivityId", activityId)
            }
            Dim rows = Await _sqlHelper.ExecuteNonQueryAsync(query, params)
            Return rows > 0
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
