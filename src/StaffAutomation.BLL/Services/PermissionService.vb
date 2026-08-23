Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Logging
Imports StaffAutomation.DAL.Interfaces

Namespace Services
    ''' <summary>
    ''' Business Logic Service for Central System Catalog Permissions lookups.
    ''' Implements IPermissionService as a read-oriented catalog provider using the database as source of truth.
    ''' </summary>
    Public Class PermissionService
        Implements IPermissionService

        Private ReadOnly _permRepo As IPermissionRepository
        Private ReadOnly _appLogger As IAppLogger

        Public Sub New(permRepo As IPermissionRepository, Optional appLogger As IAppLogger = Nothing)
            _permRepo = permRepo
            _appLogger = appLogger
        End Sub

        Public Async Function GetAllPermissionsAsync() As Task(Of List(Of PermissionDto)) Implements IPermissionService.GetAllPermissionsAsync
            Dim entities = Await _permRepo.GetAllAsync()
            Dim list As New List(Of PermissionDto)()
            For Each e In entities
                list.Add(MapToDto(e))
            Next
            Return list
        End Function

        Public Async Function GetPermissionByCodeAsync(permissionCode As String) As Task(Of PermissionDto) Implements IPermissionService.GetPermissionByCodeAsync
            If String.IsNullOrWhiteSpace(permissionCode) Then Return Nothing
            Dim normalizedCode = permissionCode.Trim().ToUpper()
            Dim entity = Await _permRepo.GetByCodeAsync(normalizedCode)
            If entity Is Nothing Then Return Nothing
            Return MapToDto(entity)
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
