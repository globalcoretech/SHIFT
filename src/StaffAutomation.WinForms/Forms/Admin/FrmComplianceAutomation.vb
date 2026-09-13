Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports Krypton.Toolkit
Imports StaffAutomation.BLL.Logging
Imports StaffAutomation.BLL.Security
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
Imports StaffAutomation.WinForms.Forms.Common
Imports StaffAutomation.WinForms.Forms.Main

Namespace Forms.Admin
    ''' <summary>
    ''' Compliance Automation Center Form managing statutory return deadlines, alert schedules,
    ''' automated task creation (with default UNASSIGNED allocation), duplicate prevention, and upcoming previews.
    ''' Styled according to Krypton design tokens and 1366x768 responsive layout guidelines.
    ''' </summary>
    Public Class FrmComplianceAutomation
        Inherits Form

        Private ReadOnly _compService As IComplianceAutomationService
        Private ReadOnly _authzService As IAuthorizationService

        Private pnlHeader As Panel
        Private lblTitle As Label
        Private lblSubtitle As Label
        Private btnHeaderBack As KryptonButton
        Private btnHeaderClose As KryptonButton

        Private pnlMetrics As FlowLayoutPanel
        Private cardActiveRules As AppActionCard
        Private cardUpcoming As AppActionCard
        Private cardTasksScheduled As AppActionCard
        Private cardOverdue As AppActionCard

        Private pnlRulesToolbar As FlowLayoutPanel
        Private btnAddRule As KryptonButton
        Private btnConfigureRule As KryptonButton
        Private btnRunAutomation As KryptonButton
        Private btnToggleRule As KryptonButton

        Private dgvRules As DataGridView
        Private dgvPreviews As DataGridView

        Private _rulesList As New System.Collections.Generic.List(Of ComplianceRuleDto)()

        Public Sub New()
            Dim config As IAppConfiguration = New AppConfiguration()
            Dim connFactory As IDatabaseConnectionFactory = New DbConnectionFactory(config)
            Dim sqlHelper As ISqlHelper = New SqlHelper(connFactory)
            Dim appLogger As IAppLogger = New AppLogger(config)
            Dim auditLogger As IAuditLogger = New AuditLogger(sqlHelper)

            Dim compRepo As New ComplianceRepository(sqlHelper)
            Dim taskRepo As ITaskRepository = New TaskRepository(sqlHelper)

            _compService = New ComplianceAutomationService(compRepo, taskRepo, auditLogger)
            _authzService = New AuthorizationService()

            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Text = "⚙️ Compliance Automation Center"
            Me.Size = New Size(1150, 750)
            Me.MinimumSize = New Size(1000, 650)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.BackColor = ThemeConstants.WorkspaceBackground
            Me.Padding = New Padding(16)

            ' 1. Header Panel
            pnlHeader = New Panel()
            lblTitle = New Label() With {.Text = "🏛️ Compliance Automation Center"}
            lblSubtitle = New Label() With {.Text = "Automatically prepare reminders and create compliance tasks before statutory deadlines."}
            ThemeConstants.ApplyHeaderStyle(pnlHeader, lblTitle, lblSubtitle)

            ' Top-Right Header Navigation Action Controls
            Dim pnlHeaderNav As New FlowLayoutPanel() With {
                .Dock = DockStyle.Right,
                .AutoSize = True,
                .FlowDirection = FlowDirection.LeftToRight,
                .WrapContents = False,
                .BackColor = Color.Transparent,
                .Padding = New Padding(0, 14, 16, 0)
            }

            btnHeaderBack = New KryptonButton() With {
                .Text = "← Back to Settings",
                .Size = New Size(140, 32),
                .Margin = New Padding(0)
            }
            ThemeConstants.ApplyKryptonSecondaryButton(btnHeaderBack)

            btnHeaderClose = New KryptonButton() With {
                .Text = "✕",
                .Size = New Size(44, 32),
                .Margin = New Padding(6, 0, 0, 0)
            }
            ThemeConstants.ApplyKryptonSecondaryButton(btnHeaderClose)
            btnHeaderClose.StateCommon.Back.Color1 = Color.FromArgb(220, 38, 38)
            btnHeaderClose.StateCommon.Back.Color2 = Color.FromArgb(220, 38, 38)
            btnHeaderClose.StateCommon.Content.ShortText.Color1 = Color.White
            btnHeaderClose.StateCommon.Content.ShortText.Font = New Font(ThemeConstants.FontNameDefault, 10.0!, FontStyle.Bold)

            AddHandler btnHeaderClose.Click, Sub() CloseForm()
            AddHandler btnHeaderBack.Click, Sub() CloseForm()

            pnlHeaderNav.Controls.Add(btnHeaderBack)
            pnlHeaderNav.Controls.Add(btnHeaderClose)

            pnlHeader.Controls.Add(pnlHeaderNav)
            pnlHeader.Controls.Add(lblSubtitle)
            pnlHeader.Controls.Add(lblTitle)

            Me.KeyPreview = True
            Me.ControlBox = True
            Me.CancelButton = btnHeaderClose

            ' 2. Metrics FlowLayoutPanel (4 Cards)
            pnlMetrics = New FlowLayoutPanel() With {
                .Dock = DockStyle.Top,
                .Height = 115,
                .FlowDirection = FlowDirection.LeftToRight,
                .WrapContents = False,
                .Padding = New Padding(0, 8, 0, 8),
                .BackColor = Color.Transparent
            }

            cardActiveRules = CreateMetricCard("ACTIVE RULES", "5 Rules Registered", "Rules Active", ThemeConstants.PrimaryAccent)
            cardUpcoming = CreateMetricCard("UPCOMING DEADLINE", "GSTR-1 (11 Sep)", "Next Cutoff", ThemeConstants.SuccessGreen)
            cardTasksScheduled = CreateMetricCard("TASKS SCHEDULED", "3 Automated Pending", "Queued Tasks", Color.FromArgb(124, 58, 237))
            cardOverdue = CreateMetricCard("OVERDUE COMPLIANCE", "0 Overdue Returns", "Zero Breaches", Color.FromArgb(217, 119, 6))

            pnlMetrics.Controls.Add(cardActiveRules)
            pnlMetrics.Controls.Add(cardUpcoming)
            pnlMetrics.Controls.Add(cardTasksScheduled)
            pnlMetrics.Controls.Add(cardOverdue)

            ' 3. Body Split/Container
            Dim pnlBodyHost As New Panel() With {.Dock = DockStyle.Fill, .Padding = New Padding(0, 8, 0, 0)}

            ' Rules Toolbar
            pnlRulesToolbar = New FlowLayoutPanel() With {
                .Dock = DockStyle.Top,
                .Height = 44,
                .FlowDirection = FlowDirection.LeftToRight,
                .BackColor = Color.Transparent
            }

            btnAddRule = New KryptonButton() With {.Text = "➕ Add Compliance Return Rule", .Size = New Size(210, 34), .Margin = New Padding(0, 0, 8, 0)}
            ThemeConstants.ApplyKryptonPrimaryButton(btnAddRule)
            AddHandler btnAddRule.Click, Sub(s, e) AddNewRule()

            btnConfigureRule = New KryptonButton() With {.Text = "⚙️ Configure Selected Rule", .Size = New Size(175, 34), .Margin = New Padding(0, 0, 8, 0)}
            ThemeConstants.ApplyKryptonSecondaryButton(btnConfigureRule)
            AddHandler btnConfigureRule.Click, Sub(s, e) EditSelectedRule()

            btnToggleRule = New KryptonButton() With {.Text = "⚡ Activate / Pause Rule", .Size = New Size(155, 34), .Margin = New Padding(0, 0, 8, 0)}
            ThemeConstants.ApplyKryptonSecondaryButton(btnToggleRule)
            AddHandler btnToggleRule.Click, Async Sub(s, e) Await ToggleSelectedRuleStateAsync()

            ' Separated System Operational Action
            btnRunAutomation = New KryptonButton() With {.Text = "▶️ Run Automation Check Now", .Size = New Size(210, 34), .Margin = New Padding(32, 0, 0, 0)}
            ThemeConstants.ApplyKryptonSecondaryButton(btnRunAutomation)
            AddHandler btnRunAutomation.Click, Async Sub(s, e) Await RunAutomationCheckNowAsync()

            pnlRulesToolbar.Controls.Add(btnAddRule)
            pnlRulesToolbar.Controls.Add(btnConfigureRule)
            pnlRulesToolbar.Controls.Add(btnToggleRule)
            pnlRulesToolbar.Controls.Add(btnRunAutomation)

            ' Grid 1: Active Rules Catalog
            Dim pnlRulesGroup As New Panel() With {.Dock = DockStyle.Top, .Height = 200, .BackColor = Color.White, .Padding = New Padding(12)}
            ThemeConstants.ApplyCardSurfaceStyle(pnlRulesGroup)

            Dim lblRulesHeader As New Label() With {
                .Text = "ACTIVE STATUTORY COMPLIANCE RULES CATALOG",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Dock = DockStyle.Top,
                .Height = 24
            }

            dgvRules = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .MultiSelect = False,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .AutoGenerateColumns = False
            }
            ThemeConstants.ApplyModernGridStyle(dgvRules)

            dgvRules.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colId", .HeaderText = "ID", .Visible = False})
            dgvRules.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colName", .HeaderText = "Compliance Return Name", .Width = 220})
            dgvRules.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colCat", .HeaderText = "Category", .Width = 110})
            dgvRules.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colFreq", .HeaderText = "Frequency", .Width = 110})
            dgvRules.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colDue", .HeaderText = "Statutory Due Date Rule", .Width = 180})
            dgvRules.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colPrio", .HeaderText = "Priority", .Width = 100})
            dgvRules.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colStatus", .HeaderText = "Status", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})

            pnlRulesGroup.Controls.Add(dgvRules)
            pnlRulesGroup.Controls.Add(lblRulesHeader)

            ' Grid 2: Upcoming Automation Preview
            Dim pnlPreviewGroup As New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.White, .Padding = New Padding(12), .Margin = New Padding(0, 12, 0, 0)}
            ThemeConstants.ApplyCardSurfaceStyle(pnlPreviewGroup)

            Dim lblPreviewHeader As New Label() With {
                .Text = "📅 UPCOMING AUTOMATED TASKS & REMINDERS PREVIEW — What is going to happen next?",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Dock = DockStyle.Top,
                .Height = 24
            }

            dgvPreviews = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .MultiSelect = False,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .AutoGenerateColumns = False
            }
            ThemeConstants.ApplyModernGridStyle(dgvPreviews)

            dgvPreviews.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colPrevRule", .HeaderText = "Compliance Return", .Width = 220})
            dgvPreviews.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colPrevPeriod", .HeaderText = "Filing Period", .Width = 130})
            dgvPreviews.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colPrevDeadline", .HeaderText = "Statutory Deadline", .Width = 150})
            dgvPreviews.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colPrevAlert", .HeaderText = "Next Alert Date", .Width = 150})
            dgvPreviews.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colPrevTask", .HeaderText = "Task Creation Date", .Width = 150})
            dgvPreviews.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colPrevStatus", .HeaderText = "Automation Status", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})

            pnlPreviewGroup.Controls.Add(dgvPreviews)
            pnlPreviewGroup.Controls.Add(lblPreviewHeader)

            pnlBodyHost.Controls.Add(pnlPreviewGroup)
            pnlBodyHost.Controls.Add(pnlRulesGroup)
            pnlBodyHost.Controls.Add(pnlRulesToolbar)

            Me.Controls.Add(pnlBodyHost)
            Me.Controls.Add(pnlMetrics)
            Me.Controls.Add(pnlHeader)

            AddHandler pnlMetrics.Resize, Sub(s, e) RecalculateMetricWidths()
            RecalculateMetricWidths()
        End Sub

        Protected Overrides Async Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)
            If Not _authzService.IsAuthorized(UserRole.Admin) AndAlso Not _authzService.IsAuthorized(UserRole.Owner) Then
                FrmInAppAlert.ShowModal(Me, "Access Denied", "Access Denied: Compliance Automation configuration requires Administrator or Owner privileges.", AlertType.WarningAlert)
                Me.Close()
                Return
            End If

            Await LoadDataAsync()
        End Sub

        Private Async Function LoadDataAsync() As Task
            Try
                _rulesList = Await _compService.GetAllRulesAsync(includeInactive:=True)
                dgvRules.Rows.Clear()

                For Each r In _rulesList
                    Dim dueStr = "Day " & r.DueDateRuleDay.ToString() & If(r.DueDateRuleMonth.HasValue, " of Month " & r.DueDateRuleMonth.Value.ToString(), " of Period")
                    Dim statusBadge = If(r.IsActive, "⚡ Active", "Inactive")
                    dgvRules.Rows.Add(r.ComplianceRuleId, r.ComplianceName, r.Category, r.Frequency, dueStr, r.Priority, statusBadge)
                Next

                cardActiveRules.Description = _rulesList.FindAll(Function(x) x.IsActive).Count.ToString() & " Active Rules"

                ' Previews
                Dim previews = Await _compService.GetUpcomingPreviewsAsync()
                dgvPreviews.Rows.Clear()

                For Each p In previews
                    dgvPreviews.Rows.Add(p.ComplianceName, p.PeriodName, p.DeadlineDate.ToString("yyyy-MM-dd"), p.NextAlertDate.ToString("yyyy-MM-dd"), p.TaskCreationDate.ToString("yyyy-MM-dd"), "📅 " & p.Status)
                Next

                If previews.Count > 0 Then
                    cardUpcoming.Description = previews(0).ComplianceName
                    cardUpcoming.BadgeText = "Deadline: " & previews(0).DeadlineDate.ToString("dd MMM yyyy")
                End If
            Catch ex As Exception
                FrmInAppAlert.ShowModal(Me, "Error", "Failed to load compliance automation data: " & ex.Message, AlertType.WarningAlert)
            End Try
        End Function

        Private Sub AddNewRule()
            Using dlg As New FrmAddEditComplianceRule(_compService, 0)
                If dlg.ShowDialog(Me) = DialogResult.OK Then
                    Dim task = LoadDataAsync()
                End If
            End Using
        End Sub

        Private Sub EditSelectedRule()
            If dgvRules.SelectedRows.Count = 0 Then
                FrmInAppAlert.ShowModal(Me, "Selection Required", "Please select a compliance rule row to configure.", AlertType.WarningAlert)
                Return
            End If

            Dim selectedId = Convert.ToInt32(dgvRules.SelectedRows(0).Cells("colId").Value)
            Using dlg As New FrmAddEditComplianceRule(_compService, selectedId)
                If dlg.ShowDialog(Me) = DialogResult.OK Then
                    Dim task = LoadDataAsync()
                End If
            End Using
        End Sub

        Private Async Function ToggleSelectedRuleStateAsync() As Task
            If dgvRules.SelectedRows.Count = 0 Then
                FrmInAppAlert.ShowModal(Me, "Selection Required", "Please select a compliance rule row to toggle.", AlertType.WarningAlert)
                Return
            End If

            Dim selectedId = Convert.ToInt32(dgvRules.SelectedRows(0).Cells("colId").Value)
            Dim rule = _rulesList.Find(Function(r) r.ComplianceRuleId = selectedId)
            If rule Is Nothing Then Return

            Try
                Dim targetState = Not rule.IsActive
                Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)
                Dim success = Await _compService.ToggleRuleStateAsync(selectedId, targetState, currentUserId)
                If success Then
                    FrmInAppAlert.ShowModal(Me, "Success", "Compliance rule '" & rule.ComplianceName & "' state updated.", AlertType.SuccessAlert)
                    Await LoadDataAsync()
                End If
            Catch ex As Exception
                FrmInAppAlert.ShowModal(Me, "Error", "Failed to toggle rule state: " & ex.Message, AlertType.WarningAlert)
            End Try
        End Function

        Private Async Function RunAutomationCheckNowAsync() As Task
            Try
                Me.Cursor = Cursors.WaitCursor
                Dim createdCount = Await _compService.EvaluateAutomationAsync(DateTime.Today)
                FrmInAppAlert.ShowModal(Me, "Automation Trigger Executed", "Compliance automation check completed." & Environment.NewLine & "Automated Compliance Tasks Generated: " & createdCount.ToString() & " (Allocated to UNASSIGNED Queue for Admin Assignment).", AlertType.SuccessAlert)
                Await LoadDataAsync()
            Catch ex As Exception
                FrmInAppAlert.ShowModal(Me, "Error", "Failed to execute compliance automation check: " & ex.Message, AlertType.WarningAlert)
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Function

        Private Function CreateMetricCard(title As String, val As String, subtext As String, accentColor As Color) As AppActionCard
            Dim card As New AppActionCard() With {
                .Title = title,
                .Description = val,
                .BadgeText = subtext,
                .AccentColor = accentColor,
                .ActionText = "View Details →",
                .Size = New Size(260, 95)
            }
            Return card
        End Function

        Private Sub RecalculateMetricWidths()
            If pnlMetrics IsNot Nothing AndAlso pnlMetrics.ClientSize.Width > 500 Then
                Dim targetWidth As Integer = Math.Max(220, (pnlMetrics.ClientSize.Width - 36) \ 4)
                For Each ctrl As Control In pnlMetrics.Controls
                    If TypeOf ctrl Is AppActionCard Then
                        ctrl.Width = targetWidth
                    End If
                Next
            End If
        End Sub

        Private Sub CloseForm()
            If Me.Modal Then
                Me.DialogResult = DialogResult.Cancel
                Me.Close()
            Else
                Dim parentShell = TryCast(Me.MdiParent, FrmMainShell)
                If parentShell Is Nothing AndAlso Me.Parent IsNot Nothing Then
                    parentShell = TryCast(Me.Parent.FindForm(), FrmMainShell)
                End If
                If parentShell IsNot Nothing Then
                    parentShell.NavigateToModule("Settings")
                Else
                    Me.Close()
                End If
            End If
        End Sub

        Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
            If keyData = Keys.Escape Then
                CloseForm()
                Return True
            End If
            Return MyBase.ProcessCmdKey(msg, keyData)
        End Function
    End Class
End Namespace
