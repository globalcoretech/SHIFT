Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Entities

Namespace Interfaces
    ''' <summary>
    ''' Repository interface contract for Role-Permission matrix data access.
    ''' </summary>
    Public Interface IRolePermissionRepository
        Function GetByRoleIdAsync(roleId As Integer) As Task(Of List(Of RolePermissionEntity))
        Function GetPermissionsForRoleAsync(roleId As Integer) As Task(Of List(Of PermissionEntity))
        Function SetRolePermissionsAsync(roleId As Integer, permissionIds As List(Of Integer), grantedBy As Integer) As Task(Of Boolean)
    End Interface
End Namespace
