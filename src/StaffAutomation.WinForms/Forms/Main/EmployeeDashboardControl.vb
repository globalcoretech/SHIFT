Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Linq
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports FontAwesome.Sharp
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
Imports StaffAutomation.DAL.Interfaces
Imports StaffAutomation.DAL.Repositories

Namespace Forms.Main
    Public Class EmployeeDashboardControl
        Inherits UserControl
        Implements IPreloadableScreen

        Private ReadOnly _mainShell As FrmMainShell
        Private ReadOnly _taskService As ITaskManagementService
        Private ReadOnly _attendanceService As IAttendanceService

        Private _activeNavigationToken As Long = 0

        Private pnlHeader As Panel
        Private lblTitle As Label
        Private lblSubtitle As Label

        Private pnlCards As FlowLayoutPanel
        Private cardDueSoon As ElevatedInteractiveCard
        Private cardNeedsAttention As ElevatedInteractiveCard
        Private cardOverdue As ElevatedInteractiveCard
        Private cardCompleted As ElevatedInteractiveCard
        Private cardAttendance As ElevatedInteractiveCard

        Private lblDueSoonVal As Label
        Private lblNeedsAttentionVal As Label
        Private lblOverdueVal As Label
        Private lblCompletedVal As Label
        
        Private lblAttendanceStatus As Label
        Private lblAttendanceAction As Label
        Private _todayRecord As AttendanceDto
        Private _currentUserId As Integer
        
        Public Sub New(Optional mainShell As FrmMainShell = Nothing)
            _mainShell = mainShell

            Dim config As IAppConfiguration = New AppConfiguration()
            Dim connFactory As IDatabaseConnectionFactory = New DbConnectionFactory(config)
            Dim sqlHelper As ISqlHelper = New SqlHelper(connFactory)
            Dim appLogger As IAppLogger = New AppLogger(config)
            Dim auditLogger As New AuditLogger(sqlHelper)
            
            Dim taskRepo As ITaskRepository = New TaskRepository(sqlHelper)
            Dim clientRepo As IClientRepository = New ClientRepository(sqlHelper)
            Dim userRepo As IUserRepository = New UserRepository(sqlHelper)
            Dim workflowEngine As ITaskWorkflowEngine = New TaskWorkflowEngine()
            Dim attendanceRepo As IAttendanceRepository = New AttendanceRepository(sqlHelper)

            _taskService = New TaskManagementService(taskRepo, workflowEngine, appLogger, auditLogger, clientRepo, userRepo)
            _attendanceService = New AttendanceService(attendanceRepo, userRepo, appLogger, auditLogger)

            Me.DoubleBuffered = True
            Me.BackColor = Color.FromArgb(248, 250, 252)
            Me.Padding = New Padding(20)
            
            InitializeUI()
        End Sub

        Private Sub InitializeUI()
            pnlHeader = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 60,
                .BackColor = Color.Transparent
            }

            lblTitle = New Label() With {
                .Text = "My Dashboard",
                .Font = New Font(ThemeConstants.FontNameDefault, 18.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .AutoSize = True,
                .Location = New Point(0, 0)
            }

            lblSubtitle = New Label() With {
                .Text = "Your operational workspace.",
                .Font = New Font(ThemeConstants.FontNameDefault, 10.0!, FontStyle.Regular),
                .ForeColor = ThemeConstants.TextSecondary,
                .AutoSize = True,
                .Location = New Point(2, 32)
            }

            pnlHeader.Controls.Add(lblTitle)
            pnlHeader.Controls.Add(lblSubtitle)

            pnlCards = New FlowLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .BackColor = Color.Transparent,
                .Padding = New Padding(0, 20, 0, 0),
                .WrapContents = True,
                .AutoScroll = True
            }

            cardDueSoon = CreateCard(IconChar.Clock, "DUE SOON", "0", "Tasks", "Assigned to me (7 Days)", Color.FromArgb(14, 165, 233), Color.White, lblDueSoonVal)
            cardNeedsAttention = CreateCard(IconChar.ExclamationCircle, "NEEDS ATTENTION", "0", "Tasks", "Requires my action", Color.FromArgb(245, 158, 11), Color.White, lblNeedsAttentionVal)
            cardOverdue = CreateCard(IconChar.ExclamationTriangle, "OVERDUE", "0", "Tasks", "Assigned to me", Color.FromArgb(239, 68, 68), Color.White, lblOverdueVal)
            cardCompleted = CreateCard(IconChar.CheckCircle, "COMPLETED TODAY", "0", "Tasks", "Finished by me today", Color.FromArgb(16, 185, 129), Color.White, lblCompletedVal)
            
            ' Special Attendance Card
            cardAttendance = CreateAttendanceCard()

            pnlCards.Controls.Add(cardDueSoon)
            pnlCards.Controls.Add(cardNeedsAttention)
            pnlCards.Controls.Add(cardOverdue)
            pnlCards.Controls.Add(cardCompleted)
            pnlCards.Controls.Add(cardAttendance)

            Me.Controls.Add(pnlCards)
            Me.Controls.Add(pnlHeader)
        End Sub

        Private Function CreateCard(iconChar As IconChar, title As String, initialValue As String, subtext As String, initialActionText As String, accentColor As Color, backColor As Color, ByRef valLabelRef As Label) As ElevatedInteractiveCard
            Dim card As New ElevatedInteractiveCard() With {
                .Width = 240,
                .Height = 140,
                .AccentColor = accentColor,
                .NormalColor = backColor,
                .HoverColor = Color.FromArgb(248, 250, 252),
                .Margin = New Padding(0, 0, 16, 16)
            }

            Dim pnlCircle As New Panel() With {
                .Location = New Point(12, 10),
                .Size = New Size(28, 28),
                .BackColor = Color.FromArgb(30, accentColor.R, accentColor.G, accentColor.B)
            }
            Dim picIcon As New PictureBox() With {
                .Image = iconChar.ToBitmap(accentColor, 15),
                .Size = New Size(28, 28),
                .Location = New Point(0, 0),
                .SizeMode = PictureBoxSizeMode.CenterImage,
                .BackColor = Color.Transparent
            }
            pnlCircle.Controls.Add(picIcon)

            Dim lblT As New Label() With {
                .Text = title,
                .Font = New Font(ThemeConstants.FontNameDefault, 8.0!, FontStyle.Bold),
                .ForeColor = accentColor,
                .BackColor = Color.Transparent,
                .Location = New Point(46, 14),
                .AutoSize = True
            }

            valLabelRef = New Label() With {
                .Text = initialValue,
                .Font = New Font(ThemeConstants.FontNameDefault, 20.0!, FontStyle.Bold),
                .ForeColor = accentColor,
                .BackColor = Color.Transparent,
                .Location = New Point(12, 40),
                .AutoSize = True
            }

            Dim lblS As New Label() With {
                .Text = subtext,
                .Font = New Font(ThemeConstants.FontNameDefault, 8.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .BackColor = Color.Transparent,
                .Location = New Point(12, 70),
                .AutoSize = True
            }

            Dim actionLabelRef = New Label() With {
                .Text = initialActionText,
                .Font = New Font(ThemeConstants.FontNameDefault, 8.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextMuted,
                .BackColor = Color.Transparent,
                .Location = New Point(12, 92),
                .AutoSize = True
            }

            card.Controls.Add(pnlCircle)
            card.Controls.Add(lblT)
            card.Controls.Add(valLabelRef)
            card.Controls.Add(lblS)
            card.Controls.Add(actionLabelRef)

            ' Dynamic Y-positioning to prevent DPI-scaling overlap
            Dim localValLabel = valLabelRef
            Dim localActionLabel = actionLabelRef
            AddHandler localValLabel.SizeChanged, Sub(s, e)
                                                    lblS.Top = localValLabel.Bottom + 2
                                                    localActionLabel.Top = lblS.Bottom + 4
                                                End Sub
            ' Trigger initial layout calculation
            lblS.Top = localValLabel.Bottom + 2
            localActionLabel.Top = lblS.Bottom + 4

            Return card
        End Function

        Private Function CreateAttendanceCard() As ElevatedInteractiveCard
            Dim card As New ElevatedInteractiveCard() With {
                .Width = 240,
                .Height = 140,
                .AccentColor = Color.FromArgb(139, 92, 246),
                .NormalColor = Color.White,
                .HoverColor = Color.FromArgb(248, 250, 252),
                .Margin = New Padding(0, 0, 16, 16)
            }

            Dim pnlCircle As New Panel() With {
                .Location = New Point(12, 10),
                .Size = New Size(28, 28),
                .BackColor = Color.FromArgb(30, 139, 92, 246)
            }
            Dim picIcon As New PictureBox() With {
                .Image = IconChar.UserClock.ToBitmap(Color.FromArgb(139, 92, 246), 15),
                .Size = New Size(28, 28),
                .Location = New Point(0, 0),
                .SizeMode = PictureBoxSizeMode.CenterImage,
                .BackColor = Color.Transparent
            }
            pnlCircle.Controls.Add(picIcon)

            Dim lblT As New Label() With {
                .Text = "ATTENDANCE",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.0!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(139, 92, 246),
                .BackColor = Color.Transparent,
                .Location = New Point(46, 14),
                .AutoSize = True
            }

            lblAttendanceStatus = New Label() With {
                .Text = "Not clocked in",
                .Font = New Font(ThemeConstants.FontNameDefault, 12.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .BackColor = Color.Transparent,
                .Location = New Point(12, 45),
                .AutoSize = True
            }

            lblAttendanceAction = New Label() With {
                .Text = "Click to clock in/out",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextMuted,
                .BackColor = Color.Transparent,
                .Location = New Point(12, 92),
                .AutoSize = True
            }

            card.Controls.Add(pnlCircle)
            card.Controls.Add(lblT)
            card.Controls.Add(lblAttendanceStatus)
            card.Controls.Add(lblAttendanceAction)
            
            ' Dynamic Y-positioning to prevent DPI-scaling overlap
            AddHandler lblAttendanceStatus.SizeChanged, Sub(s, e)
                                                            lblAttendanceAction.Top = lblAttendanceStatus.Bottom + 4
                                                        End Sub
            lblAttendanceAction.Top = lblAttendanceStatus.Bottom + 4
            
            card.SetInteractiveState(False)

            Return card
        End Function


        Public Async Function PreloadDataAsync(navigationToken As Long) As Task Implements IPreloadableScreen.PreloadDataAsync
            _activeNavigationToken = navigationToken
            Await RefreshDataInternalAsync()
        End Function

        Private Async Function RefreshDataInternalAsync() As Task
            Try
                If Not CurrentUserContext.IsAuthenticated Then Return
                _currentUserId = CurrentUserContext.CurrentUser.UserId

                ' Load Tasks
                Dim tasks = Await _taskService.GetTasksForAssignedUserAsync(_currentUserId)
                
                Dim soonDue = tasks.Where(Function(t) t.WorkflowState <> TaskWorkflowState.Completed AndAlso t.WorkflowState <> TaskWorkflowState.Closed AndAlso t.WorkflowState <> TaskWorkflowState.Cancelled AndAlso t.TargetDueDate.Date <= DateTime.Today.AddDays(7) AndAlso t.TargetDueDate.Date >= DateTime.Today).Count()
                Dim needsAtt = tasks.Where(Function(t) t.WorkflowState = TaskWorkflowState.NewTask OrElse t.WorkflowState = TaskWorkflowState.Assigned).Count()
                Dim overdue = tasks.Where(Function(t) t.WorkflowState <> TaskWorkflowState.Completed AndAlso t.WorkflowState <> TaskWorkflowState.Closed AndAlso t.WorkflowState <> TaskWorkflowState.Cancelled AndAlso t.IsOverdue).Count()
                Dim completedToday = tasks.Where(Function(t) t.WorkflowState = TaskWorkflowState.Completed AndAlso t.ModifiedOn.HasValue AndAlso t.ModifiedOn.Value.Date = DateTime.Today).Count()

                lblDueSoonVal.Text = soonDue.ToString()
                lblNeedsAttentionVal.Text = needsAtt.ToString()
                lblOverdueVal.Text = overdue.ToString()
                lblCompletedVal.Text = completedToday.ToString()

                ' Load Attendance
                _todayRecord = Await _attendanceService.GetTodayAttendanceForUserAsync(_currentUserId)
                
                If _todayRecord IsNot Nothing AndAlso _todayRecord.ClockInTime <> DateTime.MinValue Then
                    If _todayRecord.ClockOutTime.HasValue Then
                        lblAttendanceStatus.Text = "Shift Completed"
                        lblAttendanceStatus.ForeColor = Color.FromArgb(16, 185, 129)
                        lblAttendanceAction.Text = $"Completed at {_todayRecord.ClockOutTime.Value:hh:mm tt}"
                    Else
                        lblAttendanceStatus.Text = $"Clocked in at {_todayRecord.ClockInTime:hh:mm tt}"
                        lblAttendanceStatus.ForeColor = Color.FromArgb(14, 165, 233)
                        lblAttendanceAction.Text = "Working - view in header"
                    End If
                Else
                    lblAttendanceStatus.Text = "Not clocked in"
                    lblAttendanceStatus.ForeColor = ThemeConstants.TextPrimary
                    lblAttendanceAction.Text = "Use header to start"
                End If

            Catch ex As Exception
                ' Error handling
            End Try
        End Function

    End Class
End Namespace
