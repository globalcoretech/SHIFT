Imports System
Imports System.Collections.Generic
Imports System.Data
Imports Microsoft.Data.SqlClient
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.DAL.Interfaces

Namespace Repositories
    ''' <summary>
    ''' Data Access Repository for User master records executing parameterized T-SQL.
    ''' </summary>
    Public Class UserRepository
        Implements IUserRepository

        Private ReadOnly _sqlHelper As ISqlHelper

        Public Sub New(sqlHelper As ISqlHelper)
            _sqlHelper = sqlHelper
        End Sub

        Private Shared _columnEnsured As Boolean = False
        Private Shared ReadOnly _columnLock As New Object()

        Private Async Function EnsureColumnExistsAsync() As Task
            If _columnEnsured Then Return
            Try
                Const checkSql As String = "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_Users') AND name = 'MustChangePassword') " &
                                           "ALTER TABLE dbo.tbl_Users ADD MustChangePassword BIT NOT NULL CONSTRAINT DF_tbl_Users_MustChangePassword DEFAULT (0);"
                Await _sqlHelper.ExecuteNonQueryAsync(checkSql, Nothing)
                SyncLock _columnLock
                    _columnEnsured = True
                End SyncLock
            Catch
            End Try
        End Function

        Public Async Function GetByIdAsync(userId As Integer) As Task(Of UserEntity) Implements IUserRepository.GetByIdAsync
            Await EnsureColumnExistsAsync()
            Const query As String = "SELECT UserId, Username, PasswordHash, PasswordSalt, FullName, RoleId, DepartmentId, MustChangePassword, IsActive, IsDeleted, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy " &
                                   "FROM dbo.tbl_Users WHERE UserId = @UserId AND IsDeleted = 0;"
            Dim params As SqlParameter() = {New SqlParameter("@UserId", userId)}
            Dim list = Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapUserEntity)
            Return If(list.Count > 0, list(0), Nothing)
        End Function

        Public Async Function GetByUsernameAsync(username As String) As Task(Of UserEntity) Implements IUserRepository.GetByUsernameAsync
            Await EnsureColumnExistsAsync()
            Const query As String = "SELECT UserId, Username, PasswordHash, PasswordSalt, FullName, RoleId, DepartmentId, MustChangePassword, IsActive, IsDeleted, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy " &
                                   "FROM dbo.tbl_Users WHERE Username = @Username AND IsDeleted = 0;"
            Dim params As SqlParameter() = {New SqlParameter("@Username", username)}
            Dim list = Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapUserEntity)
            Return If(list.Count > 0, list(0), Nothing)
        End Function

        Public Async Function GetAllAsync() As Task(Of List(Of UserEntity)) Implements IUserRepository.GetAllAsync
            Await EnsureColumnExistsAsync()
            Const query As String = "SELECT UserId, Username, PasswordHash, PasswordSalt, FullName, RoleId, DepartmentId, MustChangePassword, IsActive, IsDeleted, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy " &
                                   "FROM dbo.tbl_Users WHERE IsDeleted = 0 ORDER BY FullName ASC;"
            Return Await _sqlHelper.ExecuteReaderAsync(query, Nothing, AddressOf MapUserEntity)
        End Function

        Public Async Function AddAsync(user As UserEntity) As Task(Of Integer) Implements IUserRepository.AddAsync
            Await EnsureColumnExistsAsync()
            Const query As String = "INSERT INTO dbo.tbl_Users (Username, PasswordHash, PasswordSalt, FullName, RoleId, DepartmentId, MustChangePassword, IsActive, IsDeleted, CreatedOn, CreatedBy) " &
                                   "VALUES (@Username, @PasswordHash, @PasswordSalt, @FullName, @RoleId, @DepartmentId, @MustChangePassword, @IsActive, 0, GETUTCDATE(), @CreatedBy); " &
                                   "SELECT SCOPE_IDENTITY();"
            Dim params As SqlParameter() = {
                New SqlParameter("@Username", user.Username),
                New SqlParameter("@PasswordHash", user.PasswordHash),
                New SqlParameter("@PasswordSalt", user.PasswordSalt),
                New SqlParameter("@FullName", user.FullName),
                New SqlParameter("@RoleId", CInt(user.Role)),
                New SqlParameter("@DepartmentId", CInt(user.Department)),
                New SqlParameter("@MustChangePassword", user.MustChangePassword),
                New SqlParameter("@IsActive", user.IsActive),
                New SqlParameter("@CreatedBy", user.CreatedBy)
            }
            Return Await _sqlHelper.ExecuteScalarAsync(Of Integer)(query, params)
        End Function

        Public Async Function UpdateAsync(user As UserEntity) As Task(Of Boolean) Implements IUserRepository.UpdateAsync
            Await EnsureColumnExistsAsync()
            Const query As String = "UPDATE dbo.tbl_Users SET FullName = @FullName, RoleId = @RoleId, DepartmentId = @DepartmentId, " &
                                   "PasswordHash = @PasswordHash, PasswordSalt = @PasswordSalt, MustChangePassword = @MustChangePassword, " &
                                   "IsActive = @IsActive, ModifiedOn = GETUTCDATE(), ModifiedBy = @ModifiedBy WHERE UserId = @UserId AND IsDeleted = 0;"
            Dim params As SqlParameter() = {
                New SqlParameter("@FullName", user.FullName),
                New SqlParameter("@RoleId", CInt(user.Role)),
                New SqlParameter("@DepartmentId", CInt(user.Department)),
                New SqlParameter("@PasswordHash", user.PasswordHash),
                New SqlParameter("@PasswordSalt", user.PasswordSalt),
                New SqlParameter("@MustChangePassword", user.MustChangePassword),
                New SqlParameter("@IsActive", user.IsActive),
                New SqlParameter("@ModifiedBy", If(user.ModifiedBy.HasValue, CObj(user.ModifiedBy.Value), DBNull.Value)),
                New SqlParameter("@UserId", user.UserId)
            }
            Dim rows = Await _sqlHelper.ExecuteNonQueryAsync(query, params)
            Return rows > 0
        End Function

        Public Async Function SoftDeleteAsync(userId As Integer, modifiedBy As Integer) As Task(Of Boolean) Implements IUserRepository.SoftDeleteAsync
            Const query As String = "UPDATE dbo.tbl_Users SET IsDeleted = 1, ModifiedOn = GETUTCDATE(), ModifiedBy = @ModifiedBy WHERE UserId = @UserId;"
            Dim params As SqlParameter() = {
                New SqlParameter("@ModifiedBy", modifiedBy),
                New SqlParameter("@UserId", userId)
            }
            Dim rows = Await _sqlHelper.ExecuteNonQueryAsync(query, params)
            Return rows > 0
        End Function

        Private Function MapUserEntity(reader As IDataReader) As UserEntity
            Dim mustChange As Boolean = False
            Try
                Dim ord = reader.GetOrdinal("MustChangePassword")
                If ord >= 0 AndAlso Not reader.IsDBNull(ord) Then
                    mustChange = Convert.ToBoolean(reader(ord))
                End If
            Catch
                mustChange = False
            End Try

            Return New UserEntity() With {
                .UserId = Convert.ToInt32(reader("UserId")),
                .Username = Convert.ToString(reader("Username")),
                .PasswordHash = Convert.ToString(reader("PasswordHash")),
                .PasswordSalt = Convert.ToString(reader("PasswordSalt")),
                .FullName = Convert.ToString(reader("FullName")),
                .Role = CType(Convert.ToInt32(reader("RoleId")), UserRole),
                .Department = CType(Convert.ToInt32(reader("DepartmentId")), DepartmentType),
                .MustChangePassword = mustChange,
                .IsActive = Convert.ToBoolean(reader("IsActive")),
                .IsDeleted = Convert.ToBoolean(reader("IsDeleted")),
                .CreatedOn = Convert.ToDateTime(reader("CreatedOn")),
                .CreatedBy = Convert.ToInt32(reader("CreatedBy")),
                .ModifiedOn = If(reader("ModifiedOn") Is DBNull.Value, CType(Nothing, Nullable(Of DateTime)), Convert.ToDateTime(reader("ModifiedOn"))),
                .ModifiedBy = If(reader("ModifiedBy") Is DBNull.Value, CType(Nothing, Nullable(Of Integer)), Convert.ToInt32(reader("ModifiedBy")))
            }
        End Function
    End Class
End Namespace
