Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.BLL.Interfaces
Imports StaffAutomation.BLL.Logging
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Logging
Imports StaffAutomation.DAL.Interfaces

Namespace Services
    ''' <summary>
    ''' Business Logic Service handling staff attendance clock-in, clock-out, late arrival checks, and audit logging.
    ''' </summary>
    Public Class AttendanceService
        Implements IAttendanceService

        Private ReadOnly _attendanceRepo As IAttendanceRepository
        Private ReadOnly _userRepo As IUserRepository
        Private ReadOnly _appLogger As IAppLogger
        Private ReadOnly _auditLogger As AuditLogger
        Private ReadOnly _timelineService As ITimelineService

        Public Sub New(attendanceRepo As IAttendanceRepository, userRepo As IUserRepository, appLogger As IAppLogger, auditLogger As AuditLogger, Optional timelineService As ITimelineService = Nothing)
            _attendanceRepo = attendanceRepo
            _userRepo = userRepo
            _appLogger = appLogger
            _auditLogger = auditLogger
            _timelineService = timelineService
        End Sub

        Public Async Function GetTodayAttendanceForUserAsync(userId As Integer) As Task(Of AttendanceDto) Implements IAttendanceService.GetTodayAttendanceForUserAsync
            Dim entity = Await _attendanceRepo.GetTodayAttendanceAsync(userId, DateTime.Today)
            If entity Is Nothing Then Return Nothing
            Dim user = Await _userRepo.GetByIdAsync(userId)
            Dim userName = If(user IsNot Nothing, user.FullName, "Staff #" & userId)
            Return MapToDto(entity, userName)
        End Function

        Public Async Function GetAllAttendanceHistoryAsync() As Task(Of List(Of AttendanceDto)) Implements IAttendanceService.GetAllAttendanceHistoryAsync
            Dim list = Await _attendanceRepo.GetAttendanceHistoryAsync()
            Dim users = Await _userRepo.GetAllAsync()
            Dim userDict As New Dictionary(Of Integer, String)()
            For Each u In users
                userDict(u.UserId) = u.FullName
            Next

            Dim dtoList As New List(Of AttendanceDto)()
            For Each item In list
                Dim uName = If(userDict.ContainsKey(item.UserId), userDict(item.UserId), "Staff #" & item.UserId)
                dtoList.Add(MapToDto(item, uName))
            Next
            Return dtoList
        End Function

        Public Async Function ClockInUserAsync(userId As Integer, clientIP As String) As Task(Of AttendanceDto) Implements IAttendanceService.ClockInUserAsync
            Dim today = DateTime.Today
            Dim existing = Await _attendanceRepo.GetTodayAttendanceAsync(userId, today)
            If existing IsNot Nothing Then
                Throw New BusinessException($"User ID {userId} has already clocked in for today ({today:yyyy-MM-dd} at {existing.ClockInTime:hh:mm tt}).", "ERR_ATTENDANCE_ALREADY_CLOCKED_IN")
            End If

            Dim now = DateTime.Now
            ' Office Start Time: 09:30 AM, Grace Period: 15 mins -> Threshold: 09:45 AM
            Dim lateThreshold = New DateTime(now.Year, now.Month, now.Day, 9, 45, 0)
            Dim isLate = now > lateThreshold
            Dim statusText = If(isLate, "Late", "Present")

            Dim entity As New AttendanceEntity() With {
                .UserId = userId,
                .AttendanceDate = today,
                .ClockInTime = now,
                .Status = statusText,
                .ClientIP = If(Not String.IsNullOrEmpty(clientIP), clientIP, "127.0.0.1"),
                .CreatedBy = userId
            }

            Dim newId = Await _attendanceRepo.ClockInAsync(entity)
            entity.AttendanceId = newId

            _appLogger.LogInfo($"User ID {userId} clocked in at {now:hh:mm:ss tt} (Status: {statusText}).", "AttendanceService")
            Await _auditLogger.LogAuditAsync(userId, "ATTENDANCE_CLOCK_IN", "AttendanceService", $"User ID {userId} clocked in at {now:hh:mm:ss tt} (Status: {statusText}).")

            If isLate Then
                _appLogger.LogWarn($"User ID {userId} flagged for Late Arrival at {now:hh:mm:ss tt}.", "AttendanceService")
                Await _auditLogger.LogAuditAsync(userId, "ATTENDANCE_LATE_ARRIVAL", "AttendanceService", $"User ID {userId} flagged for Late Arrival at {now:hh:mm:ss tt} (Punch: {now:hh:mm tt}, Allowed start: 09:45 AM).")
            End If

            Dim user = Await _userRepo.GetByIdAsync(userId)
            Dim userName = If(user IsNot Nothing, user.FullName, "Staff #" & userId)

            Return MapToDto(entity, userName)
        End Function

        Public Async Function ClockOutUserAsync(userId As Integer, totalBreakMinutes As Integer) As Task(Of Boolean) Implements IAttendanceService.ClockOutUserAsync
            Dim today = DateTime.Today
            Dim existing = Await _attendanceRepo.GetTodayAttendanceAsync(userId, today)
            If existing Is Nothing Then
                Throw New BusinessException($"No active clock-in record found for user ID {userId} today. Please punch in first.", "ERR_ATTENDANCE_NOT_CLOCKED_IN")
            End If

            If existing.ClockOutTime.HasValue Then
                Throw New BusinessException($"User ID {userId} has already clocked out for today at {existing.ClockOutTime.Value:hh:mm tt}.", "ERR_ATTENDANCE_ALREADY_CLOCKED_OUT")
            End If

            If _timelineService IsNot Nothing Then
                Try
                    Await _timelineService.AutoPauseActiveTimerOnAttendanceEventAsync(userId, "Auto-paused on Attendance Clock-Out")
                Catch ex As Exception
                    _appLogger.LogError($"Failed to auto-pause active timer on ClockOut for User ID {userId}: {ex.Message}", "AttendanceService")
                End Try
            End If

            Dim now = DateTime.Now
            ' Office End Time: 06:30 PM (18:30), Grace Period: 15 mins -> Threshold: 06:15 PM (18:15)
            Dim earlyThreshold = New DateTime(now.Year, now.Month, now.Day, 18, 15, 0)
            Dim isEarly = now < earlyThreshold

            Dim updatedStatus As String = existing.Status
            If isEarly Then
                If existing.Status = "Late" Then
                    updatedStatus = "Late & Early"
                Else
                    updatedStatus = "Early Leave"
                End If
            End If

            Dim success = Await _attendanceRepo.ClockOutAsync(existing.AttendanceId, now, totalBreakMinutes, updatedStatus)

            If success Then
                _appLogger.LogInfo($"User ID {userId} clocked out at {now:hh:mm:ss tt} (Updated Status: {updatedStatus}).", "AttendanceService")
                Await _auditLogger.LogAuditAsync(userId, "ATTENDANCE_CLOCK_OUT", "AttendanceService", $"User ID {userId} clocked out at {now:hh:mm:ss tt} (Status: {updatedStatus}).")

                If isEarly Then
                    _appLogger.LogWarn($"User ID {userId} flagged for Early Departure at {now:hh:mm:ss tt}.", "AttendanceService")
                    Await _auditLogger.LogAuditAsync(userId, "ATTENDANCE_EARLY_DEPARTURE", "AttendanceService", $"User ID {userId} flagged for Early Departure at {now:hh:mm:ss tt} (Punch: {now:hh:mm tt}, Expected end: 06:15 PM).")
                End If
            End If

            Return success
        End Function

        Public Async Function StartLunchBreakAsync(userId As Integer) As Task(Of Boolean) Implements IAttendanceService.StartLunchBreakAsync
            Dim today = DateTime.Today
            Dim existing = Await _attendanceRepo.GetTodayAttendanceAsync(userId, today)
            If existing Is Nothing Then
                Throw New BusinessException("No active clock-in record found for today. Please punch in first.", "ERR_ATTENDANCE_NOT_CLOCKED_IN")
            End If
            If existing.ClockOutTime.HasValue Then
                Throw New BusinessException("Attendance has already been completed for today.", "ERR_ATTENDANCE_COMPLETED")
            End If
            If existing.BreakStartTime.HasValue Then
                Return True ' Already on lunch break
            End If

            If _timelineService IsNot Nothing Then
                Try
                    Await _timelineService.AutoPauseActiveTimerOnAttendanceEventAsync(userId, "Auto-paused on Attendance Break")
                Catch ex As Exception
                    _appLogger.LogError($"Failed to auto-pause active timer on LunchBreak for User ID {userId}: {ex.Message}", "AttendanceService")
                End Try
            End If

            Dim now = DateTime.Now
            Dim success = Await _attendanceRepo.StartLunchBreakAsync(existing.AttendanceId, now)

            If success Then
                _appLogger.LogInfo($"User ID {userId} started lunch break at {now:hh:mm:ss tt}.", "AttendanceService")
                Await _auditLogger.LogAuditAsync(userId, "ATTENDANCE_LUNCH_START", "AttendanceService", $"User ID {userId} started lunch break at {now:hh:mm:ss tt}.")
            End If

            Return success
        End Function

        Public Async Function EndLunchBreakAsync(userId As Integer) As Task(Of Boolean) Implements IAttendanceService.EndLunchBreakAsync
            Dim today = DateTime.Today
            Dim existing = Await _attendanceRepo.GetTodayAttendanceAsync(userId, today)
            If existing Is Nothing Then
                Throw New BusinessException("No active clock-in record found for today.", "ERR_ATTENDANCE_NOT_CLOCKED_IN")
            End If

            Dim now = DateTime.Now
            Dim breakMinutes As Integer = 0

            If existing.BreakStartTime.HasValue Then
                breakMinutes = CInt(Math.Max(1, Math.Floor((now - existing.BreakStartTime.Value).TotalMinutes)))
            End If

            Dim newTotalBreak = existing.TotalBreakMinutes + breakMinutes
            Dim success = Await _attendanceRepo.EndLunchBreakAsync(existing.AttendanceId, newTotalBreak)

            If success Then
                _appLogger.LogInfo($"User ID {userId} ended lunch break at {now:hh:mm:ss tt} (Break Duration: {breakMinutes} mins).", "AttendanceService")
                Await _auditLogger.LogAuditAsync(userId, "ATTENDANCE_LUNCH_END", "AttendanceService", $"User ID {userId} ended lunch break at {now:hh:mm:ss tt} (Duration: {breakMinutes} mins, Total Break: {newTotalBreak} mins).")
            End If

            Return success
        End Function

        Public Async Function ResetTodayAttendanceAsync(userId As Integer) As Task(Of Boolean) Implements IAttendanceService.ResetTodayAttendanceAsync
            Dim success = Await _attendanceRepo.ResetTodayAttendanceAsync(userId, DateTime.Today)
            If success Then
                _appLogger.LogWarn($"Developer reset today's attendance for user ID {userId}.", "AttendanceService")
                Await _auditLogger.LogAuditAsync(userId, "ATTENDANCE_DEV_RESET", "AttendanceService", $"Developer reset today's attendance for user ID {userId}.")
            End If
            Return success
        End Function

        Public Async Function GetUserAttendanceHistoryAsync(userId As Integer) As Task(Of List(Of AttendanceDto)) Implements IAttendanceService.GetUserAttendanceHistoryAsync
            Dim list = Await _attendanceRepo.GetUserAttendanceHistoryAsync(userId)
            Dim user = Await _userRepo.GetByIdAsync(userId)
            Dim userName = If(user IsNot Nothing, user.FullName, "Staff #" & userId)

            Dim dtoList As New List(Of AttendanceDto)()
            For Each item In list
                dtoList.Add(MapToDto(item, userName))
            Next
            Return dtoList
        End Function

        Public Async Function GetTodayStaffOverviewAsync(attendanceDate As DateTime, Optional departmentId As Nullable(Of Integer) = Nothing, Optional searchQuery As String = Nothing) As Task(Of List(Of AdminAttendanceOverviewDto)) Implements IAttendanceService.GetTodayStaffOverviewAsync
            Dim allUsers = Await _userRepo.GetAllAsync()
            Dim todayLogs = Await _attendanceRepo.GetAttendanceHistoryAsync(attendanceDate.Date, attendanceDate.Date)

            Dim logDict As New Dictionary(Of Integer, AttendanceEntity)()
            For Each log In todayLogs
                logDict(log.UserId) = log
            Next

            Dim overviewList As New List(Of AdminAttendanceOverviewDto)()
            For Each u In allUsers
                If departmentId.HasValue AndAlso departmentId.Value > 0 AndAlso CInt(u.Department) <> departmentId.Value Then
                    Continue For
                End If

                If Not String.IsNullOrWhiteSpace(searchQuery) Then
                    Dim q = searchQuery.Trim().ToLower()
                    Dim matchName = If(u.FullName IsNot Nothing, u.FullName.ToLower().Contains(q), False)
                    Dim matchUser = If(u.Username IsNot Nothing, u.Username.ToLower().Contains(q), False)
                    If Not matchName AndAlso Not matchUser Then Continue For
                End If

                Dim dto As New AdminAttendanceOverviewDto With {
                    .UserId = u.UserId,
                    .StaffName = u.FullName,
                    .EmployeeCode = If(String.IsNullOrEmpty(u.Username), $"EMP-{u.UserId:D3}", u.Username),
                    .DepartmentName = u.Department.ToString(),
                    .AttendanceDate = attendanceDate.Date
                }

                If logDict.ContainsKey(u.UserId) Then
                    Dim rec = logDict(u.UserId)
                    dto.AttendanceId = rec.AttendanceId
                    dto.ClockInTime = rec.ClockInTime
                    dto.ClockOutTime = rec.ClockOutTime
                    dto.BreakStartTime = rec.BreakStartTime
                    dto.TotalBreakMinutes = rec.TotalBreakMinutes
                    dto.TotalWorkingMinutes = rec.TotalWorkingMinutes
                    dto.Status = If(rec.Status, String.Empty)
                    dto.IsManuallyCorrected = rec.IsManuallyCorrected
                    dto.CorrectionReason = rec.CorrectionReason
                End If

                overviewList.Add(dto)
            Next

            Return overviewList
        End Function

        Public Async Function CorrectStaffAttendanceAsync(adminUserId As Integer, dto As AttendanceCorrectionDto) As Task(Of Boolean) Implements IAttendanceService.CorrectStaffAttendanceAsync
            If dto Is Nothing Then Throw New ArgumentNullException(NameOf(dto))
            If String.IsNullOrWhiteSpace(dto.Reason) Then
                Throw New BusinessException("A valid correction reason is required for administrative manual attendance edits.", "ERR_ATTENDANCE_REASON_REQUIRED")
            End If

            Dim success = Await _attendanceRepo.CorrectAttendanceByAdminAsync(
                dto.AttendanceId,
                dto.UserId,
                dto.AttendanceDate,
                dto.ClockInTime,
                dto.ClockOutTime,
                dto.TotalBreakMinutes,
                dto.Status,
                dto.Reason,
                adminUserId
            )

            If success Then
                _appLogger.LogInfo($"Admin ID {adminUserId} manually corrected attendance ID {dto.AttendanceId} for User ID {dto.UserId}. Reason: {dto.Reason}", "AttendanceService")
                Await _auditLogger.LogAuditAsync(adminUserId, "ATTENDANCE_ADMIN_CORRECTED", "AttendanceService", $"Admin ID {adminUserId} corrected attendance for User ID {dto.UserId} (Record #{dto.AttendanceId}). Reason: {dto.Reason}")
            End If

            Return success
        End Function

        Private Function MapToDto(entity As AttendanceEntity, userName As String) As AttendanceDto
            Dim totalWorkingMin As Integer = 0
            If entity.ClockOutTime.HasValue Then
                If entity.TotalWorkingMinutes > 0 Then
                    totalWorkingMin = entity.TotalWorkingMinutes
                Else
                    totalWorkingMin = CInt((entity.ClockOutTime.Value - entity.ClockInTime).TotalMinutes) - entity.TotalBreakMinutes
                    If totalWorkingMin < 0 Then totalWorkingMin = 0
                End If
            End If

            Return New AttendanceDto() With {
                .AttendanceId = entity.AttendanceId,
                .UserId = entity.UserId,
                .UserName = userName,
                .AttendanceDate = entity.AttendanceDate,
                .ClockInTime = entity.ClockInTime,
                .ClockOutTime = entity.ClockOutTime,
                .TotalBreakMinutes = entity.TotalBreakMinutes,
                .TotalWorkingMinutes = totalWorkingMin,
                .BreakStartTime = entity.BreakStartTime,
                .Status = If(entity.Status, String.Empty)
            }
        End Function
    End Class
End Namespace
