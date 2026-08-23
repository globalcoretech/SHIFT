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
Imports StaffAutomation.Core.Validation
Imports StaffAutomation.DAL.Interfaces

Namespace Services
    ''' <summary>
    ''' Business Logic Service for User-level Permission Overrides management (GRANT / DENY).
    ''' Implements IUserPermissionOverrideService enforcing override rules, mandatory justifications, and audit logging.
    ''' Does NOT mutate runtime authorization engine logic (data persistence layer only).
    ''' </summary>
    Public Class UserPermissionOverrideService
        Implements IUserPermissionOverrideService

        Private ReadOnly _overrideRepo As IUserPermissionOverrideRepository
        Private ReadOnly _permRepo As IPermissionRepository
        Private ReadOnly _userRepo As IUserRepository
        Private ReadOnly _appLogger As IAppLogger
        Private ReadOnly _auditLogger As IAuditLogger

        Public Sub New(overrideRepo As IUserPermissionOverrideRepository, Optional permRepo As IPermissionRepository = Nothing, Optional userRepo As IUserRepository = Nothing, Optional appLogger As IAppLogger = Nothing, Optional auditLogger As IAuditLogger = Nothing)
            _overrideRepo = overrideRepo
            _permRepo = permRepo
            _userRepo = userRepo
            _appLogger = appLogger
            _auditLogger = auditLogger
        End Sub

        Public Async Function GetOverridesForUserAsync(userId As Integer) As Task(Of List(Of UserPermissionOverrideDto)) Implements IUserPermissionOverrideService.GetOverridesForUserAsync
            If userId <= 0 Then Return New List(Of UserPermissionOverrideDto)()
            Dim overrideEntities = Await _overrideRepo.GetByUserIdAsync(userId)
            Dim list As New List(Of UserPermissionOverrideDto)()

            Dim permsMap As New Dictionary(Of Integer, PermissionEntity)()
            If _permRepo IsNot Nothing Then
                Dim allPerms = Await _permRepo.GetAllAsync()
                For Each p In allPerms
                    permsMap(p.PermissionId) = p
                Next
            End If

            Dim targetUser = If(_userRepo IsNot Nothing, Await _userRepo.GetByIdAsync(userId), Nothing)
            Dim username = If(targetUser IsNot Nothing, targetUser.Username, $"User_{userId}")
            Dim fullName = If(targetUser IsNot Nothing, targetUser.FullName, $"User #{userId}")

            For Each o In overrideEntities
                Dim dto As New UserPermissionOverrideDto() With {
                    .OverrideId = o.OverrideId,
                    .UserId = o.UserId,
                    .Username = username,
                    .FullName = fullName,
                    .PermissionId = o.PermissionId,
                    .IsGranted = o.IsGranted,
                    .Reason = o.Reason,
                    .GrantedOn = o.GrantedOn,
                    .GrantedBy = o.GrantedBy
                }

                If permsMap.ContainsKey(o.PermissionId) Then
                    Dim perm = permsMap(o.PermissionId)
                    dto.PermissionCode = perm.PermissionCode
                    dto.PermissionName = perm.PermissionName
                    dto.ModuleName = perm.ModuleName
                End If

                list.Add(dto)
            Next

            Return list
        End Function

        Public Async Function SetOverrideAsync(userId As Integer, permissionId As Integer, isGranted As Boolean, reason As String, modifiedBy As Integer) As Task(Of Boolean) Implements IUserPermissionOverrideService.SetOverrideAsync
            If userId <= 0 Then
                Throw New ValidationException("Invalid User ID specified.", "UserId")
            End If
            If permissionId <= 0 Then
                Throw New ValidationException("Invalid Permission ID specified.", "PermissionId")
            End If

            ' Mandatory justification reason
            Dim valReason = CommonValidators.ValidateRequired(reason, "Reason")
            If Not valReason.IsValid Then Throw New ValidationException(valReason.Errors(0), "Reason")

            ' User Existence Check (if repo available)
            If _userRepo IsNot Nothing Then
                Dim user = Await _userRepo.GetByIdAsync(userId)
                If user Is Nothing Then
                    Throw New BusinessException($"User ID {userId} was not found in the system.", "ERR_USER_NOT_FOUND")
                End If
            End If

            ' Permission Existence Check (if repo available)
            If _permRepo IsNot Nothing Then
                Dim perm = Await _permRepo.GetByIdAsync(permissionId)
                If perm Is Nothing Then
                    Throw New BusinessException($"Permission ID {permissionId} was not found in the catalog.", "ERR_PERMISSION_NOT_FOUND")
                End If
            End If

            Dim entity As New UserPermissionOverrideEntity() With {
                .UserId = userId,
                .PermissionId = permissionId,
                .IsGranted = isGranted,
                .Reason = reason.Trim(),
                .GrantedBy = modifiedBy
            }

            Dim success = Await _overrideRepo.SetOverrideAsync(entity)
            If success Then
                AuthorizationService.ClearCache()
                Dim grantState = If(isGranted, "GRANT", "DENY")
                If _appLogger IsNot Nothing Then
                    _appLogger.LogInfo($"Permission override set for User ID {userId}, Permission ID {permissionId}: {grantState} (Reason: {entity.Reason}).", "UserPermissionOverrideService")
                End If
                If _auditLogger IsNot Nothing Then
                    Await _auditLogger.LogAuditAsync(modifiedBy, "USER_PERMISSION_OVERRIDE_SET", "UserPermissionOverrideService", $"User ID {userId} permission ID {permissionId} override set to {grantState}. Reason: {entity.Reason}")
                End If
            End If

            Return success
        End Function

        Public Async Function RemoveOverrideAsync(userId As Integer, permissionId As Integer) As Task(Of Boolean) Implements IUserPermissionOverrideService.RemoveOverrideAsync
            If userId <= 0 OrElse permissionId <= 0 Then Return False

            Dim success = Await _overrideRepo.DeleteOverrideAsync(userId, permissionId)
            If success Then
                AuthorizationService.ClearCache()
                If _appLogger IsNot Nothing Then
                    _appLogger.LogInfo($"Permission override removed for User ID {userId}, Permission ID {permissionId}.", "UserPermissionOverrideService")
                End If
                If _auditLogger IsNot Nothing Then
                    Await _auditLogger.LogAuditAsync(1, "USER_PERMISSION_OVERRIDE_REMOVE", "UserPermissionOverrideService", $"User ID {userId} permission ID {permissionId} override removed.")
                End If
            End If

            Return success
        End Function
    End Class
End Namespace
