Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Drawing
Imports System.IO
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
        Private dgvChecklist As DataGridView
        Private pnlChecklistContainer As Panel
        Private _uploadDirectory As String

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

            _uploadDirectory = config.GetSetting("UploadDirectory", "C:\StaffAutomation\SharedUploads")
            
            Dim taskRepo As DAL.Interfaces.ITaskRepository = New TaskRepository(sqlHelper)
            Dim clientRepo As DAL.Interfaces.IClientRepository = New ClientRepository(sqlHelper)
            Dim userRepo As DAL.Interfaces.IUserRepository = New UserRepository(sqlHelper)
            Dim activityRepo As DAL.Interfaces.ITaskActivityRepository = New TaskActivityRepository(sqlHelper)
            Dim workflowEngine As ITaskWorkflowEngine = New TaskWorkflowEngine()

            Dim checklistRepo As DAL.Interfaces.ITaskChecklistRepository = New TaskChecklistRepository(sqlHelper)
            _taskService = New TaskManagementService(taskRepo, workflowEngine, _appLogger, auditLogger, clientRepo, userRepo, activityRepo, connFactory, checklistRepo)

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
                
                ' Render Interactive Admin-Defined Checklist
                Await RenderChecklistItemsAsync(_taskId)

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

        Private Async Function RenderChecklistItemsAsync(taskId As Integer) As Task
            Try
                Dim checklist = Await _taskService.GetTaskChecklistAsync(taskId)
                
                If pnlChecklistContainer Is Nothing Then
                    pnlChecklistContainer = New Panel With {
                        .Dock = DockStyle.Bottom,
                        .Height = 250,
                        .Padding = New Padding(10),
                        .BackColor = Color.White
                    }
                    
                    Dim lblHeader As New Label With {
                        .Text = "Task Checklist (Admin Defined)",
                        .Font = New Font("Segoe UI", 10.0!, FontStyle.Bold),
                        .ForeColor = Color.FromArgb(15, 23, 42),
                        .Dock = DockStyle.Top,
                        .Height = 30
                    }
                    
                    dgvChecklist = New DataGridView With {
                        .Dock = DockStyle.Fill,
                        .AllowUserToAddRows = False,
                        .AllowUserToDeleteRows = False,
                        .AutoGenerateColumns = False,
                        .BackgroundColor = Color.White,
                        .BorderStyle = BorderStyle.None,
                        .RowHeadersVisible = False,
                        .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                        .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                        .MultiSelect = False
                    }
                    
                    dgvChecklist.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "colId", .DataPropertyName = "ChecklistId", .Visible = False})
                    dgvChecklist.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "colDesc", .HeaderText = "Task Description", .DataPropertyName = "ItemDescription", .ReadOnly = True, .FillWeight = 40})
                    dgvChecklist.Columns.Add(New DataGridViewCheckBoxColumn With {.Name = "colReqProof", .HeaderText = "Requires Proof", .DataPropertyName = "RequiresProof", .ReadOnly = True, .FillWeight = 10})
                    dgvChecklist.Columns.Add(New DataGridViewCheckBoxColumn With {.Name = "colCompleted", .HeaderText = "Completed", .DataPropertyName = "IsCompleted", .ReadOnly = False, .FillWeight = 10})
                    dgvChecklist.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "colRemarks", .HeaderText = "Remarks", .DataPropertyName = "Remarks", .FillWeight = 25})
                    
                    dgvChecklist.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "colProofPath", .DataPropertyName = "AttachmentPath", .Visible = False})
                    
                    Dim btnUploadCol As New DataGridViewButtonColumn With {
                        .Name = "colUpload",
                        .HeaderText = "Proof",
                        .Text = "Upload",
                        .UseColumnTextForButtonValue = True,
                        .FillWeight = 15
                    }
                    dgvChecklist.Columns.Add(btnUploadCol)

                    Dim btnCol As New DataGridViewButtonColumn With {
                        .Name = "colAction",
                        .HeaderText = "Action",
                        .Text = "Complete",
                        .UseColumnTextForButtonValue = True,
                        .FillWeight = 15
                    }
                    dgvChecklist.Columns.Add(btnCol)
                    
                    AddHandler dgvChecklist.CellContentClick, AddressOf OnChecklistCellContentClick
                    
                    Dim pnlBottom As New Panel With {
                        .Dock = DockStyle.Bottom,
                        .Height = 45,
                        .Padding = New Padding(0, 10, 0, 0)
                    }
                    Dim btnSaveChecklist As New Button With {
                        .Text = "Save Checklist Changes",
                        .Dock = DockStyle.Right,
                        .Width = 180,
                        .BackColor = Color.FromArgb(15, 23, 42),
                        .ForeColor = Color.White,
                        .FlatStyle = FlatStyle.Flat,
                        .Font = New Font("Segoe UI", 9.0!, FontStyle.Bold),
                        .Cursor = Cursors.Hand
                    }
                    AddHandler btnSaveChecklist.Click, AddressOf OnSaveChecklistClick
                    pnlBottom.Controls.Add(btnSaveChecklist)
                    
                    If CurrentUserContext.IsAdmin OrElse CurrentUserContext.IsOwner Then
                        Dim btnDownloadAllProofs As New Button With {
                            .Text = "Download All Proofs",
                            .Dock = DockStyle.Left,
                            .Width = 180,
                            .BackColor = Color.FromArgb(14, 165, 233),
                            .ForeColor = Color.White,
                            .FlatStyle = FlatStyle.Flat,
                            .Font = New Font("Segoe UI", 9.0!, FontStyle.Bold),
                            .Cursor = Cursors.Hand
                        }
                        AddHandler btnDownloadAllProofs.Click, AddressOf OnDownloadAllProofsClick
                        pnlBottom.Controls.Add(btnDownloadAllProofs)
                    End If

                    pnlChecklistContainer.Controls.Add(dgvChecklist)
                    pnlChecklistContainer.Controls.Add(pnlBottom)
                    pnlChecklistContainer.Controls.Add(lblHeader)
                    Me.Controls.Add(pnlChecklistContainer)
                    pnlChecklistContainer.BringToFront()
                End If
                
                dgvChecklist.DataSource = checklist
                
                ' Disable buttons for completed items
                For Each row As DataGridViewRow In dgvChecklist.Rows
                    Dim isComp = CBool(row.Cells("colCompleted").Value)
                    Dim hasProof = If(row.Cells("colProofPath").Value IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(row.Cells("colProofPath").Value.ToString()), True, False)
                    
                    If hasProof Then
                        Dim btnUpload = TryCast(row.Cells("colUpload"), DataGridViewButtonCell)
                        If btnUpload IsNot Nothing Then
                            btnUpload.UseColumnTextForButtonValue = False
                            If CurrentUserContext.IsAdmin OrElse CurrentUserContext.IsOwner Then
                                btnUpload.Value = "View Proof"
                                row.Cells("colUpload").ReadOnly = False
                            Else
                                btnUpload.Value = "Attached"
                                row.Cells("colUpload").ReadOnly = True
                            End If
                        End If
                    End If

                    If hasProof AndAlso Not isComp Then
                        ' Pending Verification State
                        row.DefaultCellStyle.BackColor = Color.LightGoldenrodYellow
                        If CurrentUserContext.IsAdmin OrElse CurrentUserContext.IsOwner Then
                            Dim btnAction = TryCast(row.Cells("colAction"), DataGridViewButtonCell)
                            If btnAction IsNot Nothing Then
                                btnAction.UseColumnTextForButtonValue = False
                                btnAction.Value = "Verify"
                            End If
                            row.Cells("colAction").ReadOnly = False
                        Else
                            Dim btnAction = TryCast(row.Cells("colAction"), DataGridViewButtonCell)
                            If btnAction IsNot Nothing Then
                                btnAction.UseColumnTextForButtonValue = False
                                btnAction.Value = "Pending"
                            End If
                            row.Cells("colAction").ReadOnly = True
                        End If
                        row.Cells("colCompleted").ReadOnly = True
                    End If

                    If isComp Then
                        ' Fully Approved/Completed State
                        Dim btnAction = TryCast(row.Cells("colAction"), DataGridViewButtonCell)
                        If btnAction IsNot Nothing Then
                            btnAction.UseColumnTextForButtonValue = False
                            btnAction.Value = "Verified"
                        End If
                        row.Cells("colAction").ReadOnly = True
                        row.Cells("colRemarks").ReadOnly = True
                        row.Cells("colCompleted").ReadOnly = True
                        row.DefaultCellStyle.BackColor = Color.LightGreen
                    End If
                Next
            Catch ex As Exception
                _appLogger.LogError($"[TaskWorkflowViewer] RenderChecklistItemsAsync failed for TaskId={taskId}: {ex.Message}", "FrmTaskWorkspace", ex)
            End Try
        End Function

        Private Async Sub OnChecklistCellContentClick(sender As Object, e As DataGridViewCellEventArgs)
            If e.RowIndex < 0 Then Return
            
            Dim isActionCol = (e.ColumnIndex = dgvChecklist.Columns("colAction").Index)
            Dim isUploadCol = (e.ColumnIndex = dgvChecklist.Columns("colUpload").Index)
            
            If Not isActionCol AndAlso Not isUploadCol Then Return

            Dim row = dgvChecklist.Rows(e.RowIndex)
            
            Dim isComp = False
            If row.Cells("colCompleted").Value IsNot Nothing AndAlso Not IsDBNull(row.Cells("colCompleted").Value) Then
                Boolean.TryParse(row.Cells("colCompleted").Value.ToString(), isComp)
            End If

            If isUploadCol Then
                Dim hasProof = (row.Cells("colProofPath").Value IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(row.Cells("colProofPath").Value.ToString()))
                If hasProof Then
                    If CurrentUserContext.IsAdmin OrElse CurrentUserContext.IsOwner Then
                        Dim path = row.Cells("colProofPath").Value.ToString()
                        If IO.File.Exists(path) Then
                            Process.Start(New ProcessStartInfo(path) With {.UseShellExecute = True})
                        Else
                            Forms.Common.FrmInAppAlert.ShowModal(Me, "Not Found", "Proof file could not be found.", Forms.Common.AlertType.ErrorAlert, actionText:="OK")
                        End If
                    Else
                        Forms.Common.FrmInAppAlert.ShowModal(Me, "Access Denied", "Only administrators can preview attached proof files.", Forms.Common.AlertType.WarningAlert, actionText:="OK")
                    End If
                    Return
                End If
                
                If row.Cells("colUpload").ReadOnly OrElse isComp Then Return
                Await HandleProofUpload(row)
                Return
            End If
            
            ' Handle Verify/Reject Action Button
            Dim btnCell = TryCast(row.Cells("colAction"), DataGridViewButtonCell)
            If btnCell IsNot Nothing AndAlso btnCell.Value IsNot Nothing AndAlso btnCell.Value.ToString() = "Verify" Then
                If CurrentUserContext.IsAdmin OrElse CurrentUserContext.IsOwner Then
                    Dim result = MessageBox.Show("Do you want to APPROVE this proof? Click YES to Approve, NO to Reject.", "Verify Proof", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question)
                    Dim rejChecklistId = CInt(row.Cells("colId").Value)
                    Dim rejUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)
                    
                    If result = DialogResult.Yes Then
                        ' Approve
                        Try
                            Dim apprRemarks = If(row.Cells("colRemarks").Value IsNot Nothing, row.Cells("colRemarks").Value.ToString(), "")
                            Dim success = Await _taskService.CompleteChecklistItemAsync(rejChecklistId, rejUserId, apprRemarks, "")
                            If success Then
                                Forms.Common.FrmInAppAlert.ShowModal(Me, "Item Approved", "Checklist item has been verified and approved.", Forms.Common.AlertType.SuccessAlert, actionText:="OK")
                                Await RenderChecklistItemsAsync(_taskId)
                            End If
                        Catch ex As Exception
                            Forms.Common.FrmInAppAlert.ShowModal(Me, "Error", $"Failed to approve item: {ex.Message}", Forms.Common.AlertType.ErrorAlert, actionText:="OK")
                        End Try
                    ElseIf result = DialogResult.No Then
                        ' Reject
                        Dim rejectRemarks = InputBox("Please enter the reason for rejection (this will be shown to the staff):", "Rejection Reason", "Invalid proof provided.")
                        If String.IsNullOrWhiteSpace(rejectRemarks) Then Return ' Cancelled
                        Try
                            Dim success = Await _taskService.InvalidateChecklistItemAsync(rejChecklistId, rejUserId, rejectRemarks)
                            If success Then
                                Forms.Common.FrmInAppAlert.ShowModal(Me, "Item Rejected", "Checklist item marked as invalid.", Forms.Common.AlertType.SuccessAlert, actionText:="OK")
                                Await RenderChecklistItemsAsync(_taskId)
                            End If
                        Catch ex As Exception
                            Forms.Common.FrmInAppAlert.ShowModal(Me, "Error", $"Failed to reject item: {ex.Message}", Forms.Common.AlertType.ErrorAlert, actionText:="OK")
                        End Try
                    End If
                End If
                Return
            End If
            
            ' If it's already verified, do nothing on click
            If btnCell IsNot Nothing AndAlso btnCell.Value IsNot Nothing AndAlso btnCell.Value.ToString() = "Verified" Then
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Already Verified", "This item has already been verified and locked.", Forms.Common.AlertType.InfoAlert, actionText:="OK")
                Return
            End If
            
            Dim checklistId = CInt(row.Cells("colId").Value)
            Dim requiresProof = CBool(row.Cells("colReqProof").Value)
            Dim remarks = If(row.Cells("colRemarks").Value IsNot Nothing, row.Cells("colRemarks").Value.ToString(), "")
            Dim attachmentPath = If(row.Cells("colProofPath").Value IsNot Nothing, row.Cells("colProofPath").Value.ToString(), "")
            
            If requiresProof AndAlso String.IsNullOrWhiteSpace(attachmentPath) Then
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Proof Required", "This checklist item requires a proof document to be uploaded before completion.", Forms.Common.AlertType.WarningAlert, actionText:="OK")
                Return
            End If
            
            Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)
            
            Try
                Dim success = Await _taskService.CompleteChecklistItemAsync(checklistId, currentUserId, remarks, attachmentPath)
                If success Then
                    Forms.Common.FrmInAppAlert.ShowModal(Me, "Item Completed", "Checklist item marked as complete.", Forms.Common.AlertType.SuccessAlert, actionText:="OK")
                    Await RenderChecklistItemsAsync(_taskId)
                End If
            Catch ex As Exception
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Error", $"Failed to complete item: {ex.Message}", Forms.Common.AlertType.ErrorAlert, actionText:="OK")
            End Try
        End Sub

        Private Async Function HandleProofUpload(row As DataGridViewRow) As Task
            Using ofd As New OpenFileDialog()
                ofd.Title = "Select Proof Document"
                ofd.Filter = "All Files (*.*)|*.*|PDF Files (*.pdf)|*.pdf|Image Files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png"
                
                If ofd.ShowDialog(Me) = DialogResult.OK Then
                    Try
                        If Not Directory.Exists(_uploadDirectory) Then
                            Directory.CreateDirectory(_uploadDirectory)
                        End If
                        
                        Dim fileExt = Path.GetExtension(ofd.FileName)
                        Dim safeFileName = Guid.NewGuid().ToString("N") & fileExt
                        Dim targetPath = Path.Combine(_uploadDirectory, safeFileName)
                        
                        File.Copy(ofd.FileName, targetPath, True)
                        
                        row.Cells("colProofPath").Value = targetPath
                        ' Do NOT mark it complete automatically, wait for admin verification
                        
                        Dim btnUpload = TryCast(row.Cells("colUpload"), DataGridViewButtonCell)
                        If btnUpload IsNot Nothing Then
                            btnUpload.UseColumnTextForButtonValue = False
                            btnUpload.Value = "Attached"
                        End If
                        
                        ' Update proof path in DB without setting IsCompleted
                        Dim checklistId = CInt(row.Cells("colId").Value)
                        Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)
                        Dim remarks = If(row.Cells("colRemarks").Value IsNot Nothing, row.Cells("colRemarks").Value.ToString(), "")
                        
                        Dim success = Await _taskService.UploadProofAsync(checklistId, currentUserId, remarks, targetPath)
                        If success Then
                            Forms.Common.FrmInAppAlert.ShowModal(Me, "Proof Uploaded", "Proof uploaded successfully. Pending admin verification.", Forms.Common.AlertType.SuccessAlert, actionText:="OK")
                            Await RenderChecklistItemsAsync(_taskId)
                        End If
                        
                    Catch ex As Exception
                        _appLogger.LogError($"[TaskWorkflowViewer] Failed to copy proof file: {ex.Message}", "FrmTaskWorkspace", ex)
                        Forms.Common.FrmInAppAlert.ShowModal(Me, "Upload Failed", $"Could not save the proof file: {ex.Message}", Forms.Common.AlertType.ErrorAlert, actionText:="OK")
                    End Try
                End If
            End Using
        End Function

        Private Async Sub OnSaveChecklistClick(sender As Object, e As EventArgs)
            If dgvChecklist Is Nothing Then Return
            
            Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)
            Dim saveCount = 0
            Dim errors = 0
            
            For Each row As DataGridViewRow In dgvChecklist.Rows
                ' Skip if already completed and read-only
                If row.Cells("colAction").ReadOnly Then Continue For
                
                Dim isComp = False
                If row.Cells("colCompleted").Value IsNot Nothing AndAlso Not IsDBNull(row.Cells("colCompleted").Value) Then
                    Boolean.TryParse(row.Cells("colCompleted").Value.ToString(), isComp)
                End If
                
                If isComp Then
                    Dim checklistId = CInt(row.Cells("colId").Value)
                    Dim requiresProof = CBool(row.Cells("colReqProof").Value)
                    Dim remarks = If(row.Cells("colRemarks").Value IsNot Nothing, row.Cells("colRemarks").Value.ToString(), "")
                    Dim attachmentPath = If(row.Cells("colProofPath").Value IsNot Nothing, row.Cells("colProofPath").Value.ToString(), "")
                    
                    If requiresProof AndAlso String.IsNullOrWhiteSpace(attachmentPath) Then
                        Forms.Common.FrmInAppAlert.ShowModal(Me, "Proof Required", $"Checklist item '{row.Cells("colDesc").Value}' requires proof but none is attached. It will not be saved.", Forms.Common.AlertType.WarningAlert, actionText:="OK")
                        errors += 1
                        Continue For
                    End If
                    
                    Try
                        Dim success = Await _taskService.CompleteChecklistItemAsync(checklistId, currentUserId, remarks, attachmentPath)
                        If success Then saveCount += 1
                    Catch ex As Exception
                        errors += 1
                    End Try
                End If
            Next
            
            If saveCount > 0 Then
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Changes Saved", $"Successfully saved {saveCount} checklist items.", Forms.Common.AlertType.SuccessAlert, actionText:="OK")
                Await RenderChecklistItemsAsync(_taskId)
            ElseIf errors = 0 Then
                Forms.Common.FrmInAppAlert.ShowModal(Me, "No Changes", "No new completed items to save. Please tick the 'Completed' checkboxes for the items you want to save.", Forms.Common.AlertType.InfoAlert, actionText:="OK")
            End If
        End Sub

        Private Sub OnDownloadAllProofsClick(sender As Object, e As EventArgs)
            If dgvChecklist Is Nothing OrElse dgvChecklist.Rows.Count = 0 Then Return
            
            Dim proofPaths As New List(Of String)()
            For Each row As DataGridViewRow In dgvChecklist.Rows
                Dim path = TryCast(row.Cells("colProofPath").Value, String)
                If Not String.IsNullOrWhiteSpace(path) AndAlso IO.File.Exists(path) Then
                    proofPaths.Add(path)
                End If
            Next
            
            If proofPaths.Count = 0 Then
                Forms.Common.FrmInAppAlert.ShowModal(Me, "No Proofs", "There are no proofs attached to this task.", Forms.Common.AlertType.InfoAlert, actionText:="OK")
                Return
            End If
            
            Using fbd As New FolderBrowserDialog()
                fbd.Description = "Select a folder to download all proofs"
                If fbd.ShowDialog() = DialogResult.OK Then
                    Dim destPath = fbd.SelectedPath
                    Dim copiedCount = 0
                    
                    Try
                        For Each sourceFile In proofPaths
                            Dim fileName = IO.Path.GetFileName(sourceFile)
                            Dim destFile = IO.Path.Combine(destPath, $"Task_{_taskId}_{fileName}")
                            If IO.File.Exists(destFile) Then
                                destFile = IO.Path.Combine(destPath, $"Task_{_taskId}_{Date.Now.Ticks}_{fileName}")
                            End If
                            IO.File.Copy(sourceFile, destFile)
                            copiedCount += 1
                        Next
                        Forms.Common.FrmInAppAlert.ShowModal(Me, "Download Complete", $"Successfully downloaded {copiedCount} proof(s) to the selected folder.", Forms.Common.AlertType.SuccessAlert, actionText:="OK")
                        Process.Start("explorer.exe", destPath)
                    Catch ex As Exception
                        Forms.Common.FrmInAppAlert.ShowModal(Me, "Error", $"An error occurred while downloading: {ex.Message}", Forms.Common.AlertType.ErrorAlert, actionText:="OK")
                    End Try
                End If
            End Using
        End Sub
    End Class
End Namespace
