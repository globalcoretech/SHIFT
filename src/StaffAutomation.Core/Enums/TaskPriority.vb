Namespace Enums
    ''' <summary>
    ''' Task priority level driving workspace sorting and visual color codes.
    ''' </summary>
    Public Enum TaskPriority
        ''' <summary>
        ''' Standard task with flexible statutory timeframe.
        ''' </summary>
        Low = 1

        ''' <summary>
        ''' Regular operational task.
        ''' </summary>
        Medium = 2

        ''' <summary>
        ''' High priority task nearing target due date.
        ''' </summary>
        High = 3

        ''' <summary>
        ''' Critical statutory deadline or overdue task requiring immediate action.
        ''' </summary>
        Urgent = 4
    End Enum
End Namespace
