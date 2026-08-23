Imports System.Collections.Generic
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Interfaces

Namespace Services
    ''' <summary>
    ''' Task Workflow State Machine enforcing allowed status transitions across 11 task states.
    ''' </summary>
    Public Class TaskWorkflowEngine
        Implements ITaskWorkflowEngine

        Private ReadOnly _allowedTransitions As Dictionary(Of TaskWorkflowState, List(Of TaskWorkflowState))

        Public Sub New()
            _allowedTransitions = New Dictionary(Of TaskWorkflowState, List(Of TaskWorkflowState))()
            ConfigureStateTransitions()
        End Sub

        Private Sub ConfigureStateTransitions()
            _allowedTransitions(TaskWorkflowState.NewTask) = New List(Of TaskWorkflowState) From {
                TaskWorkflowState.Assigned, TaskWorkflowState.Cancelled
            }

            _allowedTransitions(TaskWorkflowState.Assigned) = New List(Of TaskWorkflowState) From {
                TaskWorkflowState.InProgress, TaskWorkflowState.OnHold, TaskWorkflowState.Cancelled
            }

            _allowedTransitions(TaskWorkflowState.InProgress) = New List(Of TaskWorkflowState) From {
                TaskWorkflowState.WaitingForClient, TaskWorkflowState.WaitingForDocuments,
                TaskWorkflowState.OnHold, TaskWorkflowState.UnderReview, TaskWorkflowState.Completed, TaskWorkflowState.Cancelled
            }

            _allowedTransitions(TaskWorkflowState.WaitingForClient) = New List(Of TaskWorkflowState) From {
                TaskWorkflowState.InProgress, TaskWorkflowState.Cancelled
            }

            _allowedTransitions(TaskWorkflowState.WaitingForDocuments) = New List(Of TaskWorkflowState) From {
                TaskWorkflowState.InProgress, TaskWorkflowState.Cancelled
            }

            _allowedTransitions(TaskWorkflowState.OnHold) = New List(Of TaskWorkflowState) From {
                TaskWorkflowState.InProgress, TaskWorkflowState.Assigned, TaskWorkflowState.Cancelled
            }

            _allowedTransitions(TaskWorkflowState.UnderReview) = New List(Of TaskWorkflowState) From {
                TaskWorkflowState.InProgress, TaskWorkflowState.Completed, TaskWorkflowState.Cancelled
            }

            _allowedTransitions(TaskWorkflowState.Completed) = New List(Of TaskWorkflowState) From {
                TaskWorkflowState.Delivered, TaskWorkflowState.UnderReview
            }

            _allowedTransitions(TaskWorkflowState.Delivered) = New List(Of TaskWorkflowState) From {
                TaskWorkflowState.Closed
            }

            _allowedTransitions(TaskWorkflowState.Closed) = New List(Of TaskWorkflowState)()
            _allowedTransitions(TaskWorkflowState.Cancelled) = New List(Of TaskWorkflowState)()
        End Sub

        Public Function CanTransition(currentState As TaskWorkflowState, targetState As TaskWorkflowState, Optional userRole As Nullable(Of UserRole) = Nothing) As Boolean Implements ITaskWorkflowEngine.CanTransition
            If currentState = targetState Then Return True
            If Not _allowedTransitions.ContainsKey(currentState) Then Return False
            Return _allowedTransitions(currentState).Contains(targetState)
        End Function

        Public Sub ValidateTransition(currentState As TaskWorkflowState, targetState As TaskWorkflowState, Optional userRole As Nullable(Of UserRole) = Nothing) Implements ITaskWorkflowEngine.ValidateTransition
            If Not CanTransition(currentState, targetState, userRole) Then
                Throw New BusinessException($"Invalid task status transition from '{currentState.ToString()}' to '{targetState.ToString()}'.", "ERR_INVALID_STATUS_TRANSITION")
            End If
        End Sub

        Public Function CanEditTask(state As TaskWorkflowState) As Boolean Implements ITaskWorkflowEngine.CanEditTask
            ' Default state-only overload: Normal edit is permitted only in initial un-accepted states (NewTask, Assigned)
            Return (state = TaskWorkflowState.NewTask OrElse state = TaskWorkflowState.Assigned)
        End Function

        Public Function CanEditTask(state As TaskWorkflowState, userRole As UserRole) As Boolean Implements ITaskWorkflowEngine.CanEditTask
            ' Mandatory Rule 1: Once accepted (state >= InProgress), task master record is FROZEN for BOTH Admin and Staff.
            If state <> TaskWorkflowState.NewTask AndAlso state <> TaskWorkflowState.Assigned Then
                Return False
            End If

            ' In NewTask or Assigned state, only Admin/Owner can manage/edit master record
            Return (userRole = UserRole.Admin OrElse userRole = UserRole.Owner)
        End Function

        Public Function CanDeleteTask(state As TaskWorkflowState) As Boolean Implements ITaskWorkflowEngine.CanDeleteTask
            ' Staff cannot delete tasks; terminal states cannot be deleted
            Return False
        End Function

        Public Function CanDeleteTask(state As TaskWorkflowState, userRole As UserRole) As Boolean Implements ITaskWorkflowEngine.CanDeleteTask
            ' Terminal states (Completed, Delivered, Closed, Cancelled) cannot be deleted
            If state = TaskWorkflowState.Completed OrElse state = TaskWorkflowState.Delivered OrElse state = TaskWorkflowState.Closed OrElse state = TaskWorkflowState.Cancelled Then
                Return False
            End If

            ' Mandatory Rule: Staff delete permission is ALWAYS False; Admin/Owner can soft-delete non-terminal tasks
            Return (userRole = UserRole.Admin OrElse userRole = UserRole.Owner)
        End Function

        Public Function CanCompleteTask(state As TaskWorkflowState) As Boolean Implements ITaskWorkflowEngine.CanCompleteTask
            Return (state = TaskWorkflowState.InProgress OrElse state = TaskWorkflowState.WaitingForClient OrElse state = TaskWorkflowState.WaitingForDocuments OrElse state = TaskWorkflowState.OnHold OrElse state = TaskWorkflowState.UnderReview)
        End Function

        Public Function CanCompleteTask(state As TaskWorkflowState, assignedToUserId As Integer, currentUserId As Integer, userRole As UserRole) As Boolean Implements ITaskWorkflowEngine.CanCompleteTask
            ' Un-accepted or terminal states cannot be completed
            If Not CanCompleteTask(state) Then Return False

            ' Rule 5: Admin / Owner cannot directly Mark Complete active tasks without administrative override.
            If userRole = UserRole.Admin OrElse userRole = UserRole.Owner Then
                Return False
            End If

            ' Staff can complete only their own assigned active task
            Return (userRole = UserRole.Employee AndAlso currentUserId = assignedToUserId)
        End Function

        Public Function CanAcceptTask(state As TaskWorkflowState, assignedToUserId As Integer, currentUserId As Integer, userRole As UserRole) As Boolean Implements ITaskWorkflowEngine.CanAcceptTask
            ' Acceptance rule: Must be in Assigned state, user must be assigned staff, and user role must be Employee
            If state <> TaskWorkflowState.Assigned Then Return False
            If userRole = UserRole.Admin OrElse userRole = UserRole.Owner Then Return False
            Return (currentUserId = assignedToUserId)
        End Function

        Public Function CanChangeTaskType(state As TaskWorkflowState, currentCategoryId As Integer, newCategoryId As Integer) As Boolean Implements ITaskWorkflowEngine.CanChangeTaskType
            If state = TaskWorkflowState.Completed OrElse state = TaskWorkflowState.Delivered OrElse state = TaskWorkflowState.Closed OrElse state = TaskWorkflowState.Cancelled Then
                Return False
            End If

            If state = TaskWorkflowState.NewTask OrElse state = TaskWorkflowState.Assigned Then
                Return True
            End If

            ' Active in-progress states (InProgress, WaitingForClient, WaitingForDocuments, OnHold, UnderReview)
            ' require exact CategoryId match to preserve category-specific checklist/activity history
            Return (currentCategoryId = newCategoryId)
        End Function
    End Class
End Namespace
