Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Models.Backup

Namespace Services
    ''' <summary>
    ''' Implementation of Database Backup Retention Service managing safe cleanup of expired backup files.
    ''' Enforces 30-day retention rules, preserves newest verified recovery points, protects active/running backups,
    ''' and restricts file deletion strictly to configured backup directories.
    ''' </summary>
    Public Class DatabaseBackupRetentionService
        Implements IDatabaseBackupRetentionService

        Private ReadOnly _backupService As IDatabaseBackupService
        Private ReadOnly _auditLogger As IAuditLogger

        Public Sub New(backupService As IDatabaseBackupService, auditLogger As IAuditLogger)
            _backupService = backupService
            _auditLogger = auditLogger
        End Sub

        Public Async Function CleanupExpiredBackupsAsync(retentionDays As Integer, primaryBackupRoot As String, Optional secondaryBackupRoot As String = "") As Task(Of List(Of String)) Implements IDatabaseBackupRetentionService.CleanupExpiredBackupsAsync
            Dim deletedFiles As New List(Of String)()
            If retentionDays < 1 Then retentionDays = 30

            Try
                ' 1. Fetch complete backup history
                Dim history = Await _backupService.GetBackupHistoryAsync(500)
                If history Is Nothing OrElse history.Count = 0 Then Return deletedFiles

                ' 2. Identify the newest successfully verified backup (NEVER DELETED)
                Dim newestVerified = history.Where(Function(h) h.Status.Equals("Succeeded", StringComparison.OrdinalIgnoreCase) AndAlso
                                                              h.VerificationStatus.Equals("Verified", StringComparison.OrdinalIgnoreCase)) _
                                            .OrderByDescending(Function(h) h.StartedOn) _
                                            .FirstOrDefault()

                Dim newestVerifiedId As Guid = If(newestVerified IsNot Nothing, newestVerified.BackupId, Guid.Empty)

                ' 3. Calculate cutoff threshold date
                Dim cutoffDate = DateTime.Now.AddDays(-retentionDays)

                ' 4. Identify eligible expired records
                For Each item In history
                    ' Skip newest verified backup
                    If item.BackupId = newestVerifiedId Then Continue For

                    ' Skip running backups
                    If item.Status.Equals("Running", StringComparison.OrdinalIgnoreCase) Then Continue For

                    ' Skip unresolved VerificationFailed backups (admin review required)
                    If item.VerificationStatus.Equals("VerificationFailed", StringComparison.OrdinalIgnoreCase) Then Continue For

                    ' Skip backups created within retention window
                    If item.StartedOn >= cutoffDate Then Continue For

                    ' 5. Safely check and delete primary file
                    If Not String.IsNullOrWhiteSpace(item.FilePath) AndAlso IsPathInsideRoot(item.FilePath, primaryBackupRoot) Then
                        If File.Exists(item.FilePath) Then
                            Try
                                File.Delete(item.FilePath)
                                deletedFiles.Add(item.FilePath)
                                Await _auditLogger.LogAsync("DATABASE_BACKUP_RETENTION_CLEANUP", Environment.UserName, $"Cleaned up expired primary backup file: {item.FileName} (Age: {(DateTime.Now - item.StartedOn).TotalDays:F1} days)")
                            Catch ex As Exception
                                ' Log file deletion error gracefully
                            End Try
                        End If
                    End If

                    ' 6. Safely check and delete secondary file if secondary root configured
                    If Not String.IsNullOrWhiteSpace(secondaryBackupRoot) AndAlso Not String.IsNullOrWhiteSpace(item.SecondaryFilePath) Then
                        If IsPathInsideRoot(item.SecondaryFilePath, secondaryBackupRoot) AndAlso File.Exists(item.SecondaryFilePath) Then
                            Try
                                File.Delete(item.SecondaryFilePath)
                                deletedFiles.Add(item.SecondaryFilePath)
                                Await _auditLogger.LogAsync("DATABASE_BACKUP_RETENTION_CLEANUP", Environment.UserName, $"Cleaned up expired secondary backup copy: {Path.GetFileName(item.SecondaryFilePath)}")
                            Catch ex As Exception
                            End Try
                        End If
                    End If
                Next
            Catch ex As Exception
                ' Retention policy failure must not crash application
            End Try

            Return deletedFiles
        End Function

        Private Function IsPathInsideRoot(filePath As String, rootDirectory As String) As Boolean
            If String.IsNullOrWhiteSpace(filePath) OrElse String.IsNullOrWhiteSpace(rootDirectory) Then Return False
            Try
                Dim fullFilePath = Path.GetFullPath(filePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                Dim fullRootDir = Path.GetFullPath(rootDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                Return fullFilePath.StartsWith(fullRootDir, StringComparison.OrdinalIgnoreCase)
            Catch ex As Exception
                Return False
            End Try
        End Function
    End Class
End Namespace
