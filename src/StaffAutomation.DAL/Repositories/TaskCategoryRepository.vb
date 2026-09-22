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
    ''' Data Access Repository for Task Categories conforming to dbo.tbl_TaskCategories
    ''' </summary>
    Public Class TaskCategoryRepository
        Implements ITaskCategoryRepository

        Private ReadOnly _sqlHelper As ISqlHelper

        Public Sub New(sqlHelper As ISqlHelper)
            _sqlHelper = sqlHelper
        End Sub

        Public Async Function GetAllCategoriesAsync(Optional includeInactive As Boolean = False) As Task(Of IEnumerable(Of TaskCategoryEntity)) Implements ITaskCategoryRepository.GetAllCategoriesAsync
            Dim query As String = "SELECT CategoryId, CategoryCode, CategoryName, IsActive, CreatedOn, CreatedBy FROM dbo.tbl_TaskCategories "
            If Not includeInactive Then
                query &= "WHERE IsActive = 1 "
            End If
            query &= "ORDER BY CategoryName ASC;"

            Dim mapFunc As Func(Of IDataReader, TaskCategoryEntity) = Function(r)
                Return New TaskCategoryEntity() With {
                    .CategoryId = Convert.ToInt32(r("CategoryId")),
                    .CategoryCode = If(r("CategoryCode") Is DBNull.Value, String.Empty, Convert.ToString(r("CategoryCode"))),
                    .CategoryName = If(r("CategoryName") Is DBNull.Value, String.Empty, Convert.ToString(r("CategoryName"))),
                    .IsActive = If(r("IsActive") Is DBNull.Value, False, Convert.ToBoolean(r("IsActive"))),
                    .CreatedOn = If(r("CreatedOn") Is DBNull.Value, DateTime.MinValue, Convert.ToDateTime(r("CreatedOn"))),
                    .CreatedBy = If(r("CreatedBy") Is DBNull.Value, 0, Convert.ToInt32(r("CreatedBy")))
                }
            End Function

            Return Await _sqlHelper.ExecuteReaderAsync(query, Nothing, mapFunc)
        End Function

        Public Async Function AddCategoryAsync(entity As TaskCategoryEntity) As Task(Of Integer) Implements ITaskCategoryRepository.AddCategoryAsync
            Const query As String = "INSERT INTO dbo.tbl_TaskCategories (CategoryCode, CategoryName, IsActive, CreatedOn, CreatedBy) " &
                                    "VALUES (@CategoryCode, @CategoryName, @IsActive, GETUTCDATE(), @CreatedBy); " &
                                    "SELECT SCOPE_IDENTITY();"
            
            Dim params As SqlParameter() = {
                New SqlParameter("@CategoryCode", If(String.IsNullOrEmpty(entity.CategoryCode), CObj(DBNull.Value), entity.CategoryCode)),
                New SqlParameter("@CategoryName", entity.CategoryName),
                New SqlParameter("@IsActive", entity.IsActive),
                New SqlParameter("@CreatedBy", entity.CreatedBy)
            }

            Dim result = Await _sqlHelper.ExecuteScalarAsync(Of Object)(query, params)
            If result IsNot Nothing AndAlso result IsNot DBNull.Value Then
                Return Convert.ToInt32(result)
            End If
            Return 0
        End Function

        Public Async Function UpdateCategoryAsync(entity As TaskCategoryEntity) As Task(Of Boolean) Implements ITaskCategoryRepository.UpdateCategoryAsync
            Const query As String = "UPDATE dbo.tbl_TaskCategories SET CategoryCode = @CategoryCode, CategoryName = @CategoryName, " &
                                    "IsActive = @IsActive WHERE CategoryId = @CategoryId;"
            
            Dim params As SqlParameter() = {
                New SqlParameter("@CategoryCode", If(String.IsNullOrEmpty(entity.CategoryCode), CObj(DBNull.Value), entity.CategoryCode)),
                New SqlParameter("@CategoryName", entity.CategoryName),
                New SqlParameter("@IsActive", entity.IsActive),
                New SqlParameter("@CategoryId", entity.CategoryId)
            }

            Dim rows = Await _sqlHelper.ExecuteNonQueryAsync(query, params)
            Return rows > 0
        End Function

        Public Async Function ToggleCategoryStatusAsync(categoryId As Integer, isActive As Boolean, modifiedByUserId As Integer) As Task(Of Boolean) Implements ITaskCategoryRepository.ToggleCategoryStatusAsync
            Const query As String = "UPDATE dbo.tbl_TaskCategories SET IsActive = @IsActive WHERE CategoryId = @CategoryId;"
            Dim params As SqlParameter() = {
                New SqlParameter("@IsActive", isActive),
                New SqlParameter("@CategoryId", categoryId)
            }
            Dim rows = Await _sqlHelper.ExecuteNonQueryAsync(query, params)
            Return rows > 0
        End Function
    End Class
End Namespace
