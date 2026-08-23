Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks
Imports StaffAutomation.BLL.Security
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Logging
Imports StaffAutomation.DAL.Interfaces

Namespace Services
    ''' <summary>
    ''' Business Logic Service for Role-Permission matrix management.
    ''' Implements IRolePermissionService enforcing atomic matrix updates and audit logging.
    ''' </summary>
    Public Class RolePermissionService
        Implements IRolePermissionService

        Private ReadOnly _rolePermRepo As IRolePermissionRepository
        Private ReadOnly _permRepo As IPermissionRepository
        Private ReadOnly _appLogger As IAppLogger
        Private ReadOnly _auditLogger As IAuditLogger

        Public Sub New(rolePermRepo As IRolePermissionRepository, Optional permRepo As IPermissionRepository = Nothing, Optional appLogger As IAppLogger = Nothing, Optional auditLogger As IAuditLogger = Nothing)
            _rolePermRepo = rolePermRepo
            _permRepo = permRepo
            _appLogger = appLogger
            _auditLogger = auditLogger
        End Sub

        Public Async Function GetPermissionsForRoleAsync(roleId As Integer) As Task(Of List(Of PermissionDto)) Implements IRolePermissionService.GetPermissionsForRoleAsync
            If roleId <= 0 Then Return New List(Of PermissionDto)()
            Dim entities = Await _rolePermRepo.GetPermissionsForRoleAsync(roleId)
            Dim list As New List(Of PermissionDto)()
            For Each e In entities
                list.Add(MapToDto(e))
            Next
            Return list
        End Function

        Public Async Function UpdateRolePermissionsAsync(roleId As Integer, permissionIds As List(Of Integer), modifiedBy As Integer) As Task(Of Boolean) Implements IRolePermissionService.UpdateRolePermissionsAsync
            If roleId <= 0 Then
                Throw New ValidationException("Invalid Role ID specified.", "RoleId")
            End If

            ' Remove duplicates safely
            Dim distinctIds As New List(Of Integer)()
            If permissionIds IsNot Nothing Then
                distinctIds = permissionIds.Distinct().Where(Function(id) id > 0).ToList()
            End If

            ' Validate permission IDs against database catalog if permRepo is available
            If _permRepo IsNot Nothing AndAlso distinctIds.Count > 0 Then
                Dim allPerms = Await _permRepo.GetAllAsync()
                Dim validIds = allPerms.Select(Function(p) p.PermissionId).ToHashSet()
                Dim invalid = distinctIds.Where(Function(id) Not validIds.Contains(id)).ToList()
                If invalid.Count > 0 Then
                    Throw New ValidationException($"One or more permission IDs are invalid or inactive: {String.Join(", ", invalid)}", "PermissionIds")
                End If
            End If

            ' Atomic matrix replacement via repository transaction
            Dim success = Await _rolePermRepo.SetRolePermissionsAsync(roleId, distinctIds, modifiedBy)
            If success Then
                AuthorizationService.ClearCache()
                If _appLogger IsNot Nothing Then
                    _appLogger.LogInfo($"Role ID {roleId} permissions matrix updated ({distinctIds.Count} permissions granted).", "RolePermissionService")
                End If
                If _auditLogger IsNot Nothing Then
                    Await _auditLogger.LogAuditAsync(modifiedBy, "ROLE_PERMISSION_UPDATE", "RolePermissionService", $"Role ID {roleId} permission matrix updated ({distinctIds.Count} permissions granted).")
                End If
            End If

            Return success
        End Function

        Private Function MapToDto(entity As PermissionEntity) As PermissionDto
            Return New PermissionDto() With {
                .PermissionId = entity.PermissionId,
                .PermissionCode = entity.PermissionCode,
                .PermissionName = entity.PermissionName,
                .ModuleName = entity.ModuleName,
                .Description = entity.Description,
                .IsActive = entity.IsActive
            }
        End Function
    End Class
End Namespace
