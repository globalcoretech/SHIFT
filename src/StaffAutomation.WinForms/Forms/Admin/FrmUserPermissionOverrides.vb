Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
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
    ''' WinForms Administrative Security Console Form for User Permission Overrides.
    ''' Manages explicit user-level GRANT/DENY overrides with mandatory justification audit trail.
    ''' </summary>
    Public Class FrmUserPermissionOverrides
        Inherits Form

        Private ReadOnly _overrideService As IUserPermissionOverrideService
        Private ReadOnly _permService As IPermissionService
        Private ReadOnly _userRepo As IUserRepository
        Private ReadOnly _authzService As IAuthorizationService

        Private pnlHeader As Panel
        Private lblTitle As Label
        Private lblSubtitle As Label

        Private pnlFilterBar As Panel
        Private lblUserSelect As Label
        Private cboUsers As KryptonComboBox
        Private btnRefresh As KryptonButton

        Private dgvOverrides As DataGridView
        Private pnlFooter As Panel
        Private btnAddOverride As KryptonButton
        Private btnRemoveOverride As KryptonButton
        Private btnClose As KryptonButton

        Private _usersList As List(Of UserEntity)
        Private _overridesList As List(Of UserPermissionOverrideDto)

        Public Sub New()
            Dim config As IAppConfiguration = New AppConfiguration()
            Dim connFactory As IDatabaseConnectionFactory = New DbConnectionFactory(config)
            Dim sqlHelper As ISqlHelper = New SqlHelper(connFactory)
            Dim appLogger As IAppLogger = New AppLogger(config)
            Dim auditLogger As IAuditLogger = New AuditLogger(sqlHelper)

            Dim permRepo As IPermissionRepository = New PermissionRepository(sqlHelper)
            Dim userOverrideRepo As IUserPermissionOverrideRepository = New UserPermissionOverrideRepository(sqlHelper)
            _userRepo = New UserRepository(sqlHelper)

            _permService = New PermissionService(permRepo, appLogger)
            _overrideService = New UserPermissionOverrideService(userOverrideRepo, permRepo, _userRepo, appLogger, auditLogger)
            _authzService = New AuthorizationService()

            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Text = "🔒 User Permission Overrides"
            Me.Size = New Size(920, 600)
            Me.MinimumSize = New Size(780, 480)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.BackColor = ThemeConstants.WorkspaceBackground

            ' 1. Header Panel
            pnlHeader = New Panel()
            lblTitle = New Label() With {.Text = "🔒 User Permission Overrides"}
            lblSubtitle = New Label() With {.Text = "Provision explicit user-level GRANT or DENY permission overrides with mandatory audit justification."}
            ThemeConstants.ApplyHeaderStyle(pnlHeader, lblTitle, lblSubtitle)
            pnlHeader.Controls.Add(lblSubtitle)
            pnlHeader.Controls.Add(lblTitle)

            ' 2. Filter Bar
            pnlFilterBar = New Panel()
            ThemeConstants.ApplyToolbarStyle(pnlFilterBar)

            lblUserSelect = New Label() With {
                .Text = "Select User Account:",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(12, 16),
                .AutoSize = True
            }

            cboUsers = New KryptonComboBox() With {
                .Location = New Point(155, 12),
                .Width = 280,
                .Height = 32
            }
            ThemeConstants.ApplyAppComboBoxStyle(cboUsers)
            AddHandler cboUsers.SelectedIndexChanged, Async Sub(s, e) Await LoadOverridesForSelectedUserAsync()

            btnRefresh = New KryptonButton() With {.Text = "🔄 Refresh", .Size = New Size(100, 34), .Location = New Point(445, 11)}
            ThemeConstants.ApplyKryptonSecondaryButton(btnRefresh)
            AddHandler btnRefresh.Click, Async Sub(s, e) Await LoadOverridesForSelectedUserAsync()

            pnlFilterBar.Controls.Add(lblUserSelect)
            pnlFilterBar.Controls.Add(cboUsers)
            pnlFilterBar.Controls.Add(btnRefresh)

            ' 3. DataGridView Setup
            dgvOverrides = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .ReadOnly = True,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .MultiSelect = False,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .AutoGenerateColumns = False
            }
            ThemeConstants.ApplyModernGridStyle(dgvOverrides)

            dgvOverrides.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "OverrideId", .HeaderText = "ID", .DataPropertyName = "OverrideId", .Visible = False})
            dgvOverrides.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "PermissionId", .HeaderText = "Perm ID", .DataPropertyName = "PermissionId", .Visible = False})
            dgvOverrides.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "PermissionCode", .HeaderText = "Permission Code", .DataPropertyName = "PermissionCode", .Width = 160})
            dgvOverrides.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "PermissionName", .HeaderText = "Permission Name", .DataPropertyName = "PermissionName", .Width = 180})
            dgvOverrides.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "ModuleName", .HeaderText = "Module", .DataPropertyName = "ModuleName", .Width = 130})
            dgvOverrides.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "AccessState", .HeaderText = "Override Access", .DataPropertyName = "AccessState", .Width = 130})
            dgvOverrides.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "Reason", .HeaderText = "Justification Reason", .DataPropertyName = "Reason", .Width = 220})

            Dim pnlGridHost As New Panel() With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(16, 0, 16, 0)
            }
            pnlGridHost.Controls.Add(dgvOverrides)

            ' 4. Footer Panel
            pnlFooter = New Panel() With {
                .Dock = DockStyle.Bottom,
                .Height = 56,
                .BackColor = Color.White,
                .Padding = New Padding(16, 10, 16, 10)
            }

            btnAddOverride = New KryptonButton() With {.Text = "+ Add Override", .Size = New Size(140, 36), .Location = New Point(16, 10)}
            ThemeConstants.ApplyKryptonPrimaryButton(btnAddOverride)
            AddHandler btnAddOverride.Click, Sub(s, e) AddOverride()

            btnRemoveOverride = New KryptonButton() With {.Text = "🗑 Remove Override", .Size = New Size(150, 36), .Location = New Point(164, 10)}
            ThemeConstants.ApplyKryptonSecondaryButton(btnRemoveOverride)
            AddHandler btnRemoveOverride.Click, Async Sub(s, e) Await RemoveOverrideAsync()

            btnClose = New KryptonButton() With {.Text = "Close", .Size = New Size(90, 36), .Location = New Point(810, 10), .Anchor = AnchorStyles.Right Or AnchorStyles.Bottom}
            ThemeConstants.ApplyKryptonSecondaryButton(btnClose)
            AddHandler btnClose.Click, Sub(s, e) Me.Close()

            pnlFooter.Controls.Add(btnAddOverride)
            pnlFooter.Controls.Add(btnRemoveOverride)
            pnlFooter.Controls.Add(btnClose)

            Me.Controls.Add(pnlGridHost)
            Me.Controls.Add(pnlFooter)
            Me.Controls.Add(pnlFilterBar)
            Me.Controls.Add(pnlHeader)
        End Sub

        Protected Overrides Async Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)

            ' Authorization Guard
            If Not _authzService.IsAuthorized(UserRole.Admin) Then
                FrmInAppAlert.ShowModal(Me, "Access Denied", "Access Denied: User Permission Overrides console requires System Administrator privileges.", AlertType.WarningAlert)
                Me.Close()
                Return
            End If

            Await LoadUserAccountsAsync()
        End Sub

        Private Async Function LoadUserAccountsAsync() As Task
            Try
                _usersList = Await _userRepo.GetAllAsync()
                cboUsers.Items.Clear()

                For Each u In _usersList
                    cboUsers.Items.Add($"{u.FullName} ({u.Username})")
                Next

                If cboUsers.Items.Count > 0 Then
                    cboUsers.SelectedIndex = 0
                End If
            Catch ex As Exception
                FrmInAppAlert.ShowModal(Me, "Error", "Failed to load user accounts: " & ex.Message, AlertType.WarningAlert)
            End Try
        End Function

        Private Function GetSelectedUserId() As Integer
            If cboUsers.SelectedIndex < 0 OrElse _usersList Is Nothing OrElse cboUsers.SelectedIndex >= _usersList.Count Then Return 0
            Return _usersList(cboUsers.SelectedIndex).UserId
        End Function

        Private Async Function LoadOverridesForSelectedUserAsync() As Task
            Dim userId = GetSelectedUserId()
            If userId <= 0 Then Return

            Try
                _overridesList = Await _overrideService.GetOverridesForUserAsync(userId)
                dgvOverrides.Rows.Clear()

                For Each item In _overridesList
                    Dim accessState = If(item.IsGranted, "✅ EXPLICIT GRANT", "⛔ EXPLICIT DENY")
                    Dim rowIndex = dgvOverrides.Rows.Add(item.OverrideId, item.PermissionId, item.PermissionCode, item.PermissionName, item.ModuleName, accessState, item.Reason)

                    If Not item.IsGranted Then
                        dgvOverrides.Rows(rowIndex).DefaultCellStyle.ForeColor = ThemeConstants.DangerRed
                        dgvOverrides.Rows(rowIndex).DefaultCellStyle.Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold)
                    End If
                Next
            Catch ex As Exception
                FrmInAppAlert.ShowModal(Me, "Error", "Failed to load user permission overrides: " & ex.Message, AlertType.WarningAlert)
            End Try
        End Function

        Private Sub AddOverride()
            Dim userId = GetSelectedUserId()
            If userId <= 0 Then
                FrmInAppAlert.ShowModal(Me, "Selection Required", "Please select a target user account.", AlertType.WarningAlert)
                Return
            End If

            Dim username = If(cboUsers.SelectedIndex >= 0 AndAlso cboUsers.SelectedIndex < _usersList.Count, _usersList(cboUsers.SelectedIndex).Username, "User")
            Using dlg As New FrmAddEditUserOverride(userId, username)
                If dlg.ShowDialog(Me) = DialogResult.OK Then
                    Dim task = LoadOverridesForSelectedUserAsync()
                End If
            End Using
        End Sub

        Private Async Function RemoveOverrideAsync() As Task
            If dgvOverrides.SelectedRows.Count = 0 Then
                FrmInAppAlert.ShowModal(Me, "Selection Required", "Please select an override row to remove.", AlertType.WarningAlert)
                Return
            End If

            Dim userId = GetSelectedUserId()
            Dim permId = Convert.ToInt32(dgvOverrides.SelectedRows(0).Cells("PermissionId").Value)
            Dim permCode = Convert.ToString(dgvOverrides.SelectedRows(0).Cells("PermissionCode").Value)

            Dim result = MessageBox.Show($"Are you sure you want to remove the permission override for '{permCode}'?", "Confirm Removal", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If result <> DialogResult.Yes Then Return

            Try
                Dim success = Await _overrideService.RemoveOverrideAsync(userId, permId)
                If success Then
                    FrmInAppAlert.ShowModal(Me, "Success", $"Permission override for '{permCode}' removed.", AlertType.SuccessAlert)
                    Await LoadOverridesForSelectedUserAsync()
                End If
            Catch ex As Exception
                FrmInAppAlert.ShowModal(Me, "Error", "Failed to remove permission override: " & ex.Message, AlertType.WarningAlert)
            End Try
        End Function
    End Class
End Namespace
