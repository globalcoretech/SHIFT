Imports System

Namespace Exceptions
    ''' <summary>
    ''' Exception thrown when an authenticated user attempts an authorized action without required permissions.
    ''' </summary>
    Public Class AuthorizationException
        Inherits StaffAutomationException

        Public Property RequiredPermission As String = String.Empty

        Public Sub New()
            MyBase.New()
        End Sub

        Public Sub New(message As String)
            MyBase.New(message)
        End Sub

        Public Sub New(message As String, requiredPermission As String)
            MyBase.New(message)
            Me.RequiredPermission = requiredPermission
        End Sub

        Public Sub New(message As String, innerException As Exception)
            MyBase.New(message, innerException)
        End Sub
    End Class
End Namespace
