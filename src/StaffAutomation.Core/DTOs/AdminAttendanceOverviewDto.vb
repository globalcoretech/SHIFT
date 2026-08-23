Option Strict On
Option Explicit On

Imports System

Namespace DTOs
    ''' <summary>
    ''' Data Transfer Object for Admin Live Staff Attendance Board.
    ''' Joins staff user account info with today's attendance record and current active status.
    ''' </summary>
    Public Class AdminAttendanceOverviewDto
        Public Property UserId As Integer
        Public Property StaffName As String = String.Empty
        Public Property EmployeeCode As String = String.Empty
        Public Property DepartmentName As String = String.Empty
        Public Property AttendanceId As Nullable(Of Integer)
        Public Property AttendanceDate As DateTime = DateTime.Today
        Public Property ClockInTime As Nullable(Of DateTime)
        Public Property ClockOutTime As Nullable(Of DateTime)
        Public Property BreakStartTime As Nullable(Of DateTime)
        Public Property TotalBreakMinutes As Integer
        Public Property TotalWorkingMinutes As Integer
        Public Property Status As String = "Not Punched In"
        Public Property IsManuallyCorrected As Boolean = False
        Public Property CorrectionReason As String = String.Empty
        Public Property CorrectedByName As String = String.Empty

        ''' <summary>
        ''' Derived property indicating real-time active status for Admin Board monitoring.
        ''' </summary>
        Public ReadOnly Property LiveStateDisplay As String
            Get
                If Not ClockInTime.HasValue Then
                    Return "Not Punched In"
                ElseIf BreakStartTime.HasValue Then
                    Return "On Lunch Break"
                ElseIf ClockOutTime.HasValue Then
                    Return "Day Completed"
                Else
                    Return "Working"
                End If
            End Get
        End Property
    End Class
End Namespace
