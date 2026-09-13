Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports StaffAutomation.BLL.Interfaces
Imports StaffAutomation.BLL.Logging
Imports StaffAutomation.BLL.Security
Imports StaffAutomation.BLL.Services
Imports StaffAutomation.Core
Imports StaffAutomation.Core.Configuration
Imports StaffAutomation.Core.Constants
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Logging
Imports StaffAutomation.Core.Security
Imports StaffAutomation.DAL.Configuration
Imports StaffAutomation.DAL.Core
Imports StaffAutomation.DAL.Interfaces
Imports StaffAutomation.DAL.Repositories
Imports StaffAutomation.WinForms.UIHelpers

Namespace Forms.Main
    ''' <summary>
    ''' Permanent Enterprise Desktop Shell for the CA Office Workforce Productivity Automation System.
    ''' Hosts header, dark left navigation, status bar, and central embedded workspace container.
    ''' Strictly non-MDI with zero floating child windows.
    ''' </summary>
    Public Class FrmMainShell
        Private ReadOnly _viewNavigator As IViewNavigator
        Private ReadOnly _authService As IAuthService
        Private ReadOnly _authzService As IAuthorizationService
        Private ReadOnly _navService As NavigationService
        Private ReadOnly _appLogger As IAppLogger = New AppLogger(New AppConfiguration())
        Private ReadOnly _attendanceService As IAttendanceService
        Private ReadOnly _taskService As ITaskManagementService
        
        Private _currentAttendanceRecord As Core.DTOs.AttendanceDto

        ' Navigation item metadata tracking
        Private ReadOnly _navItems As New List(Of NavItemControlInfo)()
        Private _selectedNavItem As Panel = Nothing

        ''' <summary>
        ''' Encapsulates navigation item control visual states and permission metadata.
        ''' </summary>
        Private Class NavItemControlInfo
            Public Property Key As String
            Public Property Caption As String
            Public Property IconSymbol As String
            Public Property HostPanel As Panel
            Public Property IndicatorPanel As Panel
            Public Property IconLabel As Label
            Public Property TextLabel As Label
            Public Property IsPermissionGranted As Boolean = True
            Public Property IsCollapsed As Boolean = False
        End Class

        ' System Tray Notification Controls
        Private trayIcon As NotifyIcon
        Private trayContextMenu As ContextMenuStrip
        Private _isExplicitExit As Boolean = False
        
        <System.Runtime.InteropServices.DllImport("user32.dll", SetLastError:=True, CharSet:=System.Runtime.InteropServices.CharSet.Auto)>
        Private Shared Function RegisterWindowMessage(lpString As String) As UInteger
        End Function

        <System.Runtime.InteropServices.DllImport("user32.dll")>
        Private Shared Function SetForegroundWindow(hWnd As IntPtr) As Boolean
        End Function

        <System.Runtime.InteropServices.DllImport("user32.dll")>
        Private Shared Function ShowWindow(hWnd As IntPtr, nCmdShow As Integer) As Boolean
        End Function

        Private Const SW_RESTORE As Integer = 9
        Private _wakeUpMessage As UInteger

        Public Enum ShellPendingAction
            None
            ExitApp
            Logout
        End Enum
        Public Property CurrentPendingAction As ShellPendingAction = ShellPendingAction.None

        Public Sub New(navigator As IViewNavigator)
            InitializeComponent()
            _wakeUpMessage = RegisterWindowMessage("StaffAutomation_WakeUp_Signal")
            Me.DoubleBuffered = True
            Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer, True)
            Me.UpdateStyles()
            _viewNavigator = navigator
            _authService = InitializeAuthService()
            _authzService = New AuthorizationService()
            _navService = New NavigationService(Me.pnlWorkspace)
            
            Dim config As IAppConfiguration = New AppConfiguration()
            Dim connFactory As IDatabaseConnectionFactory = New DbConnectionFactory(config)
            Dim sqlHelper As ISqlHelper = New SqlHelper(connFactory)
            Dim attendanceRepo As DAL.Interfaces.IAttendanceRepository = New AttendanceRepository(sqlHelper)
            Dim userRepo As DAL.Interfaces.IUserRepository = New UserRepository(sqlHelper)
            Dim auditLogger As New AuditLogger(sqlHelper)
            _attendanceService = New AttendanceService(attendanceRepo, userRepo, _appLogger, auditLogger)
            
            Dim taskRepo As DAL.Interfaces.ITaskRepository = New TaskRepository(sqlHelper)
            Dim clientRepo As DAL.Interfaces.IClientRepository = New ClientRepository(sqlHelper)
            Dim workflowEngine As ITaskWorkflowEngine = New TaskWorkflowEngine()
            _taskService = New TaskManagementService(taskRepo, workflowEngine, _appLogger, auditLogger, clientRepo, userRepo)
            
            AddHandler Me.btnHeaderAttendancePrimary.Click, AddressOf btnHeaderAttendancePrimary_Click
            AddHandler Me.btnHeaderAttendanceSecondary.Click, AddressOf btnHeaderAttendanceSecondary_Click
        End Sub

        Protected Overrides Sub WndProc(ByRef m As Message)
            If m.Msg = _wakeUpMessage Then
                Me.Invoke(Sub()
                              HandleDuplicateLaunchSignal()
                          End Sub)
            End If
            MyBase.WndProc(m)
        End Sub

        Private Async Sub HandleDuplicateLaunchSignal()
            ShowWindow(Me.Handle, SW_RESTORE)
            SetForegroundWindow(Me.Handle)
            
            If Me.WindowState = FormWindowState.Minimized Then
                Me.WindowState = FormWindowState.Normal
            End If

            If CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser.Role = UserRole.Employee Then
                Dim isOnBreak = CurrentUserContext.IsOnLunchBreak
                Dim stateStr = If(isOnBreak, "on lunch", "running")
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Instance Already Active", $"An existing workday instance is already {stateStr}. Resuming this instance.", Forms.Common.AlertType.InfoAlert, actionText:="OK", showCancel:=False)
                Await RefreshAttendanceWidgetStateAsync()
            End If
        End Sub


        Private Sub FrmMainShell_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            ' Enable double buffering on workspace host and shell panels to prevent repaint flicker
            ThemeConstants.EnableDoubleBuffering(Me.pnlWorkspace)
            ThemeConstants.EnableDoubleBuffering(Me.pnlNavigation)
            ThemeConstants.EnableDoubleBuffering(Me.pnlHeader)
            

            ' Ensure StatusStrip sits at true bottom z-order below fill panel
            Me.statusStripShell.SendToBack()

            InitializeShiftLogo()
            ApplyEnterpriseTheme()
            PopulateHeaderAndStatusSessionInfo()
            BuildLeftNavigationMenu()
            InitializeSystemTray()

            ' Initially load Dashboard MVP control into workspace based on role
            NavigateToModule("Dashboard")
        End Sub
        
        Private Async Sub FrmMainShell_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
            Try
                If CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser.Role = UserRole.Employee Then
                    Dim currentUserId = CurrentUserContext.CurrentUser.UserId
                    Dim todayRecord = Await _attendanceService.GetTodayAttendanceForUserAsync(currentUserId)
                    
                    If todayRecord Is Nothing OrElse todayRecord.ClockInTime = DateTime.MinValue Then
                        Dim tasks = Await _taskService.GetTasksForAssignedUserAsync(currentUserId)
                        Dim newTasksCount = tasks.Where(Function(t) t.WorkflowState = TaskWorkflowState.NewTask OrElse t.WorkflowState = TaskWorkflowState.Assigned).Count()
                        Dim overdueCount = tasks.Where(Function(t) t.WorkflowState <> TaskWorkflowState.Completed AndAlso t.WorkflowState <> TaskWorkflowState.Closed AndAlso t.WorkflowState <> TaskWorkflowState.Cancelled AndAlso t.IsOverdue).Count()

                        Dim message As String = $"Good Morning, {CurrentUserContext.CurrentUser.FullName}! 👋" & Environment.NewLine & Environment.NewLine
                        message &= $"You have {newTasksCount} new tasks requiring your attention today." & Environment.NewLine
                        If overdueCount > 0 Then
                            message &= $"You also have {overdueCount} overdue tasks to catch up on." & Environment.NewLine
                        End If
                        message &= Environment.NewLine & "Please punch in to start your shift and unlock your workspace."
                        
                        Dim res As DialogResult
                        Do
                            res = Forms.Common.FrmInAppAlert.ShowModal(Me, "Start Your Workday", message, Forms.Common.AlertType.InfoAlert, actionText:="PUNCH IN && VIEW MY TASKS", showCancel:=False)
                        Loop Until res = DialogResult.OK
                        
                        Await _attendanceService.ClockInUserAsync(currentUserId, "127.0.0.1")
                        Forms.Common.DataStateTracker.MarkAttendanceChanged()
                        Await RefreshAttendanceWidgetStateAsync()
                        NavigateToModule("Dashboard")
                    End If
                End If
            Catch ex As Exception
                _appLogger.LogError("Error in FrmMainShell_Shown login prompt.", "FrmMainShell", ex)
            End Try
        End Sub

        ''' <summary>
        ''' Initializes and loads the approved SHIFT logo into the header PictureBox.
        ''' Falls back cleanly to neutral SHIFT text label if image is unreadable (NO CA branding).
        ''' </summary>
        Private Sub InitializeShiftLogo()
            Try
                Dim targetResDir = System.IO.Path.Combine(Application.StartupPath, "Resources")
                Dim targetLogoPath = System.IO.Path.Combine(targetResDir, "shift_logo.png")

                Dim candidatePaths As String() = {
                    targetLogoPath,
                    System.IO.Path.Combine(Application.StartupPath, "shift_logo.png"),
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Resources", "shift_logo.png"),
                    "C:\Users\user\.gemini\antigravity-ide\brain\444bcbd7-9748-406a-98cf-9cd8adc8fefb\media__1787936164425.png"
                }

                Dim foundPath As String = candidatePaths.FirstOrDefault(Function(p) System.IO.File.Exists(p))

                If Not String.IsNullOrEmpty(foundPath) Then
                    Try
                        If Not System.IO.Directory.Exists(targetResDir) Then System.IO.Directory.CreateDirectory(targetResDir)
                        If Not String.Equals(foundPath, targetLogoPath, StringComparison.OrdinalIgnoreCase) AndAlso Not System.IO.File.Exists(targetLogoPath) Then
                            System.IO.File.Copy(foundPath, targetLogoPath, True)
                            foundPath = targetLogoPath
                        End If
                    Catch
                    End Try

                    Using fs As New System.IO.FileStream(foundPath, System.IO.FileMode.Open, System.IO.FileAccess.Read)
                        picHeaderLogo.Image = Image.FromStream(fs)
                    End Using
                    picHeaderLogo.Visible = True
                    lblHeaderLogo.Visible = False
                    Return
                End If
            Catch ex As Exception
            End Try

            ' Neutral SHIFT text fallback if image cannot load (NO CA branding)
            lblHeaderLogo.Text = "SHIFT"
            lblHeaderLogo.BackColor = ThemeConstants.PrimaryAccent
            lblHeaderLogo.ForeColor = Color.White
            lblHeaderLogo.Visible = True
            picHeaderLogo.Visible = False
        End Sub

        ''' <summary>
        ''' Applies centralized ThemeConstants styling across all shell containers.
        ''' </summary>
        Private Sub ApplyEnterpriseTheme()
            Me.BackColor = ThemeConstants.WorkspaceBackground
            Me.pnlHeader.BackColor = ThemeConstants.HeaderBackground
            Me.pnlNavigation.BackColor = ThemeConstants.NavigationBackground
            Me.pnlWorkspace.BackColor = ThemeConstants.WorkspaceBackground
            Me.statusStripShell.BackColor = ThemeConstants.StatusBackground

            ' Logo Emblem Styling
            lblHeaderLogo.BackColor = ThemeConstants.PrimaryAccent
            lblHeaderLogo.ForeColor = ThemeConstants.HeaderForeground

            ' Header Labels
            lblHeaderAppName.ForeColor = ThemeConstants.HeaderForeground
            lblHeaderBranch.ForeColor = ThemeConstants.HeaderSubText
            lblHeaderUser.ForeColor = ThemeConstants.HeaderForeground
            lblHeaderRoleDept.ForeColor = ThemeConstants.HeaderSubText
            lblHeaderClock.ForeColor = ThemeConstants.HeaderForeground

            ' Header Action Buttons
            btnHeaderNotifications.BackColor = ThemeConstants.HeaderBadgeBg
            btnHeaderNotifications.ForeColor = ThemeConstants.HeaderForeground
            btnHeaderNotifications.FlatAppearance.BorderColor = ThemeConstants.NavigationItemHover

            btnHeaderLogout.BackColor = ThemeConstants.HeaderBadgeBg
            btnHeaderLogout.ForeColor = ThemeConstants.HeaderForeground
            btnHeaderLogout.FlatAppearance.BorderColor = ThemeConstants.NavigationItemHover

            btnHeaderExit.BackColor = ThemeConstants.DangerRed
            btnHeaderExit.ForeColor = Color.White
            btnHeaderExit.FlatAppearance.BorderSize = 0

            ' Navigation Header Title
            pnlNavHeader.BackColor = ThemeConstants.NavigationBackground
            lblNavHeaderTitle.ForeColor = ThemeConstants.NavigationMutedText

            ' Status Strip Items Styling
            For Each item As ToolStripItem In statusStripShell.Items
                item.ForeColor = ThemeConstants.StatusForeground
            Next
        End Sub

        ''' <summary>
        ''' Populates header and status bar labels with authenticated session data.
        ''' </summary>
        Private Sub PopulateHeaderAndStatusSessionInfo()
            Dim serverDataSource As String = GetActiveDatabaseDataSource()

            If CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser IsNot Nothing Then
                Dim user = CurrentUserContext.CurrentUser
                lblHeaderUser.Text = $"👤 {user.FullName} | Role: {user.Role}"

                lblStatusUser.Text = $"| User: {user.FullName}"
                lblStatusRole.Text = $"| Role: {user.Role}"
                lblStatusCompany.Text = "| CA Office Automation"
                lblStatusDb.Text = "DB: Connected"
                lblStatusServer.Text = $"| Server: {serverDataSource}"
                lblStatusFY.Text = "| FY: 2026-27"
                lblStatusVersion.Text = $"| Version: v{AppConstants.AppVersion}"
            Else
                lblHeaderUser.Text = "👤 System Administrator | Role: Admin"

                lblStatusUser.Text = "| User: System Administrator"
                lblStatusRole.Text = "| Role: Admin"
                lblStatusServer.Text = $"| Server: {serverDataSource}"
            End If

            UpdateClockAndMemoryStatus()
            
            ' Run this asynchronously without awaiting to avoid blocking UI thread
            Call RefreshAttendanceWidgetStateAsync()
        End Sub

        ''' <summary>
        ''' Dynamically parses active SQL Server DataSource from connection string.
        ''' </summary>
        Private Function GetActiveDatabaseDataSource() As String
            Try
                Dim config As IAppConfiguration = New AppConfiguration()
                Dim connStr = config.GetConnectionString("StaffAutomationDb")
                If Not String.IsNullOrWhiteSpace(connStr) Then
                    Dim builder As New Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connStr)
                    If Not String.IsNullOrWhiteSpace(builder.DataSource) Then
                        Return builder.DataSource
                    End If
                End If
            Catch
            End Try
            Return "LOCALHOST\SQLEXPRESS"
        End Function

        ''' <summary>
        ''' Builds the 8 reusable navigation item controls in exact top-to-bottom order.
        ''' </summary>
        Private Sub BuildLeftNavigationMenu()
            pnlNavigation.SuspendLayout()

            ' Clear existing navigation items except header
            For i As Integer = pnlNavigation.Controls.Count - 1 To 0 Step -1
                If pnlNavigation.Controls(i) IsNot pnlNavHeader Then
                    Dim ctrl = pnlNavigation.Controls(i)
                    pnlNavigation.Controls.RemoveAt(i)
                    ctrl.Dispose()
                End If
            Next
            _navItems.Clear()

            ' Define 8 mandatory navigation menu items in exact TOP TO BOTTOM order
            Dim menuDefs As New List(Of (Key As String, Caption As String, Icon As String, AllowedRoles As UserRole())) From {
                ("Dashboard", "Dashboard", "📊", {UserRole.Owner, UserRole.Admin, UserRole.Employee}),
                ("Tasks", "Daily Tasks", "📋", {UserRole.Owner, UserRole.Admin, UserRole.Employee}),
                ("Clients", "Clients", "💼", {UserRole.Owner, UserRole.Admin}),
                ("Attendance", "Attendance", "⏱️", {UserRole.Owner, UserRole.Admin}),
                ("Reports", "Reports", "📈", {UserRole.Owner, UserRole.Admin}),
                ("Settings", "Settings", "🔧", {UserRole.Owner, UserRole.Admin, UserRole.Employee}),
                ("Help", "Help", "❓", {UserRole.Owner, UserRole.Admin, UserRole.Employee})
            }

            ' Add items top-to-bottom under pnlNavHeader
            Dim currentTop As Integer = pnlNavHeader.Height

            For Each menuDef In menuDefs
                Dim itemInfo = CreateNavigationItemControl(menuDef.Key, menuDef.Caption, menuDef.Icon, currentTop)

                ' Check role permission
                If CurrentUserContext.IsAuthenticated Then
                    Dim userRole = CurrentUserContext.CurrentUser.Role
                    Dim isPermitted As Boolean = Array.Exists(menuDef.AllowedRoles, Function(r) r = userRole)
                    itemInfo.IsPermissionGranted = isPermitted
                    itemInfo.HostPanel.Enabled = isPermitted
                    If Not isPermitted Then
                        itemInfo.TextLabel.ForeColor = ThemeConstants.NavigationMutedText
                        itemInfo.IconLabel.ForeColor = ThemeConstants.NavigationMutedText
                    End If
                End If

                _navItems.Add(itemInfo)
                pnlNavigation.Controls.Add(itemInfo.HostPanel)
                itemInfo.HostPanel.BringToFront()
                currentTop += itemInfo.HostPanel.Height + 2
            Next

            pnlNavigation.ResumeLayout(True)

            ' Select Dashboard by default
            If _navItems.Count > 0 Then
                SelectNavigationItem(_navItems(0))
            End If
        End Sub

        ''' <summary>
        ''' Instantiates a custom navigation item control panel.
        ''' </summary>
        Private Function CreateNavigationItemControl(key As String, caption As String, icon As String, topPos As Integer) As NavItemControlInfo
            Dim pnlItem As New Panel() With {
                .Name = $"pnlNav_{key}",
                .Size = New Size(220, 42),
                .Location = New Point(0, topPos),
                .BackColor = ThemeConstants.NavigationBackground,
                .Cursor = Cursors.Hand
            }
            ThemeConstants.EnableDoubleBuffering(pnlItem)

            Dim pnlIndicator As New Panel() With {
                .Name = $"pnlInd_{key}",
                .Size = New Size(4, 42),
                .Dock = DockStyle.Left,
                .BackColor = ThemeConstants.NavigationBackground
            }

            Dim lblIcon As New Label() With {
                .Name = $"lblIcon_{key}",
                .Text = icon,
                .Font = New Font("Segoe UI Emoji", 11.0!, FontStyle.Regular),
                .ForeColor = ThemeConstants.NavigationText,
                .Size = New Size(34, 42),
                .Dock = DockStyle.Left,
                .TextAlign = ContentAlignment.MiddleCenter,
                .Cursor = Cursors.Hand
            }

            Dim lblCaption As New Label() With {
                .Name = $"lblCap_{key}",
                .Text = caption,
                .Font = New Font(ThemeConstants.FontNameDefault, 9.5!, FontStyle.Regular),
                .ForeColor = ThemeConstants.NavigationText,
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleLeft,
                .Cursor = Cursors.Hand
            }

            pnlItem.Controls.Add(lblCaption)
            pnlItem.Controls.Add(lblIcon)
            pnlItem.Controls.Add(pnlIndicator)

            Dim itemInfo As New NavItemControlInfo() With {
                .Key = key,
                .Caption = caption,
                .IconSymbol = icon,
                .HostPanel = pnlItem,
                .IndicatorPanel = pnlIndicator,
                .IconLabel = lblIcon,
                .TextLabel = lblCaption
            }

            ' Wire mouse hover & click event handlers recursively across item controls
            AddHandler pnlItem.MouseEnter, Sub(s, e) OnNavItemMouseEnter(itemInfo)
            AddHandler lblIcon.MouseEnter, Sub(s, e) OnNavItemMouseEnter(itemInfo)
            AddHandler lblCaption.MouseEnter, Sub(s, e) OnNavItemMouseEnter(itemInfo)
            AddHandler pnlIndicator.MouseEnter, Sub(s, e) OnNavItemMouseEnter(itemInfo)

            AddHandler pnlItem.MouseLeave, Sub(s, e) OnNavItemMouseLeave(itemInfo)
            AddHandler lblIcon.MouseLeave, Sub(s, e) OnNavItemMouseLeave(itemInfo)
            AddHandler lblCaption.MouseLeave, Sub(s, e) OnNavItemMouseLeave(itemInfo)
            AddHandler pnlIndicator.MouseLeave, Sub(s, e) OnNavItemMouseLeave(itemInfo)

            AddHandler pnlItem.Click, Sub(s, e) OnNavItemClick(itemInfo)
            AddHandler lblIcon.Click, Sub(s, e) OnNavItemClick(itemInfo)
            AddHandler lblCaption.Click, Sub(s, e) OnNavItemClick(itemInfo)
            AddHandler pnlIndicator.Click, Sub(s, e) OnNavItemClick(itemInfo)

            Return itemInfo
        End Function

        Private Sub OnNavItemMouseEnter(item As NavItemControlInfo)
            If Not item.IsPermissionGranted Then Return
            If _selectedNavItem IsNot item.HostPanel Then
                item.HostPanel.BackColor = ThemeConstants.NavigationItemHover
            End If
        End Sub

        Private Sub OnNavItemMouseLeave(item As NavItemControlInfo)
            If Not item.IsPermissionGranted Then Return
            If _selectedNavItem IsNot item.HostPanel Then
                item.HostPanel.BackColor = ThemeConstants.NavigationBackground
            End If
        End Sub

        Private Async Sub OnNavItemClick(item As NavItemControlInfo)
            ' Server-side action-boundary authorization enforcement
            If item.Key.Equals("Admin", StringComparison.OrdinalIgnoreCase) Then
                If Not _authzService.IsAuthorized(UserRole.Admin) Then
                    Forms.Common.FrmInAppAlert.ShowModal(Me, "Access Denied", "Access Denied: Administration requires System Administrator privileges.", Forms.Common.AlertType.WarningAlert, actionText:="OK")
                    Return
                End If
            ElseIf item.Key.Equals("Reports", StringComparison.OrdinalIgnoreCase) OrElse item.Key.Equals("Clients", StringComparison.OrdinalIgnoreCase) Then
                If Not _authzService.IsAuthorizedAny(UserRole.Admin, UserRole.Owner) Then
                    Forms.Common.FrmInAppAlert.ShowModal(Me, "Access Denied", $"Access Denied: Access to {item.Caption} requires Owner or Admin privileges.", Forms.Common.AlertType.WarningAlert, actionText:="OK")
                    Return
                End If
            End If

            ' Navigation Gate for Employee Shift Status
            If CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser.Role = UserRole.Employee Then
                If item.Key.Equals("Tasks", StringComparison.OrdinalIgnoreCase) OrElse 
                   item.Key.Equals("Clients", StringComparison.OrdinalIgnoreCase) OrElse 
                   item.Key.Equals("Reports", StringComparison.OrdinalIgnoreCase) Then
                   
                    Dim isClockedIn = CurrentUserContext.IsClockedIn
                    Dim isOnBreak = CurrentUserContext.IsOnLunchBreak
                    Dim isCompleted = CurrentUserContext.IsWorkdayCompleted

                    If Not isClockedIn OrElse isOnBreak OrElse isCompleted Then
                        Dim stateReason As String = "You must punch in to start your shift before accessing this module."
                        Dim actionBtnText As String = "OK"
                        Dim showCancelBtn As Boolean = False
                        Dim isPunchAction As Boolean = False
                        
                        If Not isClockedIn Then
                            actionBtnText = "Punch In Now"
                            showCancelBtn = True
                            isPunchAction = True
                        ElseIf isOnBreak Then
                            stateReason = "You are currently on lunch break. Please resume work to access this module."
                        ElseIf isCompleted Then
                            stateReason = "Your shift has ended for today."
                        End If
                        
                        Dim res = Forms.Common.FrmInAppAlert.ShowModal(Me, "Access Denied", $"Access Denied: Active Shift Required.{vbCrLf}{vbCrLf}{stateReason}", Forms.Common.AlertType.WarningAlert, actionText:=actionBtnText, showCancel:=showCancelBtn, cancelText:="Cancel")
                        
                        If isPunchAction AndAlso res = DialogResult.OK Then
                                Try
                                    Dim config As IAppConfiguration = New AppConfiguration()
                                    Dim connFactory As IDatabaseConnectionFactory = New DbConnectionFactory(config)
                                    Dim sqlHelper As ISqlHelper = New SqlHelper(connFactory)
                                    Dim appLogger As IAppLogger = New AppLogger(config)
                                    Dim auditLogger As New AuditLogger(sqlHelper)
                                    Dim userRepo As IUserRepository = New UserRepository(sqlHelper)
                                    Dim attendanceRepo As IAttendanceRepository = New AttendanceRepository(sqlHelper)
                                    Dim attendanceService As IAttendanceService = New AttendanceService(attendanceRepo, userRepo, appLogger, auditLogger, Nothing)

                                Await attendanceService.ClockInUserAsync(CurrentUserContext.CurrentUser.UserId, "127.0.0.1")
                                Forms.Common.DataStateTracker.MarkAttendanceChanged()
                                CurrentUserContext.UpdateShiftState(True, False, False)
                                
                                ' Must explicitly refresh the widget state so the shell header correctly updates
                                Await RefreshAttendanceWidgetStateAsync()
                            Catch ex As Exception
                                Forms.Common.FrmInAppAlert.ShowModal(Me, "Error", $"Failed to punch in: {ex.Message}", Forms.Common.AlertType.ErrorAlert, actionText:="OK")
                                Return
                            End Try
                        Else
                            ' Cancelled or not punch action, simply return
                            Return
                        End If
                    End If
                End If
            End If

            If Not item.Key.Equals("Attendance", StringComparison.OrdinalIgnoreCase) Then
                CurrentPendingAction = ShellPendingAction.None
            End If

            SelectNavigationItem(item)

            ' Route to MVP business module views using ultra-fast non-blocking async navigation
            Select Case item.Key
                Case "Dashboard"
                    Dim isEmployee = CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser.Role = UserRole.Employee
                    If isEmployee Then
                        Await _navService.NavigateToKeyAsync("Dashboard_Staff", Function() New EmployeeDashboardControl(Me))
                    Else
                        Await _navService.NavigateToKeyAsync("Dashboard_Admin", Function() New DashboardControl(Me))
                    End If
                Case "Tasks"
                    Await _navService.NavigateToKeyAsync("Tasks", Function() Factory.CreateDailyTasksControl(Me))
                Case "Clients"
                    Await _navService.NavigateToKeyAsync("Clients", Function() New ClientsControl())
                Case "Attendance"
                    Await _navService.NavigateToKeyAsync("Attendance_Admin", Function() New Forms.Attendance.AdminAttendanceBoardControl())
                Case "Reports"
                    Await _navService.NavigateToKeyAsync("Reports", Function() New ReportsControl())
                Case "Admin"
                    Await _navService.NavigateToKeyAsync("Admin", Function() New Forms.Admin.AdministrationControl(Me))
                Case "Settings"
                    Await _navService.NavigateToKeyAsync("Settings", Function() New SettingsControl())
                Case "Help"
                    Await _navService.NavigateToKeyAsync("Help", Function() New HelpControl())
                Case Else
                    LoadWelcomeScreen()
            End Select
        End Sub

        ''' <summary>
        ''' Synchronizes left navigation sidebar item highlighting based on unique module key.
        ''' </summary>
        Public Sub SelectNavigationItemByKey(key As String)
            Dim item = _navItems.Find(Function(n) n.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
            If item IsNot Nothing Then
                SelectNavigationItem(item)
            End If
        End Sub

        ' Static Font Cache for Zero-Allocation Navigation Highlighting
        Private ReadOnly _navFontBold As New Font(ThemeConstants.FontNameDefault, 9.5!, FontStyle.Bold)
        Private ReadOnly _navFontRegular As New Font(ThemeConstants.FontNameDefault, 9.5!, FontStyle.Regular)

        ''' <summary>
        ''' Highlights the selected navigation menu item and resets unselected items atomically without glitch/flicker.
        ''' </summary>
        Private Sub SelectNavigationItem(selectedItem As NavItemControlInfo)
            If selectedItem Is Nothing Then Return

            If pnlNavigation IsNot Nothing AndAlso pnlNavigation.IsHandleCreated Then
                WorkspaceLoader.NativeMethods.SendMessage(pnlNavigation.Handle, WorkspaceLoader.NativeMethods.WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero)
            End If

            Try
                For Each item In _navItems
                    If item.HostPanel Is selectedItem.HostPanel Then
                        item.HostPanel.BackColor = ThemeConstants.NavigationItemSelected
                        item.IndicatorPanel.BackColor = ThemeConstants.NavigationActiveIndicator
                        item.TextLabel.Font = _navFontBold
                        item.TextLabel.ForeColor = Color.White
                        item.IconLabel.ForeColor = Color.White
                        _selectedNavItem = item.HostPanel
                    Else
                        item.HostPanel.BackColor = ThemeConstants.NavigationBackground
                        item.IndicatorPanel.BackColor = ThemeConstants.NavigationBackground
                        item.TextLabel.Font = _navFontRegular
                        item.TextLabel.ForeColor = If(item.IsPermissionGranted, ThemeConstants.NavigationText, ThemeConstants.NavigationMutedText)
                        item.IconLabel.ForeColor = If(item.IsPermissionGranted, ThemeConstants.NavigationText, ThemeConstants.NavigationMutedText)
                    End If
                Next
            Finally
                If pnlNavigation IsNot Nothing AndAlso pnlNavigation.IsHandleCreated Then
                    WorkspaceLoader.NativeMethods.SendMessage(pnlNavigation.Handle, WorkspaceLoader.NativeMethods.WM_SETREDRAW, New IntPtr(1), IntPtr.Zero)
                End If
                pnlNavigation.Invalidate(True)
                pnlNavigation.Update()
            End Try
        End Sub

        ''' <summary>
        ''' Public module routing trigger called by Dashboard quick action shortcuts and Administration cards.
        ''' </summary>
        Public Sub NavigateToModule(moduleKey As String)
            Dim item = _navItems.Find(Function(n) n.Key.Equals(moduleKey, StringComparison.OrdinalIgnoreCase))
            If item IsNot Nothing Then
                OnNavItemClick(item)
            End If
        End Sub

        ''' <summary>
        ''' Routes to Daily Tasks workspace with optional filter preset applied.
        ''' </summary>
        Public Async Sub NavigateToTasks(Optional filterPreset As String = "")
            Try
                Dim ctrl = Await _navService.NavigateToKeyAsync("Tasks", Function() Factory.CreateDailyTasksControl(Me))
                If ctrl Is Nothing Then Return

                Dim tasksControl = TryCast(ctrl, Forms.Tasks.DailyTasksControl)
                If tasksControl IsNot Nothing AndAlso Not String.IsNullOrEmpty(filterPreset) Then
                    tasksControl.ApplyFilterPreset(filterPreset)
                End If

                Dim item = _navItems.Find(Function(n) n.Key.Equals("Tasks", StringComparison.OrdinalIgnoreCase))
                If item IsNot Nothing Then SelectNavigationItem(item)
            Catch oex As OperationCanceledException
                _appLogger.LogInfo("[Navigation] Navigation to 'Tasks' was superseded or cancelled naturally.")
            Catch ex As Exception
                _appLogger.LogError($"[NavigationError] Unexpected infrastructure failure navigating to Daily Tasks: {ex.GetType().FullName} - {ex.Message}", "FrmMainShell", ex)
                
                Dim correlationId = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()
                Dim safeMsg = $"An unexpected error occurred while loading the Daily Tasks view.{Environment.NewLine}{Environment.NewLine}" &
                              $"Correlation ID: {correlationId}{Environment.NewLine}" &
                              $"Please contact support if this issue persists."
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Navigation Error", safeMsg, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
            End Try
        End Sub

        ''' <summary>
        ''' Routes to Clients workspace with optional Needs Verification filter applied.
        ''' </summary>
        Public Async Sub NavigateToClients(Optional filterNeedsVerification As Boolean = False)
            Try
                Dim ctrl = Await _navService.NavigateToKeyAsync("Clients", Function() New ClientsControl(filterNeedsVerification))
                If ctrl Is Nothing Then Return

                Dim clientsControl = TryCast(ctrl, ClientsControl)
                If clientsControl IsNot Nothing Then
                    clientsControl.ApplyNeedsVerificationFilter(filterNeedsVerification)
                End If

                Dim item = _navItems.Find(Function(n) n.Key.Equals("Clients", StringComparison.OrdinalIgnoreCase))
                If item IsNot Nothing Then SelectNavigationItem(item)
            Catch oex As OperationCanceledException
                _appLogger.LogInfo("[Navigation] Navigation to 'Clients' was superseded or cancelled naturally.")
            Catch ex As Exception
                _appLogger.LogError($"[NavigationError] Unexpected infrastructure failure navigating to Clients: {ex.GetType().FullName} - {ex.Message}", "FrmMainShell", ex)
                
                Dim correlationId = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()
                Dim safeMsg = $"An unexpected error occurred while loading the Clients view.{Environment.NewLine}{Environment.NewLine}" &
                              $"Correlation ID: {correlationId}{Environment.NewLine}" &
                              $"Please contact support if this issue persists."
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Navigation Error", safeMsg, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
            End Try
        End Sub

        ''' <summary>
        ''' Embeds the WelcomeControl UserControl into pnlWorkspace.
        ''' </summary>
        Private Sub LoadWelcomeScreen()
            Dim welcomeUserControl As New WelcomeControl()
            _navService.LoadScreen(welcomeUserControl)
        End Sub

        ''' <summary>
        ''' Public reusable screen loading method for UserControls and module Forms.
        ''' </summary>
        Public Sub LoadScreen(viewControl As Control)
            _navService.LoadScreen(viewControl)
        End Sub

        Public Sub LoadScreen(childForm As Form)
            _navService.LoadScreen(childForm)
        End Sub

        ''' <summary>
        ''' Real-time clock update tick firing every 1000ms.
        ''' </summary>
        Private Sub tmrClock_Tick(sender As Object, e As EventArgs) Handles tmrClock.Tick
            UpdateClockAndMemoryStatus()
        End Sub

        Private Sub UpdateClockAndMemoryStatus()
            Dim now = DateTime.Now
            lblHeaderClock.Text = $"🕒 {now.ToString("hh:mm tt")}"
            lblHeaderClock.Text = DateTime.Now.ToString("dd MMM yyyy - hh:mm:ss tt")

            ' Update Attendance Widget Timer
            If CurrentUserContext.IsAuthenticated AndAlso _currentAttendanceRecord IsNot Nothing AndAlso Not _currentAttendanceRecord.ClockOutTime.HasValue Then
                If _currentAttendanceRecord.BreakStartTime.HasValue Then
                    Dim duration = DateTime.Now - _currentAttendanceRecord.BreakStartTime.Value
                    lblHeaderAttendanceStatus.Text = $"On Lunch - {duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}"
                Else
                    Dim totalBreaks = TimeSpan.FromMinutes(_currentAttendanceRecord.TotalBreakMinutes)
                    Dim duration = DateTime.Now - _currentAttendanceRecord.ClockInTime - totalBreaks
                    lblHeaderAttendanceStatus.Text = $"Working - {duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}"
                End If
            End If

            ' Periodic system memory GC monitor (Optional safety feature for long-running winforms)age
            Dim memoryBytes = GC.GetTotalMemory(False)
            Dim memoryMb As Double = Math.Round(memoryBytes / 1024.0 / 1024.0, 1)
                        lblStatusMemory.Text = $"| Memory: {memoryMb} MB"
        End Sub

        ''' <summary>
        ''' Dynamically updates Logout and Exit button states based on attendance shift status.
        ''' Ensures Exit/Logout buttons are visually enabled so they can trigger the guard prompt.
        ''' </summary>
        Public Sub UpdateLogoutButtonState(isClockedIn As Boolean, isWorkdayCompleted As Boolean)
            ' Leave buttons enabled so the user can click them and get the strict warning prompt
            btnHeaderLogout.Enabled = True
            btnHeaderLogout.Text = "Logout"
            btnHeaderLogout.BackColor = ThemeConstants.HeaderBadgeBg
            btnHeaderLogout.ForeColor = ThemeConstants.HeaderForeground
            btnHeaderLogout.Cursor = Cursors.Hand

            btnHeaderExit.Visible = True
            btnHeaderExit.Enabled = True
            btnHeaderExit.Text = "Exit"
            btnHeaderExit.BackColor = ThemeConstants.DangerRed
            btnHeaderExit.ForeColor = Color.White
            btnHeaderExit.Cursor = Cursors.Hand
        End Sub

        Private Async Function RefreshAttendanceWidgetStateAsync() As Task
            Try
                If CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser IsNot Nothing Then
                    Dim userId = CurrentUserContext.CurrentUser.UserId
                    _currentAttendanceRecord = Await _attendanceService.GetTodayAttendanceForUserAsync(userId)
                    
                    Dim isClockedIn = (_currentAttendanceRecord IsNot Nothing)
                    Dim isOnBreak = (_currentAttendanceRecord IsNot Nothing AndAlso _currentAttendanceRecord.BreakStartTime.HasValue)
                    Dim isCompleted = (_currentAttendanceRecord IsNot Nothing AndAlso _currentAttendanceRecord.ClockOutTime.HasValue)

                    ' Update in-memory session shift state
                    CurrentUserContext.UpdateShiftState(isClockedIn, isOnBreak, isCompleted)
                    UpdateLogoutButtonState(isClockedIn, isCompleted)
                    
                    Dim isEmployee = CurrentUserContext.CurrentUser.Role = UserRole.Employee
                    
                    If _currentAttendanceRecord Is Nothing OrElse _currentAttendanceRecord.ClockInTime = DateTime.MinValue Then
                        lblHeaderAttendanceStatus.Text = "Not clocked in"
                        lblHeaderAttendanceStatus.ForeColor = ThemeConstants.TextMuted
                        If isEmployee Then
                            btnHeaderAttendancePrimary.Visible = True
                            btnHeaderAttendancePrimary.Text = "Punch In"
                            btnHeaderAttendancePrimary.BackColor = ThemeConstants.SuccessGreen
                            btnHeaderAttendanceSecondary.Visible = False
                        Else
                            btnHeaderAttendancePrimary.Visible = False
                            btnHeaderAttendanceSecondary.Visible = False
                        End If
                    ElseIf _currentAttendanceRecord.ClockOutTime.HasValue Then
                        lblHeaderAttendanceStatus.Text = "Shift completed"
                        lblHeaderAttendanceStatus.ForeColor = ThemeConstants.TextMuted
                        btnHeaderAttendancePrimary.Visible = False
                        btnHeaderAttendanceSecondary.Visible = False
                    ElseIf _currentAttendanceRecord.BreakStartTime.HasValue Then
                        lblHeaderAttendanceStatus.Text = $"On Lunch ({_currentAttendanceRecord.BreakStartTime.Value.ToString("HH:mm")})"
                        lblHeaderAttendanceStatus.ForeColor = ThemeConstants.WarningOrange
                        If isEmployee Then
                            btnHeaderAttendancePrimary.Visible = True
                            btnHeaderAttendancePrimary.Text = "Resume Work"
                            btnHeaderAttendancePrimary.BackColor = ThemeConstants.SuccessGreen
                            btnHeaderAttendanceSecondary.Visible = False
                        Else
                            btnHeaderAttendancePrimary.Visible = False
                            btnHeaderAttendanceSecondary.Visible = False
                        End If
                    Else
                        Dim span As TimeSpan = DateTime.Now - _currentAttendanceRecord.ClockInTime
                        lblHeaderAttendanceStatus.Text = $"Working ({span.Hours:D2}:{span.Minutes:D2})"
                        lblHeaderAttendanceStatus.ForeColor = ThemeConstants.SuccessGreen
                        If isEmployee Then
                            If _currentAttendanceRecord.TotalBreakMinutes > 0 Then
                                ' WORKING AFTER RESUME: Show ONLY Punch Out
                                btnHeaderAttendancePrimary.Visible = False
                                btnHeaderAttendanceSecondary.Visible = True
                                btnHeaderAttendanceSecondary.Text = "Punch Out"
                                btnHeaderAttendanceSecondary.BackColor = ThemeConstants.DangerRed
                            Else
                                ' WORKING BEFORE LUNCH: Show LUNCH OUT and PUNCH OUT
                                btnHeaderAttendancePrimary.Visible = True
                                btnHeaderAttendancePrimary.Text = "Lunch Out"
                                btnHeaderAttendancePrimary.BackColor = ThemeConstants.WarningOrange
                                btnHeaderAttendanceSecondary.Visible = True
                                btnHeaderAttendanceSecondary.Text = "Punch Out"
                                btnHeaderAttendanceSecondary.BackColor = ThemeConstants.DangerRed
                            End If
                        Else
                            btnHeaderAttendancePrimary.Visible = False
                            btnHeaderAttendanceSecondary.Visible = False
                        End If
                    End If
                End If
            Catch ex As Exception
                _appLogger.LogError("Failed to refresh attendance widget.", "FrmMainShell", ex)
            End Try
        End Function

        Private Async Sub btnHeaderAttendancePrimary_Click(sender As Object, e As EventArgs)
            If Not CurrentUserContext.IsAuthenticated Then Return
            
            Dim userId As Integer = CurrentUserContext.CurrentUser.UserId
            
            Try
                If _currentAttendanceRecord Is Nothing Then
                    Await _attendanceService.ClockInUserAsync(userId, "127.0.0.1")
                    Await RefreshAttendanceWidgetStateAsync()
                    ReloadCurrentScreenIfAffected()
                ElseIf _currentAttendanceRecord.BreakStartTime.HasValue Then
                    Await _attendanceService.EndLunchBreakAsync(userId)
                    Await RefreshAttendanceWidgetStateAsync()
                    ReloadCurrentScreenIfAffected()
                Else
                    Await _attendanceService.StartLunchBreakAsync(userId)
                    Await RefreshAttendanceWidgetStateAsync()
                    Me.WindowState = FormWindowState.Minimized
                    Me.Hide()
                    If trayIcon IsNot Nothing Then
                        trayIcon.ShowBalloonTip(3000, "On Lunch Break", "You're on Lunch Break. You can resume your work from the system tray when you return.", ToolTipIcon.Info)
                    End If
                End If
            Catch ex As BusinessException
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Attendance Warning", ex.Message, Forms.Common.AlertType.WarningAlert, actionText:="OK")
                Call RefreshAttendanceWidgetStateAsync()
            Catch ex As Exception
                _appLogger.LogError("Unexpected error during attendance operation", "FrmMainShell", ex)
                Forms.Common.FrmInAppAlert.ShowModal(Me, "System Error", "An unexpected error occurred processing your attendance. Please try again or contact IT support.", Forms.Common.AlertType.ErrorAlert, actionText:="OK")
                Call RefreshAttendanceWidgetStateAsync()
            End Try
        End Sub

        Private Async Sub btnHeaderAttendanceSecondary_Click(sender As Object, e As EventArgs)
            If Not CurrentUserContext.IsAuthenticated Then Return
            
            Try
                If _currentAttendanceRecord IsNot Nothing AndAlso Not _currentAttendanceRecord.ClockOutTime.HasValue Then
                    Dim userId As Integer = CurrentUserContext.CurrentUser.UserId
                    
                    Dim confirmMsg As String = "End your shift for today? This will close today's attendance record."
                    If _currentAttendanceRecord.TotalBreakMinutes = 0 Then
                        confirmMsg = "You haven't taken a lunch break today." & vbCrLf & "End your shift for today anyway?"
                    End If
                    
                    Dim result = Forms.Common.FrmInAppAlert.ShowModal(Me, "Confirm Punch Out", confirmMsg, Forms.Common.AlertType.WarningAlert, actionText:="YES, PUNCH OUT", showCancel:=True, cancelText:="CANCEL")
                    If result <> DialogResult.OK Then Return
                    
                    Await _attendanceService.ClockOutUserAsync(userId, _currentAttendanceRecord.TotalBreakMinutes)
                    Await RefreshAttendanceWidgetStateAsync()
                    ReloadCurrentScreenIfAffected()
                End If
            Catch ex As BusinessException
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Attendance Warning", ex.Message, Forms.Common.AlertType.WarningAlert, actionText:="OK")
                Call RefreshAttendanceWidgetStateAsync()
            Catch ex As Exception
                _appLogger.LogError("Unexpected error during punch out", "FrmMainShell", ex)
                Forms.Common.FrmInAppAlert.ShowModal(Me, "System Error", "An unexpected error occurred processing your punch out. Please try again or contact IT support.", Forms.Common.AlertType.ErrorAlert, actionText:="OK")
                Call RefreshAttendanceWidgetStateAsync()
            End Try
        End Sub
        
        Private Sub ReloadCurrentScreenIfAffected()
            Dim currentScreen = _navService.GetCurrentScreen()
            If TypeOf currentScreen Is Tasks.DailyTasksControl Then
                _navService.LoadScreen(New Tasks.DailyTasksControl(Me))
            ElseIf TypeOf currentScreen Is AttendanceControl Then
                _navService.LoadScreen(New AttendanceControl())
            End If
        End Sub

        Private Async Sub btnHeaderLogout_Click(sender As Object, e As EventArgs) Handles btnHeaderLogout.Click
            If Not btnHeaderLogout.Enabled Then Return
            
            Dim isBlocked = Await CheckActiveShiftAndPromptAsync("Logout")
            If isBlocked Then Return
            
            Dim result = Forms.Common.FrmInAppAlert.ShowModal(Me, "Confirm Logout", "Are you sure you want to log out of CA Office Workforce System?", Forms.Common.AlertType.InfoAlert, actionText:="YES, LOGOUT", showCancel:=True, cancelText:="CANCEL")
            If result = DialogResult.OK Then
                PerformLogout()
            End If
        End Sub

        Private Async Sub btnHeaderExit_Click(sender As Object, e As EventArgs) Handles btnHeaderExit.Click
            If Not btnHeaderExit.Enabled OrElse Not btnHeaderExit.Visible Then Return
            Await ConfirmAndExecuteExitAsync()
        End Sub

        Private Async Sub Tray_ExitApp_Click(sender As Object, e As EventArgs)
            Await ConfirmAndExecuteExitAsync()
        End Sub

        Public Function ExecutePendingAction() As Boolean
            If CurrentPendingAction = ShellPendingAction.None Then Return False
            
            Dim action As ShellPendingAction = CurrentPendingAction
            CurrentPendingAction = ShellPendingAction.None ' Clear before executing
            
            If action = ShellPendingAction.ExitApp Then
                _isExplicitExit = True
                Application.Exit()
                Return True
            ElseIf action = ShellPendingAction.Logout Then
                PerformLogout()
                Return True
            End If
            
            Return False
        End Function

        Private Async Function CheckActiveShiftAndPromptAsync(actionName As String) As Task(Of Boolean)
            If CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser IsNot Nothing Then
                Dim userId = CurrentUserContext.CurrentUser.UserId
                Dim todayRec = Await _attendanceService.GetTodayAttendanceForUserAsync(userId)
                Dim isClockedIn = (todayRec IsNot Nothing)
                Dim isOnBreak = (todayRec IsNot Nothing AndAlso todayRec.BreakStartTime.HasValue)
                Dim isCompleted = (todayRec IsNot Nothing AndAlso todayRec.ClockOutTime.HasValue)
                
                CurrentUserContext.UpdateShiftState(isClockedIn, isOnBreak, isCompleted)

                If isClockedIn AndAlso Not isCompleted Then
                    Me.Show()
                    Me.WindowState = FormWindowState.Normal
                    Me.BringToFront()
                    Me.Activate()

                    Dim result As DialogResult
                    
                    If isOnBreak Then
                        Dim title = "ON LUNCH BREAK"
                        Dim msg = "You are currently on lunch break." & vbCrLf & vbCrLf & "Please resume work or punch out to end your shift before leaving."
                        result = Forms.Common.FrmInAppAlert.ShowModal(Me, title, msg, Forms.Common.AlertType.WarningAlert, actionText:="PUNCH OUT & LOG OUT", showCancel:=True, cancelText:="CANCEL", showTertiary:=True, tertiaryText:="RESUME WORK")
                    Else
                        Dim title = "BEFORE YOU LEAVE"
                        Dim msg = "You're currently working." & vbCrLf & vbCrLf & "If you're going for lunch, record your lunch break." & vbCrLf & "If you've finished for today, punch out before signing out."
                        result = Forms.Common.FrmInAppAlert.ShowModal(Me, title, msg, Forms.Common.AlertType.WarningAlert, actionText:="PUNCH OUT & LOG OUT", showCancel:=True, cancelText:="CANCEL", showTertiary:=True, tertiaryText:="LUNCH OUT")
                    End If
                    
                    If result = DialogResult.Retry Then ' Tertiary Button (Lunch Out or Resume Work)
                        Try
                            If isOnBreak Then
                                Await _attendanceService.EndLunchBreakAsync(userId)
                            Else
                                Await _attendanceService.StartLunchBreakAsync(userId)
                            End If
                            Await RefreshAttendanceWidgetStateAsync()
                            ReloadCurrentScreenIfAffected()
                        Catch ex As Exception
                            Dim errOp = If(isOnBreak, "resume work", "lunch out")
                            Forms.Common.FrmInAppAlert.ShowModal(Me, "System Error", $"An unexpected error occurred processing your {errOp}.", Forms.Common.AlertType.ErrorAlert, actionText:="OK")
                        End Try
                        ' Block the action because they are just going on lunch / resuming, not exiting
                        CurrentPendingAction = ShellPendingAction.None
                        Return True
                    ElseIf result = DialogResult.OK Then ' Punch Out & Log Out
                        Try
                            If isOnBreak Then
                                Await _attendanceService.EndLunchBreakAsync(userId)
                                todayRec = Await _attendanceService.GetTodayAttendanceForUserAsync(userId)
                            End If
                            
                            Await _attendanceService.ClockOutUserAsync(userId, todayRec.TotalBreakMinutes)
                            Await RefreshAttendanceWidgetStateAsync()
                            ReloadCurrentScreenIfAffected()
                            
                            If actionName.Equals("Exit", StringComparison.OrdinalIgnoreCase) Then
                                PerformLogout()
                                _isExplicitExit = True
                                Application.Exit()
                                Return True
                            ElseIf actionName.Equals("Logout", StringComparison.OrdinalIgnoreCase) Then
                                PerformLogout()
                                Return True
                            End If
                        Catch ex As Exception
                            Forms.Common.FrmInAppAlert.ShowModal(Me, "System Error", "An error occurred processing your punch out.", Forms.Common.AlertType.ErrorAlert, actionText:="OK")
                            CurrentPendingAction = ShellPendingAction.None
                            Return True
                        End Try
                    End If
                    
                    ' Cancelled
                    CurrentPendingAction = ShellPendingAction.None
                    Return True
                End If
            End If
            Return False
        End Function

        ''' <summary>
        ''' Centralized Application Exit Guard. Checks active shift status for ALL users.
        ''' Strictly blocks application exit during active shifts or lunch breaks for all users.
        ''' </summary>
        Private Async Function ConfirmAndExecuteExitAsync() As Task(Of Boolean)
            Dim isBlocked = Await CheckActiveShiftAndPromptAsync("Exit")
            If isBlocked Then Return False

            ' Standard Exit Confirmation for Non-Active Shift or Admin
            Dim confirmExit = Forms.Common.FrmInAppAlert.ShowModal(Me, "SIGN OUT & EXIT", "Are you sure you want to sign out and close the application?" & vbCrLf & vbCrLf & "Your saved clients, tasks, attendance and other records will remain safely stored.", Forms.Common.AlertType.InfoAlert, actionText:="LOG OUT & EXIT", showCancel:=True, cancelText:="CANCEL")
            If confirmExit = DialogResult.OK Then
                PerformLogout()
                _isExplicitExit = True
                Application.Exit()
                Return True
            End If

            Return False
        End Function

        ''' <summary>
        ''' Initializes System Tray Notification Icon and Context Menu for background attendance monitoring.
        ''' </summary>
        Private Sub InitializeSystemTray()
            Try
                trayContextMenu = New ContextMenuStrip()

                Dim itemOpen As New ToolStripMenuItem("Open Staff Automation Dashboard", Nothing, AddressOf Tray_OpenDashboard_Click)
                itemOpen.Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold)
                
                Dim itemResume As New ToolStripMenuItem("Resume Work", Nothing, AddressOf Tray_ResumeWork_Click)
                itemResume.Name = "menuItemResumeWork"

                Dim itemAttendance As New ToolStripMenuItem("Open Attendance Terminal", Nothing, AddressOf Tray_OpenAttendance_Click)
                Dim itemExit As New ToolStripMenuItem("Exit Application", Nothing, AddressOf Tray_ExitApp_Click)

                trayContextMenu.Items.Add(itemOpen)
                trayContextMenu.Items.Add(itemResume)
                trayContextMenu.Items.Add(itemAttendance)
                trayContextMenu.Items.Add(New ToolStripSeparator())
                trayContextMenu.Items.Add(itemExit)
                
                AddHandler trayContextMenu.Opening, Sub(s, e)
                                                        Dim menuItemResumeWork = trayContextMenu.Items.Find("menuItemResumeWork", False).FirstOrDefault()
                                                        If menuItemResumeWork IsNot Nothing Then
                                                            menuItemResumeWork.Visible = CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.IsOnLunchBreak
                                                        End If
                                                    End Sub

                Dim appIcon As Icon = Nothing
                Try
                    appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
                Catch
                    appIcon = Icon.FromHandle(SystemIcons.Application.Handle)
                End Try

                trayIcon = New NotifyIcon(Me.components) With {
                    .Icon = appIcon,
                    .ContextMenuStrip = trayContextMenu,
                    .Text = "Staff Automation System",
                    .Visible = True
                }

                AddHandler trayIcon.DoubleClick, AddressOf Tray_Icon_DoubleClick
            Catch ex As Exception
                ' Suppress non-critical tray initialization errors on environments without notification area
            End Try
        End Sub

        Private Sub Tray_Icon_DoubleClick(sender As Object, e As EventArgs)
            If CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.IsOnLunchBreak Then
                Tray_ResumeWork_Click(sender, e)
            Else
                Tray_OpenDashboard_Click(sender, e)
            End If
        End Sub
        
        Private Async Sub Tray_ResumeWork_Click(sender As Object, e As EventArgs)
            If CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.IsOnLunchBreak Then
                Await CheckActiveShiftAndPromptAsync("Resume")
            End If
        End Sub


        Private Sub Tray_OpenDashboard_Click(sender As Object, e As EventArgs)
            Me.Show()
            Me.WindowState = FormWindowState.Normal
            Me.BringToFront()
            Me.Activate()
        End Sub

        Private Sub Tray_OpenAttendance_Click(sender As Object, e As EventArgs)
            If CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser.Role = UserRole.Employee Then Return
            Me.Show()
            Me.WindowState = FormWindowState.Normal
            Me.BringToFront()
            Me.Activate()
            NavigateToModule("Attendance")
        End Sub

        Private _isGuardedExitInProgress As Boolean = False

        Protected Overrides Async Sub OnFormClosing(e As FormClosingEventArgs)
            If _isGuardedExitInProgress Then
                e.Cancel = True
                Return
            End If

            If Not _isExplicitExit Then
                e.Cancel = True
                _isGuardedExitInProgress = True
                Try
                    Await ConfirmAndExecuteExitAsync()
                Finally
                    _isGuardedExitInProgress = False
                End Try
                Return
            End If

            If trayIcon IsNot Nothing Then
                trayIcon.Visible = False
                trayIcon.Dispose()
            End If
            MyBase.OnFormClosing(e)
        End Sub

        Public Property IsExplicitExit As Boolean
            Get
                Return _isExplicitExit
            End Get
            Set(value As Boolean)
                _isExplicitExit = value
            End Set
        End Property

        Private Sub PerformLogout()
            _isExplicitExit = True
            If trayIcon IsNot Nothing Then
                trayIcon.Visible = False
                trayIcon.Dispose()
            End If

            If _authService IsNot Nothing Then
                _authService.Logout()
            Else
                CurrentUserContext.ClearSession()
            End If

            If _viewNavigator IsNot Nothing Then
                _viewNavigator.NavigateToLogin()
            Else
                Dim fallbackNavigator As IViewNavigator = New ViewNavigator()
                Dim loginForm = Factory.CreateLoginForm(fallbackNavigator)
                loginForm.Show()
                Me.Close()
            End If
        End Sub

        ' Composition Root Factory Helper
        Private Function InitializeAuthService() As IAuthService
            Dim config As IAppConfiguration = New AppConfiguration()
            Dim connFactory As IDatabaseConnectionFactory = New DbConnectionFactory(config)
            Dim sqlHelper As ISqlHelper = New SqlHelper(connFactory)
            Dim userRepo As DAL.Interfaces.IUserRepository = New UserRepository(sqlHelper)
            Dim hasher As New PasswordHasher()
            Dim appLogger As IAppLogger = New AppLogger(config)
            Dim auditLogger As New AuditLogger(sqlHelper)
            
            Dim deviceRepo As DAL.Interfaces.IDeviceRepository = New DeviceRepository(sqlHelper)
            Dim deviceService As IDeviceService = New DeviceService(deviceRepo, userRepo)

            Return New AuthService(userRepo, hasher, appLogger, auditLogger, deviceService)
        End Function
    End Class
End Namespace
