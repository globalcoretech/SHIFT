Imports StaffAutomation.Core.Enums

Namespace Security
    ''' <summary>
    ''' Contract for authorizing actions based on user roles, permissions, and explicit permission overrides.
    ''' Maintains 100% backward compatibility with legacy role-based checks while supporting granular permission codes.
    ''' </summary>
    Public Interface IAuthorizationService
        ' Legacy Role-Based Authorization Contracts
        Function IsAuthorized(requiredRole As UserRole) As Boolean
        Function IsAuthorizedAny(ParamArray requiredRoles() As UserRole) As Boolean
        Sub AuthorizeOrThrow(requiredRole As UserRole)
        Sub AuthorizeAnyOrThrow(ParamArray requiredRoles() As UserRole)

        ' Granular Permission-Based Authorization Contracts
        Function IsAuthorized(permissionCode As String) As Boolean
        Sub AuthorizeOrThrow(permissionCode As String)
        Sub InvalidateCache()
    End Interface
End Namespace
