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
Imports StaffAutomation.Core.DTOs
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
    ''' Modal Form for adding a User Access Exception (Explicit GRANT or Explicit DENY).
    ''' Enforces mandatory reason justification and fail-closed validation rules.
    ''' </summary>
    Public Class FrmAddEditUserOverride
        Inherits Form

        Private ReadOnly _overrideService As IUserPermissionOverrideService
        Private ReadOnly _permRepo As IPermissionRepository
        Private ReadOnly _userRepo As IUserRepository

        Private ReadOnly _targetUserId As Integer
        Private ReadOnly _targetUsername As String

        Private lblHeader As Label
        Private lblSub As Label

        Private cboPermissions As KryptonComboBox
        Private lblRoleAccessStatus As Label

        Private rdoGrant As RadioButton
        Private rdoDeny As RadioButton
        Private lblExceptionExplanation As Label

        Private txtReason As KryptonTextBox
        Private btnSave As KryptonButton
        Private btnCancel As KryptonButton

        Private _permissionsList As New System.Collections.Generic.List(Of PermissionDto)()

        Public Sub New(userId As Integer, username As String)
            _targetUserId = userId
            _targetUsername = username

            Dim config As IAppConfiguration = New AppConfiguration()
            Dim connFactory As IDatabaseConnectionFactory = New DbConnectionFactory(config)
            Dim sqlHelper As ISqlHelper = New SqlHelper(connFactory)
            Dim appLogger As IAppLogger = New AppLogger(config)
            Dim auditLogger As IAuditLogger = New AuditLogger(sqlHelper)

            Dim overrideRepo = New UserPermissionOverrideRepository(sqlHelper)
            _permRepo = New PermissionRepository(sqlHelper)
            _userRepo = New UserRepository(sqlHelper)

            _overrideService = New UserPermissionOverrideService(overrideRepo, _permRepo, _userRepo, appLogger, auditLogger)

            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Text = "➕ Add User Access Exception"
            Me.Size = New Size(540, 520)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.BackColor = Color.White
            Me.Padding = New Padding(20)

            ' Header
            lblHeader = New Label() With {
                .Text = "Add Access Exception for " & _targetUsername,
                .Font = New Font(ThemeConstants.FontNameDefault, 11.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Dock = DockStyle.Top,
                .Height = 28
            }

            lblSub = New Label() With {
                .Text = "Grant or explicitly deny a specific permission for this individual user.",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Regular),
                .ForeColor = ThemeConstants.TextSecondary,
                .Dock = DockStyle.Top,
                .Height = 24
            }

            ' Panel Body
            Dim pnlBody As New Panel() With {.Dock = DockStyle.Fill, .Padding = New Padding(0, 12, 0, 0)}

            Dim lblPerm As New Label() With {.Text = "Select Target Permission *", .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold), .Location = New Point(0, 8), .AutoSize = True}
            cboPermissions = New KryptonComboBox() With {.Location = New Point(0, 28), .Size = New Size(480, 32)}
            ThemeConstants.ApplyAppComboBoxStyle(cboPermissions)
            AddHandler cboPermissions.SelectedIndexChanged, Sub(s, e) UpdateRoleAccessStatus()

            lblRoleAccessStatus = New Label() With {
                .Text = "Current Role Access: Allowed via Role",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Italic),
                .ForeColor = ThemeConstants.SuccessGreen,
                .Location = New Point(0, 64),
                .AutoSize = True
            }

            Dim grpType As New GroupBox() With {
                .Text = "Exception Type *",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .Location = New Point(0, 92),
                .Size = New Size(480, 110)
            }

            rdoGrant = New RadioButton() With {
                .Text = "Grant Additional Access (Explicit GRANT)",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .Location = New Point(16, 24),
                .AutoSize = True,
                .Checked = True
            }
            AddHandler rdoGrant.CheckedChanged, Sub(s, e) UpdateExplanation()

            rdoDeny = New RadioButton() With {
                .Text = "Explicitly Deny Access (Explicit DENY - Overrides Role)",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(220, 38, 38),
                .Location = New Point(16, 48),
                .AutoSize = True
            }
            AddHandler rdoDeny.CheckedChanged, Sub(s, e) UpdateExplanation()

            lblExceptionExplanation = New Label() With {
                .Text = "This user will receive this permission even if their role does not normally have access.",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.0!, FontStyle.Italic),
                .ForeColor = ThemeConstants.TextSecondary,
                .Location = New Point(16, 76),
                .Size = New Size(450, 28)
            }

            grpType.Controls.Add(lblExceptionExplanation)
            grpType.Controls.Add(rdoDeny)
            grpType.Controls.Add(rdoGrant)

            Dim lblReason As New Label() With {.Text = "Justification / Business Reason * (Mandatory Audit Trail)", .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold), .Location = New Point(0, 212), .AutoSize = True}
            txtReason = New KryptonTextBox() With {
                .Location = New Point(0, 232),
                .Size = New Size(480, 70),
                .Multiline = True
            }
            ThemeConstants.ApplyAppTextBoxStyle(txtReason)

            pnlBody.Controls.Add(lblReason)
            pnlBody.Controls.Add(txtReason)
            pnlBody.Controls.Add(grpType)
            pnlBody.Controls.Add(lblRoleAccessStatus)
            pnlBody.Controls.Add(cboPermissions)
            pnlBody.Controls.Add(lblPerm)

            ' Footer Buttons
            Dim pnlFooter As New Panel() With {.Dock = DockStyle.Bottom, .Height = 50}
            btnSave = New KryptonButton() With {.Text = "💾 Save Exception", .Size = New Size(140, 34), .Location = New Point(210, 8)}
            ThemeConstants.ApplyKryptonPrimaryButton(btnSave)
            AddHandler btnSave.Click, Async Sub(s, e) Await SaveExceptionAsync()

            btnCancel = New KryptonButton() With {.Text = "Cancel", .Size = New Size(90, 34), .Location = New Point(360, 8)}
            ThemeConstants.ApplyKryptonSecondaryButton(btnCancel)
            AddHandler btnCancel.Click, Sub(s, e) Me.Close()

            pnlFooter.Controls.Add(btnSave)
            pnlFooter.Controls.Add(btnCancel)

            Me.Controls.Add(pnlBody)
            Me.Controls.Add(pnlFooter)
            Me.Controls.Add(lblSub)
            Me.Controls.Add(lblHeader)
        End Sub

        Protected Overrides Async Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)
            Try
                Dim allPerms = Await _permRepo.GetAllAsync()
                cboPermissions.Items.Clear()
                _permissionsList.Clear()

                For Each p In allPerms
                    If p.IsActive Then
                        Dim dto As New PermissionDto() With {
                            .PermissionId = p.PermissionId,
                            .PermissionCode = p.PermissionCode,
                            .PermissionName = p.PermissionName,
                            .ModuleName = p.ModuleName
                        }
                        _permissionsList.Add(dto)
                        cboPermissions.Items.Add(p.PermissionName & " (" & p.PermissionCode & ") [" & p.ModuleName & "]")
                    End If
                Next

                If cboPermissions.Items.Count > 0 Then
                    cboPermissions.SelectedIndex = 0
                End If
            Catch ex As Exception
                FrmInAppAlert.ShowModal(Me, "Error", "Failed to load permissions catalog: " & ex.Message, AlertType.WarningAlert)
            End Try
        End Sub

        Private Sub UpdateRoleAccessStatus()
            If cboPermissions.SelectedIndex < 0 OrElse cboPermissions.SelectedIndex >= _permissionsList.Count Then Return
            Dim sel = _permissionsList(cboPermissions.SelectedIndex)
            lblRoleAccessStatus.Text = "Selected Permission: " & sel.PermissionName & " (" & sel.PermissionCode & ")"
        End Sub

        Private Sub UpdateExplanation()
            If rdoGrant.Checked Then
                lblExceptionExplanation.Text = "GRANT: This user will receive this permission even if their role does not normally have access."
                lblExceptionExplanation.ForeColor = ThemeConstants.PrimaryAccent
            Else
                lblExceptionExplanation.Text = "DENY: This user will be BLOCKED from this permission even if their role normally allows it."
                lblExceptionExplanation.ForeColor = Color.FromArgb(220, 38, 38)
            End If
        End Sub

        Private Async Function SaveExceptionAsync() As Task
            If cboPermissions.SelectedIndex < 0 OrElse cboPermissions.SelectedIndex >= _permissionsList.Count Then
                FrmInAppAlert.ShowModal(Me, "Validation Error", "Please select a target permission.", AlertType.WarningAlert)
                Return
            End If

            If String.IsNullOrWhiteSpace(txtReason.Text) Then
                FrmInAppAlert.ShowModal(Me, "Validation Error", "Justification reason is mandatory for audit logging.", AlertType.WarningAlert)
                Return
            End If

            btnSave.Enabled = False
            Try
                Dim selPerm = _permissionsList(cboPermissions.SelectedIndex)
                Dim isGranted = rdoGrant.Checked
                Dim currentAdminId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)

                Dim success = Await _overrideService.SetOverrideAsync(_targetUserId, selPerm.PermissionId, isGranted, txtReason.Text.Trim(), currentAdminId)
                If success Then
                    FrmInAppAlert.ShowModal(Me, "Success", "Access exception saved successfully for " & _targetUsername & ".", AlertType.SuccessAlert)
                    Me.DialogResult = DialogResult.OK
                    Me.Close()
                Else
                    FrmInAppAlert.ShowModal(Me, "Error", "Failed to save user access exception.", AlertType.WarningAlert)
                End If
            Catch valEx As ValidationException
                FrmInAppAlert.ShowModal(Me, "Validation Error", valEx.Message, AlertType.WarningAlert)
            Catch bizEx As BusinessException
                FrmInAppAlert.ShowModal(Me, "Business Error", bizEx.Message, AlertType.WarningAlert)
            Catch ex As Exception
                FrmInAppAlert.ShowModal(Me, "Error", "Unexpected failure: " & ex.Message, AlertType.WarningAlert)
            Finally
                btnSave.Enabled = True
            End Try
        End Function
    End Class
End Namespace
