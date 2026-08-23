Imports System.Threading.Tasks
Imports StaffAutomation.Core.DTOs

Namespace Interfaces
    ''' <summary>
    ''' Business Logic Service contract for multi-user authentication, session login, and legacy bootstrap migration.
    ''' </summary>
    Public Interface IAuthService
        Function AuthenticateUserAsync(username As String, password As String) As Task(Of UserDto)
        Function HasValidAdministratorExistsAsync() As Task(Of Boolean)
        Function MigrateLegacyAdminPasswordAsync(username As String, newPassword As String) As Task(Of Boolean)
        Sub Logout()
    End Interface
End Namespace
