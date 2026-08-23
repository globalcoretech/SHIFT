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
    ''' Staff Account and Access Setup Form delegating user creation, role assignment,
    ''' password resets, and effective permission previews to BLL services.
    ''' </summary>
    Public Class FrmUserManagement
        Private ReadOnly _userService As UserService
        Private ReadOnly _userRepo As IUserRepository
        Private ReadOnly _rolePermService As IRolePermissionService
        Private ReadOnly _userOverrideService As IUserPermissionOverrideService
        Private ReadOnly _permRepo As IPermissionRepository

        Private _userList As List(Of UserDto) = New List(Of UserDto)()
        Private _selectedUserId As Integer = 0
        Private _selectedRole As UserRole = UserRole.Employee

        ' --- GUIDED WORKFLOW CONTROLS ---
        Private pnlStep1 As Panel
        Private pnlStep2 As Panel
        Private pnlStep3 As Panel
        Private pnlRoleCardsTable As TableLayoutPanel

        Private cardOwnerRole As Panel
        Private cardAdminRole As Panel
        Private cardEmployeeRole As Panel

        Private lblRoleSummaryBanner As Label
        Private lblAccessCounters As Label
        Private lblCanList As Label
        Private lblRestrictedList As Label
        Private btnViewEffectiveAccess As KryptonButton
        Private btnManageExceptions As KryptonButton

        Private btnHeaderBack As KryptonButton
        Private btnHeaderClose As KryptonButton
        Private _isClosingConfirmed As Boolean = False

        Public Sub New()
            InitializeComponent()
            Dim config As IAppConfiguration = New AppConfiguration()
            Dim connFactory As IDatabaseConnectionFactory = New DbConnectionFactory(config)
            Dim sqlHelper As ISqlHelper = New SqlHelper(connFactory)
            _userRepo = New UserRepository(sqlHelper)
            _permRepo = New PermissionRepository(sqlHelper)

            Dim hasher As New PasswordHasher()
            Dim appLogger As IAppLogger = New AppLogger(config)
            Dim auditLogger As New AuditLogger(sqlHelper)
            Dim rolePermRepo = New RolePermissionRepository(sqlHelper, connFactory)
            Dim userOverrideRepo = New UserPermissionOverrideRepository(sqlHelper)

            _userService = New UserService(_userRepo, hasher, appLogger, auditLogger)
            _rolePermService = New RolePermissionService(rolePermRepo, _permRepo, appLogger, auditLogger)
            _userOverrideService = New UserPermissionOverrideService(userOverrideRepo, _permRepo, _userRepo, appLogger, auditLogger)
        End Sub

        Private Async Sub FrmUserManagement_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            Dim authz As IAuthorizationService = New AuthorizationService()
            If Not authz.IsAuthorized(UserRole.Admin) AndAlso Not authz.IsAuthorized(UserRole.Owner) Then
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Access Denied", "Access Denied: Only System Administrators or Owners can manage staff accounts.", Forms.Common.AlertType.WarningAlert, actionText:="OK")
                Me.Close()
                Return
            End If

            ' Setup Top-Right Navigation Header Controls
            Dim pnlHeaderNav As New FlowLayoutPanel() With {
                .Dock = DockStyle.Right,
                .AutoSize = True,
                .FlowDirection = FlowDirection.LeftToRight,
                .WrapContents = False,
                .BackColor = Color.Transparent,
                .Padding = New Padding(0, 14, 16, 0)
            }

            btnHeaderBack = New KryptonButton() With {
                .Text = "← Back to Administration",
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

            Me.KeyPreview = True
            Me.ControlBox = True
            Me.CancelButton = btnHeaderClose

            ThemeConstants.ApplyModernGridStyle(dgvUsers)
            EnsureUserGridColumns()
            cboRole.DataSource = [Enum].GetValues(GetType(UserRole))
            cboDept.DataSource = [Enum].GetValues(GetType(DepartmentType))

            BuildGuidedWorkflowUI()
            Await RefreshUserGridAsync()
            ClearFormInputs()
        End Sub

        Private Sub BuildGuidedWorkflowUI()
            pnlForm.Controls.Clear()
            pnlForm.Padding = New Padding(12)
            pnlForm.AutoScroll = True

            ' --- STEP 1: STAFF IDENTITY ---
            pnlStep1 = New Panel() With {.Dock = DockStyle.Top, .Height = 135, .BackColor = Color.White, .Padding = New Padding(8)}
            ThemeConstants.ApplyCardSurfaceStyle(pnlStep1)

            Dim lblStep1Title As New Label() With {
                .Text = "STEP 1 — STAFF IDENTITY",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(8, 8),
                .AutoSize = True
            }

            lblUsername.Location = New Point(12, 30)
            txtUsername.Location = New Point(12, 48)
            txtUsername.Size = New Size(260, 26)

            lblFullName.Location = New Point(290, 30)
            txtFullName.Location = New Point(290, 48)
            txtFullName.Size = New Size(280, 26)

            lblDept.Location = New Point(12, 80)
            cboDept.Location = New Point(12, 98)
            cboDept.Size = New Size(260, 26)

            chkActive.Location = New Point(290, 100)

            pnlStep1.Controls.Add(lblStep1Title)
            pnlStep1.Controls.Add(lblUsername)
            pnlStep1.Controls.Add(txtUsername)
            pnlStep1.Controls.Add(lblFullName)
            pnlStep1.Controls.Add(txtFullName)
            pnlStep1.Controls.Add(lblDept)
            pnlStep1.Controls.Add(cboDept)
            pnlStep1.Controls.Add(chkActive)

            ' --- STEP 2: ORGANISATIONAL ROLE ---
            pnlStep2 = New Panel() With {.Dock = DockStyle.Top, .Height = 280, .BackColor = Color.White, .Margin = New Padding(0, 10, 0, 0), .Padding = New Padding(8)}
            ThemeConstants.ApplyCardSurfaceStyle(pnlStep2)

            Dim lblStep2Title As New Label() With {
                .Text = "STEP 2 — ORGANISATIONAL ROLE",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(8, 8),
                .AutoSize = True
            }

            Dim lblStep2Sub As New Label() With {
                .Text = "Select the organisational role for this staff member:",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.0!, FontStyle.Italic),
                .ForeColor = ThemeConstants.TextSecondary,
                .Location = New Point(8, 26),
                .AutoSize = True
            }

            pnlRoleCardsTable = New TableLayoutPanel() With {
                .Location = New Point(8, 44),
                .Size = New Size(580, 165),
                .ColumnCount = 3,
                .RowCount = 1
            }
            pnlRoleCardsTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33.33!))
            pnlRoleCardsTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33.33!))
            pnlRoleCardsTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33.33!))
            pnlRoleCardsTable.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0!))

            BuildSelectableRoleCards()

            lblRoleSummaryBanner = New Label() With {
                .Location = New Point(8, 218),
                .Size = New Size(580, 48),
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.PrimaryAccent,
                .BackColor = Color.FromArgb(240, 244, 255),
                .Padding = New Padding(8, 4, 8, 4),
                .Text = "SELECTED ROLE: 👤 EMPLOYEE / STAFF" & vbCrLf & "This staff member will receive standard Employee access for daily operational tasks."
            }

            pnlStep2.Controls.Add(lblStep2Title)
            pnlStep2.Controls.Add(lblStep2Sub)
            pnlStep2.Controls.Add(pnlRoleCardsTable)
            pnlStep2.Controls.Add(lblRoleSummaryBanner)

            ' --- STEP 3: EFFECTIVE ACCESS PREVIEW ---
            pnlStep3 = New Panel() With {.Dock = DockStyle.Top, .Height = 250, .BackColor = Color.White, .Margin = New Padding(0, 10, 0, 0), .Padding = New Padding(8)}
            ThemeConstants.ApplyCardSurfaceStyle(pnlStep3)

            Dim lblStep3Title As New Label() With {
                .Text = "STEP 3 — EFFECTIVE ACCESS PREVIEW",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(8, 8),
                .AutoSize = True
            }

            lblAccessCounters = New Label() With {
                .Location = New Point(8, 28),
                .Size = New Size(580, 24),
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Text = "Inherited Role Permissions: 8  |  Individual Grants: 0  |  Explicit Denies: 0  |  Effective Access: 8 Available"
            }

            Dim pnlListsHost As New Panel() With {.Location = New Point(8, 54), .Size = New Size(580, 140)}

            lblCanList = New Label() With {
                .Location = New Point(0, 0),
                .Size = New Size(280, 135),
                .Font = New Font(ThemeConstants.FontNameDefault, 8.0!),
                .ForeColor = Color.FromArgb(22, 101, 52),
                .Text = "THIS USER CAN:" & vbCrLf & "• View assigned clients" & vbCrLf & "• Manage attendance & timesheets" & vbCrLf & "• Create & update authorized task records"
            }

            lblRestrictedList = New Label() With {
                .Location = New Point(290, 0),
                .Size = New Size(280, 135),
                .Font = New Font(ThemeConstants.FontNameDefault, 8.0!),
                .ForeColor = Color.FromArgb(153, 27, 27),
                .Text = "RESTRICTED FROM:" & vbCrLf & "• Security administration & role matrix" & vbCrLf & "• User access exception overrides" & vbCrLf & "• System disaster recovery & database backups"
            }

            pnlListsHost.Controls.Add(lblCanList)
            pnlListsHost.Controls.Add(lblRestrictedList)

            ' Action buttons connecting with Security Center
            btnViewEffectiveAccess = New KryptonButton() With {.Text = "View Full Role Access", .Location = New Point(8, 200), .Size = New Size(260, 34)}
            ThemeConstants.ApplyKryptonSecondaryButton(btnViewEffectiveAccess)
            AddHandler btnViewEffectiveAccess.Click, Sub(s, e) OpenSecurityCenterRoleWorkspace()

            btnManageExceptions = New KryptonButton() With {.Text = "Manage Individual Exceptions", .Location = New Point(280, 200), .Size = New Size(290, 34)}
            ThemeConstants.ApplyKryptonSecondaryButton(btnManageExceptions)
            AddHandler btnManageExceptions.Click, Sub(s, e) OpenSecurityCenterExceptionsWorkspace()

            pnlStep3.Controls.Add(lblStep3Title)
            pnlStep3.Controls.Add(lblAccessCounters)
            pnlStep3.Controls.Add(pnlListsHost)
            pnlStep3.Controls.Add(btnViewEffectiveAccess)
            pnlStep3.Controls.Add(btnManageExceptions)

            ' Form Action Buttons Panel
            Dim pnlActions As New Panel() With {.Dock = DockStyle.Bottom, .Height = 50, .BackColor = Color.Transparent}
            btnNew.Location = New Point(0, 8)
            btnNew.Size = New Size(140, 36)

            btnResetPassword.Location = New Point(150, 8)
            btnResetPassword.Size = New Size(200, 36)

            btnSave.Location = New Point(360, 8)
            btnSave.Size = New Size(220, 36)

            pnlActions.Controls.Add(btnNew)
            pnlActions.Controls.Add(btnResetPassword)
            pnlActions.Controls.Add(btnSave)

            pnlForm.Controls.Add(pnlStep3)
            pnlForm.Controls.Add(pnlStep2)
            pnlForm.Controls.Add(pnlStep1)
            pnlForm.Controls.Add(pnlActions)
        End Sub

        Private Sub BuildSelectableRoleCards()
            pnlRoleCardsTable.Controls.Clear()

            cardOwnerRole = CreateRoleCard(UserRole.Owner, "👑", "OWNER", "EXECUTIVE CONTROL", "Full firm visibility & strategic control.", "25 / 25 Granted")
            cardAdminRole = CreateRoleCard(UserRole.Admin, "🛡️", "ADMINISTRATOR", "OPERATIONAL ADMIN", "Manage staff, operations & admin controls.", "18 / 25 Granted")
            cardEmployeeRole = CreateRoleCard(UserRole.Employee, "👤", "EMPLOYEE / STAFF", "STAFF USER", "Perform assigned daily tasks & time logs.", "8 / 25 Granted")

            pnlRoleCardsTable.Controls.Add(cardOwnerRole, 0, 0)
            pnlRoleCardsTable.Controls.Add(cardAdminRole, 1, 0)
            pnlRoleCardsTable.Controls.Add(cardEmployeeRole, 2, 0)
        End Sub

        Private Function CreateRoleCard(role As UserRole, iconStr As String, roleTitle As String, subtitle As String, desc As String, permSummary As String) As Panel
            Dim isSelected = (_selectedRole = role)
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
            tblLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 20.0!))  ' Subtitle
            tblLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 58.0!))  ' Description & Perm Summary
            tblLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 26.0!))  ' Status badge

            Dim pnlAccent As New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = If(isSelected, ThemeConstants.PrimaryAccent, Color.FromArgb(226, 232, 240))
            }

            Dim lblTitleCard As New Label() With {
                .Text = $"{iconStr} {roleTitle}",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold),
                .ForeColor = If(isSelected, ThemeConstants.PrimaryAccent, ThemeConstants.TextPrimary),
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleLeft,
                .AutoEllipsis = False
            }

            Dim lblSubCard As New Label() With {
                .Text = subtitle,
                .Font = New Font(ThemeConstants.FontNameDefault, 7.0!, FontStyle.Bold),
                .ForeColor = If(isSelected, ThemeConstants.PrimaryAccent, ThemeConstants.TextMuted),
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleLeft,
                .AutoEllipsis = False
            }

            Dim lblDescCard As New Label() With {
                .Text = desc & vbCrLf & permSummary,
                .Font = New Font(ThemeConstants.FontNameDefault, 7.5!, FontStyle.Regular),
                .ForeColor = ThemeConstants.TextSecondary,
                .Dock = DockStyle.Fill,
                .AutoEllipsis = False
            }

            Dim lblBadge As New Label() With {
                .Text = If(isSelected, "✓ CURRENTLY SELECTED", "CLICK TO SELECT"),
                .Font = New Font(ThemeConstants.FontNameDefault, 7.5!, FontStyle.Bold),
                .ForeColor = If(isSelected, Color.FromArgb(67, 56, 202), Color.FromArgb(100, 116, 139)),
                .BackColor = If(isSelected, Color.FromArgb(224, 231, 255), Color.FromArgb(241, 245, 249)),
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleCenter
            }

            tblLayout.Controls.Add(pnlAccent, 0, 0)
            tblLayout.Controls.Add(lblTitleCard, 0, 1)
            tblLayout.Controls.Add(lblSubCard, 0, 2)
            tblLayout.Controls.Add(lblDescCard, 0, 3)
            tblLayout.Controls.Add(lblBadge, 0, 4)

            card.Controls.Add(tblLayout)

            AddHandler card.Click, Async Sub(s, e) Await SelectRoleAsync(role)
            AddHandler tblLayout.Click, Async Sub(s, e) Await SelectRoleAsync(role)
            AddHandler pnlAccent.Click, Async Sub(s, e) Await SelectRoleAsync(role)
            AddHandler lblTitleCard.Click, Async Sub(s, e) Await SelectRoleAsync(role)
            AddHandler lblSubCard.Click, Async Sub(s, e) Await SelectRoleAsync(role)
            AddHandler lblDescCard.Click, Async Sub(s, e) Await SelectRoleAsync(role)
            AddHandler lblBadge.Click, Async Sub(s, e) Await SelectRoleAsync(role)

            Return card
        End Function

        Private Async Function SelectRoleAsync(role As UserRole) As Task
            _selectedRole = role
            cboRole.SelectedItem = role
            HighlightActiveRoleCard()
            Await UpdateEffectiveAccessPreviewAsync()
        End Function

        Private Sub HighlightActiveRoleCard()
            BuildSelectableRoleCards()

            Dim roleIcon = If(_selectedRole = UserRole.Owner, "👑", If(_selectedRole = UserRole.Admin, "🛡️", "👤"))
            Dim roleTitle = If(_selectedRole = UserRole.Owner, "OWNER", If(_selectedRole = UserRole.Admin, "ADMINISTRATOR", "EMPLOYEE / STAFF"))

            Dim descText As String = ""
            If _selectedRole = UserRole.Owner Then
                descText = "This staff member will receive full firm visibility and executive strategic control."
            ElseIf _selectedRole = UserRole.Admin Then
                descText = "This staff member will inherit standard Administrator access to manage staff and operations."
            Else
                descText = "This staff member will receive standard Employee access for daily operational tasks."
            End If

            lblRoleSummaryBanner.Text = $"SELECTED ROLE: {roleIcon} {roleTitle}" & vbCrLf & descText
        End Sub

        Private Async Function UpdateEffectiveAccessPreviewAsync() As Task
            Try
                Dim grantedPerms = Await _rolePermService.GetPermissionsForRoleAsync(CType(_selectedRole, Integer))
                Dim totalPerms = Await _permRepo.GetAllAsync()
                Dim userOverrides = If(_selectedUserId > 0, Await _userOverrideService.GetOverridesForUserAsync(_selectedUserId), New List(Of UserPermissionOverrideDto)())
                Dim addCount = userOverrides.FindAll(Function(o) o.IsGranted).Count
                Dim removeCount = userOverrides.FindAll(Function(o) Not o.IsGranted).Count
                Dim effectiveCount = Math.Max(0, grantedPerms.Count + addCount - removeCount)

                lblAccessCounters.Text = $"Inherited Role Permissions: {grantedPerms.Count}  |  Individual Grants: {addCount}  |  Explicit Denies: {removeCount}  |  Effective Access: {effectiveCount} Available"

                If _selectedRole = UserRole.Owner Then
                    lblCanList.Text = "THIS USER CAN:" & vbCrLf & "• Executive strategic firm controls" & vbCrLf & "• Manage user provisioning roles" & vbCrLf & "• Full financial compliance access" & vbCrLf & "• Audit logs disaster recovery"
                    lblRestrictedList.Text = "RESTRICTED FROM:" & vbCrLf & "• No restrictions applied" & vbCrLf & "• Owner possesses super-admin authority"
                ElseIf _selectedRole = UserRole.Admin Then
                    lblCanList.Text = "THIS USER CAN:" & vbCrLf & "• Manage staff accounts & password resets" & vbCrLf & "• Manage client registry & GST records" & vbCrLf & "• Supervise attendance & task workflows" & vbCrLf & "• Execute compliance automation"
                    lblRestrictedList.Text = "RESTRICTED FROM:" & vbCrLf & "• System disaster recovery & DB restore" & vbCrLf & "• Super-admin strategic owner overrides"
                Else
                    lblCanList.Text = "THIS USER CAN:" & vbCrLf & "• View assigned clients" & vbCrLf & "• Manage attendance & timesheets" & vbCrLf & "• Create & update authorized task records"
                    lblRestrictedList.Text = "RESTRICTED FROM:" & vbCrLf & "• Security administration & role matrix" & vbCrLf & "• User access exception overrides" & vbCrLf & "• System disaster recovery & database backups"
                End If
            Catch ex As Exception
                ' Silent preview fallback
            End Try
        End Function

        Private Sub OpenSecurityCenterRoleWorkspace()
            Using dlg As New FrmSecurityCenter(defaultTabIndex:=0, targetRoleId:=CInt(_selectedRole))
                dlg.ShowDialog(Me)
            End Using
        End Sub

        Private Sub OpenSecurityCenterExceptionsWorkspace()
            Using dlg As New FrmSecurityCenter(defaultTabIndex:=1, targetUserId:=_selectedUserId)
                dlg.ShowDialog(Me)
            End Using
        End Sub

        Private Sub EnsureUserGridColumns()
            If dgvUsers Is Nothing OrElse dgvUsers.Columns.Count > 0 Then Return

            dgvUsers.Columns.Clear()
            dgvUsers.AutoGenerateColumns = False

            Dim colId As New DataGridViewTextBoxColumn() With {
                .Name = "colUserId",
                .HeaderText = "ID",
                .DataPropertyName = "UserId",
                .Width = 50,
                .Visible = False
            }

            Dim colUsername As New DataGridViewTextBoxColumn() With {
                .Name = "colUsername",
                .HeaderText = "Username",
                .DataPropertyName = "Username",
                .Width = 110
            }

            Dim colFullName As New DataGridViewTextBoxColumn() With {
                .Name = "colFullName",
                .HeaderText = "Full Name",
                .DataPropertyName = "FullName",
                .Width = 150
            }

            Dim colRole As New DataGridViewTextBoxColumn() With {
                .Name = "colRole",
                .HeaderText = "Role",
                .DataPropertyName = "Role",
                .Width = 100
            }

            Dim colDept As New DataGridViewTextBoxColumn() With {
                .Name = "colDepartment",
                .HeaderText = "Department",
                .DataPropertyName = "Department",
                .Width = 110
            }

            Dim colMustChange As New DataGridViewCheckBoxColumn() With {
                .Name = "colMustChangePw",
                .HeaderText = "Reset Pw",
                .DataPropertyName = "MustChangePassword",
                .Width = 75
            }

            Dim colIsActive As New DataGridViewCheckBoxColumn() With {
                .Name = "colIsActive",
                .HeaderText = "Active Status",
                .DataPropertyName = "IsActive",
                .Width = 80
            }

            dgvUsers.Columns.AddRange(New DataGridViewColumn() {
                colId, colUsername, colFullName, colRole, colDept, colMustChange, colIsActive
            })
        End Sub

        Private Async Function RefreshUserGridAsync() As Task
            Try
                Me.Cursor = Cursors.WaitCursor
                EnsureUserGridColumns()
                If dgvUsers.Columns.Count = 0 Then Return

                _userList = Await _userService.GetAllUsersAsync()
                dgvUsers.Rows.Clear()

                If _userList IsNot Nothing Then
                    For Each u In _userList
                        dgvUsers.Rows.Add(u.UserId, u.Username, u.FullName, u.Role.ToString(), u.Department.ToString(), u.MustChangePassword, u.IsActive)
                    Next
                End If
            Catch ex As Exception
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Error", "Failed to load user records: " & ex.Message, AlertType.ErrorAlert, actionText:="OK")
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Function

        Private Sub ApplyFilter()
            EnsureUserGridColumns()
            If dgvUsers.Columns.Count = 0 Then Return

            Dim searchText = txtSearch.Text.Trim().ToLower()
            dgvUsers.Rows.Clear()

            If _userList IsNot Nothing Then
                For Each u In _userList
                    If String.IsNullOrEmpty(searchText) OrElse u.Username.ToLower().Contains(searchText) OrElse u.FullName.ToLower().Contains(searchText) Then
                        dgvUsers.Rows.Add(u.UserId, u.Username, u.FullName, u.Role.ToString(), u.Department.ToString(), u.MustChangePassword, u.IsActive)
                    End If
                Next
            End If
        End Sub

        Private Sub txtSearch_TextChanged(sender As Object, e As EventArgs) Handles txtSearch.TextChanged
            ApplyFilter()
        End Sub

        Private Async Sub dgvUsers_SelectionChanged(sender As Object, e As EventArgs) Handles dgvUsers.SelectionChanged
            If dgvUsers.SelectedRows.Count = 0 Then Return

            Dim selectedRow = dgvUsers.SelectedRows(0)
            If selectedRow.Cells("colUserId").Value IsNot Nothing Then
                _selectedUserId = Convert.ToInt32(selectedRow.Cells("colUserId").Value)
                Dim user = _userList.Find(Function(u) u.UserId = _selectedUserId)

                If user IsNot Nothing Then
                    txtUsername.Text = user.Username
                    txtFullName.Text = user.FullName
                    cboRole.SelectedItem = user.Role
                    cboDept.SelectedItem = user.Department
                    chkActive.Checked = user.IsActive
                    _selectedRole = user.Role
                    HighlightActiveRoleCard()
                    Await UpdateEffectiveAccessPreviewAsync()
                End If
            End If
        End Sub

        Private Sub btnNew_Click(sender As Object, e As EventArgs) Handles btnNew.Click
            ClearFormInputs()
        End Sub

        Private Sub ClearFormInputs()
            _selectedUserId = 0
            txtUsername.Text = String.Empty
            txtFullName.Text = String.Empty
            cboRole.SelectedItem = UserRole.Employee
            cboDept.SelectedItem = DepartmentType.Administration
            chkActive.Checked = True
            _selectedRole = UserRole.Employee
            HighlightActiveRoleCard()
            dgvUsers.ClearSelection()
        End Sub

        Private Async Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
            Dim authz As IAuthorizationService = New AuthorizationService()
            If Not authz.IsAuthorized(UserRole.Admin) AndAlso Not authz.IsAuthorized(UserRole.Owner) Then
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Access Denied", "Access Denied: Only System Administrators or Owners can save staff user accounts.", AlertType.WarningAlert, actionText:="OK")
                Return
            End If

            If String.IsNullOrWhiteSpace(txtUsername.Text) OrElse String.IsNullOrWhiteSpace(txtFullName.Text) Then
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Validation Error", "Username and Full Name are required fields.", AlertType.WarningAlert, actionText:="OK")
                Return
            End If

            Try
                Me.Cursor = Cursors.WaitCursor

                If _selectedUserId = 0 Then
                    Dim hasher As New PasswordHasher()
                    Dim tempPassword = hasher.GenerateTemporaryPassword(12)
                    Dim defaultSalt As String = String.Empty
                    Dim defaultHash = hasher.HashPassword(tempPassword, defaultSalt)

                    Dim entity As New Core.Entities.UserEntity() With {
                        .Username = txtUsername.Text.Trim().ToLower(),
                        .FullName = txtFullName.Text.Trim(),
                        .PasswordHash = defaultHash,
                        .PasswordSalt = defaultSalt,
                        .Role = _selectedRole,
                        .Department = CType(cboDept.SelectedItem, DepartmentType),
                        .MustChangePassword = True,
                        .IsActive = chkActive.Checked,
                        .CreatedBy = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)
                    }

                    Dim newId = Await _userRepo.AddAsync(entity)
                    Forms.Common.FrmInAppAlert.ShowModal(Me, "User Created", $"Staff user '{entity.Username}' created successfully.{Environment.NewLine}Temporary Password: {tempPassword}", AlertType.SuccessAlert, actionText:="OK")
                Else
                    Dim existing = Await _userRepo.GetByIdAsync(_selectedUserId)
                    If existing IsNot Nothing Then
                        existing.FullName = txtFullName.Text.Trim()
                        existing.Role = _selectedRole
                        existing.Department = CType(cboDept.SelectedItem, DepartmentType)
                        existing.IsActive = chkActive.Checked
                        existing.ModifiedBy = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)

                        Await _userRepo.UpdateAsync(existing)
                        Forms.Common.FrmInAppAlert.ShowModal(Me, "User Updated", $"Staff user '{existing.Username}' updated successfully.", AlertType.SuccessAlert, actionText:="OK")
                    End If
                End If

                Await RefreshUserGridAsync()
                ClearFormInputs()
            Catch ex As Exception
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Error", "Failed to save user account: " & ex.Message, AlertType.ErrorAlert, actionText:="OK")
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Sub

        Private Async Sub btnResetPassword_Click(sender As Object, e As EventArgs) Handles btnResetPassword.Click
            Dim authz As IAuthorizationService = New AuthorizationService()
            If Not authz.IsAuthorized(UserRole.Admin) AndAlso Not authz.IsAuthorized(UserRole.Owner) Then
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Access Denied", "Access Denied: Only System Administrators or Owners can reset user passwords.", AlertType.WarningAlert, actionText:="OK")
                Return
            End If

            If _selectedUserId = 0 Then
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Selection Required", "Please select a user from the grid to reset password.", AlertType.WarningAlert, actionText:="OK")
                Return
            End If

            Dim confirm = Forms.Common.FrmInAppAlert.ShowModal(Me, "Confirm Password Reset", $"Generate a new cryptographically secure temporary password for user '{txtUsername.Text}'?", AlertType.WarningAlert, actionText:="YES, RESET PASSWORD", showCancel:=True, cancelText:="CANCEL")
            If confirm = DialogResult.OK Then
                Try
                    Me.Cursor = Cursors.WaitCursor
                    Dim existing = Await _userRepo.GetByIdAsync(_selectedUserId)
                    If existing IsNot Nothing Then
                        Dim hasher As New PasswordHasher()
                        Dim tempPassword = hasher.GenerateTemporaryPassword(12)
                        Dim newSalt As String = String.Empty
                        Dim newHash = hasher.HashPassword(tempPassword, newSalt)

                        existing.PasswordHash = newHash
                        existing.PasswordSalt = newSalt
                        existing.MustChangePassword = True
                        existing.ModifiedBy = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)

                        Await _userRepo.UpdateAsync(existing)
                        Forms.Common.FrmInAppAlert.ShowModal(Me, "Password Reset", $"Password reset successfully for '{existing.Username}'.{Environment.NewLine}{Environment.NewLine}New Temporary Password: {tempPassword}", AlertType.SuccessAlert, actionText:="OK")
                    End If
                Catch ex As Exception
                    Forms.Common.FrmInAppAlert.ShowModal(Me, "Error", "Failed to reset password: " & ex.Message, AlertType.ErrorAlert, actionText:="OK")
                Finally
                    Me.Cursor = Cursors.Default
                End Try
            End If
        End Sub

        Private Function HasPendingChanges() As Boolean
            Return _selectedUserId = 0 AndAlso (Not String.IsNullOrWhiteSpace(txtUsername.Text) OrElse Not String.IsNullOrWhiteSpace(txtFullName.Text))
        End Function

        Private Function ConfirmAndCloseForm() As Boolean
            If HasPendingChanges() Then
                Dim dlgResult = FrmInAppAlert.ShowModal(Me, "Unsaved Changes",
                    "You have unsaved staff account changes. Do you want to leave without saving?",
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
                    "You have unsaved staff account changes. Do you want to leave without saving?",
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
End Namespace
