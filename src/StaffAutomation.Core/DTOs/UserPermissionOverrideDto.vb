Imports System

Namespace DTOs
    ''' <summary>
    ''' Data Transfer Object for User-level permission overrides (GRANT/DENY).
    ''' </summary>
    Public Class UserPermissionOverrideDto
        Public Property OverrideId As Integer
        Public Property UserId As Integer
        Public Property Username As String = String.Empty
        Public Property FullName As String = String.Empty
        Public Property PermissionId As Integer
        Public Property PermissionCode As String = String.Empty
        Public Property PermissionName As String = String.Empty
        Public Property ModuleName As String = String.Empty
        Public Property IsGranted As Boolean
        Public Property Reason As String = String.Empty
        Public Property GrantedOn As DateTime
        Public Property GrantedBy As Integer
    End Class
End Namespace
