Imports System.IO
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.DAL.Configuration

Imports Xunit

Namespace StaffAutomation.Tests
    Public Class FirstRunSetupTests
        <Fact>
        Public Sub AppConfiguration_ThrowsConfigurationException_WhenNoConfigFound()
            ' Arrange
            Dim config As New AppConfiguration()
            
            ' Isolate environment variables to simulate missing config
            Environment.SetEnvironmentVariable("STAFFAUTOMATIONDB_CONNECTIONSTRING", Nothing)
            Environment.SetEnvironmentVariable("SQLSERVER_CONNECTION_STRING", Nothing)
            Environment.SetEnvironmentVariable("STAFFAUTOMATIONDB", Nothing)
            
            ' Temporarily remove dbconnection.json if exists
            Dim machinePath = DatabaseConnectionSettings.GetDefaultMachineConfigPath()
            Dim fallbackPath = DatabaseConnectionSettings.GetFallbackLocalConfigPath()
            Dim backupMachine As String = Nothing
            Dim backupFallback As String = Nothing
            
            If File.Exists(machinePath) Then
                backupMachine = File.ReadAllText(machinePath)
                File.Delete(machinePath)
            End If
            If File.Exists(fallbackPath) Then
                backupFallback = File.ReadAllText(fallbackPath)
                File.Delete(fallbackPath)
            End If
            
            Try
                ' Act & Assert
                Dim ex = Assert.Throws(Of ConfigurationException)(Sub() config.ValidateConfiguration())
                Assert.Equal("ERR_CFG_MISSING", ex.SettingKey)
            Finally
                ' Restore
                If backupMachine IsNot Nothing Then
                    File.WriteAllText(machinePath, backupMachine)
                End If
                If backupFallback IsNot Nothing Then
                    File.WriteAllText(fallbackPath, backupFallback)
                End If
            End Try
        End Sub
        
        <Fact>
        Public Sub AppConfiguration_ThrowsMalformedException_WhenConfigIsInvalidJson()
            ' Arrange
            Dim config As New AppConfiguration()
            
            Dim fallbackPath = DatabaseConnectionSettings.GetFallbackLocalConfigPath()
            Dim backupFallback As String = Nothing
            
            If File.Exists(fallbackPath) Then
                backupFallback = File.ReadAllText(fallbackPath)
            End If
            
            Try
                ' Write invalid JSON
                File.WriteAllText(fallbackPath, "{ invalid json ]")
                
                ' Act & Assert
                Dim ex = Assert.Throws(Of ConfigurationException)(Sub() config.ValidateConfiguration())
                Assert.Equal("ERR_CFG_MALFORMED", ex.SettingKey)
            Finally
                ' Restore
                If backupFallback IsNot Nothing Then
                    File.WriteAllText(fallbackPath, backupFallback)
                Else
                    File.Delete(fallbackPath)
                End If
            End Try
        End Sub
    End Class
End Namespace
