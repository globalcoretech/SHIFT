Imports System.Threading.Tasks
Imports StaffAutomation.Core.Models.Backup

Namespace Interfaces
    ''' <summary>
    ''' Interface for database restore preparation workflow, emergency pre-restore backups, and validation.
    ''' </summary>
    Public Interface IDatabaseRestorePreparationService
        Function ValidateRestoreFileAsync(backupFilePath As String) As Task(Of Boolean)
        Function PrepareEmergencyPreRestoreBackupAsync() As Task(Of DatabaseBackupHistoryDto)
        Function ExecuteControlledRestoreAsync(backupFilePath As String) As Task(Of Boolean)
    End Interface
End Namespace
