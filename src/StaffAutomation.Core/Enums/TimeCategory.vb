Namespace Enums
    ''' <summary>
    ''' Independent time category classification for productivity auditing.
    ''' </summary>
    Public Enum TimeCategory
        ''' <summary>
        ''' Direct technical execution and preparation time on task.
        ''' </summary>
        WorkTime = 1

        ''' <summary>
        ''' Client communication, discussion, and consultation time.
        ''' </summary>
        DiscussionTime = 2

        ''' <summary>
        ''' Non-execution time spent waiting for client response or documents.
        ''' </summary>
        WaitingTime = 3

        ''' <summary>
        ''' Personal break or meal time recorded via attendance.
        ''' </summary>
        BreakTime = 4
    End Enum
End Namespace
