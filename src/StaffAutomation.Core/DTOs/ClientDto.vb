Imports System
Imports StaffAutomation.Core.Enums

Namespace DTOs
    ''' <summary>
    ''' Data Transfer Object for Client master profile.
    ''' </summary>
    Public Class ClientDto
        Public Property ClientId As Integer
        Public Property ClientCode As String = String.Empty
        Public Property ClientName As String = String.Empty
        Public Property ContactPerson As String = String.Empty
        Public Property Phone As String = String.Empty
        Public Property Email As String = String.Empty
        Public Property Department As DepartmentType
        Public Property Gstin As String = String.Empty
        Public Property GstType As String = "Regular Monthly"
        Public Property PanNumber As String = String.Empty
        Public Property StateName As String = String.Empty
        Public Property EntityType As String = "Proprietorship"
        Public Property Address As String = String.Empty
        Public Property District As String = String.Empty
        Public Property Pincode As String = String.Empty
        Public Property IsLiveApiData As Boolean = False
        Public Property IsActive As Boolean
        Public Property IsDeleted As Boolean
    End Class
End Namespace
