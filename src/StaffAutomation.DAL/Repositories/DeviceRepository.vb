Imports System
Imports System.Collections.Generic
Imports System.Data
Imports Microsoft.Data.SqlClient
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.DAL.Interfaces

Namespace Repositories
    Public Class DeviceRepository
        Implements IDeviceRepository

        Private ReadOnly _sqlHelper As ISqlHelper

        Public Sub New(sqlHelper As ISqlHelper)
            _sqlHelper = sqlHelper
        End Sub

        Public Async Function GetByIdAsync(deviceId As Integer) As Task(Of UserDeviceEntity) Implements IDeviceRepository.GetByIdAsync
            Const query As String = "SELECT DeviceId, UserId, DeviceGuid, HardwareFingerprint, MachineName, OSVersion, Status, RequestedOn, ApprovedOn, ApprovedBy, RevokedOn, RevokedBy, RevocationReason, LastActiveOn " &
                                   "FROM dbo.tbl_UserDevices WHERE DeviceId = @DeviceId;"
            Dim params As SqlParameter() = {New SqlParameter("@DeviceId", deviceId)}
            Dim list = Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapUserDeviceEntity)
            Return If(list.Count > 0, list(0), Nothing)
        End Function

        Public Async Function GetDeviceByGuidAsync(userId As Integer, deviceGuid As Guid) As Task(Of UserDeviceEntity) Implements IDeviceRepository.GetDeviceByGuidAsync
            Const query As String = "SELECT DeviceId, UserId, DeviceGuid, HardwareFingerprint, MachineName, OSVersion, Status, RequestedOn, ApprovedOn, ApprovedBy, RevokedOn, RevokedBy, RevocationReason, LastActiveOn " &
                                   "FROM dbo.tbl_UserDevices WHERE UserId = @UserId AND DeviceGuid = @DeviceGuid;"
            Dim params As SqlParameter() = {
                New SqlParameter("@UserId", userId),
                New SqlParameter("@DeviceGuid", deviceGuid)
            }
            Dim list = Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapUserDeviceEntity)
            Return If(list.Count > 0, list(0), Nothing)
        End Function

        Public Async Function GetDeviceByFingerprintAsync(userId As Integer, hardwareFingerprint As String) As Task(Of UserDeviceEntity) Implements IDeviceRepository.GetDeviceByFingerprintAsync
            Const query As String = "SELECT DeviceId, UserId, DeviceGuid, HardwareFingerprint, MachineName, OSVersion, Status, RequestedOn, ApprovedOn, ApprovedBy, RevokedOn, RevokedBy, RevocationReason, LastActiveOn " &
                                   "FROM dbo.tbl_UserDevices WHERE UserId = @UserId AND HardwareFingerprint = @HardwareFingerprint;"
            Dim params As SqlParameter() = {
                New SqlParameter("@UserId", userId),
                New SqlParameter("@HardwareFingerprint", hardwareFingerprint)
            }
            Dim list = Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapUserDeviceEntity)
            Return If(list.Count > 0, list(0), Nothing)
        End Function

        Public Async Function CreateDeviceAsync(device As UserDeviceEntity) As Task(Of Integer) Implements IDeviceRepository.CreateDeviceAsync
            Const query As String = "INSERT INTO dbo.tbl_UserDevices (UserId, DeviceGuid, HardwareFingerprint, MachineName, OSVersion, Status, RequestedOn) " &
                                   "VALUES (@UserId, @DeviceGuid, @HardwareFingerprint, @MachineName, @OSVersion, @Status, @RequestedOn); " &
                                   "SELECT SCOPE_IDENTITY();"
            Dim params As SqlParameter() = {
                New SqlParameter("@UserId", device.UserId),
                New SqlParameter("@DeviceGuid", device.DeviceGuid),
                New SqlParameter("@HardwareFingerprint", device.HardwareFingerprint),
                New SqlParameter("@MachineName", device.MachineName),
                New SqlParameter("@OSVersion", If(device.OSVersion, CObj(DBNull.Value))),
                New SqlParameter("@Status", device.Status),
                New SqlParameter("@RequestedOn", device.RequestedOn)
            }
            Return Await _sqlHelper.ExecuteScalarAsync(Of Integer)(query, params)
        End Function

        Public Async Function GetDevicesForUserAsync(userId As Integer) As Task(Of List(Of UserDeviceEntity)) Implements IDeviceRepository.GetDevicesForUserAsync
            Const query As String = "SELECT DeviceId, UserId, DeviceGuid, HardwareFingerprint, MachineName, OSVersion, Status, RequestedOn, ApprovedOn, ApprovedBy, RevokedOn, RevokedBy, RevocationReason, LastActiveOn " &
                                   "FROM dbo.tbl_UserDevices WHERE UserId = @UserId ORDER BY RequestedOn DESC;"
            Dim params As SqlParameter() = {New SqlParameter("@UserId", userId)}
            Return Await _sqlHelper.ExecuteReaderAsync(query, params, AddressOf MapUserDeviceEntity)
        End Function

        Public Async Function GetAllDevicesAsync() As Task(Of List(Of UserDeviceEntity)) Implements IDeviceRepository.GetAllDevicesAsync
            Const query As String = "SELECT DeviceId, UserId, DeviceGuid, HardwareFingerprint, MachineName, OSVersion, Status, RequestedOn, ApprovedOn, ApprovedBy, RevokedOn, RevokedBy, RevocationReason, LastActiveOn " &
                                   "FROM dbo.tbl_UserDevices ORDER BY RequestedOn DESC;"
            Return Await _sqlHelper.ExecuteReaderAsync(query, Nothing, AddressOf MapUserDeviceEntity)
        End Function

        Public Async Function UpdateDeviceAsync(device As UserDeviceEntity) As Task(Of Boolean) Implements IDeviceRepository.UpdateDeviceAsync
            Const query As String = "UPDATE dbo.tbl_UserDevices SET " &
                                   "Status = @Status, ApprovedOn = @ApprovedOn, ApprovedBy = @ApprovedBy, " &
                                   "RevokedOn = @RevokedOn, RevokedBy = @RevokedBy, RevocationReason = @RevocationReason, " &
                                   "LastActiveOn = @LastActiveOn " &
                                   "WHERE DeviceId = @DeviceId;"
            Dim params As SqlParameter() = {
                New SqlParameter("@Status", device.Status),
                New SqlParameter("@ApprovedOn", If(device.ApprovedOn.HasValue, CObj(device.ApprovedOn.Value), DBNull.Value)),
                New SqlParameter("@ApprovedBy", If(device.ApprovedBy.HasValue, CObj(device.ApprovedBy.Value), DBNull.Value)),
                New SqlParameter("@RevokedOn", If(device.RevokedOn.HasValue, CObj(device.RevokedOn.Value), DBNull.Value)),
                New SqlParameter("@RevokedBy", If(device.RevokedBy.HasValue, CObj(device.RevokedBy.Value), DBNull.Value)),
                New SqlParameter("@RevocationReason", If(device.RevocationReason, CObj(DBNull.Value))),
                New SqlParameter("@LastActiveOn", If(device.LastActiveOn.HasValue, CObj(device.LastActiveOn.Value), DBNull.Value)),
                New SqlParameter("@DeviceId", device.DeviceId)
            }
            Dim rows = Await _sqlHelper.ExecuteNonQueryAsync(query, params)
            Return rows > 0
        End Function

        Private Function MapUserDeviceEntity(reader As IDataReader) As UserDeviceEntity
            Return New UserDeviceEntity() With {
                .DeviceId = Convert.ToInt32(reader("DeviceId")),
                .UserId = Convert.ToInt32(reader("UserId")),
                .DeviceGuid = CType(reader("DeviceGuid"), Guid),
                .HardwareFingerprint = Convert.ToString(reader("HardwareFingerprint")),
                .MachineName = Convert.ToString(reader("MachineName")),
                .OSVersion = If(reader("OSVersion") Is DBNull.Value, Nothing, Convert.ToString(reader("OSVersion"))),
                .Status = Convert.ToString(reader("Status")),
                .RequestedOn = Convert.ToDateTime(reader("RequestedOn")),
                .ApprovedOn = If(reader("ApprovedOn") Is DBNull.Value, CType(Nothing, DateTime?), Convert.ToDateTime(reader("ApprovedOn"))),
                .ApprovedBy = If(reader("ApprovedBy") Is DBNull.Value, CType(Nothing, Integer?), Convert.ToInt32(reader("ApprovedBy"))),
                .RevokedOn = If(reader("RevokedOn") Is DBNull.Value, CType(Nothing, DateTime?), Convert.ToDateTime(reader("RevokedOn"))),
                .RevokedBy = If(reader("RevokedBy") Is DBNull.Value, CType(Nothing, Integer?), Convert.ToInt32(reader("RevokedBy"))),
                .RevocationReason = If(reader("RevocationReason") Is DBNull.Value, Nothing, Convert.ToString(reader("RevocationReason"))),
                .LastActiveOn = If(reader("LastActiveOn") Is DBNull.Value, CType(Nothing, DateTime?), Convert.ToDateTime(reader("LastActiveOn")))
            }
        End Function
    End Class
End Namespace
