Imports System
Imports System.Collections.Generic
Imports System.Data
Imports Microsoft.Data.SqlClient
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.DAL.Interfaces

Namespace Repositories
    ''' <summary>
    ''' Data Access Repository for User Permission Overrides executing parameterized T-SQL queries.
    ''' Manages persistence for dbo.tbl_UserPermissionOverrides with unique (UserId, PermissionId) enforcement.
    ''' </summary>
    Public Class UserPermissionOverrideRepository
        Implements IUserPermissionOverrideRepository

        Private ReadOnly _sqlHelper As ISqlHelper

        Public Sub New(sqlHelper As ISqlHelper)
            _sqlHelper = sqlHelper
        End Sub

        Public Async Function GetByUserIdAsync(userId As Integer) As Task(Of List(Of UserPermissionOverrideEntity)) Implements IUserPermissionOverrideRepository.GetByUserIdAsync
            Const query As String = "SELECT OverrideId, UserId, PermissionId, IsGranted, Reason, GrantedOn, GrantedBy " &
                                   "FROM dbo.tbl_UserPermissionOverrides WHERE UserId = @UserId;"
            Dim params As SqlParameter() = {New SqlParameter("@UserId", userId)}
            Return Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapUserPermissionOverrideEntity)
        End Function

        Public Async Function GetOverrideAsync(userId As Integer, permissionId As Integer) As Task(Of UserPermissionOverrideEntity) Implements IUserPermissionOverrideRepository.GetOverrideAsync
            Const query As String = "SELECT OverrideId, UserId, PermissionId, IsGranted, Reason, GrantedOn, GrantedBy " &
                                   "FROM dbo.tbl_UserPermissionOverrides WHERE UserId = @UserId AND PermissionId = @PermissionId;"
            Dim params As SqlParameter() = {
                New SqlParameter("@UserId", userId),
                New SqlParameter("@PermissionId", permissionId)
            }
            Dim list = Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapUserPermissionOverrideEntity)
            Return If(list.Count > 0, list(0), Nothing)
        End Function

        Public Async Function SetOverrideAsync(overrideItem As UserPermissionOverrideEntity) As Task(Of Boolean) Implements IUserPermissionOverrideRepository.SetOverrideAsync
            Const query As String = "IF EXISTS (SELECT 1 FROM dbo.tbl_UserPermissionOverrides WHERE UserId = @UserId AND PermissionId = @PermissionId) " &
                                   "BEGIN " &
                                   "    UPDATE dbo.tbl_UserPermissionOverrides " &
                                   "    SET IsGranted = @IsGranted, Reason = @Reason, GrantedOn = GETUTCDATE(), GrantedBy = @GrantedBy " &
                                   "    WHERE UserId = @UserId AND PermissionId = @PermissionId; " &
                                   "END " &
                                   "ELSE " &
                                   "BEGIN " &
                                   "    INSERT INTO dbo.tbl_UserPermissionOverrides (UserId, PermissionId, IsGranted, Reason, GrantedOn, GrantedBy) " &
                                   "    VALUES (@UserId, @PermissionId, @IsGranted, @Reason, GETUTCDATE(), @GrantedBy); " &
                                   "END;"

            Dim params As SqlParameter() = {
                New SqlParameter("@UserId", overrideItem.UserId),
                New SqlParameter("@PermissionId", overrideItem.PermissionId),
                New SqlParameter("@IsGranted", overrideItem.IsGranted),
                New SqlParameter("@Reason", If(String.IsNullOrEmpty(overrideItem.Reason), CObj(DBNull.Value), overrideItem.Reason)),
                New SqlParameter("@GrantedBy", overrideItem.GrantedBy)
            }

            Dim rows = Await _sqlHelper.ExecuteNonQueryAsync(query, params)
            Return rows > 0
        End Function

        Public Async Function DeleteOverrideAsync(userId As Integer, permissionId As Integer) As Task(Of Boolean) Implements IUserPermissionOverrideRepository.DeleteOverrideAsync
            Const query As String = "DELETE FROM dbo.tbl_UserPermissionOverrides WHERE UserId = @UserId AND PermissionId = @PermissionId;"
            Dim params As SqlParameter() = {
                New SqlParameter("@UserId", userId),
                New SqlParameter("@PermissionId", permissionId)
            }
            Dim rows = Await _sqlHelper.ExecuteNonQueryAsync(query, params)
            Return rows > 0
        End Function

        Private Function MapUserPermissionOverrideEntity(reader As IDataReader) As UserPermissionOverrideEntity
            Return New UserPermissionOverrideEntity() With {
                .OverrideId = Convert.ToInt32(reader("OverrideId")),
                .UserId = Convert.ToInt32(reader("UserId")),
                .PermissionId = Convert.ToInt32(reader("PermissionId")),
                .IsGranted = Convert.ToBoolean(reader("IsGranted")),
                .Reason = If(reader("Reason") Is DBNull.Value, String.Empty, Convert.ToString(reader("Reason"))),
                .GrantedOn = Convert.ToDateTime(reader("GrantedOn")),
                .GrantedBy = Convert.ToInt32(reader("GrantedBy"))
            }
        End Function
    End Class
End Namespace
