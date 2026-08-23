Imports System

Namespace Validation
    ''' <summary>
    ''' Reusable validation utilities for required fields, lengths, dates, and numeric ranges.
    ''' </summary>
    Public Class CommonValidators
        Public Shared Function ValidateRequired(value As String, fieldName As String) As ValidationResult
            If String.IsNullOrWhiteSpace(value) Then
                Return ValidationResult.Failure($"Field '{fieldName}' is required.")
            End If
            Return ValidationResult.Success()
        End Function

        Public Shared Function ValidateLength(value As String, minLength As Integer, maxLength As Integer, fieldName As String) As ValidationResult
            If value Is Nothing Then value = String.Empty
            If value.Length < minLength OrElse value.Length > maxLength Then
                Return ValidationResult.Failure($"Field '{fieldName}' length must be between {minLength} and {maxLength} characters.")
            End If
            Return ValidationResult.Success()
        End Function

        Public Shared Function ValidateDateRange(targetDate As DateTime, startDate As DateTime, endDate As DateTime, fieldName As String) As ValidationResult
            If targetDate < startDate OrElse targetDate > endDate Then
                Return ValidationResult.Failure($"Field '{fieldName}' must be between {startDate:yyyy-MM-dd} and {endDate:yyyy-MM-dd}.")
            End If
            Return ValidationResult.Success()
        End Function

        Public Shared Function ValidateNumericRange(value As Double, minValue As Double, maxValue As Double, fieldName As String) As ValidationResult
            If value < minValue OrElse value > maxValue Then
                Return ValidationResult.Failure($"Field '{fieldName}' must be between {minValue} and {maxValue}.")
            End If
            Return ValidationResult.Success()
        End Function
    End Class
End Namespace
