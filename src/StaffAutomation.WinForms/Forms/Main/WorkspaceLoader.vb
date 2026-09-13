Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

Namespace Forms.Main
    ''' <summary>
    ''' Manages embedding and zero-reparenting lifecycle of child views inside the Shell workspace panel.
    ''' Eliminates Krypton palette re-initialization and HWND handle creation overhead by maintaining zero-reparenting view caching.
    ''' </summary>
    Public Class WorkspaceLoader
        Public NotInheritable Class NativeMethods
            Private Sub New()
            End Sub
            Public Const WM_SETREDRAW As Integer = &HB

            <DllImport("user32.dll", CharSet:=CharSet.Auto)>
            Public Shared Function SendMessage(hWnd As IntPtr, msg As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr
            End Function
        End Class

        Private Shared _currentScreen As Control

        ''' <summary>
        ''' Embeds and activates the specified Control inside the target workspace panel.
        ''' </summary>
        Public Shared Sub LoadControlIntoPanel(targetPanel As Panel, viewControl As Control, Optional useRedrawSuppression As Boolean = False)
            If targetPanel Is Nothing OrElse viewControl Is Nothing Then Return

            ' If viewControl is already active and visible on top, no swap needed.
            If targetPanel.Controls.Count > 0 AndAlso Object.ReferenceEquals(targetPanel.Controls(0), viewControl) AndAlso viewControl.Visible Then
                Return
            End If

            Dim sw As Stopwatch = Stopwatch.StartNew()
            Debug.WriteLine($"[WorkspaceLoader] Executing view swap for '{viewControl.GetType().Name}'...")

            Try
                ' SuspendLayout on the main content host.
                targetPanel.SuspendLayout()
                viewControl.SuspendLayout()

                ' Prepare the new UserControl
                ThemeConstants.EnableDoubleBuffering(targetPanel)
                ThemeConstants.EnableDoubleBuffering(viewControl)

                If TypeOf viewControl Is Form Then
                    Dim frm = CType(viewControl, Form)
                    frm.TopLevel = False
                    frm.FormBorderStyle = FormBorderStyle.None
                    frm.WindowState = FormWindowState.Normal
                    frm.AutoSize = False
                ElseIf TypeOf viewControl Is ScrollableControl Then
                    CType(viewControl, ScrollableControl).AutoScroll = False
                End If

                ' Set newScreen.Dock = DockStyle.Fill
                viewControl.Dock = DockStyle.Fill

                ' Add newScreen to the content host if it has never been created
                If Not targetPanel.Controls.Contains(viewControl) Then
                    viewControl.Visible = False
                    targetPanel.Controls.Add(viewControl)
                End If

                ' Hide the currently visible screen
                For Each ctrl As Control In targetPanel.Controls
                    If Not Object.ReferenceEquals(ctrl, viewControl) AndAlso ctrl.Visible Then
                        ctrl.Visible = False
                    End If
                Next

                ' Make the target screen visible and bring to front
                viewControl.Visible = True
                viewControl.BringToFront()
                
                ' Set current screen reference to newScreen
                _currentScreen = viewControl

            Finally
                ' ResumeLayout(True)
                viewControl.ResumeLayout(False)
                targetPanel.ResumeLayout(True)
                
                viewControl.PerformLayout()
            End Try

            sw.Stop()
            Debug.WriteLine($"[WorkspaceLoader] View swap finished in {sw.ElapsedMilliseconds} ms for '{viewControl.GetType().Name}'.")
        End Sub

        Public Shared Sub LoadFormIntoPanel(targetPanel As Panel, childForm As Form)
            LoadControlIntoPanel(targetPanel, childForm)
        End Sub
    End Class
End Namespace
