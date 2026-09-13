Imports System.Configuration
Imports StaffAutomation.Core.Configuration
Imports StaffAutomation.Core.Constants
Imports StaffAutomation.Core.Exceptions

Namespace Configuration
    ''' <summary>
    ''' Implementation of application configuration manager handling App.config settings and validation.
    ''' Resides in DAL Infrastructure layer to preserve Core domain purity.
    ''' </summary>
    Public Class AppConfiguration
        Implements IAppConfiguration

        Public Function GetConnectionString(Optional name As String = "StaffAutomationDb") As String Implements IAppConfiguration.GetConnectionString
            ' Precedence 1: Environment Variable
            Dim envConn = Environment.GetEnvironmentVariable("STAFFAUTOMATIONDB_CONNECTIONSTRING")
            If String.IsNullOrWhiteSpace(envConn) Then
                envConn = Environment.GetEnvironmentVariable("SQLSERVER_CONNECTION_STRING")
            End If
            If String.IsNullOrWhiteSpace(envConn) Then
                envConn = Environment.GetEnvironmentVariable($"{name.ToUpper()}_CONNECTIONSTRING")
            End If
            If String.IsNullOrWhiteSpace(envConn) Then
                envConn = Environment.GetEnvironmentVariable(name)
            End If

            If Not String.IsNullOrWhiteSpace(envConn) Then
                Return envConn.Trim()
            End If

            ' Precedence 2: Local per-machine dbconnection.json (%APPDATA%\SHIFTWorkforce\dbconnection.json or local fallback)
            Dim jsonConnStr = GetLocalMachineJsonConnectionString()
            If Not String.IsNullOrWhiteSpace(jsonConnStr) Then
                Return jsonConnStr
            End If

            ' Precedence 3: Existing App.config ConnectionStrings
            Dim connObj = ConfigurationManager.ConnectionStrings(name)
            If connObj IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(connObj.ConnectionString) Then
                Return connObj.ConnectionString
            End If

            ' Precedence 4: Clear Configuration Exception if no valid configuration exists
            Throw New StaffAutomation.Core.Exceptions.ConfigurationException(
                $"Configuration Error: Database connection string '{name}' was not found. Checked: (1) Environment Variables, (2) Local Machine dbconnection.json, and (3) App.config. Please configure database settings via System Settings or Environment Variables.", name)
        End Function

        ''' <summary>
        ''' Reads local per-machine dbconnection.json configuration from %APPDATA%\SHIFTWorkforce or application root.
        ''' </summary>
        Private Function GetLocalMachineJsonConnectionString() As String
            Try
                ' Check Primary Machine Path: %APPDATA%\SHIFTWorkforce\dbconnection.json
                Dim machinePath = DatabaseConnectionSettings.GetDefaultMachineConfigPath()
                If System.IO.File.Exists(machinePath) Then
                    Dim settings = DatabaseConnectionSettings.LoadFromFile(machinePath)
                    If settings IsNot Nothing Then
                        Dim connStr = settings.BuildConnectionString()
                        If Not String.IsNullOrWhiteSpace(connStr) Then Return connStr
                    End If
                End If

                ' Check Secondary Fallback Path: dbconnection.json in app directory
                Dim fallbackPath = DatabaseConnectionSettings.GetFallbackLocalConfigPath()
                If System.IO.File.Exists(fallbackPath) Then
                    Dim settings = DatabaseConnectionSettings.LoadFromFile(fallbackPath)
                    If settings IsNot Nothing Then
                        Dim connStr = settings.BuildConnectionString()
                        If Not String.IsNullOrWhiteSpace(connStr) Then Return connStr
                    End If
                End If
            Catch
            End Try

            Return Nothing
        End Function

        Public Function GetSetting(key As String, Optional defaultValue As String = "") As String Implements IAppConfiguration.GetSetting
            ' 1. Environment Variable Precedence (e.g., GST_API_KEY or GstApiKey)
            Dim envVal = Environment.GetEnvironmentVariable(key)
            If Not String.IsNullOrWhiteSpace(envVal) Then
                Return envVal.Trim()
            End If

            Dim envKeyUpper = ConvertKeyToEnvFormat(key)
            If Not String.Equals(envKeyUpper, key, StringComparison.Ordinal) Then
                Dim envValUpper = Environment.GetEnvironmentVariable(envKeyUpper)
                If Not String.IsNullOrWhiteSpace(envValUpper) Then
                    Return envValUpper.Trim()
                End If
            End If

            ' 2. App.config AppSettings Fallback
            Dim val = ConfigurationManager.AppSettings(key)
            If String.IsNullOrWhiteSpace(val) Then
                Return defaultValue
            End If
            Return val
        End Function

        Private Function ConvertKeyToEnvFormat(key As String) As String
            If key = "GstApiKey" Then Return "GST_API_KEY"
            If key = "GstApiSecret" Then Return "GST_API_SECRET"
            Return key.ToUpper()
        End Function

        Public Function GetSettingInt(key As String, Optional defaultValue As Integer = 0) As Integer Implements IAppConfiguration.GetSettingInt
            Dim valStr = GetSetting(key, "")
            Dim parsedVal As Integer = 0
            If Integer.TryParse(valStr, parsedVal) Then
                Return parsedVal
            End If
            Return defaultValue
        End Function

        Public Sub ValidateConfiguration() Implements IAppConfiguration.ValidateConfiguration
            Dim connStr = GetConnectionString(AppConstants.DefaultConnectionStringName)
            If String.IsNullOrWhiteSpace(connStr) Then
                Throw New StaffAutomation.Core.Exceptions.ConfigurationException("Configuration validation failed: Connection string is null or empty.")
            End If
        End Sub
    End Class
End Namespace
