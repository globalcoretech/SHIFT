Imports System.Threading.Tasks
Imports StaffAutomation.Core.DTOs

Namespace Interfaces
    ''' <summary>
    ''' Service interface contract for CA Firm Profile management.
    ''' </summary>
    Public Interface IFirmProfileService
        Function GetFirmProfileAsync() As Task(Of FirmProfileDto)
        Function SaveFirmProfileAsync(dto As FirmProfileDto, modifiedBy As Integer) As Task(Of Boolean)
    End Interface
End Namespace
