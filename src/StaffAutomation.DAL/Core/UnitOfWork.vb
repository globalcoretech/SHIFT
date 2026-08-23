Imports System
Imports System.Data
Imports Microsoft.Data.SqlClient
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Interfaces

Namespace Core
    ''' <summary>
    ''' Transaction Manager and Unit of Work implementation wrapping SqlTransaction for multi-table updates.
    ''' </summary>
    Public Class UnitOfWork
        Implements IDisposable

        Private ReadOnly _connectionFactory As IDatabaseConnectionFactory
        Private _connection As IDbConnection
        Private _transaction As IDbTransaction

        Public Sub New(connectionFactory As IDatabaseConnectionFactory)
            _connectionFactory = connectionFactory
        End Sub

        Public ReadOnly Property Transaction As IDbTransaction
            Get
                Return _transaction
            End Get
        End Property

        Public Sub BeginTransaction()
            If _transaction IsNot Nothing Then
                Throw New InfrastructureException("A transaction is already active in this UnitOfWork.")
            End If
            _connection = _connectionFactory.CreateConnection()
            _connection.Open()
            _transaction = _connection.BeginTransaction(IsolationLevel.ReadCommitted)
        End Sub

        Public Sub Commit()
            If _transaction Is Nothing Then
                Throw New InfrastructureException("No active transaction to commit.")
            End If
            Try
                _transaction.Commit()
            Catch ex As Exception
                _transaction.Rollback()
                Throw New DataAccessException("Transaction commit failed and was rolled back.", ex)
            Finally
                ResetTransactionState()
            End Try
        End Sub

        Public Sub Rollback()
            If _transaction IsNot Nothing Then
                Try
                    _transaction.Rollback()
                Finally
                    ResetTransactionState()
                End Try
            End If
        End Sub

        Private Sub ResetTransactionState()
            If _transaction IsNot Nothing Then
                _transaction.Dispose()
                _transaction = Nothing
            End If
            If _connection IsNot Nothing Then
                If _connection.State <> ConnectionState.Closed Then
                    _connection.Close()
                End If
                _connection.Dispose()
                _connection = Nothing
            End If
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Rollback()
        End Sub
    End Class
End Namespace
