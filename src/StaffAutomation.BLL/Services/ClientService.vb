Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.BLL.Logging
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Logging
Imports StaffAutomation.Core.Security
Imports StaffAutomation.Core.Validation
Imports StaffAutomation.DAL.Interfaces

Namespace Services
    ''' <summary>
    ''' Client Master Service handling CRUD operations, business data protection, and audit logging.
    ''' Conforms strictly to IClientService interface contract.
    ''' </summary>
    Public Class ClientService
        Implements IClientService

        Private ReadOnly _clientRepo As IClientRepository
        Private ReadOnly _appLogger As IAppLogger
        Private ReadOnly _auditLogger As AuditLogger

        Public Sub New(clientRepo As IClientRepository, appLogger As IAppLogger, auditLogger As AuditLogger)
            _clientRepo = clientRepo
            _appLogger = appLogger
            _auditLogger = auditLogger
        End Sub

        Public Async Function GetClientByIdAsync(clientId As Integer) As Task(Of ClientDto) Implements IClientService.GetClientByIdAsync
            Dim entity = Await _clientRepo.GetByIdAsync(clientId)
            If entity Is Nothing Then Return Nothing
            Return MapToDto(entity)
        End Function

        Public Async Function GetAllClientsAsync(Optional department As Nullable(Of DepartmentType) = Nothing, Optional includeDeleted As Boolean = False) As Task(Of List(Of ClientDto)) Implements IClientService.GetAllClientsAsync
            Dim entities = Await _clientRepo.GetAllAsync(department, includeDeleted)

            Dim list As New List(Of ClientDto)()
            For Each e In entities
                list.Add(MapToDto(e))
            Next
            Return list
        End Function

        Public Async Function CreateClientAsync(clientDto As ClientDto) As Task(Of Integer) Implements IClientService.CreateClientAsync
            ' Business Validation
            Dim nameVal = CommonValidators.ValidateRequired(clientDto.ClientName, "Client Name")
            If Not nameVal.IsValid Then Throw New ValidationException(nameVal.Errors(0), "ClientName")

            Dim codeVal = CommonValidators.ValidateRequired(clientDto.ClientCode, "Client Code")
            If Not codeVal.IsValid Then Throw New ValidationException(codeVal.Errors(0), "ClientCode")

            ' Duplicate Client Prevention Check
            Dim existingClients = Await _clientRepo.GetAllAsync(includeDeleted:=False)
            Dim duplicateCode = existingClients.Find(Function(c) c.ClientCode.Equals(clientDto.ClientCode.Trim(), StringComparison.OrdinalIgnoreCase))
            If duplicateCode IsNot Nothing Then
                Throw New ValidationException($"A client with Code '{clientDto.ClientCode}' already exists in the system.", "ClientCode")
            End If

            Dim duplicateName = existingClients.Find(Function(c) c.ClientName.Equals(clientDto.ClientName.Trim(), StringComparison.OrdinalIgnoreCase))
            If duplicateName IsNot Nothing Then
                Throw New ValidationException($"A client with Name '{clientDto.ClientName}' already exists in the system.", "ClientName")
            End If

            If Not String.IsNullOrWhiteSpace(clientDto.Gstin) Then
                Dim duplicateGstin = existingClients.Find(Function(c) Not String.IsNullOrEmpty(c.Gstin) AndAlso c.Gstin.Equals(clientDto.Gstin.Trim(), StringComparison.OrdinalIgnoreCase))
                If duplicateGstin IsNot Nothing Then
                    Throw New ValidationException($"Client Master record already exists with GSTIN '{clientDto.Gstin}': {duplicateGstin.ClientName} ({duplicateGstin.ClientCode}).", "Gstin")
                End If
            End If

            Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)

            Dim entity As New ClientEntity() With {
                .ClientCode = clientDto.ClientCode.Trim().ToUpper(),
                .ClientName = clientDto.ClientName.Trim(),
                .ContactPerson = clientDto.ContactPerson,
                .Phone = clientDto.Phone,
                .Email = clientDto.Email,
                .Department = clientDto.Department,
                .Gstin = clientDto.Gstin.Trim().ToUpper(),
                .GstType = clientDto.GstType,
                .PanNumber = clientDto.PanNumber.Trim().ToUpper(),
                .StateName = clientDto.StateName,
                .EntityType = clientDto.EntityType,
                .Address = clientDto.Address,
                .District = clientDto.District,
                .Pincode = clientDto.Pincode,
                .IsLiveApiData = clientDto.IsLiveApiData,
                .IsActive = clientDto.IsActive,
                .CreatedBy = currentUserId
            }

            Dim newId = Await _clientRepo.AddAsync(entity)
            _appLogger.LogInfo($"Client '{entity.ClientName}' (Code: {entity.ClientCode}, ID: {newId}) created.", "ClientService")
            Await _auditLogger.LogAuditAsync(currentUserId, "CLIENT_CREATE", "ClientService", $"Client '{entity.ClientName}' (ID: {newId}) created.")

            Return newId
        End Function

        Public Async Function UpdateClientAsync(clientDto As ClientDto) As Task(Of Boolean) Implements IClientService.UpdateClientAsync
            Dim existing = Await _clientRepo.GetByIdAsync(clientDto.ClientId)
            If existing Is Nothing Then
                Throw New BusinessException($"Client ID {clientDto.ClientId} was not found.", "ERR_CLIENT_NOT_FOUND")
            End If

            ' Validation
            Dim nameVal = CommonValidators.ValidateRequired(clientDto.ClientName, "Client Name")
            If Not nameVal.IsValid Then Throw New ValidationException(nameVal.Errors(0), "ClientName")

            Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)

            existing.ClientName = clientDto.ClientName.Trim()
            existing.ContactPerson = clientDto.ContactPerson
            existing.Phone = clientDto.Phone
            existing.Email = clientDto.Email
            existing.Department = clientDto.Department
            existing.Gstin = clientDto.Gstin.Trim().ToUpper()
            existing.GstType = clientDto.GstType
            existing.PanNumber = clientDto.PanNumber.Trim().ToUpper()
            existing.StateName = clientDto.StateName
            existing.EntityType = clientDto.EntityType
            existing.Address = clientDto.Address
            existing.District = clientDto.District
            existing.Pincode = clientDto.Pincode
            existing.IsLiveApiData = clientDto.IsLiveApiData
            existing.IsActive = clientDto.IsActive
            existing.ModifiedBy = currentUserId

            Dim success = Await _clientRepo.UpdateAsync(existing)
            If success Then
                _appLogger.LogInfo($"Client ID {clientDto.ClientId} updated.", "ClientService")
                Await _auditLogger.LogAuditAsync(currentUserId, "CLIENT_UPDATE", "ClientService", $"Client '{clientDto.ClientName}' (ID: {clientDto.ClientId}) updated.")
            End If

            Return success
        End Function

        Public Async Function SoftDeleteClientAsync(clientId As Integer) As Task(Of Boolean) Implements IClientService.SoftDeleteClientAsync
            Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)
            Dim success = Await _clientRepo.SoftDeleteAsync(clientId, currentUserId)
            If success Then
                _appLogger.LogInfo($"Client ID {clientId} archived.", "ClientService")
                Await _auditLogger.LogAuditAsync(currentUserId, "CLIENT_ARCHIVE", "ClientService", $"Client ID {clientId} archived.")
            End If

            Return success
        End Function

        Private Function MapToDto(entity As ClientEntity) As ClientDto
            Return New ClientDto() With {
                .ClientId = entity.ClientId,
                .ClientCode = entity.ClientCode,
                .ClientName = entity.ClientName,
                .ContactPerson = entity.ContactPerson,
                .Phone = entity.Phone,
                .Email = entity.Email,
                .Department = entity.Department,
                .Gstin = entity.Gstin,
                .GstType = entity.GstType,
                .PanNumber = entity.PanNumber,
                .StateName = entity.StateName,
                .EntityType = entity.EntityType,
                .Address = entity.Address,
                .District = entity.District,
                .Pincode = entity.Pincode,
                .IsLiveApiData = entity.IsLiveApiData,
                .IsActive = entity.IsActive,
                .IsDeleted = entity.IsDeleted
            }
        End Function
    End Class
End Namespace
