Imports System
Imports System.Threading.Tasks

Namespace Interfaces
    ''' <summary>
    ''' Contract for automated multi-tab Excel productivity report generation.
    ''' </summary>
    Public Interface IExcelReportGeneratorService
        Function GenerateMonthlyProductivityReportAsync(year As Integer, month As Integer, outputFilePath As String) As Task(Of Boolean)
        Function GenerateClientWorkHistoryReportAsync(clientId As Integer, outputFilePath As String) As Task(Of Boolean)
    End Interface
End Namespace
