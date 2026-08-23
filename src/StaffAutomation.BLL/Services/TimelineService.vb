Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.BLL.Logging
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Logging
Imports StaffAutomation.Core.Security
Imports StaffAutomation.DAL.Interfaces

Namespace Services
    ''' <summary>
    ''' Service managing active work timers, timer stops, and daily user timelines conforming strictly to ITimelineService interface.
    ''' </summary>
    Public Class TimelineService
        Implements ITimelineService

        Private ReadOnly _activityRepo As ITaskActivityRepository
        Private ReadOnly _taskRepo As ITaskRepository
        Private ReadOnly _workflowEngine As ITaskWorkflowEngine
        Private ReadOnly _appLogger As IAppLogger
        Private ReadOnly _auditLogger As AuditLogger

        Public Sub New(activityRepo As ITaskActivityRepository, taskRepo As ITaskRepository, workflowEngine As ITaskWorkflowEngine, appLogger As IAppLogger, auditLogger As AuditLogger)
            _activityRepo = activityRepo
            _taskRepo = taskRepo
            _workflowEngine = workflowEngine
            _appLogger = appLogger
            _auditLogger = auditLogger
        End Sub

        Public Async Function StartTaskActivityTimerAsync(taskId As Integer, category As TimeCategory, description As String) As Task(Of Integer) Implements ITimelineService.StartTaskActivityTimerAsync
            Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)

            Dim existingActive = Await _activityRepo.GetActiveActivityForUserAsync(currentUserId)
            If existingActive IsNot Nothing Then
                Throw New BusinessException($"Staff already has an active running timer on Task ID {existingActive.TaskId}. Pause/stop it before starting another task.", "ERR_ACTIVE_TIMER_EXISTS")
            End If

            Dim task = Await _taskRepo.GetByIdAsync(taskId)
            If task Is Nothing Then
                Throw New BusinessException($"Task ID {taskId} was not found.", "ERR_TASK_NOT_FOUND")
            End If

            If task.WorkflowState = TaskWorkflowState.Assigned OrElse task.WorkflowState = TaskWorkflowState.NewTask OrElse task.WorkflowState = TaskWorkflowState.WaitingForClient OrElse task.WorkflowState = TaskWorkflowState.WaitingForDocuments OrElse task.WorkflowState = TaskWorkflowState.OnHold Then
                Dim userRole As Nullable(Of UserRole) = Nothing
                If CurrentUserContext.IsAuthenticated Then
                    userRole = CurrentUserContext.CurrentUser.Role
                End If

                If _workflowEngine.CanTransition(task.WorkflowState, TaskWorkflowState.InProgress, userRole) Then
                    Await _taskRepo.UpdateStateAsync(taskId, TaskWorkflowState.InProgress, currentUserId)
                End If
            End If

            Dim activity As New TaskActivityEntity() With {
                .TaskId = taskId,
                .UserId = currentUserId,
                .Category = category,
                .ActivityDescription = If(String.IsNullOrEmpty(description), "Timer Started", description),
                .StartTime = DateTime.UtcNow,
                .ActivityDate = DateTime.UtcNow.Date,
                .CreatedBy = currentUserId
            }

            Dim activityId = Await _activityRepo.AddAsync(activity)
            _appLogger.LogInfo($"Timer started on Task ID {taskId} (Activity ID: {activityId}).", "TimelineService")
            Await _auditLogger.LogAuditAsync(currentUserId, "TIMER_START", "TimelineService", $"Timer started on Task ID {taskId}.")

            Return activityId
        End Function

        Public Async Function StopActiveTimerAsync(activityId As Integer) As Task(Of Boolean) Implements ITimelineService.StopActiveTimerAsync
            Dim activity = Await _activityRepo.GetByIdAsync(activityId)
            If activity Is Nothing OrElse activity.EndTime.HasValue Then
                Throw New BusinessException("No active timer found to stop.", "ERR_NO_ACTIVE_TIMER")
            End If

            Dim endTime = DateTime.UtcNow
            Dim durationMinutes = CInt((endTime - activity.StartTime).TotalMinutes)
            If durationMinutes < 1 Then durationMinutes = 1

            Dim success = Await _activityRepo.CompleteActiveTimerAsync(activityId, endTime, durationMinutes)
            If success Then
                _appLogger.LogInfo($"Timer stopped on Activity ID {activityId} (Duration: {durationMinutes} mins).", "TimelineService")
                Await _auditLogger.LogAuditAsync(activity.UserId, "TIMER_STOP", "TimelineService", $"Timer stopped on Task ID {activity.TaskId} ({durationMinutes} mins).")
            End If

            Return success
        End Function

        Public Async Function GetDailyTimelineForUserAsync(userId As Integer, activityDate As DateTime) As Task(Of List(Of TaskActivityDto)) Implements ITimelineService.GetDailyTimelineForUserAsync
            Dim list = Await _activityRepo.GetDailyTimelineByUserAsync(userId, activityDate)
            Dim dtoList As New List(Of TaskActivityDto)()
            For Each item In list
                dtoList.Add(New TaskActivityDto() With {
                    .ActivityId = item.ActivityId,
                    .TaskId = item.TaskId,
                    .UserId = item.UserId,
                    .Category = item.Category,
                    .ActivityDescription = item.ActivityDescription,
                    .StartTime = item.StartTime,
                    .EndTime = item.EndTime,
                    .DurationMinutes = item.DurationMinutes,
                    .ActivityDate = item.ActivityDate
                })
            Next
            Return dtoList
        End Function
    End Class
End Namespace
