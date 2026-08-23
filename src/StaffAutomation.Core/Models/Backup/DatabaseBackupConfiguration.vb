Imports System

Namespace Models.Backup
    ''' <summary>
    ''' Strongly typed configuration model for database backups, retention rules, and secondary replication.
    ''' </summary>
    Public Class DatabaseBackupConfiguration
        Public Property ConfigId As Integer = 1
        Public Shared Function GetDefaultPrimaryBackupPath() As String
            Dim programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
            If String.IsNullOrWhiteSpace(programData) Then programData = "C:\ProgramData"
            Return IO.Path.Combine(programData, "StaffAutomation", "Backups")
        End Function

        Public Property PrimaryBackupPath As String = GetDefaultPrimaryBackupPath()
        Public Property SecondaryBackupPath As String = String.Empty
        Public Property EnableSecondaryCopy As Boolean = False
        Public Property RetentionDays As Integer = 30
        Public Property EnableAutomaticBackup As Boolean = True
        Public Property ScheduledBackupTime As String = "23:00"
        Public Property LastSuccessfulBackupOn As Nullable(Of DateTime)
        Public Property UpdatedOn As DateTime = DateTime.Now
    End Class
End Namespace
