Imports System
Imports System.IO
Imports Xunit

Namespace StaffAutomation.Tests
    ''' <summary>
    ''' Query integrity unit tests verifying that DiscussionRepository 
    ''' targets CommunicationTypeId for SQL Server table columns and parameters.
    ''' </summary>
    Public Class DiscussionRepositoryQueryTests
        <Fact>
        Public Sub DiscussionRepository_ReflectsCommunicationTypeIdInSqlQueries()
            Dim repoSourceCode = File.ReadAllText("e:\Staff automation\src\StaffAutomation.DAL\Repositories\DiscussionRepository.vb")

            ' 1. Verify INSERT statements use CommunicationTypeId
            Assert.Contains("INSERT INTO dbo.tbl_Discussions (ClientId, TaskId, UserId, CommunicationTypeId", repoSourceCode)
            Assert.DoesNotContain("INSERT INTO dbo.tbl_Discussions (ClientId, TaskId, UserId, ChannelId", repoSourceCode)

            ' 2. Verify SELECT statements alias CommunicationTypeId AS ChannelId
            Assert.Contains("CommunicationTypeId AS ChannelId", repoSourceCode)
            Assert.DoesNotContain("SELECT DiscussionId, ClientId, TaskId, UserId, ChannelId,", repoSourceCode)

            ' 3. Verify parameter creation uses @CommunicationTypeId
            Assert.Contains("New SqlParameter(""@CommunicationTypeId""", repoSourceCode)
            Assert.DoesNotContain("New SqlParameter(""@ChannelId""", repoSourceCode)
        End Sub
    End Class
End Namespace
