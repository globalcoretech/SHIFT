Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms

Namespace Forms.Main
    ''' <summary>
    ''' Ultra-smooth double-buffered workspace host panel.
    ''' Suppresses WM_ERASEBKGND white background flashes during screen navigation swaps.
    ''' </summary>
    Public Class WorkspaceHostPanel
        Inherits Panel

        Public Sub New()
            MyBase.New()
            SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
            UpdateStyles()
            Me.DoubleBuffered = True
        End Sub

        Protected Overrides Sub OnPaintBackground(pevent As PaintEventArgs)
            Using b As New SolidBrush(ThemeConstants.WorkspaceBackground)
                pevent.Graphics.FillRectangle(b, pevent.ClipRectangle)
            End Using
        End Sub
    End Class
End Namespace
