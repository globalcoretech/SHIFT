Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Security
Imports StaffAutomation.BLL.Security
Imports StaffAutomation.WinForms.Forms.Common
Imports StaffAutomation.WinForms.Forms.Main
Imports StaffAutomation.Core.Configuration
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.DAL.Core
Imports StaffAutomation.DAL.Repositories
Imports StaffAutomation.BLL.Services
Imports StaffAutomation.BLL.Logging
Imports StaffAutomation.DAL.Configuration

Namespace Forms.Admin
    ''' <summary>
    ''' Administration Master Console UserControl with full-width responsive layout.
    ''' Uses centralized Krypton design system ThemeConstants and reusable AppActionCard components.
    ''' </summary>
    Public Class AdministrationControl
        Inherits UserControl

        Private ReadOnly _mainShellHost As FrmMainShell

        Private pnlHeroHeader As Panel
        Private lblHeroTitle As Label
        Private lblHeroSubtitle As Label
        Private flowContainer As FlowLayoutPanel

        Public Sub New(Optional mainShellHost As FrmMainShell = Nothing)
            _mainShellHost = mainShellHost
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.SuspendLayout()
            Me.Dock = DockStyle.Fill
            Me.BackColor = ThemeConstants.WorkspaceBackground
            Me.Padding = New Padding(16)
            Me.AutoScroll = False

            ' 1. Hero Page Header
            pnlHeroHeader = New Panel()
            lblHeroTitle = New Label() With {.Text = "⚙️ Administration Master Console"}
            lblHeroSubtitle = New Label() With {.Text = "Manage staff user accounts, role permissions, client master data, attendance supervision, and security logs."}
            ThemeConstants.ApplyHeaderStyle(pnlHeroHeader, lblHeroTitle, lblHeroSubtitle)

            pnlHeroHeader.Controls.Add(lblHeroSubtitle)
            pnlHeroHeader.Controls.Add(lblHeroTitle)

            ' 2. Responsive Cards Grid FlowLayoutPanel
            flowContainer = New FlowLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .AutoScroll = True,
                .FlowDirection = FlowDirection.LeftToRight,
                .WrapContents = True,
                .Padding = New Padding(0, 12, 0, 0),
                .BackColor = Color.Transparent
            }

            ' Build 6 Master Administration Cards using reusable AppActionCard component
            Dim cardUsers = CreateActionCard("👤  User Account Management", "Manage staff user accounts, credentials, role permissions (Owner / Admin / Employee), and department assignments.", "Role Security Active", ThemeConstants.PrimaryAccent, "Manage Users →")
            AddHandler cardUsers.ActionClicked, Sub(s, e) OpenUserManagement()

            Dim cardClients = CreateActionCard("💼  Client Master Registry", "Configure firm client directory, GST verification status, PAN records, and contact profiles.", "Client Directory Active", ThemeConstants.SuccessGreen, "Open Registry →")
            AddHandler cardClients.ActionClicked, Sub(s, e) NavigateToModule("Clients")

            Dim cardAttendance = CreateActionCard("⏱️  Staff Attendance Supervision", "Monitor live staff punch status, shift logs, lunch breaks, and manual attendance correction overrides.", "Live Monitoring Ready", Color.FromArgb(124, 58, 237), "Supervise →")
            AddHandler cardAttendance.ActionClicked, Sub(s, e) NavigateToModule("Attendance")

            Dim cardDatabase = CreateActionCard("💾  Database Health & Backups", "Configure automated daily SQL Server database backups, local directory paths, and point-in-time restore points.", "SQL Express Connected", Color.FromArgb(13, 148, 136), "Configure →")
            AddHandler cardDatabase.ActionClicked, Sub(s, e)
                                                       Dim authz As IAuthorizationService = New AuthorizationService()
                                                       If Not authz.IsAuthorized(UserRole.Admin) Then
                                                           FrmInAppAlert.ShowModal(Me.FindForm(), "Access Denied", "Access Denied: Only System Administrators can access database backup and disaster recovery settings.", AlertType.WarningAlert, actionText:="OK")
                                                           Return
                                                       End If
                                                       Using dlg As New FrmDatabaseBackupRestore()
                                                           dlg.ShowDialog(Me.FindForm())
                                                       End Using
                                                   End Sub

            Dim cardReports = CreateActionCard("📊  Executive Reports & Audit Logs", "Export firm performance summaries, task turnaround metrics, statutory filing progress, and system security audit logs.", "Audit Logging Active", Color.FromArgb(217, 119, 6), "View Reports →")
            AddHandler cardReports.ActionClicked, Sub(s, e) NavigateToModule("Reports")

            Dim cardSecurity = CreateActionCard("🔒  Security & Access Controls", "Provision user permission overrides (GRANT/DENY), enforce password policies, and manage administrative security controls.", "User Overrides Active", Color.FromArgb(71, 85, 105), "Security Console →")
            AddHandler cardSecurity.ActionClicked, Sub(s, e)
                                                       Dim authz As IAuthorizationService = New AuthorizationService()
                                                       If Not authz.IsAuthorized(UserRole.Admin) Then
                                                           FrmInAppAlert.ShowModal(Me.FindForm(), "Access Denied", "Access Denied: Security Console requires System Administrator privileges.", AlertType.WarningAlert, actionText:="OK")
                                                           Return
                                                       End If
                                                       Using dlg As New FrmSecurityCenter(0)
                                                           dlg.ShowDialog(Me.FindForm())
                                                       End Using
                                                   End Sub

            Dim cardCategories = CreateActionCard("📋  Task Categories", "Manage dynamic task categories for client assignments. Add, edit, or disable types.", "Dynamic Tasks", Color.FromArgb(234, 88, 12), "Manage Categories →")
            AddHandler cardCategories.ActionClicked, Sub(s, e)
                                                         Dim authz As IAuthorizationService = New AuthorizationService()
                                                         If Not authz.IsAuthorized(UserRole.Admin) Then
                                                             FrmInAppAlert.ShowModal(Me.FindForm(), "Access Denied", "Access Denied: Category management requires System Administrator privileges.", AlertType.WarningAlert, actionText:="OK")
                                                             Return
                                                         End If
                                                         ' Placeholder for DB resolving logic in real app, assuming DI resolves service here
                                                         ' For WinForms directly instantiating: 
                                                         ' Note: Assuming Service is resolved correctly in FrmMainShell or instantiated manually
                                                         ' For simplicity here we just show it if possible or user can click
                                                         Dim config As IAppConfiguration = New AppConfiguration()
                                                         Dim connFactory As IDatabaseConnectionFactory = New DbConnectionFactory(config)
                                                         Dim sqlHelper = New SqlHelper(connFactory)
                                                         Dim repo = New TaskCategoryRepository(sqlHelper)
                                                         Dim audit = New AuditLogger(sqlHelper)
                                                         Dim svc = New TaskCategoryService(repo, audit)
                                                         Using dlg As New FrmTaskCategoryManagement(svc, 1) ' passing 1 for Admin ID temporarily
                                                             dlg.ShowDialog(Me.FindForm())
                                                         End Using
                                                     End Sub

            Dim cardDbConn = CreateActionCard("🌐  Central Database Setup", "Configure central SQL Server hostname, IP address, instance, authentication credentials, and test connection.", "Multi-System Config", Color.FromArgb(37, 99, 235), "Configure →")
            AddHandler cardDbConn.ActionClicked, Sub(s, e)
                                                     Dim authz As IAuthorizationService = New AuthorizationService()
                                                     If Not authz.IsAuthorizedAny(UserRole.Admin, UserRole.Owner) Then
                                                         FrmInAppAlert.ShowModal(Me.FindForm(), "Access Denied", "Access Denied: Database server configuration is restricted to System Administrators and Firm Owners.", AlertType.WarningAlert, actionText:="OK")
                                                         Return
                                                     End If
                                                     Using dlg As New FrmDatabaseConnectionConfig()
                                                         dlg.ShowDialog(Me.FindForm())
                                                     End Using
                                                 End Sub

            Dim cardDevices = CreateActionCard("📱  Device Approvals", "Review and approve login access for new staff devices.", "Pending Approvals", Color.FromArgb(16, 185, 129), "Review Devices →")
            AddHandler cardDevices.ActionClicked, Sub(s, e)
                                                       Dim authz As IAuthorizationService = New AuthorizationService()
                                                       If Not authz.IsAuthorizedAny(UserRole.Admin, UserRole.Owner) Then
                                                           FrmInAppAlert.ShowModal(Me.FindForm(), "Access Denied", "Access Denied: Device approvals are restricted to Administrators and Firm Owners.", AlertType.WarningAlert, actionText:="OK")
                                                           Return
                                                       End If
                                                       Using dlg As New FrmDeviceApprovals()
                                                           dlg.ShowDialog(Me.FindForm())
                                                       End Using
                                                   End Sub

            flowContainer.Controls.Add(cardUsers)
            flowContainer.Controls.Add(cardClients)
            flowContainer.Controls.Add(cardAttendance)
            flowContainer.Controls.Add(cardDatabase)
            flowContainer.Controls.Add(cardDbConn)
            flowContainer.Controls.Add(cardReports)
            flowContainer.Controls.Add(cardDevices)
            flowContainer.Controls.Add(cardSecurity)
            flowContainer.Controls.Add(cardCategories)

            ' Dynamic card width recalculation on container resize for 1366x768 support
            AddHandler flowContainer.Resize, Sub(s, e) RecalculateCardWidths()

            Me.Controls.Add(flowContainer)
            Me.Controls.Add(pnlHeroHeader)
            Me.ResumeLayout(False)

            RecalculateCardWidths()
        End Sub

        Protected Overrides Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)
            Dim authz As IAuthorizationService = New AuthorizationService()
            If Not authz.IsAuthorized(UserRole.Admin) Then
                FrmInAppAlert.ShowModal(Me.FindForm(), "Access Denied", "Access Denied: Administration Console requires System Administrator privileges.", AlertType.WarningAlert, actionText:="OK")
                Me.Visible = False
            End If
        End Sub

        Private Function CreateActionCard(title As String, desc As String, badge As String, accentColor As Color, actionText As String) As AppActionCard
            Dim card As New AppActionCard() With {
                .Title = title,
                .Description = desc,
                .BadgeText = badge,
                .AccentColor = accentColor,
                .ActionText = actionText,
                .Size = New Size(510, 155)
            }
            Return card
        End Function

        Private Sub OpenUserManagement()
            Dim authz As IAuthorizationService = New AuthorizationService()
            If Not authz.IsAuthorized(UserRole.Admin) Then
                FrmInAppAlert.ShowModal(Me.FindForm(), "Access Denied", "Access Denied: User account management requires System Administrator privileges.", AlertType.WarningAlert, actionText:="OK")
                Return
            End If

            If _mainShellHost IsNot Nothing Then
                _mainShellHost.LoadScreen(New FrmUserManagement())
            Else
                Using dlg As New FrmUserManagement()
                    dlg.ShowDialog(Me.FindForm())
                End Using
            End If
        End Sub

        Private Sub NavigateToModule(moduleKey As String)
            If _mainShellHost IsNot Nothing Then
                _mainShellHost.NavigateToModule(moduleKey)
            End If
        End Sub

        Private _isRecalculating As Boolean = False

        Private Sub RecalculateCardWidths()
            If _isRecalculating OrElse flowContainer Is Nothing OrElse flowContainer.IsDisposed Then Return

            Try
                _isRecalculating = True
                Dim scrollbarWidth As Integer = SystemInformation.VerticalScrollBarWidth + 8
                Dim availWidth As Integer = flowContainer.ClientSize.Width - scrollbarWidth - 12
                If availWidth < 300 Then Return

                Dim targetWidth As Integer = If(availWidth >= 800, (availWidth - 16) \ 2, availWidth)
                targetWidth = Math.Max(320, targetWidth)

                flowContainer.SuspendLayout()
                For Each ctrl As Control In flowContainer.Controls
                    If TypeOf ctrl Is AppActionCard AndAlso ctrl.Width <> targetWidth Then
                        ctrl.Width = targetWidth
                    End If
                Next
            Finally
                If flowContainer IsNot Nothing AndAlso Not flowContainer.IsDisposed Then
                    flowContainer.ResumeLayout(True)
                End If
                _isRecalculating = False
            End Try
        End Sub
    End Class
End Namespace
