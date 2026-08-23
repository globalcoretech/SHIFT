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
Imports StaffAutomation.WinForms.UIHelpers

Namespace Forms.Auth
    ''' <summary>
    ''' Ultra-Premium Enterprise Login Screen delegating all authentication decisions to BLL AuthService.
    ''' </summary>
    Public Class FrmLogin
        Private ReadOnly _navigator As IViewNavigator
        Private ReadOnly _authService As IAuthService

        Public Sub New(navigator As IViewNavigator)
            InitializeComponent()
            _navigator = navigator
            _authService = InitializeAuthService()
        End Sub

        Private Sub FrmLogin_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            ' Configure interactive UI input focus events
            AddHandler txtUsername.GotFocus, AddressOf OnUsernameGotFocus
            AddHandler txtUsername.LostFocus, AddressOf OnUsernameLostFocus
            AddHandler txtPassword.GotFocus, AddressOf OnPasswordGotFocus
            AddHandler txtPassword.LostFocus, AddressOf OnPasswordLostFocus

            AddHandler btnLogin.MouseEnter, Sub(s, ev) btnLogin.BackColor = Color.FromArgb(67, 56, 202)
            AddHandler btnLogin.MouseLeave, Sub(s, ev) btnLogin.BackColor = Color.FromArgb(79, 70, 229)

            AddHandler btnExit.MouseEnter, Sub(s, ev) btnExit.BackColor = Color.FromArgb(226, 232, 240)
            AddHandler btnExit.MouseLeave, Sub(s, ev) btnExit.BackColor = Color.FromArgb(241, 245, 249)

            txtUsername.Focus()
        End Sub

        Private Sub OnUsernameGotFocus(sender As Object, e As EventArgs)
            pnlUsernameInput.BackColor = Color.White
            txtUsername.BackColor = Color.White
            lblUsernameIcon.ForeColor = Color.FromArgb(79, 70, 229)
        End Sub

        Private Sub OnUsernameLostFocus(sender As Object, e As EventArgs)
            pnlUsernameInput.BackColor = Color.FromArgb(248, 250, 252)
            txtUsername.BackColor = Color.FromArgb(248, 250, 252)
            lblUsernameIcon.ForeColor = Color.FromArgb(148, 163, 184)
        End Sub

        Private Sub OnPasswordGotFocus(sender As Object, e As EventArgs)
            pnlPasswordInput.BackColor = Color.White
            txtPassword.BackColor = Color.White
            lblPasswordIcon.ForeColor = Color.FromArgb(79, 70, 229)
        End Sub

        Private Sub OnPasswordLostFocus(sender As Object, e As EventArgs)
            pnlPasswordInput.BackColor = Color.FromArgb(248, 250, 252)
            txtPassword.BackColor = Color.FromArgb(248, 250, 252)
            lblPasswordIcon.ForeColor = Color.FromArgb(148, 163, 184)
        End Sub

        Private Async Sub btnLogin_Click(sender As Object, e As EventArgs) Handles btnLogin.Click
            errProvider.Clear()

            Dim username As String = txtUsername.Text.Trim()
            Dim password As String = txtPassword.Text

            ' UI Field Validation
            Dim isValid As Boolean = True
            If String.IsNullOrEmpty(username) Then
                errProvider.SetError(pnlUsernameInput, "Please enter your username.")
                isValid = False
            End If
            If String.IsNullOrEmpty(password) Then
                errProvider.SetError(pnlPasswordInput, "Please enter your password.")
                isValid = False
            End If

            If Not isValid Then Return

            Try
                btnLogin.Enabled = False
                btnExit.Enabled = False
                Me.Cursor = Cursors.WaitCursor

                ' Delegate authentication to BLL Engine
                Dim userDto = Await _authService.AuthenticateUserAsync(username, password)
                Me.Cursor = Cursors.Default

                ' Forced Password Change Workflow (H5 Requirement)
                If userDto.MustChangePassword Then
                    Forms.Common.FrmInAppAlert.ShowModal(Me, "Security Update Required", "🔒 SECURITY UPDATE REQUIRED: You are logged in with a temporary password. You must set a new secure password before continuing.", Forms.Common.AlertType.WarningAlert, actionText:="SET NEW PASSWORD")

                    Using forceDlg As New FrmForceChangePassword(userDto.UserId, password)
                        If forceDlg.ShowDialog(Me) <> DialogResult.OK Then
                            ' User cancelled password change -> clear session and stay on login
                            CurrentUserContext.ClearSession()
                            btnLogin.Enabled = True
                            btnExit.Enabled = True
                            txtPassword.Clear()
                            txtPassword.Focus()
                            Return
                        End If
                    End Using
                    userDto.MustChangePassword = False
                End If

                ' Navigate based on role
                If _navigator IsNot Nothing Then
                    Select Case userDto.Role
                        Case Core.Enums.UserRole.Employee
                            _navigator.NavigateToEmployeeDashboard()
                        Case Core.Enums.UserRole.Owner
                            _navigator.NavigateToOwnerDashboard()
                        Case Core.Enums.UserRole.Admin
                            _navigator.NavigateToAdminConsole()
                        Case Else
                            _navigator.NavigateToEmployeeDashboard()
                    End Select
                End If

                Me.Hide()
            Catch ex As LegacyAdminBootstrapRecoveryException
                Me.Cursor = Cursors.Default
                btnLogin.Enabled = True
                btnExit.Enabled = True

                Forms.Common.FrmInAppAlert.ShowModal(Me, "Bootstrap Recovery Required", "🔑 EMERGENCY ADMIN RECOVERY: Account '" & ex.Username & "' is using a legacy password. Set a new secure administrator password to continue.", Forms.Common.AlertType.WarningAlert, actionText:="RECOVER ADMIN")

                Using recoveryDlg As New FrmLegacyAdminRecovery(ex.Username)
                    If recoveryDlg.ShowDialog(Me) = DialogResult.OK Then
                        txtPassword.Clear()
                        txtPassword.Focus()
                    End If
                End Using
            Catch ex As AuthenticationException
                Me.Cursor = Cursors.Default
                btnLogin.Enabled = True
                btnExit.Enabled = True
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Authentication Failed", ex.Message, Forms.Common.AlertType.WarningAlert, actionText:="TRY AGAIN")
                txtPassword.Clear()
                txtPassword.Focus()
            Catch ex As Exception
                Me.Cursor = Cursors.Default
                btnLogin.Enabled = True
                btnExit.Enabled = True
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Login Failure", "Unable to log in: " & ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
                txtPassword.Clear()
            End Try
        End Sub

        Private Sub btnExit_Click(sender As Object, e As EventArgs) Handles btnExit.Click
            Application.Exit()
        End Sub

        Private Sub chkShowPassword_CheckedChanged(sender As Object, e As EventArgs) Handles chkShowPassword.CheckedChanged
            If chkShowPassword.Checked Then
                txtPassword.PasswordChar = ControlChars.NullChar
            Else
                txtPassword.PasswordChar = ChrW(8226)
            End If
        End Sub

        ''' <summary>
        ''' Composition root helper for instantiating BLL AuthService without tight coupling.
        ''' </summary>
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
