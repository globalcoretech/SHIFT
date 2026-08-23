Imports System

Namespace Exceptions
    ''' <summary>
    ''' Exception thrown when a business rule or state machine transition validation fails.
    ''' </summary>
    Public Class BusinessException
        Inherits StaffAutomationException

        Public Property RuleCode As String = String.Empty

        Public Sub New()
            MyBase.New()
        End Sub

        Public Sub New(message As String)
            MyBase.New(message)
        End Sub

        Public Sub New(message As String, ruleCode As String)
            MyBase.New(message)
            Me.RuleCode = ruleCode
        End Sub

        Public Sub New(message As String, innerException As Exception)
            MyBase.New(message, innerException)
        End Sub
    End Class
End Namespace
