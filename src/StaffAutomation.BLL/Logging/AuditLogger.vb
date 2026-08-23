Imports System
Imports System.Data
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Interfaces
Imports Microsoft.Data.SqlClient

Namespace Logging
    ''' <summary>
    ''' System Audit Logging service persisting security and user actions to tbl_AuditLogs.
    ''' </summary>
    Public Class AuditLogger
        Implements IAuditLogger

        Private ReadOnly _sqlHelper As ISqlHelper

        Public Sub New(sqlHelper As ISqlHelper)
            _sqlHelper = sqlHelper
        End Sub

        Public Async Function LogAsync(action As String, username As String, details As String) As Task(Of Boolean) Implements IAuditLogger.LogAsync
            Return Await LogAuditAsync(1, action, "SYSTEM", $"{username}: {details}")
        End Function

        Public Async Function LogAuditAsync(userId As Integer, action As String, moduleName As String, details As String, Optional ipAddress As String = "", Optional transaction As IDbTransaction = Nothing) As Task(Of Boolean) Implements IAuditLogger.LogAuditAsync
            Const query As String = "INSERT INTO dbo.tbl_AuditLogs (UserId, Action, ModuleName, Details, IPAddress, Timestamp) " &
                                   "VALUES (@UserId, @Action, @ModuleName, @Details, @IPAddress, GETUTCDATE());"

            Dim params As SqlParameter() = {
                New SqlParameter("@UserId", userId),
                New SqlParameter("@Action", action),
                New SqlParameter("@ModuleName", moduleName),
                New SqlParameter("@Details", details),
                New SqlParameter("@IPAddress", If(String.IsNullOrEmpty(ipAddress), CObj(DBNull.Value), ipAddress))
            }

            Dim rowsAffected = Await _sqlHelper.ExecuteNonQueryAsync(query, params, transaction)
            Return rowsAffected > 0
        End Function
    End Class
End Namespace
