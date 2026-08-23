Option Strict On
Option Explicit On

Imports System

Namespace DTOs
    ''' <summary>
    ''' Data Transfer Object for Administrative Manual Attendance Corrections.
    ''' Carries updated punch timestamps, status overrides, and mandatory justification reasons.
    ''' </summary>
    Public Class AttendanceCorrectionDto
        Public Property AttendanceId As Integer
        Public Property UserId As Integer
        Public Property AttendanceDate As DateTime
        Public Property ClockInTime As DateTime
        Public Property ClockOutTime As Nullable(Of DateTime)
        Public Property TotalBreakMinutes As Integer
        Public Property Status As String = String.Empty
        Public Property Reason As String = String.Empty
        Public Property AdminUserId As Integer
    End Class
End Namespace
