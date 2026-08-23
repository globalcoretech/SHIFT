Imports System

Namespace DTOs
    ''' <summary>
    ''' Data Transfer Object for CA Firm Master Profile details.
    ''' </summary>
    Public Class FirmProfileDto
        Public Property FirmId As Integer = 1
        Public Property FirmName As String = String.Empty
        Public Property LegalName As String = String.Empty
        Public Property FRN As String = String.Empty
        Public Property PAN As String = String.Empty
        Public Property GSTIN As String = String.Empty
        Public Property AddressLine1 As String = String.Empty
        Public Property AddressLine2 As String = String.Empty
        Public Property City As String = String.Empty
        Public Property StateName As String = String.Empty
        Public Property Pincode As String = String.Empty
        Public Property Phone As String = String.Empty
        Public Property Email As String = String.Empty
        Public Property Website As String = String.Empty
        Public Property HeaderFormatJson As String = String.Empty
    End Class
End Namespace
