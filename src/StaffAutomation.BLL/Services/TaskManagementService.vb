Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports System.Data
Imports StaffAutomation.BLL.Logging
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Logging
Imports StaffAutomation.Core.Security
Imports StaffAutomation.Core.Validation
Imports StaffAutomation.Core.Workflow
Imports StaffAutomation.DAL.Core
Imports StaffAutomation.DAL.Interfaces

Namespace Services
    ''' <summary>
    ''' Core Task Management Service controlling task creation, assignment, state machine transitions, and audit trails.
    ''' Conforms strictly to ITaskManagementService interface contract.
    ''' </summary>
    Public Class TaskManagementService
        Implements ITaskManagementService

        Private ReadOnly _taskRepo As ITaskRepository
        Private ReadOnly _workflowEngine As ITaskWorkflowEngine
        Private ReadOnly _appLogger As IAppLogger
        Private ReadOnly _auditLogger As AuditLogger
        Private ReadOnly _clientRepo As IClientRepository
        Private ReadOnly _userRepo As IUserRepository
        Private ReadOnly _activityRepo As ITaskActivityRepository
        Private ReadOnly _connectionFactory As IDatabaseConnectionFactory

        Public Sub New(taskRepo As ITaskRepository, workflowEngine As ITaskWorkflowEngine, appLogger As IAppLogger, auditLogger As AuditLogger, Optional clientRepo As IClientRepository = Nothing, Optional userRepo As IUserRepository = Nothing, Optional activityRepo As ITaskActivityRepository = Nothing, Optional connectionFactory As IDatabaseConnectionFactory = Nothing)
            _taskRepo = taskRepo
            _workflowEngine = workflowEngine
            _appLogger = appLogger
            _auditLogger = auditLogger
            _clientRepo = clientRepo
            _userRepo = userRepo
            _activityRepo = activityRepo
            _connectionFactory = connectionFactory
        End Sub

        Public Async Function GetTaskByIdAsync(taskId As Integer) As Task(Of TaskDto) Implements ITaskManagementService.GetTaskByIdAsync
            Dim entity = Await _taskRepo.GetByIdAsync(taskId)
            If entity Is Nothing Then Return Nothing
            Dim catDict As New Dictionary(Of Integer, Tuple(Of String, String))()
            Try
                catDict = Await _taskRepo.GetCategoryInfoMapAsync()
            Catch
            End Try
            Dim dto = MapToDto(entity, catDict)

            If _clientRepo IsNot Nothing AndAlso dto.ClientId > 0 Then
                Try
                    Dim client = Await _clientRepo.GetByIdAsync(dto.ClientId)
                    If client IsNot Nothing Then dto.ClientName = client.ClientName
                Catch
                End Try
            End If

            If _userRepo IsNot Nothing AndAlso dto.AssignedToUserId > 0 Then
                Try
                    Dim user = Await _userRepo.GetByIdAsync(dto.AssignedToUserId)
                    If user IsNot Nothing Then dto.AssignedToName = user.FullName
                Catch
                End Try
            End If

            Return dto
        End Function

        Public Async Function GetTasksForAssignedUserAsync(userId As Integer) As Task(Of List(Of TaskDto)) Implements ITaskManagementService.GetTasksForAssignedUserAsync
            Dim list = Await _taskRepo.GetTasksByAssigneeAsync(userId)
            Dim catDict As New Dictionary(Of Integer, Tuple(Of String, String))()
            Try
                catDict = Await _taskRepo.GetCategoryInfoMapAsync()
            Catch
            End Try
            Dim dtoList As New List(Of TaskDto)()
            For Each item In list
                dtoList.Add(MapToDto(item, catDict))
            Next
            Return dtoList
        End Function

        Public Async Function GetAllActiveTasksAsync() As Task(Of List(Of TaskDto)) Implements ITaskManagementService.GetAllActiveTasksAsync
            Return Await GetAllTasksAsync(includeDeleted:=False)
        End Function

        Public Async Function GetAllTasksAsync(Optional includeDeleted As Boolean = False) As Task(Of List(Of TaskDto)) Implements ITaskManagementService.GetAllTasksAsync
            Dim list = Await _taskRepo.GetAllTasksAsync(includeDeleted)

            Dim catDict As New Dictionary(Of Integer, Tuple(Of String, String))()
            Try
                catDict = Await _taskRepo.GetCategoryInfoMapAsync()
            Catch
            End Try

            Dim clientDict As New Dictionary(Of Integer, String)()
            If _clientRepo IsNot Nothing Then
                Try
                    Dim clients = Await _clientRepo.GetAllAsync(includeDeleted:=True)
                    For Each c In clients
                        clientDict(c.ClientId) = c.ClientName
                    Next
                Catch
                End Try
            End If

            Dim userDict As New Dictionary(Of Integer, String)()
            If _userRepo IsNot Nothing Then
                Try
                    Dim users = Await _userRepo.GetAllAsync()
                    For Each u In users
                        userDict(u.UserId) = u.FullName
                    Next
                Catch
                End Try
            End If

            Dim dtoList As New List(Of TaskDto)()
            For Each item In list
                Dim dto = MapToDto(item, catDict)
                If clientDict.ContainsKey(dto.ClientId) Then
                    dto.ClientName = clientDict(dto.ClientId)
                End If
                If userDict.ContainsKey(dto.AssignedToUserId) Then
                    dto.AssignedToName = userDict(dto.AssignedToUserId)
                End If
                dtoList.Add(dto)
            Next
            Return dtoList
        End Function

        Public Async Function CreateAndAssignTaskAsync(taskDto As TaskDto) As Task(Of Integer) Implements ITaskManagementService.CreateAndAssignTaskAsync
            Dim currentRole As UserRole = If(CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser IsNot Nothing, CurrentUserContext.CurrentUser.Role, UserRole.Employee)
            If currentRole = UserRole.Employee Then
                Throw New BusinessException("Employees are not authorized to create new tasks.", "ERR_UNAUTHORIZED_TASK_CREATION")
            End If

            Dim valRes = CommonValidators.ValidateRequired(taskDto.Description, "Description")
            If Not valRes.IsValid Then Throw New ValidationException(valRes.Errors(0), "Description")

            If taskDto.ClientId <= 0 Then
                Throw New ValidationException("Please select a valid Client for the task.", "ClientId")
            End If

            ' 1. Resolve Task Workflow Template & CategoryId
            Dim tmpl = TaskWorkflowTemplateProvider.GetTemplate(taskDto.TaskType)
            Dim categoryId As Integer = Await _taskRepo.GetCategoryIdByCodeAsync(tmpl.CategoryCode)
            If categoryId <= 0 Then
                Throw New BusinessException($"No active Task Category matching code '{tmpl.CategoryCode}' found in the system for Task Type '{taskDto.TaskType}'. Task creation aborted.", "ERR_INVALID_TASK_CATEGORY")
            End If

            ' 2. Resolve Active Financial Year
            Dim financialYearId As Integer = Await _taskRepo.GetActiveFinancialYearIdAsync()
            If financialYearId <= 0 Then
                Throw New BusinessException("No active Financial Year configured in the system. Please configure an active Financial Year before creating tasks.", "ERR_NO_ACTIVE_FY")
            End If

            ' 3. Resolve Dynamic StatusId for Target Workflow State
            Dim targetState = If(taskDto.WorkflowState <> TaskWorkflowState.NewTask, taskDto.WorkflowState, If(taskDto.AssignedToUserId > 0, TaskWorkflowState.Assigned, TaskWorkflowState.NewTask))
            Dim resolvedStatusId = Await _taskRepo.GetStatusIdByStateAsync(targetState)

            ' 4. Resolve Dynamic DepartmentId and PriorityId
            Dim resolvedDeptId = Await _taskRepo.GetDepartmentIdByEnumAsync(tmpl.Department)
            Dim resolvedPriorityId = Await _taskRepo.GetPriorityIdByEnumAsync(taskDto.Priority)

            Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)

            Dim taskTitle = If(Not String.IsNullOrWhiteSpace(taskDto.Title), taskDto.Title.Trim(), If(String.IsNullOrEmpty(taskDto.Description), "New Task", taskDto.Description.Substring(0, Math.Min(taskDto.Description.Length, 50))))

            Dim entity As New TaskEntity() With {
                .TaskCode = "TSK-" & DateTime.UtcNow.Ticks.ToString().Substring(10),
                .Title = taskTitle,
                .Description = taskDto.Description.Trim(),
                .ClientId = taskDto.ClientId,
                .CategoryId = categoryId,
                .FinancialYearId = financialYearId,
                .AssignedToUserId = taskDto.AssignedToUserId,
                .AssignedByUserId = currentUserId,
                .Department = CType(resolvedDeptId, DepartmentType),
                .Priority = CType(resolvedPriorityId, TaskPriority),
                .WorkflowState = CType(resolvedStatusId, TaskWorkflowState),
                .AssignmentDate = If(taskDto.AssignmentDate <> DateTime.MinValue, taskDto.AssignmentDate, DateTime.UtcNow),
                .TargetDueDate = taskDto.TargetDueDate,
                .CreatedBy = currentUserId
            }

            Dim newId = Await _taskRepo.AddAsync(entity)
            _appLogger.LogInfo($"Task ID {newId} created for Client ID {entity.ClientId} (Category: {categoryId}, FY: {financialYearId}, State: {entity.WorkflowState}).", "TaskManagementService")
            Await _auditLogger.LogAuditAsync(currentUserId, "TASK_CREATED", "TaskManagement", $"[TASK_CREATED] Task ID {newId} ({entity.TaskCode}) created for Client ID {entity.ClientId}.")

            Return newId
        End Function

        Public Function CanAcceptTask(task As TaskDto, currentUserId As Integer, userRole As UserRole) As Boolean Implements ITaskManagementService.CanAcceptTask
            If task Is Nothing Then Return False
            Return CanAcceptTask(task.WorkflowState, task.AssignedToUserId, currentUserId, userRole)
        End Function

        Public Function CanAcceptTask(state As TaskWorkflowState, assignedToUserId As Integer, currentUserId As Integer, userRole As UserRole) As Boolean Implements ITaskManagementService.CanAcceptTask
            Return _workflowEngine.CanAcceptTask(state, assignedToUserId, currentUserId, userRole)
        End Function

        Public Function CanEditTask(state As TaskWorkflowState) As Boolean Implements ITaskManagementService.CanEditTask
            Return _workflowEngine.CanEditTask(state)
        End Function

        Public Function CanEditTask(task As TaskDto, userRole As UserRole) As Boolean Implements ITaskManagementService.CanEditTask
            If task Is Nothing Then Return False
            Return CanEditTask(task.WorkflowState, userRole)
        End Function

        Public Function CanEditTask(state As TaskWorkflowState, userRole As UserRole) As Boolean Implements ITaskManagementService.CanEditTask
            Return _workflowEngine.CanEditTask(state, userRole)
        End Function

        Public Function CanDeleteTask(state As TaskWorkflowState) As Boolean Implements ITaskManagementService.CanDeleteTask
            Return _workflowEngine.CanDeleteTask(state)
        End Function

        Public Function CanDeleteTask(task As TaskDto, userRole As UserRole) As Boolean Implements ITaskManagementService.CanDeleteTask
            If task Is Nothing Then Return False
            Return CanDeleteTask(task.WorkflowState, userRole)
        End Function

        Public Function CanDeleteTask(state As TaskWorkflowState, userRole As UserRole) As Boolean Implements ITaskManagementService.CanDeleteTask
            Return _workflowEngine.CanDeleteTask(state, userRole)
        End Function

        Public Function CanCompleteTask(state As TaskWorkflowState) As Boolean Implements ITaskManagementService.CanCompleteTask
            Return _workflowEngine.CanCompleteTask(state)
        End Function

        Public Function CanCompleteTask(task As TaskDto, currentUserId As Integer, userRole As UserRole) As Boolean Implements ITaskManagementService.CanCompleteTask
            If task Is Nothing Then Return False
            Return CanCompleteTask(task.WorkflowState, task.AssignedToUserId, currentUserId, userRole)
        End Function

        Public Function CanCompleteTask(state As TaskWorkflowState, assignedToUserId As Integer, currentUserId As Integer, userRole As UserRole) As Boolean Implements ITaskManagementService.CanCompleteTask
            Return _workflowEngine.CanCompleteTask(state, assignedToUserId, currentUserId, userRole)
        End Function

        Public Function CanChangeTaskType(state As TaskWorkflowState, currentCategoryId As Integer, newCategoryId As Integer) As Boolean Implements ITaskManagementService.CanChangeTaskType
            Return _workflowEngine.CanChangeTaskType(state, currentCategoryId, newCategoryId)
        End Function

        Public Async Function AcceptTaskAsync(taskId As Integer, Optional originalModifiedOn As Nullable(Of DateTime) = Nothing) As Task(Of Boolean) Implements ITaskManagementService.AcceptTaskAsync
            ' Ground-Truth Database Re-Query Protection
            Dim existing = Await _taskRepo.GetByIdAsync(taskId)
            If existing Is Nothing OrElse existing.IsDeleted Then
                Throw New BusinessException($"Task ID {taskId} was not found or has been deleted.", "ERR_TASK_NOT_FOUND")
            End If

            Dim currentUserId As Integer = 1
            Dim currentRole As UserRole = UserRole.Employee
            If CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser IsNot Nothing Then
                currentUserId = CurrentUserContext.CurrentUser.UserId
                currentRole = CurrentUserContext.CurrentUser.Role
            End If

            ' Strict Permission Check: Only assigned staff member can accept in Assigned state
            If Not CanAcceptTask(existing.WorkflowState, existing.AssignedToUserId, currentUserId, currentRole) Then
                Throw New BusinessException($"Only the assigned staff member can accept Task ID {taskId} ({existing.TaskCode}).", "ERR_TASK_ACCEPTANCE_UNAUTHORIZED")
            End If

            ' Dynamic Lookup of Master Status IDs from dbo.tbl_TaskStatuses
            Dim assignedStatusId = Await _taskRepo.GetStatusIdByStateAsync(TaskWorkflowState.Assigned)
            Dim inProgressStatusId = Await _taskRepo.GetStatusIdByStateAsync(TaskWorkflowState.InProgress)

            Dim modDate = If(originalModifiedOn.HasValue, originalModifiedOn, existing.ModifiedOn)

            ' MANDATORY RULE 2: Atomic Transaction Boundary for Status Change, Activity Entry & Audit Log
            Dim uow As UnitOfWork = Nothing
            If _connectionFactory IsNot Nothing Then
                uow = New UnitOfWork(_connectionFactory)
                uow.BeginTransaction()
            End If

            Try
                Dim trans As IDbTransaction = If(uow IsNot Nothing, uow.Transaction, Nothing)

                ' Mandatory Rule 3: State-Guarded Atomic SQL Update with Optimistic Concurrency
                Dim success = Await _taskRepo.UpdateTaskAcceptanceStateAsync(taskId, assignedStatusId, inProgressStatusId, currentUserId, modDate, trans)

                If Not success Then
                    ' Zero rows affected -> Concurrency or State Conflict
                    Dim fresh = Await _taskRepo.GetByIdAsync(taskId)
                    If fresh Is Nothing OrElse fresh.IsDeleted Then
                        Throw New BusinessException($"Task ID {taskId} no longer exists in database.", "ERR_TASK_NOT_FOUND")
                    ElseIf fresh.WorkflowState <> TaskWorkflowState.Assigned Then
                        Throw New BusinessException($"Task '{fresh.Title}' ({fresh.TaskCode}) has already been accepted or transitioned to '{fresh.WorkflowState}'.", "ERR_TASK_ALREADY_ACCEPTED")
                    Else
                        Throw New ConcurrencyException($"Task '{fresh.Title}' ({fresh.TaskCode}) was modified by another user while you were accepting it. Please refresh and retry.")
                    End If
                End If

                ' Transactional Insert of [TASK_ACCEPTED] Activity Record into tbl_TaskActivities
                If _activityRepo IsNot Nothing Then
                    Dim actEntity As New TaskActivityEntity() With {
                        .TaskId = taskId,
                        .UserId = currentUserId,
                        .Category = TimeCategory.WorkTime,
                        .ActivityDescription = $"[TASK_ACCEPTED] Task ID {taskId} ({existing.TaskCode}) accepted by staff member.",
                        .StartTime = DateTime.UtcNow,
                        .EndTime = DateTime.UtcNow,
                        .DurationMinutes = 0,
                        .ActivityDate = DateTime.UtcNow.Date,
                        .CreatedBy = currentUserId
                    }
                    Await _activityRepo.AddAsync(actEntity, trans)
                End If

                ' Transactional Audit Log
                Await _auditLogger.LogAuditAsync(currentUserId, "TASK_ACCEPTED", "TaskManagement", $"[TASK_ACCEPTED] Task ID {taskId} ({existing.TaskCode}) accepted by staff member.", ipAddress:="", transaction:=trans)

                If uow IsNot Nothing Then uow.Commit()

                _appLogger.LogInfo($"Task ID {taskId} ({existing.TaskCode}) accepted by User ID {currentUserId}.", "TaskManagementService")
                Return True
            Catch ex As Exception
                If uow IsNot Nothing Then uow.Rollback()
                Throw
            End Try
        End Function

        Public Async Function UpdateTaskAsync(updateDto As UpdateTaskDto) As Task(Of Boolean) Implements ITaskManagementService.UpdateTaskAsync
            ' Ground-Truth Database Re-Query Protection
            Dim existing = Await _taskRepo.GetByIdAsync(updateDto.TaskId)
            If existing Is Nothing OrElse existing.IsDeleted Then
                Throw New BusinessException($"Task ID {updateDto.TaskId} was not found or has been deleted.", "ERR_TASK_NOT_FOUND")
            End If

            Dim currentRole As UserRole = If(CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser IsNot Nothing, CurrentUserContext.CurrentUser.Role, UserRole.Employee)

            ' Mandatory Rule 1: Post-acceptance master record freeze enforcement
            If Not CanEditTask(existing.WorkflowState, currentRole) Then
                Throw New BusinessException($"Task ID {updateDto.TaskId} ({existing.TaskCode}) is in '{existing.WorkflowState}' state and cannot be edited per Task Freeze Policy.", "ERR_TASK_IMMUTABLE")
            End If

            Dim valRes = CommonValidators.ValidateRequired(updateDto.Description, "Description")
            If Not valRes.IsValid Then Throw New ValidationException(valRes.Errors(0), "Description")

            If updateDto.ClientId <= 0 Then
                Throw New ValidationException("Please select a valid Client for the task.", "ClientId")
            End If

            If String.IsNullOrWhiteSpace(updateDto.Title) Then
                Throw New ValidationException("Task Title is required.", "Title")
            End If

            ' Resolve CategoryId and DepartmentId based on Task Type
            Dim taskTypeStr = If(Not String.IsNullOrWhiteSpace(updateDto.TaskType), updateDto.TaskType.Trim(), "Taxation")
            Dim tmpl = TaskWorkflowTemplateProvider.GetTemplate(taskTypeStr)
            Dim categoryId As Integer = Await _taskRepo.GetCategoryIdByCodeAsync(tmpl.CategoryCode)
            Dim deptId As Integer = Await _taskRepo.GetDepartmentIdByEnumAsync(tmpl.Department)

            ' Enforce Task Type Category change rule based on workflow state
            If Not CanChangeTaskType(existing.WorkflowState, existing.CategoryId, categoryId) Then
                Throw New BusinessException("Cannot change task category while task is in progress. Existing checklist and workflow activity records are tied to category-specific requirements.", "ERR_CATEGORY_CHANGE_FORBIDDEN")
            End If

            Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)

            ' Track detailed changed fields summary for audit logging
            Dim changes As New List(Of String)()
            If existing.Title <> updateDto.Title.Trim() Then changes.Add($"Title: '{existing.Title}' -> '{updateDto.Title.Trim()}'")
            If existing.ClientId <> updateDto.ClientId Then changes.Add($"ClientId: {existing.ClientId} -> {updateDto.ClientId}")
            If existing.AssignedToUserId <> updateDto.AssignedToUserId Then changes.Add($"AssignedToUserId: {existing.AssignedToUserId} -> {updateDto.AssignedToUserId}")
            If existing.Priority <> updateDto.Priority Then changes.Add($"Priority: {existing.Priority} -> {updateDto.Priority}")
            If existing.TargetDueDate.Date <> updateDto.TargetDueDate.Date Then changes.Add($"TargetDueDate: {existing.TargetDueDate:yyyy-MM-dd} -> {updateDto.TargetDueDate:yyyy-MM-dd}")
            If existing.Description <> updateDto.Description.Trim() Then changes.Add("Description updated")

            Dim changeSummary = If(changes.Count > 0, String.Join("; ", changes), "No fields modified")

            ' Transaction Boundary: Bind task update and [TASK_EDITED] activity log in an atomic SQL Transaction
            Dim uow As UnitOfWork = Nothing
            If _connectionFactory IsNot Nothing Then
                uow = New UnitOfWork(_connectionFactory)
                uow.BeginTransaction()
            End If

            Try
                Dim trans As IDbTransaction = If(uow IsNot Nothing, uow.Transaction, Nothing)
                Dim success = Await _taskRepo.UpdateTaskAsync(updateDto, categoryId, deptId, currentUserId, trans)

                If Not success Then
                    ' Zero rows affected -> Diagnose exact failure reason
                    Dim fresh = Await _taskRepo.GetByIdAsync(updateDto.TaskId)
                    If fresh Is Nothing Then
                        Throw New BusinessException($"Task ID {updateDto.TaskId} no longer exists in database.", "ERR_TASK_NOT_FOUND")
                    ElseIf fresh.IsDeleted Then
                        Throw New BusinessException($"Task '{fresh.Title}' ({fresh.TaskCode}) was deleted by another user.", "ERR_TASK_ALREADY_DELETED")
                    Else
                        Throw New ConcurrencyException($"Task '{fresh.Title}' ({fresh.TaskCode}) was modified by another user while you were editing it. Please refresh and review the updated details.")
                    End If
                End If

                ' Transactional persistence of [TASK_EDITED] activity record
                If _activityRepo IsNot Nothing Then
                    Dim actEntity As New TaskActivityEntity() With {
                        .TaskId = updateDto.TaskId,
                        .UserId = currentUserId,
                        .Category = TimeCategory.WorkTime,
                        .ActivityDescription = $"[TASK_EDITED] Task ID {updateDto.TaskId} ({existing.TaskCode}) edited. Changes: {changeSummary}",
                        .StartTime = DateTime.UtcNow,
                        .EndTime = DateTime.UtcNow,
                        .DurationMinutes = 0,
                        .ActivityDate = DateTime.UtcNow.Date,
                        .CreatedBy = currentUserId
                    }
                    Await _activityRepo.AddAsync(actEntity, trans)
                End If

                If uow IsNot Nothing Then uow.Commit()

                _appLogger.LogInfo($"Task ID {updateDto.TaskId} updated. Changes: {changeSummary}", "TaskManagementService")
                Await _auditLogger.LogAuditAsync(currentUserId, "TASK_EDITED", "TaskManagement", $"[TASK_EDITED] Task ID {updateDto.TaskId} ({existing.TaskCode}) edited. Changes: {changeSummary}")
                Return True
            Catch ex As Exception
                If uow IsNot Nothing Then uow.Rollback()
                Throw
            End Try
        End Function

        Public Async Function UpdateTaskAsync(taskDto As TaskDto) As Task(Of Boolean) Implements ITaskManagementService.UpdateTaskAsync
            Dim existing = Await _taskRepo.GetByIdAsync(taskDto.TaskId)
            If existing Is Nothing OrElse existing.IsDeleted Then
                Throw New BusinessException($"Task ID {taskDto.TaskId} was not found or is deleted.", "ERR_TASK_NOT_FOUND")
            End If

            If Not CanEditTask(existing.WorkflowState) Then
                Throw New BusinessException($"Task ID {taskDto.TaskId} is in '{existing.WorkflowState}' state and cannot be edited.", "ERR_TASK_IMMUTABLE")
            End If

            Dim valRes = CommonValidators.ValidateRequired(taskDto.Description, "Description")
            If Not valRes.IsValid Then Throw New ValidationException(valRes.Errors(0), "Description")

            If taskDto.ClientId <= 0 Then
                Throw New ValidationException("Please select a valid Client for the task.", "ClientId")
            End If

            Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)

            existing.Title = If(Not String.IsNullOrWhiteSpace(taskDto.Title), taskDto.Title.Trim(), If(String.IsNullOrEmpty(taskDto.Description), "Updated Task", taskDto.Description.Substring(0, Math.Min(taskDto.Description.Length, 50))))
            existing.Description = taskDto.Description.Trim()
            existing.ClientId = taskDto.ClientId
            existing.AssignedToUserId = taskDto.AssignedToUserId
            existing.Priority = taskDto.Priority
            existing.WorkflowState = taskDto.WorkflowState
            existing.TargetDueDate = taskDto.TargetDueDate
            existing.ModifiedBy = currentUserId

            If taskDto.WorkflowState = TaskWorkflowState.Completed AndAlso Not existing.CompletionDate.HasValue Then
                existing.CompletionDate = DateTime.UtcNow
            End If

            Dim success = Await _taskRepo.UpdateTaskAsync(existing)
            If success Then
                _appLogger.LogInfo($"Task ID {taskDto.TaskId} updated.", "TaskManagementService")
                Await _auditLogger.LogAuditAsync(currentUserId, "TASK_EDITED", "TaskManagement", $"[TASK_EDITED] Task ID {taskDto.TaskId} ({existing.TaskCode}) updated.")
            End If

            Return success
        End Function

        Public Async Function ReassignTaskAsync(taskId As Integer, newAssigneeUserId As Integer) As Task(Of Boolean) Implements ITaskManagementService.ReassignTaskAsync
            Dim task = Await _taskRepo.GetByIdAsync(taskId)
            If task Is Nothing OrElse task.IsDeleted Then
                Throw New BusinessException($"Task ID {taskId} was not found or is deleted.", "ERR_TASK_NOT_FOUND")
            End If

            EnsureTaskIsEditable(task)

            Dim previousUserId = task.AssignedToUserId
            task.AssignedToUserId = newAssigneeUserId

            If task.WorkflowState = TaskWorkflowState.NewTask Then
                task.WorkflowState = TaskWorkflowState.Assigned
            End If

            Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)
            task.ModifiedBy = currentUserId

            Dim success = Await _taskRepo.UpdateTaskAsync(task)
            If success Then
                _appLogger.LogInfo($"Task ID {taskId} assigned to User ID {newAssigneeUserId}.", "TaskManagementService")
                Await _auditLogger.LogAuditAsync(currentUserId, "TASK_REASSIGN", "TaskManagement", $"Task ID {taskId} assigned to User ID {newAssigneeUserId} (Prev: {previousUserId}).")
            End If

            Return success
        End Function

        Public Async Function TransitionTaskStateAsync(taskId As Integer, targetState As TaskWorkflowState) As Task(Of Boolean) Implements ITaskManagementService.TransitionTaskStateAsync
            Dim task = Await _taskRepo.GetByIdAsync(taskId)
            If task Is Nothing OrElse task.IsDeleted Then
                Throw New BusinessException($"Task ID {taskId} was not found or is deleted.", "ERR_TASK_NOT_FOUND")
            End If

            EnsureTaskIsEditable(task)

            Dim userRole As Nullable(Of UserRole) = Nothing
            If CurrentUserContext.IsAuthenticated Then
                userRole = CurrentUserContext.CurrentUser.Role
            End If

            _workflowEngine.ValidateTransition(task.WorkflowState, targetState, userRole)

            Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)

            Dim success = Await _taskRepo.UpdateStateAsync(taskId, targetState, currentUserId)
            If success Then
                _appLogger.LogInfo($"Task ID {taskId} state transitioned from '{task.WorkflowState}' to '{targetState}'.", "TaskManagementService")
                Await _auditLogger.LogAuditAsync(currentUserId, "TASK_STATE_TRANSITION", "TaskManagement", $"Task ID {taskId} state changed to '{targetState}'.")
            End If

            Return success
        End Function

        Public Async Function CompleteTaskAsync(taskId As Integer) As Task(Of Boolean) Implements ITaskManagementService.CompleteTaskAsync
            ' Ground-Truth Database Re-Query Protection
            Dim task = Await _taskRepo.GetByIdAsync(taskId)
            If task Is Nothing OrElse task.IsDeleted Then
                Throw New BusinessException($"Task ID {taskId} was not found or is deleted.", "ERR_TASK_NOT_FOUND")
            End If

            Dim currentUserId As Integer = 1
            Dim currentRole As UserRole = UserRole.Employee
            If CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser IsNot Nothing Then
                currentUserId = CurrentUserContext.CurrentUser.UserId
                currentRole = CurrentUserContext.CurrentUser.Role
            End If

            If Not CanCompleteTask(task.WorkflowState, task.AssignedToUserId, currentUserId, currentRole) Then
                Throw New BusinessException($"Task ID {taskId} ({task.TaskCode}) cannot be completed in state '{task.WorkflowState}' by current user.", "ERR_TASK_COMPLETION_UNAUTHORIZED")
            End If

            ' Dynamically resolve StatusId for Completed state from master table
            Dim completedStatusId = Await _taskRepo.GetStatusIdByStateAsync(TaskWorkflowState.Completed)
            Dim completionDate = DateTime.UtcNow

            ' Dedicated state transition update targeting ONLY StatusId, CompletionDate, ModifiedOn, and ModifiedBy
            Dim success = Await _taskRepo.UpdateTaskCompletionStateAsync(taskId, completedStatusId, completionDate, currentUserId)
            If success Then
                _appLogger.LogInfo($"Task ID {taskId} marked completed.", "TaskManagementService")
                Await _auditLogger.LogAuditAsync(currentUserId, "TASK_COMPLETED", "TaskManagement", $"[TASK_COMPLETED] Task ID {taskId} ({task.TaskCode}) marked completed.")
            End If

            Return success
        End Function

        Public Function SoftDeleteTaskAsync(taskId As Integer) As Task(Of Boolean) Implements ITaskManagementService.SoftDeleteTaskAsync
            Return SoftDeleteTaskAsync(taskId, Nothing)
        End Function

        Public Async Function SoftDeleteTaskAsync(taskId As Integer, originalModifiedOn As Nullable(Of DateTime)) As Task(Of Boolean) Implements ITaskManagementService.SoftDeleteTaskAsync
            ' Ground-Truth Database Re-Query Protection
            Dim existing = Await _taskRepo.GetByIdAsync(taskId)
            If existing Is Nothing OrElse existing.IsDeleted Then
                Throw New BusinessException($"Task ID {taskId} was not found or is already deleted.", "ERR_TASK_ALREADY_DELETED")
            End If

            Dim currentUserId As Integer = 1
            Dim currentRole As UserRole = UserRole.Employee
            If CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser IsNot Nothing Then
                currentUserId = CurrentUserContext.CurrentUser.UserId
                currentRole = CurrentUserContext.CurrentUser.Role
            End If

            If Not CanDeleteTask(existing.WorkflowState, currentRole) Then
                Throw New BusinessException($"Task '{existing.Title}' ({existing.TaskCode}) is in '{existing.WorkflowState}' state and cannot be deleted per firm policy.", "ERR_COMPLETED_TASK_DELETE_FORBIDDEN")
            End If

            ' Transaction Boundary: Bind soft delete and [TASK_DELETED] activity log in an atomic SQL Transaction
            Dim uow As UnitOfWork = Nothing
            If _connectionFactory IsNot Nothing Then
                uow = New UnitOfWork(_connectionFactory)
                uow.BeginTransaction()
            End If

            Try
                Dim trans As IDbTransaction = If(uow IsNot Nothing, uow.Transaction, Nothing)
                Dim success = Await _taskRepo.SoftDeleteAsync(taskId, originalModifiedOn, currentUserId, trans)

                If Not success Then
                    ' Zero rows affected -> Diagnose exact failure reason
                    Dim fresh = Await _taskRepo.GetByIdAsync(taskId)
                    If fresh Is Nothing Then
                        Throw New BusinessException($"Task ID {taskId} no longer exists in database.", "ERR_TASK_NOT_FOUND")
                    ElseIf fresh.IsDeleted Then
                        Throw New BusinessException($"Task '{existing.Title}' ({existing.TaskCode}) has already been deleted or removed from queue.", "ERR_TASK_ALREADY_DELETED")
                    Else
                        Throw New ConcurrencyException($"Task '{fresh.Title}' ({fresh.TaskCode}) was modified by another user while you were deleting it. Please refresh and review the updated details.")
                    End If
                End If

                ' Transactional persistence of [TASK_DELETED] activity record
                If _activityRepo IsNot Nothing Then
                    Dim actEntity As New TaskActivityEntity() With {
                        .TaskId = taskId,
                        .UserId = currentUserId,
                        .Category = TimeCategory.WorkTime,
                        .ActivityDescription = $"[TASK_DELETED] Task ID {taskId} ({existing.TaskCode}) soft deleted.",
                        .StartTime = DateTime.UtcNow,
                        .EndTime = DateTime.UtcNow,
                        .DurationMinutes = 0,
                        .ActivityDate = DateTime.UtcNow.Date,
                        .CreatedBy = currentUserId
                    }
                    Await _activityRepo.AddAsync(actEntity, trans)
                End If

                If uow IsNot Nothing Then uow.Commit()

                _appLogger.LogInfo($"Task ID {taskId} ({existing.TaskCode}) soft deleted.", "TaskManagementService")
                Await _auditLogger.LogAuditAsync(currentUserId, "TASK_DELETED", "TaskManagement", $"[TASK_DELETED] Task ID {taskId} ({existing.TaskCode}) soft deleted.")
                Return True
            Catch ex As Exception
                If uow IsNot Nothing Then uow.Rollback()
                Throw
            End Try
        End Function

        Public Async Function RestoreTaskAsync(taskId As Integer) As Task(Of Boolean) Implements ITaskManagementService.RestoreTaskAsync
            Dim existing = Await _taskRepo.GetByIdAsync(taskId)
            If existing Is Nothing OrElse Not existing.IsDeleted Then
                Throw New BusinessException($"Task ID {taskId} was not found or is not deleted.", "ERR_TASK_NOT_DELETED")
            End If

            Dim currentUserId As Integer = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)
            
            Dim success = Await _taskRepo.RestoreAsync(taskId, currentUserId)
            If success Then
                _appLogger.LogInfo($"Task ID {taskId} ({existing.TaskCode}) restored.", "TaskManagementService")
                Await _auditLogger.LogAuditAsync(currentUserId, "TASK_RESTORED", "TaskManagement", $"[TASK_RESTORED] Task ID {taskId} ({existing.TaskCode}) restored.")
            End If
            Return success
        End Function

        Public Async Function GetTaskDependencyCountsAsync(taskId As Integer) As Task(Of Tuple(Of Integer, Integer)) Implements ITaskManagementService.GetTaskDependencyCountsAsync
            Return Await _taskRepo.GetTaskDependencyCountsAsync(taskId)
        End Function

        Public Async Function PermanentlyDeleteTaskAsync(taskId As Integer) As Task(Of Boolean) Implements ITaskManagementService.PermanentlyDeleteTaskAsync
            Dim existing = Await _taskRepo.GetByIdAsync(taskId)
            If existing Is Nothing Then
                Throw New BusinessException($"Task ID {taskId} was not found.", "ERR_TASK_NOT_FOUND")
            End If

            Dim currentUserId As Integer = 1
            Dim currentRole As UserRole = UserRole.Employee
            If CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser IsNot Nothing Then
                currentUserId = CurrentUserContext.CurrentUser.UserId
                currentRole = CurrentUserContext.CurrentUser.Role
            End If

            If currentRole <> UserRole.Admin AndAlso currentRole <> UserRole.Owner Then
                Throw New BusinessException("Only Administrators or Owners can permanently delete tasks.", "ERR_UNAUTHORIZED")
            End If

            Dim counts = Await _taskRepo.GetTaskDependencyCountsAsync(taskId)
            If counts.Item1 > 0 OrElse counts.Item2 > 0 Then
                Throw New BusinessException($"This task has {counts.Item1} activity logs and {counts.Item2} discussion threads. It cannot be permanently deleted to preserve the audit trail. It will remain soft-deleted.", "ERR_HAS_DEPENDENCIES")
            End If

            Dim success = Await _taskRepo.HardDeleteAsync(taskId)
            If success Then
                _appLogger.LogInfo($"Task ID {taskId} ({existing.TaskCode}) permanently deleted.", "TaskManagementService")
                Await _auditLogger.LogAuditAsync(currentUserId, "TASK_HARD_DELETED", "TaskManagement", $"[TASK_HARD_DELETED] Task ID {taskId} ({existing.TaskCode}) permanently deleted.")
            End If
            Return success
        End Function

        Private Sub EnsureTaskIsEditable(task As TaskEntity)
            If task.WorkflowState = TaskWorkflowState.Completed OrElse task.WorkflowState = TaskWorkflowState.Delivered OrElse task.WorkflowState = TaskWorkflowState.Closed Then
                Throw New BusinessException($"Task ID {task.TaskId} is in '{task.WorkflowState}' state and cannot be modified per Task Immutability Policy.", "ERR_TASK_IMMUTABLE")
            End If
        End Sub

        Private Function MapToDto(entity As TaskEntity, Optional catDict As Dictionary(Of Integer, Tuple(Of String, String)) = Nothing) As TaskDto
            Dim today = DateTime.UtcNow.Date
            Dim dueDate = entity.TargetDueDate.Date
            Dim daysDiff = CInt((dueDate - today).TotalDays)

            Dim resolvedCategoryCode As String = ""
            Dim resolvedTaskType As String = entity.Department.ToString()

            If catDict IsNot Nothing AndAlso catDict.ContainsKey(entity.CategoryId) Then
                resolvedCategoryCode = catDict(entity.CategoryId).Item1
                resolvedTaskType = catDict(entity.CategoryId).Item2
            End If

            If String.IsNullOrWhiteSpace(resolvedCategoryCode) Then
                Dim tmpl = TaskWorkflowTemplateProvider.GetTemplate(entity.Department.ToString(), Nothing)
                resolvedCategoryCode = tmpl.CategoryCode
            End If

            Return New TaskDto() With {
                .TaskId = entity.TaskId,
                .TaskCode = entity.TaskCode,
                .Title = entity.Title,
                .Description = entity.Description,
                .ClientId = entity.ClientId,
                .CategoryId = entity.CategoryId,
                .CategoryCode = resolvedCategoryCode,
                .FinancialYearId = entity.FinancialYearId,
                .AssignedToUserId = entity.AssignedToUserId,
                .AssignedByUserId = entity.AssignedByUserId,
                .Department = entity.Department,
                .TaskType = resolvedTaskType,
                .Priority = entity.Priority,
                .WorkflowState = entity.WorkflowState,
                .AssignmentDate = entity.AssignmentDate,
                .TargetDueDate = entity.TargetDueDate,
                .ReminderDate = entity.ReminderDate,
                .ModifiedOn = entity.ModifiedOn,
                .DaysRemaining = If(daysDiff > 0, daysDiff, 0),
                .DaysOverdue = If(daysDiff < 0, Math.Abs(daysDiff), 0),
                .IsOverdue = daysDiff < 0,
                .IsDeleted = entity.IsDeleted
            }
        End Function
    End Class
End Namespace
