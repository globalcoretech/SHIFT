Namespace Enums
    ''' <summary>
    ''' Workflow states enforcing the 11-state Task Lifecycle State Machine.
    ''' </summary>
    Public Enum TaskWorkflowState
        ''' <summary>
        ''' Task newly created in system; pending delegation.
        ''' </summary>
        NewTask = 1

        ''' <summary>
        ''' Task assigned to a specific staff member.
        ''' </summary>
        Assigned = 2

        ''' <summary>
        ''' Employee actively executing work on the task.
        ''' </summary>
        InProgress = 3

        ''' <summary>
        ''' Paused pending client response or consultation.
        ''' </summary>
        WaitingForClient = 4

        ''' <summary>
        ''' Paused pending client vouchers or supporting documents.
        ''' </summary>
        WaitingForDocuments = 5

        ''' <summary>
        ''' Suspended temporarily by Owner or Administrator.
        ''' </summary>
        OnHold = 6

        ''' <summary>
        ''' Work completed by staff; pending Owner/Partner verification.
        ''' </summary>
        UnderReview = 7

        ''' <summary>
        ''' Work verified and completed.
        ''' </summary>
        Completed = 8

        ''' <summary>
        ''' Work product delivered to client.
        ''' </summary>
        Delivered = 9

        ''' <summary>
        ''' Archived and billed terminal state.
        ''' </summary>
        Closed = 10

        ''' <summary>
        ''' Voided terminal state.
        ''' </summary>
        Cancelled = 11
    End Enum
End Namespace
