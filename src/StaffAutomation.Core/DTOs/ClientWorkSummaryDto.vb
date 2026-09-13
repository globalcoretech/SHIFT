Imports System

Namespace DTOs
    ''' <summary>
    ''' Data Transfer Object representing client-wise work duration and task completion metrics.
    ''' </summary>
    Public Class ClientWorkSummaryDto
        Public Property ClientId As Integer
        Public Property ClientCode As String = String.Empty
        Public Property ClientName As String = String.Empty
        Public Property TotalTasksCount As Integer
        Public Property CompletedTasksCount As Integer
        Public Property AssignedStaffCount As Integer
        Public Property TotalProductiveMinutes As Integer
        Public ReadOnly Property TotalProductiveHours As Double
            Get
                Return Math.Round(TotalProductiveMinutes / 60.0, 2)
            End Get
        End Property
        Public ReadOnly Property AvgMinutesPerCompletedTask As Double
            Get
                If CompletedTasksCount <= 0 Then Return 0.0
                Return Math.Round(TotalProductiveMinutes / CDbl(CompletedTasksCount), 1)
            End Get
        End Property
    End Class
End Namespace
