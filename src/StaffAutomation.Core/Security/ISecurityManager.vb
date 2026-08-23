Imports StaffAutomation.Core.Enums

Namespace Security
    ''' <summary>
    ''' Contract for Role-Based Access Control (RBAC) authorization enforcement.
    ''' </summary>
    Public Interface ISecurityManager
        Sub DemandRole(requiredRole As UserRole)
        Sub DemandAnyRole(ParamArray requiredRoles() As UserRole)
        Function HasRole(role As UserRole) As Boolean
    End Interface
End Namespace
