Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Entities

Namespace Interfaces
    ''' <summary>
    ''' Data Access Repository contract for chronological Task Activity execution logs.
    ''' </summary>
    Public Interface ITaskActivityRepository
        Function GetByIdAsync(activityId As Integer) As Task(Of TaskActivityEntity)
        Function GetActivitiesByTaskAsync(taskId As Integer) As Task(Of List(Of TaskActivityEntity))
        Function GetActiveActivityForUserAsync(userId As Integer) As Task(Of TaskActivityEntity)
        Function GetDailyTimelineByUserAsync(userId As Integer, activityDate As DateTime) As Task(Of List(Of TaskActivityEntity))
        Function AddAsync(activity As TaskActivityEntity, Optional transaction As IDbTransaction = Nothing) As Task(Of Integer)
        Function AddChecklistCompletionIdempotentAsync(activity As TaskActivityEntity) As Task(Of Integer)
        Function CompleteActiveTimerAsync(activityId As Integer, endTime As DateTime, durationMinutes As Integer) As Task(Of Boolean)
    End Interface
End Namespace
