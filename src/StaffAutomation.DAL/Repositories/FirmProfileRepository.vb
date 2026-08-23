Imports System
Imports System.Data
Imports Microsoft.Data.SqlClient
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.DAL.Interfaces

Namespace Repositories
    ''' <summary>
    ''' Data Access Repository for singleton CA Firm Master Profile executing parameterized T-SQL queries.
    ''' Manages persistence for dbo.tbl_FirmProfile with a single-row constraint (FirmId = 1).
    ''' </summary>
    Public Class FirmProfileRepository
        Implements IFirmProfileRepository

        Private ReadOnly _sqlHelper As ISqlHelper

        Public Sub New(sqlHelper As ISqlHelper)
            _sqlHelper = sqlHelper
        End Sub

        Public Async Function GetProfileAsync() As Task(Of FirmProfileEntity) Implements IFirmProfileRepository.GetProfileAsync
            Const query As String = "SELECT FirmId, FirmName, LegalName, FRN, PAN, GSTIN, AddressLine1, AddressLine2, City, StateName, Pincode, Phone, Email, Website, HeaderFormatJson, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy " &
                                   "FROM dbo.tbl_FirmProfile WHERE FirmId = 1;"
            Dim list = Await _sqlHelper.ExecuteReaderAsync(query, Nothing, AddressOf MapFirmProfileEntity)
            Return If(list.Count > 0, list(0), Nothing)
        End Function

        Public Async Function SaveProfileAsync(profile As FirmProfileEntity) As Task(Of Boolean) Implements IFirmProfileRepository.SaveProfileAsync
            Const query As String = "IF EXISTS (SELECT 1 FROM dbo.tbl_FirmProfile WHERE FirmId = 1) " &
                                   "BEGIN " &
                                   "    UPDATE dbo.tbl_FirmProfile " &
                                   "    SET FirmName = @FirmName, LegalName = @LegalName, FRN = @FRN, PAN = @PAN, GSTIN = @GSTIN, " &
                                   "        AddressLine1 = @AddressLine1, AddressLine2 = @AddressLine2, City = @City, StateName = @StateName, " &
                                   "        Pincode = @Pincode, Phone = @Phone, Email = @Email, Website = @Website, HeaderFormatJson = @HeaderFormatJson, " &
                                   "        ModifiedOn = GETUTCDATE(), ModifiedBy = @ModifiedBy " &
                                   "    WHERE FirmId = 1; " &
                                   "END " &
                                   "ELSE " &
                                   "BEGIN " &
                                   "    SET IDENTITY_INSERT dbo.tbl_FirmProfile ON; " &
                                   "    INSERT INTO dbo.tbl_FirmProfile (FirmId, FirmName, LegalName, FRN, PAN, GSTIN, AddressLine1, AddressLine2, City, StateName, Pincode, Phone, Email, Website, HeaderFormatJson, CreatedOn, CreatedBy) " &
                                   "    VALUES (1, @FirmName, @LegalName, @FRN, @PAN, @GSTIN, @AddressLine1, @AddressLine2, @City, @StateName, @Pincode, @Phone, @Email, @Website, @HeaderFormatJson, GETUTCDATE(), @CreatedBy); " &
                                   "    SET IDENTITY_INSERT dbo.tbl_FirmProfile OFF; " &
                                   "END;"

            Dim params As SqlParameter() = {
                New SqlParameter("@FirmName", profile.FirmName),
                New SqlParameter("@LegalName", profile.LegalName),
                New SqlParameter("@FRN", profile.FRN),
                New SqlParameter("@PAN", profile.PAN),
                New SqlParameter("@GSTIN", If(String.IsNullOrEmpty(profile.GSTIN), CObj(DBNull.Value), profile.GSTIN)),
                New SqlParameter("@AddressLine1", profile.AddressLine1),
                New SqlParameter("@AddressLine2", If(String.IsNullOrEmpty(profile.AddressLine2), CObj(DBNull.Value), profile.AddressLine2)),
                New SqlParameter("@City", profile.City),
                New SqlParameter("@StateName", profile.StateName),
                New SqlParameter("@Pincode", profile.Pincode),
                New SqlParameter("@Phone", profile.Phone),
                New SqlParameter("@Email", profile.Email),
                New SqlParameter("@Website", If(String.IsNullOrEmpty(profile.Website), CObj(DBNull.Value), profile.Website)),
                New SqlParameter("@HeaderFormatJson", If(String.IsNullOrEmpty(profile.HeaderFormatJson), CObj(DBNull.Value), profile.HeaderFormatJson)),
                New SqlParameter("@CreatedBy", profile.CreatedBy),
                New SqlParameter("@ModifiedBy", If(profile.ModifiedBy.HasValue, CObj(profile.ModifiedBy.Value), DBNull.Value))
            }

            Dim rows = Await _sqlHelper.ExecuteNonQueryAsync(query, params)
            Return rows > 0
        End Function

        Private Function MapFirmProfileEntity(reader As IDataReader) As FirmProfileEntity
            Return New FirmProfileEntity() With {
                .FirmId = Convert.ToInt32(reader("FirmId")),
                .FirmName = Convert.ToString(reader("FirmName")),
                .LegalName = Convert.ToString(reader("LegalName")),
                .FRN = Convert.ToString(reader("FRN")),
                .PAN = Convert.ToString(reader("PAN")),
                .GSTIN = If(reader("GSTIN") Is DBNull.Value, String.Empty, Convert.ToString(reader("GSTIN"))),
                .AddressLine1 = Convert.ToString(reader("AddressLine1")),
                .AddressLine2 = If(reader("AddressLine2") Is DBNull.Value, String.Empty, Convert.ToString(reader("AddressLine2"))),
                .City = Convert.ToString(reader("City")),
                .StateName = Convert.ToString(reader("StateName")),
                .Pincode = Convert.ToString(reader("Pincode")),
                .Phone = Convert.ToString(reader("Phone")),
                .Email = Convert.ToString(reader("Email")),
                .Website = If(reader("Website") Is DBNull.Value, String.Empty, Convert.ToString(reader("Website"))),
                .HeaderFormatJson = If(reader("HeaderFormatJson") Is DBNull.Value, String.Empty, Convert.ToString(reader("HeaderFormatJson"))),
                .CreatedOn = Convert.ToDateTime(reader("CreatedOn")),
                .CreatedBy = Convert.ToInt32(reader("CreatedBy")),
                .ModifiedOn = If(reader("ModifiedOn") Is DBNull.Value, CType(Nothing, Nullable(Of DateTime)), Convert.ToDateTime(reader("ModifiedOn"))),
                .ModifiedBy = If(reader("ModifiedBy") Is DBNull.Value, CType(Nothing, Nullable(Of Integer)), Convert.ToInt32(reader("ModifiedBy")))
            }
        End Function
    End Class
End Namespace
