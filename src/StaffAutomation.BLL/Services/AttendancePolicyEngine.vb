Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports StaffAutomation.BLL.Interfaces
Imports StaffAutomation.Core.Configuration
Imports StaffAutomation.Core.DTOs

Namespace Services
    ''' <summary>
    ''' Modular, production-grade calculation engine enforcing office policies, dynamic lunch deductions,
    ''' dynamic expected exit calculations, capped progress ratios, and overtime tracking. Pure domain facts calculation service.
    ''' </summary>
    Public Class AttendancePolicyEngine
        Implements IAttendancePolicyEngine

        Public Function CalculateProgress(clockInTime As Nullable(Of DateTime),
                                          clockOutTime As Nullable(Of DateTime),
                                          currentTime As DateTime,
                                          policy As AttendancePolicyConfig,
                                          Optional breakStartTime As Nullable(Of DateTime) = Nothing,
                                          Optional totalBreakMinutes As Integer = 0) As WorkdayProgressResult Implements IAttendancePolicyEngine.CalculateProgress

            If policy Is Nothing Then policy = New AttendancePolicyConfig()

            Dim result As New WorkdayProgressResult With {
                .ClockInTime = clockInTime,
                .ClockOutTime = clockOutTime,
                .BreakStartTime = breakStartTime,
                .TotalBreakMinutes = totalBreakMinutes,
                .CurrentTime = currentTime,
                .TargetWorkingMinutes = policy.TargetWorkingMinutes,
                .OfficeClosingTime = currentTime.Date.Add(policy.OfficeEndTime)
            }

            ' 1. Determine Attendance State & Action Flags
            DetermineAttendanceState(result, policy)

            If Not clockInTime.HasValue Then
                ' State: Not Punched In
                result.RemainingMinutes = policy.TargetWorkingMinutes
                result.ProgressPercentage = 0
                result.TimelineItems = GenerateTimelineItems(result, policy)
                Return result
            End If

            Dim shiftEnd = If(clockOutTime.HasValue, clockOutTime.Value, currentTime)

            ' 2. Calculate Durations via Focused Helper Methods
            result.GrossWorkedMinutes = CalculateGrossMinutes(clockInTime.Value, shiftEnd)
            
            Dim effectiveBreakMinutes As Integer = 0
            If policy.EnableManualBreakPunch AndAlso (result.TotalBreakMinutes > 0 OrElse result.BreakStartTime.HasValue) Then
                effectiveBreakMinutes = result.TotalBreakMinutes
            Else
                result.LunchDeductedMinutes = CalculateLunchDeduction(clockInTime.Value, shiftEnd, policy)
                effectiveBreakMinutes = result.LunchDeductedMinutes
            End If

            ' If currently on active break, add current break duration
            If result.BreakStartTime.HasValue Then
                Dim activeBreakMins = CInt(Math.Max(0, Math.Floor((currentTime - result.BreakStartTime.Value).TotalMinutes)))
                effectiveBreakMinutes += activeBreakMins
            End If

            result.TotalBreakMinutes = effectiveBreakMinutes
            result.NetWorkedMinutes = Math.Max(0, result.GrossWorkedMinutes - effectiveBreakMinutes)
            result.RemainingMinutes = CalculateRemainingTime(result.NetWorkedMinutes, policy.TargetWorkingMinutes)
            result.OvertimeMinutes = CalculateOvertime(result.NetWorkedMinutes, policy.TargetWorkingMinutes)
            result.ProgressPercentage = CalculateProgressPercentage(result.NetWorkedMinutes, policy.TargetWorkingMinutes)
            result.ExpectedExitTime = CalculateExpectedExit(clockInTime.Value, policy)
            result.LunchState = DetermineLunchState(currentTime, policy)
            result.IsOverbreak = effectiveBreakMinutes > policy.MaxAllowedBreakMinutes

            ' 3. Flags for Overtime and HalfDay
            result.IsOvertime = result.OvertimeMinutes > 0
            If policy.EnableHalfDay Then
                ' Half Day threshold = 50% of target minutes
                result.IsHalfDay = result.NetWorkedMinutes < (policy.TargetWorkingMinutes \ 2)
            End If

            ' 4. Populate Dynamic Timeline Items
            result.TimelineItems = GenerateTimelineItems(result, policy)

            Return result
        End Function

        Public Function CalculateGrossMinutes(shiftStart As DateTime, shiftEnd As DateTime) As Integer Implements IAttendancePolicyEngine.CalculateGrossMinutes
            If shiftEnd < shiftStart Then Return 0
            Return CInt(Math.Floor((shiftEnd - shiftStart).TotalMinutes))
        End Function

        Public Function CalculateLunchDeduction(shiftStart As DateTime, shiftEnd As DateTime, policy As AttendancePolicyConfig) As Integer Implements IAttendancePolicyEngine.CalculateLunchDeduction
            If policy Is Nothing OrElse Not policy.EnableLunchDeduction OrElse shiftEnd <= shiftStart Then Return 0

            Dim lunchStart = shiftStart.Date.Add(policy.LunchStartTime)
            Dim lunchEnd = shiftStart.Date.Add(policy.LunchEndTime)

            Dim overlapStart = If(shiftStart > lunchStart, shiftStart, lunchStart)
            Dim overlapEnd = If(shiftEnd < lunchEnd, shiftEnd, lunchEnd)

            If overlapEnd > overlapStart Then
                Return CInt(Math.Floor((overlapEnd - overlapStart).TotalMinutes))
            End If
            Return 0
        End Function

        Public Function CalculateExpectedExit(clockInTime As DateTime, policy As AttendancePolicyConfig) As DateTime Implements IAttendancePolicyEngine.CalculateExpectedExit
            If policy Is Nothing Then Return clockInTime.AddHours(8)

            ' Base expected exit = ClockInTime + TargetWorkingMinutes
            Dim expected = clockInTime.AddMinutes(policy.TargetWorkingMinutes)

            If policy.EnableLunchDeduction Then
                Dim lunchStart = clockInTime.Date.Add(policy.LunchStartTime)
                Dim lunchEnd = clockInTime.Date.Add(policy.LunchEndTime)

                ' If Clock In is before lunch finish, full target shift will span lunch, so add lunch duration
                If clockInTime < lunchEnd Then
                    Dim lunchDuration = CInt((lunchEnd - lunchStart).TotalMinutes)
                    expected = expected.AddMinutes(lunchDuration)
                End If
            End If
            Return expected
        End Function

        Public Function CalculateRemainingTime(netWorkedMins As Integer, targetMins As Integer) As Integer Implements IAttendancePolicyEngine.CalculateRemainingTime
            Return Math.Max(0, targetMins - netWorkedMins)
        End Function

        Public Function CalculateOvertime(netWorkedMins As Integer, targetMins As Integer) As Integer Implements IAttendancePolicyEngine.CalculateOvertime
            Return Math.Max(0, netWorkedMins - targetMins)
        End Function

        Public Function CalculateProgressPercentage(netWorkedMins As Integer, targetMins As Integer) As Integer Implements IAttendancePolicyEngine.CalculateProgressPercentage
            If targetMins <= 0 Then Return 0
            Dim pct = CInt(Math.Round((netWorkedMins / CDbl(targetMins)) * 100))
            Return Math.Min(100, Math.Max(0, pct)) ' Strictly capped at 100%
        End Function

        Private Sub DetermineAttendanceState(result As WorkdayProgressResult, policy As AttendancePolicyConfig)
            If Not result.ClockInTime.HasValue Then
                result.AttendanceState = "NotPunchedIn"
                result.CanPunchIn = True
                result.CanPunchOut = False
                result.CanPunchBreakOut = False
                result.CanPunchBreakIn = False
                result.IsWorkdayCompleted = False
                result.IsLate = False
                result.IsEarlyLeave = False
            ElseIf Not result.ClockOutTime.HasValue Then
                If result.BreakStartTime.HasValue Then
                    ' State: On Lunch Break
                    result.AttendanceState = "OnBreak"
                    result.CanPunchIn = False
                    result.CanPunchOut = False
                    result.CanPunchBreakOut = False
                    result.CanPunchBreakIn = True
                    result.IsWorkdayCompleted = False
                Else
                    ' State: Working
                    result.AttendanceState = "Working"
                    result.CanPunchIn = False
                    result.CanPunchOut = True
                    result.CanPunchBreakOut = policy.EnableManualBreakPunch
                    result.CanPunchBreakIn = False
                    result.IsWorkdayCompleted = False
                End If

                ' Check late status based on office start + grace
                Dim lateThreshold = result.ClockInTime.Value.Date.Add(policy.OfficeStartTime).AddMinutes(policy.GraceMinutes)
                result.IsLate = result.ClockInTime.Value > lateThreshold
                result.IsEarlyLeave = False
            Else
                ' State: Day Completed
                result.AttendanceState = "DayCompleted"
                result.CanPunchIn = False
                result.CanPunchOut = False
                result.CanPunchBreakOut = False
                result.CanPunchBreakIn = False
                result.IsWorkdayCompleted = True
                result.ActualExitTime = result.ClockOutTime.Value

                Dim lateThreshold = result.ClockInTime.Value.Date.Add(policy.OfficeStartTime).AddMinutes(policy.GraceMinutes)
                result.IsLate = result.ClockInTime.Value > lateThreshold

                ' Check early departure (office end - grace)
                Dim earlyThreshold = result.ClockOutTime.Value.Date.Add(policy.OfficeEndTime).AddMinutes(-policy.GraceMinutes)
                result.IsEarlyLeave = result.ClockOutTime.Value < earlyThreshold
            End If
        End Sub

        Private Function DetermineLunchState(currentTime As DateTime, policy As AttendancePolicyConfig) As String
            If Not policy.EnableLunchDeduction Then Return "Disabled"
            Dim lunchStart = currentTime.Date.Add(policy.LunchStartTime)
            Dim lunchEnd = currentTime.Date.Add(policy.LunchEndTime)

            If currentTime < lunchStart Then
                Return "BeforeLunch"
            ElseIf currentTime >= lunchStart AndAlso currentTime < lunchEnd Then
                Return "LunchInProgress"
            Else
                Return "LunchCompleted"
            End If
        End Function

        Private Function GenerateTimelineItems(result As WorkdayProgressResult, policy As AttendancePolicyConfig) As List(Of TimelineItemDto)
            Dim list As New List(Of TimelineItemDto)()

            ' 1. Punch In Step
            Dim inItem As New TimelineItemDto With {.Title = "Punch In"}
            If result.ClockInTime.HasValue Then
                inItem.TimeText = result.ClockInTime.Value.ToString("hh:mm tt")
                inItem.SubText = If(result.IsLate, "Late Arrival", "On Time")
                inItem.IconSymbol = "🟢"
                inItem.IsCompleted = True
            Else
                inItem.TimeText = "Waiting..."
                inItem.SubText = $"Scheduled {result.CurrentTime.Date.Add(policy.OfficeStartTime):hh:mm tt}"
                inItem.IconSymbol = "⚪"
                inItem.IsCompleted = False
            End If
            list.Add(inItem)

            ' 2. Lunch Break Out Step
            Dim breakOutItem As New TimelineItemDto With {.Title = "Lunch Out"}
            If result.BreakStartTime.HasValue Then
                breakOutItem.TimeText = result.BreakStartTime.Value.ToString("hh:mm tt")
                breakOutItem.SubText = "On Lunch Break"
                breakOutItem.IconSymbol = "🥪"
                breakOutItem.IsCompleted = True
            ElseIf result.TotalBreakMinutes > 0 Then
                breakOutItem.TimeText = "Completed"
                breakOutItem.SubText = $"{result.TotalBreakMinutes} mins taken"
                breakOutItem.IconSymbol = "🥪"
                breakOutItem.IsCompleted = True
            Else
                breakOutItem.TimeText = "02:00 PM (Scheduled)"
                breakOutItem.SubText = "Optional Lunch Break"
                breakOutItem.IconSymbol = "⚪"
                breakOutItem.IsCompleted = False
            End If
            list.Add(breakOutItem)

            ' 3. Resume Work Step
            Dim resumeItem As New TimelineItemDto With {.Title = "Resume Work"}
            If result.TotalBreakMinutes > 0 AndAlso Not result.BreakStartTime.HasValue Then
                resumeItem.TimeText = "Resumed"
                resumeItem.SubText = "Work Restored"
                resumeItem.IconSymbol = "▶"
                resumeItem.IsCompleted = True
            Else
                resumeItem.TimeText = "Waiting..."
                resumeItem.SubText = "Pending Lunch Return"
                resumeItem.IconSymbol = "⚪"
                resumeItem.IsCompleted = False
            End If
            list.Add(resumeItem)

            ' 4. Punch Out Step
            Dim outItem As New TimelineItemDto With {.Title = "Punch Out"}
            If result.ClockOutTime.HasValue Then
                outItem.TimeText = result.ClockOutTime.Value.ToString("hh:mm tt")
                outItem.SubText = If(result.IsEarlyLeave, "Early Departure", "Shift Completed")
                outItem.IconSymbol = "🔴"
                outItem.IsCompleted = True
            Else
                outItem.TimeText = "Waiting..."
                outItem.SubText = If(result.ExpectedExitTime.HasValue, $"Expected {result.ExpectedExitTime.Value:hh:mm tt}", "Pending")
                outItem.IconSymbol = "⚪"
                outItem.IsCompleted = False
            End If
            list.Add(outItem)

            Return list
        End Function
    End Class
End Namespace
