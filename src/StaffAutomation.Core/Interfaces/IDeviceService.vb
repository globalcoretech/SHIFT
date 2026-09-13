Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.Core.DTOs

Namespace Interfaces
    Public Interface IDeviceService
        Function ValidateDeviceAsync(userId As Integer, clientInfo As DeviceClientInfoDto) As Task(Of DeviceValidationResultDto)
        Function GetAllDevicesAsync() As Task(Of IEnumerable(Of UserDeviceDto))
        Function GetDevicesForUserAsync(userId As Integer) As Task(Of IEnumerable(Of UserDeviceDto))
        Function ApproveDeviceAsync(deviceId As Integer, approvedByUserId As Integer) As Task
        Function RejectDeviceAsync(deviceId As Integer, rejectedByUserId As Integer) As Task
        Function RevokeDeviceAsync(deviceId As Integer, revokedByUserId As Integer, reason As String) As Task
    End Interface
End Namespace
