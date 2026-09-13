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
    ''' Emergency Legacy Administrator Bootstrap Recovery Form.
    ''' Allows one-time secure PBKDF2 password migration for legacy sentinel bootstrap admin accounts
    ''' when no other active administrator exists.
    ''' </summary>
    Public Class FrmLegacyAdminRecovery
        Inherits Form

        Private ReadOnly _username As String
        Private ReadOnly _authService As IAuthService

        Private pnlHeader As Panel
        Private lblTitle As Label
        Private lblSubtitle As Label

        Private lblUsername As Label
        Private txtUsername As TextBox
        Private lblNewPass As Label
        Private txtNewPass As TextBox
        Private lblConfirmPass As Label
        Private txtConfirmPass As TextBox

        Private btnMigrate As ModernButton
        Private btnCancel As ModernButton
        Private errProvider As ErrorProvider

        Public Sub New(username As String)
            _username = username
            _authService = InitializeAuthService()
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Text = "Emergency Administrator Bootstrap Recovery"
            Me.Size = New Size(480, 420)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.BackColor = ThemeConstants.WorkspaceBackground
            Me.Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular)

            errProvider = New ErrorProvider() With {.BlinkStyle = ErrorBlinkStyle.NeverBlink}

            ' Header
            pnlHeader = New Panel() With {.Dock = DockStyle.Top, .Height = 85, .BackColor = Color.FromArgb(15, 23, 42)}
            lblTitle = New Label() With {
                .Text = "🔑 Administrator Bootstrap Recovery",
                .Font = New Font(ThemeConstants.FontNameDefault, 11.0!, FontStyle.Bold),
                .ForeColor = Color.White,
                .Location = New Point(16, 16),
                .AutoSize = True
            }
            lblSubtitle = New Label() With {
                .Text = "Account '" & _username & "' is using a legacy bootstrap credential. Set a new secure password to migrate your account.",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Regular),
                .ForeColor = Color.FromArgb(148, 163, 184),
                .Location = New Point(16, 42),
                .Size = New Size(430, 36)
            }
            pnlHeader.Controls.Add(lblTitle)
            pnlHeader.Controls.Add(lblSubtitle)

            ' Form Inputs
            lblUsername = New Label() With {.Text = "Administrator Username:", .Location = New Point(24, 105), .AutoSize = True, .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold)}
            txtUsername = New TextBox() With {.Text = _username, .ReadOnly = True, .Location = New Point(24, 127), .Size = New Size(410, 26), .BackColor = Color.FromArgb(241, 245, 249)}

            lblNewPass = New Label() With {.Text = "New Password (Min 8 chars):", .Location = New Point(24, 170), .AutoSize = True, .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold)}
            txtNewPass = New TextBox() With {.Location = New Point(24, 192), .Size = New Size(410, 26), .PasswordChar = ChrW(8226)}

            lblConfirmPass = New Label() With {.Text = "Confirm New Password:", .Location = New Point(24, 235), .AutoSize = True, .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold)}
            txtConfirmPass = New TextBox() With {.Location = New Point(24, 257), .Size = New Size(410, 26), .PasswordChar = ChrW(8226)}

            ' Buttons
            btnMigrate = New ModernButton() With {
                .Text = "🔒 Save & Migrate",
                .Scheme = ModernButton.ButtonScheme.Primary,
                .Size = New Size(190, 36),
                .Location = New Point(24, 320)
            }
            AddHandler btnMigrate.Click, AddressOf btnMigrate_Click

            btnCancel = New ModernButton() With {
                .Text = "Cancel",
                .Scheme = ModernButton.ButtonScheme.Secondary,
                .Size = New Size(150, 36),
                .Location = New Point(284, 320)
            }
            AddHandler btnCancel.Click, Sub(s, e)
                                            Me.DialogResult = DialogResult.Cancel
                                            Me.Close()
                                        End Sub

            Me.Controls.Add(pnlHeader)
            Me.Controls.Add(lblUsername)
            Me.Controls.Add(txtUsername)
            Me.Controls.Add(lblNewPass)
            Me.Controls.Add(txtNewPass)
            Me.Controls.Add(lblConfirmPass)
            Me.Controls.Add(txtConfirmPass)
            Me.Controls.Add(btnMigrate)
            Me.Controls.Add(btnCancel)
        End Sub

        Private Async Sub btnMigrate_Click(sender As Object, e As EventArgs)
            errProvider.Clear()

            Dim newPass = txtNewPass.Text
            Dim confirmPass = txtConfirmPass.Text

            If String.IsNullOrWhiteSpace(newPass) OrElse newPass.Length < 8 Then
                errProvider.SetError(txtNewPass, "New password must be at least 8 characters long.")
                Return
            End If

            If newPass <> confirmPass Then
                errProvider.SetError(txtConfirmPass, "New password and confirm password do not match.")
                Return
            End If

            Try
                Me.Cursor = Cursors.WaitCursor
                btnMigrate.Enabled = False

                Dim success = Await _authService.MigrateLegacyAdminPasswordAsync(_username, newPass)
                If success Then
                    FrmInAppAlert.ShowModal(Me, "Bootstrap Recovery Complete", "Administrator password updated successfully. You may now log in with your new password.", AlertType.SuccessAlert, actionText:="LOG IN NOW")
                    Me.DialogResult = DialogResult.OK
                    Me.Close()
                Else
                    FrmInAppAlert.ShowModal(Me, "Migration Failed", "Unable to complete admin recovery.", AlertType.ErrorAlert, actionText:="OK")
                End If
            Catch ex As BusinessException
                FrmInAppAlert.ShowModal(Me, "Validation Error", ex.Message, AlertType.WarningAlert, actionText:="OK")
            Catch ex As Exception
                FrmInAppAlert.ShowModal(Me, "Error", "An unexpected error occurred: " & ex.Message, AlertType.ErrorAlert, actionText:="OK")
            Finally
                btnMigrate.Enabled = True
                Me.Cursor = Cursors.Default
            End Try
        End Sub

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
