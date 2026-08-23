Option Strict On
Option Explicit On

Imports System
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports StaffAutomation.BLL.Interfaces
Imports StaffAutomation.BLL.Services
Imports StaffAutomation.Core.Configuration
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Interfaces

Namespace StaffAutomation.Tests
    ''' <summary>
    ''' Unit tests for Attendance Module State Machine, Policy Engine, and Progress Ratio Calculations.
    ''' </summary>
    <TestClass>
    Public Class AttendanceWorkflowTests
        <TestMethod>
        Public Sub TestAttendanceProgressCalculations()
            Dim policyEngine As IAttendancePolicyEngine = New AttendancePolicyEngine()
            Dim policy As New AttendancePolicyConfig() With {
                .TargetWorkingMinutes = 480,
                .OfficeStartTime = New TimeSpan(9, 30, 0),
                .OfficeEndTime = New TimeSpan(18, 30, 0),
                .GraceMinutes = 15,
                .EnableManualBreakPunch = True
            }

            Dim today = DateTime.Today
            Dim now = today.AddHours(14) ' 02:00 PM
            Dim clockIn = today.AddHours(9).AddMinutes(30) ' 09:30 AM

            ' 1. Test Working State
            Dim resultWorking = policyEngine.CalculateProgress(clockIn, Nothing, now, policy, Nothing, 0)
            If resultWorking.AttendanceState <> "Working" Then
                Throw New InvalidOperationException("Attendance state should be 'Working'.")
            End If

            If resultWorking.GrossWorkedMinutes <> 270 Then ' 09:30 AM to 02:00 PM = 270 mins
                Throw New InvalidOperationException($"Expected gross 270 mins, got {resultWorking.GrossWorkedMinutes}")
            End If

            If resultWorking.RemainingMinutes <> 210 Then ' 480 - 270 = 210 mins remaining
                Throw New InvalidOperationException($"Expected remaining 210 mins, got {resultWorking.RemainingMinutes}")
            End If

            ' 2. Test On Lunch Break State
            Dim breakStart = today.AddHours(13).AddMinutes(30) ' 01:30 PM
            Dim resultBreak = policyEngine.CalculateProgress(clockIn, Nothing, now, policy, breakStart, 0)

            If resultBreak.AttendanceState <> "OnBreak" Then
                Throw New InvalidOperationException("Attendance state should be 'OnBreak'.")
            End If

            If Not resultBreak.CanPunchBreakIn Then
                Throw New InvalidOperationException("CanPunchBreakIn must be True when on lunch break (Resume Work CTA enabled).")
            End If

            ' 3. Test Day Completed State
            Dim clockOut = today.AddHours(18).AddMinutes(30) ' 06:30 PM
            Dim resultCompleted = policyEngine.CalculateProgress(clockIn, clockOut, now, policy, Nothing, 30)

            If resultCompleted.AttendanceState <> "DayCompleted" Then
                Throw New InvalidOperationException("Attendance state should be 'DayCompleted'.")
            End If

            If resultCompleted.NetWorkedMinutes <> 510 Then ' 540 gross - 30 break = 510 net
                Throw New InvalidOperationException($"Expected net 510 mins, got {resultCompleted.NetWorkedMinutes}")
            End If

            If resultCompleted.ProgressPercentage <> 100 Then
                Throw New InvalidOperationException($"Progress percentage should be strictly capped at 100%, got {resultCompleted.ProgressPercentage}")
            End If
        End Sub

        <TestMethod>
        Public Sub TestLateAndEarlyThresholds()
            Dim policyEngine As IAttendancePolicyEngine = New AttendancePolicyEngine()
            Dim policy As New AttendancePolicyConfig() With {
                .OfficeStartTime = New TimeSpan(9, 30, 0),
                .OfficeEndTime = New TimeSpan(18, 30, 0),
                .GraceMinutes = 15
            }

            Dim today = DateTime.Today

            ' Punch In at 09:50 AM -> Should be Late (> 09:45 AM threshold)
            Dim lateClockIn = today.AddHours(9).AddMinutes(50)
            Dim resultLate = policyEngine.CalculateProgress(lateClockIn, Nothing, today.AddHours(12), policy, Nothing, 0)
            If Not resultLate.IsLate Then
                Throw New InvalidOperationException("Clock In at 09:50 AM should be flagged as Late.")
            End If

            ' Punch In at 09:40 AM -> Should NOT be Late (within 15 min grace of 09:30 AM)
            Dim onTimeClockIn = today.AddHours(9).AddMinutes(40)
            Dim resultOnTime = policyEngine.CalculateProgress(onTimeClockIn, Nothing, today.AddHours(12), policy, Nothing, 0)
            If resultOnTime.IsLate Then
                Throw New InvalidOperationException("Clock In at 09:40 AM should NOT be flagged as Late.")
            End If
        End Sub
    End Class
End Namespace
