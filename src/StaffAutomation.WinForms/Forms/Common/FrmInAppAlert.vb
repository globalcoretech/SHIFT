Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports StaffAutomation.WinForms.Forms.Main

Namespace Forms.Common
    Public Enum AlertType
        ErrorAlert
        WarningAlert
        InfoAlert
        SuccessAlert
    End Enum

    ''' <summary>
    ''' Modern Custom In-App Modal Dialog replacing native Windows MessageBox prompts.
    ''' Uses high-DPI GDI+ anti-aliased vector graphics rendering for professional icons.
    ''' Matches the dark slate and vibrant accent enterprise design system of Staff Automation.
    ''' </summary>
    Public Class FrmInAppAlert
        Inherits Form

        Private pnlHeader As Panel
        Private pnlAccentStripe As Panel
        Private lblTitle As Label
        Private pnlBody As Panel
        Private picIcon As PictureBox
        Private lblMessage As Label
        Private pnlFooter As Panel
        Private btnAction As Button
        Private btnSecondaryAction As Button
        Private btnTertiaryAction As Button
        Private _currentAlertType As AlertType = AlertType.ErrorAlert

        Public Sub New(title As String, message As String, type As AlertType, Optional actionText As String = "OK", Optional showCancel As Boolean = False, Optional cancelText As String = "Cancel", Optional showTertiary As Boolean = False, Optional tertiaryText As String = "")
            InitializeComponent()
            ConfigureAlert(title, message, type, actionText, showCancel, cancelText, showTertiary, tertiaryText)
        End Sub

        Private Sub InitializeComponent()
            Me.pnlHeader = New Panel()
            Me.pnlAccentStripe = New Panel()
            Me.lblTitle = New Label()
            Me.pnlBody = New Panel()
            Me.picIcon = New PictureBox()
            Me.lblMessage = New Label()
            Me.pnlFooter = New Panel()
            Me.btnAction = New Button()
            Me.btnSecondaryAction = New Button()
            Me.btnTertiaryAction = New Button()

            Me.pnlHeader.SuspendLayout()
            Me.pnlBody.SuspendLayout()
            Me.pnlFooter.SuspendLayout()
            CType(Me.picIcon, System.ComponentModel.ISupportInitialize).BeginInit()
            Me.SuspendLayout()

            ' Form Properties
            Me.FormBorderStyle = FormBorderStyle.None
            Me.StartPosition = FormStartPosition.CenterParent
            Me.ShowInTaskbar = False
            Me.Size = New Size(520, 260)
            Me.BackColor = Color.FromArgb(203, 213, 225)
            Me.Padding = New Padding(1)
            Me.DoubleBuffered = True

            ' Header Panel (Dark Slate Slate #0F172A)
            Me.pnlHeader.Dock = DockStyle.Top
            Me.pnlHeader.Height = 48
            Me.pnlHeader.BackColor = ThemeConstants.HeaderBackground
            Me.pnlHeader.Controls.Add(Me.lblTitle)
            Me.pnlHeader.Controls.Add(Me.pnlAccentStripe)

            ' Accent Stripe (Top edge color accent)
            Me.pnlAccentStripe.Dock = DockStyle.Top
            Me.pnlAccentStripe.Height = 4

            ' Header Title Label
            Me.lblTitle.Dock = DockStyle.Fill
            Me.lblTitle.Font = New Font(ThemeConstants.FontNameDefault, 10.5!, FontStyle.Bold)
            Me.lblTitle.ForeColor = ThemeConstants.HeaderForeground
            Me.lblTitle.Padding = New Padding(16, 0, 16, 0)
            Me.lblTitle.TextAlign = ContentAlignment.MiddleLeft

            ' Footer Panel (Action Buttons Container)
            Me.pnlFooter.Dock = DockStyle.Bottom
            Me.pnlFooter.Height = 58
            Me.pnlFooter.BackColor = ThemeConstants.WorkspaceBackground
            Me.pnlFooter.Padding = New Padding(16, 10, 16, 10)
            Me.pnlFooter.Controls.Add(Me.btnAction)
            Me.pnlFooter.Controls.Add(Me.btnSecondaryAction)
            Me.pnlFooter.Controls.Add(Me.btnTertiaryAction)

            ' Main Action Button
            Me.btnAction.Dock = DockStyle.Right
            Me.btnAction.Size = New Size(240, 38)
            Me.btnAction.FlatStyle = FlatStyle.Flat
            Me.btnAction.Font = New Font(ThemeConstants.FontNameDefault, 9.25!, FontStyle.Bold)
            Me.btnAction.ForeColor = Color.White
            Me.btnAction.Cursor = Cursors.Hand
            Me.btnAction.FlatAppearance.BorderSize = 0

            ' Secondary Action Button (Optional)
            Me.btnSecondaryAction.Dock = DockStyle.Right
            Me.btnSecondaryAction.Size = New Size(110, 38)
            Me.btnSecondaryAction.FlatStyle = FlatStyle.Flat
            Me.btnSecondaryAction.Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular)
            Me.btnSecondaryAction.ForeColor = ThemeConstants.TextSecondary
            Me.btnSecondaryAction.BackColor = Color.FromArgb(226, 232, 240)
            Me.btnSecondaryAction.Cursor = Cursors.Hand
            Me.btnSecondaryAction.FlatAppearance.BorderSize = 0
            Me.btnSecondaryAction.Visible = False

            ' Tertiary Action Button (Optional)
            Me.btnTertiaryAction.Dock = DockStyle.Right
            Me.btnTertiaryAction.Size = New Size(110, 38)
            Me.btnTertiaryAction.FlatStyle = FlatStyle.Flat
            Me.btnTertiaryAction.Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold)
            Me.btnTertiaryAction.ForeColor = Color.White
            Me.btnTertiaryAction.BackColor = ThemeConstants.WarningOrange
            Me.btnTertiaryAction.Cursor = Cursors.Hand
            Me.btnTertiaryAction.FlatAppearance.BorderSize = 0
            Me.btnTertiaryAction.Visible = False

            ' Body Panel
            Me.pnlBody.Dock = DockStyle.Fill
            Me.pnlBody.Padding = New Padding(20, 16, 20, 16)
            Me.pnlBody.Controls.Add(Me.lblMessage)
            Me.pnlBody.Controls.Add(Me.picIcon)

            ' Vector Icon PictureBox Container
            Me.picIcon.Location = New Point(20, 20)
            Me.picIcon.Size = New Size(48, 48)
            Me.picIcon.BackColor = Color.Transparent

            ' Alert Message Text
            Me.lblMessage.Location = New Point(82, 18)
            Me.lblMessage.Size = New Size(406, 118)
            Me.lblMessage.Font = New Font(ThemeConstants.FontNameDefault, 9.5!, FontStyle.Regular)
            Me.lblMessage.ForeColor = ThemeConstants.TextPrimary
            Me.lblMessage.TextAlign = ContentAlignment.TopLeft

            ' Add Controls
            Me.Controls.Add(Me.pnlBody)
            Me.Controls.Add(Me.pnlFooter)
            Me.Controls.Add(Me.pnlHeader)

            Me.pnlHeader.ResumeLayout(False)
            Me.pnlBody.ResumeLayout(False)
            Me.pnlFooter.ResumeLayout(False)
            CType(Me.picIcon, System.ComponentModel.ISupportInitialize).EndInit()
            Me.ResumeLayout(False)

            ' Wire Handlers
            AddHandler Me.btnAction.Click, AddressOf BtnAction_Click
            AddHandler Me.btnSecondaryAction.Click, AddressOf BtnSecondaryAction_Click
            AddHandler Me.btnTertiaryAction.Click, AddressOf BtnTertiaryAction_Click
            AddHandler Me.picIcon.Paint, AddressOf PicIcon_Paint
        End Sub

        Private Sub ConfigureAlert(title As String, message As String, type As AlertType, actionText As String, showCancel As Boolean, cancelText As String, showTertiary As Boolean, tertiaryText As String)
            Me._currentAlertType = type
            Me.lblTitle.Text = title
            Me.lblMessage.Text = message
            Me.btnAction.Text = actionText

            Select Case type
                Case AlertType.ErrorAlert
                    Me.pnlAccentStripe.BackColor = ThemeConstants.DangerRed
                    Me.btnAction.BackColor = ThemeConstants.DangerRed

                Case AlertType.WarningAlert
                    Me.pnlAccentStripe.BackColor = ThemeConstants.WarningOrange
                    Me.btnAction.BackColor = ThemeConstants.WarningOrange

                Case AlertType.SuccessAlert
                    Me.pnlAccentStripe.BackColor = ThemeConstants.SuccessGreen
                    Me.btnAction.BackColor = ThemeConstants.SuccessGreen

                Case AlertType.InfoAlert
                    Me.pnlAccentStripe.BackColor = ThemeConstants.PrimaryAccent
                    Me.btnAction.BackColor = ThemeConstants.PrimaryAccent
            End Select

            If showCancel Then
                Me.btnSecondaryAction.Text = cancelText
                Me.btnSecondaryAction.Visible = True
                Me.btnAction.Margin = New Padding(8, 0, 0, 0)
            End If

            If showTertiary Then
                Me.btnTertiaryAction.Text = tertiaryText
                Me.btnTertiaryAction.Visible = True
                Me.btnSecondaryAction.Margin = New Padding(8, 0, 0, 0)
            End If

            Me.picIcon.Invalidate()
        End Sub

        ''' <summary>
        ''' Renders anti-aliased vector graphics icons matching enterprise design standard.
        ''' </summary>
        Private Sub PicIcon_Paint(sender As Object, e As PaintEventArgs)
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality

            Select Case _currentAlertType
                Case AlertType.ErrorAlert
                    ' Vibrant Red Circle Badge
                    Using b As New SolidBrush(ThemeConstants.DangerRed)
                        e.Graphics.FillEllipse(b, 2, 2, 44, 44)
                    End Using
                    ' Sharp White Cross (X)
                    Using p As New Pen(Color.White, 3.5F)
                        p.StartCap = LineCap.Round
                        p.EndCap = LineCap.Round
                        e.Graphics.DrawLine(p, 16, 16, 32, 32)
                        e.Graphics.DrawLine(p, 32, 16, 16, 32)
                    End Using

                Case AlertType.WarningAlert
                    ' Amber Orange Circle Badge
                    Using b As New SolidBrush(ThemeConstants.WarningOrange)
                        e.Graphics.FillEllipse(b, 2, 2, 44, 44)
                    End Using
                    ' Sharp White Exclamation Mark (!)
                    Using p As New Pen(Color.White, 3.5F)
                        p.StartCap = LineCap.Round
                        p.EndCap = LineCap.Round
                        e.Graphics.DrawLine(p, 24, 14, 24, 26)
                    End Using
                    Using b As New SolidBrush(Color.White)
                        e.Graphics.FillEllipse(b, 22.2F, 30.5F, 3.6F, 3.6F)
                    End Using

                Case AlertType.SuccessAlert
                    ' Emerald Green Circle Badge
                    Using b As New SolidBrush(ThemeConstants.SuccessGreen)
                        e.Graphics.FillEllipse(b, 2, 2, 44, 44)
                    End Using
                    ' Sharp White Checkmark (✓)
                    Using p As New Pen(Color.White, 3.5F)
                        p.StartCap = LineCap.Round
                        p.EndCap = LineCap.Round
                        e.Graphics.DrawLine(p, 14, 24, 21, 31)
                        e.Graphics.DrawLine(p, 21, 31, 34, 17)
                    End Using

                Case AlertType.InfoAlert
                    ' Royal Blue Circle Badge
                    Using b As New SolidBrush(ThemeConstants.PrimaryAccent)
                        e.Graphics.FillEllipse(b, 2, 2, 44, 44)
                    End Using
                    ' Sharp White Info Mark (i)
                    Using b As New SolidBrush(Color.White)
                        e.Graphics.FillEllipse(b, 22.2F, 12.5F, 3.6F, 3.6F)
                    End Using
                    Using p As New Pen(Color.White, 3.5F)
                        p.StartCap = LineCap.Round
                        p.EndCap = LineCap.Round
                        e.Graphics.DrawLine(p, 24, 19, 24, 32)
                    End Using
            End Select
        End Sub


        Private Sub BtnAction_Click(sender As Object, e As EventArgs)
            Me.DialogResult = DialogResult.OK
            Me.Close()
        End Sub

        Private Sub BtnSecondaryAction_Click(sender As Object, e As EventArgs)
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
        End Sub

        Private Sub BtnTertiaryAction_Click(sender As Object, e As EventArgs)
            Me.DialogResult = DialogResult.Retry
            Me.Close()
        End Sub

        ''' <summary>
        ''' Displays custom in-app themed alert dialog with vector icons, native DWM drop shadow,
        ''' and modern backdrop dimming overlay over specified parent window.
        ''' </summary>
        Public Shared Function ShowModal(owner As Form, title As String, message As String, Optional type As AlertType = AlertType.ErrorAlert, Optional actionText As String = "OK", Optional showCancel As Boolean = False, Optional cancelText As String = "Cancel", Optional showTertiary As Boolean = False, Optional tertiaryText As String = "") As DialogResult
            If owner IsNot Nothing Then
                owner.Update()
            End If
            Dim overlay As Form = Nothing
            Try
                If owner IsNot Nothing AndAlso owner.Visible AndAlso owner.WindowState <> FormWindowState.Minimized Then
                    overlay = New Form() With {
                        .FormBorderStyle = FormBorderStyle.None,
                        .BackColor = Color.Black,
                        .Opacity = 0.45,
                        .ShowInTaskbar = False,
                        .StartPosition = FormStartPosition.Manual,
                        .Bounds = owner.Bounds
                    }
                    overlay.Show(owner)
                    overlay.Refresh()
                End If

                Using dlg As New FrmInAppAlert(title, message, type, actionText, showCancel, cancelText, showTertiary, tertiaryText)
                    Dim res = dlg.ShowDialog(If(overlay, owner))
                    Return res
                End Using
            Finally
                If overlay IsNot Nothing Then
                    overlay.Close()
                    overlay.Dispose()
                End If
            End Try
        End Function
    End Class
End Namespace
