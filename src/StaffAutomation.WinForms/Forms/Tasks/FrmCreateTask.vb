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
    ''' Dedicated Centered Modal Dialog Form for Task Creation.
    ''' Centered over FrmMainShell, prevents interaction with the work queue while open.
    ''' Integrates with authoritative ITaskManagementService and TaskWorkflowTemplateProvider.
    ''' </summary>
    Public Class FrmCreateTask
        Inherits Form

        ' Services
        Private ReadOnly _taskService As ITaskManagementService
        Private ReadOnly _clientService As IClientService
        Private ReadOnly _userRepo As DAL.Interfaces.IUserRepository
        Private ReadOnly _appLogger As IAppLogger

        ' Output Result Property
        Public ReadOnly Property CreatedTaskId As Integer

        ' State Management
        Private _isDirty As Boolean = False
        Private _isSubmitting As Boolean = False
        Private _isLoading As Boolean = False
        Private _isWorkflowExpanded As Boolean = False

        ' Smart Default Trackers (Preserve manual edits)
        Private _isTitleManuallyEdited As Boolean = False
        Private _isPriorityManuallyOverridden As Boolean = False
        Private _isDescriptionManuallyEdited As Boolean = False
        Private _isUpdatingDefaults As Boolean = False

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
        Private btnCreateTask As ModernButton

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

        Public Sub New(taskService As ITaskManagementService, clientService As IClientService, userRepo As DAL.Interfaces.IUserRepository, appLogger As IAppLogger)
            If clientService Is Nothing Then Throw New ArgumentNullException(NameOf(clientService), "IClientService dependency cannot be Nothing in FrmCreateTask.")
            If taskService Is Nothing Then Throw New ArgumentNullException(NameOf(taskService), "ITaskManagementService dependency cannot be Nothing in FrmCreateTask.")

            _taskService = taskService
            _clientService = clientService
            _userRepo = userRepo
            _appLogger = appLogger

            InitializeComponent()
        End Sub

        Protected Overrides Async Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)
            Await InitializeDataAsync()
        End Sub

        Private Sub InitializeComponent()
            Me.SuspendLayout()

            ' Form Dimensions & Centered Modal Configuration
            Me.Size = New Size(680, 700)
            Me.MinimumSize = New Size(620, 480)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.ShowInTaskbar = False
            Me.Text = "Create New Task"
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
                .Text = "+ CREATE NEW TASK",
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

            btnCreateTask = New ModernButton() With {
                .Text = "Create Task →",
                .Scheme = ModernButton.ButtonScheme.Custom,
                .BackColor = Color.FromArgb(16, 185, 129),
                .ForeColor = Color.White,
                .Size = New Size(140, 34),
                .Location = New Point(pnlFooter.Width - 150, 12),
                .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
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
                .Padding = New Padding(20, 12, 24, 12)
            }

            Dim yPos As Integer = 8

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
            AddHandler txtTaskTitle.TextChanged, AddressOf OnTaskTitleTextChanged
            yPos += 34

            lblTaskTypeTag = CreateFieldLabel("Task Type / Category *", yPos)
            yPos += 20
            cboTaskType = New ComboBox() With {
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular),
                .Location = New Point(20, yPos),
                .Size = New Size(615, 25)
            }
            ' Populate task types from authoritative provider
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
                .Text = "📋 Workflow: GSTR-3B Filing (4 standard steps attached)",
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
            cboPriority.SelectedIndex = 0 ' Default Medium
            AddHandler cboPriority.SelectionChangeCommitted, AddressOf OnPrioritySelectionCommitted
            AddHandler cboPriority.SelectedIndexChanged, AddressOf OnPrioritySelectedIndexChanged

            dtpDueDate = New DateTimePicker() With {
                .Format = DateTimePickerFormat.Custom,
                .CustomFormat = "dd MMM yyyy",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular),
                .Location = New Point(330, yPos),
                .Size = New Size(305, 24),
                .Value = DateTime.Today.AddDays(3) ' Deterministic Default: Today + 3 Days
            }
            AddHandler dtpDueDate.ValueChanged, AddressOf OnDueDateValueChanged
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
            AddHandler txtDescription.TextChanged, AddressOf OnDescriptionTextChanged
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

            Me.Controls.Add(pnlFormScroll)
            Me.Controls.Add(pnlFooter)
            Me.Controls.Add(pnlHeader)

            ' Final Container Assembly
            For Each ctrl In New Control() {cboClient, txtTaskTitle, cboTaskType, cboPriority, cboAssignee, txtDescription}
                ThemeConstants.ApplyStandardInputStyle(ctrl)
            Next

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

        ''' <summary>
        ''' Asynchronously populates Clients and Staff dropdowns from authoritative service layer.
        ''' Resets form fields and dirty flags.
        ''' </summary>
        Private _isInitialized As Boolean = False

        ''' <summary>
        ''' Asynchronously populates Clients and Staff dropdowns from authoritative service layer.
        ''' Resets form fields and dirty flags.
        ''' </summary>
        Public Async Function InitializeDataAsync() As System.Threading.Tasks.Task
            If _isLoading OrElse _isInitialized Then Return
            _isLoading = True
            _appLogger.LogInfo("[CreateTaskClientLoad] Starting client load", "FrmCreateTask")

            Try
                ' 1. Load Active Clients (Database Fetch Phase)
                cboClient.Items.Clear()
                cboClient.Items.Add("-- Select Client --")

                Dim rawClients As List(Of ClientDto) = Nothing
                If _clientService IsNot Nothing Then
                    Try
                        _appLogger.LogInfo("[ClientDropdown] DB fetch started", "FrmCreateTask")
                        rawClients = Await _clientService.GetAllClientsAsync(includeDeleted:=False)
                        Dim sqlCount As Integer = If(rawClients IsNot Nothing, rawClients.Count, 0)
                        _appLogger.LogInfo($"[ClientDropdown] Form received: {sqlCount} clients", "FrmCreateTask")
                    Catch exDb As Exception
                        _appLogger.LogError($"[CreateTaskClientLoad:DbError] Database query failed: {exDb.Message}", "FrmCreateTask", exDb)
                        FrmInAppAlert.ShowModal(Me, "CLIENT FETCH ERROR", "Unable to load client list from database. " & exDb.Message, AlertType.ErrorAlert, actionText:="OK")
                    End Try
                End If

                ' 2. UI Mapping & Dropdown Binding Phase
                Try
                    Dim mappedItems As New List(Of ClientComboItem)()
                    If rawClients IsNot Nothing Then
                        For Each c As ClientDto In rawClients
                            If Not c.IsDeleted Then
                                Dim gstinRef As String = If(Not String.IsNullOrEmpty(c.Gstin), c.Gstin, If(Not String.IsNullOrEmpty(c.PanNumber), c.PanNumber, c.ClientCode))
                                mappedItems.Add(New ClientComboItem With {
                                    .ClientId = c.ClientId,
                                    .ClientName = c.ClientName,
                                    .GstinOrPan = gstinRef
                                })
                            End If
                        Next
                    End If

                    If mappedItems.Count > 0 Then
                        For Each item In mappedItems
                            cboClient.Items.Add(item)
                        Next
                    End If

                    _appLogger.LogInfo($"[ClientDropdown] ComboBox bound: {cboClient.Items.Count} items (1 header + {mappedItems.Count} clients)", "FrmCreateTask")
                    cboClient.SelectedIndex = 0
                Catch exUi As Exception
                    _appLogger.LogError($"[CreateTaskClientLoad:UIError] UI dropdown binding failed: {exUi.Message}", "FrmCreateTask", exUi)
                End Try

                ' 3. Load Active Staff Members
                cboAssignee.Items.Clear()
                cboAssignee.Items.Add("-- Select Assignee --")
                If _userRepo IsNot Nothing Then
                    Dim users = Await _userRepo.GetAllAsync()
                    If users IsNot Nothing Then
                        For Each u As UserEntity In users
                            If Not u.IsDeleted Then
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

                ' 4. Reset Fields and Smart Trackers
                _isTitleManuallyEdited = False
                _isPriorityManuallyOverridden = False
                _isDescriptionManuallyEdited = False

                txtGstinRef.Text = String.Empty
                txtTaskTitle.Text = String.Empty
                txtDescription.Text = String.Empty
                If cboTaskType.Items.Count > 0 Then cboTaskType.SelectedIndex = 0

                If cboTaskType.SelectedIndex >= 0 Then
                    OnTaskTypeSelectionChanged(cboTaskType, EventArgs.Empty)
                End If

                _isTitleManuallyEdited = False
                _isPriorityManuallyOverridden = False
                _isDescriptionManuallyEdited = False
                _isDirty = False
                _isInitialized = True
            Catch ex As Exception
                _appLogger.LogError($"Failed to initialize FrmCreateTask dropdowns: {ex.Message}", "FrmCreateTask", ex)
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

        ''' <summary>
        ''' Task Type Selection Handler: Updates workflow preview, sets default due date,
        ''' auto-generates task title (if title not custom), auto-calculates priority (if priority not custom),
        ''' and prefills default instructions (if description not custom).
        ''' </summary>
        Private Sub OnTaskTypeSelectionChanged(sender As Object, e As EventArgs)
            If _isLoading Then Return
            MarkDirty()

            If cboTaskType.SelectedIndex < 0 Then Return

            Dim selectedType = cboTaskType.SelectedItem.ToString()
            Dim tmpl = TaskWorkflowTemplateProvider.GetTemplate(selectedType)

            _isUpdatingDefaults = True
            Try
                ' 1. Update Workflow Preview Box
                lblWorkflowHeader.Text = $"📋 Workflow: {tmpl.TaskType} ({tmpl.StandardSteps.Count} standard steps attached)"
                lblWorkflowSteps.Text = String.Join(Environment.NewLine, tmpl.StandardSteps.Select(Function(s, idx) $"{idx + 1}. {s}"))

                ' 2. Set default due date offset for selected task type
                dtpDueDate.Value = DateTime.Today.AddDays(tmpl.DefaultDueDateDays)

                ' 3. Auto-generate Task Title if user has not manually edited title
                If Not _isTitleManuallyEdited Then
                    txtTaskTitle.Text = TaskWorkflowTemplateProvider.GenerateDefaultTitle(selectedType, dtpDueDate.Value)
                End If

                ' 4. Auto-calculate Priority from Due Date if user has not manually overridden priority
                If Not _isPriorityManuallyOverridden Then
                    Dim suggestedPriority = TaskWorkflowTemplateProvider.CalculatePriorityFromDueDate(dtpDueDate.Value)
                    SelectPriorityInCombo(suggestedPriority)
                End If

                ' 5. Prefill default instructions if description has not been manually edited by user
                If Not _isDescriptionManuallyEdited Then
                    txtDescription.Text = $"Standard statutory execution for {selectedType}. Please complete all steps and verify with senior partner."
                End If
            Finally
                _isUpdatingDefaults = False
            End Try
        End Sub

        ''' <summary>
        ''' Due Date Changed Handler: Updates title period (if title not custom) and auto-calculates suggested priority (if priority not custom).
        ''' </summary>
        Private Sub OnDueDateValueChanged(sender As Object, e As EventArgs)
            If _isLoading Then Return
            MarkDirty()

            If _isUpdatingDefaults Then Return

            _isUpdatingDefaults = True
            Try
                Dim selectedType = If(cboTaskType.SelectedIndex >= 0, cboTaskType.SelectedItem.ToString(), "Task")

                ' 1. Auto-generate Task Title period if title is not manually edited by user
                If Not _isTitleManuallyEdited Then
                    txtTaskTitle.Text = TaskWorkflowTemplateProvider.GenerateDefaultTitle(selectedType, dtpDueDate.Value)
                End If

                ' 2. Auto-calculate Priority from Due Date if priority is not manually overridden by user
                If Not _isPriorityManuallyOverridden Then
                    Dim suggestedPriority = TaskWorkflowTemplateProvider.CalculatePriorityFromDueDate(dtpDueDate.Value)
                    SelectPriorityInCombo(suggestedPriority)
                End If
            Finally
                _isUpdatingDefaults = False
            End Try
        End Sub

        Private Sub OnTaskTitleTextChanged(sender As Object, e As EventArgs)
            If _isLoading OrElse _isUpdatingDefaults Then Return
            MarkDirty()

            ' Intentional Clearing Behavior:
            ' If the user clears the title box completely, reset the manual edit flag
            ' so subsequent Task Type or Due Date adjustments can auto-fill the default title again.
            If String.IsNullOrWhiteSpace(txtTaskTitle.Text) Then
                _isTitleManuallyEdited = False
            ElseIf txtTaskTitle.ContainsFocus OrElse txtTaskTitle.Focused Then
                _isTitleManuallyEdited = True
            End If
        End Sub

        Private Sub OnPrioritySelectionCommitted(sender As Object, e As EventArgs)
            If _isLoading OrElse _isUpdatingDefaults Then Return
            MarkDirty()
            _isPriorityManuallyOverridden = True
        End Sub

        Private Sub OnPrioritySelectedIndexChanged(sender As Object, e As EventArgs)
            If _isLoading OrElse _isUpdatingDefaults Then Return
            MarkDirty()

            ' Only set manual override flag if change was triggered by direct user focus/interaction
            If cboPriority.ContainsFocus OrElse cboPriority.Focused Then
                _isPriorityManuallyOverridden = True
            End If
        End Sub

        Private Sub OnDescriptionTextChanged(sender As Object, e As EventArgs)
            If _isLoading OrElse _isUpdatingDefaults Then Return
            MarkDirty()

            ' Intentional Clearing Behavior:
            ' If the user clears the description box completely, reset the manual edit flag
            ' so subsequent Task Type adjustments can prefill the default instruction template again.
            If String.IsNullOrWhiteSpace(txtDescription.Text) Then
                _isDescriptionManuallyEdited = False
            ElseIf txtDescription.ContainsFocus OrElse txtDescription.Focused Then
                _isDescriptionManuallyEdited = True
            End If
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

        Private Sub ToggleWorkflowPreview(sender As Object, e As EventArgs)
            _isWorkflowExpanded = Not _isWorkflowExpanded
            pnlWorkflowContent.Visible = _isWorkflowExpanded
            lblWorkflowToggle.Text = If(_isWorkflowExpanded, "[ Hide workflow steps ▲ ]", "[ View workflow steps ▼ ]")
            pnlWorkflowPreviewHost.Height = If(_isWorkflowExpanded, 110, 34)
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
                Dim dlgRes = FrmInAppAlert.ShowModal(Me, "DISCARD UNSAVED CHANGES?", "You have unsaved task creation changes. Are you sure you want to cancel?", AlertType.WarningAlert, actionText:="DISCARD CHANGES", showCancel:=True, cancelText:="KEEP EDITING")
                If dlgRes <> DialogResult.OK Then Return
            End If

            _isDirty = False
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
        End Sub

        Private Async Sub OnCreateTaskClick(sender As Object, e As EventArgs)
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

            If dtpDueDate.Value.Date < DateTime.Today Then
                FrmInAppAlert.ShowModal(Me, "INVALID DUE DATE", "Due date is required and cannot be set in the past.", AlertType.WarningAlert, actionText:="OK")
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
                ' Resolve Template & Department
                Dim selectedType = cboTaskType.SelectedItem.ToString()
                Dim tmpl = TaskWorkflowTemplateProvider.GetTemplate(selectedType)

                ' Map Priority
                Dim selPriorityStr = cboPriority.SelectedItem.ToString()
                Dim priorityVal As TaskPriority = TaskPriority.Medium
                [Enum].TryParse(Of TaskPriority)(selPriorityStr, priorityVal)

                Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)

                ' Construct DTO conforming to existing ITaskManagementService.CreateAndAssignTaskAsync
                ' Initial state is explicitly Assigned because Assignee is mandatory
                Dim taskDto As New TaskDto() With {
                    .Title = txtTaskTitle.Text.Trim(),
                    .Description = If(Not String.IsNullOrWhiteSpace(txtDescription.Text), txtDescription.Text.Trim(), txtTaskTitle.Text.Trim()),
                    .TaskType = selectedType,
                    .ClientId = clientItem.ClientId,
                    .ClientName = clientItem.ClientName,
                    .AssignedToUserId = staffItem.UserId,
                    .AssignedToName = staffItem.FullName,
                    .AssignedByUserId = currentUserId,
                    .Department = tmpl.Department,
                    .Priority = priorityVal,
                    .WorkflowState = TaskWorkflowState.Assigned,
                    .AssignmentDate = DateTime.UtcNow,
                    .TargetDueDate = dtpDueDate.Value.Date
                }

                ' 3. Call Authoritative Business Service Layer
                Dim newTaskId As Integer = Await _taskService.CreateAndAssignTaskAsync(taskDto)

                If newTaskId > 0 Then
                    _appLogger.LogInfo($"Successfully created Task ID {newTaskId} for Client '{clientItem.ClientName}'.", "FrmCreateTask")
                    Forms.Common.DataStateTracker.MarkTasksChanged()
                    _createdTaskId = newTaskId
                    _isDirty = False

                    Me.DialogResult = DialogResult.OK
                    Me.Close()
                Else
                    Throw New BusinessException("Database task creation returned invalid Task ID.", "ERR_TASK_CREATE_FAILED")
                End If

            Catch ex As Exception
                ' On failure: DO NOT close modal! Keep user inputs intact and display actual error dialog
                _appLogger.LogError($"Task creation failed: {ex.Message}")
                FrmInAppAlert.ShowModal(Me, "TASK CREATION FAILED", ex.Message, AlertType.ErrorAlert, actionText:="OK")
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
