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
Imports StaffAutomation.DAL.Configuration
Imports StaffAutomation.DAL.Core
Imports StaffAutomation.DAL.Repositories

Namespace Forms.Tasks
    ''' <summary>
    ''' Daily Task Management Form handling task creation, assignment, state updates, soft-deletion, and audit logging.
    ''' Delegating all business operations to BLL TaskManagementService.
    ''' </summary>
    Public Class FrmTaskManagement
        Private ReadOnly _taskService As ITaskManagementService
        Private ReadOnly _clientService As IClientService
        Private ReadOnly _userRepo As DAL.Interfaces.IUserRepository
        Private _taskList As List(Of TaskDto) = New List(Of TaskDto)()
        Private _selectedTaskId As Integer = 0

        Public Sub New()
            InitializeComponent()
            Dim config As IAppConfiguration = New AppConfiguration()
            Dim connFactory As IDatabaseConnectionFactory = New DbConnectionFactory(config)
            Dim sqlHelper As ISqlHelper = New SqlHelper(connFactory)
            Dim appLogger As IAppLogger = New AppLogger(config)
            Dim auditLogger As New AuditLogger(sqlHelper)
            Dim taskRepo As DAL.Interfaces.ITaskRepository = New TaskRepository(sqlHelper)
            Dim clientRepo As DAL.Interfaces.IClientRepository = New ClientRepository(sqlHelper)
            _userRepo = New UserRepository(sqlHelper)
            Dim workflowEngine As ITaskWorkflowEngine = New TaskWorkflowEngine()

            _taskService = New TaskManagementService(taskRepo, workflowEngine, appLogger, auditLogger, clientRepo, _userRepo)
            _clientService = New ClientService(clientRepo, appLogger, auditLogger)
        End Sub

        Private Async Sub FrmTaskManagement_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            Forms.Main.ThemeConstants.ApplyModernGridStyle(dgvTasks)
            cboPriority.DataSource = [Enum].GetValues(GetType(TaskPriority))
            cboStatus.DataSource = [Enum].GetValues(GetType(TaskWorkflowState))

            If cboStatusFilter.Items.Count > 0 Then
                cboStatusFilter.SelectedIndex = 0 ' Default to Pending
            End If

            dtpStartDate.Value = DateTime.Today
            dtpDueDate.Value = DateTime.Today.AddDays(7)

            Await LoadDropdownDataAsync()
            Await RefreshTaskGridAsync()
            ClearFormInputs()
        End Sub

        Private Async Function LoadDropdownDataAsync() As System.Threading.Tasks.Task
            Try
                Dim clients = Await _clientService.GetAllClientsAsync()
                cboClient.DataSource = clients
                cboClient.DisplayMember = "ClientName"
                cboClient.ValueMember = "ClientId"

                Dim users = Await _userRepo.GetAllAsync()
                cboAssignee.DataSource = users
                cboAssignee.DisplayMember = "FullName"
                cboAssignee.ValueMember = "UserId"
            Catch ex As Exception
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Error", "Failed to load reference dropdowns: " & ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
            End Try
        End Function

        Private Async Function RefreshTaskGridAsync() As System.Threading.Tasks.Task
            Try
                Me.Cursor = Cursors.WaitCursor
                _taskList = Await _taskService.GetAllTasksAsync(includeDeleted:=False)
                ApplyFilter()
            Catch ex As Exception
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Error", "Failed to load task registry: " & ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Function

        Private Sub ApplyFilter()
            Dim searchText = txtSearch.Text.Trim().ToLower()
            Dim filterMode = If(cboStatusFilter.SelectedItem IsNot Nothing, cboStatusFilter.SelectedItem.ToString(), "Pending")

            Dim filtered = _taskList.FindAll(Function(t)
                ' Status Filter
                Dim matchesStatus As Boolean = True
                If filterMode = "Pending" Then
                    matchesStatus = (t.WorkflowState <> TaskWorkflowState.Completed AndAlso t.WorkflowState <> TaskWorkflowState.Delivered AndAlso t.WorkflowState <> TaskWorkflowState.Closed AndAlso t.WorkflowState <> TaskWorkflowState.Cancelled)
                ElseIf filterMode = "In Progress" Then
                    matchesStatus = (t.WorkflowState = TaskWorkflowState.InProgress)
                ElseIf filterMode = "Completed" Then
                    matchesStatus = (t.WorkflowState = TaskWorkflowState.Completed OrElse t.WorkflowState = TaskWorkflowState.Delivered OrElse t.WorkflowState = TaskWorkflowState.Closed)
                ElseIf filterMode = "Overdue" Then
                    matchesStatus = (t.IsOverdue AndAlso t.WorkflowState <> TaskWorkflowState.Completed AndAlso t.WorkflowState <> TaskWorkflowState.Closed)
                Else ' All
                    matchesStatus = True
                End If

                If Not matchesStatus Then Return False

                ' Search Filter (Task Code, Client Name, Assigned User, Title, Description)
                If String.IsNullOrEmpty(searchText) Then Return True

                Dim codeMatch = (Not String.IsNullOrEmpty(t.TaskCode) AndAlso t.TaskCode.ToLower().Contains(searchText))
                Dim clientMatch = (Not String.IsNullOrEmpty(t.ClientName) AndAlso t.ClientName.ToLower().Contains(searchText))
                Dim userMatch = (Not String.IsNullOrEmpty(t.AssignedToName) AndAlso t.AssignedToName.ToLower().Contains(searchText))
                Dim titleMatch = (Not String.IsNullOrEmpty(t.Title) AndAlso t.Title.ToLower().Contains(searchText))
                Dim descMatch = (Not String.IsNullOrEmpty(t.Description) AndAlso t.Description.ToLower().Contains(searchText))

                Return codeMatch OrElse clientMatch OrElse userMatch OrElse titleMatch OrElse descMatch
            End Function)

            dgvTasks.DataSource = Nothing
            dgvTasks.DataSource = filtered

            ' Custom column headers formatting if columns present
            If dgvTasks.Columns.Count > 0 Then
                If dgvTasks.Columns("TaskCode") IsNot Nothing Then dgvTasks.Columns("TaskCode").HeaderText = "Task Code"
                If dgvTasks.Columns("ClientName") IsNot Nothing Then dgvTasks.Columns("ClientName").HeaderText = "Client"
                If dgvTasks.Columns("Title") IsNot Nothing Then dgvTasks.Columns("Title").HeaderText = "Task Type / Title"
                If dgvTasks.Columns("Priority") IsNot Nothing Then dgvTasks.Columns("Priority").HeaderText = "Priority"
                If dgvTasks.Columns("AssignedToName") IsNot Nothing Then dgvTasks.Columns("AssignedToName").HeaderText = "Assigned To"
                If dgvTasks.Columns("TargetDueDate") IsNot Nothing Then dgvTasks.Columns("TargetDueDate").HeaderText = "Due Date"
                If dgvTasks.Columns("WorkflowState") IsNot Nothing Then dgvTasks.Columns("WorkflowState").HeaderText = "Status"

                ' Hide internal columns from main view for clean presentation
                If dgvTasks.Columns("TaskId") IsNot Nothing Then dgvTasks.Columns("TaskId").Visible = False
                If dgvTasks.Columns("ClientId") IsNot Nothing Then dgvTasks.Columns("ClientId").Visible = False
                If dgvTasks.Columns("AssignedToUserId") IsNot Nothing Then dgvTasks.Columns("AssignedToUserId").Visible = False
                If dgvTasks.Columns("AssignedByUserId") IsNot Nothing Then dgvTasks.Columns("AssignedByUserId").Visible = False
                If dgvTasks.Columns("AssignedByName") IsNot Nothing Then dgvTasks.Columns("AssignedByName").Visible = False
                If dgvTasks.Columns("Department") IsNot Nothing Then dgvTasks.Columns("Department").Visible = False
                If dgvTasks.Columns("ReminderDate") IsNot Nothing Then dgvTasks.Columns("ReminderDate").Visible = False
                If dgvTasks.Columns("DaysRemaining") IsNot Nothing Then dgvTasks.Columns("DaysRemaining").Visible = False
                If dgvTasks.Columns("DaysOverdue") IsNot Nothing Then dgvTasks.Columns("DaysOverdue").Visible = False
                If dgvTasks.Columns("IsOverdue") IsNot Nothing Then dgvTasks.Columns("IsOverdue").Visible = False
            End If
        End Sub

        Private Sub txtSearch_TextChanged(sender As Object, e As EventArgs) Handles txtSearch.TextChanged
            ApplyFilter()
        End Sub

        Private Sub cboStatusFilter_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboStatusFilter.SelectedIndexChanged
            ApplyFilter()
        End Sub

        Private Async Sub btnRefresh_Click(sender As Object, e As EventArgs) Handles btnRefresh.Click
            Await RefreshTaskGridAsync()
        End Sub

        Private Sub dgvTasks_SelectionChanged(sender As Object, e As EventArgs) Handles dgvTasks.SelectionChanged
            If dgvTasks.SelectedRows.Count > 0 Then
                Dim row = dgvTasks.SelectedRows(0)
                Dim task = CType(row.DataBoundItem, TaskDto)
                If task IsNot Nothing Then
                    _selectedTaskId = task.TaskId
                    txtTaskTitle.Text = task.Title
                    txtDesc.Text = task.Description
                    If task.ClientId > 0 Then cboClient.SelectedValue = task.ClientId
                    If task.AssignedToUserId > 0 Then cboAssignee.SelectedValue = task.AssignedToUserId
                    cboPriority.SelectedItem = task.Priority
                    cboStatus.SelectedItem = task.WorkflowState
                    If task.AssignmentDate <> DateTime.MinValue Then dtpStartDate.Value = task.AssignmentDate
                    If task.TargetDueDate <> DateTime.MinValue Then dtpDueDate.Value = task.TargetDueDate
                End If
            End If
        End Sub

        Private Sub btnNew_Click(sender As Object, e As EventArgs) Handles btnNew.Click
            ClearFormInputs()
        End Sub

        Private Sub ClearFormInputs()
            _selectedTaskId = 0
            txtTaskTitle.Text = String.Empty
            txtDesc.Text = String.Empty
            If cboClient.Items.Count > 0 Then cboClient.SelectedIndex = 0
            If cboAssignee.Items.Count > 0 Then cboAssignee.SelectedIndex = 0
            If cboPriority.Items.Count > 0 Then cboPriority.SelectedIndex = 0
            If cboStatus.Items.Count > 0 Then cboStatus.SelectedIndex = 0
            dtpStartDate.Value = DateTime.Today
            dtpDueDate.Value = DateTime.Today.AddDays(7)
            errProvider.Clear()
            cboClient.Focus()
        End Sub

        Private Async Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
            errProvider.Clear()

            If String.IsNullOrWhiteSpace(txtDesc.Text) Then
                errProvider.SetError(txtDesc, "Task description is required.")
                Return
            End If
            If cboClient.SelectedValue Is Nothing Then
                errProvider.SetError(cboClient, "Please select a client.")
                Return
            End If
            If cboAssignee.SelectedValue Is Nothing Then
                errProvider.SetError(cboAssignee, "Please select an assigned staff member.")
                Return
            End If

            Dim selectedClientId As Integer = 0
            If TypeOf cboClient.SelectedValue Is Integer Then
                selectedClientId = CInt(cboClient.SelectedValue)
            ElseIf TypeOf cboClient.SelectedItem Is ClientDto Then
                selectedClientId = DirectCast(cboClient.SelectedItem, ClientDto).ClientId
            End If

            Dim selectedAssigneeId As Integer = 0
            If TypeOf cboAssignee.SelectedValue Is Integer Then
                selectedAssigneeId = CInt(cboAssignee.SelectedValue)
            ElseIf TypeOf cboAssignee.SelectedItem Is UserDto Then
                selectedAssigneeId = DirectCast(cboAssignee.SelectedItem, UserDto).UserId
            End If

            Dim dto As New TaskDto() With {
                .TaskId = _selectedTaskId,
                .Title = If(Not String.IsNullOrWhiteSpace(txtTaskTitle.Text), txtTaskTitle.Text.Trim(), txtDesc.Text.Trim()),
                .Description = txtDesc.Text.Trim(),
                .ClientId = selectedClientId,
                .AssignedToUserId = selectedAssigneeId,
                .Priority = CType(cboPriority.SelectedItem, TaskPriority),
                .WorkflowState = CType(cboStatus.SelectedItem, TaskWorkflowState),
                .AssignmentDate = dtpStartDate.Value,
                .TargetDueDate = dtpDueDate.Value
            }

            Try
                Me.Cursor = Cursors.WaitCursor
                If _selectedTaskId = 0 Then
                    Await _taskService.CreateAndAssignTaskAsync(dto)
                    Forms.Common.DataStateTracker.MarkTasksChanged()
                    Me.DialogResult = DialogResult.OK
                    Forms.Common.FrmInAppAlert.ShowModal(Me, "Success", "Task created and assigned successfully.", Forms.Common.AlertType.SuccessAlert, actionText:="OK")
                Else
                    Await _taskService.UpdateTaskAsync(dto)
                    Forms.Common.DataStateTracker.MarkTasksChanged()
                    Me.DialogResult = DialogResult.OK
                    Forms.Common.FrmInAppAlert.ShowModal(Me, "Success", "Task details updated successfully.", Forms.Common.AlertType.SuccessAlert, actionText:="OK")
                End If
                Await RefreshTaskGridAsync()
                ClearFormInputs()
            Catch ex As BusinessException
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Business Rule Violation", ex.Message, Forms.Common.AlertType.WarningAlert, actionText:="OK")
            Catch ex As Exception
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Error", "Failed to save task: " & ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Sub

        Private Async Sub btnComplete_Click(sender As Object, e As EventArgs) Handles btnComplete.Click
            If _selectedTaskId = 0 Then
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Selection Required", "Please select a task from the grid to mark completed.", Forms.Common.AlertType.WarningAlert, actionText:="OK")
                Return
            End If

            Dim confirm = Forms.Common.FrmInAppAlert.ShowModal(Me, "Confirm Completion", $"Are you sure you want to mark task ID {_selectedTaskId} as Completed?", Forms.Common.AlertType.WarningAlert, actionText:="YES, COMPLETE TASK", showCancel:=True, cancelText:="CANCEL")
            If confirm = DialogResult.OK Then
                Try
                    Me.Cursor = Cursors.WaitCursor
                    Await _taskService.CompleteTaskAsync(_selectedTaskId)
                    Forms.Common.DataStateTracker.MarkTasksChanged()
                    Forms.Common.FrmInAppAlert.ShowModal(Me, "Completed", "Task marked completed successfully.", Forms.Common.AlertType.SuccessAlert, actionText:="OK")
                    Await RefreshTaskGridAsync()
                    ClearFormInputs()
                Catch ex As Exception
                    Forms.Common.FrmInAppAlert.ShowModal(Me, "Error", "Failed to mark task completed: " & ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
                Finally
                    Me.Cursor = Cursors.Default
                End Try
            End If
        End Sub

        Private Async Sub btnDelete_Click(sender As Object, e As EventArgs) Handles btnDelete.Click
            If _selectedTaskId = 0 Then
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Selection Required", "Please select a task from the grid to delete.", Forms.Common.AlertType.WarningAlert, actionText:="OK")
                Return
            End If

            Dim confirm = Forms.Common.FrmInAppAlert.ShowModal(Me, "Confirm Delete", $"Are you sure you want to soft-delete task ID {_selectedTaskId}?{Environment.NewLine}Task record will be archived per firm policy.", Forms.Common.AlertType.WarningAlert, actionText:="YES, DELETE TASK", showCancel:=True, cancelText:="CANCEL")
            If confirm = DialogResult.OK Then
                Try
                    Me.Cursor = Cursors.WaitCursor
                    Await _taskService.SoftDeleteTaskAsync(_selectedTaskId)
                    Forms.Common.DataStateTracker.MarkTasksChanged()
                    Forms.Common.FrmInAppAlert.ShowModal(Me, "Deleted", "Task soft-deleted successfully.", Forms.Common.AlertType.SuccessAlert, actionText:="OK")
                    Await RefreshTaskGridAsync()
                    ClearFormInputs()
                Catch ex As Exception
                    Forms.Common.FrmInAppAlert.ShowModal(Me, "Error", "Failed to delete task: " & ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
                Finally
                    Me.Cursor = Cursors.Default
                End Try
            End If
        End Sub
    End Class
End Namespace
