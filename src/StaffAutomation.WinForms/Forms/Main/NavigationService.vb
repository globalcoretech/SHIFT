Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Windows.Forms

Namespace Forms.Main
    ''' <summary>
    ''' Ultra-Fast Navigation Service with View Instance Caching and Instant Screen Switching.
    ''' Eliminates WinForms layout freezing, GDI handle re-allocation, and main-thread database blocking.
    ''' </summary>
    Public Class NavigationService
        Private ReadOnly _workspacePanel As Panel
        Private ReadOnly _screenCache As New Dictionary(Of String, Control)(StringComparer.OrdinalIgnoreCase)

        ''' <summary>
        ''' Initializes a new instance of NavigationService bound to the specified workspace host panel.
        ''' </summary>
        Public Sub New(workspacePanel As Panel)
            _workspacePanel = workspacePanel
        End Sub

        ''' <summary>
        ''' Loads and activates a screen identified by a unique key instantly into the workspace panel.
        ''' </summary>
        Public Function NavigateToKey(screenKey As String, factoryMethod As Func(Of Control)) As Control
            If _workspacePanel Is Nothing OrElse factoryMethod Is Nothing Then Return Nothing

            Try
                Dim targetControl As Control = Nothing
                If _screenCache.ContainsKey(screenKey) AndAlso _screenCache(screenKey) IsNot Nothing AndAlso Not _screenCache(screenKey).IsDisposed Then
                    targetControl = _screenCache(screenKey)
                Else
                    targetControl = factoryMethod.Invoke()
                    If targetControl IsNot Nothing Then
                        _screenCache(screenKey) = targetControl
                    End If
                End If

                If targetControl IsNot Nothing Then
                    WorkspaceLoader.LoadControlIntoPanel(_workspacePanel, targetControl)
                End If
                Return targetControl
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"Navigation error for key '{screenKey}': {ex.Message}")
                Return Nothing
            End Try
        End Function

        ''' <summary>
        ''' Embeds and displays a child UserControl or Form inside the workspace container.
        ''' </summary>
        Public Sub LoadScreen(viewControl As Control)
            WorkspaceLoader.LoadControlIntoPanel(_workspacePanel, viewControl)
        End Sub

        ''' <summary>
        ''' Embeds and displays a child form inside the workspace container.
        ''' </summary>
        Public Sub LoadScreen(childForm As Form)
            WorkspaceLoader.LoadFormIntoPanel(_workspacePanel, childForm)
        End Sub
    End Class
End Namespace
