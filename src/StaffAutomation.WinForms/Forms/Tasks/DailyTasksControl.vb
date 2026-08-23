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
Imports StaffAutomation.WinForms.Forms.Main

Namespace Forms.Tasks
    ''' <summary>
    ''' Professional Operational Work Queue &amp; Task Management UserControl.
    ''' Implements 64% Queue / 36% Sticky Task Details two-column layout, urgency sectioning (Overdue, Due Today, Upcoming),
    ''' dynamic multi-criteria filtering, data-backed next action workflow checklists, and guarded statutory completion logic.
    ''' </summary>
    Public Class DailyTasksControl
        Inherits UserControl

        ' Services & Repositories
        Private ReadOnly _taskService As ITaskManagementService
        Private ReadOnly _clientService As IClientService
        Private ReadOnly _userRepo As DAL.Interfaces.IUserRepository
        Private ReadOnly _activityRepo As DAL.Interfaces.ITaskActivityRepository
        Private ReadOnly _discussionService As IDiscussionService
        Private ReadOnly _appLogger As IAppLogger
        Private ReadOnly _auditLogger As AuditLogger
        Private ReadOnly _mainShellHost As Forms.Main.FrmMainShell

        ' In-Memory Data State
        Private _allTasksList As New List(Of TaskDto)()
        Private _filteredTasksList As New List(Of TaskDto)()
        Private _selectedTask As TaskDto = Nothing
        Private _activeTab As String = "NeedsAttention" ' NeedsAttention, MyTasks, AllActive, Completed
        Private _activePresetFilter As String = ""
        Private _isUpcomingExpanded As Boolean = False
        Private _isOperationRunning As Boolean = False

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
            Dim workflowEngine As ITaskWorkflowEngine = New TaskWorkflowEngine()

            _taskService = New TaskManagementService(taskRepo, workflowEngine, _appLogger, _auditLogger, clientRepo, _userRepo, _activityRepo, connFactory)
            _clientService = New ClientService(clientRepo, _appLogger, _auditLogger)
            _discussionService = New DiscussionService(discussionRepo, taskRepo, _appLogger, _auditLogger)

            InitializeComponent()
        End Sub

        Protected Overrides Async Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)
            SetSearchPlaceholder()
            Await LoadInitialDataAsync()
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

        Protected Overrides Async Sub OnVisibleChanged(e As EventArgs)
            MyBase.OnVisibleChanged(e)
            If Me.Visible AndAlso Not Me.DesignMode Then
                Await LoadInitialDataAsync()
            End If
        End Sub

        Private Sub SetSearchPlaceholder()
            If txtSearch IsNot Nothing AndAlso txtSearch.IsHandleCreated Then
                SendMessage(txtSearch.Handle, EM_SETCUEBANNER, New IntPtr(1), "Search tasks, client, GSTIN...")
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
                .BackColor = Color.Transparent
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
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
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

            AddHandler btnTabNeedsAttention.Click, Sub(s, e) SwitchActiveTab("NeedsAttention")
            AddHandler btnTabMyTasks.Click, Sub(s, e) SwitchActiveTab("MyTasks")
            AddHandler btnTabAllActive.Click, Sub(s, e) SwitchActiveTab("AllActive")
            AddHandler btnTabCompleted.Click, Sub(s, e) SwitchActiveTab("Completed")

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
                .Size = New Size(195, 24),
                .TabIndex = 0
            }
            ThemeConstants.ApplyAppTextBoxStyle(txtSearch)
            AddHandler txtSearch.HandleCreated, Sub(s, e) SetSearchPlaceholder()
            AddHandler txtSearch.TextChanged, AddressOf OnFilterChanged

            cboStatusFilter = New Krypton.Toolkit.KryptonComboBox() With {
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Location = New Point(211, 9),
                .Size = New Size(130, 24),
                .TabIndex = 1
            }
            ThemeConstants.ApplyAppComboBoxStyle(cboStatusFilter)
            cboStatusFilter.Items.AddRange(New Object() {"All Status", "Pending", "In Progress", "Waiting for Client", "Waiting for Documents", "Under Review", "On Hold"})
            cboStatusFilter.SelectedIndex = 0
            AddHandler cboStatusFilter.SelectedIndexChanged, AddressOf OnFilterChanged

            cboStaffFilter = New Krypton.Toolkit.KryptonComboBox() With {
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Location = New Point(347, 9),
                .Size = New Size(115, 24),
                .TabIndex = 2
            }
            ThemeConstants.ApplyAppComboBoxStyle(cboStaffFilter)
            cboStaffFilter.Items.Add("All Staff")
            cboStaffFilter.SelectedIndex = 0
            AddHandler cboStaffFilter.SelectedIndexChanged, AddressOf OnFilterChanged

            cboPriorityFilter = New Krypton.Toolkit.KryptonComboBox() With {
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Location = New Point(468, 9),
                .Size = New Size(105, 24),
                .TabIndex = 3
            }
            ThemeConstants.ApplyAppComboBoxStyle(cboPriorityFilter)
            cboPriorityFilter.Items.AddRange(New Object() {"All Priority", "Critical", "High", "Medium", "Low"})
            cboPriorityFilter.SelectedIndex = 0
            AddHandler cboPriorityFilter.SelectedIndexChanged, AddressOf OnFilterChanged

            cboTaskTypeFilter = New Krypton.Toolkit.KryptonComboBox() With {
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Location = New Point(579, 9),
                .Size = New Size(130, 24),
                .TabIndex = 4
            }
            ThemeConstants.ApplyAppComboBoxStyle(cboTaskTypeFilter)
            cboTaskTypeFilter.Items.AddRange(New Object() {"All Task Types", "GST Compliance", "Income Tax", "TDS Returns", "ROC / MCA", "Accounting", "Audit", "Client Onboarding", "Other"})
            cboTaskTypeFilter.SelectedIndex = 0
            AddHandler cboTaskTypeFilter.SelectedIndexChanged, AddressOf OnFilterChanged

            cboDueDateFilter = New Krypton.Toolkit.KryptonComboBox() With {
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Location = New Point(715, 9),
                .Size = New Size(115, 24),
                .TabIndex = 5
            }
            ThemeConstants.ApplyAppComboBoxStyle(cboDueDateFilter)
            cboDueDateFilter.Items.AddRange(New Object() {"All Due Dates", "Overdue", "Due Today", "Due This Week", "Due This Month"})
            cboDueDateFilter.SelectedIndex = 0
            AddHandler cboDueDateFilter.SelectedIndexChanged, AddressOf OnFilterChanged

            btnClearFilters = New Forms.Common.ModernButton() With {
                .Text = "↺ Clear",
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
                .Size = New Size(320, 130),
                .ColumnCount = 2,
                .RowCount = 5,
                .BackColor = Color.FromArgb(248, 250, 252)
            }
            pnlMetaGrid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 45.0!))
            pnlMetaGrid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 55.0!))

            lblMetaClientVal = AddMetaGridRow(pnlMetaGrid, 0, "Client:", "ABC Traders")
            lblMetaGstinVal = AddMetaGridRow(pnlMetaGrid, 1, "GSTIN / Ref:", "27ABCDE1234F1Z5 🔗")
            lblMetaDueDateVal = AddMetaGridRow(pnlMetaGrid, 2, "Due Date:", "Due Today (19 Aug)")
            lblMetaAssigneeVal = AddMetaGridRow(pnlMetaGrid, 3, "Assigned To:", "Priya Shah")
            lblMetaStatusVal = AddMetaGridRow(pnlMetaGrid, 4, "Status:", "Pending")

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

            btnEditTask = New Forms.Common.ModernButton() With {
                .Text = "✏ Edit Task",
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Secondary,
                .Size = New Size(150, 32),
                .Location = New Point(0, 4)
            }
            AddHandler btnEditTask.Click, AddressOf btnEditTask_Click

            btnDeleteTask = New Forms.Common.ModernButton() With {
                .Text = "🗑 Delete Task",
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Custom,
                .BackColor = Color.FromArgb(239, 68, 68),
                .ForeColor = Color.White,
                .Size = New Size(150, 32),
                .Location = New Point(158, 4)
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
                .Text = "✓ Mark as Complete",
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

            ' Final Container Assembly
            Me.Controls.Add(scMainSplit)
            Me.Controls.Add(pnlFilterBar)
            Me.Controls.Add(pnlHeader)

            Me.ResumeLayout(False)
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
            Dim itemAccept As New ToolStripMenuItem("✓ Accept Task", Nothing, AddressOf btnAcceptTask_Click)
            Dim itemEdit As New ToolStripMenuItem("✏️ Edit Task", Nothing, AddressOf btnEditTask_Click)
            Dim itemDelete As New ToolStripMenuItem("🗑️ Soft Delete Task", Nothing, AddressOf btnDeleteTask_Click)
            Dim itemOpen As New ToolStripMenuItem("➔ Open Task / Workflow", Nothing, AddressOf btnOpenWorkflow_Click)
            Dim itemReassign As New ToolStripMenuItem("👤 Reassign Staff", Nothing, Sub(s, e) PromptReassignTask())
            Dim itemPriority As New ToolStripMenuItem("⚡ Change Priority", Nothing, Sub(s, e) PromptChangePriority())
            Dim itemComplete As New ToolStripMenuItem("✓ Mark as Complete", Nothing, AddressOf btnMarkComplete_Click)

            ctxRowMenu.Items.Add(itemAccept)
            ctxRowMenu.Items.Add(itemEdit)
            ctxRowMenu.Items.Add(itemDelete)
            ctxRowMenu.Items.Add(New ToolStripSeparator())
            ctxRowMenu.Items.Add(itemOpen)
            ctxRowMenu.Items.Add(itemReassign)
            ctxRowMenu.Items.Add(itemPriority)
            ctxRowMenu.Items.Add(New ToolStripSeparator())
            ctxRowMenu.Items.Add(itemComplete)
        End Sub

        ' Async Data Loading
        Public Async Function LoadInitialDataAsync() As System.Threading.Tasks.Task
            Try
                Me.Cursor = Cursors.WaitCursor

                ' 1. Fetch Authoritative Task Collection First
                _allTasksList = Await _taskService.GetAllTasksAsync(includeDeleted:=False)

                ' 2. Populate Staff Filter Dropdown Safely
                Try
                    Dim users = Await _userRepo.GetAllAsync()
                    cboStaffFilter.Items.Clear()
                    cboStaffFilter.Items.Add("All Staff")
                    If users IsNot Nothing Then
                        For Each u In users
                            If u.IsActive Then cboStaffFilter.Items.Add(u.FullName)
                        Next
                    End If
                    If cboStaffFilter.Items.Count > 0 Then cboStaffFilter.SelectedIndex = 0
                Catch exUser As Exception
                    _appLogger.LogWarn($"Failed to load staff list for dropdown filter: {exUser.Message}")
                End Try

                ' 3. Apply Filter and Render Rows
                ApplyTabAndFilterLogic()

            Catch ex As Exception
                _appLogger.LogError($"Failed to load Daily Tasks data: {ex.Message}")
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Function

        Private Sub OnFilterChanged(sender As Object, e As EventArgs)
            If btnClearFilters IsNot Nothing Then
                Dim hasActiveFilters As Boolean = Not String.IsNullOrWhiteSpace(txtSearch.Text) OrElse
                                                  (cboStatusFilter IsNot Nothing AndAlso cboStatusFilter.SelectedIndex > 0) OrElse
                                                  (cboStaffFilter IsNot Nothing AndAlso cboStaffFilter.SelectedIndex > 0) OrElse
                                                  (cboPriorityFilter IsNot Nothing AndAlso cboPriorityFilter.SelectedIndex > 0) OrElse
                                                  (cboTaskTypeFilter IsNot Nothing AndAlso cboTaskTypeFilter.SelectedIndex > 0) OrElse
                                                  (cboDueDateFilter IsNot Nothing AndAlso cboDueDateFilter.SelectedIndex > 0)
                btnClearFilters.Enabled = hasActiveFilters
            End If
            ApplyTabAndFilterLogic()
        End Sub

        Private Sub btnClearFilters_Click(sender As Object, e As EventArgs)
            txtSearch.Text = String.Empty
            cboStatusFilter.SelectedIndex = 0
            cboStaffFilter.SelectedIndex = 0
            cboPriorityFilter.SelectedIndex = 0
            cboTaskTypeFilter.SelectedIndex = 0
            cboDueDateFilter.SelectedIndex = 0
            If btnClearFilters IsNot Nothing Then btnClearFilters.Enabled = False
            ApplyTabAndFilterLogic()
        End Sub

        Private Sub OpenTaskWorkflowScreen(task As TaskDto)
            Dim tId As Integer = If(task IsNot Nothing, task.TaskId, 0)
            If _mainShellHost IsNot Nothing Then
                _mainShellHost.LoadScreen(New FrmTaskWorkspace(tId, _mainShellHost))
            Else
                Using dlg As New FrmTaskWorkspace(tId, Nothing)
                    dlg.ShowDialog(Me.FindForm())
                End Using
            End If
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

            ApplyTabAndFilterLogic()
        End Sub

        Private Sub ApplyTabAndFilterLogic()
            If _allTasksList Is Nothing Then Return

            Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)
            Dim searchText = If(txtSearch IsNot Nothing, txtSearch.Text.Trim().ToLower(), String.Empty)
            Dim selectedStatus = If(cboStatusFilter IsNot Nothing AndAlso cboStatusFilter.SelectedItem IsNot Nothing, cboStatusFilter.SelectedItem.ToString(), "All Status")
            Dim selectedStaff = If(cboStaffFilter IsNot Nothing AndAlso cboStaffFilter.SelectedItem IsNot Nothing, cboStaffFilter.SelectedItem.ToString(), "All Staff")
            Dim selectedPriority = If(cboPriorityFilter IsNot Nothing AndAlso cboPriorityFilter.SelectedItem IsNot Nothing, cboPriorityFilter.SelectedItem.ToString(), "All Priority")
            Dim selectedTaskType = If(cboTaskTypeFilter IsNot Nothing AndAlso cboTaskTypeFilter.SelectedItem IsNot Nothing, cboTaskTypeFilter.SelectedItem.ToString(), "All Task Types")
            Dim selectedDueDate = If(cboDueDateFilter IsNot Nothing AndAlso cboDueDateFilter.SelectedItem IsNot Nothing, cboDueDateFilter.SelectedItem.ToString(), "All Due Dates")

            Dim today = DateTime.Today

            _filteredTasksList = _allTasksList.FindAll(Function(t)
                                                           ' 1. TAB ISOLATION RULE (Completed tasks NEVER in Needs Attention)
                                                           If _activeTab = "Completed" Then
                                                               If t.WorkflowState <> TaskWorkflowState.Completed AndAlso t.WorkflowState <> TaskWorkflowState.Delivered AndAlso t.WorkflowState <> TaskWorkflowState.Closed Then
                                                                   Return False
                                                               End If

                                                               ' Completed Today preset: strictly only tasks completed or due today
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

                                                               ' DueSoon preset: strictly active tasks due within next 48 hours (today to today + 2 days)
                                                               If _activePresetFilter = "duesoon" OrElse _activePresetFilter = "due_soon" OrElse _activePresetFilter = "due48h" Then
                                                                   If Not (t.TargetDueDate.Date >= today AndAlso t.TargetDueDate.Date <= today.AddDays(2)) Then
                                                                       Return False
                                                                   End If
                                                               ElseIf _activePresetFilter = "overdue" OrElse selectedDueDate = "Overdue" Then
                                                                   If Not (t.IsOverdue OrElse t.TargetDueDate.Date < today) Then
                                                                       Return False
                                                                   End If
                                                               End If
                                                           End If

                                                           ' 2. FILTER MATCHING
                                                           If Not String.IsNullOrEmpty(searchText) Then
                                                               Dim matchName = (Not String.IsNullOrEmpty(t.Title) AndAlso t.Title.ToLower().Contains(searchText))
                                                               Dim matchClient = (Not String.IsNullOrEmpty(t.ClientName) AndAlso t.ClientName.ToLower().Contains(searchText))
                                                               Dim matchCode = (Not String.IsNullOrEmpty(t.TaskCode) AndAlso t.TaskCode.ToLower().Contains(searchText))
                                                               If Not (matchName OrElse matchClient OrElse matchCode) Then Return False
                                                           End If

                                                           If selectedStatus <> "All Status" AndAlso t.WorkflowState.ToString() <> selectedStatus Then Return False
                                                           If selectedStaff <> "All Staff" AndAlso t.AssignedToName <> selectedStaff Then Return False
                                                           If selectedPriority <> "All Priority" AndAlso t.Priority.ToString() <> selectedPriority Then Return False
                                                           If selectedTaskType <> "All Task Types" AndAlso t.TaskType <> selectedTaskType AndAlso t.Department.ToString() <> selectedTaskType Then Return False

                                                           Return True
                                                       End Function)

            ' Update Tab Badge Counts
            Dim needsAttentionCount = _allTasksList.Where(Function(t) t.WorkflowState <> TaskWorkflowState.Completed AndAlso t.WorkflowState <> TaskWorkflowState.Closed AndAlso t.WorkflowState <> TaskWorkflowState.Cancelled).Count()
            Dim myTasksCount = _allTasksList.Where(Function(t) t.AssignedToUserId = currentUserId AndAlso t.WorkflowState <> TaskWorkflowState.Completed AndAlso t.WorkflowState <> TaskWorkflowState.Closed).Count()
            Dim allActiveCount = needsAttentionCount
            Dim completedCount = _allTasksList.Where(Function(t) t.WorkflowState = TaskWorkflowState.Completed OrElse t.WorkflowState = TaskWorkflowState.Closed).Count()

            btnTabNeedsAttention.Text = $"Needs Attention ({needsAttentionCount})"
            btnTabMyTasks.Text = $"My Tasks ({myTasksCount})"
            btnTabAllActive.Text = $"All Active ({allActiveCount})"
            btnTabCompleted.Text = $"Completed ({completedCount})"

            lblSubtitle.Text = $"My work queue  •  {_filteredTasksList.Count} active tasks displayed"

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

            If _filteredTasksList.Count = 0 Then
                pnlEmptyStateHost.Visible = True
                pnlEmptyStateHost.BringToFront()

                If hasActiveFilters Then
                    lblEmptyStateIcon.Text = "🔍"
                    lblEmptyStateTitle.Text = "No tasks found"
                    lblEmptyStateSubtitle.Text = "No tasks match the selected filters."
                    btnEmptyStateClearFilters.Visible = True
                Else
                    lblEmptyStateIcon.Text = "📋"
                    lblEmptyStateTitle.Text = "No active tasks"
                    lblEmptyStateSubtitle.Text = "There are no active tasks in your work queue. Click '+ Create Task' to create a new task."
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
            If ctxRowMenu IsNot Nothing AndAlso ctxRowMenu.Items.Count >= 9 Then
                ctxRowMenu.Items(0).Enabled = canAccept
                ctxRowMenu.Items(0).Visible = canAccept
                ctxRowMenu.Items(1).Enabled = canEdit
                ctxRowMenu.Items(2).Enabled = canDelete
                ctxRowMenu.Items(8).Enabled = canComplete
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

            lblMetaClientVal.Text = task.ClientName
            lblMetaGstinVal.Text = $"{task.TaskCode} 🔗"
            lblMetaDueDateVal.Text = If(task.IsOverdue, $"Overdue by {task.DaysOverdue} days", task.TargetDueDate.ToString("dd MMM yyyy"))
            lblMetaAssigneeVal.Text = task.AssignedToName
            lblMetaStatusVal.Text = task.WorkflowState.ToString()
            lblMetaCreatedOnVal.Text = task.AssignmentDate.ToString("dd MMM yyyy")

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
                    lblNotesAuthorDate.Text = "Use FrmTaskWorkspace to log discussions."
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

            ' Default workflow checklist steps for compliance tasks
            Dim steps As New List(Of ChecklistStepItem)() From {
                New ChecklistStepItem With {.StepText = "1. Verify purchase & sales data", .IsCompleted = False, .IsMandatory = True},
                New ChecklistStepItem With {.StepText = "2. Match with GSTR-2B ITC statement", .IsCompleted = False, .IsMandatory = True},
                New ChecklistStepItem With {.StepText = "3. Prepare return summary draft", .IsCompleted = False, .IsMandatory = True},
                New ChecklistStepItem With {.StepText = "4. File return on GST Govt Portal", .IsCompleted = False, .IsMandatory = True}
            }

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
            End Try

            _taskChecklistCache(task.TaskId) = steps

            For Each st In steps
                Dim chk As New CheckBox() With {
                    .Text = st.StepText,
                    .Checked = st.IsCompleted,
                    .Font = New Font(ThemeConstants.FontNameDefault, 8.25!, FontStyle.Regular),
                    .ForeColor = Color.FromArgb(30, 41, 59),
                    .AutoSize = True,
                    .Margin = New Padding(0, 2, 0, 2),
                    .Tag = st
                }
                AddHandler chk.CheckedChanged, Async Sub(s, e)
                                                   st.IsCompleted = chk.Checked
                                                   If chk.Checked Then
                                                       ' Idempotency Guard: Ensure step activity has not already been logged in tbl_TaskActivities
                                                       Try
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
                                                       Catch ex As Exception
                                                       End Try
                                                   End If
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

            lblMetaClientVal.Text = "—"
            lblMetaGstinVal.Text = "—"
            lblMetaDueDateVal.Text = "—"
            lblMetaAssigneeVal.Text = "—"
            lblMetaStatusVal.Text = "—"
            lblMetaCreatedOnVal.Text = "—"
            lblNotesHeader.Text = "📝 NOTES (0)"
            lblNotesContent.Text = "No task selected."
            lblNotesAuthorDate.Text = "—"
            pnlChecklistHost.Controls.Clear()

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
                Using dlg As New FrmCreateTask(_taskService, _clientService, _userRepo, _appLogger)
                    If dlg.ShowDialog(Me.FindForm()) = DialogResult.OK Then
                        Dim newId As Integer = dlg.CreatedTaskId

                        ' 1. Re-query authoritative tasks from database
                        Await LoadInitialDataAsync()

                        If newId > 0 Then
                            ' Locate created task in authoritative collection
                            Dim createdTask = _allTasksList.FirstOrDefault(Function(t) t.TaskId = newId)

                            If createdTask Is Nothing Then
                                ' Direct database verification if missing from main collection
                                Dim dbVerify = Await _taskService.GetTaskByIdAsync(newId)
                                If dbVerify IsNot Nothing Then
                                    _appLogger.LogWarn($"Created TaskId {newId} ({dbVerify.Title}) exists in database but was excluded from GetAllTasksAsync collection. State: {dbVerify.WorkflowState}")
                                    _allTasksList.Insert(0, dbVerify)
                                    createdTask = dbVerify
                                Else
                                    Dim errDetails As String = $"TaskId #{newId} was returned by FrmCreateTask but GetTaskByIdAsync({newId}) returned NULL."
                                    _appLogger.LogError(errDetails)
                                    Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "DIAGNOSTIC ERROR", errDetails, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
                                End If
                            End If

                            If createdTask IsNot Nothing Then
                                ' Explicit tab & filter alignment
                                EnsureTaskMatchesActiveFilters(createdTask)

                                ' Re-run filter logic and refresh view
                                ApplyTabAndFilterLogic()

                                ' Verify task presence in filtered list
                                If _filteredTasksList.Exists(Function(t) t.TaskId = newId) Then
                                    SelectTaskRow(createdTask)
                                Else
                                    Dim activeFiltersSummary As String = $"ActiveTab: {_activeTab}, Search: '{txtSearch.Text}', Status: '{cboStatusFilter?.SelectedItem}', Staff: '{cboStaffFilter?.SelectedItem}', Priority: '{cboPriorityFilter?.SelectedItem}', TaskType: '{cboTaskTypeFilter?.SelectedItem}'"
                                    Dim diagError As String = $"Task #{newId} ({createdTask.Title}) was created but remains hidden by filters. UI State: [{activeFiltersSummary}], Task State: [WorkflowState: {createdTask.WorkflowState}, AssignedTo: {createdTask.AssignedToUserId}]."
                                    _appLogger.LogError(diagError)
                                    Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "POST-CREATE FILTER ERROR", diagError, Forms.Common.AlertType.WarningAlert, actionText:="OK")

                                    ' Fallback: Select task directly to show details sidebar
                                    SelectTaskRow(createdTask)
                                End If
                            End If
                        End If

                        Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "TASK CREATED", "Task created successfully.", Forms.Common.AlertType.SuccessAlert, actionText:="OK")
                    End If
                End Using
            Catch ex As Exception
                _appLogger.LogError($"Task creation modal dialog error: {ex.Message}")
            Finally
                SetActionButtonsEnabled(True)
                _isOperationRunning = False
            End Try
        End Sub

        Private Sub btnOpenWorkflow_Click(sender As Object, e As EventArgs)
            If _isOperationRunning Then Return
            If _selectedTask IsNot Nothing Then
                OpenTaskWorkflowScreen(_selectedTask)
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

                Using dlg As New FrmEditTask(_selectedTask.TaskId, _taskService, _clientService, _userRepo, _appLogger)
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
                Dim confirmMsg = $"Complete {_selectedTask.Title}?{Environment.NewLine}{Environment.NewLine}Confirm that the return has been filed and all required work has been verified."
                Dim confirmRes = Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "Confirm Completion", confirmMsg, Forms.Common.AlertType.WarningAlert, actionText:="CONFIRM COMPLETE", showCancel:=True, cancelText:="CANCEL")

                If confirmRes = DialogResult.OK Then
                    Me.Cursor = Cursors.WaitCursor
                    Await _taskService.CompleteTaskAsync(_selectedTask.TaskId)
                    Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "Task Completed", $"Task '{_selectedTask.Title}' marked completed successfully.", Forms.Common.AlertType.SuccessAlert, actionText:="OK")
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
                Await LoadInitialDataAsync()
            Catch ex As Exception
                Forms.Common.FrmInAppAlert.ShowModal(Me.FindForm(), "Error", ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
            Finally
                SetActionButtonsEnabled(True)
                _isOperationRunning = False
            End Try
        End Sub
    End Class
End Namespace
