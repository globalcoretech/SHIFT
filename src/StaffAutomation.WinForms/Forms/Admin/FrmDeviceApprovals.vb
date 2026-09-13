Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Linq
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports Krypton.Toolkit
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Security
Imports StaffAutomation.Core.Configuration
Imports StaffAutomation.DAL.Configuration
Imports StaffAutomation.DAL.Core
Imports StaffAutomation.DAL.Repositories
Imports StaffAutomation.BLL.Services
Imports StaffAutomation.WinForms.Forms.Common
Imports StaffAutomation.WinForms.Forms.Main

Namespace Forms.Admin
    Public Class FrmDeviceApprovals
        Inherits Form

        Private ReadOnly _deviceService As IDeviceService
        Private _pendingDevices As List(Of UserDeviceDto)
        
        Private dgvDevices As DataGridView
        Private btnApprove As KryptonButton
        Private btnReject As KryptonButton
        Private btnClose As KryptonButton
        Private lblEmptyState As Label

        Public Sub New()
            ' Setup DI manually (as done in other forms)
            Dim config As IAppConfiguration = New AppConfiguration()
            Dim connFactory As IDatabaseConnectionFactory = New DbConnectionFactory(config)
            Dim sqlHelper As ISqlHelper = New SqlHelper(connFactory)
            Dim deviceRepo As DAL.Interfaces.IDeviceRepository = New DeviceRepository(sqlHelper)
            Dim userRepo As DAL.Interfaces.IUserRepository = New UserRepository(sqlHelper)
            _deviceService = New DeviceService(deviceRepo, userRepo)

            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Text = "📱 Device Approvals"
            Me.Size = New Size(900, 500)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.BackColor = ThemeConstants.WorkspaceBackground
            Me.Padding = New Padding(20)
            
            Dim pnlHeader As New Panel() With {.Dock = DockStyle.Top, .Height = 80}
            Dim lblTitle As New Label() With {.Text = "Pending Device Approvals", .Font = New Font(ThemeConstants.FontNameDefault, 14.0!, FontStyle.Bold), .ForeColor = ThemeConstants.TextPrimary, .Location = New Point(0, 0), .AutoSize = True}
            Dim lblSubtitle As New Label() With {.Text = "Review and approve login access for new staff devices.", .Font = New Font(ThemeConstants.FontNameDefault, 9.5!), .ForeColor = ThemeConstants.TextSecondary, .Location = New Point(2, 30), .AutoSize = True}
            pnlHeader.Controls.Add(lblTitle)
            pnlHeader.Controls.Add(lblSubtitle)
            Me.Controls.Add(pnlHeader)

            Dim pnlFooter As New Panel() With {.Dock = DockStyle.Bottom, .Height = 60, .Padding = New Padding(0, 20, 0, 0)}
            
            Dim pnlActions As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.LeftToRight}
            
            btnApprove = New KryptonButton() With {.Text = "✅ Approve Selected", .Size = New Size(180, 40), .Margin = New Padding(0,0,10,0)}
            ThemeConstants.ApplyKryptonPrimaryButton(btnApprove)
            btnApprove.StateCommon.Back.Color1 = ThemeConstants.SuccessGreen
            btnApprove.StateCommon.Back.Color2 = ThemeConstants.SuccessGreen
            AddHandler btnApprove.Click, Async Sub(s, e) Await ApproveSelectedAsync()

            btnReject = New KryptonButton() With {.Text = "❌ Reject", .Size = New Size(120, 40), .Margin = New Padding(0,0,20,0)}
            ThemeConstants.ApplyKryptonSecondaryButton(btnReject)
            AddHandler btnReject.Click, Async Sub(s, e) Await RejectSelectedAsync()
            
            btnClose = New KryptonButton() With {.Text = "Close", .Size = New Size(100, 40), .Margin = New Padding(0)}
            ThemeConstants.ApplyKryptonSecondaryButton(btnClose)
            AddHandler btnClose.Click, Sub(s, e) Me.Close()
            
            pnlActions.Controls.Add(btnApprove)
            pnlActions.Controls.Add(btnReject)
            pnlActions.Controls.Add(btnClose)
            
            pnlFooter.Controls.Add(pnlActions)
            Me.Controls.Add(pnlFooter)

            Dim pnlGridHost As New Panel() With {.Dock = DockStyle.Fill, .Padding = New Padding(0, 10, 0, 10)}
            
            lblEmptyState = New Label() With {
                .Text = "🎉 No pending device approvals! Everything is up to date.",
                .Font = New Font(ThemeConstants.FontNameDefault, 11.0!, FontStyle.Italic),
                .ForeColor = ThemeConstants.TextSecondary,
                .Dock = DockStyle.Top,
                .Height = 40,
                .TextAlign = ContentAlignment.MiddleCenter,
                .Visible = False
            }

            dgvDevices = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .MultiSelect = False,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .AutoGenerateColumns = False,
                .ReadOnly = True,
                .BackgroundColor = Color.White,
                .BorderStyle = BorderStyle.None
            }
            ThemeConstants.ApplyModernGridStyle(dgvDevices)

            dgvDevices.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colId", .DataPropertyName = "DeviceId", .Visible = False})
            dgvDevices.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colStaff", .DataPropertyName = "StaffName", .HeaderText = "Staff Name", .Width = 200})
            dgvDevices.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colMachine", .DataPropertyName = "MachineName", .HeaderText = "Machine Name", .Width = 200})
            dgvDevices.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colOS", .DataPropertyName = "OSVersion", .HeaderText = "OS Version", .Width = 150})
            dgvDevices.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colRequested", .DataPropertyName = "RequestedOn", .HeaderText = "Requested On (UTC)", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})
            
            pnlGridHost.Controls.Add(dgvDevices)
            pnlGridHost.Controls.Add(lblEmptyState)
            
            Me.Controls.Add(pnlGridHost)
            
            pnlGridHost.BringToFront()
            pnlHeader.BringToFront()
        End Sub

        Protected Overrides Async Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)
            Await LoadPendingDevicesAsync()
        End Sub

        Private Async Function LoadPendingDevicesAsync() As Task
            Try
                Dim allDevices = Await _deviceService.GetAllDevicesAsync()
                _pendingDevices = allDevices.Where(Function(d) d.Status = DeviceStatus.Pending).OrderByDescending(Function(d) d.RequestedOn).ToList()
                
                dgvDevices.DataSource = _pendingDevices
                
                If _pendingDevices.Count = 0 Then
                    dgvDevices.Visible = False
                    lblEmptyState.Visible = True
                    btnApprove.Enabled = False
                    btnReject.Enabled = False
                Else
                    dgvDevices.Visible = True
                    lblEmptyState.Visible = False
                    btnApprove.Enabled = True
                    btnReject.Enabled = True
                End If
            Catch ex As Exception
                FrmInAppAlert.ShowModal(Me, "Error", "Failed to load devices: " & ex.Message, AlertType.ErrorAlert)
            End Try
        End Function

        Private Async Function ApproveSelectedAsync() As Task
            If dgvDevices.SelectedRows.Count = 0 Then Return
            Dim deviceId = Convert.ToInt32(dgvDevices.SelectedRows(0).Cells("colId").Value)
            Dim staffName = dgvDevices.SelectedRows(0).Cells("colStaff").Value.ToString()
            
            Dim confirm = FrmInAppAlert.ShowModal(Me, "Approve Device", $"Are you sure you want to approve this device for {staffName}?", AlertType.InfoAlert, actionText:="YES, APPROVE", showCancel:=True)
            If confirm = DialogResult.OK Then
                Try
                    Me.Cursor = Cursors.WaitCursor
                    Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)
                    Await _deviceService.ApproveDeviceAsync(deviceId, currentUserId)
                    Await LoadPendingDevicesAsync()
                Catch ex As Exception
                    FrmInAppAlert.ShowModal(Me, "Error", "Failed to approve: " & ex.Message, AlertType.ErrorAlert)
                Finally
                    Me.Cursor = Cursors.Default
                End Try
            End If
        End Function

        Private Async Function RejectSelectedAsync() As Task
            If dgvDevices.SelectedRows.Count = 0 Then Return
            Dim deviceId = Convert.ToInt32(dgvDevices.SelectedRows(0).Cells("colId").Value)
            Dim staffName = dgvDevices.SelectedRows(0).Cells("colStaff").Value.ToString()
            
            Dim confirm = FrmInAppAlert.ShowModal(Me, "Reject Device", $"Are you sure you want to REJECT this device for {staffName}?", AlertType.WarningAlert, actionText:="YES, REJECT", showCancel:=True)
            If confirm = DialogResult.OK Then
                Try
                    Me.Cursor = Cursors.WaitCursor
                    Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)
                    Await _deviceService.RejectDeviceAsync(deviceId, currentUserId)
                    Await LoadPendingDevicesAsync()
                Catch ex As Exception
                    FrmInAppAlert.ShowModal(Me, "Error", "Failed to reject: " & ex.Message, AlertType.ErrorAlert)
                Finally
                    Me.Cursor = Cursors.Default
                End Try
            End If
        End Function
    End Class
End Namespace
