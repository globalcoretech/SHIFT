Imports System
Imports System.Threading
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports StaffAutomation.WinForms.UIHelpers

Namespace StaffAutomation.WinForms
    ''' <summary>
    ''' Main application entry point initializing WinForms visual styles and global exception handling.
    ''' </summary>
    Friend Module Program
        <DllImport("user32.dll", SetLastError:=True, CharSet:=CharSet.Auto)>
        Private Function PostMessage(hWnd As IntPtr, Msg As UInteger, wParam As IntPtr, lParam As IntPtr) As Boolean
        End Function

        <DllImport("user32.dll", SetLastError:=True, CharSet:=CharSet.Auto)>
        Private Function RegisterWindowMessage(lpString As String) As UInteger
        End Function

        Private Const HWND_BROADCAST As Integer = &HFFFF
        Private _wakeUpMessage As UInteger
        Private _appMutex As Mutex

        <STAThread()>
        Sub Main()
            _wakeUpMessage = RegisterWindowMessage("StaffAutomation_WakeUp_Signal")
            
            Dim createdNew As Boolean
            _appMutex = New Mutex(True, "Global\CAOfficeWorkforceAutomation_Instance", createdNew)

            If Not createdNew Then
                ' Secondary instance detected. Signal the primary instance and exit immediately.
                PostMessage(New IntPtr(HWND_BROADCAST), _wakeUpMessage, IntPtr.Zero, IntPtr.Zero)
                Return
            End If

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

            ' First-run / Configuration Setup
            Dim isConfigured As Boolean = False
            While Not isConfigured
                Try
                    Dim config As Core.Configuration.IAppConfiguration = New DAL.Configuration.AppConfiguration()
                    config.ValidateConfiguration()
                    isConfigured = True
                Catch ex As Core.Exceptions.ConfigurationException
                    If ex.SettingKey = "ERR_CFG_MALFORMED" Then
                        Throw ' Let global handler catch malformed config
                    End If
                    
                    ' Missing config - First run setup
                    Using setupForm = New Forms.Admin.FrmDatabaseConnectionConfig()
                        If setupForm.ShowDialog() <> DialogResult.OK Then
                            ' User cancelled setup
                            Return
                        End If
                    End Using
                End Try
            End While

            ' Launch application starting at Login Screen
            Dim navigator As IViewNavigator = New ViewNavigator()
            Dim loginForm = Factory.CreateLoginForm(navigator)
            System.Windows.Forms.Application.Run(loginForm)

            GC.KeepAlive(_appMutex)
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
                Dim activeFormName = If(Application.OpenForms.Count > 0 AndAlso Application.OpenForms(Application.OpenForms.Count - 1) IsNot Nothing, Application.OpenForms(Application.OpenForms.Count - 1).GetType().Name, "UnknownForm")

                Dim fullLogDump As String = $"========================================================================{Environment.NewLine}" &
                                            $"[CRASH DUMP] Correlation ID: {correlationId} | Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}" &
                                            $"Active Form: {activeFormName} | IsTerminating: {isTerminating}{Environment.NewLine}" &
                                            $"Exception Type: {ex.GetType().FullName}{Environment.NewLine}" &
                                            $"Message: {ex.Message}{Environment.NewLine}" &
                                            $"StackTrace: {Environment.NewLine}{ex.StackTrace}{Environment.NewLine}" &
                                            $"InnerException: {If(ex.InnerException IsNot Nothing, ex.InnerException.GetType().FullName & ": " & ex.InnerException.Message, "None")}{Environment.NewLine}" &
                                            $"Inner StackTrace: {If(ex.InnerException IsNot Nothing, Environment.NewLine & ex.InnerException.StackTrace, "None")}{Environment.NewLine}" &
                                            $"========================================================================"

                System.Diagnostics.Debug.WriteLine(fullLogDump)

                ' 1. Log structured crash details to AppLogger
                Try
                    Dim config As Core.Configuration.IAppConfiguration = New DAL.Configuration.AppConfiguration()
                    Dim logger As Core.Logging.IAppLogger = New BLL.Logging.AppLogger(config)
                    logger.LogFatal(fullLogDump, "GlobalExceptionHandler", ex)
                Catch
                    ' Secondary logging fallback
                    Try
                        Dim logDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs")
                        If Not System.IO.Directory.Exists(logDir) Then System.IO.Directory.CreateDirectory(logDir)
                        Dim fatalLogPath = System.IO.Path.Combine(logDir, $"fatal-{DateTime.UtcNow:yyyy-MM-dd}.log")
                        System.IO.File.AppendAllText(fatalLogPath, fullLogDump & Environment.NewLine)
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
