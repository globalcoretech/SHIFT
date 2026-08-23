Imports System.Collections.Generic
Imports StaffAutomation.Core.Enums

Namespace Security
    ''' <summary>
    ''' Contract for looking up permissions allocated to roles.
    ''' </summary>
    Public Interface IPermissionProvider
        Function GetPermissionsForRole(role As UserRole) As List(Of String)
    End Interface
End Namespace
