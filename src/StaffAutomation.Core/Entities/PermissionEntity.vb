Imports System

Namespace Entities
    ''' <summary>
    ''' System Domain Entity representing a cataloged permission.
    ''' Maps conceptually to dbo.tbl_Permissions.
    ''' </summary>
    Public Class PermissionEntity
        Public Property PermissionId As Integer
        Public Property PermissionCode As String = String.Empty
        Public Property PermissionName As String = String.Empty
        Public Property ModuleName As String = String.Empty
        Public Property Description As String = String.Empty
        Public Property IsActive As Boolean = True
        Public Property CreatedOn As DateTime = DateTime.UtcNow
        Public Property CreatedBy As Integer = 1
        Public Property ModifiedOn As Nullable(Of DateTime)
        Public Property ModifiedBy As Nullable(Of Integer)
    End Class
End Namespace
