Imports System

Namespace DTOs
    ''' <summary>
    ''' Data Transfer Object representing a single chronological daily staff activity timeline item.
    ''' </summary>
    Public Class DailyTimelineItemDto
        Public Property ActivityId As Integer
        Public Property StaffName As String = String.Empty
        Public Property WorkDate As DateTime
        Public Property StartTime As DateTime
        Public Property EndTime As Nullable(Of DateTime)
        Public Property ClientName As String = String.Empty
        Public Property TaskCode As String = String.Empty
        Public Property TaskTitle As String = String.Empty
        Public Property TaskCategory As String = String.Empty
        Public Property ActivityDescription As String = String.Empty
        Public Property DurationMinutes As Integer
        Public Property ActivityStatus As String = "Completed"

        Public ReadOnly Property DurationHours As Double
            Get
                Return Math.Round(DurationMinutes / 60.0, 2)
            End Get
        End Property
    End Class
End Namespace
