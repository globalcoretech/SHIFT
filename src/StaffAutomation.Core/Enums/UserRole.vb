Namespace Enums
    ''' <summary>
    ''' System user access roles enforcing Role-Based Access Control (RBAC).
    ''' </summary>
    Public Enum UserRole
        ''' <summary>
        ''' Staff member responsible for executing tasks and logging activity timeline.
        ''' </summary>
        Employee = 1

        ''' <summary>
        ''' Executive partner/owner with full visibility into live dashboards, analytics, and reports.
        ''' </summary>
        Owner = 2

        ''' <summary>
        ''' System administrator responsible for user provisioning, catalog masters, and audit logs.
        ''' </summary>
        Admin = 3
    End Enum
End Namespace
