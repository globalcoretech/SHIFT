Imports System.Threading.Tasks
Imports StaffAutomation.Core.Entities

Namespace Interfaces
    ''' <summary>
    ''' Repository interface contract for CA Firm Profile data access.
    ''' </summary>
    Public Interface IFirmProfileRepository
        Function GetProfileAsync() As Task(Of FirmProfileEntity)
        Function SaveProfileAsync(profile As FirmProfileEntity) As Task(Of Boolean)
    End Interface
End Namespace
