Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Namespace Forms.Main
    ''' <summary>
    ''' Ultra-Premium App-Native SaaS Dialog Prompt Window.
    ''' Replaces legacy Windows MessageBox.Show with modern rounded dialog card,
    ''' circular badge icon container, darkened modal backdrop overlay, and ModernButton controls.
    ''' </summary>
    Public Class FrmCustomMessageBox
        Inherits Form

        Public Enum NotificationType
            Information
            Success
            Warning
            [Error]
            Question
        End Enum

        Private pnlCardContainer As Panel
        Private pnlTopAccentBar As Panel
        Private pnlBody As Panel
        Private pbBadgeIcon As PictureBox
        Private lblTitle As Label
        Private lblMessage As Label

        Private pnlFooter As Panel
        Private btnPrimary As Forms.Common.ModernButton
        Private btnSecondary As Forms.Common.ModernButton
        Private btnCloseHeader As Button

        Private _notifType As NotificationType
        Private _buttons As MessageBoxButtons

        Public Sub New(message As String, title As String, notifType As NotificationType, buttons As MessageBoxButtons)
            _notifType = notifType
            _buttons = buttons
            InitializeComponent(message, title)
        End Sub

        Private Sub InitializeComponent(message As String, title As String)
            Me.SuspendLayout()

            Me.FormBorderStyle = FormBorderStyle.None
            Me.StartPosition = FormStartPosition.CenterParent
            Me.Size = New Size(480, 230)
            Me.BackColor = Color.White
            Me.ShowInTaskbar = False
            Me.TopMost = True
            Me.DoubleBuffered = True

            ' Card Top Accent Bar & Circle Badge colors
            Dim accentColor As Color
            Dim badgeBgColor As Color
            Dim badgeIconColor As Color
            Dim iconBitmap As Bitmap = Nothing

            Select Case _notifType
                Case NotificationType.Success
                    accentColor = Color.FromArgb(16, 185, 129)   ' Emerald #10B981
                    badgeBgColor = Color.FromArgb(220, 252, 231) ' Soft Green #DCFCE7
                    badgeIconColor = Color.FromArgb(22, 101, 52) ' Dark Emerald #166534
                    iconBitmap = VectorIconHelper.CreateCheckIcon(badgeIconColor, 24)

                Case NotificationType.Warning
                    accentColor = Color.FromArgb(245, 158, 11)   ' Amber #F59E0B
                    badgeBgColor = Color.FromArgb(254, 243, 199) ' Soft Amber #FEF3C7
                    badgeIconColor = Color.FromArgb(146, 64, 14) ' Dark Amber #92400E
                    iconBitmap = VectorIconHelper.CreateAlertIcon(badgeIconColor, 24)

                Case NotificationType.Error
                    accentColor = Color.FromArgb(239, 68, 68)   ' Red #EF4444
                    badgeBgColor = Color.FromArgb(254, 226, 226) ' Soft Red #FEE2E2
                    badgeIconColor = Color.FromArgb(153, 27, 27) ' Dark Red #991B1B
                    iconBitmap = VectorIconHelper.CreateCrossIcon(badgeIconColor, 24)

                Case NotificationType.Question
                    accentColor = Color.FromArgb(99, 102, 241)  ' Indigo #6366F1
                    badgeBgColor = Color.FromArgb(224, 231, 255) ' Soft Indigo #E0E7FF
                    badgeIconColor = Color.FromArgb(55, 48, 163) ' Dark Indigo #3730A3
                    iconBitmap = VectorIconHelper.CreateQuestionIcon(badgeIconColor, 24)

                Case Else
                    accentColor = Color.FromArgb(79, 70, 229)   ' Indigo Accent #4F46E5
                    badgeBgColor = Color.FromArgb(238, 242, 255) ' Soft Blue #EEEF8
                    badgeIconColor = Color.FromArgb(67, 56, 202)
                    iconBitmap = VectorIconHelper.CreateSearchIcon(badgeIconColor, 24)
            End Select

            ' 1. Top Colored Accent Indicator Stripe (4px)
            pnlTopAccentBar = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 4,
                .BackColor = accentColor
            }

            ' 2. Footer Action Bar (#F8FAFC)
            pnlFooter = New Panel() With {
                .Dock = DockStyle.Bottom,
                .Height = 56,
                .BackColor = Color.FromArgb(248, 250, 252)
            }
            AddHandler pnlFooter.Paint, Sub(s, e)
                                            Using p As New Pen(Color.FromArgb(226, 232, 240), 1)
                                                e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0)
                                            End Using
                                        End Sub

            ' Close (✕) Button Top Right
            btnCloseHeader = New Button() With {
                .Text = "✕",
                .Font = New Font("Segoe UI", 9.0!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(148, 163, 184),
                .BackColor = Color.Transparent,
                .FlatStyle = FlatStyle.Flat,
                .Size = New Size(28, 28),
                .Location = New Point(444, 10),
                .Cursor = Cursors.Hand
            }
            btnCloseHeader.FlatAppearance.BorderSize = 0
            btnCloseHeader.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249)
            AddHandler btnCloseHeader.Click, Sub(s, e)
                                                  Me.DialogResult = DialogResult.Cancel
                                                  Me.Close()
                                              End Sub

            ' 3. Main Body Container
            pnlBody = New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = Color.White,
                .Padding = New Padding(20, 16, 20, 16)
            }

            ' Circular Icon Badge (48x48 Circle)
            pbBadgeIcon = New PictureBox() With {
                .Size = New Size(46, 46),
                .Location = New Point(20, 16),
                .SizeMode = PictureBoxSizeMode.CenterImage,
                .Image = iconBitmap,
                .BackColor = Color.Transparent
            }
            AddHandler pbBadgeIcon.Paint, Sub(s, e)
                                              e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
                                              Using b As New SolidBrush(badgeBgColor)
                                                  e.Graphics.FillEllipse(b, 0, 0, pbBadgeIcon.Width - 1, pbBadgeIcon.Height - 1)
                                              End Using
                                              If pbBadgeIcon.Image IsNot Nothing Then
                                                  Dim ix = (pbBadgeIcon.Width - pbBadgeIcon.Image.Width) \ 2
                                                  Dim iy = (pbBadgeIcon.Height - pbBadgeIcon.Image.Height) \ 2
                                                  e.Graphics.DrawImage(pbBadgeIcon.Image, ix, iy)
                                              End If
                                          End Sub

            ' Title Label
            lblTitle = New Label() With {
                .Text = If(String.IsNullOrWhiteSpace(title), "System Notification", title),
                .Font = New Font("Segoe UI", 11.0!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(15, 23, 42), ' #0F172A
                .Location = New Point(78, 16),
                .Size = New Size(355, 26),
                .AutoEllipsis = True
            }

            ' Message Body Label
            lblMessage = New Label() With {
                .Text = message,
                .Font = New Font("Segoe UI", 9.5!, FontStyle.Regular),
                .ForeColor = Color.FromArgb(51, 65, 85), ' #334155
                .Location = New Point(78, 44),
                .Size = New Size(370, 115),
                .AutoEllipsis = True
            }

            pnlBody.Controls.Add(btnCloseHeader)
            pnlBody.Controls.Add(pbBadgeIcon)
            pnlBody.Controls.Add(lblTitle)
            pnlBody.Controls.Add(lblMessage)

            ' Configure Action Buttons with ModernButton Controls
            If _buttons = MessageBoxButtons.YesNo OrElse _buttons = MessageBoxButtons.YesNoCancel Then
                btnPrimary = New Forms.Common.ModernButton() With {
                    .Text = "Yes, Proceed",
                    .Scheme = Forms.Common.ModernButton.ButtonScheme.Primary,
                    .Size = New Size(110, 36),
                    .Location = New Point(354, 10)
                }
                AddHandler btnPrimary.Click, Sub(s, e)
                                                 Me.DialogResult = DialogResult.Yes
                                                 Me.Close()
                                             End Sub

                btnSecondary = New Forms.Common.ModernButton() With {
                    .Text = "Cancel",
                    .Scheme = Forms.Common.ModernButton.ButtonScheme.Secondary,
                    .Size = New Size(90, 36),
                    .Location = New Point(254, 10)
                }
                AddHandler btnSecondary.Click, Sub(s, e)
                                                   Me.DialogResult = DialogResult.No
                                                   Me.Close()
                                               End Sub

                pnlFooter.Controls.Add(btnPrimary)
                pnlFooter.Controls.Add(btnSecondary)
            Else
                Dim primaryScheme = Forms.Common.ModernButton.ButtonScheme.Primary
                If _notifType = NotificationType.Success Then primaryScheme = Forms.Common.ModernButton.ButtonScheme.Success
                If _notifType = NotificationType.Error Then primaryScheme = Forms.Common.ModernButton.ButtonScheme.Danger

                btnPrimary = New Forms.Common.ModernButton() With {
                    .Text = "OK, Got It",
                    .Scheme = primaryScheme,
                    .Size = New Size(105, 36),
                    .Location = New Point(359, 10)
                }
                AddHandler btnPrimary.Click, Sub(s, e)
                                                 Me.DialogResult = DialogResult.OK
                                                 Me.Close()
                                             End Sub

                pnlFooter.Controls.Add(btnPrimary)
            End If

            Me.Controls.Add(pnlBody)
            Me.Controls.Add(pnlFooter)
            Me.Controls.Add(pnlTopAccentBar)

            AddHandler Me.Paint, AddressOf FrmCustomMessageBox_Paint
            Me.ResumeLayout(False)
        End Sub

        Private Sub FrmCustomMessageBox_Paint(sender As Object, e As PaintEventArgs)
            ' Draw Crisp Modern 1px Outer Border (#CBD5E1)
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
            Using p As New Pen(Color.FromArgb(203, 213, 225), 1)
                e.Graphics.DrawRectangle(p, 0, 0, Me.Width - 1, Me.Height - 1)
            End Using
        End Sub
    End Class

    ''' <summary>
    ''' Semi-Transparent Dark Modal Backdrop Overlay for true SaaS Depth.
    ''' Dimms the background window when dialog prompts appear.
    ''' </summary>
    Public Class FrmModalBackdrop
        Inherits Form

        Public Sub New(parentForm As Form)
            Me.FormBorderStyle = FormBorderStyle.None
            Me.ShowInTaskbar = False
            Me.StartPosition = FormStartPosition.Manual
            Me.BackColor = Color.FromArgb(15, 23, 42) ' Slate Black #0F172A
            Me.Opacity = 0.45
            Me.DoubleBuffered = True

            If parentForm IsNot Nothing Then
                Me.Location = parentForm.PointToScreen(Point.Empty)
                Me.Size = parentForm.Size
            Else
                Me.Location = Screen.PrimaryScreen.Bounds.Location
                Me.Size = Screen.PrimaryScreen.Bounds.Size
            End If
        End Sub
    End Class

    ''' <summary>
    ''' Global Static Helper for invoking App-Native SaaS Dialog Prompt Notifications across the App.
    ''' </summary>
    Public Class AppNotificationHelper
        Public Shared Function ShowSuccess(message As String, Optional title As String = "Success", Optional owner As IWin32Window = Nothing) As DialogResult
            Return ShowDialogWithBackdrop(message, title, FrmCustomMessageBox.NotificationType.Success, MessageBoxButtons.OK, owner)
        End Function

        Public Shared Function ShowInfo(message As String, Optional title As String = "Information", Optional owner As IWin32Window = Nothing) As DialogResult
            Return ShowDialogWithBackdrop(message, title, FrmCustomMessageBox.NotificationType.Information, MessageBoxButtons.OK, owner)
        End Function

        Public Shared Function ShowWarning(message As String, Optional title As String = "Warning", Optional owner As IWin32Window = Nothing) As DialogResult
            Return ShowDialogWithBackdrop(message, title, FrmCustomMessageBox.NotificationType.Warning, MessageBoxButtons.OK, owner)
        End Function

        Public Shared Function ShowError(message As String, Optional title As String = "Error", Optional owner As IWin32Window = Nothing) As DialogResult
            Return ShowDialogWithBackdrop(message, title, FrmCustomMessageBox.NotificationType.Error, MessageBoxButtons.OK, owner)
        End Function

        Public Shared Function ShowQuestion(message As String, Optional title As String = "Confirm Action", Optional owner As IWin32Window = Nothing) As DialogResult
            Return ShowDialogWithBackdrop(message, title, FrmCustomMessageBox.NotificationType.Question, MessageBoxButtons.YesNo, owner)
        End Function

        Private Shared Function ShowDialogWithBackdrop(message As String, title As String, notifType As FrmCustomMessageBox.NotificationType, buttons As MessageBoxButtons, owner As IWin32Window) As DialogResult
            Dim parentForm As Form = TryCast(owner, Form)
            If parentForm Is Nothing Then
                parentForm = Form.ActiveForm
            End If

            If parentForm IsNot Nothing AndAlso parentForm.Visible Then
                Using backdrop As New FrmModalBackdrop(parentForm)
                    backdrop.Show(parentForm)
                    Using dlg As New FrmCustomMessageBox(message, title, notifType, buttons)
                        Dim result = dlg.ShowDialog(backdrop)
                        backdrop.Close()
                        Return result
                    End Using
                End Using
            Else
                Using dlg As New FrmCustomMessageBox(message, title, notifType, buttons)
                    Return dlg.ShowDialog()
                End Using
            End If
        End Function
    End Class
End Namespace
