Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.DAL.Interfaces

Namespace Services
    Public Class DeviceService
        Implements IDeviceService

        Private ReadOnly _deviceRepository As IDeviceRepository
        Private ReadOnly _userRepository As IUserRepository

        Public Sub New(deviceRepository As IDeviceRepository, userRepository As IUserRepository)
            _deviceRepository = deviceRepository
            _userRepository = userRepository
        End Sub

        Public Async Function ValidateDeviceAsync(userId As Integer, clientInfo As DeviceClientInfoDto) As Task(Of DeviceValidationResultDto) Implements IDeviceService.ValidateDeviceAsync
            Dim user = Await _userRepository.GetByIdAsync(userId)
            If user Is Nothing Then
                Return New DeviceValidationResultDto With {.IsAllowed = False, .Status = DeviceStatus.Rejected, .Message = "User not found."}
            End If

            Dim existingDevice = Await _deviceRepository.GetDeviceByGuidAsync(userId, clientInfo.DeviceGuid)

            If existingDevice Is Nothing Then
                existingDevice = Await _deviceRepository.GetDeviceByFingerprintAsync(userId, clientInfo.HardwareFingerprint)
            End If

            If existingDevice Is Nothing Then
                Dim newDevice As New UserDeviceEntity() With {
                    .UserId = userId,
                    .DeviceGuid = clientInfo.DeviceGuid,
                    .HardwareFingerprint = clientInfo.HardwareFingerprint,
                    .MachineName = clientInfo.MachineName,
                    .OSVersion = clientInfo.OSVersion,
                    .Status = DeviceStatus.Approved.ToString(),
                    .RequestedOn = DateTime.UtcNow,
                    .ApprovedOn = DateTime.UtcNow
                }
                
                Try
                    Dim newId = Await _deviceRepository.CreateDeviceAsync(newDevice)
                    Return New DeviceValidationResultDto With {
                        .IsAllowed = True, 
                        .Status = DeviceStatus.Approved, 
                        .Message = "Device verified successfully.",
                        .DeviceId = newId
                    }
                Catch ex As Exception
                    Return New DeviceValidationResultDto With {
                        .IsAllowed = True, 
                        .Status = DeviceStatus.Approved, 
                        .Message = "Device verified successfully."
                    }
                End Try
            End If

            Dim deviceStatusEnum As DeviceStatus
            If Not [Enum].TryParse(existingDevice.Status, deviceStatusEnum) Then
                deviceStatusEnum = DeviceStatus.Rejected
            End If

            Dim result As New DeviceValidationResultDto() With {
                .DeviceId = existingDevice.DeviceId,
                .Status = deviceStatusEnum
            }

            Select Case deviceStatusEnum
                Case DeviceStatus.Approved
                    result.IsAllowed = True
                    result.Message = "Device authorized."
                    existingDevice.LastActiveOn = DateTime.UtcNow
                    Await _deviceRepository.UpdateDeviceAsync(existingDevice)
                    
                Case DeviceStatus.Pending
                    result.IsAllowed = False
                    result.Message = "Your device registration is pending administrator approval."
                    
                Case DeviceStatus.Rejected
                    result.IsAllowed = False
                    result.Message = "Access denied. This device registration was rejected by an administrator."
                    
                Case DeviceStatus.Revoked
                    result.IsAllowed = False
                    result.Message = "Access denied. This device has been revoked."
                    
                Case Else
                    result.IsAllowed = False
                    result.Message = "Access denied. Invalid device state."
            End Select

            Return result
        End Function

        Public Async Function GetAllDevicesAsync() As Task(Of IEnumerable(Of UserDeviceDto)) Implements IDeviceService.GetAllDevicesAsync
            Dim entities = Await _deviceRepository.GetAllDevicesAsync()
            Return Await MapToDtoListAsync(entities)
        End Function

        Public Async Function GetDevicesForUserAsync(userId As Integer) As Task(Of IEnumerable(Of UserDeviceDto)) Implements IDeviceService.GetDevicesForUserAsync
            Dim entities = Await _deviceRepository.GetDevicesForUserAsync(userId)
            Return Await MapToDtoListAsync(entities)
        End Function

        Public Async Function ApproveDeviceAsync(deviceId As Integer, approvedByUserId As Integer) As Task Implements IDeviceService.ApproveDeviceAsync
            Dim device = Await _deviceRepository.GetByIdAsync(deviceId)
            If device Is Nothing Then Throw New ArgumentException("Device not found.")
            
            Dim currentStatus As DeviceStatus
            [Enum].TryParse(device.Status, currentStatus)

            If currentStatus = DeviceStatus.Revoked Then
                Throw New InvalidOperationException("Cannot approve a revoked device. A new registration is required.")
            End If

            device.Status = DeviceStatus.Approved.ToString()
            device.ApprovedOn = DateTime.UtcNow
            device.ApprovedBy = approvedByUserId
            device.RevokedOn = Nothing
            device.RevokedBy = Nothing
            device.RevocationReason = Nothing

            Await _deviceRepository.UpdateDeviceAsync(device)
        End Function

        Public Async Function RejectDeviceAsync(deviceId As Integer, rejectedByUserId As Integer) As Task Implements IDeviceService.RejectDeviceAsync
            Dim device = Await _deviceRepository.GetByIdAsync(deviceId)
            If device Is Nothing Then Throw New ArgumentException("Device not found.")

            Dim currentStatus As DeviceStatus
            [Enum].TryParse(device.Status, currentStatus)

            If currentStatus <> DeviceStatus.Pending Then
                Throw New InvalidOperationException("Only pending devices can be rejected. Use revoke for approved devices.")
            End If

            device.Status = DeviceStatus.Rejected.ToString()
            device.RevokedOn = DateTime.UtcNow
            device.RevokedBy = rejectedByUserId
            device.RevocationReason = "Rejected during initial registration."

            Await _deviceRepository.UpdateDeviceAsync(device)
        End Function

        Public Async Function RevokeDeviceAsync(deviceId As Integer, revokedByUserId As Integer, reason As String) As Task Implements IDeviceService.RevokeDeviceAsync
            Dim device = Await _deviceRepository.GetByIdAsync(deviceId)
            If device Is Nothing Then Throw New ArgumentException("Device not found.")

            Dim currentStatus As DeviceStatus
            [Enum].TryParse(device.Status, currentStatus)

            If currentStatus <> DeviceStatus.Approved Then
                Throw New InvalidOperationException("Only approved devices can be revoked.")
            End If

            device.Status = DeviceStatus.Revoked.ToString()
            device.RevokedOn = DateTime.UtcNow
            device.RevokedBy = revokedByUserId
            device.RevocationReason = If(String.IsNullOrWhiteSpace(reason), "Administratively revoked", reason)

            Await _deviceRepository.UpdateDeviceAsync(device)
        End Function

        Private Async Function MapToDtoListAsync(entities As List(Of UserDeviceEntity)) As Task(Of List(Of UserDeviceDto))
            Dim dtoList As New List(Of UserDeviceDto)()
            For Each entity In entities
                Dim dto As New UserDeviceDto() With {
                    .DeviceId = entity.DeviceId,
                    .UserId = entity.UserId,
                    .DeviceGuid = entity.DeviceGuid,
                    .HardwareFingerprint = entity.HardwareFingerprint,
                    .MachineName = entity.MachineName,
                    .OSVersion = entity.OSVersion,
                    .RequestedOn = entity.RequestedOn,
                    .ApprovedOn = entity.ApprovedOn,
                    .ApprovedBy = entity.ApprovedBy,
                    .RevokedOn = entity.RevokedOn,
                    .RevokedBy = entity.RevokedBy,
                    .RevocationReason = entity.RevocationReason,
                    .LastActiveOn = entity.LastActiveOn
                }
                Dim parsedStatus As DeviceStatus
                If [Enum].TryParse(entity.Status, parsedStatus) Then
                    dto.Status = parsedStatus
                End If

                Dim user = Await _userRepository.GetByIdAsync(entity.UserId)
                If user IsNot Nothing Then dto.StaffName = user.FullName

                If entity.ApprovedBy.HasValue Then
                    Dim approver = Await _userRepository.GetByIdAsync(entity.ApprovedBy.Value)
                    If approver IsNot Nothing Then dto.ApprovedByName = approver.FullName
                End If

                If entity.RevokedBy.HasValue Then
                    Dim revoker = Await _userRepository.GetByIdAsync(entity.RevokedBy.Value)
                    If revoker IsNot Nothing Then dto.RevokedByName = revoker.FullName
                End If

                dtoList.Add(dto)
            Next
            Return dtoList
        End Function

    End Class
End Namespace
