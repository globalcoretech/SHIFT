Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Entities

Imports StaffAutomation.Core.DTOs

Namespace Interfaces
    ''' <summary>
    ''' Data Access Repository contract for chronological Task Activity execution logs and productivity aggregations.
    ''' </summary>
    Public Interface ITaskActivityRepository
        Function GetByIdAsync(activityId As Integer) As Task(Of TaskActivityEntity)
        Function GetActivitiesByTaskAsync(taskId As Integer) As Task(Of List(Of TaskActivityEntity))
        Function GetActiveActivityForUserAsync(userId As Integer) As Task(Of TaskActivityEntity)
        Function GetDailyTimelineByUserAsync(userId As Integer, activityDate As DateTime) As Task(Of List(Of TaskActivityEntity))
        Function AddAsync(activity As TaskActivityEntity, Optional transaction As IDbTransaction = Nothing) As Task(Of Integer)
        Function AddChecklistCompletionIdempotentAsync(activity As TaskActivityEntity) As Task(Of Integer)
        Function RemoveChecklistCompletionAsync(taskId As Integer, stepText As String) As Task(Of Boolean)
        Function CompleteActiveTimerAsync(activityId As Integer, endTime As DateTime, durationMinutes As Integer, Optional updatedDescription As String = Nothing) As Task(Of Boolean)
        Function GetChronologicalDailyTimelineAsync(startDate As DateTime, endDate As DateTime, Optional userId As Nullable(Of Integer) = Nothing) As Task(Of List(Of DailyTimelineItemDto))
        Function GetClientWorkSummaryReportAsync(startDate As DateTime, endDate As DateTime, Optional clientId As Nullable(Of Integer) = Nothing) As Task(Of List(Of ClientWorkSummaryDto))
        Function GetStaffProductivitySummaryReportAsync(startDate As DateTime, endDate As DateTime, Optional userId As Nullable(Of Integer) = Nothing) As Task(Of List(Of StaffProductivitySummaryDto))
        Function GetTotalWorkMinutesByTaskAsync(taskId As Integer) As Task(Of Integer)
    End Interface
End Namespace
