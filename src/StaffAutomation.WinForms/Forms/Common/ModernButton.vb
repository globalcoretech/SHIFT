Option Strict On
Option Explicit On

Imports System
Imports System.ComponentModel
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Text
Imports System.Windows.Forms

Namespace Forms.Common
    ''' <summary>
    ''' Ultra-Premium Anti-Aliased Modern Button Control for WinForms.
    ''' Provides true rounded corners, dynamic depth/shadow elevation, smooth hover glow,
    ''' press feedback, and crisp vector icon alignment.
    ''' </summary>
    Public Class ModernButton
        Inherits Button

        Public Enum ButtonScheme
            Custom
            Primary      ' Indigo Accent (#4F46E5)
            Secondary    ' Light Slate Card (#F8FAFC with #CBD5E1 border)
            Success      ' Emerald Green (#059669)
            Danger       ' Crimson Red (#DC2626)
            Ghost        ' Transparent with hover highlight
        End Enum

        Private _scheme As ButtonScheme = ButtonScheme.Primary
        Private _cornerRadius As Integer = 7
        Private _hoverColor As Color = Color.FromArgb(67, 56, 202)
        Private _pressedColor As Color = Color.FromArgb(55, 48, 163)
        Private _borderColor As Color = Color.FromArgb(79, 70, 229)
        Private _borderWidth As Single = 1.0!

        Private _isHovered As Boolean = False
        Private _isPressed As Boolean = False

        Public Sub New()
            Me.DoubleBuffered = True
            Me.FlatStyle = FlatStyle.Flat
            Me.FlatAppearance.BorderSize = 0
            Me.Cursor = Cursors.Hand
            Me.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
            Me.Size = New Size(120, 36)
            Me.Padding = New Padding(10, 0, 10, 0)
            ApplyScheme(_scheme)
        End Sub

        <Category("Appearance"), DefaultValue(GetType(ButtonScheme), "Primary")>
        Public Property Scheme As ButtonScheme
            Get
                Return _scheme
            End Get
            Set(value As ButtonScheme)
                _scheme = value
                ApplyScheme(_scheme)
                Me.Invalidate()
            End Set
        End Property

        <Category("Appearance"), DefaultValue(7)>
        Public Property CornerRadius As Integer
            Get
                Return _cornerRadius
            End Get
            Set(value As Integer)
                _cornerRadius = Math.Max(0, value)
                Me.Invalidate()
            End Set
        End Property

        <Category("Appearance")>
        Public Property HoverColor As Color
            Get
                Return _hoverColor
            End Get
            Set(value As Color)
                _hoverColor = value
                Me.Invalidate()
            End Set
        End Property

        <Category("Appearance")>
        Public Property PressedColor As Color
            Get
                Return _pressedColor
            End Get
            Set(value As Color)
                _pressedColor = value
                Me.Invalidate()
            End Set
        End Property

        <Category("Appearance")>
        Public Property CustomBorderColor As Color
            Get
                Return _borderColor
            End Get
            Set(value As Color)
                _borderColor = value
                Me.Invalidate()
            End Set
        End Property

        Private Sub ApplyScheme(s As ButtonScheme)
            Select Case s
                Case ButtonScheme.Primary
                    Me.BackColor = Color.FromArgb(79, 70, 229) ' #4F46E5
                    Me.ForeColor = Color.White
                    _hoverColor = Color.FromArgb(67, 56, 202)  ' #4338CA
                    _pressedColor = Color.FromArgb(55, 48, 163) ' #3730A3
                    _borderColor = Color.FromArgb(67, 56, 202)

                Case ButtonScheme.Secondary
                    Me.BackColor = Color.FromArgb(248, 250, 252) ' #F8FAFC
                    Me.ForeColor = Color.FromArgb(51, 65, 85)     ' #334155
                    _hoverColor = Color.FromArgb(241, 245, 249)  ' #F1F5F9
                    _pressedColor = Color.FromArgb(226, 232, 240) ' #E2E8F0
                    _borderColor = Color.FromArgb(203, 213, 225)  ' #CBD5E1

                Case ButtonScheme.Success
                    Me.BackColor = Color.FromArgb(16, 185, 129) ' #10B981
                    Me.ForeColor = Color.White
                    _hoverColor = Color.FromArgb(5, 150, 105)   ' #059669
                    _pressedColor = Color.FromArgb(4, 120, 87)   ' #047857
                    _borderColor = Color.FromArgb(5, 150, 105)

                Case ButtonScheme.Danger
                    Me.BackColor = Color.FromArgb(239, 68, 68) ' #EF4444
                    Me.ForeColor = Color.White
                    _hoverColor = Color.FromArgb(220, 38, 38)  ' #DC2626
                    _pressedColor = Color.FromArgb(185, 28, 28) ' #B91C1C
                    _borderColor = Color.FromArgb(220, 38, 38)

                Case ButtonScheme.Ghost
                    Me.BackColor = Color.Transparent
                    Me.ForeColor = Color.FromArgb(71, 85, 105)   ' #475569
                    _hoverColor = Color.FromArgb(241, 245, 249)
                    _pressedColor = Color.FromArgb(226, 232, 240)
                    _borderColor = Color.Transparent
            End Select
        End Sub

        Protected Overrides Sub OnMouseEnter(e As EventArgs)
            MyBase.OnMouseEnter(e)
            _isHovered = True
            Me.Invalidate()
        End Sub

        Protected Overrides Sub OnMouseLeave(e As EventArgs)
            MyBase.OnMouseLeave(e)
            _isHovered = False
            _isPressed = False
            Me.Invalidate()
        End Sub

        Protected Overrides Sub OnMouseDown(mevent As MouseEventArgs)
            MyBase.OnMouseDown(mevent)
            If mevent.Button = MouseButtons.Left Then
                _isPressed = True
                Me.Invalidate()
            End If
        End Sub

        Protected Overrides Sub OnMouseUp(mevent As MouseEventArgs)
            MyBase.OnMouseUp(mevent)
            _isPressed = False
            Me.Invalidate()
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            Dim g = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit
            g.InterpolationMode = InterpolationMode.HighQualityBicubic

            ' Clear background using parent background color to blend anti-aliased corners smoothly
            Dim parentBg = If(Me.Parent IsNot Nothing, Me.Parent.BackColor, Color.White)
            If parentBg = Color.Transparent AndAlso Me.Parent IsNot Nothing AndAlso Me.Parent.Parent IsNot Nothing Then
                parentBg = Me.Parent.Parent.BackColor
            End If
            If parentBg = Color.Transparent Then parentBg = Color.White

            Using bgBrush As New SolidBrush(parentBg)
                g.FillRectangle(bgBrush, Me.ClientRectangle)
            End Using

            ' Determine current fill color
            Dim currentBg As Color = Me.BackColor
            If Not Me.Enabled Then
                currentBg = Color.FromArgb(226, 232, 240)
            ElseIf _isPressed Then
                currentBg = _pressedColor
            ElseIf _isHovered Then
                currentBg = _hoverColor
            End If

            ' Draw button body with rounded path
            Dim rect = New RectangleF(0.5!, 0.5!, Me.Width - 1.0!, Me.Height - 1.0!)
            Using path = CreateRoundedRectanglePath(rect, _cornerRadius)
                ' Fill background
                Using fillBrush As New SolidBrush(currentBg)
                    g.FillPath(fillBrush, path)
                End Using

                ' Draw top subtle highlight line for Primary buttons (3D depth elevation)
                If _scheme = ButtonScheme.Primary AndAlso Not _isPressed AndAlso Me.Enabled Then
                    Dim topHighlightRect = New RectangleF(1.0!, 1.0!, Me.Width - 2.0!, Math.Max(2.0!, Me.Height * 0.45!))
                    Using topPath = CreateRoundedRectanglePath(topHighlightRect, Math.Max(1, _cornerRadius - 1))
                        Using topBrush As New LinearGradientBrush(topHighlightRect, Color.FromArgb(40, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 90.0!)
                            g.FillPath(topBrush, topPath)
                        End Using
                    End Using
                End If

                ' Draw border
                Dim currentBorder = _borderColor
                If _scheme = ButtonScheme.Secondary AndAlso _isHovered Then
                    currentBorder = Color.FromArgb(148, 163, 184) ' #94A3B8 on hover
                End If

                If currentBorder <> Color.Transparent Then
                    Using pen As New Pen(currentBorder, _borderWidth)
                        g.DrawPath(pen, path)
                    End Using
                End If
            End Using

            ' Draw Content (Icon + Text) with press offset
            Dim contentOffset As Integer = If(_isPressed, 1, 0)
            Dim textColor = If(Me.Enabled, Me.ForeColor, Color.FromArgb(148, 163, 184))

            Dim hasIcon As Boolean = (Me.Image IsNot Nothing)
            Dim textStr As String = Me.Text.Trim()

            ' Measure text size
            Dim textSize As Size = TextRenderer.MeasureText(g, textStr, Me.Font)
            Dim iconWidth As Integer = If(hasIcon, Me.Image.Width, 0)
            Dim iconHeight As Integer = If(hasIcon, Me.Image.Height, 0)
            Dim spacing As Integer = If(hasIcon AndAlso Not String.IsNullOrEmpty(textStr), 8, 0)

            Dim totalContentWidth As Integer = iconWidth + spacing + textSize.Width
            Dim startX As Integer = (Me.Width - totalContentWidth) \ 2 + contentOffset
            If startX < 6 Then startX = 6

            ' Draw Icon
            If hasIcon Then
                Dim iconY As Integer = (Me.Height - iconHeight) \ 2 + contentOffset
                g.DrawImage(Me.Image, New Rectangle(startX, iconY, iconWidth, iconHeight))
                startX += iconWidth + spacing
            End If

            ' Draw Text
            If Not String.IsNullOrEmpty(textStr) Then
                Dim textRect As New Rectangle(startX, contentOffset, Me.Width - startX - 4, Me.Height)
                TextRenderer.DrawText(g, textStr, Me.Font, textRect, textColor, TextFormatFlags.VerticalCenter Or TextFormatFlags.Left Or TextFormatFlags.SingleLine)
            End If
        End Sub

        Private Shared Function CreateRoundedRectanglePath(rect As RectangleF, radius As Single) As GraphicsPath
            Dim path As New GraphicsPath()
            If radius <= 0 Then
                path.AddRectangle(rect)
                Return path
            End If

            Dim diameter As Single = radius * 2.0!
            If diameter > rect.Width Then diameter = rect.Width
            If diameter > rect.Height Then diameter = rect.Height

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
