Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.Core.DTOs

Namespace Interfaces
    ''' <summary>
    ''' Service interface contract for User-level permission overrides management (GRANT/DENY).
    ''' </summary>
    Public Interface IUserPermissionOverrideService
        Function GetOverridesForUserAsync(userId As Integer) As Task(Of List(Of UserPermissionOverrideDto))
        Function SetOverrideAsync(userId As Integer, permissionId As Integer, isGranted As Boolean, reason As String, modifiedBy As Integer) As Task(Of Boolean)
        Function RemoveOverrideAsync(userId As Integer, permissionId As Integer) As Task(Of Boolean)
    End Interface
End Namespace
