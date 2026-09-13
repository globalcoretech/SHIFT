Imports System.ComponentModel
Imports System.Drawing
Imports System.Windows.Forms

Namespace Forms.Auth
    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
    Partial Class FrmLogin
        Inherits System.Windows.Forms.Form

        Private components As IContainer

        <System.Diagnostics.DebuggerNonUserCode()>
        Protected Overrides Sub Dispose(disposing As Boolean)
            Try
                If disposing AndAlso components IsNot Nothing Then
                    components.Dispose()
                End If
            Finally
                MyBase.Dispose(disposing)
            End Try
        End Sub

        Private WithEvents pnlLeftHero As Panel
        Private WithEvents lblHeroLogo As Label
        Private WithEvents lblHeroTitle As Label
        Private WithEvents lblHeroSub As Label
        Private WithEvents pnlHeroDivider As Panel
        Private WithEvents lblFeature1 As Label
        Private WithEvents lblFeature2 As Label
        Private WithEvents lblFeature3 As Label
        Private WithEvents lblFeature4 As Label
        Private WithEvents lblVersion As Label

        Private WithEvents pnlRightForm As Panel
        Private WithEvents lblHeaderTitle As Label
        Private WithEvents lblHeaderSub As Label
        Private WithEvents lblUsernameLabel As Label
        Private WithEvents pnlUsernameInput As Panel
        Private WithEvents lblUsernameIcon As Label
        Private WithEvents txtUsername As TextBox
        Private WithEvents lblPasswordLabel As Label
        Private WithEvents pnlPasswordInput As Panel
        Private WithEvents lblPasswordIcon As Label
        Private WithEvents txtPassword As TextBox
        Private WithEvents chkShowPassword As CheckBox
        Private WithEvents btnLogin As Button
        Private WithEvents btnExit As Button
        Private WithEvents lblSecurityFooter As Label
        Private WithEvents errProvider As ErrorProvider

        <System.Diagnostics.DebuggerStepThrough()>
        Private Sub InitializeComponent()
            Me.components = New Container()
            Me.pnlLeftHero = New Panel()
            Me.lblVersion = New Label()
            Me.lblFeature4 = New Label()
            Me.lblFeature3 = New Label()
            Me.lblFeature2 = New Label()
            Me.lblFeature1 = New Label()
            Me.pnlHeroDivider = New Panel()
            Me.lblHeroSub = New Label()
            Me.lblHeroTitle = New Label()
            Me.lblHeroLogo = New Label()

            Me.pnlRightForm = New Panel()
            Me.lblSecurityFooter = New Label()
            Me.btnExit = New Button()
            Me.btnLogin = New Button()
            Me.chkShowPassword = New CheckBox()
            Me.pnlPasswordInput = New Panel()
            Me.txtPassword = New TextBox()
            Me.lblPasswordIcon = New Label()
            Me.lblPasswordLabel = New Label()
            Me.pnlUsernameInput = New Panel()
            Me.txtUsername = New TextBox()
            Me.lblUsernameIcon = New Label()
            Me.lblUsernameLabel = New Label()
            Me.lblHeaderSub = New Label()
            Me.lblHeaderTitle = New Label()
            Me.errProvider = New ErrorProvider(Me.components)

            Me.pnlLeftHero.SuspendLayout()
            Me.pnlRightForm.SuspendLayout()
            Me.pnlUsernameInput.SuspendLayout()
            Me.pnlPasswordInput.SuspendLayout()
            CType(Me.errProvider, ISupportInitialize).BeginInit()
            Me.SuspendLayout()

            '
            ' pnlLeftHero
            '
            Me.pnlLeftHero.BackColor = Color.FromArgb(CType(CType(15, Byte), Integer), CType(CType(23, Byte), Integer), CType(CType(42, Byte), Integer))
            Me.pnlLeftHero.Controls.Add(Me.lblVersion)
            Me.pnlLeftHero.Controls.Add(Me.lblFeature4)
            Me.pnlLeftHero.Controls.Add(Me.lblFeature3)
            Me.pnlLeftHero.Controls.Add(Me.lblFeature2)
            Me.pnlLeftHero.Controls.Add(Me.lblFeature1)
            Me.pnlLeftHero.Controls.Add(Me.pnlHeroDivider)
            Me.pnlLeftHero.Controls.Add(Me.lblHeroSub)
            Me.pnlLeftHero.Controls.Add(Me.lblHeroTitle)
            Me.pnlLeftHero.Controls.Add(Me.lblHeroLogo)
            Me.pnlLeftHero.Dock = DockStyle.Left
            Me.pnlLeftHero.Location = New Point(0, 0)
            Me.pnlLeftHero.Name = "pnlLeftHero"
            Me.pnlLeftHero.Size = New Size(240, 410)
            Me.pnlLeftHero.TabIndex = 0
            '
            ' lblHeroLogo
            '
            Me.lblHeroLogo.AutoSize = True
            Me.lblHeroLogo.Font = New Font("Segoe UI", 26.0!, FontStyle.Bold)
            Me.lblHeroLogo.ForeColor = Color.FromArgb(CType(CType(99, Byte), Integer), CType(CType(102, Byte), Integer), CType(CType(241, Byte), Integer))
            Me.lblHeroLogo.Location = New Point(22, 25)
            Me.lblHeroLogo.Name = "lblHeroLogo"
            Me.lblHeroLogo.Size = New Size(62, 47)
            Me.lblHeroLogo.TabIndex = 0
            Me.lblHeroLogo.Text = "🏛️"
            '
            ' lblHeroTitle
            '
            Me.lblHeroTitle.AutoSize = True
            Me.lblHeroTitle.Font = New Font("Segoe UI Semibold", 15.0!, FontStyle.Bold)
            Me.lblHeroTitle.ForeColor = Color.White
            Me.lblHeroTitle.Location = New Point(25, 75)
            Me.lblHeroTitle.Name = "lblHeroTitle"
            Me.lblHeroTitle.Size = New Size(148, 28)
            Me.lblHeroTitle.TabIndex = 1
            Me.lblHeroTitle.Text = "CA Workforce"
            '
            ' lblHeroSub
            '
            Me.lblHeroSub.Font = New Font("Segoe UI", 8.5!, FontStyle.Regular)
            Me.lblHeroSub.ForeColor = Color.FromArgb(CType(CType(148, Byte), Integer), CType(CType(163, Byte), Integer), CType(CType(184, Byte), Integer))
            Me.lblHeroSub.Location = New Point(27, 105)
            Me.lblHeroSub.Name = "lblHeroSub"
            Me.lblHeroSub.Size = New Size(185, 36)
            Me.lblHeroSub.TabIndex = 2
            Me.lblHeroSub.Text = "Enterprise Practice Management & Time Tracking System"
            '
            ' pnlHeroDivider
            '
            Me.pnlHeroDivider.BackColor = Color.FromArgb(CType(CType(51, Byte), Integer), CType(CType(65, Byte), Integer), CType(CType(85, Byte), Integer))
            Me.pnlHeroDivider.Location = New Point(28, 155)
            Me.pnlHeroDivider.Name = "pnlHeroDivider"
            Me.pnlHeroDivider.Size = New Size(180, 1)
            Me.pnlHeroDivider.TabIndex = 3
            '
            ' lblFeature1
            '
            Me.lblFeature1.AutoSize = True
            Me.lblFeature1.Font = New Font("Segoe UI", 9.0!, FontStyle.Regular)
            Me.lblFeature1.ForeColor = Color.FromArgb(CType(CType(226, Byte), Integer), CType(CType(232, Byte), Integer), CType(CType(240, Byte), Integer))
            Me.lblFeature1.Location = New Point(26, 175)
            Me.lblFeature1.Name = "lblFeature1"
            Me.lblFeature1.Size = New Size(155, 15)
            Me.lblFeature1.TabIndex = 4
            Me.lblFeature1.Text = "✓ Real-Time Attendance Lock"
            '
            ' lblFeature2
            '
            Me.lblFeature2.AutoSize = True
            Me.lblFeature2.Font = New Font("Segoe UI", 9.0!, FontStyle.Regular)
            Me.lblFeature2.ForeColor = Color.FromArgb(CType(CType(226, Byte), Integer), CType(CType(232, Byte), Integer), CType(CType(240, Byte), Integer))
            Me.lblFeature2.Location = New Point(26, 205)
            Me.lblFeature2.Name = "lblFeature2"
            Me.lblFeature2.Size = New Size(157, 15)
            Me.lblFeature2.TabIndex = 5
            Me.lblFeature2.Text = "✓ Task Workflow State Machine"
            '
            ' lblFeature3
            '
            Me.lblFeature3.AutoSize = True
            Me.lblFeature3.Font = New Font("Segoe UI", 9.0!, FontStyle.Regular)
            Me.lblFeature3.ForeColor = Color.FromArgb(CType(CType(226, Byte), Integer), CType(CType(232, Byte), Integer), CType(CType(240, Byte), Integer))
            Me.lblFeature3.Location = New Point(26, 235)
            Me.lblFeature3.Name = "lblFeature3"
            Me.lblFeature3.Size = New Size(158, 15)
            Me.lblFeature3.TabIndex = 6
            Me.lblFeature3.Text = "✓ Executive PDF & Excel Reports"
            '
            ' lblFeature4
            '
            Me.lblFeature4.AutoSize = True
            Me.lblFeature4.Font = New Font("Segoe UI", 9.0!, FontStyle.Regular)
            Me.lblFeature4.ForeColor = Color.FromArgb(CType(CType(226, Byte), Integer), CType(CType(232, Byte), Integer), CType(CType(240, Byte), Integer))
            Me.lblFeature4.Location = New Point(26, 265)
            Me.lblFeature4.Name = "lblFeature4"
            Me.lblFeature4.Size = New Size(152, 15)
            Me.lblFeature4.TabIndex = 7
            Me.lblFeature4.Text = "✓ 256-Bit Encrypted Audit Log"
            '
            ' lblVersion
            '
            Me.lblVersion.AutoSize = True
            Me.lblVersion.Font = New Font("Segoe UI", 8.0!, FontStyle.Regular)
            Me.lblVersion.ForeColor = Color.FromArgb(CType(CType(100, Byte), Integer), CType(CType(116, Byte), Integer), CType(CType(139, Byte), Integer))
            Me.lblVersion.Location = New Point(27, 375)
            Me.lblVersion.Name = "lblVersion"
            Me.lblVersion.Size = New Size(116, 13)
            Me.lblVersion.TabIndex = 8
            ' Version set dynamically in form load
            '
            ' pnlRightForm
            '
            Me.pnlRightForm.BackColor = Color.White
            Me.pnlRightForm.Controls.Add(Me.lblSecurityFooter)
            Me.pnlRightForm.Controls.Add(Me.btnExit)
            Me.pnlRightForm.Controls.Add(Me.btnLogin)
            Me.pnlRightForm.Controls.Add(Me.chkShowPassword)
            Me.pnlRightForm.Controls.Add(Me.pnlPasswordInput)
            Me.pnlRightForm.Controls.Add(Me.lblPasswordLabel)
            Me.pnlRightForm.Controls.Add(Me.pnlUsernameInput)
            Me.pnlRightForm.Controls.Add(Me.lblUsernameLabel)
            Me.pnlRightForm.Controls.Add(Me.lblHeaderSub)
            Me.pnlRightForm.Controls.Add(Me.lblHeaderTitle)
            Me.pnlRightForm.Dock = DockStyle.Fill
            Me.pnlRightForm.Location = New Point(240, 0)
            Me.pnlRightForm.Name = "pnlRightForm"
            Me.pnlRightForm.Size = New Size(400, 410)
            Me.pnlRightForm.TabIndex = 1
            '
            ' lblHeaderTitle
            '
            Me.lblHeaderTitle.AutoSize = True
            Me.lblHeaderTitle.Font = New Font("Segoe UI Semibold", 17.0!, FontStyle.Bold)
            Me.lblHeaderTitle.ForeColor = Color.FromArgb(CType(CType(15, Byte), Integer), CType(CType(23, Byte), Integer), CType(CType(42, Byte), Integer))
            Me.lblHeaderTitle.Location = New Point(32, 28)
            Me.lblHeaderTitle.Name = "lblHeaderTitle"
            Me.lblHeaderTitle.Text = "Sign In to Workspace"
            '
            ' lblHeaderSub
            '
            Me.lblHeaderSub.AutoSize = True
            Me.lblHeaderSub.Font = New Font("Segoe UI", 9.0!, FontStyle.Regular)
            Me.lblHeaderSub.ForeColor = Color.FromArgb(CType(CType(100, Byte), Integer), CType(CType(116, Byte), Integer), CType(CType(139, Byte), Integer))
            Me.lblHeaderSub.Location = New Point(34, 62)
            Me.lblHeaderSub.Name = "lblHeaderSub"
            Me.lblHeaderSub.Size = New Size(244, 15)
            Me.lblHeaderSub.Text = "Enter your account details to access your daily tasks."
            '
            ' lblUsernameLabel
            '
            Me.lblUsernameLabel.AutoSize = True
            Me.lblUsernameLabel.Font = New Font("Segoe UI", 8.0!, FontStyle.Bold)
            Me.lblUsernameLabel.ForeColor = Color.FromArgb(CType(CType(71, Byte), Integer), CType(CType(85, Byte), Integer), CType(CType(105, Byte), Integer))
            Me.lblUsernameLabel.Location = New Point(34, 102)
            Me.lblUsernameLabel.Name = "lblUsernameLabel"
            Me.lblUsernameLabel.Size = New Size(125, 13)
            Me.lblUsernameLabel.Text = "USERNAME OR STAFF ID"
            '
            ' pnlUsernameInput
            '
            Me.pnlUsernameInput.BackColor = Color.FromArgb(CType(CType(248, Byte), Integer), CType(CType(250, Byte), Integer), CType(CType(252, Byte), Integer))
            Me.pnlUsernameInput.BorderStyle = BorderStyle.FixedSingle
            Me.pnlUsernameInput.Controls.Add(Me.txtUsername)
            Me.pnlUsernameInput.Controls.Add(Me.lblUsernameIcon)
            Me.pnlUsernameInput.Location = New Point(34, 120)
            Me.pnlUsernameInput.Name = "pnlUsernameInput"
            Me.pnlUsernameInput.Size = New Size(330, 42)
            Me.pnlUsernameInput.TabIndex = 2
            '
            ' lblUsernameIcon
            '
            Me.lblUsernameIcon.AutoSize = True
            Me.lblUsernameIcon.Font = New Font("Segoe UI", 11.0!)
            Me.lblUsernameIcon.ForeColor = Color.FromArgb(CType(CType(148, Byte), Integer), CType(CType(163, Byte), Integer), CType(CType(184, Byte), Integer))
            Me.lblUsernameIcon.Location = New Point(10, 10)
            Me.lblUsernameIcon.Name = "lblUsernameIcon"
            Me.lblUsernameIcon.Size = New Size(24, 20)
            Me.lblUsernameIcon.Text = "👤"
            '
            ' txtUsername
            '
            Me.txtUsername.BackColor = Color.FromArgb(CType(CType(248, Byte), Integer), CType(CType(250, Byte), Integer), CType(CType(252, Byte), Integer))
            Me.txtUsername.BorderStyle = BorderStyle.None
            Me.txtUsername.Font = New Font("Segoe UI", 10.5!, FontStyle.Regular)
            Me.txtUsername.ForeColor = Color.FromArgb(CType(CType(15, Byte), Integer), CType(CType(23, Byte), Integer), CType(CType(42, Byte), Integer))
            Me.txtUsername.Location = New Point(38, 11)
            Me.txtUsername.Name = "txtUsername"
            Me.txtUsername.Size = New Size(280, 19)
            Me.txtUsername.TabIndex = 0
            '
            ' lblPasswordLabel
            '
            Me.lblPasswordLabel.AutoSize = True
            Me.lblPasswordLabel.Font = New Font("Segoe UI", 8.0!, FontStyle.Bold)
            Me.lblPasswordLabel.ForeColor = Color.FromArgb(CType(CType(71, Byte), Integer), CType(CType(85, Byte), Integer), CType(CType(105, Byte), Integer))
            Me.lblPasswordLabel.Location = New Point(34, 177)
            Me.lblPasswordLabel.Name = "lblPasswordLabel"
            Me.lblPasswordLabel.Size = New Size(67, 13)
            Me.lblPasswordLabel.Text = "PASSWORD"
            '
            ' pnlPasswordInput
            '
            Me.pnlPasswordInput.BackColor = Color.FromArgb(CType(CType(248, Byte), Integer), CType(CType(250, Byte), Integer), CType(CType(252, Byte), Integer))
            Me.pnlPasswordInput.BorderStyle = BorderStyle.FixedSingle
            Me.pnlPasswordInput.Controls.Add(Me.txtPassword)
            Me.pnlPasswordInput.Controls.Add(Me.lblPasswordIcon)
            Me.pnlPasswordInput.Location = New Point(34, 195)
            Me.pnlPasswordInput.Name = "pnlPasswordInput"
            Me.pnlPasswordInput.Size = New Size(330, 42)
            Me.pnlPasswordInput.TabIndex = 4
            '
            ' lblPasswordIcon
            '
            Me.lblPasswordIcon.AutoSize = True
            Me.lblPasswordIcon.Font = New Font("Segoe UI", 11.0!)
            Me.lblPasswordIcon.ForeColor = Color.FromArgb(CType(CType(148, Byte), Integer), CType(CType(163, Byte), Integer), CType(CType(184, Byte), Integer))
            Me.lblPasswordIcon.Location = New Point(10, 10)
            Me.lblPasswordIcon.Name = "lblPasswordIcon"
            Me.lblPasswordIcon.Size = New Size(24, 20)
            Me.lblPasswordIcon.Text = "🔑"
            '
            ' txtPassword
            '
            Me.txtPassword.BackColor = Color.FromArgb(CType(CType(248, Byte), Integer), CType(CType(250, Byte), Integer), CType(CType(252, Byte), Integer))
            Me.txtPassword.BorderStyle = BorderStyle.None
            Me.txtPassword.Font = New Font("Segoe UI", 10.5!, FontStyle.Regular)
            Me.txtPassword.ForeColor = Color.FromArgb(CType(CType(15, Byte), Integer), CType(CType(23, Byte), Integer), CType(CType(42, Byte), Integer))
            Me.txtPassword.Location = New Point(38, 11)
            Me.txtPassword.Name = "txtPassword"
            Me.txtPassword.PasswordChar = Global.Microsoft.VisualBasic.ChrW(8226)
            Me.txtPassword.Size = New Size(280, 19)
            Me.txtPassword.TabIndex = 0
            '
            ' chkShowPassword
            '
            Me.chkShowPassword.AutoSize = True
            Me.chkShowPassword.Cursor = Cursors.Hand
            Me.chkShowPassword.Font = New Font("Segoe UI", 9.0!, FontStyle.Regular)
            Me.chkShowPassword.ForeColor = Color.FromArgb(CType(CType(71, Byte), Integer), CType(CType(85, Byte), Integer), CType(CType(105, Byte), Integer))
            Me.chkShowPassword.Location = New Point(34, 245)
            Me.chkShowPassword.Name = "chkShowPassword"
            Me.chkShowPassword.Size = New Size(108, 19)
            Me.chkShowPassword.TabIndex = 5
            Me.chkShowPassword.Text = "Show Password"
            Me.chkShowPassword.UseVisualStyleBackColor = True
            '
            ' btnExit
            '
            Me.btnExit.BackColor = Color.FromArgb(CType(CType(241, Byte), Integer), CType(CType(245, Byte), Integer), CType(CType(249, Byte), Integer))
            Me.btnExit.Cursor = Cursors.Hand
            Me.btnExit.FlatAppearance.BorderSize = 0
            Me.btnExit.FlatStyle = FlatStyle.Flat
            Me.btnExit.Font = New Font("Segoe UI Semibold", 9.5!, FontStyle.Bold)
            Me.btnExit.ForeColor = Color.FromArgb(CType(CType(71, Byte), Integer), CType(CType(85, Byte), Integer), CType(CType(105, Byte), Integer))
            Me.btnExit.Location = New Point(34, 282)
            Me.btnExit.Name = "btnExit"
            Me.btnExit.Size = New Size(100, 42)
            Me.btnExit.TabIndex = 7
            Me.btnExit.Text = "Exit"
            Me.btnExit.UseVisualStyleBackColor = False
            '
            ' btnLogin
            '
            Me.btnLogin.BackColor = Color.FromArgb(CType(CType(79, Byte), Integer), CType(CType(70, Byte), Integer), CType(CType(229, Byte), Integer))
            Me.btnLogin.Cursor = Cursors.Hand
            Me.btnLogin.FlatAppearance.BorderSize = 0
            Me.btnLogin.FlatStyle = FlatStyle.Flat
            Me.btnLogin.Font = New Font("Segoe UI Semibold", 10.0!, FontStyle.Bold)
            Me.btnLogin.ForeColor = Color.White
            Me.btnLogin.Location = New Point(144, 282)
            Me.btnLogin.Name = "btnLogin"
            Me.btnLogin.Size = New Size(220, 42)
            Me.btnLogin.TabIndex = 6
            Me.btnLogin.Text = "SIGN IN  ➔"
            Me.btnLogin.UseVisualStyleBackColor = False
            '
            ' lblSecurityFooter
            '
            Me.lblSecurityFooter.AutoSize = True
            Me.lblSecurityFooter.Font = New Font("Segoe UI", 8.0!, FontStyle.Regular)
            Me.lblSecurityFooter.ForeColor = Color.FromArgb(CType(CType(148, Byte), Integer), CType(CType(163, Byte), Integer), CType(CType(184, Byte), Integer))
            Me.lblSecurityFooter.Location = New Point(34, 355)
            Me.lblSecurityFooter.Name = "lblSecurityFooter"
            Me.lblSecurityFooter.Size = New Size(230, 13)
            Me.lblSecurityFooter.TabIndex = 8
            Me.lblSecurityFooter.Text = "🔒 Protected by 256-Bit SSL Encrypted Auth"
            '
            ' errProvider
            '
            Me.errProvider.ContainerControl = Me
            '
            ' FrmLogin
            '
            Me.AcceptButton = Me.btnLogin
            Me.AutoScaleDimensions = New SizeF(7.0!, 15.0!)
            Me.AutoScaleMode = AutoScaleMode.Font
            Me.CancelButton = Me.btnExit
            Me.ClientSize = New Size(640, 410)
            Me.Controls.Add(Me.pnlRightForm)
            Me.Controls.Add(Me.pnlLeftHero)
            Me.FormBorderStyle = FormBorderStyle.FixedSingle
            Me.MaximizeBox = False
            Me.Name = "FrmLogin"
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.Text = "CA Workforce — Enterprise Sign In"
            Me.pnlLeftHero.ResumeLayout(False)
            Me.pnlLeftHero.PerformLayout()
            Me.pnlRightForm.ResumeLayout(False)
            Me.pnlRightForm.PerformLayout()
            Me.pnlUsernameInput.ResumeLayout(False)
            Me.pnlUsernameInput.PerformLayout()
            Me.pnlPasswordInput.ResumeLayout(False)
            Me.pnlPasswordInput.PerformLayout()
            CType(Me.errProvider, ISupportInitialize).EndInit()
            Me.ResumeLayout(False)
        End Sub
    End Class
End Namespace
