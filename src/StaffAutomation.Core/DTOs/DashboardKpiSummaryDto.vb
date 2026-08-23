Namespace DTOs
    ''' <summary>
    ''' Data Transfer Object encapsulating the 12 measurable executive KPIs for Owner Dashboard.
    ''' </summary>
    Public Class DashboardKpiSummaryDto
        Public Property ActiveTasksCount As Integer
        Public Property PendingTasksCount As Integer
        Public Property CompletedTodayCount As Integer
        Public Property OverdueTasksCount As Integer
        Public Property WaitingForClientCount As Integer
        Public Property WaitingForDocumentsCount As Integer
        Public Property DepartmentProductivityAccounting As Double
        Public Property DepartmentProductivityTax As Double
        Public Property OverallEmployeeProductivity As Double
        Public Property AverageCompletionTimeHours As Double
        Public Property AverageDiscussionTimeMinutes As Double
        Public Property TaskCompletionRatePercentage As Double
        Public Property LastRefreshedTimestamp As DateTime = DateTime.Now
    End Class
End Namespace
