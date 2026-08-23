Imports StaffAutomation.Core.Enums

Namespace DTOs
    ''' <summary>
    ''' Data Transfer Object for client discussion and communication records.
    ''' </summary>
    Public Class DiscussionDto
        Public Property DiscussionId As Integer
        Public Property ClientId As Integer
        Public Property ClientName As String = String.Empty
        Public Property TaskId As Nullable(Of Integer)
        Public Property TaskCode As String = String.Empty
        Public Property UserId As Integer
        Public Property UserName As String = String.Empty
        Public Property Channel As CommunicationType
        Public Property Outcome As CommunicationOutcome
        Public Property DiscussionNotes As String = String.Empty
        Public Property DurationMinutes As Integer
        Public Property DiscussionTimestamp As DateTime
    End Class
End Namespace
