Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Entities

Namespace Interfaces
    ''' <summary>
    ''' Repository interface contract for Permission catalog data access.
    ''' </summary>
    Public Interface IPermissionRepository
        Function GetByIdAsync(permissionId As Integer) As Task(Of PermissionEntity)
        Function GetByCodeAsync(permissionCode As String) As Task(Of PermissionEntity)
        Function GetAllAsync() As Task(Of List(Of PermissionEntity))
        Function GetByModuleAsync(moduleName As String) As Task(Of List(Of PermissionEntity))
    End Interface
End Namespace
