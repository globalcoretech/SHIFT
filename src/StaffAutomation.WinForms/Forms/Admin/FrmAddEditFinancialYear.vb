Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports Krypton.Toolkit
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Security
Imports StaffAutomation.WinForms.Forms.Common
Imports StaffAutomation.WinForms.Forms.Main

Namespace Forms.Admin
    ''' <summary>
    ''' Modal Dialog Form for creating or editing a Financial Year record.
    ''' Conforms to Krypton design system ThemeConstants.
    ''' </summary>
    Public Class FrmAddEditFinancialYear
        Inherits Form

        Private ReadOnly _fyService As IFinancialYearService
        Private ReadOnly _editingFY As FinancialYearDto

        Private txtFYCode As KryptonTextBox
        Private dtpStartDate As KryptonDateTimePicker
        Private dtpEndDate As KryptonDateTimePicker
        Private chkIsCurrentFY As CheckBox

        Private btnSave As KryptonButton
        Private btnCancel As KryptonButton

        Public Sub New(fyService As IFinancialYearService, Optional editingFY As FinancialYearDto = Nothing)
            _fyService = fyService
            _editingFY = editingFY
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Text = If(_editingFY Is Nothing, "Add Financial Year", "Edit Financial Year")
            Me.Size = New Size(460, 360)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.BackColor = Color.White

            Dim pnlContainer As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(20),
                .ColumnCount = 2,
                .RowCount = 5
            }
            pnlContainer.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 130.0!))
            pnlContainer.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0!))

            ' Row 1: FY Code
            Dim lblCode As New Label() With {.Text = "FY Code *", .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold), .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleLeft}
            txtFYCode = New KryptonTextBox() With {.Dock = DockStyle.Fill}
            ThemeConstants.ApplyAppTextBoxStyle(txtFYCode)
            pnlContainer.Controls.Add(lblCode, 0, 0)
            pnlContainer.Controls.Add(txtFYCode, 1, 0)

            ' Row 2: Start Date
            Dim lblStart As New Label() With {.Text = "Start Date *", .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold), .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleLeft}
            dtpStartDate = New KryptonDateTimePicker() With {.Dock = DockStyle.Fill, .Format = DateTimePickerFormat.Custom, .CustomFormat = "yyyy-MM-dd"}
            ThemeConstants.ApplyAppDatePickerStyle(dtpStartDate)
            pnlContainer.Controls.Add(lblStart, 0, 1)
            pnlContainer.Controls.Add(dtpStartDate, 1, 1)

            ' Row 3: End Date
            Dim lblEnd As New Label() With {.Text = "End Date *", .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold), .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleLeft}
            dtpEndDate = New KryptonDateTimePicker() With {.Dock = DockStyle.Fill, .Format = DateTimePickerFormat.Custom, .CustomFormat = "yyyy-MM-dd"}
            ThemeConstants.ApplyAppDatePickerStyle(dtpEndDate)
            pnlContainer.Controls.Add(lblEnd, 0, 2)
            pnlContainer.Controls.Add(dtpEndDate, 1, 2)

            ' Row 4: Is Current Active FY
            Dim lblCurrent As New Label() With {.Text = "Set Active", .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold), .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleLeft}
            chkIsCurrentFY = New CheckBox() With {.Text = "Set as Current Active Financial Year", .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular), .Dock = DockStyle.Fill, .AutoSize = True}
            pnlContainer.Controls.Add(lblCurrent, 0, 3)
            pnlContainer.Controls.Add(chkIsCurrentFY, 1, 3)

            ' Row 5: Action Buttons
            Dim pnlButtons As New FlowLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .FlowDirection = FlowDirection.RightToLeft,
                .Padding = New Padding(0, 10, 0, 0)
            }

            btnCancel = New KryptonButton() With {.Text = "Cancel", .Size = New Size(90, 34)}
            ThemeConstants.ApplyKryptonSecondaryButton(btnCancel)
            AddHandler btnCancel.Click, Sub(s, e) Me.Close()

            btnSave = New KryptonButton() With {.Text = "Save", .Size = New Size(110, 34)}
            ThemeConstants.ApplyKryptonPrimaryButton(btnSave)
            AddHandler btnSave.Click, Async Sub(s, e) Await SaveFYAsync()

            pnlButtons.Controls.Add(btnCancel)
            pnlButtons.Controls.Add(btnSave)
            pnlContainer.Controls.Add(pnlButtons, 1, 4)

            Me.Controls.Add(pnlContainer)
        End Sub

        Protected Overrides Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)

            If _editingFY IsNot Nothing Then
                txtFYCode.Text = _editingFY.FYCode
                dtpStartDate.Value = _editingFY.StartDate
                dtpEndDate.Value = _editingFY.EndDate
                chkIsCurrentFY.Checked = _editingFY.IsCurrentFY
            Else
                dtpStartDate.Value = New DateTime(DateTime.Now.Year, 4, 1)
                dtpEndDate.Value = New DateTime(DateTime.Now.Year + 1, 3, 31)
                txtFYCode.Text = $"FY{DateTime.Now.Year}-{(DateTime.Now.Year + 1) Mod 100:D2}"
            End If
        End Sub

        Private Async Function SaveFYAsync() As Task
            btnSave.Enabled = False
            Try
                Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)

                Dim dto As New FinancialYearDto() With {
                    .FinancialYearId = If(_editingFY IsNot Nothing, _editingFY.FinancialYearId, 0),
                    .FYCode = txtFYCode.Text.Trim(),
                    .StartDate = dtpStartDate.Value.Date,
                    .EndDate = dtpEndDate.Value.Date,
                    .IsCurrentFY = chkIsCurrentFY.Checked
                }

                If _editingFY Is Nothing Then
                    Dim newId = Await _fyService.CreateFinancialYearAsync(dto, currentUserId)
                    If newId > 0 Then
                        FrmInAppAlert.ShowModal(Me, "Success", $"Financial Year '{dto.FYCode}' created.", AlertType.SuccessAlert)
                        Me.DialogResult = DialogResult.OK
                        Me.Close()
                    End If
                Else
                    Dim success = Await _fyService.UpdateFinancialYearAsync(dto, currentUserId)
                    If success Then
                        FrmInAppAlert.ShowModal(Me, "Success", $"Financial Year '{dto.FYCode}' updated.", AlertType.SuccessAlert)
                        Me.DialogResult = DialogResult.OK
                        Me.Close()
                    End If
                End If
            Catch valEx As ValidationException
                FrmInAppAlert.ShowModal(Me, "Validation Error", valEx.Message, AlertType.WarningAlert)
            Catch bizEx As BusinessException
                FrmInAppAlert.ShowModal(Me, "Business Error", bizEx.Message, AlertType.WarningAlert)
            Catch ex As Exception
                FrmInAppAlert.ShowModal(Me, "Error", "Failed to save Financial Year: " & ex.Message, AlertType.WarningAlert)
            Finally
                btnSave.Enabled = True
            End Try
        End Function
    End Class
End Namespace
