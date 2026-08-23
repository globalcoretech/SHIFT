Option Strict On
Option Explicit On

Imports System

Namespace Exceptions
    ''' <summary>
    ''' Exception thrown when an optimistic concurrency conflict occurs (e.g. record modified or deleted by another user).
    ''' </summary>
    Public Class ConcurrencyException
        Inherits BusinessException

        Public Sub New()
            MyBase.New("The record was modified or deleted by another user. Please refresh and try again.", "ERR_CONCURRENCY_CONFLICT")
        End Sub

        Public Sub New(message As String)
            MyBase.New(message, "ERR_CONCURRENCY_CONFLICT")
        End Sub

        Public Sub New(message As String, innerException As Exception)
            MyBase.New(message, innerException)
            Me.RuleCode = "ERR_CONCURRENCY_CONFLICT"
        End Sub
    End Class
End Namespace
