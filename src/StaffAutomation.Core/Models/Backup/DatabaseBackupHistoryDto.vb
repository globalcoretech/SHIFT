Imports System

Namespace Models.Backup
    ''' <summary>
    ''' Data Transfer Object representing a database backup execution record in tbl_DatabaseBackupHistory.
    ''' </summary>
    Public Class DatabaseBackupHistoryDto
        Public Property BackupId As Guid = Guid.NewGuid()
        Public Property DatabaseName As String = "StaffAutomationDb"
        Public Property BackupType As String = "FULL"
        Public Property FilePath As String = String.Empty
        Public Property FileName As String = String.Empty
        Public Property FileSizeBytes As Long = 0
        Public Property Sha256Hash As String = String.Empty
        Public Property StartedOn As DateTime = DateTime.Now
        Public Property CompletedOn As Nullable(Of DateTime)
        Public Property Status As String = "Pending" ' Pending, Running, Succeeded, Failed, VerificationFailed
        Public Property VerificationStatus As String = "None" ' Verified, VerificationFailed, Skipped
        Public Property VerificationMessage As String = String.Empty
        Public Property SecondaryCopyStatus As String = "None" ' None, Pending, Succeeded, Failed
        Public Property SecondaryFilePath As String = String.Empty
        Public Property SqlServerVersion As String = String.Empty
        Public Property SqlServerEdition As String = String.Empty
        Public Property IsCompressed As Boolean = False
        Public Property IsChecksumEnabled As Boolean = True
        Public Property ErrorMessage As String = String.Empty
        Public Property CreatedBy As String = Environment.UserName
    End Class
End Namespace
