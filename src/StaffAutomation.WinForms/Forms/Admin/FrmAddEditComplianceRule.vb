Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports Krypton.Toolkit
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
Imports StaffAutomation.WinForms.Forms.Common
Imports StaffAutomation.WinForms.Forms.Main

Namespace Forms.Admin
    ''' <summary>
    ''' Modal Form for configuring Compliance Automation Rules and Alert Schedules.
    ''' </summary>
    Public Class FrmAddEditComplianceRule
        Inherits Form

        Private ReadOnly _compService As IComplianceAutomationService
        Private ReadOnly _targetRuleId As Integer
        Private _editingRule As ComplianceRuleDto

        Private lblHeader As Label
        Private lblSub As Label

        Private txtRuleName As KryptonTextBox
        Private cboCategory As KryptonComboBox
        Private cboFrequency As KryptonComboBox
        Private numDueDateDay As NumericUpDown
        Private numDueDateMonth As NumericUpDown
        Private cboPriority As KryptonComboBox
        Private chkIsActive As CheckBox

        Private txtAlertDays As KryptonTextBox
        Private btnSave As KryptonButton
        Private btnCancel As KryptonButton

        Public Sub New(compService As IComplianceAutomationService, Optional ruleId As Integer = 0)
            _compService = compService
            _targetRuleId = ruleId

            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Text = If(_targetRuleId > 0, "⚙️ Configure Compliance Rule", "➕ New Compliance Automation Rule")
            Me.Size = New Size(560, 560)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.BackColor = Color.White
            Me.Padding = New Padding(20)

            lblHeader = New Label() With {
                .Text = If(_targetRuleId > 0, "Edit Statutory Compliance Rule", "Add New Compliance Rule"),
                .Font = New Font(ThemeConstants.FontNameDefault, 11.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Dock = DockStyle.Top,
                .Height = 28
            }

            lblSub = New Label() With {
                .Text = "Configure statutory return deadlines, priority, and automated alert schedule.",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Regular),
                .ForeColor = ThemeConstants.TextSecondary,
                .Dock = DockStyle.Top,
                .Height = 24
            }

            Dim pnlBody As New Panel() With {.Dock = DockStyle.Fill, .Padding = New Padding(0, 12, 0, 0)}

            Dim lblName As New Label() With {.Text = "Compliance Rule Name *", .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold), .Location = New Point(0, 4), .AutoSize = True}
            txtRuleName = New KryptonTextBox() With {.Location = New Point(0, 24), .Size = New Size(500, 32)}
            ThemeConstants.ApplyAppTextBoxStyle(txtRuleName)

            Dim lblCat As New Label() With {.Text = "Category *", .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold), .Location = New Point(0, 64), .AutoSize = True}
            cboCategory = New KryptonComboBox() With {.Location = New Point(0, 84), .Size = New Size(240, 32)}
            ThemeConstants.ApplyAppComboBoxStyle(cboCategory)
            cboCategory.Items.AddRange(New Object() {"GST", "TDS", "Income Tax", "Tax Audit", "Corporate", "Other"})
            cboCategory.SelectedIndex = 0

            Dim lblFreq As New Label() With {.Text = "Frequency *", .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold), .Location = New Point(260, 64), .AutoSize = True}
            cboFrequency = New KryptonComboBox() With {.Location = New Point(260, 84), .Size = New Size(240, 32)}
            ThemeConstants.ApplyAppComboBoxStyle(cboFrequency)
            cboFrequency.Items.AddRange(New Object() {"Monthly", "Quarterly", "Annual", "Custom"})
            cboFrequency.SelectedIndex = 0
            AddHandler cboFrequency.SelectedIndexChanged, Sub(s, e) UpdateMonthVisibility()

            Dim lblDay As New Label() With {.Text = "Due Date Day * (1-31)", .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold), .Location = New Point(0, 124), .AutoSize = True}
            numDueDateDay = New NumericUpDown() With {.Minimum = 1, .Maximum = 31, .Value = 11, .Location = New Point(0, 144), .Size = New Size(240, 28)}

            Dim lblMonth As New Label() With {.Text = "Due Date Month (Annual)", .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold), .Location = New Point(260, 124), .AutoSize = True}
            numDueDateMonth = New NumericUpDown() With {.Minimum = 1, .Maximum = 12, .Value = 9, .Location = New Point(260, 144), .Size = New Size(240, 28)}

            Dim lblPrio As New Label() With {.Text = "Task Priority *", .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold), .Location = New Point(0, 184), .AutoSize = True}
            cboPriority = New KryptonComboBox() With {.Location = New Point(0, 204), .Size = New Size(240, 32)}
            ThemeConstants.ApplyAppComboBoxStyle(cboPriority)
            cboPriority.Items.AddRange(New Object() {"Normal", "High", "Critical"})
            cboPriority.SelectedIndex = 1

            chkIsActive = New CheckBox() With {.Text = "Rule Active", .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold), .Checked = True, .Location = New Point(260, 210), .AutoSize = True}

            Dim lblAlerts As New Label() With {.Text = "Alert Reminders Schedule (Days Before Deadline, Comma-Separated)", .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold), .Location = New Point(0, 248), .AutoSize = True}
            txtAlertDays = New KryptonTextBox() With {.Text = "10, 3", .Location = New Point(0, 268), .Size = New Size(500, 32)}
            ThemeConstants.ApplyAppTextBoxStyle(txtAlertDays)

            Dim lblAlertHelp As New Label() With {
                .Text = "Example: '30, 15, 7, 3' creates automated alerts 30, 15, 7, and 3 days before deadline.",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.0!, FontStyle.Italic),
                .ForeColor = ThemeConstants.TextSecondary,
                .Location = New Point(0, 304),
                .AutoSize = True
            }

            pnlBody.Controls.Add(lblAlertHelp)
            pnlBody.Controls.Add(txtAlertDays)
            pnlBody.Controls.Add(lblAlerts)
            pnlBody.Controls.Add(chkIsActive)
            pnlBody.Controls.Add(cboPriority)
            pnlBody.Controls.Add(lblPrio)
            pnlBody.Controls.Add(numDueDateMonth)
            pnlBody.Controls.Add(lblMonth)
            pnlBody.Controls.Add(numDueDateDay)
            pnlBody.Controls.Add(lblDay)
            pnlBody.Controls.Add(cboFrequency)
            pnlBody.Controls.Add(lblFreq)
            pnlBody.Controls.Add(cboCategory)
            pnlBody.Controls.Add(lblCat)
            pnlBody.Controls.Add(txtRuleName)
            pnlBody.Controls.Add(lblName)

            Dim pnlFooter As New Panel() With {.Dock = DockStyle.Bottom, .Height = 50}
            btnSave = New KryptonButton() With {.Text = "💾 Save Rule", .Size = New Size(140, 34), .Location = New Point(230, 8)}
            ThemeConstants.ApplyKryptonPrimaryButton(btnSave)
            AddHandler btnSave.Click, Async Sub(s, e) Await SaveRuleAsync()

            btnCancel = New KryptonButton() With {.Text = "Cancel", .Size = New Size(90, 34), .Location = New Point(380, 8)}
            ThemeConstants.ApplyKryptonSecondaryButton(btnCancel)
            AddHandler btnCancel.Click, Sub(s, e) Me.Close()

            pnlFooter.Controls.Add(btnSave)
            pnlFooter.Controls.Add(btnCancel)

            Me.Controls.Add(pnlBody)
            Me.Controls.Add(pnlFooter)
            Me.Controls.Add(lblSub)
            Me.Controls.Add(lblHeader)
        End Sub

        Protected Overrides Async Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)
            If _targetRuleId > 0 Then
                Try
                    _editingRule = Await _compService.GetRuleByIdAsync(_targetRuleId)
                    If _editingRule IsNot Nothing Then
                        txtRuleName.Text = _editingRule.ComplianceName
                        cboCategory.SelectedItem = _editingRule.Category
                        cboFrequency.SelectedItem = _editingRule.Frequency
                        numDueDateDay.Value = _editingRule.DueDateRuleDay
                        If _editingRule.DueDateRuleMonth.HasValue Then numDueDateMonth.Value = _editingRule.DueDateRuleMonth.Value
                        cboPriority.SelectedItem = _editingRule.Priority
                        chkIsActive.Checked = _editingRule.IsActive

                        If _editingRule.AlertSchedules IsNot Nothing AndAlso _editingRule.AlertSchedules.Count > 0 Then
                            Dim daysList As New System.Collections.Generic.List(Of String)()
                            For Each s In _editingRule.AlertSchedules
                                daysList.Add(s.DaysBeforeDeadline.ToString())
                            Next
                            txtAlertDays.Text = String.Join(", ", daysList)
                        End If
                    End If
                Catch ex As Exception
                    FrmInAppAlert.ShowModal(Me, "Error", "Failed to load rule: " & ex.Message, AlertType.WarningAlert)
                End Try
            End If
            UpdateMonthVisibility()
        End Sub

        Private Sub UpdateMonthVisibility()
            Dim isAnnual = String.Equals(Convert.ToString(cboFrequency.SelectedItem), "Annual", StringComparison.OrdinalIgnoreCase)
            numDueDateMonth.Enabled = isAnnual
        End Sub

        Private Async Function SaveRuleAsync() As Task
            If String.IsNullOrWhiteSpace(txtRuleName.Text) Then
                FrmInAppAlert.ShowModal(Me, "Validation Error", "Compliance Rule Name is required.", AlertType.WarningAlert)
                Return
            End If

            btnSave.Enabled = False
            Try
                Dim rule As New ComplianceRuleDto() With {
                    .ComplianceRuleId = _targetRuleId,
                    .ComplianceName = txtRuleName.Text.Trim(),
                    .Category = Convert.ToString(cboCategory.SelectedItem),
                    .Frequency = Convert.ToString(cboFrequency.SelectedItem),
                    .DueDateRuleDay = Convert.ToInt32(numDueDateDay.Value),
                    .DueDateRuleMonth = If(numDueDateMonth.Enabled, Convert.ToInt32(numDueDateMonth.Value), CType(Nothing, Nullable(Of Integer))),
                    .Priority = Convert.ToString(cboPriority.SelectedItem),
                    .IsActive = chkIsActive.Checked
                }

                ' Parse alert schedule days
                Dim daysParts = txtAlertDays.Text.Split(New Char() {","c}, StringSplitOptions.RemoveEmptyEntries)
                For Each p In daysParts
                    Dim d As Integer
                    If Integer.TryParse(p.Trim(), d) AndAlso d > 0 Then
                        rule.AlertSchedules.Add(New ComplianceAlertScheduleDto() With {
                            .DaysBeforeDeadline = d,
                            .AlertType = If(d <= 3, "Urgent", "Reminder"),
                            .IsEnabled = True
                        })
                    End If
                Next

                Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)
                Dim success = Await _compService.SaveRuleAsync(rule, currentUserId)
                If success Then
                    FrmInAppAlert.ShowModal(Me, "Success", "Compliance Automation Rule saved successfully.", AlertType.SuccessAlert)
                    Me.DialogResult = DialogResult.OK
                    Me.Close()
                Else
                    FrmInAppAlert.ShowModal(Me, "Error", "Failed to save compliance rule.", AlertType.WarningAlert)
                End If
            Catch valEx As ValidationException
                FrmInAppAlert.ShowModal(Me, "Validation Error", valEx.Message, AlertType.WarningAlert)
            Catch bizEx As BusinessException
                FrmInAppAlert.ShowModal(Me, "Business Error", bizEx.Message, AlertType.WarningAlert)
            Catch ex As Exception
                FrmInAppAlert.ShowModal(Me, "Error", "Unexpected error: " & ex.Message, AlertType.WarningAlert)
            Finally
                btnSave.Enabled = True
            End Try
        End Function
    End Class
End Namespace
