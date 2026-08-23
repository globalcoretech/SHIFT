Imports System

Namespace Models.Backup
    ''' <summary>
    ''' Strongly typed result model containing SQL Server capabilities, recovery model, backup compression support,
    ''' storage availability, and service health status.
    ''' </summary>
    Public Class ServerCapabilityResult
        Public Property SqlServerVersion As String = String.Empty
        Public Property SqlServerEdition As String = String.Empty
        Public Property RecoveryModel As String = String.Empty
        Public Property SupportsBackupCompression As Boolean = False
        Public Property SupportsChecksum As Boolean = True
        Public Property IsServiceAvailable As Boolean = False
        Public Property DatabaseState As String = "UNKNOWN"
        Public Property AvailableDiskSpaceBytes As Long = 0
        Public Property HasWritePermission As Boolean = False
        Public Property PrimaryBackupPath As String = String.Empty
        Public Property SecondaryBackupPath As String = String.Empty
        Public Property StatusMessage As String = String.Empty
    End Class
End Namespace
