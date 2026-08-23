Option Strict On
Option Explicit On

Imports System
Imports System.Threading.Tasks
Imports StaffAutomation.BLL.Logging
Imports StaffAutomation.Core.Constants
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Logging
Imports StaffAutomation.Core.Security
Imports StaffAutomation.DAL.Interfaces

Namespace Services
    ''' <summary>
    ''' Authentication Service providing user authentication, password verification, and legacy bootstrap admin migration.
    ''' Conforms strictly to IAuthService interface contract.
    ''' </summary>
    Public Class AuthService
        Implements IAuthService

        Private ReadOnly _userRepo As IUserRepository
        Private ReadOnly _passwordHasher As IPasswordHasher
        Private ReadOnly _appLogger As IAppLogger
        Private ReadOnly _auditLogger As AuditLogger

        Public Sub New(userRepo As IUserRepository, passwordHasher As IPasswordHasher, appLogger As IAppLogger, auditLogger As AuditLogger)
            _userRepo = userRepo
            _passwordHasher = passwordHasher
            _appLogger = appLogger
            _auditLogger = auditLogger
        End Sub

        Public Async Function AuthenticateUserAsync(username As String, password As String) As Task(Of UserDto) Implements IAuthService.AuthenticateUserAsync
            If String.IsNullOrWhiteSpace(username) OrElse String.IsNullOrWhiteSpace(password) Then
                Throw New AuthenticationException("Username and Password are required.")
            End If

            Dim userEntity = Await _userRepo.GetByUsernameAsync(username.Trim())
            If userEntity Is Nothing Then
                _appLogger.LogWarn($"Failed login attempt for nonexistent user '{username}'.", "AuthService")
                Throw New AuthenticationException("Invalid username or password.")
            End If

            If Not userEntity.IsActive Then
                _appLogger.LogWarn($"Failed login attempt for inactive user account '{username}'.", "AuthService")
                Throw New AuthenticationException($"User account '{username}' is disabled. Please contact your system administrator.")
            End If

            ' Sentinel Hash Check: Detect legacy unmigrated sentinel hash accounts
            If userEntity.PasswordHash = AppConstants.LegacySentinelHash Then
                ' Determine whether emergency bootstrap admin recovery is allowed
                Dim hasValidAdmin = Await HasValidAdministratorExistsAsync()
                If userEntity.Role = UserRole.Admin AndAlso Not hasValidAdmin Then
                    _appLogger.LogWarn($"Triggering emergency legacy admin bootstrap recovery for user '{username}'.", "AuthService")
                    Throw New LegacyAdminBootstrapRecoveryException(userEntity.Username, userEntity.UserId)
                Else
                    _appLogger.LogWarn($"Blocked login attempt for user '{username}' with legacy sentinel hash.", "AuthService")
                    Throw New AuthenticationException($"Account '{username}' relies on a legacy sentinel password. An administrator must reset your password before logging in.")
                End If
            End If

            Dim isPasswordValid = _passwordHasher.VerifyPassword(password, userEntity.PasswordHash, userEntity.PasswordSalt)
            If Not isPasswordValid Then
                _appLogger.LogWarn($"Failed login attempt for user '{username}' due to invalid password.", "AuthService")
                Throw New AuthenticationException("Invalid username or password.")
            End If

            ' Initialize global user session context
            Dim userDto As New UserDto() With {
                .UserId = userEntity.UserId,
                .Username = userEntity.Username,
                .FullName = userEntity.FullName,
                .Role = userEntity.Role,
                .Department = userEntity.Department,
                .MustChangePassword = userEntity.MustChangePassword,
                .IsActive = userEntity.IsActive
            }

            CurrentUserContext.CurrentUser = userDto

            _appLogger.LogInfo($"User '{username}' (ID: {userEntity.UserId}, Role: {userEntity.Role}) authenticated successfully.", "AuthService")
            Await _auditLogger.LogAuditAsync(userEntity.UserId, "USER_LOGIN", "AuthService", $"User '{username}' logged in successfully.")

            Return userDto
        End Function

        Public Async Function HasValidAdministratorExistsAsync() As Task(Of Boolean) Implements IAuthService.HasValidAdministratorExistsAsync
            Dim allUsers = Await _userRepo.GetAllAsync()
            If allUsers Is Nothing OrElse allUsers.Count = 0 Then Return False

            For Each u In allUsers
                If u.IsActive AndAlso Not u.IsDeleted AndAlso u.Role = UserRole.Admin Then
                    If Not String.IsNullOrEmpty(u.PasswordHash) AndAlso u.PasswordHash <> AppConstants.LegacySentinelHash Then
                        Return True
                    End If
                End If
            Next

            Return False
        End Function

        Public Async Function MigrateLegacyAdminPasswordAsync(username As String, newPassword As String) As Task(Of Boolean) Implements IAuthService.MigrateLegacyAdminPasswordAsync
            If String.IsNullOrWhiteSpace(username) Then
                Throw New ValidationException("Username is required for admin recovery.", "username")
            End If

            Dim errorMsg As String = String.Empty
            Dim verifier As IPasswordVerifier = TryCast(_passwordHasher, IPasswordVerifier)
            If verifier IsNot Nothing AndAlso Not verifier.IsPasswordValid(newPassword, errorMsg) Then
                Throw New BusinessException(If(Not String.IsNullOrEmpty(errorMsg), errorMsg, "Password does not meet complexity requirements."))
            ElseIf String.IsNullOrWhiteSpace(newPassword) OrElse newPassword.Length < AppConstants.MinimumPasswordLength Then
                Throw New BusinessException($"New password must be at least {AppConstants.MinimumPasswordLength} characters long.")
            End If

            Dim userEntity = Await _userRepo.GetByUsernameAsync(username.Trim())
            If userEntity Is Nothing OrElse Not userEntity.IsActive Then
                Throw New AuthenticationException("Invalid recovery request.")
            End If

            ' Strict Authorization & Safety Guards
            If userEntity.PasswordHash <> AppConstants.LegacySentinelHash Then
                Throw New BusinessException("Account does not require legacy migration.")
            End If

            If userEntity.Role <> UserRole.Admin Then
                Throw New BusinessException("Self-recovery is restricted strictly to administrator accounts.")
            End If

            Dim hasValidAdmin = Await HasValidAdministratorExistsAsync()
            If hasValidAdmin Then
                Throw New BusinessException("Legacy admin self-recovery is disabled because a valid administrator account already exists. Please contact an administrator.")
            End If

            ' Perform PBKDF2 Hashing
            Dim newSalt As String = String.Empty
            Dim newHash = _passwordHasher.HashPassword(newPassword, newSalt)

            userEntity.PasswordHash = newHash
            userEntity.PasswordSalt = newSalt
            userEntity.MustChangePassword = False
            userEntity.ModifiedOn = DateTime.UtcNow

            Dim updated = Await _userRepo.UpdateAsync(userEntity)
            If Not updated Then
                Throw New InvalidOperationException("Failed to update database record during legacy admin recovery.")
            End If

            _appLogger.LogInfo($"Successfully migrated legacy sentinel password for bootstrap admin '{username}' (ID: {userEntity.UserId}).", "AuthService")
            Await _auditLogger.LogAuditAsync(userEntity.UserId, "LEGACY_ADMIN_BOOTSTRAP_MIGRATION", "AuthService", $"Successfully migrated legacy sentinel password for bootstrap admin '{username}'.")

            Return True
        End Function

        Public Sub Logout() Implements IAuthService.Logout
            If CurrentUserContext.IsAuthenticated Then
                Dim userId = CurrentUserContext.CurrentUser.UserId
                _appLogger.LogInfo($"User ID {userId} logged out.", "AuthService")
                CurrentUserContext.ClearSession()
            End If
        End Sub
    End Class
End Namespace
