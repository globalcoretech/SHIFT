Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports StaffAutomation.Core.DTOs

Namespace UIHelpers
    ''' <summary>
    ''' Presentation model holding mapped UI strings, badge colors, guidance text, and formatted durations for AttendanceControl.
    ''' </summary>
    Public Class AttendanceUiPresentationModel
        Public Property StatusBadgeText As String = String.Empty
        Public Property StatusBadgeColor As Color = Color.Black
        Public Property StatusSubText As String = String.Empty
        Public Property NextActionText As String = String.Empty
        Public Property ExplanationBannerText As String = String.Empty
        
        Public Property FormattedNetWorked As String = "00:00:00"
        Public Property FormattedGrossWorked As String = "00:00:00"
        Public Property FormattedLunchDeducted As String = "00:00:00"
        Public Property FormattedRemaining As String = "00:00:00"
        Public Property FormattedExpectedExit As String = "--"
        Public Property FormattedOvertime As String = "00:00:00"
        
        Public Property ProgressText As String = "0% (0h 0m / 8h 0m)"
        Public Property ProgressPercentage As Integer = 0
        Public Property LunchStatusText As String = String.Empty
    End Class

    ''' <summary>
    ''' Dedicated presentation mapper translating pure domain facts from WorkdayProgressResult into UI presentation models.
    ''' Keeps BLL calculation engine 100% free of UI text and ready for future localization.
    ''' </summary>
    Public Class AttendanceUiPresenter
        Public Shared Function MapToPresentation(result As WorkdayProgressResult) As AttendanceUiPresentationModel
            If result Is Nothing Then Return New AttendanceUiPresentationModel()

            Dim model As New AttendanceUiPresentationModel With {
                .FormattedNetWorked = FormatMinutes(result.NetWorkedMinutes),
                .FormattedGrossWorked = FormatMinutes(result.GrossWorkedMinutes),
                .FormattedLunchDeducted = FormatMinutes(result.LunchDeductedMinutes),
                .FormattedRemaining = FormatMinutes(result.RemainingMinutes),
                .FormattedOvertime = FormatMinutes(result.OvertimeMinutes),
                .FormattedExpectedExit = If(result.ExpectedExitTime.HasValue, result.ExpectedExitTime.Value.ToString("hh:mm tt"), "--"),
                .ProgressPercentage = Math.Max(0, Math.Min(100, result.ProgressPercentage)),
                .ProgressText = $"{result.ProgressPercentage}% ({FormatHoursShort(result.NetWorkedMinutes)} / {FormatHoursShort(result.TargetWorkingMinutes)})"
            }

            Select Case result.AttendanceState
                Case "NotPunchedIn"
                    model.StatusBadgeText = "🔴 Not Punched In"
                    model.StatusBadgeColor = Color.FromArgb(239, 68, 68)
                    model.StatusSubText = "Your attendance has not started today."
                    model.NextActionText = "Next Action: Click Punch In to begin today's attendance."
                    model.ExplanationBannerText = "👉 Click PUNCH IN to start your workday."

                Case "Working"
                    model.StatusBadgeText = "🟢 Working"
                    model.StatusBadgeColor = Color.FromArgb(16, 185, 129)
                    model.StatusSubText = "You are currently clocked in."
                    model.NextActionText = "Next Action: Click LUNCH OUT when leaving for lunch, or PUNCH OUT when leaving office."
                    model.ExplanationBannerText = "⏱️ You are working. Click LUNCH OUT for lunch break or PUNCH OUT at end of day."

                Case "OnBreak"
                    model.StatusBadgeText = "🥪 On Lunch Break"
                    model.StatusBadgeColor = Color.FromArgb(217, 119, 6) ' Warm Amber
                    model.StatusSubText = "You are currently on Lunch Break."
                    model.NextActionText = "Next Action: Click RESUME WORK when returning from lunch."
                    model.ExplanationBannerText = "🥪 You are on Lunch Break. Click RESUME WORK when returning to office."

                Case "DayCompleted"
                    model.StatusBadgeText = "⚪ Day Completed"
                    model.StatusBadgeColor = Color.FromArgb(100, 116, 139)
                    model.StatusSubText = "Today's attendance has been completed."
                    model.NextActionText = "Next Action: Attendance completed for today."
                    model.ExplanationBannerText = "✅ Today's attendance has been completed."
            End Select

            If result.TotalBreakMinutes > 0 OrElse result.BreakStartTime.HasValue Then
                Dim overbreakWarning = If(result.IsOverbreak, " ⚠️ Overbreak Exceeded!", "")
                model.LunchStatusText = $"Lunch Break: {FormatHoursShort(result.TotalBreakMinutes)} taken (Max: 1h){overbreakWarning}"
            Else
                Select Case result.LunchState
                    Case "BeforeLunch"
                        model.LunchStatusText = "Lunch: 02:00 PM – 03:00 PM (Manual Lunch Out / In active)"
                    Case "LunchInProgress"
                        model.LunchStatusText = "Lunch: In Progress (Click RESUME WORK)"
                    Case "LunchCompleted"
                        model.LunchStatusText = "Lunch: Completed"
                    Case Else
                        model.LunchStatusText = "Lunch: N/A"
                End Select
            End If

            Return model
        End Function

        Public Shared Function FormatMinutes(totalMins As Integer) As String
            If totalMins <= 0 Then Return "00:00:00"
            Dim hrs = totalMins \ 60
            Dim mins = totalMins Mod 60
            Return $"{hrs:D2}:{mins:D2}:00"
        End Function

        Public Shared Function FormatHoursShort(totalMins As Integer) As String
            If totalMins <= 0 Then Return "0h 0m"
            Dim hrs = totalMins \ 60
            Dim mins = totalMins Mod 60
            If hrs = 0 Then
                Return $"{mins}m"
            ElseIf mins = 0 Then
                Return $"{hrs}h"
            Else
                Return $"{hrs}h {mins}m"
            End If
        End Function
    End Class
End Namespace
