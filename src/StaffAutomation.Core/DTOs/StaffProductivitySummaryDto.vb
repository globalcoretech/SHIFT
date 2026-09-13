Imports System

Namespace DTOs
    ''' <summary>
    ''' Data Transfer Object representing staff attendance vs. productive task work comparison metrics.
    ''' </summary>
    Public Class StaffProductivitySummaryDto
        Public Property UserId As Integer
        Public Property UserName As String = String.Empty
        Public Property DepartmentName As String = String.Empty
        Public Property WorkingDaysCount As Integer
        Public Property AttendanceWorkingMinutes As Integer
        Public Property AttendanceBreakMinutes As Integer
        Public Property ProductiveTaskMinutes As Integer
        Public Property CompletedTasksCount As Integer

        Public ReadOnly Property AttendanceWorkingHours As Double
            Get
                Return Math.Round(AttendanceWorkingMinutes / 60.0, 2)
            End Get
        End Property

        Public ReadOnly Property AttendanceBreakHours As Double
            Get
                Return Math.Round(AttendanceBreakMinutes / 60.0, 2)
            End Get
        End Property

        Public ReadOnly Property ProductiveTaskHours As Double
            Get
                Return Math.Round(ProductiveTaskMinutes / 60.0, 2)
            End Get
        End Property

        Public ReadOnly Property AvgMinutesPerCompletedTask As Double
            Get
                If CompletedTasksCount <= 0 Then Return 0.0
                Return Math.Round(ProductiveTaskMinutes / CDbl(CompletedTasksCount), 1)
            End Get
        End Property

        ''' <summary>
        ''' Productivity Ratio % = (ProductiveTaskMinutes / NetAvailableMinutes) * 100
        ''' NetAvailableMinutes = AttendanceWorkingMinutes - AttendanceBreakMinutes
        ''' Returns 0 if NetAvailableMinutes is less than or equal to 0.
        ''' </summary>
        Public ReadOnly Property ProductivityRatioPercentage As Double
            Get
                Dim netAvailable = AttendanceWorkingMinutes - AttendanceBreakMinutes
                If netAvailable <= 0 Then Return 0.0
                Return Math.Round((ProductiveTaskMinutes / CDbl(netAvailable)) * 100.0, 1)
            End Get
        End Property
    End Class
End Namespace
