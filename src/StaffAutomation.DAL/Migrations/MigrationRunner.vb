Imports System.Data
Imports Microsoft.Data.SqlClient
Imports System.IO
Imports System.Reflection
Imports StaffAutomation.Core.Exceptions

Namespace Migrations
    Public Class MigrationRunner
        Private ReadOnly _connectionString As String

        Public Sub New(connectionString As String)
            If String.IsNullOrWhiteSpace(connectionString) Then
                Throw New ArgumentException("Connection string cannot be empty", NameOf(connectionString))
            End If
            _connectionString = connectionString
        End Sub

        Public Sub RunMigrations()
            ' Ensure Database exists
            EnsureDatabaseExists()

            Using connection As New SqlConnection(_connectionString)
                connection.Open()
                
                ' Acquire application lock for concurrency
                Dim lockAcquired As Boolean = AcquireLock(connection)
                If Not lockAcquired Then
                    Throw New DataAccessException("Could not acquire exclusive migration lock. Another instance might be applying migrations.")
                End If

                Try
                    ' Ensure SchemaHistory table exists and establish baseline if necessary
                    EnsureSchemaHistoryExists(connection)

                    ' Get applied migrations
                    Dim appliedMigrations = GetAppliedMigrations(connection)

                    ' Get embedded migration scripts
                    Dim scripts = GetEmbeddedScripts()

                    ' Apply pending migrations
                    For Each script In scripts
                        If Not appliedMigrations.Contains(script.Number) Then
                            ApplyMigration(connection, script)
                        End If
                    Next
                Finally
                    ReleaseLock(connection)
                End Try
            End Using
        End Sub

        Private Sub EnsureDatabaseExists()
            Dim builder As New SqlConnectionStringBuilder(_connectionString)
            Dim databaseName = builder.InitialCatalog
            builder.InitialCatalog = "master"

            Using connection As New SqlConnection(builder.ConnectionString)
                connection.Open()
                Using cmd = connection.CreateCommand()
                    cmd.CommandText = $"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'{databaseName}') CREATE DATABASE [{databaseName}];"
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        Private Function AcquireLock(connection As SqlConnection) As Boolean
            Using cmd = connection.CreateCommand()
                cmd.CommandText = "sp_getapplock"
                cmd.CommandType = CommandType.StoredProcedure
                cmd.Parameters.AddWithValue("@Resource", "StaffAutomation_Migration_Lock")
                cmd.Parameters.AddWithValue("@LockMode", "Exclusive")
                cmd.Parameters.AddWithValue("@LockOwner", "Session")
                cmd.Parameters.AddWithValue("@LockTimeout", 15000) ' 15 seconds

                Dim result = Convert.ToInt32(cmd.ExecuteScalar())
                Return result >= 0
            End Using
        End Function

        Private Sub ReleaseLock(connection As SqlConnection)
            Using cmd = connection.CreateCommand()
                cmd.CommandText = "sp_releaseapplock"
                cmd.CommandType = CommandType.StoredProcedure
                cmd.Parameters.AddWithValue("@Resource", "StaffAutomation_Migration_Lock")
                cmd.Parameters.AddWithValue("@LockOwner", "Session")
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        Private Sub EnsureSchemaHistoryExists(connection As SqlConnection)
            Using cmd = connection.CreateCommand()
                cmd.CommandText = "IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_SchemaHistory]') AND type in (N'U')) " &
                                  "BEGIN " &
                                  "  CREATE TABLE [dbo].[tbl_SchemaHistory] ( " &
                                  "    [Id] INT IDENTITY(1,1) PRIMARY KEY, " &
                                  "    [MigrationNumber] INT NOT NULL, " &
                                  "    [MigrationName] NVARCHAR(255) NOT NULL, " &
                                  "    [AppliedOn] DATETIME NOT NULL, " &
                                  "    [Status] NVARCHAR(50) NOT NULL " &
                                  "  ); " &
                                  "END"
                cmd.ExecuteNonQuery()

                ' Check if existing database needs baseline
                cmd.CommandText = "SELECT COUNT(*) FROM tbl_SchemaHistory"
                If Convert.ToInt32(cmd.ExecuteScalar()) = 0 Then
                    cmd.CommandText = "SELECT COUNT(*) FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Users]') AND type in (N'U')"
                    If Convert.ToInt32(cmd.ExecuteScalar()) > 0 Then
                        ' Database exists, check if Phase15 schema is present
                        cmd.CommandText = "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_Clients') AND name = 'DepartmentId' AND is_nullable = 1"
                        If Convert.ToInt32(cmd.ExecuteScalar()) > 0 Then
                            ' We are at version 15
                            cmd.CommandText = "INSERT INTO tbl_SchemaHistory (MigrationNumber, MigrationName, AppliedOn, Status) VALUES (15, 'Baseline', GETUTCDATE(), 'Baseline')"
                            cmd.ExecuteNonQuery()
                        Else
                            ' Check version 14/12
                            cmd.CommandText = "SELECT COUNT(*) FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_UserDevices]') AND type in (N'U')"
                            If Convert.ToInt32(cmd.ExecuteScalar()) > 0 Then
                                cmd.CommandText = "INSERT INTO tbl_SchemaHistory (MigrationNumber, MigrationName, AppliedOn, Status) VALUES (14, 'Baseline', GETUTCDATE(), 'Baseline')"
                                cmd.ExecuteNonQuery()
                            Else
                                ' If tbl_Users exists but it's an old DB, maybe we just set baseline to 10
                                ' But to be safe, if we don't know, we set to 15.
                                cmd.CommandText = "INSERT INTO tbl_SchemaHistory (MigrationNumber, MigrationName, AppliedOn, Status) VALUES (15, 'Baseline_Unknown', GETUTCDATE(), 'Baseline')"
                                cmd.ExecuteNonQuery()
                            End If
                        End If
                    End If
                End If
            End Using
        End Sub

        Private Function GetAppliedMigrations(connection As SqlConnection) As HashSet(Of Integer)
            Dim applied As New HashSet(Of Integer)()
            Using cmd = connection.CreateCommand()
                cmd.CommandText = "SELECT MigrationNumber FROM tbl_SchemaHistory WHERE Status = 'Applied' OR Status = 'Baseline'"
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        applied.Add(reader.GetInt32(0))
                    End While
                End Using
            End Using
            Return applied
        End Function

        Private Class MigrationScript
            Public Property Number As Integer
            Public Property Name As String
            Public Property Content As String
        End Class

        Private Function GetEmbeddedScripts() As List(Of MigrationScript)
            Dim scripts As New List(Of MigrationScript)()
            Dim assembly = GetType(MigrationRunner).Assembly
            Dim resourceNames = assembly.GetManifestResourceNames().Where(Function(n) n.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)).OrderBy(Function(n) n).ToList()

            For Each resourceName In resourceNames
                Dim parts = resourceName.Split("."c)
                Dim fileName = parts(parts.Length - 2)
                
                ' Extract number from prefix (e.g. 001_Master)
                If fileName.Length >= 3 Then
                    Dim prefix = fileName.Substring(0, 3)
                    Dim number As Integer
                    If Integer.TryParse(prefix, number) Then
                        Using stream = assembly.GetManifestResourceStream(resourceName)
                            Using reader = New StreamReader(stream)
                                Dim content = reader.ReadToEnd()
                                scripts.Add(New MigrationScript With {
                                    .Number = number,
                                    .Name = fileName,
                                    .Content = content
                                })
                            End Using
                        End Using
                    End If
                End If
            Next

            Return scripts
        End Function

        Private Sub ApplyMigration(connection As SqlConnection, script As MigrationScript)
            Using transaction = connection.BeginTransaction()
                Try
                    ' Split by GO
                    ' Support multiple forms of GO (case insensitive, surrounding whitespace)
                    Dim batches = System.Text.RegularExpressions.Regex.Split(script.Content, "^\s*GO\s*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase Or System.Text.RegularExpressions.RegexOptions.Multiline)
                    
                    For Each batch In batches
                        Dim trimmed = batch.Trim()
                        If Not String.IsNullOrWhiteSpace(trimmed) Then
                            Using cmd = connection.CreateCommand()
                                cmd.Transaction = transaction
                                cmd.CommandText = trimmed
                                cmd.ExecuteNonQuery()
                            End Using
                        End If
                    Next
                    
                    ' Log success
                    Using cmd = connection.CreateCommand()
                        cmd.Transaction = transaction
                        cmd.CommandText = "INSERT INTO tbl_SchemaHistory (MigrationNumber, MigrationName, AppliedOn, Status) VALUES (@Number, @Name, GETUTCDATE(), 'Applied')"
                        cmd.Parameters.AddWithValue("@Number", script.Number)
                        cmd.Parameters.AddWithValue("@Name", script.Name)
                        cmd.ExecuteNonQuery()
                    End Using
                    
                    transaction.Commit()
                Catch ex As Exception
                    transaction.Rollback()
                    Throw New DataAccessException($"Migration {script.Name} failed: {ex.Message}", ex)
                End Try
            End Using
        End Sub
    End Class
End Namespace
