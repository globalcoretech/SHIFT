Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing
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
    ''' WinForms Configuration Console Form for Financial Year Management.
    ''' Conforms to Krypton design system ThemeConstants and provides grid listing, activation, and lock controls.
    ''' </summary>
    Public Class FrmFinancialYearManagement
        Inherits Form

        Private ReadOnly _fyService As IFinancialYearService
        Private ReadOnly _authzService As IAuthorizationService

        Private pnlHeader As Panel
        Private lblTitle As Label
        Private lblSubtitle As Label

        Private pnlToolbar As Panel
        Private btnAddFY As KryptonButton
        Private btnEditFY As KryptonButton
        Private btnSetActive As KryptonButton
        Private btnLock As KryptonButton
        Private btnUnlock As KryptonButton
        Private btnRefresh As KryptonButton

        Private dgvFY As DataGridView
        Private _fyList As List(Of FinancialYearDto)

        Public Sub New()
            Dim config As IAppConfiguration = New AppConfiguration()
            Dim connFactory As IDatabaseConnectionFactory = New DbConnectionFactory(config)
            Dim sqlHelper As ISqlHelper = New SqlHelper(connFactory)
            Dim appLogger As IAppLogger = New AppLogger(config)
            Dim auditLogger As IAuditLogger = New AuditLogger(sqlHelper)

            Dim fyRepo As IFinancialYearRepository = New FinancialYearRepository(sqlHelper, connFactory)
            _fyService = New FinancialYearService(fyRepo, appLogger, auditLogger)
            _authzService = New AuthorizationService()

            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Text = "📅 Financial Year Management"
            Me.Size = New Size(880, 580)
            Me.MinimumSize = New Size(760, 500)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.BackColor = ThemeConstants.WorkspaceBackground

            ' 1. Header Panel
            pnlHeader = New Panel()
            lblTitle = New Label() With {.Text = "📅 Accounting Financial Years"}
            lblSubtitle = New Label() With {.Text = "Set active accounting period, statutory cutoff windows, and lock completed financial years."}
            ThemeConstants.ApplyHeaderStyle(pnlHeader, lblTitle, lblSubtitle)
            pnlHeader.Controls.Add(lblSubtitle)
            pnlHeader.Controls.Add(lblTitle)

            ' 2. Toolbar Panel
            pnlToolbar = New Panel()
            ThemeConstants.ApplyToolbarStyle(pnlToolbar)

            btnAddFY = New KryptonButton() With {.Text = "+ Add FY", .Size = New Size(110, 36), .Location = New Point(12, 10)}
            ThemeConstants.ApplyKryptonPrimaryButton(btnAddFY)
            AddHandler btnAddFY.Click, Sub(s, e) AddFinancialYear()

            btnEditFY = New KryptonButton() With {.Text = "Edit FY", .Size = New Size(95, 36), .Location = New Point(130, 10)}
            ThemeConstants.ApplyKryptonSecondaryButton(btnEditFY)
            AddHandler btnEditFY.Click, Sub(s, e) EditFinancialYear()

            btnSetActive = New KryptonButton() With {.Text = "⚡ Set Active", .Size = New Size(115, 36), .Location = New Point(233, 10)}
            ThemeConstants.ApplyKryptonSecondaryButton(btnSetActive)
            AddHandler btnSetActive.Click, Async Sub(s, e) Await SetActiveFinancialYearAsync()

            btnLock = New KryptonButton() With {.Text = "🔒 Lock FY", .Size = New Size(100, 36), .Location = New Point(356, 10)}
            ThemeConstants.ApplyKryptonSecondaryButton(btnLock)
            AddHandler btnLock.Click, Async Sub(s, e) Await ToggleLockStateAsync(True)

            btnUnlock = New KryptonButton() With {.Text = "🔓 Unlock FY", .Size = New Size(110, 36), .Location = New Point(464, 10)}
            ThemeConstants.ApplyKryptonSecondaryButton(btnUnlock)
            AddHandler btnUnlock.Click, Async Sub(s, e) Await ToggleLockStateAsync(False)

            btnRefresh = New KryptonButton() With {.Text = "🔄 Refresh", .Size = New Size(100, 36), .Location = New Point(582, 10)}
            ThemeConstants.ApplyKryptonSecondaryButton(btnRefresh)
            AddHandler btnRefresh.Click, Async Sub(s, e) Await LoadGridAsync()

            pnlToolbar.Controls.Add(btnAddFY)
            pnlToolbar.Controls.Add(btnEditFY)
            pnlToolbar.Controls.Add(btnSetActive)
            pnlToolbar.Controls.Add(btnLock)
            pnlToolbar.Controls.Add(btnUnlock)
            pnlToolbar.Controls.Add(btnRefresh)

            ' 3. DataGridView Setup
            dgvFY = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .ReadOnly = True,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .MultiSelect = False,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .AutoGenerateColumns = False
            }
            ThemeConstants.ApplyModernGridStyle(dgvFY)

            dgvFY.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "FinancialYearId", .HeaderText = "ID", .DataPropertyName = "FinancialYearId", .Visible = False})
            dgvFY.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "FYCode", .HeaderText = "FY Code", .DataPropertyName = "FYCode", .Width = 140})
            dgvFY.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "StartDate", .HeaderText = "Start Date", .DataPropertyName = "StartDate", .Width = 140})
            dgvFY.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "EndDate", .HeaderText = "End Date", .DataPropertyName = "EndDate", .Width = 140})
            dgvFY.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "IsCurrentFY", .HeaderText = "Active Status", .DataPropertyName = "IsCurrentFY", .Width = 140})
            dgvFY.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "IsLocked", .HeaderText = "Lock State", .DataPropertyName = "IsLocked", .Width = 140})

            Dim pnlGridHost As New Panel() With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(16, 0, 16, 16)
            }
            pnlGridHost.Controls.Add(dgvFY)

            Me.Controls.Add(pnlGridHost)
            Me.Controls.Add(pnlToolbar)
            Me.Controls.Add(pnlHeader)
        End Sub

        Protected Overrides Async Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)

            ' Authorization Guard
            If Not _authzService.IsAuthorized(UserRole.Admin) AndAlso Not _authzService.IsAuthorized(UserRole.Owner) Then
                FrmInAppAlert.ShowModal(Me, "Access Denied", "Access Denied: Financial Year Management requires Administrator or Owner privileges.", AlertType.WarningAlert)
                Me.Close()
                Return
            End If

            Await LoadGridAsync()
        End Sub

        Private Async Function LoadGridAsync() As Task
            Try
                _fyList = Await _fyService.GetAllFinancialYearsAsync(includeDeleted:=False)
                dgvFY.Rows.Clear()

                For Each item In _fyList
                    Dim activeBadge = If(item.IsCurrentFY, "⚡ CURRENT ACTIVE", "Inactive")
                    Dim lockBadge = If(item.IsLocked, "🔒 LOCKED", "Open")

                    Dim rowIndex = dgvFY.Rows.Add(item.FinancialYearId, item.FYCode, item.StartDate.ToString("yyyy-MM-dd"), item.EndDate.ToString("yyyy-MM-dd"), activeBadge, lockBadge)
                    If item.IsCurrentFY Then
                        dgvFY.Rows(rowIndex).DefaultCellStyle.Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold)
                        dgvFY.Rows(rowIndex).DefaultCellStyle.BackColor = Color.FromArgb(236, 253, 245) ' Emerald tint
                    End If
                Next
            Catch ex As Exception
                FrmInAppAlert.ShowModal(Me, "Error", "Failed to load Financial Years: " & ex.Message, AlertType.WarningAlert)
            End Try
        End Function

        Private Function GetSelectedFY() As FinancialYearDto
            If dgvFY.SelectedRows.Count = 0 Then Return Nothing
            Dim selectedId = Convert.ToInt32(dgvFY.SelectedRows(0).Cells("FinancialYearId").Value)
            Return _fyList.Find(Function(f) f.FinancialYearId = selectedId)
        End Function

        Private Sub AddFinancialYear()
            Using dlg As New FrmAddEditFinancialYear(_fyService)
                If dlg.ShowDialog(Me) = DialogResult.OK Then
                    Dim task = LoadGridAsync()
                End If
            End Using
        End Sub

        Private Sub EditFinancialYear()
            Dim item = GetSelectedFY()
            If item Is Nothing Then
                FrmInAppAlert.ShowModal(Me, "Selection Required", "Please select a Financial Year row to edit.", AlertType.WarningAlert)
                Return
            End If

            If item.IsLocked Then
                FrmInAppAlert.ShowModal(Me, "Locked FY", $"Financial Year '{item.FYCode}' is locked and cannot be edited.", AlertType.WarningAlert)
                Return
            End If

            Using dlg As New FrmAddEditFinancialYear(_fyService, item)
                If dlg.ShowDialog(Me) = DialogResult.OK Then
                    Dim task = LoadGridAsync()
                End If
            End Using
        End Sub

        Private Async Function SetActiveFinancialYearAsync() As Task
            Dim item = GetSelectedFY()
            If item Is Nothing Then
                FrmInAppAlert.ShowModal(Me, "Selection Required", "Please select a Financial Year row to activate.", AlertType.WarningAlert)
                Return
            End If

            If item.IsCurrentFY Then
                FrmInAppAlert.ShowModal(Me, "Notice", $"Financial Year '{item.FYCode}' is already the current active FY.", AlertType.WarningAlert)
                Return
            End If

            Dim result = MessageBox.Show($"Are you sure you want to set '{item.FYCode}' as the active financial year for the firm?", "Confirm Activation", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If result <> DialogResult.Yes Then Return

            Try
                Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)
                Dim success = Await _fyService.ActivateFinancialYearAsync(item.FinancialYearId, currentUserId)
                If success Then
                    FrmInAppAlert.ShowModal(Me, "Success", $"Financial Year '{item.FYCode}' is now the current active accounting period.", AlertType.SuccessAlert)
                    Await LoadGridAsync()
                End If
            Catch ex As Exception
                FrmInAppAlert.ShowModal(Me, "Error", "Failed to activate Financial Year: " & ex.Message, AlertType.WarningAlert)
            End Try
        End Function

        Private Async Function ToggleLockStateAsync(lockTarget As Boolean) As Task
            Dim item = GetSelectedFY()
            If item Is Nothing Then
                FrmInAppAlert.ShowModal(Me, "Selection Required", "Please select a Financial Year row.", AlertType.WarningAlert)
                Return
            End If

            Dim actionName = If(lockTarget, "Lock", "Unlock")
            Dim result = MessageBox.Show($"Are you sure you want to {actionName.ToLower()} Financial Year '{item.FYCode}'?", $"Confirm {actionName}", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If result <> DialogResult.Yes Then Return

            Try
                Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)
                Dim success = Await _fyService.LockFinancialYearAsync(item.FinancialYearId, lockTarget, currentUserId)
                If success Then
                    FrmInAppAlert.ShowModal(Me, "Success", $"Financial Year '{item.FYCode}' has been {actionName.ToLower()}ed.", AlertType.SuccessAlert)
                    Await LoadGridAsync()
                End If
            Catch ex As Exception
                FrmInAppAlert.ShowModal(Me, "Error", $"Failed to {actionName.ToLower()} Financial Year: " & ex.Message, AlertType.WarningAlert)
            End Try
        End Function
    End Class
End Namespace
