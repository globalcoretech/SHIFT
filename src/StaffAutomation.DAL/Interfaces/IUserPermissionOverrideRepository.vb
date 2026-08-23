Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Entities

Namespace Interfaces
    ''' <summary>
    ''' Repository interface contract for User Permission Override data access.
    ''' </summary>
    Public Interface IUserPermissionOverrideRepository
        Function GetByUserIdAsync(userId As Integer) As Task(Of List(Of UserPermissionOverrideEntity))
        Function GetOverrideAsync(userId As Integer, permissionId As Integer) As Task(Of UserPermissionOverrideEntity)
        Function SetOverrideAsync(overrideItem As UserPermissionOverrideEntity) As Task(Of Boolean)
        Function DeleteOverrideAsync(userId As Integer, permissionId As Integer) As Task(Of Boolean)
    End Interface
End Namespace
