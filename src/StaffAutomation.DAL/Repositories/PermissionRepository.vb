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
    ''' Data Access Repository for Central Permissions Catalog executing parameterized T-SQL queries.
    ''' Manages queries for dbo.tbl_Permissions catalog items.
    ''' </summary>
    Public Class PermissionRepository
        Implements IPermissionRepository

        Private ReadOnly _sqlHelper As ISqlHelper

        Public Sub New(sqlHelper As ISqlHelper)
            _sqlHelper = sqlHelper
        End Sub

        Public Async Function GetByIdAsync(permissionId As Integer) As Task(Of PermissionEntity) Implements IPermissionRepository.GetByIdAsync
            Const query As String = "SELECT PermissionId, PermissionCode, PermissionName, ModuleName, Description, IsActive, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy " &
                                   "FROM dbo.tbl_Permissions WHERE PermissionId = @PermissionId AND IsActive = 1;"
            Dim params As SqlParameter() = {New SqlParameter("@PermissionId", permissionId)}
            Dim list = Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapPermissionEntity)
            Return If(list.Count > 0, list(0), Nothing)
        End Function

        Public Async Function GetByCodeAsync(permissionCode As String) As Task(Of PermissionEntity) Implements IPermissionRepository.GetByCodeAsync
            If String.IsNullOrWhiteSpace(permissionCode) Then Return Nothing
            Const query As String = "SELECT PermissionId, PermissionCode, PermissionName, ModuleName, Description, IsActive, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy " &
                                   "FROM dbo.tbl_Permissions WHERE PermissionCode = @PermissionCode AND IsActive = 1;"
            Dim params As SqlParameter() = {New SqlParameter("@PermissionCode", permissionCode.Trim())}
            Dim list = Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapPermissionEntity)
            Return If(list.Count > 0, list(0), Nothing)
        End Function

        Public Async Function GetAllAsync() As Task(Of List(Of PermissionEntity)) Implements IPermissionRepository.GetAllAsync
            Const query As String = "SELECT PermissionId, PermissionCode, PermissionName, ModuleName, Description, IsActive, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy " &
                                   "FROM dbo.tbl_Permissions WHERE IsActive = 1 ORDER BY ModuleName ASC, PermissionCode ASC;"
            Return Await _sqlHelper.ExecuteReaderAsync(query, Nothing, AddressOf MapPermissionEntity)
        End Function

        Public Async Function GetByModuleAsync(moduleName As String) As Task(Of List(Of PermissionEntity)) Implements IPermissionRepository.GetByModuleAsync
            If String.IsNullOrWhiteSpace(moduleName) Then Return New List(Of PermissionEntity)()
            Const query As String = "SELECT PermissionId, PermissionCode, PermissionName, ModuleName, Description, IsActive, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy " &
                                   "FROM dbo.tbl_Permissions WHERE ModuleName = @ModuleName AND IsActive = 1 ORDER BY PermissionCode ASC;"
            Dim params As SqlParameter() = {New SqlParameter("@ModuleName", moduleName.Trim())}
            Return Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapPermissionEntity)
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
