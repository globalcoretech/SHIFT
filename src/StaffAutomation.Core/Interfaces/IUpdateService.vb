Imports System.Threading.Tasks
Imports StaffAutomation.Core.DTOs

Namespace Interfaces
    Public Interface IUpdateService
        ''' <summary>
        ''' Checks if an application update is available compared to the current application version.
        ''' Returns an UpdateCheckResult indicating success/failure and update availability.
        ''' </summary>
        Function CheckForUpdatesAsync() As Task(Of UpdateCheckResult)

        ''' <summary>
        ''' Downloads the update package securely and verifies its SHA-256 hash.
        ''' </summary>
        Function DownloadAndVerifyUpdateAsync(manifest As UpdateManifestDto, progress As IProgress(Of Integer)) As Task(Of UpdateCheckResult)
    End Interface
End Namespace
