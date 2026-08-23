Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Entities

Namespace Interfaces
    ''' <summary>
    ''' Data Access Repository contract for Client Discussion and Communication logs.
    ''' </summary>
    Public Interface IDiscussionRepository
        Function GetByIdAsync(discussionId As Integer) As Task(Of DiscussionEntity)
        Function GetDiscussionsByClientAsync(clientId As Integer) As Task(Of List(Of DiscussionEntity))
        Function GetDiscussionsByTaskAsync(taskId As Integer) As Task(Of List(Of DiscussionEntity))
        Function AddAsync(discussion As DiscussionEntity) As Task(Of Integer)
    End Interface
End Namespace
