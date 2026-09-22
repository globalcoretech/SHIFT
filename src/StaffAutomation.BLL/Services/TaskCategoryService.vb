Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.DAL.Interfaces

Namespace Services
    ''' <summary>
    ''' Business Logic Service for Task Categories
    ''' </summary>
    Public Class TaskCategoryService
        Implements ITaskCategoryService

        Private ReadOnly _repository As ITaskCategoryRepository
        Private ReadOnly _auditLogger As IAuditLogger

        Public Sub New(repository As ITaskCategoryRepository, auditLogger As IAuditLogger)
            _repository = repository
            _auditLogger = auditLogger
        End Sub

        Public Async Function GetAllCategoriesAsync(Optional includeInactive As Boolean = False) As Task(Of IEnumerable(Of TaskCategoryDto)) Implements ITaskCategoryService.GetAllCategoriesAsync
            Dim entities = Await _repository.GetAllCategoriesAsync(includeInactive)
            Return entities.Select(Function(e) New TaskCategoryDto With {
                .CategoryId = e.CategoryId,
                .CategoryCode = e.CategoryCode,
                .CategoryName = e.CategoryName,
                .IsActive = e.IsActive
            }).ToList()
        End Function

        Public Async Function AddCategoryAsync(categoryDto As TaskCategoryDto, currentUserId As Integer) As Task(Of Integer) Implements ITaskCategoryService.AddCategoryAsync
            If String.IsNullOrWhiteSpace(categoryDto.CategoryName) Then
                Throw New BusinessException("Category Name is required.", "ERR_CAT_NAME_REQUIRED")
            End If

            Dim entity As New TaskCategoryEntity With {
                .CategoryCode = categoryDto.CategoryCode,
                .CategoryName = categoryDto.CategoryName.Trim(),
                .IsActive = categoryDto.IsActive,
                .CreatedBy = currentUserId
            }
            
            Dim newId = Await _repository.AddCategoryAsync(entity)
            Await _auditLogger.LogAuditAsync(currentUserId, "ADD_TASK_CATEGORY", "TaskCategories", $"Added new task category: {entity.CategoryName}")
            Return newId
        End Function

        Public Async Function UpdateCategoryAsync(categoryDto As TaskCategoryDto, currentUserId As Integer) As Task(Of Boolean) Implements ITaskCategoryService.UpdateCategoryAsync
            If categoryDto.CategoryId <= 0 Then
                Throw New BusinessException("Invalid Category ID.", "ERR_INVALID_CAT_ID")
            End If
            If String.IsNullOrWhiteSpace(categoryDto.CategoryName) Then
                Throw New BusinessException("Category Name is required.", "ERR_CAT_NAME_REQUIRED")
            End If

            Dim entity As New TaskCategoryEntity With {
                .CategoryId = categoryDto.CategoryId,
                .CategoryCode = categoryDto.CategoryCode,
                .CategoryName = categoryDto.CategoryName.Trim(),
                .IsActive = categoryDto.IsActive
            }

            Dim result = Await _repository.UpdateCategoryAsync(entity)
            If result Then
                Await _auditLogger.LogAuditAsync(currentUserId, "UPDATE_TASK_CATEGORY", "TaskCategories", $"Updated task category ID: {entity.CategoryId}")
            End If
            Return result
        End Function

        Public Async Function ToggleCategoryStatusAsync(categoryId As Integer, isActive As Boolean, currentUserId As Integer) As Task(Of Boolean) Implements ITaskCategoryService.ToggleCategoryStatusAsync
            If categoryId <= 0 Then
                Throw New BusinessException("Invalid Category ID.", "ERR_INVALID_CAT_ID")
            End If

            Dim result = Await _repository.ToggleCategoryStatusAsync(categoryId, isActive, currentUserId)
            If result Then
                Dim action = If(isActive, "Activated", "Deactivated")
                Await _auditLogger.LogAuditAsync(currentUserId, "TOGGLE_TASK_CATEGORY", "TaskCategories", $"{action} task category ID: {categoryId}")
            End If
            Return result
        End Function
    End Class
End Namespace
