Imports System
Imports System.Collections.Generic
Imports System.Data
Imports Microsoft.Data.SqlClient
Imports StaffAutomation.Core.Configuration
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Security
Imports StaffAutomation.DAL.Configuration
Imports StaffAutomation.DAL.Core

Namespace Security
    ''' <summary>
    ''' In-memory permission snapshot cache item per user session.
    ''' </summary>
    Friend Class UserPermissionSnapshot
        Public Property UserId As Integer
        Public Property RoleId As Integer
        Public Property ExplicitDenies As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Public Property ExplicitGrants As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Public Property RolePermissions As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Public Property LoadedAt As DateTime = DateTime.UtcNow
    End Class

    ''' <summary>
    ''' Runtime Authorization Engine validating active session roles, granular permission codes, and explicit overrides.
    ''' Enforces the security evaluation cascade with 100% backward compatibility and session caching.
    ''' </summary>
    Public Class AuthorizationService
        Implements IAuthorizationService

        Private Shared ReadOnly LockObj As New Object()
        Private Shared _permissionCache As New Dictionary(Of Integer, UserPermissionSnapshot)()
        Private Shared ReadOnly CacheTtl As TimeSpan = TimeSpan.FromMinutes(5)

        Shared Sub New()
            AddHandler CurrentUserContext.SessionCleared, Sub(s, e) ClearCache()
        End Sub

        Private ReadOnly _sqlHelper As ISqlHelper

        Public Sub New()
            Try
                Dim config As IAppConfiguration = New AppConfiguration()
                Dim connFactory As IDatabaseConnectionFactory = New DbConnectionFactory(config)
                _sqlHelper = New SqlHelper(connFactory)
            Catch
                _sqlHelper = Nothing
            End Try
        End Sub

        Public Sub New(sqlHelper As ISqlHelper)
            _sqlHelper = sqlHelper
        End Sub

        ' =========================================================================
        ' Legacy Role-Based Authorization Engine (100% Backward Compatible)
        ' =========================================================================

        Public Function IsAuthorized(requiredRole As UserRole) As Boolean Implements IAuthorizationService.IsAuthorized
            If Not CurrentUserContext.IsAuthenticated Then Return False
            If CurrentUserContext.CurrentUser.Role = UserRole.Admin Then Return True
            Return CurrentUserContext.CurrentUser.Role = requiredRole
        End Function

        Public Function IsAuthorizedAny(ParamArray requiredRoles() As UserRole) As Boolean Implements IAuthorizationService.IsAuthorizedAny
            If Not CurrentUserContext.IsAuthenticated Then Return False
            If CurrentUserContext.CurrentUser.Role = UserRole.Admin Then Return True
            Return Array.Exists(requiredRoles, Function(r) r = CurrentUserContext.CurrentUser.Role)
        End Function

        Public Sub AuthorizeOrThrow(requiredRole As UserRole) Implements IAuthorizationService.AuthorizeOrThrow
            If Not IsAuthorized(requiredRole) Then
                Throw New SecurityAuthorizationException($"Access denied. Required role: {requiredRole.ToString()}.", requiredRole.ToString())
            End If
        End Sub

        Public Sub AuthorizeAnyOrThrow(ParamArray requiredRoles() As UserRole) Implements IAuthorizationService.AuthorizeAnyOrThrow
            If Not IsAuthorizedAny(requiredRoles) Then
                Dim rolesStr = String.Join(", ", requiredRoles)
                Throw New SecurityAuthorizationException($"Access denied. Required role: {rolesStr}.", rolesStr)
            End If
        End Sub

        ' =========================================================================
        ' Granular Permission-Based Authorization Cascade (Phase 11.6)
        ' =========================================================================

        Public Function IsAuthorized(permissionCode As String) As Boolean Implements IAuthorizationService.IsAuthorized
            If String.IsNullOrWhiteSpace(permissionCode) Then Return False

            ' STEP 1: Authenticated Check
            If Not CurrentUserContext.IsAuthenticated Then Return False

            ' STEP 2: Current User Existence Check
            Dim user = CurrentUserContext.CurrentUser
            If user Is Nothing OrElse user.UserId <= 0 Then Return False

            ' STEP 3: Active User Check
            If Not user.IsActive Then Return False

            Dim normalizedCode = permissionCode.Trim().ToUpper()

            ' Fetch or load user permission snapshot safely
            Dim snapshot = GetUserPermissionSnapshot(user.UserId, CInt(user.Role))
            If snapshot Is Nothing Then
                ' Fail Closed on DB failure
                Return FallbackRoleCheck(user.Role, normalizedCode, hasExplicitDeny:=False)
            End If

            ' STEP 4: Explicit User DENY Override (Highest Priority across ALL roles)
            If snapshot.ExplicitDenies.Contains(normalizedCode) Then
                Return False
            End If

            ' STEP 5: Explicit User GRANT Override
            If snapshot.ExplicitGrants.Contains(normalizedCode) Then
                Return True
            End If

            ' STEP 6: Role-Permission Matrix Assignment
            If snapshot.RolePermissions.Contains(normalizedCode) Then
                Return True
            End If

            ' STEP 7: Owner / Admin Fallback (Only allowed when NOT explicitly denied in Step 4)
            If user.Role = UserRole.Admin OrElse user.Role = UserRole.Owner Then
                Return True
            End If

            ' Default Deny
            Return False
        End Function

        Public Sub AuthorizeOrThrow(permissionCode As String) Implements IAuthorizationService.AuthorizeOrThrow
            If Not IsAuthorized(permissionCode) Then
                Throw New SecurityAuthorizationException($"Access denied for permission '{permissionCode}'.", permissionCode)
            End If
        End Sub

        Public Sub InvalidateCache() Implements IAuthorizationService.InvalidateCache
            SyncLockObjectClear()
        End Sub

        Public Shared Sub ClearCache()
            SyncLockObjectClear()
        End Sub

        Private Shared Sub SyncLockObjectClear()
            SyncLock LockObj
                _permissionCache.Clear()
            End SyncLock
        End Sub

        ' =========================================================================
        ' Private Helper & Caching Methods
        ' =========================================================================

        Private Function FallbackRoleCheck(role As UserRole, permissionCode As String, hasExplicitDeny As Boolean) As Boolean
            If hasExplicitDeny Then Return False
            If role = UserRole.Admin OrElse role = UserRole.Owner Then Return True
            Return False
        End Function

        Private Function GetUserPermissionSnapshot(userId As Integer, roleId As Integer) As UserPermissionSnapshot
            SyncLock LockObj
                If _permissionCache.ContainsKey(userId) Then
                    Dim cached = _permissionCache(userId)
                    If (DateTime.UtcNow - cached.LoadedAt) < CacheTtl Then
                        Return cached
                    End If
                End If
            End SyncLock

            If _sqlHelper Is Nothing Then Return Nothing

            Try
                Dim snapshot As New UserPermissionSnapshot() With {
                    .UserId = userId,
                    .RoleId = roleId,
                    .LoadedAt = DateTime.UtcNow
                }

                ' 1. Load User Overrides
                Const overrideQuery As String = "SELECT p.PermissionCode, uo.IsGranted " &
                                               "FROM dbo.tbl_UserPermissionOverrides uo " &
                                               "INNER JOIN dbo.tbl_Permissions p ON uo.PermissionId = p.PermissionId " &
                                               "WHERE uo.UserId = @UserId AND p.IsActive = 1;"
                Dim paramsUser As SqlParameter() = {New SqlParameter("@UserId", userId)}
                Dim mapOverride As Func(Of IDataReader, Tuple(Of String, Boolean)) =
                    Function(r) New Tuple(Of String, Boolean)(Convert.ToString(r("PermissionCode")).ToUpper(), Convert.ToBoolean(r("IsGranted")))

                Dim overrideList = _sqlHelper.ExecuteReaderAsync(overrideQuery, paramsUser, mapOverride).GetAwaiter().GetResult()
                For Each item In overrideList
                    If item.Item2 Then
                        snapshot.ExplicitGrants.Add(item.Item1)
                    Else
                        snapshot.ExplicitDenies.Add(item.Item1)
                    End If
                Next

                ' 2. Load Role Permissions
                Const roleQuery As String = "SELECT p.PermissionCode " &
                                           "FROM dbo.tbl_RolePermissions rp " &
                                           "INNER JOIN dbo.tbl_Permissions p ON rp.PermissionId = p.PermissionId " &
                                           "WHERE rp.RoleId = @RoleId AND p.IsActive = 1;"
                Dim paramsRole As SqlParameter() = {New SqlParameter("@RoleId", roleId)}
                Dim mapRole As Func(Of IDataReader, String) = Function(r) Convert.ToString(r("PermissionCode")).ToUpper()

                Dim rolePerms = _sqlHelper.ExecuteReaderAsync(roleQuery, paramsRole, mapRole).GetAwaiter().GetResult()
                For Each code In rolePerms
                    snapshot.RolePermissions.Add(code)
                Next

                SyncLock LockObj
                    _permissionCache(userId) = snapshot
                End SyncLock

                Return snapshot
            Catch ex As Exception
                Return Nothing
            End Try
        End Function
    End Class
End Namespace
