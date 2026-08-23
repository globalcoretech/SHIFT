Imports System.Threading.Tasks
Imports StaffAutomation.Core.Models.Backup

Namespace Interfaces
    ''' <summary>
    ''' Service interface detecting SQL Server version, edition, recovery model, backup compression support, and storage write readiness.
    ''' </summary>
    Public Interface IDatabaseServerCapabilityService
        Function DetectCapabilitiesAsync(Optional destinationPath As String = "") As Task(Of ServerCapabilityResult)
    End Interface
End Namespace
