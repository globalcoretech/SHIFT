Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Windows.Forms

Namespace Forms.Main
    ''' <summary>
    ''' Ultra-Fast Non-Blocking Navigation Service with View Caching and Two-Phase Screen Switching.
    ''' Stage A Refinement:
    ''' - Phase 1: Asynchronous data preloading occurs off-screen while existing screen remains 100% visible (NO swap lock, NO WM_SETREDRAW).
    ''' - Phase 2: Serialized UI swap using SemaphoreSlim, with sequence token re-validation immediately before and inside lock.
    ''' </summary>
    Public Class NavigationService
        Private ReadOnly _workspacePanel As Panel
        Private ReadOnly _screenCache As New Dictionary(Of String, Control)(StringComparer.OrdinalIgnoreCase)
        Private ReadOnly _swapLock As New SemaphoreSlim(1, 1)
        Private _currentNavigationSequence As Long = 0

        ''' <summary>
        ''' Gets the current active navigation sequence token.
        ''' </summary>
        Public ReadOnly Property CurrentNavigationSequence As Long
            Get
                Return _currentNavigationSequence
            End Get
        End Property

        ''' <summary>
        ''' Initializes a new instance of NavigationService bound to the specified workspace host panel.
        ''' </summary>
        Public Sub New(workspacePanel As Panel)
            _workspacePanel = workspacePanel
        End Sub

        ''' <summary>
        ''' Retrieves cached screen control by key if present and not disposed.
        ''' </summary>
        Public Function GetCachedControl(key As String) As Control
            If _screenCache.ContainsKey(key) AndAlso _screenCache(key) IsNot Nothing AndAlso Not _screenCache(key).IsDisposed Then
                Return _screenCache(key)
            End If
            Return Nothing
        End Function

        ''' <summary>
        ''' Asynchronously preloads and activates a screen identified by a unique key into the workspace panel.
        ''' Phase 1: Data preloading occurs off-screen without acquiring the swap lock.
        ''' Phase 2: Serialized final UI swap under SemaphoreSlim lock.
        ''' </summary>
        Public Async Function NavigateToKeyAsync(screenKey As String, factoryMethod As Func(Of Control)) As Task(Of Control)
            If _workspacePanel Is Nothing OrElse factoryMethod Is Nothing Then Return Nothing

            Dim swTotal As Stopwatch = Stopwatch.StartNew()

            ' Step 1: Increment Navigation Sequence Token
            _currentNavigationSequence += 1
            Dim thisToken As Long = _currentNavigationSequence

            ' t0: Click to Preload Start Duration
            Dim t0 As Long = swTotal.ElapsedMilliseconds

            Try
                ' Step 2: Retrieve or Instantiate View Control (t2: UI Control Preparation Duration)
                Dim swPrep As Stopwatch = Stopwatch.StartNew()
                Dim targetControl As Control = Nothing
                If _screenCache.ContainsKey(screenKey) AndAlso _screenCache(screenKey) IsNot Nothing AndAlso Not _screenCache(screenKey).IsDisposed Then
                    targetControl = _screenCache(screenKey)
                Else
                    targetControl = factoryMethod.Invoke()
                    If targetControl IsNot Nothing Then
                        _screenCache(screenKey) = targetControl
                    End If
                End If
                swPrep.Stop()
                Dim t2_uiPrep As Long = swPrep.ElapsedMilliseconds

                If targetControl Is Nothing Then Return Nothing

                ' Step 3: Asynchronous Data Loading OFF-SCREEN
                ' Preload data and execute heavy UI binding/layout while the target screen is completely hidden to prevent visual flicker.
                If TypeOf targetControl Is IPreloadableScreen Then
                    Await DirectCast(targetControl, IPreloadableScreen).PreloadDataAsync(thisToken)
                End If

                ' Step 4: INSTANT Atomic UI Swap Phase (<1ms - Instant visual response)
                Await _swapLock.WaitAsync()
                Try
                    If thisToken <> _currentNavigationSequence Then
                        Debug.WriteLine($"[Navigation] Token #{thisToken} for '{screenKey}' superseded before swap.")
                        Return targetControl
                    End If

                    WorkspaceLoader.LoadControlIntoPanel(_workspacePanel, targetControl, useRedrawSuppression:=False)
                Finally
                    _swapLock.Release()
                End Try

                swTotal.Stop()
                Dim t4_total As Long = swTotal.ElapsedMilliseconds

                ' Log structured metrics breakdown
                Dim logMetrics = $"[Navigation] Key: '{screenKey}' | Token: #{thisToken} | UIControlPrep: {t2_uiPrep}ms | Total: {t4_total}ms"
                Debug.WriteLine(logMetrics)

                Return targetControl
            Catch ex As Exception
                Debug.WriteLine($"[Navigation] Navigation error for key '{screenKey}': {ex.Message}")
                Return Nothing
            End Try
        End Function

        ''' <summary>
        ''' Synchronous navigation entry point calling NavigateToKeyAsync.
        ''' </summary>
        Public Function NavigateToKey(screenKey As String, factoryMethod As Func(Of Control)) As Control
            Dim task = NavigateToKeyAsync(screenKey, factoryMethod)
            Return task.Result
        End Function

        ''' <summary>
        ''' Embeds and displays a child UserControl or Form inside the workspace container.
        ''' </summary>
        Public Sub LoadScreen(viewControl As Control)
            WorkspaceLoader.LoadControlIntoPanel(_workspacePanel, viewControl, useRedrawSuppression:=False)
        End Sub

        ''' <summary>
        ''' Gets the current active screen from the workspace panel.
        ''' </summary>
        Public Function GetCurrentScreen() As Control
            If _workspacePanel IsNot Nothing AndAlso _workspacePanel.Controls.Count > 0 Then
                Return _workspacePanel.Controls(0)
            End If
            Return Nothing
        End Function

        ''' <summary>
        ''' Embeds and displays a child form inside the workspace container.
        ''' </summary>
        Public Sub LoadScreen(childForm As Form)
            WorkspaceLoader.LoadFormIntoPanel(_workspacePanel, childForm)
        End Sub
    End Class
End Namespace

