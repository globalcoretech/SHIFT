Imports System

Namespace Exceptions
    ''' <summary>
    ''' Exception thrown when a user attempts an action forbidden by Role-Based Access Control (RBAC).
    ''' </summary>
    Public Class SecurityAuthorizationException
        Inherits StaffAutomationException

        Public Property RequiredRole As String = String.Empty

        Public Sub New()
            MyBase.New()
        End Sub

        Public Sub New(message As String)
            MyBase.New(message)
        End Sub

        Public Sub New(message As String, requiredRole As String)
            MyBase.New(message)
            Me.RequiredRole = requiredRole
        End Sub

        Public Sub New(message As String, innerException As Exception)
            MyBase.New(message, innerException)
        End Sub
    End Class
End Namespace
