Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Enums

Namespace Interfaces
    ''' <summary>
    ''' Data Access Repository contract for Client master persistence.
    ''' </summary>
    Public Interface IClientRepository
        Function GetByIdAsync(clientId As Integer) As Task(Of ClientEntity)
        Function GetByCodeAsync(clientCode As String) As Task(Of ClientEntity)
        Function GetByPanAsync(panNumber As String) As Task(Of ClientEntity)
        Function GetByGstinAsync(gstin As String) As Task(Of ClientEntity)
        Function GetAllAsync(Optional department As Nullable(Of DepartmentType) = Nothing, Optional includeDeleted As Boolean = False) As Task(Of List(Of ClientEntity))
        Function AddAsync(client As ClientEntity) As Task(Of Integer)
        Function UpdateAsync(client As ClientEntity) As Task(Of Boolean)
        Function SoftDeleteAsync(clientId As Integer, modifiedBy As Integer) As Task(Of Boolean)
    End Interface
End Namespace
