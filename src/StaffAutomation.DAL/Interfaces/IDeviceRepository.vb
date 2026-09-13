Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Entities

Namespace Interfaces
    Public Interface IDeviceRepository
        Function GetByIdAsync(deviceId As Integer) As Task(Of UserDeviceEntity)
        Function GetDeviceByGuidAsync(userId As Integer, deviceGuid As Guid) As Task(Of UserDeviceEntity)
        Function GetDeviceByFingerprintAsync(userId As Integer, hardwareFingerprint As String) As Task(Of UserDeviceEntity)
        Function CreateDeviceAsync(device As UserDeviceEntity) As Task(Of Integer)
        Function GetDevicesForUserAsync(userId As Integer) As Task(Of List(Of UserDeviceEntity))
        Function GetAllDevicesAsync() As Task(Of List(Of UserDeviceEntity))
        Function UpdateDeviceAsync(device As UserDeviceEntity) As Task(Of Boolean)
    End Interface
End Namespace
