Imports System

Namespace Entities
    Public Class UserDeviceEntity
        Public Property DeviceId As Integer
        Public Property UserId As Integer
        Public Property DeviceGuid As Guid
        Public Property HardwareFingerprint As String
        Public Property MachineName As String
        Public Property OSVersion As String
        Public Property Status As String
        Public Property RequestedOn As DateTime
        Public Property ApprovedOn As DateTime?
        Public Property ApprovedBy As Integer?
        Public Property RevokedOn As DateTime?
        Public Property RevokedBy As Integer?
        Public Property RevocationReason As String
        Public Property LastActiveOn As DateTime?
    End Class
End Namespace
