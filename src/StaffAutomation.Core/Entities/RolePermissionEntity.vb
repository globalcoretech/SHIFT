Imports System

Namespace Entities
    ''' <summary>
    ''' System Domain Entity representing role-to-permission mapping.
    ''' Maps conceptually to dbo.tbl_RolePermissions.
    ''' </summary>
    Public Class RolePermissionEntity
        Public Property RolePermissionId As Integer
        Public Property RoleId As Integer
        Public Property PermissionId As Integer
        Public Property GrantedOn As DateTime = DateTime.UtcNow
        Public Property GrantedBy As Integer = 1
    End Class
End Namespace
