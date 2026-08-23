Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Enums

Namespace Interfaces
    ''' <summary>
    ''' Business Logic Service contract for employee daily timeline logging and stopwatch timer.
    ''' </summary>
    Public Interface ITimelineService
        Function StartTaskActivityTimerAsync(taskId As Integer, category As TimeCategory, description As String) As Task(Of Integer)
        Function StopActiveTimerAsync(activityId As Integer) As Task(Of Boolean)
        Function GetDailyTimelineForUserAsync(userId As Integer, activityDate As DateTime) As Task(Of List(Of TaskActivityDto))
    End Interface
End Namespace
