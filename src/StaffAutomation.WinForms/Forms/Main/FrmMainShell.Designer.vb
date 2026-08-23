Imports System.ComponentModel
Imports System.Drawing
Imports System.Windows.Forms

Namespace Forms.Main
    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
    Partial Class FrmMainShell
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

        ' Layout Panels
        Friend WithEvents statusStripShell As StatusStrip
        Friend WithEvents pnlHeader As Panel
        Friend WithEvents pnlNavigation As Panel
        Friend WithEvents pnlWorkspace As Panel

        ' Header Left Cluster Containers
        Friend WithEvents pnlHeaderLeftLogo As Panel
        Friend WithEvents lblHeaderLogo As Label
        Friend WithEvents pnlHeaderTitleContainer As Panel
        Friend WithEvents lblHeaderAppName As Label
        Friend WithEvents lblHeaderBranch As Label
        Friend WithEvents pnlSearchContainer As Panel
        Friend WithEvents txtGlobalSearchPlaceholder As TextBox

        ' Header Right Cluster Container (Dock Right, Width: 540px)
        Friend WithEvents pnlHeaderRight As FlowLayoutPanel
        Friend WithEvents lblHeaderUser As Label
        Friend WithEvents lblHeaderRoleDept As Label
        Friend WithEvents btnHeaderNotifications As Button
        Friend WithEvents lblHeaderClock As Label
        Friend WithEvents btnHeaderLogout As Button
        Friend WithEvents btnHeaderExit As Button
        Friend WithEvents tmrClock As Timer

        ' Navigation Menu Controls
        Friend WithEvents pnlNavHeader As Panel
        Friend WithEvents lblNavHeaderTitle As Label

        ' Status Strip Items
        Friend WithEvents lblStatusDb As ToolStripStatusLabel
        Friend WithEvents lblStatusServer As ToolStripStatusLabel
        Friend WithEvents lblStatusCompany As ToolStripStatusLabel
        Friend WithEvents lblStatusUser As ToolStripStatusLabel
        Friend WithEvents lblStatusRole As ToolStripStatusLabel
        Friend WithEvents lblStatusFY As ToolStripStatusLabel
        Friend WithEvents lblStatusVersion As ToolStripStatusLabel
        Friend WithEvents lblStatusMemory As ToolStripStatusLabel
        Friend WithEvents lblStatusTime As ToolStripStatusLabel

        <System.Diagnostics.DebuggerStepThrough()>
        Private Sub InitializeComponent()
            Me.components = New Container()
            Me.statusStripShell = New StatusStrip()
            Me.pnlHeader = New Panel()
            Me.pnlNavigation = New Panel()
            Me.pnlWorkspace = New Panel()

            ' Header Left Controls
            Me.pnlHeaderLeftLogo = New Panel()
            Me.lblHeaderLogo = New Label()
            Me.pnlHeaderTitleContainer = New Panel()
            Me.lblHeaderAppName = New Label()
            Me.lblHeaderBranch = New Label()
            Me.pnlSearchContainer = New Panel()
            Me.txtGlobalSearchPlaceholder = New TextBox()

            ' Header Right Controls (FlowLayoutPanel Docked Right)
            Me.pnlHeaderRight = New FlowLayoutPanel()
            Me.lblHeaderUser = New Label()
            Me.lblHeaderRoleDept = New Label()
            Me.btnHeaderNotifications = New Button()
            Me.lblHeaderClock = New Label()
            Me.btnHeaderLogout = New Button()
            Me.btnHeaderExit = New Button()
            Me.tmrClock = New Timer(Me.components)

            ' Nav Controls
            Me.pnlNavHeader = New Panel()
            Me.lblNavHeaderTitle = New Label()

            ' Status Strip Labels
            Me.lblStatusDb = New ToolStripStatusLabel()
            Me.lblStatusServer = New ToolStripStatusLabel()
            Me.lblStatusCompany = New ToolStripStatusLabel()
            Me.lblStatusUser = New ToolStripStatusLabel()
            Me.lblStatusRole = New ToolStripStatusLabel()
            Me.lblStatusFY = New ToolStripStatusLabel()
            Me.lblStatusVersion = New ToolStripStatusLabel()
            Me.lblStatusMemory = New ToolStripStatusLabel()
            Me.lblStatusTime = New ToolStripStatusLabel()

            Me.pnlHeader.SuspendLayout()
            Me.pnlHeaderLeftLogo.SuspendLayout()
            Me.pnlHeaderTitleContainer.SuspendLayout()
            Me.pnlSearchContainer.SuspendLayout()
            Me.pnlHeaderRight.SuspendLayout()
            Me.pnlNavigation.SuspendLayout()
            Me.statusStripShell.SuspendLayout()
            Me.SuspendLayout()

            '
            ' pnlHeader (Height: 70px)
            '
            ' Order of Docking: pnlHeaderRight FIRST so it docks right immediately!
            Me.pnlHeader.Controls.Add(Me.pnlHeaderRight)
            Me.pnlHeader.Controls.Add(Me.pnlSearchContainer)
            Me.pnlHeader.Controls.Add(Me.pnlHeaderTitleContainer)
            Me.pnlHeader.Controls.Add(Me.pnlHeaderLeftLogo)
            Me.pnlHeader.Dock = DockStyle.Top
            Me.pnlHeader.Height = 70
            Me.pnlHeader.Name = "pnlHeader"
            Me.pnlHeader.TabIndex = 0

            '
            ' pnlHeaderRight (Dock Right, Width: 540px)
            '
            Me.pnlHeaderRight.Controls.Add(Me.lblHeaderUser)
            Me.pnlHeaderRight.Controls.Add(Me.lblHeaderRoleDept)
            Me.pnlHeaderRight.Controls.Add(Me.btnHeaderNotifications)
            Me.pnlHeaderRight.Controls.Add(Me.lblHeaderClock)
            Me.pnlHeaderRight.Controls.Add(Me.btnHeaderLogout)
            Me.pnlHeaderRight.Controls.Add(Me.btnHeaderExit)
            Me.pnlHeaderRight.Dock = DockStyle.Right
            Me.pnlHeaderRight.Width = 540
            Me.pnlHeaderRight.FlowDirection = FlowDirection.LeftToRight
            Me.pnlHeaderRight.Padding = New Padding(0, 16, 10, 5)
            Me.pnlHeaderRight.WrapContents = False

            '
            ' lblHeaderUser
            '
            Me.lblHeaderUser.AutoSize = True
            Me.lblHeaderUser.Margin = New Padding(0, 4, 8, 0)
            Me.lblHeaderUser.Text = "👤 System Administrator"
            Me.lblHeaderUser.Font = New Font(ThemeConstants.FontNameDefault, 8.75!, FontStyle.Bold)

            '
            ' lblHeaderRoleDept
            '
            Me.lblHeaderRoleDept.AutoSize = True
            Me.lblHeaderRoleDept.Margin = New Padding(0, 6, 8, 0)
            Me.lblHeaderRoleDept.Text = "Role: Admin | FY: 2026-27"
            Me.lblHeaderRoleDept.Font = New Font(ThemeConstants.FontNameDefault, 8.0!, FontStyle.Regular)

            '
            ' btnHeaderNotifications
            '
            Me.btnHeaderNotifications.Margin = New Padding(0, 0, 6, 0)
            Me.btnHeaderNotifications.Size = New Size(80, 32)
            Me.btnHeaderNotifications.Text = "🔔 Alerts (0)"
            Me.btnHeaderNotifications.FlatStyle = FlatStyle.Flat
            Me.btnHeaderNotifications.Cursor = Cursors.Hand

            '
            ' lblHeaderClock
            '
            Me.lblHeaderClock.AutoSize = True
            Me.lblHeaderClock.Margin = New Padding(0, 7, 8, 0)
            Me.lblHeaderClock.Text = "🕒 01:58 PM"
            Me.lblHeaderClock.Font = New Font(ThemeConstants.FontNameDefault, 8.25!, FontStyle.Bold)

            '
            ' btnHeaderLogout
            '
            Me.btnHeaderLogout.Margin = New Padding(0, 0, 6, 0)
            Me.btnHeaderLogout.Size = New Size(65, 32)
            Me.btnHeaderLogout.Text = "Logout"
            Me.btnHeaderLogout.FlatStyle = FlatStyle.Flat
            Me.btnHeaderLogout.Cursor = Cursors.Hand

            '
            ' btnHeaderExit
            '
            Me.btnHeaderExit.Margin = New Padding(0, 0, 0, 0)
            Me.btnHeaderExit.Size = New Size(50, 32)
            Me.btnHeaderExit.Text = "Exit"
            Me.btnHeaderExit.FlatStyle = FlatStyle.Flat
            Me.btnHeaderExit.Cursor = Cursors.Hand

            '
            ' pnlHeaderLeftLogo (Dock Left, Width: 55px)
            '
            Me.pnlHeaderLeftLogo.Controls.Add(Me.lblHeaderLogo)
            Me.pnlHeaderLeftLogo.Dock = DockStyle.Left
            Me.pnlHeaderLeftLogo.Width = 55

            Me.lblHeaderLogo.Location = New Point(8, 12)
            Me.lblHeaderLogo.Size = New Size(42, 42)
            Me.lblHeaderLogo.Text = "CA"
            Me.lblHeaderLogo.TextAlign = ContentAlignment.MiddleCenter
            Me.lblHeaderLogo.Font = New Font(ThemeConstants.FontNameDefault, 13.0!, FontStyle.Bold)

            '
            ' pnlHeaderTitleContainer (Dock Left, Width: 360px)
            '
            Me.pnlHeaderTitleContainer.Controls.Add(Me.lblHeaderAppName)
            Me.pnlHeaderTitleContainer.Controls.Add(Me.lblHeaderBranch)
            Me.pnlHeaderTitleContainer.Dock = DockStyle.Left
            Me.pnlHeaderTitleContainer.Width = 360

            Me.lblHeaderAppName.AutoSize = True
            Me.lblHeaderAppName.Location = New Point(4, 12)
            Me.lblHeaderAppName.Text = "CA Office Workforce Automation"
            Me.lblHeaderAppName.Font = New Font(ThemeConstants.FontNameDefault, 13.0!, FontStyle.Bold)

            Me.lblHeaderBranch.AutoSize = True
            Me.lblHeaderBranch.Location = New Point(5, 39)
            Me.lblHeaderBranch.Text = "Head Office • Commercial Automation Suite"
            Me.lblHeaderBranch.Font = New Font(ThemeConstants.FontNameDefault, 8.75!, FontStyle.Regular)

            '
            ' pnlSearchContainer (Dock Left, Width: 240px)
            '
            Me.pnlSearchContainer.Controls.Add(Me.txtGlobalSearchPlaceholder)
            Me.pnlSearchContainer.Dock = DockStyle.Left
            Me.pnlSearchContainer.Width = 240

            Me.txtGlobalSearchPlaceholder.Location = New Point(10, 20)
            Me.txtGlobalSearchPlaceholder.Size = New Size(210, 26)
            Me.txtGlobalSearchPlaceholder.Text = "Search Modules................."
            Me.txtGlobalSearchPlaceholder.Enabled = False
            Me.txtGlobalSearchPlaceholder.Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular)

            '
            ' tmrClock
            '
            Me.tmrClock.Enabled = True
            Me.tmrClock.Interval = 1000

            '
            ' pnlNavigation (Width: 220px, Dock Left)
            '
            Me.pnlNavigation.Controls.Add(Me.pnlNavHeader)
            Me.pnlNavigation.Dock = DockStyle.Left
            Me.pnlNavigation.Width = 220
            Me.pnlNavigation.Name = "pnlNavigation"
            Me.pnlNavigation.TabIndex = 1

            '
            ' pnlNavHeader
            '
            Me.pnlNavHeader.Controls.Add(Me.lblNavHeaderTitle)
            Me.pnlNavHeader.Dock = DockStyle.Top
            Me.pnlNavHeader.Height = 36
            Me.pnlNavHeader.Name = "pnlNavHeader"

            '
            ' lblNavHeaderTitle
            '
            Me.lblNavHeaderTitle.AutoSize = True
            Me.lblNavHeaderTitle.Location = New Point(14, 10)
            Me.lblNavHeaderTitle.Text = "MAIN NAVIGATION"
            Me.lblNavHeaderTitle.Font = New Font(ThemeConstants.FontNameDefault, 8.0!, FontStyle.Bold)

            '
            ' pnlWorkspace (Dock Fill)
            '
            Me.pnlWorkspace.Dock = DockStyle.Fill
            Me.pnlWorkspace.Name = "pnlWorkspace"
            Me.pnlWorkspace.TabIndex = 2

            '
            ' statusStripShell (Dock Bottom)
            '
            Me.statusStripShell.Items.AddRange(New ToolStripItem() {
                Me.lblStatusDb,
                Me.lblStatusServer,
                Me.lblStatusCompany,
                Me.lblStatusUser,
                Me.lblStatusRole,
                Me.lblStatusFY,
                Me.lblStatusVersion,
                Me.lblStatusMemory,
                Me.lblStatusTime
            })
            Me.statusStripShell.Dock = DockStyle.Bottom
            Me.statusStripShell.Name = "statusStripShell"
            Me.statusStripShell.TabIndex = 3

            '
            ' Status Items
            '
            Me.lblStatusDb.Name = "lblStatusDb"
            Me.lblStatusDb.Text = "DB: Connected"

            Me.lblStatusServer.Name = "lblStatusServer"
            Me.lblStatusServer.Text = "| Server: LOCALHOST\SQLEXPRESS"

            Me.lblStatusCompany.Name = "lblStatusCompany"
            Me.lblStatusCompany.Text = "| CA Office Automation"

            Me.lblStatusUser.Name = "lblStatusUser"
            Me.lblStatusUser.Text = "| User: System Administrator"

            Me.lblStatusRole.Name = "lblStatusRole"
            Me.lblStatusRole.Text = "| Role: Admin"

            Me.lblStatusFY.Name = "lblStatusFY"
            Me.lblStatusFY.Text = "| FY: 2026-27"

            Me.lblStatusVersion.Name = "lblStatusVersion"
            Me.lblStatusVersion.Text = "| Version: v1.0.0"

            Me.lblStatusMemory.Name = "lblStatusMemory"
            Me.lblStatusMemory.Text = "| Memory: 42.5 MB"

            Me.lblStatusTime.Name = "lblStatusTime"
            Me.lblStatusTime.Spring = True
            Me.lblStatusTime.TextAlign = ContentAlignment.MiddleRight
            Me.lblStatusTime.Text = "Ready"

            '
            ' FrmMainShell
            '
            Me.AutoScaleDimensions = New SizeF(7.0!, 15.0!)
            Me.AutoScaleMode = AutoScaleMode.Font
            Me.ClientSize = New Size(1366, 768)

            ' Add Controls in explicit Dock z-order (pnlWorkspace fills remaining area)
            Me.Controls.Add(Me.pnlWorkspace)
            Me.Controls.Add(Me.pnlNavigation)
            Me.Controls.Add(Me.statusStripShell)
            Me.Controls.Add(Me.pnlHeader)

            Me.IsMdiContainer = False
            Me.MinimumSize = New Size(1280, 720)
            Me.Name = "FrmMainShell"
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.Text = "CA Office Workforce Productivity Automation System"

            Me.pnlHeader.ResumeLayout(False)
            Me.pnlHeaderLeftLogo.ResumeLayout(False)
            Me.pnlHeaderTitleContainer.ResumeLayout(False)
            Me.pnlHeaderTitleContainer.PerformLayout()
            Me.pnlSearchContainer.ResumeLayout(False)
            Me.pnlSearchContainer.PerformLayout()
            Me.pnlHeaderRight.ResumeLayout(False)
            Me.pnlHeaderRight.PerformLayout()
            Me.pnlNavigation.ResumeLayout(False)
            Me.statusStripShell.ResumeLayout(False)
            Me.statusStripShell.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()
        End Sub
    End Class
End Namespace
