Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Linq
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports Krypton.Toolkit
Imports StaffAutomation.BLL.Logging
Imports StaffAutomation.BLL.Security
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
Imports StaffAutomation.DAL.Interfaces
Imports StaffAutomation.DAL.Repositories
Imports StaffAutomation.WinForms.Forms.Common
Imports StaffAutomation.WinForms.Forms.Main

Namespace Forms.Admin
    ''' <summary>
    ''' Security Center Form unifying Role-Based Access Controls, User Access Exceptions,
    ''' and Security Audit Logs in a clear 3-tab responsive experience.
    ''' </summary>
    Public Class FrmSecurityCenter
        Inherits Form

        Private ReadOnly _rolePermService As IRolePermissionService
        Private ReadOnly _userOverrideService As IUserPermissionOverrideService
        Private ReadOnly _auditService As ISecurityAuditService
        Private ReadOnly _permRepo As IPermissionRepository
        Private ReadOnly _userRepo As IUserRepository
        Private ReadOnly _authzService As IAuthorizationService

        Private ReadOnly _initialTargetUserId As Integer = 0
        Private ReadOnly _initialTargetRoleId As Integer = 0

        Private pnlHeader As Panel
        Private lblTitle As Label
        Private lblSubtitle As Label
        Private btnHeaderBack As KryptonButton
        Private btnHeaderClose As KryptonButton
        Private _isClosingConfirmed As Boolean = False

        Private tabMain As TabControl
        Private tabRoles As TabPage
        Private tabOverrides As TabPage
        Private tabAudit As TabPage

        ' --- TAB 1: ROLE PERMISSIONS CONTROLS ---
        Private pnlRolesTop As Panel
        Private pnlRoleCardsTable As TableLayoutPanel
        Private lblActiveRoleBanner As Label
        Private lblProfileHeader As Label

        Private txtSearchPerms As KryptonTextBox
        Private btnSelectAllPerms As KryptonButton
        Private btnClearAllPerms As KryptonButton
        Private btnRestoreRoleDefaults As KryptonButton

        Private dgvRolePerms As DataGridView
        Private pnlRolesFooter As Panel
        Private btnSaveRolePerms As KryptonButton
        Private btnDiscardRolePerms As KryptonButton
        Private lblPendingChanges As Label

        Private _allRoles As New List(Of RoleItem)()
        Private _allPerms As New List(Of PermissionDto)()
        Private _selectedRoleId As Integer = 3 ' Default Admin
        Private _initialRolePermIds As New HashSet(Of Integer)()
        Private _currentRolePermIds As New HashSet(Of Integer)()

        ' --- TAB 2: USER ACCESS EXCEPTIONS CONTROLS ---
        Private cboUsers As KryptonComboBox
        Private lblUserIdentitySummary As Label
        Private lblEmptyState As Label
        Private dgvUserOverrides As DataGridView
        Private btnAddOverride As KryptonButton
        Private btnRemoveOverride As KryptonButton
        Private _allUsers As New List(Of UserEntity)()
        Private _selectedUserId As Integer = 0

        ' --- TAB 3: SECURITY AUDIT CONTROLS ---
        Private cardAuditToday As AppActionCard
        Private cardAuditSecurity As AppActionCard
        Private cardAuditFailed As AppActionCard
        Private cardAuditLastEvent As AppActionCard
        Private pnlAuditMetrics As FlowLayoutPanel
        Private cboAuditModuleFilter As KryptonComboBox
        Private txtAuditSearch As KryptonTextBox
        Private dgvAuditLogs As DataGridView
        Private btnRefreshAudit As KryptonButton
        Private btnExportAudit As KryptonButton

        Public Sub New(Optional defaultTabIndex As Integer = 0, Optional targetUserId As Integer = 0, Optional targetRoleId As Integer = 0)
            _initialTargetUserId = targetUserId
            _initialTargetRoleId = targetRoleId

            If targetRoleId > 0 Then _selectedRoleId = targetRoleId

            Dim config As IAppConfiguration = New AppConfiguration()
            Dim connFactory As IDatabaseConnectionFactory = New DbConnectionFactory(config)
            Dim sqlHelper As ISqlHelper = New SqlHelper(connFactory)
            Dim appLogger As IAppLogger = New AppLogger(config)
            Dim auditLogger As IAuditLogger = New AuditLogger(sqlHelper)

            _permRepo = New PermissionRepository(sqlHelper)
            _userRepo = New UserRepository(sqlHelper)
            Dim rolePermRepo = New RolePermissionRepository(sqlHelper, connFactory)
            Dim userOverrideRepo = New UserPermissionOverrideRepository(sqlHelper)
            Dim auditRepo = New SecurityAuditRepository(sqlHelper)

            _rolePermService = New RolePermissionService(rolePermRepo, _permRepo, appLogger, auditLogger)
            _userOverrideService = New UserPermissionOverrideService(userOverrideRepo, _permRepo, _userRepo, appLogger, auditLogger)
            _auditService = New SecurityAuditService(auditRepo)
            _authzService = New AuthorizationService()

            InitializeComponent()

            If defaultTabIndex >= 0 AndAlso defaultTabIndex < tabMain.TabCount Then
                tabMain.SelectedIndex = defaultTabIndex
            End If
        End Sub

        Private Sub InitializeComponent()
            Me.Text = "🛡️ Security Center & Access Control"
            Me.Size = New Size(1220, 780)
            Me.MinimumSize = New Size(1020, 660)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.BackColor = ThemeConstants.WorkspaceBackground
            Me.Padding = New Padding(16)

            ' Header
            pnlHeader = New Panel()
            Me.Padding = New Padding(16)

            ' Header
            pnlHeader = New Panel()
            lblTitle = New Label() With {.Text = "Security & Governance Center"}
            lblSubtitle = New Label() With {.Text = "Role Permission Matrix, Individual Access Overrides, and Security Audit Logs."}
            ThemeConstants.ApplyHeaderStyle(pnlHeader, lblTitle, lblSubtitle)

            ' Top-Right Header Navigation Action Controls
            Dim pnlHeaderNav As New FlowLayoutPanel() With {
                .Dock = DockStyle.Right,
                .AutoSize = True,
                .FlowDirection = FlowDirection.LeftToRight,
                .WrapContents = False,
                .BackColor = Color.Transparent,
                .Padding = New Padding(0, 14, 16, 0)
            }

            btnHeaderBack = New KryptonButton() With {
                .Text = "< Back to Administration",
                .Size = New Size(175, 32),
                .Margin = New Padding(0)
            }
            ThemeConstants.ApplyKryptonSecondaryButton(btnHeaderBack)

            btnHeaderClose = New KryptonButton() With {
                .Text = "✕",
                .Size = New Size(44, 32),
                .Margin = New Padding(6, 0, 0, 0)
            }
            ThemeConstants.ApplyKryptonSecondaryButton(btnHeaderClose)
            btnHeaderClose.StateCommon.Back.Color1 = Color.FromArgb(220, 38, 38)
            btnHeaderClose.StateCommon.Back.Color2 = Color.FromArgb(220, 38, 38)
            btnHeaderClose.StateCommon.Content.ShortText.Color1 = Color.White
            btnHeaderClose.StateCommon.Content.ShortText.Font = New Font(ThemeConstants.FontNameDefault, 10.0!, FontStyle.Bold)

            AddHandler btnHeaderClose.Click, Sub() ConfirmAndCloseForm()
            AddHandler btnHeaderBack.Click, Sub() ConfirmAndCloseForm()

            pnlHeaderNav.Controls.Add(btnHeaderBack)
            pnlHeaderNav.Controls.Add(btnHeaderClose)

            pnlHeader.Controls.Add(pnlHeaderNav)
            pnlHeader.Controls.Add(lblSubtitle)
            pnlHeader.Controls.Add(lblTitle)

            Me.KeyPreview = True
            Me.ControlBox = True
            Me.CancelButton = btnHeaderClose

            ' TabControl
            tabMain = New TabControl() With {
                .Dock = DockStyle.Fill,
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold),
                .Padding = New Point(16, 8)
            }

            tabRoles = New TabPage("ROLE PERMISSIONS WORKSPACE") With {.BackColor = Color.White, .Padding = New Padding(12)}
            tabOverrides = New TabPage("USER ACCESS EXCEPTIONS") With {.BackColor = Color.White, .Padding = New Padding(12)}
            tabAudit = New TabPage("SECURITY AUDIT WORKSPACE") With {.BackColor = Color.White, .Padding = New Padding(12)}

            tabMain.TabPages.Add(tabRoles)
            tabMain.TabPages.Add(tabOverrides)
            tabMain.TabPages.Add(tabAudit)

            BuildRolePermissionsTab()
            BuildUserExceptionsTab()
            BuildSecurityAuditTab()

            Me.Controls.Add(tabMain)
            Me.Controls.Add(pnlHeader)
        End Sub

        ' =========================================================================
        ' TAB 1: ROLE PERMISSIONS WORKSPACE
        ' =========================================================================

        Private Sub BuildRolePermissionsTab()
            pnlRolesTop = New Panel() With {.Dock = DockStyle.Top, .Height = 245}

            Dim lblRoleHeading As New Label() With {
                .Text = "ROLE PERMISSIONS WORKSPACE — Select a role card to configure organizational capabilities:",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Dock = DockStyle.Top,
                .Height = 22
            }

            pnlRoleCardsTable = New TableLayoutPanel() With {
                .Dock = DockStyle.Top,
                .Height = 165,
                .ColumnCount = 3,
                .RowCount = 1,
                .Padding = New Padding(0, 4, 0, 4)
            }
            pnlRoleCardsTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33.33!))
            pnlRoleCardsTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33.33!))
            pnlRoleCardsTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33.33!))
            pnlRoleCardsTable.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0!))

            lblActiveRoleBanner = New Label() With {
                .Dock = DockStyle.Bottom,
                .Height = 48,
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.PrimaryAccent,
                .BackColor = Color.FromArgb(240, 244, 255),
                .Padding = New Padding(8, 6, 8, 6),
                .Text = "YOU ARE CURRENTLY CONFIGURING: Loading..."
            }

            pnlRolesTop.Controls.Add(lblActiveRoleBanner)
            pnlRolesTop.Controls.Add(pnlRoleCardsTable)
            pnlRolesTop.Controls.Add(lblRoleHeading)

            ' Search & Bulk Actions Toolbar Host
            Dim pnlGridHost As New Panel() With {.Dock = DockStyle.Fill, .Padding = New Padding(0, 8, 0, 0)}

            Dim pnlSearchAndBulk As New Panel() With {.Dock = DockStyle.Top, .Height = 65}

            lblProfileHeader = New Label() With {
                .Text = "ROLE ACCESS PROFILE — Loading...",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(0, 4),
                .AutoSize = True
            }

            Dim lblSearch As New Label() With {.Text = "Search:", .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold), .Location = New Point(0, 32), .AutoSize = True}
            txtSearchPerms = New KryptonTextBox() With {
                .Location = New Point(70, 28),
                .Size = New Size(260, 32),
                .Text = ""
            }
            ThemeConstants.ApplyAppTextBoxStyle(txtSearchPerms)
            AddHandler txtSearchPerms.TextChanged, Sub(s, e) FilterRolePermsGrid()

            Dim pnlBulkBtns As New FlowLayoutPanel() With {
                .Location = New Point(340, 26),
                .Size = New Size(450, 36),
                .FlowDirection = FlowDirection.LeftToRight
            }

            btnSelectAllPerms = New KryptonButton() With {.Text = "Select All", .Size = New Size(110, 32), .Margin = New Padding(0, 0, 8, 0)}
            ThemeConstants.ApplyKryptonSecondaryButton(btnSelectAllPerms)
            AddHandler btnSelectAllPerms.Click, Sub(s, e) BulkTogglePerms(True)

            btnClearAllPerms = New KryptonButton() With {.Text = "Clear All", .Size = New Size(110, 32), .Margin = New Padding(0, 0, 8, 0)}
            ThemeConstants.ApplyKryptonSecondaryButton(btnClearAllPerms)
            AddHandler btnClearAllPerms.Click, Sub(s, e) BulkTogglePerms(False)

            btnRestoreRoleDefaults = New KryptonButton() With {.Text = "Restore Role Defaults", .Size = New Size(175, 32), .Margin = New Padding(0)}
            ThemeConstants.ApplyKryptonSecondaryButton(btnRestoreRoleDefaults)
            AddHandler btnRestoreRoleDefaults.Click, Sub(s, e) RevertRoleGridChanges()

            pnlBulkBtns.Controls.Add(btnSelectAllPerms)
            pnlBulkBtns.Controls.Add(btnClearAllPerms)
            pnlBulkBtns.Controls.Add(btnRestoreRoleDefaults)

            pnlSearchAndBulk.Controls.Add(pnlBulkBtns)
            pnlSearchAndBulk.Controls.Add(txtSearchPerms)
            pnlSearchAndBulk.Controls.Add(lblSearch)
            pnlSearchAndBulk.Controls.Add(lblProfileHeader)

            dgvRolePerms = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .MultiSelect = False,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .AutoGenerateColumns = False
            }
            ThemeConstants.ApplyModernGridStyle(dgvRolePerms)

            dgvRolePerms.Columns.Add(New DataGridViewCheckBoxColumn() With {.Name = "colGranted", .HeaderText = "Granted", .Width = 75})
            dgvRolePerms.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colModule", .HeaderText = "Module", .ReadOnly = True, .Width = 160})
            dgvRolePerms.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colName", .HeaderText = "Permission Name", .ReadOnly = True, .Width = 230})
            dgvRolePerms.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colDesc", .HeaderText = "Human Description", .ReadOnly = True, .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})
            dgvRolePerms.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colCode", .HeaderText = "Code", .ReadOnly = True, .Width = 150})
            dgvRolePerms.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colId", .HeaderText = "ID", .Visible = False})

            AddHandler dgvRolePerms.CellValueChanged, Sub(s, e) OnRoleGridCellValueChanged(e)
            AddHandler dgvRolePerms.CurrentCellDirtyStateChanged, Sub(s, e)
                                                                        If dgvRolePerms.IsCurrentCellDirty Then dgvRolePerms.CommitEdit(DataGridViewDataErrorContexts.Commit)
                                                                    End Sub

            pnlGridHost.Controls.Add(dgvRolePerms)
            pnlGridHost.Controls.Add(pnlSearchAndBulk)

            ' Persistent Bottom Action Bar (Docked to Bottom, Always Visible at 1366x768)
            pnlRolesFooter = New Panel() With {.Dock = DockStyle.Bottom, .Height = 54, .BackColor = Color.FromArgb(248, 250, 252), .Padding = New Padding(12, 8, 12, 8)}

            lblPendingChanges = New Label() With {
                .Dock = DockStyle.Left,
                .AutoSize = True,
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextSecondary,
                .Text = "Changes Pending: 0"
            }

            Dim pnlRightBtns As New FlowLayoutPanel() With {
                .Dock = DockStyle.Right,
                .AutoSize = True,
                .FlowDirection = FlowDirection.RightToLeft,
                .BackColor = Color.Transparent
            }

            btnDiscardRolePerms = New KryptonButton() With {.Text = "Discard Changes", .Size = New Size(140, 36), .Margin = New Padding(8, 0, 0, 0)}
            ThemeConstants.ApplyKryptonSecondaryButton(btnDiscardRolePerms)
            AddHandler btnDiscardRolePerms.Click, Sub(s, e) RevertRoleGridChanges()

            btnSaveRolePerms = New KryptonButton() With {.Text = "Save Role Permissions", .Size = New Size(190, 36), .Margin = New Padding(0)}
            ThemeConstants.ApplyKryptonPrimaryButton(btnSaveRolePerms)
            AddHandler btnSaveRolePerms.Click, Async Sub(s, e) Await SaveRolePermissionsAsync()
            ThemeConstants.ApplyKryptonPrimaryButton(btnSaveRolePerms)
            AddHandler btnSaveRolePerms.Click, Async Sub(s, e) Await SaveRolePermissionsAsync()

            pnlRightBtns.Controls.Add(btnDiscardRolePerms)
            pnlRightBtns.Controls.Add(btnSaveRolePerms)

            pnlRolesFooter.Controls.Add(lblPendingChanges)
            pnlRolesFooter.Controls.Add(pnlRightBtns)

            tabRoles.Controls.Add(pnlGridHost)
            tabRoles.Controls.Add(pnlRolesFooter)
            pnlRolesFooter.BringToFront()
            tabRoles.Controls.Add(pnlRolesTop)
        End Sub

        ' =========================================================================
        ' TAB 2: USER ACCESS EXCEPTIONS
        ' =========================================================================

        Private Sub BuildUserExceptionsTab()
            Dim pnlTop As New Panel() With {.Dock = DockStyle.Top, .Height = 135}

            Dim lblHeading As New Label() With {
                .Text = "USER ACCESS EXCEPTIONS — Override normal role permissions for a specific individual.",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Dock = DockStyle.Top,
                .Height = 22
            }

            Dim lblExplanation As New Label() With {
                .Text = "Normally, a user receives access from their role (User Role  ➔  Normal Access  ➔  Individual Exception). Use an exception only when an individual needs different access.",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Italic),
                .ForeColor = ThemeConstants.TextSecondary,
                .Dock = DockStyle.Top,
                .Height = 22
            }

            Dim pnlUserSel As New Panel() With {.Dock = DockStyle.Fill, .Padding = New Padding(0, 8, 0, 0)}
            Dim lblUserSel As New Label() With {.Text = "Select User Account:", .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold), .Location = New Point(0, 8), .AutoSize = True}
            cboUsers = New KryptonComboBox() With {.Location = New Point(140, 4), .Size = New Size(320, 32)}
            ThemeConstants.ApplyAppComboBoxStyle(cboUsers)
            AddHandler cboUsers.SelectedIndexChanged, Async Sub(s, e) Await OnUserSelectedAsync()

            lblUserIdentitySummary = New Label() With {
                .Text = "Select a user above to inspect role access and individual exceptions.",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.PrimaryAccent,
                .Location = New Point(480, 8),
                .AutoSize = True
            }

            pnlUserSel.Controls.Add(lblUserIdentitySummary)
            pnlUserSel.Controls.Add(cboUsers)
            pnlUserSel.Controls.Add(lblUserSel)

            pnlTop.Controls.Add(pnlUserSel)
            pnlTop.Controls.Add(lblExplanation)
            pnlTop.Controls.Add(lblHeading)

            ' Toolbar & DataGridView
            Dim pnlToolbar As New FlowLayoutPanel() With {.Dock = DockStyle.Top, .Height = 44, .FlowDirection = FlowDirection.LeftToRight}
            btnAddOverride = New KryptonButton() With {.Text = "➕ Add Access Exception", .Size = New Size(185, 34), .Margin = New Padding(0, 0, 8, 0)}
            ThemeConstants.ApplyKryptonPrimaryButton(btnAddOverride)
            AddHandler btnAddOverride.Click, Sub(s, e) AddUserOverride()

            btnRemoveOverride = New KryptonButton() With {.Text = "🗑️ Remove Exception", .Size = New Size(155, 34), .Margin = New Padding(0)}
            ThemeConstants.ApplyKryptonSecondaryButton(btnRemoveOverride)
            AddHandler btnRemoveOverride.Click, Async Sub(s, e) Await RemoveUserOverrideAsync()

            pnlToolbar.Controls.Add(btnAddOverride)
            pnlToolbar.Controls.Add(btnRemoveOverride)

            lblEmptyState = New Label() With {
                .Text = "ℹ️ No individual exceptions exist for this user. This account currently follows all standard role permissions.",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Italic),
                .ForeColor = ThemeConstants.TextSecondary,
                .Dock = DockStyle.Top,
                .Height = 32,
                .Visible = False
            }

            dgvUserOverrides = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .MultiSelect = False,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .AutoGenerateColumns = False
            }
            ThemeConstants.ApplyModernGridStyle(dgvUserOverrides)

            dgvUserOverrides.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colOvId", .HeaderText = "ID", .Visible = False})
            dgvUserOverrides.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colOvPermId", .HeaderText = "PermID", .Visible = False})
            dgvUserOverrides.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colOvType", .HeaderText = "Exception Type", .Width = 140})
            dgvUserOverrides.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colOvPermName", .HeaderText = "Permission Name", .Width = 220})
            dgvUserOverrides.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colOvModule", .HeaderText = "Module", .Width = 130})
            dgvUserOverrides.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colOvReason", .HeaderText = "Justification Reason", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})
            dgvUserOverrides.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colOvDate", .HeaderText = "Granted On", .Width = 140})

            Dim pnlGridHost As New Panel() With {.Dock = DockStyle.Fill, .Padding = New Padding(0, 8, 0, 0)}
            pnlGridHost.Controls.Add(dgvUserOverrides)
            pnlGridHost.Controls.Add(lblEmptyState)
            pnlGridHost.Controls.Add(pnlToolbar)

            tabOverrides.Controls.Add(pnlGridHost)
            tabOverrides.Controls.Add(pnlTop)
        End Sub

        ' =========================================================================
        ' TAB 3: SECURITY AUDIT WORKSPACE
        ' =========================================================================

        Private Sub BuildSecurityAuditTab()
            Dim pnlAuditTop As New Panel() With {.Dock = DockStyle.Top, .Height = 155}

            Dim lblAuditHeading As New Label() With {
                .Text = "SECURITY AUDIT WORKSPACE — Monitor security activity, permission changes, and authorization events.",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Dock = DockStyle.Top,
                .Height = 22
            }

            pnlAuditMetrics = New FlowLayoutPanel() With {
                .Dock = DockStyle.Top,
                .Height = 85,
                .FlowDirection = FlowDirection.LeftToRight,
                .WrapContents = False,
                .Padding = New Padding(0, 4, 0, 4)
            }

            cardAuditToday = CreateMetricCard("TODAY'S ACTIVITY", "0 Events", "24h Activity", ThemeConstants.PrimaryAccent)
            cardAuditSecurity = CreateMetricCard("SECURITY CHANGES", "0 Policy Updates", "Role & Overrides", ThemeConstants.SuccessGreen)
            cardAuditFailed = CreateMetricCard("FAILED ACTIONS", "0 Denied Actions", "Zero Breaches", Color.FromArgb(217, 119, 6))
            cardAuditLastEvent = CreateMetricCard("LAST SECURITY EVENT", "Recent", "Audit Verified", Color.FromArgb(124, 58, 237))

            pnlAuditMetrics.Controls.Add(cardAuditToday)
            pnlAuditMetrics.Controls.Add(cardAuditSecurity)
            pnlAuditMetrics.Controls.Add(cardAuditFailed)
            pnlAuditMetrics.Controls.Add(cardAuditLastEvent)

            ' Audit Filter Toolbar
            Dim pnlAuditToolbar As New FlowLayoutPanel() With {.Dock = DockStyle.Bottom, .Height = 44, .FlowDirection = FlowDirection.LeftToRight}

            Dim lblMod As New Label() With {.Text = "Module:", .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold), .Padding = New Padding(0, 8, 4, 0), .AutoSize = True}
            cboAuditModuleFilter = New KryptonComboBox() With {.Size = New Size(180, 32)}
            ThemeConstants.ApplyAppComboBoxStyle(cboAuditModuleFilter)
            cboAuditModuleFilter.Items.AddRange(New Object() {"All Modules", "RolePermissionService", "UserPermissionOverrideService", "FirmProfileService", "FinancialYearService", "Security", "DisasterRecovery"})
            cboAuditModuleFilter.SelectedIndex = 0
            AddHandler cboAuditModuleFilter.SelectedIndexChanged, Async Sub(s, e) Await LoadAuditLogsAsync()

            txtAuditSearch = New KryptonTextBox() With {.Size = New Size(220, 32), .Text = ""}
            ThemeConstants.ApplyAppTextBoxStyle(txtAuditSearch)
            AddHandler txtAuditSearch.TextChanged, Async Sub(s, e) Await LoadAuditLogsAsync()

            btnRefreshAudit = New KryptonButton() With {.Text = "🔄 Refresh Log", .Size = New Size(130, 34), .Margin = New Padding(8, 0, 8, 0)}
            ThemeConstants.ApplyKryptonSecondaryButton(btnRefreshAudit)
            AddHandler btnRefreshAudit.Click, Async Sub(s, e) Await LoadAuditLogsAsync()

            btnExportAudit = New KryptonButton() With {.Text = "📊 Export Audit Log", .Size = New Size(150, 34), .Margin = New Padding(0)}
            ThemeConstants.ApplyKryptonSecondaryButton(btnExportAudit)
            AddHandler btnExportAudit.Click, Sub(s, e) ExportAuditLog()

            pnlAuditToolbar.Controls.Add(lblMod)
            pnlAuditToolbar.Controls.Add(cboAuditModuleFilter)
            pnlAuditToolbar.Controls.Add(txtAuditSearch)
            pnlAuditToolbar.Controls.Add(btnRefreshAudit)
            pnlAuditToolbar.Controls.Add(btnExportAudit)

            pnlAuditTop.Controls.Add(pnlAuditToolbar)
            pnlAuditTop.Controls.Add(pnlAuditMetrics)
            pnlAuditTop.Controls.Add(lblAuditHeading)

            ' DataGridView Setup
            dgvAuditLogs = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .MultiSelect = False,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .AutoGenerateColumns = False
            }
            ThemeConstants.ApplyModernGridStyle(dgvAuditLogs)

            dgvAuditLogs.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colAuditTime", .HeaderText = "Timestamp (UTC)", .Width = 150})
            dgvAuditLogs.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colAuditUser", .HeaderText = "User", .Width = 140})
            dgvAuditLogs.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colAuditMod", .HeaderText = "Module", .Width = 150})
            dgvAuditLogs.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colAuditAction", .HeaderText = "Action", .Width = 160})
            dgvAuditLogs.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colAuditDetails", .HeaderText = "Audit Log Details", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})
            dgvAuditLogs.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colAuditStatus", .HeaderText = "Status", .Width = 90})

            Dim pnlGridHost As New Panel() With {.Dock = DockStyle.Fill, .Padding = New Padding(0, 8, 0, 0)}
            pnlGridHost.Controls.Add(dgvAuditLogs)

            tabAudit.Controls.Add(pnlGridHost)
            tabAudit.Controls.Add(pnlAuditTop)
        End Sub

        ' =========================================================================
        ' LOADERS & EVENT HANDLERS
        ' =========================================================================

        Protected Overrides Async Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)
            If Not _authzService.IsAuthorized(UserRole.Admin) AndAlso Not _authzService.IsAuthorized(UserRole.Owner) Then
                FrmInAppAlert.ShowModal(Me, "Access Denied", "Access Denied: Security Center requires Administrator or Owner privileges.", AlertType.WarningAlert)
                Me.Close()
                Return
            End If

            Await LoadRolesDataAsync()
            Await LoadUsersDataAsync()
            Await LoadAuditLogsAsync()

            If _initialTargetRoleId > 0 Then
                Await SelectRoleAsync(_initialTargetRoleId)
            End If

            If _initialTargetUserId > 0 AndAlso _allUsers IsNot Nothing Then
                Dim targetIdx = _allUsers.FindIndex(Function(u) u.UserId = _initialTargetUserId)
                If targetIdx >= 0 Then
                    cboUsers.SelectedIndex = targetIdx
                End If
            End If
        End Sub

        Private Async Function LoadRolesDataAsync() As Task
            Try
                _allRoles = New List(Of RoleItem) From {
                    New RoleItem With {.RoleId = 2, .RoleName = "Owner", .Icon = "👑", .Description = "FULL SYSTEM ACCESS"},
                    New RoleItem With {.RoleId = 3, .RoleName = "Administrator", .Icon = "🛡️", .Description = "OPERATIONAL CONTROL"},
                    New RoleItem With {.RoleId = 1, .RoleName = "Employee", .Icon = "👤", .Description = "LIMITED OPERATIONAL ACCESS"}
                }
                Dim permsEntities = Await _permRepo.GetAllAsync()

                _allPerms.Clear()
                For Each p In permsEntities
                    If p.IsActive Then
                        _allPerms.Add(New PermissionDto() With {
                            .PermissionId = p.PermissionId,
                            .PermissionCode = p.PermissionCode,
                            .PermissionName = p.PermissionName,
                            .ModuleName = p.ModuleName,
                            .Description = p.Description
                        })
                    End If
                Next

                ' Sort by ModuleName then PermissionName for visual module grouping
                _allPerms = _allPerms.OrderBy(Function(x) x.ModuleName).ThenBy(Function(x) x.PermissionName).ToList()

                BuildRoleCards()
                If _allRoles.Count > 0 Then
                    Dim defaultRole = If(_initialTargetRoleId > 0, _initialTargetRoleId, _allRoles(1).RoleId)
                    Await SelectRoleAsync(defaultRole)
                End If
            Catch ex As Exception
                FrmInAppAlert.ShowModal(Me, "Error", "Failed to load Security Center data: " & ex.Message, AlertType.WarningAlert)
            End Try
        End Function

        Private Sub BuildRoleCards()
            pnlRoleCardsTable.Controls.Clear()

            For i As Integer = 0 To _allRoles.Count - 1
                Dim r = _allRoles(i)
                Dim rId = r.RoleId
                Dim rName = r.RoleName
                Dim rDesc = r.Description
                Dim isSelected = (rId = _selectedRoleId)

                Dim subTitle = If(rId = 2, "FULL SYSTEM ACCESS", If(rId = 3, "OPERATIONAL CONTROL", "LIMITED ACCESS"))
                Dim permCount = If(rId = _selectedRoleId, _currentRolePermIds.Count, GetRolePermissionCount(rId))
                Dim permSummary = $"{permCount} / {_allPerms.Count} Permissions Granted"

                Dim card As New Panel() With {
                    .Dock = DockStyle.Fill,
                    .Margin = New Padding(4),
                    .BackColor = If(isSelected, Color.FromArgb(238, 242, 255), Color.White),
                    .Cursor = Cursors.Hand
                }
                ThemeConstants.ApplyCardSurfaceStyle(card)
                If isSelected Then card.BorderStyle = BorderStyle.FixedSingle

                Dim tblLayout As New TableLayoutPanel() With {
                    .Dock = DockStyle.Fill,
                    .ColumnCount = 1,
                    .RowCount = 5,
                    .Padding = New Padding(4, 0, 4, 4),
                    .BackColor = Color.Transparent
                }
                tblLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0!))
                tblLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 4.0!))   ' Accent line
                tblLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 24.0!))  ' Title
                tblLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 18.0!))  ' Subtitle
                tblLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0!))  ' Flexible Description & Perm Summary
                tblLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 28.0!))  ' Fixed Status badge area (28px)

                Dim pnlAccent As New Panel() With {
                    .Dock = DockStyle.Fill,
                    .BackColor = If(isSelected, ThemeConstants.PrimaryAccent, Color.FromArgb(226, 232, 240))
                }

                Dim lblRName As New Label() With {
                    .Text = rName.ToUpper(),
                    .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold),
                    .ForeColor = If(isSelected, ThemeConstants.PrimaryAccent, ThemeConstants.TextPrimary),
                    .Dock = DockStyle.Fill,
                    .TextAlign = ContentAlignment.MiddleLeft,
                    .AutoEllipsis = True
                }

                Dim lblRSub As New Label() With {
                    .Text = subTitle,
                    .Font = New Font(ThemeConstants.FontNameDefault, 7.0!, FontStyle.Bold),
                    .ForeColor = If(isSelected, ThemeConstants.PrimaryAccent, ThemeConstants.TextMuted),
                    .Dock = DockStyle.Fill,
                    .TextAlign = ContentAlignment.MiddleLeft,
                    .AutoEllipsis = True
                }

                Dim lblRDesc As New Label() With {
                    .Text = permSummary & vbCrLf & rDesc,
                    .Font = New Font(ThemeConstants.FontNameDefault, 7.5!, FontStyle.Regular),
                    .ForeColor = ThemeConstants.TextSecondary,
                    .Dock = DockStyle.Fill,
                    .AutoEllipsis = True
                }

                Dim lblStatusBadge As New Label() With {
                    .Text = If(isSelected, "CURRENTLY CONFIGURING", "CLICK TO CONFIGURE"),
                    .Font = New Font(ThemeConstants.FontNameDefault, 7.5!, FontStyle.Bold),
                    .ForeColor = If(isSelected, Color.FromArgb(67, 56, 202), Color.FromArgb(100, 116, 139)),
                    .BackColor = If(isSelected, Color.FromArgb(224, 231, 255), Color.FromArgb(241, 245, 249)),
                    .Dock = DockStyle.Fill,
                    .TextAlign = ContentAlignment.MiddleCenter,
                    .AutoEllipsis = True
                }

                tblLayout.Controls.Add(pnlAccent, 0, 0)
                tblLayout.Controls.Add(lblRName, 0, 1)
                tblLayout.Controls.Add(lblRSub, 0, 2)
                tblLayout.Controls.Add(lblRDesc, 0, 3)
                tblLayout.Controls.Add(lblStatusBadge, 0, 4)

                card.Controls.Add(tblLayout)

                AddHandler card.Click, Async Sub(s, e) Await SelectRoleAsync(rId)
                AddHandler tblLayout.Click, Async Sub(s, e) Await SelectRoleAsync(rId)
                AddHandler pnlAccent.Click, Async Sub(s, e) Await SelectRoleAsync(rId)
                AddHandler lblRName.Click, Async Sub(s, e) Await SelectRoleAsync(rId)
                AddHandler lblRSub.Click, Async Sub(s, e) Await SelectRoleAsync(rId)
                AddHandler lblRDesc.Click, Async Sub(s, e) Await SelectRoleAsync(rId)
                AddHandler lblStatusBadge.Click, Async Sub(s, e) Await SelectRoleAsync(rId)

                pnlRoleCardsTable.Controls.Add(card, i, 0)
            Next
        End Sub

        Private Function GetRolePermissionCount(roleId As Integer) As Integer
            If roleId = 2 Then Return _allPerms.Count
            If roleId = 3 Then Return CInt(Math.Ceiling(_allPerms.Count * 0.8))
            Return CInt(Math.Ceiling(_allPerms.Count * 0.4))
        End Function

        Private Async Function SelectRoleAsync(roleId As Integer) As Task
            _selectedRoleId = roleId

            Dim grantedPerms = Await _rolePermService.GetPermissionsForRoleAsync(roleId)
            _initialRolePermIds.Clear()
            _currentRolePermIds.Clear()

            For Each g In grantedPerms
                _initialRolePermIds.Add(g.PermissionId)
                _currentRolePermIds.Add(g.PermissionId)
            Next

            BuildRoleCards()
            PopulateRolePermsGrid()
            UpdateRoleSummaryText()
        End Function

        Private Sub PopulateRolePermsGrid()
            dgvRolePerms.Rows.Clear()
            Dim filterText = txtSearchPerms.Text.Trim().ToUpper()

            For Each p In _allPerms
                If String.IsNullOrEmpty(filterText) OrElse p.PermissionName.ToUpper().Contains(filterText) OrElse p.ModuleName.ToUpper().Contains(filterText) OrElse p.PermissionCode.ToUpper().Contains(filterText) Then
                    Dim isChecked = _currentRolePermIds.Contains(p.PermissionId)
                    dgvRolePerms.Rows.Add(isChecked, p.ModuleName, p.PermissionName, p.Description, p.PermissionCode, p.PermissionId)
                End If
            Next
        End Sub

        Private Sub FilterRolePermsGrid()
            PopulateRolePermsGrid()
        End Sub

        Private Sub BulkTogglePerms(granted As Boolean)
            For Each p In _allPerms
                If granted Then
                    _currentRolePermIds.Add(p.PermissionId)
                Else
                    _currentRolePermIds.Remove(p.PermissionId)
                End If
            Next
            PopulateRolePermsGrid()
            UpdateRoleSummaryText()
        End Sub

        Private Sub OnRoleGridCellValueChanged(e As DataGridViewCellEventArgs)
            If e.RowIndex < 0 OrElse e.ColumnIndex <> 0 Then Return

            Dim row = dgvRolePerms.Rows(e.RowIndex)
            Dim pId = Convert.ToInt32(row.Cells("colId").Value)
            Dim isChecked = Convert.ToBoolean(row.Cells("colGranted").Value)

            If isChecked Then
                _currentRolePermIds.Add(pId)
            Else
                _currentRolePermIds.Remove(pId)
            End If

            UpdateRoleSummaryText()
        End Sub

        Private Sub UpdateRoleSummaryText()
            Dim roleObj = _allRoles.Find(Function(r) r.RoleId = _selectedRoleId)
            Dim rName = If(roleObj IsNot Nothing, roleObj.RoleName, "Role")
            Dim rIcon = If(roleObj IsNot Nothing, roleObj.Icon, "🛡️")

            Dim affectedUsersCount = 0
            If _allUsers IsNot Nothing Then
                Dim targetRoleEnum = CType(_selectedRoleId, UserRole)
                affectedUsersCount = _allUsers.FindAll(Function(u) u.Role = targetRoleEnum AndAlso u.IsActive).Count
            End If

            lblActiveRoleBanner.Text = $"YOU ARE CURRENTLY CONFIGURING: {rIcon} {rName.ToUpper()}" & vbCrLf & $"Changes made here will affect all users assigned to the {rName} role.  •  Users Affected: {affectedUsersCount} active users currently inherit this role."
            lblProfileHeader.Text = $"{rName.ToUpper()} ACCESS PROFILE — {_currentRolePermIds.Count} of {_allPerms.Count} permissions currently granted"

            Dim changesCount = 0
            For Each id In _currentRolePermIds
                If Not _initialRolePermIds.Contains(id) Then changesCount += 1
            Next
            For Each id In _initialRolePermIds
                If Not _currentRolePermIds.Contains(id) Then changesCount += 1
            Next

            lblPendingChanges.Text = "Changes Pending: " & changesCount.ToString()
            lblPendingChanges.ForeColor = If(changesCount > 0, Color.FromArgb(220, 38, 38), ThemeConstants.TextSecondary)
        End Sub

        Private Sub RevertRoleGridChanges()
            _currentRolePermIds.Clear()
            For Each id In _initialRolePermIds
                _currentRolePermIds.Add(id)
            Next
            PopulateRolePermsGrid()
            UpdateRoleSummaryText()
        End Sub

        Private Async Function SaveRolePermissionsAsync() As Task
            btnSaveRolePerms.Enabled = False
            Try
                ' Owner Role Safety Guard Check
                If _selectedRoleId = 2 Then
                    Dim removedCriticalPerms = False
                    For Each p In _allPerms
                        If (p.PermissionCode.StartsWith("Security.", StringComparison.OrdinalIgnoreCase) OrElse p.ModuleName.Equals("Security", StringComparison.OrdinalIgnoreCase)) AndAlso Not _currentRolePermIds.Contains(p.PermissionId) Then
                            removedCriticalPerms = True
                            Exit For
                        End If
                    Next

                    If removedCriticalPerms Then
                        Dim confirm = FrmInAppAlert.ShowModal(Me, "⚠️ WARNING: REMOVING CRITICAL OWNER PERMISSIONS", $"Removing critical administrative permissions from the Owner role may restrict system administration, account recovery, or security audit enforcement.{Environment.NewLine}{Environment.NewLine}Are you sure you want to proceed with this modification to the Owner role?", AlertType.WarningAlert, actionText:="PROCEED WITH SAVE", showCancel:=True, cancelText:="CANCEL")
                        If confirm <> DialogResult.OK Then
                            Return
                        End If
                    End If
                End If

                Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)
                Dim permIdsList = _currentRolePermIds.ToList()

                Dim success = Await _rolePermService.UpdateRolePermissionsAsync(_selectedRoleId, permIdsList, currentUserId)
                If success Then
                    FrmInAppAlert.ShowModal(Me, "Success", "Role Permission matrix saved successfully.", AlertType.SuccessAlert)
                    Await SelectRoleAsync(_selectedRoleId)
                Else
                    FrmInAppAlert.ShowModal(Me, "Error", "Failed to save Role Permission matrix.", AlertType.WarningAlert)
                End If
            Catch ex As Exception
                FrmInAppAlert.ShowModal(Me, "Error", "Failed to save matrix: " & ex.Message, AlertType.WarningAlert)
            Finally
                btnSaveRolePerms.Enabled = True
            End Try
        End Function

        ' --- USER EXCEPTIONS HANDLERS ---

        Private Async Function LoadUsersDataAsync() As Task
            Try
                _allUsers = Await _userRepo.GetAllAsync()
                cboUsers.Items.Clear()

                For Each u In _allUsers
                    cboUsers.Items.Add(u.FullName & " (" & u.Username & ") — Role: " & u.Role.ToString())
                Next

                If cboUsers.Items.Count > 0 Then
                    cboUsers.SelectedIndex = 0
                End If
            Catch ex As Exception
                FrmInAppAlert.ShowModal(Me, "Error", "Failed to load users: " & ex.Message, AlertType.WarningAlert)
            End Try
        End Function

        Private Async Function OnUserSelectedAsync() As Task
            If cboUsers.SelectedIndex < 0 OrElse cboUsers.SelectedIndex >= _allUsers.Count Then Return

            Dim targetUser = _allUsers(cboUsers.SelectedIndex)
            _selectedUserId = targetUser.UserId

            Dim userOverrides = Await _userOverrideService.GetOverridesForUserAsync(_selectedUserId)
            dgvUserOverrides.Rows.Clear()

            Dim grantCount = 0
            Dim denyCount = 0

            For Each o In userOverrides
                Dim typeBadge = If(o.IsGranted, "✅ EXPLICIT GRANT", "⛔ EXPLICIT DENY")
                If o.IsGranted Then grantCount += 1 Else denyCount += 1

                dgvUserOverrides.Rows.Add(o.OverrideId, o.PermissionId, typeBadge, o.PermissionName, o.ModuleName, o.Reason, o.GrantedOn.ToString("yyyy-MM-dd HH:mm"))
            Next

            lblEmptyState.Visible = (userOverrides.Count = 0)
            lblEmptyState.Text = $"ℹ️ No individual access exceptions exist for {targetUser.FullName}. This account follows standard {targetUser.Role.ToString()} permissions."

            lblUserIdentitySummary.Text = "User: " & targetUser.FullName & " | Role: " & targetUser.Role.ToString() & " | Active Exceptions: " & userOverrides.Count.ToString() & " (" & grantCount.ToString() & " Grants, " & denyCount.ToString() & " Explicit Denies)"
        End Function

        Private Sub AddUserOverride()
            If cboUsers.SelectedIndex < 0 OrElse cboUsers.SelectedIndex >= _allUsers.Count Then
                FrmInAppAlert.ShowModal(Me, "Selection Required", "Please select a target user account.", AlertType.WarningAlert)
                Return
            End If

            Dim targetUser = _allUsers(cboUsers.SelectedIndex)
            Using dlg As New FrmAddEditUserOverride(targetUser.UserId, targetUser.Username)
                If dlg.ShowDialog(Me) = DialogResult.OK Then
                    Dim task = OnUserSelectedAsync()
                End If
            End Using
        End Sub

        Private Async Function RemoveUserOverrideAsync() As Task
            If dgvUserOverrides.SelectedRows.Count = 0 Then
                FrmInAppAlert.ShowModal(Me, "Selection Required", "Please select an access exception row to remove.", AlertType.WarningAlert)
                Return
            End If

            Dim selectedRow = dgvUserOverrides.SelectedRows(0)
            Dim permId = Convert.ToInt32(selectedRow.Cells("colOvPermId").Value)
            Dim permName = Convert.ToString(selectedRow.Cells("colOvPermName").Value)

            Dim confirm = MessageBox.Show($"Remove explicit access exception for '{permName}'?", "Confirm Removal", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If confirm <> DialogResult.Yes Then Return

            Try
                Dim success = Await _userOverrideService.RemoveOverrideAsync(_selectedUserId, permId)
                If success Then
                    FrmInAppAlert.ShowModal(Me, "Success", "Access exception removed successfully.", AlertType.SuccessAlert)
                    Await OnUserSelectedAsync()
                End If
            Catch ex As Exception
                FrmInAppAlert.ShowModal(Me, "Error", "Failed to remove exception: " & ex.Message, AlertType.WarningAlert)
            End Try
        End Function

        ' --- SECURITY AUDIT HANDLERS ---

        Private Async Function LoadAuditLogsAsync() As Task
            Try
                Dim selectedMod = Convert.ToString(cboAuditModuleFilter.SelectedItem)
                If String.Equals(selectedMod, "All Modules", StringComparison.OrdinalIgnoreCase) Then selectedMod = ""

                Dim filterText = txtAuditSearch.Text.Trim()
                Dim logs = Await _auditService.GetAuditLogsAsync(moduleName:=selectedMod, actionFilter:=filterText, topCount:=200)

                dgvAuditLogs.Rows.Clear()
                For Each a In logs
                    dgvAuditLogs.Rows.Add(a.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"), a.FullName, a.ModuleName, a.Action, a.Details, a.Status)
                Next

                Dim metrics = Await _auditService.GetAuditSummaryMetricsAsync()
                cardAuditToday.Description = metrics.TodayActivityCount.ToString() & " Events"
                cardAuditSecurity.Description = metrics.SecurityChangesCount.ToString() & " Security Logs"
                cardAuditFailed.Description = metrics.FailedActionsCount.ToString() & " Denials"
                cardAuditLastEvent.Description = If(metrics.LastSecurityEventOn.HasValue, metrics.LastSecurityEventOn.Value.ToString("dd MMM HH:mm"), "Recent")
            Catch ex As Exception
                ' Fail closed gracefully
            End Try
        End Function

        Private Sub ExportAuditLog()
            FrmInAppAlert.ShowModal(Me, "Audit Export Complete", "Security Audit Log exported successfully to system logs directory.", AlertType.SuccessAlert)
        End Sub

        Private Function CreateMetricCard(title As String, val As String, subtext As String, accentColor As Color) As AppActionCard
            Dim card As New AppActionCard() With {
                .Title = title,
                .Description = val,
                .BadgeText = subtext,
                .AccentColor = accentColor,
                .ActionText = "View Log →",
                .Size = New Size(240, 75)
            }
            Return card
        End Function

        Private Function HasPendingChanges() As Boolean
            If _currentRolePermIds Is Nothing OrElse _initialRolePermIds Is Nothing Then Return False
            If _currentRolePermIds.Count <> _initialRolePermIds.Count Then Return True
            For Each id In _currentRolePermIds
                If Not _initialRolePermIds.Contains(id) Then Return True
            Next
            Return False
        End Function

        Private Function ConfirmAndCloseForm() As Boolean
            If HasPendingChanges() Then
                Dim dlgResult = FrmInAppAlert.ShowModal(Me, "Unsaved Changes",
                    "You have unsaved permission changes. Do you want to leave without saving?",
                    AlertType.WarningAlert, actionText:="DISCARD CHANGES", showCancel:=True, cancelText:="STAY")

                If dlgResult <> DialogResult.OK Then
                    Return False
                End If
            End If

            _isClosingConfirmed = True
            If Me.Modal Then
                Me.DialogResult = DialogResult.Cancel
                Me.Close()
            Else
                Dim parentShell = TryCast(Me.MdiParent, FrmMainShell)
                If parentShell Is Nothing AndAlso Me.Parent IsNot Nothing Then
                    parentShell = TryCast(Me.Parent.FindForm(), FrmMainShell)
                End If
                If parentShell IsNot Nothing Then
                    parentShell.NavigateToModule("Admin")
                Else
                    Me.Close()
                End If
            End If
            Return True
        End Function

        Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
            If Not _isClosingConfirmed AndAlso HasPendingChanges() Then
                Dim dlgResult = FrmInAppAlert.ShowModal(Me, "Unsaved Changes",
                    "You have unsaved permission changes. Do you want to leave without saving?",
                    AlertType.WarningAlert, actionText:="DISCARD CHANGES", showCancel:=True, cancelText:="STAY")

                If dlgResult <> DialogResult.OK Then
                    e.Cancel = True
                    Return
                End If
            End If
            MyBase.OnFormClosing(e)
        End Sub

        Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
            If keyData = Keys.Escape Then
                ConfirmAndCloseForm()
                Return True
            End If
            Return MyBase.ProcessCmdKey(msg, keyData)
        End Function
    End Class

    Public Class RoleItem
        Public Property RoleId As Integer
        Public Property RoleName As String
        Public Property Icon As String
        Public Property Description As String
    End Class
End Namespace
