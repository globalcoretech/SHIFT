Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.Core.DTOs

Namespace Interfaces
    ''' <summary>
    ''' Service interface contract for Role-Permission matrix management.
    ''' </summary>
    Public Interface IRolePermissionService
        Function GetPermissionsForRoleAsync(roleId As Integer) As Task(Of List(Of PermissionDto))
        Function UpdateRolePermissionsAsync(roleId As Integer, permissionIds As List(Of Integer), modifiedBy As Integer) As Task(Of Boolean)
    End Interface
End Namespace
