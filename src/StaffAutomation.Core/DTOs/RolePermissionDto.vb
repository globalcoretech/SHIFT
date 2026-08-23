Imports System

Namespace DTOs
    ''' <summary>
    ''' Data Transfer Object for Role to Permission mappings.
    ''' </summary>
    Public Class RolePermissionDto
        Public Property RolePermissionId As Integer
        Public Property RoleId As Integer
        Public Property RoleName As String = String.Empty
        Public Property PermissionId As Integer
        Public Property PermissionCode As String = String.Empty
        Public Property PermissionName As String = String.Empty
        Public Property ModuleName As String = String.Empty
        Public Property GrantedOn As DateTime
        Public Property GrantedBy As Integer
    End Class
End Namespace
