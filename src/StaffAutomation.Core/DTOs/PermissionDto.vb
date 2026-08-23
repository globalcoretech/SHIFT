Imports System

Namespace DTOs
    ''' <summary>
    ''' Data Transfer Object for system catalog permissions.
    ''' </summary>
    Public Class PermissionDto
        Public Property PermissionId As Integer
        Public Property PermissionCode As String = String.Empty
        Public Property PermissionName As String = String.Empty
        Public Property ModuleName As String = String.Empty
        Public Property Description As String = String.Empty
        Public Property IsActive As Boolean = True
    End Class
End Namespace
