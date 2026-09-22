Option Strict On
Option Explicit On

Imports System
Imports System.IO
Imports System.Runtime.Versioning
Imports System.Text.Json
Imports System.Text.Json.Serialization
Imports Microsoft.Data.SqlClient

Namespace Configuration
    ''' <summary>
    ''' Data model representing per-machine database connection configuration.
    ''' Supports hostnames, IP addresses, named instances, custom ports, and DPAPI protected credentials.
    ''' </summary>
    Public Class DatabaseConnectionSettings
        Public Property Server As String = ".\SQLEXPRESS"
        Public Property Instance As String = ""
        Public Property Port As Integer = 0
        Public Property Database As String = "StaffAutomationDb"
        Public Property UseIntegratedSecurity As Boolean = True
        Public Property UserId As String = ""
        Public Property EncryptedPassword As String = ""
        Public Property ConnectTimeout As Integer = 15
        Public Property TrustServerCertificate As Boolean = True
        Public Property Encrypt As Boolean = False

        ''' <summary>
        ''' Builds a fully qualified ADO.NET SQL Server connection string.
        ''' </summary>
        Public Function BuildConnectionString() As String
            Dim builder As New SqlConnectionStringBuilder()

            Dim fullServer As String = If(String.IsNullOrWhiteSpace(Server), ".\SQLEXPRESS", Server.Trim())

            ' Append instance name if provided separately and not already in Server string
            If Not String.IsNullOrWhiteSpace(Instance) AndAlso Not fullServer.Contains("\") AndAlso Not fullServer.Contains(",") Then
                fullServer = $"{fullServer}\{Instance.Trim()}"
            End If

            ' Append custom TCP port if specified and not already in Server string
            If Port > 0 AndAlso Not fullServer.Contains(",") Then
                fullServer = $"{fullServer},{Port}"
            End If

            builder.DataSource = fullServer
            builder.InitialCatalog = If(String.IsNullOrWhiteSpace(Database), "StaffAutomationDb", Database.Trim())
            builder.IntegratedSecurity = UseIntegratedSecurity

            If Not UseIntegratedSecurity Then
                builder.UserID = If(UserId, "").Trim()
                If Not String.IsNullOrWhiteSpace(EncryptedPassword) AndAlso OperatingSystem.IsWindows() Then
                    builder.Password = UnprotectPassword(EncryptedPassword)
                End If
            End If

            builder.ConnectTimeout = If(ConnectTimeout > 0, ConnectTimeout, 15)
            builder.TrustServerCertificate = TrustServerCertificate
            builder.Encrypt = Encrypt
            builder.Pooling = True
            builder.MaxPoolSize = 50

            Return builder.ConnectionString
        End Function

        ''' <summary>
        ''' Serializes current settings to JSON and writes to disk.
        ''' </summary>
        Public Sub SaveToFile(filePath As String)
            If String.IsNullOrWhiteSpace(filePath) Then Throw New ArgumentNullException(NameOf(filePath))

            Dim dirPath = Path.GetDirectoryName(filePath)
            If Not String.IsNullOrWhiteSpace(dirPath) AndAlso Not Directory.Exists(dirPath) Then
                Directory.CreateDirectory(dirPath)
            End If

            Dim options As New JsonSerializerOptions With {
                .WriteIndented = True
            }

            Dim jsonString = JsonSerializer.Serialize(Me, options)
            File.WriteAllText(filePath, jsonString)
        End Sub

        ''' <summary>
        ''' Deserializes connection settings from a JSON file.
        ''' </summary>
        Public Shared Function LoadFromFile(filePath As String) As DatabaseConnectionSettings
            If String.IsNullOrWhiteSpace(filePath) OrElse Not File.Exists(filePath) Then
                Return Nothing
            End If

            Try
                Dim jsonString = File.ReadAllText(filePath)
                If String.IsNullOrWhiteSpace(jsonString) Then Return Nothing

                Dim options As New JsonSerializerOptions With {
                    .PropertyNameCaseInsensitive = True
                }

                Return JsonSerializer.Deserialize(Of DatabaseConnectionSettings)(jsonString, options)
            Catch ex As Exception
                Throw New StaffAutomation.Core.Exceptions.ConfigurationException("Database connection settings file is malformed or invalid.", "ERR_CFG_MALFORMED")
            End Try
        End Function

        ''' <summary>
        ''' Gets standard per-machine configuration file path: %APPDATA%\SHIFTWorkforce\dbconnection.json
        ''' </summary>
        Public Shared Function GetDefaultMachineConfigPath() As String
            Dim appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            Return Path.Combine(appData, "SHIFTWorkforce", "dbconnection.json")
        End Function

        ''' <summary>
        ''' Gets fallback local application directory configuration file path: dbconnection.json
        ''' </summary>
        Public Shared Function GetFallbackLocalConfigPath() As String
            Return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dbconnection.json")
        End Function
    End Class
End Namespace
