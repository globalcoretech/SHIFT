Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Enums

Namespace Interfaces
    ''' <summary>
    ''' Business Logic Service contract for Client Master management and 360-degree history.
    ''' </summary>
    Public Interface IClientService
        Function GetClientByIdAsync(clientId As Integer) As Task(Of ClientDto)
        Function GetAllClientsAsync(Optional department As Nullable(Of DepartmentType) = Nothing, Optional includeDeleted As Boolean = False) As Task(Of List(Of ClientDto))
        Function CreateClientAsync(clientDto As ClientDto) As Task(Of Integer)
        Function UpdateClientAsync(clientDto As ClientDto) As Task(Of Boolean)
        Function SoftDeleteClientAsync(clientId As Integer) As Task(Of Boolean)
    End Interface
End Namespace
