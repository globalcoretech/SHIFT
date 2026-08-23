Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Models.Backup

Namespace Interfaces
    ''' <summary>
    ''' Service interface orchestrating T-SQL database backups, VERIFYONLY integrity checks,
    ''' backup history recording, and secondary destination replication.
    ''' </summary>
    Public Interface IDatabaseBackupService
        Function ExecuteBackupAsync(Optional destinationDirectory As String = "", Optional createdBy As String = "") As Task(Of DatabaseBackupHistoryDto)
        Function VerifyBackupFileAsync(backupFilePath As String) As Task(Of Boolean)
        Function GetBackupHistoryAsync(Optional topCount As Integer = 50) As Task(Of List(Of DatabaseBackupHistoryDto))
    End Interface
End Namespace
