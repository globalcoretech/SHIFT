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
            ' 1. Check Environment Variable Precedence (e.g., STAFFAUTOMATIONDB_CONNECTIONSTRING, SQLSERVER_CONNECTION_STRING, or StaffAutomationDb_ConnectionString)
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

            ' 2. Fallback to App.config ConnectionStrings
            Dim connObj = ConfigurationManager.ConnectionStrings(name)
            If connObj Is Nothing OrElse String.IsNullOrWhiteSpace(connObj.ConnectionString) Then
                Throw New StaffAutomation.Core.Exceptions.ConfigurationException($"Database connection string '{name}' was not found in Environment variables or App.config.", name)
            End If
            Return connObj.ConnectionString
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
