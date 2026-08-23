Imports System.Collections.Generic
Imports System.Threading.Tasks

Namespace Interfaces
    ''' <summary>
    ''' Service interface managing backup retention rules, cleanup of expired backup archives,
    ''' and preservation of critical backup recovery points.
    ''' </summary>
    Public Interface IDatabaseBackupRetentionService
        Function CleanupExpiredBackupsAsync(retentionDays As Integer, primaryBackupRoot As String, Optional secondaryBackupRoot As String = "") As Task(Of List(Of String))
    End Interface
End Namespace
