Namespace DTOs
    ''' <summary>
    ''' Data Transfer Object for daily attendance and working hours reconciliation.
    ''' </summary>
    Public Class AttendanceDto
        Public Property AttendanceId As Integer
        Public Property UserId As Integer
        Public Property UserName As String = String.Empty
        Public Property AttendanceDate As DateTime
        Public Property ClockInTime As DateTime
        Public Property ClockOutTime As Nullable(Of DateTime)
        Public Property TotalBreakMinutes As Integer
        Public Property TotalWorkingMinutes As Integer
        Public Property BreakStartTime As Nullable(Of DateTime)
        Public Property Status As String = String.Empty
    End Class
End Namespace
