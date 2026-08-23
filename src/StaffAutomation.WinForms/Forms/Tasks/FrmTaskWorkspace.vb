Imports System
Imports System.Collections.Generic
Imports System.Windows.Forms
Imports StaffAutomation.BLL.Logging
Imports StaffAutomation.BLL.Services
Imports StaffAutomation.Core.Configuration
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Logging
Imports StaffAutomation.Core.Security
Imports StaffAutomation.DAL.Configuration
Imports StaffAutomation.DAL.Core
Imports StaffAutomation.DAL.Repositories

Namespace Forms.Tasks
    ''' <summary>
    ''' Employee Daily Task Workspace Form handling timer start/pause, discussion logging, and state machine updates.
    ''' </summary>
    Public Class FrmTaskWorkspace
        Private ReadOnly _taskService As ITaskManagementService
        Private ReadOnly _timelineService As ITimelineService
        Private ReadOnly _discussionService As IDiscussionService
        Private ReadOnly _mainShellHost As Forms.Main.FrmMainShell
        Private _myTasks As List(Of TaskDto) = New List(Of TaskDto)()
        Private _selectedTaskId As Integer = 0
        Private _activeActivityId As Integer = 0

        Public Sub New()
            Me.New(0, Nothing)
        End Sub

        Public Sub New(initialTaskId As Integer)
            Me.New(initialTaskId, Nothing)
        End Sub

        Public Sub New(initialTaskId As Integer, mainShellHost As Forms.Main.FrmMainShell)
            InitializeComponent()
            _selectedTaskId = initialTaskId
            _mainShellHost = mainShellHost
            Dim config As IAppConfiguration = New AppConfiguration()
            Dim connFactory As IDatabaseConnectionFactory = New DbConnectionFactory(config)
            Dim sqlHelper As ISqlHelper = New SqlHelper(connFactory)
            Dim appLogger As IAppLogger = New AppLogger(config)
            Dim auditLogger As New AuditLogger(sqlHelper)
            Dim taskRepo As DAL.Interfaces.ITaskRepository = New TaskRepository(sqlHelper)
            Dim activityRepo As DAL.Interfaces.ITaskActivityRepository = New TaskActivityRepository(sqlHelper)
            Dim discussionRepo As DAL.Interfaces.IDiscussionRepository = New DiscussionRepository(sqlHelper)
            Dim workflowEngine As ITaskWorkflowEngine = New TaskWorkflowEngine()

            _taskService = New TaskManagementService(taskRepo, workflowEngine, appLogger, auditLogger)
            _timelineService = New TimelineService(activityRepo, taskRepo, workflowEngine, appLogger, auditLogger)
            _discussionService = New DiscussionService(discussionRepo, taskRepo, appLogger, auditLogger)
        End Sub

        Private Sub btnBackToTasks_Click(sender As Object, e As EventArgs) Handles btnBackToTasks.Click
            If _mainShellHost IsNot Nothing Then
                _mainShellHost.NavigateToModule("Tasks")
            Else
                Me.Close()
            End If
        End Sub

        Private Async Sub FrmTaskWorkspace_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            cboTargetStatus.DataSource = [Enum].GetValues(GetType(TaskWorkflowState))
            cboCommType.DataSource = [Enum].GetValues(GetType(CommunicationType))
            cboOutcome.DataSource = [Enum].GetValues(GetType(CommunicationOutcome))
            Await RefreshMyTasksGridAsync()
        End Sub

        Private Async Function RefreshMyTasksGridAsync() As System.Threading.Tasks.Task
            Try
                Me.Cursor = Cursors.WaitCursor
                Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)
                Dim userRole As UserRole = UserRole.Employee
                If CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser IsNot Nothing Then
                    userRole = CurrentUserContext.CurrentUser.Role
                End If

                ' Role-based task retrieval: Admin/Owner view active tasks across organization, Employee views assigned tasks
                If userRole = UserRole.Admin OrElse userRole = UserRole.Owner Then
                    _myTasks = Await _taskService.GetAllActiveTasksAsync()
                Else
                    _myTasks = Await _taskService.GetTasksForAssignedUserAsync(currentUserId)
                End If

                If _myTasks Is Nothing Then _myTasks = New List(Of TaskDto)()

                ' Guarantee Selected Task Context: If an authorized task was explicitly opened, load it even if assigned to another user
                If _selectedTaskId > 0 AndAlso Not _myTasks.Exists(Function(t) t.TaskId = _selectedTaskId) Then
                    Dim specificTask = Await _taskService.GetTaskByIdAsync(_selectedTaskId)
                    If specificTask IsNot Nothing Then
                        _myTasks.Insert(0, specificTask)
                    End If
                End If

                dgvMyTasks.DataSource = Nothing
                dgvMyTasks.DataSource = _myTasks

                If _selectedTaskId > 0 AndAlso _myTasks IsNot Nothing Then
                    For i As Integer = 0 To dgvMyTasks.Rows.Count - 1
                        Dim rowTask = TryCast(dgvMyTasks.Rows(i).DataBoundItem, TaskDto)
                        If rowTask IsNot Nothing AndAlso rowTask.TaskId = _selectedTaskId Then
                            dgvMyTasks.Rows(i).Selected = True
                            dgvMyTasks.FirstDisplayedScrollingRowIndex = i
                            UpdateWorkspaceTaskHeader(rowTask)
                            Exit For
                        End If
                    Next
                End If
            Catch ex As Exception
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Error", "Failed to load task workspace: " & ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Function

        Private Sub dgvMyTasks_SelectionChanged(sender As Object, e As EventArgs) Handles dgvMyTasks.SelectionChanged
            If dgvMyTasks.SelectedRows.Count > 0 Then
                Dim row = dgvMyTasks.SelectedRows(0)
                Dim task = CType(row.DataBoundItem, TaskDto)
                If task IsNot Nothing Then
                    _selectedTaskId = task.TaskId
                    cboTargetStatus.SelectedItem = task.WorkflowState
                    UpdateWorkspaceTaskHeader(task)
                End If
            End If
        End Sub

        Private Sub UpdateWorkspaceTaskHeader(task As TaskDto)
            If task Is Nothing Then Return
            lblTitle.Text = $"Task Workspace: {task.TaskCode} – {task.Title}"

            ' Respect TaskWorkflowEngine immutability for terminal tasks
            Dim isTerminal = (task.WorkflowState = TaskWorkflowState.Completed OrElse task.WorkflowState = TaskWorkflowState.Delivered OrElse task.WorkflowState = TaskWorkflowState.Closed OrElse task.WorkflowState = TaskWorkflowState.Cancelled)
            btnStartTimer.Enabled = Not isTerminal
            btnPauseTimer.Enabled = Not isTerminal
            btnChangeStatus.Enabled = Not isTerminal
            cboTargetStatus.Enabled = Not isTerminal
        End Sub

        Private Async Sub btnStartTimer_Click(sender As Object, e As EventArgs) Handles btnStartTimer.Click
            If _selectedTaskId = 0 Then
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Selection Required", "Please select a task from your workspace to start timer.", Forms.Common.AlertType.WarningAlert, actionText:="OK")
                Return
            End If

            Try
                Me.Cursor = Cursors.WaitCursor
                Dim currentUserId = 1
                Dim currentRole As UserRole = UserRole.Employee
                If CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser IsNot Nothing Then
                    currentUserId = CurrentUserContext.CurrentUser.UserId
                    currentRole = CurrentUserContext.CurrentUser.Role
                End If

                Dim currentTask = Await _taskService.GetTaskByIdAsync(_selectedTaskId)
                If currentTask IsNot Nothing AndAlso currentTask.WorkflowState = TaskWorkflowState.Assigned Then
                    If _taskService.CanAcceptTask(currentTask, currentUserId, currentRole) Then
                        Await _taskService.AcceptTaskAsync(_selectedTaskId, currentTask.ModifiedOn)
                    End If
                End If

                _activeActivityId = Await _timelineService.StartTaskActivityTimerAsync(_selectedTaskId, TimeCategory.WorkTime, "Started active work")
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Timer Active", "Task timer started successfully.", Forms.Common.AlertType.SuccessAlert, actionText:="OK")
                Await RefreshMyTasksGridAsync()
            Catch ex As BusinessException
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Timer Conflict", ex.Message, Forms.Common.AlertType.WarningAlert, actionText:="OK")
            Catch ex As Exception
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Error", "Failed to start timer: " & ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Sub

        Private Async Sub btnPauseTimer_Click(sender As Object, e As EventArgs) Handles btnPauseTimer.Click
            If _activeActivityId = 0 Then
                Forms.Common.FrmInAppAlert.ShowModal(Me, "No Active Timer", "No active running timer was found for your session.", Forms.Common.AlertType.WarningAlert, actionText:="OK")
                Return
            End If

            Try
                Me.Cursor = Cursors.WaitCursor
                Await _timelineService.StopActiveTimerAsync(_activeActivityId)
                _activeActivityId = 0
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Timer Paused", "Task timer paused successfully.", Forms.Common.AlertType.SuccessAlert, actionText:="OK")
                Await RefreshMyTasksGridAsync()
            Catch ex As Exception
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Error", "Failed to pause timer: " & ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Sub

        Private Sub btnLogDiscussion_Click(sender As Object, e As EventArgs) Handles btnLogDiscussion.Click
            If _selectedTaskId = 0 Then
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Selection Required", "Please select a task to log discussion.", Forms.Common.AlertType.WarningAlert, actionText:="OK")
                Return
            End If
            pnlDiscussion.Visible = Not pnlDiscussion.Visible
        End Sub

        Private Async Sub btnSubmitDiscussion_Click(sender As Object, e As EventArgs) Handles btnSubmitDiscussion.Click
            errProvider.Clear()

            If String.IsNullOrWhiteSpace(txtDiscNotes.Text) Then
                errProvider.SetError(txtDiscNotes, "Discussion notes are required.")
                Return
            End If

            Dim durationMinutes As Integer = 15
            Integer.TryParse(txtDiscDuration.Text, durationMinutes)

            Dim dto As New DiscussionDto() With {
                .TaskId = _selectedTaskId,
                .Channel = CType(cboCommType.SelectedItem, CommunicationType),
                .Outcome = CType(cboOutcome.SelectedItem, CommunicationOutcome),
                .DiscussionNotes = txtDiscNotes.Text.Trim(),
                .DurationMinutes = durationMinutes
            }

            Try
                Me.Cursor = Cursors.WaitCursor
                Await _discussionService.LogDiscussionAsync(dto)
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Discussion Saved", "Discussion logged successfully.", Forms.Common.AlertType.SuccessAlert, actionText:="OK")

                ' Automatically update status if Waiting for Client / Documents outcome selected
                If dto.Outcome = CommunicationOutcome.WaitingForReply Then
                    Await _taskService.TransitionTaskStateAsync(_selectedTaskId, TaskWorkflowState.WaitingForClient)
                ElseIf dto.Outcome = CommunicationOutcome.DocumentsRequested Then
                    Await _taskService.TransitionTaskStateAsync(_selectedTaskId, TaskWorkflowState.WaitingForDocuments)
                End If

                pnlDiscussion.Visible = False
                txtDiscNotes.Text = String.Empty
                Await RefreshMyTasksGridAsync()
            Catch ex As Exception
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Error", "Failed to log discussion: " & ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Sub

        Private Async Sub btnChangeStatus_Click(sender As Object, e As EventArgs) Handles btnChangeStatus.Click
            If _selectedTaskId = 0 Then
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Selection Required", "Please select a task to update status.", Forms.Common.AlertType.WarningAlert, actionText:="OK")
                Return
            End If

            Dim targetState = CType(cboTargetStatus.SelectedItem, TaskWorkflowState)

            Try
                Me.Cursor = Cursors.WaitCursor
                Await _taskService.TransitionTaskStateAsync(_selectedTaskId, targetState)
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Status Updated", $"Task status successfully updated to '{targetState.ToString()}'.", Forms.Common.AlertType.SuccessAlert, actionText:="OK")
                Await RefreshMyTasksGridAsync()
            Catch ex As BusinessException
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Invalid State Transition", ex.Message, Forms.Common.AlertType.WarningAlert, actionText:="OK")
            Catch ex As Exception
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Error", "Failed to update status: " & ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Sub
    End Class
End Namespace
