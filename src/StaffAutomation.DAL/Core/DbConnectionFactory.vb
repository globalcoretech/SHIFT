Imports System.Data
Imports Microsoft.Data.SqlClient
Imports StaffAutomation.Core.Configuration
Imports StaffAutomation.Core.Interfaces

Namespace Core
    ''' <summary>
    ''' Implementation of database connection factory supplying pooled Microsoft SqlClient connections.
    ''' </summary>
    Public Class DbConnectionFactory
        Implements IDatabaseConnectionFactory

        Private ReadOnly _config As IAppConfiguration

        Public Sub New(config As IAppConfiguration)
            _config = config
        End Sub

        Public Function CreateConnection() As IDbConnection Implements IDatabaseConnectionFactory.CreateConnection
            Dim connStr = GetConnectionString()
            Return New SqlConnection(connStr)
        End Function

        Public Function GetConnectionString() As String Implements IDatabaseConnectionFactory.GetConnectionString
            Return _config.GetConnectionString("StaffAutomationDb")
        End Function
    End Class
End Namespace
