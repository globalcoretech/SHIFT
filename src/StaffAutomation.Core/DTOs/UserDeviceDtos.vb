Imports System
Imports StaffAutomation.Core.Enums

Namespace DTOs
    Public Class UserDeviceDto
        Public Property DeviceId As Integer
        Public Property UserId As Integer
        Public Property DeviceGuid As Guid
        Public Property HardwareFingerprint As String
        Public Property MachineName As String
        Public Property OSVersion As String
        Public Property Status As DeviceStatus
        Public Property RequestedOn As DateTime
        Public Property ApprovedOn As DateTime?
        Public Property ApprovedBy As Integer?
        Public Property RevokedOn As DateTime?
        Public Property RevokedBy As Integer?
        Public Property RevocationReason As String
        Public Property LastActiveOn As DateTime?
        
        ' Optional joined properties for UI convenience
        Public Property StaffName As String
        Public Property ApprovedByName As String
        Public Property RevokedByName As String
    End Class

    Public Class DeviceClientInfoDto
        Public Property DeviceGuid As Guid
        Public Property HardwareFingerprint As String
        Public Property MachineName As String
        Public Property OSVersion As String
    End Class

    Public Class DeviceValidationResultDto
        Public Property IsAllowed As Boolean
        Public Property Status As DeviceStatus
        Public Property Message As String
        Public Property DeviceId As Integer?
    End Class
End Namespace
