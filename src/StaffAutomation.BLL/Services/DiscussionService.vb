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
Imports StaffAutomation.Core.Validation
Imports StaffAutomation.DAL.Interfaces

Namespace Services
    ''' <summary>
    ''' Service managing structured Client Discussion records conforming strictly to IDiscussionService interface.
    ''' </summary>
    Public Class DiscussionService
        Implements IDiscussionService

        Private ReadOnly _discussionRepo As IDiscussionRepository
        Private ReadOnly _taskRepo As ITaskRepository
        Private ReadOnly _appLogger As IAppLogger
        Private ReadOnly _auditLogger As AuditLogger

        Public Sub New(discussionRepo As IDiscussionRepository, taskRepo As ITaskRepository, appLogger As IAppLogger, auditLogger As AuditLogger)
            _discussionRepo = discussionRepo
            _taskRepo = taskRepo
            _appLogger = appLogger
            _auditLogger = auditLogger
        End Sub

        Public Async Function LogDiscussionAsync(discussionDto As DiscussionDto) As Task(Of Integer) Implements IDiscussionService.LogDiscussionAsync
            If String.IsNullOrWhiteSpace(discussionDto.DiscussionNotes) Then
                Throw New ValidationException("Discussion notes are required.", "DiscussionNotes")
            End If

            Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)

            Dim entity As New DiscussionEntity() With {
                .ClientId = discussionDto.ClientId,
                .TaskId = discussionDto.TaskId,
                .UserId = currentUserId,
                .Channel = discussionDto.Channel,
                .Outcome = discussionDto.Outcome,
                .DiscussionNotes = discussionDto.DiscussionNotes.Trim(),
                .DurationMinutes = discussionDto.DurationMinutes,
                .DiscussionTimestamp = DateTime.UtcNow,
                .CreatedBy = currentUserId
            }

            Dim discussionId = Await _discussionRepo.AddAsync(entity)

            _appLogger.LogInfo($"Discussion logged for Task ID {discussionDto.TaskId} (ID: {discussionId}, Outcome: {discussionDto.Outcome}).", "DiscussionService")
            Await _auditLogger.LogAuditAsync(currentUserId, "DISCUSSION_LOG", "DiscussionService", $"Discussion logged for Task ID {discussionDto.TaskId} ({discussionDto.DurationMinutes} mins).")

            Return discussionId
        End Function

        Public Async Function GetDiscussionsForClientAsync(clientId As Integer) As Task(Of List(Of DiscussionDto)) Implements IDiscussionService.GetDiscussionsForClientAsync
            Dim list = Await _discussionRepo.GetDiscussionsByClientAsync(clientId)
            Dim dtoList As New List(Of DiscussionDto)()
            For Each item In list
                dtoList.Add(MapToDto(item))
            Next
            Return dtoList
        End Function

        Public Async Function GetDiscussionsForTaskAsync(taskId As Integer) As Task(Of List(Of DiscussionDto)) Implements IDiscussionService.GetDiscussionsForTaskAsync
            Dim list = Await _discussionRepo.GetDiscussionsByTaskAsync(taskId)
            Dim dtoList As New List(Of DiscussionDto)()
            For Each item In list
                dtoList.Add(MapToDto(item))
            Next
            Return dtoList
        End Function

        Private Function MapToDto(item As DiscussionEntity) As DiscussionDto
            Return New DiscussionDto() With {
                .DiscussionId = item.DiscussionId,
                .ClientId = item.ClientId,
                .TaskId = item.TaskId,
                .UserId = item.UserId,
                .Channel = item.Channel,
                .Outcome = item.Outcome,
                .DiscussionNotes = item.DiscussionNotes,
                .DurationMinutes = item.DurationMinutes,
                .DiscussionTimestamp = item.DiscussionTimestamp
            }
        End Function
    End Class
End Namespace
