Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.Core.DTOs

Namespace Interfaces
    ''' <summary>
    ''' Business Logic Service contract for structured client communication logging.
    ''' </summary>
    Public Interface IDiscussionService
        Function LogDiscussionAsync(discussionDto As DiscussionDto) As Task(Of Integer)
        Function GetDiscussionsForClientAsync(clientId As Integer) As Task(Of List(Of DiscussionDto))
        Function GetDiscussionsForTaskAsync(taskId As Integer) As Task(Of List(Of DiscussionDto))
    End Interface
End Namespace
