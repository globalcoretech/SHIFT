Imports System

Namespace Logging
    ''' <summary>
    ''' Contract for centralized file logger across all application layers.
    ''' </summary>
    Public Interface IAppLogger
        Sub Log(level As LogLevel, message As String, Optional moduleName As String = "", Optional ex As Exception = Nothing)
        Sub LogDebug(message As String, Optional moduleName As String = "")
        Sub LogInfo(message As String, Optional moduleName As String = "")
        Sub LogWarn(message As String, Optional moduleName As String = "")
        Sub LogError(message As String, Optional moduleName As String = "", Optional ex As Exception = Nothing)
        Sub LogFatal(message As String, Optional moduleName As String = "", Optional ex As Exception = Nothing)
    End Interface
End Namespace
