Option Strict On
Option Explicit On

Imports System
Imports System.Threading.Tasks
Imports Xunit
Imports Moq
Imports StaffAutomation.BLL.Services
Imports StaffAutomation.BLL.Logging
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Logging
Imports StaffAutomation.Core.Security
Imports StaffAutomation.DAL.Interfaces
Imports StaffAutomation.Core.Interfaces

Namespace StaffAutomation.Tests
    Public Class UserAccountSecurityTests

        Private ReadOnly _mockUserRepo As Mock(Of IUserRepository)
        Private ReadOnly _mockLogger As Mock(Of IAppLogger)
        Private ReadOnly _userService As UserService
        Private ReadOnly _auditLogger As AuditLogger
        Private ReadOnly _hasher As PasswordHasher

        Public Sub New()
            _mockUserRepo = New Mock(Of IUserRepository)()
            _mockLogger = New Mock(Of IAppLogger)()
            
            Dim mockSqlHelper = New Mock(Of Global.StaffAutomation.Core.Interfaces.ISqlHelper)()
            _auditLogger = New AuditLogger(mockSqlHelper.Object)
            _hasher = New PasswordHasher()
            
            _userService = New UserService(_mockUserRepo.Object, _hasher, _mockLogger.Object, _auditLogger)
        End Sub

        <Fact>
        Public Async Function UpdateUserAsync_AdminCannotChangeOwnRole() As Task
            ' Arrange
            Dim currentUserId = 5
            Dim originalUser = New UserEntity With {.UserId = currentUserId, .Role = UserRole.Admin, .IsActive = True}
            Dim updatedUser = New UserEntity With {.UserId = currentUserId, .Role = UserRole.Employee, .IsActive = True}

            _mockUserRepo.Setup(Function(r) r.GetByIdAsync(currentUserId)).ReturnsAsync(originalUser)

            ' Act & Assert
            Dim ex = Await Assert.ThrowsAsync(Of BusinessException)(Function() _userService.UpdateUserAsync(updatedUser, currentUserId))
            Assert.Contains("cannot modify their own role", ex.Message)
        End Function

        <Fact>
        Public Async Function UpdateUserAsync_AdminCannotDeactivateSelf() As Task
            ' Arrange
            Dim currentUserId = 5
            Dim originalUser = New UserEntity With {.UserId = currentUserId, .Role = UserRole.Admin, .IsActive = True}
            Dim updatedUser = New UserEntity With {.UserId = currentUserId, .Role = UserRole.Admin, .IsActive = False}

            _mockUserRepo.Setup(Function(r) r.GetByIdAsync(currentUserId)).ReturnsAsync(originalUser)

            ' Act & Assert
            Dim ex = Await Assert.ThrowsAsync(Of BusinessException)(Function() _userService.UpdateUserAsync(updatedUser, currentUserId))
            Assert.Contains("cannot deactivate their own account", ex.Message)
        End Function

        <Fact>
        Public Async Function UpdateUserAsync_OwnerCannotChangeOwnRole() As Task
            ' Arrange
            Dim currentUserId = 1
            Dim originalUser = New UserEntity With {.UserId = currentUserId, .Role = UserRole.Owner, .IsActive = True}
            Dim updatedUser = New UserEntity With {.UserId = currentUserId, .Role = UserRole.Admin, .IsActive = True}

            _mockUserRepo.Setup(Function(r) r.GetByIdAsync(currentUserId)).ReturnsAsync(originalUser)

            ' Act & Assert
            Dim ex = Await Assert.ThrowsAsync(Of BusinessException)(Function() _userService.UpdateUserAsync(updatedUser, currentUserId))
            Assert.Contains("cannot modify their own role", ex.Message)
        End Function

        <Fact>
        Public Async Function UpdateUserAsync_OwnerCannotDeactivateSelf() As Task
            ' Arrange
            Dim currentUserId = 1
            Dim originalUser = New UserEntity With {.UserId = currentUserId, .Role = UserRole.Owner, .IsActive = True}
            Dim updatedUser = New UserEntity With {.UserId = currentUserId, .Role = UserRole.Owner, .IsActive = False}

            _mockUserRepo.Setup(Function(r) r.GetByIdAsync(currentUserId)).ReturnsAsync(originalUser)

            ' Act & Assert
            Dim ex = Await Assert.ThrowsAsync(Of BusinessException)(Function() _userService.UpdateUserAsync(updatedUser, currentUserId))
            Assert.Contains("cannot deactivate their own account", ex.Message)
        End Function

        <Fact>
        Public Async Function UpdateUserAsync_AdminCanModifyAnotherUser() As Task
            ' Arrange
            Dim adminId = 5
            Dim targetUserId = 10
            Dim originalUser = New UserEntity With {.UserId = targetUserId, .Role = UserRole.Employee, .IsActive = True}
            Dim updatedUser = New UserEntity With {.UserId = targetUserId, .Role = UserRole.Admin, .IsActive = False}

            _mockUserRepo.Setup(Function(r) r.GetByIdAsync(targetUserId)).ReturnsAsync(originalUser)
            _mockUserRepo.Setup(Function(r) r.UpdateAsync(updatedUser)).ReturnsAsync(True)

            ' Act
            Dim result = Await _userService.UpdateUserAsync(updatedUser, adminId)

            ' Assert
            Assert.True(result)
            _mockUserRepo.Verify(Function(r) r.UpdateAsync(updatedUser), Times.Once)
        End Function

        <Fact>
        Public Async Function ChangePasswordAsync_CorrectCurrentPasswordSucceeds() As Task
            ' Arrange
            Dim userId = 5
            Dim currentPassword = "CurrentPassword123!"
            Dim newPassword = "NewPassword456@"
            Dim salt As String = ""
            Dim hash = _hasher.HashPassword(currentPassword, salt)
            
            Dim user = New UserEntity With {.UserId = userId, .PasswordHash = hash, .PasswordSalt = salt}
            _mockUserRepo.Setup(Function(r) r.GetByIdAsync(userId)).ReturnsAsync(user)
            _mockUserRepo.Setup(Function(r) r.UpdateAsync(It.IsAny(Of UserEntity))).ReturnsAsync(True)

            ' Act
            Dim result = Await _userService.ChangePasswordAsync(userId, currentPassword, newPassword)

            ' Assert
            Assert.True(result)
            Assert.False(user.MustChangePassword)
            _mockUserRepo.Verify(Function(r) r.UpdateAsync(user), Times.Once)
        End Function

        <Fact>
        Public Async Function ChangePasswordAsync_IncorrectCurrentPasswordFails() As Task
            ' Arrange
            Dim userId = 5
            Dim correctPassword = "CorrectPassword123!"
            Dim wrongPassword = "WrongPassword123!"
            Dim newPassword = "NewPassword456@"
            Dim salt As String = ""
            Dim hash = _hasher.HashPassword(correctPassword, salt)
            
            Dim user = New UserEntity With {.UserId = userId, .PasswordHash = hash, .PasswordSalt = salt}
            _mockUserRepo.Setup(Function(r) r.GetByIdAsync(userId)).ReturnsAsync(user)

            ' Act & Assert
            Dim ex = Await Assert.ThrowsAsync(Of BusinessException)(Function() _userService.ChangePasswordAsync(userId, wrongPassword, newPassword))
            Assert.Contains("Current password verification failed", ex.Message)
            _mockUserRepo.Verify(Function(r) r.UpdateAsync(It.IsAny(Of UserEntity)), Times.Never)
        End Function

        <Fact>
        Public Async Function ChangePasswordAsync_UsesExistingHashingMechanism() As Task
            ' Arrange
            Dim userId = 5
            Dim currentPassword = "CurrentPassword123!"
            Dim newPassword = "NewPassword456@"
            Dim salt As String = ""
            Dim originalHash = _hasher.HashPassword(currentPassword, salt)
            
            Dim user = New UserEntity With {.UserId = userId, .PasswordHash = originalHash, .PasswordSalt = salt}
            _mockUserRepo.Setup(Function(r) r.GetByIdAsync(userId)).ReturnsAsync(user)
            _mockUserRepo.Setup(Function(r) r.UpdateAsync(It.IsAny(Of UserEntity))).ReturnsAsync(True)

            ' Act
            Await _userService.ChangePasswordAsync(userId, currentPassword, newPassword)

            ' Assert
            ' The user object should now have a new hash and salt generated by _hasher
            Assert.NotEqual(originalHash, user.PasswordHash)
            Assert.True(_hasher.VerifyPassword(newPassword, user.PasswordHash, user.PasswordSalt))
        End Function

    End Class
End Namespace
