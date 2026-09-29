Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Entities

Namespace Interfaces
    ''' <summary>
    ''' Contract for Task Checklist data access operations.
    ''' </summary>
    Public Interface ITaskChecklistRepository
        Function GetChecklistItemByIdAsync(checklistId As Integer) As Task(Of TaskChecklistItemEntity)
        Function GetChecklistForTaskAsync(taskId As Integer) As Task(Of List(Of TaskChecklistItemEntity))
        Function AddChecklistItemsAsync(items As List(Of TaskChecklistItemEntity)) As Task
        Function UpdateChecklistItemAsync(item As TaskChecklistItemEntity) As Task
        Function DeleteChecklistItemsForTaskAsync(taskId As Integer) As Task
    End Interface
End Namespace
