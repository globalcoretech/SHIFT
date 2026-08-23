Imports System.Data
Imports System.Threading.Tasks

Namespace Interfaces
    ''' <summary>
    ''' Helper contract for parameterized SQL query execution and mapping.
    ''' </summary>
    Public Interface ISqlHelper
        Function ExecuteNonQueryAsync(commandText As String, parameters As IDbDataParameter(), Optional transaction As IDbTransaction = Nothing) As Task(Of Integer)
        Function ExecuteScalarAsync(Of T)(commandText As String, parameters As IDbDataParameter(), Optional transaction As IDbTransaction = Nothing) As Task(Of T)
        Function ExecuteReaderAsync(Of T)(commandText As String, parameters As IDbDataParameter(), mapFunc As Func(Of IDataReader, T), Optional transaction As IDbTransaction = Nothing) As Task(Of List(Of T))
    End Interface
End Namespace
