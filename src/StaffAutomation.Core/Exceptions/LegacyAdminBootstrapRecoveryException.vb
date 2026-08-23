Imports System

Namespace Exceptions
    ''' <summary>
    ''' Exception thrown when an authenticating account is a legacy sentinel Administrator
    ''' and no active, non-sentinel Administrator account exists in the system.
    ''' Triggers emergency one-time bootstrap admin recovery.
    ''' </summary>
    Public Class LegacyAdminBootstrapRecoveryException
        Inherits AuthenticationException

        Public Property Username As String
        Public Property UserId As Integer

        Public Sub New(username As String, userId As Integer)
            MyBase.New($"Account '{username}' relies on a legacy sentinel password and requires initial bootstrap recovery.")
            Me.Username = username
            Me.UserId = userId
        End Sub
    End Class
End Namespace
