Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports StaffAutomation.BLL.Interfaces
Imports StaffAutomation.BLL.Logging
Imports StaffAutomation.BLL.Services
Imports StaffAutomation.Core.Configuration
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Logging
Imports StaffAutomation.Core.Security
Imports StaffAutomation.DAL.Configuration
Imports StaffAutomation.DAL.Core
Imports StaffAutomation.DAL.Repositories
Imports StaffAutomation.WinForms.UIHelpers

Namespace Forms.Main
    ''' <summary>
    ''' High-Resolution Modern Enterprise Action Punch Button with Custom OnPaint rendering.
    ''' Prevents low-quality standard WinForms grayed-out disabled button appearance.
    ''' Supports icons, two-line bold typography, hover elevation glow, and linear gradients.
    ''' </summary>
    Public Class ActionPunchButton
        Inherits Button

        Public Property IconSymbol As String = "▶"
        Public Property SubText As String = "Click to Action"
        Public Property EnabledGradientStart As Color = Color.FromArgb(16, 185, 129)
        Public Property EnabledGradientEnd As Color = Color.FromArgb(5, 150, 105)
        Public Property EnabledTextColor As Color = Color.White
        Public Property DisabledBackColor As Color = Color.FromArgb(241, 245, 249)
        Public Property DisabledTextColor As Color = Color.FromArgb(71, 85, 105)
        Public Property DisabledBorderColor As Color = Color.FromArgb(203, 213, 225)
        Public Property CornerRadius As Integer = 8

        Private _isHovered As Boolean = False

        Public Sub New()
            Me.SetStyle(ControlStyles.UserPaint, True)
            Me.SetStyle(ControlStyles.AllPaintingInWmPaint, True)
            Me.SetStyle(ControlStyles.OptimizedDoubleBuffer, True)
            Me.SetStyle(ControlStyles.ResizeRedraw, True)
            Me.SetStyle(ControlStyles.SupportsTransparentBackColor, True)
            Me.DoubleBuffered = True
            Me.FlatStyle = FlatStyle.Flat
            Me.FlatAppearance.BorderSize = 0
            Me.Cursor = Cursors.Hand
            Me.BackColor = Color.White
        End Sub

        Protected Overrides ReadOnly Property ShowFocusCues As Boolean
            Get
                Return False
            End Get
        End Property

        Protected Overrides Sub OnSizeChanged(e As EventArgs)
            MyBase.OnSizeChanged(e)
            UpdateControlRegion()
        End Sub

        Private Sub UpdateControlRegion()
            If Me.Width <= 0 OrElse Me.Height <= 0 Then Return
            Dim rect As New Rectangle(0, 0, Me.Width, Me.Height)
            Using path = CreateRoundedRectanglePath(rect, CornerRadius)
                If Me.Region IsNot Nothing Then
                    Me.Region.Dispose()
                End If
                Me.Region = New Region(path)
            End Using
        End Sub

        Protected Overrides Sub OnMouseEnter(e As EventArgs)
            _isHovered = True
            Me.Invalidate()
            MyBase.OnMouseEnter(e)
        End Sub

        Protected Overrides Sub OnMouseLeave(e As EventArgs)
            _isHovered = False
            Me.Invalidate()
            MyBase.OnMouseLeave(e)
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            Dim g = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.TextRenderingHint = Drawing.Text.TextRenderingHint.ClearTypeGridFit

            ' Fill parent container background first to prevent any edge artifacts
            Dim bgCol = If(Me.Parent IsNot Nothing, Me.Parent.BackColor, Color.White)
            g.Clear(bgCol)

            Dim rect = Me.ClientRectangle
            rect.Width -= 1
            rect.Height -= 1

            Using path = CreateRoundedRectanglePath(rect, CornerRadius)
                If Me.Enabled Then
                    ' Active Linear Gradient Fill
                    Dim colStart = If(_isHovered, ControlPaint.Light(EnabledGradientStart, 0.15!), EnabledGradientStart)
                    Dim colEnd = If(_isHovered, ControlPaint.Light(EnabledGradientEnd, 0.15!), EnabledGradientEnd)

                    Using br As New LinearGradientBrush(rect, colStart, colEnd, LinearGradientMode.Vertical)
                        g.FillPath(br, path)
                    End Using

                    ' Subtle Top Edge Glass Reflection Highlight
                    Using penHighlight As New Pen(Color.FromArgb(80, 255, 255, 255), 1.5!)
                        g.DrawLine(penHighlight, rect.X + CornerRadius, rect.Y + 2, rect.Right - CornerRadius, rect.Y + 2)
                    End Using

                    ' Draw Bold Primary Icon + Text
                    Using brText As New SolidBrush(EnabledTextColor)
                        Using fontMain As New Font("Segoe UI", 11.0!, FontStyle.Bold)
                            Using sfMain As New StringFormat() With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Near}
                                Dim mainRect As New Rectangle(rect.X, rect.Y + 12, rect.Width, 24)
                                g.DrawString($"{IconSymbol}  {Me.Text}", fontMain, brText, mainRect, sfMain)
                            End Using
                        End Using
                    End Using

                    ' Draw Subtitle Text
                    Using brSub As New SolidBrush(Color.FromArgb(235, 255, 255, 255))
                        Using fontSub As New Font("Segoe UI", 8.0!, FontStyle.Bold)
                            Using sfSub As New StringFormat() With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Near}
                                Dim subRect As New Rectangle(rect.X, rect.Y + 38, rect.Width, 18)
                                g.DrawString(SubText, fontSub, brSub, subRect, sfSub)
                            End Using
                        End Using
                    End Using
                Else
                    ' High-Contrast Premium Slate Container for Disabled / Inactive State
                    Using br As New SolidBrush(Color.FromArgb(248, 250, 252))
                        g.FillPath(br, path)
                    End Using
                    Using pen As New Pen(Color.FromArgb(203, 213, 225), 1.5!)
                        g.DrawPath(pen, path)
                    End Using

                    ' Sharp Dark Slate Text for Inactive State (Never washed out!)
                    Using brText As New SolidBrush(Color.FromArgb(71, 85, 105))
                        Using fontMain As New Font("Segoe UI", 10.5!, FontStyle.Bold)
                            Using sfMain As New StringFormat() With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Near}
                                Dim mainRect As New Rectangle(rect.X, rect.Y + 12, rect.Width, 24)
                                g.DrawString($"🔒 {Me.Text}", fontMain, brText, mainRect, sfMain)
                            End Using
                        End Using
                    End Using

                    ' Inactive Subtext
                    Using brSub As New SolidBrush(Color.FromArgb(148, 163, 184))
                        Using fontSub As New Font("Segoe UI", 8.0!, FontStyle.Bold)
                            Using sfSub As New StringFormat() With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Near}
                                Dim subRect As New Rectangle(rect.X, rect.Y + 38, rect.Width, 18)
                                g.DrawString(SubText, fontSub, brSub, subRect, sfSub)
                            End Using
                        End Using
                    End Using
                End If
            End Using
        End Sub

        Private Function CreateRoundedRectanglePath(rect As Rectangle, radius As Integer) As GraphicsPath
            Dim path As New GraphicsPath()
            If radius <= 0 Then
                path.AddRectangle(rect)
                Return path
            End If

            Dim diameter = radius * 2
            Dim arc = New Rectangle(rect.X, rect.Y, diameter, diameter)

            ' Top Left Arc
            path.AddArc(arc, 180, 90)
            ' Top Right Arc
            arc.X = rect.Right - diameter
            path.AddArc(arc, 270, 90)
            ' Bottom Right Arc
            arc.Y = rect.Bottom - diameter
            path.AddArc(arc, 0, 90)
            ' Bottom Left Arc
            arc.X = rect.Left
            path.AddArc(arc, 90, 90)

            path.CloseFigure()
            Return path
        End Function
    End Class

    ''' <summary>
    ''' Ultra-High-Quality GDI+ Anti-Aliased Status Badge Pill Control.
    ''' Replaces cheap pixelated emoji text labels with high-end SaaS vector status pills.
    ''' </summary>
    Public Class StatusBadgePill
        Inherits Control

        Private _badgeText As String = "Not Punched In"
        Private _badgeColor As Color = Color.FromArgb(220, 38, 38)
        Private _badgeBackColor As Color = Color.FromArgb(254, 242, 242)
        Private _badgeBorderColor As Color = Color.FromArgb(254, 202, 202)

        Public Property BadgeText As String
            Get
                Return _badgeText
            End Get
            Set(value As String)
                ' Clean out any text emojis if passed
                _badgeText = If(value IsNot Nothing, value.Replace("🟢", "").Replace("🔴", "").Replace("🟡", "").Trim(), "")
                Me.Invalidate()
            End Set
        End Property

        Public Property BadgeColor As Color
            Get
                Return _badgeColor
            End Get
            Set(value As Color)
                _badgeColor = value
                Me.Invalidate()
            End Set
        End Property

        Public Property BadgeBackColor As Color
            Get
                Return _badgeBackColor
            End Get
            Set(value As Color)
                _badgeBackColor = value
                Me.Invalidate()
            End Set
        End Property

        Public Property BadgeBorderColor As Color
            Get
                Return _badgeBorderColor
            End Get
            Set(value As Color)
                _badgeBorderColor = value
                Me.Invalidate()
            End Set
        End Property

        Public Sub New()
            Me.DoubleBuffered = True
            Me.SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.SupportsTransparentBackColor, True)
            Me.Size = New Size(150, 24)
            Me.BackColor = Color.White
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            Dim g = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.TextRenderingHint = Drawing.Text.TextRenderingHint.ClearTypeGridFit

            Dim bgCol = If(Me.Parent IsNot Nothing, Me.Parent.BackColor, Color.White)
            g.Clear(bgCol)

            Dim rect As New Rectangle(0, 0, Me.Width - 1, Me.Height - 1)
            Using path = CreateRoundedPath(rect, 12)
                Using brBg As New SolidBrush(_badgeBackColor)
                    g.FillPath(brBg, path)
                End Using
                Using penB As New Pen(_badgeBorderColor, 1.5!)
                    g.DrawPath(penB, path)
                End Using

                ' Vector status pulsing circle dot
                Using brDot As New SolidBrush(_badgeColor)
                    g.FillEllipse(brDot, 10, (Me.Height \ 2) - 4, 8, 8)
                End Using

                ' Crisp bold typography
                Using brText As New SolidBrush(_badgeColor)
                    Using fontT As New Font("Segoe UI", 9.0!, FontStyle.Bold)
                        Dim textRect As New Rectangle(24, 0, Me.Width - 28, Me.Height)
                        Using sf As New StringFormat() With {.Alignment = StringAlignment.Near, .LineAlignment = StringAlignment.Center}
                            g.DrawString(_badgeText, fontT, brText, textRect, sf)
                        End Using
                    End Using
                End Using
            End Using
        End Sub

        Private Function CreateRoundedPath(rect As Rectangle, radius As Integer) As GraphicsPath
            Dim path As New GraphicsPath()
            Dim d = radius * 2
            path.AddArc(rect.X, rect.Y, d, d, 180, 90)
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90)
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90)
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90)
            path.CloseFigure()
            Return path
        End Function
    End Class

    ''' <summary>
    ''' Action-First Staff Attendance Terminal UserControl with Top-Notch Modern Enterprise UI.
    ''' Contains zero business or calculation logic. Delegates all workday metrics, expected exit times, 
    ''' manual break tracking, and progress ratios to AttendancePolicyEngine and AttendanceUiPresenter.
    ''' </summary>
    Public Class AttendanceControl
        Inherits UserControl

        Private ReadOnly _attendanceService As IAttendanceService
        Private ReadOnly _policyEngine As IAttendancePolicyEngine
        Private ReadOnly _policyProvider As IPolicyProvider

        Private _currentUserId As Integer = 1
        Private _todayRecord As AttendanceDto = Nothing
        Private _lastResult As WorkdayProgressResult = Nothing
        Private _allHistory As List(Of AttendanceDto) = New List(Of AttendanceDto)()

        Private ReadOnly tmrClock As Timer

        ' Header Controls
        Private pnlHeader As Panel
        Private lblTitle As Label
        Private lblCurrentUser As Label
        Private lblStreakPill As Label
        Private lblLiveClockAndDate As Label

        ' Action & Status Panel
        Private pnlActionBox As Panel
        Private lblStatusHeader As Label
        Private pnlStatusPill As StatusBadgePill
        Private lblShiftInfo As Label
        Private lblCurrentStatusSubText As Label
        Private lblNextActionVal As Label

        ' Hero Working Time Container
        Private pnlHeroTimer As Panel
        Private lblTimeHeader As Label
        Private lblWorkingHoursVal As Label

        Private lblPunchInVal As Label
        Private lblPunchOutVal As Label
        Private lblAttendanceStatusVal As Label

        ' Workday Policy & Progress Controls
        Private pbWorkdayProgress As ProgressBar
        Private lblProgressPercentage As Label
        Private pnlMetricsBar As FlowLayoutPanel
        Private lblGrossTimeVal As Label
        Private lblLunchDeductionVal As Label
        Private lblRemainingTimeVal As Label
        Private lblExpectedExitVal As Label
        Private lblOvertimeVal As Label
        Private lblLunchInfo As Label

        ' Dynamic Collection-Driven Timeline Card Controls
        Private pnlTimelineBox As Panel
        Private lblTimelineTitle As Label
        Private pnlTimelineItemsHost As FlowLayoutPanel

        ' Primary High-Impact Custom Action Buttons
        Private btnPunchIn As ActionPunchButton
        Private btnPunchBreak As ActionPunchButton
        Private btnPunchOut As ActionPunchButton
        Private lblDisabledReason As Label

#If DEBUG Then
        ' Developer Reset Button compiled ONLY in DEBUG build (hidden in Production)
        Private btnDevReset As Button
#End If

        ' Data Grid Container & Monthly Filter Toolbar
        Private pnlGridBox As Panel
        Private lblGridHeader As Label
        Private pnlGridToolbar As Panel
        Private lblFilterMonthLabel As Label
        Private dtpMonthFilter As DateTimePicker
        Private lblFilterStatusLabel As Label
        Private cmbStatusFilter As ComboBox
        Private btnExportPunchReport As Button
        Private lblMonthlySummaryStats As Label
        Private dgvAttendance As DataGridView

        Public Sub New()
            Dim config As IAppConfiguration = New AppConfiguration()
            Dim connFactory As IDatabaseConnectionFactory = New DbConnectionFactory(config)
            Dim sqlHelper As ISqlHelper = New SqlHelper(connFactory)
            Dim appLogger As IAppLogger = New AppLogger(config)
            Dim auditLogger As New AuditLogger(sqlHelper)
            Dim attendanceRepo As DAL.Interfaces.IAttendanceRepository = New AttendanceRepository(sqlHelper)
            Dim userRepo As DAL.Interfaces.IUserRepository = New UserRepository(sqlHelper)

            _attendanceService = New AttendanceService(attendanceRepo, userRepo, appLogger, auditLogger)
            _policyEngine = New AttendancePolicyEngine()
            _policyProvider = New DefaultAttendancePolicyProvider()

            If CurrentUserContext.IsAuthenticated Then
                _currentUserId = CurrentUserContext.CurrentUser.UserId
            End If

            tmrClock = New Timer() With {.Interval = 1000}
            AddHandler tmrClock.Tick, AddressOf tmrClock_Tick

            InitializeComponent()
            tmrClock.Start()

            RefreshAttendanceScreenAsync()
        End Sub

        Private Sub InitializeComponent()
            Me.SuspendLayout()
            Me.Dock = DockStyle.Fill
            Me.BackColor = ThemeConstants.WorkspaceBackground
            Me.Padding = New Padding(16)
            Me.AutoScroll = False

            ' -----------------------------------------------------------------
            ' 1. Top Header Panel (Title + User Context + Active Streak + Live Clock)
            ' -----------------------------------------------------------------
            pnlHeader = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 52,
                .BackColor = Color.White,
                .BorderStyle = BorderStyle.FixedSingle,
                .Padding = New Padding(12, 8, 12, 8),
                .Margin = New Padding(0, 0, 0, 12)
            }

            lblTitle = New Label() With {
                .Text = "⏱️ Staff Attendance Terminal",
                .Font = New Font(ThemeConstants.FontNameDefault, 12.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(12, 13),
                .AutoSize = True
            }

            lblCurrentUser = New Label() With {
                .Text = "👤 Staff: Active User",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextSecondary,
                .Location = New Point(275, 16),
                .AutoSize = True
            }

            lblStreakPill = New Label() With {
                .Text = "🔥 7-Day Active Streak!",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .BackColor = Color.FromArgb(254, 243, 199),
                .ForeColor = Color.FromArgb(180, 83, 9),
                .Padding = New Padding(6, 3, 6, 3),
                .Location = New Point(475, 14),
                .AutoSize = True
            }

            lblLiveClockAndDate = New Label() With {
                .Text = $"Today: {DateTime.Now:dddd, dd MMM yyyy}    {DateTime.Now:hh:mm:ss tt}",
                .Font = New Font(ThemeConstants.FontNameDefault, 11.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.PrimaryAccent,
                .Dock = DockStyle.Right,
                .AutoSize = True,
                .TextAlign = ContentAlignment.MiddleRight
            }

            pnlHeader.Controls.Add(lblLiveClockAndDate)
            pnlHeader.Controls.Add(lblStreakPill)
            pnlHeader.Controls.Add(lblCurrentUser)
            pnlHeader.Controls.Add(lblTitle)

            ' -----------------------------------------------------------------
            ' 2. Action & Status Hero Box (Buttons + Shift Info + Policy Metrics + Progress + Timeline)
            ' -----------------------------------------------------------------
            pnlActionBox = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 265,
                .BackColor = Color.White,
                .BorderStyle = BorderStyle.FixedSingle,
                .Padding = New Padding(16),
                .Margin = New Padding(0, 10, 0, 12)
            }

            ' Current Status Header & Status Badge Pill
            lblStatusHeader = New Label() With {
                .Text = "Current Status :",
                .Font = New Font(ThemeConstants.FontNameDefault, 10.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextSecondary,
                .Location = New Point(16, 10),
                .AutoSize = True
            }

            pnlStatusPill = New StatusBadgePill() With {
                .BadgeText = "Not Punched In",
                .BadgeColor = ThemeConstants.DangerRed,
                .BadgeBackColor = Color.FromArgb(254, 242, 242),
                .BadgeBorderColor = Color.FromArgb(254, 202, 202),
                .Location = New Point(135, 7),
                .Size = New Size(150, 24)
            }

            lblShiftInfo = New Label() With {
                .Text = "📅 Standard Shift: 09:30 AM – 06:30 PM (9.0h) | Grace: 15 Mins",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextMuted,
                .Location = New Point(300, 11),
                .AutoSize = True
            }

            lblCurrentStatusSubText = New Label() With {
                .Text = "Your attendance has not started today.",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular),
                .ForeColor = ThemeConstants.TextSecondary,
                .Location = New Point(16, 32),
                .AutoSize = True
            }

            lblNextActionVal = New Label() With {
                .Text = "Next Action: Click Punch In to begin today's attendance.",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.PrimaryAccent,
                .Location = New Point(16, 50),
                .AutoSize = True
            }

#If DEBUG Then
            ' Developer Reset Button compiled ONLY in DEBUG builds
            btnDevReset = New Button() With {
                .Text = "🔄 Reset Today (Dev)",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.0!, FontStyle.Bold),
                .BackColor = Color.FromArgb(254, 243, 199),
                .ForeColor = Color.FromArgb(180, 83, 9),
                .FlatStyle = FlatStyle.Flat,
                .Size = New Size(135, 24),
                .Location = New Point(505, 8),
                .Cursor = Cursors.Hand
            }
            btnDevReset.FlatAppearance.BorderColor = Color.FromArgb(245, 158, 11)
            AddHandler btnDevReset.Click, AddressOf btnDevReset_Click
            pnlActionBox.Controls.Add(btnDevReset)
#End If

            ' Premium Dark Hero Working Time Container (#0F172A Slate Dark)
            pnlHeroTimer = New Panel() With {
                .Location = New Point(16, 68),
                .Size = New Size(145, 65),
                .BackColor = Color.FromArgb(15, 23, 42),
                .BorderStyle = BorderStyle.FixedSingle
            }

            lblTimeHeader = New Label() With {
                .Text = "NET WORKING TIME",
                .Font = New Font(ThemeConstants.FontNameDefault, 7.5!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(148, 163, 184),
                .Location = New Point(0, 6),
                .Size = New Size(143, 14),
                .TextAlign = ContentAlignment.MiddleCenter
            }

            lblWorkingHoursVal = New Label() With {
                .Text = "00:00:00",
                .Font = New Font("Consolas", 18.0!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(56, 189, 248), ' Sky Blue
                .Location = New Point(0, 22),
                .Size = New Size(143, 36),
                .TextAlign = ContentAlignment.MiddleCenter
            }

            pnlHeroTimer.Controls.Add(lblTimeHeader)
            pnlHeroTimer.Controls.Add(lblWorkingHoursVal)

            ' Hidden compatibility labels
            lblPunchInVal = New Label() With {.Visible = False}
            lblPunchOutVal = New Label() With {.Visible = False}
            lblAttendanceStatusVal = New Label() With {.Visible = False}

            ' 3 Custom Owner-Drawn High-Resolution Action Buttons
            btnPunchIn = New ActionPunchButton() With {
                .Text = "PUNCH IN",
                .IconSymbol = "▶",
                .SubText = "Clock In Shift",
                .EnabledGradientStart = Color.FromArgb(16, 185, 129),
                .EnabledGradientEnd = Color.FromArgb(4, 120, 87),
                .Size = New Size(152, 65),
                .Location = New Point(170, 68)
            }

            btnPunchBreak = New ActionPunchButton() With {
                .Text = "LUNCH OUT",
                .IconSymbol = "☕",
                .SubText = "Start Lunch Break",
                .EnabledGradientStart = Color.FromArgb(245, 158, 11),
                .EnabledGradientEnd = Color.FromArgb(180, 83, 9),
                .Size = New Size(152, 65),
                .Location = New Point(330, 68)
            }

            btnPunchOut = New ActionPunchButton() With {
                .Text = "PUNCH OUT",
                .IconSymbol = "⏹",
                .SubText = "Clock Out Shift",
                .EnabledGradientStart = Color.FromArgb(239, 68, 68),
                .EnabledGradientEnd = Color.FromArgb(185, 28, 28),
                .Size = New Size(152, 65),
                .Location = New Point(490, 68)
            }

            ' Non-Overlapping Metrics FlowContainer (Zero Overlap Guaranteed)
            pnlMetricsBar = New FlowLayoutPanel() With {
                .Location = New Point(16, 142),
                .Size = New Size(635, 22),
                .BackColor = Color.Transparent,
                .WrapContents = False,
                .AutoScroll = False
            }

            lblGrossTimeVal = New Label() With {.Text = "Gross: 00:00:00", .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold), .ForeColor = ThemeConstants.TextSecondary, .AutoSize = True, .Margin = New Padding(0, 0, 14, 0)}
            lblLunchDeductionVal = New Label() With {.Text = "Lunch: 00:00:00", .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold), .ForeColor = ThemeConstants.TextSecondary, .AutoSize = True, .Margin = New Padding(0, 0, 14, 0)}
            lblRemainingTimeVal = New Label() With {.Text = "Remaining: 08:00:00", .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold), .ForeColor = ThemeConstants.PrimaryAccent, .AutoSize = True, .Margin = New Padding(0, 0, 14, 0)}
            lblExpectedExitVal = New Label() With {.Text = "Expected Exit: --", .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold), .ForeColor = ThemeConstants.TextPrimary, .AutoSize = True, .Margin = New Padding(0, 0, 14, 0)}
            lblOvertimeVal = New Label() With {.Text = "Overtime: 00:00:00", .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold), .ForeColor = Color.FromArgb(217, 119, 6), .AutoSize = True, .Margin = New Padding(0, 0, 0, 0)}

            pnlMetricsBar.Controls.Add(lblGrossTimeVal)
            pnlMetricsBar.Controls.Add(lblLunchDeductionVal)
            pnlMetricsBar.Controls.Add(lblRemainingTimeVal)
            pnlMetricsBar.Controls.Add(lblExpectedExitVal)
            pnlMetricsBar.Controls.Add(lblOvertimeVal)

            ' Daily Target Progress Bar
            pbWorkdayProgress = New ProgressBar() With {
                .Minimum = 0,
                .Maximum = 100,
                .Value = 0,
                .Location = New Point(16, 168),
                .Size = New Size(465, 16),
                .Style = ProgressBarStyle.Blocks
            }

            lblProgressPercentage = New Label() With {
                .Text = "0% (0h 0m / 8h 0m)",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(490, 168),
                .AutoSize = True
            }

            lblLunchInfo = New Label() With {
                .Text = "Lunch Break: 02:00 PM – 03:00 PM (Manual Lunch Out / In active)",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Regular),
                .ForeColor = ThemeConstants.TextMuted,
                .Location = New Point(16, 188),
                .AutoSize = True
            }

            ' Explanation Banner Label
            lblDisabledReason = New Label() With {
                .Text = "👉 Click PUNCH IN to start your workday.",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(71, 85, 105),
                .Location = New Point(16, 210),
                .Size = New Size(625, 20)
            }

            AddHandler btnPunchIn.Click, AddressOf btnPunchIn_Click
            AddHandler btnPunchBreak.Click, AddressOf btnPunchBreak_Click
            AddHandler btnPunchOut.Click, AddressOf btnPunchOut_Click

            ' -----------------------------------------------------------------
            ' Dynamic Collection-Driven Timeline Card Panel (Right Side Docked cleanly)
            ' -----------------------------------------------------------------
            pnlTimelineBox = New Panel() With {
                .Location = New Point(665, 12),
                .Size = New Size(255, 238),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right,
                .BackColor = Color.FromArgb(248, 250, 252),
                .BorderStyle = BorderStyle.FixedSingle,
                .Padding = New Padding(10)
            }

            lblTimelineTitle = New Label() With {
                .Text = "Today's Timeline",
                .Font = New Font(ThemeConstants.FontNameDefault, 10.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(10, 8),
                .AutoSize = True
            }

            pnlTimelineItemsHost = New FlowLayoutPanel() With {
                .Location = New Point(8, 30),
                .Size = New Size(237, 198),
                .AutoScroll = True,
                .FlowDirection = FlowDirection.TopDown,
                .WrapContents = False,
                .BackColor = Color.Transparent
            }

            pnlTimelineBox.Controls.Add(lblTimelineTitle)
            pnlTimelineBox.Controls.Add(pnlTimelineItemsHost)

            AddHandler pnlActionBox.Resize, Sub(s, e)
                                                If pnlTimelineBox IsNot Nothing AndAlso pnlActionBox IsNot Nothing Then
                                                    pnlTimelineBox.Location = New Point(Math.Max(665, pnlActionBox.Width - pnlTimelineBox.Width - 16), 12)
                                                End If
                                            End Sub

            pnlActionBox.Controls.Add(lblStatusHeader)
            pnlActionBox.Controls.Add(pnlStatusPill)
            pnlActionBox.Controls.Add(lblShiftInfo)
            pnlActionBox.Controls.Add(lblCurrentStatusSubText)
            pnlActionBox.Controls.Add(lblNextActionVal)
            pnlActionBox.Controls.Add(pnlHeroTimer)
            pnlActionBox.Controls.Add(btnPunchIn)
            pnlActionBox.Controls.Add(btnPunchBreak)
            pnlActionBox.Controls.Add(btnPunchOut)
            pnlActionBox.Controls.Add(pnlMetricsBar)
            pnlActionBox.Controls.Add(pbWorkdayProgress)
            pnlActionBox.Controls.Add(lblProgressPercentage)
            pnlActionBox.Controls.Add(lblLunchInfo)
            pnlActionBox.Controls.Add(pnlTimelineBox)
            pnlActionBox.Controls.Add(lblDisabledReason)

            ' -----------------------------------------------------------------
            ' 3. Attendance Log History Grid Panel & Filter Toolbar
            ' -----------------------------------------------------------------
            pnlGridBox = New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = Color.White,
                .BorderStyle = BorderStyle.FixedSingle,
                .Margin = New Padding(0, 12, 0, 0)
            }

            lblGridHeader = New Label() With {
                .Text = "📅 Monthly Attendance History & Punch Logs",
                .Font = New Font(ThemeConstants.FontNameDefault, 10.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Dock = DockStyle.Top,
                .Height = 32,
                .Padding = New Padding(12, 7, 0, 0)
            }

            pnlGridToolbar = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 40,
                .BackColor = Color.FromArgb(248, 250, 252),
                .Padding = New Padding(8, 6, 8, 6)
            }

            lblFilterMonthLabel = New Label() With {
                .Text = "Month:",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextSecondary,
                .Location = New Point(10, 10),
                .AutoSize = True
            }

            dtpMonthFilter = New DateTimePicker() With {
                .Format = DateTimePickerFormat.Custom,
                .CustomFormat = "MMMM yyyy",
                .ShowUpDown = True,
                .Value = DateTime.Today,
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular),
                .Location = New Point(60, 7),
                .Width = 140
            }

            lblFilterStatusLabel = New Label() With {
                .Text = "Status:",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextSecondary,
                .Location = New Point(215, 10),
                .AutoSize = True
            }

            cmbStatusFilter = New ComboBox() With {
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular),
                .Location = New Point(268, 7),
                .Width = 120
            }
            cmbStatusFilter.Items.AddRange(New Object() {"All Statuses", "Present", "Late", "Early Leave", "Late & Early"})
            cmbStatusFilter.SelectedIndex = 0

            btnExportPunchReport = New Button() With {
                .Text = "📥 Export Punch Report (CSV)",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .BackColor = Color.FromArgb(16, 185, 129),
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Location = New Point(400, 6),
                .Size = New Size(195, 26),
                .Cursor = Cursors.Hand
            }
            btnExportPunchReport.FlatAppearance.BorderSize = 0

            lblMonthlySummaryStats = New Label() With {
                .Text = "Present: 0 Days | Late: 0 Days | Total Net: 0.0 hrs",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.PrimaryAccent,
                .Location = New Point(610, 10),
                .AutoSize = True
            }

            AddHandler dtpMonthFilter.ValueChanged, Sub(s, e) ApplyGridFilters()
            AddHandler cmbStatusFilter.SelectedIndexChanged, Sub(s, e) ApplyGridFilters()
            AddHandler btnExportPunchReport.Click, AddressOf btnExportPunchReport_Click

            pnlGridToolbar.Controls.Add(lblMonthlySummaryStats)
            pnlGridToolbar.Controls.Add(btnExportPunchReport)
            pnlGridToolbar.Controls.Add(cmbStatusFilter)
            pnlGridToolbar.Controls.Add(lblFilterStatusLabel)
            pnlGridToolbar.Controls.Add(dtpMonthFilter)
            pnlGridToolbar.Controls.Add(lblFilterMonthLabel)

            dgvAttendance = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                .BackgroundColor = Color.White,
                .BorderStyle = BorderStyle.None,
                .ReadOnly = True,
                .AllowUserToAddRows = False,
                .RowHeadersVisible = False,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect
            }
            ThemeConstants.ApplyModernGridStyle(dgvAttendance)
            AddHandler dgvAttendance.CellFormatting, AddressOf dgvAttendance_CellFormatting

            pnlGridBox.Controls.Add(dgvAttendance)
            pnlGridBox.Controls.Add(pnlGridToolbar)
            pnlGridBox.Controls.Add(lblGridHeader)

            Me.Controls.Add(pnlGridBox)
            Me.Controls.Add(pnlActionBox)
            Me.Controls.Add(pnlHeader)

            Me.ResumeLayout(False)
        End Sub

        Public Shared Sub ApplyDataGridTheme(dgv As DataGridView)
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252)
            dgv.RowsDefaultCellStyle.BackColor = Color.White
            dgv.RowsDefaultCellStyle.Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular)
            dgv.RowsDefaultCellStyle.ForeColor = ThemeConstants.TextPrimary

            dgv.ColumnHeadersDefaultCellStyle.Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold)
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59)
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
            dgv.ColumnHeadersHeight = 38
            dgv.EnableHeadersVisualStyles = False

            dgv.RowTemplate.Height = 38
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
            dgv.GridColor = Color.FromArgb(226, 232, 240)

            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 231, 255)
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42)
        End Sub

        Private Async Sub tmrClock_Tick(sender As Object, e As EventArgs)
            Dim now = DateTime.Now
            lblLiveClockAndDate.Text = $"Today: {now:dddd, dd MMM yyyy}    {now:hh:mm:ss tt}"

            ' Dumb UI: Delegate 100% calculation to AttendancePolicyEngine
            Dim policy = Await _policyProvider.GetEffectivePolicyAsync(_currentUserId, DateTime.Today)
            Dim clockIn = If(_todayRecord IsNot Nothing, CType(_todayRecord.ClockInTime, DateTime?), Nothing)
            Dim clockOut = If(_todayRecord IsNot Nothing, _todayRecord.ClockOutTime, Nothing)
            Dim breakStart = If(_todayRecord IsNot Nothing, _todayRecord.BreakStartTime, Nothing)
            Dim totalBreak = If(_todayRecord IsNot Nothing, _todayRecord.TotalBreakMinutes, 0)

            _lastResult = _policyEngine.CalculateProgress(clockIn, clockOut, now, policy, breakStart, totalBreak)
            BindWorkdayProgressUI(_lastResult)
        End Sub

        Private Async Sub RefreshAttendanceScreenAsync()
            Try
                Me.Cursor = Cursors.WaitCursor

                If CurrentUserContext.IsAuthenticated Then
                    _currentUserId = CurrentUserContext.CurrentUser.UserId
                    lblCurrentUser.Text = $"👤 Staff: {CurrentUserContext.CurrentUser.FullName}"
                Else
                    lblCurrentUser.Text = "👤 Staff: Active User"
                End If

                ' 1. Load Today's Attendance Record for Current User
                _todayRecord = Await _attendanceService.GetTodayAttendanceForUserAsync(_currentUserId)

                ' 2. Calculate Workday Progress Facts via Policy Engine
                Dim policy = Await _policyProvider.GetEffectivePolicyAsync(_currentUserId, DateTime.Today)
                Dim clockIn = If(_todayRecord IsNot Nothing, CType(_todayRecord.ClockInTime, DateTime?), Nothing)
                Dim clockOut = If(_todayRecord IsNot Nothing, _todayRecord.ClockOutTime, Nothing)
                Dim breakStart = If(_todayRecord IsNot Nothing, _todayRecord.BreakStartTime, Nothing)
                Dim totalBreak = If(_todayRecord IsNot Nothing, _todayRecord.TotalBreakMinutes, 0)

                _lastResult = _policyEngine.CalculateProgress(clockIn, clockOut, DateTime.Now, policy, breakStart, totalBreak)
                BindWorkdayProgressUI(_lastResult)

                ' 3. Load Personal Attendance History Grid & Cache for Filters
                _allHistory = Await _attendanceService.GetUserAttendanceHistoryAsync(_currentUserId)
                ApplyGridFilters()
            Catch ex As Exception
                pnlStatusPill.BadgeText = "Error Loading Status"
                pnlStatusPill.BadgeColor = ThemeConstants.DangerRed
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Sub

        Private Sub ApplyGridFilters()
            If _allHistory Is Nothing Then Return

            Dim selDate = dtpMonthFilter.Value
            Dim selStatus = If(cmbStatusFilter.SelectedItem IsNot Nothing, cmbStatusFilter.SelectedItem.ToString(), "All Statuses")

            Dim filtered = _allHistory.Where(Function(x) x.AttendanceDate.Year = selDate.Year AndAlso x.AttendanceDate.Month = selDate.Month).ToList()

            If selStatus <> "All Statuses" Then
                filtered = filtered.Where(Function(x) String.Equals(x.Status, selStatus, StringComparison.OrdinalIgnoreCase)).ToList()
            End If

            Dim dt As New DataTable()
            dt.Columns.Add("Date")
            dt.Columns.Add("Punch In")
            dt.Columns.Add("Punch Out")
            dt.Columns.Add("Working Hours")
            dt.Columns.Add("Status")

            Dim totalPresent As Integer = 0
            Dim totalLate As Integer = 0
            Dim totalWorkedMinutes As Integer = 0

            For Each item In filtered
                Dim pIn = item.ClockInTime.ToString("hh:mm tt")
                Dim pOut = If(item.ClockOutTime.HasValue, item.ClockOutTime.Value.ToString("hh:mm tt"), "--")
                Dim hrsFormatted = If(item.ClockOutTime.HasValue, AttendanceUiPresenter.FormatHoursShort(item.TotalWorkingMinutes), "--")
                dt.Rows.Add(item.AttendanceDate.ToString("dd-MMM-yyyy"), pIn, pOut, hrsFormatted, item.Status)

                If item.Status = "Present" OrElse item.Status = "Late" OrElse item.Status = "Early Leave" OrElse item.Status = "Late & Early" Then
                    totalPresent += 1
                End If
                If item.Status.Contains("Late") Then
                    totalLate += 1
                End If
                totalWorkedMinutes += item.TotalWorkingMinutes
            Next

            dgvAttendance.DataSource = dt

            Dim hrsPart = totalWorkedMinutes \ 60
            Dim minsPart = totalWorkedMinutes Mod 60
            lblMonthlySummaryStats.Text = $"Present: {totalPresent} Days | Late: {totalLate} Days | Total Net: {hrsPart}h {minsPart}m"
        End Sub

        Private Sub btnExportPunchReport_Click(sender As Object, e As EventArgs)
            Try
                Dim selDate = dtpMonthFilter.Value
                Dim monthName = selDate.ToString("MMMM_yyyy")

                Using sfd As New SaveFileDialog()
                    sfd.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*"
                    sfd.FileName = $"Attendance_Report_{monthName}.csv"
                    sfd.Title = "Export Monthly Attendance Punch Report"

                    If sfd.ShowDialog() = DialogResult.OK Then
                        Dim sb As New StringBuilder()
                        sb.AppendLine("STAFF AUTOMATION SYSTEM - MONTHLY ATTENDANCE PUNCH REPORT")
                        sb.AppendLine($"Report Period: {selDate:MMMM yyyy}")
                        sb.AppendLine($"Exported On: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                        sb.AppendLine($"Staff Name: {If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.FullName, "Active Staff User")}")
                        sb.AppendLine()
                        sb.AppendLine("Date,Punch In,Punch Out,Working Hours,Status")

                        If dgvAttendance.DataSource IsNot Nothing AndAlso TypeOf dgvAttendance.DataSource Is DataTable Then
                            Dim dt = CType(dgvAttendance.DataSource, DataTable)
                            For Each row As DataRow In dt.Rows
                                Dim d = row("Date").ToString()
                                Dim pIn = row("Punch In").ToString()
                                Dim pOut = row("Punch Out").ToString()
                                Dim hrs = row("Working Hours").ToString()
                                Dim st = row("Status").ToString()
                                sb.AppendLine(String.Format("""{0}"",""{1}"",""{2}"",""{3}"",""{4}""", d, pIn, pOut, hrs, st))
                            Next
                        End If

                        File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8)

                        Dim shell = TryCast(Me.FindForm(), FrmMainShell)
                        Forms.Common.FrmInAppAlert.ShowModal(shell, "Report Exported", $"Monthly Punch Report successfully exported to:" & vbCrLf & sfd.FileName, Forms.Common.AlertType.SuccessAlert, actionText:="OK")
                    End If
                End Using
            Catch ex As Exception
                Dim shell = TryCast(Me.FindForm(), FrmMainShell)
                Forms.Common.FrmInAppAlert.ShowModal(shell, "Export Error", "Failed to export report: " & ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
            End Try
        End Sub

        ''' <summary>
        ''' Dumb UI Binding Method. Evaluates ZERO business math inside WinForms.
        ''' Consumes WorkdayProgressResult facts and AttendanceUiPresenter presentation model.
        ''' </summary>
        Private Sub BindWorkdayProgressUI(result As WorkdayProgressResult)
            If result Is Nothing Then Return

            ' Map facts to presentation model
            Dim presentation = AttendanceUiPresenter.MapToPresentation(result)

            ' 1. Bind Action Button States with Strict Visual Hierarchy (Primary vs Secondary)
            btnPunchIn.Enabled = result.CanPunchIn
            btnPunchIn.IconSymbol = "▶"
            If result.CanPunchIn Then
                btnPunchIn.SubText = "PRIMARY ACTION"
            ElseIf result.AttendanceState = "Working" Then
                btnPunchIn.SubText = "Shift Active"
            ElseIf result.IsWorkdayCompleted OrElse result.AttendanceState = "DayCompleted" Then
                btnPunchIn.SubText = "Shift Completed"
            Else
                btnPunchIn.SubText = "Click to Start"
            End If

            ' Dynamic Lunch Break Button Text & State Management
            If result.CanPunchBreakIn Then
                ' State: On Lunch Break -> Button becomes RESUME WORK (PRIMARY ACTION)
                btnPunchBreak.Text = "RESUME WORK"
                btnPunchBreak.IconSymbol = "⏯️"
                btnPunchBreak.SubText = "PRIMARY ACTION"
                btnPunchBreak.Enabled = True
                btnPunchBreak.EnabledGradientStart = Color.FromArgb(16, 185, 129)
                btnPunchBreak.EnabledGradientEnd = Color.FromArgb(4, 120, 87)
            Else
                ' State: Working or Not Punched In -> Button becomes LUNCH OUT (Amber)
                btnPunchBreak.Text = "LUNCH OUT"
                btnPunchBreak.IconSymbol = "☕"
                btnPunchBreak.Enabled = result.CanPunchBreakOut
                If result.CanPunchBreakOut Then
                    btnPunchBreak.SubText = "RECOMMENDED ACTION"
                Else
                    btnPunchBreak.SubText = "Optional Break"
                End If
                btnPunchBreak.EnabledGradientStart = Color.FromArgb(245, 158, 11)
                btnPunchBreak.EnabledGradientEnd = Color.FromArgb(180, 83, 9)
            End If

            btnPunchOut.Enabled = result.CanPunchOut
            btnPunchOut.IconSymbol = "⏹"
            If result.CanPunchOut Then
                btnPunchOut.SubText = "Secondary Action"
            ElseIf result.IsWorkdayCompleted OrElse result.AttendanceState = "DayCompleted" Then
                btnPunchOut.SubText = "Day Completed"
            Else
                btnPunchOut.SubText = "End Workday"
            End If

            ' Redraw custom action buttons
            btnPunchIn.Invalidate()
            btnPunchBreak.Invalidate()
            btnPunchOut.Invalidate()

            ' 2. Bind Status & Guidance Labels
            pnlStatusPill.BadgeText = presentation.StatusBadgeText
            pnlStatusPill.BadgeColor = presentation.StatusBadgeColor
            If result.AttendanceState = "Working" Then
                pnlStatusPill.BadgeBackColor = Color.FromArgb(236, 253, 245)
                pnlStatusPill.BadgeBorderColor = Color.FromArgb(167, 243, 208)
            ElseIf result.AttendanceState = "OnBreak" OrElse result.IsOnLunchBreak Then
                pnlStatusPill.BadgeBackColor = Color.FromArgb(254, 243, 199)
                pnlStatusPill.BadgeBorderColor = Color.FromArgb(253, 230, 138)
            ElseIf result.IsWorkdayCompleted OrElse result.AttendanceState = "DayCompleted" Then
                pnlStatusPill.BadgeBackColor = Color.FromArgb(239, 246, 255)
                pnlStatusPill.BadgeBorderColor = Color.FromArgb(191, 219, 254)
            Else
                pnlStatusPill.BadgeBackColor = Color.FromArgb(254, 242, 242)
                pnlStatusPill.BadgeBorderColor = Color.FromArgb(254, 202, 202)
            End If
            lblCurrentStatusSubText.Text = presentation.StatusSubText
            lblNextActionVal.Text = presentation.NextActionText
            lblDisabledReason.Text = presentation.ExplanationBannerText

            ' 3. Bind Hero Timer & Metrics Summary
            lblWorkingHoursVal.Text = presentation.FormattedNetWorked
            lblGrossTimeVal.Text = $"Gross: {presentation.FormattedGrossWorked}"
            lblLunchDeductionVal.Text = $"Lunch: {presentation.FormattedLunchDeducted}"
            lblRemainingTimeVal.Text = $"Remaining: {presentation.FormattedRemaining}"
            lblExpectedExitVal.Text = $"Expected Exit: {presentation.FormattedExpectedExit}"
            lblOvertimeVal.Text = $"Overtime: {presentation.FormattedOvertime}"

            ' 4. Bind Daily Target Progress Bar & Text
            pbWorkdayProgress.Value = presentation.ProgressPercentage
            lblProgressPercentage.Text = presentation.ProgressText
            lblLunchInfo.Text = presentation.LunchStatusText

            ' 5. Collection-Driven Timeline Rendering
            RenderTimelineCollection(result.TimelineItems)

            ' 6. Dynamically update Header Logout button & CurrentUserContext shift state
            Dim isClockedIn = result.ClockInTime.HasValue
            Dim isOnBreak = result.IsOnLunchBreak
            Dim isCompleted = result.IsWorkdayCompleted

            CurrentUserContext.UpdateShiftState(isClockedIn, isOnBreak, isCompleted)

            Dim shell = TryCast(Me.FindForm(), FrmMainShell)
            If shell IsNot Nothing Then
                shell.UpdateLogoutButtonState(isClockedIn, isCompleted)
            End If
        End Sub

        ''' <summary>
        ''' Collection-driven Timeline Renderer. Renders dynamic TimelineItemDto collection cleanly inside 237x198 container.
        ''' </summary>
        Private Sub RenderTimelineCollection(items As List(Of TimelineItemDto))
            pnlTimelineItemsHost.SuspendLayout()
            pnlTimelineItemsHost.Controls.Clear()

            If items IsNot Nothing Then
                For Each item In items
                    Dim pnlItem As New Panel() With {
                        .Size = New Size(220, 40),
                        .Margin = New Padding(0, 0, 0, 4),
                        .BackColor = Color.White,
                        .BorderStyle = BorderStyle.FixedSingle
                    }

                    Dim lblIcon As New Label() With {
                        .Text = item.IconSymbol,
                        .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Regular),
                        .Location = New Point(4, 10),
                        .Size = New Size(20, 18)
                    }

                    Dim lblItemTitle As New Label() With {
                        .Text = item.Title,
                        .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                        .ForeColor = ThemeConstants.TextPrimary,
                        .Location = New Point(26, 3),
                        .AutoSize = True
                    }

                    Dim lblTime As New Label() With {
                        .Text = $"{item.TimeText} ({item.SubText})",
                        .Font = New Font(ThemeConstants.FontNameDefault, 7.5!, FontStyle.Regular),
                        .ForeColor = If(item.IsCompleted, ThemeConstants.SuccessGreen, ThemeConstants.TextMuted),
                        .Location = New Point(26, 20),
                        .AutoSize = True
                    }

                    pnlItem.Controls.Add(lblIcon)
                    pnlItem.Controls.Add(lblItemTitle)
                    pnlItem.Controls.Add(lblTime)
                    pnlTimelineItemsHost.Controls.Add(pnlItem)
                Next
            End If

            pnlTimelineItemsHost.ResumeLayout(True)
        End Sub

        Private Sub dgvAttendance_CellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs)
            If dgvAttendance.Columns(e.ColumnIndex).Name = "Status" AndAlso e.Value IsNot Nothing Then
                Dim statusStr = e.Value.ToString()
                Select Case statusStr
                    Case "Present"
                        e.CellStyle.ForeColor = Color.FromArgb(16, 185, 129)
                        e.CellStyle.Font = New Font(dgvAttendance.Font, FontStyle.Bold)
                    Case "Late"
                        e.CellStyle.ForeColor = Color.FromArgb(217, 119, 6)
                        e.CellStyle.Font = New Font(dgvAttendance.Font, FontStyle.Bold)
                    Case "Early Leave"
                        e.CellStyle.ForeColor = Color.FromArgb(234, 88, 12)
                        e.CellStyle.Font = New Font(dgvAttendance.Font, FontStyle.Bold)
                    Case "Late & Early"
                        e.CellStyle.ForeColor = Color.FromArgb(239, 68, 68)
                        e.CellStyle.Font = New Font(dgvAttendance.Font, FontStyle.Bold)
                End Select
            End If
        End Sub

        Private Async Sub btnPunchIn_Click(sender As Object, e As EventArgs)
            Try
                Me.Cursor = Cursors.WaitCursor
                Await _attendanceService.ClockInUserAsync(_currentUserId, "127.0.0.1")
                RefreshAttendanceScreenAsync()

                Dim shell = TryCast(Me.FindForm(), FrmMainShell)
                If shell IsNot Nothing Then
                    Forms.Common.FrmInAppAlert.ShowModal(shell, "Shift Activated", "Punch In Successful! Welcome to today's shift." & vbCrLf & "Redirecting to your Daily Dashboard...", Forms.Common.AlertType.SuccessAlert, actionText:="CONTINUE TO DASHBOARD")
                    shell.NavigateToModule("Dashboard")
                End If
            Catch ex As BusinessException
                Dim shell = TryCast(Me.FindForm(), FrmMainShell)
                Forms.Common.FrmInAppAlert.ShowModal(shell, "Clock-In Warning", ex.Message, Forms.Common.AlertType.WarningAlert, actionText:="OK")
            Catch ex As Exception
                Dim shell = TryCast(Me.FindForm(), FrmMainShell)
                Forms.Common.FrmInAppAlert.ShowModal(shell, "Error", "Unable to save attendance: " & ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Sub

        Private Async Sub btnPunchBreak_Click(sender As Object, e As EventArgs)
            Try
                Me.Cursor = Cursors.WaitCursor
                If _lastResult IsNot Nothing AndAlso _lastResult.CanPunchBreakIn Then
                    ' Resume Work from Lunch
                    Await _attendanceService.EndLunchBreakAsync(_currentUserId)
                Else
                    ' Start Lunch Break
                    Await _attendanceService.StartLunchBreakAsync(_currentUserId)
                End If
                RefreshAttendanceScreenAsync()
            Catch ex As BusinessException
                Dim shell = TryCast(Me.FindForm(), FrmMainShell)
                Forms.Common.FrmInAppAlert.ShowModal(shell, "Lunch Break Warning", ex.Message, Forms.Common.AlertType.WarningAlert, actionText:="OK")
            Catch ex As Exception
                Dim shell = TryCast(Me.FindForm(), FrmMainShell)
                Forms.Common.FrmInAppAlert.ShowModal(shell, "Error", "Unable to update lunch break status: " & ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Sub

        Private Async Sub btnPunchOut_Click(sender As Object, e As EventArgs)
            Try
                Me.Cursor = Cursors.WaitCursor
                Dim success = Await _attendanceService.ClockOutUserAsync(_currentUserId, totalBreakMinutes:=0)
                If success Then
                    RefreshAttendanceScreenAsync()

                    Dim shell = TryCast(Me.FindForm(), FrmMainShell)
                    If shell IsNot Nothing Then
                        shell.UpdateLogoutButtonState(isClockedIn:=True, isWorkdayCompleted:=True)
                        Forms.Common.FrmInAppAlert.ShowModal(shell, "Shift Ended", "Punch Out Successful! Your shift has ended for today." & vbCrLf & "Logout is now unlocked.", Forms.Common.AlertType.SuccessAlert, actionText:="GREAT — SHIFT COMPLETED")
                    End If
                End If
            Catch ex As BusinessException
                Dim shell = TryCast(Me.FindForm(), FrmMainShell)
                Forms.Common.FrmInAppAlert.ShowModal(shell, "Clock-Out Warning", ex.Message, Forms.Common.AlertType.WarningAlert, actionText:="OK")
            Catch ex As Exception
                Dim shell = TryCast(Me.FindForm(), FrmMainShell)
                Forms.Common.FrmInAppAlert.ShowModal(shell, "Error", "Unable to save attendance: " & ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Sub

#If DEBUG Then
        Private Async Sub btnDevReset_Click(sender As Object, e As EventArgs)
            Dim shell = TryCast(Me.FindForm(), FrmMainShell)
            Dim confirm = Forms.Common.FrmInAppAlert.ShowModal(shell, "Developer Test Reset", "Reset today's attendance record for testing? (DEBUG ONLY)", Forms.Common.AlertType.WarningAlert, actionText:="RESET RECORD", showCancel:=True, cancelText:="CANCEL")
            If confirm = DialogResult.OK Then
                Try
                    Me.Cursor = Cursors.WaitCursor
                    Await _attendanceService.ResetTodayAttendanceAsync(_currentUserId)
                    Forms.Common.FrmInAppAlert.ShowModal(shell, "Dev Reset Complete", "Today's attendance record reset successfully! You can now test Punch In & Punch Out live.", Forms.Common.AlertType.SuccessAlert, actionText:="OK")
                    RefreshAttendanceScreenAsync()
                Catch ex As Exception
                    Forms.Common.FrmInAppAlert.ShowModal(shell, "Error", "Reset failed: " & ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
                Finally
                    Me.Cursor = Cursors.Default
                End Try
            End If
        End Sub
#End If

        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                If tmrClock IsNot Nothing Then
                    tmrClock.Stop()
                    tmrClock.Dispose()
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub
    End Class
End Namespace
