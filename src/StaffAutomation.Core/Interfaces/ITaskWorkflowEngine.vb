Imports StaffAutomation.Core.Enums

Namespace Interfaces
    ''' <summary>
    ''' Contract for validating and executing 11-state Task Lifecycle transitions.
    ''' </summary>
    Public Interface ITaskWorkflowEngine
        Function CanTransition(currentState As TaskWorkflowState, targetState As TaskWorkflowState, Optional userRole As Nullable(Of UserRole) = Nothing) As Boolean
        Sub ValidateTransition(currentState As TaskWorkflowState, targetState As TaskWorkflowState, Optional userRole As Nullable(Of UserRole) = Nothing)
        Function CanEditTask(state As TaskWorkflowState) As Boolean
        Function CanEditTask(state As TaskWorkflowState, userRole As UserRole) As Boolean
        Function CanDeleteTask(state As TaskWorkflowState) As Boolean
        Function CanDeleteTask(state As TaskWorkflowState, userRole As UserRole) As Boolean
        Function CanCompleteTask(state As TaskWorkflowState) As Boolean
        Function CanCompleteTask(state As TaskWorkflowState, assignedToUserId As Integer, currentUserId As Integer, userRole As UserRole) As Boolean
        Function CanAcceptTask(state As TaskWorkflowState, assignedToUserId As Integer, currentUserId As Integer, userRole As UserRole) As Boolean
        Function CanChangeTaskType(state As TaskWorkflowState, currentCategoryId As Integer, newCategoryId As Integer) As Boolean
    End Interface
End Namespace
