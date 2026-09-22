Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Linq
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports StaffAutomation.BLL.Interfaces
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
Imports StaffAutomation.DAL.Configuration
Imports StaffAutomation.DAL.Core
Imports StaffAutomation.DAL.Repositories
Imports StaffAutomation.Core.Workflow
Imports StaffAutomation.WinForms.Forms.Main

Namespace Forms.Tasks
    ''' <summary>
    ''' Professional Operational Work Queue &amp; Task Management UserControl.
    ''' Implements 64% Queue / 36% Sticky Task Details two-column layout, urgency sectioning (Overdue, Due Today, Upcoming),
    ''' dynamic multi-criteria filtering, data-backed next action workflow checklists, and guarded statutory completion logic.
    ''' </summary>
    Public Class DailyTasksControl
        Inherits UserControl
        Implements IPreloadableScreen

        ' Services & Repositories
        Private ReadOnly _taskService As ITaskManagementService
        Private ReadOnly _clientService As IClientService
        Private ReadOnly _userRepo As DAL.Interfaces.IUserRepository
        Private ReadOnly _activityRepo As DAL.Interfaces.ITaskActivityRepository
        Private ReadOnly _discussionService As IDiscussionService
        Private ReadOnly _attendanceService As IAttendanceService
        Private _activeNavigationToken As Long = 0
        Private ReadOnly _appLogger As IAppLogger
        Private ReadOnly _auditLogger As AuditLogger
        Private ReadOnly _taskCategoryService As ITaskCategoryService
        Private ReadOnly _mainShellHost As Forms.Main.FrmMainShell

        ' In-Memory Data State
        Private _allTasksList As New List(Of TaskDto)()
        Private _filteredTasksList As New List(Of TaskDto)()
        Private _selectedTask As TaskDto = Nothing
        Private _activeTab As String = "NeedsAttention" ' NeedsAttention, MyTasks, AllActive, Completed
        Private _activePresetFilter As String = ""
        Private _isUpcomingExpanded As Boolean = False
        Private _isOperationRunning As Boolean = False

        ' Initialization & Clearing Lifecycle Guards, Debouncing & Version Sequencing
        Private _isInitializingFilters As Boolean = True
        Private _isClearingFilters As Boolean = False
        Private _filterVersionSequence As Long = 0
        Private ReadOnly _searchDebounceTimer As New System.Windows.Forms.Timer() With {.Interval = 400}

        ' Controlled DB Exception Error State & Load Lock
        Private _isDataLoading As Boolean = False
        Private _hasDataLoadError As Boolean = False
        Private _dataLoadErrorRefId As String = String.Empty

        ' Request ID Sequence Tracking & Cache Freshness Policy
        Private _dbFetchRequestCounter As Long = 0
        Private _localTasksDataVersion As Long = 0

        ''' <summary>
        ''' Strongly-typed filter option wrapper for ComboBox items.
        ''' Eliminates error-prone string parsing and preserves type safety.
        ''' </summary>
        Public Class FilterOption(Of T)
            Public Property DisplayText As String = String.Empty
            Public Property Value As T

            Public Sub New(display As String, val As T)
                Me.DisplayText = display
                Me.Value = val
            End Sub

            Public Overrides Function ToString() As String
                Return DisplayText
            End Function
        End Class

        ''' <summary>
        ''' Explicit Status Filter Kinds mapping UI options to domain state predicates.
        ''' </summary>
        Public Enum StatusFilterKind
            AllStatus = 0
            Pending = 1          ' Explicitly maps to NewTask OR Assigned
            InProgress = 2       ' Maps to InProgress
            WaitingForClient = 3 ' Maps to WaitingForClient
            WaitingForDocuments = 4 ' Maps to WaitingForDocuments
            UnderReview = 5      ' Maps to UnderReview
            OnHold = 6           ' Maps to OnHold
            Completed = 7        ' Maps to Completed
        End Enum

        ' Task Checklist Persistence State Cache (TaskId -> List of (StepText, IsCompleted))
        Private ReadOnly _taskChecklistCache As New Dictionary(Of Integer, List(Of ChecklistStepItem))()

        Public Class ChecklistStepItem
            Public Property StepText As String = String.Empty
            Public Property IsCompleted As Boolean = False
            Public Property IsMandatory As Boolean = True
        End Class

        ' Top Page Header & Tabs
        Private pnlHeader As Panel
        Private lblTitle As Label
        Private lblSubtitle As Label
        Private btnCreateTask As Forms.Common.ModernButton

        Private pnlTabsContainer As Panel
        Private btnTabNeedsAttention As Button
        Private btnTabMyTasks As Button
        Private btnTabAllActive As Button
        Private btnTabCompleted As Button
        Private btnTabDeletedTasks As Button

        ' Filter Bar Controls
        Private pnlFilterBar As Panel
        Private txtSearch As Krypton.Toolkit.KryptonTextBox
        Private cboStatusFilter As Krypton.Toolkit.KryptonComboBox
        Private cboStaffFilter As Krypton.Toolkit.KryptonComboBox
        Private cboPriorityFilter As Krypton.Toolkit.KryptonComboBox
        Private cboTaskTypeFilter As Krypton.Toolkit.KryptonComboBox
        Private cboDueDateFilter As Krypton.Toolkit.KryptonComboBox
        Private btnClearFilters As Forms.Common.ModernButton

        ' Main Workspace Split Layout
        Private scMainSplit As SplitContainer

        ' Left Column — Work Queue
        Private pnlWorkQueueHost As Panel
        Private pnlQueueContentScroll As Panel

        ' Urgency Grouping Containers
        Private pnlOverdueGroup As Panel
        Private pnlOverdueHeader As Panel
        Private lblOverdueHeader As Label
        Private pnlOverdueList As Panel

        Private pnlDueTodayGroup As Panel
        Private pnlDueTodayHeader As Panel
        Private lblDueTodayHeader As Label
        Private pnlDueTodayList As Panel

        Private pnlUpcomingGroup As Panel
        Private pnlUpcomingHeader As Panel
        Private lblUpcomingHeader As Label
        Private btnToggleUpcoming As Label
        Private pnlUpcomingList As Panel

        ' Pagination / Footer
        Private pnlQueueFooter As Panel
        Private lblPaginationInfo As Label

        ' Empty State Controls
        Private pnlEmptyStateHost As Panel
        Private lblEmptyStateIcon As Label
        Private lblEmptyStateTitle As Label
        Private lblEmptyStateSubtitle As Label
        Private btnEmptyStateClearFilters As Forms.Common.ModernButton
        Private lblPaginationPages As Label

        ' Attendance Gate Controls
        Private pnlAttendanceGate As Panel
        Private lblAttendanceMessage As Label

        ' Right Column — Task Details Sidebar
        Private pnlTaskDetailsSidebar As Panel
        Private pnlDetailsHeader As Panel
        Private lblDetailsTitle As Label
        Private lblCloseDetails As Label

        Private pnlDetailsScrollContent As Panel

        ' Task Details Sub-sections
        Private lblDetailTaskName As Label
        Private lblDetailTaskType As Label
        Private lblDetailPriorityBadge As Label

        ' Metadata Grid Container
        Private pnlMetaGrid As TableLayoutPanel
        Private lblMetaClientVal As Label
        Private lblMetaGstinVal As Label
        Private lblMetaDueDateVal As Label
        Private lblMetaAssigneeVal As Label
        Private lblMetaStatusVal As Label
        Private lblMetaCreatedOnVal As Label
        Private lblMetaCreatedByVal As Label

        ' Next Action Section
        Private pnlNextActionBox As Panel
        Private lblNextActionHeader As Label
        Private lblNextActionDesc As Label
        Private pnlChecklistHost As FlowLayoutPanel

        ' Notes & Attachments
        Private pnlNotesBox As Panel
        Private lblNotesHeader As Label
        Private lblNotesContent As Label
        Private lblNotesAuthorDate As Label

        Private pnlAttachmentsBox As Panel
        Private lblAttachmentsHeader As Label
        Private pnlAttachmentCard As Panel
        Private lblAttFileName As Label
        Private lblAttMeta As Label
        Private btnDownloadAtt As PictureBox

        ' Bottom Action Bar
        Private pnlBottomActionBar As Panel
        Private btnAcceptTask As Forms.Common.ModernButton
        Private btnEditTask As Forms.Common.ModernButton
        Private btnDeleteTask As Forms.Common.ModernButton
        Private btnOpenWorkflow As Forms.Common.ModernButton
        Private btnMarkComplete As Forms.Common.ModernButton
        Private btnReassign As Forms.Common.ModernButton
        Private lblActionTip As Label

        ' Row Context Menu
        Private ctxRowMenu As ContextMenuStrip

        ' Win32 Search Banner
        <System.Runtime.InteropServices.DllImport("user32.dll", CharSet:=System.Runtime.InteropServices.CharSet.Auto)>
        Private Shared Function SendMessage(hWnd As IntPtr, msg As Integer, wParam As IntPtr, <System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)> lParam As String) As IntPtr
        End Function
        Private Const EM_SETCUEBANNER As Integer = &H1501

        Public Sub New(Optional mainShellHost As Forms.Main.FrmMainShell = Nothing)
            _mainShellHost = mainShellHost

            ' Initialize Data Services
            Dim config As IAppConfiguration = New AppConfiguration()
            Dim connFactory As IDatabaseConnectionFactory = New DbConnectionFactory(config)
            Dim sqlHelper As ISqlHelper = New SqlHelper(connFactory)
            _appLogger = New AppLogger(config)
            _auditLogger = New AuditLogger(sqlHelper)

            Dim taskRepo As DAL.Interfaces.ITaskRepository = New TaskRepository(sqlHelper)
            Dim clientRepo As DAL.Interfaces.IClientRepository = New ClientRepository(sqlHelper)
            Dim discussionRepo As DAL.Interfaces.IDiscussionRepository = New DiscussionRepository(sqlHelper)
            _activityRepo = New TaskActivityRepository(sqlHelper)
            _userRepo = New UserRepository(sqlHelper)
            Dim attendanceRepo As DAL.Interfaces.IAttendanceRepository = New AttendanceRepository(sqlHelper)
            Dim taskCategoryRepo As DAL.Interfaces.ITaskCategoryRepository = New TaskCategoryRepository(sqlHelper)
            Dim workflowEngine As ITaskWorkflowEngine = New TaskWorkflowEngine()

            _clientService = New ClientService(clientRepo, _appLogger, _auditLogger)
            _discussionService = New DiscussionService(discussionRepo, taskRepo, _appLogger, _auditLogger)
            _attendanceService = New AttendanceService(attendanceRepo, _userRepo, _appLogger, _auditLogger, Nothing)
            _taskCategoryService = New TaskCategoryService(taskCategoryRepo, _auditLogger)
            _taskService = New TaskManagementService(taskRepo, workflowEngine, _appLogger, _auditLogger, clientRepo, _userRepo, _activityRepo, connFactory)
            Me.DoubleBuffered = True
            Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
            Me.UpdateStyles()

            _isInitializingFilters = True
            Try
                InitializeComponent()
            Finally
                _isInitializingFilters = False
            End Try

            ' Execute startup assertions for dependencies and UI controls
            ValidateDependenciesInitialized()
            ValidateUiControlsInitialized()

            AddHandler _searchDebounceTimer.Tick, AddressOf OnSearchDebounceTimer_Tick

            Forms.Main.ThemeConstants.EnableDoubleBuffering(pnlWorkQueueHost)
            Forms.Main.ThemeConstants.EnableDoubleBuffering(pnlQueueContentScroll)
            Forms.Main.ThemeConstants.EnableDoubleBuffering(pnlTaskDetailsSidebar)
        End Sub

        Protected Overrides Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)
            SetSearchPlaceholder()
            CheckAttendanceGateAsync()
        End Sub

        Public Sub ApplyFilterPreset(filterPreset As String)
            If String.IsNullOrEmpty(filterPreset) Then Return
            _activePresetFilter = filterPreset.Trim().ToLowerInvariant()
            Select Case _activePresetFilter
                Case "duesoon", "due_soon", "due48h"
                    _activeTab = "NeedsAttention"
                    If cboDueDateFilter IsNot Nothing AndAlso cboDueDateFilter.Items.Count > 3 Then
                        cboDueDateFilter.SelectedIndex = 3
                    End If
                Case "overdue"
                    _activeTab = "NeedsAttention"
                    If cboDueDateFilter IsNot Nothing AndAlso cboDueDateFilter.Items.Count > 1 Then
                        cboDueDateFilter.SelectedIndex = 1
                    End If
                Case "completedtoday", "completed_today"
                    _activeTab = "Completed"
                    If cboDueDateFilter IsNot Nothing AndAlso cboDueDateFilter.Items.Count > 2 Then
                        cboDueDateFilter.SelectedIndex = 2
                    End If
                Case "completed"
                    _activeTab = "Completed"
                    If cboDueDateFilter IsNot Nothing Then
                        cboDueDateFilter.SelectedIndex = 0
                    End If
                Case "needsattention"
                    _activeTab = "NeedsAttention"
                    If cboDueDateFilter IsNot Nothing Then
                        cboDueDateFilter.SelectedIndex = 0
                    End If
            End Select
        End Sub

        Private _isDataLoaded As Boolean = False

        Protected Overrides Sub OnVisibleChanged(e As EventArgs)
            MyBase.OnVisibleChanged(e)
            If Me.Visible Then
                CheckAttendanceGateAsync()
            End If
            ' Data loading is exclusively managed by PreloadDataAsync via NavigationService contract.
            ' OnVisibleChanged performs visual layout adjustments only.
        End Sub

        Private Sub SetSearchPlaceholder()
            If txtSearch IsNot Nothing AndAlso txtSearch.IsHandleCreated Then
                SendMessage(txtSearch.Handle, EM_SETCUEBANNER, New IntPtr(1), "Search tasks, client, GSTIN...")
            End If
        End Sub

        Private Async Sub CheckAttendanceGateAsync()
            If CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser.Role = UserRole.Employee Then
                Dim todayRec = Await _attendanceService.GetTodayAttendanceForUserAsync(CurrentUserContext.CurrentUser.UserId)
                
                Dim isGated As Boolean = False
                Dim gateMsg As String = ""
                
                If todayRec Is Nothing Then
                    isGated = True
                    gateMsg = "Punch in to see your tasks"
                ElseIf todayRec.ClockOutTime.HasValue Then
                    isGated = True
                    gateMsg = "Your shift has ended for today"
                ElseIf todayRec.BreakStartTime.HasValue Then
                    isGated = True
                    gateMsg = "You're on lunch — resume work to continue"
                End If
                
                If isGated Then
                    lblAttendanceMessage.Text = gateMsg
                    lblAttendanceMessage.Location = New Point((pnlAttendanceGate.Width - lblAttendanceMessage.Width) \ 2, (pnlAttendanceGate.Height - lblAttendanceMessage.Height) \ 2)
                    pnlAttendanceGate.Visible = True
                    pnlAttendanceGate.BringToFront()
                    
                    ' Disable action buttons
                    If btnCreateTask IsNot Nothing Then btnCreateTask.Enabled = False
                    If btnAcceptTask IsNot Nothing Then btnAcceptTask.Enabled = False
                    If btnEditTask IsNot Nothing Then btnEditTask.Enabled = False
                    If btnDeleteTask IsNot Nothing Then btnDeleteTask.Enabled = False
                    If btnOpenWorkflow IsNot Nothing Then btnOpenWorkflow.Enabled = False
                    If btnMarkComplete IsNot Nothing Then btnMarkComplete.Enabled = False
                    If btnReassign IsNot Nothing Then btnReassign.Enabled = False
                Else
                    pnlAttendanceGate.Visible = False
                    If btnCreateTask IsNot Nothing Then btnCreateTask.Enabled = True
                    If btnAcceptTask IsNot Nothing Then btnAcceptTask.Enabled = True
                    If btnEditTask IsNot Nothing Then btnEditTask.Enabled = True
                    If btnDeleteTask IsNot Nothing Then btnDeleteTask.Enabled = True
                    If btnOpenWorkflow IsNot Nothing Then btnOpenWorkflow.Enabled = True
                    If btnMarkComplete IsNot Nothing Then btnMarkComplete.Enabled = True
                    If btnReassign IsNot Nothing Then btnReassign.Enabled = True
                End If
            Else
                If pnlAttendanceGate IsNot Nothing Then
                    pnlAttendanceGate.Visible = False
                End If
            End If
        End Sub

        Private Sub InitializeComponent()
            Me.SuspendLayout()
            Me.Dock = DockStyle.Fill
            Me.BackColor = ThemeConstants.WorkspaceBackground
            Me.Padding = New Padding(16, 12, 16, 16)
            Me.Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular)

            ' =========================================================================
            ' 1. TOP PAGE HEADER (+ Create Task & Navigation Tabs)
            ' =========================================================================
            pnlHeader = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 78,
                .BackColor = ThemeConstants.PearlHeaderBackground
            }

            lblTitle = New Label() With {
                .Text = "TASKS",
                .Font = New Font(ThemeConstants.FontNameDefault, 14.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(0, 0),
                .AutoSize = True
            }

            lblSubtitle = New Label() With {
                .Text = "My work queue  •  Loading active tasks...",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular),
                .ForeColor = ThemeConstants.TextSecondary,
                .Location = New Point(0, 26),
                .AutoSize = True
            }

            btnCreateTask = New Forms.Common.ModernButton() With {
                .Text = " + Create Task  ▼",
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Primary,
                .Size = New Size(140, 36),
                .Location = New Point(pnlHeader.Width - 140, 0),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right,
                .Visible = If(CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser IsNot Nothing, CurrentUserContext.CurrentUser.Role <> UserRole.Employee, True)
            }
            AddHandler btnCreateTask.Click, AddressOf btnCreateTask_Click

            ' Navigation Tabs Container
            pnlTabsContainer = New Panel() With {
                .Location = New Point(0, 48),
                .Size = New Size(600, 30),
                .BackColor = Color.Transparent
            }

            btnTabNeedsAttention = CreateTabButton("Needs Attention (0)", True, 0)
            btnTabMyTasks = CreateTabButton("My Tasks (0)", False, 155)
            btnTabAllActive = CreateTabButton("All Active (0)", False, 280)
            btnTabCompleted = CreateTabButton("Completed", False, 400)
            
            Dim currentRole As UserRole = If(CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser IsNot Nothing, CurrentUserContext.CurrentUser.Role, UserRole.Employee)
            If currentRole = UserRole.Admin OrElse currentRole = UserRole.Owner Then
                btnTabDeletedTasks = CreateTabButton("Deleted Tasks", False, 520)
                AddHandler btnTabDeletedTasks.Click, Sub(s, e) SwitchActiveTab("DeletedTasks")
            End If

            AddHandler btnTabNeedsAttention.Click, Sub(s, e) SwitchActiveTab("NeedsAttention")
            AddHandler btnTabMyTasks.Click, Sub(s, e) SwitchActiveTab("MyTasks")
            AddHandler btnTabAllActive.Click, Sub(s, e) SwitchActiveTab("AllActive")
            AddHandler btnTabCompleted.Click, Sub(s, e) SwitchActiveTab("Completed")

            If btnTabDeletedTasks IsNot Nothing Then pnlTabsContainer.Controls.Add(btnTabDeletedTasks)
            pnlTabsContainer.Controls.Add(btnTabCompleted)
            pnlTabsContainer.Controls.Add(btnTabAllActive)
            pnlTabsContainer.Controls.Add(btnTabMyTasks)
            pnlTabsContainer.Controls.Add(btnTabNeedsAttention)

            pnlHeader.Controls.Add(pnlTabsContainer)
            pnlHeader.Controls.Add(btnCreateTask)
            pnlHeader.Controls.Add(lblSubtitle)
            pnlHeader.Controls.Add(lblTitle)

            ' =========================================================================
            ' 2. FILTER BAR
            ' Exact set: Search | Status | Assigned Staff | Priority | Task Type | Due Date | Clear
            ' =========================================================================
            pnlFilterBar = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 44,
                .BackColor = Color.White,
                .Margin = New Padding(0, 0, 0, 10),
                .Padding = New Padding(8, 8, 8, 8)
            }
            AddHandler pnlFilterBar.Paint, Sub(s, e)
                                               e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
                                               Using p As New Pen(Color.FromArgb(226, 232, 240), 1)
                                                   e.Graphics.DrawRectangle(p, 0, 0, pnlFilterBar.Width - 1, pnlFilterBar.Height - 1)
                                               End Using
                                           End Sub

            txtSearch = New Krypton.Toolkit.KryptonTextBox() With {
                .Location = New Point(10, 9),
                .Size = New Size(175, 24),
                .TabIndex = 0
            }
            ThemeConstants.ApplyAppTextBoxStyle(txtSearch)
            AddHandler txtSearch.HandleCreated, Sub(s, e) SetSearchPlaceholder()
            AddHandler txtSearch.TextChanged, AddressOf OnSearchTextChanged

            cboStatusFilter = New Krypton.Toolkit.KryptonComboBox() With {
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Location = New Point(191, 9),
                .Size = New Size(115, 24),
                .TabIndex = 1
            }
            ThemeConstants.ApplyAppComboBoxStyle(cboStatusFilter)
            cboStatusFilter.Items.Add(New FilterOption(Of StatusFilterKind)("All Status", StatusFilterKind.AllStatus))
            cboStatusFilter.Items.Add(New FilterOption(Of StatusFilterKind)("Pending", StatusFilterKind.Pending))
            cboStatusFilter.Items.Add(New FilterOption(Of StatusFilterKind)("In Progress", StatusFilterKind.InProgress))
            cboStatusFilter.Items.Add(New FilterOption(Of StatusFilterKind)("Waiting for Client", StatusFilterKind.WaitingForClient))
            cboStatusFilter.Items.Add(New FilterOption(Of StatusFilterKind)("Waiting for Documents", StatusFilterKind.WaitingForDocuments))
            cboStatusFilter.Items.Add(New FilterOption(Of StatusFilterKind)("Under Review", StatusFilterKind.UnderReview))
            cboStatusFilter.Items.Add(New FilterOption(Of StatusFilterKind)("On Hold", StatusFilterKind.OnHold))
            cboStatusFilter.SelectedIndex = 0
            AddHandler cboStatusFilter.SelectedIndexChanged, AddressOf OnFilterChanged

            cboStaffFilter = New Krypton.Toolkit.KryptonComboBox() With {
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Location = New Point(312, 9),
                .Size = New Size(105, 24),
                .TabIndex = 2
            }
            ThemeConstants.ApplyAppComboBoxStyle(cboStaffFilter)
            cboStaffFilter.Items.Add(New FilterOption(Of String)("All Staff", "all"))
            cboStaffFilter.SelectedIndex = 0
            AddHandler cboStaffFilter.SelectedIndexChanged, AddressOf OnFilterChanged

            cboPriorityFilter = New Krypton.Toolkit.KryptonComboBox() With {
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Location = New Point(423, 9),
                .Size = New Size(95, 24),
                .TabIndex = 3
            }
            ThemeConstants.ApplyAppComboBoxStyle(cboPriorityFilter)
            cboPriorityFilter.Items.Add(New FilterOption(Of Nullable(Of TaskPriority))("All Priority", Nothing))
            cboPriorityFilter.Items.Add(New FilterOption(Of Nullable(Of TaskPriority))("Critical", TaskPriority.Urgent))
            cboPriorityFilter.Items.Add(New FilterOption(Of Nullable(Of TaskPriority))("High", TaskPriority.High))
            cboPriorityFilter.Items.Add(New FilterOption(Of Nullable(Of TaskPriority))("Medium", TaskPriority.Medium))
            cboPriorityFilter.Items.Add(New FilterOption(Of Nullable(Of TaskPriority))("Low", TaskPriority.Low))
            cboPriorityFilter.SelectedIndex = 0
            AddHandler cboPriorityFilter.SelectedIndexChanged, AddressOf OnFilterChanged

            cboTaskTypeFilter = New Krypton.Toolkit.KryptonComboBox() With {
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Location = New Point(524, 9),
                .Size = New Size(115, 24),
                .TabIndex = 4
            }
            ThemeConstants.ApplyAppComboBoxStyle(cboTaskTypeFilter)
            cboTaskTypeFilter.Items.Add(New FilterOption(Of String)("All Task Types", "all"))
            cboTaskTypeFilter.Items.Add(New FilterOption(Of String)("GST Compliance", "GST Compliance"))
            cboTaskTypeFilter.Items.Add(New FilterOption(Of String)("Income Tax", "Income Tax"))
            cboTaskTypeFilter.Items.Add(New FilterOption(Of String)("TDS Returns", "TDS Returns"))
            cboTaskTypeFilter.Items.Add(New FilterOption(Of String)("ROC / MCA", "ROC / MCA"))
            cboTaskTypeFilter.Items.Add(New FilterOption(Of String)("Accounting", "Accounting"))
            cboTaskTypeFilter.Items.Add(New FilterOption(Of String)("Audit", "Audit"))
            cboTaskTypeFilter.Items.Add(New FilterOption(Of String)("Client Onboarding", "Client Onboarding"))
            cboTaskTypeFilter.Items.Add(New FilterOption(Of String)("Other", "Other"))
            cboTaskTypeFilter.SelectedIndex = 0
            AddHandler cboTaskTypeFilter.SelectedIndexChanged, AddressOf OnFilterChanged

            cboDueDateFilter = New Krypton.Toolkit.KryptonComboBox() With {
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Location = New Point(645, 9),
                .Size = New Size(105, 24),
                .TabIndex = 5
            }
            ThemeConstants.ApplyAppComboBoxStyle(cboDueDateFilter)
            cboDueDateFilter.Items.Add(New FilterOption(Of String)("All Due Dates", "all"))
            cboDueDateFilter.Items.Add(New FilterOption(Of String)("Overdue", "overdue"))
            cboDueDateFilter.Items.Add(New FilterOption(Of String)("Due Today", "duetoday"))
            cboDueDateFilter.Items.Add(New FilterOption(Of String)("Due This Week", "dueweek"))
            cboDueDateFilter.Items.Add(New FilterOption(Of String)("Due This Month", "duemonth"))
            cboDueDateFilter.SelectedIndex = 0
            AddHandler cboDueDateFilter.SelectedIndexChanged, AddressOf OnFilterChanged

            btnClearFilters = New Forms.Common.ModernButton() With {
                .Text = "Clear",
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Secondary,
                .Size = New Size(70, 26),
                .Location = New Point(pnlFilterBar.Width - 80, 8),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            AddHandler btnClearFilters.Click, AddressOf btnClearFilters_Click

            pnlFilterBar.Controls.Add(btnClearFilters)
            pnlFilterBar.Controls.Add(cboDueDateFilter)
            pnlFilterBar.Controls.Add(cboTaskTypeFilter)
            pnlFilterBar.Controls.Add(cboPriorityFilter)
            pnlFilterBar.Controls.Add(cboStaffFilter)
            pnlFilterBar.Controls.Add(cboStatusFilter)
            pnlFilterBar.Controls.Add(txtSearch)

            ' =========================================================================
            ' 3. MAIN SPLIT CONTAINER (64% Left Work Queue / 36% Right Sticky Task Details)
            ' =========================================================================
            scMainSplit = New SplitContainer() With {
                .Dock = DockStyle.Fill,
                .Orientation = Orientation.Vertical,
                .SplitterDistance = CInt(Me.Width * 0.64),
                .SplitterWidth = 8,
                .BackColor = ThemeConstants.WorkspaceBackground
            }

            ' -------------------------------------------------------------------------
            ' LEFT PANEL: Work Queue Container
            ' -------------------------------------------------------------------------
            pnlWorkQueueHost = New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = Color.White,
                .Padding = New Padding(12)
            }
            AddHandler pnlWorkQueueHost.Paint, Sub(s, e)
                                                   e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
                                                   Using p As New Pen(Color.FromArgb(226, 232, 240), 1)
                                                       e.Graphics.DrawRectangle(p, 0, 0, pnlWorkQueueHost.Width - 1, pnlWorkQueueHost.Height - 1)
                                                   End Using
                                               End Sub

            pnlQueueContentScroll = New Panel() With {
                .Dock = DockStyle.Fill,
                .AutoScroll = True,
                .BackColor = Color.White
            }

            ' Section 1: OVERDUE Group
            pnlOverdueGroup = CreateUrgencyGroupPanel("OVERDUE", Color.FromArgb(239, 68, 68), pnlOverdueHeader, lblOverdueHeader, pnlOverdueList)
            ' Section 2: DUE TODAY Group
            pnlDueTodayGroup = CreateUrgencyGroupPanel("DUE TODAY", Color.FromArgb(245, 158, 11), pnlDueTodayHeader, lblDueTodayHeader, pnlDueTodayList)
            ' Section 3: UPCOMING Group (Collapsed by default)
            pnlUpcomingGroup = CreateUpcomingGroupPanel()

            pnlQueueContentScroll.Controls.Add(pnlUpcomingGroup)
            pnlQueueContentScroll.Controls.Add(pnlDueTodayGroup)
            pnlQueueContentScroll.Controls.Add(pnlOverdueGroup)

            ' Empty State Container
            pnlEmptyStateHost = New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = Color.White,
                .Visible = False
            }
            lblEmptyStateIcon = New Label() With {
                .Text = "🔍",
                .Font = New Font("Segoe UI Emoji", 26.0!, FontStyle.Regular),
                .ForeColor = ThemeConstants.TextSecondary,
                .Size = New Size(60, 42),
                .TextAlign = ContentAlignment.MiddleCenter
            }
            lblEmptyStateTitle = New Label() With {
                .Text = "No tasks found",
                .Font = New Font(ThemeConstants.FontNameDefault, 12.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .AutoSize = True,
                .TextAlign = ContentAlignment.MiddleCenter
            }
            lblEmptyStateSubtitle = New Label() With {
                .Text = "No tasks match the selected filters.",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular),
                .ForeColor = ThemeConstants.TextSecondary,
                .AutoSize = True,
                .TextAlign = ContentAlignment.MiddleCenter
            }
            btnEmptyStateClearFilters = New Forms.Common.ModernButton() With {
                .Text = "Clear Filters",
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Secondary,
                .Size = New Size(120, 32),
                .Visible = False
            }
            AddHandler btnEmptyStateClearFilters.Click, AddressOf btnClearFilters_Click

            pnlEmptyStateHost.Controls.Add(btnEmptyStateClearFilters)
            pnlEmptyStateHost.Controls.Add(lblEmptyStateSubtitle)
            pnlEmptyStateHost.Controls.Add(lblEmptyStateTitle)
            pnlEmptyStateHost.Controls.Add(lblEmptyStateIcon)

            AddHandler pnlEmptyStateHost.Resize, Sub(s, e) CenterEmptyStateControls()

            pnlQueueContentScroll.Controls.Add(pnlEmptyStateHost)

            ' Work Queue Footer / Pagination Bar
            pnlQueueFooter = New Panel() With {
                .Dock = DockStyle.Bottom,
                .Height = 36,
                .BackColor = Color.FromArgb(248, 250, 252)
            }
            lblPaginationInfo = New Label() With {
                .Text = "Showing tasks...",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Regular),
                .ForeColor = ThemeConstants.TextSecondary,
                .Location = New Point(12, 10),
                .AutoSize = True
            }
            lblPaginationPages = New Label() With {
                .Text = "Task Queue Mode",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.PrimaryAccent,
                .Location = New Point(pnlQueueFooter.Width - 140, 10),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right,
                .AutoSize = True
            }
            pnlQueueFooter.Controls.Add(lblPaginationPages)
            pnlQueueFooter.Controls.Add(lblPaginationInfo)

            pnlWorkQueueHost.Controls.Add(pnlQueueContentScroll)
            pnlWorkQueueHost.Controls.Add(pnlQueueFooter)
            scMainSplit.Panel1.Controls.Add(pnlWorkQueueHost)

            ' -------------------------------------------------------------------------
            ' RIGHT PANEL: Sticky Task Details Sidebar (36%)
            ' -------------------------------------------------------------------------
            pnlTaskDetailsSidebar = New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = Color.White,
                .Padding = New Padding(14)
            }
            AddHandler pnlTaskDetailsSidebar.Paint, Sub(s, e)
                                                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
                                                        Using p As New Pen(Color.FromArgb(226, 232, 240), 1)
                                                            e.Graphics.DrawRectangle(p, 0, 0, pnlTaskDetailsSidebar.Width - 1, pnlTaskDetailsSidebar.Height - 1)
                                                        End Using
                                                    End Sub

            ' Details Header
            pnlDetailsHeader = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 30,
                .BackColor = Color.White
            }
            lblDetailsTitle = New Label() With {
                .Text = "📌 TASK DETAILS",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(0, 4),
                .AutoSize = True
            }
            lblCloseDetails = New Label() With {
                .Text = "✕",
                .Font = New Font(ThemeConstants.FontNameDefault, 10.0!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(148, 163, 184),
                .Location = New Point(pnlDetailsHeader.Width - 24, 2),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right,
                .Cursor = Cursors.Hand
            }
            pnlDetailsHeader.Controls.Add(lblCloseDetails)
            pnlDetailsHeader.Controls.Add(lblDetailsTitle)

            ' Scrollable Content Area for Details
            pnlDetailsScrollContent = New Panel() With {
                .Dock = DockStyle.Fill,
                .AutoScroll = True,
                .BackColor = Color.White,
                .Padding = New Padding(0, 8, 0, 8)
            }

            ' Title & Priority Row
            lblDetailTaskName = New Label() With {
                .Text = "Select a task from queue",
                .Font = New Font(ThemeConstants.FontNameDefault, 11.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(0, 4),
                .Size = New Size(220, 44)
            }
            lblDetailPriorityBadge = New Label() With {
                .Text = "HIGH PRIORITY",
                .Font = New Font(ThemeConstants.FontNameDefault, 7.5!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(220, 38, 38),
                .BackColor = Color.FromArgb(254, 226, 226),
                .Size = New Size(95, 20),
                .Location = New Point(230, 4),
                .TextAlign = ContentAlignment.MiddleCenter
            }
            lblDetailTaskType = New Label() With {
                .Text = "Compliance Return",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Regular),
                .ForeColor = ThemeConstants.TextSecondary,
                .Location = New Point(0, 48),
                .AutoSize = True
            }

            ' Metadata Table Layout
            pnlMetaGrid = New TableLayoutPanel() With {
                .Location = New Point(0, 72),
                .Size = New Size(320, 185),
                .ColumnCount = 2,
                .RowCount = 7,
                .BackColor = Color.FromArgb(248, 250, 252)
            }
            pnlMetaGrid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 45.0!))
            pnlMetaGrid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 55.0!))

            lblMetaClientVal = AddMetaGridRow(pnlMetaGrid, 0, "Client:", "ABC Traders")
            lblMetaGstinVal = AddMetaGridRow(pnlMetaGrid, 1, "GSTIN / Ref:", "27ABCDE1234F1Z5 🔗")
            lblMetaDueDateVal = AddMetaGridRow(pnlMetaGrid, 2, "Due Date:", "Due Today (19 Aug)")
            lblMetaAssigneeVal = AddMetaGridRow(pnlMetaGrid, 3, "Assigned To:", "Priya Shah")
            lblMetaStatusVal = AddMetaGridRow(pnlMetaGrid, 4, "Status:", "Pending")
            lblMetaCreatedOnVal = AddMetaGridRow(pnlMetaGrid, 5, "Created On:", "—")
            lblMetaCreatedByVal = AddMetaGridRow(pnlMetaGrid, 6, "Created By:", "—")

            ' NEXT ACTION Box
            pnlNextActionBox = New Panel() With {
                .Location = New Point(0, 210),
                .Size = New Size(320, 160),
                .BackColor = Color.FromArgb(238, 242, 255),
                .Padding = New Padding(10)
            }
            lblNextActionHeader = New Label() With {
                .Text = "🎯 NEXT ACTION",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(49, 46, 129),
                .Location = New Point(8, 8),
                .AutoSize = True
            }
            lblNextActionDesc = New Label() With {
                .Text = "Prepare return for statutory client filing.",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.25!, FontStyle.Regular),
                .ForeColor = Color.FromArgb(55, 48, 163),
                .Location = New Point(8, 28),
                .Size = New Size(300, 28)
            }
            pnlChecklistHost = New FlowLayoutPanel() With {
                .Location = New Point(8, 58),
                .Size = New Size(304, 96),
                .FlowDirection = FlowDirection.TopDown,
                .WrapContents = False,
                .AutoScroll = True
            }
            pnlNextActionBox.Controls.Add(pnlChecklistHost)
            pnlNextActionBox.Controls.Add(lblNextActionDesc)
            pnlNextActionBox.Controls.Add(lblNextActionHeader)

            ' NOTES Box
            pnlNotesBox = New Panel() With {
                .Location = New Point(0, 380),
                .Size = New Size(320, 85),
                .BackColor = Color.FromArgb(254, 243, 199),
                .Padding = New Padding(10)
            }
            lblNotesHeader = New Label() With {
                .Text = "📝 NOTES",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(146, 64, 14),
                .Location = New Point(8, 6),
                .AutoSize = True
            }
            lblNotesContent = New Label() With {
                .Text = "Reconcile GSTR-2B ITC carefully before filing.",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.25!, FontStyle.Regular),
                .ForeColor = Color.FromArgb(120, 53, 15),
                .Location = New Point(8, 24),
                .Size = New Size(300, 36)
            }
            lblNotesAuthorDate = New Label() With {
                .Text = "— Amit Kumar (18 Aug 2026)",
                .Font = New Font(ThemeConstants.FontNameDefault, 7.5!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(180, 83, 9),
                .Location = New Point(8, 62),
                .AutoSize = True
            }
            pnlNotesBox.Controls.Add(lblNotesAuthorDate)
            pnlNotesBox.Controls.Add(lblNotesContent)
            pnlNotesBox.Controls.Add(lblNotesHeader)

            ' ATTACHMENTS Box
            pnlAttachmentsBox = New Panel() With {
                .Location = New Point(0, 475),
                .Size = New Size(320, 75),
                .BackColor = Color.White
            }
            lblAttachmentsHeader = New Label() With {
                .Text = "📎 ATTACHMENTS (0)",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(0, 4),
                .AutoSize = True
            }
            pnlAttachmentCard = New Panel() With {
                .Location = New Point(0, 24),
                .Size = New Size(320, 46),
                .BackColor = Color.FromArgb(248, 250, 252)
            }
            lblAttFileName = New Label() With {
                .Text = "No file attachments uploaded",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.25!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(100, 116, 139),
                .Location = New Point(8, 6),
                .AutoSize = True
            }
            lblAttMeta = New Label() With {
                .Text = "No attachments linked to this task",
                .Font = New Font(ThemeConstants.FontNameDefault, 7.5!, FontStyle.Regular),
                .ForeColor = ThemeConstants.TextSecondary,
                .Location = New Point(8, 24),
                .AutoSize = True
            }
            pnlAttachmentCard.Controls.Add(lblAttMeta)
            pnlAttachmentCard.Controls.Add(lblAttFileName)
            pnlAttachmentsBox.Controls.Add(pnlAttachmentCard)
            pnlAttachmentsBox.Controls.Add(lblAttachmentsHeader)

            pnlDetailsScrollContent.Controls.Add(pnlAttachmentsBox)
            pnlDetailsScrollContent.Controls.Add(pnlNotesBox)
            pnlDetailsScrollContent.Controls.Add(pnlNextActionBox)
            pnlDetailsScrollContent.Controls.Add(pnlMetaGrid)
            pnlDetailsScrollContent.Controls.Add(lblDetailTaskType)
            pnlDetailsScrollContent.Controls.Add(lblDetailPriorityBadge)
            pnlDetailsScrollContent.Controls.Add(lblDetailTaskName)

            ' Bottom Action Bar (2x2 Clean Action Buttons Grid + Tip Label)
            pnlBottomActionBar = New Panel() With {
                .Dock = DockStyle.Bottom,
                .Height = 115,
                .BackColor = Color.White,
                .Padding = New Padding(0, 4, 0, 0)
            }

            btnAcceptTask = New Forms.Common.ModernButton() With {
                .Text = "✓ Accept Task",
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Custom,
                .BackColor = Color.FromArgb(16, 185, 129),
                .ForeColor = Color.White,
                .Size = New Size(150, 32),
                .Location = New Point(0, 4),
                .Visible = False
            }
            AddHandler btnAcceptTask.Click, AddressOf btnAcceptTask_Click

            currentRole = If(CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser IsNot Nothing, CurrentUserContext.CurrentUser.Role, UserRole.Employee)
            Dim isEmployee = currentRole = UserRole.Employee

            btnEditTask = New Forms.Common.ModernButton() With {
                .Text = "✏ Edit Task",
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Secondary,
                .Size = New Size(150, 32),
                .Location = New Point(0, 4),
                .Visible = Not isEmployee
            }
            AddHandler btnEditTask.Click, AddressOf btnEditTask_Click

            btnDeleteTask = New Forms.Common.ModernButton() With {
                .Text = "🗑 Delete Task",
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Custom,
                .BackColor = Color.FromArgb(239, 68, 68),
                .ForeColor = Color.White,
                .Size = New Size(150, 32),
                .Location = New Point(158, 4),
                .Visible = Not isEmployee
            }
            AddHandler btnDeleteTask.Click, AddressOf btnDeleteTask_Click

            btnOpenWorkflow = New Forms.Common.ModernButton() With {
                .Text = "➔ Open Workflow",
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Primary,
                .Size = New Size(150, 32),
                .Location = New Point(0, 42)
            }
            AddHandler btnOpenWorkflow.Click, AddressOf btnOpenWorkflow_Click

            btnMarkComplete = New Forms.Common.ModernButton() With {
                .Text = If(isEmployee, "✓ Submit for Review", "✓ Mark as Complete"),
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Custom,
                .BackColor = Color.FromArgb(16, 185, 129),
                .ForeColor = Color.White,
                .Size = New Size(150, 32),
                .Location = New Point(158, 42)
            }
            AddHandler btnMarkComplete.Click, AddressOf btnMarkComplete_Click

            lblActionTip = New Label() With {
                .Text = "💡 Tip: Double click a task row to open workflow workspace",
                .Font = New Font(ThemeConstants.FontNameDefault, 7.75!, FontStyle.Regular),
                .ForeColor = ThemeConstants.TextSecondary,
                .Location = New Point(0, 82),
                .AutoSize = True
            }

            pnlBottomActionBar.Controls.Add(lblActionTip)
            pnlBottomActionBar.Controls.Add(btnMarkComplete)
            pnlBottomActionBar.Controls.Add(btnOpenWorkflow)
            pnlBottomActionBar.Controls.Add(btnDeleteTask)
            pnlBottomActionBar.Controls.Add(btnEditTask)
            pnlBottomActionBar.Controls.Add(btnAcceptTask)

            pnlTaskDetailsSidebar.Controls.Add(pnlDetailsScrollContent)
            pnlTaskDetailsSidebar.Controls.Add(pnlBottomActionBar)
            pnlTaskDetailsSidebar.Controls.Add(pnlDetailsHeader)

            scMainSplit.Panel2.Controls.Add(pnlTaskDetailsSidebar)

            ' Context Menu
            InitializeRowContextMenu()

            ' Attendance Gate Initialization
            pnlAttendanceGate = New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = Color.FromArgb(240, Color.White),
                .Visible = False
            }
            lblAttendanceMessage = New Label() With {
                .Text = "Punch in to see your tasks",
                .Font = New Font(ThemeConstants.FontNameDefault, 14.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .AutoSize = True,
                .TextAlign = ContentAlignment.MiddleCenter
            }
            pnlAttendanceGate.Controls.Add(lblAttendanceMessage)
            AddHandler pnlAttendanceGate.Resize, Sub(s, e)
                                                     lblAttendanceMessage.Location = New Point((pnlAttendanceGate.Width - lblAttendanceMessage.Width) \ 2, (pnlAttendanceGate.Height - lblAttendanceMessage.Height) \ 2)
                                                 End Sub

            Me.Controls.Add(pnlAttendanceGate)
            Me.Controls.Add(scMainSplit)
            Me.Controls.Add(pnlFilterBar)
            Me.Controls.Add(pnlHeader)

            Me.ResumeLayout(False)

            ' Debug Assertion: Validate that all declared UI controls are properly instantiated
            ValidateUiControlsInitialized()
        End Sub

        ''' <summary>
        ''' Debug assertion validator verifying that all declared UI controls are properly instantiated.
        ''' Throws InvalidOperationException if any required control is Nothing.
        ''' </summary>
        Private Sub ValidateDependenciesInitialized()
            Dim missingDeps As New List(Of String)()

            If _taskService Is Nothing Then missingDeps.Add("_taskService")
            If _clientService Is Nothing Then missingDeps.Add("_clientService")
            If _userRepo Is Nothing Then missingDeps.Add("_userRepo")
            If _activityRepo Is Nothing Then missingDeps.Add("_activityRepo")
            If _discussionService Is Nothing Then missingDeps.Add("_discussionService")
            If _appLogger Is Nothing Then missingDeps.Add("_appLogger")
            If _auditLogger Is Nothing Then missingDeps.Add("_auditLogger")

            If missingDeps.Count > 0 Then
                Dim errList As String = String.Join(", ", missingDeps)
                If _appLogger IsNot Nothing Then
                    _appLogger.LogError($"[DependencyError] Service dependency initialization check failed! Missing dependencies: {errList}", "DailyTasksControl")
                End If
#If DEBUG Then
                Throw New InvalidOperationException($"[DEBUG FAIL-FAST] DailyTasksControl dependency initialization incomplete. Missing: {errList}")
#End If
            End If
        End Sub

        Private Sub ValidateUiControlsInitialized()
            Dim missingControls As New List(Of String)()

            If pnlHeader Is Nothing Then missingControls.Add("pnlHeader")
            If pnlFilterBar Is Nothing Then missingControls.Add("pnlFilterBar")
            If txtSearch Is Nothing Then missingControls.Add("txtSearch")
            If cboStatusFilter Is Nothing Then missingControls.Add("cboStatusFilter")
            If cboStaffFilter Is Nothing Then missingControls.Add("cboStaffFilter")
            If cboPriorityFilter Is Nothing Then missingControls.Add("cboPriorityFilter")
            If cboTaskTypeFilter Is Nothing Then missingControls.Add("cboTaskTypeFilter")
            If cboDueDateFilter Is Nothing Then missingControls.Add("cboDueDateFilter")
            If btnClearFilters Is Nothing Then missingControls.Add("btnClearFilters")
            If scMainSplit Is Nothing Then missingControls.Add("scMainSplit")
            If pnlWorkQueueHost Is Nothing Then missingControls.Add("pnlWorkQueueHost")
            If pnlTaskDetailsSidebar Is Nothing Then missingControls.Add("pnlTaskDetailsSidebar")
            If pnlMetaGrid Is Nothing Then missingControls.Add("pnlMetaGrid")
            If lblMetaClientVal Is Nothing Then missingControls.Add("lblMetaClientVal")
            If lblMetaGstinVal Is Nothing Then missingControls.Add("lblMetaGstinVal")
            If lblMetaDueDateVal Is Nothing Then missingControls.Add("lblMetaDueDateVal")
            If lblMetaAssigneeVal Is Nothing Then missingControls.Add("lblMetaAssigneeVal")
            If lblMetaStatusVal Is Nothing Then missingControls.Add("lblMetaStatusVal")
            If lblMetaCreatedOnVal Is Nothing Then missingControls.Add("lblMetaCreatedOnVal")
            If lblMetaCreatedByVal Is Nothing Then missingControls.Add("lblMetaCreatedByVal")
            If pnlChecklistHost Is Nothing Then missingControls.Add("pnlChecklistHost")
            If lblNotesHeader Is Nothing Then missingControls.Add("lblNotesHeader")

            If missingControls.Count > 0 Then
                Dim errList As String = String.Join(", ", missingControls)
                If _appLogger IsNot Nothing Then
                    _appLogger.LogError($"[TaskUiError] UI Initialization Check Failed! Missing controls: {errList}", "DailyTasksControl")
                End If
#If DEBUG Then
                Throw New InvalidOperationException($"[DEBUG FAIL-FAST] DailyTasksControl UI initialization incomplete. Missing controls: {errList}")
#End If
            End If
        End Sub

        ' Helper to build urgent section header panel
        Private Function CreateUrgencyGroupPanel(headerTitle As String, headerColor As Color, ByRef pnlHeaderOut As Panel, ByRef lblHeaderOut As Label, ByRef pnlListOut As Panel) As Panel
            Dim pnlGroup As New Panel() With {
                .Dock = DockStyle.Top,
                .AutoSize = True,
                .AutoSizeMode = AutoSizeMode.GrowAndShrink,
                .Margin = New Padding(0, 0, 0, 16)
            }

            pnlHeaderOut = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 30,
                .BackColor = Color.Transparent
            }

            lblHeaderOut = New Label() With {
                .Text = headerTitle & " (0)",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.5!, FontStyle.Bold),
                .ForeColor = headerColor,
                .Location = New Point(0, 4),
                .AutoSize = True
            }
            pnlHeaderOut.Controls.Add(lblHeaderOut)

            pnlListOut = New Panel() With {
                .Dock = DockStyle.Top,
                .AutoSize = True,
                .AutoSizeMode = AutoSizeMode.GrowAndShrink,
                .BackColor = Color.White
            }

            pnlGroup.Controls.Add(pnlListOut)
            pnlGroup.Controls.Add(pnlHeaderOut)

            Return pnlGroup
        End Function

        Private Function CreateUpcomingGroupPanel() As Panel
            Dim pnlGroup As New Panel() With {
                .Dock = DockStyle.Top,
                .AutoSize = True,
                .AutoSizeMode = AutoSizeMode.GrowAndShrink,
                .Margin = New Padding(0, 0, 0, 16)
            }

            pnlUpcomingHeader = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 30,
                .BackColor = Color.Transparent,
                .Cursor = Cursors.Hand
            }
            AddHandler pnlUpcomingHeader.Click, AddressOf ToggleUpcomingSection

            lblUpcomingHeader = New Label() With {
                .Text = "🟡 UPCOMING (0)",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.5!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(202, 138, 4),
                .Location = New Point(0, 4),
                .AutoSize = True,
                .Cursor = Cursors.Hand
            }
            AddHandler lblUpcomingHeader.Click, AddressOf ToggleUpcomingSection

            btnToggleUpcoming = New Label() With {
                .Text = "▼ Expand",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextSecondary,
                .Location = New Point(200, 6),
                .AutoSize = True,
                .Cursor = Cursors.Hand
            }
            AddHandler btnToggleUpcoming.Click, AddressOf ToggleUpcomingSection

            pnlUpcomingHeader.Controls.Add(btnToggleUpcoming)
            pnlUpcomingHeader.Controls.Add(lblUpcomingHeader)

            pnlUpcomingList = New Panel() With {
                .Dock = DockStyle.Top,
                .AutoSize = True,
                .AutoSizeMode = AutoSizeMode.GrowAndShrink,
                .BackColor = Color.White,
                .Visible = False ' Collapsed by default per requirements
            }

            pnlGroup.Controls.Add(pnlUpcomingList)
            pnlGroup.Controls.Add(pnlUpcomingHeader)

            Return pnlGroup
        End Function

        Private Sub ToggleUpcomingSection(sender As Object, e As EventArgs)
            _isUpcomingExpanded = Not _isUpcomingExpanded
            pnlUpcomingList.Visible = _isUpcomingExpanded
            btnToggleUpcoming.Text = If(_isUpcomingExpanded, "▲ Collapse", "▼ Expand")
        End Sub

        Private Function AddMetaGridRow(tbl As TableLayoutPanel, rowIndex As Integer, labelText As String, valText As String) As Label
            Dim lblTag As New Label() With {
                .Text = labelText,
                .Font = New Font(ThemeConstants.FontNameDefault, 8.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextSecondary,
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleLeft
            }
            Dim lblVal As New Label() With {
                .Text = valText,
                .Font = New Font(ThemeConstants.FontNameDefault, 8.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleLeft
            }
            tbl.Controls.Add(lblTag, 0, rowIndex)
            tbl.Controls.Add(lblVal, 1, rowIndex)
            Return lblVal
        End Function

        Private Function CreateTabButton(text As String, isSelected As Boolean, leftPos As Integer) As Button
            Dim btn As New Button() With {
                .Text = text,
                .Location = New Point(leftPos, 0),
                .AutoSize = True
            }
            ThemeConstants.ApplyTabStyle(btn, isSelected)
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(238, 242, 255)
            Return btn
        End Function

        Private Sub InitializeRowContextMenu()
            ctxRowMenu = New ContextMenuStrip()
            Dim currentRole As UserRole = If(CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser IsNot Nothing, CurrentUserContext.CurrentUser.Role, UserRole.Employee)
            Dim isEmployee = currentRole = UserRole.Employee

            Dim itemAccept As New ToolStripMenuItem("✓ Accept Task", Nothing, AddressOf btnAcceptTask_Click)
            Dim itemEdit As New ToolStripMenuItem("✏️ Edit Task", Nothing, AddressOf btnEditTask_Click)
            itemEdit.Visible = Not isEmployee
            Dim itemDelete As New ToolStripMenuItem("🗑️ Soft Delete Task", Nothing, AddressOf btnDeleteTask_Click)
            itemDelete.Visible = Not isEmployee
            Dim itemOpen As New ToolStripMenuItem("➔ Open Task / Workflow", Nothing, AddressOf btnOpenWorkflow_Click)
            Dim itemReassign As New ToolStripMenuItem("👤 Reassign Staff", Nothing, Sub(s, e) PromptReassignTask())
            Dim itemPriority As New ToolStripMenuItem("⚡ Change Priority", Nothing, Sub(s, e) PromptChangePriority())
            Dim itemComplete As New ToolStripMenuItem(If(isEmployee, "✓ Submit for Review", "✓ Mark as Complete"), Nothing, AddressOf btnMarkComplete_Click)
            Dim itemRestore As New ToolStripMenuItem("↩️ Restore Task", Nothing, AddressOf btnRestoreTask_Click)
            itemRestore.Visible = Not isEmployee
            Dim itemHardDelete As New ToolStripMenuItem("⚠️ Permanently Delete", Nothing, AddressOf btnHardDeleteTask_Click)
            itemHardDelete.Visible = Not isEmployee

            ctxRowMenu.Items.Add(itemAccept)
            ctxRowMenu.Items.Add(itemEdit)
            ctxRowMenu.Items.Add(itemDelete)
            ctxRowMenu.Items.Add(New ToolStripSeparator())
            ctxRowMenu.Items.Add(itemOpen)
            ctxRowMenu.Items.Add(itemReassign)
            ctxRowMenu.Items.Add(itemPriority)
            ctxRowMenu.Items.Add(New ToolStripSeparator())
            ctxRowMenu.Items.Add(itemComplete)
            ctxRowMenu.Items.Add(New ToolStripSeparator())
            ctxRowMenu.Items.Add(itemRestore)
            ctxRowMenu.Items.Add(itemHardDelete)
        End Sub

        Public Async Function PreloadDataAsync(navigationToken As Long) As Task Implements IPreloadableScreen.PreloadDataAsync
            _activeNavigationToken = navigationToken
            
            ' Explicit Cache Freshness Policy: Reuse cached dataset if data version hasn't changed and no data error exists
            If _isDataLoaded AndAlso _localTasksDataVersion = Forms.Common.DataStateTracker.TasksDataVersion AndAlso Not _hasDataLoadError Then
                _appLogger.LogInfo($"[TaskCache] Reusing cached dataset for navigation token #{navigationToken} (Version: {_localTasksDataVersion}). Skipping SQL query.")
                ApplyTabAndFilterLogic(0)
                Return
            End If


            _isDataLoaded = True
            Await LoadInitialDataInternalAsync(navigationToken)
        End Function

        Public Async Function LoadInitialDataAsync() As System.Threading.Tasks.Task
            Await LoadInitialDataInternalAsync(0)
        End Function

        Private Async Function LoadInitialDataInternalAsync(token As Long) As System.Threading.Tasks.Task
            If _isDataLoading Then Return
            _isDataLoading = True
            _hasDataLoadError = False
            _dataLoadErrorRefId = String.Empty

            _dbFetchRequestCounter += 1
            Dim currentDbReqId As Long = _dbFetchRequestCounter

            _appLogger.LogInfo($"[DbFetch Req #{currentDbReqId}] Starting SQL task fetch (Token #{token})")

            Dim swTotal As System.Diagnostics.Stopwatch = System.Diagnostics.Stopwatch.StartNew()
            Dim swSql As System.Diagnostics.Stopwatch = System.Diagnostics.Stopwatch.StartNew()
            Dim fetchSuccess As Boolean = False

            ' Phase 1: SQL Data Fetching (Failures set _hasDataLoadError = True)
            Try
                Me.Cursor = Cursors.WaitCursor
                Dim currentRole As UserRole = If(CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser IsNot Nothing, CurrentUserContext.CurrentUser.Role, UserRole.Employee)
                Dim includeDeleted As Boolean = (currentRole = UserRole.Admin OrElse currentRole = UserRole.Owner)
                _allTasksList = Await _taskService.GetAllTasksAsync(includeDeleted)
                swSql.Stop()
                _localTasksDataVersion = Forms.Common.DataStateTracker.TasksDataVersion
                fetchSuccess = True

                Dim taskCount As Integer = If(_allTasksList IsNot Nothing, _allTasksList.Count, 0)
                _appLogger.LogInfo($"[DbFetch Req #{currentDbReqId}] SQL fetch completed successfully. Fetched {taskCount} tasks.")
            Catch exDb As Exception
                _hasDataLoadError = True
                _dataLoadErrorRefId = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()
                _appLogger.LogError($"[TaskDataError Req #{currentDbReqId}] SQL fetch failure (Ref ID: {_dataLoadErrorRefId}): {exDb.GetType().FullName} - {exDb.Message}", "DailyTasksControl", exDb)
                If _allTasksList Is Nothing Then _allTasksList = New List(Of TaskDto)()
                If _filteredTasksList Is Nothing Then _filteredTasksList = New List(Of TaskDto)()
                RenderWorkQueueRows()
                Return
            Finally
                If Not fetchSuccess Then
                    _isDataLoading = False
                    Me.Cursor = Cursors.Default
                End If
            End Try

            ' Phase 2: UI Binding & Rendering (Failures logged as [TaskUiError], NEVER as database error)
            Try
                If token <> 0 AndAlso token <> _activeNavigationToken Then
                    _appLogger.LogInfo($"[DbFetch Req #{currentDbReqId}] Token #{token} superseded by active token #{_activeNavigationToken}. Discarding stale UI binding.")
                    Return
                End If

                _isInitializingFilters = True
                Try
                    Dim users = Await _userRepo.GetAllAsync()
                    cboStaffFilter.Items.Clear()
                    cboStaffFilter.Items.Add(New FilterOption(Of String)("All Staff", "all"))
                    If users IsNot Nothing Then
                        For Each u In users
                            If u.IsActive Then cboStaffFilter.Items.Add(New FilterOption(Of String)(u.FullName, u.FullName))
                        Next
                    End If
                    If cboStaffFilter.Items.Count > 0 Then cboStaffFilter.SelectedIndex = 0
                Catch exUser As Exception
                    _appLogger.LogWarn($"Failed to load staff list for dropdown filter: {exUser.Message}")
                Finally
                    _isInitializingFilters = False
                End Try

                ApplyTabAndFilterLogic(0)
                swTotal.Stop()

                Dim currentProc = System.Diagnostics.Process.GetCurrentProcess()
                Dim memUsageMb As Double = Math.Round(currentProc.WorkingSet64 / 1024.0 / 1024.0, 2)
                Dim taskCountVal As Integer = If(_allTasksList IsNot Nothing, _allTasksList.Count, 0)
                _appLogger.LogInfo($"[TaskPerf Req #{currentDbReqId}] Task count: {taskCountVal} | SQL duration: {swSql.ElapsedMilliseconds}ms | First-render duration: {swTotal.ElapsedMilliseconds}ms | Memory: {memUsageMb} MB")

            Catch exUi As Exception
                _appLogger.LogError($"[TaskUiError Req #{currentDbReqId}] UI rendering exception: {exUi.GetType().FullName} - {exUi.Message}", "DailyTasksControl", exUi)
            Finally
                _isDataLoading = False
                Me.Cursor = Cursors.Default
            End Try
        End Function

        Private Sub OnSearchTextChanged(sender As Object, e As EventArgs)
            If _isInitializingFilters OrElse _isClearingFilters OrElse Me.IsDisposed OrElse Me.Disposing Then Return
            _searchDebounceTimer.Stop()
            _searchDebounceTimer.Start()
        End Sub

        Private Sub OnSearchDebounceTimer_Tick(sender As Object, e As EventArgs)
            _searchDebounceTimer.Stop()
            If _isInitializingFilters OrElse _isClearingFilters OrElse Me.IsDisposed OrElse Me.Disposing Then Return
            OnFilterChanged(sender, e)
        End Sub

        Private Sub OnFilterChanged(sender As Object, e As EventArgs)
            If _isInitializingFilters OrElse _isClearingFilters OrElse Me.IsDisposed OrElse Me.Disposing Then Return

            _filterVersionSequence += 1
            Dim currentVersion = _filterVersionSequence

            If btnClearFilters IsNot Nothing Then
                Dim hasActiveFilters As Boolean = Not String.IsNullOrWhiteSpace(txtSearch.Text) OrElse
                                                  (cboStatusFilter IsNot Nothing AndAlso cboStatusFilter.SelectedIndex > 0) OrElse
                                                  (cboStaffFilter IsNot Nothing AndAlso cboStaffFilter.SelectedIndex > 0) OrElse
                                                  (cboPriorityFilter IsNot Nothing AndAlso cboPriorityFilter.SelectedIndex > 0) OrElse
                                                  (cboTaskTypeFilter IsNot Nothing AndAlso cboTaskTypeFilter.SelectedIndex > 0) OrElse
                                                  (cboDueDateFilter IsNot Nothing AndAlso cboDueDateFilter.SelectedIndex > 0)
                btnClearFilters.Enabled = hasActiveFilters
            End If

            Dim statusStr = If(cboStatusFilter IsNot Nothing AndAlso cboStatusFilter.SelectedItem IsNot Nothing, cboStatusFilter.SelectedItem.ToString(), "All")
            Dim staffStr = If(cboStaffFilter IsNot Nothing AndAlso cboStaffFilter.SelectedItem IsNot Nothing, cboStaffFilter.SelectedItem.ToString(), "All")
            Dim prioStr = If(cboPriorityFilter IsNot Nothing AndAlso cboPriorityFilter.SelectedItem IsNot Nothing, cboPriorityFilter.SelectedItem.ToString(), "All")
            Dim typeStr = If(cboTaskTypeFilter IsNot Nothing AndAlso cboTaskTypeFilter.SelectedItem IsNot Nothing, cboTaskTypeFilter.SelectedItem.ToString(), "All")
            Dim dueStr = If(cboDueDateFilter IsNot Nothing AndAlso cboDueDateFilter.SelectedItem IsNot Nothing, cboDueDateFilter.SelectedItem.ToString(), "All")
            Dim searchStr = If(txtSearch IsNot Nothing, txtSearch.Text, "")

            _appLogger.LogInfo($"[FilterExec Req #{currentVersion}] Request started")
            _appLogger.LogInfo($"[FilterExec Req #{currentVersion}] Filter values: Status='{statusStr}', Staff='{staffStr}', Priority='{prioStr}', TaskType='{typeStr}', DueDate='{dueStr}', Search='{searchStr}'")

            Try
                ApplyTabAndFilterLogic(currentVersion)
            Catch ex As Exception
                _appLogger.LogError($"[FilterExec Req #{currentVersion}] Exception during filter execution: {ex.GetType().FullName} - {ex.Message}", "DailyTasksControl", ex)
            End Try
        End Sub

        Private Sub btnClearFilters_Click(sender As Object, e As EventArgs)
            If _isInitializingFilters OrElse _isClearingFilters Then Return

            If _hasDataLoadError Then
                _appLogger.LogInfo("[TaskError] Retry Loading initiated - executing actual database fetch")
                Dim taskUnused = LoadInitialDataInternalAsync(0)
                Return
            End If

            _searchDebounceTimer.Stop()
            _isClearingFilters = True
            _appLogger.LogInfo($"[ClearAll] Reset sequence started - events suppressed")

            Try
                txtSearch.Text = String.Empty
                cboStatusFilter.SelectedIndex = 0
                cboStaffFilter.SelectedIndex = 0
                cboPriorityFilter.SelectedIndex = 0
                cboTaskTypeFilter.SelectedIndex = 0
                cboDueDateFilter.SelectedIndex = 0
                If btnClearFilters IsNot Nothing Then btnClearFilters.Enabled = False
            Finally
                _isClearingFilters = False
            End Try

            _filterVersionSequence += 1
            Dim finalVersion = _filterVersionSequence
            _appLogger.LogInfo($"[ClearAll Req #{finalVersion}] Reset completed. Executing exactly ONE filter pass against authoritative cached dataset.")
            ApplyTabAndFilterLogic(finalVersion)
            _appLogger.LogInfo($"[ClearAll Req #{finalVersion}] Completed successfully.")
        End Sub

        Private Sub OpenTaskWorkflowScreen(task As TaskDto, Optional entryPoint As String = "ContextMenu/Button")
            Dim tId As Integer = If(task IsNot Nothing, task.TaskId, 0)
            If tId <= 0 Then Return

            Dim traceMsg As String = $"[WorkflowOpenTrace]{Environment.NewLine}" &
                                     $"TaskId={tId}{Environment.NewLine}" &
                                     $"EntryPoint={entryPoint}{Environment.NewLine}" &
                                     $"BeforeOpenTaskWorkflowScreen=True{Environment.NewLine}" &
                                     $"BeforeFrmTaskWorkspaceConstructor=True"
            _appLogger.LogInfo(traceMsg, "DailyTasksControl")

            Try
                Dim dlg As FrmTaskWorkspace = Nothing
                Try
                    dlg = New FrmTaskWorkspace(tId, _mainShellHost)
                Catch exCtor As Exception
                    Dim errCtorTrace As String = $"[WorkflowOpenTrace]{Environment.NewLine}" &
                                                 $"TaskId={tId}{Environment.NewLine}" &
                                                 $"EntryPoint={entryPoint}{Environment.NewLine}" &
                                                 $"BeforeOpenTaskWorkflowScreen=True{Environment.NewLine}" &
                                                 $"BeforeFrmTaskWorkspaceConstructor=True{Environment.NewLine}" &
                                                 $"FrmTaskWorkspaceConstructed=False{Environment.NewLine}" &
                                                 $"ExceptionType={exCtor.GetType().FullName}{Environment.NewLine}" &
                                                 $"ExceptionMessage={exCtor.Message}{Environment.NewLine}" &
                                                 $"StackTrace={exCtor.StackTrace}"
                    _appLogger.LogError(errCtorTrace, "DailyTasksControl", exCtor)
                    Throw
                End Try

                _appLogger.LogInfo($"[WorkflowOpenTrace] TaskId={tId} | FrmTaskWorkspaceConstructed=True | BeforeShowDialog=True", "DailyTasksControl")

                Using dlg
                    dlg.ShowDialog(Me.FindForm())
                End Using

                _appLogger.LogInfo($"[WorkflowOpenTrace] TaskId={tId} | ShowDialogReturned=True | ExceptionType=None | ExceptionMessage=None", "DailyTasksControl")
            Catch ex As Exception
                Dim errTrace As String = $"[WorkflowOpenTrace]{Environment.NewLine}" &
                                         $"TaskId={tId}{Environment.NewLine}" &
                                         $"EntryPoint={entryPoint}{Environment.NewLine}" &
                                         $"ExceptionType={ex.GetType().FullName}{Environment.NewLine}" &
                                         $"ExceptionMessage={ex.Message}{Environment.NewLine}" &
                                         $"StackTrace={ex.StackTrace}"
                _appLogger.LogError(errTrace, "DailyTasksControl", ex)
                Throw
            End Try
        End Sub

        Private Sub SwitchActiveTab(tabKey As String)
            _activeTab = tabKey
            btnTabNeedsAttention.Font = New Font(ThemeConstants.FontNameDefault, 9.0!, If(tabKey = "NeedsAttention", FontStyle.Bold, FontStyle.Regular))
            btnTabNeedsAttention.ForeColor = If(tabKey = "NeedsAttention", ThemeConstants.PrimaryAccent, ThemeConstants.TextSecondary)

            btnTabMyTasks.Font = New Font(ThemeConstants.FontNameDefault, 9.0!, If(tabKey = "MyTasks", FontStyle.Bold, FontStyle.Regular))
            btnTabMyTasks.ForeColor = If(tabKey = "MyTasks", ThemeConstants.PrimaryAccent, ThemeConstants.TextSecondary)

            btnTabAllActive.Font = New Font(ThemeConstants.FontNameDefault, 9.0!, If(tabKey = "AllActive", FontStyle.Bold, FontStyle.Regular))
            btnTabAllActive.ForeColor = If(tabKey = "AllActive", ThemeConstants.PrimaryAccent, ThemeConstants.TextSecondary)

            btnTabCompleted.Font = New Font(ThemeConstants.FontNameDefault, 9.0!, If(tabKey = "Completed", FontStyle.Bold, FontStyle.Regular))
            btnTabCompleted.ForeColor = If(tabKey = "Completed", ThemeConstants.PrimaryAccent, ThemeConstants.TextSecondary)

            If btnTabDeletedTasks IsNot Nothing Then
                btnTabDeletedTasks.Font = New Font(ThemeConstants.FontNameDefault, 9.0!, If(tabKey = "DeletedTasks", FontStyle.Bold, FontStyle.Regular))
                btnTabDeletedTasks.ForeColor = If(tabKey = "DeletedTasks", ThemeConstants.PrimaryAccent, ThemeConstants.TextSecondary)
            End If

            _filterVersionSequence += 1
            ApplyTabAndFilterLogic(_filterVersionSequence)
        End Sub

        Private Sub ApplyTabAndFilterLogic(Optional requestVersion As Long = 0)
            If _allTasksList Is Nothing Then Return
            If requestVersion > 0 AndAlso requestVersion <> _filterVersionSequence Then
                _appLogger.LogInfo($"[TaskFilter] Discarding stale filter result (Request Version {requestVersion} < Current Version {_filterVersionSequence})")
                Return
            End If

            Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)
            Dim searchText = If(txtSearch IsNot Nothing, txtSearch.Text.Trim().ToLower(), String.Empty)

            ' Extract strongly-typed filter options
            Dim statusOpt = TryCast(cboStatusFilter.SelectedItem, FilterOption(Of StatusFilterKind))
            Dim selectedStatusKind = If(statusOpt IsNot Nothing, statusOpt.Value, StatusFilterKind.AllStatus)

            Dim priorityOpt = TryCast(cboPriorityFilter.SelectedItem, FilterOption(Of Nullable(Of TaskPriority)))
            Dim selectedPriorityVal = If(priorityOpt IsNot Nothing, priorityOpt.Value, CType(Nothing, Nullable(Of TaskPriority)))

            Dim dueDateOpt = TryCast(cboDueDateFilter.SelectedItem, FilterOption(Of String))
            Dim selectedDueDateVal = If(dueDateOpt IsNot Nothing, dueDateOpt.Value, "all")

            Dim taskTypeOpt = TryCast(cboTaskTypeFilter.SelectedItem, FilterOption(Of String))
            Dim selectedTaskTypeVal = If(taskTypeOpt IsNot Nothing, taskTypeOpt.Value, "all")

            Dim staffOpt = TryCast(cboStaffFilter.SelectedItem, FilterOption(Of String))
            Dim selectedStaffVal = If(staffOpt IsNot Nothing, staffOpt.Value, "all")

            Dim today = DateTime.Today

            _filteredTasksList = _allTasksList.FindAll(Function(t)
                                                           ' 1. TAB ISOLATION RULE
                                                           If _activeTab = "DeletedTasks" Then
                                                               If Not t.IsDeleted Then Return False
                                                           Else
                                                               If t.IsDeleted Then Return False
                                                               
                                                               If _activeTab = "Completed" Then
                                                               If t.WorkflowState <> TaskWorkflowState.Completed AndAlso t.WorkflowState <> TaskWorkflowState.Delivered AndAlso t.WorkflowState <> TaskWorkflowState.Closed Then
                                                                   Return False
                                                               End If

                                                               If _activePresetFilter = "completedtoday" OrElse _activePresetFilter = "completed_today" Then
                                                                   If Not (t.TargetDueDate.Date = today OrElse t.AssignmentDate.Date = today) Then
                                                                       Return False
                                                                   End If
                                                               End If
                                                           Else
                                                               If t.WorkflowState = TaskWorkflowState.Completed OrElse t.WorkflowState = TaskWorkflowState.Delivered OrElse t.WorkflowState = TaskWorkflowState.Closed OrElse t.WorkflowState = TaskWorkflowState.Cancelled Then
                                                                   Return False
                                                               End If

                                                               If _activeTab = "MyTasks" AndAlso t.AssignedToUserId <> currentUserId Then
                                                                   Return False
                                                               End If

                                                               If _activePresetFilter = "duesoon" OrElse _activePresetFilter = "due_soon" OrElse _activePresetFilter = "due48h" Then
                                                                   If Not (t.TargetDueDate.Date >= today AndAlso t.TargetDueDate.Date <= today.AddDays(2)) Then
                                                                       Return False
                                                                   End If
                                                               ElseIf _activePresetFilter = "overdue" OrElse selectedDueDateVal = "overdue" Then
                                                                   If Not (t.IsOverdue OrElse t.TargetDueDate.Date < today) Then
                                                                       Return False
                                                                   End If
                                                               End If
                                                            End If
                                                           End If

                                                           ' 2. SEARCH MATCHING
                                                           If Not String.IsNullOrEmpty(searchText) Then
                                                               Dim matchName = (Not String.IsNullOrEmpty(t.Title) AndAlso t.Title.ToLower().Contains(searchText))
                                                               Dim matchClient = (Not String.IsNullOrEmpty(t.ClientName) AndAlso t.ClientName.ToLower().Contains(searchText))
                                                               Dim matchCode = (Not String.IsNullOrEmpty(t.TaskCode) AndAlso t.TaskCode.ToLower().Contains(searchText))
                                                               If Not (matchName OrElse matchClient OrElse matchCode) Then Return False
                                                           End If

                                                           ' 3. STRONGLY TYPED FILTER MATCHING
                                                           ' Status Filter (Explicit domain mapping: Pending = NewTask OR Assigned)
                                                           Select Case selectedStatusKind
                                                               Case StatusFilterKind.Pending
                                                                   If t.WorkflowState <> TaskWorkflowState.NewTask AndAlso t.WorkflowState <> TaskWorkflowState.Assigned Then Return False
                                                               Case StatusFilterKind.InProgress
                                                                   If t.WorkflowState <> TaskWorkflowState.InProgress Then Return False
                                                               Case StatusFilterKind.WaitingForClient
                                                                   If t.WorkflowState <> TaskWorkflowState.WaitingForClient Then Return False
                                                               Case StatusFilterKind.WaitingForDocuments
                                                                   If t.WorkflowState <> TaskWorkflowState.WaitingForDocuments Then Return False
                                                               Case StatusFilterKind.UnderReview
                                                                   If t.WorkflowState <> TaskWorkflowState.UnderReview Then Return False
                                                               Case StatusFilterKind.OnHold
                                                                   If t.WorkflowState <> TaskWorkflowState.OnHold Then Return False
                                                               Case StatusFilterKind.Completed
                                                                   If t.WorkflowState <> TaskWorkflowState.Completed Then Return False
                                                               Case StatusFilterKind.AllStatus
                                                                   ' No status filter restriction
                                                           End Select

                                                           ' Priority Filter
                                                           If selectedPriorityVal.HasValue AndAlso t.Priority <> selectedPriorityVal.Value Then
                                                               Return False
                                                           End If

                                                           ' Staff Filter
                                                           If selectedStaffVal <> "all" AndAlso Not String.Equals(t.AssignedToName, selectedStaffVal, StringComparison.OrdinalIgnoreCase) Then
                                                               Return False
                                                           End If

                                                           ' Task Type Filter
                                                           If selectedTaskTypeVal <> "all" AndAlso Not String.Equals(t.TaskType, selectedTaskTypeVal, StringComparison.OrdinalIgnoreCase) AndAlso Not String.Equals(t.Department.ToString(), selectedTaskTypeVal, StringComparison.OrdinalIgnoreCase) Then
                                                               Return False
                                                           End If

                                                           ' Due Date Filter
                                                           If selectedDueDateVal <> "all" Then
                                                               If selectedDueDateVal = "overdue" AndAlso Not (t.IsOverdue OrElse t.TargetDueDate.Date < today) Then
                                                                   Return False
                                                               ElseIf selectedDueDateVal = "duetoday" AndAlso t.TargetDueDate.Date <> today Then
                                                                   Return False
                                                               ElseIf selectedDueDateVal = "dueweek" AndAlso Not (t.TargetDueDate.Date >= today AndAlso t.TargetDueDate.Date <= today.AddDays(7)) Then
                                                                   Return False
                                                               ElseIf selectedDueDateVal = "duemonth" AndAlso Not (t.TargetDueDate.Date.Month = today.Month AndAlso t.TargetDueDate.Date.Year = today.Year) Then
                                                                   Return False
                                                               End If
                                                           End If

                                                           Return True
                                                       End Function)

            ' Update Tab Badge Counts
            Dim needsAttentionCount = _allTasksList.Where(Function(t) Not t.IsDeleted AndAlso t.WorkflowState <> TaskWorkflowState.Completed AndAlso t.WorkflowState <> TaskWorkflowState.Closed AndAlso t.WorkflowState <> TaskWorkflowState.Cancelled).Count()
            Dim myTasksCount = _allTasksList.Where(Function(t) Not t.IsDeleted AndAlso t.AssignedToUserId = currentUserId AndAlso t.WorkflowState <> TaskWorkflowState.Completed AndAlso t.WorkflowState <> TaskWorkflowState.Closed).Count()
            Dim allActiveCount = needsAttentionCount
            Dim completedCount = _allTasksList.Where(Function(t) Not t.IsDeleted AndAlso (t.WorkflowState = TaskWorkflowState.Completed OrElse t.WorkflowState = TaskWorkflowState.Closed)).Count()
            Dim deletedCount = _allTasksList.Where(Function(t) t.IsDeleted).Count()

            btnTabNeedsAttention.Text = $"Needs Attention ({needsAttentionCount})"
            btnTabMyTasks.Text = $"My Tasks ({myTasksCount})"
            btnTabAllActive.Text = $"All Active ({allActiveCount})"
            btnTabCompleted.Text = $"Completed ({completedCount})"
            If btnTabDeletedTasks IsNot Nothing Then btnTabDeletedTasks.Text = $"Deleted Tasks ({deletedCount})"

            lblSubtitle.Text = $"My work queue  •  {_filteredTasksList.Count} active tasks displayed"

            _appLogger.LogInfo($"[FilterExec Req #{requestVersion}] Execution finished successfully. Result count: {_filteredTasksList.Count}")

            ' Render Work Queue Items into Urgency Groups
            RenderWorkQueueRows()
        End Sub

        Private Sub CenterEmptyStateControls()
            If pnlEmptyStateHost Is Nothing Then Return
            Dim centerX As Integer = pnlEmptyStateHost.Width \ 2
            Dim startY As Integer = Math.Max(20, (pnlEmptyStateHost.Height \ 2) - 75)

            If lblEmptyStateIcon IsNot Nothing Then
                lblEmptyStateIcon.Location = New Point(centerX - (lblEmptyStateIcon.Width \ 2), startY)
            End If
            If lblEmptyStateTitle IsNot Nothing Then
                lblEmptyStateTitle.Location = New Point(centerX - (lblEmptyStateTitle.Width \ 2), startY + 46)
            End If
            If lblEmptyStateSubtitle IsNot Nothing Then
                lblEmptyStateSubtitle.Location = New Point(centerX - (lblEmptyStateSubtitle.Width \ 2), startY + 74)
            End If
            If btnEmptyStateClearFilters IsNot Nothing Then
                btnEmptyStateClearFilters.Location = New Point(centerX - (btnEmptyStateClearFilters.Width \ 2), startY + 106)
            End If
        End Sub

        Private Sub RenderWorkQueueRows()
            Try
                pnlQueueContentScroll.SuspendLayout()
                pnlOverdueList.SuspendLayout()
                pnlDueTodayList.SuspendLayout()
                pnlUpcomingList.SuspendLayout()

                pnlOverdueList.Controls.Clear()
                pnlDueTodayList.Controls.Clear()
                pnlUpcomingList.Controls.Clear()

                Dim overdueTasks = _filteredTasksList.FindAll(Function(t) t.IsOverdue OrElse t.TargetDueDate.Date < DateTime.Today)
                Dim dueTodayTasks = _filteredTasksList.FindAll(Function(t) Not overdueTasks.Contains(t) AndAlso t.TargetDueDate.Date = DateTime.Today)
                Dim upcomingTasks = _filteredTasksList.FindAll(Function(t) Not overdueTasks.Contains(t) AndAlso Not dueTodayTasks.Contains(t))

                lblOverdueHeader.Text = $"🔴 OVERDUE ({overdueTasks.Count})"
                lblDueTodayHeader.Text = $"🟠 DUE TODAY ({dueTodayTasks.Count})"
                lblUpcomingHeader.Text = $"🟡 UPCOMING / WORK QUEUE ({upcomingTasks.Count})"

                pnlOverdueGroup.Visible = (overdueTasks.Count > 0)
                pnlDueTodayGroup.Visible = (dueTodayTasks.Count > 0)
                pnlUpcomingGroup.Visible = (upcomingTasks.Count > 0 OrElse (_filteredTasksList.Count > 0 AndAlso overdueTasks.Count = 0 AndAlso dueTodayTasks.Count = 0))

                ' Add rows top-to-bottom
                For Each t In overdueTasks
                    pnlOverdueList.Controls.Add(CreateTaskRowCard(t, "OVERDUE"))
                Next
                For Each t In dueTodayTasks
                    pnlDueTodayList.Controls.Add(CreateTaskRowCard(t, "DUETODAY"))
                Next
                For Each t In upcomingTasks
                    pnlUpcomingList.Controls.Add(CreateTaskRowCard(t, "UPCOMING"))
                Next

                Dim hasActiveFilters As Boolean = Not String.IsNullOrWhiteSpace(txtSearch.Text) OrElse
                                                  (cboStatusFilter IsNot Nothing AndAlso cboStatusFilter.SelectedIndex > 0) OrElse
                                                  (cboStaffFilter IsNot Nothing AndAlso cboStaffFilter.SelectedIndex > 0) OrElse
                                                  (cboPriorityFilter IsNot Nothing AndAlso cboPriorityFilter.SelectedIndex > 0) OrElse
                                                  (cboTaskTypeFilter IsNot Nothing AndAlso cboTaskTypeFilter.SelectedIndex > 0) OrElse
                                                  (cboDueDateFilter IsNot Nothing AndAlso cboDueDateFilter.SelectedIndex > 0) OrElse
                                                  (_activeTab = "Completed" OrElse _activeTab = "MyTasks")

                If _hasDataLoadError Then
                    pnlEmptyStateHost.Visible = True
                    pnlEmptyStateHost.BringToFront()

                    lblEmptyStateIcon.Text = "⚠️"
                    lblEmptyStateTitle.Text = "Unable to load tasks"
                    lblEmptyStateSubtitle.Text = $"We couldn't retrieve task data right now. Please check your connection and try again. (Ref: {_dataLoadErrorRefId})"
                    btnEmptyStateClearFilters.Text = "🔄 Retry Loading"
                    btnEmptyStateClearFilters.Visible = True

                    CenterEmptyStateControls()
                    ClearTaskDetailsPanel()
                    Return
                End If

                If _filteredTasksList.Count = 0 Then
                    pnlEmptyStateHost.Visible = True
                    pnlEmptyStateHost.BringToFront()

                    btnEmptyStateClearFilters.Text = "Clear Filters"
                    If hasActiveFilters Then
                        lblEmptyStateIcon.Text = "🔍"
                        lblEmptyStateTitle.Text = "No tasks match your filters"
                        lblEmptyStateSubtitle.Text = "Try changing or clearing your filters."
                        btnEmptyStateClearFilters.Visible = True
                    Else
                        lblEmptyStateIcon.Text = "📋"
                        lblEmptyStateTitle.Text = "No tasks need attention right now"
                        Dim totalCompletedCount As Integer = If(_allTasksList IsNot Nothing, _allTasksList.FindAll(Function(t) t.WorkflowState = TaskWorkflowState.Completed OrElse t.WorkflowState = TaskWorkflowState.Closed).Count, 0)
                        If totalCompletedCount > 0 Then
                            lblEmptyStateSubtitle.Text = $"You have {totalCompletedCount} completed tasks. Switch to the Completed tab to view them."
                        Else
                            lblEmptyStateSubtitle.Text = "There are currently no active tasks in your workspace."
                        End If
                        btnEmptyStateClearFilters.Visible = False
                    End If

                    CenterEmptyStateControls()
                    ClearTaskDetailsPanel()
                Else
                    pnlEmptyStateHost.Visible = False
                    Dim targetTask = If(_selectedTask IsNot Nothing AndAlso _filteredTasksList.Exists(Function(t) t.TaskId = _selectedTask.TaskId),
                                        _filteredTasksList.First(Function(t) t.TaskId = _selectedTask.TaskId),
                                        _filteredTasksList(0))
                    SelectTaskRow(targetTask)
                End If

                lblPaginationInfo.Text = $"Showing 1 to {_filteredTasksList.Count} of {_filteredTasksList.Count} tasks"
            Finally
                pnlUpcomingList.ResumeLayout(True)
                pnlDueTodayList.ResumeLayout(True)
                pnlOverdueList.ResumeLayout(True)
                pnlQueueContentScroll.ResumeLayout(True)
            End Try
        End Sub

        Private Function CreateTaskRowCard(task As TaskDto, urgencyCategory As String) As Panel
            Dim isInitiallySelected As Boolean = (_selectedTask IsNot Nothing AndAlso task.TaskId = _selectedTask.TaskId)

            Dim pnlRow As New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 54,
                .BackColor = If(isInitiallySelected, Color.FromArgb(238, 242, 255), Color.White),
                .Cursor = Cursors.Hand,
                .Tag = task
            }

            pnlRow.SuspendLayout()

            AddHandler pnlRow.Paint, Sub(s, e)
                                         e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
                                         ' Draw bottom row divider line
                                         Using p As New Pen(Color.FromArgb(241, 245, 249), 1)
                                             e.Graphics.DrawLine(p, 0, pnlRow.Height - 1, pnlRow.Width, pnlRow.Height - 1)
                                         End Using

                                         ' Visual Row Selection Indicator: Draw active 5px left indigo accent bar
                                         If _selectedTask IsNot Nothing AndAlso task IsNot Nothing AndAlso task.TaskId = _selectedTask.TaskId Then
                                             Using b As New SolidBrush(Color.FromArgb(79, 70, 229))
                                                 e.Graphics.FillRectangle(b, 0, 0, 5, pnlRow.Height - 1)
                                             End Using
                                         End If
                                     End Sub

            ' Task Title & Subtitle
            Dim lblTitle As New Label() With {
                .Text = task.Title,
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(12, 8),
                .Size = New Size(220, 18),
                .Cursor = Cursors.Hand
            }
            Dim lblClient As New Label() With {
                .Text = $"{task.ClientName}  ({task.TaskCode})",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.0!, FontStyle.Regular),
                .ForeColor = ThemeConstants.TextSecondary,
                .Location = New Point(12, 28),
                .Size = New Size(220, 16),
                .Cursor = Cursors.Hand
            }

            ' Due Date Tag
            Dim dueText As String = If(task.IsOverdue, $"Overdue by {task.DaysOverdue} days", $"Due Today ({task.TargetDueDate.ToString("dd MMM")})")
            Dim dueColor As Color = If(task.IsOverdue, Color.FromArgb(220, 38, 38), Color.FromArgb(217, 119, 6))

            Dim lblDue As New Label() With {
                .Text = dueText,
                .Font = New Font(ThemeConstants.FontNameDefault, 8.0!, FontStyle.Bold),
                .ForeColor = dueColor,
                .Location = New Point(240, 16),
                .Size = New Size(130, 20),
                .Cursor = Cursors.Hand
            }

            ' Priority Badge
            Dim pBg As Color = Color.FromArgb(241, 245, 249)
            Dim pFg As Color = Color.FromArgb(71, 85, 105)
            If task.Priority = TaskPriority.Urgent OrElse task.Priority = TaskPriority.High Then
                pBg = Color.FromArgb(254, 226, 226)
                pFg = Color.FromArgb(220, 38, 38)
            ElseIf task.Priority = TaskPriority.Medium Then
                pBg = Color.FromArgb(254, 243, 199)
                pFg = Color.FromArgb(217, 119, 6)
            End If

            Dim lblPriority As New Label() With {
                .Text = task.Priority.ToString().ToUpper(),
                .Font = New Font(ThemeConstants.FontNameDefault, 7.5!, FontStyle.Bold),
                .ForeColor = pFg,
                .BackColor = pBg,
                .Size = New Size(64, 20),
                .Location = New Point(380, 16),
                .TextAlign = ContentAlignment.MiddleCenter,
                .Cursor = Cursors.Hand
            }

            ' Status Pill
            Dim lblStatus As New Label() With {
                .Text = task.WorkflowState.ToString(),
                .Font = New Font(ThemeConstants.FontNameDefault, 8.0!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(79, 70, 229),
                .BackColor = Color.FromArgb(238, 242, 255),
                .Size = New Size(95, 20),
                .Location = New Point(452, 16),
                .TextAlign = ContentAlignment.MiddleCenter,
                .Cursor = Cursors.Hand
            }

            ' Assignee Avatar Circle & Name
            Dim initials As String = GetInitials(task.AssignedToName)
            Dim lblAvatar As New Label() With {
                .Text = initials,
                .Font = New Font(ThemeConstants.FontNameDefault, 8.0!, FontStyle.Bold),
                .ForeColor = Color.White,
                .BackColor = Color.FromArgb(79, 70, 229),
                .Size = New Size(24, 24),
                .Location = New Point(556, 14),
                .TextAlign = ContentAlignment.MiddleCenter,
                .Cursor = Cursors.Hand
            }
            Dim lblAssignee As New Label() With {
                .Text = task.AssignedToName,
                .Font = New Font(ThemeConstants.FontNameDefault, 8.0!, FontStyle.Regular),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(584, 18),
                .Size = New Size(90, 18),
                .Cursor = Cursors.Hand
            }

            ' Three-dot menu button
            Dim btnMenu As New Label() With {
                .Text = "⋮",
                .Font = New Font(ThemeConstants.FontNameDefault, 12.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextSecondary,
                .Location = New Point(pnlRow.Width - 30, 12),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right,
                .Cursor = Cursors.Hand
            }
            AddHandler btnMenu.Click, Sub(s, e)
                                          SelectTaskRow(task)
                                          ctxRowMenu.Show(btnMenu, New Point(0, btnMenu.Height))
                                      End Sub

            pnlRow.Controls.Add(btnMenu)
            pnlRow.Controls.Add(lblAssignee)
            pnlRow.Controls.Add(lblAvatar)
            pnlRow.Controls.Add(lblStatus)
            pnlRow.Controls.Add(lblPriority)
            pnlRow.Controls.Add(lblDue)
            pnlRow.Controls.Add(lblClient)
            pnlRow.Controls.Add(lblTitle)

            pnlRow.ResumeLayout(False)


            ' Event Handlers for Row Selection & Double Click across ALL card child controls
            Dim clickHandler As EventHandler = Sub(s, e) SelectTaskRow(task)
            Dim dblClickHandler As EventHandler = Sub(s, e)
                                                        SelectTaskRow(task)
                                                        OpenTaskWorkflowScreen(task)
                                                    End Sub

            For Each c As Control In pnlRow.Controls
                If c IsNot btnMenu Then
                    AddHandler c.Click, clickHandler
                    AddHandler c.DoubleClick, dblClickHandler
                End If
            Next
            AddHandler pnlRow.Click, clickHandler
            AddHandler pnlRow.DoubleClick, dblClickHandler

            Return pnlRow
        End Function

        Private Function GetInitials(name As String) As String
            If String.IsNullOrWhiteSpace(name) Then Return "TS"
            Dim parts = name.Trim().Split(" "c)
            If parts.Length >= 2 Then
                Return (parts(0)(0) & parts(1)(0)).ToUpper()
            Else
                Return name.Substring(0, Math.Min(2, name.Length)).ToUpper()
            End If
        End Function

        Private Sub UpdateVisualRowHighlights()
            Dim groupPanels As Panel() = {pnlOverdueList, pnlDueTodayList, pnlUpcomingList}
            For Each grp In groupPanels
                If grp Is Nothing Then Continue For
                For Each ctrl As Control In grp.Controls
                    Dim pnlRow = TryCast(ctrl, Panel)
                    If pnlRow IsNot Nothing AndAlso pnlRow.Tag IsNot Nothing Then
                        Dim task = TryCast(pnlRow.Tag, TaskDto)
                        If task IsNot Nothing Then
                            Dim isSelected As Boolean = (_selectedTask IsNot Nothing AndAlso task.TaskId = _selectedTask.TaskId)
                            pnlRow.BackColor = If(isSelected, Color.FromArgb(238, 242, 255), Color.White)
                            pnlRow.Invalidate()
                        End If
                    End If
                Next
            Next
        End Sub

        Private Async Sub SelectTaskRow(task As TaskDto)
            _selectedTask = task
            If task Is Nothing Then
                ClearTaskDetailsPanel()
                Return
            End If

            ' Centralized Permission Evaluation via ITaskManagementService
            Dim currentUserId = 1
            Dim currentRole As UserRole = UserRole.Employee
            If CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser IsNot Nothing Then
                currentUserId = CurrentUserContext.CurrentUser.UserId
                currentRole = CurrentUserContext.CurrentUser.Role
            End If

            Dim canAccept As Boolean = _taskService.CanAcceptTask(task, currentUserId, currentRole)
            Dim canEdit As Boolean = _taskService.CanEditTask(task, currentRole)
            Dim canDelete As Boolean = _taskService.CanDeleteTask(task, currentRole)
            Dim canComplete As Boolean = _taskService.CanCompleteTask(task, currentUserId, currentRole)

            ' Mandatory Rule 4: Contextual Action Button Replacement (No 5th button cramming)
            If canAccept Then
                ' Assigned State + Assigned Staff: Slot 1 -> Accept Task; Slot 2 -> Open Workflow
                If btnAcceptTask IsNot Nothing Then
                    btnAcceptTask.Location = New Point(0, 4)
                    btnAcceptTask.Visible = True
                    btnAcceptTask.Enabled = True
                End If
                If btnOpenWorkflow IsNot Nothing Then
                    btnOpenWorkflow.Location = New Point(158, 4)
                    btnOpenWorkflow.Visible = True
                    btnOpenWorkflow.Enabled = True
                End If
                If btnEditTask IsNot Nothing Then btnEditTask.Visible = False
                If btnDeleteTask IsNot Nothing Then btnDeleteTask.Visible = False
                If btnMarkComplete IsNot Nothing Then btnMarkComplete.Visible = False
            ElseIf currentRole = UserRole.Employee Then
                ' InProgress / Active State + Assigned Staff: Slot 1 -> Open Workflow; Slot 2 -> Mark Complete
                If btnAcceptTask IsNot Nothing Then btnAcceptTask.Visible = False
                If btnOpenWorkflow IsNot Nothing Then
                    btnOpenWorkflow.Location = New Point(0, 4)
                    btnOpenWorkflow.Visible = True
                    btnOpenWorkflow.Enabled = True
                End If
                If btnMarkComplete IsNot Nothing Then
                    btnMarkComplete.Location = New Point(158, 4)
                    btnMarkComplete.Visible = canComplete
                    btnMarkComplete.Enabled = canComplete
                End If
                If btnEditTask IsNot Nothing Then btnEditTask.Visible = False
                If btnDeleteTask IsNot Nothing Then btnDeleteTask.Visible = False
            Else
                ' Admin / Owner Role
                If btnAcceptTask IsNot Nothing Then btnAcceptTask.Visible = False

                If task.WorkflowState = TaskWorkflowState.NewTask OrElse task.WorkflowState = TaskWorkflowState.Assigned Then
                    ' Un-accepted Task: Slot 1 -> Edit Task; Slot 2 -> Delete Task; Slot 3 -> Open Workflow
                    If btnEditTask IsNot Nothing Then
                        btnEditTask.Text = "✏ Edit Task"
                        btnEditTask.Location = New Point(0, 4)
                        btnEditTask.Visible = True
                        btnEditTask.Enabled = canEdit
                    End If
                    If btnDeleteTask IsNot Nothing Then
                        btnDeleteTask.Location = New Point(158, 4)
                        btnDeleteTask.Visible = True
                        btnDeleteTask.Enabled = canDelete
                    End If
                    If btnOpenWorkflow IsNot Nothing Then
                        btnOpenWorkflow.Text = "➔ Open Workflow"
                        btnOpenWorkflow.Location = New Point(0, 42)
                        btnOpenWorkflow.Visible = True
                        btnOpenWorkflow.Enabled = True
                    End If
                    If btnMarkComplete IsNot Nothing Then btnMarkComplete.Visible = False
                ElseIf task.WorkflowState = TaskWorkflowState.Completed OrElse task.WorkflowState = TaskWorkflowState.Delivered OrElse task.WorkflowState = TaskWorkflowState.Closed OrElse task.WorkflowState = TaskWorkflowState.Cancelled Then
                    ' Terminal State: Slot 1 -> Locked Indicator; Slot 2 -> Open Workflow
                    If btnEditTask IsNot Nothing Then
                        btnEditTask.Text = "🔒 Completed"
                        btnEditTask.Location = New Point(0, 4)
                        btnEditTask.Visible = True
                        btnEditTask.Enabled = False
                    End If
                    If btnOpenWorkflow IsNot Nothing Then
                        btnOpenWorkflow.Text = "➔ Open Workflow"
                        btnOpenWorkflow.Location = New Point(158, 4)
                        btnOpenWorkflow.Visible = True
                        btnOpenWorkflow.Enabled = True
                    End If
                    If btnDeleteTask IsNot Nothing Then btnDeleteTask.Visible = False
                    If btnMarkComplete IsNot Nothing Then btnMarkComplete.Visible = False
                Else
                    ' Active Working State (InProgress, etc.): Slot 1 -> Locked Indicator; Slot 2 -> Delete Task; Slot 3 -> Monitor Progress
                    If btnEditTask IsNot Nothing Then
                        btnEditTask.Text = "🔒 Task Locked"
                        btnEditTask.Location = New Point(0, 4)
                        btnEditTask.Visible = True
                        btnEditTask.Enabled = False
                    End If
                    If btnDeleteTask IsNot Nothing Then
                        btnDeleteTask.Location = New Point(158, 4)
                        btnDeleteTask.Visible = True
                        btnDeleteTask.Enabled = canDelete
                    End If
                    If btnOpenWorkflow IsNot Nothing Then
                        btnOpenWorkflow.Text = "➔ Monitor Progress"
                        btnOpenWorkflow.Location = New Point(0, 42)
                        btnOpenWorkflow.Visible = True
                        btnOpenWorkflow.Enabled = True
                    End If
                    If btnMarkComplete IsNot Nothing Then
                        btnMarkComplete.Location = New Point(158, 42)
                        btnMarkComplete.Visible = canComplete
                        btnMarkComplete.Enabled = canComplete
                    End If
                End If
            End If

            ' Synchronize Visual Row Selection Highlighting Across Cards
            UpdateVisualRowHighlights()

            ' Context Menu Item Enablement
            If ctxRowMenu IsNot Nothing AndAlso ctxRowMenu.Items.Count >= 12 Then
                Dim isDeletedTask = task.IsDeleted
                Dim isAdminOrOwner = (currentRole = UserRole.Admin OrElse currentRole = UserRole.Owner)
                
                ctxRowMenu.Items(0).Enabled = canAccept AndAlso Not isDeletedTask
                ctxRowMenu.Items(0).Visible = canAccept AndAlso Not isDeletedTask
                ctxRowMenu.Items(1).Enabled = canEdit AndAlso Not isDeletedTask
                ctxRowMenu.Items(1).Visible = Not isDeletedTask
                ctxRowMenu.Items(2).Enabled = canDelete AndAlso Not isDeletedTask
                ctxRowMenu.Items(2).Visible = Not isDeletedTask
                
                ctxRowMenu.Items(3).Visible = Not isDeletedTask
                ctxRowMenu.Items(4).Visible = Not isDeletedTask
                ctxRowMenu.Items(5).Visible = Not isDeletedTask
                ctxRowMenu.Items(6).Visible = Not isDeletedTask
                ctxRowMenu.Items(7).Visible = Not isDeletedTask
                ctxRowMenu.Items(8).Enabled = canComplete AndAlso Not isDeletedTask
                ctxRowMenu.Items(8).Visible = Not isDeletedTask
                
                ctxRowMenu.Items(9).Visible = isDeletedTask AndAlso isAdminOrOwner
                ctxRowMenu.Items(10).Visible = isDeletedTask AndAlso isAdminOrOwner
                ctxRowMenu.Items(11).Visible = isDeletedTask AndAlso isAdminOrOwner
            End If

            lblDetailTaskName.Text = task.Title
            lblDetailTaskType.Text = $"Category: {task.Department} Compliance"

            lblDetailPriorityBadge.Text = $"{task.Priority.ToString().ToUpper()} PRIORITY"
            If task.Priority = TaskPriority.Urgent OrElse task.Priority = TaskPriority.High Then
                lblDetailPriorityBadge.ForeColor = Color.FromArgb(220, 38, 38)
                lblDetailPriorityBadge.BackColor = Color.FromArgb(254, 226, 226)
            Else
                lblDetailPriorityBadge.ForeColor = Color.FromArgb(217, 119, 6)
                lblDetailPriorityBadge.BackColor = Color.FromArgb(254, 243, 199)
            End If

            If lblMetaClientVal IsNot Nothing Then lblMetaClientVal.Text = task.ClientName
            If lblMetaGstinVal IsNot Nothing Then lblMetaGstinVal.Text = $"{task.TaskCode} 🔗"
            If lblMetaDueDateVal IsNot Nothing Then lblMetaDueDateVal.Text = If(task.IsOverdue, $"Overdue by {task.DaysOverdue} days", task.TargetDueDate.ToString("dd MMM yyyy"))
            If lblMetaAssigneeVal IsNot Nothing Then lblMetaAssigneeVal.Text = task.AssignedToName
            If lblMetaStatusVal IsNot Nothing Then lblMetaStatusVal.Text = task.WorkflowState.ToString()
            If lblMetaCreatedOnVal IsNot Nothing Then lblMetaCreatedOnVal.Text = task.AssignmentDate.ToString("dd MMM yyyy")
            If lblMetaCreatedByVal IsNot Nothing Then lblMetaCreatedByVal.Text = If(Not String.IsNullOrEmpty(task.AssignedByName), task.AssignedByName, "Admin")

            ' Load Real Task Notes / Discussions from IDiscussionService
            Try
                Dim discussions = Await _discussionService.GetDiscussionsForTaskAsync(task.TaskId)
                If discussions IsNot Nothing AndAlso discussions.Count > 0 Then
                    Dim lastDisc = discussions.Last()
                    lblNotesHeader.Text = $"📝 NOTES ({discussions.Count})"
                    lblNotesContent.Text = lastDisc.DiscussionNotes
                    lblNotesAuthorDate.Text = $"— Logged on {lastDisc.DiscussionTimestamp.ToString("dd MMM yyyy hh:mm tt")}"
                Else
                    lblNotesHeader.Text = "📝 NOTES (0)"
                    lblNotesContent.Text = "No discussion notes logged for this task yet."
                    lblNotesAuthorDate.Text = "—"
                End If
            Catch ex As Exception
                lblNotesHeader.Text = "📝 NOTES (0)"
                lblNotesContent.Text = "No notes logged for this task."
                lblNotesAuthorDate.Text = "—"
            End Try

            ' Render Data-Backed Next Action Checklist
            Await RenderNextActionChecklistAsync(task)
        End Sub

        Private Async Function RenderNextActionChecklistAsync(task As TaskDto) As System.Threading.Tasks.Task
            pnlChecklistHost.Controls.Clear()
            If task Is Nothing Then Return

            ' Authoritative 3-tier task-type workflow template resolution (Canonical ID -> Display String -> Fallback Normalization)
            Dim resolutionTier As String = ""
            Dim tmpl = TaskWorkflowTemplateProvider.GetTemplateWithTier(task.TaskType, task.CategoryCode, task.Title, resolutionTier)

            If Not String.IsNullOrWhiteSpace(task.CategoryCode) AndAlso Not resolutionTier.StartsWith("Tier 1") Then
                _appLogger.LogWarn($"[WorkflowCanonicalMappingError] TaskId={task.TaskId} | CategoryCode='{task.CategoryCode}' exists but failed Tier 1 resolution! Fell back to '{resolutionTier}' (ResolvedTemplate='{tmpl.TaskType}')", "DailyTasksControl")
            End If

            Dim steps As New List(Of ChecklistStepItem)()

            If tmpl IsNot Nothing AndAlso tmpl.StandardSteps IsNot Nothing AndAlso tmpl.StandardSteps.Count > 0 Then
                For idx As Integer = 0 To tmpl.StandardSteps.Count - 1
                    steps.Add(New ChecklistStepItem With {
                        .StepText = $"{idx + 1}. {tmpl.StandardSteps(idx)}",
                        .IsCompleted = False,
                        .IsMandatory = True
                    })
                Next
            End If

            ' Fetch persisted activities from SQL Server database via ITaskActivityRepository
            Try
                Dim activities = Await _activityRepo.GetActivitiesByTaskAsync(task.TaskId)
                If activities IsNot Nothing Then
                    For Each act In activities
                        If Not String.IsNullOrEmpty(act.ActivityDescription) AndAlso act.ActivityDescription.StartsWith("[CHECKLIST_COMPLETE]") Then
                            Dim stepName = act.ActivityDescription.Replace("[CHECKLIST_COMPLETE]", "").Trim()
                            Dim matchingStep = steps.FirstOrDefault(Function(s) s.StepText.Trim() = stepName)
                            If matchingStep IsNot Nothing Then
                                matchingStep.IsCompleted = True
                            End If
                        End If
                    Next
                End If
            Catch ex As Exception
                _appLogger.LogError($"Checklist activity fetch error: {ex.Message}", "DailyTasksControl", ex)
            End Try

            _taskChecklistCache(task.TaskId) = steps

            ' Identify current active step index (first uncompleted step)
            Dim currentActiveIndex As Integer = steps.FindIndex(Function(s) Not s.IsCompleted)
            Dim completedCount As Integer = steps.FindAll(Function(s) s.IsCompleted).Count

            Dim renderedStepsStr As String = String.Join(" | ", steps.Select(Function(s) (If(s.IsCompleted, "[✓] ", "[ ] ")) & s.StepText))
            Dim traceMsg As String = $"[WorkflowTrace]" & vbCrLf &
                                     $"TaskId={task.TaskId}" & vbCrLf &
                                     $"SQL.Department={task.Department}" & vbCrLf &
                                     $"SQL.TaskType={task.TaskType}" & vbCrLf &
                                     $"SQL.CategoryCode={task.CategoryCode}" & vbCrLf &
                                     $"DTO.Department={task.Department}" & vbCrLf &
                                     $"DTO.TaskType={task.TaskType}" & vbCrLf &
                                     $"DTO.CategoryCode={task.CategoryCode}" & vbCrLf &
                                     $"ResolverInput.TaskType={task.TaskType}" & vbCrLf &
                                     $"ResolverInput.CategoryCode={task.CategoryCode}" & vbCrLf &
                                     $"ResolutionTier={resolutionTier}" & vbCrLf &
                                     $"ResolvedTemplate={tmpl.TaskType}" & vbCrLf &
                                     $"RenderedSteps={renderedStepsStr}"
            _appLogger.LogInfo(traceMsg, "DailyTasksControl")

            For idx As Integer = 0 To steps.Count - 1
                Dim st = steps(idx)
                Dim chk As New CheckBox() With {
                    .Checked = st.IsCompleted,
                    .AutoSize = True,
                    .Margin = New Padding(0, 3, 0, 3),
                    .Tag = st
                }

                If st.IsCompleted Then
                    chk.Text = $"✓  {st.StepText}"
                    chk.Font = New Font(ThemeConstants.FontNameDefault, 8.25!, FontStyle.Regular)
                    chk.ForeColor = Color.FromArgb(16, 185, 129) ' Emerald Green
                ElseIf idx = currentActiveIndex Then
                    chk.Text = $"➔  {st.StepText}  (NEXT ACTION)"
                    chk.Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold)
                    chk.ForeColor = Color.FromArgb(37, 99, 235) ' Vibrant Primary Blue
                Else
                    chk.Text = $"○  {st.StepText}"
                    chk.Font = New Font(ThemeConstants.FontNameDefault, 8.25!, FontStyle.Regular)
                    chk.ForeColor = Color.FromArgb(100, 116, 139) ' Slate Gray
                End If

                AddHandler chk.CheckedChanged, Async Sub(s, e)
                                                   st.IsCompleted = chk.Checked
                                                   Try
                                                       If chk.Checked Then
                                                           Dim stepDesc As String = "[CHECKLIST_COMPLETE] " & st.StepText
                                                           Dim existingActs = Await _activityRepo.GetActivitiesByTaskAsync(task.TaskId)
                                                           Dim alreadyLogged As Boolean = (existingActs IsNot Nothing AndAlso existingActs.Exists(Function(a) Not String.IsNullOrEmpty(a.ActivityDescription) AndAlso a.ActivityDescription.Trim() = stepDesc.Trim()))

                                                           If Not alreadyLogged Then
                                                               Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)
                                                               Dim actEntity As New TaskActivityEntity() With {
                                                                   .TaskId = task.TaskId,
                                                                   .UserId = currentUserId,
                                                                   .Category = TimeCategory.WorkTime,
                                                                   .ActivityDescription = stepDesc,
                                                                   .StartTime = DateTime.UtcNow,
                                                                   .EndTime = DateTime.UtcNow,
                                                                   .DurationMinutes = 1,
                                                                   .ActivityDate = DateTime.UtcNow.Date,
                                                                   .CreatedBy = currentUserId
                                                               }
                                                               Await _activityRepo.AddChecklistCompletionIdempotentAsync(actEntity)
                                                           End If
                                                       Else
                                                           ' Two-way reversible checklist persistence: remove completion activity when unchecked
                                                           Await _activityRepo.RemoveChecklistCompletionAsync(task.TaskId, st.StepText)
                                                       End If

                                                       ' Check if ALL statutory steps are now completed for this task
                                                       If steps.Count > 0 AndAlso steps.All(Function(stepItem) stepItem.IsCompleted) Then
                                                           _appLogger.LogInfo($"[WorkflowComplete] All {steps.Count} steps completed for TaskId #{task.TaskId} ('{task.Title}'). Transitioning WorkflowState to Completed.", "DailyTasksControl")
                                                           Await _taskService.CompleteTaskAsync(task.TaskId)
                                                           Forms.Common.DataStateTracker.MarkTasksChanged()
                                                           Await LoadInitialDataAsync()
                                                           Return
                                                       End If

                                                   Catch ex As Exception
                                                       _appLogger.LogError($"Activity step persistence error: {ex.Message}", "DailyTasksControl", ex)
                                                   End Try

                                                   ' Dynamically refresh checklist visual progression states
                                                   Await RenderNextActionChecklistAsync(task)
                                               End Sub
                pnlChecklistHost.Controls.Add(chk)
            Next
        End Function

        Private Sub SetActionButtonsEnabled(enabled As Boolean)
            Dim hasSelection = (_selectedTask IsNot Nothing)
            Dim canEdit = (hasSelection AndAlso _taskService.CanEditTask(_selectedTask.WorkflowState))
            Dim canDelete = (hasSelection AndAlso _taskService.CanDeleteTask(_selectedTask.WorkflowState))
            Dim canComplete = (hasSelection AndAlso _taskService.CanCompleteTask(_selectedTask.WorkflowState))

            If btnEditTask IsNot Nothing Then btnEditTask.Enabled = enabled AndAlso canEdit
            If btnDeleteTask IsNot Nothing Then btnDeleteTask.Enabled = enabled AndAlso canDelete
            If btnMarkComplete IsNot Nothing Then btnMarkComplete.Enabled = enabled AndAlso canComplete
            If btnOpenWorkflow IsNot Nothing Then btnOpenWorkflow.Enabled = enabled AndAlso hasSelection
            If btnCreateTask IsNot Nothing Then btnCreateTask.Enabled = enabled
        End Sub

        Private Sub ClearTaskDetailsPanel()
            _selectedTask = Nothing
            lblDetailTaskName.Text = "Select a task from queue"
            lblDetailTaskType.Text = "No active selection"
            lblDetailPriorityBadge.Text = "NONE"
            lblDetailPriorityBadge.ForeColor = ThemeConstants.TextSecondary
            lblDetailPriorityBadge.BackColor = Color.FromArgb(241, 245, 249)

            If lblMetaClientVal IsNot Nothing Then lblMetaClientVal.Text = "—"
            If lblMetaGstinVal IsNot Nothing Then lblMetaGstinVal.Text = "—"
            If lblMetaDueDateVal IsNot Nothing Then lblMetaDueDateVal.Text = "—"
            If lblMetaAssigneeVal IsNot Nothing Then lblMetaAssigneeVal.Text = "—"
            If lblMetaStatusVal IsNot Nothing Then lblMetaStatusVal.Text = "—"
            If lblMetaCreatedOnVal IsNot Nothing Then lblMetaCreatedOnVal.Text = "—"
            If lblMetaCreatedByVal IsNot Nothing Then lblMetaCreatedByVal.Text = "—"
            If lblNotesHeader IsNot Nothing Then lblNotesHeader.Text = "📝 NOTES (0)"
            If lblNotesContent IsNot Nothing Then lblNotesContent.Text = "No task selected."
            If lblNotesAuthorDate IsNot Nothing Then lblNotesAuthorDate.Text = "—"
            If pnlChecklistHost IsNot Nothing Then pnlChecklistHost.Controls.Clear()

            If btnEditTask IsNot Nothing Then btnEditTask.Enabled = False
            If btnDeleteTask IsNot Nothing Then btnDeleteTask.Enabled = False
            If btnMarkComplete IsNot Nothing Then btnMarkComplete.Enabled = False
            If btnOpenWorkflow IsNot Nothing Then btnOpenWorkflow.Enabled = False

            UpdateVisualRowHighlights()
        End Sub

        Private Sub EnsureTaskMatchesActiveFilters(task As TaskDto)
            If task Is Nothing Then Return

            Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)
            Dim isTerminal = (task.WorkflowState = TaskWorkflowState.Completed OrElse
                              task.WorkflowState = TaskWorkflowState.Delivered OrElse
                              task.WorkflowState = TaskWorkflowState.Closed OrElse
                              task.WorkflowState = TaskWorkflowState.Cancelled)

            ' Explicit tab switching for created active tasks
            If Not isTerminal Then
                If _activeTab = "Completed" Then
                    _activeTab = "AllActive"
                ElseIf _activeTab = "MyTasks" AndAlso task.AssignedToUserId <> currentUserId Then
                    _activeTab = "AllActive"
                End If
            End If

            ' Reset only incompatible dropdown & search filters
            If cboStatusFilter IsNot Nothing AndAlso cboStatusFilter.SelectedIndex > 0 Then
                Dim selectedStatusStr = If(cboStatusFilter.SelectedItem IsNot Nothing, cboStatusFilter.SelectedItem.ToString(), "")
                If task.WorkflowState.ToString() <> selectedStatusStr Then
                    cboStatusFilter.SelectedIndex = 0
                End If
            End If

            If cboStaffFilter IsNot Nothing AndAlso cboStaffFilter.SelectedIndex > 0 Then
                Dim selectedStaffStr = If(cboStaffFilter.SelectedItem IsNot Nothing, cboStaffFilter.SelectedItem.ToString(), "")
                If task.AssignedToName <> selectedStaffStr Then
                    cboStaffFilter.SelectedIndex = 0
                End If
            End If

            If cboPriorityFilter IsNot Nothing AndAlso cboPriorityFilter.SelectedIndex > 0 Then
                Dim selectedPriorityStr = If(cboPriorityFilter.SelectedItem IsNot Nothing, cboPriorityFilter.SelectedItem.ToString(), "")
                If task.Priority.ToString() <> selectedPriorityStr Then
                    cboPriorityFilter.SelectedIndex = 0
                End If
            End If

            If cboTaskTypeFilter IsNot Nothing AndAlso cboTaskTypeFilter.SelectedIndex > 0 Then
                Dim selectedTypeStr = If(cboTaskTypeFilter.SelectedItem IsNot Nothing, cboTaskTypeFilter.SelectedItem.ToString(), "")
                If task.TaskType <> selectedTypeStr AndAlso task.Department.ToString() <> selectedTypeStr Then
                    cboTaskTypeFilter.SelectedIndex = 0
                End If
            End If

            If cboDueDateFilter IsNot Nothing AndAlso cboDueDateFilter.SelectedIndex > 0 Then
                Dim selectedDueDate = If(cboDueDateFilter.SelectedItem IsNot Nothing, cboDueDateFilter.SelectedItem.ToString(), "")
                If selectedDueDate = "Overdue" AndAlso Not task.IsOverdue Then
                    cboDueDateFilter.SelectedIndex = 0
                ElseIf selectedDueDate = "Due Today" AndAlso (task.IsOverdue OrElse task.DaysRemaining > 0) Then
                    cboDueDateFilter.SelectedIndex = 0
                End If
            End If

            If Not String.IsNullOrWhiteSpace(txtSearch.Text) Then
                Dim search = txtSearch.Text.Trim().ToLower()
                Dim matchName = (Not String.IsNullOrEmpty(task.Title) AndAlso task.Title.ToLower().Contains(search))
                Dim matchClient = (Not String.IsNullOrEmpty(task.ClientName) AndAlso task.ClientName.ToLower().Contains(search))
                Dim matchCode = (Not String.IsNullOrEmpty(task.TaskCode) AndAlso task.TaskCode.ToLower().Contains(search))
                If Not (matchName OrElse matchClient OrElse matchCode) Then
                    txtSearch.Text = String.Empty
                End If
            End If

            If btnClearFilters IsNot Nothing Then
                Dim hasActiveFilters As Boolean = Not String.IsNullOrWhiteSpace(txtSearch.Text) OrElse
                                                  (cboStatusFilter IsNot Nothing AndAlso cboStatusFilter.SelectedIndex > 0) OrElse
                                                  (cboStaffFilter IsNot Nothing AndAlso cboStaffFilter.SelectedIndex > 0) OrElse
                                                  (cboPriorityFilter IsNot Nothing AndAlso cboPriorityFilter.SelectedIndex > 0) OrElse
                                                  (cboTaskTypeFilter IsNot Nothing AndAlso cboTaskTypeFilter.SelectedIndex > 0) OrElse
                                                  (cboDueDateFilter IsNot Nothing AndAlso cboDueDateFilter.SelectedIndex > 0)
                btnClearFilters.Enabled = hasActiveFilters
            End If
        End Sub

        Private Async Sub btnCreateTask_Click(sender As Object, e As EventArgs)
            If _isOperationRunning Then Return
            _isOperationRunning = True
            SetActionButtonsEnabled(False)

            Try
                Using dlg As New FrmCreateTask(_taskService, _clientService, _userRepo, _taskCategoryService, _appLogger)
                    If dlg.ShowDialog(Me.FindForm()) = DialogResult.OK Then
                        Dim newId As Integer = dlg.CreatedTaskId

                        ' Post-Create Refresh Sequence
                        Try
                            ' 1. Re-query authoritative tasks from database
                            Await LoadInitialDataAsync()

                            If newId > 0 Then
                                ' Locate created task in authoritative collection
                                Dim createdTask = _allTasksList.FirstOrDefault(Function(t) t.TaskId = newId)

                                If createdTask Is Nothing Then
                                    Dim dbVerify = Await _taskService.GetTaskByIdAsync(newId)
                                    If dbVerify IsNot Nothing Then
                                        _appLogger.LogWarn($"Created TaskId {newId} ({dbVerify.Title}) exists in database but was excluded from GetAllTasksAsync collection. State: {dbVerify.WorkflowState}")
                                        _allTasksList.Insert(0, dbVerify)
                                        createdTask = dbVerify
                                    End If
                                End If

                                If createdTask IsNot Nothing Then
                                    EnsureTaskMatchesActiveFilters(createdTask)
                                    ApplyTabAndFilterLogic(0)
                                    SelectTaskRow(createdTask)
                                End If
                            End If
                        Catch exRefresh As Exception
                            _appLogger.LogError($"[TaskPostCreateError] Post-create refresh warning: {exRefresh.Message}", "DailyTasksControl", exRefresh)
                        End Try

                        Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "TASK CREATED", "Task created successfully.", Forms.Common.AlertType.SuccessAlert, actionText:="OK")
                    End If
                End Using
            Catch ex As Exception
                _appLogger.LogError($"Task creation modal dialog error: {ex.Message}", "DailyTasksControl", ex)
            Finally
                SetActionButtonsEnabled(True)
                _isOperationRunning = False
            End Try
        End Sub

        Private Sub btnOpenWorkflow_Click(sender As Object, e As EventArgs)
            If _isOperationRunning Then Return
            Dim entryPointStr As String = If(TypeOf sender Is ToolStripMenuItem, "ContextMenu ('Open Task / Workflow')", "SidebarButton ('Open Workflow')")
            If _selectedTask IsNot Nothing Then
                OpenTaskWorkflowScreen(_selectedTask, entryPointStr)
            Else
                Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "Selection Required", "Please select a task from the queue to open workflow.", Forms.Common.AlertType.WarningAlert, actionText:="OK")
            End If
        End Sub

        Private Async Sub btnEditTask_Click(sender As Object, e As EventArgs)
            If _isOperationRunning Then Return
            If _selectedTask Is Nothing Then
                Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "Selection Required", "Please select an active task from the queue to edit.", Forms.Common.AlertType.WarningAlert, actionText:="OK")
                Return
            End If

            _isOperationRunning = True
            SetActionButtonsEnabled(False)

            Try
                ' Rule 4 Ground-Truth Database Re-Query Protection
                Dim latestTask = Await _taskService.GetTaskByIdAsync(_selectedTask.TaskId)
                If latestTask Is Nothing OrElse Not _taskService.CanEditTask(latestTask.WorkflowState) Then
                    Dim stateName As String = If(latestTask IsNot Nothing, latestTask.WorkflowState.ToString(), "Deleted / Archived")
                    Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "TASK CANNOT BE EDITED", $"Task '{_selectedTask.Title}' ({_selectedTask.TaskCode}) is in state '{stateName}' and cannot be edited per firm policy.", Forms.Common.AlertType.WarningAlert, actionText:="UNDERSTOOD")
                    Await LoadInitialDataAsync()
                    Return
                End If

                Using dlg As New FrmEditTask(_selectedTask.TaskId, _taskService, _clientService, _userRepo, _taskCategoryService, _appLogger)
                    If dlg.ShowDialog(Me.FindForm()) = DialogResult.OK Then
                        Dim updatedId As Integer = dlg.EditedTaskId

                        ' Rule 9 Queue refresh behavior
                        Await LoadInitialDataAsync()

                        If updatedId > 0 Then
                            Dim editedTask = _filteredTasksList.FirstOrDefault(Function(t) t.TaskId = updatedId)
                            If editedTask Is Nothing Then
                                editedTask = _allTasksList.FirstOrDefault(Function(t) t.TaskId = updatedId)
                            End If

                            If editedTask IsNot Nothing Then
                                SelectTaskRow(editedTask)
                            End If
                        End If

                        Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "TASK UPDATED", "Task details updated successfully.", Forms.Common.AlertType.SuccessAlert, actionText:="OK")
                    End If
                End Using
            Catch ex As Exception
                _appLogger.LogError($"Task edit modal dialog error: {ex.Message}")
                Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "Error", "Failed to edit task: " & ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
            Finally
                SetActionButtonsEnabled(True)
                _isOperationRunning = False
            End Try
        End Sub

        Private Async Sub btnMarkComplete_Click(sender As Object, e As EventArgs)
            If _isOperationRunning Then Return
            If _selectedTask Is Nothing Then
                Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "Selection Required", "Please select a task from the queue to mark complete.", Forms.Common.AlertType.WarningAlert, actionText:="OK")
                Return
            End If

            _isOperationRunning = True
            SetActionButtonsEnabled(False)
            
            Dim currentRole As UserRole = If(CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser IsNot Nothing, CurrentUserContext.CurrentUser.Role, UserRole.Employee)
            Dim isEmployee = currentRole = UserRole.Employee

            Try
                ' 1. Ground-Truth Status Re-Query: Re-fetch latest status from database before executing completion
                Dim latestTask = Await _taskService.GetTaskByIdAsync(_selectedTask.TaskId)
                If latestTask Is Nothing OrElse Not _taskService.CanCompleteTask(latestTask.WorkflowState) Then
                    Dim stateName As String = If(latestTask IsNot Nothing, latestTask.WorkflowState.ToString(), "Archived / Deleted")
                    Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "TASK ALREADY COMPLETED OR TERMINATED", $"Task '{_selectedTask.Title}' ({_selectedTask.TaskCode}) is in state '{stateName}' and cannot be completed again.", Forms.Common.AlertType.InfoAlert, actionText:="UNDERSTOOD")
                    Await LoadInitialDataAsync()
                    Return
                End If

                ' 2. Guarded Statutory Completion Rules
                Dim isStatutory As Boolean = (_selectedTask.Department = DepartmentType.IncomeTax OrElse _selectedTask.Department = DepartmentType.Accounting OrElse _selectedTask.Title.ToLower().Contains("gstr") OrElse _selectedTask.Title.ToLower().Contains("tds"))

                If isStatutory AndAlso _taskChecklistCache.ContainsKey(_selectedTask.TaskId) Then
                    Dim steps = _taskChecklistCache(_selectedTask.TaskId)
                    Dim incompleteSteps = steps.FindAll(Function(s) s.IsMandatory AndAlso Not s.IsCompleted)

                    If incompleteSteps.Count > 0 Then
                        Dim stepListText As String = String.Join(Environment.NewLine, incompleteSteps.Select(Function(s) "  • " & s.StepText))
                        Dim alertMsg = $"Cannot complete statutory return task automatically.{Environment.NewLine}{Environment.NewLine}Incomplete Mandatory Steps:{Environment.NewLine}{stepListText}{Environment.NewLine}{Environment.NewLine}Please complete all required workflow steps before marking complete."
                        Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "MANDATORY WORKFLOW INCOMPLETE", alertMsg, Forms.Common.AlertType.WarningAlert, actionText:="UNDERSTOOD")
                        Return
                    End If
                End If

                ' 3. Confirmation Modal Guard
                Dim actionVerb = If(isEmployee, "Submit", "Complete")
                Dim confirmMsg = If(isEmployee,
                    $"Submit {_selectedTask.Title} for review?{Environment.NewLine}{Environment.NewLine}Confirm that you have finished your work.",
                    $"Complete {_selectedTask.Title}?{Environment.NewLine}{Environment.NewLine}Confirm that the return has been filed and all required work has been verified.")

                Dim confirmRes = Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), $"Confirm {actionVerb}", confirmMsg, Forms.Common.AlertType.WarningAlert, actionText:=$"CONFIRM {actionVerb.ToUpper()}", showCancel:=True, cancelText:="CANCEL")

                If confirmRes = DialogResult.OK Then
                    Me.Cursor = Cursors.WaitCursor
                    
                    If isEmployee Then
                        Await _taskService.TransitionTaskStateAsync(_selectedTask.TaskId, TaskWorkflowState.UnderReview)
                        Forms.Common.DataStateTracker.MarkTasksChanged()
                        Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "Task Submitted", $"Task '{_selectedTask.Title}' submitted for review successfully.", Forms.Common.AlertType.SuccessAlert, actionText:="OK")
                    Else
                        Await _taskService.CompleteTaskAsync(_selectedTask.TaskId)
                        Forms.Common.DataStateTracker.MarkTasksChanged()
                        Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "Task Completed", $"Task '{_selectedTask.Title}' marked completed successfully.", Forms.Common.AlertType.SuccessAlert, actionText:="OK")
                    End If
                    Await LoadInitialDataAsync()
                End If
            Catch ex As Exception
                Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "Error", "Failed to complete task: " & ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
            Finally
                Me.Cursor = Cursors.Default
                SetActionButtonsEnabled(True)
                _isOperationRunning = False
            End Try
        End Sub

        Private Async Sub btnAcceptTask_Click(sender As Object, e As EventArgs)
            If _isOperationRunning Then Return
            If _selectedTask Is Nothing Then
                Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "Selection Required", "Please select an assigned task to accept.", Forms.Common.AlertType.WarningAlert, actionText:="OK")
                Return
            End If

            _isOperationRunning = True
            SetActionButtonsEnabled(False)
            Dim needsRefresh As Boolean = False

            Try
                Me.Cursor = Cursors.WaitCursor

                ' Re-query latest task ground-truth
                Dim latestTask = Await _taskService.GetTaskByIdAsync(_selectedTask.TaskId)
                If latestTask Is Nothing OrElse latestTask.WorkflowState <> TaskWorkflowState.Assigned Then
                    Dim stateName = If(latestTask IsNot Nothing, latestTask.WorkflowState.ToString(), "Deleted / Archived")
                    Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "TASK CANNOT BE ACCEPTED", $"Task '{_selectedTask.Title}' ({_selectedTask.TaskCode}) is in state '{stateName}' and cannot be accepted.", Forms.Common.AlertType.WarningAlert, actionText:="UNDERSTOOD")
                    needsRefresh = True
                    Return
                End If

                Dim success = Await _taskService.AcceptTaskAsync(latestTask.TaskId, latestTask.ModifiedOn)

                If success Then
                    Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "TASK ACCEPTED", $"Task '{latestTask.Title}' ({latestTask.TaskCode}) accepted successfully! Work lock active.", Forms.Common.AlertType.SuccessAlert, actionText:="OK")
                    Await LoadInitialDataAsync()
                Else
                    Throw New BusinessException("Acceptance operation returned false.", "ERR_ACCEPT_FAILED")
                End If

            Catch ex As ConcurrencyException
                Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "CONCURRENCY CONFLICT", ex.Message, Forms.Common.AlertType.WarningAlert, actionText:="REFRESH QUEUE")
                needsRefresh = True
            Catch ex As BusinessException
                Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "ACCEPTANCE FAILED", ex.Message, Forms.Common.AlertType.WarningAlert, actionText:="OK")
                needsRefresh = True
            Catch ex As Exception
                Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "Error", "Failed to accept task: " & ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
            Finally
                Me.Cursor = Cursors.Default
                SetActionButtonsEnabled(True)
                _isOperationRunning = False
            End Try

            If needsRefresh Then
                Await LoadInitialDataAsync()
            End If
        End Sub

        Private Async Sub btnDeleteTask_Click(sender As Object, e As EventArgs)
            If _isOperationRunning Then Return
            If _selectedTask Is Nothing Then
                Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "Selection Required", "Please select an active task from the queue to delete.", Forms.Common.AlertType.WarningAlert, actionText:="OK")
                Return
            End If

            _isOperationRunning = True
            SetActionButtonsEnabled(False)

            Dim needsRefresh As Boolean = False

            Try
                ' Rule 4 Ground-Truth Database Re-Query Protection
                Dim currentRole As UserRole = If(CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser IsNot Nothing, CurrentUserContext.CurrentUser.Role, UserRole.Employee)
                Dim latestTask = Await _taskService.GetTaskByIdAsync(_selectedTask.TaskId)
                If latestTask Is Nothing OrElse Not _taskService.CanDeleteTask(latestTask.WorkflowState, currentRole) Then
                    Dim stateName As String = If(latestTask IsNot Nothing, latestTask.WorkflowState.ToString(), "Already Deleted / Archived")
                    Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "TASK CANNOT BE DELETED", $"Task '{_selectedTask.Title}' ({_selectedTask.TaskCode}) is in state '{stateName}' and cannot be deleted per firm policy.", Forms.Common.AlertType.WarningAlert, actionText:="UNDERSTOOD")
                    Await LoadInitialDataAsync()
                    Return
                End If

                ' Rule 6 Delete Confirmation containing Task Code, Task Title, and Client Name
                Dim taskCodeStr = If(Not String.IsNullOrEmpty(latestTask.TaskCode), latestTask.TaskCode, _selectedTask.TaskCode)
                Dim taskTitleStr = If(Not String.IsNullOrEmpty(latestTask.Title), latestTask.Title, _selectedTask.Title)
                Dim clientNameStr = If(Not String.IsNullOrEmpty(latestTask.ClientName), latestTask.ClientName, If(Not String.IsNullOrEmpty(_selectedTask.ClientName), _selectedTask.ClientName, "Firm Master"))

                Dim isInProgress = (latestTask.WorkflowState <> TaskWorkflowState.NewTask AndAlso latestTask.WorkflowState <> TaskWorkflowState.Assigned)

                Dim confirmMsg As String
                If isInProgress Then
                    confirmMsg = $"⚠️ WARNING: Task {taskCodeStr} – '{taskTitleStr}' is currently IN PROGRESS (being worked on by staff member '{_selectedTask.AssignedToName}').{Environment.NewLine}{Environment.NewLine}Deleting this task will interrupt active work and soft-delete the record from the queue while preserving audit logs.{Environment.NewLine}{Environment.NewLine}Are you sure you want to soft-delete this active task?"
                Else
                    confirmMsg = $"Are you sure you want to remove task {taskCodeStr} – '{taskTitleStr}' for Client '{clientNameStr}' from the active work queue?{Environment.NewLine}{Environment.NewLine}This will remove the task from the active queue while preserving the database audit record."
                End If

                Dim confirmRes = Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), If(isInProgress, "SOFT-DELETE ACTIVE TASK?", "Delete Task?"), confirmMsg, Forms.Common.AlertType.WarningAlert, actionText:="SOFT-DELETE TASK", showCancel:=True, cancelText:="CANCEL")

                If confirmRes = DialogResult.OK Then
                    Me.Cursor = Cursors.WaitCursor
                    Dim success = Await _taskService.SoftDeleteTaskAsync(latestTask.TaskId, latestTask.ModifiedOn)

                    If success Then

                        Forms.Common.DataStateTracker.MarkTasksChanged()
                        ' Rule 9 Queue refresh behavior
                        Await LoadInitialDataAsync()

                        ' Clear Task Details sidebar
                        ClearTaskDetailsPanel()
                        _selectedTask = Nothing

                        Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "TASK REMOVED", $"Task {taskCodeStr} ({taskTitleStr}) removed from active work queue.", Forms.Common.AlertType.SuccessAlert, actionText:="OK")
                    Else
                        Throw New BusinessException("Soft delete operation returned false.", "ERR_DELETE_FAILED")
                    End If
                End If
            Catch ex As ConcurrencyException
                ' Await not permitted in Catch block — defer refresh via flag
                Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "CONCURRENCY CONFLICT", ex.Message, Forms.Common.AlertType.WarningAlert, actionText:="REFRESH QUEUE")
                needsRefresh = True
            Catch ex As BusinessException
                Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "DELETE FAILED", ex.Message, Forms.Common.AlertType.WarningAlert, actionText:="OK")
                needsRefresh = True
            Catch ex As Exception
                Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "Error", ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
            Finally
                Me.Cursor = Cursors.Default
                SetActionButtonsEnabled(True)
                _isOperationRunning = False
            End Try

            ' Deferred async refresh — executed after Try/Catch/Finally completes
            If needsRefresh Then
                Await LoadInitialDataAsync()
            End If
        End Sub

        Private Async Sub PromptReassignTask()
            If _isOperationRunning Then Return
            If _selectedTask Is Nothing Then Return

            _isOperationRunning = True
            SetActionButtonsEnabled(False)

            ' Quick Staff Reassignment Trigger
            Try
                Dim users = Await _userRepo.GetAllAsync()
                If users IsNot Nothing AndAlso users.Count > 0 Then
                    Dim nextUser = users.FirstOrDefault(Function(u) u.UserId <> _selectedTask.AssignedToUserId AndAlso u.IsActive)
                    If nextUser IsNot Nothing Then
                        Await _taskService.ReassignTaskAsync(_selectedTask.TaskId, nextUser.UserId)
                        Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "Reassigned", $"Task reassigned to {nextUser.FullName}.", Forms.Common.AlertType.SuccessAlert, actionText:="OK")
                        Await LoadInitialDataAsync()
                    End If
                End If
            Catch ex As Exception
                Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "Error", ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
            Finally
                SetActionButtonsEnabled(True)
                _isOperationRunning = False
            End Try
        End Sub

        Private Async Sub PromptChangePriority()
            If _isOperationRunning Then Return
            If _selectedTask Is Nothing Then Return

            _isOperationRunning = True
            SetActionButtonsEnabled(False)

            Try
                _selectedTask.Priority = If(_selectedTask.Priority = TaskPriority.High, TaskPriority.Medium, TaskPriority.High)
                Await _taskService.UpdateTaskAsync(_selectedTask)
                Forms.Common.DataStateTracker.MarkTasksChanged()
                Await LoadInitialDataAsync()
            Catch ex As Exception
                Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "Error", ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
            Finally
                SetActionButtonsEnabled(True)
                _isOperationRunning = False
            End Try
        End Sub

        Private Async Sub btnRestoreTask_Click(sender As Object, e As EventArgs)
            If _isOperationRunning Then Return
            If _selectedTask Is Nothing Then Return

            _isOperationRunning = True
            SetActionButtonsEnabled(False)

            Dim needsRefresh As Boolean = False
            Try
                Dim confirmRes = Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "Restore Task?", $"Are you sure you want to restore task {_selectedTask.TaskCode} – '{_selectedTask.Title}' to the active queue?", Forms.Common.AlertType.WarningAlert, actionText:="RESTORE TASK", showCancel:=True, cancelText:="CANCEL")
                If confirmRes = DialogResult.OK Then
                    Me.Cursor = Cursors.WaitCursor
                    Dim success = Await _taskService.RestoreTaskAsync(_selectedTask.TaskId)
                    If success Then
                        _localTasksDataVersion = 0
                        Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "Task Restored", $"Task {_selectedTask.TaskCode} has been restored to the active queue.", Forms.Common.AlertType.SuccessAlert, actionText:="OK")
                        Await LoadInitialDataAsync()
                    End If
                End If
            Catch ex As Exception
                _appLogger.LogError($"Error restoring task ID {_selectedTask.TaskId}", "DailyTasksControl", ex)
                Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "Restore Failed", ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
                needsRefresh = True
            Finally
                Me.Cursor = Cursors.Default
                _isOperationRunning = False
                SetActionButtonsEnabled(True)
            End Try

            If needsRefresh Then
                Await LoadInitialDataAsync()
            End If
        End Sub

        Private Async Sub btnHardDeleteTask_Click(sender As Object, e As EventArgs)
            If _isOperationRunning Then Return
            If _selectedTask Is Nothing Then Return

            _isOperationRunning = True
            SetActionButtonsEnabled(False)

            Dim needsRefresh As Boolean = False
            Try
                Dim confirmRes = ShowDeleteConfirmationDialog(_selectedTask.TaskCode)
                If confirmRes Then
                    Me.Cursor = Cursors.WaitCursor
                    Dim success = Await _taskService.PermanentlyDeleteTaskAsync(_selectedTask.TaskId)
                    If success Then
                        _localTasksDataVersion = 0
                        Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "Task Deleted", $"Task {_selectedTask.TaskCode} has been permanently deleted.", Forms.Common.AlertType.SuccessAlert, actionText:="OK")
                        Await LoadInitialDataAsync()
                    End If
                End If
            Catch ex As Exception
                _appLogger.LogError($"Error permanently deleting task ID {_selectedTask.TaskId}", "DailyTasksControl", ex)
                Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "Delete Failed", ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
                needsRefresh = True
            Finally
                Me.Cursor = Cursors.Default
                _isOperationRunning = False
                SetActionButtonsEnabled(True)
            End Try

            If needsRefresh Then
                Await LoadInitialDataAsync()
            End If
        End Sub

        Private Function ShowDeleteConfirmationDialog(taskCode As String) As Boolean
            Dim frm As New Form()
            frm.Text = "Permanently Delete Task"
            frm.Size = New Size(420, 200)
            frm.StartPosition = FormStartPosition.CenterParent
            frm.FormBorderStyle = FormBorderStyle.FixedDialog
            frm.MaximizeBox = False
            frm.MinimizeBox = False
            frm.ShowInTaskbar = False

            Dim lbl As New Label()
            lbl.Text = $"To permanently delete this task, please type its reference code ({taskCode}) below:"
            lbl.Location = New Point(20, 20)
            lbl.AutoSize = True
            frm.Controls.Add(lbl)

            Dim txt As New TextBox()
            txt.Location = New Point(20, 50)
            txt.Width = 360
            frm.Controls.Add(txt)

            Dim btnOk As New Button()
            btnOk.Text = "Permanently Delete"
            btnOk.Location = New Point(180, 100)
            btnOk.Width = 120
            btnOk.Enabled = False
            btnOk.DialogResult = DialogResult.OK
            frm.Controls.Add(btnOk)

            Dim btnCancel As New Button()
            btnCancel.Text = "Cancel"
            btnCancel.Location = New Point(310, 100)
            btnCancel.Width = 70
            btnCancel.DialogResult = DialogResult.Cancel
            frm.Controls.Add(btnCancel)

            AddHandler txt.TextChanged, Sub(s, e)
                                            btnOk.Enabled = (txt.Text.Trim().ToUpper() = taskCode.ToUpper())
                                        End Sub

            frm.AcceptButton = btnOk
            frm.CancelButton = btnCancel

            Dim result = frm.ShowDialog(Me.FindForm())
            Return (result = DialogResult.OK)
        End Function
    End Class
End Namespace
