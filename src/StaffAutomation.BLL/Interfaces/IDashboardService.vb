Imports System.Threading.Tasks
Imports StaffAutomation.Core.DTOs

Namespace Interfaces
    ''' <summary>
    ''' Business Logic Service contract for computing the 12 Owner Dashboard KPIs.
    ''' </summary>
    Public Interface IDashboardService
        Function GetLiveExecutiveKpisAsync() As Task(Of DashboardKpiSummaryDto)
    End Interface
End Namespace
