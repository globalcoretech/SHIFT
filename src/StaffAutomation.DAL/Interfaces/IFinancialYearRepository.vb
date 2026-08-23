Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Entities

Namespace Interfaces
    ''' <summary>
    ''' Repository interface contract for Financial Year accounting periods data access.
    ''' </summary>
    Public Interface IFinancialYearRepository
        Function GetByIdAsync(financialYearId As Integer) As Task(Of FinancialYearEntity)
        Function GetActiveAsync() As Task(Of FinancialYearEntity)
        Function GetAllAsync(Optional includeDeleted As Boolean = False) As Task(Of List(Of FinancialYearEntity))
        Function AddAsync(financialYear As FinancialYearEntity) As Task(Of Integer)
        Function UpdateAsync(financialYear As FinancialYearEntity) As Task(Of Boolean)
        Function SetActiveAsync(financialYearId As Integer, modifiedBy As Integer) As Task(Of Boolean)
        Function SetLockStateAsync(financialYearId As Integer, isLocked As Boolean, modifiedBy As Integer) As Task(Of Boolean)
    End Interface
End Namespace
