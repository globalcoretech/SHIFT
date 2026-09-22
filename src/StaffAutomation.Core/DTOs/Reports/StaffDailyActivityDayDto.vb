Imports System.Collections.Generic
Imports System.Linq

Namespace DTOs.Reports
    Public Class StaffDailyActivityDayDto
        Public Property ActivityDate As Date
        Public Property HasAttendance As Boolean
        Public Property ClockInTime As Date?
        Public Property ClockOutTime As Date?
        Public Property BreakStartTime As Date?
        Public Property TotalBreakMinutes As Integer
        Public Property TotalWorkingMinutes As Integer ' From Attendance
        Public Property AttendanceStatus As String ' Present, Absent, Half Day, Leave
        
        Public Property Tasks As New List(Of StaffDailyActivityTaskDto)()
        
        Public ReadOnly Property TotalTaskDurationMinutes As Integer
            Get
                Return Tasks.Sum(Function(t) t.DurationMinutes)
            End Get
        End Property

        Public ReadOnly Property SummaryText As String
            Get
                Dim totalTaskTime = If(TotalTaskDurationMinutes > 0, $"{TotalTaskDurationMinutes \ 60}h {TotalTaskDurationMinutes Mod 60}m", "0h")
                
                If Not HasAttendance Then
                    Return $"{ActivityDate:dd MMM yyyy} - No Attendance Logged, Tasks: {totalTaskTime}"
                End If
                
                Dim clockInStr = If(ClockInTime.HasValue, ClockInTime.Value.ToString("h:mm tt"), "--")
                Dim clockOutStr = If(ClockOutTime.HasValue, ClockOutTime.Value.ToString("h:mm tt"), "--")
                Dim attTotal = If(TotalWorkingMinutes > 0, $"{TotalWorkingMinutes \ 60}h {TotalWorkingMinutes Mod 60}m", "0h")
                
                Return $"{ActivityDate:dd MMM yyyy} - In: {clockInStr}, Out: {clockOutStr}, Att: {attTotal} / Tasks: {totalTaskTime}"
            End Get
        End Property
    End Class
End Namespace
