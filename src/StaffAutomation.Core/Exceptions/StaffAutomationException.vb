Imports System

Namespace Exceptions
    ''' <summary>
    ''' Base exception class for all custom domain and application exceptions in StaffAutomation.
    ''' </summary>
    Public Class StaffAutomationException
        Inherits Exception

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
