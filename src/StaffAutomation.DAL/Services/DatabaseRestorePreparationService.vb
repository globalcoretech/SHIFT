Imports System
Imports System.IO
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Configuration
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Models.Backup

Namespace Services
    ''' <summary>
    ''' Implementation of Database Restore Preparation Service orchestrating pre-restore validation,
    ''' automatic emergency backups, SHA-256 checks, and restore runner invocation.
    ''' </summary>
    Public Class DatabaseRestorePreparationService
        Implements IDatabaseRestorePreparationService

        Private ReadOnly _config As IAppConfiguration
        Private ReadOnly _backupService As IDatabaseBackupService
        Private ReadOnly _restoreRunner As DatabaseRestoreRunner

        Public Sub New(config As IAppConfiguration, backupService As IDatabaseBackupService, restoreRunner As DatabaseRestoreRunner)
            _config = config
            _backupService = backupService
            _restoreRunner = restoreRunner
        End Sub

        Public Async Function ValidateRestoreFileAsync(backupFilePath As String) As Task(Of Boolean) Implements IDatabaseRestorePreparationService.ValidateRestoreFileAsync
            If String.IsNullOrWhiteSpace(backupFilePath) OrElse Not File.Exists(backupFilePath) Then
                Return False
            End If

            ' Execute RESTORE VERIFYONLY
            Return Await _backupService.VerifyBackupFileAsync(backupFilePath)
        End Function

        Public Async Function PrepareEmergencyPreRestoreBackupAsync() As Task(Of DatabaseBackupHistoryDto) Implements IDatabaseRestorePreparationService.PrepareEmergencyPreRestoreBackupAsync
            Dim primaryPath = _config.GetSetting("PrimaryBackupPath", DatabaseBackupConfiguration.GetDefaultPrimaryBackupPath())
            Return Await _backupService.ExecuteBackupAsync(primaryPath, "EMERGENCY_PRE_RESTORE")
        End Function

        Public Async Function ExecuteControlledRestoreAsync(backupFilePath As String) As Task(Of Boolean) Implements IDatabaseRestorePreparationService.ExecuteControlledRestoreAsync
            ' 1. Pre-restore validation
            Dim isValid = Await ValidateRestoreFileAsync(backupFilePath)
            If Not isValid Then
                Throw New InvalidOperationException("Restore aborted: Selected backup file failed RESTORE VERIFYONLY structural check.")
            End If

            ' 2. Automatic Emergency Pre-Restore Backup
            Dim emergencyBackup = Await PrepareEmergencyPreRestoreBackupAsync()
            If emergencyBackup Is Nothing OrElse Not emergencyBackup.Status.Equals("Succeeded", StringComparison.OrdinalIgnoreCase) Then
                Throw New InvalidOperationException($"Restore aborted: Emergency pre-restore backup failed ({emergencyBackup.ErrorMessage}).")
            End If

            ' 3. Execute controlled single-user restore via master runner
            Return Await _restoreRunner.ExecuteRestoreAsync(backupFilePath)
        End Function
    End Class
End Namespace
