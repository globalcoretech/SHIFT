Imports StaffAutomation.Core.Enums

Namespace Entities
    ''' <summary>
    ''' System User Domain Entity representing staff members, owners, and administrators.
    ''' </summary>
    Public Class UserEntity
        Public Property UserId As Integer
        Public Property Username As String = String.Empty
        Public Property PasswordHash As String = String.Empty
        Public Property PasswordSalt As String = String.Empty
        Public Property FullName As String = String.Empty
        Public Property Role As UserRole
        Public Property Department As DepartmentType
        Public Property MustChangePassword As Boolean = False
        Public Property IsActive As Boolean = True
        Public Property IsDeleted As Boolean = False
        Public Property CreatedOn As DateTime = DateTime.UtcNow
        Public Property CreatedBy As Integer
        Public Property ModifiedOn As Nullable(Of DateTime)
        Public Property ModifiedBy As Nullable(Of Integer)
    End Class
End Namespace
