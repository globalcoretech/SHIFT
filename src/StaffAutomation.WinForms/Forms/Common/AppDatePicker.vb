Option Strict On
Option Explicit On

Imports System
Imports System.ComponentModel
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Text
Imports System.Windows.Forms
Imports StaffAutomation.WinForms.Forms.Main

Namespace Forms.Common
    ''' <summary>
    ''' Reusable SaaS Application DatePicker Control for WinForms.
    ''' Eliminates native Windows 3D/bevel appearance by wrapping DateTimePicker in a flat container panel
    ''' with anti-aliased rounded borders and active focus accent highlighting.
    ''' </summary>
    Public Class AppDatePicker
        Inherits UserControl

        Private dtpInner As DateTimePicker
        Private _isFocused As Boolean = False
        Private _isHovered As Boolean = False

        Public Event ValueChangedCustom As EventHandler

        Public Sub New()
            Me.DoubleBuffered = True
            Me.Height = 32
            Me.Width = 115
            Me.BackColor = Color.White
            Me.Padding = New Padding(6, 4, 6, 4)

            dtpInner = New DateTimePicker() With {
                .Format = DateTimePickerFormat.Short,
                .Value = DateTime.Today,
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular),
                .Location = New Point(4, 5),
                .Width = Me.Width - 8,
                .Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top
            }

            AddHandler dtpInner.ValueChanged, Sub(s, e)
                                                  RaiseEvent ValueChangedCustom(Me, e)
                                              End Sub

            AddHandler dtpInner.GotFocus, Sub(s, e)
                                             _isFocused = True
                                             Me.Invalidate()
                                         End Sub

            AddHandler dtpInner.LostFocus, Sub(s, e)
                                              _isFocused = False
                                              Me.Invalidate()
                                          End Sub

            Me.Controls.Add(dtpInner)
        End Sub

        Public Property Value As DateTime
            Get
                Return If(dtpInner IsNot Nothing, dtpInner.Value, DateTime.Today)
            End Get
            Set(value As DateTime)
                If dtpInner IsNot Nothing Then
                    dtpInner.Value = value
                End If
            End Set
        End Property

        Protected Overrides Sub OnMouseEnter(e As EventArgs)
            MyBase.OnMouseEnter(e)
            _isHovered = True
            Me.Invalidate()
        End Sub

        Protected Overrides Sub OnMouseLeave(e As EventArgs)
            MyBase.OnMouseLeave(e)
            _isHovered = False
            Me.Invalidate()
        End Sub

        Protected Overrides Sub OnResize(e As EventArgs)
            MyBase.OnResize(e)
            If dtpInner IsNot Nothing Then
                dtpInner.Width = Me.Width - 8
            End If
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            Dim g = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit

            ' Background fill
            Dim parentBg = If(Me.Parent IsNot Nothing, Me.Parent.BackColor, Color.White)
            Using bgBrush As New SolidBrush(parentBg)
                g.FillRectangle(bgBrush, Me.ClientRectangle)
            End Using

            ' Control Body Fill
            Dim rectF = New RectangleF(0.5!, 0.5!, Me.Width - 1.0!, Me.Height - 1.0!)
            Using bodyPath = CreateRoundedRectanglePath(rectF, 6.0!)
                Using bodyBrush As New SolidBrush(Color.White)
                    g.FillPath(bodyBrush, bodyPath)
                End Using

                ' Border Color
                Dim borderColor As Color = Color.FromArgb(203, 213, 225) ' Slate-300
                Dim borderWidth As Single = 1.0!

                If _isFocused Then
                    borderColor = ThemeConstants.PrimaryAccent '#4F46E5 Indigo Focus
                    borderWidth = 1.5!
                ElseIf _isHovered Then
                    borderColor = Color.FromArgb(148, 163, 184) ' Slate-400 Hover
                End If

                Using borderPen As New Pen(borderColor, borderWidth)
                    g.DrawPath(borderPen, bodyPath)
                End Using
            End Using
        End Sub

        Private Shared Function CreateRoundedRectanglePath(rect As RectangleF, radius As Single) As GraphicsPath
            Dim path As New GraphicsPath()
            Dim diameter As Single = radius * 2.0!
            Dim arc As New RectangleF(rect.X, rect.Y, diameter, diameter)

            path.AddArc(arc, 180, 90)

            arc.X = rect.Right - diameter
            path.AddArc(arc, 270, 90)

            arc.Y = rect.Bottom - diameter
            path.AddArc(arc, 0, 90)

            arc.X = rect.X
            path.AddArc(arc, 90, 90)

            path.CloseFigure()
            Return path
        End Function
    End Class
End Namespace
