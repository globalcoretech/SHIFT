Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports StaffAutomation.Core.Constants
Imports StaffAutomation.Core.Security

Namespace Forms.Main
    ''' <summary>
    ''' Reusable Welcome Screen UserControl hosted inside pnlWorkspace.
    ''' Provides the modular baseline pattern for all future UserControl view modules.
    ''' </summary>
    Public Class WelcomeControl
        Inherits UserControl

        Private pnlCenterCard As Panel
        Private lblGreetingTime As Label
        Private lblUserName As Label
        Private lblAppSubTitle As Label
        Private pnlMetricsGrid As Panel
        Private pnlDashboardNotice As Panel

        Public Sub New()
            InitializeComponent()
            ApplyStylesAndData()
        End Sub

        Private Sub InitializeComponent()
            Me.SuspendLayout()

            Me.Dock = DockStyle.Fill
            Me.BackColor = ThemeConstants.WorkspaceBackground
            Me.AutoScroll = False

            ' Card Container
            pnlCenterCard = New Panel() With {
                .Size = New Size(860, 560),
                .BackColor = ThemeConstants.CardBackground,
                .BorderStyle = BorderStyle.FixedSingle
            }

            ' 1. Greeting & Title
            lblGreetingTime = New Label() With {
                .Location = New Point(30, 25),
                .Size = New Size(800, 28),
                .Font = New Font(ThemeConstants.FontNameDefault, 13.0!, FontStyle.Regular),
                .ForeColor = ThemeConstants.TextSecondary,
                .TextAlign = ContentAlignment.TopCenter
            }

            lblUserName = New Label() With {
                .Location = New Point(30, 53),
                .Size = New Size(800, 36),
                .Font = New Font(ThemeConstants.FontNameDefault, 18.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.PrimaryAccent,
                .TextAlign = ContentAlignment.TopCenter
            }

            lblAppSubTitle = New Label() With {
                .Location = New Point(30, 92),
                .Size = New Size(800, 22),
                .Font = New Font(ThemeConstants.FontNameDefault, 10.5!, FontStyle.Regular),
                .ForeColor = ThemeConstants.TextMuted,
                .TextAlign = ContentAlignment.TopCenter,
                .Text = "CA Office Workforce Productivity Automation System"
            }

            ' 2. Quick Metrics Panel
            pnlMetricsGrid = New Panel() With {
                .Location = New Point(40, 130),
                .Size = New Size(778, 140),
                .BackColor = Color.FromArgb(248, 250, 252),
                .BorderStyle = BorderStyle.FixedSingle
            }

            BuildMetricsWidgets()

            ' 3. Dashboard Under Construction Card
            pnlDashboardNotice = New Panel() With {
                .Location = New Point(40, 290),
                .Size = New Size(778, 230),
                .BackColor = Color.FromArgb(238, 242, 255),
                .BorderStyle = BorderStyle.FixedSingle
            }

            BuildDashboardNoticeContent()

            pnlCenterCard.Controls.Add(pnlDashboardNotice)
            pnlCenterCard.Controls.Add(pnlMetricsGrid)
            pnlCenterCard.Controls.Add(lblAppSubTitle)
            pnlCenterCard.Controls.Add(lblUserName)
            pnlCenterCard.Controls.Add(lblGreetingTime)

            Me.Controls.Add(pnlCenterCard)

            AddHandler Me.Resize, AddressOf OnControlResize
            Me.ResumeLayout(False)
        End Sub

        Private Sub OnControlResize(sender As Object, e As EventArgs)
            If pnlCenterCard IsNot Nothing Then
                Dim leftPos As Integer = Math.Max(20, (Me.Width - pnlCenterCard.Width) \ 2)
                Dim topPos As Integer = Math.Max(20, (Me.Height - pnlCenterCard.Height) \ 2)
                pnlCenterCard.Location = New Point(leftPos, topPos)
            End If
        End Sub

        Private Sub ApplyStylesAndData()
            Dim user = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser, Nothing)
            Dim userRoleStr = If(user IsNot Nothing, user.Role.ToString(), "Administrator")
            Dim fullNameStr = If(user IsNot Nothing, user.FullName, "System Administrator")

            ' Time-based greeting
            Dim currentHour = DateTime.Now.Hour
            Dim timeGreeting = "Good Morning,"
            If currentHour >= 12 AndAlso currentHour < 17 Then
                timeGreeting = "Good Afternoon,"
            ElseIf currentHour >= 17 Then
                timeGreeting = "Good Evening,"
            End If

            lblGreetingTime.Text = timeGreeting
            lblUserName.Text = fullNameStr
        End Sub

        Private Sub BuildMetricsWidgets()
            Dim user = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser, Nothing)
            Dim roleStr = If(user IsNot Nothing, user.Role.ToString(), "Admin")
            Dim deptStr = If(user IsNot Nothing, user.Department.ToString(), "Administration")

            Dim metricsTable As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 3,
                .RowCount = 2,
                .Padding = New Padding(10)
            }

            metricsTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33.33!))
            metricsTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33.33!))
            metricsTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33.33!))
            metricsTable.RowStyles.Add(New RowStyle(SizeType.Percent, 50.0!))
            metricsTable.RowStyles.Add(New RowStyle(SizeType.Percent, 50.0!))

            metricsTable.Controls.Add(CreateMetricBox("📋 Today's Tasks", "0 Pending"), 0, 0)
            metricsTable.Controls.Add(CreateMetricBox("✅ Approvals", "0 Required"), 1, 0)
            metricsTable.Controls.Add(CreateMetricBox("⏱️ Attendance", "Present"), 2, 0)

            metricsTable.Controls.Add(CreateMetricBox("👤 Logged Role", $"{roleStr} ({deptStr})"), 0, 1)
            metricsTable.Controls.Add(CreateMetricBox("📅 Current FY", "2026-2027"), 1, 1)
            metricsTable.Controls.Add(CreateMetricBox("🔑 Last Login", DateTime.Now.ToString("hh:mm tt")), 2, 1)

            pnlMetricsGrid.Controls.Add(metricsTable)
        End Sub

        Private Function CreateMetricBox(title As String, value As String) As Panel
            Dim pnl As New Panel() With {
                .Dock = DockStyle.Fill,
                .Margin = New Padding(5),
                .BackColor = Color.White,
                .BorderStyle = BorderStyle.FixedSingle
            }

            Dim lblT As New Label() With {
                .Text = title,
                .Font = New Font(ThemeConstants.FontNameDefault, 8.25!, FontStyle.Regular),
                .ForeColor = ThemeConstants.TextSecondary,
                .Dock = DockStyle.Top,
                .Height = 20,
                .Padding = New Padding(5, 3, 0, 0)
            }

            Dim lblV As New Label() With {
                .Text = value,
                .Font = New Font(ThemeConstants.FontNameDefault, 10.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Dock = DockStyle.Fill,
                .Padding = New Padding(5, 0, 0, 0),
                .TextAlign = ContentAlignment.MiddleLeft
            }

            pnl.Controls.Add(lblV)
            pnl.Controls.Add(lblT)
            Return pnl
        End Function

        Private Sub BuildDashboardNoticeContent()
            Dim lblRocket As New Label() With {
                .Text = "🚀",
                .Font = New Font("Segoe UI Emoji", 26.0!, FontStyle.Regular),
                .Size = New Size(778, 45),
                .Location = New Point(0, 12),
                .TextAlign = ContentAlignment.MiddleCenter
            }

            Dim lblDashTitle As New Label() With {
                .Text = "Dashboard • Coming in Phase 07.2",
                .Font = New Font(ThemeConstants.FontNameDefault, 14.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.PrimaryAccent,
                .Size = New Size(778, 30),
                .Location = New Point(0, 58),
                .TextAlign = ContentAlignment.MiddleCenter
            }

            Dim lblFeatures As New Label() With {
                .Text = "Features being prepared for Executive Owner Workspace:" & Environment.NewLine &
                        "• Executive KPIs & Real-time Financial Metrics" & Environment.NewLine &
                        "• Productivity Charts & Team Workload Distribution" & Environment.NewLine &
                        "• Attendance Analytics & Daily Log Summaries" & Environment.NewLine &
                        "• Task Delegation Matrix & Milestone Tracking",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.5!, FontStyle.Regular),
                .ForeColor = ThemeConstants.TextSecondary,
                .Size = New Size(778, 120),
                .Location = New Point(0, 92),
                .TextAlign = ContentAlignment.TopCenter
            }

            pnlDashboardNotice.Controls.Add(lblFeatures)
            pnlDashboardNotice.Controls.Add(lblDashTitle)
            pnlDashboardNotice.Controls.Add(lblRocket)
        End Sub
    End Class
End Namespace
