Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.Core.DTOs.Reports

Namespace Interfaces
    Public Interface IReportRepository
        Function GetStaffDailyActivityReportAsync(userId As Integer?, startDate As Date, endDate As Date) As Task(Of List(Of StaffDailyActivitySummaryDto))
    End Interface
End Namespace
