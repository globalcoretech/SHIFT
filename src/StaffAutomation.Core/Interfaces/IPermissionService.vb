Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.Core.DTOs

Namespace Interfaces
    ''' <summary>
    ''' Service interface contract for system catalog permission lookups.
    ''' </summary>
    Public Interface IPermissionService
        Function GetAllPermissionsAsync() As Task(Of List(Of PermissionDto))
        Function GetPermissionByCodeAsync(permissionCode As String) As Task(Of PermissionDto)
    End Interface
End Namespace
