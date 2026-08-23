Namespace Enums
    ''' <summary>
    ''' Actionable outcome classification for client communications.
    ''' </summary>
    Public Enum CommunicationOutcome
        ''' <summary>
        ''' Requested supporting documents or vouchers from client.
        ''' </summary>
        DocumentsRequested = 1

        ''' <summary>
        ''' Received requested vouchers or data files from client.
        ''' </summary>
        DocumentsReceived = 2

        ''' <summary>
        ''' Sent query; awaiting client response or confirmation.
        ''' </summary>
        WaitingForReply = 3

        ''' <summary>
        ''' Provided tax/accounting consultation and guidance to client.
        ''' </summary>
        Explained = 4

        ''' <summary>
        ''' Further internal or client follow-up required.
        ''' </summary>
        FollowUpRequired = 5

        ''' <summary>
        ''' Discussion successfully resolved all pending points.
        ''' </summary>
        Completed = 6
    End Enum
End Namespace
