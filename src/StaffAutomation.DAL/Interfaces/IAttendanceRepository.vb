Imports System
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Entities

Namespace Interfaces
    ''' <summary>
    ''' Data Access Repository contract for employee daily Attendance records.
    ''' </summary>
    Public Interface IAttendanceRepository
        Function GetTodayAttendanceAsync(userId As Integer, attendanceDate As DateTime) As Task(Of AttendanceEntity)
        Function GetAttendanceHistoryAsync(Optional startDate As Nullable(Of DateTime) = Nothing, Optional endDate As Nullable(Of DateTime) = Nothing) As Task(Of List(Of AttendanceEntity))
        Function ClockInAsync(attendance As AttendanceEntity) As Task(Of Integer)
        Function ClockOutAsync(attendanceId As Integer, clockOutTime As DateTime, totalBreakMinutes As Integer, Optional newStatus As String = Nothing) As Task(Of Boolean)
        Function StartLunchBreakAsync(attendanceId As Integer, breakStartTime As DateTime) As Task(Of Boolean)
        Function EndLunchBreakAsync(attendanceId As Integer, totalBreakMinutes As Integer) As Task(Of Boolean)
        Function ResetTodayAttendanceAsync(userId As Integer, attendanceDate As DateTime) As Task(Of Boolean)
        Function GetUserAttendanceHistoryAsync(userId As Integer, Optional startDate As Nullable(Of DateTime) = Nothing, Optional endDate As Nullable(Of DateTime) = Nothing) As Task(Of List(Of AttendanceEntity))
        Function CorrectAttendanceByAdminAsync(attendanceId As Integer, clockInTime As DateTime, clockOutTime As Nullable(Of DateTime), totalBreakMinutes As Integer, status As String, reason As String, adminUserId As Integer) As Task(Of Boolean)
    End Interface
End Namespace
