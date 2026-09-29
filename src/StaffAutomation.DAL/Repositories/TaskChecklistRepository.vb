Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports Microsoft.Data.SqlClient
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.DAL.Core
Imports StaffAutomation.DAL.Interfaces

Namespace Repositories
    ''' <summary>
    ''' Implementation of data access for Task Checklists.
    ''' </summary>
    Public Class TaskChecklistRepository
        Implements ITaskChecklistRepository

        Private ReadOnly _sqlHelper As ISqlHelper

        Public Sub New(sqlHelper As ISqlHelper)
            If sqlHelper Is Nothing Then Throw New ArgumentNullException(NameOf(sqlHelper))
            _sqlHelper = sqlHelper
        End Sub

        Private Function MapChecklistItem(reader As IDataReader) As TaskChecklistItemEntity
            Return New TaskChecklistItemEntity With {
                .ChecklistId = Convert.ToInt32(reader("ChecklistId")),
                .TaskId = Convert.ToInt32(reader("TaskId")),
                .ItemDescription = reader("ItemDescription").ToString(),
                .RequiresProof = Convert.ToBoolean(reader("RequiresProof")),
                .IsCompleted = Convert.ToBoolean(reader("IsCompleted")),
                .CompletedByUserId = If(IsDBNull(reader("CompletedByUserId")), CType(Nothing, Integer?), Convert.ToInt32(reader("CompletedByUserId"))),
                .CompletedOn = If(IsDBNull(reader("CompletedOn")), CType(Nothing, DateTime?), Convert.ToDateTime(reader("CompletedOn"))),
                .Remarks = If(IsDBNull(reader("Remarks")), Nothing, reader("Remarks").ToString()),
                .AttachmentPath = If(IsDBNull(reader("AttachmentPath")), Nothing, reader("AttachmentPath").ToString()),
                .SortOrder = Convert.ToInt32(reader("SortOrder")),
                .CreatedOn = Convert.ToDateTime(reader("CreatedOn"))
            }
        End Function

        Public Async Function GetChecklistItemByIdAsync(checklistId As Integer) As Task(Of TaskChecklistItemEntity) Implements ITaskChecklistRepository.GetChecklistItemByIdAsync
            Dim sql = "SELECT ChecklistId, TaskId, ItemDescription, RequiresProof, IsCompleted, CompletedByUserId, CompletedOn, Remarks, AttachmentPath, SortOrder, CreatedOn " &
                      "FROM tbl_TaskChecklistItems WHERE ChecklistId = @ChecklistId"
            
            Dim parameters As SqlParameter() = {
                New SqlParameter("@ChecklistId", checklistId)
            }
            
            Dim results = Await _sqlHelper.ExecuteReaderAsync(sql, parameters, AddressOf MapChecklistItem)
            If results IsNot Nothing AndAlso results.Count > 0 Then Return results(0)
            Return Nothing
        End Function

        Public Async Function GetChecklistForTaskAsync(taskId As Integer) As Task(Of List(Of TaskChecklistItemEntity)) Implements ITaskChecklistRepository.GetChecklistForTaskAsync
            Dim sql = "SELECT ChecklistId, TaskId, ItemDescription, RequiresProof, IsCompleted, CompletedByUserId, CompletedOn, Remarks, AttachmentPath, SortOrder, CreatedOn " &
                      "FROM tbl_TaskChecklistItems WHERE TaskId = @TaskId ORDER BY SortOrder ASC, ChecklistId ASC"
            
            Dim parameters As SqlParameter() = {
                New SqlParameter("@TaskId", taskId)
            }
            
            Return Await _sqlHelper.ExecuteReaderAsync(sql, parameters, AddressOf MapChecklistItem)
        End Function

        Public Async Function AddChecklistItemsAsync(items As List(Of TaskChecklistItemEntity)) As Task Implements ITaskChecklistRepository.AddChecklistItemsAsync
            If items Is Nothing OrElse items.Count = 0 Then Return

            Dim sql = "INSERT INTO tbl_TaskChecklistItems (TaskId, ItemDescription, RequiresProof, IsCompleted, SortOrder) " &
                      "VALUES (@TaskId, @ItemDescription, @RequiresProof, @IsCompleted, @SortOrder)"
                      
            For Each item In items
                Dim parameters As SqlParameter() = {
                    New SqlParameter("@TaskId", item.TaskId),
                    New SqlParameter("@ItemDescription", item.ItemDescription),
                    New SqlParameter("@RequiresProof", item.RequiresProof),
                    New SqlParameter("@IsCompleted", item.IsCompleted),
                    New SqlParameter("@SortOrder", item.SortOrder)
                }
                Await _sqlHelper.ExecuteNonQueryAsync(sql, parameters)
            Next
        End Function

        Public Async Function UpdateChecklistItemAsync(item As TaskChecklistItemEntity) As Task Implements ITaskChecklistRepository.UpdateChecklistItemAsync
            Dim sql = "UPDATE tbl_TaskChecklistItems SET " &
                      "ItemDescription = @ItemDescription, RequiresProof = @RequiresProof, IsCompleted = @IsCompleted, " &
                      "CompletedByUserId = @CompletedByUserId, CompletedOn = @CompletedOn, " &
                      "Remarks = @Remarks, AttachmentPath = @AttachmentPath, SortOrder = @SortOrder " &
                      "WHERE ChecklistId = @ChecklistId"
                      
            Dim parameters As SqlParameter() = {
                New SqlParameter("@ChecklistId", item.ChecklistId),
                New SqlParameter("@ItemDescription", item.ItemDescription),
                New SqlParameter("@RequiresProof", item.RequiresProof),
                New SqlParameter("@IsCompleted", item.IsCompleted),
                New SqlParameter("@CompletedByUserId", If(item.CompletedByUserId.HasValue, CObj(item.CompletedByUserId.Value), CObj(DBNull.Value))),
                New SqlParameter("@CompletedOn", If(item.CompletedOn.HasValue, CObj(item.CompletedOn.Value), CObj(DBNull.Value))),
                New SqlParameter("@Remarks", If(String.IsNullOrEmpty(item.Remarks), CObj(DBNull.Value), CObj(item.Remarks))),
                New SqlParameter("@AttachmentPath", If(String.IsNullOrEmpty(item.AttachmentPath), CObj(DBNull.Value), CObj(item.AttachmentPath))),
                New SqlParameter("@SortOrder", item.SortOrder)
            }
            
            Await _sqlHelper.ExecuteNonQueryAsync(sql, parameters)
        End Function

        Public Async Function DeleteChecklistItemsForTaskAsync(taskId As Integer) As Task Implements ITaskChecklistRepository.DeleteChecklistItemsForTaskAsync
            Dim sql = "DELETE FROM tbl_TaskChecklistItems WHERE TaskId = @TaskId"
            Dim parameters As SqlParameter() = {
                New SqlParameter("@TaskId", taskId)
            }
            Await _sqlHelper.ExecuteNonQueryAsync(sql, parameters)
        End Function
    End Class
End Namespace
