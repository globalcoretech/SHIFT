Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.Core.DTOs

Namespace Interfaces
    ''' <summary>
    ''' Service interface for managing Task Categories
    ''' </summary>
    Public Interface ITaskCategoryService
        Function GetAllCategoriesAsync(Optional includeInactive As Boolean = False) As Task(Of IEnumerable(Of TaskCategoryDto))
        Function AddCategoryAsync(categoryDto As TaskCategoryDto, currentUserId As Integer) As Task(Of Integer)
        Function UpdateCategoryAsync(categoryDto As TaskCategoryDto, currentUserId As Integer) As Task(Of Boolean)
        Function ToggleCategoryStatusAsync(categoryId As Integer, isActive As Boolean, currentUserId As Integer) As Task(Of Boolean)
    End Interface
End Namespace
