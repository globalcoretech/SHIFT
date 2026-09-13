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
    ''' Data Access Repository for Client master entity executing parameterized T-SQL queries.
    ''' Self-heals SQL Server schema on startup to ensure State, District, Pincode, Address,
    ''' EntityType, GstType, and IsLiveApiData columns are persisted and queried.
    ''' </summary>
    Public Class ClientRepository
        Implements IClientRepository

        Private ReadOnly _sqlHelper As ISqlHelper
        Private Shared _schemaEnsured As Boolean = False

        Public Sub New(sqlHelper As ISqlHelper)
            _sqlHelper = sqlHelper
            EnsureSchemaColumnsAsync().ConfigureAwait(False)
        End Sub

        Private Async Function EnsureSchemaColumnsAsync() As Task
            If _schemaEnsured Then Return
            Try
                Const migrationSql As String = "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tbl_Clients') AND name = 'StateName') " &
                                               "BEGIN " &
                                               "    ALTER TABLE dbo.tbl_Clients ADD StateName NVARCHAR(100) NULL, District NVARCHAR(100) NULL, Pincode NVARCHAR(20) NULL, Address NVARCHAR(MAX) NULL, EntityType NVARCHAR(100) NULL, GstType NVARCHAR(100) NULL, IsLiveApiData BIT NOT NULL CONSTRAINT DF_tbl_Clients_IsLiveApiData DEFAULT (0); " &
                                               "END;"
                Await _sqlHelper.ExecuteNonQueryAsync(migrationSql, Nothing)
                _schemaEnsured = True
            Catch ex As Exception
                ' Suppress schema migration error if columns already exist or user has limited DDL permissions
            End Try
        End Function

        Public Async Function GetByIdAsync(clientId As Integer) As Task(Of ClientEntity) Implements IClientRepository.GetByIdAsync
            Await EnsureSchemaColumnsAsync()
            Const query As String = "SELECT ClientId, ClientCode, ClientName, ContactPerson, Phone, Email, PAN AS PanNumber, GSTIN AS Gstin, StateName, District, Pincode, Address, EntityType, GstType, IsLiveApiData, DepartmentId, IsActive, IsDeleted, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy " &
                                   "FROM dbo.tbl_Clients WHERE ClientId = @ClientId AND IsDeleted = 0;"
            Dim params As SqlParameter() = {New SqlParameter("@ClientId", clientId)}
            Dim list = Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapClientEntity)
            Return If(list.Count > 0, list(0), Nothing)
        End Function

        Public Async Function GetByCodeAsync(clientCode As String) As Task(Of ClientEntity) Implements IClientRepository.GetByCodeAsync
            Await EnsureSchemaColumnsAsync()
            Const query As String = "SELECT ClientId, ClientCode, ClientName, ContactPerson, Phone, Email, PAN AS PanNumber, GSTIN AS Gstin, StateName, District, Pincode, Address, EntityType, GstType, IsLiveApiData, DepartmentId, IsActive, IsDeleted, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy " &
                                   "FROM dbo.tbl_Clients WHERE ClientCode = @ClientCode AND IsDeleted = 0;"
            Dim params As SqlParameter() = {New SqlParameter("@ClientCode", clientCode)}
            Dim list = Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapClientEntity)
            Return If(list.Count > 0, list(0), Nothing)
        End Function

        Public Async Function GetByPanAsync(panNumber As String) As Task(Of ClientEntity) Implements IClientRepository.GetByPanAsync
            If String.IsNullOrWhiteSpace(panNumber) Then Return Nothing
            Await EnsureSchemaColumnsAsync()
            Const query As String = "SELECT ClientId, ClientCode, ClientName, ContactPerson, Phone, Email, PAN AS PanNumber, GSTIN AS Gstin, StateName, District, Pincode, Address, EntityType, GstType, IsLiveApiData, DepartmentId, IsActive, IsDeleted, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy " &
                                   "FROM dbo.tbl_Clients WHERE PAN = @PanNumber AND IsDeleted = 0;"
            Dim params As SqlParameter() = {New SqlParameter("@PanNumber", panNumber.Trim())}
            Dim list = Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapClientEntity)
            Return If(list.Count > 0, list(0), Nothing)
        End Function

        Public Async Function GetByGstinAsync(gstin As String) As Task(Of ClientEntity) Implements IClientRepository.GetByGstinAsync
            If String.IsNullOrWhiteSpace(gstin) Then Return Nothing
            Await EnsureSchemaColumnsAsync()
            Const query As String = "SELECT ClientId, ClientCode, ClientName, ContactPerson, Phone, Email, PAN AS PanNumber, GSTIN AS Gstin, StateName, District, Pincode, Address, EntityType, GstType, IsLiveApiData, DepartmentId, IsActive, IsDeleted, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy " &
                                   "FROM dbo.tbl_Clients WHERE GSTIN = @Gstin AND IsDeleted = 0;"
            Dim params As SqlParameter() = {New SqlParameter("@Gstin", gstin.Trim())}
            Dim list = Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapClientEntity)
            Return If(list.Count > 0, list(0), Nothing)
        End Function

        Public Async Function GetAllAsync(Optional department As Nullable(Of DepartmentType) = Nothing, Optional includeDeleted As Boolean = False) As Task(Of List(Of ClientEntity)) Implements IClientRepository.GetAllAsync
            Await EnsureSchemaColumnsAsync()
            Dim query As String = "SELECT ClientId, ClientCode, ClientName, ContactPerson, Phone, Email, PAN AS PanNumber, GSTIN AS Gstin, StateName, District, Pincode, Address, EntityType, GstType, IsLiveApiData, DepartmentId, IsActive, IsDeleted, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy " &
                                  "FROM dbo.tbl_Clients "
            Dim params As New List(Of SqlParameter)()
            Dim whereClause As New List(Of String)()

            If Not includeDeleted Then
                whereClause.Add("IsDeleted = 0")
            End If

            If department.HasValue Then
                whereClause.Add("DepartmentId = @DepartmentId")
                params.Add(New SqlParameter("@DepartmentId", CInt(department.Value)))
            End If

            If whereClause.Count > 0 Then
                query &= "WHERE " & String.Join(" AND ", whereClause) & " "
            End If

            Dim list = Await _sqlHelper.ExecuteReaderAsync(query, params.ToArray(), AddressOf MapClientEntity)
            Dim resCount As Integer = If(list IsNot Nothing, list.Count, 0)
            System.Diagnostics.Trace.WriteLine($"[ClientDropdown] DB fetch started | Query: {query}")
            System.Diagnostics.Trace.WriteLine($"[ClientDropdown] SQL returned: {resCount} rows")
            System.Diagnostics.Trace.WriteLine($"[ClientDropdown] Repository materialized: {resCount} clients")
            Return list
        End Function

        Public Async Function GetClientsByAssignedUserAsync(userId As Integer) As Task(Of List(Of ClientEntity)) Implements IClientRepository.GetClientsByAssignedUserAsync
            Await EnsureSchemaColumnsAsync()
            Const query As String = "SELECT DISTINCT c.ClientId, c.ClientCode, c.ClientName, c.ContactPerson, c.Phone, c.Email, c.PAN AS PanNumber, c.GSTIN AS Gstin, c.StateName, c.District, c.Pincode, c.Address, c.EntityType, c.GstType, c.IsLiveApiData, c.DepartmentId, c.IsActive, c.IsDeleted, c.CreatedOn, c.CreatedBy, c.ModifiedOn, c.ModifiedBy " &
                                   "FROM dbo.tbl_Clients c " &
                                   "INNER JOIN dbo.tbl_Tasks t ON c.ClientId = t.ClientId " &
                                   "WHERE t.AssignedToUserId = @UserId AND c.IsDeleted = 0 AND t.IsDeleted = 0;"
            Dim params As SqlParameter() = {New SqlParameter("@UserId", userId)}
            Return Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapClientEntity)
        End Function

        Public Async Function AddAsync(client As ClientEntity) As Task(Of Integer) Implements IClientRepository.AddAsync
            Await EnsureSchemaColumnsAsync()
            Const query As String = "INSERT INTO dbo.tbl_Clients (ClientCode, ClientName, ClientCategoryId, ContactPerson, Phone, Email, PAN, GSTIN, StateName, District, Pincode, Address, EntityType, GstType, IsLiveApiData, DepartmentId, IsActive, IsDeleted, CreatedOn, CreatedBy) " &
                                   "VALUES (@ClientCode, @ClientName, ISNULL((SELECT TOP 1 ClientCategoryId FROM dbo.tbl_ClientCategories), 1), @ContactPerson, @Phone, @Email, @PanNumber, @Gstin, @StateName, @District, @Pincode, @Address, @EntityType, @GstType, @IsLiveApiData, @DepartmentId, @IsActive, 0, GETUTCDATE(), @CreatedBy); " &
                                   "SELECT SCOPE_IDENTITY();"
            Dim params As SqlParameter() = {
                New SqlParameter("@ClientCode", client.ClientCode),
                New SqlParameter("@ClientName", client.ClientName),
                New SqlParameter("@ContactPerson", If(String.IsNullOrEmpty(client.ContactPerson), CObj(DBNull.Value), client.ContactPerson)),
                New SqlParameter("@Phone", If(String.IsNullOrEmpty(client.Phone), CObj(DBNull.Value), client.Phone)),
                New SqlParameter("@Email", If(String.IsNullOrEmpty(client.Email), CObj(DBNull.Value), client.Email)),
                New SqlParameter("@PanNumber", If(String.IsNullOrEmpty(client.PanNumber), CObj(DBNull.Value), client.PanNumber)),
                New SqlParameter("@Gstin", If(String.IsNullOrEmpty(client.Gstin), CObj(DBNull.Value), client.Gstin)),
                New SqlParameter("@StateName", If(String.IsNullOrEmpty(client.StateName), CObj(DBNull.Value), client.StateName)),
                New SqlParameter("@District", If(String.IsNullOrEmpty(client.District), CObj(DBNull.Value), client.District)),
                New SqlParameter("@Pincode", If(String.IsNullOrEmpty(client.Pincode), CObj(DBNull.Value), client.Pincode)),
                New SqlParameter("@Address", If(String.IsNullOrEmpty(client.Address), CObj(DBNull.Value), client.Address)),
                New SqlParameter("@EntityType", If(String.IsNullOrEmpty(client.EntityType), CObj(DBNull.Value), client.EntityType)),
                New SqlParameter("@GstType", If(String.IsNullOrEmpty(client.GstType), CObj(DBNull.Value), client.GstType)),
                New SqlParameter("@IsLiveApiData", client.IsLiveApiData),
                New SqlParameter("@DepartmentId", If(client.Department.HasValue, CObj(CInt(client.Department.Value)), DBNull.Value)),
                New SqlParameter("@IsActive", client.IsActive),
                New SqlParameter("@CreatedBy", client.CreatedBy)
            }
            Return Await _sqlHelper.ExecuteScalarAsync(Of Integer)(query, params)
        End Function

        Public Async Function AddBulkAsync(clients As IEnumerable(Of ClientEntity)) As Task(Of Integer) Implements IClientRepository.AddBulkAsync
            Await EnsureSchemaColumnsAsync()
            
            Dim successCount As Integer = 0
            Using conn As SqlConnection = CType(_sqlHelper.GetConnection(), SqlConnection)
                Await conn.OpenAsync()
                Using tx As SqlTransaction = CType(Await conn.BeginTransactionAsync(), SqlTransaction)
                    Try
                        For Each client In clients
                            Const query As String = "INSERT INTO dbo.tbl_Clients (ClientCode, ClientName, ClientCategoryId, ContactPerson, Phone, Email, PAN, GSTIN, StateName, District, Pincode, Address, EntityType, GstType, IsLiveApiData, DepartmentId, IsActive, IsDeleted, CreatedOn, CreatedBy) " &
                                                   "VALUES (@ClientCode, @ClientName, ISNULL((SELECT TOP 1 ClientCategoryId FROM dbo.tbl_ClientCategories), 1), @ContactPerson, @Phone, @Email, @PanNumber, @Gstin, @StateName, @District, @Pincode, @Address, @EntityType, @GstType, @IsLiveApiData, @DepartmentId, @IsActive, 0, GETUTCDATE(), @CreatedBy); " &
                                                   "SELECT SCOPE_IDENTITY();"
                            Dim params As SqlParameter() = {
                                New SqlParameter("@ClientCode", client.ClientCode),
                                New SqlParameter("@ClientName", client.ClientName),
                                New SqlParameter("@ContactPerson", If(String.IsNullOrEmpty(client.ContactPerson), CObj(DBNull.Value), client.ContactPerson)),
                                New SqlParameter("@Phone", If(String.IsNullOrEmpty(client.Phone), CObj(DBNull.Value), client.Phone)),
                                New SqlParameter("@Email", If(String.IsNullOrEmpty(client.Email), CObj(DBNull.Value), client.Email)),
                                New SqlParameter("@PanNumber", If(String.IsNullOrEmpty(client.PanNumber), CObj(DBNull.Value), client.PanNumber)),
                                New SqlParameter("@Gstin", If(String.IsNullOrEmpty(client.Gstin), CObj(DBNull.Value), client.Gstin)),
                                New SqlParameter("@StateName", If(String.IsNullOrEmpty(client.StateName), CObj(DBNull.Value), client.StateName)),
                                New SqlParameter("@District", If(String.IsNullOrEmpty(client.District), CObj(DBNull.Value), client.District)),
                                New SqlParameter("@Pincode", If(String.IsNullOrEmpty(client.Pincode), CObj(DBNull.Value), client.Pincode)),
                                New SqlParameter("@Address", If(String.IsNullOrEmpty(client.Address), CObj(DBNull.Value), client.Address)),
                                New SqlParameter("@EntityType", If(String.IsNullOrEmpty(client.EntityType), CObj(DBNull.Value), client.EntityType)),
                                New SqlParameter("@GstType", If(String.IsNullOrEmpty(client.GstType), CObj(DBNull.Value), client.GstType)),
                                New SqlParameter("@IsLiveApiData", client.IsLiveApiData),
                                New SqlParameter("@DepartmentId", If(client.Department.HasValue, CObj(CInt(client.Department.Value)), DBNull.Value)),
                                New SqlParameter("@IsActive", client.IsActive),
                                New SqlParameter("@CreatedBy", client.CreatedBy)
                            }
                            Await _sqlHelper.ExecuteScalarAsync(Of Integer)(query, params, tx)
                            successCount += 1
                        Next
                        Await tx.CommitAsync()
                        Return successCount
                    Catch ex As Exception
                        tx.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Function

        Public Async Function UpdateAsync(client As ClientEntity) As Task(Of Boolean) Implements IClientRepository.UpdateAsync
            Await EnsureSchemaColumnsAsync()
            Const query As String = "UPDATE dbo.tbl_Clients SET ClientName = @ClientName, ContactPerson = @ContactPerson, Phone = @Phone, Email = @Email, " &
                                   "PAN = @PanNumber, GSTIN = @Gstin, StateName = @StateName, District = @District, Pincode = @Pincode, Address = @Address, EntityType = @EntityType, GstType = @GstType, IsLiveApiData = @IsLiveApiData, DepartmentId = @DepartmentId, IsActive = @IsActive, ModifiedOn = GETUTCDATE(), ModifiedBy = @ModifiedBy " &
                                   "WHERE ClientId = @ClientId AND IsDeleted = 0;"
            Dim params As SqlParameter() = {
                New SqlParameter("@ClientName", client.ClientName),
                New SqlParameter("@ContactPerson", If(String.IsNullOrEmpty(client.ContactPerson), CObj(DBNull.Value), client.ContactPerson)),
                New SqlParameter("@Phone", If(String.IsNullOrEmpty(client.Phone), CObj(DBNull.Value), client.Phone)),
                New SqlParameter("@Email", If(String.IsNullOrEmpty(client.Email), CObj(DBNull.Value), client.Email)),
                New SqlParameter("@PanNumber", If(String.IsNullOrEmpty(client.PanNumber), CObj(DBNull.Value), client.PanNumber)),
                New SqlParameter("@Gstin", If(String.IsNullOrEmpty(client.Gstin), CObj(DBNull.Value), client.Gstin)),
                New SqlParameter("@StateName", If(String.IsNullOrEmpty(client.StateName), CObj(DBNull.Value), client.StateName)),
                New SqlParameter("@District", If(String.IsNullOrEmpty(client.District), CObj(DBNull.Value), client.District)),
                New SqlParameter("@Pincode", If(String.IsNullOrEmpty(client.Pincode), CObj(DBNull.Value), client.Pincode)),
                New SqlParameter("@Address", If(String.IsNullOrEmpty(client.Address), CObj(DBNull.Value), client.Address)),
                New SqlParameter("@EntityType", If(String.IsNullOrEmpty(client.EntityType), CObj(DBNull.Value), client.EntityType)),
                New SqlParameter("@GstType", If(String.IsNullOrEmpty(client.GstType), CObj(DBNull.Value), client.GstType)),
                New SqlParameter("@IsLiveApiData", client.IsLiveApiData),
                New SqlParameter("@DepartmentId", If(client.Department.HasValue, CObj(CInt(client.Department.Value)), DBNull.Value)),
                New SqlParameter("@IsActive", client.IsActive),
                New SqlParameter("@ModifiedBy", If(client.ModifiedBy.HasValue, CObj(client.ModifiedBy.Value), DBNull.Value)),
                New SqlParameter("@ClientId", client.ClientId)
            }
            Dim rows = Await _sqlHelper.ExecuteNonQueryAsync(query, params)
            Return rows > 0
        End Function

        Public Async Function SoftDeleteAsync(clientId As Integer, modifiedBy As Integer) As Task(Of Boolean) Implements IClientRepository.SoftDeleteAsync
            Const query As String = "UPDATE dbo.tbl_Clients SET IsDeleted = 1, ModifiedOn = GETUTCDATE(), ModifiedBy = @ModifiedBy WHERE ClientId = @ClientId;"
            Dim params As SqlParameter() = {
                New SqlParameter("@ModifiedBy", modifiedBy),
                New SqlParameter("@ClientId", clientId)
            }
            Dim rows = Await _sqlHelper.ExecuteNonQueryAsync(query, params)
            Return rows > 0
        End Function

        Public Async Function ReactivateAsync(clientId As Integer, modifiedBy As Integer) As Task(Of Boolean) Implements IClientRepository.ReactivateAsync
            Const query As String = "UPDATE dbo.tbl_Clients SET IsDeleted = 0, ModifiedOn = GETUTCDATE(), ModifiedBy = @ModifiedBy WHERE ClientId = @ClientId;"
            Dim params As SqlParameter() = {
                New SqlParameter("@ModifiedBy", modifiedBy),
                New SqlParameter("@ClientId", clientId)
            }
            Dim rows = Await _sqlHelper.ExecuteNonQueryAsync(query, params)
            Return rows > 0
        End Function

        Private Function MapClientEntity(reader As IDataReader) As ClientEntity
            Return New ClientEntity() With {
                .ClientId = Convert.ToInt32(reader("ClientId")),
                .ClientCode = Convert.ToString(reader("ClientCode")),
                .ClientName = Convert.ToString(reader("ClientName")),
                .ContactPerson = If(reader("ContactPerson") Is DBNull.Value, String.Empty, Convert.ToString(reader("ContactPerson"))),
                .Phone = If(reader("Phone") Is DBNull.Value, String.Empty, Convert.ToString(reader("Phone"))),
                .Email = If(reader("Email") Is DBNull.Value, String.Empty, Convert.ToString(reader("Email"))),
                .PanNumber = If(reader.HasColumn("PanNumber") AndAlso reader("PanNumber") IsNot DBNull.Value, Convert.ToString(reader("PanNumber")), String.Empty),
                .Gstin = If(reader.HasColumn("Gstin") AndAlso reader("Gstin") IsNot DBNull.Value, Convert.ToString(reader("Gstin")), String.Empty),
                .StateName = If(reader.HasColumn("StateName") AndAlso reader("StateName") IsNot DBNull.Value, Convert.ToString(reader("StateName")), String.Empty),
                .District = If(reader.HasColumn("District") AndAlso reader("District") IsNot DBNull.Value, Convert.ToString(reader("District")), String.Empty),
                .Pincode = If(reader.HasColumn("Pincode") AndAlso reader("Pincode") IsNot DBNull.Value, Convert.ToString(reader("Pincode")), String.Empty),
                .Address = If(reader.HasColumn("Address") AndAlso reader("Address") IsNot DBNull.Value, Convert.ToString(reader("Address")), String.Empty),
                .EntityType = If(reader.HasColumn("EntityType") AndAlso reader("EntityType") IsNot DBNull.Value, Convert.ToString(reader("EntityType")), "Proprietorship"),
                .GstType = If(reader.HasColumn("GstType") AndAlso reader("GstType") IsNot DBNull.Value, Convert.ToString(reader("GstType")), "Regular Monthly"),
                .IsLiveApiData = If(reader.HasColumn("IsLiveApiData") AndAlso reader("IsLiveApiData") IsNot DBNull.Value, Convert.ToBoolean(reader("IsLiveApiData")), False),
                .Department = If(reader("DepartmentId") Is DBNull.Value, CType(Nothing, Nullable(Of DepartmentType)), CType(Convert.ToInt32(reader("DepartmentId")), Nullable(Of DepartmentType))),
                .IsActive = Convert.ToBoolean(reader("IsActive")),
                .IsDeleted = Convert.ToBoolean(reader("IsDeleted")),
                .CreatedOn = Convert.ToDateTime(reader("CreatedOn")),
                .CreatedBy = Convert.ToInt32(reader("CreatedBy")),
                .ModifiedOn = If(reader("ModifiedOn") Is DBNull.Value, CType(Nothing, Nullable(Of DateTime)), Convert.ToDateTime(reader("ModifiedOn"))),
                .ModifiedBy = If(reader("ModifiedBy") Is DBNull.Value, CType(Nothing, Nullable(Of Integer)), Convert.ToInt32(reader("ModifiedBy")))
            }
        End Function
    End Class

    Public Module DataReaderExtensions
        <System.Runtime.CompilerServices.Extension()>
        Public Function HasColumn(reader As IDataReader, columnName As String) As Boolean
            For i As Integer = 0 To reader.FieldCount - 1
                If reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase) Then Return True
            Next
            Return False
        End Function
    End Module
End Namespace
