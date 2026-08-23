Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Drawing
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports FontAwesome.Sharp
Imports Krypton.Toolkit
Imports StaffAutomation.BLL.Interfaces
Imports StaffAutomation.BLL.Logging
Imports StaffAutomation.BLL.Services
Imports StaffAutomation.Core.Configuration
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Logging
Imports StaffAutomation.Core.Security
Imports StaffAutomation.DAL.Configuration
Imports StaffAutomation.DAL.Core
Imports StaffAutomation.DAL.Repositories
Imports StaffAutomation.WinForms.Forms.Common
Imports StaffAutomation.WinForms.Forms.Main
Imports StaffAutomation.WinForms.UIHelpers

Namespace Forms.Attendance
    ''' <summary>
    ''' Operational Management Dashboard for Admin Staff Attendance Monitoring.
    ''' Pilot conversion using Krypton Toolkit UI controls and FontAwesome.Sharp vector icons.
    ''' Maintains 100% of existing attendance service logic, permissions, filters, and grid data bindings.
    ''' </summary>
    Public Class AdminAttendanceBoardControl
        Inherits UserControl

        Private ReadOnly _attendanceService As IAttendanceService
        Private ReadOnly _userRepo As DAL.Interfaces.IUserRepository
        Private ReadOnly _appLogger As IAppLogger

        Private _overviewData As List(Of AdminAttendanceOverviewDto) = New List(Of AdminAttendanceOverviewDto)()

        ' Header Krypton Containers
        Private pnlHeader As KryptonPanel
        Private lblTitle As Label
        Private lblOpSummary As Label
        Private lblLiveClock As Label

        ' Reusable Metric KPI Cards
        Private pnlCardsHost As FlowLayoutPanel
        Private cardPresent As AppKpiCard
        Private cardWorking As AppKpiCard
        Private cardBreak As AppKpiCard
        Private cardLate As AppKpiCard
        Private cardNotPunched As AppKpiCard

        ' Reusable Krypton Filter Toolbar Controls
        Private pnlToolbar As KryptonPanel
        Private lblDateLabel As Label
        Private dtpAttendanceDate As KryptonDateTimePicker
        Private lblDeptLabel As Label
        Private cboDepartment As KryptonComboBox
        Private lblStatusLabel As Label
        Private cboStatusFilter As KryptonComboBox
        Private txtSearch As KryptonTextBox
        Private btnRefresh As KryptonButton
        Private btnExportCsv As KryptonButton

        ' Live Staff Board Krypton Grid Container
        Private pnlGridBox As KryptonPanel
        Private lblGridHeader As Label
        Private dgvStaffBoard As DataGridView

        Private ReadOnly tmrRefresh As Timer

        Public Sub New()
            Dim config As IAppConfiguration = New AppConfiguration()
            Dim connFactory As IDatabaseConnectionFactory = New DbConnectionFactory(config)
            Dim sqlHelper As ISqlHelper = New SqlHelper(connFactory)
            _appLogger = New AppLogger(config)
            Dim auditLogger As New AuditLogger(sqlHelper)
            Dim attendanceRepo As DAL.Interfaces.IAttendanceRepository = New AttendanceRepository(sqlHelper)
            _userRepo = New UserRepository(sqlHelper)

            _attendanceService = New AttendanceService(attendanceRepo, _userRepo, _appLogger, auditLogger)

            tmrRefresh = New Timer() With {.Interval = 10000} ' Refresh live board every 10 sec
            AddHandler tmrRefresh.Tick, Async Sub(s, e) Await LoadBoardDataAsync()

            InitializeComponent()
            tmrRefresh.Start()

            AddHandler Me.Load, Async Sub(s, e) Await LoadBoardDataAsync()
        End Sub

        Private Sub InitializeComponent()
            Me.SuspendLayout()
            Me.Dock = DockStyle.Fill
            Me.BackColor = Color.FromArgb(248, 250, 252) '#F8FAFC workspace
            Me.Padding = New Padding(20)
            Me.AutoScroll = False

            ' -----------------------------------------------------------------
            ' 0. Outer Main Content Card Surface (White, Rounded 14px, Soft Border #E2E8F0)
            ' -----------------------------------------------------------------
            Dim pnlMainCard As New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = Color.White,
                .Padding = New Padding(20, 16, 20, 20)
            }

            ' -----------------------------------------------------------------
            ' 1. Header Area with Operational Summary & Date Badge
            ' -----------------------------------------------------------------
            pnlHeader = New KryptonPanel() With {
                .Dock = DockStyle.Top,
                .Height = 56,
                .Padding = New Padding(0, 0, 0, 8),
                .Margin = New Padding(0, 0, 0, 12)
            }
            pnlHeader.StateCommon.Color1 = Color.White
            pnlHeader.StateCommon.Color2 = Color.White

            lblTitle = New Label() With {
                .Text = "Admin Staff Attendance Monitoring Board",
                .Font = New Font(ThemeConstants.FontNameDefault, 13.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(0, 2),
                .AutoSize = True
            }

            lblOpSummary = New Label() With {
                .Text = "⚠️ 3 staff members have not punched in today",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(220, 38, 38),
                .Location = New Point(0, 30),
                .AutoSize = True
            }

            lblLiveClock = New Label() With {
                .Text = $"📅 {DateTime.Now:dddd, dd MMM yyyy}",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.PrimaryAccent,
                .Dock = DockStyle.Right,
                .AutoSize = True,
                .TextAlign = ContentAlignment.MiddleRight
            }

            pnlHeader.Controls.Add(lblLiveClock)
            pnlHeader.Controls.Add(lblOpSummary)
            pnlHeader.Controls.Add(lblTitle)

            ' -----------------------------------------------------------------
            ' 2. Executive Reusable KPI Cards Host with FontAwesome Icons
            ' -----------------------------------------------------------------
            pnlCardsHost = New FlowLayoutPanel() With {
                .Dock = DockStyle.Top,
                .Height = 94,
                .BackColor = Color.White,
                .WrapContents = False,
                .Margin = New Padding(0, 6, 0, 16)
            }

            cardPresent = New AppKpiCard() With {
                .TitleText = "PRESENT TODAY",
                .ValueText = "0",
                .Count = 0,
                .Scheme = AppKpiCard.KpiScheme.PresentToday,
                .Icon = IconChar.Users,
                .FilterStatusTag = "Present",
                .Size = New Size(195, 88),
                .Margin = New Padding(0, 0, 12, 0)
            }

            cardWorking = New AppKpiCard() With {
                .TitleText = "CURRENTLY WORKING",
                .ValueText = "0",
                .Count = 0,
                .Scheme = AppKpiCard.KpiScheme.CurrentlyWorking,
                .Icon = IconChar.Bolt,
                .FilterStatusTag = "Working",
                .Size = New Size(195, 88),
                .Margin = New Padding(0, 0, 12, 0)
            }

            cardBreak = New AppKpiCard() With {
                .TitleText = "ON LUNCH BREAK",
                .ValueText = "0",
                .Count = 0,
                .Scheme = AppKpiCard.KpiScheme.OnLunchBreak,
                .Icon = IconChar.Utensils,
                .FilterStatusTag = "On Lunch Break",
                .Size = New Size(195, 88),
                .Margin = New Padding(0, 0, 12, 0)
            }

            cardLate = New AppKpiCard() With {
                .TitleText = "LATE ARRIVALS",
                .ValueText = "0",
                .Count = 0,
                .Scheme = AppKpiCard.KpiScheme.LateArrivals,
                .Icon = IconChar.Clock,
                .FilterStatusTag = "Late",
                .Size = New Size(195, 88),
                .Margin = New Padding(0, 0, 12, 0)
            }

            cardNotPunched = New AppKpiCard() With {
                .TitleText = "NOT PUNCHED IN",
                .ValueText = "0",
                .Count = 0,
                .Scheme = AppKpiCard.KpiScheme.NotPunchedIn,
                .Icon = IconChar.ExclamationTriangle,
                .FilterStatusTag = "Not Punched In",
                .Size = New Size(195, 88),
                .Margin = New Padding(0, 0, 0, 0)
            }

            AddHandler pnlCardsHost.SizeChanged, Sub(s, e)
                Dim totalW = pnlCardsHost.ClientSize.Width
                If totalW > 200 Then
                    Dim cardW = Math.Max(190, (totalW - 48 - 10) \ 5)
                    cardPresent.Width = cardW
                    cardWorking.Width = cardW
                    cardBreak.Width = cardW
                    cardLate.Width = cardW
                    cardNotPunched.Width = cardW
                End If
            End Sub

            AddHandler cardPresent.Click, AddressOf SummaryCard_Click
            AddHandler cardWorking.Click, AddressOf SummaryCard_Click
            AddHandler cardBreak.Click, AddressOf SummaryCard_Click
            AddHandler cardLate.Click, AddressOf SummaryCard_Click
            AddHandler cardNotPunched.Click, AddressOf SummaryCard_Click

            pnlCardsHost.Controls.Add(cardPresent)
            pnlCardsHost.Controls.Add(cardWorking)
            pnlCardsHost.Controls.Add(cardBreak)
            pnlCardsHost.Controls.Add(cardLate)
            pnlCardsHost.Controls.Add(cardNotPunched)

            ' -----------------------------------------------------------------
            ' 3. Elevated Light Rounded Filter Toolbar Surface (#FFFFFF, Soft Border #CBD5E1)
            ' -----------------------------------------------------------------
            pnlToolbar = New KryptonPanel() With {
                .Dock = DockStyle.Top,
                .Height = 56,
                .Padding = New Padding(12, 8, 12, 8),
                .Margin = New Padding(0, 8, 0, 16)
            }
            pnlToolbar.StateCommon.Color1 = Color.White
            pnlToolbar.StateCommon.Color2 = Color.White

            lblDateLabel = New Label() With {.Text = "Date:", .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold), .ForeColor = ThemeConstants.TextSecondary, .Location = New Point(12, 17), .AutoSize = True}
            dtpAttendanceDate = New KryptonDateTimePicker() With {
                .Format = DateTimePickerFormat.Short,
                .Value = DateTime.Today,
                .Location = New Point(52, 8),
                .Width = 125,
                .Height = 40
            }

            lblDeptLabel = New Label() With {.Text = "Dept:", .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold), .ForeColor = ThemeConstants.TextSecondary, .Location = New Point(190, 17), .AutoSize = True}
            cboDepartment = New KryptonComboBox() With {
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Location = New Point(232, 8),
                .Width = 145,
                .Height = 40
            }
            PopulateDepartmentCombo()

            lblStatusLabel = New Label() With {.Text = "Status:", .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold), .ForeColor = ThemeConstants.TextSecondary, .Location = New Point(390, 17), .AutoSize = True}
            cboStatusFilter = New KryptonComboBox() With {
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Location = New Point(442, 8),
                .Width = 140,
                .Height = 40
            }
            cboStatusFilter.Items.AddRange(New Object() {"All Statuses", "Present", "Working", "On Lunch Break", "Day Completed", "Not Punched In", "Late"})
            cboStatusFilter.SelectedIndex = 0

            txtSearch = New KryptonTextBox() With {
                .Location = New Point(595, 8),
                .Width = 175,
                .Height = 40
            }
            txtSearch.CueHint.CueHintText = "🔍 Search staff..."
            txtSearch.CueHint.Color1 = ThemeConstants.TextMuted

            btnRefresh = New KryptonButton() With {
                .Text = "Refresh",
                .Location = New Point(782, 8),
                .Size = New Size(115, 40)
            }
            btnRefresh.Values.Image = IconChar.SyncAlt.ToBitmap(Color.White, 16)

            btnExportCsv = New KryptonButton() With {
                .Text = "Export CSV",
                .Location = New Point(907, 8),
                .Size = New Size(125, 40)
            }
            btnExportCsv.Values.Image = IconChar.FileCsv.ToBitmap(Color.FromArgb(30, 41, 59), 16)

            AddHandler dtpAttendanceDate.ValueChanged, Async Sub(s, e) Await LoadBoardDataAsync()
            AddHandler cboDepartment.SelectedIndexChanged, Sub(s, e) ApplyBoardFilters()
            AddHandler txtSearch.TextChanged, Sub(s, e) ApplyBoardFilters()
            AddHandler cboStatusFilter.SelectedIndexChanged, Sub(s, e) ApplyBoardFilters()
            AddHandler btnRefresh.Click, Async Sub(s, e) Await ResetAndReloadBoardAsync()
            AddHandler btnExportCsv.Click, AddressOf btnExportCsv_Click

            ThemeConstants.ApplyKryptonInputStyle(dtpAttendanceDate)
            ThemeConstants.ApplyKryptonInputStyle(cboDepartment)
            ThemeConstants.ApplyKryptonInputStyle(cboStatusFilter)
            ThemeConstants.ApplyKryptonInputStyle(txtSearch)

            ThemeConstants.ApplyKryptonPrimaryButton(btnRefresh)
            ThemeConstants.ApplyKryptonSecondaryButton(btnExportCsv)

            pnlToolbar.Controls.Add(btnExportCsv)
            pnlToolbar.Controls.Add(btnRefresh)
            pnlToolbar.Controls.Add(txtSearch)
            pnlToolbar.Controls.Add(cboStatusFilter)
            pnlToolbar.Controls.Add(lblStatusLabel)
            pnlToolbar.Controls.Add(cboDepartment)
            pnlToolbar.Controls.Add(lblDeptLabel)
            pnlToolbar.Controls.Add(dtpAttendanceDate)
            pnlToolbar.Controls.Add(lblDateLabel)

            ' -----------------------------------------------------------------
            ' 4. Live Staff Attendance Board Krypton Grid Container
            ' -----------------------------------------------------------------
            pnlGridBox = New KryptonPanel() With {
                .Dock = DockStyle.Fill,
                .Margin = New Padding(0, 8, 0, 0)
            }
            pnlGridBox.StateCommon.Color1 = Color.White
            pnlGridBox.StateCommon.Color2 = Color.White

            lblGridHeader = New Label() With {
                .Text = "Staff Live Attendance Working Status Board",
                .Font = New Font(ThemeConstants.FontNameDefault, 11.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Dock = DockStyle.Top,
                .Height = 36,
                .Padding = New Padding(0, 8, 0, 0)
            }

            dgvStaffBoard = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                .BackgroundColor = Color.White,
                .BorderStyle = BorderStyle.None,
                .ReadOnly = True,
                .AllowUserToAddRows = False,
                .RowHeadersVisible = False,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect
            }
            dgvStaffBoard.RowTemplate.Height = 44
            ThemeConstants.ApplyModernGridStyle(dgvStaffBoard)

            ' Single-line non-wrapping column headers with Deep Indigo Background
            dgvStaffBoard.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False
            dgvStaffBoard.ColumnHeadersDefaultCellStyle.BackColor = ThemeConstants.PrimaryAccent '#4F46E5
            dgvStaffBoard.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
            dgvStaffBoard.ColumnHeadersDefaultCellStyle.Font = New Font(ThemeConstants.FontNameDefault, 9.5!, FontStyle.Bold)
            dgvStaffBoard.ColumnHeadersHeight = 40

            AddHandler dgvStaffBoard.CellPainting, AddressOf dgvStaffBoard_CellPainting
            AddHandler dgvStaffBoard.CellContentClick, AddressOf dgvStaffBoard_CellContentClick

            pnlGridBox.Controls.Add(dgvStaffBoard)
            pnlGridBox.Controls.Add(lblGridHeader)

            pnlMainCard.Controls.Add(pnlGridBox)
            pnlMainCard.Controls.Add(pnlToolbar)
            pnlMainCard.Controls.Add(pnlCardsHost)
            pnlMainCard.Controls.Add(pnlHeader)

            Me.Controls.Add(pnlMainCard)

            Me.ResumeLayout(False)
        End Sub

        Private Sub SummaryCard_Click(sender As Object, e As EventArgs)
            Dim card = TryCast(sender, AppKpiCard)
            If card Is Nothing OrElse String.IsNullOrEmpty(card.FilterStatusTag) Then Return

            Dim targetStatus = card.FilterStatusTag
            Dim currentStatus = If(cboStatusFilter.SelectedItem IsNot Nothing, cboStatusFilter.SelectedItem.ToString(), "All Statuses")

            If String.Equals(currentStatus, targetStatus, StringComparison.OrdinalIgnoreCase) Then
                cboStatusFilter.SelectedIndex = 0
            Else
                For i As Integer = 0 To cboStatusFilter.Items.Count - 1
                    If cboStatusFilter.Items(i).ToString().Equals(targetStatus, StringComparison.OrdinalIgnoreCase) Then
                        cboStatusFilter.SelectedIndex = i
                        Exit For
                    End If
                Next
            End If
        End Sub

        Private Sub PopulateDepartmentCombo()
            cboDepartment.Items.Clear()
            cboDepartment.Items.Add("-- All Depts --")
            For Each dept In [Enum].GetNames(GetType(DepartmentType))
                cboDepartment.Items.Add(dept)
            Next
            cboDepartment.SelectedIndex = 0
        End Sub

        Private Async Function ResetAndReloadBoardAsync() As Task
            cboStatusFilter.SelectedIndex = 0
            txtSearch.Text = ""
            cboDepartment.SelectedIndex = 0
            Await LoadBoardDataAsync()
        End Function

        Private Async Function LoadBoardDataAsync() As Task
            Try
                Me.Cursor = Cursors.WaitCursor
                Dim selDate = dtpAttendanceDate.Value.Date

                _overviewData = Await _attendanceService.GetTodayStaffOverviewAsync(selDate)
                ApplyBoardFilters()
            Catch ex As Exception
                _appLogger.LogError($"Error loading admin attendance board: {ex.Message}", "AdminAttendanceBoardControl", ex)
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Function

        Private Sub ApplyBoardFilters()
            If _overviewData Is Nothing Then Return

            Dim selectedDept = If(cboDepartment.SelectedItem IsNot Nothing, cboDepartment.SelectedItem.ToString(), "-- All Depts --")
            Dim search = txtSearch.Text.Trim().ToLower()
            Dim selectedStatus = If(cboStatusFilter.SelectedItem IsNot Nothing, cboStatusFilter.SelectedItem.ToString(), "All Statuses")

            Dim filtered = _overviewData.AsEnumerable()

            If selectedDept <> "-- All Depts --" AndAlso Not String.IsNullOrEmpty(selectedDept) Then
                filtered = filtered.Where(Function(x) String.Equals(x.DepartmentName, selectedDept, StringComparison.OrdinalIgnoreCase))
            End If

            If Not String.IsNullOrEmpty(search) Then
                filtered = filtered.Where(Function(x) (x.StaffName IsNot Nothing AndAlso x.StaffName.ToLower().Contains(search)) OrElse (x.EmployeeCode IsNot Nothing AndAlso x.EmployeeCode.ToLower().Contains(search)))
            End If

            If selectedStatus <> "All Statuses" AndAlso Not String.IsNullOrEmpty(selectedStatus) Then
                If selectedStatus = "Present" Then
                    filtered = filtered.Where(Function(x) x.ClockInTime.HasValue)
                ElseIf selectedStatus = "Late" Then
                    filtered = filtered.Where(Function(x) x.Status.Contains("Late"))
                Else
                    filtered = filtered.Where(Function(x) String.Equals(x.LiveStateDisplay, selectedStatus, StringComparison.OrdinalIgnoreCase))
                End If
            End If

            Dim list = filtered.ToList()

            ' 1. Calculate Summary Cards & Exception Emphasis
            Dim countPresent = list.Where(Function(x) x.ClockInTime.HasValue).Count()
            Dim countWorking = list.Where(Function(x) x.LiveStateDisplay = "Working").Count()
            Dim countBreak = list.Where(Function(x) x.LiveStateDisplay = "On Lunch Break").Count()
            Dim countLate = list.Where(Function(x) x.Status.Contains("Late")).Count()
            Dim countNotPunched = list.Where(Function(x) Not x.ClockInTime.HasValue).Count()

            cardPresent.Count = countPresent
            cardPresent.ValueText = countPresent.ToString()
            cardPresent.IsSelected = String.Equals(selectedStatus, "Present", StringComparison.OrdinalIgnoreCase)

            cardWorking.Count = countWorking
            cardWorking.ValueText = countWorking.ToString()
            cardWorking.IsSelected = String.Equals(selectedStatus, "Working", StringComparison.OrdinalIgnoreCase)

            cardBreak.Count = countBreak
            cardBreak.ValueText = countBreak.ToString()
            cardBreak.IsSelected = String.Equals(selectedStatus, "On Lunch Break", StringComparison.OrdinalIgnoreCase)

            cardLate.Count = countLate
            cardLate.ValueText = countLate.ToString()
            cardLate.IsSelected = String.Equals(selectedStatus, "Late", StringComparison.OrdinalIgnoreCase)

            cardNotPunched.Count = countNotPunched
            cardNotPunched.ValueText = countNotPunched.ToString()
            cardNotPunched.IsSelected = String.Equals(selectedStatus, "Not Punched In", StringComparison.OrdinalIgnoreCase)

            ' Dynamic Operational Summary Subtitle
            Dim attentionCount = countLate + countNotPunched
            If attentionCount > 0 Then
                If countLate > 0 AndAlso countNotPunched > 0 Then
                    lblOpSummary.Text = $"{attentionCount} staff require attention today ({countNotPunched} not punched in, {countLate} late)"
                ElseIf countLate > 0 Then
                    lblOpSummary.Text = If(countLate = 1, "1 late arrival requires review", $"{countLate} late arrivals require review")
                Else
                    lblOpSummary.Text = If(countNotPunched = 1, "1 staff member has not punched in today", $"{countNotPunched} staff members have not punched in today")
                End If
                lblOpSummary.ForeColor = Color.FromArgb(220, 38, 38)
            Else
                lblOpSummary.Text = "All staff attendance is on track today"
                lblOpSummary.ForeColor = Color.FromArgb(16, 185, 129)
            End If

            ' 2. Bind DataGridView
            Dim dt As New DataTable()
            dt.Columns.Add("UserId", GetType(Integer))
            dt.Columns.Add("AttendanceId", GetType(Object))
            dt.Columns.Add("Emp Code")
            dt.Columns.Add("Staff Name")
            dt.Columns.Add("Department")
            dt.Columns.Add("Punch In")
            dt.Columns.Add("Punch Out")
            dt.Columns.Add("Break")
            dt.Columns.Add("Working Hrs")
            dt.Columns.Add("Status")
            dt.Columns.Add("Corrected?")

            For Each item In list
                Dim attIdObj = If(item.AttendanceId.HasValue, CType(item.AttendanceId.Value, Object), DBNull.Value)
                Dim pIn = If(item.ClockInTime.HasValue, item.ClockInTime.Value.ToString("hh:mm tt"), "--")
                Dim pOut = If(item.ClockOutTime.HasValue, item.ClockOutTime.Value.ToString("hh:mm tt"), "--")
                Dim brkStr = If(item.TotalBreakMinutes > 0, $"{item.TotalBreakMinutes} m", "--")
                Dim hrsStr = If(item.TotalWorkingMinutes > 0, FormatHoursShort(item.TotalWorkingMinutes), "--")
                Dim corrStr = If(item.IsManuallyCorrected, "Corrected", "—")

                dt.Rows.Add(item.UserId, attIdObj, item.EmployeeCode, item.StaffName, item.DepartmentName, pIn, pOut, brkStr, hrsStr, item.LiveStateDisplay, corrStr)
            Next

            dgvStaffBoard.DataSource = dt

            ' Configure Action Button Columns
            If Not dgvStaffBoard.Columns.Contains("btnInspect") Then
                Dim btnInspectCol As New DataGridViewButtonColumn() With {
                    .Name = "btnInspect",
                    .HeaderText = "Inspect",
                    .Text = "Inspect",
                    .UseColumnTextForButtonValue = True,
                    .Width = 85
                }
                dgvStaffBoard.Columns.Add(btnInspectCol)
            End If

            If Not dgvStaffBoard.Columns.Contains("btnCorrect") Then
                Dim btnCorrectCol As New DataGridViewButtonColumn() With {
                    .Name = "btnCorrect",
                    .HeaderText = "Action",
                    .Text = "Correct",
                    .UseColumnTextForButtonValue = True,
                    .Width = 85
                }
                dgvStaffBoard.Columns.Add(btnCorrectCol)
            End If

            ' Hide ID columns from view
            If dgvStaffBoard.Columns.Contains("UserId") Then dgvStaffBoard.Columns("UserId").Visible = False
            If dgvStaffBoard.Columns.Contains("AttendanceId") Then dgvStaffBoard.Columns("AttendanceId").Visible = False

            ' Configure Priority-Balanced Hybrid Column Sizing Model
            If dgvStaffBoard.Columns.Contains("Emp Code") Then
                Dim col = dgvStaffBoard.Columns("Emp Code")
                col.HeaderText = "Emp Code"
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                col.Width = 75
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            End If
            If dgvStaffBoard.Columns.Contains("Staff Name") Then
                Dim col = dgvStaffBoard.Columns("Staff Name")
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                col.MinimumWidth = 140
                col.FillWeight = 190
            End If
            If dgvStaffBoard.Columns.Contains("Department") Then
                Dim col = dgvStaffBoard.Columns("Department")
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                col.MinimumWidth = 110
                col.FillWeight = 130
            End If
            If dgvStaffBoard.Columns.Contains("Punch In") Then
                Dim col = dgvStaffBoard.Columns("Punch In")
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                col.Width = 72
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            End If
            If dgvStaffBoard.Columns.Contains("Punch Out") Then
                Dim col = dgvStaffBoard.Columns("Punch Out")
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                col.Width = 78
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            End If
            If dgvStaffBoard.Columns.Contains("Break") Then
                Dim col = dgvStaffBoard.Columns("Break")
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                col.Width = 52
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            End If
            If dgvStaffBoard.Columns.Contains("Working Hrs") Then
                Dim col = dgvStaffBoard.Columns("Working Hrs")
                col.HeaderText = "Working Hrs"
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                col.Width = 82
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            End If
            If dgvStaffBoard.Columns.Contains("Status") Then
                Dim col = dgvStaffBoard.Columns("Status")
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                col.Width = 115
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            End If
            If dgvStaffBoard.Columns.Contains("Corrected?") Then
                Dim col = dgvStaffBoard.Columns("Corrected?")
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                col.Width = 80
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            End If
            If dgvStaffBoard.Columns.Contains("btnInspect") Then
                Dim col = dgvStaffBoard.Columns("btnInspect")
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                col.Width = 76
                col.Resizable = DataGridViewTriState.False
            End If
            If dgvStaffBoard.Columns.Contains("btnCorrect") Then
                Dim col = dgvStaffBoard.Columns("btnCorrect")
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                col.Width = 76
                col.Resizable = DataGridViewTriState.False
            End If
        End Sub

        Private Sub dgvStaffBoard_CellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs)
            If e.ColumnIndex < 0 Then Return

            ' Enforce 100% Uniform Solid Indigo Header Background across ALL Columns (#4F46E5)
            If e.RowIndex = -1 Then
                Using bgBrush As New SolidBrush(ThemeConstants.PrimaryAccent),
                      textBrush As New SolidBrush(Color.White),
                      sf As New StringFormat() With {
                          .Alignment = If(dgvStaffBoard.Columns(e.ColumnIndex).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter, StringAlignment.Center, StringAlignment.Near),
                          .LineAlignment = StringAlignment.Center,
                          .FormatFlags = StringFormatFlags.NoWrap
                      }

                    e.Graphics.FillRectangle(bgBrush, e.CellBounds)

                    ' Subtle bottom border for grid header separator
                    Using linePen As New Pen(Color.FromArgb(67, 56, 202))
                        e.Graphics.DrawLine(linePen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1)
                    End Using

                    Dim headerText = dgvStaffBoard.Columns(e.ColumnIndex).HeaderText
                    Dim headerFont = dgvStaffBoard.ColumnHeadersDefaultCellStyle.Font
                    If headerFont Is Nothing Then headerFont = New Font(ThemeConstants.FontNameDefault, 9.5!, FontStyle.Bold)

                    Dim textRect = New Rectangle(e.CellBounds.X + 4, e.CellBounds.Y, e.CellBounds.Width - 8, e.CellBounds.Height)
                    e.Graphics.DrawString(headerText, headerFont, textBrush, textRect, sf)
                End Using

                e.Handled = True
                Return
            End If

            Dim colName = dgvStaffBoard.Columns(e.ColumnIndex).Name

            ' Custom styling for Emp Code and Department columns matching reference screenshot
            If colName = "Emp Code" AndAlso e.Value IsNot Nothing Then
                e.CellStyle.ForeColor = ThemeConstants.PrimaryAccent '#4F46E5 Indigo
                e.CellStyle.Font = New Font(dgvStaffBoard.Font.FontFamily, 9.0!, FontStyle.Bold)
            ElseIf colName = "Department" AndAlso e.Value IsNot Nothing Then
                e.CellStyle.ForeColor = ThemeConstants.PrimaryAccent '#4F46E5 Indigo
                e.CellStyle.Font = New Font(dgvStaffBoard.Font.FontFamily, 9.0!, FontStyle.Bold)
            End If

            ' Custom status badge rendering matching reference screenshot
            If colName = "Status" AndAlso e.Value IsNot Nothing Then
                e.Paint(e.CellBounds, DataGridViewPaintParts.Background Or DataGridViewPaintParts.Border)

                Dim statusText = e.Value.ToString()
                Dim bgCol As Color
                Dim textCol As Color
                Dim borderCol As Color
                Dim displayBadgeText As String = statusText

                Select Case statusText
                    Case "Working"
                        bgCol = Color.FromArgb(236, 253, 245)     ' Emerald-50
                        textCol = Color.FromArgb(4, 120, 87)       ' Emerald-700
                        borderCol = Color.FromArgb(167, 243, 208)   ' Emerald-200
                        displayBadgeText = "Working"
                    Case "On Lunch Break"
                        bgCol = Color.FromArgb(255, 251, 235)     ' Amber-50
                        textCol = Color.FromArgb(180, 83, 9)       ' Amber-700
                        borderCol = Color.FromArgb(253, 230, 138)   ' Amber-200
                        displayBadgeText = "On Break"
                    Case "Day Completed", "Completed"
                        bgCol = Color.FromArgb(241, 245, 249)     ' Slate-100
                        textCol = Color.FromArgb(51, 65, 85)       ' Slate-700
                        borderCol = Color.FromArgb(203, 213, 225)   ' Slate-300
                        displayBadgeText = "Completed"
                    Case "Not Punched In", "Not Punched"
                        bgCol = Color.FromArgb(254, 242, 242)     ' Red-50
                        textCol = Color.FromArgb(220, 38, 38)      ' Red-600
                        borderCol = Color.FromArgb(254, 202, 202)   ' Red-200
                        displayBadgeText = "Not Punched"
                    Case Else
                        If statusText.Contains("Late") Then
                            bgCol = Color.FromArgb(255, 247, 237) ' Orange-50
                            textCol = Color.FromArgb(194, 65, 12)  ' Orange-700
                            borderCol = Color.FromArgb(253, 186, 116)
                            displayBadgeText = "Late"
                        Else
                            bgCol = Color.FromArgb(241, 245, 249)
                            textCol = Color.FromArgb(51, 65, 85)
                            borderCol = Color.FromArgb(203, 213, 225)
                            displayBadgeText = statusText
                        End If
                End Select

                Dim badgeWidth = Math.Min(e.CellBounds.Width - 12, 115)
                Dim badgeHeight = 24
                Dim badgeRect As New Rectangle(
                    e.CellBounds.X + (e.CellBounds.Width - badgeWidth) \ 2,
                    e.CellBounds.Y + (e.CellBounds.Height - badgeHeight) \ 2,
                    badgeWidth,
                    badgeHeight
                )

                Using bgBrush As New SolidBrush(bgCol),
                      borderPen As New Pen(borderCol),
                      textBrush As New SolidBrush(textCol),
                      sf As New StringFormat() With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}

                    Using path = GetRoundedRectPath(badgeRect, 6)
                        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias
                        e.Graphics.FillPath(bgBrush, path)
                        e.Graphics.DrawPath(borderPen, path)
                    End Using

                    Using badgeFont As New Font(ThemeConstants.FontNameDefault, 8.25!, FontStyle.Bold)
                        e.Graphics.DrawString(displayBadgeText, badgeFont, textBrush, badgeRect, sf)
                    End Using
                End Using

                e.Handled = True

            ElseIf colName = "Corrected?" AndAlso e.Value IsNot Nothing Then
                e.Paint(e.CellBounds, DataGridViewPaintParts.Background Or DataGridViewPaintParts.Border)

                Dim valStr = e.Value.ToString()
                If valStr.Contains("Corrected") Then
                    Dim badgeWidth = 82
                    Dim badgeHeight = 22
                    Dim badgeRect As New Rectangle(
                        e.CellBounds.X + (e.CellBounds.Width - badgeWidth) \ 2,
                        e.CellBounds.Y + (e.CellBounds.Height - badgeHeight) \ 2,
                        badgeWidth,
                        badgeHeight
                    )

                    Using bgBrush As New SolidBrush(Color.FromArgb(224, 242, 254)),
                          borderPen As New Pen(Color.FromArgb(125, 211, 252)),
                          textBrush As New SolidBrush(Color.FromArgb(7, 89, 133)),
                          sf As New StringFormat() With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}

                        Using path = GetRoundedRectPath(badgeRect, 5)
                            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias
                            e.Graphics.FillPath(bgBrush, path)
                            e.Graphics.DrawPath(borderPen, path)
                        End Using

                        Using badgeFont As New Font(ThemeConstants.FontNameDefault, 8.0!, FontStyle.Bold)
                            e.Graphics.DrawString("Corrected", badgeFont, textBrush, badgeRect, sf)
                        End Using
                    End Using
                Else
                    Using textBrush As New SolidBrush(Color.FromArgb(148, 163, 184)),
                          sf As New StringFormat() With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
                        e.Graphics.DrawString("—", dgvStaffBoard.Font, textBrush, e.CellBounds, sf)
                    End Using
                End If

                e.Handled = True

            ElseIf colName = "btnInspect" Then
                e.Paint(e.CellBounds, DataGridViewPaintParts.Background Or DataGridViewPaintParts.Border)

                ' Primary Action (Solid Indigo Button with FontAwesome Vector Search Icon)
                Dim btnRect As New Rectangle(e.CellBounds.X + 4, e.CellBounds.Y + 6, e.CellBounds.Width - 8, e.CellBounds.Height - 12)
                Using bgBrush As New SolidBrush(ThemeConstants.PrimaryAccent),
                      textBrush As New SolidBrush(Color.White)

                    Using path = GetRoundedRectPath(btnRect, 6)
                        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias
                        e.Graphics.FillPath(bgBrush, path)
                    End Using

                    Using iconBmp As Bitmap = IconChar.Search.ToBitmap(Color.White, 12)
                        If iconBmp IsNot Nothing Then
                            Using btnFont As New Font(ThemeConstants.FontNameDefault, 8.25!, FontStyle.Bold)
                                Dim textStr = "Inspect"
                                Dim textSize = e.Graphics.MeasureString(textStr, btnFont)
                                Dim totalW As Single = iconBmp.Width + 3 + textSize.Width
                                Dim startX As Single = btnRect.X + (btnRect.Width - totalW) / 2.0!
                                Dim iconY As Single = btnRect.Y + (btnRect.Height - iconBmp.Height) / 2.0!
                                Dim textY As Single = btnRect.Y + (btnRect.Height - textSize.Height) / 2.0!

                                e.Graphics.DrawImage(iconBmp, New PointF(startX, iconY))
                                e.Graphics.DrawString(textStr, btnFont, textBrush, New PointF(startX + iconBmp.Width + 3, textY))
                            End Using
                        End If
                    End Using
                End Using

                e.Handled = True

            ElseIf colName = "btnCorrect" Then
                e.Paint(e.CellBounds, DataGridViewPaintParts.Background Or DataGridViewPaintParts.Border)

                ' Secondary Action (Outline Slate Button)
                Dim btnRect As New Rectangle(e.CellBounds.X + 4, e.CellBounds.Y + 6, e.CellBounds.Width - 8, e.CellBounds.Height - 12)
                Using bgBrush As New SolidBrush(Color.White),
                      borderPen As New Pen(Color.FromArgb(203, 213, 225)),
                      textBrush As New SolidBrush(Color.FromArgb(30, 41, 59)),
                      sf As New StringFormat() With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}

                    Using path = GetRoundedRectPath(btnRect, 6)
                        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias
                        e.Graphics.FillPath(bgBrush, path)
                        e.Graphics.DrawPath(borderPen, path)
                    End Using

                    Using btnFont As New Font(ThemeConstants.FontNameDefault, 8.25!, FontStyle.Bold)
                        e.Graphics.DrawString("Correct", btnFont, textBrush, btnRect, sf)
                    End Using
                End Using

                e.Handled = True
            End If
        End Sub

        Private Function GetRoundedRectPath(rect As Rectangle, radius As Integer) As System.Drawing.Drawing2D.GraphicsPath
            Dim path As New System.Drawing.Drawing2D.GraphicsPath()
            Dim diameter = radius * 2
            Dim arc As New Rectangle(rect.X, rect.Y, diameter, diameter)

            path.AddArc(arc, 180, 90)

            arc.X = rect.Right - diameter
            path.AddArc(arc, 270, 90)

            arc.Y = rect.Bottom - diameter
            path.AddArc(arc, 0, 90)

            arc.X = rect.X
            path.AddArc(arc, 90, 90)

            path.CloseFigure()
            Return path
        End Function

        Private Async Sub dgvStaffBoard_CellContentClick(sender As Object, e As DataGridViewCellEventArgs)
            If e.RowIndex < 0 Then Return

            Dim colName = dgvStaffBoard.Columns(e.ColumnIndex).Name
            Dim userId = Convert.ToInt32(dgvStaffBoard.Rows(e.RowIndex).Cells("UserId").Value)
            Dim staffName = dgvStaffBoard.Rows(e.RowIndex).Cells("Staff Name").Value.ToString()

            If colName = "btnInspect" Then
                Using dlg As New FrmStaffAttendanceDetail(userId, staffName, _attendanceService, _userRepo)
                    dlg.ShowDialog(Me.FindForm())
                End Using
            ElseIf colName = "btnCorrect" Then
                Dim attIdObj = dgvStaffBoard.Rows(e.RowIndex).Cells("AttendanceId").Value
                Dim attId = If(attIdObj IsNot DBNull.Value AndAlso attIdObj IsNot Nothing, Convert.ToInt32(attIdObj), 0)

                Dim pInStr = dgvStaffBoard.Rows(e.RowIndex).Cells("Punch In").Value.ToString()
                Dim pOutStr = dgvStaffBoard.Rows(e.RowIndex).Cells("Punch Out").Value.ToString()
                Dim currStatus = dgvStaffBoard.Rows(e.RowIndex).Cells("Status").Value.ToString()

                Using dlg As New FrmManualAttendanceCorrection(attId, userId, staffName, dtpAttendanceDate.Value.Date, pInStr, pOutStr, currStatus, _attendanceService)
                    If dlg.ShowDialog(Me.FindForm()) = DialogResult.OK Then
                        Await LoadBoardDataAsync()
                    End If
                End Using
            End If
        End Sub

        Private Sub btnExportCsv_Click(sender As Object, e As EventArgs)
            Try
                Dim selDate = dtpAttendanceDate.Value.Date
                Using sfd As New SaveFileDialog()
                    sfd.Filter = "CSV Files (*.csv)|*.csv"
                    sfd.FileName = $"Admin_Staff_Attendance_{selDate:yyyy-MM-dd}.csv"
                    sfd.Title = "Export Staff Live Attendance Board"

                    If sfd.ShowDialog() = DialogResult.OK Then
                        Dim sb As New StringBuilder()
                        sb.AppendLine("STAFF AUTOMATION SYSTEM - ADMIN ATTENDANCE MONITORING REPORT")
                        sb.AppendLine($"Date: {selDate:yyyy-MM-dd}")
                        sb.AppendLine($"Exported On: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                        sb.AppendLine()
                        sb.AppendLine("Emp Code,Staff Name,Department,Punch In,Punch Out,Break Mins,Working Hours,Status,Manually Corrected")

                        If dgvStaffBoard.DataSource IsNot Nothing AndAlso TypeOf dgvStaffBoard.DataSource Is DataTable Then
                            Dim dt = CType(dgvStaffBoard.DataSource, DataTable)
                            For Each row As DataRow In dt.Rows
                                sb.AppendLine(String.Format("""{0}"",""{1}"",""{2}"",""{3}"",""{4}"",""{5}"",""{6}"",""{7}"",""{8}""",
                                    row("Emp Code"), row("Staff Name"), row("Department"), row("Punch In"), row("Punch Out"), row("Break"), row("Working Hrs"), row("Status"), row("Corrected?")))
                            Next
                        End If

                        File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8)

                        Dim parentForm = TryCast(Me.FindForm(), Form)
                        Forms.Common.FrmInAppAlert.ShowModal(parentForm, "Export Complete", $"Live Staff Attendance Report successfully exported to:{vbCrLf}{sfd.FileName}", Forms.Common.AlertType.SuccessAlert, actionText:="OK")
                    End If
                End Using
            Catch ex As Exception
                Dim parentForm = TryCast(Me.FindForm(), Form)
                Forms.Common.FrmInAppAlert.ShowModal(parentForm, "Export Error", "Export failed: " & ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
            End Try
        End Sub

        Private Shared Function FormatHoursShort(totalMins As Integer) As String
            If totalMins <= 0 Then Return "0h 0m"
            Dim hrs = totalMins \ 60
            Dim mins = totalMins Mod 60
            If hrs = 0 Then Return $"{mins}m"
            If mins = 0 Then Return $"{hrs}h"
            Return $"{hrs}h {mins}m"
        End Function

        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                If tmrRefresh IsNot Nothing Then
                    tmrRefresh.Stop()
                    tmrRefresh.Dispose()
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub
    End Class
End Namespace
