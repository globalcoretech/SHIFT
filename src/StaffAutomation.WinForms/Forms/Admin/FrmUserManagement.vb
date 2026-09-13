Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Linq
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports Microsoft.Data.SqlClient
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

        Private btnHeaderBack As KryptonButton
        Private btnHeaderClose As KryptonButton
        Private _isClosingConfirmed As Boolean = False
        Private _lastSelectedRowIndex As Integer = -1
        Private _isRestoringSelection As Boolean = False
        Private _lnkAdvanced As LinkLabel
        Private _txtPassword As TextBox

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
            .Text = "← Back to Settings",
            .Size = New Size(140, 32),
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
            Me.MinimumSize = New Size(760, 520)

            ThemeConstants.ApplyModernGridStyle(dgvUsers)
            ThemeConstants.ApplyStandardInputStyle(txtUsername)
            ThemeConstants.ApplyStandardInputStyle(txtFullName)
            ThemeConstants.ApplyStandardInputStyle(txtSearch)
            ThemeConstants.ApplyStandardInputStyle(cboDept)
            ThemeConstants.ApplyStandardInputStyle(cboRole)
            EnsureUserGridColumns()
            
            cboRole.DataSource = [Enum].GetValues(GetType(UserRole)).Cast(Of UserRole)().Select(Function(r) New With { .Key = r, .Value = r.ToString() }).ToList()
            cboRole.DisplayMember = "Value"
            cboRole.ValueMember = "Key"
            
            cboDept.DataSource = [Enum].GetValues(GetType(DepartmentType)).Cast(Of DepartmentType)().Select(Function(d) New With { .Key = d, .Value = d.ToString() }).ToList()
            cboDept.DisplayMember = "Value"
            cboDept.ValueMember = "Key"

            BuildStaffDetailsUI()
            Await RefreshUserGridAsync()
            ClearFormInputs()
        End Sub

        Private Sub BuildStaffDetailsUI()
            pnlForm.Controls.Clear()
            pnlForm.Padding = New Padding(12)
            pnlForm.AutoScroll = True

            Dim pnlDetails As New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.White, .Padding = New Padding(12)}
            ThemeConstants.ApplyCardSurfaceStyle(pnlDetails)

            Dim lblTitle As New Label() With {
                .Text = "STAFF DETAILS",
                .Font = New Font(ThemeConstants.FontNameDefault, 10.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(12, 12),
                .AutoSize = True
            }
            pnlDetails.Controls.Add(lblTitle)

            lblFullName.Location = New Point(12, 40)
            txtFullName.Location = New Point(12, 60)
            txtFullName.Size = New Size(300, 26)

            lblUsername.Location = New Point(12, 90)
            txtUsername.Location = New Point(12, 110)
            txtUsername.Size = New Size(300, 26)

            Dim lblPassword As New Label() With {.Text = "Set Password", .Location = New Point(330, 90), .AutoSize = True}
            lblPassword.Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular)
            lblPassword.ForeColor = ThemeConstants.TextPrimary

            _txtPassword = New TextBox() With {.Location = New Point(330, 110), .Size = New Size(210, 26), .UseSystemPasswordChar = True}
            ThemeConstants.ApplyStandardInputStyle(_txtPassword)

            lblDept.Location = New Point(12, 140)
            cboDept.Location = New Point(12, 160)
            cboDept.Size = New Size(300, 26)

            Dim lblRole As New Label() With {.Text = "Role", .Location = New Point(12, 190), .AutoSize = True}
            lblRole.Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular)
            lblRole.ForeColor = ThemeConstants.TextPrimary
            cboRole.Location = New Point(12, 210)
            cboRole.Size = New Size(300, 26)
            cboRole.Visible = True

            chkActive.Location = New Point(12, 250)
            
            _lnkAdvanced = New LinkLabel() With {
                .Text = "Advanced — customize permissions",
                .Location = New Point(12, 280),
                .AutoSize = True,
                .LinkColor = ThemeConstants.PrimaryAccent,
                .Visible = False
            }
            AddHandler _lnkAdvanced.LinkClicked, Sub(s, e)
                                                    Using dlg As New FrmSecurityCenter(defaultTabIndex:=1, targetUserId:=_selectedUserId)
                                                        dlg.ShowDialog(Me)
                                                    End Using
                                                End Sub

            pnlDetails.Controls.Add(lblFullName)
            pnlDetails.Controls.Add(txtFullName)
            pnlDetails.Controls.Add(lblUsername)
            pnlDetails.Controls.Add(txtUsername)
            pnlDetails.Controls.Add(lblPassword)
            pnlDetails.Controls.Add(_txtPassword)
            pnlDetails.Controls.Add(lblDept)
            pnlDetails.Controls.Add(cboDept)
            pnlDetails.Controls.Add(lblRole)
            pnlDetails.Controls.Add(cboRole)
            pnlDetails.Controls.Add(chkActive)
            pnlDetails.Controls.Add(_lnkAdvanced)

            ' Form Action Buttons Panel
            Dim pnlActions As New Panel() With {.Dock = DockStyle.Bottom, .Height = 50, .BackColor = Color.Transparent}
            btnSave.Text = "💾 Save Account"
            btnSave.Location = New Point(12, 8)
            btnSave.Size = New Size(140, 36)

            btnNew.Text = "Cancel"
            btnNew.Location = New Point(160, 8)
            btnNew.Size = New Size(100, 36)
            btnNew.FlatStyle = FlatStyle.Flat
            btnNew.FlatAppearance.BorderColor = Color.LightGray
            btnNew.BackColor = Color.White
            btnNew.ForeColor = ThemeConstants.TextPrimary

            btnResetPassword.Location = New Point(270, 8)
            btnResetPassword.Size = New Size(150, 36)
            btnResetPassword.FlatStyle = FlatStyle.Flat
            btnResetPassword.FlatAppearance.BorderColor = Color.LightGray
            btnResetPassword.BackColor = Color.White
            btnResetPassword.ForeColor = ThemeConstants.TextPrimary

            pnlActions.Controls.Add(btnSave)
            pnlActions.Controls.Add(btnNew)
            pnlActions.Controls.Add(btnResetPassword)

            pnlForm.Controls.Add(pnlActions)
            pnlForm.Controls.Add(pnlDetails)
            
            AddHandler cboRole.SelectedIndexChanged, Sub(s, e)
                                                         If cboRole.SelectedValue IsNot Nothing Then
                                                             _selectedRole = CType(cboRole.SelectedValue, UserRole)
                                                         End If
                                                     End Sub
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

        Private Sub dgvUsers_SelectionChanged(sender As Object, e As EventArgs) Handles dgvUsers.SelectionChanged
            If _isRestoringSelection OrElse dgvUsers.SelectedRows.Count = 0 Then Return

            If HasPendingChanges() Then
                Dim selectedRowName As String = If(Not String.IsNullOrWhiteSpace(txtFullName.Text), txtFullName.Text, txtUsername.Text)
                If String.IsNullOrWhiteSpace(selectedRowName) Then selectedRowName = "New User"
                Dim dlgResult = Forms.Common.FrmInAppAlert.ShowModal(Me, "Unsaved Changes",
                    $"You have unsaved changes for '{selectedRowName}'. Discard and load the selected user instead?",
                    Forms.Common.AlertType.WarningAlert, actionText:="YES", showCancel:=True, cancelText:="NO")
                
                If dlgResult <> DialogResult.OK Then
                    _isRestoringSelection = True
                    If _lastSelectedRowIndex >= 0 AndAlso _lastSelectedRowIndex < dgvUsers.Rows.Count Then
                        dgvUsers.ClearSelection()
                        dgvUsers.Rows(_lastSelectedRowIndex).Selected = True
                    Else
                        dgvUsers.ClearSelection()
                    End If
                    _isRestoringSelection = False
                    Return
                End If
            End If

            Dim selectedRow = dgvUsers.SelectedRows(0)
            _lastSelectedRowIndex = selectedRow.Index

            If selectedRow.Cells("colUserId").Value IsNot Nothing Then
                _selectedUserId = Convert.ToInt32(selectedRow.Cells("colUserId").Value)
                Dim user = _userList.Find(Function(u) u.UserId = _selectedUserId)

                If user IsNot Nothing Then
                    txtUsername.Text = user.Username
                    txtFullName.Text = user.FullName
                    cboRole.SelectedValue = user.Role
                    cboDept.SelectedValue = user.Department
                    chkActive.Checked = user.IsActive
                    _selectedRole = user.Role

                    Dim isCurrentUser = CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser.UserId = _selectedUserId
                    cboRole.Enabled = Not isCurrentUser
                    chkActive.Enabled = Not isCurrentUser
                    
                    If _lnkAdvanced IsNot Nothing Then _lnkAdvanced.Visible = True
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
            If _txtPassword IsNot Nothing Then _txtPassword.Text = String.Empty
            cboRole.SelectedValue = UserRole.Employee
            cboDept.SelectedValue = DepartmentType.Administration
            chkActive.Checked = True
            cboRole.Enabled = True
            chkActive.Enabled = True

            _selectedRole = UserRole.Employee
            If _lnkAdvanced IsNot Nothing Then _lnkAdvanced.Visible = False

            _isRestoringSelection = True
            dgvUsers.ClearSelection()
            _lastSelectedRowIndex = -1
            _isRestoringSelection = False
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

                Dim newUserId As Integer = 0
                If _selectedUserId = 0 Then
                    ' --- Pre-check: Duplicate username validation ---
                    Dim normalizedUsername As String = txtUsername.Text.Trim().ToLower()
                    Dim existingUser = Await _userRepo.GetByUsernameAsync(normalizedUsername)
                    If existingUser IsNot Nothing Then
                        Forms.Common.FrmInAppAlert.ShowModal(Me, "Username Already Exists",
                            $"The username '{normalizedUsername}' is already assigned to another staff account ({existingUser.FullName})." & Environment.NewLine & Environment.NewLine &
                            "Please choose a different username.",
                            AlertType.WarningAlert, actionText:="OK")
                        txtUsername.Focus()
                        txtUsername.SelectAll()
                        Return
                    End If

                    Dim hasher As New PasswordHasher()
                    Dim tempPassword = If(Not String.IsNullOrWhiteSpace(_txtPassword.Text), _txtPassword.Text, hasher.GenerateTemporaryPassword(12))
                    Dim defaultSalt As String = String.Empty
                    Dim defaultHash = hasher.HashPassword(tempPassword, defaultSalt)

                    Dim entity As New Core.Entities.UserEntity() With {
                        .Username = normalizedUsername,
                        .FullName = txtFullName.Text.Trim(),
                        .PasswordHash = defaultHash,
                        .PasswordSalt = defaultSalt,
                        .Role = _selectedRole,
                        .Department = CType(cboDept.SelectedValue, DepartmentType),
                        .MustChangePassword = True,
                        .IsActive = chkActive.Checked,
                        .CreatedBy = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)
                    }

                    Try
                        Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)
                        newUserId = Await _userService.CreateUserAsync(entity, currentUserId)
                        Forms.Common.FrmInAppAlert.ShowModal(Me, "User Created", $"Staff user '{entity.Username}' created successfully.{Environment.NewLine}Temporary Password: {tempPassword}", AlertType.SuccessAlert, actionText:="OK")
                    Catch sqlEx As SqlException When sqlEx.Number = 2627 OrElse sqlEx.Number = 2601
                        ' Defensive safety net: SQL unique constraint violation caught at DB level
                        Forms.Common.FrmInAppAlert.ShowModal(Me, "Username Already Exists",
                            $"The username '{normalizedUsername}' is already in use. Please choose a different username.",
                            AlertType.WarningAlert, actionText:="OK")
                        txtUsername.Focus()
                        txtUsername.SelectAll()
                        Return
                    End Try
                Else
                    Dim existing = Await _userRepo.GetByIdAsync(_selectedUserId)
                    If existing IsNot Nothing Then
                        existing.FullName = txtFullName.Text.Trim()
                        existing.Role = _selectedRole
                        existing.Department = CType(cboDept.SelectedValue, DepartmentType)
                        existing.IsActive = chkActive.Checked
                        existing.ModifiedBy = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)

                        If Not String.IsNullOrWhiteSpace(_txtPassword.Text) Then
                            Dim hasher As New PasswordHasher()
                            Dim newSalt As String = String.Empty
                            existing.PasswordHash = hasher.HashPassword(_txtPassword.Text, newSalt)
                            existing.PasswordSalt = newSalt
                            existing.MustChangePassword = True
                        End If

                        Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)
                        Try
                            Await _userService.UpdateUserAsync(existing, currentUserId)
                            Forms.Common.FrmInAppAlert.ShowModal(Me, "User Updated", $"Staff user '{existing.Username}' updated successfully.", AlertType.SuccessAlert, actionText:="OK")
                        Catch bex As BusinessException
                            Forms.Common.FrmInAppAlert.ShowModal(Me, "Security Violation", bex.Message, AlertType.WarningAlert, actionText:="OK")
                        End Try
                    End If
                End If

                Dim selectNewUserId As Integer = 0
                If _selectedUserId = 0 Then
                    selectNewUserId = newUserId
                Else
                    selectNewUserId = _selectedUserId
                End If

                Await RefreshUserGridAsync()

                If selectNewUserId > 0 Then
                    ' Find the row and select it instead of clearing the form
                    _isRestoringSelection = True
                    dgvUsers.ClearSelection()
                    For Each row As DataGridViewRow In dgvUsers.Rows
                        If Convert.ToInt32(row.Cells("colUserId").Value) = selectNewUserId Then
                            row.Selected = True
                            _lastSelectedRowIndex = row.Index
                            Exit For
                        End If
                    Next
                    _isRestoringSelection = False
                    ' Manually trigger selection to load the data back into the form cleanly
                    If dgvUsers.SelectedRows.Count > 0 Then
                        dgvUsers_SelectionChanged(dgvUsers, EventArgs.Empty)
                    End If
                Else
                    ClearFormInputs()
                End If
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
            
            Dim isCurrentUser = CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser.UserId = _selectedUserId
            If isCurrentUser Then
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Self-Reset Not Allowed", "You cannot administratively reset your own password here. Please use the 'My Account' section to securely change your own password.", AlertType.WarningAlert, actionText:="OK")
                Return
            End If

            Dim confirmMsg As String = If(Not String.IsNullOrWhiteSpace(_txtPassword.Text),
                $"Set password to the manually entered value for user '{txtUsername.Text}'?",
                $"Generate a new secure temporary password for user '{txtUsername.Text}'?")

            Dim confirm = Forms.Common.FrmInAppAlert.ShowModal(Me, "Confirm Password Reset", confirmMsg, AlertType.WarningAlert, actionText:="YES, RESET PASSWORD", showCancel:=True, cancelText:="CANCEL")
            If confirm = DialogResult.OK Then
                Try
                    Me.Cursor = Cursors.WaitCursor
                    Dim existing = Await _userRepo.GetByIdAsync(_selectedUserId)
                    If existing IsNot Nothing Then
                        Dim hasher As New PasswordHasher()
                        Dim tempPassword = If(Not String.IsNullOrWhiteSpace(_txtPassword.Text), _txtPassword.Text, hasher.GenerateTemporaryPassword(12))
                        Dim newSalt As String = String.Empty
                        Dim newHash = hasher.HashPassword(tempPassword, newSalt)

                        existing.PasswordHash = newHash
                        existing.PasswordSalt = newSalt
                        existing.MustChangePassword = True
                        existing.ModifiedBy = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)
                        
                        Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)

                        Await _userService.UpdateUserAsync(existing, currentUserId)
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
                    parentShell.NavigateToModule("Settings")
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
