Option Strict On
Option Explicit On

Imports System
Imports StaffAutomation.Core.Configuration
Imports StaffAutomation.Core.DTOs

Namespace Interfaces
    ''' <summary>
    ''' Core calculation engine interface for evaluating workday metrics, lunch deductions, dynamic exit times,
    ''' progress ratios, overtime, and timeline collections against policy configurations.
    ''' </summary>
    Public Interface IAttendancePolicyEngine
        Function CalculateProgress(clockInTime As Nullable(Of DateTime),
                                  clockOutTime As Nullable(Of DateTime),
                                  currentTime As DateTime,
                                  policy As AttendancePolicyConfig,
                                  Optional breakStartTime As Nullable(Of DateTime) = Nothing,
                                  Optional totalBreakMinutes As Integer = 0) As WorkdayProgressResult

        Function CalculateGrossMinutes(shiftStart As DateTime, shiftEnd As DateTime) As Integer
        Function CalculateLunchDeduction(shiftStart As DateTime, shiftEnd As DateTime, policy As AttendancePolicyConfig) As Integer
        Function CalculateExpectedExit(clockInTime As DateTime, policy As AttendancePolicyConfig) As DateTime
        Function CalculateRemainingTime(netWorkedMins As Integer, targetMins As Integer) As Integer
        Function CalculateOvertime(netWorkedMins As Integer, targetMins As Integer) As Integer
        Function CalculateProgressPercentage(netWorkedMins As Integer, targetMins As Integer) As Integer
    End Interface
End Namespace
