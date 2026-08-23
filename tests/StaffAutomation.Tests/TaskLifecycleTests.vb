Imports System
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports StaffAutomation.BLL.Services
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Interfaces

Namespace StaffAutomation.Tests
    ''' <summary>
    ''' Unit tests for Phase 5B Task Workflow State Machine and Task Immutability Policy.
    ''' </summary>
    <TestClass>
    Public Class TaskLifecycleTests
        <TestMethod>
        Public Sub TestStateMachineTransitions()
            Dim engine As ITaskWorkflowEngine = New TaskWorkflowEngine()

            ' Valid Transitions
            If Not engine.CanTransition(TaskWorkflowState.NewTask, TaskWorkflowState.Assigned) Then
                Throw New InvalidOperationException("NewTask -> Assigned should be allowed.")
            End If

            If Not engine.CanTransition(TaskWorkflowState.Assigned, TaskWorkflowState.InProgress) Then
                Throw New InvalidOperationException("Assigned -> InProgress should be allowed.")
            End If

            If Not engine.CanTransition(TaskWorkflowState.InProgress, TaskWorkflowState.WaitingForClient) Then
                Throw New InvalidOperationException("InProgress -> WaitingForClient should be allowed.")
            End If

            If Not engine.CanTransition(TaskWorkflowState.WaitingForClient, TaskWorkflowState.InProgress) Then
                Throw New InvalidOperationException("WaitingForClient -> InProgress should be allowed.")
            End If

            ' Invalid Transitions
            If engine.CanTransition(TaskWorkflowState.NewTask, TaskWorkflowState.Closed) Then
                Throw New InvalidOperationException("NewTask -> Closed must NOT be allowed.")
            End If

            If engine.CanTransition(TaskWorkflowState.Closed, TaskWorkflowState.InProgress) Then
                Throw New InvalidOperationException("Closed -> InProgress must NOT be allowed.")
            End If
        End Sub

        <TestMethod>
        Public Sub TestPostCreateTaskVisibilityLogic()
            Dim createdTask As New TaskDto() With {
                .TaskId = 999,
                .TaskCode = "TASK-0999",
                .Title = "Newly Created Test Task",
                .WorkflowState = TaskWorkflowState.Assigned,
                .AssignedToUserId = 2,
                .AssignedToName = "Priya Shah",
                .Priority = TaskPriority.High,
                .TaskType = "ITR Filing",
                .TargetDueDate = DateTime.Today.AddDays(5)
            }

            ' Scenario 1: Active Tab is Completed -> must switch to AllActive
            Dim activeTab As String = "Completed"
            Dim statusFilter As String = "All Status"
            Dim staffFilter As String = "All Staff"
            Dim priorityFilter As String = "All Priority"
            Dim taskTypeFilter As String = "All Task Types"
            Dim searchText As String = String.Empty

            AlignFiltersHelper(createdTask, activeTab, statusFilter, staffFilter, priorityFilter, taskTypeFilter, searchText, currentUserId:=1)
            If activeTab <> "AllActive" Then
                Throw New InvalidOperationException("Active tab 'Completed' must be switched to 'AllActive' for newly created active task.")
            End If

            ' Scenario 2: Status filter set to Pending (conflicting with Assigned) -> status filter must reset
            activeTab = "AllActive"
            statusFilter = "Pending"
            AlignFiltersHelper(createdTask, activeTab, statusFilter, staffFilter, priorityFilter, taskTypeFilter, searchText, currentUserId:=1)
            If statusFilter <> "All Status" Then
                Throw New InvalidOperationException("Conflicting status filter 'Pending' must be reset to 'All Status'.")
            End If

            ' Scenario 3: Staff filter set to Rahul Sharma (conflicting with Priya Shah) -> staff filter must reset
            staffFilter = "Rahul Sharma"
            AlignFiltersHelper(createdTask, activeTab, statusFilter, staffFilter, priorityFilter, taskTypeFilter, searchText, currentUserId:=1)
            If staffFilter <> "All Staff" Then
                Throw New InvalidOperationException("Conflicting staff filter 'Rahul Sharma' must be reset to 'All Staff'.")
            End If

            ' Scenario 4: Search text 'GST' (conflicting with title 'Newly Created Test Task') -> search text must reset
            searchText = "GST"
            AlignFiltersHelper(createdTask, activeTab, statusFilter, staffFilter, priorityFilter, taskTypeFilter, searchText, currentUserId:=1)
            If searchText <> String.Empty Then
                Throw New InvalidOperationException("Conflicting search text 'GST' must be cleared.")
            End If

            ' Scenario 5: Compatible filter (Priority = High) -> must remain unchanged
            priorityFilter = "High"
            AlignFiltersHelper(createdTask, activeTab, statusFilter, staffFilter, priorityFilter, taskTypeFilter, searchText, currentUserId:=1)
            If priorityFilter <> "High" Then
                Throw New InvalidOperationException("Compatible priority filter 'High' must remain unchanged.")
            End If
        End Sub

        <TestMethod>
        Public Sub TestTaskAcceptanceAndPermissionMatrix()
            Dim engine As ITaskWorkflowEngine = New TaskWorkflowEngine()

            ' 1. CanAcceptTask Tests
            ' Assigned staff (UserId = 5, Employee) on Assigned task -> Must be TRUE
            If Not engine.CanAcceptTask(TaskWorkflowState.Assigned, assignedToUserId:=5, currentUserId:=5, userRole:=UserRole.Employee) Then
                Throw New InvalidOperationException("Assigned staff member MUST be allowed to accept Assigned task.")
            End If

            ' Other staff (UserId = 8, Employee) on Assigned task -> Must be FALSE
            If engine.CanAcceptTask(TaskWorkflowState.Assigned, assignedToUserId:=5, currentUserId:=8, userRole:=UserRole.Employee) Then
                Throw New InvalidOperationException("Unassigned staff member must NOT be allowed to accept task.")
            End If

            ' Admin on Assigned task -> Must be FALSE (Admin cannot use staff Accept action)
            If engine.CanAcceptTask(TaskWorkflowState.Assigned, assignedToUserId:=5, currentUserId:=1, userRole:=UserRole.Admin) Then
                Throw New InvalidOperationException("Admin/Owner should not use staff Accept action.")
            End If

            ' 2. CanEditTask Post-Acceptance Freeze Tests
            ' NewTask or Assigned state -> Admin can edit
            If Not engine.CanEditTask(TaskWorkflowState.Assigned, userRole:=UserRole.Admin) Then
                Throw New InvalidOperationException("Admin should be allowed to edit Assigned task before acceptance.")
            End If

            ' InProgress state -> BOTH Admin and Staff CANNOT edit (Master Record Freeze)
            If engine.CanEditTask(TaskWorkflowState.InProgress, userRole:=UserRole.Admin) Then
                Throw New InvalidOperationException("Admin must NOT be allowed to perform normal edit on InProgress task (Master Record Freeze).")
            End If

            If engine.CanEditTask(TaskWorkflowState.InProgress, userRole:=UserRole.Employee) Then
                Throw New InvalidOperationException("Staff must NOT be allowed to edit InProgress task.")
            End If

            ' 3. CanCompleteTask Rule 5 Tests
            ' Assigned staff member on InProgress task -> Must be TRUE
            If Not engine.CanCompleteTask(TaskWorkflowState.InProgress, assignedToUserId:=5, currentUserId:=5, userRole:=UserRole.Employee) Then
                Throw New InvalidOperationException("Assigned staff member MUST be allowed to complete active InProgress task.")
            End If

            ' Different staff member on InProgress task -> Must be FALSE
            If engine.CanCompleteTask(TaskWorkflowState.InProgress, assignedToUserId:=5, currentUserId:=8, userRole:=UserRole.Employee) Then
                Throw New InvalidOperationException("Unassigned staff member must NOT be allowed to complete task.")
            End If

            ' Admin/Owner on InProgress task -> Must be FALSE per Rule 5 (No direct Mark Complete without override)
            If engine.CanCompleteTask(TaskWorkflowState.InProgress, assignedToUserId:=5, currentUserId:=1, userRole:=UserRole.Admin) Then
                Throw New InvalidOperationException("Admin/Owner must NOT directly Mark Complete active task per Rule 5 policy.")
            End If

            ' 4. CanDeleteTask Tests
            ' Staff can NEVER delete tasks
            If engine.CanDeleteTask(TaskWorkflowState.Assigned, userRole:=UserRole.Employee) OrElse engine.CanDeleteTask(TaskWorkflowState.InProgress, userRole:=UserRole.Employee) Then
                Throw New InvalidOperationException("Staff members must NEVER be allowed to delete tasks.")
            End If

            ' Admin can soft-delete non-terminal tasks (NewTask, Assigned, InProgress)
            If Not engine.CanDeleteTask(TaskWorkflowState.InProgress, userRole:=UserRole.Admin) Then
                Throw New InvalidOperationException("Admin must be allowed to soft-delete InProgress task.")
            End If

            ' Completed/Terminal tasks cannot be soft-deleted by anyone through normal workflow
            If engine.CanDeleteTask(TaskWorkflowState.Completed, userRole:=UserRole.Admin) Then
                Throw New InvalidOperationException("Completed tasks must NOT be deleted.")
            End If
        End Sub

        Private Sub AlignFiltersHelper(task As TaskDto, ByRef activeTab As String, ByRef statusFilter As String, ByRef staffFilter As String, ByRef priorityFilter As String, ByRef taskTypeFilter As String, ByRef searchText As String, currentUserId As Integer)
            Dim isTerminal = (task.WorkflowState = TaskWorkflowState.Completed OrElse
                              task.WorkflowState = TaskWorkflowState.Delivered OrElse
                              task.WorkflowState = TaskWorkflowState.Closed OrElse
                              task.WorkflowState = TaskWorkflowState.Cancelled)

            If Not isTerminal Then
                If activeTab = "Completed" Then
                    activeTab = "AllActive"
                ElseIf activeTab = "MyTasks" AndAlso task.AssignedToUserId <> currentUserId Then
                    activeTab = "AllActive"
                End If
            End If

            If Not String.IsNullOrEmpty(statusFilter) AndAlso statusFilter <> "All Status" Then
                If task.WorkflowState.ToString() <> statusFilter Then
                    statusFilter = "All Status"
                End If
            End If

            If Not String.IsNullOrEmpty(staffFilter) AndAlso staffFilter <> "All Staff" Then
                If task.AssignedToName <> staffFilter Then
                    staffFilter = "All Staff"
                End If
            End If

            If Not String.IsNullOrEmpty(priorityFilter) AndAlso priorityFilter <> "All Priority" Then
                If task.Priority.ToString() <> priorityFilter Then
                    priorityFilter = "All Priority"
                End If
            End If

            If Not String.IsNullOrEmpty(taskTypeFilter) AndAlso taskTypeFilter <> "All Task Types" Then
                If task.TaskType <> taskTypeFilter Then
                    taskTypeFilter = "All Task Types"
                End If
            End If

            If Not String.IsNullOrWhiteSpace(searchText) Then
                Dim search = searchText.Trim().ToLower()
                Dim matchName = (Not String.IsNullOrEmpty(task.Title) AndAlso task.Title.ToLower().Contains(search))
                Dim matchClient = (Not String.IsNullOrEmpty(task.ClientName) AndAlso task.ClientName.ToLower().Contains(search))
                Dim matchCode = (Not String.IsNullOrEmpty(task.TaskCode) AndAlso task.TaskCode.ToLower().Contains(search))
                If Not (matchName OrElse matchClient OrElse matchCode) Then
                    searchText = String.Empty
                End If
            End If
        End Sub
    End Class
End Namespace
