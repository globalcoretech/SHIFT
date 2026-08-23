Imports System
Imports System.Threading.Tasks
Imports StaffAutomation.Core.DTOs

Namespace Interfaces
    ''' <summary>
    ''' Business Logic Service contract for employee Attendance Clock-In/Out and hours reconciliation.
    ''' </summary>
    Public Interface IAttendanceService
        Function ClockInUserAsync(userId As Integer, clientIP As String) As Task(Of AttendanceDto)
        Function ClockOutUserAsync(userId As Integer, totalBreakMinutes As Integer) As Task(Of Boolean)
        Function StartLunchBreakAsync(userId As Integer) As Task(Of Boolean)
        Function EndLunchBreakAsync(userId As Integer) As Task(Of Boolean)
        Function GetTodayAttendanceForUserAsync(userId As Integer) As Task(Of AttendanceDto)
        Function GetAllAttendanceHistoryAsync() As Task(Of List(Of AttendanceDto))
        Function ResetTodayAttendanceAsync(userId As Integer) As Task(Of Boolean)
        Function GetUserAttendanceHistoryAsync(userId As Integer) As Task(Of List(Of AttendanceDto))
        Function GetTodayStaffOverviewAsync(attendanceDate As DateTime, Optional departmentId As Nullable(Of Integer) = Nothing, Optional searchQuery As String = Nothing) As Task(Of List(Of AdminAttendanceOverviewDto))
        Function CorrectStaffAttendanceAsync(adminUserId As Integer, dto As AttendanceCorrectionDto) As Task(Of Boolean)
    End Interface
End Namespace
