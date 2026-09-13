Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports FontAwesome.Sharp
Imports StaffAutomation.BLL.Interfaces
Imports StaffAutomation.BLL.Services
Imports StaffAutomation.Core.Configuration
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.WinForms.Forms.Common
Imports StaffAutomation.WinForms.Forms.Main
Imports StaffAutomation.WinForms.UIHelpers

Namespace Forms.Attendance
    ''' <summary>
    ''' Modal Inspection Dashboard Form inheriting standard System.Windows.Forms.Form
    ''' with FontAwesome.Sharp vector icons and clean, exception-free WinForms layout panels.
    ''' Allows Admin to view an individual staff member's detailed workday progress,
    ''' semantic status hero banner, compact metric cards, and complete monthly history grid.
    ''' </summary>
    Public Class FrmStaffAttendanceDetail
        Inherits Form

        Private ReadOnly _targetUserId As Integer
        Private ReadOnly _staffName As String
        Private ReadOnly _attendanceService As IAttendanceService
        Private ReadOnly _userRepo As DAL.Interfaces.IUserRepository
        Private ReadOnly _policyEngine As IAttendancePolicyEngine
        Private ReadOnly _policyProvider As IPolicyProvider

        ' Standard WinForms UI Layout Containers & Controls
        Private pnlHeader As Panel
        Private lblStaffName As Label
        Private lblMetaId As Label
        Private btnCloseHeader As Button

        Private pnlHeroBanner As Panel
        Private lblHeroStatusTitle As Label
        Private lblHeroStatusSub As Label

        Private pnlMetricSummaryHost As Panel
        Private cardStatus As Panel
        Private lblStatusTitle As Label
        Private lblStatusValueBadge As Label

        Private cardNetWork As Panel
        Private lblNetTitle As Label
        Private lblNetValue As Label

        Private cardGrossWork As Panel
        Private lblGrossTitle As Label
        Private lblGrossValue As Label

        Private cardLunchBreak As Panel
        Private lblBreakTitle As Label
        Private lblBreakValue As Label

        Private pnlGridBox As Panel
        Private lblGridHeader As Label
        Private dgvHistory As DataGridView
        Private pnlEmptyState As Panel
        Private picEmptyIcon As IconPictureBox
        Private lblEmptyTitle As Label
        Private lblEmptySub As Label

        Public Sub New(userId As Integer, staffName As String, attendanceService As IAttendanceService, userRepo As DAL.Interfaces.IUserRepository)
            _targetUserId = userId
            _staffName = staffName
            _attendanceService = attendanceService
            _userRepo = userRepo
            _policyEngine = New AttendancePolicyEngine()
            _policyProvider = New DefaultAttendancePolicyProvider()

            Me.DoubleBuffered = True
            Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
            Me.UpdateStyles()

            InitializeComponent()
        End Sub

        Protected Overrides Sub OnPaintBackground(pevent As PaintEventArgs)
            MyBase.OnPaintBackground(pevent)
            If pevent IsNot Nothing AndAlso pevent.Graphics IsNot Nothing Then
                Using b As New SolidBrush(ThemeConstants.WorkspaceBackground)
                    pevent.Graphics.FillRectangle(b, ClientRectangle)
                End Using
            End If
        End Sub

        Protected Overrides Sub OnShown(e As EventArgs)
            MyBase.OnShown(e)
            LoadStaffDetailDataAsync()
        End Sub

        Private Sub InitializeComponent()
            Me.SuspendLayout()
            Me.Size = New Size(760, 610)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.Text = $"Staff Attendance Inspection — {_staffName}"
            Me.BackColor = ThemeConstants.WorkspaceBackground
            Me.Padding = New Padding(16)

            ' -----------------------------------------------------------------
            ' 1. Application Dark Navy Identity Header Surface
            ' -----------------------------------------------------------------
            pnlHeader = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 56,
                .Padding = New Padding(16, 8, 16, 8),
                .BackColor = Color.FromArgb(15, 23, 42)
            }

            lblStaffName = New Label() With {
                .Text = _staffName,
                .Font = New Font(ThemeConstants.FontNameDefault, 12.5!, FontStyle.Bold),
                .ForeColor = Color.White,
                .BackColor = Color.Transparent,
                .Location = New Point(14, 8),
                .AutoSize = True
            }

            lblMetaId = New Label() With {
                .Text = $"User ID: #{_targetUserId}  |  Staff Member Detailed Inspection",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Regular),
                .ForeColor = Color.FromArgb(148, 163, 184),
                .BackColor = Color.Transparent,
                .Location = New Point(15, 31),
                .AutoSize = True
            }

            btnCloseHeader = New Button() With {
                .Text = "Close",
                .Dock = DockStyle.Right,
                .Width = 80,
                .Height = 32,
                .Margin = New Padding(0, 4, 0, 4),
                .FlatStyle = FlatStyle.Flat,
                .BackColor = Color.FromArgb(51, 65, 85),
                .ForeColor = Color.White,
                .Cursor = Cursors.Hand
            }
            btnCloseHeader.FlatAppearance.BorderSize = 0
            AddHandler btnCloseHeader.Click, Sub(s, e) Me.Close()

            pnlHeader.Controls.Add(btnCloseHeader)
            pnlHeader.Controls.Add(lblMetaId)
            pnlHeader.Controls.Add(lblStaffName)

            ' -----------------------------------------------------------------
            ' 2. Prominent Semantic Status Hero Area
            ' -----------------------------------------------------------------
            pnlHeroBanner = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 54,
                .Padding = New Padding(14, 8, 14, 8),
                .Margin = New Padding(0, 10, 0, 8),
                .BackColor = Color.FromArgb(248, 250, 252)
            }

            lblHeroStatusTitle = New Label() With {
                .Text = "Evaluating live attendance status...",
                .Font = New Font(ThemeConstants.FontNameDefault, 11.0!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(30, 41, 59),
                .BackColor = Color.Transparent,
                .Location = New Point(12, 6),
                .AutoSize = True
            }

            lblHeroStatusSub = New Label() With {
                .Text = "Fetching workday timeline and attendance records...",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Regular),
                .ForeColor = Color.FromArgb(100, 116, 139),
                .BackColor = Color.Transparent,
                .Location = New Point(13, 28),
                .AutoSize = True
            }

            pnlHeroBanner.Controls.Add(lblHeroStatusSub)
            pnlHeroBanner.Controls.Add(lblHeroStatusTitle)

            ' -----------------------------------------------------------------
            ' 3. Visually Distinct Semantic Metric Cards Row with FontAwesome Icons
            ' -----------------------------------------------------------------
            pnlMetricSummaryHost = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 68,
                .Margin = New Padding(0, 0, 0, 10),
                .BackColor = ThemeConstants.WorkspaceBackground
            }

            ' Card 1: Today's Status
            cardStatus = CreateSummaryMetricBox("TODAY'S STATUS", 0, 172, IconChar.ChartBar, Color.FromArgb(241, 245, 249), lblStatusTitle)
            lblStatusValueBadge = New Label() With {
                .Text = "Loading...",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.PrimaryAccent,
                .BackColor = Color.Transparent,
                .Location = New Point(12, 28),
                .AutoSize = True
            }
            cardStatus.Controls.Add(lblStatusValueBadge)

            ' Card 2: Net Working Time
            cardNetWork = CreateSummaryMetricBox("TODAY'S NET WORK", 182, 172, IconChar.Clock, Color.FromArgb(240, 249, 255), lblNetTitle)
            lblNetValue = New Label() With {
                .Text = "--",
                .Font = New Font(ThemeConstants.FontNameDefault, 16.0!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(3, 105, 161),
                .BackColor = Color.Transparent,
                .Location = New Point(12, 26),
                .AutoSize = True
            }
            cardNetWork.Controls.Add(lblNetValue)

            ' Card 3: Gross Working Time
            cardGrossWork = CreateSummaryMetricBox("GROSS WORKING TIME", 364, 172, IconChar.HourglassHalf, Color.FromArgb(236, 253, 245), lblGrossTitle)
            lblGrossValue = New Label() With {
                .Text = "--",
                .Font = New Font(ThemeConstants.FontNameDefault, 16.0!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(4, 120, 87),
                .BackColor = Color.Transparent,
                .Location = New Point(12, 26),
                .AutoSize = True
            }
            cardGrossWork.Controls.Add(lblGrossValue)

            ' Card 4: Lunch / Break Time
            cardLunchBreak = CreateSummaryMetricBox("LUNCH / BREAK TIME", 546, 172, IconChar.Utensils, Color.FromArgb(255, 251, 235), lblBreakTitle)
            lblBreakValue = New Label() With {
                .Text = "--",
                .Font = New Font(ThemeConstants.FontNameDefault, 16.0!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(180, 83, 9),
                .BackColor = Color.Transparent,
                .Location = New Point(12, 26),
                .AutoSize = True
            }
            cardLunchBreak.Controls.Add(lblBreakValue)

            pnlMetricSummaryHost.Controls.Add(cardLunchBreak)
            pnlMetricSummaryHost.Controls.Add(cardGrossWork)
            pnlMetricSummaryHost.Controls.Add(cardNetWork)
            pnlMetricSummaryHost.Controls.Add(cardStatus)

            ' -----------------------------------------------------------------
            ' 4. Monthly Attendance History Grid Box & Empty State
            ' -----------------------------------------------------------------
            pnlGridBox = New Panel() With {
                .Dock = DockStyle.Fill,
                .Margin = New Padding(0, 10, 0, 0),
                .BackColor = Color.White
            }

            lblGridHeader = New Label() With {
                .Text = "Complete Monthly Punch Log History",
                .Font = New Font(ThemeConstants.FontNameDefault, 10.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .BackColor = Color.Transparent,
                .Dock = DockStyle.Top,
                .Height = 36,
                .Padding = New Padding(12, 9, 0, 0)
            }

            dgvHistory = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                .BackgroundColor = Color.White,
                .BorderStyle = BorderStyle.None,
                .ReadOnly = True,
                .AllowUserToAddRows = False,
                .RowHeadersVisible = False,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect
            }
            dgvHistory.RowTemplate.Height = 38
            ThemeConstants.ApplyModernGridStyle(dgvHistory)

            ' Ensure single-line non-wrapping column headers
            dgvHistory.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False
            dgvHistory.ColumnHeadersHeight = 34

            AddHandler dgvHistory.CellPainting, AddressOf dgvHistory_CellPainting

            ' Intentional Empty State Container Panel
            pnlEmptyState = New Panel() With {
                .Dock = DockStyle.Fill,
                .Visible = False,
                .BackColor = Color.White
            }

            picEmptyIcon = New IconPictureBox() With {
                .IconChar = IconChar.ClipboardList,
                .IconColor = Color.FromArgb(148, 163, 184),
                .IconSize = 48,
                .Size = New Size(48, 48),
                .Location = New Point(340, 80),
                .BackColor = Color.White
            }

            lblEmptyTitle = New Label() With {
                .Text = "No Attendance History Available",
                .Font = New Font(ThemeConstants.FontNameDefault, 11.0!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(51, 65, 85),
                .BackColor = Color.Transparent,
                .Size = New Size(728, 24),
                .Location = New Point(0, 134),
                .TextAlign = ContentAlignment.MiddleCenter
            }

            lblEmptySub = New Label() With {
                .Text = "No punch log records were found for this staff member for the selected period.",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular),
                .ForeColor = Color.FromArgb(148, 163, 184),
                .BackColor = Color.Transparent,
                .Size = New Size(728, 20),
                .Location = New Point(0, 160),
                .TextAlign = ContentAlignment.MiddleCenter
            }

            pnlEmptyState.Controls.Add(lblEmptySub)
            pnlEmptyState.Controls.Add(lblEmptyTitle)
            pnlEmptyState.Controls.Add(picEmptyIcon)

            pnlGridBox.Controls.Add(pnlEmptyState)
            pnlGridBox.Controls.Add(dgvHistory)
            pnlGridBox.Controls.Add(lblGridHeader)

            Me.Controls.Add(pnlGridBox)
            Me.Controls.Add(pnlMetricSummaryHost)
            Me.Controls.Add(pnlHeroBanner)
            Me.Controls.Add(pnlHeader)

            Me.ResumeLayout(False)
        End Sub

        Private Function CreateSummaryMetricBox(title As String, leftPos As Integer, boxWidth As Integer, iconChar As IconChar, bgCol As Color, ByRef lblTitleRef As Label) As Panel
            Dim pnl As New Panel() With {
                .Location = New Point(leftPos, 0),
                .Size = New Size(boxWidth, 64),
                .BackColor = bgCol,
                .BorderStyle = BorderStyle.FixedSingle
            }

            lblTitleRef = New Label() With {
                .Text = title,
                .Font = New Font(ThemeConstants.FontNameDefault, 7.5!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(71, 85, 105),
                .BackColor = Color.Transparent,
                .Location = New Point(12, 8),
                .AutoSize = True
            }

            Dim picIcon As New IconPictureBox() With {
                .IconChar = iconChar,
                .IconColor = Color.FromArgb(100, 116, 139),
                .IconSize = 18,
                .Size = New Size(18, 18),
                .Location = New Point(boxWidth - 24, 6),
                .BackColor = bgCol
            }

            pnl.Controls.Add(picIcon)
            pnl.Controls.Add(lblTitleRef)
            Return pnl
        End Function

        Private Async Sub LoadStaffDetailDataAsync()
            Try
                Me.Cursor = Cursors.WaitCursor

                ' 1. Load Today's Attendance Record for Target Staff Member
                Dim todayRec = Await _attendanceService.GetTodayAttendanceForUserAsync(_targetUserId)
                If todayRec IsNot Nothing Then
                    Dim policy = Await _policyProvider.GetEffectivePolicyAsync(_targetUserId, DateTime.Today)
                    Dim clockIn = CType(todayRec.ClockInTime, DateTime?)
                    Dim clockOut = todayRec.ClockOutTime
                    Dim breakStart = todayRec.BreakStartTime
                    Dim totalBreak = todayRec.TotalBreakMinutes

                    Dim prog = _policyEngine.CalculateProgress(clockIn, clockOut, DateTime.Now, policy, breakStart, totalBreak)

                    lblStatusValueBadge.Text = If(String.IsNullOrEmpty(prog.AttendanceState), "Not Punched In", prog.AttendanceState)
                    lblNetValue.Text = AttendanceUiPresenter.FormatHoursShort(prog.NetWorkedMinutes)
                    lblGrossValue.Text = AttendanceUiPresenter.FormatHoursShort(prog.GrossWorkedMinutes)
                    lblBreakValue.Text = AttendanceUiPresenter.FormatHoursShort(prog.TotalBreakMinutes)

                    ' Apply Hero Banner Semantic Background & Contextual Explanation
                    Select Case prog.AttendanceState
                        Case "Working"
                            pnlHeroBanner.BackColor = Color.FromArgb(236, 253, 245)
                            lblHeroStatusTitle.Text = "Currently Working"
                            lblHeroStatusTitle.ForeColor = Color.FromArgb(6, 95, 70)
                            Dim pInStr = If(clockIn.HasValue AndAlso clockIn.Value <> DateTime.MinValue, clockIn.Value.ToString("hh:mm tt"), "--")
                            lblHeroStatusSub.Text = $"Clocked in today at {pInStr} — Net working duration: {AttendanceUiPresenter.FormatHoursShort(prog.NetWorkedMinutes)}"
                            lblHeroStatusSub.ForeColor = Color.FromArgb(4, 120, 87)

                        Case "On Lunch Break"
                            pnlHeroBanner.BackColor = Color.FromArgb(255, 251, 235)
                            lblHeroStatusTitle.Text = "On Lunch Break"
                            lblHeroStatusTitle.ForeColor = Color.FromArgb(146, 64, 14)
                            lblHeroStatusSub.Text = $"Staff member is currently on break — Total break duration: {prog.TotalBreakMinutes} minutes"
                            lblHeroStatusSub.ForeColor = Color.FromArgb(180, 83, 9)

                        Case "Day Completed", "Completed"
                            pnlHeroBanner.BackColor = Color.FromArgb(241, 245, 249)
                            lblHeroStatusTitle.Text = "Workday Completed"
                            lblHeroStatusTitle.ForeColor = Color.FromArgb(30, 41, 59)
                            Dim pOutStr = If(clockOut.HasValue AndAlso clockOut.Value <> DateTime.MinValue, clockOut.Value.ToString("hh:mm tt"), "--")
                            lblHeroStatusSub.Text = $"Shift completed today at {pOutStr} — Total worked: {AttendanceUiPresenter.FormatHoursShort(prog.NetWorkedMinutes)}"
                            lblHeroStatusSub.ForeColor = Color.FromArgb(71, 85, 105)

                        Case Else
                            If todayRec.Status IsNot Nothing AndAlso todayRec.Status.Contains("Late") Then
                                pnlHeroBanner.BackColor = Color.FromArgb(254, 242, 242)
                                lblHeroStatusTitle.Text = "Late Arrival Flagged"
                                lblHeroStatusTitle.ForeColor = Color.FromArgb(153, 27, 27)
                                Dim pInStr = If(clockIn.HasValue AndAlso clockIn.Value <> DateTime.MinValue, clockIn.Value.ToString("hh:mm tt"), "--")
                                lblHeroStatusSub.Text = $"Clocked in late at {pInStr} — Workday in progress"
                                lblHeroStatusSub.ForeColor = Color.FromArgb(185, 28, 28)
                            Else
                                pnlHeroBanner.BackColor = Color.FromArgb(248, 250, 252)
                                lblHeroStatusTitle.Text = $"Status: {prog.AttendanceState}"
                                lblHeroStatusTitle.ForeColor = Color.FromArgb(30, 41, 59)
                                lblHeroStatusSub.Text = "Staff attendance record actively logged"
                                lblHeroStatusSub.ForeColor = Color.FromArgb(100, 116, 139)
                            End If
                    End Select
                Else
                    pnlHeroBanner.BackColor = Color.FromArgb(255, 247, 237)
                    lblHeroStatusTitle.Text = "Not Punched In Today"
                    lblHeroStatusTitle.ForeColor = Color.FromArgb(154, 52, 18)
                    lblHeroStatusSub.Text = "No punch log or attendance entry has been recorded for today yet."
                    lblHeroStatusSub.ForeColor = Color.FromArgb(194, 65, 12)

                    lblStatusValueBadge.Text = "Not Punched In"
                    lblNetValue.Text = "0h 0m"
                    lblGrossValue.Text = "0h 0m"
                    lblBreakValue.Text = "0m"
                End If

                ' 2. Load Complete Historical Attendance for Target Staff Member
                Dim history = Await _attendanceService.GetUserAttendanceHistoryAsync(_targetUserId)

                If history IsNot Nothing AndAlso history.Count > 0 Then
                    pnlEmptyState.Visible = False
                    dgvHistory.Visible = True

                    Dim dt As New DataTable()
                    dt.Columns.Add("Date")
                    dt.Columns.Add("Punch In")
                    dt.Columns.Add("Punch Out")
                    dt.Columns.Add("Break Time")
                    dt.Columns.Add("Working Hours")
                    dt.Columns.Add("Status")

                    For Each item In history
                        Dim pIn = If(item.ClockInTime = DateTime.MinValue, "--", item.ClockInTime.ToString("hh:mm tt"))
                        Dim pOut = If(item.ClockOutTime.HasValue AndAlso item.ClockOutTime.Value <> DateTime.MinValue, item.ClockOutTime.Value.ToString("hh:mm tt"), "--")
                        Dim brk = If(item.TotalBreakMinutes > 0, $"{item.TotalBreakMinutes} m", "--")
                        Dim hrs = If(item.ClockOutTime.HasValue, AttendanceUiPresenter.FormatHoursShort(item.TotalWorkingMinutes), "--")
                        Dim statusStr = If(String.IsNullOrEmpty(item.Status), "Not Punched", item.Status)
                        dt.Rows.Add(item.AttendanceDate.ToString("dd-MMM-yyyy"), pIn, pOut, brk, hrs, statusStr)
                    Next

                    dgvHistory.DataSource = dt

                    ' Configure Single-Line Column Alignment & Minimum Widths
                    If dgvHistory.Columns.Contains("Date") Then
                        dgvHistory.Columns("Date").FillWeight = 110
                        dgvHistory.Columns("Date").MinimumWidth = 100
                    End If
                    If dgvHistory.Columns.Contains("Punch In") Then
                        dgvHistory.Columns("Punch In").FillWeight = 90
                        dgvHistory.Columns("Punch In").MinimumWidth = 80
                        dgvHistory.Columns("Punch In").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
                    End If
                    If dgvHistory.Columns.Contains("Punch Out") Then
                        dgvHistory.Columns("Punch Out").FillWeight = 90
                        dgvHistory.Columns("Punch Out").MinimumWidth = 80
                        dgvHistory.Columns("Punch Out").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
                    End If
                    If dgvHistory.Columns.Contains("Break Time") Then
                        dgvHistory.Columns("Break Time").HeaderText = "Break Time"
                        dgvHistory.Columns("Break Time").FillWeight = 85
                        dgvHistory.Columns("Break Time").MinimumWidth = 80
                        dgvHistory.Columns("Break Time").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
                    End If
                    If dgvHistory.Columns.Contains("Working Hours") Then
                        dgvHistory.Columns("Working Hours").HeaderText = "Working Hours"
                        dgvHistory.Columns("Working Hours").FillWeight = 105
                        dgvHistory.Columns("Working Hours").MinimumWidth = 95
                        dgvHistory.Columns("Working Hours").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
                    End If
                    If dgvHistory.Columns.Contains("Status") Then
                        dgvHistory.Columns("Status").FillWeight = 125
                        dgvHistory.Columns("Status").MinimumWidth = 115
                        dgvHistory.Columns("Status").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
                    End If
                Else
                    ' Display Intentional Empty State Container
                    dgvHistory.Visible = False
                    pnlEmptyState.Visible = True
                End If
            Catch ex As Exception
                lblStatusValueBadge.Text = "Error loading data"
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Sub

        Private Sub dgvHistory_CellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs)
            Try
                If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return
                If dgvHistory.Columns Is Nothing OrElse e.ColumnIndex >= dgvHistory.Columns.Count Then Return

                Dim colName = dgvHistory.Columns(e.ColumnIndex).Name

                ' Owner-drawn status badges matching AdminAttendanceBoardControl renderer
                If colName = "Status" AndAlso e.Value IsNot Nothing AndAlso e.Value IsNot DBNull.Value Then
                    e.Paint(e.CellBounds, DataGridViewPaintParts.Background Or DataGridViewPaintParts.Border)

                    Dim statusText = e.Value.ToString()
                    Dim bgCol As Color
                    Dim textCol As Color
                    Dim borderCol As Color
                    Dim displayBadgeText As String = statusText

                    Select Case statusText
                        Case "Working"
                            bgCol = Color.FromArgb(220, 252, 231)
                            textCol = Color.FromArgb(22, 101, 52)
                            borderCol = Color.FromArgb(134, 239, 172)
                            displayBadgeText = "Working"
                        Case "On Lunch Break"
                            bgCol = Color.FromArgb(254, 243, 199)
                            textCol = Color.FromArgb(146, 64, 14)
                            borderCol = Color.FromArgb(253, 230, 138)
                            displayBadgeText = "On Break"
                        Case "Day Completed", "Completed"
                            bgCol = Color.FromArgb(241, 245, 249)
                            textCol = Color.FromArgb(51, 65, 85)
                            borderCol = Color.FromArgb(203, 213, 225)
                            displayBadgeText = "Completed"
                        Case "Not Punched In"
                            bgCol = Color.FromArgb(254, 226, 226)
                            textCol = Color.FromArgb(153, 27, 27)
                            borderCol = Color.FromArgb(254, 202, 202)
                            displayBadgeText = "Not Punched"
                        Case Else
                            If statusText.Contains("Late") Then
                                bgCol = Color.FromArgb(255, 237, 213)
                                textCol = Color.FromArgb(154, 52, 18)
                                borderCol = Color.FromArgb(253, 186, 116)
                                displayBadgeText = "Late"
                            Else
                                bgCol = Color.FromArgb(241, 245, 249)
                                textCol = Color.FromArgb(51, 65, 85)
                                borderCol = Color.FromArgb(203, 213, 225)
                                displayBadgeText = statusText
                            End If
                    End Select

                    Dim badgeWidth = Math.Min(e.CellBounds.Width - 12, 110)
                    Dim badgeHeight = 22
                    If badgeWidth < 20 OrElse badgeHeight < 10 OrElse e.CellBounds.Width < 25 Then
                        e.Handled = False
                        Return
                    End If

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

                        Using path = GetRoundedRectPath(badgeRect, 5)
                            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias
                            e.Graphics.FillPath(bgBrush, path)
                            e.Graphics.DrawPath(borderPen, path)
                        End Using

                        Using badgeFont As New Font(ThemeConstants.FontNameDefault, 8.0!, FontStyle.Bold)
                            e.Graphics.DrawString(displayBadgeText, badgeFont, textBrush, badgeRect, sf)
                        End Using
                    End Using

                    e.Handled = True
                End If
            Catch ex As Exception
                e.Handled = False
            End Try
        End Sub

        Private Function GetRoundedRectPath(rect As Rectangle, radius As Integer) As GraphicsPath
            Dim path As New GraphicsPath()
            Dim diameter = radius * 2
            Dim arcRect As New Rectangle(rect.X, rect.Y, diameter, diameter)

            ' Top-Left Arc
            path.AddArc(arcRect, 180, 90)

            ' Top-Right Arc
            arcRect.X = rect.Right - diameter
            path.AddArc(arcRect, 270, 90)

            ' Bottom-Right Arc
            arcRect.Y = rect.Bottom - diameter
            path.AddArc(arcRect, 0, 90)

            ' Bottom-Left Arc
            arcRect.X = rect.X
            path.AddArc(arcRect, 90, 90)

            path.CloseFigure()
            Return path
        End Function

        Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
            MyBase.OnKeyDown(e)
            If e.KeyCode = Keys.Escape Then
                Me.Close()
            End If
        End Sub
    End Class
End Namespace
