Imports System
Imports System.IO
Imports System.Text
Imports StaffAutomation.Core.Configuration
Imports StaffAutomation.Core.Constants
Imports StaffAutomation.Core.Logging

Namespace Logging
    ''' <summary>
    ''' Centralized rolling file logger implementation writing structured logs to logs/app-YYYY-MM-DD.log.
    ''' </summary>
    Public Class AppLogger
        Implements IAppLogger

        Private ReadOnly _config As IAppConfiguration
        Private ReadOnly _syncLock As New Object()

        Public Sub New(config As IAppConfiguration)
            _config = config
        End Sub

        Public Sub Log(level As LogLevel, message As String, Optional moduleName As String = "", Optional ex As Exception = Nothing) Implements IAppLogger.Log
            Try
                Dim folderPath As String = _config.GetSetting("LogFolderPath", AppConstants.DefaultLogFolderPath)
                If Not Path.IsPathRooted(folderPath) Then
                    folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folderPath)
                End If

                If Not Directory.Exists(folderPath) Then
                    Directory.CreateDirectory(folderPath)
                End If

                Dim fileName As String = $"app-{DateTime.UtcNow:yyyy-MM-dd}.log"
                Dim filePath As String = Path.Combine(folderPath, fileName)

                Dim sb As New StringBuilder()
                sb.Append($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC] ")
                sb.Append($"[{level.ToString().ToUpper()}] ")
                If Not String.IsNullOrEmpty(moduleName) Then
                    sb.Append($"[{moduleName}] ")
                End If
                sb.Append(message)

                If ex IsNot Nothing Then
                    sb.AppendLine()
                    sb.Append($"   Exception: {ex.GetType().FullName}: {ex.Message}")
                    sb.AppendLine()
                    sb.Append($"   StackTrace: {ex.StackTrace}")
                End If

                SyncLock _syncLock
                    File.AppendAllText(filePath, sb.ToString() & Environment.NewLine)
                End SyncLock
            Catch
                ' Fail-safe logging exception suppression
            End Try
        End Sub

        Public Sub LogDebug(message As String, Optional moduleName As String = "") Implements IAppLogger.LogDebug
            Log(LogLevel.Debug, message, moduleName)
        End Sub

        Public Sub LogInfo(message As String, Optional moduleName As String = "") Implements IAppLogger.LogInfo
            Log(LogLevel.Info, message, moduleName)
        End Sub

        Public Sub LogWarn(message As String, Optional moduleName As String = "") Implements IAppLogger.LogWarn
            Log(LogLevel.Warn, message, moduleName)
        End Sub

        Public Sub LogError(message As String, Optional moduleName As String = "", Optional ex As Exception = Nothing) Implements IAppLogger.LogError
            Log(LogLevel.ErrorLevel, message, moduleName, ex)
        End Sub

        Public Sub LogFatal(message As String, Optional moduleName As String = "", Optional ex As Exception = Nothing) Implements IAppLogger.LogFatal
            Log(LogLevel.Fatal, message, moduleName, ex)
        End Sub
    End Class
End Namespace
