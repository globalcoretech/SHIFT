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
    ''' Data Access Repository for Financial Year accounting periods executing parameterized T-SQL queries.
    ''' Manages persistence for dbo.tbl_FinancialYears with single-active-FY transaction enforcement.
    ''' </summary>
    Public Class FinancialYearRepository
        Implements IFinancialYearRepository

        Private ReadOnly _sqlHelper As ISqlHelper
        Private ReadOnly _connectionFactory As IDatabaseConnectionFactory

        Public Sub New(sqlHelper As ISqlHelper, connectionFactory As IDatabaseConnectionFactory)
            _sqlHelper = sqlHelper
            _connectionFactory = connectionFactory
        End Sub

        Public Async Function GetByIdAsync(financialYearId As Integer) As Task(Of FinancialYearEntity) Implements IFinancialYearRepository.GetByIdAsync
            Const query As String = "SELECT FinancialYearId, FYCode, StartDate, EndDate, IsCurrentFY, IsLocked, IsDeleted, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy " &
                                   "FROM dbo.tbl_FinancialYears WHERE FinancialYearId = @FinancialYearId AND IsDeleted = 0;"
            Dim params As SqlParameter() = {New SqlParameter("@FinancialYearId", financialYearId)}
            Dim list = Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapFinancialYearEntity)
            Return If(list.Count > 0, list(0), Nothing)
        End Function

        Public Async Function GetActiveAsync() As Task(Of FinancialYearEntity) Implements IFinancialYearRepository.GetActiveAsync
            Const query As String = "SELECT FinancialYearId, FYCode, StartDate, EndDate, IsCurrentFY, IsLocked, IsDeleted, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy " &
                                   "FROM dbo.tbl_FinancialYears WHERE IsCurrentFY = 1 AND IsDeleted = 0;"
            Dim list = Await _sqlHelper.ExecuteReaderAsync(query, Nothing, AddressOf MapFinancialYearEntity)
            Return If(list.Count > 0, list(0), Nothing)
        End Function

        Public Async Function GetAllAsync(Optional includeDeleted As Boolean = False) As Task(Of List(Of FinancialYearEntity)) Implements IFinancialYearRepository.GetAllAsync
            Dim query As String = "SELECT FinancialYearId, FYCode, StartDate, EndDate, IsCurrentFY, IsLocked, IsDeleted, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy " &
                                  "FROM dbo.tbl_FinancialYears "
            If Not includeDeleted Then
                query &= "WHERE IsDeleted = 0 "
            End If
            query &= "ORDER BY StartDate DESC;"

            Return Await _sqlHelper.ExecuteReaderAsync(query, Nothing, AddressOf MapFinancialYearEntity)
        End Function

        Public Async Function AddAsync(financialYear As FinancialYearEntity) As Task(Of Integer) Implements IFinancialYearRepository.AddAsync
            Const query As String = "INSERT INTO dbo.tbl_FinancialYears (FYCode, StartDate, EndDate, IsCurrentFY, IsLocked, IsDeleted, CreatedOn, CreatedBy) " &
                                   "VALUES (@FYCode, @StartDate, @EndDate, @IsCurrentFY, @IsLocked, 0, GETUTCDATE(), @CreatedBy); " &
                                   "SELECT SCOPE_IDENTITY();"
            Dim params As SqlParameter() = {
                New SqlParameter("@FYCode", financialYear.FYCode),
                New SqlParameter("@StartDate", financialYear.StartDate),
                New SqlParameter("@EndDate", financialYear.EndDate),
                New SqlParameter("@IsCurrentFY", financialYear.IsCurrentFY),
                New SqlParameter("@IsLocked", financialYear.IsLocked),
                New SqlParameter("@CreatedBy", financialYear.CreatedBy)
            }
            Return Await _sqlHelper.ExecuteScalarAsync(Of Integer)(query, params)
        End Function

        Public Async Function UpdateAsync(financialYear As FinancialYearEntity) As Task(Of Boolean) Implements IFinancialYearRepository.UpdateAsync
            Const query As String = "UPDATE dbo.tbl_FinancialYears " &
                                   "SET FYCode = @FYCode, StartDate = @StartDate, EndDate = @EndDate, IsCurrentFY = @IsCurrentFY, IsLocked = @IsLocked, ModifiedOn = GETUTCDATE(), ModifiedBy = @ModifiedBy " &
                                   "WHERE FinancialYearId = @FinancialYearId AND IsDeleted = 0 AND IsLocked = 0;"
            Dim params As SqlParameter() = {
                New SqlParameter("@FYCode", financialYear.FYCode),
                New SqlParameter("@StartDate", financialYear.StartDate),
                New SqlParameter("@EndDate", financialYear.EndDate),
                New SqlParameter("@IsCurrentFY", financialYear.IsCurrentFY),
                New SqlParameter("@IsLocked", financialYear.IsLocked),
                New SqlParameter("@ModifiedBy", If(financialYear.ModifiedBy.HasValue, CObj(financialYear.ModifiedBy.Value), DBNull.Value)),
                New SqlParameter("@FinancialYearId", financialYear.FinancialYearId)
            }
            Dim rows = Await _sqlHelper.ExecuteNonQueryAsync(query, params)
            Return rows > 0
        End Function

        Public Async Function SetActiveAsync(financialYearId As Integer, modifiedBy As Integer) As Task(Of Boolean) Implements IFinancialYearRepository.SetActiveAsync
            Using conn As SqlConnection = CType(_connectionFactory.CreateConnection(), SqlConnection)
                Await conn.OpenAsync()
                Using trans As SqlTransaction = conn.BeginTransaction()
                    Try
                        ' Step 1: Clear current FY flag across all financial years
                        Const clearQuery As String = "UPDATE dbo.tbl_FinancialYears SET IsCurrentFY = 0, ModifiedOn = GETUTCDATE(), ModifiedBy = @ModifiedBy WHERE IsCurrentFY = 1;"
                        Using cmdClear As SqlCommand = conn.CreateCommand()
                            cmdClear.Transaction = trans
                            cmdClear.CommandText = clearQuery
                            cmdClear.Parameters.AddWithValue("@ModifiedBy", modifiedBy)
                            Await cmdClear.ExecuteNonQueryAsync()
                        End Using

                        ' Step 2: Set target financial year active
                        Const setActiveQuery As String = "UPDATE dbo.tbl_FinancialYears SET IsCurrentFY = 1, ModifiedOn = GETUTCDATE(), ModifiedBy = @ModifiedBy WHERE FinancialYearId = @FinancialYearId AND IsDeleted = 0;"
                        Dim rowsUpdated As Integer = 0
                        Using cmdSet As SqlCommand = conn.CreateCommand()
                            cmdSet.Transaction = trans
                            cmdSet.CommandText = setActiveQuery
                            cmdSet.Parameters.AddWithValue("@ModifiedBy", modifiedBy)
                            cmdSet.Parameters.AddWithValue("@FinancialYearId", financialYearId)
                            rowsUpdated = Await cmdSet.ExecuteNonQueryAsync()
                        End Using

                        If rowsUpdated > 0 Then
                            trans.Commit()
                            Return True
                        Else
                            trans.Rollback()
                            Return False
                        End If
                    Catch ex As Exception
                        trans.Rollback()
                        Throw New DataAccessException("Failed to set active financial year within transaction: " & ex.Message, ex)
                    End Try
                End Using
            End Using
        End Function

        Public Async Function SetLockStateAsync(financialYearId As Integer, isLocked As Boolean, modifiedBy As Integer) As Task(Of Boolean) Implements IFinancialYearRepository.SetLockStateAsync
            Const query As String = "UPDATE dbo.tbl_FinancialYears " &
                                   "SET IsLocked = @IsLocked, ModifiedOn = GETUTCDATE(), ModifiedBy = @ModifiedBy " &
                                   "WHERE FinancialYearId = @FinancialYearId AND IsDeleted = 0;"
            Dim params As SqlParameter() = {
                New SqlParameter("@IsLocked", isLocked),
                New SqlParameter("@ModifiedBy", modifiedBy),
                New SqlParameter("@FinancialYearId", financialYearId)
            }
            Dim rows = Await _sqlHelper.ExecuteNonQueryAsync(query, params)
            Return rows > 0
        End Function

        Private Function MapFinancialYearEntity(reader As IDataReader) As FinancialYearEntity
            Return New FinancialYearEntity() With {
                .FinancialYearId = Convert.ToInt32(reader("FinancialYearId")),
                .FYCode = Convert.ToString(reader("FYCode")),
                .StartDate = Convert.ToDateTime(reader("StartDate")),
                .EndDate = Convert.ToDateTime(reader("EndDate")),
                .IsCurrentFY = Convert.ToBoolean(reader("IsCurrentFY")),
                .IsLocked = Convert.ToBoolean(reader("IsLocked")),
                .IsDeleted = Convert.ToBoolean(reader("IsDeleted")),
                .CreatedOn = Convert.ToDateTime(reader("CreatedOn")),
                .CreatedBy = Convert.ToInt32(reader("CreatedBy")),
                .ModifiedOn = If(reader("ModifiedOn") Is DBNull.Value, CType(Nothing, Nullable(Of DateTime)), Convert.ToDateTime(reader("ModifiedOn"))),
                .ModifiedBy = If(reader("ModifiedBy") Is DBNull.Value, CType(Nothing, Nullable(Of Integer)), Convert.ToInt32(reader("ModifiedBy")))
            }
        End Function
    End Class
End Namespace
