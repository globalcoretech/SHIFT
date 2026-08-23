Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Linq
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports StaffAutomation.BLL.Logging
Imports StaffAutomation.BLL.Services
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Logging
Imports StaffAutomation.Core.Security
Imports StaffAutomation.DAL.Configuration
Imports StaffAutomation.DAL.Core
Imports StaffAutomation.DAL.Repositories
Imports StaffAutomation.WinForms.Forms.Common
Imports StaffAutomation.WinForms.Forms.Main

Namespace Forms.Tasks
    ''' <summary>
    ''' Right-side slide-over Task Creation Panel UserControl.
    ''' Allows staff to define task details, client selection, priority, due date, staff assignment,
    ''' and presentation-level workflow preview without breaking out of the Daily Tasks workspace.
    ''' </summary>
    Public Class CreateTaskPanel
        Inherits UserControl

        ' Injected Services & Repositories
        Private ReadOnly _taskService As ITaskManagementService
        Private ReadOnly _clientService As IClientService
        Private ReadOnly _userRepo As DAL.Interfaces.IUserRepository
        Private ReadOnly _appLogger As IAppLogger

        ' Events
        Public Event TaskCreated(sender As Object, newTaskId As Integer)
        Public Event Cancelled(sender As Object, e As EventArgs)

        ' State Management
        Private _isDirty As Boolean = False
        Private _isSubmitting As Boolean = False
        Private _isLoading As Boolean = False

        ' UI Components
        Private pnlHeader As Panel
        Private lblTitle As Label
        Private lblClose As Label

        Private pnlFormScroll As Panel

        ' Client Section
        Private lblClientHeader As Label
        Private lblClientTag As Label
        Private cboClient As ComboBox
        Private lblGstinRefTag As Label
        Private txtGstinRef As TextBox

        ' Task Information Section
        Private lblTaskInfoHeader As Label
        Private lblTaskTitleTag As Label
        Private txtTaskTitle As TextBox

        Private lblTaskTypeTag As Label
        Private cboTaskType As ComboBox

        ' Workflow Steps Preview Box
        Private pnlWorkflowPreview As Panel
        Private lblWorkflowHeader As Label
        Private lblWorkflowSteps As Label

        ' Task Configuration Section
        Private lblConfigHeader As Label
        Private lblPriorityTag As Label
        Private cboPriority As ComboBox

        Private lblDueDateTag As Label
        Private dtpDueDate As DateTimePicker

        Private lblAssigneeTag As Label
        Private cboAssignee As ComboBox

        ' Description Section
        Private lblDescriptionTag As Label
        Private txtDescription As TextBox

        ' Sticky Footer Bar
        Private pnlFooter As Panel
        Private btnCancel As ModernButton
        Private btnCreateTask As ModernButton

        ' Combo Helper Classes
        Private Class ClientComboItem
            Public Property ClientId As Integer
            Public Property ClientName As String = String.Empty
            Public Property GstinOrPan As String = String.Empty

            Public Overrides Function ToString() As String
                If Not String.IsNullOrEmpty(GstinOrPan) Then
                    Return $"{ClientName} ({GstinOrPan})"
                Else
                    Return ClientName
                End If
            End Function
        End Class

        Private Class UserComboItem
            Public Property UserId As Integer
            Public Property FullName As String = String.Empty

            Public Overrides Function ToString() As String
                Return FullName
            End Function
        End Class

        Public Sub New(taskService As ITaskManagementService, clientService As IClientService, userRepo As DAL.Interfaces.IUserRepository, appLogger As IAppLogger)
            _taskService = taskService
            _clientService = clientService
            _userRepo = userRepo
            _appLogger = appLogger

            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.SuspendLayout()
            Me.Dock = DockStyle.Fill
            Me.BackColor = Color.White
            Me.Padding = New Padding(14)
            Me.Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular)

            ' Paint Border
            AddHandler Me.Paint, Sub(s, e)
                                     e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
                                     Using p As New Pen(Color.FromArgb(226, 232, 240), 1)
                                         e.Graphics.DrawRectangle(p, 0, 0, Me.Width - 1, Me.Height - 1)
                                     End Using
                                 End Sub

            ' =========================================================================
            ' 1. HEADER BAR
            ' =========================================================================
            pnlHeader = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 32,
                .BackColor = Color.White
            }
            lblTitle = New Label() With {
                .Text = "← CREATE NEW TASK",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(0, 4),
                .AutoSize = True
            }
            lblClose = New Label() With {
                .Text = "✕",
                .Font = New Font(ThemeConstants.FontNameDefault, 10.0!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(148, 163, 184),
                .Location = New Point(pnlHeader.Width - 24, 2),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right,
                .Cursor = Cursors.Hand
            }
            AddHandler lblClose.Click, AddressOf OnCancelClick
            pnlHeader.Controls.Add(lblClose)
            pnlHeader.Controls.Add(lblTitle)

            ' =========================================================================
            ' 2. STICKY FOOTER ACTION BAR
            ' =========================================================================
            pnlFooter = New Panel() With {
                .Dock = DockStyle.Bottom,
                .Height = 54,
                .BackColor = Color.White,
                .Padding = New Padding(0, 10, 0, 0)
            }
            AddHandler pnlFooter.Paint, Sub(s, e)
                                           Using p As New Pen(Color.FromArgb(226, 232, 240), 1)
                                               e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0)
                                           End Using
                                       End Sub

            btnCancel = New ModernButton() With {
                .Text = "Cancel",
                .Scheme = ModernButton.ButtonScheme.Secondary,
                .Size = New Size(90, 34),
                .Location = New Point(0, 10)
            }
            AddHandler btnCancel.Click, AddressOf OnCancelClick

            btnCreateTask = New ModernButton() With {
                .Text = "Create Task →",
                .Scheme = ModernButton.ButtonScheme.Custom,
                .BackColor = Color.FromArgb(16, 185, 129),
                .ForeColor = Color.White,
                .Size = New Size(140, 34),
                .Location = New Point(100, 10)
            }
            AddHandler btnCreateTask.Click, AddressOf OnCreateTaskClick

            pnlFooter.Controls.Add(btnCreateTask)
            pnlFooter.Controls.Add(btnCancel)

            ' =========================================================================
            ' 3. SCROLLABLE FORM CONTENT
            ' =========================================================================
            pnlFormScroll = New Panel() With {
                .Dock = DockStyle.Fill,
                .AutoScroll = True,
                .BackColor = Color.White,
                .Padding = New Padding(0, 6, 8, 6)
            }

            Dim yPos As Integer = 4

            ' SECTION A: CLIENT INFORMATION
            lblClientHeader = CreateSectionHeader("CLIENT INFORMATION", yPos)
            yPos += 22

            lblClientTag = CreateFieldLabel("Client Name *", yPos)
            yPos += 18
            cboClient = New ComboBox() With {
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = New Font(ThemeConstants.FontNameDefault, 8.75!, FontStyle.Regular),
                .Location = New Point(0, yPos),
                .Size = New Size(310, 24)
            }
            AddHandler cboClient.SelectedIndexChanged, AddressOf OnClientSelectionChanged
            yPos += 30

            lblGstinRefTag = CreateFieldLabel("GSTIN / PAN Reference", yPos)
            yPos += 18
            txtGstinRef = New TextBox() With {
                .Font = New Font(ThemeConstants.FontNameDefault, 8.75!, FontStyle.Regular),
                .Location = New Point(0, yPos),
                .Size = New Size(310, 23),
                .ReadOnly = True,
                .BackColor = Color.FromArgb(248, 250, 252),
                .ForeColor = ThemeConstants.TextSecondary
            }
            yPos += 34

            ' SECTION B: TASK DETAILS
            lblTaskInfoHeader = CreateSectionHeader("TASK INFORMATION", yPos)
            yPos += 22

            lblTaskTitleTag = CreateFieldLabel("Task Title *", yPos)
            yPos += 18
            txtTaskTitle = New TextBox() With {
                .Font = New Font(ThemeConstants.FontNameDefault, 8.75!, FontStyle.Regular),
                .Location = New Point(0, yPos),
                .Size = New Size(310, 23)
            }
            AddHandler txtTaskTitle.TextChanged, AddressOf OnFieldValueChanged
            yPos += 30

            lblTaskTypeTag = CreateFieldLabel("Task Type / Category *", yPos)
            yPos += 18
            cboTaskType = New ComboBox() With {
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = New Font(ThemeConstants.FontNameDefault, 8.75!, FontStyle.Regular),
                .Location = New Point(0, yPos),
                .Size = New Size(310, 24)
            }
            cboTaskType.Items.AddRange(New Object() {
                "GSTR-3B Filing",
                "GSTR-1 Filing",
                "Income Tax Return (ITR)",
                "TDS Quarterly Return (26Q/27Q)",
                "GST Verification & Audit",
                "ROC Annual Filing",
                "Statutory Audit",
                "Accounting & Bookkeeping",
                "Client Onboarding",
                "Other Task"
            })
            AddHandler cboTaskType.SelectedIndexChanged, AddressOf OnTaskTypeSelectionChanged
            yPos += 34

            ' WORKFLOW STEPS PREVIEW BOX
            pnlWorkflowPreview = New Panel() With {
                .Location = New Point(0, yPos),
                .Size = New Size(310, 95),
                .BackColor = Color.FromArgb(238, 242, 255),
                .Padding = New Padding(8)
            }
            AddHandler pnlWorkflowPreview.Paint, Sub(s, e)
                                                     e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
                                                     Using p As New Pen(Color.FromArgb(199, 210, 254), 1)
                                                         e.Graphics.DrawRectangle(p, 0, 0, pnlWorkflowPreview.Width - 1, pnlWorkflowPreview.Height - 1)
                                                     End Using
                                                 End Sub

            lblWorkflowHeader = New Label() With {
                .Text = "📋 WORKFLOW STEPS PREVIEW",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.0!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(49, 46, 129),
                .Location = New Point(8, 6),
                .AutoSize = True
            }
            lblWorkflowSteps = New Label() With {
                .Text = "✓ Select a task type to preview workflow steps",
                .Font = New Font(ThemeConstants.FontNameDefault, 7.75!, FontStyle.Regular),
                .ForeColor = Color.FromArgb(55, 48, 163),
                .Location = New Point(8, 24),
                .Size = New Size(294, 64)
            }
            pnlWorkflowPreview.Controls.Add(lblWorkflowSteps)
            pnlWorkflowPreview.Controls.Add(lblWorkflowHeader)
            pnlFormScroll.Controls.Add(pnlWorkflowPreview)
            yPos += 105

            ' SECTION C: TASK CONFIGURATION
            lblConfigHeader = CreateSectionHeader("TASK CONFIGURATION", yPos)
            yPos += 22

            lblPriorityTag = CreateFieldLabel("Priority *", yPos)
            yPos += 18
            cboPriority = New ComboBox() With {
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = New Font(ThemeConstants.FontNameDefault, 8.75!, FontStyle.Regular),
                .Location = New Point(0, yPos),
                .Size = New Size(310, 24)
            }
            cboPriority.Items.AddRange(New Object() {"Medium", "High", "Urgent", "Low"})
            cboPriority.SelectedIndex = 0 ' Default Medium
            AddHandler cboPriority.SelectedIndexChanged, AddressOf OnFieldValueChanged
            yPos += 30

            lblDueDateTag = CreateFieldLabel("Due Date *", yPos)
            yPos += 18
            dtpDueDate = New DateTimePicker() With {
                .Format = DateTimePickerFormat.Custom,
                .CustomFormat = "dd MMM yyyy",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.75!, FontStyle.Regular),
                .Location = New Point(0, yPos),
                .Size = New Size(310, 23),
                .Value = DateTime.Today.AddDays(3) ' Deterministic Default: Today + 3 Days
            }
            AddHandler dtpDueDate.ValueChanged, AddressOf OnFieldValueChanged
            yPos += 30

            lblAssigneeTag = CreateFieldLabel("Assign To Staff *", yPos)
            yPos += 18
            cboAssignee = New ComboBox() With {
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = New Font(ThemeConstants.FontNameDefault, 8.75!, FontStyle.Regular),
                .Location = New Point(0, yPos),
                .Size = New Size(310, 24)
            }
            AddHandler cboAssignee.SelectedIndexChanged, AddressOf OnFieldValueChanged
            yPos += 34

            ' SECTION D: DESCRIPTION
            lblDescriptionTag = CreateFieldLabel("Description / Special Instructions", yPos)
            yPos += 18
            txtDescription = New TextBox() With {
                .Font = New Font(ThemeConstants.FontNameDefault, 8.75!, FontStyle.Regular),
                .Location = New Point(0, yPos),
                .Size = New Size(310, 60),
                .Multiline = True,
                .ScrollBars = ScrollBars.Vertical
            }
            AddHandler txtDescription.TextChanged, AddressOf OnFieldValueChanged
            yPos += 70

            pnlFormScroll.Controls.Add(lblDescriptionTag)
            pnlFormScroll.Controls.Add(txtDescription)
            pnlFormScroll.Controls.Add(lblAssigneeTag)
            pnlFormScroll.Controls.Add(cboAssignee)
            pnlFormScroll.Controls.Add(lblDueDateTag)
            pnlFormScroll.Controls.Add(dtpDueDate)
            pnlFormScroll.Controls.Add(lblPriorityTag)
            pnlFormScroll.Controls.Add(cboPriority)
            pnlFormScroll.Controls.Add(lblConfigHeader)

            pnlFormScroll.Controls.Add(lblTaskTypeTag)
            pnlFormScroll.Controls.Add(cboTaskType)
            pnlFormScroll.Controls.Add(lblTaskTitleTag)
            pnlFormScroll.Controls.Add(txtTaskTitle)
            pnlFormScroll.Controls.Add(lblTaskInfoHeader)

            pnlFormScroll.Controls.Add(lblGstinRefTag)
            pnlFormScroll.Controls.Add(txtGstinRef)
            pnlFormScroll.Controls.Add(lblClientTag)
            pnlFormScroll.Controls.Add(cboClient)
            pnlFormScroll.Controls.Add(lblClientHeader)

            ' Final Container Assembly
            Me.Controls.Add(pnlFormScroll)
            Me.Controls.Add(pnlFooter)
            Me.Controls.Add(pnlHeader)

            Me.ResumeLayout(False)
        End Sub

        Private Function CreateSectionHeader(text As String, topPos As Integer) As Label
            Dim lbl As New Label() With {
                .Text = text,
                .Font = New Font(ThemeConstants.FontNameDefault, 8.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.PrimaryAccent,
                .Location = New Point(0, topPos),
                .AutoSize = True
            }
            pnlFormScroll.Controls.Add(lbl)
            Return lbl
        End Function

        Private Function CreateFieldLabel(text As String, topPos As Integer) As Label
            Dim lbl As New Label() With {
                .Text = text,
                .Font = New Font(ThemeConstants.FontNameDefault, 8.25!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(0, topPos),
                .AutoSize = True
            }
            pnlFormScroll.Controls.Add(lbl)
            Return lbl
        End Function

        ''' <summary>
        ''' Asynchronously populates Clients and Staff dropdowns from authoritative service layer.
        ''' Resets form fields and dirty flags.
        ''' </summary>
        Public Async Function InitializeDataAsync() As System.Threading.Tasks.Task
            _isLoading = True
            Try
                ' 1. Load Clients
                cboClient.Items.Clear()
                cboClient.Items.Add("-- Select Client --")
                If _clientService IsNot Nothing Then
                    Dim clients = Await _clientService.GetAllClientsAsync(includeDeleted:=False)
                    If clients IsNot Nothing Then
                        For Each c As ClientDto In clients
                            If c.IsActive Then
                                Dim gstinRef As String = If(Not String.IsNullOrEmpty(c.Gstin), c.Gstin, If(Not String.IsNullOrEmpty(c.PanNumber), c.PanNumber, c.ClientCode))
                                cboClient.Items.Add(New ClientComboItem With {
                                    .ClientId = c.ClientId,
                                    .ClientName = c.ClientName,
                                    .GstinOrPan = gstinRef
                                })
                            End If
                        Next
                    End If
                End If
                cboClient.SelectedIndex = 0

                ' 2. Load Staff Members
                cboAssignee.Items.Clear()
                cboAssignee.Items.Add("-- Select Assignee --")
                If _userRepo IsNot Nothing Then
                    Dim users = Await _userRepo.GetAllAsync()
                    If users IsNot Nothing Then
                        For Each u As UserEntity In users
                            If u.IsActive Then
                                cboAssignee.Items.Add(New UserComboItem With {
                                    .UserId = u.UserId,
                                    .FullName = u.FullName
                                })
                            End If
                        Next
                    End If
                End If

                ' Default Assignee to Current Logged In User if present
                Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)
                For i As Integer = 1 To cboAssignee.Items.Count - 1
                    Dim item = TryCast(cboAssignee.Items(i), UserComboItem)
                    If item IsNot Nothing AndAlso item.UserId = currentUserId Then
                        cboAssignee.SelectedIndex = i
                        Exit For
                    End If
                Next
                If cboAssignee.SelectedIndex <= 0 AndAlso cboAssignee.Items.Count > 1 Then
                    cboAssignee.SelectedIndex = 1
                End If

                ' 3. Reset Fields
                txtGstinRef.Text = String.Empty
                txtTaskTitle.Text = String.Empty
                cboTaskType.SelectedIndex = 0
                cboPriority.SelectedIndex = 0 ' Medium
                dtpDueDate.Value = DateTime.Today.AddDays(3) ' Deterministic Default: Today + 3 Days
                txtDescription.Text = String.Empty

                UpdateWorkflowPreviewText("GSTR-3B Filing")

                _isDirty = False
            Catch ex As Exception
                _appLogger.LogError($"Failed to initialize CreateTaskPanel dropdowns: {ex.Message}")
            Finally
                _isLoading = False
            End Try
        End Function

        Private Sub OnClientSelectionChanged(sender As Object, e As EventArgs)
            If _isLoading Then Return
            MarkDirty()

            Dim item = TryCast(cboClient.SelectedItem, ClientComboItem)
            If item IsNot Nothing Then
                txtGstinRef.Text = item.GstinOrPan
            Else
                txtGstinRef.Text = String.Empty
            End If
        End Sub

        Private Sub OnTaskTypeSelectionChanged(sender As Object, e As EventArgs)
            If _isLoading Then Return
            MarkDirty()

            Dim selectedType = cboTaskType.SelectedItem.ToString()
            UpdateWorkflowPreviewText(selectedType)

            ' Auto-suggest Task Title if title is currently empty
            If String.IsNullOrWhiteSpace(txtTaskTitle.Text) Then
                Dim clientItem = TryCast(cboClient.SelectedItem, ClientComboItem)
                Dim cName = If(clientItem IsNot Nothing, clientItem.ClientName, "Client")
                txtTaskTitle.Text = $"{selectedType} - {DateTime.Today.ToString("MMM yyyy")}"
            End If
        End Sub

        Private Sub UpdateWorkflowPreviewText(taskType As String)
            Select Case taskType
                Case "GSTR-3B Filing"
                    lblWorkflowSteps.Text = $"• Verify purchase & sales data{Environment.NewLine}• Match with GSTR-2B ITC statement{Environment.NewLine}• Prepare return summary draft{Environment.NewLine}• File return on GST Govt Portal"
                Case "GSTR-1 Filing"
                    lblWorkflowSteps.Text = $"• Export B2B & B2C sales register{Environment.NewLine}• Validate HSN summary & tax rates{Environment.NewLine}• Generate JSON & upload to portal{Environment.NewLine}• File GSTR-1 return with EVC/DSC"
                Case "Income Tax Return (ITR)"
                    lblWorkflowSteps.Text = $"• Collect AIS / TIS & Form 26AS{Environment.NewLine}• Reconcile bank statements & TDS{Environment.NewLine}• Prepare tax computation & draft{Environment.NewLine}• File ITR & verify via EVC"
                Case "TDS Quarterly Return (26Q/27Q)"
                    lblWorkflowSteps.Text = $"• Collect deduction vouchers & Challans{Environment.NewLine}• Validate PAN numbers via Traces{Environment.NewLine}• Generate FVU file & verify errors{Environment.NewLine}• File return & generate Form 16/16A"
                Case "GST Verification & Audit"
                    lblWorkflowSteps.Text = $"• Verify GSTIN registration status{Environment.NewLine}• Cross-examine GSTR-3B vs 2A/2B{Environment.NewLine}• Audit turnover & liability mismatches{Environment.NewLine}• Prepare audit observations report"
                Case "ROC Annual Filing"
                    lblWorkflowSteps.Text = $"• Prepare AOC-4 & MGT-7 drafts{Environment.NewLine}• Attach Auditor's Report & Financials{Environment.NewLine}• Obtain Director DSC signatures{Environment.NewLine}• Upload MCA forms & pay fee"
                Case Else
                    lblWorkflowSteps.Text = $"• Review task instructions & scope{Environment.NewLine}• Execute operational work steps{Environment.NewLine}• Verify output with Senior Partner{Environment.NewLine}• Mark task complete upon delivery"
            End Select
        End Sub

        Private Sub OnFieldValueChanged(sender As Object, e As EventArgs)
            If _isLoading Then Return
            MarkDirty()
        End Sub

        Private Sub MarkDirty()
            _isDirty = True
        End Sub

        Private Sub OnCancelClick(sender As Object, e As EventArgs)
            If _isSubmitting Then Return

            If _isDirty Then
                Dim dlgRes = FrmInAppAlert.ShowModal(Me.FindForm(), "DISCARD UNSAVED CHANGES?", "You have unsaved task creation changes. Are you sure you want to cancel?", AlertType.WarningAlert, actionText:="DISCARD CHANGES", showCancel:=True, cancelText:="KEEP EDITING")
                If dlgRes <> DialogResult.OK Then Return
            End If

            _isDirty = False
            RaiseEvent Cancelled(Me, EventArgs.Empty)
        End Sub

        Private Async Sub OnCreateTaskClick(sender As Object, e As EventArgs)
            If _isSubmitting Then Return

            ' 1. Field Validations
            Dim clientItem = TryCast(cboClient.SelectedItem, ClientComboItem)
            If clientItem Is Nothing OrElse clientItem.ClientId <= 0 Then
                FrmInAppAlert.ShowModal(Me.FindForm(), "CLIENT REQUIRED", "Please select a valid Client for the task.", AlertType.WarningAlert, actionText:="OK")
                cboClient.Focus()
                Return
            End If

            If String.IsNullOrWhiteSpace(txtTaskTitle.Text) Then
                FrmInAppAlert.ShowModal(Me.FindForm(), "TASK TITLE REQUIRED", "Please enter a descriptive Task Title.", AlertType.WarningAlert, actionText:="OK")
                txtTaskTitle.Focus()
                Return
            End If

            If cboTaskType.SelectedIndex < 0 Then
                FrmInAppAlert.ShowModal(Me.FindForm(), "TASK TYPE REQUIRED", "Please select a Task Type / Category.", AlertType.WarningAlert, actionText:="OK")
                cboTaskType.Focus()
                Return
            End If

            Dim staffItem = TryCast(cboAssignee.SelectedItem, UserComboItem)
            If staffItem Is Nothing OrElse staffItem.UserId <= 0 Then
                FrmInAppAlert.ShowModal(Me.FindForm(), "STAFF ASSIGNEE REQUIRED", "Please assign the task to a staff member.", AlertType.WarningAlert, actionText:="OK")
                cboAssignee.Focus()
                Return
            End If

            If dtpDueDate.Value.Date < DateTime.Today Then
                FrmInAppAlert.ShowModal(Me.FindForm(), "INVALID DUE DATE", "Target due date cannot be set in the past.", AlertType.WarningAlert, actionText:="OK")
                dtpDueDate.Focus()
                Return
            End If

            ' 2. Double-Submit Protection Guard
            _isSubmitting = True
            btnCreateTask.Enabled = False
            btnCancel.Enabled = False
            lblClose.Enabled = False
            Me.Cursor = Cursors.WaitCursor

            Try
                ' Map Priority
                Dim selPriorityStr = cboPriority.SelectedItem.ToString()
                Dim priorityVal As TaskPriority = TaskPriority.Medium
                [Enum].TryParse(Of TaskPriority)(selPriorityStr, priorityVal)

                Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)

                ' Construct DTO conforming to existing ITaskManagementService.CreateAndAssignTaskAsync
                Dim taskDto As New TaskDto() With {
                    .Title = txtTaskTitle.Text.Trim(),
                    .Description = If(Not String.IsNullOrWhiteSpace(txtDescription.Text), txtDescription.Text.Trim(), txtTaskTitle.Text.Trim()),
                    .ClientId = clientItem.ClientId,
                    .ClientName = clientItem.ClientName,
                    .AssignedToUserId = staffItem.UserId,
                    .AssignedToName = staffItem.FullName,
                    .AssignedByUserId = currentUserId,
                    .Department = DepartmentType.IncomeTax,
                    .Priority = priorityVal,
                    .WorkflowState = TaskWorkflowState.Assigned,
                    .AssignmentDate = DateTime.UtcNow,
                    .TargetDueDate = dtpDueDate.Value.Date
                }

                ' 3. Call Authoritative Business Service Layer
                Dim newTaskId As Integer = Await _taskService.CreateAndAssignTaskAsync(taskDto)

                If newTaskId > 0 Then
                    _appLogger.LogInfo($"Successfully created Task ID {newTaskId} for Client '{clientItem.ClientName}'.", "CreateTaskPanel")
                    _isDirty = False

                    ' Raise Event with authoritative created TaskId
                    RaiseEvent TaskCreated(Me, newTaskId)
                Else
                    Throw New BusinessException("Database creation returned invalid Task ID.", "ERR_TASK_CREATE_FAILED")
                End If

            Catch ex As Exception
                ' On failure: keep panel open, keep user inputs intact!
                _appLogger.LogError($"Task creation failed: {ex.Message}")
                FrmInAppAlert.ShowModal(Me.FindForm(), "TASK CREATION FAILED", ex.Message, AlertType.ErrorAlert, actionText:="OK")
            Finally
                _isSubmitting = False
                btnCreateTask.Enabled = True
                btnCancel.Enabled = True
                lblClose.Enabled = True
                Me.Cursor = Cursors.Default
            End Try
        End Sub
    End Class
End Namespace
