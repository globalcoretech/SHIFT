Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Enums

Namespace Interfaces
    ''' <summary>
    ''' Business Logic Service contract for Task lifecycle, assignment, and deadline tracking.
    ''' </summary>
    Public Interface ITaskManagementService
        Function GetTaskByIdAsync(taskId As Integer) As Task(Of TaskDto)
        Function GetTasksForAssignedUserAsync(userId As Integer) As Task(Of List(Of TaskDto))
        Function GetAllActiveTasksAsync() As Task(Of List(Of TaskDto))
        Function GetAllTasksAsync(Optional includeDeleted As Boolean = False) As Task(Of List(Of TaskDto))
        Function CreateAndAssignTaskAsync(taskDto As TaskDto) As Task(Of Integer)
        Function UpdateTaskAsync(taskDto As TaskDto) As Task(Of Boolean)
        Function UpdateTaskAsync(updateDto As UpdateTaskDto) As Task(Of Boolean)
        Function AcceptTaskAsync(taskId As Integer, Optional originalModifiedOn As Nullable(Of DateTime) = Nothing) As Task(Of Boolean)
        Function TransitionTaskStateAsync(taskId As Integer, targetState As TaskWorkflowState) As Task(Of Boolean)
        Function CompleteTaskAsync(taskId As Integer) As Task(Of Boolean)
        Function ReassignTaskAsync(taskId As Integer, newAssigneeUserId As Integer) As Task(Of Boolean)
        Function SoftDeleteTaskAsync(taskId As Integer) As Task(Of Boolean)
        Function SoftDeleteTaskAsync(taskId As Integer, originalModifiedOn As Nullable(Of DateTime)) As Task(Of Boolean)
        Function CanAcceptTask(task As TaskDto, currentUserId As Integer, userRole As UserRole) As Boolean
        Function CanAcceptTask(state As TaskWorkflowState, assignedToUserId As Integer, currentUserId As Integer, userRole As UserRole) As Boolean
        Function CanEditTask(state As TaskWorkflowState) As Boolean
        Function CanEditTask(task As TaskDto, userRole As UserRole) As Boolean
        Function CanEditTask(state As TaskWorkflowState, userRole As UserRole) As Boolean
        Function CanDeleteTask(state As TaskWorkflowState) As Boolean
        Function CanDeleteTask(task As TaskDto, userRole As UserRole) As Boolean
        Function CanDeleteTask(state As TaskWorkflowState, userRole As UserRole) As Boolean
        Function CanCompleteTask(state As TaskWorkflowState) As Boolean
        Function CanCompleteTask(task As TaskDto, currentUserId As Integer, userRole As UserRole) As Boolean
        Function CanCompleteTask(state As TaskWorkflowState, assignedToUserId As Integer, currentUserId As Integer, userRole As UserRole) As Boolean
        Function CanChangeTaskType(state As TaskWorkflowState, currentCategoryId As Integer, newCategoryId As Integer) As Boolean
    End Interface
End Namespace
