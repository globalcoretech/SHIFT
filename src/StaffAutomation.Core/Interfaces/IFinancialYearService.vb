Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.Core.DTOs

Namespace Interfaces
    ''' <summary>
    ''' Service interface contract for Financial Year accounting period management.
    ''' </summary>
    Public Interface IFinancialYearService
        Function GetActiveFinancialYearAsync() As Task(Of FinancialYearDto)
        Function GetFinancialYearByIdAsync(financialYearId As Integer) As Task(Of FinancialYearDto)
        Function GetAllFinancialYearsAsync(Optional includeDeleted As Boolean = False) As Task(Of List(Of FinancialYearDto))
        Function CreateFinancialYearAsync(dto As FinancialYearDto, createdBy As Integer) As Task(Of Integer)
        Function UpdateFinancialYearAsync(dto As FinancialYearDto, modifiedBy As Integer) As Task(Of Boolean)
        Function ActivateFinancialYearAsync(financialYearId As Integer, modifiedBy As Integer) As Task(Of Boolean)
        Function LockFinancialYearAsync(financialYearId As Integer, isLocked As Boolean, modifiedBy As Integer) As Task(Of Boolean)
    End Interface
End Namespace
