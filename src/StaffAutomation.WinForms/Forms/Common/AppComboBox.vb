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
    ''' Reusable SaaS Application ComboBox Control for WinForms.
    ''' Eliminates legacy Windows 3D/bevel appearance by wrapping ComboBox in a flat container panel
    ''' with anti-aliased rounded borders, clean dropdown arrow, and active focus accent highlighting.
    ''' </summary>
    Public Class AppComboBox
        Inherits UserControl

        Private cboInner As ComboBox
        Private _isFocused As Boolean = False
        Private _isHovered As Boolean = False

        Public Event SelectedIndexChangedCustom As EventHandler
        Public Event SelectedIndexChanged As EventHandler

        Public Sub New()
            Me.DoubleBuffered = True
            Me.Height = 32
            Me.Width = 135
            Me.BackColor = Color.White
            Me.Padding = New Padding(8, 4, 22, 4)

            cboInner = New ComboBox() With {
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .FlatStyle = FlatStyle.Standard,
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular),
                .ForeColor = ThemeConstants.TextPrimary,
                .BackColor = Color.White,
                .Location = New Point(3, 3),
                .Width = Me.Width - 6,
                .Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top
            }

            AddHandler cboInner.SelectedIndexChanged, Sub(s, e)
                                                          RaiseEvent SelectedIndexChangedCustom(Me, e)
                                                          RaiseEvent SelectedIndexChanged(Me, e)
                                                      End Sub

            AddHandler cboInner.GotFocus, Sub(s, e)
                                             _isFocused = True
                                             Me.Invalidate()
                                         End Sub

            AddHandler cboInner.LostFocus, Sub(s, e)
                                              _isFocused = False
                                              Me.Invalidate()
                                          End Sub

            Me.Controls.Add(cboInner)
        End Sub

        Public ReadOnly Property Items As ComboBox.ObjectCollection
            Get
                Return cboInner.Items
            End Get
        End Property

        Public Property DropDownStyle As ComboBoxStyle
            Get
                Return cboInner.DropDownStyle
            End Get
            Set(value As ComboBoxStyle)
                cboInner.DropDownStyle = value
            End Set
        End Property

        Public Property SelectedIndex As Integer
            Get
                Return cboInner.SelectedIndex
            End Get
            Set(value As Integer)
                cboInner.SelectedIndex = value
            End Set
        End Property

        Public Property SelectedItem As Object
            Get
                Return cboInner.SelectedItem
            End Get
            Set(value As Object)
                cboInner.SelectedItem = value
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
            If cboInner IsNot Nothing Then
                cboInner.Width = Me.Width - 6
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

            ' Control Body Fill & Outer Border
            Dim rectF = New RectangleF(0.5!, 0.5!, Me.Width - 1.0!, Me.Height - 1.0!)
            Using bodyPath = CreateRoundedRectanglePath(rectF, 4.0!)
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

        Private Shared Sub PointArrayHelperDrawTriangle(g As Graphics, x As Integer, y As Integer, c As Color)
            Dim points As Point() = {
                New Point(x, y),
                New Point(x + 8, y),
                New Point(x + 4, y + 5)
            }
            Using brush As New SolidBrush(c)
                g.FillPolygon(brush, points)
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
