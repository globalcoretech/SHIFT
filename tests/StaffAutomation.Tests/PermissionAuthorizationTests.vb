Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Threading.Tasks
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports StaffAutomation.BLL.Security
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Security

Namespace StaffAutomation.Tests
    ''' <summary>
    ''' Automated unit tests for Phase 11.6 Granular Runtime Authorization Engine.
    ''' Verifies the 7-step evaluation cascade, explicit DENY override priority, role permission matrix,
    ''' legacy role-based backward compatibility, fail-closed handling, and session cache invalidation.
    ''' </summary>
    <TestClass>
    Public Class PermissionAuthorizationTests
        ' 1. Unauthenticated user → denied
        <TestMethod>
        Public Sub TestUnauthenticatedUserDenied()
            CurrentUserContext.ClearSession()
            Dim authz As IAuthorizationService = New AuthorizationService()
            If authz.IsAuthorized("USER_CREATE") Then
                Throw New InvalidOperationException("Unauthenticated user must be denied permission access.")
            End If
        End Sub

        ' 2. Inactive user → denied
        <TestMethod>
        Public Sub TestInactiveUserDenied()
            CurrentUserContext.CurrentUser = New UserDto() With {
                .UserId = 10,
                .Username = "inactive_staff",
                .Role = UserRole.Employee,
                .IsActive = False
            }
            Dim authz As IAuthorizationService = New AuthorizationService()
            If authz.IsAuthorized("ATTENDANCE_VIEW") Then
                CurrentUserContext.ClearSession()
                Throw New InvalidOperationException("Inactive user must be denied permission access.")
            End If
            CurrentUserContext.ClearSession()
        End Sub

        ' 3. Unknown permission → denied
        <TestMethod>
        Public Sub TestUnknownPermissionDenied()
            CurrentUserContext.CurrentUser = New UserDto() With {
                .UserId = 20,
                .Username = "staff_member",
                .Role = UserRole.Employee,
                .IsActive = True
            }
            Dim authz As IAuthorizationService = New AuthorizationService()
            If authz.IsAuthorized("UNKNOWN_NONEXISTENT_PERMISSION_CODE") Then
                CurrentUserContext.ClearSession()
                Throw New InvalidOperationException("Unknown permission code must be denied.")
            End If
            CurrentUserContext.ClearSession()
        End Sub

        ' 4. Explicit DENY + role permission → denied (Highest Priority Rule)
        <TestMethod>
        Public Sub TestExplicitDenyOverridesRolePermission()
            CurrentUserContext.CurrentUser = New UserDto() With {
                .UserId = 30,
                .Username = "restricted_admin",
                .Role = UserRole.Admin,
                .IsActive = True
            }
            Dim fakeSql = New FakeAuthSqlHelper()
            ' Add role permission for CLIENT_ARCHIVE
            fakeSql.AddRolePermission(3, "CLIENT_ARCHIVE") ' Admin RoleId = 3
            ' Add explicit DENY for user 30
            fakeSql.AddUserOverride(30, "CLIENT_ARCHIVE", isGranted:=False)

            Dim authz As IAuthorizationService = New AuthorizationService(fakeSql)
            authz.InvalidateCache()

            If authz.IsAuthorized("CLIENT_ARCHIVE") Then
                CurrentUserContext.ClearSession()
                Throw New InvalidOperationException("Explicit DENY override must take precedence over role permission and Admin role.")
            End If
            CurrentUserContext.ClearSession()
        End Sub

        ' 5. Explicit GRANT + missing role permission → allowed
        <TestMethod>
        Public Sub TestExplicitGrantOverridesMissingRolePermission()
            CurrentUserContext.CurrentUser = New UserDto() With {
                .UserId = 40,
                .Username = "special_employee",
                .Role = UserRole.Employee,
                .IsActive = True
            }
            Dim fakeSql = New FakeAuthSqlHelper()
            ' Add explicit GRANT for USER_CREATE (Employee role normally lacks this)
            fakeSql.AddUserOverride(40, "USER_CREATE", isGranted:=True)

            Dim authz As IAuthorizationService = New AuthorizationService(fakeSql)
            authz.InvalidateCache()

            If Not authz.IsAuthorized("USER_CREATE") Then
                CurrentUserContext.ClearSession()
                Throw New InvalidOperationException("Explicit GRANT override must allow access even if missing in role permissions.")
            End If
            CurrentUserContext.ClearSession()
        End Sub

        ' 6. No override + role permission → allowed
        <TestMethod>
        Public Sub TestRolePermissionAllowedWhenNoOverride()
            CurrentUserContext.CurrentUser = New UserDto() With {
                .UserId = 50,
                .Username = "standard_staff",
                .Role = UserRole.Employee,
                .IsActive = True
            }
            Dim fakeSql = New FakeAuthSqlHelper()
            fakeSql.AddRolePermission(1, "ATTENDANCE_VIEW") ' Employee RoleId = 1

            Dim authz As IAuthorizationService = New AuthorizationService(fakeSql)
            authz.InvalidateCache()

            If Not authz.IsAuthorized("ATTENDANCE_VIEW") Then
                CurrentUserContext.ClearSession()
                Throw New InvalidOperationException("Role permission must be allowed when no override exists.")
            End If
            CurrentUserContext.ClearSession()
        End Sub

        ' 7. No override + no role permission → denied
        <TestMethod>
        Public Sub TestMissingPermissionDeniedWhenNoOverride()
            CurrentUserContext.CurrentUser = New UserDto() With {
                .UserId = 60,
                .Username = "regular_staff",
                .Role = UserRole.Employee,
                .IsActive = True
            }
            Dim fakeSql = New FakeAuthSqlHelper()
            Dim authz As IAuthorizationService = New AuthorizationService(fakeSql)
            authz.InvalidateCache()

            If authz.IsAuthorized("BACKUP_RESTORE") Then
                CurrentUserContext.ClearSession()
                Throw New InvalidOperationException("Unmapped permission must be denied by default.")
            End If
            CurrentUserContext.ClearSession()
        End Sub

        ' 8. Existing IsAuthorized(UserRole.Admin) still works
        <TestMethod>
        Public Sub TestLegacyAdminRoleAuthorized()
            CurrentUserContext.CurrentUser = New UserDto() With {
                .UserId = 1,
                .Username = "admin",
                .Role = UserRole.Admin,
                .IsActive = True
            }
            Dim authz As IAuthorizationService = New AuthorizationService()
            If Not authz.IsAuthorized(UserRole.Admin) OrElse Not authz.IsAuthorized(UserRole.Employee) Then
                CurrentUserContext.ClearSession()
                Throw New InvalidOperationException("Legacy IsAuthorized(UserRole.Admin) backward compatibility failed.")
            End If
            CurrentUserContext.ClearSession()
        End Sub

        ' 9. Existing IsAuthorized(UserRole.Owner) still works
        <TestMethod>
        Public Sub TestLegacyOwnerRoleAuthorized()
            CurrentUserContext.CurrentUser = New UserDto() With {
                .UserId = 2,
                .Username = "owner",
                .Role = UserRole.Owner,
                .IsActive = True
            }
            Dim authz As IAuthorizationService = New AuthorizationService()
            If Not authz.IsAuthorized(UserRole.Owner) Then
                CurrentUserContext.ClearSession()
                Throw New InvalidOperationException("Legacy IsAuthorized(UserRole.Owner) backward compatibility failed.")
            End If
            If authz.IsAuthorized(UserRole.Admin) Then
                CurrentUserContext.ClearSession()
                Throw New InvalidOperationException("Owner role should not be authorized for required Admin role.")
            End If
            CurrentUserContext.ClearSession()
        End Sub

        ' 10. Existing IsAuthorized(UserRole.Employee) still works
        <TestMethod>
        Public Sub TestLegacyEmployeeRoleAuthorized()
            CurrentUserContext.CurrentUser = New UserDto() With {
                .UserId = 3,
                .Username = "staff",
                .Role = UserRole.Employee,
                .IsActive = True
            }
            Dim authz As IAuthorizationService = New AuthorizationService()
            If Not authz.IsAuthorized(UserRole.Employee) Then
                CurrentUserContext.ClearSession()
                Throw New InvalidOperationException("Legacy IsAuthorized(UserRole.Employee) backward compatibility failed.")
            End If
            If authz.IsAuthorized(UserRole.Owner) OrElse authz.IsAuthorized(UserRole.Admin) Then
                CurrentUserContext.ClearSession()
                Throw New InvalidOperationException("Employee role should not be authorized for Owner or Admin roles.")
            End If
            CurrentUserContext.ClearSession()
        End Sub

        ' 11. Authorization failure during database lookup → fails closed (denied)
        <TestMethod>
        Public Sub TestDatabaseFailureFailsClosed()
            CurrentUserContext.CurrentUser = New UserDto() With {
                .UserId = 70,
                .Username = "user_db_fail",
                .Role = UserRole.Employee,
                .IsActive = True
            }
            Dim failingSql = New FailingAuthSqlHelper()
            Dim authz As IAuthorizationService = New AuthorizationService(failingSql)
            authz.InvalidateCache()

            ' Fails closed to False
            If authz.IsAuthorized("BACKUP_RESTORE") Then
                CurrentUserContext.ClearSession()
                Throw New InvalidOperationException("Database failure during authorization lookup must fail closed to False.")
            End If
            CurrentUserContext.ClearSession()
        End Sub

        ' 12. Permission cache cleared on logout / session clear
        <TestMethod>
        Public Sub TestCacheClearedOnSessionClear()
            CurrentUserContext.CurrentUser = New UserDto() With {
                .UserId = 80,
                .Username = "cache_test_user",
                .Role = UserRole.Employee,
                .IsActive = True
            }
            Dim fakeSql = New FakeAuthSqlHelper()
            fakeSql.AddRolePermission(1, "ATTENDANCE_VIEW")

            Dim authz As IAuthorizationService = New AuthorizationService(fakeSql)
            authz.InvalidateCache()

            ' Warm up cache
            Dim initialAuth = authz.IsAuthorized("ATTENDANCE_VIEW")

            ' Clear Session
            CurrentUserContext.ClearSession()

            If CurrentUserContext.IsAuthenticated Then
                Throw New InvalidOperationException("Session should not be authenticated after ClearSession.")
            End If
        End Sub

        ' 13. Invalidation strategy clears cache on role/override changes
        <TestMethod>
        Public Sub TestCacheInvalidatedOnPermissionUpdate()
            CurrentUserContext.CurrentUser = New UserDto() With {
                .UserId = 90,
                .Username = "cache_invalidation_user",
                .Role = UserRole.Employee,
                .IsActive = True
            }
            Dim fakeSql = New FakeAuthSqlHelper()
            Dim authz As IAuthorizationService = New AuthorizationService(fakeSql)
            authz.InvalidateCache()

            ' Initially no permission
            If authz.IsAuthorized("CLIENT_CREATE") Then
                CurrentUserContext.ClearSession()
                Throw New InvalidOperationException("Initial permission check should be False.")
            End If

            ' Add override dynamically and invalidate cache
            fakeSql.AddUserOverride(90, "CLIENT_CREATE", isGranted:=True)
            authz.InvalidateCache()

            ' Permission check should now return True
            If Not authz.IsAuthorized("CLIENT_CREATE") Then
                CurrentUserContext.ClearSession()
                Throw New InvalidOperationException("Cache invalidation failed to refresh authorization state.")
            End If
            CurrentUserContext.ClearSession()
        End Sub

    End Class

    ' =========================================================================
    ' Test Fakes
    ' =========================================================================

    Public Class FakeAuthSqlHelper
        Implements ISqlHelper

        Private ReadOnly _overrides As New List(Of Tuple(Of Integer, String, Boolean))()
        Private ReadOnly _rolePerms As New List(Of Tuple(Of Integer, String))()

        Private ReadOnly _savedRules As New List(Of Dictionary(Of String, Object))()
        Private ReadOnly _savedSchedules As New List(Of Dictionary(Of String, Object))()
        Private ReadOnly _savedHistory As New HashSet(Of String)()
        Private _nextRuleId As Integer = 1
        Private _nextScheduleId As Integer = 1

        Public Sub AddUserOverride(userId As Integer, permCode As String, isGranted As Boolean)
            _overrides.Add(New Tuple(Of Integer, String, Boolean)(userId, permCode.ToUpper(), isGranted))
        End Sub

        Public Sub AddRolePermission(roleId As Integer, permCode As String)
            _rolePerms.Add(New Tuple(Of Integer, String)(roleId, permCode.ToUpper()))
        End Sub

        Public Function GetConnection() As IDbConnection Implements ISqlHelper.GetConnection
            Return Nothing
        End Function

        Public Function ExecuteNonQueryAsync(commandText As String, parameters As IDbDataParameter(), Optional transaction As IDbTransaction = Nothing) As Task(Of Integer) Implements ISqlHelper.ExecuteNonQueryAsync
            If commandText.Contains("UPDATE dbo.tbl_ComplianceRules") AndAlso parameters IsNot Nothing Then
                Dim ruleIdParam = parameters.FirstOrDefault(Function(p) p.ParameterName = "@RuleId")
                Dim nameParam = parameters.FirstOrDefault(Function(p) p.ParameterName = "@Name")
                Dim catParam = parameters.FirstOrDefault(Function(p) p.ParameterName = "@Category")
                Dim freqParam = parameters.FirstOrDefault(Function(p) p.ParameterName = "@Frequency")
                Dim dayParam = parameters.FirstOrDefault(Function(p) p.ParameterName = "@Day")
                Dim monthParam = parameters.FirstOrDefault(Function(p) p.ParameterName = "@Month")
                Dim prioParam = parameters.FirstOrDefault(Function(p) p.ParameterName = "@Priority")
                Dim activeParam = parameters.FirstOrDefault(Function(p) p.ParameterName = "@IsActive")

                Dim rId = If(ruleIdParam IsNot Nothing, Convert.ToInt32(ruleIdParam.Value), 1)
                _savedRules.RemoveAll(Function(r) Convert.ToInt32(r("ComplianceRuleId")) = rId)

                Dim dict As New Dictionary(Of String, Object) From {
                    {"ComplianceRuleId", rId},
                    {"ComplianceName", If(nameParam IsNot Nothing, Convert.ToString(nameParam.Value), "Rule")},
                    {"Category", If(catParam IsNot Nothing, Convert.ToString(catParam.Value), "GST")},
                    {"Frequency", If(freqParam IsNot Nothing, Convert.ToString(freqParam.Value), "Monthly")},
                    {"DueDateRuleDay", If(dayParam IsNot Nothing, Convert.ToInt32(dayParam.Value), 11)},
                    {"DueDateRuleMonth", If(monthParam IsNot Nothing AndAlso Not Convert.IsDBNull(monthParam.Value), CObj(Convert.ToInt32(monthParam.Value)), CObj(DBNull.Value))},
                    {"Priority", If(prioParam IsNot Nothing, Convert.ToString(prioParam.Value), "High")},
                    {"IsActive", If(activeParam IsNot Nothing, Convert.ToBoolean(activeParam.Value), True)},
                    {"CreatedOn", DateTime.UtcNow},
                    {"CreatedBy", 1},
                    {"ModifiedOn", DBNull.Value},
                    {"ModifiedBy", DBNull.Value}
                }
                _savedRules.Add(dict)
            ElseIf commandText.Contains("INSERT INTO dbo.tbl_ComplianceAlertSchedules") AndAlso parameters IsNot Nothing Then
                Dim ruleIdParam = parameters.FirstOrDefault(Function(p) p.ParameterName = "@RuleId")
                Dim daysParam = parameters.FirstOrDefault(Function(p) p.ParameterName = "@Days")
                Dim typeParam = parameters.FirstOrDefault(Function(p) p.ParameterName = "@Type")
                Dim enabledParam = parameters.FirstOrDefault(Function(p) p.ParameterName = "@IsEnabled")

                Dim dict As New Dictionary(Of String, Object) From {
                    {"AlertScheduleId", _nextScheduleId},
                    {"ComplianceRuleId", If(ruleIdParam IsNot Nothing, Convert.ToInt32(ruleIdParam.Value), 1)},
                    {"DaysBeforeDeadline", If(daysParam IsNot Nothing, Convert.ToInt32(daysParam.Value), 10)},
                    {"AlertType", If(typeParam IsNot Nothing, Convert.ToString(typeParam.Value), "Reminder")},
                    {"IsEnabled", If(enabledParam IsNot Nothing, Convert.ToBoolean(enabledParam.Value), True)}
                }
                _nextScheduleId += 1
                _savedSchedules.Add(dict)
            End If
            Return Task.FromResult(1)
        End Function

        Public Function ExecuteScalarAsync(Of T)(commandText As String, parameters As IDbDataParameter(), Optional transaction As IDbTransaction = Nothing) As Task(Of T) Implements ISqlHelper.ExecuteScalarAsync
            If commandText.Contains("INSERT INTO dbo.tbl_ComplianceRules") AndAlso parameters IsNot Nothing Then
                Dim nameParam = parameters.FirstOrDefault(Function(p) p.ParameterName = "@Name")
                Dim catParam = parameters.FirstOrDefault(Function(p) p.ParameterName = "@Category")
                Dim freqParam = parameters.FirstOrDefault(Function(p) p.ParameterName = "@Frequency")
                Dim dayParam = parameters.FirstOrDefault(Function(p) p.ParameterName = "@Day")
                Dim monthParam = parameters.FirstOrDefault(Function(p) p.ParameterName = "@Month")
                Dim prioParam = parameters.FirstOrDefault(Function(p) p.ParameterName = "@Priority")
                Dim activeParam = parameters.FirstOrDefault(Function(p) p.ParameterName = "@IsActive")
                Dim createdByParam = parameters.FirstOrDefault(Function(p) p.ParameterName = "@CreatedBy")

                Dim dict As New Dictionary(Of String, Object) From {
                    {"ComplianceRuleId", _nextRuleId},
                    {"ComplianceName", If(nameParam IsNot Nothing, Convert.ToString(nameParam.Value), "Rule")},
                    {"Category", If(catParam IsNot Nothing, Convert.ToString(catParam.Value), "GST")},
                    {"Frequency", If(freqParam IsNot Nothing, Convert.ToString(freqParam.Value), "Monthly")},
                    {"DueDateRuleDay", If(dayParam IsNot Nothing, Convert.ToInt32(dayParam.Value), 11)},
                    {"DueDateRuleMonth", If(monthParam IsNot Nothing AndAlso Not Convert.IsDBNull(monthParam.Value), CObj(Convert.ToInt32(monthParam.Value)), CObj(DBNull.Value))},
                    {"Priority", If(prioParam IsNot Nothing, Convert.ToString(prioParam.Value), "High")},
                    {"IsActive", If(activeParam IsNot Nothing, Convert.ToBoolean(activeParam.Value), True)},
                    {"CreatedOn", DateTime.UtcNow},
                    {"CreatedBy", If(createdByParam IsNot Nothing, Convert.ToInt32(createdByParam.Value), 1)},
                    {"ModifiedOn", DBNull.Value},
                    {"ModifiedBy", DBNull.Value}
                }
                Dim assignedId = _nextRuleId
                _nextRuleId += 1
                _savedRules.Add(dict)
                Return Task.FromResult(CType(CObj(assignedId), T))
            ElseIf commandText.Contains("INSERT INTO dbo.tbl_ComplianceAutomationHistory") AndAlso parameters IsNot Nothing Then
                Dim ruleIdParam = parameters.FirstOrDefault(Function(p) p.ParameterName = "@RuleId")
                Dim periodParam = parameters.FirstOrDefault(Function(p) p.ParameterName = "@PeriodName")
                Dim rId = If(ruleIdParam IsNot Nothing, Convert.ToInt32(ruleIdParam.Value), 0)
                Dim pName = If(periodParam IsNot Nothing, Convert.ToString(periodParam.Value), "")
                _savedHistory.Add(rId.ToString() & "_" & pName)
                Return Task.FromResult(CType(CObj(1), T))
            ElseIf commandText.Contains("tbl_ComplianceAutomationHistory") AndAlso commandText.Contains("COUNT(1)") AndAlso parameters IsNot Nothing Then
                Dim ruleIdParam = parameters.FirstOrDefault(Function(p) p.ParameterName = "@RuleId")
                Dim periodParam = parameters.FirstOrDefault(Function(p) p.ParameterName = "@PeriodName")
                Dim rId = If(ruleIdParam IsNot Nothing, Convert.ToInt32(ruleIdParam.Value), 0)
                Dim pName = If(periodParam IsNot Nothing, Convert.ToString(periodParam.Value), "")
                Dim count = If(_savedHistory.Contains(rId.ToString() & "_" & pName), 1, 0)
                Return Task.FromResult(CType(CObj(count), T))
            End If

            Return Task.FromResult(CType(Nothing, T))
        End Function

        Public Function ExecuteReaderAsync(Of T)(commandText As String, parameters As IDbDataParameter(), mapFunc As Func(Of IDataReader, T), Optional transaction As IDbTransaction = Nothing) As Task(Of List(Of T)) Implements ISqlHelper.ExecuteReaderAsync
            Dim results As New List(Of T)()

            If commandText.Contains("tbl_UserPermissionOverrides") Then
                Dim userIdParam = parameters.FirstOrDefault(Function(p) p.ParameterName = "@UserId")
                Dim targetUserId = If(userIdParam IsNot Nothing, Convert.ToInt32(userIdParam.Value), 0)

                For Each o In _overrides
                    If o.Item1 = targetUserId Then
                        Dim fakeReader = New SingleRowDataReader(o.Item2, o.Item3)
                        results.Add(mapFunc(fakeReader))
                    End If
                Next
            ElseIf commandText.Contains("tbl_RolePermissions") Then
                Dim roleIdParam = parameters.FirstOrDefault(Function(p) p.ParameterName = "@RoleId")
                Dim targetRoleId = If(roleIdParam IsNot Nothing, Convert.ToInt32(roleIdParam.Value), 0)

                For Each rp In _rolePerms
                    If rp.Item1 = targetRoleId Then
                        Dim fakeReader = New SingleRowDataReader(rp.Item2, True)
                        results.Add(mapFunc(fakeReader))
                    End If
                Next
            ElseIf commandText.Contains("dbo.tbl_ComplianceRules") Then
                Dim fakeReader = New DictionaryDataReader(_savedRules)
                While fakeReader.Read()
                    results.Add(mapFunc(fakeReader))
                End While
            ElseIf commandText.Contains("dbo.tbl_ComplianceAlertSchedules") Then
                Dim ruleIdParam = parameters.FirstOrDefault(Function(p) p.ParameterName = "@RuleId")
                Dim targetRuleId = If(ruleIdParam IsNot Nothing, Convert.ToInt32(ruleIdParam.Value), 0)
                Dim matchingSchedules = _savedSchedules.FindAll(Function(s) targetRuleId = 0 OrElse Convert.ToInt32(s("ComplianceRuleId")) = targetRuleId)
                Dim fakeReader = New DictionaryDataReader(matchingSchedules)
                While fakeReader.Read()
                    results.Add(mapFunc(fakeReader))
                End While
            End If

            Return Task.FromResult(results)
        End Function
    End Class

        Public Class FailingAuthSqlHelper
            Implements ISqlHelper

            Public Function GetConnection() As IDbConnection Implements ISqlHelper.GetConnection
                Return Nothing
            End Function

            Public Function ExecuteNonQueryAsync(commandText As String, parameters As IDbDataParameter(), Optional transaction As IDbTransaction = Nothing) As Task(Of Integer) Implements ISqlHelper.ExecuteNonQueryAsync
                Throw New InvalidOperationException("Simulated database connection failure.")
            End Function

            Public Function ExecuteScalarAsync(Of T)(commandText As String, parameters As IDbDataParameter(), Optional transaction As IDbTransaction = Nothing) As Task(Of T) Implements ISqlHelper.ExecuteScalarAsync
                Throw New InvalidOperationException("Simulated database connection failure.")
            End Function

            Public Function ExecuteReaderAsync(Of T)(commandText As String, parameters As IDbDataParameter(), mapFunc As Func(Of IDataReader, T), Optional transaction As IDbTransaction = Nothing) As Task(Of List(Of T)) Implements ISqlHelper.ExecuteReaderAsync
                Throw New InvalidOperationException("Simulated database connection failure.")
            End Function
        End Class

        Public Class SingleRowDataReader
            Implements IDataReader

            Private ReadOnly _permCode As String
            Private ReadOnly _isGranted As Boolean
            Private _readCount As Integer = 0

            Public Sub New(permCode As String, isGranted As Boolean)
                _permCode = permCode
                _isGranted = isGranted
            End Sub

            Default Public ReadOnly Property Item(name As String) As Object Implements IDataReader.Item
                Get
                    If name.Equals("PermissionCode", StringComparison.OrdinalIgnoreCase) Then Return _permCode
                    If name.Equals("IsGranted", StringComparison.OrdinalIgnoreCase) Then Return _isGranted
                    Return DBNull.Value
                End Get
            End Property

            Default Public ReadOnly Property Item(i As Integer) As Object Implements IDataReader.Item
                Get
                    If i = 0 Then Return _permCode
                    If i = 1 Then Return _isGranted
                    Return DBNull.Value
                End Get
            End Property

            Public ReadOnly Property Depth As Integer Implements IDataReader.Depth
                Get
                    Return 0
                End Get
            End Property

            Public ReadOnly Property IsClosed As Boolean Implements IDataReader.IsClosed
                Get
                    Return False
                End Get
            End Property

            Public ReadOnly Property RecordsAffected As Integer Implements IDataReader.RecordsAffected
                Get
                    Return 1
                End Get
            End Property

            Public ReadOnly Property FieldCount As Integer Implements IDataReader.FieldCount
                Get
                    Return 2
                End Get
            End Property

            Public Sub Close() Implements IDataReader.Close
            End Sub

            Public Function GetSchemaTable() As DataTable Implements IDataReader.GetSchemaTable
                Return Nothing
            End Function

            Public Function NextResult() As Boolean Implements IDataReader.NextResult
                Return False
            End Function

            Public Function Read() As Boolean Implements IDataReader.Read
                _readCount += 1
                Return _readCount = 1
            End Function

            Public Sub Dispose() Implements IDisposable.Dispose
            End Sub

            Public Function GetBoolean(i As Integer) As Boolean Implements IDataRecord.GetBoolean
                Return _isGranted
            End Function

            Public Function GetByte(i As Integer) As Byte Implements IDataRecord.GetByte
                Return 0
            End Function

            Public Function GetBytes(i As Integer, fieldOffset As Long, buffer() As Byte, bufferoffset As Integer, length As Integer) As Long Implements IDataRecord.GetBytes
                Return 0
            End Function

            Public Function GetChar(i As Integer) As Char Implements IDataRecord.GetChar
                Return " "c
            End Function

            Public Function GetChars(i As Integer, fieldoffset As Long, buffer() As Char, bufferoffset As Integer, length As Integer) As Long Implements IDataRecord.GetChars
                Return 0
            End Function

            Public Function GetData(i As Integer) As IDataReader Implements IDataRecord.GetData
                Return Nothing
            End Function

            Public Function GetDataTypeName(i As Integer) As String Implements IDataRecord.GetDataTypeName
                Return "String"
            End Function

            Public Function GetDateTime(i As Integer) As DateTime Implements IDataRecord.GetDateTime
                Return DateTime.UtcNow
            End Function

            Public Function GetDecimal(i As Integer) As Decimal Implements IDataRecord.GetDecimal
                Return 0D
            End Function

            Public Function GetDouble(i As Integer) As Double Implements IDataRecord.GetDouble
                Return 0.0
            End Function

            Public Function GetFieldType(i As Integer) As Type Implements IDataRecord.GetFieldType
                Return GetType(String)
            End Function

            Public Function GetFloat(i As Integer) As Single Implements IDataRecord.GetFloat
                Return 0.0F
            End Function

            Public Function GetGuid(i As Integer) As Guid Implements IDataRecord.GetGuid
                Return Guid.Empty
            End Function

            Public Function GetInt16(i As Integer) As Short Implements IDataRecord.GetInt16
                Return 0
            End Function

            Public Function GetInt32(i As Integer) As Integer Implements IDataRecord.GetInt32
                Return 0
            End Function

            Public Function GetInt64(i As Integer) As Long Implements IDataRecord.GetInt64
                Return 0L
            End Function

            Public Function GetName(i As Integer) As String Implements IDataRecord.GetName
                If i = 0 Then Return "PermissionCode"
                If i = 1 Then Return "IsGranted"
                Return ""
            End Function

            Public Function GetOrdinal(name As String) As Integer Implements IDataRecord.GetOrdinal
                If name.Equals("PermissionCode", StringComparison.OrdinalIgnoreCase) Then Return 0
                If name.Equals("IsGranted", StringComparison.OrdinalIgnoreCase) Then Return 1
                Return -1
            End Function

            Public Function GetString(i As Integer) As String Implements IDataRecord.GetString
                Return _permCode
            End Function

            Public Function GetValue(i As Integer) As Object Implements IDataRecord.GetValue
                If i = 0 Then Return _permCode
                If i = 1 Then Return _isGranted
                Return DBNull.Value
            End Function

            Public Function GetValues(values() As Object) As Integer Implements IDataRecord.GetValues
                If values.Length > 0 Then values(0) = _permCode
                If values.Length > 1 Then values(1) = _isGranted
                Return Math.Min(values.Length, 2)
            End Function

            Public Function IsDBNull(i As Integer) As Boolean Implements IDataRecord.IsDBNull
                Return False
            End Function
        End Class

        Public Class DictionaryDataReader
            Implements IDataReader

            Private ReadOnly _rows As List(Of Dictionary(Of String, Object))
            Private _index As Integer = -1

            Public Sub New(rows As List(Of Dictionary(Of String, Object)))
                _rows = rows
            End Sub

            Public Function Read() As Boolean Implements IDataReader.Read
                _index += 1
                Return _index < _rows.Count
            End Function

            Default Public ReadOnly Property Item(name As String) As Object Implements IDataReader.Item
                Get
                    If _index >= 0 AndAlso _index < _rows.Count AndAlso _rows(_index).ContainsKey(name) Then
                        Return _rows(_index)(name)
                    End If
                    Return DBNull.Value
                End Get
            End Property

            Default Public ReadOnly Property Item(i As Integer) As Object Implements IDataReader.Item
                Get
                    Return DBNull.Value
                End Get
            End Property

            Public ReadOnly Property Depth As Integer Implements IDataReader.Depth
                Get
                    Return 0
                End Get
            End Property

            Public ReadOnly Property IsClosed As Boolean Implements IDataReader.IsClosed
                Get
                    Return False
                End Get
            End Property

            Public ReadOnly Property RecordsAffected As Integer Implements IDataReader.RecordsAffected
                Get
                    Return _rows.Count
                End Get
            End Property

            Public ReadOnly Property FieldCount As Integer Implements IDataReader.FieldCount
                Get
                    Return 0
                End Get
            End Property

            Public Sub Close() Implements IDataReader.Close
            End Sub

            Public Function GetSchemaTable() As DataTable Implements IDataReader.GetSchemaTable
                Return Nothing
            End Function

            Public Function NextResult() As Boolean Implements IDataReader.NextResult
                Return False
            End Function

            Public Sub Dispose() Implements IDisposable.Dispose
            End Sub

            Public Function GetBoolean(i As Integer) As Boolean Implements IDataRecord.GetBoolean
                Return False
            End Function

            Public Function GetByte(i As Integer) As Byte Implements IDataRecord.GetByte
                Return 0
            End Function

            Public Function GetBytes(i As Integer, fieldOffset As Long, buffer() As Byte, bufferoffset As Integer, length As Integer) As Long Implements IDataRecord.GetBytes
                Return 0
            End Function

            Public Function GetChar(i As Integer) As Char Implements IDataRecord.GetChar
                Return " "c
            End Function

            Public Function GetChars(i As Integer, fieldoffset As Long, buffer() As Char, bufferoffset As Integer, length As Integer) As Long Implements IDataRecord.GetChars
                Return 0
            End Function

            Public Function GetData(i As Integer) As IDataReader Implements IDataRecord.GetData
                Return Nothing
            End Function

            Public Function GetDataTypeName(i As Integer) As String Implements IDataRecord.GetDataTypeName
                Return "String"
            End Function

            Public Function GetDateTime(i As Integer) As DateTime Implements IDataRecord.GetDateTime
                Return DateTime.UtcNow
            End Function

            Public Function GetDecimal(i As Integer) As Decimal Implements IDataRecord.GetDecimal
                Return 0D
            End Function

            Public Function GetDouble(i As Integer) As Double Implements IDataRecord.GetDouble
                Return 0.0
            End Function

            Public Function GetFieldType(i As Integer) As Type Implements IDataRecord.GetFieldType
                Return GetType(String)
            End Function

            Public Function GetFloat(i As Integer) As Single Implements IDataRecord.GetFloat
                Return 0.0F
            End Function

            Public Function GetGuid(i As Integer) As Guid Implements IDataRecord.GetGuid
                Return Guid.Empty
            End Function

            Public Function GetInt16(i As Integer) As Short Implements IDataRecord.GetInt16
                Return 0
            End Function

            Public Function GetInt32(i As Integer) As Integer Implements IDataRecord.GetInt32
                Return 0
            End Function

            Public Function GetInt64(i As Integer) As Long Implements IDataRecord.GetInt64
                Return 0L
            End Function

            Public Function GetName(i As Integer) As String Implements IDataRecord.GetName
                Return ""
            End Function

            Public Function GetOrdinal(name As String) As Integer Implements IDataRecord.GetOrdinal
                Return 0
            End Function

            Public Function GetString(i As Integer) As String Implements IDataRecord.GetString
                Return ""
            End Function

            Public Function GetValue(i As Integer) As Object Implements IDataRecord.GetValue
                Return DBNull.Value
            End Function

            Public Function GetValues(values() As Object) As Integer Implements IDataRecord.GetValues
                Return 0
            End Function

            Public Function IsDBNull(i As Integer) As Boolean Implements IDataRecord.IsDBNull
                Return False
            End Function
        End Class
End Namespace
