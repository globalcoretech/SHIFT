Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms
Imports StaffAutomation.BLL.Logging
Imports StaffAutomation.BLL.Services
Imports StaffAutomation.Core.Configuration
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Logging
Imports StaffAutomation.Core.Security
Imports StaffAutomation.Core.Workflow
Imports StaffAutomation.DAL.Configuration
Imports StaffAutomation.DAL.Core
Imports StaffAutomation.DAL.Repositories
Imports StaffAutomation.WinForms.Forms.Main

Namespace Forms.Tasks
    ''' <summary>
    ''' Lightweight Read-Only Admin Task Workflow Viewer.
    ''' Displays task metadata (Title, Client, Department, Due Date, Category Code)
    ''' and resolved ordered standard workflow steps with zero database writes.
    ''' </summary>
    Public Class FrmTaskWorkspace
        Private ReadOnly _taskId As Integer
        Private ReadOnly _taskService As ITaskManagementService
        Private ReadOnly _appLogger As IAppLogger
        Private ReadOnly _mainShellHost As Forms.Main.FrmMainShell

        Private _currentTask As TaskDto = Nothing

        Public Sub New()
            Me.New(0, Nothing)
        End Sub

        Public Sub New(initialTaskId As Integer)
            Me.New(initialTaskId, Nothing)
        End Sub

        Public Sub New(initialTaskId As Integer, mainShellHost As Forms.Main.FrmMainShell)
            InitializeComponent()
            _taskId = initialTaskId
            _mainShellHost = mainShellHost

            Dim config As IAppConfiguration = New AppConfiguration()
            Dim connFactory As IDatabaseConnectionFactory = New DbConnectionFactory(config)
            Dim sqlHelper As ISqlHelper = New SqlHelper(connFactory)
            _appLogger = New AppLogger(config)
            Dim auditLogger As New AuditLogger(sqlHelper)

            Dim taskRepo As DAL.Interfaces.ITaskRepository = New TaskRepository(sqlHelper)
            Dim clientRepo As DAL.Interfaces.IClientRepository = New ClientRepository(sqlHelper)
            Dim userRepo As DAL.Interfaces.IUserRepository = New UserRepository(sqlHelper)
            Dim activityRepo As DAL.Interfaces.ITaskActivityRepository = New TaskActivityRepository(sqlHelper)
            Dim workflowEngine As ITaskWorkflowEngine = New TaskWorkflowEngine()

            _taskService = New TaskManagementService(taskRepo, workflowEngine, _appLogger, auditLogger, clientRepo, userRepo, activityRepo, connFactory)

            _appLogger.LogInfo($"[TaskWorkflowViewer] Instantiated Admin Workflow Viewer for TaskId={_taskId}", "FrmTaskWorkspace")
        End Sub

        Private Sub btnBackToTasks_Click(sender As Object, e As EventArgs) Handles btnBackToTasks.Click
            Me.Close()
        End Sub

        Private Async Sub FrmTaskWorkspace_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            Await LoadSingleTaskWorkspaceAsync()
        End Sub

        Private Async Function LoadSingleTaskWorkspaceAsync() As System.Threading.Tasks.Task
            If _taskId <= 0 Then
                _appLogger.LogError($"[TaskWorkflowViewer] Invalid TaskId={_taskId} passed to Admin Workflow Viewer.", "FrmTaskWorkspace")
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Invalid Task Context", "Task ID is invalid or unspecified.", Forms.Common.AlertType.ErrorAlert, actionText:="OK")
                Me.Close()
                Return
            End If

            Try
                Me.Cursor = Cursors.WaitCursor
                _appLogger.LogInfo($"[TaskWorkflowViewer] Loading task details for TaskId={_taskId}", "FrmTaskWorkspace")

                ' Single Read Operation: Fetch exact TaskDto by TaskId
                Dim sw As Stopwatch = Stopwatch.StartNew()
                Try
                    _currentTask = Await _taskService.GetTaskByIdAsync(_taskId)
                    sw.Stop()
                    _appLogger.LogInfo($"[TaskWorkflowViewer] Operation=GetTaskByIdAsync | TaskId={_taskId} | Succeeded=True | ElapsedMs={sw.ElapsedMilliseconds}", "FrmTaskWorkspace")
                Catch exGetTask As Exception
                    sw.Stop()
                    _appLogger.LogError($"[TaskWorkflowViewer] Operation=GetTaskByIdAsync | TaskId={_taskId} | Succeeded=False | Exception={exGetTask.Message}", "FrmTaskWorkspace", exGetTask)
                    Forms.Common.FrmInAppAlert.ShowModal(Me, "Database Error", $"Failed to load Task #{_taskId}: {exGetTask.Message}", Forms.Common.AlertType.ErrorAlert, actionText:="OK")
                    Return
                End Try

                If _currentTask Is Nothing Then
                    _appLogger.LogError($"[TaskWorkflowViewer] TaskId={_taskId} not found in database.", "FrmTaskWorkspace")
                    Forms.Common.FrmInAppAlert.ShowModal(Me, "Task Not Found", $"Task #{_taskId} could not be found in the system.", Forms.Common.AlertType.ErrorAlert, actionText:="OK")
                    Return
                End If

                ' TaskId Context Mismatch Validation
                If _currentTask.TaskId <> _taskId Then
                    _appLogger.LogError($"[TaskWorkflowViewer] TaskContextMismatch: RequestedTaskId={_taskId} != LoadedTaskId={_currentTask.TaskId}", "FrmTaskWorkspace")
                    Forms.Common.FrmInAppAlert.ShowModal(Me, "Context Mismatch", "Task loading error: Loaded task context does not match request.", Forms.Common.AlertType.ErrorAlert, actionText:="OK")
                    Return
                End If

                _appLogger.LogInfo($"[TaskWorkflowViewer] Loaded TaskId={_currentTask.TaskId} | Title='{_currentTask.Title}' | CategoryCode='{_currentTask.CategoryCode}' | Dept='{_currentTask.Department.ToString()}'", "FrmTaskWorkspace")

                ' Update Header & Metadata Summary Card UI
                lblTitle.Text = $"Admin Task Workflow Viewer — {_currentTask.TaskCode}"
                lblTaskHeaderTitle.Text = $"{_currentTask.TaskCode} — {_currentTask.Title}"
                lblClientName.Text = $"Client: {If(String.IsNullOrWhiteSpace(_currentTask.ClientName), "--", _currentTask.ClientName)}"
                lblDepartment.Text = $"Department: {_currentTask.Department.ToString()}"
                lblDueDate.Text = $"Due Date: {_currentTask.TargetDueDate:yyyy-MM-dd}"
                lblCategoryCode.Text = $"Category Code: {If(String.IsNullOrWhiteSpace(_currentTask.CategoryCode), "--", _currentTask.CategoryCode)}"

                ' Render Read-Only Workflow Checklist Steps for _currentTask
                RenderWorkflowSteps(_currentTask)

            Catch ex As Exception
                _appLogger.LogError($"[TaskWorkflowViewer] LoadSingleTaskWorkspaceAsync failed for TaskId={_taskId}: {ex.Message}", "FrmTaskWorkspace", ex)
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Error", $"Failed to load task workflow viewer: {ex.Message}", Forms.Common.AlertType.ErrorAlert, actionText:="OK")
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Function

        Private Sub RenderWorkflowSteps(task As TaskDto)
            Try
                pnlWorkflowStepsHost.Controls.Clear()
                If task Is Nothing Then Return

                Dim resolutionTier As String = ""
                Dim tmpl = TaskWorkflowTemplateProvider.GetTemplateWithTier(task.TaskType, task.CategoryCode, task.Title, resolutionTier)

                If tmpl IsNot Nothing Then
                    lblWorkflowName.Text = $"Resolved Workflow: {tmpl.TaskType}"
                    lblWorkflowTitle.Text = $"Workflow Template: {tmpl.TaskType} ({resolutionTier}) — Task #{task.TaskId}"
                Else
                    lblWorkflowName.Text = "Resolved Workflow: Standard Workflow"
                    lblWorkflowTitle.Text = $"Workflow Steps — Task #{task.TaskId}"
                End If

                If tmpl Is Nothing OrElse tmpl.StandardSteps Is Nothing OrElse tmpl.StandardSteps.Count = 0 Then
                    Dim lblEmpty As New Label With {
                        .Text = "No standard workflow steps defined for this task category.",
                        .AutoSize = True,
                        .ForeColor = Color.Gray,
                        .Font = New Font("Segoe UI", 9.5!, FontStyle.Italic),
                        .Margin = New Padding(10, 10, 10, 10)
                    }
                    pnlWorkflowStepsHost.Controls.Add(lblEmpty)
                    Return
                End If

                pnlWorkflowStepsHost.SuspendLayout()

                For idx As Integer = 0 To tmpl.StandardSteps.Count - 1
                    Dim stepNumber As Integer = idx + 1
                    Dim rawStepText As String = tmpl.StandardSteps(idx)

                    ' Read-Only Informational Step Panel
                    Dim pnlStepRow As New Panel With {
                        .Size = New Size(880, 36),
                        .Margin = New Padding(4, 4, 4, 4),
                        .BackColor = Color.FromArgb(248, 250, 252)
                    }

                    Dim lblStepBadge As New Label With {
                        .Text = $"Step {stepNumber}",
                        .Font = New Font("Segoe UI", 8.5!, FontStyle.Bold),
                        .ForeColor = Color.FromArgb(30, 58, 138),
                        .BackColor = Color.FromArgb(219, 234, 254),
                        .Size = New Size(60, 22),
                        .TextAlign = ContentAlignment.MiddleCenter,
                        .Location = New Point(8, 7)
                    }

                    Dim lblStepContent As New Label With {
                        .Text = rawStepText,
                        .Font = New Font("Segoe UI", 9.5!, FontStyle.Regular),
                        .ForeColor = Color.FromArgb(30, 41, 59),
                        .AutoSize = True,
                        .Location = New Point(78, 8)
                    }

                    pnlStepRow.Controls.Add(lblStepBadge)
                    pnlStepRow.Controls.Add(lblStepContent)
                    pnlWorkflowStepsHost.Controls.Add(pnlStepRow)
                Next

                pnlWorkflowStepsHost.ResumeLayout(True)
            Catch ex As Exception
                _appLogger.LogError($"[TaskWorkflowViewer] RenderWorkflowSteps failed for TaskId={task.TaskId}: {ex.Message}", "FrmTaskWorkspace", ex)
            End Try
        End Sub
    End Class
End Namespace
