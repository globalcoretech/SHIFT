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
Imports StaffAutomation.Core.Configuration
Imports StaffAutomation.Core.Constants
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Logging
Imports StaffAutomation.Core.Security
Imports StaffAutomation.DAL.Configuration
Imports StaffAutomation.DAL.Core
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

        Public Sub New(navigator As IViewNavigator)
            InitializeComponent()
            _viewNavigator = navigator
            _authService = InitializeAuthService()
            _authzService = New AuthorizationService()
            _navService = New NavigationService(Me.pnlWorkspace)
        End Sub

        Private Sub FrmMainShell_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            ' Ensure StatusStrip sits at true bottom z-order below fill panel
            Me.statusStripShell.SendToBack()

            ApplyEnterpriseTheme()
            PopulateHeaderAndStatusSessionInfo()
            BuildLeftNavigationMenu()
            InitializeSystemTray()

            ' Initially load Dashboard MVP control into workspace
            _navService.LoadScreen(New DashboardControl(Me))
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
            If CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser IsNot Nothing Then
                Dim user = CurrentUserContext.CurrentUser
                lblHeaderUser.Text = $"👤 {user.FullName}"
                lblHeaderRoleDept.Text = $"Role: {user.Role} | FY: 2026-27"

                lblStatusUser.Text = $"| User: {user.FullName}"
                lblStatusRole.Text = $"| Role: {user.Role}"
                lblStatusCompany.Text = "| CA Office Automation"
                lblStatusDb.Text = "DB: Connected"
                lblStatusServer.Text = "| Server: LOCALHOST\SQLEXPRESS"
                lblStatusFY.Text = "| FY: 2026-27"
                lblStatusVersion.Text = $"| Version: v{AppConstants.AppVersion}"
            Else
                lblHeaderUser.Text = "👤 System Administrator"
                lblHeaderRoleDept.Text = "Role: Admin | FY: 2026-27"

                lblStatusUser.Text = "| User: System Administrator"
                lblStatusRole.Text = "| Role: Admin"
            End If

            UpdateClockAndMemoryStatus()
            CheckAndUpdateInitialShiftStatusAsync()
        End Sub

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
                ("Attendance", "Attendance", "⏱️", {UserRole.Owner, UserRole.Admin, UserRole.Employee}),
                ("Reports", "Reports", "📈", {UserRole.Owner, UserRole.Admin}),
                ("Admin", "Administration", "⚙️", {UserRole.Admin}),
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

        Private Sub OnNavItemClick(item As NavItemControlInfo)
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

            SelectNavigationItem(item)

            ' Route to MVP business module views using ultra-fast instant screen caching
            Select Case item.Key
                Case "Dashboard"
                    _navService.NavigateToKey("Dashboard", Function() New DashboardControl(Me))
                Case "Tasks"
                    _navService.NavigateToKey("Tasks", Function() Factory.CreateDailyTasksControl(Me))
                Case "Clients"
                    _navService.NavigateToKey("Clients", Function() New ClientsControl())
                Case "Attendance"
                    Dim isEmployee = CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser.Role = UserRole.Employee
                    If isEmployee Then
                        _navService.NavigateToKey("Attendance_Staff", Function() New AttendanceControl())
                    Else
                        _navService.NavigateToKey("Attendance_Admin", Function() New Forms.Attendance.AdminAttendanceBoardControl())
                    End If
                Case "Reports"
                    _navService.NavigateToKey("Reports", Function() New ReportsControl())
                Case "Admin"
                    _navService.NavigateToKey("Admin", Function() New Forms.Admin.AdministrationControl(Me))
                Case "Settings"
                    _navService.NavigateToKey("Settings", Function() New SettingsControl())
                Case "Help"
                    _navService.NavigateToKey("Help", Function() New HelpControl())
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

        ''' <summary>
        ''' Highlights the selected navigation menu item and resets unselected items.
        ''' </summary>
        Private Sub SelectNavigationItem(selectedItem As NavItemControlInfo)
            For Each item In _navItems
                If item.HostPanel Is selectedItem.HostPanel Then
                    item.HostPanel.BackColor = ThemeConstants.NavigationItemSelected
                    item.IndicatorPanel.BackColor = ThemeConstants.NavigationActiveIndicator
                    item.TextLabel.Font = New Font(ThemeConstants.FontNameDefault, 9.5!, FontStyle.Bold)
                    item.TextLabel.ForeColor = Color.White
                    item.IconLabel.ForeColor = Color.White
                    _selectedNavItem = item.HostPanel
                Else
                    item.HostPanel.BackColor = ThemeConstants.NavigationBackground
                    item.IndicatorPanel.BackColor = ThemeConstants.NavigationBackground
                    item.TextLabel.Font = New Font(ThemeConstants.FontNameDefault, 9.5!, FontStyle.Regular)
                    item.TextLabel.ForeColor = If(item.IsPermissionGranted, ThemeConstants.NavigationText, ThemeConstants.NavigationMutedText)
                    item.IconLabel.ForeColor = If(item.IsPermissionGranted, ThemeConstants.NavigationText, ThemeConstants.NavigationMutedText)
                End If
            Next
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
        Public Sub NavigateToTasks(Optional filterPreset As String = "")
            Dim ctrl = _navService.NavigateToKey("Tasks", Function() Factory.CreateDailyTasksControl(Me))
            Dim tasksControl = TryCast(ctrl, Forms.Tasks.DailyTasksControl)
            If tasksControl IsNot Nothing AndAlso Not String.IsNullOrEmpty(filterPreset) Then
                tasksControl.ApplyFilterPreset(filterPreset)
            End If
            Dim item = _navItems.Find(Function(n) n.Key.Equals("Tasks", StringComparison.OrdinalIgnoreCase))
            If item IsNot Nothing Then SelectNavigationItem(item)
        End Sub

        ''' <summary>
        ''' Routes to Clients workspace with optional Needs Verification filter applied.
        ''' </summary>
        Public Sub NavigateToClients(Optional filterNeedsVerification As Boolean = False)
            Dim ctrl = _navService.NavigateToKey("Clients", Function() New ClientsControl(filterNeedsVerification))
            Dim clientsControl = TryCast(ctrl, ClientsControl)
            If clientsControl IsNot Nothing Then
                clientsControl.ApplyNeedsVerificationFilter(filterNeedsVerification)
            End If
            Dim item = _navItems.Find(Function(n) n.Key.Equals("Clients", StringComparison.OrdinalIgnoreCase))
            If item IsNot Nothing Then SelectNavigationItem(item)
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
            lblStatusTime.Text = now.ToString("dddd, MMMM dd, yyyy  •  hh:mm:ss tt")

            ' Calculate app working set memory usage
            Dim memoryBytes = GC.GetTotalMemory(False)
            Dim memoryMb As Double = Math.Round(memoryBytes / 1024.0 / 1024.0, 1)
                        lblStatusMemory.Text = $"| Memory: {memoryMb} MB"
        End Sub

        ''' <summary>
        ''' Dynamically updates Logout and Exit button states based on attendance shift status.
        ''' Hides Red Exit button and locks Logout button during active shifts for ALL clocked in users.
        ''' </summary>
        Public Sub UpdateLogoutButtonState(isClockedIn As Boolean, isWorkdayCompleted As Boolean)
            If isClockedIn AndAlso Not isWorkdayCompleted Then
                ' Active Shift -> Lock Logout button for all users
                btnHeaderLogout.Enabled = False
                btnHeaderLogout.Text = "🔒 Shift Active"
                btnHeaderLogout.BackColor = Color.FromArgb(100, 116, 139) ' Slate Gray Locked
                btnHeaderLogout.ForeColor = Color.White
                btnHeaderLogout.Cursor = Cursors.No

                ' Hide Red Exit button completely for ANY user during active shift or lunch break
                btnHeaderExit.Visible = False
                btnHeaderExit.Enabled = False
            Else
                ' Not clocked in yet OR Shift Completed -> Enable Logout and Exit
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
            End If
        End Sub

        Private Async Sub CheckAndUpdateInitialShiftStatusAsync()
            Try
                If CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser IsNot Nothing Then
                    Dim userId = CurrentUserContext.CurrentUser.UserId
                    Dim config As IAppConfiguration = New AppConfiguration()
                    Dim connFactory As IDatabaseConnectionFactory = New DbConnectionFactory(config)
                    Dim sqlHelper As ISqlHelper = New SqlHelper(connFactory)
                    Dim attendanceRepo As DAL.Interfaces.IAttendanceRepository = New AttendanceRepository(sqlHelper)

                    Dim todayRec = Await attendanceRepo.GetTodayAttendanceAsync(userId, DateTime.Today)
                    Dim isClockedIn = (todayRec IsNot Nothing)
                    Dim isOnBreak = (todayRec IsNot Nothing AndAlso todayRec.BreakStartTime.HasValue)
                    Dim isCompleted = (todayRec IsNot Nothing AndAlso todayRec.ClockOutTime.HasValue)

                    ' Update in-memory session shift state
                    CurrentUserContext.UpdateShiftState(isClockedIn, isOnBreak, isCompleted)
                    UpdateLogoutButtonState(isClockedIn, isCompleted)

                    ' If user is Employee and not clocked in yet today, automatically route to Attendance Terminal
                    If CurrentUserContext.CurrentUser.Role = UserRole.Employee AndAlso Not isClockedIn Then
                        NavigateToModule("Attendance")
                    End If
                End If
            Catch ex As Exception
            End Try
        End Sub

        Private Sub btnHeaderLogout_Click(sender As Object, e As EventArgs) Handles btnHeaderLogout.Click
            If Not btnHeaderLogout.Enabled Then Return
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

        ''' <summary>
        ''' Centralized Application Exit Guard. Checks active shift status for ALL users.
        ''' Strictly blocks application exit during active shifts or lunch breaks for all users.
        ''' </summary>
        Private Async Function ConfirmAndExecuteExitAsync() As Task(Of Boolean)
            If CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser IsNot Nothing Then
                Dim userId = CurrentUserContext.CurrentUser.UserId
                Dim config As IAppConfiguration = New AppConfiguration()
                Dim connFactory As IDatabaseConnectionFactory = New DbConnectionFactory(config)
                Dim sqlHelper As ISqlHelper = New SqlHelper(connFactory)
                Dim attendanceRepo As DAL.Interfaces.IAttendanceRepository = New AttendanceRepository(sqlHelper)

                ' Query ground-truth attendance from SQL Server DB directly
                Dim todayRec = Await attendanceRepo.GetTodayAttendanceAsync(userId, DateTime.Today)
                Dim isClockedIn = (todayRec IsNot Nothing)
                Dim isOnBreak = (todayRec IsNot Nothing AndAlso todayRec.BreakStartTime.HasValue)
                Dim isCompleted = (todayRec IsNot Nothing AndAlso todayRec.ClockOutTime.HasValue)

                CurrentUserContext.UpdateShiftState(isClockedIn, isOnBreak, isCompleted)

                ' Strict Lock: Block Exit Completely for ANY user during Active Shift or Lunch Break
                If isClockedIn AndAlso Not isCompleted Then
                    Me.Show()
                    Me.WindowState = FormWindowState.Normal
                    Me.BringToFront()
                    Me.Activate()

                    Dim alertTitle As String = If(isOnBreak, "EXIT DENIED — LUNCH BREAK ACTIVE", "EXIT DENIED — WORK SHIFT ACTIVE")
                    Dim alertMessage As String = If(isOnBreak,
                        "You are currently on Lunch Break." & vbCrLf & vbCrLf & "Application exit is STRICTLY BLOCKED during active shifts." & vbCrLf & vbCrLf & "Please go to the Attendance Terminal and click PUNCH OUT first if you are finishing work for today.",
                        "You are currently Clocked In." & vbCrLf & vbCrLf & "Application exit is STRICTLY BLOCKED during active shifts." & vbCrLf & vbCrLf & "Please go to the Attendance Terminal and click PUNCH OUT first if you are finishing work for today.")

                    Forms.Common.FrmInAppAlert.ShowModal(Me, alertTitle, alertMessage, Forms.Common.AlertType.ErrorAlert, actionText:="I UNDERSTAND — BACK TO TERMINAL")

                    ' Navigate staff directly to Attendance Terminal
                    NavigateToModule("Attendance")
                    Return False
                End If
            End If

            ' Standard Exit Confirmation for Non-Active Shift or Admin
            Dim confirmExit = Forms.Common.FrmInAppAlert.ShowModal(Me, "Confirm Application Exit", "Are you sure you want to completely exit Staff Automation?", Forms.Common.AlertType.WarningAlert, actionText:="YES, EXIT APPLICATION", showCancel:=True, cancelText:="CANCEL")
            If confirmExit = DialogResult.OK Then
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

                Dim itemAttendance As New ToolStripMenuItem("Open Attendance Terminal", Nothing, AddressOf Tray_OpenAttendance_Click)
                Dim itemExit As New ToolStripMenuItem("Exit Application", Nothing, AddressOf Tray_ExitApp_Click)

                trayContextMenu.Items.Add(itemOpen)
                trayContextMenu.Items.Add(itemAttendance)
                trayContextMenu.Items.Add(New ToolStripSeparator())
                trayContextMenu.Items.Add(itemExit)

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

                AddHandler trayIcon.DoubleClick, AddressOf Tray_OpenDashboard_Click
            Catch ex As Exception
                ' Suppress non-critical tray initialization errors on environments without notification area
            End Try
        End Sub

        Private Sub Tray_OpenDashboard_Click(sender As Object, e As EventArgs)
            Me.Show()
            Me.WindowState = FormWindowState.Normal
            Me.BringToFront()
            Me.Activate()
        End Sub

        Private Sub Tray_OpenAttendance_Click(sender As Object, e As EventArgs)
            Me.Show()
            Me.WindowState = FormWindowState.Normal
            Me.BringToFront()
            Me.Activate()
            NavigateToModule("Attendance")
        End Sub

        Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
            ' Active Shift & Lunch Break Security Guard: Intercept ALL exit/close attempts during active shift or break for ALL users
            If CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser IsNot Nothing Then
                Dim isClockedIn = CurrentUserContext.IsClockedIn
                Dim isOnBreak = CurrentUserContext.IsOnLunchBreak
                Dim isCompleted = CurrentUserContext.IsWorkdayCompleted

                ' If shift is active (Clocked In and Not Clocked Out for the day)
                If isClockedIn AndAlso Not isCompleted Then
                    ' Routine Minimize Case: Staff clicks [X] window close (App stays running in background tray)
                    If Not _isExplicitExit AndAlso e.CloseReason = CloseReason.UserClosing Then
                        e.Cancel = True
                        Me.Hide()
                        If trayIcon IsNot Nothing Then
                            Dim balloonMsg = If(isOnBreak, "⏸️ On Lunch Break: Application running in background tray. Double-click icon when back.", "Staff Automation active in background tray.")
                            trayIcon.ShowBalloonTip(3000, "Shift Active", balloonMsg, ToolTipIcon.Info)
                        End If
                        Return
                    End If

                    ' Explicit Exit Attempt (Taskbar Tray Exit / Task Manager / Alt+F4): Strictly Block Exit
                    Me.Show()
                    Me.WindowState = FormWindowState.Normal
                    Me.BringToFront()
                    Me.Activate()

                    Dim alertTitle As String = If(isOnBreak, "EXIT DENIED — LUNCH BREAK ACTIVE", "EXIT DENIED — WORK SHIFT ACTIVE")
                    Dim alertMessage As String = If(isOnBreak,
                        "You are currently on Lunch Break." & vbCrLf & vbCrLf & "Application exit is STRICTLY BLOCKED during active shifts." & vbCrLf & vbCrLf & "Please go to the Attendance Terminal and click PUNCH OUT first if you are finishing work for today.",
                        "You are currently Clocked In." & vbCrLf & vbCrLf & "Application exit is STRICTLY BLOCKED during active shifts." & vbCrLf & vbCrLf & "Please go to the Attendance Terminal and click PUNCH OUT first if you are finishing work for today.")

                    Forms.Common.FrmInAppAlert.ShowModal(Me, alertTitle, alertMessage, Forms.Common.AlertType.ErrorAlert, actionText:="I UNDERSTAND — BACK TO TERMINAL")

                    ' Cancel close & restore Attendance Terminal
                    e.Cancel = True
                    NavigateToModule("Attendance")
                    Return
                End If
            End If

            If Not _isExplicitExit AndAlso e.CloseReason = CloseReason.UserClosing Then
                e.Cancel = True
                Me.Hide()
                If trayIcon IsNot Nothing Then
                    trayIcon.ShowBalloonTip(3000, "Staff Automation Running", "Application minimized to System Tray. Attendance and Task tracking active in background.", ToolTipIcon.Info)
                End If
            Else
                If trayIcon IsNot Nothing Then
                    trayIcon.Visible = False
                    trayIcon.Dispose()
                End If
                MyBase.OnFormClosing(e)
            End If
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

            Return New AuthService(userRepo, hasher, appLogger, auditLogger)
        End Function
    End Class
End Namespace
