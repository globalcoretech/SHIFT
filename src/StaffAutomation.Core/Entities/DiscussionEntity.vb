Imports StaffAutomation.Core.Enums

Namespace Entities
    ''' <summary>
    ''' Structured Client Discussion Entity capturing communication logs and outcomes.
    ''' </summary>
    Public Class DiscussionEntity
        Public Property DiscussionId As Integer
        Public Property ClientId As Integer
        Public Property TaskId As Nullable(Of Integer)
        Public Property UserId As Integer
        Public Property Channel As CommunicationType
        Public Property Outcome As CommunicationOutcome
        Public Property DiscussionNotes As String = String.Empty
        Public Property DurationMinutes As Integer
        Public Property DiscussionTimestamp As DateTime = DateTime.UtcNow
        Public Property CreatedOn As DateTime = DateTime.UtcNow
        Public Property CreatedBy As Integer
    End Class
End Namespace
