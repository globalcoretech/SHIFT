Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Threading.Tasks
Imports Xunit
Imports StaffAutomation.BLL.Logging
Imports StaffAutomation.BLL.Services
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Logging
Imports StaffAutomation.DAL.Interfaces

Namespace Tests
    Public Class TimeTrackingReliabilityTests
        Private Class MockTaskActivityRepo
            Implements ITaskActivityRepository

            Public Property ActiveEntity As TaskActivityEntity = Nothing
            Public Property CompletedId As Integer = 0
            Public Property CompletedEndTime As Nullable(Of DateTime) = Nothing
            Public Property CompletedDuration As Integer = 0
            Public Property CompletedDescription As String = String.Empty

            Public Function GetByIdAsync(activityId As Integer) As Task(Of TaskActivityEntity) Implements ITaskActivityRepository.GetByIdAsync
                Return Task.FromResult(ActiveEntity)
            End Function

            Public Function GetActivitiesByTaskAsync(taskId As Integer) As Task(Of List(Of TaskActivityEntity)) Implements ITaskActivityRepository.GetActivitiesByTaskAsync
                Return Task.FromResult(New List(Of TaskActivityEntity)())
            End Function

            Public Function GetActiveActivityForUserAsync(userId As Integer) As Task(Of TaskActivityEntity) Implements ITaskActivityRepository.GetActiveActivityForUserAsync
                If ActiveEntity IsNot Nothing AndAlso ActiveEntity.UserId = userId AndAlso Not ActiveEntity.EndTime.HasValue Then
                    Return Task.FromResult(ActiveEntity)
                End If
                Return Task.FromResult(CType(Nothing, TaskActivityEntity))
            End Function

            Public Function GetDailyTimelineByUserAsync(userId As Integer, activityDate As DateTime) As Task(Of List(Of TaskActivityEntity)) Implements ITaskActivityRepository.GetDailyTimelineByUserAsync
                Return Task.FromResult(New List(Of TaskActivityEntity)())
            End Function

            Public Function AddAsync(activity As TaskActivityEntity, Optional transaction As IDbTransaction = Nothing) As Task(Of Integer) Implements ITaskActivityRepository.AddAsync
                ActiveEntity = activity
                Return Task.FromResult(101)
            End Function

            Public Function AddChecklistCompletionIdempotentAsync(activity As TaskActivityEntity) As Task(Of Integer) Implements ITaskActivityRepository.AddChecklistCompletionIdempotentAsync
                Return Task.FromResult(101)
            End Function

            Public Function RemoveChecklistCompletionAsync(taskId As Integer, stepText As String) As Task(Of Boolean) Implements ITaskActivityRepository.RemoveChecklistCompletionAsync
                Return Task.FromResult(True)
            End Function

            Public Function CompleteActiveTimerAsync(activityId As Integer, endTime As DateTime, durationMinutes As Integer, Optional updatedDescription As String = Nothing) As Task(Of Boolean) Implements ITaskActivityRepository.CompleteActiveTimerAsync
                CompletedId = activityId
                CompletedEndTime = endTime
                CompletedDuration = durationMinutes
                CompletedDescription = updatedDescription
                If ActiveEntity IsNot Nothing AndAlso ActiveEntity.ActivityId = activityId Then
                    ActiveEntity.EndTime = endTime
                    ActiveEntity.DurationMinutes = durationMinutes
                    If Not String.IsNullOrEmpty(updatedDescription) Then
                        ActiveEntity.ActivityDescription = updatedDescription
                    End If
                End If
                Return Task.FromResult(True)
            End Function

            Public Function GetChronologicalDailyTimelineAsync(startDate As DateTime, endDate As DateTime, Optional userId As Nullable(Of Integer) = Nothing) As Task(Of List(Of DailyTimelineItemDto)) Implements ITaskActivityRepository.GetChronologicalDailyTimelineAsync
                Return Task.FromResult(New List(Of DailyTimelineItemDto)())
            End Function

            Public Function GetClientWorkSummaryReportAsync(startDate As DateTime, endDate As DateTime, Optional clientId As Nullable(Of Integer) = Nothing) As Task(Of List(Of ClientWorkSummaryDto)) Implements ITaskActivityRepository.GetClientWorkSummaryReportAsync
                Return Task.FromResult(New List(Of ClientWorkSummaryDto)())
            End Function

            Public Function GetStaffProductivitySummaryReportAsync(startDate As DateTime, endDate As DateTime, Optional userId As Nullable(Of Integer) = Nothing) As Task(Of List(Of StaffProductivitySummaryDto)) Implements ITaskActivityRepository.GetStaffProductivitySummaryReportAsync
                Return Task.FromResult(New List(Of StaffProductivitySummaryDto)())
            End Function

            Public Function GetTotalWorkMinutesByTaskAsync(taskId As Integer) As Task(Of Integer) Implements ITaskActivityRepository.GetTotalWorkMinutesByTaskAsync
                Return Task.FromResult(0)
            End Function
        End Class

        Private Class MockTaskRepo
            Implements ITaskRepository

            Public Function GetByIdAsync(taskId As Integer) As Task(Of TaskEntity) Implements ITaskRepository.GetByIdAsync
                Return Task.FromResult(New TaskEntity() With {.TaskId = taskId, .TaskCode = "TSK-TEST-101", .WorkflowState = TaskWorkflowState.InProgress})
            End Function

            Public Function GetByTaskCodeAsync(taskCode As String) As Task(Of TaskEntity) Implements ITaskRepository.GetByTaskCodeAsync
                Return Task.FromResult(New TaskEntity() With {.TaskId = 1, .TaskCode = taskCode, .WorkflowState = TaskWorkflowState.InProgress})
            End Function

            Public Function GetTasksByAssigneeAsync(userId As Integer, Optional state As Nullable(Of TaskWorkflowState) = Nothing) As Task(Of List(Of TaskEntity)) Implements ITaskRepository.GetTasksByAssigneeAsync
                Return Task.FromResult(New List(Of TaskEntity)())
            End Function

            Public Function GetTasksByClientAsync(clientId As Integer) As Task(Of List(Of TaskEntity)) Implements ITaskRepository.GetTasksByClientAsync
                Return Task.FromResult(New List(Of TaskEntity)())
            End Function

            Public Function GetAllActiveTasksAsync() As Task(Of List(Of TaskEntity)) Implements ITaskRepository.GetAllActiveTasksAsync
                Return Task.FromResult(New List(Of TaskEntity)())
            End Function

            Public Function GetAllTasksAsync(Optional includeDeleted As Boolean = False) As Task(Of List(Of TaskEntity)) Implements ITaskRepository.GetAllTasksAsync
                Return Task.FromResult(New List(Of TaskEntity)())
            End Function

            Public Function AddAsync(taskItem As TaskEntity) As Task(Of Integer) Implements ITaskRepository.AddAsync
                Return Task.FromResult(1)
            End Function

            Public Function UpdateStateAsync(taskId As Integer, newState As TaskWorkflowState, modifiedBy As Integer) As Task(Of Boolean) Implements ITaskRepository.UpdateStateAsync
                Return Task.FromResult(True)
            End Function

            Public Function UpdateTaskAcceptanceStateAsync(taskId As Integer, assignedStatusId As Integer, inProgressStatusId As Integer, modifiedBy As Integer, originalModifiedOn As Nullable(Of DateTime), Optional transaction As IDbTransaction = Nothing) As Task(Of Boolean) Implements ITaskRepository.UpdateTaskAcceptanceStateAsync
                Return Task.FromResult(True)
            End Function

            Public Function UpdateTaskCompletionStateAsync(taskId As Integer, statusId As Integer, completionDate As Nullable(Of DateTime), modifiedBy As Integer) As Task(Of Boolean) Implements ITaskRepository.UpdateTaskCompletionStateAsync
                Return Task.FromResult(True)
            End Function

            Public Function UpdateTaskAsync(taskItem As TaskEntity) As Task(Of Boolean) Implements ITaskRepository.UpdateTaskAsync
                Return Task.FromResult(True)
            End Function

            Public Function UpdateTaskAsync(updateDto As UpdateTaskDto, categoryId As Integer, departmentId As Integer, modifiedBy As Integer, Optional transaction As IDbTransaction = Nothing) As Task(Of Boolean) Implements ITaskRepository.UpdateTaskAsync
                Return Task.FromResult(True)
            End Function

            Public Function SoftDeleteAsync(taskId As Integer, originalModifiedOn As Nullable(Of DateTime), modifiedBy As Integer, Optional transaction As IDbTransaction = Nothing) As Task(Of Boolean) Implements ITaskRepository.SoftDeleteAsync
                Return Task.FromResult(True)
            End Function

            Public Function GetCategoryIdByCodeAsync(categoryCode As String) As Task(Of Integer) Implements ITaskRepository.GetCategoryIdByCodeAsync
                Return Task.FromResult(1)
            End Function

            Public Function GetActiveFinancialYearIdAsync() As Task(Of Integer) Implements ITaskRepository.GetActiveFinancialYearIdAsync
                Return Task.FromResult(1)
            End Function

            Public Function GetStatusIdByStateAsync(state As TaskWorkflowState) As Task(Of Integer) Implements ITaskRepository.GetStatusIdByStateAsync
                Return Task.FromResult(1)
            End Function

            Public Function GetPriorityIdByEnumAsync(priority As TaskPriority) As Task(Of Integer) Implements ITaskRepository.GetPriorityIdByEnumAsync
                Return Task.FromResult(1)
            End Function

            Public Function GetDepartmentIdByEnumAsync(department As DepartmentType) As Task(Of Integer) Implements ITaskRepository.GetDepartmentIdByEnumAsync
                Return Task.FromResult(1)
            End Function

            Public Function GetCategoryInfoMapAsync() As Task(Of Dictionary(Of Integer, Tuple(Of String, String))) Implements ITaskRepository.GetCategoryInfoMapAsync
                Return Task.FromResult(New Dictionary(Of Integer, Tuple(Of String, String))())
            End Function

            Public Function RestoreAsync(taskId As Integer, modifiedBy As Integer) As Task(Of Boolean) Implements ITaskRepository.RestoreAsync
                Return Task.FromResult(True)
            End Function

            Public Function HardDeleteAsync(taskId As Integer) As Task(Of Boolean) Implements ITaskRepository.HardDeleteAsync
                Return Task.FromResult(True)
            End Function

            Public Function GetTaskDependencyCountsAsync(taskId As Integer) As Task(Of Tuple(Of Integer, Integer)) Implements ITaskRepository.GetTaskDependencyCountsAsync
                Return Task.FromResult(New Tuple(Of Integer, Integer)(0, 0))
            End Function
        End Class

        Private Class MockNullLogger
            Implements IAppLogger

            Public Sub Log(level As LogLevel, message As String, Optional moduleName As String = "", Optional ex As Exception = Nothing) Implements IAppLogger.Log
            End Sub
            Public Sub LogDebug(message As String, Optional moduleName As String = "") Implements IAppLogger.LogDebug
            End Sub
            Public Sub LogInfo(message As String, Optional moduleName As String = "") Implements IAppLogger.LogInfo
            End Sub
            Public Sub LogWarn(message As String, Optional moduleName As String = "") Implements IAppLogger.LogWarn
            End Sub
            Public Sub LogError(message As String, Optional moduleName As String = "", Optional ex As Exception = Nothing) Implements IAppLogger.LogError
            End Sub
            Public Sub LogFatal(message As String, Optional moduleName As String = "", Optional ex As Exception = Nothing) Implements IAppLogger.LogFatal
            End Sub
        End Class

        Private Class MockNullSqlHelper
            Implements ISqlHelper

            Public Function GetConnection() As IDbConnection Implements ISqlHelper.GetConnection
                Return Nothing
            End Function

            Public Function ExecuteNonQueryAsync(commandText As String, parameters As IDbDataParameter(), Optional transaction As IDbTransaction = Nothing) As Task(Of Integer) Implements ISqlHelper.ExecuteNonQueryAsync
                Return Task.FromResult(1)
            End Function
            Public Function ExecuteScalarAsync(Of T)(commandText As String, parameters As IDbDataParameter(), Optional transaction As IDbTransaction = Nothing) As Task(Of T) Implements ISqlHelper.ExecuteScalarAsync
                Return Task.FromResult(CType(Nothing, T))
            End Function
            Public Function ExecuteReaderAsync(Of T)(commandText As String, parameters As IDbDataParameter(), mapFunc As Func(Of IDataReader, T), Optional transaction As IDbTransaction = Nothing) As Task(Of List(Of T)) Implements ISqlHelper.ExecuteReaderAsync
                Return Task.FromResult(New List(Of T)())
            End Function
        End Class

        <Fact>
        Public Async Function TestActiveTimerRehydrationLookup() As Task
            Dim mockRepo As New MockTaskActivityRepo()
            mockRepo.ActiveEntity = New TaskActivityEntity() With {
                .ActivityId = 55,
                .TaskId = 101,
                .UserId = 5,
                .StartTime = DateTime.UtcNow.AddMinutes(-30),
                .EndTime = Nothing,
                .ActivityDescription = "Working on GST Return"
            }

            Dim mockTaskRepo As New MockTaskRepo()
            Dim mockEngine As ITaskWorkflowEngine = New TaskWorkflowEngine()
            Dim logger As IAppLogger = New MockNullLogger()
            Dim auditLogger As New AuditLogger(New MockNullSqlHelper())

            Dim timelineService As ITimelineService = New TimelineService(mockRepo, mockTaskRepo, mockEngine, logger, auditLogger)

            Dim activeDto = Await timelineService.GetActiveActivityForUserAsync(5)

            Assert.NotNull(activeDto)
            Assert.Equal(55, activeDto.ActivityId)
            Assert.Equal(101, activeDto.TaskId)
            Assert.Equal("Working on GST Return", activeDto.ActivityDescription)
        End Function

        <Fact>
        Public Async Function TestAttendanceBreakAutoPauseGuard() As Task
            Dim mockRepo As New MockTaskActivityRepo()
            mockRepo.ActiveEntity = New TaskActivityEntity() With {
                .ActivityId = 88,
                .TaskId = 202,
                .UserId = 7,
                .StartTime = DateTime.UtcNow.AddMinutes(-45),
                .EndTime = Nothing,
                .ActivityDescription = "Drafting Tax Audit Report"
            }

            Dim mockTaskRepo As New MockTaskRepo()
            Dim mockEngine As ITaskWorkflowEngine = New TaskWorkflowEngine()
            Dim logger As IAppLogger = New MockNullLogger()
            Dim auditLogger As New AuditLogger(New MockNullSqlHelper())

            Dim timelineService As ITimelineService = New TimelineService(mockRepo, mockTaskRepo, mockEngine, logger, auditLogger)

            Dim result = Await timelineService.AutoPauseActiveTimerOnAttendanceEventAsync(7, "Auto-paused on Attendance Break")

            Assert.True(result)
            Assert.Equal(88, mockRepo.CompletedId)
            Assert.True(mockRepo.CompletedDuration >= 45)
            Assert.Contains("Drafting Tax Audit Report", mockRepo.CompletedDescription)
            Assert.Contains("[Auto-paused on Attendance Break]", mockRepo.CompletedDescription)
        End Function

        <Fact>
        Public Async Function TestIdempotentAutoPause_NoActiveTimer() As Task
            Dim mockRepo As New MockTaskActivityRepo()
            mockRepo.ActiveEntity = Nothing ' No active timer

            Dim mockTaskRepo As New MockTaskRepo()
            Dim mockEngine As ITaskWorkflowEngine = New TaskWorkflowEngine()
            Dim logger As IAppLogger = New MockNullLogger()
            Dim auditLogger As New AuditLogger(New MockNullSqlHelper())

            Dim timelineService As ITimelineService = New TimelineService(mockRepo, mockTaskRepo, mockEngine, logger, auditLogger)

            ' Call auto-pause twice to verify idempotency
            Dim result1 = Await timelineService.AutoPauseActiveTimerOnAttendanceEventAsync(7, "Auto-paused on Attendance Break")
            Dim result2 = Await timelineService.AutoPauseActiveTimerOnAttendanceEventAsync(7, "Auto-paused on Attendance Break")

            Assert.False(result1)
            Assert.False(result2)
            Assert.Equal(0, mockRepo.CompletedId)
        End Function
    End Class
End Namespace
