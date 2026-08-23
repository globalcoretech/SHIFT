Imports System
Imports System.Collections.Generic
Imports System.Data
Imports Microsoft.Data.SqlClient
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.DAL.Interfaces

Namespace Repositories
    ''' <summary>
    ''' Data Access Repository implementing T-SQL persistence for dbo.tbl_Attendance.
    ''' </summary>
    Public Class AttendanceRepository
        Implements IAttendanceRepository

        Private ReadOnly _sqlHelper As ISqlHelper

        Public Sub New(sqlHelper As ISqlHelper)
            _sqlHelper = sqlHelper
        End Sub

        Private Shared _columnEnsured As Boolean = False

        Private Async Function EnsureBreakStartTimeColumnExistsAsync() As Task
            If _columnEnsured Then Return
            Try
                Const alterQuery As String = "IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'tbl_Attendance') " &
                                            "BEGIN " &
                                            "    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tbl_Attendance') AND name = N'BreakStartTime') " &
                                            "    BEGIN " &
                                            "        ALTER TABLE dbo.tbl_Attendance ADD BreakStartTime DATETIME2 NULL; " &
                                            "    END " &
                                            "END;"
                Await _sqlHelper.ExecuteNonQueryAsync(alterQuery, Array.Empty(Of SqlParameter)())
                _columnEnsured = True
            Catch ex As Exception
                ' Suppress if schema alter is managed externally or permission restricted
            End Try
        End Function

        Public Async Function GetTodayAttendanceAsync(userId As Integer, attendanceDate As DateTime) As Task(Of AttendanceEntity) Implements IAttendanceRepository.GetTodayAttendanceAsync
            Await EnsureBreakStartTimeColumnExistsAsync()
            Const query As String = "SELECT AttendanceId, UserId, AttendanceDate, ClockInTime, ClockOutTime, BreakStartTime, TotalBreakMinutes, TotalWorkingMinutes, Status, ClientIP, CreatedOn, CreatedBy " &
                                   "FROM dbo.tbl_Attendance WHERE UserId = @UserId AND AttendanceDate = @AttendanceDate;"
            Dim params As SqlParameter() = {
                New SqlParameter("@UserId", userId),
                New SqlParameter("@AttendanceDate", attendanceDate.Date)
            }
            Dim list = Await _sqlHelper.ExecuteReaderAsync(Of AttendanceEntity)(query, params, AddressOf MapAttendanceEntity)
            Return If(list.Count > 0, list(0), Nothing)
        End Function

        Public Async Function GetAttendanceHistoryAsync(Optional startDate As Nullable(Of DateTime) = Nothing, Optional endDate As Nullable(Of DateTime) = Nothing) As Task(Of List(Of AttendanceEntity)) Implements IAttendanceRepository.GetAttendanceHistoryAsync
            Dim query As String = "SELECT AttendanceId, UserId, AttendanceDate, ClockInTime, ClockOutTime, BreakStartTime, TotalBreakMinutes, TotalWorkingMinutes, Status, ClientIP, CreatedOn, CreatedBy " &
                                  "FROM dbo.tbl_Attendance "
            Dim params As New List(Of SqlParameter)()
            Dim whereClause As New List(Of String)()

            If startDate.HasValue Then
                whereClause.Add("AttendanceDate >= @StartDate")
                params.Add(New SqlParameter("@StartDate", startDate.Value.Date))
            End If

            If endDate.HasValue Then
                whereClause.Add("AttendanceDate <= @EndDate")
                params.Add(New SqlParameter("@EndDate", endDate.Value.Date))
            End If

            If whereClause.Count > 0 Then
                query &= "WHERE " & String.Join(" AND ", whereClause) & " "
            End If

            query &= "ORDER BY AttendanceDate DESC, ClockInTime DESC;"

            Return Await _sqlHelper.ExecuteReaderAsync(Of AttendanceEntity)(query, params.ToArray(), AddressOf MapAttendanceEntity)
        End Function

        Public Async Function ClockInAsync(attendance As AttendanceEntity) As Task(Of Integer) Implements IAttendanceRepository.ClockInAsync
            Await EnsureBreakStartTimeColumnExistsAsync()
            Const query As String = "INSERT INTO dbo.tbl_Attendance (UserId, AttendanceDate, ClockInTime, ClockOutTime, BreakStartTime, TotalBreakMinutes, TotalWorkingMinutes, Status, ClientIP, CreatedOn, CreatedBy) " &
                                   "VALUES (@UserId, @AttendanceDate, @ClockInTime, NULL, NULL, 0, 0, @Status, @ClientIP, GETUTCDATE(), @CreatedBy); " &
                                   "SELECT SCOPE_IDENTITY();"
            Dim params As SqlParameter() = {
                New SqlParameter("@UserId", attendance.UserId),
                New SqlParameter("@AttendanceDate", attendance.AttendanceDate.Date),
                New SqlParameter("@ClockInTime", attendance.ClockInTime),
                New SqlParameter("@Status", If(Not String.IsNullOrEmpty(attendance.Status), attendance.Status, "Present")),
                New SqlParameter("@ClientIP", If(Not String.IsNullOrEmpty(attendance.ClientIP), attendance.ClientIP, "127.0.0.1")),
                New SqlParameter("@CreatedBy", attendance.CreatedBy)
            }
            Dim result = Await _sqlHelper.ExecuteScalarAsync(Of Object)(query, params)
            Return Convert.ToInt32(result)
        End Function

        Public Async Function ClockOutAsync(attendanceId As Integer, clockOutTime As DateTime, totalBreakMinutes As Integer, Optional newStatus As String = Nothing) As Task(Of Boolean) Implements IAttendanceRepository.ClockOutAsync
            Const query As String = "UPDATE dbo.tbl_Attendance " &
                                   "SET ClockOutTime = @ClockOutTime, " &
                                   "    BreakStartTime = NULL, " &
                                   "    TotalBreakMinutes = @TotalBreakMinutes, " &
                                   "    TotalWorkingMinutes = CASE WHEN DATEDIFF(MINUTE, ClockInTime, @ClockOutTime) - @TotalBreakMinutes < 0 THEN 0 ELSE DATEDIFF(MINUTE, ClockInTime, @ClockOutTime) - @TotalBreakMinutes END, " &
                                   "    Status = ISNULL(@Status, Status) " &
                                   "WHERE AttendanceId = @AttendanceId;"
            Dim params As SqlParameter() = {
                New SqlParameter("@ClockOutTime", clockOutTime),
                New SqlParameter("@TotalBreakMinutes", totalBreakMinutes),
                New SqlParameter("@Status", If(String.IsNullOrEmpty(newStatus), CType(DBNull.Value, Object), newStatus)),
                New SqlParameter("@AttendanceId", attendanceId)
            }
            Dim rowsAffected = Await _sqlHelper.ExecuteNonQueryAsync(query, params)
            Return rowsAffected > 0
        End Function

        Public Async Function StartLunchBreakAsync(attendanceId As Integer, breakStartTime As DateTime) As Task(Of Boolean) Implements IAttendanceRepository.StartLunchBreakAsync
            Const query As String = "UPDATE dbo.tbl_Attendance SET BreakStartTime = @BreakStartTime WHERE AttendanceId = @AttendanceId;"
            Dim params As SqlParameter() = {
                New SqlParameter("@BreakStartTime", breakStartTime),
                New SqlParameter("@AttendanceId", attendanceId)
            }
            Dim rowsAffected = Await _sqlHelper.ExecuteNonQueryAsync(query, params)
            Return rowsAffected > 0
        End Function

        Public Async Function EndLunchBreakAsync(attendanceId As Integer, totalBreakMinutes As Integer) As Task(Of Boolean) Implements IAttendanceRepository.EndLunchBreakAsync
            Const query As String = "UPDATE dbo.tbl_Attendance SET TotalBreakMinutes = @TotalBreakMinutes, BreakStartTime = NULL WHERE AttendanceId = @AttendanceId;"
            Dim params As SqlParameter() = {
                New SqlParameter("@TotalBreakMinutes", totalBreakMinutes),
                New SqlParameter("@AttendanceId", attendanceId)
            }
            Dim rowsAffected = Await _sqlHelper.ExecuteNonQueryAsync(query, params)
            Return rowsAffected > 0
        End Function

        Public Async Function ResetTodayAttendanceAsync(userId As Integer, attendanceDate As DateTime) As Task(Of Boolean) Implements IAttendanceRepository.ResetTodayAttendanceAsync
            Const query As String = "DELETE FROM dbo.tbl_Attendance WHERE UserId = @UserId AND AttendanceDate = @AttendanceDate;"
            Dim params As SqlParameter() = {
                New SqlParameter("@UserId", userId),
                New SqlParameter("@AttendanceDate", attendanceDate.Date)
            }
            Dim rowsAffected = Await _sqlHelper.ExecuteNonQueryAsync(query, params)
            Return rowsAffected > 0
        End Function

        Public Async Function GetUserAttendanceHistoryAsync(userId As Integer, Optional startDate As Nullable(Of DateTime) = Nothing, Optional endDate As Nullable(Of DateTime) = Nothing) As Task(Of List(Of AttendanceEntity)) Implements IAttendanceRepository.GetUserAttendanceHistoryAsync
            Dim query As String = "SELECT AttendanceId, UserId, AttendanceDate, ClockInTime, ClockOutTime, BreakStartTime, TotalBreakMinutes, TotalWorkingMinutes, Status, ClientIP, CreatedOn, CreatedBy " &
                                  "FROM dbo.tbl_Attendance WHERE UserId = @UserId "
            Dim params As New List(Of SqlParameter) From {
                New SqlParameter("@UserId", userId)
            }

            If startDate.HasValue Then
                query &= "AND AttendanceDate >= @StartDate "
                params.Add(New SqlParameter("@StartDate", startDate.Value.Date))
            End If

            If endDate.HasValue Then
                query &= "AND AttendanceDate <= @EndDate "
                params.Add(New SqlParameter("@EndDate", endDate.Value.Date))
            End If

            query &= "ORDER BY AttendanceDate DESC, ClockInTime DESC;"

            Return Await _sqlHelper.ExecuteReaderAsync(Of AttendanceEntity)(query, params.ToArray(), AddressOf MapAttendanceEntity)
        End Function

        Public Async Function CorrectAttendanceByAdminAsync(attendanceId As Integer, clockInTime As DateTime, clockOutTime As Nullable(Of DateTime), totalBreakMinutes As Integer, status As String, reason As String, adminUserId As Integer) As Task(Of Boolean) Implements IAttendanceRepository.CorrectAttendanceByAdminAsync
            Const query As String = "UPDATE dbo.tbl_Attendance " &
                                   "SET ClockInTime = @ClockInTime, " &
                                   "    ClockOutTime = @ClockOutTime, " &
                                   "    TotalBreakMinutes = @TotalBreakMinutes, " &
                                   "    TotalWorkingMinutes = CASE WHEN @ClockOutTime IS NULL THEN 0 ELSE (CASE WHEN DATEDIFF(MINUTE, @ClockInTime, @ClockOutTime) - @TotalBreakMinutes < 0 THEN 0 ELSE DATEDIFF(MINUTE, @ClockInTime, @ClockOutTime) - @TotalBreakMinutes END) END, " &
                                   "    Status = @Status, " &
                                   "    IsManuallyCorrected = 1, " &
                                   "    CorrectionReason = @Reason, " &
                                   "    CorrectedBy = @AdminUserId, " &
                                   "    CorrectedOn = GETUTCDATE() " &
                                   "WHERE AttendanceId = @AttendanceId;"
            Dim params As SqlParameter() = {
                New SqlParameter("@ClockInTime", clockInTime),
                New SqlParameter("@ClockOutTime", If(clockOutTime.HasValue, CType(clockOutTime.Value, Object), DBNull.Value)),
                New SqlParameter("@TotalBreakMinutes", totalBreakMinutes),
                New SqlParameter("@Status", status),
                New SqlParameter("@Reason", If(String.IsNullOrEmpty(reason), "Admin Manual Correction", reason)),
                New SqlParameter("@AdminUserId", adminUserId),
                New SqlParameter("@AttendanceId", attendanceId)
            }
            Dim rowsAffected = Await _sqlHelper.ExecuteNonQueryAsync(query, params)
            Return rowsAffected > 0
        End Function

        Private Function MapAttendanceEntity(reader As IDataReader) As AttendanceEntity
            Dim breakStartVal As Nullable(Of DateTime) = Nothing
            Try
                If reader("BreakStartTime") IsNot DBNull.Value Then
                    breakStartVal = Convert.ToDateTime(reader("BreakStartTime"))
                End If
            Catch ex As Exception
                ' Column might not exist in old schemas during transition
            End Try

            Dim isCorrected As Boolean = False
            Dim corrReason As String = String.Empty
            Dim corrBy As Nullable(Of Integer) = Nothing
            Dim corrOn As Nullable(Of DateTime) = Nothing

            Try
                If reader("IsManuallyCorrected") IsNot DBNull.Value Then
                    isCorrected = Convert.ToBoolean(reader("IsManuallyCorrected"))
                End If
                If reader("CorrectionReason") IsNot DBNull.Value Then
                    corrReason = Convert.ToString(reader("CorrectionReason"))
                End If
                If reader("CorrectedBy") IsNot DBNull.Value Then
                    corrBy = Convert.ToInt32(reader("CorrectedBy"))
                End If
                If reader("CorrectedOn") IsNot DBNull.Value Then
                    corrOn = Convert.ToDateTime(reader("CorrectedOn"))
                End If
            Catch ex As Exception
            End Try

            Return New AttendanceEntity() With {
                .AttendanceId = Convert.ToInt32(reader("AttendanceId")),
                .UserId = Convert.ToInt32(reader("UserId")),
                .AttendanceDate = Convert.ToDateTime(reader("AttendanceDate")),
                .ClockInTime = Convert.ToDateTime(reader("ClockInTime")),
                .ClockOutTime = If(reader("ClockOutTime") Is DBNull.Value, CType(Nothing, Nullable(Of DateTime)), Convert.ToDateTime(reader("ClockOutTime"))),
                .BreakStartTime = breakStartVal,
                .TotalBreakMinutes = Convert.ToInt32(reader("TotalBreakMinutes")),
                .TotalWorkingMinutes = Convert.ToInt32(reader("TotalWorkingMinutes")),
                .Status = Convert.ToString(reader("Status")),
                .ClientIP = If(reader("ClientIP") Is DBNull.Value, String.Empty, Convert.ToString(reader("ClientIP"))),
                .CreatedOn = Convert.ToDateTime(reader("CreatedOn")),
                .CreatedBy = Convert.ToInt32(reader("CreatedBy")),
                .IsManuallyCorrected = isCorrected,
                .CorrectionReason = corrReason,
                .CorrectedBy = corrBy,
                .CorrectedOn = corrOn
            }
        End Function
    End Class
End Namespace
