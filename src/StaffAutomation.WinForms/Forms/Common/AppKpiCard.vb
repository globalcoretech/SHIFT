Option Strict On
Option Explicit On

Imports System
Imports System.ComponentModel
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Text
Imports System.Windows.Forms
Imports FontAwesome.Sharp
Imports StaffAutomation.WinForms.Forms.Main

Namespace Forms.Common
    ''' <summary>
    ''' Enhanced SaaS KPI Card Control integrated with FontAwesome.Sharp vector icons
    ''' and Krypton Toolkit color palettes.
    ''' Eliminates emoji/unicode fallback box rendering issues.
    ''' </summary>
    Public Class AppKpiCard
        Inherits Control

        Public Enum KpiScheme
            Neutral
            Healthy          ' Emerald Tint (#ECFDF5) - Green
            Info             ' Sky Tint (#F0F9FF) - Blue
            Warning          ' Amber Tint (#FFFBEB) - Yellow/Amber
            Critical         ' Crimson Tint (#FEF2F2) - Red
            OrangeAlert      ' Orange Tint (#FFF7ED)

            ' Domain Aliases for Backward Compatibility
            PresentToday = Healthy
            CurrentlyWorking = Info
            OnLunchBreak = Warning
            LateArrivals = Critical
            NotPunchedIn = OrangeAlert
        End Enum

        Private _scheme As KpiScheme = KpiScheme.Neutral
        Private _titleText As String = "METRIC TITLE"
        Private _valueText As String = "0"
        Private _count As Integer = 0
        Private _iconChar As IconChar = IconChar.ChartBar
        Private _isSelected As Boolean = False
        Private _filterStatusTag As String = ""

        Private _isHovered As Boolean = False

        Public Sub New()
            Me.DoubleBuffered = True
            Me.Size = New Size(198, 88)
            Me.Cursor = Cursors.Hand
            Me.Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular)
        End Sub

        <Category("Appearance"), DefaultValue(GetType(KpiScheme), "Neutral")>
        Public Property Scheme As KpiScheme
            Get
                Return _scheme
            End Get
            Set(value As KpiScheme)
                _scheme = value
                Me.Invalidate()
            End Set
        End Property

        <Category("Appearance"), DefaultValue("METRIC TITLE")>
        Public Property TitleText As String
            Get
                Return _titleText
            End Get
            Set(value As String)
                _titleText = value
                Me.Invalidate()
            End Set
        End Property

        <Category("Appearance"), DefaultValue("0")>
        Public Property ValueText As String
            Get
                Return _valueText
            End Get
            Set(value As String)
                _valueText = value
                Me.Invalidate()
            End Set
        End Property

        <Category("Data"), DefaultValue(0)>
        Public Property Count As Integer
            Get
                Return _count
            End Get
            Set(value As Integer)
                _count = value
                Me.Invalidate()
            End Set
        End Property

        <Category("Appearance"), DefaultValue(GetType(IconChar), "ChartBar")>
        Public Property Icon As IconChar
            Get
                Return _iconChar
            End Get
            Set(value As IconChar)
                _iconChar = value
                Me.Invalidate()
            End Set
        End Property

        <Category("Behavior"), DefaultValue(False)>
        Public Property IsSelected As Boolean
            Get
                Return _isSelected
            End Get
            Set(value As Boolean)
                _isSelected = value
                Me.Invalidate()
            End Set
        End Property

        <Category("Behavior"), DefaultValue("")>
        Public Property FilterStatusTag As String
            Get
                Return _filterStatusTag
            End Get
            Set(value As String)
                _filterStatusTag = value
            End Set
        End Property

        Public Property Title As String
            Get
                Return TitleText
            End Get
            Set(value As String)
                TitleText = value
            End Set
        End Property

        Public Property Value As String
            Get
                Return ValueText
            End Get
            Set(value As String)
                ValueText = value
            End Set
        End Property

        Public Property Subtext As String
            Get
                Return FilterStatusTag
            End Get
            Set(value As String)
                FilterStatusTag = value
            End Set
        End Property

        Public Property AccentColor As Color
            Get
                Return Color.Empty
            End Get
            Set(value As Color)
                ' No-op for backwards compatibility
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

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            Dim g = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit

            ' Clear background with parent/workspace color for clean rounded corners
            Dim parentBg = If(Me.Parent IsNot Nothing, Me.Parent.BackColor, Color.White)
            If parentBg = Color.Transparent Then parentBg = ThemeConstants.WorkspaceBackground

            Using bgBrush As New SolidBrush(parentBg)
                g.FillRectangle(bgBrush, Me.ClientRectangle)
            End Using

            ' Derive Color Scheme Tokens
            Dim surfaceBg As Color
            Dim borderColor As Color
            Dim iconBgColor As Color
            Dim iconColor As Color
            Dim titleColor As Color
            Dim valueColor As Color
            Dim subtextColor As Color

            Select Case _scheme
                Case KpiScheme.Healthy, KpiScheme.PresentToday
                    surfaceBg = Color.FromArgb(236, 253, 245)     ' Emerald-50
                    borderColor = Color.FromArgb(167, 243, 208)   ' Emerald-200
                    iconBgColor = Color.FromArgb(209, 250, 229)   ' Emerald-100
                    iconColor = Color.FromArgb(16, 185, 129)     ' Emerald-500
                    titleColor = Color.FromArgb(6, 95, 70)       ' Emerald-800
                    valueColor = Color.FromArgb(4, 120, 87)      ' Emerald-700
                    subtextColor = Color.FromArgb(15, 118, 110)

                Case KpiScheme.Info, KpiScheme.CurrentlyWorking
                    surfaceBg = Color.FromArgb(240, 249, 255)     ' Sky-50
                    borderColor = Color.FromArgb(186, 230, 253)   ' Sky-200
                    iconBgColor = Color.FromArgb(224, 242, 254)   ' Sky-100
                    iconColor = Color.FromArgb(14, 165, 233)     ' Sky-500
                    titleColor = Color.FromArgb(7, 89, 133)      ' Sky-800
                    valueColor = Color.FromArgb(3, 105, 161)     ' Sky-700
                    subtextColor = Color.FromArgb(3, 105, 161)

                Case KpiScheme.Warning, KpiScheme.OnLunchBreak
                    surfaceBg = Color.FromArgb(255, 251, 235)     ' Amber-50
                    borderColor = Color.FromArgb(253, 230, 138)   ' Amber-200
                    iconBgColor = Color.FromArgb(254, 243, 199)   ' Amber-100
                    iconColor = Color.FromArgb(245, 158, 11)     ' Amber-500
                    titleColor = Color.FromArgb(146, 64, 14)     ' Amber-800
                    valueColor = Color.FromArgb(180, 83, 9)      ' Amber-700
                    subtextColor = Color.FromArgb(180, 83, 9)

                Case KpiScheme.Critical, KpiScheme.LateArrivals
                    surfaceBg = Color.FromArgb(254, 242, 242)     ' Red-50
                    borderColor = Color.FromArgb(254, 202, 202)   ' Red-200
                    iconBgColor = Color.FromArgb(254, 226, 226)   ' Red-100
                    iconColor = Color.FromArgb(239, 68, 68)     ' Red-500
                    titleColor = Color.FromArgb(153, 27, 27)     ' Red-800
                    valueColor = Color.FromArgb(185, 28, 28)     ' Red-700
                    subtextColor = Color.FromArgb(185, 28, 28)

                Case KpiScheme.OrangeAlert, KpiScheme.NotPunchedIn
                    surfaceBg = Color.FromArgb(255, 247, 237)     ' Orange-50
                    borderColor = Color.FromArgb(253, 186, 116)   ' Orange-200
                    iconBgColor = Color.FromArgb(254, 215, 170)   ' Orange-100
                    iconColor = Color.FromArgb(249, 115, 22)     ' Orange-500
                    titleColor = Color.FromArgb(154, 52, 18)     ' Orange-800
                    valueColor = Color.FromArgb(194, 65, 12)     ' Orange-700
                    subtextColor = Color.FromArgb(194, 65, 12)

                Case Else
                    surfaceBg = Color.White
                    borderColor = Color.FromArgb(226, 232, 240)
                    iconBgColor = Color.FromArgb(241, 245, 249)
                    iconColor = Color.FromArgb(100, 116, 139)
                    titleColor = Color.FromArgb(71, 85, 105)
                    valueColor = Color.FromArgb(15, 23, 42)
                    subtextColor = Color.FromArgb(100, 116, 139)
            End Select

            ' Active Selected / Hover State
            Dim activeBorderPenColor As Color = If(_isSelected, ThemeConstants.PrimaryAccent, If(_isHovered, Color.FromArgb(148, 163, 184), borderColor))
            Dim borderWidth As Single = If(_isSelected, 2.0!, 1.0!)

            ' 1. Draw Card Surface Container Path (10px Corner Radius)
            Dim rectF = New RectangleF(0.5!, 0.5!, Me.Width - 1.0!, Me.Height - 1.0!)
            Using cardPath = CreateRoundedRectanglePath(rectF, 10.0!)
                Using surfaceBrush As New SolidBrush(surfaceBg)
                    g.FillPath(surfaceBrush, cardPath)
                End Using

                Using borderPen As New Pen(activeBorderPenColor, borderWidth)
                    g.DrawPath(borderPen, cardPath)
                End Using
            End Using

            ' 2. Draw Left Vector Icon Circle Badge Container (30x30px)
            Dim iconBoxRect = New RectangleF(8.0!, 12.0!, 30.0!, 30.0!)
            Using iconBoxPath = CreateRoundedRectanglePath(iconBoxRect, 15.0!) ' Perfect circle
                Using iconBgBrush As New SolidBrush(iconBgColor)
                    g.FillPath(iconBgBrush, iconBoxPath)
                End Using
            End Using

            If _iconChar <> IconChar.None Then
                Using iconBmp As Bitmap = _iconChar.ToBitmap(iconColor, 16)
                    If iconBmp IsNot Nothing Then
                        g.DrawImage(iconBmp, New Point(15, 19))
                    End If
                End Using
            End If

            ' 3. Calculate Dedicated Layout Regions for Title, Value, Subtitle
            Dim textX As Integer = 42
            Dim textWidth As Integer = Math.Max(70, Me.Width - textX - 4)

            ' Draw Uppercase Metric Title (8.5pt Bold - Prominent & Readable)
            Dim titleRect As New Rectangle(textX, 10, textWidth, 16)
            Using titleFont As New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold)
                TextRenderer.DrawText(g, _titleText, titleFont, titleRect, titleColor, TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.NoPadding Or TextFormatFlags.SingleLine)
            End Using

            ' 4. Draw Prominent Count Number
            Dim valRect As New Rectangle(textX, 26, textWidth, 32)
            Using valFont As New Font(ThemeConstants.FontNameDefault, 20.0!, FontStyle.Bold)
                TextRenderer.DrawText(g, _valueText, valFont, valRect, valueColor, TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.NoPadding Or TextFormatFlags.SingleLine)
            End Using

            ' 5. Draw Subtitle ("Staff Members")
            Dim subRect As New Rectangle(textX, 58, textWidth, 16)
            Using subFont As New Font(ThemeConstants.FontNameDefault, 8.0!, FontStyle.Bold)
                TextRenderer.DrawText(g, "Staff Members", subFont, subRect, subtextColor, TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.NoPadding Or TextFormatFlags.SingleLine)
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

        Private Shared Function CreateLeftRoundedPath(rect As RectangleF, radius As Single) As GraphicsPath
            Dim path As New GraphicsPath()
            Dim diameter As Single = radius * 2.0!
            Dim arc As New RectangleF(rect.X, rect.Y, diameter, diameter)

            path.AddArc(arc, 180, 90)
            path.AddLine(rect.X + radius, rect.Y, rect.Right, rect.Y)
            path.AddLine(rect.Right, rect.Y, rect.Right, rect.Bottom)
            path.AddLine(rect.Right, rect.Bottom, rect.X + radius, rect.Bottom)

            arc.Y = rect.Bottom - diameter
            path.AddArc(arc, 90, 90)

            path.CloseFigure()
            Return path
        End Function
    End Class
End Namespace
