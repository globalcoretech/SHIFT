Imports System.Collections.Generic

Namespace Validation
    ''' <summary>
    ''' Generic Validation Result model encapsulating pass/fail status and error messages.
    ''' </summary>
    Public Class ValidationResult
        Public Property IsValid As Boolean = True
        Public Property Errors As List(Of String) = New List(Of String)()

        Public Shared Function Success() As ValidationResult
            Return New ValidationResult() With {.IsValid = True}
        End Function

        Public Shared Function Failure(errorMessage As String) As ValidationResult
            Dim result As New ValidationResult() With {.IsValid = False}
            result.Errors.Add(errorMessage)
            Return result
        End Function

        Public Shared Function Failure(errorMessages As IEnumerable(Of String)) As ValidationResult
            Dim result As New ValidationResult() With {.IsValid = False}
            result.Errors.AddRange(errorMessages)
            Return result
        End Function
    End Class
End Namespace
