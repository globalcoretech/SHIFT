Imports System
Imports System.Threading.Tasks
Imports Moq
Imports StaffAutomation.BLL.Services
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.DAL.Interfaces
Imports Xunit

Namespace Tests
    Public Class DeviceRegistrationTests

        Private ReadOnly _mockDeviceRepo As Mock(Of IDeviceRepository)
        Private ReadOnly _mockUserRepo As Mock(Of IUserRepository)
        Private ReadOnly _deviceService As DeviceService

        Public Sub New()
            _mockDeviceRepo = New Mock(Of IDeviceRepository)()
            _mockUserRepo = New Mock(Of IUserRepository)()
            _deviceService = New DeviceService(_mockDeviceRepo.Object, _mockUserRepo.Object)
        End Sub

        <Fact>
        Public Async Function ValidateDeviceAsync_UnknownDevice_RegistersAsApproved() As Task
            ' Arrange
            Dim userId = 1
            Dim clientInfo As New DeviceClientInfoDto With {
                .DeviceGuid = Guid.NewGuid(),
                .HardwareFingerprint = "test-hash",
                .MachineName = "TEST-PC",
                .OSVersion = "Windows 10"
            }
            
            _mockUserRepo.Setup(Function(r) r.GetByIdAsync(userId)).ReturnsAsync(New UserEntity With {.UserId = userId})
            _mockDeviceRepo.Setup(Function(r) r.GetDeviceByGuidAsync(userId, clientInfo.DeviceGuid)).ReturnsAsync(CType(Nothing, UserDeviceEntity))
            _mockDeviceRepo.Setup(Function(r) r.GetDeviceByFingerprintAsync(userId, clientInfo.HardwareFingerprint)).ReturnsAsync(CType(Nothing, UserDeviceEntity))
            _mockDeviceRepo.Setup(Function(r) r.CreateDeviceAsync(It.IsAny(Of UserDeviceEntity)())).ReturnsAsync(100)

            ' Act
            Dim result = Await _deviceService.ValidateDeviceAsync(userId, clientInfo)

            ' Assert
            Assert.True(result.IsAllowed)
            Assert.Equal(DeviceStatus.Approved, result.Status)
            Assert.Equal(100, result.DeviceId)
            _mockDeviceRepo.Verify(Function(r) r.CreateDeviceAsync(It.Is(Of UserDeviceEntity)(Function(d) d.Status = "Approved" AndAlso d.UserId = userId)), Times.Once)
        End Function

        <Fact>
        Public Async Function ValidateDeviceAsync_ApprovedDevice_ReturnsAllowedAndUpdatesLastActive() As Task
            ' Arrange
            Dim userId = 1
            Dim clientInfo As New DeviceClientInfoDto With {
                .DeviceGuid = Guid.NewGuid(),
                .HardwareFingerprint = "test-hash"
            }
            
            Dim existingDevice As New UserDeviceEntity With {
                .DeviceId = 100,
                .UserId = userId,
                .DeviceGuid = clientInfo.DeviceGuid,
                .Status = "Approved"
            }

            _mockUserRepo.Setup(Function(r) r.GetByIdAsync(userId)).ReturnsAsync(New UserEntity With {.UserId = userId})
            _mockDeviceRepo.Setup(Function(r) r.GetDeviceByGuidAsync(userId, clientInfo.DeviceGuid)).ReturnsAsync(existingDevice)

            ' Act
            Dim result = Await _deviceService.ValidateDeviceAsync(userId, clientInfo)

            ' Assert
            Assert.True(result.IsAllowed)
            Assert.Equal(DeviceStatus.Approved, result.Status)
            _mockDeviceRepo.Verify(Function(r) r.UpdateDeviceAsync(It.Is(Of UserDeviceEntity)(Function(d) d.LastActiveOn.HasValue)), Times.Once)
        End Function

        <Fact>
        Public Async Function ValidateDeviceAsync_RejectedDevice_ReturnsNotAllowed() As Task
            ' Arrange
            Dim userId = 1
            Dim clientInfo As New DeviceClientInfoDto With {
                .DeviceGuid = Guid.NewGuid(),
                .HardwareFingerprint = "test-hash"
            }
            
            Dim existingDevice As New UserDeviceEntity With {
                .DeviceId = 100,
                .UserId = userId,
                .DeviceGuid = clientInfo.DeviceGuid,
                .Status = "Rejected"
            }

            _mockUserRepo.Setup(Function(r) r.GetByIdAsync(userId)).ReturnsAsync(New UserEntity With {.UserId = userId})
            _mockDeviceRepo.Setup(Function(r) r.GetDeviceByGuidAsync(userId, clientInfo.DeviceGuid)).ReturnsAsync(existingDevice)

            ' Act
            Dim result = Await _deviceService.ValidateDeviceAsync(userId, clientInfo)

            ' Assert
            Assert.False(result.IsAllowed)
            Assert.Equal(DeviceStatus.Rejected, result.Status)
        End Function

        <Fact>
        Public Async Function ApproveDeviceAsync_RevokedDevice_ThrowsException() As Task
            ' Arrange
            Dim deviceId = 100
            Dim existingDevice As New UserDeviceEntity With {
                .DeviceId = deviceId,
                .Status = "Revoked"
            }
            _mockDeviceRepo.Setup(Function(r) r.GetByIdAsync(deviceId)).ReturnsAsync(existingDevice)

            ' Act & Assert
            Await Assert.ThrowsAsync(Of InvalidOperationException)(Function() _deviceService.ApproveDeviceAsync(deviceId, 1))
        End Function

        <Fact>
        Public Async Function RevokeDeviceAsync_ApprovedDevice_RevokesSuccessfully() As Task
            ' Arrange
            Dim deviceId = 100
            Dim adminId = 99
            Dim existingDevice As New UserDeviceEntity With {
                .DeviceId = deviceId,
                .Status = "Approved"
            }
            _mockDeviceRepo.Setup(Function(r) r.GetByIdAsync(deviceId)).ReturnsAsync(existingDevice)

            ' Act
            Await _deviceService.RevokeDeviceAsync(deviceId, adminId, "Lost device")

            ' Assert
            _mockDeviceRepo.Verify(Function(r) r.UpdateDeviceAsync(It.Is(Of UserDeviceEntity)(Function(d) d.Status = "Revoked" AndAlso d.RevocationReason = "Lost device" AndAlso d.RevokedBy.HasValue AndAlso d.RevokedBy.Value = adminId)), Times.Once())
        End Function

    End Class
End Namespace
