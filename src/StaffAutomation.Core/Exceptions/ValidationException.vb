Imports System

Namespace Exceptions
    ''' <summary>
    ''' Exception thrown when user input validation fails.
    ''' </summary>
    Public Class ValidationException
        Inherits StaffAutomationException

        Public Property FieldName As String = String.Empty

        Public Sub New()
            MyBase.New()
        End Sub

        Public Sub New(message As String)
            MyBase.New(message)
        End Sub

        Public Sub New(message As String, fieldName As String)
            MyBase.New(message)
            Me.FieldName = fieldName
        End Sub

        Public Sub New(message As String, innerException As Exception)
            MyBase.New(message, innerException)
        End Sub
    End Class
End Namespace
