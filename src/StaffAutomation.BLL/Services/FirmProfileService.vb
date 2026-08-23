Imports System
Imports System.Text.RegularExpressions
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
    ''' Business Logic Service for CA Firm Master Profile management.
    ''' Implements IFirmProfileService enforcing validation rules, singleton FirmId = 1, and audit logging.
    ''' </summary>
    Public Class FirmProfileService
        Implements IFirmProfileService

        Private ReadOnly _firmRepo As IFirmProfileRepository
        Private ReadOnly _appLogger As IAppLogger
        Private ReadOnly _auditLogger As IAuditLogger

        Public Sub New(firmRepo As IFirmProfileRepository, Optional appLogger As IAppLogger = Nothing, Optional auditLogger As IAuditLogger = Nothing)
            _firmRepo = firmRepo
            _appLogger = appLogger
            _auditLogger = auditLogger
        End Sub

        Public Async Function GetFirmProfileAsync() As Task(Of FirmProfileDto) Implements IFirmProfileService.GetFirmProfileAsync
            Dim entity = Await _firmRepo.GetProfileAsync()
            If entity Is Nothing Then
                Return New FirmProfileDto() With {.FirmId = 1}
            End If
            Return MapToDto(entity)
        End Function

        Public Async Function SaveFirmProfileAsync(dto As FirmProfileDto, modifiedBy As Integer) As Task(Of Boolean) Implements IFirmProfileService.SaveFirmProfileAsync
            ' Business Validation
            Dim valFirmName = CommonValidators.ValidateRequired(dto.FirmName, "Firm Name")
            If Not valFirmName.IsValid Then Throw New ValidationException(valFirmName.Errors(0), "FirmName")

            Dim valLegalName = CommonValidators.ValidateRequired(dto.LegalName, "Legal Name")
            If Not valLegalName.IsValid Then Throw New ValidationException(valLegalName.Errors(0), "LegalName")

            Dim valFrn = CommonValidators.ValidateRequired(dto.FRN, "Firm Registration Number (FRN)")
            If Not valFrn.IsValid Then Throw New ValidationException(valFrn.Errors(0), "FRN")

            Dim valPan = CommonValidators.ValidateRequired(dto.PAN, "PAN")
            If Not valPan.IsValid Then Throw New ValidationException(valPan.Errors(0), "PAN")

            Dim valAddr = CommonValidators.ValidateRequired(dto.AddressLine1, "Address Line 1")
            If Not valAddr.IsValid Then Throw New ValidationException(valAddr.Errors(0), "AddressLine1")

            Dim valCity = CommonValidators.ValidateRequired(dto.City, "City")
            If Not valCity.IsValid Then Throw New ValidationException(valCity.Errors(0), "City")

            Dim valState = CommonValidators.ValidateRequired(dto.StateName, "State")
            If Not valState.IsValid Then Throw New ValidationException(valState.Errors(0), "StateName")

            Dim valPincode = CommonValidators.ValidateRequired(dto.Pincode, "Pincode")
            If Not valPincode.IsValid Then Throw New ValidationException(valPincode.Errors(0), "Pincode")

            Dim valPhone = CommonValidators.ValidateRequired(dto.Phone, "Phone")
            If Not valPhone.IsValid Then Throw New ValidationException(valPhone.Errors(0), "Phone")

            Dim valEmail = CommonValidators.ValidateRequired(dto.Email, "Email")
            If Not valEmail.IsValid Then Throw New ValidationException(valEmail.Errors(0), "Email")

            ' Format Validations (when populated)
            If Not String.IsNullOrWhiteSpace(dto.Email) Then
                Dim emailPattern As String = "^[^@\s]+@[^@\s]+\.[^@\s]+$"
                If Not Regex.IsMatch(dto.Email.Trim(), emailPattern) Then
                    Throw New ValidationException("Invalid email format for Firm Profile.", "Email")
                End If
            End If

            If Not String.IsNullOrWhiteSpace(dto.PAN) Then
                Dim panPattern As String = "^[A-Z]{5}[0-9]{4}[A-Z]{1}$"
                If Not Regex.IsMatch(dto.PAN.Trim().ToUpper(), panPattern) Then
                    Throw New ValidationException("Invalid Income Tax PAN format (expected e.g. ABCDE1234F).", "PAN")
                End If
            End If

            If Not String.IsNullOrWhiteSpace(dto.GSTIN) Then
                Dim gstinPattern As String = "^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$"
                If Not Regex.IsMatch(dto.GSTIN.Trim().ToUpper(), gstinPattern) Then
                    Throw New ValidationException("Invalid GSTIN format (expected 15-character GSTIN).", "GSTIN")
                End If
            End If

            ' Construct Entity enforcing FirmId = 1
            Dim entity As New FirmProfileEntity() With {
                .FirmId = 1,
                .FirmName = dto.FirmName.Trim(),
                .LegalName = dto.LegalName.Trim(),
                .FRN = dto.FRN.Trim().ToUpper(),
                .PAN = dto.PAN.Trim().ToUpper(),
                .GSTIN = If(String.IsNullOrWhiteSpace(dto.GSTIN), String.Empty, dto.GSTIN.Trim().ToUpper()),
                .AddressLine1 = dto.AddressLine1.Trim(),
                .AddressLine2 = If(String.IsNullOrWhiteSpace(dto.AddressLine2), String.Empty, dto.AddressLine2.Trim()),
                .City = dto.City.Trim(),
                .StateName = dto.StateName.Trim(),
                .Pincode = dto.Pincode.Trim(),
                .Phone = dto.Phone.Trim(),
                .Email = dto.Email.Trim().ToLower(),
                .Website = If(String.IsNullOrWhiteSpace(dto.Website), String.Empty, dto.Website.Trim()),
                .HeaderFormatJson = If(String.IsNullOrWhiteSpace(dto.HeaderFormatJson), String.Empty, dto.HeaderFormatJson.Trim()),
                .CreatedBy = modifiedBy,
                .ModifiedBy = modifiedBy
            }

            Dim success = Await _firmRepo.SaveProfileAsync(entity)
            If success Then
                If _appLogger IsNot Nothing Then
                    _appLogger.LogInfo($"Firm Profile saved: {entity.FirmName} (FRN: {entity.FRN}).", "FirmProfileService")
                End If
                If _auditLogger IsNot Nothing Then
                    Await _auditLogger.LogAuditAsync(modifiedBy, "FIRM_PROFILE_UPDATE", "FirmProfileService", $"Firm Profile updated: {entity.FirmName} (FRN: {entity.FRN})")
                End If
            End If

            Return success
        End Function

        Private Function MapToDto(entity As FirmProfileEntity) As FirmProfileDto
            Return New FirmProfileDto() With {
                .FirmId = entity.FirmId,
                .FirmName = entity.FirmName,
                .LegalName = entity.LegalName,
                .FRN = entity.FRN,
                .PAN = entity.PAN,
                .GSTIN = entity.GSTIN,
                .AddressLine1 = entity.AddressLine1,
                .AddressLine2 = entity.AddressLine2,
                .City = entity.City,
                .StateName = entity.StateName,
                .Pincode = entity.Pincode,
                .Phone = entity.Phone,
                .Email = entity.Email,
                .Website = entity.Website,
                .HeaderFormatJson = entity.HeaderFormatJson
            }
        End Function
    End Class
End Namespace
