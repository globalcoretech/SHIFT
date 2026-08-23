Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Models.Backup

Namespace Interfaces
    ''' <summary>
    ''' Service interface executing DBCC CHECKDB database integrity diagnostics and tracking audit history.
    ''' </summary>
    Public Interface IDatabaseIntegrityService
        Function ExecuteIntegrityCheckAsync(Optional executedBy As String = "") As Task(Of DatabaseIntegrityHistoryDto)
        Function GetIntegrityHistoryAsync(Optional topCount As Integer = 50) As Task(Of List(Of DatabaseIntegrityHistoryDto))
    End Interface
End Namespace
