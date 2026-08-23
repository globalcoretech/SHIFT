Imports System.Data

Namespace Interfaces
    ''' <summary>
    ''' Factory contract for instantiating pooled SQL Server database connections.
    ''' </summary>
    Public Interface IDatabaseConnectionFactory
        Function CreateConnection() As IDbConnection
        Function GetConnectionString() As String
    End Interface
End Namespace
