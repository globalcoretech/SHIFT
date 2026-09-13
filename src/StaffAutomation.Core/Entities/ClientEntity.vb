Imports System
Imports StaffAutomation.Core.Enums

Namespace Entities
    ''' <summary>
    ''' Client Master Domain Entity representing tax and accounting consultancy clients.
    ''' </summary>
    Public Class ClientEntity
        Public Property ClientId As Integer
        Public Property ClientCode As String = String.Empty
        Public Property ClientName As String = String.Empty
        Public Property ContactPerson As String = String.Empty
        Public Property Phone As String = String.Empty
        Public Property Email As String = String.Empty
        Public Property Department As Nullable(Of DepartmentType)
        Public Property Gstin As String = String.Empty
        Public Property GstType As String = "Regular Monthly"
        Public Property PanNumber As String = String.Empty
        Public Property StateName As String = String.Empty
        Public Property EntityType As String = "Proprietorship"
        Public Property Address As String = String.Empty
        Public Property District As String = String.Empty
        Public Property Pincode As String = String.Empty
        Public Property IsLiveApiData As Boolean = False
        Public Property IsActive As Boolean = True
        Public Property IsDeleted As Boolean = False
        Public Property CreatedOn As DateTime = DateTime.UtcNow
        Public Property CreatedBy As Integer
        Public Property ModifiedOn As Nullable(Of DateTime)
        Public Property ModifiedBy As Nullable(Of Integer)
    End Class
End Namespace
