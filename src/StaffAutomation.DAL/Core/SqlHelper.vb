Imports System
Imports System.Collections.Generic
Imports System.Data
Imports Microsoft.Data.SqlClient
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Interfaces

Namespace Core
    ''' <summary>
    ''' Implementation of SQL helper executing parameterized T-SQL queries against SQL Server Express.
    ''' </summary>
    Public Class SqlHelper
        Implements ISqlHelper

        Private ReadOnly _connectionFactory As IDatabaseConnectionFactory

        Public Sub New(connectionFactory As IDatabaseConnectionFactory)
            _connectionFactory = connectionFactory
        End Sub

        Public Function GetConnection() As IDbConnection Implements ISqlHelper.GetConnection
            Return _connectionFactory.CreateConnection()
        End Function

        Public Async Function ExecuteNonQueryAsync(commandText As String, parameters As IDbDataParameter(), Optional transaction As IDbTransaction = Nothing) As Task(Of Integer) Implements ISqlHelper.ExecuteNonQueryAsync
            Try
                If transaction IsNot Nothing Then
                    Dim cmd As SqlCommand = CType(transaction.Connection.CreateCommand(), SqlCommand)
                    cmd.Transaction = CType(transaction, SqlTransaction)
                    cmd.CommandText = commandText
                    AddParameters(cmd, parameters)
                    Return Await cmd.ExecuteNonQueryAsync()
                Else
                    Using conn As SqlConnection = CType(_connectionFactory.CreateConnection(), SqlConnection)
                        Await conn.OpenAsync()
                        Using cmd As SqlCommand = conn.CreateCommand()
                            cmd.CommandText = commandText
                            AddParameters(cmd, parameters)
                            Return Await cmd.ExecuteNonQueryAsync()
                        End Using
                    End Using
                End If
            Catch ex As Exception
                Throw New DataAccessException("Database execution failed: " & ex.Message, ex)
            End Try
        End Function

        Public Async Function ExecuteScalarAsync(Of T)(commandText As String, parameters As IDbDataParameter(), Optional transaction As IDbTransaction = Nothing) As Task(Of T) Implements ISqlHelper.ExecuteScalarAsync
            Try
                If transaction IsNot Nothing Then
                    Dim cmd As SqlCommand = CType(transaction.Connection.CreateCommand(), SqlCommand)
                    cmd.Transaction = CType(transaction, SqlTransaction)
                    cmd.CommandText = commandText
                    AddParameters(cmd, parameters)
                    Dim rawVal = Await cmd.ExecuteScalarAsync()
                    If rawVal Is DBNull.Value OrElse rawVal Is Nothing Then Return CType(Nothing, T)
                    Return CType(Convert.ChangeType(rawVal, GetType(T)), T)
                Else
                    Using conn As SqlConnection = CType(_connectionFactory.CreateConnection(), SqlConnection)
                        Await conn.OpenAsync()
                        Using cmd As SqlCommand = conn.CreateCommand()
                            cmd.CommandText = commandText
                            AddParameters(cmd, parameters)
                            Dim rawVal = Await cmd.ExecuteScalarAsync()
                            If rawVal Is DBNull.Value OrElse rawVal Is Nothing Then Return CType(Nothing, T)
                            Return CType(Convert.ChangeType(rawVal, GetType(T)), T)
                        End Using
                    End Using
                End If
            Catch ex As Exception
                Throw New DataAccessException("Database scalar query failed: " & ex.Message, ex)
            End Try
        End Function

        Public Async Function ExecuteReaderAsync(Of T)(commandText As String, parameters As IDbDataParameter(), mapFunc As Func(Of IDataReader, T), Optional transaction As IDbTransaction = Nothing) As Task(Of List(Of T)) Implements ISqlHelper.ExecuteReaderAsync
            Dim results As New List(Of T)()
            Try
                If transaction IsNot Nothing Then
                    Dim cmd As SqlCommand = CType(transaction.Connection.CreateCommand(), SqlCommand)
                    cmd.Transaction = CType(transaction, SqlTransaction)
                    cmd.CommandText = commandText
                    AddParameters(cmd, parameters)
                    Using reader As SqlDataReader = Await cmd.ExecuteReaderAsync()
                        While Await reader.ReadAsync()
                            results.Add(mapFunc(reader))
                        End While
                    End Using
                Else
                    Using conn As SqlConnection = CType(_connectionFactory.CreateConnection(), SqlConnection)
                        Await conn.OpenAsync()
                        Using cmd As SqlCommand = conn.CreateCommand()
                            cmd.CommandText = commandText
                            AddParameters(cmd, parameters)
                            Using reader As SqlDataReader = Await cmd.ExecuteReaderAsync()
                                While Await reader.ReadAsync()
                                    results.Add(mapFunc(reader))
                                End While
                            End Using
                        End Using
                    End Using
                End If
                Return results
            Catch ex As Exception
                Throw New DataAccessException("Database reader query failed: " & ex.Message, ex)
            End Try
        End Function

        Private Sub AddParameters(cmd As SqlCommand, parameters As IDbDataParameter())
            If parameters IsNot Nothing Then
                For Each p In parameters
                    If p.Value Is Nothing Then p.Value = DBNull.Value
                    cmd.Parameters.Add(p)
                Next
            End If
        End Sub
    End Class
End Namespace
