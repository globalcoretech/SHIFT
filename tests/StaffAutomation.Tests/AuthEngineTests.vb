Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Linq
Imports System.Threading.Tasks
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports StaffAutomation.BLL.Logging
Imports StaffAutomation.BLL.Services
Imports StaffAutomation.Core.Constants
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Logging
Imports StaffAutomation.Core.Security
Imports StaffAutomation.DAL.Interfaces

Namespace StaffAutomation.Tests
    ''' <summary>
    ''' Unit tests for Authentication Core, Password Hashing, and Legacy Admin Bootstrap Migration.
    ''' </summary>
    <TestClass>
    Public Class AuthEngineTests
        <TestMethod>
        Public Sub TestPasswordHasherSaltGeneration()
            Dim hasher As New PasswordHasher()
            Dim salt As String = String.Empty
            Dim hash As String = hasher.HashPassword("SecretPass123!", salt)

            If String.IsNullOrEmpty(salt) OrElse String.IsNullOrEmpty(hash) Then
                Throw New InvalidOperationException("PasswordHasher failed to generate valid salt and hash.")
            End If

            If Not hasher.VerifyPassword("SecretPass123!", hash, salt) Then
                Throw New InvalidOperationException("PasswordHasher verification failed for correct password.")
            End If

            If hasher.VerifyPassword("WrongPass!", hash, salt) Then
                Throw New InvalidOperationException("PasswordHasher verification incorrectly passed for wrong password.")
            End If
        End Sub

        <TestMethod>
        Public Sub TestPasswordValidationRules()
            Dim verifier As IPasswordVerifier = New PasswordHasher()
            Dim errorMsg As String = String.Empty

            If verifier.IsPasswordValid("short", errorMsg) Then
                Throw New InvalidOperationException("PasswordVerifier failed to reject short password.")
            End If

            If Not verifier.IsPasswordValid("ValidLength123!", errorMsg) Then
                Throw New InvalidOperationException("PasswordVerifier rejected valid length password.")
            End If
        End Sub

        <TestMethod>
        Public Sub TestSessionClearingOnLogout()
            CurrentUserContext.ClearSession()
            If CurrentUserContext.IsAuthenticated Then
                Throw New InvalidOperationException("CurrentUserContext failed to clear session.")
            End If
        End Sub

        <TestMethod>
        Public Async Function TestLegacyAdminBootstrapRecoveryTriggersWhenOnlyAdminHasSentinelHash() As Task
            Dim repo As New FakeUserRepository()
            repo.AddUser(New UserEntity() With {
                .UserId = 1,
                .Username = "admin",
                .PasswordHash = AppConstants.LegacySentinelHash,
                .PasswordSalt = "",
                .Role = UserRole.Admin,
                .IsActive = True,
                .IsDeleted = False
            })

            Dim authService = CreateAuthService(repo)

            Try
                Await authService.AuthenticateUserAsync("admin", "admin123")
                Throw New InvalidOperationException("Expected LegacyAdminBootstrapRecoveryException was not thrown.")
            Catch ex As LegacyAdminBootstrapRecoveryException
                ' Expected behavior: emergency bootstrap recovery exception thrown
                If ex.Username <> "admin" OrElse ex.UserId <> 1 Then
                    Throw New InvalidOperationException("LegacyAdminBootstrapRecoveryException contained invalid user data.")
                End If
            End Try
        End Function

        <TestMethod>
        Public Async Function TestLegacyAdminRecoveryBlockedWhenAnotherValidAdminExists() As Task
            Dim repo As New FakeUserRepository()
            ' Legacy sentinel admin
            repo.AddUser(New UserEntity() With {
                .UserId = 1,
                .Username = "admin_legacy",
                .PasswordHash = AppConstants.LegacySentinelHash,
                .Role = UserRole.Admin,
                .IsActive = True
            })
            ' Modern active valid admin
            Dim hasher As New PasswordHasher()
            Dim salt As String = String.Empty
            Dim hash = hasher.HashPassword("ModernAdminPass123!", salt)
            repo.AddUser(New UserEntity() With {
                .UserId = 2,
                .Username = "admin_valid",
                .PasswordHash = hash,
                .PasswordSalt = salt,
                .Role = UserRole.Admin,
                .IsActive = True
            })

            Dim authService = CreateAuthService(repo)

            Try
                Await authService.AuthenticateUserAsync("admin_legacy", "anypass")
                Throw New InvalidOperationException("Expected AuthenticationException was not thrown.")
            Catch ex As LegacyAdminBootstrapRecoveryException
                Throw New InvalidOperationException("Self-recovery should be blocked when another valid admin exists.")
            Catch ex As AuthenticationException
                ' Expected behavior: standard blocked authentication exception
            End Try
        End Function

        <TestMethod>
        Public Async Function TestLegacyEmployeeRecoveryBlocked() As Task
            Dim repo As New FakeUserRepository()
            repo.AddUser(New UserEntity() With {
                .UserId = 5,
                .Username = "employee1",
                .PasswordHash = AppConstants.LegacySentinelHash,
                .Role = UserRole.Employee,
                .IsActive = True
            })

            Dim authService = CreateAuthService(repo)

            Try
                Await authService.AuthenticateUserAsync("employee1", "anypass")
                Throw New InvalidOperationException("Expected AuthenticationException was not thrown.")
            Catch ex As LegacyAdminBootstrapRecoveryException
                Throw New InvalidOperationException("Employees with sentinel hash must not be allowed to self-recover.")
            Catch ex As AuthenticationException
                ' Expected behavior: blocked for employee
            End Try
        End Function

        <TestMethod>
        Public Async Function TestMigrateLegacyAdminPasswordAsyncSuccessAndOldSentinelPasswordRejection() As Task
            Dim repo As New FakeUserRepository()
            repo.AddUser(New UserEntity() With {
                .UserId = 1,
                .Username = "admin",
                .PasswordHash = AppConstants.LegacySentinelHash,
                .Role = UserRole.Admin,
                .IsActive = True
            })

            Dim authService = CreateAuthService(repo)

            ' Migrate legacy admin password to new PBKDF2 hash
            Dim migrated = Await authService.MigrateLegacyAdminPasswordAsync("admin", "NewSecureAdmin123!")
            If Not migrated Then
                Throw New InvalidOperationException("MigrateLegacyAdminPasswordAsync returned False.")
            End If

            ' Verify migration in repository
            Dim updatedUser = Await repo.GetByUsernameAsync("admin")
            If updatedUser.PasswordHash = AppConstants.LegacySentinelHash Then
                Throw New InvalidOperationException("PasswordHash was not updated from sentinel hash.")
            End If

            If String.IsNullOrEmpty(updatedUser.PasswordSalt) Then
                Throw New InvalidOperationException("PasswordSalt was not generated.")
            End If

            If updatedUser.MustChangePassword Then
                Throw New InvalidOperationException("MustChangePassword should be False post-migration.")
            End If

            ' Verify login succeeds with NEW password
            Dim userDto = Await authService.AuthenticateUserAsync("admin", "NewSecureAdmin123!")
            If userDto Is Nothing OrElse userDto.Username <> "admin" Then
                Throw New InvalidOperationException("Failed to authenticate with newly migrated password.")
            End If

            ' Verify login FAILS with old sentinel / default password
            Try
                Await authService.AuthenticateUserAsync("admin", "admin123")
                Throw New InvalidOperationException("Login with old default password succeeded after migration.")
            Catch ex As AuthenticationException
                ' Expected failure for old default password
            End Try
        End Function

        Private Function CreateAuthService(repo As IUserRepository) As IAuthService
            Dim hasher As New PasswordHasher()
            Dim appLogger As IAppLogger = New FakeAppLogger()
            Dim sqlHelper As ISqlHelper = New FakeSqlHelper()
            Dim auditLogger As New AuditLogger(sqlHelper)
            Return New AuthService(repo, hasher, appLogger, auditLogger)
        End Function

        Private Class FakeUserRepository
            Implements IUserRepository

            Private ReadOnly _users As New List(Of UserEntity)()

            Public Sub AddUser(user As UserEntity)
                _users.Add(user)
            End Sub

            Public Function GetByIdAsync(userId As Integer) As Task(Of UserEntity) Implements IUserRepository.GetByIdAsync
                Return Task.FromResult(_users.FirstOrDefault(Function(u) u.UserId = userId))
            End Function

            Public Function GetByUsernameAsync(username As String) As Task(Of UserEntity) Implements IUserRepository.GetByUsernameAsync
                Return Task.FromResult(_users.FirstOrDefault(Function(u) u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            End Function

            Public Function GetAllAsync() As Task(Of List(Of UserEntity)) Implements IUserRepository.GetAllAsync
                Return Task.FromResult(_users.ToList())
            End Function

            Public Function AddAsync(user As UserEntity) As Task(Of Integer) Implements IUserRepository.AddAsync
                _users.Add(user)
                Return Task.FromResult(user.UserId)
            End Function

            Public Function UpdateAsync(user As UserEntity) As Task(Of Boolean) Implements IUserRepository.UpdateAsync
                Dim existing = _users.FirstOrDefault(Function(u) u.UserId = user.UserId)
                If existing IsNot Nothing Then
                    existing.PasswordHash = user.PasswordHash
                    existing.PasswordSalt = user.PasswordSalt
                    existing.MustChangePassword = user.MustChangePassword
                    existing.ModifiedOn = user.ModifiedOn
                    Return Task.FromResult(True)
                End If
                Return Task.FromResult(False)
            End Function

            Public Function SoftDeleteAsync(userId As Integer, modifiedBy As Integer) As Task(Of Boolean) Implements IUserRepository.SoftDeleteAsync
                Return Task.FromResult(True)
            End Function
        End Class

        Private Class FakeSqlHelper
            Implements ISqlHelper

            Public Function ExecuteNonQueryAsync(commandText As String, parameters As IDbDataParameter(), Optional transaction As IDbTransaction = Nothing) As Task(Of Integer) Implements ISqlHelper.ExecuteNonQueryAsync
                Return Task.FromResult(1)
            End Function

            Public Function ExecuteScalarAsync(Of T)(commandText As String, parameters As IDbDataParameter(), Optional transaction As IDbTransaction = Nothing) As Task(Of T) Implements ISqlHelper.ExecuteScalarAsync
                Return Task.FromResult(CType(Nothing, T))
            End Function

            Public Function ExecuteReaderAsync(Of T)(commandText As String, parameters As IDbDataParameter(), mapFunc As Func(Of IDataReader, T), Optional transaction As IDbTransaction = Nothing) As Task(Of List(Of T)) Implements ISqlHelper.ExecuteReaderAsync
                Return Task.FromResult(New List(Of T)())
            End Function
        End Class

        Private Class FakeAppLogger
            Implements IAppLogger

            Public Sub Log(level As LogLevel, message As String, Optional moduleName As String = "", Optional ex As Exception = Nothing) Implements IAppLogger.Log
            End Sub

            Public Sub LogDebug(message As String, Optional moduleName As String = "") Implements IAppLogger.LogDebug
            End Sub

            Public Sub LogInfo(message As String, Optional moduleName As String = "") Implements IAppLogger.LogInfo
            End Sub

            Public Sub LogWarn(message As String, Optional moduleName As String = "") Implements IAppLogger.LogWarn
            End Sub

            Public Sub LogError(message As String, Optional moduleName As String = "", Optional ex As Exception = Nothing) Implements IAppLogger.LogError
            End Sub

            Public Sub LogFatal(message As String, Optional moduleName As String = "", Optional ex As Exception = Nothing) Implements IAppLogger.LogFatal
            End Sub
        End Class
    End Class
End Namespace
