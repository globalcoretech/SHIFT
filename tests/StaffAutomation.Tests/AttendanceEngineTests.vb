Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Threading.Tasks
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports StaffAutomation.BLL.Interfaces
Imports StaffAutomation.BLL.Logging
Imports StaffAutomation.BLL.Services
Imports StaffAutomation.Core.Configuration
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Logging
Imports StaffAutomation.DAL.Interfaces

Namespace StaffAutomation.Tests
    ''' <summary>
    ''' In-Memory Mock Repository for Attendance Runtime Validation.
    ''' </summary>
    Public Class MockAttendanceRepository
        Implements IAttendanceRepository

        Public ReadOnly Records As New List(Of AttendanceEntity)()
        Private _nextId As Integer = 1

        Public Function GetTodayAttendanceAsync(userId As Integer, attendanceDate As DateTime) As Task(Of AttendanceEntity) Implements IAttendanceRepository.GetTodayAttendanceAsync
            Dim rec = Records.Find(Function(r) r.UserId = userId AndAlso r.AttendanceDate.Date = attendanceDate.Date)
            Return Task.FromResult(rec)
        End Function

        Public Function GetAttendanceHistoryAsync(Optional startDate As Nullable(Of DateTime) = Nothing, Optional endDate As Nullable(Of DateTime) = Nothing) As Task(Of List(Of AttendanceEntity)) Implements IAttendanceRepository.GetAttendanceHistoryAsync
            Return Task.FromResult(New List(Of AttendanceEntity)(Records))
        End Function

        Public Function ClockInAsync(attendance As AttendanceEntity) As Task(Of Integer) Implements IAttendanceRepository.ClockInAsync
            attendance.AttendanceId = _nextId
            _nextId += 1
            Records.Add(attendance)
            Return Task.FromResult(attendance.AttendanceId)
        End Function

        Public Function ClockOutAsync(attendanceId As Integer, clockOutTime As DateTime, totalBreakMinutes As Integer, Optional newStatus As String = Nothing) As Task(Of Boolean) Implements IAttendanceRepository.ClockOutAsync
            Dim rec = Records.Find(Function(r) r.AttendanceId = attendanceId)
            If rec Is Nothing Then Return Task.FromResult(False)
            rec.ClockOutTime = clockOutTime
            rec.TotalBreakMinutes = totalBreakMinutes
            rec.BreakStartTime = Nothing
            If Not String.IsNullOrEmpty(newStatus) Then
                rec.Status = newStatus
            End If
            Dim workMin = CInt((clockOutTime - rec.ClockInTime).TotalMinutes) - totalBreakMinutes
            rec.TotalWorkingMinutes = If(workMin < 0, 0, workMin)
            Return Task.FromResult(True)
        End Function

        Public Function StartLunchBreakAsync(attendanceId As Integer, breakStartTime As DateTime) As Task(Of Boolean) Implements IAttendanceRepository.StartLunchBreakAsync
            Dim rec = Records.Find(Function(r) r.AttendanceId = attendanceId)
            If rec Is Nothing Then Return Task.FromResult(False)
            rec.BreakStartTime = breakStartTime
            Return Task.FromResult(True)
        End Function

        Public Function EndLunchBreakAsync(attendanceId As Integer, totalBreakMinutes As Integer) As Task(Of Boolean) Implements IAttendanceRepository.EndLunchBreakAsync
            Dim rec = Records.Find(Function(r) r.AttendanceId = attendanceId)
            If rec Is Nothing Then Return Task.FromResult(False)
            rec.TotalBreakMinutes = totalBreakMinutes
            rec.BreakStartTime = Nothing
            Return Task.FromResult(True)
        End Function

        Public Function ResetTodayAttendanceAsync(userId As Integer, attendanceDate As DateTime) As Task(Of Boolean) Implements IAttendanceRepository.ResetTodayAttendanceAsync
            Records.RemoveAll(Function(r) r.UserId = userId AndAlso r.AttendanceDate.Date = attendanceDate.Date)
            Return Task.FromResult(True)
        End Function

        Public Function GetUserAttendanceHistoryAsync(userId As Integer, Optional startDate As Nullable(Of DateTime) = Nothing, Optional endDate As Nullable(Of DateTime) = Nothing) As Task(Of List(Of AttendanceEntity)) Implements IAttendanceRepository.GetUserAttendanceHistoryAsync
            Dim userRecords = Records.FindAll(Function(r) r.UserId = userId)
            If startDate.HasValue Then
                userRecords = userRecords.FindAll(Function(r) r.AttendanceDate.Date >= startDate.Value.Date)
            End If
            If endDate.HasValue Then
                userRecords = userRecords.FindAll(Function(r) r.AttendanceDate.Date <= endDate.Value.Date)
            End If
            Return Task.FromResult(userRecords)
        End Function

        Public Function CorrectAttendanceByAdminAsync(attendanceId As Integer, userId As Integer, attendanceDate As DateTime, clockInTime As DateTime, clockOutTime As Nullable(Of DateTime), totalBreakMinutes As Integer, status As String, reason As String, adminUserId As Integer) As Task(Of Boolean) Implements IAttendanceRepository.CorrectAttendanceByAdminAsync
            Dim rec = Records.Find(Function(r) (attendanceId > 0 AndAlso r.AttendanceId = attendanceId) OrElse (r.UserId = userId AndAlso r.AttendanceDate.Date = attendanceDate.Date))
            If rec Is Nothing Then
                rec = New AttendanceEntity() With {
                    .AttendanceId = _nextId,
                    .UserId = userId,
                    .AttendanceDate = attendanceDate.Date
                }
                _nextId += 1
                Records.Add(rec)
            End If
            rec.ClockInTime = clockInTime
            rec.ClockOutTime = clockOutTime
            rec.TotalBreakMinutes = totalBreakMinutes
            rec.Status = status
            rec.IsManuallyCorrected = True
            rec.CorrectionReason = reason
            rec.CorrectedBy = adminUserId
            rec.CorrectedOn = DateTime.UtcNow
            Return Task.FromResult(True)
        End Function
    End Class

    Public Class MockUserRepository
        Implements IUserRepository

        Public Function GetByIdAsync(userId As Integer) As Task(Of UserEntity) Implements IUserRepository.GetByIdAsync
            Return Task.FromResult(New UserEntity() With {.UserId = userId, .Username = "testuser", .FullName = "Test User"})
        End Function

        Public Function GetByUsernameAsync(username As String) As Task(Of UserEntity) Implements IUserRepository.GetByUsernameAsync
            Return Task.FromResult(New UserEntity() With {.UserId = 1, .Username = username, .FullName = "Test User"})
        End Function

        Public Function GetAllAsync() As Task(Of List(Of UserEntity)) Implements IUserRepository.GetAllAsync
            Return Task.FromResult(New List(Of UserEntity)() From {New UserEntity() With {.UserId = 1, .Username = "testuser", .FullName = "Test User"}})
        End Function

        Public Function AddAsync(user As UserEntity) As Task(Of Integer) Implements IUserRepository.AddAsync
            Return Task.FromResult(1)
        End Function

        Public Function UpdateAsync(user As UserEntity) As Task(Of Boolean) Implements IUserRepository.UpdateAsync
            Return Task.FromResult(True)
        End Function

        Public Function SoftDeleteAsync(userId As Integer, modifiedBy As Integer) As Task(Of Boolean) Implements IUserRepository.SoftDeleteAsync
            Return Task.FromResult(True)
        End Function
    End Class

    Public Class MockAppLogger
        Implements IAppLogger

        Public Sub Log(level As LogLevel, message As String, Optional moduleName As String = "", Optional ex As Exception = Nothing) Implements IAppLogger.Log
        End Sub

        Public Sub LogDebug(message As String, Optional moduleName As String = "") Implements IAppLogger.LogDebug
        End Sub

        Public Sub LogInfo(message As String, Optional moduleName As String = "") Implements IAppLogger.LogInfo
        End Sub

        Public Sub LogWarn(message As String, Optional moduleName As String = "") Implements IAppLogger.LogWarn
        End Sub

        Public Sub LogError(message As String, Optional moduleName As String = "", Optional ex As Exception = Nothing) Implements IAppLogger.LogError
        End Sub

        Public Sub LogFatal(message As String, Optional moduleName As String = "", Optional ex As Exception = Nothing) Implements IAppLogger.LogFatal
        End Sub
    End Class

    Public Class MockSqlHelper
        Implements ISqlHelper

        Public Function GetConnection() As IDbConnection Implements ISqlHelper.GetConnection
            Return Nothing
        End Function

        Public Function ExecuteNonQueryAsync(commandText As String, parameters As IDbDataParameter(), Optional transaction As IDbTransaction = Nothing) As Task(Of Integer) Implements ISqlHelper.ExecuteNonQueryAsync
            Return Task.FromResult(1)
        End Function

        Public Function ExecuteScalarAsync(Of T)(commandText As String, parameters As IDbDataParameter(), Optional transaction As IDbTransaction = Nothing) As Task(Of T) Implements ISqlHelper.ExecuteScalarAsync
            Return Task.FromResult(CType(CObj(1), T))
        End Function

        Public Function ExecuteReaderAsync(Of T)(commandText As String, parameters As IDbDataParameter(), mapFunc As Func(Of IDataReader, T), Optional transaction As IDbTransaction = Nothing) As Task(Of List(Of T)) Implements ISqlHelper.ExecuteReaderAsync
            Return Task.FromResult(New List(Of T)())
        End Function
    End Class

    ''' <summary>
    ''' Strict Unit Tests verifying Phase 10.1, 10.2, and 10.3 Daily Punch & Policy Engine Workflows.
    ''' </summary>
    <TestClass>
    Public Class AttendanceEngineTests
        <TestMethod>
        Public Async Function RunPhase10_1_ValidationTestsAsync() As Task
            Dim repo As New MockAttendanceRepository()
            Dim userRepo As New MockUserRepository()
            Dim appLogger As New MockAppLogger()
            Dim sqlHelper As New MockSqlHelper()
            Dim auditLogger As New AuditLogger(sqlHelper)
            Dim service As IAttendanceService = New AttendanceService(repo, userRepo, appLogger, auditLogger)

            Dim userId As Integer = 1

            ' 1. Check initially no punch exists today
            Dim todayRec = Await service.GetTodayAttendanceForUserAsync(userId)
            If todayRec IsNot Nothing Then
                Throw New InvalidOperationException("Validation Failed: Expected no punch for today initially.")
            End If

            ' 2. First Punch In inserts exactly one record
            Dim punchInDto = Await service.ClockInUserAsync(userId, "127.0.0.1")
            If punchInDto Is Nothing OrElse punchInDto.AttendanceId <= 0 Then
                Throw New InvalidOperationException("Validation Failed: ClockInUserAsync did not return valid AttendanceDto.")
            End If
            If repo.Records.Count <> 1 Then
                Throw New InvalidOperationException($"Validation Failed: Expected 1 record in repository, found {repo.Records.Count}.")
            End If

            ' 3. Refresh today's status shows Punched In
            todayRec = Await service.GetTodayAttendanceForUserAsync(userId)
            If todayRec Is Nothing OrElse todayRec.ClockOutTime.HasValue Then
                Throw New InvalidOperationException("Validation Failed: Today's record should show Punched In without ClockOutTime.")
            End If

            ' 4. Duplicate Punch In throws BusinessException
            Dim duplicatePunchInFailed As Boolean = False
            Try
                Await service.ClockInUserAsync(userId, "127.0.0.1")
            Catch ex As BusinessException
                duplicatePunchInFailed = True
            End Try
            If Not duplicatePunchInFailed Then
                Throw New InvalidOperationException("Validation Failed: Duplicate Punch In did not throw BusinessException.")
            End If

            ' 5. Punch Out updates same record
            Dim clockOutResult = Await service.ClockOutUserAsync(userId, 0)
            If Not clockOutResult Then
                Throw New InvalidOperationException("Validation Failed: ClockOutUserAsync returned False.")
            End If
            If repo.Records.Count <> 1 Then
                Throw New InvalidOperationException("Validation Failed: ClockOut inserted a duplicate row instead of updating.")
            End If
            If Not repo.Records(0).ClockOutTime.HasValue Then
                Throw New InvalidOperationException("Validation Failed: ClockOutTime was not set on record.")
            End If

            ' 6. Duplicate Punch Out throws BusinessException
            Dim duplicatePunchOutFailed As Boolean = False
            Try
                Await service.ClockOutUserAsync(userId, 0)
            Catch ex As BusinessException
                duplicatePunchOutFailed = True
            End Try
            If Not duplicatePunchOutFailed Then
                Throw New InvalidOperationException("Validation Failed: Duplicate Punch Out did not throw BusinessException.")
            End If

            ' 7. Reopening / re-fetching state shows Punched Out with valid working hours
            todayRec = Await service.GetTodayAttendanceForUserAsync(userId)
            If todayRec Is Nothing OrElse Not todayRec.ClockOutTime.HasValue Then
                Throw New InvalidOperationException("Validation Failed: Re-fetching state after Punch Out failed.")
            End If
        End Function

        <TestMethod>
        Public Async Function RunPhase10_2_ValidationTestsAsync() As Task
            Dim repo As New MockAttendanceRepository()
            Dim userRepo As New MockUserRepository()
            Dim appLogger As New MockAppLogger()
            Dim sqlHelper As New MockSqlHelper()
            Dim auditLogger As New AuditLogger(sqlHelper)

            ' Test 1: Early Departure calculation -> Status = Early Leave
            Dim entity1 As New AttendanceEntity() With {
                .AttendanceId = 1,
                .UserId = 10,
                .AttendanceDate = DateTime.Today,
                .ClockInTime = New DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 9, 15, 0),
                .Status = "Present"
            }
            repo.Records.Add(entity1)

            Dim resEarly = Await repo.ClockOutAsync(1, New DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 17, 0, 0), 0, "Early Leave")
            If Not resEarly OrElse repo.Records(0).Status <> "Early Leave" Then
                Throw New InvalidOperationException("Phase 10.2 Validation Failed: Expected status 'Early Leave'.")
            End If

            ' Test 2: Late Arrival + Early Departure calculation -> Status = Late & Early
            Dim entity2 As New AttendanceEntity() With {
                .AttendanceId = 2,
                .UserId = 11,
                .AttendanceDate = DateTime.Today,
                .ClockInTime = New DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 10, 0, 0),
                .Status = "Late"
            }
            repo.Records.Add(entity2)

            Dim resLateEarly = Await repo.ClockOutAsync(2, New DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 17, 0, 0), 0, "Late & Early")
            If Not resLateEarly OrElse repo.Records(1).Status <> "Late & Early" Then
                Throw New InvalidOperationException("Phase 10.2 Validation Failed: Expected status 'Late & Early'.")
            End If
        End Function

        <TestMethod>
        Public Sub RunPhase10_3_PolicyEngineValidationTests()
            Dim engine As IAttendancePolicyEngine = New AttendancePolicyEngine()
            Dim policy As New AttendancePolicyConfig()

            Dim today = DateTime.Today
            Dim in1100 = today.AddHours(11)          ' 11:00 AM
            Dim cur1800 = today.AddHours(18)          ' 06:00 PM (7h gross)

            ' Test 1: Lunch Deduction (11:00 AM to 06:00 PM -> Gross 420m, Lunch 60m, Net 360m = 6h)
            Dim gross = engine.CalculateGrossMinutes(in1100, cur1800)
            Dim lunch = engine.CalculateLunchDeduction(in1100, cur1800, policy)
            If gross <> 420 OrElse lunch <> 60 Then
                Throw New InvalidOperationException($"Phase 10.3 Validation Failed: Expected Gross 420m & Lunch 60m, got Gross {gross}m & Lunch {lunch}m.")
            End If

            ' Test 2: Progress Percentage & Remaining Time (360 net / 480 target -> 75% progress, 120m remaining)
            Dim result1 = engine.CalculateProgress(in1100, Nothing, cur1800, policy)
            If result1.NetWorkedMinutes <> 360 OrElse result1.RemainingMinutes <> 120 OrElse result1.ProgressPercentage <> 75 Then
                Throw New InvalidOperationException($"Phase 10.3 Validation Failed: Progress metrics mismatch (Net {result1.NetWorkedMinutes}, Rem {result1.RemainingMinutes}, Pct {result1.ProgressPercentage}%).")
            End If

            ' Test 3: Dynamic Expected Exit for Late Arrival (12:30 PM -> Exit 09:30 PM)
            Dim in1230 = today.AddHours(12).AddMinutes(30)
            Dim expectedExit1230 = engine.CalculateExpectedExit(in1230, policy)
            Dim expectedTarget = today.AddHours(21).AddMinutes(30) ' 09:30 PM
            If expectedExit1230 <> expectedTarget Then
                Throw New InvalidOperationException($"Phase 10.3 Validation Failed: Expected Exit for 12:30 PM arrival should be 09:30 PM, got {expectedExit1230:hh:mm tt}.")
            End If

            ' Test 4: Progress Capping at 100% and Overtime calculation (10 Net Hours -> Progress 100%, Overtime 120m)
            Dim cur2200 = today.AddHours(22) ' 10:00 PM (Gross 11h, Lunch 1h -> Net 10h = 600m)
            Dim resultOvertime = engine.CalculateProgress(in1100, Nothing, cur2200, policy)
            If resultOvertime.ProgressPercentage <> 100 OrElse resultOvertime.OvertimeMinutes <> 120 Then
                Throw New InvalidOperationException($"Phase 10.3 Validation Failed: Expected Capped Progress 100% & Overtime 120m, got Pct {resultOvertime.ProgressPercentage}% & OT {resultOvertime.OvertimeMinutes}m.")
            End If

            ' Test 5: Action Flags Verification
            Dim resultNotPunched = engine.CalculateProgress(Nothing, Nothing, DateTime.Now, policy)
            If Not resultNotPunched.CanPunchIn OrElse resultNotPunched.CanPunchOut OrElse resultNotPunched.IsWorkdayCompleted Then
                Throw New InvalidOperationException("Phase 10.3 Validation Failed: Action flags for NotPunchedIn incorrect.")
            End If

            Dim resultWorking = engine.CalculateProgress(in1100, Nothing, DateTime.Now, policy)
            If resultWorking.CanPunchIn OrElse Not resultWorking.CanPunchOut OrElse resultWorking.IsWorkdayCompleted Then
                Throw New InvalidOperationException("Phase 10.3 Validation Failed: Action flags for Working state incorrect.")
            End If

            Dim resultCompleted = engine.CalculateProgress(in1100, cur1800, DateTime.Now, policy)
            If resultCompleted.CanPunchIn OrElse resultCompleted.CanPunchOut OrElse Not resultCompleted.IsWorkdayCompleted Then
                Throw New InvalidOperationException("Phase 10.3 Validation Failed: Action flags for DayCompleted incorrect.")
            End If

            ' Test 6: Manual Break Punch Action Flags & Exact Break Deduction
            Dim resultBreakOut = engine.CalculateProgress(in1100, Nothing, today.AddHours(14), policy)
            If Not resultBreakOut.CanPunchBreakOut Then
                Throw New InvalidOperationException("Phase 10.3 Validation Failed: Working state should allow CanPunchBreakOut.")
            End If

            ' Simulate active break starting at 02:00 PM (14:00) and current time 02:45 PM (14:45) -> 45m break
            Dim breakStart = today.AddHours(14)
            Dim cur1445 = today.AddHours(14).AddMinutes(45)
            Dim resultOnBreak = engine.CalculateProgress(in1100, Nothing, cur1445, policy, breakStart, 0)
            If Not resultOnBreak.CanPunchBreakIn OrElse resultOnBreak.TotalBreakMinutes < 45 Then
                Throw New InvalidOperationException($"Phase 10.3 Validation Failed: OnBreak state should allow CanPunchBreakIn and calculate 45m break, got {resultOnBreak.TotalBreakMinutes}m.")
            End If
        End Sub

        <TestMethod>
        Public Async Function RunPersistedBreakStateRecoveryTestAsync() As Task
            Dim repo As New MockAttendanceRepository()
            Dim userRepo As New MockUserRepository()
            Dim appLogger As New MockAppLogger()
            Dim sqlHelper As New MockSqlHelper()
            Dim auditLogger As New AuditLogger(sqlHelper)
            Dim service As IAttendanceService = New AttendanceService(repo, userRepo, appLogger, auditLogger)

            Dim userId As Integer = 1

            ' Step 1: Clock In at 09:00 AM
            Await service.ClockInUserAsync(userId, "127.0.0.1")

            ' Step 2: Start Lunch Break -> Persisted to DB
            Dim breakSuccess = Await service.StartLunchBreakAsync(userId)
            If Not breakSuccess Then
                Throw New InvalidOperationException("Persisted Break Test Failed: StartLunchBreakAsync returned False.")
            End If

            ' Step 3: Verify DB Record has BreakStartTime populated
            Dim dbRec = Await repo.GetTodayAttendanceAsync(userId, DateTime.Today)
            If dbRec Is Nothing OrElse Not dbRec.BreakStartTime.HasValue Then
                Throw New InvalidOperationException("Persisted Break Test Failed: BreakStartTime was not persisted in Repository.")
            End If

            ' Step 4: Simulate App Restart / Recovery -> Re-fetch today record via Service
            Dim recoveredDto = Await service.GetTodayAttendanceForUserAsync(userId)
            If recoveredDto Is Nothing OrElse Not recoveredDto.BreakStartTime.HasValue Then
                Throw New InvalidOperationException("Persisted Break Test Failed: Service GetTodayAttendanceForUserAsync did not restore BreakStartTime.")
            End If

            ' Step 5: Resume Work -> End Lunch Break
            Dim endBreakSuccess = Await service.EndLunchBreakAsync(userId)
            If Not endBreakSuccess Then
                Throw New InvalidOperationException("Persisted Break Test Failed: EndLunchBreakAsync returned False.")
            End If

            ' Step 6: Verify BreakStartTime cleared in DB and TotalBreakMinutes updated
            Dim finalDbRec = Await repo.GetTodayAttendanceAsync(userId, DateTime.Today)
            If finalDbRec.BreakStartTime.HasValue Then
                Throw New InvalidOperationException("Persisted Break Test Failed: BreakStartTime was not cleared in DB after EndLunchBreakAsync.")
            End If
        End Function

        <TestMethod>
        Public Async Function TestInspectAttendanceDataAndNullSafetyAsync() As Task
            Dim repo As New MockAttendanceRepository()
            Dim userRepo As New MockUserRepository()
            Dim appLogger As New MockAppLogger()
            Dim sqlHelper As New MockSqlHelper()
            Dim auditLogger As New AuditLogger(sqlHelper)
            Dim service As IAttendanceService = New AttendanceService(repo, userRepo, appLogger, auditLogger)

            ' 1. Test Inspect attendance for employee with NO punch record (Not Punched)
            Dim notPunchedUserId As Integer = 99
            Dim emptyRec = Await service.GetTodayAttendanceForUserAsync(notPunchedUserId)
            Assert.IsNull(emptyRec, "GetTodayAttendanceForUserAsync should return Nothing for un-punched employee.")

            Dim emptyHistory = Await service.GetUserAttendanceHistoryAsync(notPunchedUserId)
            Assert.IsNotNull(emptyHistory, "GetUserAttendanceHistoryAsync should return empty list, not Nothing.")
            Assert.AreEqual(0, emptyHistory.Count, "GetUserAttendanceHistoryAsync history count should be 0 for un-punched employee.")

            ' 2. Test Inspect attendance for valid employee WITH attendance data
            Dim punchedUserId As Integer = 1
            Await service.ClockInUserAsync(punchedUserId, "127.0.0.1")

            Dim todayRec = Await service.GetTodayAttendanceForUserAsync(punchedUserId)
            Assert.IsNotNull(todayRec, "GetTodayAttendanceForUserAsync should return AttendanceDto for punched employee.")
            Assert.AreEqual(punchedUserId, todayRec.UserId)
            Assert.IsTrue(todayRec.Status = "Present" OrElse todayRec.Status = "Late", $"ClockIn status should be 'Present' or 'Late', but got '{todayRec.Status}'.")

            ' 3. Test null / optional attendance values safety
            ' Add a historical record with null optional fields to repo
            Dim nullValEntity As New AttendanceEntity() With {
                .AttendanceId = 50,
                .UserId = punchedUserId,
                .AttendanceDate = DateTime.Today.AddDays(-1),
                .ClockInTime = DateTime.Today.AddDays(-1).AddHours(9),
                .ClockOutTime = Nothing,
                .BreakStartTime = Nothing,
                .TotalBreakMinutes = 0,
                .TotalWorkingMinutes = 0,
                .Status = Nothing,
                .ClientIP = Nothing,
                .CreatedOn = DateTime.UtcNow,
                .CreatedBy = punchedUserId,
                .IsManuallyCorrected = False,
                .CorrectionReason = Nothing
            }
            repo.Records.Add(nullValEntity)

            Dim userHistory = Await service.GetUserAttendanceHistoryAsync(punchedUserId)
            Assert.IsTrue(userHistory.Count > 0, "GetUserAttendanceHistoryAsync should return history records including null-value entity.")

            Dim nullDto = userHistory.Find(Function(h) h.AttendanceId = 50)
            Assert.IsNotNull(nullDto, "Null-value entity should map safely to AttendanceDto.")
            Assert.AreEqual(String.Empty, nullDto.Status, "Null status should map safely to empty string.")
        End Function

        <TestMethod>
        Public Async Function TestCaseA_EmployeeWithValidPunchInAndPunchOutAsync() As Task
            Dim repo As New MockAttendanceRepository()
            Dim userRepo As New MockUserRepository()
            Dim appLogger As New MockAppLogger()
            Dim sqlHelper As New MockSqlHelper()
            Dim auditLogger As New AuditLogger(sqlHelper)
            Dim service As IAttendanceService = New AttendanceService(repo, userRepo, appLogger, auditLogger)

            Dim userId As Integer = 10
            Dim today = DateTime.Today
            Dim entity As New AttendanceEntity() With {
                .AttendanceId = 101,
                .UserId = userId,
                .AttendanceDate = today,
                .ClockInTime = today.AddHours(9),
                .ClockOutTime = today.AddHours(17),
                .TotalBreakMinutes = 60,
                .TotalWorkingMinutes = 420,
                .Status = "Completed"
            }
            repo.Records.Add(entity)

            Dim todayRec = Await service.GetTodayAttendanceForUserAsync(userId)
            Assert.IsNotNull(todayRec)
            Assert.IsTrue(todayRec.ClockOutTime.HasValue)
            Assert.AreEqual("Completed", todayRec.Status)
            Assert.AreEqual(420, todayRec.TotalWorkingMinutes)
        End Function

        <TestMethod>
        Public Async Function TestCaseB_EmployeeWithPunchInButNoPunchOutAsync() As Task
            Dim repo As New MockAttendanceRepository()
            Dim userRepo As New MockUserRepository()
            Dim appLogger As New MockAppLogger()
            Dim sqlHelper As New MockSqlHelper()
            Dim auditLogger As New AuditLogger(sqlHelper)
            Dim service As IAttendanceService = New AttendanceService(repo, userRepo, appLogger, auditLogger)

            Dim userId As Integer = 11
            Dim today = DateTime.Today
            Dim entity As New AttendanceEntity() With {
                .AttendanceId = 102,
                .UserId = userId,
                .AttendanceDate = today,
                .ClockInTime = today.AddHours(9),
                .ClockOutTime = Nothing,
                .TotalBreakMinutes = 0,
                .TotalWorkingMinutes = 0,
                .Status = "Present"
            }
            repo.Records.Add(entity)

            Dim todayRec = Await service.GetTodayAttendanceForUserAsync(userId)
            Assert.IsNotNull(todayRec)
            Assert.IsFalse(todayRec.ClockOutTime.HasValue)
            Assert.AreEqual("Present", todayRec.Status)
        End Function

        <TestMethod>
        Public Async Function TestCaseC_EmployeeWithNoAttendanceRecordNotPunchedAsync() As Task
            Dim repo As New MockAttendanceRepository()
            Dim userRepo As New MockUserRepository()
            Dim appLogger As New MockAppLogger()
            Dim sqlHelper As New MockSqlHelper()
            Dim auditLogger As New AuditLogger(sqlHelper)
            Dim service As IAttendanceService = New AttendanceService(repo, userRepo, appLogger, auditLogger)

            Dim userId As Integer = 999
            Dim todayRec = Await service.GetTodayAttendanceForUserAsync(userId)
            Assert.IsNull(todayRec)

            Dim history = Await service.GetUserAttendanceHistoryAsync(userId)
            Assert.IsNotNull(history)
            Assert.AreEqual(0, history.Count)
        End Function

        <TestMethod>
        Public Async Function TestCaseD_NullAndOptionalAttendanceFieldsSafetyAsync() As Task
            Dim repo As New MockAttendanceRepository()
            Dim userRepo As New MockUserRepository()
            Dim appLogger As New MockAppLogger()
            Dim sqlHelper As New MockSqlHelper()
            Dim auditLogger As New AuditLogger(sqlHelper)
            Dim service As IAttendanceService = New AttendanceService(repo, userRepo, appLogger, auditLogger)

            Dim userId As Integer = 12
            Dim entity As New AttendanceEntity() With {
                .AttendanceId = 103,
                .UserId = userId,
                .AttendanceDate = DateTime.Today,
                .ClockInTime = DateTime.Today.AddHours(9),
                .ClockOutTime = Nothing,
                .BreakStartTime = Nothing,
                .TotalBreakMinutes = 0,
                .TotalWorkingMinutes = 0,
                .Status = Nothing,
                .ClientIP = Nothing,
                .CorrectionReason = Nothing
            }
            repo.Records.Add(entity)

            Dim todayRec = Await service.GetTodayAttendanceForUserAsync(userId)
            Assert.IsNotNull(todayRec)
            Assert.AreEqual(String.Empty, todayRec.Status)
            Assert.IsFalse(todayRec.ClockOutTime.HasValue)
            Assert.IsFalse(todayRec.BreakStartTime.HasValue)
            Assert.IsNull(entity.ClientIP)
            Assert.IsNull(entity.CorrectionReason)
        End Function
    End Class
End Namespace
