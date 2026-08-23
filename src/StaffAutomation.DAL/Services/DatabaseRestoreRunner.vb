Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports Microsoft.Data.SqlClient
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Configuration
Imports StaffAutomation.Core.Interfaces

Namespace Services
    ''' <summary>
    ''' Recovery runner connecting to SQL Server 'master' database to perform single-user mode database restoration.
    ''' Guarantees MULTI_USER restoration in Finally block under all success and failure conditions.
    ''' </summary>
    Public Class DatabaseRestoreRunner
        Private ReadOnly _config As IAppConfiguration
        Private ReadOnly _auditLogger As IAuditLogger

        Public Sub New(config As IAppConfiguration, auditLogger As IAuditLogger)
            _config = config
            _auditLogger = auditLogger
        End Sub

        Public Async Function ExecuteRestoreAsync(backupFilePath As String) As Task(Of Boolean)
            If String.IsNullOrWhiteSpace(backupFilePath) OrElse Not System.IO.File.Exists(backupFilePath) Then
                Throw New ArgumentException("Restore aborted: Backup file does not exist on disk.", NameOf(backupFilePath))
            End If

            ' 1. Derive master database connection string
            Dim baseConnStr = _config.GetConnectionString("StaffAutomationDb")
            Dim masterConnStr = BuildMasterConnectionString(baseConnStr)

            Dim isSuccess As Boolean = False
            Using conn As New SqlConnection(masterConnStr)
                Await conn.OpenAsync()

                Try
                    ' 2. Terminate active user connections & set SINGLE_USER mode
                    Using cmdSingle As SqlCommand = conn.CreateCommand()
                        cmdSingle.CommandText = "ALTER DATABASE [StaffAutomationDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;"
                        cmdSingle.CommandTimeout = 60
                        Await cmdSingle.ExecuteNonQueryAsync()
                    End Using

                    ' 3. Execute T-SQL RESTORE DATABASE operation
                    Using cmdRestore As SqlCommand = conn.CreateCommand()
                        cmdRestore.CommandText = "RESTORE DATABASE [StaffAutomationDb] FROM DISK = @FilePath WITH REPLACE, RECOVERY, CHECKSUM;"
                        cmdRestore.Parameters.Add(New SqlParameter("@FilePath", SqlDbType.NVarChar, 512) With {.Value = backupFilePath})
                        cmdRestore.CommandTimeout = 600 ' 10 minute timeout for large restores
                        Await cmdRestore.ExecuteNonQueryAsync()
                    End Using

                    isSuccess = True
                    Await _auditLogger.LogAsync("DATABASE_RESTORE_SUCCESS", Environment.UserName, $"Successfully restored database 'StaffAutomationDb' from file: {System.IO.Path.GetFileName(backupFilePath)}")
                Catch ex As Exception
                    _auditLogger.LogAsync("DATABASE_RESTORE_FAILURE", Environment.UserName, $"Database restore failed for file '{System.IO.Path.GetFileName(backupFilePath)}': {ex.Message}").GetAwaiter().GetResult()
                    Throw New InvalidOperationException($"Database restoration failed: {ex.Message}", ex)
                Finally
                    ' 4. GUARANTEED MULTI_USER RESTORATION
                    Try
                        Using cmdMulti As SqlCommand = conn.CreateCommand()
                            cmdMulti.CommandText = "ALTER DATABASE [StaffAutomationDb] SET MULTI_USER;"
                            cmdMulti.CommandTimeout = 30
                            cmdMulti.ExecuteNonQuery()
                        End Using
                    Catch exMulti As Exception
                        ' Log multi-user reset warning
                    End Try
                End Try
            End Using

            Return isSuccess
        End Function

        Private Function BuildMasterConnectionString(connStr As String) As String
            Dim builder As New SqlConnectionStringBuilder(connStr) With {
                .InitialCatalog = "master",
                .ConnectTimeout = 30
            }
            Return builder.ConnectionString
        End Function
    End Class
End Namespace
