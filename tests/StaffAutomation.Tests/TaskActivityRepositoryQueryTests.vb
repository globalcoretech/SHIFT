Imports System
Imports System.Reflection
Imports Xunit
Imports StaffAutomation.DAL.Repositories

Namespace StaffAutomation.Tests
    ''' <summary>
    ''' Query structure & schema alignment unit tests verifying that TaskActivityRepository 
    ''' targets TimeCategoryId for SQL Server table columns and aliases TimeCategoryId AS CategoryId.
    ''' </summary>
    Public Class TaskActivityRepositoryQueryTests
        <Fact>
        Public Sub TaskActivityRepository_ReflectsTimeCategoryIdInSqlQueries()
            ' Inspect private constant query strings in TaskActivityRepository
            Dim repoType = GetType(TaskActivityRepository)
            Dim fields = repoType.GetFields(BindingFlags.NonPublic Or BindingFlags.Instance Or BindingFlags.Static)

            ' Verify repository methods do not attempt to INSERT into un-aliased CategoryId column on dbo.tbl_TaskActivities
            Dim repoSourceCode = System.IO.File.ReadAllText("e:\Staff automation\src\StaffAutomation.DAL\Repositories\TaskActivityRepository.vb")

            ' 1. Verify INSERT statements use TimeCategoryId
            Assert.Contains("INSERT INTO dbo.tbl_TaskActivities (TaskId, UserId, TimeCategoryId", repoSourceCode)
            Assert.DoesNotContain("INSERT INTO dbo.tbl_TaskActivities (TaskId, UserId, CategoryId", repoSourceCode)

            ' 2. Verify SELECT statements alias TimeCategoryId AS CategoryId
            Assert.Contains("TimeCategoryId AS CategoryId", repoSourceCode)
            Assert.DoesNotContain("SELECT ActivityId, TaskId, UserId, CategoryId,", repoSourceCode)

            ' 3. Verify parameter creation uses @TimeCategoryId
            Assert.Contains("New SqlParameter(""@TimeCategoryId""", repoSourceCode)
            Assert.DoesNotContain("New SqlParameter(""@CategoryId""", repoSourceCode)
        End Sub
    End Class
End Namespace
