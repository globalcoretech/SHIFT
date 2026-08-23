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
    ''' Reusable SaaS Application Search Component for WinForms.
    ''' Provides flat rounded borders, integrated search icon, clear placeholder text,
    ''' and active focus accent highlighting.
    ''' </summary>
    Public Class AppSearchBox
        Inherits UserControl

        Private txtInner As TextBox
        Private _placeholderText As String = "Search..."
        Private _isFocused As Boolean = False
        Private _isHovered As Boolean = False

        Public Event TextChangedCustom As EventHandler

        Public Sub New()
            Me.DoubleBuffered = True
            Me.Height = 32
            Me.Width = 170
            Me.BackColor = Color.White
            Me.Padding = New Padding(28, 6, 8, 6)

            txtInner = New TextBox() With {
                .BorderStyle = BorderStyle.None,
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(28, 7),
                .Width = Me.Width - 36,
                .Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top
            }

            AddHandler txtInner.TextChanged, Sub(s, e)
                                                 RaiseEvent TextChangedCustom(Me, e)
                                             End Sub

            AddHandler txtInner.GotFocus, Sub(s, e)
                                             _isFocused = True
                                             Me.Invalidate()
                                         End Sub

            AddHandler txtInner.LostFocus, Sub(s, e)
                                              _isFocused = False
                                              Me.Invalidate()
                                          End Sub

            Me.Controls.Add(txtInner)
        End Sub

        <Category("Appearance")>
        Public Overrides Property Text As String
            Get
                Return If(txtInner IsNot Nothing, txtInner.Text, "")
            End Get
            Set(value As String)
                If txtInner IsNot Nothing Then
                    txtInner.Text = value
                End If
            End Set
        End Property

        <Category("Appearance"), DefaultValue("Search...")>
        Public Property PlaceholderText As String
            Get
                Return _placeholderText
            End Get
            Set(value As String)
                _placeholderText = value
                Me.Invalidate()
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
            If txtInner IsNot Nothing Then
                txtInner.Width = Me.Width - 36
            End If
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            Dim g = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit

            ' Background
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

            ' Draw Integrated Search Icon (Magnifying Glass vector/symbol)
            Using iconFont As New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                  iconBrush As New SolidBrush(If(_isFocused, ThemeConstants.PrimaryAccent, Color.FromArgb(148, 163, 184)))
                Dim iconRect As New Rectangle(8, 0, 16, Me.Height)
                TextRenderer.DrawText(g, "🔍", iconFont, iconRect, iconBrush.Color, TextFormatFlags.VerticalCenter Or TextFormatFlags.Left)
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
