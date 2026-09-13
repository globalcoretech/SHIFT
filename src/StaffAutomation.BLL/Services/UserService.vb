Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.BLL.Logging
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Logging
Imports StaffAutomation.Core.Security
Imports StaffAutomation.DAL.Interfaces

Namespace Services
    ''' <summary>
    ''' Service managing User accounts, profiles, and password updates.
    ''' </summary>
    Public Class UserService
        Private ReadOnly _userRepository As IUserRepository
        Private ReadOnly _passwordHasher As PasswordHasher
        Private ReadOnly _appLogger As IAppLogger
        Private ReadOnly _auditLogger As AuditLogger

        Public Sub New(userRepository As IUserRepository, passwordHasher As PasswordHasher, appLogger As IAppLogger, auditLogger As AuditLogger)
            _userRepository = userRepository
            _passwordHasher = passwordHasher
            _appLogger = appLogger
            _auditLogger = auditLogger
        End Sub

        Public Async Function GetUserByIdAsync(userId As Integer) As Task(Of UserDto)
            Dim user = Await _userRepository.GetByIdAsync(userId)
            If user Is Nothing Then Return Nothing
            Return MapToDto(user)
        End Function

        Public Async Function GetAllUsersAsync() As Task(Of List(Of UserDto))
            Dim list = Await _userRepository.GetAllAsync()
            Dim dtoList As New List(Of UserDto)()
            For Each item In list
                dtoList.Add(MapToDto(item))
            Next
            Return dtoList
        End Function

        Public Async Function ChangePasswordAsync(userId As Integer, currentPassword As String, newPassword As String) As Task(Of Boolean)
            Dim user = Await _userRepository.GetByIdAsync(userId)
            If user Is Nothing Then
                Throw New BusinessException("User account not found.", "ERR_USER_NOT_FOUND")
            End If

            If Not _passwordHasher.VerifyPassword(currentPassword, user.PasswordHash, user.PasswordSalt) Then
                Throw New BusinessException("Current password verification failed.", "ERR_INVALID_PASSWORD")
            End If

            Dim validationError As String = String.Empty
            If Not _passwordHasher.IsPasswordValid(newPassword, validationError) Then
                Throw New ValidationException(validationError, "newPassword")
            End If

            Dim newSalt As String = String.Empty
            Dim newHash = _passwordHasher.HashPassword(newPassword, newSalt)

            user.PasswordHash = newHash
            user.PasswordSalt = newSalt
            user.MustChangePassword = False
            user.ModifiedBy = userId

            Dim success = Await _userRepository.UpdateAsync(user)
            If success Then
                _appLogger.LogInfo($"Password successfully changed for user ID {userId}.", "UserService")
                Await _auditLogger.LogAuditAsync(userId, "PASSWORD_CHANGE", "IdentityServices", "Password successfully updated.")
            End If
            Return success
        End Function

        Public Async Function CreateUserAsync(newUser As Core.Entities.UserEntity, currentUserId As Integer) As Task(Of Integer)
            ' Duplicate username validation is usually handled by repository/DB, but we can rely on existing SQL exception catch or add check here
            Dim newId = Await _userRepository.AddAsync(newUser)
            If newId > 0 Then
                _appLogger.LogInfo($"User account created successfully for user '{newUser.Username}'.", "UserService")
                Await _auditLogger.LogAuditAsync(currentUserId, "USER_CREATE", "IdentityServices", $"Created user account {newUser.Username}.")
            End If
            Return newId
        End Function

        Public Async Function UpdateUserAsync(userToUpdate As Core.Entities.UserEntity, currentUserId As Integer) As Task(Of Boolean)
            Dim originalUser = Await _userRepository.GetByIdAsync(userToUpdate.UserId)
            If originalUser Is Nothing Then
                Throw New BusinessException("User account not found.", "ERR_USER_NOT_FOUND")
            End If

            ' Self-Lockout Protection
            If userToUpdate.UserId = currentUserId Then
                If userToUpdate.Role <> originalUser.Role Then
                    Throw New BusinessException("Security Violation: Users cannot modify their own role.", "ERR_SECURITY_SELF_ROLE_CHANGE")
                End If
                If userToUpdate.IsActive = False AndAlso originalUser.IsActive = True Then
                    Throw New BusinessException("Security Violation: Users cannot deactivate their own account.", "ERR_SECURITY_SELF_DEACTIVATE")
                End If
            End If

            Dim success = Await _userRepository.UpdateAsync(userToUpdate)
            If success Then
                _appLogger.LogInfo($"User account updated successfully for user ID {userToUpdate.UserId}.", "UserService")
                Await _auditLogger.LogAuditAsync(currentUserId, "USER_UPDATE", "IdentityServices", $"Updated user account {userToUpdate.Username}.")
            End If
            Return success
        End Function

        Private Function MapToDto(entity As Core.Entities.UserEntity) As UserDto
            Return New UserDto() With {
                .UserId = entity.UserId,
                .Username = entity.Username,
                .FullName = entity.FullName,
                .Role = entity.Role,
                .Department = entity.Department,
                .MustChangePassword = entity.MustChangePassword,
                .IsActive = entity.IsActive
            }
        End Function
    End Class
End Namespace
