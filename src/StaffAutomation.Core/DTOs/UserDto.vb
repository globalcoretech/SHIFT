Imports StaffAutomation.Core.Enums

Namespace DTOs
    ''' <summary>
    ''' Data Transfer Object for User information across application layers.
    ''' </summary>
    Public Class UserDto
        Public Property UserId As Integer
        Public Property Username As String = String.Empty
        Public Property FullName As String = String.Empty
        Public Property Role As UserRole
        Public Property Department As DepartmentType
        Public Property MustChangePassword As Boolean = False
        Public Property IsActive As Boolean
    End Class
End Namespace
