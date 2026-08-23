Imports System

Namespace Entities
    ''' <summary>
    ''' System Domain Entity representing financial year accounting periods.
    ''' Maps conceptually to dbo.tbl_FinancialYears.
    ''' </summary>
    Public Class FinancialYearEntity
        Public Property FinancialYearId As Integer
        Public Property FYCode As String = String.Empty
        Public Property StartDate As DateTime
        Public Property EndDate As DateTime
        Public Property IsCurrentFY As Boolean = False
        Public Property IsLocked As Boolean = False
        Public Property IsDeleted As Boolean = False
        Public Property CreatedOn As DateTime = DateTime.UtcNow
        Public Property CreatedBy As Integer = 1
        Public Property ModifiedOn As Nullable(Of DateTime)
        Public Property ModifiedBy As Nullable(Of Integer)
    End Class
End Namespace
