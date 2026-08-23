Imports System

Namespace Exceptions
    ''' <summary>
    ''' Exception thrown when application infrastructure services (caching, files, networking) fail.
    ''' </summary>
    Public Class InfrastructureException
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
