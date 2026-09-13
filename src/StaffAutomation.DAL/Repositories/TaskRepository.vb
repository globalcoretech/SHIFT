Imports System
Imports System.Collections.Generic
Imports System.Data
Imports Microsoft.Data.SqlClient
Imports System.Threading.Tasks
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.DAL.Interfaces

Namespace Repositories
    ''' <summary>
    ''' Data Access Repository for Task transactional entity conforming strictly to TaskEntity properties and dbo.tbl_Tasks SQL schema.
    ''' </summary>
    Public Class TaskRepository
        Implements ITaskRepository

        Private ReadOnly _sqlHelper As ISqlHelper

        Public Sub New(sqlHelper As ISqlHelper)
            _sqlHelper = sqlHelper
        End Sub

        Public Async Function GetByIdAsync(taskId As Integer) As Task(Of TaskEntity) Implements ITaskRepository.GetByIdAsync
            Const query As String = "SELECT TaskId, TaskCode, Title, Description, ClientId, CategoryId, FinancialYearId, AssignedToUserId, AssignedByUserId, DepartmentId, PriorityId, StatusId, " &
                                   "AssignmentDate, TargetDueDate, ReminderDate, CompletionDate, IsDeleted, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy " &
                                   "FROM dbo.tbl_Tasks WHERE TaskId = @TaskId AND IsDeleted = 0;"
            Dim params As SqlParameter() = {New SqlParameter("@TaskId", taskId)}
            Dim list = Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapTaskEntity)
            Return If(list.Count > 0, list(0), Nothing)
        End Function

        Public Async Function GetByTaskCodeAsync(taskCode As String) As Task(Of TaskEntity) Implements ITaskRepository.GetByTaskCodeAsync
            Const query As String = "SELECT TaskId, TaskCode, Title, Description, ClientId, CategoryId, FinancialYearId, AssignedToUserId, AssignedByUserId, DepartmentId, PriorityId, StatusId, " &
                                   "AssignmentDate, TargetDueDate, ReminderDate, CompletionDate, IsDeleted, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy " &
                                   "FROM dbo.tbl_Tasks WHERE TaskCode = @TaskCode AND IsDeleted = 0;"
            Dim params As SqlParameter() = {New SqlParameter("@TaskCode", taskCode)}
            Dim list = Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapTaskEntity)
            Return If(list.Count > 0, list(0), Nothing)
        End Function

        Public Async Function GetTasksByAssigneeAsync(userId As Integer, Optional state As Nullable(Of TaskWorkflowState) = Nothing) As Task(Of List(Of TaskEntity)) Implements ITaskRepository.GetTasksByAssigneeAsync
            Dim query As String = "SELECT TaskId, TaskCode, Title, Description, ClientId, CategoryId, FinancialYearId, AssignedToUserId, AssignedByUserId, DepartmentId, PriorityId, StatusId, " &
                                  "AssignmentDate, TargetDueDate, ReminderDate, CompletionDate, IsDeleted, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy " &
                                  "FROM dbo.tbl_Tasks WHERE AssignedToUserId = @AssignedToUserId AND IsDeleted = 0 "
            Dim params As New List(Of SqlParameter)()
            params.Add(New SqlParameter("@AssignedToUserId", userId))

            If state.HasValue Then
                Dim statusId = Await GetStatusIdByStateAsync(state.Value)
                query &= "AND StatusId = @StatusId "
                params.Add(New SqlParameter("@StatusId", statusId))
            End If

            query &= "ORDER BY TargetDueDate ASC, TaskId DESC;"
            Return Await _sqlHelper.ExecuteReaderAsync(query, params.ToArray(), AddressOf MapTaskEntity)
        End Function

        Public Async Function GetTasksByClientAsync(clientId As Integer) As Task(Of List(Of TaskEntity)) Implements ITaskRepository.GetTasksByClientAsync
            Const query As String = "SELECT TaskId, TaskCode, Title, Description, ClientId, CategoryId, FinancialYearId, AssignedToUserId, AssignedByUserId, DepartmentId, PriorityId, StatusId, " &
                                   "AssignmentDate, TargetDueDate, ReminderDate, CompletionDate, IsDeleted, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy " &
                                   "FROM dbo.tbl_Tasks WHERE ClientId = @ClientId AND IsDeleted = 0 ORDER BY TargetDueDate ASC, TaskId DESC;"
            Dim params As SqlParameter() = {New SqlParameter("@ClientId", clientId)}
            Return Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapTaskEntity)
        End Function

        Public Async Function GetAllActiveTasksAsync() As Task(Of List(Of TaskEntity)) Implements ITaskRepository.GetAllActiveTasksAsync
            Const query As String = "SELECT TaskId, TaskCode, Title, Description, ClientId, CategoryId, FinancialYearId, AssignedToUserId, AssignedByUserId, DepartmentId, PriorityId, StatusId, " &
                                   "AssignmentDate, TargetDueDate, ReminderDate, CompletionDate, IsDeleted, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy " &
                                   "FROM dbo.tbl_Tasks WHERE IsDeleted = 0 AND StatusId NOT IN (10, 11) ORDER BY TargetDueDate ASC, TaskId DESC;"
            Return Await _sqlHelper.ExecuteReaderAsync(query, Nothing, AddressOf MapTaskEntity)
        End Function

        Public Async Function GetAllTasksAsync(Optional includeDeleted As Boolean = False) As Task(Of List(Of TaskEntity)) Implements ITaskRepository.GetAllTasksAsync
            Dim query As String = "SELECT TaskId, TaskCode, Title, Description, ClientId, CategoryId, FinancialYearId, AssignedToUserId, AssignedByUserId, DepartmentId, PriorityId, StatusId, " &
                                  "AssignmentDate, TargetDueDate, ReminderDate, CompletionDate, IsDeleted, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy " &
                                  "FROM dbo.tbl_Tasks "
            If Not includeDeleted Then
                query &= "WHERE IsDeleted = 0 "
            End If
            query &= "ORDER BY TargetDueDate ASC, TaskId DESC;"
            Return Await _sqlHelper.ExecuteReaderAsync(query, Nothing, AddressOf MapTaskEntity)
        End Function

        Public Async Function AddAsync(taskItem As TaskEntity) As Task(Of Integer) Implements ITaskRepository.AddAsync
            Const query As String = "INSERT INTO dbo.tbl_Tasks (TaskCode, Title, Description, ClientId, CategoryId, FinancialYearId, AssignedToUserId, AssignedByUserId, DepartmentId, PriorityId, StatusId, " &
                                   "AssignmentDate, TargetDueDate, ReminderDate, CompletionDate, IsDeleted, CreatedOn, CreatedBy) " &
                                   "VALUES (@TaskCode, @Title, @Description, @ClientId, @CategoryId, @FinancialYearId, @AssignedToUserId, @AssignedByUserId, @DepartmentId, @PriorityId, @StatusId, " &
                                   "@AssignmentDate, @TargetDueDate, @ReminderDate, @CompletionDate, 0, GETUTCDATE(), @CreatedBy); " &
                                   "SELECT SCOPE_IDENTITY();"
            Dim params As SqlParameter() = {
                New SqlParameter("@TaskCode", taskItem.TaskCode),
                New SqlParameter("@Title", taskItem.Title),
                New SqlParameter("@Description", If(Not String.IsNullOrEmpty(taskItem.Description), taskItem.Description, CObj(DBNull.Value))),
                New SqlParameter("@ClientId", taskItem.ClientId),
                New SqlParameter("@CategoryId", taskItem.CategoryId),
                New SqlParameter("@FinancialYearId", taskItem.FinancialYearId),
                New SqlParameter("@AssignedToUserId", taskItem.AssignedToUserId),
                New SqlParameter("@AssignedByUserId", taskItem.AssignedByUserId),
                New SqlParameter("@DepartmentId", CInt(taskItem.Department)),
                New SqlParameter("@PriorityId", CInt(taskItem.Priority)),
                New SqlParameter("@StatusId", CInt(taskItem.WorkflowState)),
                New SqlParameter("@AssignmentDate", taskItem.AssignmentDate),
                New SqlParameter("@TargetDueDate", taskItem.TargetDueDate),
                New SqlParameter("@ReminderDate", If(taskItem.ReminderDate.HasValue, CObj(taskItem.ReminderDate.Value), DBNull.Value)),
                New SqlParameter("@CompletionDate", If(taskItem.CompletionDate.HasValue, CObj(taskItem.CompletionDate.Value), DBNull.Value)),
                New SqlParameter("@CreatedBy", taskItem.CreatedBy)
            }
            Return Await _sqlHelper.ExecuteScalarAsync(Of Integer)(query, params)
        End Function

        Public Async Function GetCategoryIdByCodeAsync(categoryCode As String) As Task(Of Integer) Implements ITaskRepository.GetCategoryIdByCodeAsync
            Dim searchCode = If(Not String.IsNullOrEmpty(categoryCode), categoryCode.Trim(), "")
            Const query As String = "SELECT CategoryId FROM dbo.tbl_TaskCategories WHERE CategoryCode = @CategoryCode AND IsActive = 1;"
            Dim params As SqlParameter() = {New SqlParameter("@CategoryCode", searchCode)}

            Dim mapFunc As Func(Of IDataReader, Integer) = Function(r) Convert.ToInt32(r("CategoryId"))
            Dim catIds = Await _sqlHelper.ExecuteReaderAsync(query, params, mapFunc)

            If catIds.Count > 1 Then
                Throw New BusinessException($"Duplicate active Task Categories detected in database for CategoryCode '{searchCode}'. Task creation aborted.", "ERR_DUPLICATE_CATEGORY")
            ElseIf catIds.Count = 1 Then
                Return catIds(0)
            End If

            ' Fallback alias lookup if exact code match was not found (e.g. TDS_RETURN -> ITR_FILING)
            Const fallbackQuery As String = "SELECT TOP 1 CategoryId FROM dbo.tbl_TaskCategories WHERE (CategoryCode = 'ITR_FILING' OR CategoryName LIKE '%TDS%' OR CategoryName LIKE '%Return%') AND IsActive = 1 ORDER BY CategoryId ASC;"
            Dim fbRes = Await _sqlHelper.ExecuteScalarAsync(Of Object)(fallbackQuery, Nothing)
            If fbRes IsNot Nothing AndAlso fbRes IsNot DBNull.Value Then
                Return Convert.ToInt32(fbRes)
            End If

            ' Final active category fallback
            Const firstActiveQuery As String = "SELECT TOP 1 CategoryId FROM dbo.tbl_TaskCategories WHERE IsActive = 1 ORDER BY CategoryId ASC;"
            Dim faRes = Await _sqlHelper.ExecuteScalarAsync(Of Object)(firstActiveQuery, Nothing)
            If faRes IsNot Nothing AndAlso faRes IsNot DBNull.Value Then
                Return Convert.ToInt32(faRes)
            End If

            Return 0
        End Function

        Public Async Function GetActiveFinancialYearIdAsync() As Task(Of Integer) Implements ITaskRepository.GetActiveFinancialYearIdAsync
            ' Step 1: Resolve records with IsCurrentFY = 1
            Const query As String = "SELECT FinancialYearId FROM dbo.tbl_FinancialYears WHERE IsCurrentFY = 1;"
            Dim mapFunc As Func(Of IDataReader, Integer) = Function(r) Convert.ToInt32(r("FinancialYearId"))
            Dim fyIds = Await _sqlHelper.ExecuteReaderAsync(query, Nothing, mapFunc)

            If fyIds.Count > 1 Then
                Throw New BusinessException("Multiple active Financial Years configured in database with IsCurrentFY = 1. Please ensure only one Financial Year is marked current.", "ERR_MULTIPLE_CURRENT_FY")
            ElseIf fyIds.Count = 1 Then
                Return fyIds(0)
            End If

            ' Step 2: Fallback to current business date range match (StartDate <= GETUTCDATE() AND EndDate >= GETUTCDATE())
            Const fallbackQuery As String = "SELECT FinancialYearId FROM dbo.tbl_FinancialYears WHERE CAST(GETUTCDATE() AS DATE) BETWEEN StartDate AND EndDate;"
            Dim fbIds = Await _sqlHelper.ExecuteReaderAsync(fallbackQuery, Nothing, mapFunc)
            If fbIds.Count > 1 Then
                Throw New BusinessException("Multiple Financial Years match current business date range. Please mark one Financial Year as current.", "ERR_MULTIPLE_MATCHING_FY")
            ElseIf fbIds.Count = 1 Then
                Return fbIds(0)
            End If

            ' Step 3: Return 0 if no matching Financial Year exists (let TaskManagementService throw explicit BusinessException)
            Return 0
        End Function

        Public Async Function GetStatusIdByStateAsync(state As TaskWorkflowState) As Task(Of Integer) Implements ITaskRepository.GetStatusIdByStateAsync
            Dim statusCodeStr = state.ToString()
            Const query As String = "SELECT StatusId FROM dbo.tbl_TaskStatuses WHERE StatusCode = @StatusCode;"
            Dim params As SqlParameter() = {New SqlParameter("@StatusCode", statusCodeStr)}
            Dim res = Await _sqlHelper.ExecuteScalarAsync(Of Object)(query, params)
            If res IsNot Nothing AndAlso res IsNot DBNull.Value Then
                Return Convert.ToInt32(res)
            End If

            ' Fallback by numeric ID match
            Const fallbackQuery As String = "SELECT StatusId FROM dbo.tbl_TaskStatuses WHERE StatusId = @StatusId;"
            Dim fbParams As SqlParameter() = {New SqlParameter("@StatusId", CInt(state))}
            Dim fbRes = Await _sqlHelper.ExecuteScalarAsync(Of Object)(fallbackQuery, fbParams)
            If fbRes IsNot Nothing AndAlso fbRes IsNot DBNull.Value Then
                Return Convert.ToInt32(fbRes)
            End If

            Throw New BusinessException($"Task Workflow Status '{statusCodeStr}' is not configured in dbo.tbl_TaskStatuses master table. Task creation aborted.", "ERR_INVALID_STATUS")
        End Function

        Public Async Function GetPriorityIdByEnumAsync(priority As TaskPriority) As Task(Of Integer) Implements ITaskRepository.GetPriorityIdByEnumAsync
            Dim codeStr = priority.ToString().ToUpperInvariant()
            Const query As String = "SELECT PriorityId FROM dbo.tbl_TaskPriorities WHERE PriorityCode = @PriorityCode OR PriorityName = @PriorityName;"
            Dim params As SqlParameter() = {
                New SqlParameter("@PriorityCode", codeStr),
                New SqlParameter("@PriorityName", priority.ToString())
            }
            Dim res = Await _sqlHelper.ExecuteScalarAsync(Of Object)(query, params)
            If res IsNot Nothing AndAlso res IsNot DBNull.Value Then
                Return Convert.ToInt32(res)
            End If

            ' Fallback by numeric rank/ID
            Const fallbackQuery As String = "SELECT PriorityId FROM dbo.tbl_TaskPriorities WHERE PriorityId = @PriorityId;"
            Dim fbParams As SqlParameter() = {New SqlParameter("@PriorityId", CInt(priority))}
            Dim fbRes = Await _sqlHelper.ExecuteScalarAsync(Of Object)(fallbackQuery, fbParams)
            If fbRes IsNot Nothing AndAlso fbRes IsNot DBNull.Value Then
                Return Convert.ToInt32(fbRes)
            End If

            Throw New BusinessException($"Task Priority '{priority}' is not configured in dbo.tbl_TaskPriorities master table. Task creation aborted.", "ERR_INVALID_PRIORITY")
        End Function

        Public Async Function GetDepartmentIdByEnumAsync(dept As DepartmentType) As Task(Of Integer) Implements ITaskRepository.GetDepartmentIdByEnumAsync
            Dim deptCodeStr As String = "TAX"
            Select Case dept
                Case DepartmentType.Accounting
                    deptCodeStr = "ACCT"
                Case DepartmentType.Administration
                    deptCodeStr = "ADMIN"
                Case Else
                    deptCodeStr = "TAX"
            End Select

            Const query As String = "SELECT DepartmentId FROM dbo.tbl_Departments WHERE DepartmentCode = @DeptCode;"
            Dim params As SqlParameter() = {New SqlParameter("@DeptCode", deptCodeStr)}
            Dim res = Await _sqlHelper.ExecuteScalarAsync(Of Object)(query, params)
            If res IsNot Nothing AndAlso res IsNot DBNull.Value Then
                Return Convert.ToInt32(res)
            End If

            ' Fallback by numeric ID
            Const fallbackQuery As String = "SELECT DepartmentId FROM dbo.tbl_Departments WHERE DepartmentId = @DeptId;"
            Dim fbParams As SqlParameter() = {New SqlParameter("@DeptId", CInt(dept))}
            Dim fbRes = Await _sqlHelper.ExecuteScalarAsync(Of Object)(fallbackQuery, fbParams)
            If fbRes IsNot Nothing AndAlso fbRes IsNot DBNull.Value Then
                Return Convert.ToInt32(fbRes)
            End If

            Throw New BusinessException($"Department '{dept}' ({deptCodeStr}) is not configured in dbo.tbl_Departments master table. Task creation aborted.", "ERR_INVALID_DEPARTMENT")
        End Function

        Public Async Function GetCategoryInfoMapAsync() As Task(Of Dictionary(Of Integer, Tuple(Of String, String))) Implements ITaskRepository.GetCategoryInfoMapAsync
            Const query As String = "SELECT CategoryId, CategoryCode, CategoryName FROM dbo.tbl_TaskCategories WHERE IsActive = 1;"
            Dim mapFunc As Func(Of IDataReader, Tuple(Of Integer, String, String)) = Function(r)
                Dim id As Integer = Convert.ToInt32(r("CategoryId"))
                Dim code As String = If(r("CategoryCode") IsNot DBNull.Value, r("CategoryCode").ToString(), "")
                Dim name As String = If(r("CategoryName") IsNot DBNull.Value, r("CategoryName").ToString(), "")
                Return New Tuple(Of Integer, String, String)(id, code, name)
            End Function

            Dim list = Await _sqlHelper.ExecuteReaderAsync(query, Nothing, mapFunc)
            Dim dict As New Dictionary(Of Integer, Tuple(Of String, String))()
            For Each item In list
                dict(item.Item1) = New Tuple(Of String, String)(item.Item2, item.Item3)
            Next
            Return dict
        End Function

        Public Async Function UpdateTaskAsync(taskItem As TaskEntity) As Task(Of Boolean) Implements ITaskRepository.UpdateTaskAsync
            Dim deptId = Await GetDepartmentIdByEnumAsync(taskItem.Department)
            Dim priorityId = Await GetPriorityIdByEnumAsync(taskItem.Priority)
            Dim statusId = Await GetStatusIdByStateAsync(taskItem.WorkflowState)

            Const query As String = "UPDATE dbo.tbl_Tasks SET Title = @Title, Description = @Description, ClientId = @ClientId, CategoryId = @CategoryId, FinancialYearId = @FinancialYearId, AssignedToUserId = @AssignedToUserId, " &
                                   "AssignedByUserId = @AssignedByUserId, DepartmentId = @DepartmentId, PriorityId = @PriorityId, StatusId = @StatusId, " &
                                   "TargetDueDate = @TargetDueDate, ReminderDate = @ReminderDate, CompletionDate = @CompletionDate, " &
                                   "ModifiedOn = GETUTCDATE(), ModifiedBy = @ModifiedBy WHERE TaskId = @TaskId AND IsDeleted = 0;"
            Dim params As SqlParameter() = {
                New SqlParameter("@Title", taskItem.Title),
                New SqlParameter("@Description", If(Not String.IsNullOrEmpty(taskItem.Description), taskItem.Description, CObj(DBNull.Value))),
                New SqlParameter("@ClientId", taskItem.ClientId),
                New SqlParameter("@CategoryId", taskItem.CategoryId),
                New SqlParameter("@FinancialYearId", taskItem.FinancialYearId),
                New SqlParameter("@AssignedToUserId", taskItem.AssignedToUserId),
                New SqlParameter("@AssignedByUserId", taskItem.AssignedByUserId),
                New SqlParameter("@DepartmentId", deptId),
                New SqlParameter("@PriorityId", priorityId),
                New SqlParameter("@StatusId", statusId),
                New SqlParameter("@TargetDueDate", taskItem.TargetDueDate),
                New SqlParameter("@ReminderDate", If(taskItem.ReminderDate.HasValue, CObj(taskItem.ReminderDate.Value), DBNull.Value)),
                New SqlParameter("@CompletionDate", If(taskItem.CompletionDate.HasValue, CObj(taskItem.CompletionDate.Value), DBNull.Value)),
                New SqlParameter("@ModifiedBy", If(taskItem.ModifiedBy.HasValue, CObj(taskItem.ModifiedBy.Value), DBNull.Value)),
                New SqlParameter("@TaskId", taskItem.TaskId)
            }
            Dim rows = Await _sqlHelper.ExecuteNonQueryAsync(query, params)
            Return rows > 0
        End Function

        Public Async Function UpdateTaskAsync(updateDto As UpdateTaskDto, categoryId As Integer, departmentId As Integer, modifiedBy As Integer, Optional transaction As IDbTransaction = Nothing) As Task(Of Boolean) Implements ITaskRepository.UpdateTaskAsync
            Dim priorityId = Await GetPriorityIdByEnumAsync(updateDto.Priority)
            Const query As String = "UPDATE dbo.tbl_Tasks SET Title = @Title, Description = @Description, ClientId = @ClientId, CategoryId = @CategoryId, " &
                                   "AssignedToUserId = @AssignedToUserId, DepartmentId = @DepartmentId, PriorityId = @PriorityId, " &
                                   "TargetDueDate = @TargetDueDate, ReminderDate = @ReminderDate, " &
                                   "ModifiedOn = GETUTCDATE(), ModifiedBy = @ModifiedBy WHERE TaskId = @TaskId AND IsDeleted = 0 " &
                                   "AND ((@OriginalModifiedOn IS NULL AND ModifiedOn IS NULL) OR (ModifiedOn IS NOT NULL AND @OriginalModifiedOn IS NOT NULL AND ABS(DATEDIFF_BIG(ms, ModifiedOn, @OriginalModifiedOn)) <= 10));"

            Dim pOrigMod As New SqlParameter("@OriginalModifiedOn", SqlDbType.DateTime2)
            If updateDto.OriginalModifiedOn.HasValue Then
                pOrigMod.Value = updateDto.OriginalModifiedOn.Value
            Else
                pOrigMod.Value = DBNull.Value
            End If

            Dim params As SqlParameter() = {
                New SqlParameter("@Title", updateDto.Title),
                New SqlParameter("@Description", If(Not String.IsNullOrEmpty(updateDto.Description), updateDto.Description, CObj(DBNull.Value))),
                New SqlParameter("@ClientId", updateDto.ClientId),
                New SqlParameter("@CategoryId", categoryId),
                New SqlParameter("@AssignedToUserId", updateDto.AssignedToUserId),
                New SqlParameter("@DepartmentId", departmentId),
                New SqlParameter("@PriorityId", priorityId),
                New SqlParameter("@TargetDueDate", updateDto.TargetDueDate),
                New SqlParameter("@ReminderDate", If(updateDto.ReminderDate.HasValue, CObj(updateDto.ReminderDate.Value), DBNull.Value)),
                New SqlParameter("@ModifiedBy", modifiedBy),
                New SqlParameter("@TaskId", updateDto.TaskId),
                pOrigMod
            }
            Dim rows = Await _sqlHelper.ExecuteNonQueryAsync(query, params, transaction)
            Return rows > 0
        End Function

        Public Async Function UpdateStateAsync(taskId As Integer, newState As TaskWorkflowState, modifiedBy As Integer) As Task(Of Boolean) Implements ITaskRepository.UpdateStateAsync
            Dim statusId = Await GetStatusIdByStateAsync(newState)
            Const query As String = "UPDATE dbo.tbl_Tasks SET StatusId = @StatusId, ModifiedOn = GETUTCDATE(), ModifiedBy = @ModifiedBy WHERE TaskId = @TaskId AND IsDeleted = 0;"
            Dim params As SqlParameter() = {
                New SqlParameter("@StatusId", statusId),
                New SqlParameter("@ModifiedBy", modifiedBy),
                New SqlParameter("@TaskId", taskId)
            }
            Dim rows = Await _sqlHelper.ExecuteNonQueryAsync(query, params)
            Return rows > 0
        End Function

        Public Async Function UpdateTaskAcceptanceStateAsync(taskId As Integer, assignedStatusId As Integer, inProgressStatusId As Integer, modifiedBy As Integer, originalModifiedOn As Nullable(Of DateTime), Optional transaction As IDbTransaction = Nothing) As Task(Of Boolean) Implements ITaskRepository.UpdateTaskAcceptanceStateAsync
            Const query As String = "UPDATE dbo.tbl_Tasks SET StatusId = @InProgressStatusId, ModifiedOn = GETUTCDATE(), ModifiedBy = @ModifiedBy " &
                                   "WHERE TaskId = @TaskId AND IsDeleted = 0 AND StatusId = @AssignedStatusId " &
                                   "AND ((@OriginalModifiedOn IS NULL AND ModifiedOn IS NULL) OR (ModifiedOn IS NOT NULL AND @OriginalModifiedOn IS NOT NULL AND ABS(DATEDIFF_BIG(ms, ModifiedOn, @OriginalModifiedOn)) <= 10));"

            Dim pOrigMod As New SqlParameter("@OriginalModifiedOn", SqlDbType.DateTime2)
            If originalModifiedOn.HasValue Then
                pOrigMod.Value = originalModifiedOn.Value
            Else
                pOrigMod.Value = DBNull.Value
            End If

            Dim params As SqlParameter() = {
                New SqlParameter("@InProgressStatusId", inProgressStatusId),
                New SqlParameter("@AssignedStatusId", assignedStatusId),
                New SqlParameter("@ModifiedBy", modifiedBy),
                New SqlParameter("@TaskId", taskId),
                pOrigMod
            }
            Dim rows = Await _sqlHelper.ExecuteNonQueryAsync(query, params, transaction)
            Return rows > 0
        End Function

        Public Async Function UpdateTaskCompletionStateAsync(taskId As Integer, statusId As Integer, completionDate As Nullable(Of DateTime), modifiedBy As Integer) As Task(Of Boolean) Implements ITaskRepository.UpdateTaskCompletionStateAsync
            Const query As String = "UPDATE dbo.tbl_Tasks SET StatusId = @StatusId, CompletionDate = @CompletionDate, ModifiedOn = GETUTCDATE(), ModifiedBy = @ModifiedBy WHERE TaskId = @TaskId AND IsDeleted = 0;"
            Dim params As SqlParameter() = {
                New SqlParameter("@StatusId", statusId),
                New SqlParameter("@CompletionDate", If(completionDate.HasValue, CObj(completionDate.Value), DBNull.Value)),
                New SqlParameter("@ModifiedBy", modifiedBy),
                New SqlParameter("@TaskId", taskId)
            }
            Dim rows = Await _sqlHelper.ExecuteNonQueryAsync(query, params)
            Return rows > 0
        End Function

        Public Async Function SoftDeleteAsync(taskId As Integer, originalModifiedOn As Nullable(Of DateTime), modifiedBy As Integer, Optional transaction As IDbTransaction = Nothing) As Task(Of Boolean) Implements ITaskRepository.SoftDeleteAsync
            Const query As String = "UPDATE dbo.tbl_Tasks SET IsDeleted = 1, ModifiedOn = GETUTCDATE(), ModifiedBy = @ModifiedBy WHERE TaskId = @TaskId AND IsDeleted = 0;"
            Dim params As SqlParameter() = {
                New SqlParameter("@ModifiedBy", modifiedBy),
                New SqlParameter("@TaskId", taskId)
            }
            Dim rows = Await _sqlHelper.ExecuteNonQueryAsync(query, params, transaction)
            Return rows > 0
        End Function

        Public Async Function RestoreAsync(taskId As Integer, modifiedBy As Integer) As Task(Of Boolean) Implements ITaskRepository.RestoreAsync
            Const query As String = "UPDATE dbo.tbl_Tasks SET IsDeleted = 0, ModifiedOn = GETUTCDATE(), ModifiedBy = @ModifiedBy WHERE TaskId = @TaskId AND IsDeleted = 1;"
            Dim params As SqlParameter() = {
                New SqlParameter("@ModifiedBy", modifiedBy),
                New SqlParameter("@TaskId", taskId)
            }
            Dim rows = Await _sqlHelper.ExecuteNonQueryAsync(query, params)
            Return rows > 0
        End Function

        Public Async Function HardDeleteAsync(taskId As Integer) As Task(Of Boolean) Implements ITaskRepository.HardDeleteAsync
            Const query As String = "DELETE FROM dbo.tbl_Tasks WHERE TaskId = @TaskId AND IsDeleted = 1;"
            Dim params As SqlParameter() = {
                New SqlParameter("@TaskId", taskId)
            }
            Dim rows = Await _sqlHelper.ExecuteNonQueryAsync(query, params)
            Return rows > 0
        End Function

        Public Async Function GetTaskDependencyCountsAsync(taskId As Integer) As Task(Of Tuple(Of Integer, Integer)) Implements ITaskRepository.GetTaskDependencyCountsAsync
            Const activityQuery As String = "SELECT COUNT(*) FROM dbo.tbl_TaskActivities WHERE TaskId = @TaskId;"
            Const discussionQuery As String = "SELECT COUNT(*) FROM dbo.tbl_Discussions WHERE TaskId = @TaskId;"
            
            Dim activityCount = Await _sqlHelper.ExecuteScalarAsync(Of Integer)(activityQuery, {New SqlParameter("@TaskId", taskId)})
            Dim discussionCount = Await _sqlHelper.ExecuteScalarAsync(Of Integer)(discussionQuery, {New SqlParameter("@TaskId", taskId)})
            
            Return New Tuple(Of Integer, Integer)(activityCount, discussionCount)
        End Function


        Private Function MapStatusIdToWorkflowState(statusId As Integer) As TaskWorkflowState
            If [Enum].IsDefined(GetType(TaskWorkflowState), statusId) Then
                Return CType(statusId, TaskWorkflowState)
            End If
            Return TaskWorkflowState.Assigned
        End Function

        Private Function MapTaskEntity(reader As IDataReader) As TaskEntity
            Dim entity As New TaskEntity() With {
                .TaskId = Convert.ToInt32(reader("TaskId")),
                .TaskCode = Convert.ToString(reader("TaskCode")),
                .Title = Convert.ToString(reader("Title")),
                .Description = If(reader("Description") Is DBNull.Value, String.Empty, Convert.ToString(reader("Description"))),
                .ClientId = Convert.ToInt32(reader("ClientId")),
                .AssignedToUserId = Convert.ToInt32(reader("AssignedToUserId")),
                .AssignedByUserId = Convert.ToInt32(reader("AssignedByUserId")),
                .Department = CType(Convert.ToInt32(reader("DepartmentId")), DepartmentType),
                .Priority = CType(Convert.ToInt32(reader("PriorityId")), TaskPriority),
                .WorkflowState = MapStatusIdToWorkflowState(Convert.ToInt32(reader("StatusId"))),
                .AssignmentDate = Convert.ToDateTime(reader("AssignmentDate")),
                .TargetDueDate = Convert.ToDateTime(reader("TargetDueDate")),
                .ReminderDate = If(reader("ReminderDate") Is DBNull.Value, CType(Nothing, Nullable(Of DateTime)), Convert.ToDateTime(reader("ReminderDate"))),
                .CompletionDate = If(reader("CompletionDate") Is DBNull.Value, CType(Nothing, Nullable(Of DateTime)), Convert.ToDateTime(reader("CompletionDate"))),
                .IsDeleted = Convert.ToBoolean(reader("IsDeleted")),
                .CreatedOn = Convert.ToDateTime(reader("CreatedOn")),
                .CreatedBy = Convert.ToInt32(reader("CreatedBy")),
                .ModifiedOn = If(reader("ModifiedOn") Is DBNull.Value, CType(Nothing, Nullable(Of DateTime)), Convert.ToDateTime(reader("ModifiedOn"))),
                .ModifiedBy = If(reader("ModifiedBy") Is DBNull.Value, CType(Nothing, Nullable(Of Integer)), Convert.ToInt32(reader("ModifiedBy")))
            }

            Try
                If reader("CategoryId") IsNot DBNull.Value Then
                    entity.CategoryId = Convert.ToInt32(reader("CategoryId"))
                End If
                If reader("FinancialYearId") IsNot DBNull.Value Then
                    entity.FinancialYearId = Convert.ToInt32(reader("FinancialYearId"))
                End If
            Catch
            End Try

            Return entity
        End Function
    End Class
End Namespace
