Imports System.Collections.Generic
Imports System.Data
Imports System.Threading.Tasks
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Enums

Namespace Interfaces
    ''' <summary>
    ''' Data Access Repository contract for central Task persistence.
    ''' </summary>
    Public Interface ITaskRepository
        Function GetByIdAsync(taskId As Integer) As Task(Of TaskEntity)
        Function GetByTaskCodeAsync(taskCode As String) As Task(Of TaskEntity)
        Function GetTasksByAssigneeAsync(userId As Integer, Optional state As Nullable(Of TaskWorkflowState) = Nothing) As Task(Of List(Of TaskEntity))
        Function GetTasksByClientAsync(clientId As Integer) As Task(Of List(Of TaskEntity))
        Function GetAllActiveTasksAsync() As Task(Of List(Of TaskEntity))
        Function GetAllTasksAsync(Optional includeDeleted As Boolean = False) As Task(Of List(Of TaskEntity))
        Function AddAsync(taskItem As TaskEntity) As Task(Of Integer)
        Function UpdateStateAsync(taskId As Integer, newState As TaskWorkflowState, modifiedBy As Integer) As Task(Of Boolean)
        Function UpdateTaskAcceptanceStateAsync(taskId As Integer, assignedStatusId As Integer, inProgressStatusId As Integer, modifiedBy As Integer, originalModifiedOn As Nullable(Of DateTime), Optional transaction As IDbTransaction = Nothing) As Task(Of Boolean)
        Function UpdateTaskCompletionStateAsync(taskId As Integer, statusId As Integer, completionDate As Nullable(Of DateTime), modifiedBy As Integer) As Task(Of Boolean)
        Function UpdateTaskAsync(taskItem As TaskEntity) As Task(Of Boolean)
        Function UpdateTaskAsync(updateDto As UpdateTaskDto, categoryId As Integer, departmentId As Integer, modifiedBy As Integer, Optional transaction As IDbTransaction = Nothing) As Task(Of Boolean)
        Function SoftDeleteAsync(taskId As Integer, originalModifiedOn As Nullable(Of DateTime), modifiedBy As Integer, Optional transaction As IDbTransaction = Nothing) As Task(Of Boolean)
        Function RestoreAsync(taskId As Integer, modifiedBy As Integer) As Task(Of Boolean)
        Function HardDeleteAsync(taskId As Integer) As Task(Of Boolean)
        Function GetTaskDependencyCountsAsync(taskId As Integer) As Task(Of Tuple(Of Integer, Integer))
        Function GetCategoryIdByCodeAsync(categoryCode As String) As Task(Of Integer)
        Function GetActiveFinancialYearIdAsync() As Task(Of Integer)
        Function GetStatusIdByStateAsync(state As TaskWorkflowState) As Task(Of Integer)
        Function GetPriorityIdByEnumAsync(priority As TaskPriority) As Task(Of Integer)
        Function GetDepartmentIdByEnumAsync(dept As DepartmentType) As Task(Of Integer)
        Function GetCategoryInfoMapAsync() As Task(Of Dictionary(Of Integer, Tuple(Of String, String)))
    End Interface
End Namespace
