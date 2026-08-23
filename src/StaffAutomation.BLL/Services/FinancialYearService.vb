Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Logging
Imports StaffAutomation.Core.Validation
Imports StaffAutomation.DAL.Interfaces

Namespace Services
    ''' <summary>
    ''' Business Logic Service for Financial Year accounting period management.
    ''' Implements IFinancialYearService enforcing single-active-FY, lock controls, non-overlapping dates, and audit logging.
    ''' </summary>
    Public Class FinancialYearService
        Implements IFinancialYearService

        Private ReadOnly _fyRepo As IFinancialYearRepository
        Private ReadOnly _appLogger As IAppLogger
        Private ReadOnly _auditLogger As IAuditLogger

        Public Sub New(fyRepo As IFinancialYearRepository, Optional appLogger As IAppLogger = Nothing, Optional auditLogger As IAuditLogger = Nothing)
            _fyRepo = fyRepo
            _appLogger = appLogger
            _auditLogger = auditLogger
        End Sub

        Public Async Function GetActiveFinancialYearAsync() As Task(Of FinancialYearDto) Implements IFinancialYearService.GetActiveFinancialYearAsync
            Dim entity = Await _fyRepo.GetActiveAsync()
            If entity Is Nothing Then Return Nothing
            Return MapToDto(entity)
        End Function

        Public Async Function GetFinancialYearByIdAsync(financialYearId As Integer) As Task(Of FinancialYearDto) Implements IFinancialYearService.GetFinancialYearByIdAsync
            Dim entity = Await _fyRepo.GetByIdAsync(financialYearId)
            If entity Is Nothing Then Return Nothing
            Return MapToDto(entity)
        End Function

        Public Async Function GetAllFinancialYearsAsync(Optional includeDeleted As Boolean = False) As Task(Of List(Of FinancialYearDto)) Implements IFinancialYearService.GetAllFinancialYearsAsync
            Dim entities = Await _fyRepo.GetAllAsync(includeDeleted)
            Dim list As New List(Of FinancialYearDto)()
            For Each e In entities
                list.Add(MapToDto(e))
            Next
            Return list
        End Function

        Public Async Function CreateFinancialYearAsync(dto As FinancialYearDto, createdBy As Integer) As Task(Of Integer) Implements IFinancialYearService.CreateFinancialYearAsync
            ' Business Validation
            Dim valCode = CommonValidators.ValidateRequired(dto.FYCode, "Financial Year Code")
            If Not valCode.IsValid Then Throw New ValidationException(valCode.Errors(0), "FYCode")

            If dto.StartDate >= dto.EndDate Then
                Throw New ValidationException("Start Date must be strictly earlier than End Date.", "StartDate")
            End If

            Dim existingFYs = Await _fyRepo.GetAllAsync(includeDeleted:=False)

            ' Duplicate Code Prevention
            Dim duplicateCode = existingFYs.Find(Function(fy) fy.FYCode.Equals(dto.FYCode.Trim(), StringComparison.OrdinalIgnoreCase))
            If duplicateCode IsNot Nothing Then
                Throw New ValidationException($"Financial Year Code '{dto.FYCode}' already exists.", "FYCode")
            End If

            ' Date Overlap Check
            Dim dateOverlap = existingFYs.Find(Function(fy) (dto.StartDate <= fy.EndDate AndAlso dto.EndDate >= fy.StartDate))
            If dateOverlap IsNot Nothing Then
                Throw New BusinessException($"Financial Year period ({dto.StartDate:yyyy-MM-dd} to {dto.EndDate:yyyy-MM-dd}) overlaps with existing Financial Year '{dateOverlap.FYCode}'.", "ERR_FY_OVERLAP")
            End If

            Dim entity As New FinancialYearEntity() With {
                .FYCode = dto.FYCode.Trim().ToUpper(),
                .StartDate = dto.StartDate,
                .EndDate = dto.EndDate,
                .IsCurrentFY = dto.IsCurrentFY,
                .IsLocked = False,
                .IsDeleted = False,
                .CreatedBy = createdBy
            }

            Dim newId = Await _fyRepo.AddAsync(entity)

            If dto.IsCurrentFY Then
                Await _fyRepo.SetActiveAsync(newId, createdBy)
            End If

            If _appLogger IsNot Nothing Then
                _appLogger.LogInfo($"Financial Year '{entity.FYCode}' (ID: {newId}) created.", "FinancialYearService")
            End If
            If _auditLogger IsNot Nothing Then
                Await _auditLogger.LogAuditAsync(createdBy, "FINANCIAL_YEAR_CREATE", "FinancialYearService", $"Financial Year '{entity.FYCode}' (ID: {newId}) created.")
            End If

            Return newId
        End Function

        Public Async Function UpdateFinancialYearAsync(dto As FinancialYearDto, modifiedBy As Integer) As Task(Of Boolean) Implements IFinancialYearService.UpdateFinancialYearAsync
            Dim existing = Await _fyRepo.GetByIdAsync(dto.FinancialYearId)
            If existing Is Nothing Then
                Throw New BusinessException($"Financial Year ID {dto.FinancialYearId} was not found.", "ERR_FY_NOT_FOUND")
            End If

            If existing.IsLocked Then
                Throw New BusinessException($"Financial Year '{existing.FYCode}' is locked and cannot be modified.", "ERR_FY_LOCKED")
            End If

            Dim valCode = CommonValidators.ValidateRequired(dto.FYCode, "Financial Year Code")
            If Not valCode.IsValid Then Throw New ValidationException(valCode.Errors(0), "FYCode")

            If dto.StartDate >= dto.EndDate Then
                Throw New ValidationException("Start Date must be strictly earlier than End Date.", "StartDate")
            End If

            Dim existingFYs = Await _fyRepo.GetAllAsync(includeDeleted:=False)

            ' Duplicate Code Check
            Dim duplicateCode = existingFYs.Find(Function(fy) fy.FinancialYearId <> dto.FinancialYearId AndAlso fy.FYCode.Equals(dto.FYCode.Trim(), StringComparison.OrdinalIgnoreCase))
            If duplicateCode IsNot Nothing Then
                Throw New ValidationException($"Financial Year Code '{dto.FYCode}' is already in use by another record.", "FYCode")
            End If

            ' Date Overlap Check
            Dim dateOverlap = existingFYs.Find(Function(fy) fy.FinancialYearId <> dto.FinancialYearId AndAlso (dto.StartDate <= fy.EndDate AndAlso dto.EndDate >= fy.StartDate))
            If dateOverlap IsNot Nothing Then
                Throw New BusinessException($"Financial Year period ({dto.StartDate:yyyy-MM-dd} to {dto.EndDate:yyyy-MM-dd}) overlaps with existing Financial Year '{dateOverlap.FYCode}'.", "ERR_FY_OVERLAP")
            End If

            existing.FYCode = dto.FYCode.Trim().ToUpper()
            existing.StartDate = dto.StartDate
            existing.EndDate = dto.EndDate
            existing.IsCurrentFY = dto.IsCurrentFY
            existing.ModifiedBy = modifiedBy

            Dim success = Await _fyRepo.UpdateAsync(existing)
            If success Then
                If dto.IsCurrentFY Then
                    Await _fyRepo.SetActiveAsync(dto.FinancialYearId, modifiedBy)
                End If

                If _appLogger IsNot Nothing Then
                    _appLogger.LogInfo($"Financial Year ID {dto.FinancialYearId} updated.", "FinancialYearService")
                End If
                If _auditLogger IsNot Nothing Then
                    Await _auditLogger.LogAuditAsync(modifiedBy, "FINANCIAL_YEAR_UPDATE", "FinancialYearService", $"Financial Year '{existing.FYCode}' (ID: {dto.FinancialYearId}) updated.")
                End If
            End If

            Return success
        End Function

        Public Async Function ActivateFinancialYearAsync(financialYearId As Integer, modifiedBy As Integer) As Task(Of Boolean) Implements IFinancialYearService.ActivateFinancialYearAsync
            Dim existing = Await _fyRepo.GetByIdAsync(financialYearId)
            If existing Is Nothing Then
                Throw New BusinessException($"Financial Year ID {financialYearId} was not found or is deleted.", "ERR_FY_NOT_FOUND")
            End If

            Dim success = Await _fyRepo.SetActiveAsync(financialYearId, modifiedBy)
            If success Then
                If _appLogger IsNot Nothing Then
                    _appLogger.LogInfo($"Financial Year '{existing.FYCode}' (ID: {financialYearId}) set as Active FY.", "FinancialYearService")
                End If
                If _auditLogger IsNot Nothing Then
                    Await _auditLogger.LogAuditAsync(modifiedBy, "FINANCIAL_YEAR_ACTIVATE", "FinancialYearService", $"Financial Year '{existing.FYCode}' (ID: {financialYearId}) activated.")
                End If
            End If

            Return success
        End Function

        Public Async Function LockFinancialYearAsync(financialYearId As Integer, isLocked As Boolean, modifiedBy As Integer) As Task(Of Boolean) Implements IFinancialYearService.LockFinancialYearAsync
            Dim existing = Await _fyRepo.GetByIdAsync(financialYearId)
            If existing Is Nothing Then
                Throw New BusinessException($"Financial Year ID {financialYearId} was not found or is deleted.", "ERR_FY_NOT_FOUND")
            End If

            Dim success = Await _fyRepo.SetLockStateAsync(financialYearId, isLocked, modifiedBy)
            If success Then
                Dim actionCode = If(isLocked, "FINANCIAL_YEAR_LOCK", "FINANCIAL_YEAR_UNLOCK")
                Dim statusText = If(isLocked, "LOCKED", "UNLOCKED")

                If _appLogger IsNot Nothing Then
                    _appLogger.LogInfo($"Financial Year '{existing.FYCode}' (ID: {financialYearId}) {statusText}.", "FinancialYearService")
                End If
                If _auditLogger IsNot Nothing Then
                    Await _auditLogger.LogAuditAsync(modifiedBy, actionCode, "FinancialYearService", $"Financial Year '{existing.FYCode}' (ID: {financialYearId}) state changed to {statusText}.")
                End If
            End If

            Return success
        End Function

        Private Function MapToDto(entity As FinancialYearEntity) As FinancialYearDto
            Return New FinancialYearDto() With {
                .FinancialYearId = entity.FinancialYearId,
                .FYCode = entity.FYCode,
                .StartDate = entity.StartDate,
                .EndDate = entity.EndDate,
                .IsCurrentFY = entity.IsCurrentFY,
                .IsLocked = entity.IsLocked,
                .IsDeleted = entity.IsDeleted
            }
        End Function
    End Class
End Namespace
