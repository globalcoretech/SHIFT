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
    ''' Data Access Repository for Client Discussion records conforming strictly to DiscussionEntity properties.
    ''' </summary>
    Public Class DiscussionRepository
        Implements IDiscussionRepository

        Private ReadOnly _sqlHelper As ISqlHelper

        Public Sub New(sqlHelper As ISqlHelper)
            _sqlHelper = sqlHelper
        End Sub

        Public Async Function GetByIdAsync(discussionId As Integer) As Task(Of DiscussionEntity) Implements IDiscussionRepository.GetByIdAsync
            Const query As String = "SELECT DiscussionId, ClientId, TaskId, UserId, ChannelId, OutcomeId, DiscussionNotes, DurationMinutes, DiscussionTimestamp, CreatedOn, CreatedBy " &
                                   "FROM dbo.tbl_Discussions WHERE DiscussionId = @DiscussionId;"
            Dim params As SqlParameter() = {New SqlParameter("@DiscussionId", discussionId)}
            Dim list = Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapDiscussionEntity)
            Return If(list.Count > 0, list(0), Nothing)
        End Function

        Public Async Function GetDiscussionsByClientAsync(clientId As Integer) As Task(Of List(Of DiscussionEntity)) Implements IDiscussionRepository.GetDiscussionsByClientAsync
            Const query As String = "SELECT DiscussionId, ClientId, TaskId, UserId, ChannelId, OutcomeId, DiscussionNotes, DurationMinutes, DiscussionTimestamp, CreatedOn, CreatedBy " &
                                   "FROM dbo.tbl_Discussions WHERE ClientId = @ClientId ORDER BY DiscussionTimestamp DESC;"
            Dim params As SqlParameter() = {New SqlParameter("@ClientId", clientId)}
            Return Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapDiscussionEntity)
        End Function

        Public Async Function GetDiscussionsByTaskAsync(taskId As Integer) As Task(Of List(Of DiscussionEntity)) Implements IDiscussionRepository.GetDiscussionsByTaskAsync
            Const query As String = "SELECT DiscussionId, ClientId, TaskId, UserId, ChannelId, OutcomeId, DiscussionNotes, DurationMinutes, DiscussionTimestamp, CreatedOn, CreatedBy " &
                                   "FROM dbo.tbl_Discussions WHERE TaskId = @TaskId ORDER BY DiscussionTimestamp DESC;"
            Dim params As SqlParameter() = {New SqlParameter("@TaskId", taskId)}
            Return Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapDiscussionEntity)
        End Function

        Public Async Function AddAsync(discussion As DiscussionEntity) As Task(Of Integer) Implements IDiscussionRepository.AddAsync
            Const query As String = "INSERT INTO dbo.tbl_Discussions (ClientId, TaskId, UserId, ChannelId, OutcomeId, DiscussionNotes, DurationMinutes, DiscussionTimestamp, CreatedOn, CreatedBy) " &
                                   "VALUES (@ClientId, @TaskId, @UserId, @ChannelId, @OutcomeId, @DiscussionNotes, @DurationMinutes, @DiscussionTimestamp, GETUTCDATE(), @CreatedBy); " &
                                   "SELECT SCOPE_IDENTITY();"
            Dim params As SqlParameter() = {
                New SqlParameter("@ClientId", discussion.ClientId),
                New SqlParameter("@TaskId", If(discussion.TaskId.HasValue, CObj(discussion.TaskId.Value), DBNull.Value)),
                New SqlParameter("@UserId", discussion.UserId),
                New SqlParameter("@ChannelId", CInt(discussion.Channel)),
                New SqlParameter("@OutcomeId", CInt(discussion.Outcome)),
                New SqlParameter("@DiscussionNotes", discussion.DiscussionNotes),
                New SqlParameter("@DurationMinutes", discussion.DurationMinutes),
                New SqlParameter("@DiscussionTimestamp", discussion.DiscussionTimestamp),
                New SqlParameter("@CreatedBy", discussion.CreatedBy)
            }
            Return Await _sqlHelper.ExecuteScalarAsync(Of Integer)(query, params)
        End Function

        Private Function MapDiscussionEntity(reader As IDataReader) As DiscussionEntity
            Return New DiscussionEntity() With {
                .DiscussionId = Convert.ToInt32(reader("DiscussionId")),
                .ClientId = Convert.ToInt32(reader("ClientId")),
                .TaskId = If(reader("TaskId") Is DBNull.Value, CType(Nothing, Nullable(Of Integer)), Convert.ToInt32(reader("TaskId"))),
                .UserId = Convert.ToInt32(reader("UserId")),
                .Channel = CType(Convert.ToInt32(reader("ChannelId")), CommunicationType),
                .Outcome = CType(Convert.ToInt32(reader("OutcomeId")), CommunicationOutcome),
                .DiscussionNotes = Convert.ToString(reader("DiscussionNotes")),
                .DurationMinutes = Convert.ToInt32(reader("DurationMinutes")),
                .DiscussionTimestamp = Convert.ToDateTime(reader("DiscussionTimestamp")),
                .CreatedOn = Convert.ToDateTime(reader("CreatedOn")),
                .CreatedBy = Convert.ToInt32(reader("CreatedBy"))
            }
        End Function
    End Class
End Namespace
