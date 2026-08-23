Imports StaffAutomation.Core.Enums

Namespace Entities
    ''' <summary>
    ''' Chronological Task Activity Entity capturing individual work execution segments on a task.
    ''' </summary>
    Public Class TaskActivityEntity
        Public Property ActivityId As Integer
        Public Property TaskId As Integer
        Public Property UserId As Integer
        Public Property Category As TimeCategory
        Public Property ActivityDescription As String = String.Empty
        Public Property StartTime As DateTime
        Public Property EndTime As Nullable(Of DateTime)
        Public Property DurationMinutes As Integer
        Public Property ActivityDate As DateTime
        Public Property CreatedOn As DateTime = DateTime.UtcNow
        Public Property CreatedBy As Integer
    End Class
End Namespace
