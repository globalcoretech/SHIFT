Imports System
Imports System.Collections.Generic
Imports System.Data
Imports Microsoft.Data.SqlClient
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.DAL.Interfaces

Namespace Repositories
    ''' <summary>
    ''' Data Access Repository for Role-Permission Matrix executing parameterized T-SQL queries.
    ''' Manages persistence for dbo.tbl_RolePermissions with atomic transaction replacement.
    ''' </summary>
    Public Class RolePermissionRepository
        Implements IRolePermissionRepository

        Private ReadOnly _sqlHelper As ISqlHelper
        Private ReadOnly _connectionFactory As IDatabaseConnectionFactory

        Public Sub New(sqlHelper As ISqlHelper, connectionFactory As IDatabaseConnectionFactory)
            _sqlHelper = sqlHelper
            _connectionFactory = connectionFactory
        End Sub

        Public Async Function GetByRoleIdAsync(roleId As Integer) As Task(Of List(Of RolePermissionEntity)) Implements IRolePermissionRepository.GetByRoleIdAsync
            Const query As String = "SELECT RolePermissionId, RoleId, PermissionId, GrantedOn, GrantedBy " &
                                   "FROM dbo.tbl_RolePermissions WHERE RoleId = @RoleId;"
            Dim params As SqlParameter() = {New SqlParameter("@RoleId", roleId)}
            Return Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapRolePermissionEntity)
        End Function

        Public Async Function GetPermissionsForRoleAsync(roleId As Integer) As Task(Of List(Of PermissionEntity)) Implements IRolePermissionRepository.GetPermissionsForRoleAsync
            Const query As String = "SELECT p.PermissionId, p.PermissionCode, p.PermissionName, p.ModuleName, p.Description, p.IsActive, p.CreatedOn, p.CreatedBy, p.ModifiedOn, p.ModifiedBy " &
                                   "FROM dbo.tbl_Permissions p " &
                                   "INNER JOIN dbo.tbl_RolePermissions rp ON p.PermissionId = rp.PermissionId " &
                                   "WHERE rp.RoleId = @RoleId AND p.IsActive = 1 " &
                                   "ORDER BY p.ModuleName ASC, p.PermissionCode ASC;"
            Dim params As SqlParameter() = {New SqlParameter("@RoleId", roleId)}
            Return Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapPermissionEntity)
        End Function

        Public Async Function SetRolePermissionsAsync(roleId As Integer, permissionIds As List(Of Integer), grantedBy As Integer) As Task(Of Boolean) Implements IRolePermissionRepository.SetRolePermissionsAsync
            Using conn As SqlConnection = CType(_connectionFactory.CreateConnection(), SqlConnection)
                Await conn.OpenAsync()
                Using trans As SqlTransaction = conn.BeginTransaction()
                    Try
                        ' Step 1: Remove existing role-permission assignments for this role
                        Const deleteQuery As String = "DELETE FROM dbo.tbl_RolePermissions WHERE RoleId = @RoleId;"
                        Using cmdDelete As SqlCommand = conn.CreateCommand()
                            cmdDelete.Transaction = trans
                            cmdDelete.CommandText = deleteQuery
                            cmdDelete.Parameters.AddWithValue("@RoleId", roleId)
                            Await cmdDelete.ExecuteNonQueryAsync()
                        End Using

                        ' Step 2: Insert new permission assignments atomically
                        If permissionIds IsNot Nothing AndAlso permissionIds.Count > 0 Then
                            Const insertQuery As String = "INSERT INTO dbo.tbl_RolePermissions (RoleId, PermissionId, GrantedOn, GrantedBy) " &
                                                         "VALUES (@RoleId, @PermissionId, GETUTCDATE(), @GrantedBy);"
                            For Each permId In permissionIds
                                Using cmdInsert As SqlCommand = conn.CreateCommand()
                                    cmdInsert.Transaction = trans
                                    cmdInsert.CommandText = insertQuery
                                    cmdInsert.Parameters.AddWithValue("@RoleId", roleId)
                                    cmdInsert.Parameters.AddWithValue("@PermissionId", permId)
                                    cmdInsert.Parameters.AddWithValue("@GrantedBy", grantedBy)
                                    Await cmdInsert.ExecuteNonQueryAsync()
                                End Using
                            Next
                        End If

                        trans.Commit()
                        Return True
                    Catch ex As Exception
                        trans.Rollback()
                        Throw New DataAccessException("Failed to replace role permissions matrix within transaction: " & ex.Message, ex)
                    End Try
                End Using
            End Using
        End Function

        Private Function MapRolePermissionEntity(reader As IDataReader) As RolePermissionEntity
            Return New RolePermissionEntity() With {
                .RolePermissionId = Convert.ToInt32(reader("RolePermissionId")),
                .RoleId = Convert.ToInt32(reader("RoleId")),
                .PermissionId = Convert.ToInt32(reader("PermissionId")),
                .GrantedOn = Convert.ToDateTime(reader("GrantedOn")),
                .GrantedBy = Convert.ToInt32(reader("GrantedBy"))
            }
        End Function

        Private Function MapPermissionEntity(reader As IDataReader) As PermissionEntity
            Return New PermissionEntity() With {
                .PermissionId = Convert.ToInt32(reader("PermissionId")),
                .PermissionCode = Convert.ToString(reader("PermissionCode")),
                .PermissionName = Convert.ToString(reader("PermissionName")),
                .ModuleName = Convert.ToString(reader("ModuleName")),
                .Description = If(reader("Description") Is DBNull.Value, String.Empty, Convert.ToString(reader("Description"))),
                .IsActive = Convert.ToBoolean(reader("IsActive")),
                .CreatedOn = Convert.ToDateTime(reader("CreatedOn")),
                .CreatedBy = Convert.ToInt32(reader("CreatedBy")),
                .ModifiedOn = If(reader("ModifiedOn") Is DBNull.Value, CType(Nothing, Nullable(Of DateTime)), Convert.ToDateTime(reader("ModifiedOn"))),
                .ModifiedBy = If(reader("ModifiedBy") Is DBNull.Value, CType(Nothing, Nullable(Of Integer)), Convert.ToInt32(reader("ModifiedBy")))
            }
        End Function
    End Class
End Namespace
