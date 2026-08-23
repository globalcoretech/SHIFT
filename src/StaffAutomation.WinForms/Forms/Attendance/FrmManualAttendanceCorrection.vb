Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports FontAwesome.Sharp
Imports Krypton.Toolkit
Imports StaffAutomation.BLL.Interfaces
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Security
Imports StaffAutomation.WinForms.Forms.Main
Imports StaffAutomation.WinForms.UIHelpers

Namespace Forms.Attendance
    ''' <summary>
    ''' Modal Dialog inheriting KryptonForm allowing Admin to perform manual attendance punch corrections.
    ''' Converted to Krypton Toolkit controls. Enforces mandatory reason entry and writes complete audit logs.
    ''' </summary>
    Public Class FrmManualAttendanceCorrection
        Inherits KryptonForm

        Private ReadOnly _attendanceId As Integer
        Private ReadOnly _targetUserId As Integer
        Private ReadOnly _staffName As String
        Private ReadOnly _attendanceDate As DateTime
        Private ReadOnly _attendanceService As IAttendanceService

        ' Krypton Controls
        Private pnlHeader As KryptonPanel
        Private lblTitle As Label

        Private lblStaffInfo As Label
        Private lblDateInfo As Label

        Private lblClockIn As Label
        Private dtpClockIn As KryptonDateTimePicker

        Private chkEnableClockOut As KryptonCheckBox
        Private dtpClockOut As KryptonDateTimePicker

        Private lblBreak As Label
        Private numBreakMinutes As KryptonNumericUpDown

        Private lblStatus As Label
        Private cboStatus As KryptonComboBox

        Private lblReason As Label
        Private txtReason As KryptonTextBox

        Private btnSave As KryptonButton
        Private btnCancel As KryptonButton

        Public Sub New(attendanceId As Integer, userId As Integer, staffName As String, attendanceDate As DateTime, initialClockIn As String, initialClockOut As String, initialStatus As String, attendanceService As IAttendanceService)
            _attendanceId = attendanceId
            _targetUserId = userId
            _staffName = staffName
            _attendanceDate = attendanceDate
            _attendanceService = attendanceService

            InitializeComponent()
            PrepopulateValues(initialClockIn, initialClockOut, initialStatus)
        End Sub

        Private Sub InitializeComponent()
            Me.SuspendLayout()
            Me.Size = New Size(480, 430)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.Text = "Administrative Manual Attendance Correction"
            Me.BackColor = ThemeConstants.WorkspaceBackground
            Me.Padding = New Padding(16)

            ' Header Krypton Panel
            pnlHeader = New KryptonPanel() With {
                .Dock = DockStyle.Top,
                .Height = 44,
                .Padding = New Padding(12, 8, 12, 8)
            }
            pnlHeader.StateCommon.Color1 = Color.FromArgb(15, 23, 42)
            pnlHeader.StateCommon.Color2 = Color.FromArgb(15, 23, 42)

            lblTitle = New Label() With {
                .Text = "Administrative Attendance Correction",
                .Font = New Font(ThemeConstants.FontNameDefault, 10.5!, FontStyle.Bold),
                .ForeColor = Color.White,
                .BackColor = Color.Transparent,
                .Location = New Point(12, 10),
                .AutoSize = True
            }
            pnlHeader.Controls.Add(lblTitle)

            ' Staff & Date Info Labels
            lblStaffInfo = New Label() With {
                .Text = $"Staff Member: {_staffName} (User ID: {_targetUserId})",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(16, 56),
                .AutoSize = True
            }

            lblDateInfo = New Label() With {
                .Text = $"Attendance Date: {_attendanceDate:dddd, dd MMM yyyy}",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular),
                .ForeColor = ThemeConstants.TextSecondary,
                .Location = New Point(16, 78),
                .AutoSize = True
            }

            ' Clock In Controls
            lblClockIn = New Label() With {
                .Text = "Punch In Time:",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold),
                .Location = New Point(16, 110),
                .AutoSize = True
            }

            dtpClockIn = New KryptonDateTimePicker() With {
                .Format = DateTimePickerFormat.Custom,
                .CustomFormat = "hh:mm tt",
                .ShowUpDown = True,
                .Location = New Point(130, 107),
                .Width = 120
            }

            ' Clock Out Controls
            chkEnableClockOut = New KryptonCheckBox() With {
                .Text = "Set Punch Out:",
                .Location = New Point(16, 144),
                .AutoSize = True,
                .Checked = True
            }

            dtpClockOut = New KryptonDateTimePicker() With {
                .Format = DateTimePickerFormat.Custom,
                .CustomFormat = "hh:mm tt",
                .ShowUpDown = True,
                .Location = New Point(130, 141),
                .Width = 120
            }

            AddHandler chkEnableClockOut.CheckedChanged, Sub(s, e) dtpClockOut.Enabled = chkEnableClockOut.Checked

            ' Break Minutes
            lblBreak = New Label() With {
                .Text = "Break Mins:",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold),
                .Location = New Point(270, 110),
                .AutoSize = True
            }

            numBreakMinutes = New KryptonNumericUpDown() With {
                .Minimum = 0,
                .Maximum = 480,
                .Value = 0,
                .Location = New Point(350, 107),
                .Width = 80
            }

            ' Status
            lblStatus = New Label() With {
                .Text = "Status:",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold),
                .Location = New Point(270, 144),
                .AutoSize = True
            }

            cboStatus = New KryptonComboBox() With {
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Location = New Point(325, 141),
                .Width = 105
            }
            cboStatus.Items.AddRange(New Object() {"Present", "Late", "Early Leave", "Late & Early"})
            cboStatus.SelectedIndex = 0

            ' Mandatory Reason
            lblReason = New Label() With {
                .Text = "Mandatory Correction Reason (Required for Audit Log):",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.DangerRed,
                .Location = New Point(16, 182),
                .AutoSize = True
            }

            txtReason = New KryptonTextBox() With {
                .Multiline = True,
                .ScrollBars = ScrollBars.Vertical,
                .Location = New Point(16, 202),
                .Size = New Size(430, 70)
            }

            ' Krypton Buttons
            btnSave = New KryptonButton() With {
                .Text = "Save Correction",
                .Location = New Point(210, 314),
                .Size = New Size(130, 34)
            }

            btnCancel = New KryptonButton() With {
                .Text = "Cancel",
                .Location = New Point(350, 314),
                .Size = New Size(95, 34)
            }

            ThemeConstants.ApplyKryptonPrimaryButton(btnSave)
            ThemeConstants.ApplyKryptonSecondaryButton(btnCancel)

            ThemeConstants.ApplyKryptonInputStyle(dtpClockIn)
            ThemeConstants.ApplyKryptonInputStyle(dtpClockOut)
            ThemeConstants.ApplyKryptonInputStyle(cboStatus)
            ThemeConstants.ApplyKryptonInputStyle(txtReason)

            AddHandler btnSave.Click, AddressOf btnSave_Click
            AddHandler btnCancel.Click, Sub(s, e) Me.DialogResult = DialogResult.Cancel

            Me.Controls.Add(btnCancel)
            Me.Controls.Add(btnSave)
            Me.Controls.Add(txtReason)
            Me.Controls.Add(lblReason)
            Me.Controls.Add(cboStatus)
            Me.Controls.Add(lblStatus)
            Me.Controls.Add(numBreakMinutes)
            Me.Controls.Add(lblBreak)
            Me.Controls.Add(dtpClockOut)
            Me.Controls.Add(chkEnableClockOut)
            Me.Controls.Add(dtpClockIn)
            Me.Controls.Add(lblClockIn)
            Me.Controls.Add(lblDateInfo)
            Me.Controls.Add(lblStaffInfo)
            Me.Controls.Add(pnlHeader)

            Me.ResumeLayout(False)
        End Sub

        Private Sub PrepopulateValues(initialIn As String, initialOut As String, initialSt As String)
            Dim baseDate = _attendanceDate.Date

            Dim parsedIn As DateTime
            If DateTime.TryParse(initialIn, parsedIn) Then
                dtpClockIn.Value = New DateTime(baseDate.Year, baseDate.Month, baseDate.Day, parsedIn.Hour, parsedIn.Minute, 0)
            Else
                dtpClockIn.Value = New DateTime(baseDate.Year, baseDate.Month, baseDate.Day, 9, 30, 0)
            End If

            Dim parsedOut As DateTime
            If DateTime.TryParse(initialOut, parsedOut) Then
                chkEnableClockOut.Checked = True
                dtpClockOut.Value = New DateTime(baseDate.Year, baseDate.Month, baseDate.Day, parsedOut.Hour, parsedOut.Minute, 0)
            Else
                chkEnableClockOut.Checked = False
                dtpClockOut.Value = New DateTime(baseDate.Year, baseDate.Month, baseDate.Day, 18, 30, 0)
                dtpClockOut.Enabled = False
            End If

            If cboStatus.Items.Contains(initialSt) Then
                cboStatus.SelectedItem = initialSt
            Else
                cboStatus.SelectedIndex = 0
            End If
        End Sub

        Private Async Sub btnSave_Click(sender As Object, e As EventArgs)
            Try
                Dim reason = txtReason.Text.Trim()
                If String.IsNullOrWhiteSpace(reason) Then
                    Forms.Common.FrmInAppAlert.ShowModal(Me, "Reason Required", "Please enter a valid correction reason before saving manual attendance edits.", Forms.Common.AlertType.WarningAlert, actionText:="OK")
                    txtReason.Focus()
                    Return
                End If

                Dim adminUserId As Integer = 1
                If CurrentUserContext.IsAuthenticated Then
                    adminUserId = CurrentUserContext.CurrentUser.UserId
                End If

                Dim pInTime = New DateTime(_attendanceDate.Year, _attendanceDate.Month, _attendanceDate.Day, dtpClockIn.Value.Hour, dtpClockIn.Value.Minute, 0)
                Dim pOutTime As Nullable(Of DateTime) = Nothing
                If chkEnableClockOut.Checked Then
                    pOutTime = New DateTime(_attendanceDate.Year, _attendanceDate.Month, _attendanceDate.Day, dtpClockOut.Value.Hour, dtpClockOut.Value.Minute, 0)
                End If

                Dim dto As New AttendanceCorrectionDto With {
                    .AttendanceId = _attendanceId,
                    .UserId = _targetUserId,
                    .AttendanceDate = _attendanceDate,
                    .ClockInTime = pInTime,
                    .ClockOutTime = pOutTime,
                    .TotalBreakMinutes = CInt(numBreakMinutes.Value),
                    .Status = cboStatus.SelectedItem.ToString(),
                    .Reason = reason,
                    .AdminUserId = adminUserId
                }

                Me.Cursor = Cursors.WaitCursor
                Dim success = Await _attendanceService.CorrectStaffAttendanceAsync(adminUserId, dto)

                If success Then
                    Forms.Common.FrmInAppAlert.ShowModal(Me, "Correction Saved", $"Attendance correction successfully saved for {_staffName}!{vbCrLf}Audit trail logged.", Forms.Common.AlertType.SuccessAlert, actionText:="OK")
                    Me.DialogResult = DialogResult.OK
                    Me.Close()
                End If
            Catch ex As Exception
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Error", "Failed to save correction: " & ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Sub
    End Class
End Namespace
