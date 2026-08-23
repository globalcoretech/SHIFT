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
Imports Microsoft.Data.SqlClient
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
    ''' WinForms Security Matrix Console Form for Role Permission Management.
    ''' Loads role catalog dynamically from dbo.tbl_Roles and maps catalog permissions via IRolePermissionService.
    ''' </summary>
    Public Class FrmRolePermissionManagement
        Inherits Form

        Private ReadOnly _rolePermService As IRolePermissionService
        Private ReadOnly _permService As IPermissionService
        Private ReadOnly _authzService As IAuthorizationService
        Private ReadOnly _sqlHelper As ISqlHelper

        Private pnlHeader As Panel
        Private lblTitle As Label
        Private lblSubtitle As Label

        Private splitMain As SplitContainer
        Private cboRoles As KryptonComboBox
        Private lblRoleDesc As Label

        Private dgvPermissions As DataGridView
        Private btnSelectAll As KryptonButton
        Private btnClearAll As KryptonButton
        Private btnSave As KryptonButton
        Private btnCancel As KryptonButton

        Private _rolesMap As New Dictionary(Of Integer, String)()

        Public Sub New()
            Dim config As IAppConfiguration = New AppConfiguration()
            Dim connFactory As IDatabaseConnectionFactory = New DbConnectionFactory(config)
            _sqlHelper = New SqlHelper(connFactory)
            Dim appLogger As IAppLogger = New AppLogger(config)
            Dim auditLogger As IAuditLogger = New AuditLogger(_sqlHelper)

            Dim permRepo As IPermissionRepository = New PermissionRepository(_sqlHelper)
            Dim rolePermRepo As IRolePermissionRepository = New RolePermissionRepository(_sqlHelper, connFactory)

            _permService = New PermissionService(permRepo, appLogger)
            _rolePermService = New RolePermissionService(rolePermRepo, permRepo, appLogger, auditLogger)
            _authzService = New AuthorizationService()

            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Text = "👤 Role Permission Security Matrix"
            Me.Size = New Size(940, 620)
            Me.MinimumSize = New Size(800, 500)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.BackColor = ThemeConstants.WorkspaceBackground

            ' 1. Header Panel
            pnlHeader = New Panel()
            lblTitle = New Label() With {.Text = "👤 Role-Based Access Controls"}
            lblSubtitle = New Label() With {.Text = "Assign catalog permissions to database security roles (Owner / Admin / Staff)."}
            ThemeConstants.ApplyHeaderStyle(pnlHeader, lblTitle, lblSubtitle)
            pnlHeader.Controls.Add(lblSubtitle)
            pnlHeader.Controls.Add(lblTitle)

            ' 2. Split Container Layout
            splitMain = New SplitContainer() With {
                .Dock = DockStyle.Fill,
                .Orientation = Orientation.Vertical,
                .FixedPanel = FixedPanel.Panel1,
                .SplitterDistance = 280,
                .Padding = New Padding(16, 8, 16, 8),
                .BackColor = ThemeConstants.WorkspaceBackground
            }

            ' Left Role Selector Panel
            Dim pnlLeft As New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.White, .Padding = New Padding(16)}
            ThemeConstants.ApplyCardSurfaceStyle(pnlLeft)

            Dim lblSelectRole As New Label() With {
                .Text = "Select Target Role:",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Dock = DockStyle.Top,
                .Height = 24
            }

            cboRoles = New KryptonComboBox() With {.Dock = DockStyle.Top, .Height = 34}
            ThemeConstants.ApplyAppComboBoxStyle(cboRoles)
            AddHandler cboRoles.SelectedIndexChanged, Async Sub(s, e) Await OnRoleSelectedAsync()

            lblRoleDesc = New Label() With {
                .Dock = DockStyle.Fill,
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Italic),
                .ForeColor = ThemeConstants.TextSecondary,
                .Padding = New Padding(0, 16, 0, 0)
            }

            pnlLeft.Controls.Add(lblRoleDesc)
            pnlLeft.Controls.Add(cboRoles)
            pnlLeft.Controls.Add(lblSelectRole)
            splitMain.Panel1.Controls.Add(pnlLeft)

            ' Right Permission Grid & Footer Panel
            Dim pnlRight As New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.White, .Padding = New Padding(12)}
            ThemeConstants.ApplyCardSurfaceStyle(pnlRight)

            ' DataGridView Setup
            dgvPermissions = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .MultiSelect = False,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .AutoGenerateColumns = False
            }
            ThemeConstants.ApplyModernGridStyle(dgvPermissions)

            Dim colCheck As New DataGridViewCheckBoxColumn() With {.Name = "colSelect", .HeaderText = "Granted", .Width = 65}
            Dim colCode As New DataGridViewTextBoxColumn() With {.Name = "colCode", .HeaderText = "Permission Code", .ReadOnly = True, .Width = 150}
            Dim colName As New DataGridViewTextBoxColumn() With {.Name = "colName", .HeaderText = "Permission Name", .ReadOnly = True, .Width = 170}
            Dim colModule As New DataGridViewTextBoxColumn() With {.Name = "colModule", .HeaderText = "Module", .ReadOnly = True, .Width = 130}
            Dim colDesc As New DataGridViewTextBoxColumn() With {.Name = "colDesc", .HeaderText = "Description", .ReadOnly = True, .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill}
            Dim colId As New DataGridViewTextBoxColumn() With {.Name = "colId", .HeaderText = "ID", .Visible = False}

            dgvPermissions.Columns.AddRange(colCheck, colCode, colName, colModule, colDesc, colId)

            ' Footer Action Toolbar (Always visible at bottom)
            Dim pnlFooter As New Panel() With {.Dock = DockStyle.Bottom, .Height = 50, .Padding = New Padding(0, 8, 0, 0)}

            Dim pnlFooterLeft As New FlowLayoutPanel() With {
                .Dock = DockStyle.Left,
                .AutoSize = True,
                .FlowDirection = FlowDirection.LeftToRight,
                .BackColor = Color.Transparent
            }

            btnSelectAll = New KryptonButton() With {.Text = "Select All", .Size = New Size(95, 34), .Margin = New Padding(0, 0, 8, 0)}
            ThemeConstants.ApplyKryptonSecondaryButton(btnSelectAll)
            AddHandler btnSelectAll.Click, Sub(s, e) SetAllCheckStates(True)

            btnClearAll = New KryptonButton() With {.Text = "Clear All", .Size = New Size(95, 34), .Margin = New Padding(0)}
            ThemeConstants.ApplyKryptonSecondaryButton(btnClearAll)
            AddHandler btnClearAll.Click, Sub(s, e) SetAllCheckStates(False)

            pnlFooterLeft.Controls.Add(btnSelectAll)
            pnlFooterLeft.Controls.Add(btnClearAll)

            Dim pnlFooterRight As New FlowLayoutPanel() With {
                .Dock = DockStyle.Right,
                .AutoSize = True,
                .FlowDirection = FlowDirection.RightToLeft,
                .BackColor = Color.Transparent
            }

            btnCancel = New KryptonButton() With {.Text = "Cancel", .Size = New Size(90, 34), .Margin = New Padding(8, 0, 0, 0)}
            ThemeConstants.ApplyKryptonSecondaryButton(btnCancel)
            AddHandler btnCancel.Click, Sub(s, e) Me.Close()

            btnSave = New KryptonButton() With {.Text = "💾 Save Matrix", .Size = New Size(130, 34), .Margin = New Padding(0)}
            ThemeConstants.ApplyKryptonPrimaryButton(btnSave)
            AddHandler btnSave.Click, Async Sub(s, e) Await SaveRolePermissionsAsync()

            pnlFooterRight.Controls.Add(btnCancel)
            pnlFooterRight.Controls.Add(btnSave)

            pnlFooter.Controls.Add(pnlFooterLeft)
            pnlFooter.Controls.Add(pnlFooterRight)

            pnlRight.Controls.Add(dgvPermissions)
            pnlRight.Controls.Add(pnlFooter)
            pnlFooter.BringToFront()

            splitMain.Panel2.Controls.Add(pnlRight)
            Me.Controls.Add(splitMain)
            Me.Controls.Add(pnlHeader)
        End Sub

        Protected Overrides Async Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)

            ' Authorization Guard
            If Not _authzService.IsAuthorized(UserRole.Admin) Then
                FrmInAppAlert.ShowModal(Me, "Access Denied", "Access Denied: Role Permission Management requires System Administrator privileges.", AlertType.WarningAlert)
                Me.Close()
                Return
            End If

            Await LoadRolesAndCatalogAsync()
        End Sub

        Private Async Function LoadRolesAndCatalogAsync() As Task
            Try
                ' 1. Load roles dynamically from dbo.tbl_Roles
                Const rolesQuery As String = "SELECT RoleId, RoleName, Description FROM dbo.tbl_Roles ORDER BY RoleId ASC;"
                Dim mapFunc As Func(Of IDataReader, Tuple(Of Integer, String, String)) =
                    Function(r) New Tuple(Of Integer, String, String)(Convert.ToInt32(r("RoleId")), Convert.ToString(r("RoleName")), If(r("Description") Is DBNull.Value, "", Convert.ToString(r("Description"))))

                Dim rolesList = Await _sqlHelper.ExecuteReaderAsync(rolesQuery, Nothing, mapFunc)
                cboRoles.Items.Clear()
                _rolesMap.Clear()

                For Each r In rolesList
                    cboRoles.Items.Add($"{r.Item2} (ID: {r.Item1})")
                    _rolesMap(r.Item1) = r.Item3
                Next

                ' 2. Load Permissions Catalog
                Dim perms = Await _permService.GetAllPermissionsAsync()
                dgvPermissions.Rows.Clear()
                For Each p In perms
                    dgvPermissions.Rows.Add(False, p.PermissionCode, p.PermissionName, p.ModuleName, p.Description, p.PermissionId)
                Next

                If cboRoles.Items.Count > 0 Then
                    cboRoles.SelectedIndex = 0
                End If
            Catch ex As Exception
                FrmInAppAlert.ShowModal(Me, "Error", "Failed to load Role Catalog: " & ex.Message, AlertType.WarningAlert)
            End Try
        End Function

        Private Async Function OnRoleSelectedAsync() As Task
            If cboRoles.SelectedIndex < 0 Then Return
            Dim roleId = GetSelectedRoleId()
            If roleId <= 0 Then Return

            lblRoleDesc.Text = If(_rolesMap.ContainsKey(roleId), _rolesMap(roleId), "")

            Try
                Dim grantedPerms = Await _rolePermService.GetPermissionsForRoleAsync(roleId)
                Dim grantedIds = grantedPerms.Select(Function(p) p.PermissionId).ToHashSet()

                For Each row As DataGridViewRow In dgvPermissions.Rows
                    Dim permId = Convert.ToInt32(row.Cells("colId").Value)
                    row.Cells("colSelect").Value = grantedIds.Contains(permId)
                Next
            Catch ex As Exception
                FrmInAppAlert.ShowModal(Me, "Error", "Failed to fetch role permissions: " & ex.Message, AlertType.WarningAlert)
            End Try
        End Function

        Private Function GetSelectedRoleId() As Integer
            If cboRoles.SelectedIndex < 0 Then Return 0
            Dim selectedText = cboRoles.SelectedItem.ToString()
            ' Format: "Admin (ID: 3)"
            Dim startIdx = selectedText.IndexOf("ID: ")
            If startIdx > 0 Then
                Dim idStr = selectedText.Substring(startIdx + 4).Replace(")", "").Trim()
                Dim roleId As Integer
                If Integer.TryParse(idStr, roleId) Then Return roleId
            End If
            Return 0
        End Function

        Private Sub SetAllCheckStates(isSelect As Boolean)
            For Each row As DataGridViewRow In dgvPermissions.Rows
                row.Cells("colSelect").Value = isSelect
            Next
        End Sub

        Private Async Function SaveRolePermissionsAsync() As Task
            Dim roleId = GetSelectedRoleId()
            If roleId <= 0 Then
                FrmInAppAlert.ShowModal(Me, "Selection Required", "Please select a target role.", AlertType.WarningAlert)
                Return
            End If

            btnSave.Enabled = False
            Try
                Dim selectedIds As New List(Of Integer)()
                For Each row As DataGridViewRow In dgvPermissions.Rows
                    If Convert.ToBoolean(row.Cells("colSelect").Value) = True Then
                        selectedIds.Add(Convert.ToInt32(row.Cells("colId").Value))
                    End If
                Next

                Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)
                Dim success = Await _rolePermService.UpdateRolePermissionsAsync(roleId, selectedIds, currentUserId)
                If success Then
                    FrmInAppAlert.ShowModal(Me, "Success", $"Role Permissions matrix updated successfully ({selectedIds.Count} permissions granted).", AlertType.SuccessAlert)
                Else
                    FrmInAppAlert.ShowModal(Me, "Error", "Failed to update Role Permissions matrix.", AlertType.WarningAlert)
                End If
            Catch ex As Exception
                FrmInAppAlert.ShowModal(Me, "Error", "Failed to save permissions matrix: " & ex.Message, AlertType.WarningAlert)
            Finally
                btnSave.Enabled = True
            End Try
        End Function
    End Class
End Namespace
