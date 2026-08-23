Imports StaffAutomation.Core.Enums

Namespace DTOs
    ''' <summary>
    ''' Data Transfer Object for Task Activity logs forming the daily chronological timeline.
    ''' </summary>
    Public Class TaskActivityDto
        Public Property ActivityId As Integer
        Public Property TaskId As Integer
        Public Property TaskCode As String = String.Empty
        Public Property ClientName As String = String.Empty
        Public Property UserId As Integer
        Public Property UserName As String = String.Empty
        Public Property Category As TimeCategory
        Public Property ActivityDescription As String = String.Empty
        Public Property StartTime As DateTime
        Public Property EndTime As Nullable(Of DateTime)
        Public Property DurationMinutes As Integer
        Public Property ActivityDate As DateTime
    End Class
End Namespace
