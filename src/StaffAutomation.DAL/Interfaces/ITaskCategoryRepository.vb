Imports StaffAutomation.Core.Entities

Namespace Interfaces
    ''' <summary>
    ''' Repository for Task Categories
    ''' </summary>
    Public Interface ITaskCategoryRepository
        Function GetAllCategoriesAsync(Optional includeInactive As Boolean = False) As Task(Of IEnumerable(Of TaskCategoryEntity))
        Function AddCategoryAsync(entity As TaskCategoryEntity) As Task(Of Integer)
        Function UpdateCategoryAsync(entity As TaskCategoryEntity) As Task(Of Boolean)
        Function ToggleCategoryStatusAsync(categoryId As Integer, isActive As Boolean, modifiedByUserId As Integer) As Task(Of Boolean)
    End Interface
End Namespace
