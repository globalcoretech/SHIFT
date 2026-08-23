Imports System.Threading.Tasks
Imports StaffAutomation.Core.DTOs

Namespace Interfaces
    ''' <summary>
    ''' Data Access Repository contract for executing pre-aggregated dashboard KPI queries.
    ''' </summary>
    Public Interface IDashboardRepository
        Function GetLiveExecutiveKpisAsync() As Task(Of DashboardKpiSummaryDto)
    End Interface
End Namespace
