Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.Core.DTOs.Reports
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.DAL.Interfaces

Namespace Services
    Public Class ReportService
        Implements IReportService

        Private ReadOnly _reportRepo As IReportRepository

        Public Sub New(reportRepo As IReportRepository)
            _reportRepo = reportRepo
        End Sub

        Public Async Function GetStaffDailyActivityReportAsync(userId As Integer?, startDate As Date, endDate As Date) As Task(Of List(Of StaffDailyActivitySummaryDto)) Implements IReportService.GetStaffDailyActivityReportAsync
            If endDate < startDate Then
                Throw New ArgumentException("EndDate cannot be before StartDate.")
            End If

            Return Await _reportRepo.GetStaffDailyActivityReportAsync(userId, startDate, endDate)
        End Function
    End Class
End Namespace
