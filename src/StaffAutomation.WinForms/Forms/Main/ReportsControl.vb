Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Diagnostics
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Printing
Imports System.IO
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports StaffAutomation.BLL.Logging
Imports StaffAutomation.BLL.Services
Imports StaffAutomation.Core.Configuration
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Logging
Imports StaffAutomation.DAL.Configuration
Imports StaffAutomation.DAL.Core
Imports StaffAutomation.DAL.Repositories
Imports StaffAutomation.Reports.Services

Namespace Forms.Main
    ''' <summary>
    ''' Modernized Executive Reports and Analytics Hub UserControl.
    ''' Conforms to the Centralized Krypton Design System: Hero Header, Clean Filter Toolbar,
    ''' Standardized Action Buttons, Modern DataGrid Styling, and Zero-Record Psychological Empty State.
    ''' Preserves 100% of underlying SQL query engine, PDF/Excel generation, and Print Preview logic.
    ''' </summary>
    Public Class ReportsControl
        Inherits UserControl

        Private ReadOnly _clientService As IClientService
        Private ReadOnly _taskService As ITaskManagementService
        Private ReadOnly _attendanceService As BLL.Interfaces.IAttendanceService
        Private ReadOnly _reportGenerator As ReportGeneratorService

        ' Header & Layout Containers
        Private pnlHeroHeader As Panel
        Private lblHeroTitle As Label
        Private lblHeroSubtitle As Label

        Private pnlFilterBar As Panel
        Private pnlFilterFields As Panel
        Private pnlActionButtons As Panel

        Private lblReportType As Label
        Private cboReportType As Krypton.Toolkit.KryptonComboBox
        Private lblStartDate As Label
        Private dtpStartDate As Krypton.Toolkit.KryptonDateTimePicker
        Private lblEndDate As Label
        Private dtpEndDate As Krypton.Toolkit.KryptonDateTimePicker
        Private lblClientFilter As Label
        Private cboClientFilter As Krypton.Toolkit.KryptonComboBox

        Private btnGenerate As Forms.Common.ModernButton
        Private btnExportExcel As Forms.Common.ModernButton
        Private btnExportPdf As Forms.Common.ModernButton
        Private btnPrintPreview As Forms.Common.ModernButton

        Private pnlGridCard As Panel
        Private pnlGridHeader As Panel
        Private lblRecordCount As Label
        Private dgvReportData As DataGridView

        ' Psychological Zero-Record Empty State Card
        Private pnlEmptyState As Panel
        Private pbEmptyIcon As PictureBox
        Private lblEmptyTitle As Label
        Private lblEmptySubtitle As Label
        Private btnEmptyGenerate As Forms.Common.ModernButton

        Public Sub New()
            Dim config As IAppConfiguration = New AppConfiguration()
            Dim connFactory As IDatabaseConnectionFactory = New DbConnectionFactory(config)
            Dim sqlHelper As ISqlHelper = New SqlHelper(connFactory)
            Dim appLogger As IAppLogger = New AppLogger(config)
            Dim auditLogger As New BLL.Logging.AuditLogger(sqlHelper)
            Dim clientRepo As DAL.Interfaces.IClientRepository = New ClientRepository(sqlHelper)
            Dim taskRepo As DAL.Interfaces.ITaskRepository = New TaskRepository(sqlHelper)
            Dim userRepo As DAL.Interfaces.IUserRepository = New UserRepository(sqlHelper)
            Dim attendanceRepo As DAL.Interfaces.IAttendanceRepository = New AttendanceRepository(sqlHelper)
            Dim workflowEngine As ITaskWorkflowEngine = New TaskWorkflowEngine()

            _clientService = New ClientService(clientRepo, appLogger, auditLogger)
            _taskService = New TaskManagementService(taskRepo, workflowEngine, appLogger, auditLogger, clientRepo, userRepo)
            _attendanceService = New AttendanceService(attendanceRepo, userRepo, appLogger, auditLogger)
            _reportGenerator = New ReportGeneratorService()

            InitializeComponent()
            PopulateDropdownsAsync()
        End Sub

        Private Sub InitializeComponent()
            Me.SuspendLayout()
            Me.Dock = DockStyle.Fill
            Me.BackColor = ThemeConstants.WorkspaceBackground
            Me.Padding = New Padding(16)
            Me.AutoScroll = False

            ' 1. Hero Page Header
            pnlHeroHeader = New Panel()
            lblHeroTitle = New Label() With {.Text = "📈 Executive Reports & Analytics Hub"}
            lblHeroSubtitle = New Label() With {.Text = "Generate statutory tax summaries, task progress reports, and client billing exports with SQL Server data query engine."}
            ThemeConstants.ApplyHeaderStyle(pnlHeroHeader, lblHeroTitle, lblHeroSubtitle)

            pnlHeroHeader.Controls.Add(lblHeroSubtitle)
            pnlHeroHeader.Controls.Add(lblHeroTitle)

            ' 2. Filter & Selection Toolbar Container
            pnlFilterBar = New Panel()
            ThemeConstants.ApplyToolbarStyle(pnlFilterBar)
            pnlFilterBar.Height = 100

            ' Filter Controls Panel (Left-aligned)
            pnlFilterFields = New Panel() With {
                .Dock = DockStyle.Left,
                .Width = 620,
                .BackColor = Color.Transparent
            }

            ' Row 1: Report Type, From Date, To Date
            lblReportType = New Label() With {
                .Text = "Report Type:",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(10, 14),
                .AutoSize = True
            }

            cboReportType = New Krypton.Toolkit.KryptonComboBox() With {
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Location = New Point(95, 10),
                .Size = New Size(240, 24),
                .TabIndex = 0
            }
            ThemeConstants.ApplyAppComboBoxStyle(cboReportType)
            cboReportType.Items.Add("Task Completion & Status Summary")
            cboReportType.Items.Add("Client Work History & Compliance")
            cboReportType.Items.Add("Staff Attendance & Hours Report")
            cboReportType.SelectedIndex = 0

            lblStartDate = New Label() With {
                .Text = "From:",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(348, 14),
                .AutoSize = True
            }

            dtpStartDate = New Krypton.Toolkit.KryptonDateTimePicker() With {
                .Format = DateTimePickerFormat.Short,
                .Value = DateTime.Today.AddDays(-30),
                .Location = New Point(390, 10),
                .Size = New Size(105, 24),
                .TabIndex = 1
            }
            ThemeConstants.ApplyAppDatePickerStyle(dtpStartDate)

            lblEndDate = New Label() With {
                .Text = "To:",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(505, 14),
                .AutoSize = True
            }

            dtpEndDate = New Krypton.Toolkit.KryptonDateTimePicker() With {
                .Format = DateTimePickerFormat.Short,
                .Value = DateTime.Today,
                .Location = New Point(530, 10),
                .Size = New Size(105, 24),
                .TabIndex = 2
            }
            ThemeConstants.ApplyAppDatePickerStyle(dtpEndDate)

            ' Row 2: Client Filter
            lblClientFilter = New Label() With {
                .Text = "Client Filter:",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(10, 54),
                .AutoSize = True
            }

            cboClientFilter = New Krypton.Toolkit.KryptonComboBox() With {
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Location = New Point(95, 50),
                .Size = New Size(240, 24),
                .TabIndex = 3
            }
            ThemeConstants.ApplyAppComboBoxStyle(cboClientFilter)

            pnlFilterFields.Controls.Add(cboClientFilter)
            pnlFilterFields.Controls.Add(lblClientFilter)
            pnlFilterFields.Controls.Add(dtpEndDate)
            pnlFilterFields.Controls.Add(lblEndDate)
            pnlFilterFields.Controls.Add(dtpStartDate)
            pnlFilterFields.Controls.Add(lblStartDate)
            pnlFilterFields.Controls.Add(cboReportType)
            pnlFilterFields.Controls.Add(lblReportType)

            ' Standardized Action Buttons Panel (Right-aligned)
            pnlActionButtons = New Panel() With {
                .Dock = DockStyle.Right,
                .Width = 500,
                .BackColor = Color.Transparent
            }

            btnGenerate = New Forms.Common.ModernButton() With {
                .Text = " 📊 Generate",
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Primary,
                .Size = New Size(110, 36),
                .Location = New Point(10, 32),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            AddHandler btnGenerate.Click, AddressOf btnGenerate_Click

            btnExportExcel = New Forms.Common.ModernButton() With {
                .Text = " 📗 Excel Export",
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Custom,
                .BackColor = ThemeConstants.SuccessGreen,
                .ForeColor = Color.White,
                .Size = New Size(118, 36),
                .Location = New Point(126, 32),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            AddHandler btnExportExcel.Click, AddressOf btnExportExcel_Click

            btnExportPdf = New Forms.Common.ModernButton() With {
                .Text = " 📕 PDF Export",
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Custom,
                .BackColor = Color.FromArgb(220, 38, 38),
                .ForeColor = Color.White,
                .Size = New Size(110, 36),
                .Location = New Point(250, 32),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            AddHandler btnExportPdf.Click, AddressOf btnExportPdf_Click

            btnPrintPreview = New Forms.Common.ModernButton() With {
                .Text = " 🖨️ Print Preview",
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Secondary,
                .Size = New Size(122, 36),
                .Location = New Point(366, 32),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            AddHandler btnPrintPreview.Click, AddressOf btnPrintPreview_Click

            pnlActionButtons.Controls.Add(btnPrintPreview)
            pnlActionButtons.Controls.Add(btnExportPdf)
            pnlActionButtons.Controls.Add(btnExportExcel)
            pnlActionButtons.Controls.Add(btnGenerate)

            pnlFilterBar.Controls.Add(pnlActionButtons)
            pnlFilterBar.Controls.Add(pnlFilterFields)

            ' 3. Report Data Grid Surface Card Container
            pnlGridCard = New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = Color.White,
                .Padding = New Padding(12),
                .Margin = New Padding(0)
            }

            pnlGridHeader = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 34,
                .BackColor = Color.White
            }

            lblRecordCount = New Label() With {
                .Text = "📋 Report Grid Preview (0 Records)",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(0, 6),
                .AutoSize = True
            }
            pnlGridHeader.Controls.Add(lblRecordCount)

            dgvReportData = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .ReadOnly = True,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                .ScrollBars = ScrollBars.Both,
                .AllowUserToOrderColumns = True,
                .AllowUserToResizeColumns = True,
                .MultiSelect = False
            }
            ThemeConstants.ApplyModernGridStyle(dgvReportData)

            ' 4. Psychological Zero-Record Empty State Card Overlay
            pnlEmptyState = New Panel() With {
                .Size = New Size(540, 220),
                .BackColor = Color.White,
                .Visible = True
            }
            AddHandler pnlEmptyState.Paint, Sub(s, e)
                                                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
                                                Using p As New Pen(Color.FromArgb(226, 232, 240), 1)
                                                    e.Graphics.DrawRectangle(p, 0, 0, pnlEmptyState.Width - 1, pnlEmptyState.Height - 1)
                                                End Using
                                            End Sub

            pbEmptyIcon = New PictureBox() With {
                .Size = New Size(50, 50),
                .Location = New Point(245, 16),
                .SizeMode = PictureBoxSizeMode.CenterImage,
                .Image = VectorIconHelper.CreateExportIcon(Color.FromArgb(79, 70, 229), 22),
                .BackColor = Color.FromArgb(238, 242, 255)
            }

            lblEmptyTitle = New Label() With {
                .Text = "No Report Data Loaded",
                .Font = New Font(ThemeConstants.FontNameDefault, 12.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(20, 72),
                .Size = New Size(500, 24),
                .TextAlign = ContentAlignment.MiddleCenter
            }

            lblEmptySubtitle = New Label() With {
                .Text = "Select a report type and date range above, then click 'Generate' to query SQL records and preview data.",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular),
                .ForeColor = ThemeConstants.TextSecondary,
                .Location = New Point(30, 98),
                .Size = New Size(480, 36),
                .TextAlign = ContentAlignment.MiddleCenter
            }

            btnEmptyGenerate = New Forms.Common.ModernButton() With {
                .Text = " 📊 Generate Report",
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Primary,
                .Size = New Size(160, 36),
                .Location = New Point(190, 148)
            }
            AddHandler btnEmptyGenerate.Click, AddressOf btnGenerate_Click

            pnlEmptyState.Controls.Add(btnEmptyGenerate)
            pnlEmptyState.Controls.Add(lblEmptySubtitle)
            pnlEmptyState.Controls.Add(lblEmptyTitle)
            pnlEmptyState.Controls.Add(pbEmptyIcon)

            pnlGridCard.Controls.Add(pnlEmptyState)
            pnlGridCard.Controls.Add(dgvReportData)
            pnlGridCard.Controls.Add(pnlGridHeader)
            AddHandler pnlGridCard.Resize, Sub(s, e) PositionEmptyStateOverlay()

            Me.Controls.Add(pnlGridCard)
            Me.Controls.Add(pnlFilterBar)
            Me.Controls.Add(pnlHeroHeader)

            Me.ResumeLayout(False)
            PositionEmptyStateOverlay()
        End Sub

        Private Sub PositionEmptyStateOverlay()
            If pnlEmptyState IsNot Nothing AndAlso pnlGridCard IsNot Nothing Then
                Dim x = Math.Max(10, (pnlGridCard.Width - pnlEmptyState.Width) \ 2)
                Dim y = Math.Max(40, (pnlGridCard.Height - pnlEmptyState.Height) \ 2)
                pnlEmptyState.Location = New Point(x, y)
                pnlEmptyState.BringToFront()
            End If
        End Sub

        Private Async Sub PopulateDropdownsAsync()
            Try
                Dim clients = Await _clientService.GetAllClientsAsync()
                Dim displayList As New List(Of ClientDto)()
                displayList.Add(New ClientDto() With {.ClientId = 0, .ClientName = "-- All Registered Clients --"})
                displayList.AddRange(clients)

                cboClientFilter.DataSource = displayList
                cboClientFilter.DisplayMember = "ClientName"
                cboClientFilter.ValueMember = "ClientId"
                cboClientFilter.SelectedIndex = 0
            Catch ex As Exception
            End Try
        End Sub

        Private Async Sub btnGenerate_Click(sender As Object, e As EventArgs)
            Await GenerateReportDataAsync()
        End Sub

        Private Async Function GenerateReportDataAsync() As Task(Of DataTable)
            Try
                Me.Cursor = Cursors.WaitCursor
                Dim selectedReport = If(cboReportType.SelectedItem IsNot Nothing, cboReportType.SelectedItem.ToString(), "Task Completion & Status Summary")
                Dim selectedClientId As Integer = 0
                If cboClientFilter.SelectedValue IsNot Nothing Then
                    If TypeOf cboClientFilter.SelectedValue Is Integer Then
                        selectedClientId = CInt(cboClientFilter.SelectedValue)
                    ElseIf TypeOf cboClientFilter.SelectedItem Is ClientDto Then
                        selectedClientId = DirectCast(cboClientFilter.SelectedItem, ClientDto).ClientId
                    End If
                End If
                Dim startDate = dtpStartDate.Value.Date
                Dim endDate = dtpEndDate.Value.Date

                Dim dt As New DataTable()

                If selectedReport.Contains("Task") Then
                    dt.Columns.Add("Task Code")
                    dt.Columns.Add("Client Name")
                    dt.Columns.Add("Task Title")
                    dt.Columns.Add("Assigned To")
                    dt.Columns.Add("Priority")
                    dt.Columns.Add("Due Date")
                    dt.Columns.Add("Status")

                    Dim tasks = Await _taskService.GetAllTasksAsync(includeDeleted:=False)
                    Dim filtered = tasks.FindAll(Function(t)
                                                     Dim dateMatch = (t.TargetDueDate.Date >= startDate AndAlso t.TargetDueDate.Date <= endDate)
                                                     Dim clientMatch = (selectedClientId = 0 OrElse t.ClientId = selectedClientId)
                                                     Return dateMatch AndAlso clientMatch
                                                 End Function)

                    For Each t In filtered
                        dt.Rows.Add(t.TaskCode, t.ClientName, t.Title, t.AssignedToName, t.Priority.ToString(), t.TargetDueDate.ToString("dd-MMM-yyyy"), t.WorkflowState.ToString())
                    Next

                ElseIf selectedReport.Contains("Client") Then
                    dt.Columns.Add("Client Code")
                    dt.Columns.Add("Client Name")
                    dt.Columns.Add("Contact Person")
                    dt.Columns.Add("Phone")
                    dt.Columns.Add("Email")
                    dt.Columns.Add("Department")

                    Dim clients = Await _clientService.GetAllClientsAsync()
                    Dim filtered = clients.FindAll(Function(c) selectedClientId = 0 OrElse c.ClientId = selectedClientId)

                    For Each c In filtered
                        dt.Rows.Add(c.ClientCode, c.ClientName, c.ContactPerson, c.Phone, c.Email, c.Department.ToString())
                    Next

                Else ' Staff Attendance Report
                    dt.Columns.Add("Attendance ID")
                    dt.Columns.Add("Staff Name")
                    dt.Columns.Add("Date")
                    dt.Columns.Add("Punch In")
                    dt.Columns.Add("Punch Out")
                    dt.Columns.Add("Hours Worked")
                    dt.Columns.Add("Status")

                    Dim attendanceLogs = Await _attendanceService.GetAllAttendanceHistoryAsync()
                    Dim filtered = attendanceLogs.FindAll(Function(a) a.AttendanceDate.Date >= startDate AndAlso a.AttendanceDate.Date <= endDate)

                    For Each a In filtered
                        Dim pIn = a.ClockInTime.ToString("hh:mm tt")
                        Dim pOut = If(a.ClockOutTime.HasValue, a.ClockOutTime.Value.ToString("hh:mm tt"), "--")
                        Dim hrs = If(a.ClockOutTime.HasValue, Math.Round(a.TotalWorkingMinutes / 60.0, 1).ToString() & " hrs", "--")
                        dt.Rows.Add(a.AttendanceId.ToString(), a.UserName, a.AttendanceDate.ToString("dd-MMM-yyyy"), pIn, pOut, hrs, a.Status)
                    Next
                End If

                dgvReportData.DataSource = dt
                ThemeConstants.ApplyHybridGridColumnSizing(dgvReportData)

                lblRecordCount.Text = $"📋 Report Grid Preview ({dt.Rows.Count} Records Loaded from SQL)"

                If dt.Rows.Count > 0 Then
                    pnlEmptyState.Visible = False
                Else
                    pnlEmptyState.Visible = True
                    PositionEmptyStateOverlay()
                End If

                Return dt
            Catch ex As Exception
                Dim parentForm = TryCast(Me.FindForm(), Form)
                Forms.Common.FrmInAppAlert.ShowModal(parentForm, "Error", "Failed to query report records: " & ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
                Return New DataTable()
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Function

        Private Async Sub btnExportExcel_Click(sender As Object, e As EventArgs)
            Dim parentForm = TryCast(Me.FindForm(), Form)
            Dim dt = TryCast(dgvReportData.DataSource, DataTable)
            If dt Is Nothing OrElse dt.Rows.Count = 0 Then
                dt = Await GenerateReportDataAsync()
            End If

            If dt Is Nothing OrElse dt.Rows.Count = 0 Then
                Forms.Common.FrmInAppAlert.ShowModal(parentForm, "Export Notice", "No report data available to export.", Forms.Common.AlertType.WarningAlert, actionText:="OK")
                Return
            End If

            Dim exportDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CA_Automation_Exports")
            Dim fileName = $"Report_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            Dim fullPath = Path.Combine(exportDir, fileName)

            Dim success = Await _reportGenerator.ExportToCsvAsync(dt, fullPath)
            If success Then
                Forms.Common.FrmInAppAlert.ShowModal(parentForm, "Export Complete", $"Excel/CSV Report Exported Successfully!{Environment.NewLine}{Environment.NewLine}File Path: {fullPath}", Forms.Common.AlertType.SuccessAlert, actionText:="GREAT")
                Try
                    Process.Start("explorer.exe", $"/select,""{fullPath}""")
                Catch
                End Try
            Else
                Forms.Common.FrmInAppAlert.ShowModal(parentForm, "Error", "Failed to export Excel report file.", Forms.Common.AlertType.ErrorAlert, actionText:="OK")
            End If
        End Sub

        Private Async Sub btnExportPdf_Click(sender As Object, e As EventArgs)
            Dim parentForm = TryCast(Me.FindForm(), Form)
            Dim dt = TryCast(dgvReportData.DataSource, DataTable)
            If dt Is Nothing OrElse dt.Rows.Count = 0 Then
                dt = Await GenerateReportDataAsync()
            End If

            If dt Is Nothing OrElse dt.Rows.Count = 0 Then
                Forms.Common.FrmInAppAlert.ShowModal(parentForm, "Export Notice", "No report data available to export.", Forms.Common.AlertType.WarningAlert, actionText:="OK")
                Return
            End If

            Dim selectedReport = If(cboReportType.SelectedItem IsNot Nothing, cboReportType.SelectedItem.ToString(), "Executive Summary Report")
            Dim exportDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CA_Automation_Exports")
            Dim fileName = $"Report_Document_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
            Dim fullPath = Path.Combine(exportDir, fileName)

            Dim success = Await _reportGenerator.ExportToPdfReportAsync(selectedReport, dt, fullPath)
            If success Then
                Forms.Common.FrmInAppAlert.ShowModal(parentForm, "PDF Export Complete", $"PDF Report Document Generated Successfully!{Environment.NewLine}{Environment.NewLine}File Path: {fullPath}", Forms.Common.AlertType.SuccessAlert, actionText:="GREAT")
                Try
                    Process.Start("explorer.exe", $"/select,""{fullPath}""")
                Catch
                End Try
            Else
                Forms.Common.FrmInAppAlert.ShowModal(parentForm, "Error", "Failed to export PDF report document.", Forms.Common.AlertType.ErrorAlert, actionText:="OK")
            End If
        End Sub

        Private Sub btnPrintPreview_Click(sender As Object, e As EventArgs)
            Dim parentForm = TryCast(Me.FindForm(), Form)
            Dim dt = TryCast(dgvReportData.DataSource, DataTable)
            If dt Is Nothing OrElse dt.Rows.Count = 0 Then
                Forms.Common.FrmInAppAlert.ShowModal(parentForm, "No Data", "Please click 'Generate' first to load data before opening Print Preview.", Forms.Common.AlertType.WarningAlert, actionText:="OK")
                Return
            End If

            Try
                Dim pd As New PrintDocument()
                AddHandler pd.PrintPage, AddressOf OnPrintReportPage

                Dim dlg As New PrintPreviewDialog()
                dlg.Document = pd
                dlg.Width = 900
                dlg.Height = 700
                dlg.ShowDialog()
            Catch ex As Exception
                Forms.Common.FrmInAppAlert.ShowModal(parentForm, "Error", "Failed to initialize Print Preview: " & ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
            End Try
        End Sub

        Private Sub OnPrintReportPage(sender As Object, pe As PrintPageEventArgs)
            Dim dt = TryCast(dgvReportData.DataSource, DataTable)
            If dt Is Nothing Then Return

            Dim fTitle As New Font(ThemeConstants.FontNameDefault, 14.0!, FontStyle.Bold)
            Dim fHeader As New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold)
            Dim fRow As New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Regular)

            Dim y As Single = 50
            pe.Graphics.DrawString("CA Office Workforce System — Executive Report", fTitle, Brushes.Black, 50, y)
            y += 40

            ' Draw Headers
            Dim x As Single = 50
            For Each col As DataColumn In dt.Columns
                pe.Graphics.DrawString(col.ColumnName, fHeader, Brushes.Black, x, y)
                x += 110
            Next
            y += 25
            pe.Graphics.DrawLine(Pens.Black, 50, y, 750, y)
            y += 10

            ' Draw Rows
            For Each row As DataRow In dt.Rows
                x = 50
                For Each col As DataColumn In dt.Columns
                    Dim cellText = row(col).ToString()
                    If cellText.Length > 15 Then cellText = cellText.Substring(0, 12) & "..."
                    pe.Graphics.DrawString(cellText, fRow, Brushes.Black, x, y)
                    x += 110
                Next
                y += 22
                If y > pe.MarginBounds.Bottom Then
                    pe.HasMorePages = False
                    Exit For
                End If
            Next
        End Sub
    End Class
End Namespace
