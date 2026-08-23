Imports System

Namespace Entities
    ''' <summary>
    ''' System Domain Entity representing explicit user-level permission overrides (GRANT/DENY).
    ''' Maps conceptually to dbo.tbl_UserPermissionOverrides.
    ''' </summary>
    Public Class UserPermissionOverrideEntity
        Public Property OverrideId As Integer
        Public Property UserId As Integer
        Public Property PermissionId As Integer
        Public Property IsGranted As Boolean
        Public Property Reason As String = String.Empty
        Public Property GrantedOn As DateTime = DateTime.UtcNow
        Public Property GrantedBy As Integer = 1
    End Class
End Namespace
