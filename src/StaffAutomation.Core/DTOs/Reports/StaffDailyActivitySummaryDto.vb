Imports System.Collections.Generic

Namespace DTOs.Reports
    Public Class StaffDailyActivitySummaryDto
        Public Property UserId As Integer
        Public Property StaffName As String
        Public Property TotalDaysPresent As Integer
        Public Property TotalWorkingHours As Double
        Public Property TotalAttendanceHours As Double
        Public Property Days As New List(Of StaffDailyActivityDayDto)()

        Public ReadOnly Property SummaryText As String
            Get
                Return $"{StaffName} ({TotalDaysPresent} days, {TotalWorkingHours:F1} hrs tasks)"
            End Get
        End Property
    End Class
End Namespace
