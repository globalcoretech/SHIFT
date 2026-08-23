Imports System
Imports System.Collections.Generic

Namespace DTOs
    ''' <summary>
    ''' Data Transfer Object for Compliance Automation Rules.
    ''' </summary>
    Public Class ComplianceRuleDto
        Public Property ComplianceRuleId As Integer
        Public Property ComplianceName As String = String.Empty
        Public Property Category As String = "GST" ' GST, TDS, Income Tax, Tax Audit, Corporate, Other
        Public Property Frequency As String = "Monthly" ' Monthly, Quarterly, Annual, Custom
        Public Property DueDateRuleDay As Integer = 11
        Public Property DueDateRuleMonth As Nullable(Of Integer)
        Public Property Priority As String = "High" ' Normal, High, Critical
        Public Property IsActive As Boolean = True
        Public Property CreatedOn As DateTime = DateTime.UtcNow
        Public Property CreatedBy As Integer = 1
        Public Property ModifiedOn As Nullable(Of DateTime)
        Public Property ModifiedBy As Nullable(Of Integer)

        ' List of alert schedules (e.g. 10 days before, 3 days before)
        Public Property AlertSchedules As New List(Of ComplianceAlertScheduleDto)()
    End Class

    Public Class ComplianceAlertScheduleDto
        Public Property AlertScheduleId As Integer
        Public Property ComplianceRuleId As Integer
        Public Property DaysBeforeDeadline As Integer
        Public Property AlertType As String = "Reminder" ' Reminder, Urgent, Overdue
        Public Property IsEnabled As Boolean = True
    End Class

    Public Class ComplianceAutomationHistoryDto
        Public Property HistoryId As Integer
        Public Property ComplianceRuleId As Integer
        Public Property ComplianceName As String = String.Empty
        Public Property CompliancePeriod As String = String.Empty
        Public Property DeadlineDate As DateTime
        Public Property TriggerDate As DateTime
        Public Property TaskId As Nullable(Of Integer)
        Public Property AlertGenerated As Boolean = True
        Public Property TaskGenerated As Boolean = True
        Public Property Status As String = "Triggered"
        Public Property CreatedOn As DateTime = DateTime.UtcNow
    End Class

    Public Class UpcomingComplianceAutomationPreviewDto
        Public Property RuleId As Integer
        Public Property ComplianceName As String = String.Empty
        Public Property PeriodName As String = String.Empty
        Public Property DeadlineDate As DateTime
        Public Property NextAlertDate As DateTime
        Public Property TaskCreationDate As DateTime
        Public Property Status As String = "Scheduled"
    End Class
End Namespace
