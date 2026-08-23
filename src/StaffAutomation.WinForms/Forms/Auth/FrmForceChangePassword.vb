Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports StaffAutomation.BLL.Logging
Imports StaffAutomation.BLL.Services
Imports StaffAutomation.Core.Configuration
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Logging
Imports StaffAutomation.Core.Security
Imports StaffAutomation.DAL.Configuration
Imports StaffAutomation.DAL.Core
Imports StaffAutomation.DAL.Repositories
Imports StaffAutomation.WinForms.Forms.Common
Imports StaffAutomation.WinForms.Forms.Main

Namespace Forms.Auth
    ''' <summary>
    ''' Mandatory forced password change dialog triggered when MustChangePassword is True.
    ''' Prevents application access until a new, secure password is set.
    ''' </summary>
    Public Class FrmForceChangePassword
        Inherits Form

        Private ReadOnly _userId As Integer
        Private ReadOnly _currentPassword As String
        Private ReadOnly _userService As UserService

        Private pnlHeader As Panel
        Private lblTitle As Label
        Private lblSubtitle As Label

        Private lblNewPass As Label
        Private txtNewPass As TextBox
        Private lblConfirmPass As Label
        Private txtConfirmPass As TextBox

        Private btnSubmit As ModernButton
        Private btnCancel As ModernButton
        Private errProvider As ErrorProvider

        Public Sub New(userId As Integer, currentPassword As String)
            _userId = userId
            _currentPassword = currentPassword
            _userService = InitializeUserService()
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Text = "Security Required: Change Password"
            Me.Size = New Size(480, 360)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.BackColor = ThemeConstants.WorkspaceBackground
            Me.Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular)

            errProvider = New ErrorProvider() With {.BlinkStyle = ErrorBlinkStyle.NeverBlink}

            ' Header
            pnlHeader = New Panel() With {.Dock = DockStyle.Top, .Height = 80, .BackColor = Color.FromArgb(15, 23, 42)}
            lblTitle = New Label() With {
                .Text = "🔒 Set Your New Secure Password",
                .Font = New Font(ThemeConstants.FontNameDefault, 11.0!, FontStyle.Bold),
                .ForeColor = Color.White,
                .Location = New Point(16, 16),
                .AutoSize = True
            }
            lblSubtitle = New Label() With {
                .Text = "Your account is using a temporary password. Update your password to continue.",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Regular),
                .ForeColor = Color.FromArgb(148, 163, 184),
                .Location = New Point(16, 42),
                .Size = New Size(430, 32)
            }
            pnlHeader.Controls.Add(lblTitle)
            pnlHeader.Controls.Add(lblSubtitle)

            ' Form inputs
            lblNewPass = New Label() With {.Text = "New Password (Min 8 chars):", .Location = New Point(24, 100), .AutoSize = True, .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold)}
            txtNewPass = New TextBox() With {.Location = New Point(24, 122), .Size = New Size(410, 26), .PasswordChar = ChrW(8226)}

            lblConfirmPass = New Label() With {.Text = "Confirm New Password:", .Location = New Point(24, 165), .AutoSize = True, .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold)}
            txtConfirmPass = New TextBox() With {.Location = New Point(24, 187), .Size = New Size(410, 26), .PasswordChar = ChrW(8226)}

            ' Buttons
            btnSubmit = New ModernButton() With {
                .Text = "🔒 Save & Continue",
                .Scheme = ModernButton.ButtonScheme.Primary,
                .Size = New Size(190, 36),
                .Location = New Point(24, 250)
            }
            AddHandler btnSubmit.Click, AddressOf btnSubmit_Click

            btnCancel = New ModernButton() With {
                .Text = "Cancel & Logout",
                .Scheme = ModernButton.ButtonScheme.Secondary,
                .Size = New Size(150, 36),
                .Location = New Point(284, 250)
            }
            AddHandler btnCancel.Click, Sub(s, e)
                                            Me.DialogResult = DialogResult.Cancel
                                            Me.Close()
                                        End Sub

            Me.Controls.Add(pnlHeader)
            Me.Controls.Add(lblNewPass)
            Me.Controls.Add(txtNewPass)
            Me.Controls.Add(lblConfirmPass)
            Me.Controls.Add(txtConfirmPass)
            Me.Controls.Add(btnSubmit)
            Me.Controls.Add(btnCancel)
        End Sub

        Private Async Sub btnSubmit_Click(sender As Object, e As EventArgs)
            errProvider.Clear()

            Dim newPass = txtNewPass.Text
            Dim confirmPass = txtConfirmPass.Text

            If String.IsNullOrWhiteSpace(newPass) OrElse newPass.Length < 8 Then
                errProvider.SetError(txtNewPass, "New password must be at least 8 characters long.")
                Return
            End If

            If newPass = _currentPassword Then
                errProvider.SetError(txtNewPass, "New password must be different from temporary password.")
                Return
            End If

            If newPass <> confirmPass Then
                errProvider.SetError(txtConfirmPass, "New password and confirm password do not match.")
                Return
            End If

            Try
                Me.Cursor = Cursors.WaitCursor
                btnSubmit.Enabled = False

                Dim success = Await _userService.ChangePasswordAsync(_userId, _currentPassword, newPass)
                If success Then
                    FrmInAppAlert.ShowModal(Me, "Password Updated", "Your password has been updated successfully. Proceeding to application...", AlertType.SuccessAlert, actionText:="OK")
                    Me.DialogResult = DialogResult.OK
                    Me.Close()
                Else
                    FrmInAppAlert.ShowModal(Me, "Update Failed", "Unable to update password. Please check requirement rules.", AlertType.ErrorAlert, actionText:="OK")
                End If
            Catch ex As BusinessException
                FrmInAppAlert.ShowModal(Me, "Validation Error", ex.Message, AlertType.WarningAlert, actionText:="OK")
            Catch ex As Exception
                FrmInAppAlert.ShowModal(Me, "Error", "An unexpected error occurred: " & ex.Message, AlertType.ErrorAlert, actionText:="OK")
            Finally
                btnSubmit.Enabled = True
                Me.Cursor = Cursors.Default
            End Try
        End Sub

        Private Function InitializeUserService() As UserService
            Dim config As IAppConfiguration = New AppConfiguration()
            Dim connFactory As IDatabaseConnectionFactory = New DbConnectionFactory(config)
            Dim sqlHelper As ISqlHelper = New SqlHelper(connFactory)
            Dim userRepo As DAL.Interfaces.IUserRepository = New UserRepository(sqlHelper)
            Dim hasher As New PasswordHasher()
            Dim appLogger As IAppLogger = New AppLogger(config)
            Dim auditLogger As New AuditLogger(sqlHelper)

            Return New UserService(userRepo, hasher, appLogger, auditLogger)
        End Function
    End Class
End Namespace
