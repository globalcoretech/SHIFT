Imports System
Imports System.Windows.Forms
Imports StaffAutomation.WinForms.UIHelpers

Namespace StaffAutomation.WinForms
    ''' <summary>
    ''' Main application entry point initializing WinForms visual styles and global exception handling.
    ''' </summary>
    Friend Module Program
        <STAThread()>
        Sub Main()
            System.Windows.Forms.Application.EnableVisualStyles()
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(False)

            ' Initialize Krypton Global Enterprise Dark Theme Palette
            Try
                Dim kryptonManager As New Krypton.Toolkit.KryptonManager()
                kryptonManager.GlobalPaletteMode = Krypton.Toolkit.PaletteMode.Office2010Black
            Catch
            End Try

            ' Global unhandled exception handlers baseline
            AddHandler System.Windows.Forms.Application.ThreadException, AddressOf OnThreadException
            AddHandler AppDomain.CurrentDomain.UnhandledException, AddressOf OnUnhandledException

            ' Launch application starting at Login Screen
            Dim navigator As IViewNavigator = New ViewNavigator()
            Dim loginForm = Factory.CreateLoginForm(navigator)
            System.Windows.Forms.Application.Run(loginForm)
        End Sub

        Private _isHandlingException As Boolean = False
        Private ReadOnly _exceptionSyncLock As New Object()

        Private Sub OnThreadException(sender As Object, e As System.Threading.ThreadExceptionEventArgs)
            HandleGlobalException(e.Exception, isTerminating:=False)
        End Sub

        Private Sub OnUnhandledException(sender As Object, e As UnhandledExceptionEventArgs)
            Dim ex = TryCast(e.ExceptionObject, Exception)
            If ex Is Nothing Then
                ex = New Exception("An unknown non-Exception unhandled error occurred.")
            End If
            HandleGlobalException(ex, isTerminating:=e.IsTerminating)
        End Sub

        Private Sub HandleGlobalException(ex As Exception, isTerminating As Boolean)
            ' Recursive exception loop guard
            SyncLock _exceptionSyncLock
                If _isHandlingException Then Return
                _isHandlingException = True
            End SyncLock

            Try
                Dim correlationId = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()

                ' 1. Log structured crash details to AppLogger
                Try
                    Dim config As Core.Configuration.IAppConfiguration = New DAL.Configuration.AppConfiguration()
                    Dim logger As Core.Logging.IAppLogger = New BLL.Logging.AppLogger(config)
                    logger.LogFatal($"[CRASH] Correlation ID: {correlationId} - Unhandled exception caught.", "GlobalExceptionHandler", ex)
                Catch
                    ' Secondary logging fallback
                    Try
                        Dim logDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs")
                        If Not System.IO.Directory.Exists(logDir) Then System.IO.Directory.CreateDirectory(logDir)
                        Dim fatalLogPath = System.IO.Path.Combine(logDir, $"fatal-{DateTime.UtcNow:yyyy-MM-dd}.log")
                        System.IO.File.AppendAllText(fatalLogPath, $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] [FATAL] [{correlationId}] {ex.GetType().FullName}: {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}")
                    Catch
                    End Try
                End Try

                ' 2. Display sanitized user-facing error message without SQL strings, stack traces, or secrets
                Dim safeMessage As String = $"An unexpected system error occurred.{Environment.NewLine}{Environment.NewLine}" &
                                            $"Correlation ID: {correlationId}{Environment.NewLine}" &
                                            $"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}{Environment.NewLine}" &
                                            $"Please provide this Correlation ID to your IT support administrator."

                Forms.Common.FrmInAppAlert.ShowModal(Nothing, "System Error", safeMessage, Forms.Common.AlertType.ErrorAlert, actionText:="OK")

                If isTerminating Then
                    Application.Exit()
                End If
            Catch
                ' Suppress secondary handler exceptions
            Finally
                SyncLock _exceptionSyncLock
                    _isHandlingException = False
                End SyncLock
            End Try
        End Sub
    End Module
End Namespace
