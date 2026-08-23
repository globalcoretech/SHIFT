Imports System

Namespace DTOs
    ''' <summary>
    ''' Data Transfer Object for Financial Year accounting periods.
    ''' </summary>
    Public Class FinancialYearDto
        Public Property FinancialYearId As Integer
        Public Property FYCode As String = String.Empty
        Public Property StartDate As DateTime
        Public Property EndDate As DateTime
        Public Property IsCurrentFY As Boolean = False
        Public Property IsLocked As Boolean = False
        Public Property IsDeleted As Boolean = False
    End Class
End Namespace
