Namespace Entities
    ''' <summary>
    ''' Supporting Attendance Entity tracking employee daily clock-in, clock-out, and breaks.
    ''' </summary>
    Public Class AttendanceEntity
        Public Property AttendanceId As Integer
        Public Property UserId As Integer
        Public Property AttendanceDate As DateTime
        Public Property ClockInTime As DateTime
        Public Property ClockOutTime As Nullable(Of DateTime)
        Public Property BreakStartTime As Nullable(Of DateTime)
        Public Property TotalBreakMinutes As Integer
        Public Property TotalWorkingMinutes As Integer
        Public Property Status As String = "Present"
        Public Property ClientIP As String = String.Empty
        Public Property CreatedOn As DateTime = DateTime.UtcNow
        Public Property CreatedBy As Integer

        Public Property IsManuallyCorrected As Boolean = False
        Public Property CorrectionReason As String = String.Empty
        Public Property CorrectedBy As Nullable(Of Integer)
        Public Property CorrectedOn As Nullable(Of DateTime)
    End Class
End Namespace
