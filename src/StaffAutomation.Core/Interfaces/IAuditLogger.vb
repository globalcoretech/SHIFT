Imports System
Imports System.Data
Imports System.Threading.Tasks

Namespace Interfaces
    ''' <summary>
    ''' Contract for asynchronous security and audit logging operations.
    ''' </summary>
    Public Interface IAuditLogger
        Function LogAsync(action As String, username As String, details As String) As Task(Of Boolean)
        Function LogAuditAsync(userId As Integer, action As String, moduleName As String, details As String, Optional ipAddress As String = "", Optional transaction As IDbTransaction = Nothing) As Task(Of Boolean)
    End Interface
End Namespace
