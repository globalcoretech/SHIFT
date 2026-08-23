Option Strict On
Option Explicit On

Imports System
Imports System.ComponentModel
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports StaffAutomation.WinForms.Forms.Main

Namespace Forms.Common
    ''' <summary>
    ''' Reusable Action Card Component for Settings, Help, and Administration consoles.
    ''' Encapsulates top accent border, title, description, status badge pill, action button,
    ''' and subtle GDI+ hover depth animations.
    ''' </summary>
    Public Class AppActionCard
        Inherits UserControl

        Private pnlTopAccent As Panel
        Private lblTitle As Label
        Private lblDescription As Label
        Private lblBadge As Label
        Private btnAction As ModernButton

        Private _accentColor As Color = ThemeConstants.PrimaryAccent
        Private _isHovered As Boolean = False

        Public Event ActionClicked As EventHandler

        Public Sub New()
            Me.DoubleBuffered = True
            Me.Size = New Size(500, 155)
            Me.BackColor = Color.White
            Me.Margin = New Padding(0, 0, 16, 16)
            Me.Padding = New Padding(16)

            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.SuspendLayout()

            pnlTopAccent = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 4,
                .BackColor = _accentColor
            }

            lblTitle = New Label() With {
                .Text = "Card Title",
                .Font = New Font(ThemeConstants.FontNameDefault, 11.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(16, 14),
                .AutoSize = True
            }

            lblDescription = New Label() With {
                .Text = "Card description text goes here.",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular),
                .ForeColor = ThemeConstants.TextSecondary,
                .Location = New Point(16, 42),
                .Size = New Size(Me.Width - 32, 52),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
            }

            lblBadge = New Label() With {
                .Text = "Active",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = _accentColor,
                .Location = New Point(16, 115),
                .AutoSize = True
            }

            btnAction = New ModernButton() With {
                .Text = "Configure →",
                .Scheme = ModernButton.ButtonScheme.Secondary,
                .Size = New Size(115, 28),
                .Location = New Point(Me.Width - 131, 110),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            AddHandler btnAction.Click, Sub(s, e) RaiseEvent ActionClicked(Me, e)

            Me.Controls.Add(btnAction)
            Me.Controls.Add(lblBadge)
            Me.Controls.Add(lblDescription)
            Me.Controls.Add(lblTitle)
            Me.Controls.Add(pnlTopAccent)

            Me.ResumeLayout(False)
        End Sub

        Public Property Title As String
            Get
                Return lblTitle.Text
            End Get
            Set(value As String)
                lblTitle.Text = value
            End Set
        End Property

        Public Property Description As String
            Get
                Return lblDescription.Text
            End Get
            Set(value As String)
                lblDescription.Text = value
            End Set
        End Property

        Public Property BadgeText As String
            Get
                Return lblBadge.Text
            End Get
            Set(value As String)
                lblBadge.Text = value
            End Set
        End Property

        Public Property ActionText As String
            Get
                Return btnAction.Text
            End Get
            Set(value As String)
                btnAction.Text = value
            End Set
        End Property

        Public Property AccentColor As Color
            Get
                Return _accentColor
            End Get
            Set(value As Color)
                _accentColor = value
                pnlTopAccent.BackColor = value
                lblBadge.ForeColor = value
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
            If lblDescription IsNot Nothing AndAlso btnAction IsNot Nothing Then
                lblDescription.Width = Me.Width - 32
                btnAction.Location = New Point(Me.Width - btnAction.Width - 16, 110)
            End If
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)
            Dim g = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias

            Dim borderColor As Color = If(_isHovered, _accentColor, Color.FromArgb(226, 232, 240))
            Dim borderWidth As Integer = If(_isHovered, 2, 1)

            Using p As New Pen(borderColor, borderWidth)
                g.DrawRectangle(p, 0, 0, Me.Width - 1, Me.Height - 1)
            End Using
        End Sub
    End Class
End Namespace
