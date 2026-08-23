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
Imports StaffAutomation.Core.Workflow
Imports StaffAutomation.WinForms.Forms.Common
Imports StaffAutomation.WinForms.Forms.Main

Namespace Forms.Tasks
    ''' <summary>
    ''' Dedicated Centered Modal Dialog Form for Editing Active Tasks.
    ''' Centered over parent form, displays read-only system metadata and editable business fields.
    ''' Integrates with authoritative ITaskManagementService and UpdateTaskDto.
    ''' </summary>
    Public Class FrmEditTask
        Inherits Form

        ' Services & Repositories
        Private ReadOnly _taskId As Integer
        Private ReadOnly _taskService As ITaskManagementService
        Private ReadOnly _clientService As IClientService
        Private ReadOnly _userRepo As DAL.Interfaces.IUserRepository
        Private ReadOnly _appLogger As IAppLogger

        ' Loaded Task Entity State
        Private _loadedTask As TaskDto = Nothing

        ' Output Result Property
        Public ReadOnly Property EditedTaskId As Integer
            Get
                Return _taskId
            End Get
        End Property

        ' State Management
        Private _isDirty As Boolean = False
        Private _isSubmitting As Boolean = False
        Private _isLoading As Boolean = False
        Private _isWorkflowExpanded As Boolean = False

        ' UI Components
        Private pnlHeader As Panel
        Private lblTitle As Label
        Private lblClose As Label

        Private pnlFormScroll As Panel

        ' Read-Only System Metadata Section
        Private lblMetaHeader As Label
        Private pnlMetaContainer As TableLayoutPanel
        Private lblValTaskId As Label
        Private lblValTaskCode As Label
        Private lblValStatus As Label
        Private lblValCreatedOn As Label
        Private lblValCreatedBy As Label

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

        ' Collapsible Workflow Preview Box
        Private pnlWorkflowPreviewHost As Panel
        Private pnlWorkflowPreviewHeader As Panel
        Private lblWorkflowHeader As Label
        Private lblWorkflowToggle As Label
        Private pnlWorkflowContent As Panel
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
        Private btnSaveTask As ModernButton

        ' Helper Item Classes
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

        Public Sub New(taskId As Integer, taskService As ITaskManagementService, clientService As IClientService, userRepo As DAL.Interfaces.IUserRepository, appLogger As IAppLogger)
            _taskId = taskId
            _taskService = taskService
            _clientService = clientService
            _userRepo = userRepo
            _appLogger = appLogger

            InitializeComponent()
        End Sub

        Protected Overrides Async Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)
            Await InitializeAndLoadDataAsync()
        End Sub

        Private Sub InitializeComponent()
            Me.SuspendLayout()

            ' Form Dimensions & Centered Modal Configuration
            Me.Size = New Size(680, 780)
            Me.MinimumSize = New Size(640, 720)
            Me.MaximumSize = New Size(800, 850)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.ShowInTaskbar = False
            Me.Text = "Edit Task"
            Me.BackColor = Color.White
            Me.Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular)

            ' =========================================================================
            ' 1. HEADER BAR
            ' =========================================================================
            pnlHeader = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 46,
                .BackColor = Color.FromArgb(15, 23, 42),
                .Padding = New Padding(16, 10, 16, 10)
            }
            lblTitle = New Label() With {
                .Text = "✏ EDIT TASK",
                .Font = New Font(ThemeConstants.FontNameDefault, 10.5!, FontStyle.Bold),
                .ForeColor = Color.White,
                .Location = New Point(16, 12),
                .AutoSize = True
            }
            lblClose = New Label() With {
                .Text = "✕",
                .Font = New Font(ThemeConstants.FontNameDefault, 11.0!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(148, 163, 184),
                .Location = New Point(pnlHeader.Width - 32, 10),
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
                .Height = 58,
                .BackColor = Color.FromArgb(248, 250, 252),
                .Padding = New Padding(16, 12, 16, 12)
            }
            AddHandler pnlFooter.Paint, Sub(s, e)
                                           Using p As New Pen(Color.FromArgb(226, 232, 240), 1)
                                               e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0)
                                           End Using
                                       End Sub

            btnCancel = New ModernButton() With {
                .Text = "Cancel",
                .Scheme = ModernButton.ButtonScheme.Secondary,
                .Size = New Size(100, 34),
                .Location = New Point(pnlFooter.Width - 260, 12),
                .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
            }
            AddHandler btnCancel.Click, AddressOf OnCancelClick

            btnSaveTask = New ModernButton() With {
                .Text = "Save Changes →",
                .Scheme = ModernButton.ButtonScheme.Custom,
                .BackColor = Color.FromArgb(16, 185, 129),
                .ForeColor = Color.White,
                .Size = New Size(140, 34),
                .Location = New Point(pnlFooter.Width - 150, 12),
                .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
            }
            AddHandler btnSaveTask.Click, AddressOf OnSaveTaskClick

            pnlFooter.Controls.Add(btnSaveTask)
            pnlFooter.Controls.Add(btnCancel)

            ' =========================================================================
            ' 3. SCROLLABLE FORM CONTENT
            ' =========================================================================
            pnlFormScroll = New Panel() With {
                .Dock = DockStyle.Fill,
                .AutoScroll = True,
                .BackColor = Color.White,
                .Padding = New Padding(20, 12, 24, 12)
            }

            Dim yPos As Integer = 8

            ' SECTION 0: IMMUTABLE SYSTEM METADATA (READ-ONLY)
            lblMetaHeader = CreateSectionHeader("IMMUTABLE SYSTEM METADATA", yPos)
            yPos += 24

            pnlMetaContainer = New TableLayoutPanel() With {
                .Location = New Point(20, yPos),
                .Size = New Size(615, 62),
                .ColumnCount = 6,
                .RowCount = 2,
                .BackColor = Color.FromArgb(241, 245, 249),
                .Padding = New Padding(6)
            }
            For colIdx As Integer = 0 To 5
                pnlMetaContainer.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 16.66!))
            Next

            AddMetaHeaderCell(pnlMetaContainer, 0, 0, "TASK ID")
            lblValTaskId = AddMetaValueCell(pnlMetaContainer, 0, 1, "—")

            AddMetaHeaderCell(pnlMetaContainer, 1, 0, "TASK CODE")
            lblValTaskCode = AddMetaValueCell(pnlMetaContainer, 1, 1, "—")

            AddMetaHeaderCell(pnlMetaContainer, 2, 0, "STATUS")
            lblValStatus = AddMetaValueCell(pnlMetaContainer, 2, 1, "—")

            AddMetaHeaderCell(pnlMetaContainer, 3, 0, "CREATED ON")
            lblValCreatedOn = AddMetaValueCell(pnlMetaContainer, 3, 1, "—")

            AddMetaHeaderCell(pnlMetaContainer, 4, 0, "CREATED BY")
            lblValCreatedBy = AddMetaValueCell(pnlMetaContainer, 4, 1, "—")

            pnlFormScroll.Controls.Add(pnlMetaContainer)
            yPos += 72

            ' SECTION A: CLIENT INFORMATION
            lblClientHeader = CreateSectionHeader("CLIENT INFORMATION", yPos)
            yPos += 24

            lblClientTag = CreateFieldLabel("Client Name *", yPos)
            yPos += 20
            cboClient = New ComboBox() With {
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular),
                .Location = New Point(20, yPos),
                .Size = New Size(615, 25)
            }
            AddHandler cboClient.SelectedIndexChanged, AddressOf OnClientSelectionChanged
            yPos += 34

            lblGstinRefTag = CreateFieldLabel("GSTIN / PAN Reference", yPos)
            yPos += 20
            txtGstinRef = New TextBox() With {
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular),
                .Location = New Point(20, yPos),
                .Size = New Size(615, 24),
                .ReadOnly = True,
                .BackColor = Color.FromArgb(248, 250, 252),
                .ForeColor = ThemeConstants.TextSecondary
            }
            yPos += 40

            ' SECTION B: TASK INFORMATION
            lblTaskInfoHeader = CreateSectionHeader("TASK INFORMATION", yPos)
            yPos += 24

            lblTaskTitleTag = CreateFieldLabel("Task Title *", yPos)
            yPos += 20
            txtTaskTitle = New TextBox() With {
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular),
                .Location = New Point(20, yPos),
                .Size = New Size(615, 24)
            }
            AddHandler txtTaskTitle.TextChanged, AddressOf OnFieldValueChanged
            yPos += 34

            lblTaskTypeTag = CreateFieldLabel("Task Type / Category *", yPos)
            yPos += 20
            cboTaskType = New ComboBox() With {
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular),
                .Location = New Point(20, yPos),
                .Size = New Size(615, 25)
            }
            cboTaskType.Items.AddRange(TaskWorkflowTemplateProvider.GetAllTaskTypes().Cast(Of Object)().ToArray())
            AddHandler cboTaskType.SelectedIndexChanged, AddressOf OnTaskTypeSelectionChanged
            yPos += 38

            ' COLLAPSIBLE WORKFLOW PREVIEW CONTAINER
            pnlWorkflowPreviewHost = New Panel() With {
                .Location = New Point(20, yPos),
                .Size = New Size(615, 34),
                .BackColor = Color.FromArgb(238, 242, 255),
                .Padding = New Padding(10)
            }
            AddHandler pnlWorkflowPreviewHost.Paint, Sub(s, e)
                                                         e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
                                                         Using p As New Pen(Color.FromArgb(199, 210, 254), 1)
                                                             e.Graphics.DrawRectangle(p, 0, 0, pnlWorkflowPreviewHost.Width - 1, pnlWorkflowPreviewHost.Height - 1)
                                                         End Using
                                                     End Sub

            pnlWorkflowPreviewHeader = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 24,
                .BackColor = Color.Transparent
            }
            lblWorkflowHeader = New Label() With {
                .Text = "📋 Workflow Details",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(49, 46, 129),
                .Location = New Point(4, 2),
                .AutoSize = True
            }
            lblWorkflowToggle = New Label() With {
                .Text = "[ View workflow steps ▼ ]",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.25!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(79, 70, 229),
                .Location = New Point(440, 2),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right,
                .AutoSize = True,
                .Cursor = Cursors.Hand
            }
            AddHandler lblWorkflowToggle.Click, AddressOf ToggleWorkflowPreview
            AddHandler lblWorkflowHeader.Click, AddressOf ToggleWorkflowPreview

            pnlWorkflowPreviewHeader.Controls.Add(lblWorkflowToggle)
            pnlWorkflowPreviewHeader.Controls.Add(lblWorkflowHeader)

            pnlWorkflowContent = New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = Color.Transparent,
                .Visible = False,
                .Padding = New Padding(6, 4, 6, 4)
            }
            lblWorkflowSteps = New Label() With {
                .Text = "",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.25!, FontStyle.Regular),
                .ForeColor = Color.FromArgb(55, 48, 163),
                .Dock = DockStyle.Fill
            }
            pnlWorkflowContent.Controls.Add(lblWorkflowSteps)

            pnlWorkflowPreviewHost.Controls.Add(pnlWorkflowContent)
            pnlWorkflowPreviewHost.Controls.Add(pnlWorkflowPreviewHeader)

            pnlFormScroll.Controls.Add(pnlWorkflowPreviewHost)
            yPos += 44

            ' SECTION C: TASK CONFIGURATION
            lblConfigHeader = CreateSectionHeader("TASK CONFIGURATION", yPos)
            yPos += 24

            ' Priority & Due Date (Side by Side)
            lblPriorityTag = CreateFieldLabel("Priority *", yPos)
            lblPriorityTag.Location = New Point(20, yPos)

            lblDueDateTag = CreateFieldLabel("Due Date *", yPos)
            lblDueDateTag.Location = New Point(330, yPos)
            yPos += 20

            cboPriority = New ComboBox() With {
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular),
                .Location = New Point(20, yPos),
                .Size = New Size(295, 25)
            }
            cboPriority.Items.AddRange(New Object() {"Medium", "High", "Urgent", "Low"})
            cboPriority.SelectedIndex = 0
            AddHandler cboPriority.SelectedIndexChanged, AddressOf OnFieldValueChanged

            dtpDueDate = New DateTimePicker() With {
                .Format = DateTimePickerFormat.Custom,
                .CustomFormat = "dd MMM yyyy",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular),
                .Location = New Point(330, yPos),
                .Size = New Size(305, 24)
            }
            AddHandler dtpDueDate.ValueChanged, AddressOf OnFieldValueChanged
            yPos += 34

            lblAssigneeTag = CreateFieldLabel("Assign To Staff *", yPos)
            yPos += 20
            cboAssignee = New ComboBox() With {
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular),
                .Location = New Point(20, yPos),
                .Size = New Size(615, 25)
            }
            AddHandler cboAssignee.SelectedIndexChanged, AddressOf OnFieldValueChanged
            yPos += 38

            ' SECTION D: DESCRIPTION
            lblDescriptionTag = CreateFieldLabel("Description / Special Instructions", yPos)
            yPos += 20
            txtDescription = New TextBox() With {
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular),
                .Location = New Point(20, yPos),
                .Size = New Size(615, 65),
                .Multiline = True,
                .ScrollBars = ScrollBars.Vertical
            }
            AddHandler txtDescription.TextChanged, AddressOf OnFieldValueChanged
            yPos += 75

            pnlFormScroll.Controls.Add(lblDescriptionTag)
            pnlFormScroll.Controls.Add(txtDescription)
            pnlFormScroll.Controls.Add(lblAssigneeTag)
            pnlFormScroll.Controls.Add(cboAssignee)
            pnlFormScroll.Controls.Add(dtpDueDate)
            pnlFormScroll.Controls.Add(lblDueDateTag)
            pnlFormScroll.Controls.Add(cboPriority)
            pnlFormScroll.Controls.Add(lblPriorityTag)
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
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.PrimaryAccent,
                .Location = New Point(20, topPos),
                .AutoSize = True
            }
            pnlFormScroll.Controls.Add(lbl)
            Return lbl
        End Function

        Private Function CreateFieldLabel(text As String, topPos As Integer) As Label
            Dim lbl As New Label() With {
                .Text = text,
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(20, topPos),
                .AutoSize = True
            }
            pnlFormScroll.Controls.Add(lbl)
            Return lbl
        End Function

        Private Sub AddMetaHeaderCell(tbl As TableLayoutPanel, col As Integer, row As Integer, text As String)
            Dim lbl As New Label() With {
                .Text = text,
                .Font = New Font(ThemeConstants.FontNameDefault, 7.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextSecondary,
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleCenter
            }
            tbl.Controls.Add(lbl, col, row)
        End Sub

        Private Function AddMetaValueCell(tbl As TableLayoutPanel, col As Integer, row As Integer, text As String) As Label
            Dim lbl As New Label() With {
                .Text = text,
                .Font = New Font(ThemeConstants.FontNameDefault, 8.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleCenter
            }
            tbl.Controls.Add(lbl, col, row)
            Return lbl
        End Function

        ''' <summary>
        ''' Ground-Truth Data Loading: Re-queries task from database and populates controls.
        ''' </summary>
        Private Async Function InitializeAndLoadDataAsync() As System.Threading.Tasks.Task
            _isLoading = True
            Try
                ' 1. Re-query Ground-Truth Task Record from Database
                _loadedTask = Await _taskService.GetTaskByIdAsync(_taskId)
                If _loadedTask Is Nothing Then
                    FrmInAppAlert.ShowModal(Me, "TASK NOT FOUND", $"Task ID {_taskId} could not be loaded from database.", AlertType.ErrorAlert, actionText:="OK")
                    Me.DialogResult = DialogResult.Cancel
                    Me.Close()
                    Return
                End If

                ' 2. Verify Task State Permissibility
                If Not _taskService.CanEditTask(_loadedTask.WorkflowState) Then
                    FrmInAppAlert.ShowModal(Me, "EDIT FORBIDDEN", $"Task '{_loadedTask.Title}' ({_loadedTask.TaskCode}) is in '{_loadedTask.WorkflowState}' state and cannot be edited.", AlertType.WarningAlert, actionText:="OK")
                    Me.DialogResult = DialogResult.Cancel
                    Me.Close()
                    Return
                End If

                ' 3. Populate Client Dropdown
                cboClient.Items.Clear()
                cboClient.Items.Add("-- Select Client --")
                If _clientService IsNot Nothing Then
                    Dim clients = Await _clientService.GetAllClientsAsync(includeDeleted:=False)
                    If clients IsNot Nothing Then
                        For Each c As ClientDto In clients
                            If c.IsActive OrElse c.ClientId = _loadedTask.ClientId Then
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

                ' Select Client
                For i As Integer = 1 To cboClient.Items.Count - 1
                    Dim cItem = TryCast(cboClient.Items(i), ClientComboItem)
                    If cItem IsNot Nothing AndAlso cItem.ClientId = _loadedTask.ClientId Then
                        cboClient.SelectedIndex = i
                        Exit For
                    End If
                Next
                If cboClient.SelectedIndex < 0 AndAlso cboClient.Items.Count > 0 Then cboClient.SelectedIndex = 0

                ' 4. Populate Staff Dropdown
                cboAssignee.Items.Clear()
                cboAssignee.Items.Add("-- Select Assignee --")
                If _userRepo IsNot Nothing Then
                    Dim users = Await _userRepo.GetAllAsync()
                    If users IsNot Nothing Then
                        For Each u As UserEntity In users
                            If u.IsActive OrElse u.UserId = _loadedTask.AssignedToUserId Then
                                cboAssignee.Items.Add(New UserComboItem With {
                                    .UserId = u.UserId,
                                    .FullName = u.FullName
                                })
                            End If
                        Next
                    End If
                End If

                ' Select Assignee
                For i As Integer = 1 To cboAssignee.Items.Count - 1
                    Dim uItem = TryCast(cboAssignee.Items(i), UserComboItem)
                    If uItem IsNot Nothing AndAlso uItem.UserId = _loadedTask.AssignedToUserId Then
                        cboAssignee.SelectedIndex = i
                        Exit For
                    End If
                Next
                If cboAssignee.SelectedIndex < 0 AndAlso cboAssignee.Items.Count > 0 Then cboAssignee.SelectedIndex = 0

                ' 5. Fill Read-Only System Metadata
                lblValTaskId.Text = _loadedTask.TaskId.ToString()
                lblValTaskCode.Text = _loadedTask.TaskCode
                lblValStatus.Text = _loadedTask.WorkflowState.ToString()
                lblValCreatedOn.Text = _loadedTask.AssignmentDate.ToString("dd MMM yyyy")
                lblValCreatedBy.Text = If(Not String.IsNullOrEmpty(_loadedTask.AssignedByName), _loadedTask.AssignedByName, "System")

                ' 6. Fill Editable Fields
                txtTaskTitle.Text = _loadedTask.Title
                txtDescription.Text = _loadedTask.Description

                If _loadedTask.TargetDueDate <> DateTime.MinValue Then
                    dtpDueDate.Value = _loadedTask.TargetDueDate
                Else
                    dtpDueDate.Value = DateTime.Today
                End If

                ' Select Priority
                SelectPriorityInCombo(_loadedTask.Priority)

                ' Select Task Type
                SelectTaskTypeInCombo(_loadedTask.TaskType)

                _isDirty = False
            Catch ex As Exception
                _appLogger.LogError($"Failed to load FrmEditTask data for Task ID {_taskId}: {ex.Message}")
                FrmInAppAlert.ShowModal(Me, "ERROR", "Failed to load task data: " & ex.Message, AlertType.ErrorAlert, actionText:="OK")
            Finally
                _isLoading = False
            End Try
        End Function

        Private Sub SelectTaskTypeInCombo(taskTypeStr As String)
            If String.IsNullOrWhiteSpace(taskTypeStr) Then
                If cboTaskType.Items.Count > 0 Then cboTaskType.SelectedIndex = 0
                Return
            End If

            For i As Integer = 0 To cboTaskType.Items.Count - 1
                If String.Equals(cboTaskType.Items(i).ToString(), taskTypeStr.Trim(), StringComparison.OrdinalIgnoreCase) Then
                    cboTaskType.SelectedIndex = i
                    Return
                End If
            Next

            If cboTaskType.Items.Count > 0 Then cboTaskType.SelectedIndex = 0
        End Sub

        Private Sub SelectPriorityInCombo(priority As TaskPriority)
            Dim targetText = priority.ToString()
            For i As Integer = 0 To cboPriority.Items.Count - 1
                If String.Equals(cboPriority.Items(i).ToString(), targetText, StringComparison.OrdinalIgnoreCase) Then
                    cboPriority.SelectedIndex = i
                    Exit For
                End If
            Next
        End Sub

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

            If cboTaskType.SelectedIndex < 0 Then Return

            Dim selectedType = cboTaskType.SelectedItem.ToString()
            Dim tmpl = TaskWorkflowTemplateProvider.GetTemplate(selectedType)

            ' Update Workflow Preview Box
            lblWorkflowHeader.Text = $"📋 Workflow: {tmpl.TaskType} ({tmpl.StandardSteps.Count} standard steps attached)"
            lblWorkflowSteps.Text = String.Join(Environment.NewLine, tmpl.StandardSteps.Select(Function(s, idx) $"{idx + 1}. {s}"))
        End Sub

        Private Sub OnFieldValueChanged(sender As Object, e As EventArgs)
            If _isLoading Then Return
            MarkDirty()
        End Sub

        Private Sub MarkDirty()
            _isDirty = True
        End Sub

        Private Sub ToggleWorkflowPreview(sender As Object, e As EventArgs)
            _isWorkflowExpanded = Not _isWorkflowExpanded
            pnlWorkflowContent.Visible = _isWorkflowExpanded
            lblWorkflowToggle.Text = If(_isWorkflowExpanded, "[ Hide workflow steps ▲ ]", "[ View workflow steps ▼ ]")
            pnlWorkflowPreviewHost.Height = If(_isWorkflowExpanded, 110, 34)
        End Sub

        Private Sub OnCancelClick(sender As Object, e As EventArgs)
            If _isSubmitting Then Return

            If _isDirty Then
                Dim dlgRes = FrmInAppAlert.ShowModal(Me, "DISCARD UNSAVED CHANGES?", "You have unsaved task edits. Are you sure you want to cancel?", AlertType.WarningAlert, actionText:="DISCARD CHANGES", showCancel:=True, cancelText:="KEEP EDITING")
                If dlgRes <> DialogResult.OK Then Return
            End If

            _isDirty = False
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
        End Sub

        Private Async Sub OnSaveTaskClick(sender As Object, e As EventArgs)
            If _isSubmitting Then Return

            ' 1. Field-Level Validations
            Dim clientItem = TryCast(cboClient.SelectedItem, ClientComboItem)
            If clientItem Is Nothing OrElse clientItem.ClientId <= 0 Then
                FrmInAppAlert.ShowModal(Me, "CLIENT REQUIRED", "Please select a client.", AlertType.WarningAlert, actionText:="OK")
                cboClient.Focus()
                Return
            End If

            If String.IsNullOrWhiteSpace(txtTaskTitle.Text) Then
                FrmInAppAlert.ShowModal(Me, "TASK TITLE REQUIRED", "Task title is required.", AlertType.WarningAlert, actionText:="OK")
                txtTaskTitle.Focus()
                Return
            End If

            If cboTaskType.SelectedIndex < 0 Then
                FrmInAppAlert.ShowModal(Me, "TASK TYPE REQUIRED", "Please select a task type.", AlertType.WarningAlert, actionText:="OK")
                cboTaskType.Focus()
                Return
            End If

            Dim staffItem = TryCast(cboAssignee.SelectedItem, UserComboItem)
            If staffItem Is Nothing OrElse staffItem.UserId <= 0 Then
                FrmInAppAlert.ShowModal(Me, "STAFF ASSIGNEE REQUIRED", "Please assign the task to a staff member.", AlertType.WarningAlert, actionText:="OK")
                cboAssignee.Focus()
                Return
            End If

            ' 2. Double-Submit Protection Guard
            _isSubmitting = True
            btnSaveTask.Enabled = False
            btnCancel.Enabled = False
            lblClose.Enabled = False
            Me.Cursor = Cursors.WaitCursor

            Try
                Dim selectedType = cboTaskType.SelectedItem.ToString()

                Dim selPriorityStr = cboPriority.SelectedItem.ToString()
                Dim priorityVal As TaskPriority = TaskPriority.Medium
                [Enum].TryParse(Of TaskPriority)(selPriorityStr, priorityVal)

                ' Construct UpdateTaskDto (Targeted edit DTO)
                Dim updateDto As New UpdateTaskDto() With {
                    .TaskId = _taskId,
                    .Title = txtTaskTitle.Text.Trim(),
                    .Description = If(Not String.IsNullOrWhiteSpace(txtDescription.Text), txtDescription.Text.Trim(), txtTaskTitle.Text.Trim()),
                    .TaskType = selectedType,
                    .ClientId = clientItem.ClientId,
                    .AssignedToUserId = staffItem.UserId,
                    .Priority = priorityVal,
                    .TargetDueDate = dtpDueDate.Value.Date,
                    .OriginalModifiedOn = If(_loadedTask IsNot Nothing, _loadedTask.ModifiedOn, CType(Nothing, Nullable(Of DateTime)))
                }

                ' 3. Call Authoritative Service Layer
                Dim success As Boolean = Await _taskService.UpdateTaskAsync(updateDto)

                If success Then
                    _appLogger.LogInfo($"Successfully updated Task ID {_taskId}.", "FrmEditTask")
                    _isDirty = False
                    Me.DialogResult = DialogResult.OK
                    Me.Close()
                Else
                    Throw New BusinessException("Database update returned false.", "ERR_TASK_UPDATE_FAILED")
                End If

            Catch ex As ConcurrencyException
                FrmInAppAlert.ShowModal(Me, "CONCURRENCY CONFLICT", ex.Message, AlertType.WarningAlert, actionText:="REFRESH & REVIEW")
            Catch ex As BusinessException
                ' On business rule violation or ground-truth state rejection: keep form open and preserve user input
                Dim alertTitle = If(ex.RuleCode = "ERR_CATEGORY_CHANGE_FORBIDDEN", "CATEGORY CHANGE RESTRICTED", "BUSINESS RULE REJECTION")
                FrmInAppAlert.ShowModal(Me, alertTitle, ex.Message, AlertType.WarningAlert, actionText:="OK")
            Catch ex As Exception
                ' On system exception: keep form open and preserve user input
                FrmInAppAlert.ShowModal(Me, "SAVE FAILED", "An error occurred while saving task changes: " & ex.Message, AlertType.ErrorAlert, actionText:="OK")
            Finally
                _isSubmitting = False
                btnSaveTask.Enabled = True
                btnCancel.Enabled = True
                lblClose.Enabled = True
                Me.Cursor = Cursors.Default
            End Try
        End Sub
    End Class
End Namespace
