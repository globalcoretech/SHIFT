Namespace DTOs
    ''' <summary>
    ''' Data Transfer Object for Task Category.
    ''' </summary>
    Public Class TaskCategoryDto
        Public Property CategoryId As Integer
        Public Property CategoryCode As String = String.Empty
        Public Property CategoryName As String = String.Empty
        Public Property IsActive As Boolean
        
        Public Overrides Function ToString() As String
            Return CategoryName
        End Function
    End Class
End Namespace
