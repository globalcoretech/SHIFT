Imports System

Namespace Exceptions
    ''' <summary>
    ''' Exception thrown when user login or identity verification fails.
    ''' </summary>
    Public Class AuthenticationException
        Inherits StaffAutomationException

        Public Sub New()
            MyBase.New()
        End Sub

        Public Sub New(message As String)
            MyBase.New(message)
        End Sub

        Public Sub New(message As String, innerException As Exception)
            MyBase.New(message, innerException)
        End Sub
    End Class
End Namespace
