Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports Krypton.Toolkit
Imports StaffAutomation.BLL.Logging
Imports StaffAutomation.BLL.Security
Imports StaffAutomation.BLL.Services
Imports StaffAutomation.Core.Configuration
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Logging
Imports StaffAutomation.Core.Security
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.DAL.Configuration
Imports StaffAutomation.DAL.Core
Imports StaffAutomation.DAL.Interfaces
Imports StaffAutomation.DAL.Repositories
Imports StaffAutomation.WinForms.Forms.Common
Imports StaffAutomation.WinForms.Forms.Main

Namespace Forms.Admin
    Public Class FrmMyAccount
        Inherits Form

        Private ReadOnly _userService As UserService
        Private ReadOnly _userRepo As IUserRepository
        Private _currentUserId As Integer
        Private _currentUser As UserEntity

        Private pnlHeader As Panel
        Private lblTitle As Label
        Private pnlMain As Panel

        Private txtFullName As KryptonTextBox
        Private txtUsername As KryptonTextBox
        Private lblRoleVal As Label
        Private lblDeptVal As Label
        Private lblActiveVal As Label

        Private txtCurrentPassword As KryptonTextBox
        Private txtNewPassword As KryptonTextBox
        Private txtConfirmPassword As KryptonTextBox

        Private btnSaveProfile As KryptonButton
        Private btnChangePassword As KryptonButton
        Private btnHeaderClose As KryptonButton

        Public Sub New()
            InitializeComponent()

            Dim config As IAppConfiguration = New AppConfiguration()
            Dim connFactory As IDatabaseConnectionFactory = New DbConnectionFactory(config)
            Dim sqlHelper As ISqlHelper = New SqlHelper(connFactory)
            _userRepo = New UserRepository(sqlHelper)

            Dim hasher As New PasswordHasher()
            Dim appLogger As IAppLogger = New AppLogger(config)
            Dim auditLogger As New AuditLogger(sqlHelper)
            _userService = New UserService(_userRepo, hasher, appLogger, auditLogger)
        End Sub

        Private Sub InitializeComponent()
            Me.Size = New Size(600, 700)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.Text = "My Account"
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.BackColor = ThemeConstants.WorkspaceBackground

            pnlHeader = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 60,
                .BackColor = ThemeConstants.NavigationBackground
            }

            lblTitle = New Label() With {
                .Text = "🔑 My Account",
                .Font = New Font(ThemeConstants.FontNameDefault, 14.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.NavigationText,
                .AutoSize = True,
                .Location = New Point(16, 18)
            }

            btnHeaderClose = New KryptonButton() With {
                .Text = "✕",
                .Size = New Size(40, 32),
                .Location = New Point(Me.ClientSize.Width - 60, 14)
            }
            ThemeConstants.ApplyKryptonSecondaryButton(btnHeaderClose)
            btnHeaderClose.StateCommon.Back.Color1 = Color.FromArgb(220, 38, 38)
            btnHeaderClose.StateCommon.Back.Color2 = Color.FromArgb(220, 38, 38)
            btnHeaderClose.StateCommon.Content.ShortText.Color1 = Color.White
            AddHandler btnHeaderClose.Click, Sub() Me.Close()
            Me.CancelButton = btnHeaderClose

            pnlHeader.Controls.Add(lblTitle)
            pnlHeader.Controls.Add(btnHeaderClose)

            pnlMain = New Panel() With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(20),
                .AutoScroll = True
            }

            ' Profile Section
            Dim lblProfileSection As New Label() With {
                .Text = "Profile Details",
                .Font = New Font(ThemeConstants.FontNameDefault, 10.0!, FontStyle.Bold),
                .Location = New Point(20, 20),
                .AutoSize = True
            }

            Dim lblFullName As New Label() With {.Text = "Full Name:", .Location = New Point(20, 50), .AutoSize = True}
            txtFullName = New KryptonTextBox() With {.Location = New Point(20, 70), .Size = New Size(250, 30)}
            ThemeConstants.ApplyStandardInputStyle(txtFullName)

            Dim lblUsername As New Label() With {.Text = "Username (Login ID):", .Location = New Point(300, 50), .AutoSize = True}
            txtUsername = New KryptonTextBox() With {.Location = New Point(300, 70), .Size = New Size(250, 30)}
            ThemeConstants.ApplyStandardInputStyle(txtUsername)

            ' Read-only Info
            Dim pnlReadOnly As New Panel() With {
                .Location = New Point(20, 120),
                .Size = New Size(530, 80),
                .BackColor = ThemeConstants.CardBackground,
                .BorderStyle = BorderStyle.FixedSingle
            }

            Dim lblRole = New Label() With {.Text = "Role:", .Location = New Point(10, 10), .AutoSize = True, .ForeColor = ThemeConstants.TextSecondary}
            lblRoleVal = New Label() With {.Location = New Point(80, 10), .AutoSize = True, .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold)}

            Dim lblDept = New Label() With {.Text = "Department:", .Location = New Point(200, 10), .AutoSize = True, .ForeColor = ThemeConstants.TextSecondary}
            lblDeptVal = New Label() With {.Location = New Point(300, 10), .AutoSize = True, .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold)}

            Dim lblActive = New Label() With {.Text = "Status:", .Location = New Point(10, 40), .AutoSize = True, .ForeColor = ThemeConstants.TextSecondary}
            lblActiveVal = New Label() With {.Location = New Point(80, 40), .AutoSize = True, .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold)}

            pnlReadOnly.Controls.Add(lblRole)
            pnlReadOnly.Controls.Add(lblRoleVal)
            pnlReadOnly.Controls.Add(lblDept)
            pnlReadOnly.Controls.Add(lblDeptVal)
            pnlReadOnly.Controls.Add(lblActive)
            pnlReadOnly.Controls.Add(lblActiveVal)

            btnSaveProfile = New KryptonButton() With {.Text = "Save Profile", .Location = New Point(20, 220), .Size = New Size(150, 36)}
            ThemeConstants.ApplyKryptonPrimaryButton(btnSaveProfile)
            AddHandler btnSaveProfile.Click, AddressOf btnSaveProfile_Click

            ' Divider
            Dim divider As New Label() With {
                .AutoSize = False,
                .Height = 2,
                .Width = 530,
                .BackColor = Color.LightGray,
                .Location = New Point(20, 280)
            }

            ' Password Section
            Dim lblPwdSection As New Label() With {
                .Text = "Change Password",
                .Font = New Font(ThemeConstants.FontNameDefault, 10.0!, FontStyle.Bold),
                .Location = New Point(20, 300),
                .AutoSize = True
            }

            Dim lblCurrentPwd As New Label() With {.Text = "Current Password:", .Location = New Point(20, 330), .AutoSize = True}
            txtCurrentPassword = New KryptonTextBox() With {.Location = New Point(20, 350), .Size = New Size(250, 30), .PasswordChar = "*"c}
            ThemeConstants.ApplyStandardInputStyle(txtCurrentPassword)

            Dim lblNewPwd As New Label() With {.Text = "New Password:", .Location = New Point(20, 390), .AutoSize = True}
            txtNewPassword = New KryptonTextBox() With {.Location = New Point(20, 410), .Size = New Size(250, 30), .PasswordChar = "*"c}
            ThemeConstants.ApplyStandardInputStyle(txtNewPassword)

            Dim lblConfirmPwd As New Label() With {.Text = "Confirm New Password:", .Location = New Point(300, 390), .AutoSize = True}
            txtConfirmPassword = New KryptonTextBox() With {.Location = New Point(300, 410), .Size = New Size(250, 30), .PasswordChar = "*"c}
            ThemeConstants.ApplyStandardInputStyle(txtConfirmPassword)

            btnChangePassword = New KryptonButton() With {.Text = "Update Password", .Location = New Point(20, 460), .Size = New Size(180, 36)}
            ThemeConstants.ApplyKryptonPrimaryButton(btnChangePassword)
            AddHandler btnChangePassword.Click, AddressOf btnChangePassword_Click

            pnlMain.Controls.Add(lblProfileSection)
            pnlMain.Controls.Add(lblFullName)
            pnlMain.Controls.Add(txtFullName)
            pnlMain.Controls.Add(lblUsername)
            pnlMain.Controls.Add(txtUsername)
            pnlMain.Controls.Add(pnlReadOnly)
            pnlMain.Controls.Add(btnSaveProfile)
            pnlMain.Controls.Add(divider)
            pnlMain.Controls.Add(lblPwdSection)
            pnlMain.Controls.Add(lblCurrentPwd)
            pnlMain.Controls.Add(txtCurrentPassword)
            pnlMain.Controls.Add(lblNewPwd)
            pnlMain.Controls.Add(txtNewPassword)
            pnlMain.Controls.Add(lblConfirmPwd)
            pnlMain.Controls.Add(txtConfirmPassword)
            pnlMain.Controls.Add(btnChangePassword)

            Me.Controls.Add(pnlMain)
            Me.Controls.Add(pnlHeader)
        End Sub

        Private Async Sub FrmMyAccount_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            If Not CurrentUserContext.IsAuthenticated Then
                FrmInAppAlert.ShowModal(Me, "Error", "Not authenticated.", AlertType.ErrorAlert, actionText:="OK")
                Me.Close()
                Return
            End If
            
            _currentUserId = CurrentUserContext.CurrentUser.UserId
            Await LoadUserDataAsync()
        End Sub

        Private Async Function LoadUserDataAsync() As Task
            Try
                Me.Cursor = Cursors.WaitCursor
                _currentUser = Await _userRepo.GetByIdAsync(_currentUserId)
                If _currentUser IsNot Nothing Then
                    txtFullName.Text = _currentUser.FullName
                    txtUsername.Text = _currentUser.Username
                    lblRoleVal.Text = _currentUser.Role.ToString()
                    lblDeptVal.Text = _currentUser.Department.ToString()
                    lblActiveVal.Text = If(_currentUser.IsActive, "Active", "Inactive")
                Else
                    FrmInAppAlert.ShowModal(Me, "Error", "Failed to load account details.", AlertType.ErrorAlert, actionText:="OK")
                    Me.Close()
                End If
            Catch ex As Exception
                FrmInAppAlert.ShowModal(Me, "Error", "Failed to load account details: " & ex.Message, AlertType.ErrorAlert, actionText:="OK")
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Function

        Private Async Sub btnSaveProfile_Click(sender As Object, e As EventArgs)
            If String.IsNullOrWhiteSpace(txtFullName.Text) OrElse String.IsNullOrWhiteSpace(txtUsername.Text) Then
                FrmInAppAlert.ShowModal(Me, "Validation", "Full Name and Username are required.", AlertType.WarningAlert, actionText:="OK")
                Return
            End If

            Try
                Me.Cursor = Cursors.WaitCursor
                
                ' Duplicate check
                Dim normalizedUsername As String = txtUsername.Text.Trim().ToLower()
                If normalizedUsername <> _currentUser.Username.ToLower() Then
                    Dim existingUser = Await _userRepo.GetByUsernameAsync(normalizedUsername)
                    If existingUser IsNot Nothing AndAlso existingUser.UserId <> _currentUserId Then
                        FrmInAppAlert.ShowModal(Me, "Username Already Exists", "The username is already in use.", AlertType.WarningAlert, actionText:="OK")
                        Return
                    End If
                End If

                _currentUser.FullName = txtFullName.Text.Trim()
                _currentUser.Username = txtUsername.Text.Trim()
                _currentUser.ModifiedBy = _currentUserId

                ' Send to BLL which also enforces self-lockout (preventing tampering)
                Await _userService.UpdateUserAsync(_currentUser, _currentUserId)
                
                ' Update Context
                CurrentUserContext.CurrentUser.FullName = _currentUser.FullName
                CurrentUserContext.CurrentUser.Username = _currentUser.Username

                FrmInAppAlert.ShowModal(Me, "Profile Updated", "Profile details updated successfully.", AlertType.SuccessAlert, actionText:="OK")
            Catch bex As BusinessException
                FrmInAppAlert.ShowModal(Me, "Security Violation", bex.Message, AlertType.WarningAlert, actionText:="OK")
            Catch ex As Exception
                FrmInAppAlert.ShowModal(Me, "Error", "Failed to update profile: " & ex.Message, AlertType.ErrorAlert, actionText:="OK")
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Sub

        Private Async Sub btnChangePassword_Click(sender As Object, e As EventArgs)
            Dim currentPwd = txtCurrentPassword.Text
            Dim newPwd = txtNewPassword.Text
            Dim confirmPwd = txtConfirmPassword.Text

            If String.IsNullOrWhiteSpace(currentPwd) OrElse String.IsNullOrWhiteSpace(newPwd) OrElse String.IsNullOrWhiteSpace(confirmPwd) Then
                FrmInAppAlert.ShowModal(Me, "Validation", "Please fill in all password fields.", AlertType.WarningAlert, actionText:="OK")
                Return
            End If

            If newPwd <> confirmPwd Then
                FrmInAppAlert.ShowModal(Me, "Validation", "New password and confirmation do not match.", AlertType.WarningAlert, actionText:="OK")
                Return
            End If

            Try
                Me.Cursor = Cursors.WaitCursor
                Dim success = Await _userService.ChangePasswordAsync(_currentUserId, currentPwd, newPwd)
                If success Then
                    FrmInAppAlert.ShowModal(Me, "Password Changed", "Your password has been changed successfully.", AlertType.SuccessAlert, actionText:="OK")
                    txtCurrentPassword.Text = ""
                    txtNewPassword.Text = ""
                    txtConfirmPassword.Text = ""
                End If
            Catch valEx As ValidationException
                FrmInAppAlert.ShowModal(Me, "Validation", valEx.Message, AlertType.WarningAlert, actionText:="OK")
            Catch bex As BusinessException
                FrmInAppAlert.ShowModal(Me, "Error", bex.Message, AlertType.WarningAlert, actionText:="OK")
            Catch ex As Exception
                FrmInAppAlert.ShowModal(Me, "Error", "An unexpected error occurred: " & ex.Message, AlertType.ErrorAlert, actionText:="OK")
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Sub

    End Class
End Namespace
