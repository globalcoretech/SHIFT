Imports System

Namespace Entities
    ''' <summary>
    ''' System Domain Entity representing singleton CA Firm Master Profile.
    ''' Maps conceptually to dbo.tbl_FirmProfile.
    ''' </summary>
    Public Class FirmProfileEntity
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
        Public Property CreatedOn As DateTime = DateTime.UtcNow
        Public Property CreatedBy As Integer = 1
        Public Property ModifiedOn As Nullable(Of DateTime)
        Public Property ModifiedBy As Nullable(Of Integer)
    End Class
End Namespace
