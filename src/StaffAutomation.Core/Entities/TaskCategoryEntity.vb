Namespace Entities
    ''' <summary>
    ''' Represents a Task Category (Task Type) in the domain.
    ''' </summary>
    Public Class TaskCategoryEntity
        Public Property CategoryId As Integer
        Public Property CategoryCode As String = String.Empty
        Public Property CategoryName As String = String.Empty
        Public Property IsActive As Boolean = True
        Public Property IsDeleted As Boolean = False
        Public Property CreatedOn As DateTime = DateTime.UtcNow
        Public Property CreatedBy As Integer
    End Class
End Namespace
