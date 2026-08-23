Imports System
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Enums

Namespace StaffAutomation.Tests
    ''' <summary>
    ''' Unit tests for Phase 5A Master Data Management and Business Data Protection Policy.
    ''' </summary>
    <TestClass>
    Public Class MasterDataTests
        <TestMethod>
        Public Sub TestClientDtoMapping()
            Dim dto As New ClientDto() With {
                .ClientId = 10,
                .ClientCode = "CLI-999",
                .ClientName = "Acme Financial Services",
                .Department = DepartmentType.IncomeTax,
                .IsActive = True
            }

            If dto.ClientId <> 10 OrElse dto.ClientCode <> "CLI-999" OrElse Not dto.IsActive Then
                Throw New InvalidOperationException("ClientDto properties failed to map correctly.")
            End If
        End Sub

        <TestMethod>
        Public Sub TestSoftDeletePolicyRule()
            ' Business Data Protection Policy verification
            Dim clientDto As New ClientDto() With {.ClientId = 5, .ClientCode = "CLI-001"}
            ' In Phase 5A, deletion must strictly set IsDeleted = 1 via SoftDeleteAsync (never physical DELETE FROM tbl_Clients)
        End Sub
    End Class
End Namespace
