Imports System.ComponentModel
Imports System.Drawing
Imports System.Windows.Forms

Namespace Forms.Tasks
    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
    Partial Class FrmTaskWorkspace
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

        Private WithEvents pnlHeader As Panel
        Private WithEvents lblTitle As Label
        Private WithEvents btnBackToTasks As Button
        Private WithEvents dgvMyTasks As DataGridView
        Private WithEvents pnlActions As Panel
        Private WithEvents btnStartTimer As Button
        Private WithEvents btnPauseTimer As Button
        Private WithEvents btnLogDiscussion As Button
        Private WithEvents btnChangeStatus As Button
        Private WithEvents cboTargetStatus As ComboBox
        Private WithEvents lblTargetStatus As Label
        Private WithEvents pnlDiscussion As Panel
        Private WithEvents lblCommType As Label
        Private WithEvents cboCommType As ComboBox
        Private WithEvents lblOutcome As Label
        Private WithEvents cboOutcome As ComboBox
        Private WithEvents lblDiscDuration As Label
        Private WithEvents txtDiscDuration As TextBox
        Private WithEvents lblDiscNotes As Label
        Private WithEvents txtDiscNotes As TextBox
        Private WithEvents btnSubmitDiscussion As Button
        Private WithEvents errProvider As ErrorProvider

        <System.Diagnostics.DebuggerStepThrough()>
        Private Sub InitializeComponent()
            Me.components = New Container()
            Me.pnlHeader = New Panel()
            Me.lblTitle = New Label()
            Me.btnBackToTasks = New Button()
            Me.dgvMyTasks = New DataGridView()
            Me.pnlActions = New Panel()
            Me.lblTargetStatus = New Label()
            Me.cboTargetStatus = New ComboBox()
            Me.btnChangeStatus = New Button()
            Me.btnLogDiscussion = New Button()
            Me.btnPauseTimer = New Button()
            Me.btnStartTimer = New Button()
            Me.pnlDiscussion = New Panel()
            Me.btnSubmitDiscussion = New Button()
            Me.txtDiscNotes = New TextBox()
            Me.lblDiscNotes = New Label()
            Me.txtDiscDuration = New TextBox()
            Me.lblDiscDuration = New Label()
            Me.cboOutcome = New ComboBox()
            Me.lblOutcome = New Label()
            Me.cboCommType = New ComboBox()
            Me.lblCommType = New Label()
            Me.errProvider = New ErrorProvider(Me.components)
            Me.pnlHeader.SuspendLayout()
            CType(Me.dgvMyTasks, ISupportInitialize).BeginInit()
            Me.pnlActions.SuspendLayout()
            Me.pnlDiscussion.SuspendLayout()
            CType(Me.errProvider, ISupportInitialize).BeginInit()
            Me.SuspendLayout()
            '
            ' pnlHeader
            '
            Me.pnlHeader.BackColor = Color.FromArgb(CType(CType(24, Byte), Integer), CType(CType(43, Byte), Integer), CType(CType(73, Byte), Integer))
            Me.pnlHeader.Controls.Add(Me.btnBackToTasks)
            Me.pnlHeader.Controls.Add(Me.lblTitle)
            Me.pnlHeader.Dock = DockStyle.Top
            Me.pnlHeader.Location = New Point(0, 0)
            Me.pnlHeader.Name = "pnlHeader"
            Me.pnlHeader.Size = New Size(984, 60)
            Me.pnlHeader.TabIndex = 0
            '
            ' btnBackToTasks
            '
            Me.btnBackToTasks.Anchor = AnchorStyles.Top Or AnchorStyles.Right
            Me.btnBackToTasks.BackColor = Color.FromArgb(CType(CType(51, Byte), Integer), CType(CType(65, Byte), Integer), CType(CType(85, Byte), Integer))
            Me.btnBackToTasks.FlatStyle = FlatStyle.Flat
            Me.btnBackToTasks.Font = New Font("Segoe UI Semibold", 9.0!, FontStyle.Bold)
            Me.btnBackToTasks.ForeColor = Color.White
            Me.btnBackToTasks.Location = New Point(790, 14)
            Me.btnBackToTasks.Name = "btnBackToTasks"
            Me.btnBackToTasks.Size = New Size(174, 32)
            Me.btnBackToTasks.TabIndex = 1
            Me.btnBackToTasks.Text = "← Back to Daily Tasks"
            Me.btnBackToTasks.UseVisualStyleBackColor = False
            '
            ' lblTitle
            '
            Me.lblTitle.AutoSize = True
            Me.lblTitle.Font = New Font("Segoe UI Semibold", 14.0!, FontStyle.Bold, GraphicsUnit.Point, CType(0, Byte))
            Me.lblTitle.ForeColor = Color.White
            Me.lblTitle.Location = New Point(20, 15)
            Me.lblTitle.Name = "lblTitle"
            Me.lblTitle.Size = New Size(247, 25)
            Me.lblTitle.TabIndex = 0
            Me.lblTitle.Text = "Employee Task Workspace"
            '
            ' dgvMyTasks
            '
            Me.dgvMyTasks.AllowUserToAddRows = False
            Me.dgvMyTasks.AllowUserToDeleteRows = False
            Me.dgvMyTasks.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            Me.dgvMyTasks.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            Me.dgvMyTasks.Location = New Point(20, 80)
            Me.dgvMyTasks.MultiSelect = False
            Me.dgvMyTasks.Name = "dgvMyTasks"
            Me.dgvMyTasks.ReadOnly = True
            Me.dgvMyTasks.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            Me.dgvMyTasks.Size = New Size(944, 250)
            Me.dgvMyTasks.TabIndex = 1
            '
            ' pnlActions
            '
            Me.pnlActions.BorderStyle = BorderStyle.FixedSingle
            Me.pnlActions.Controls.Add(Me.lblTargetStatus)
            Me.pnlActions.Controls.Add(Me.cboTargetStatus)
            Me.pnlActions.Controls.Add(Me.btnChangeStatus)
            Me.pnlActions.Controls.Add(Me.btnLogDiscussion)
            Me.pnlActions.Controls.Add(Me.btnPauseTimer)
            Me.pnlActions.Controls.Add(Me.btnStartTimer)
            Me.pnlActions.Location = New Point(20, 345)
            Me.pnlActions.Name = "pnlActions"
            Me.pnlActions.Size = New Size(944, 55)
            Me.pnlActions.TabIndex = 2
            '
            ' lblTargetStatus
            '
            Me.lblTargetStatus.AutoSize = True
            Me.lblTargetStatus.Location = New Point(530, 20)
            Me.lblTargetStatus.Name = "lblTargetStatus"
            Me.lblTargetStatus.Size = New Size(77, 15)
            Me.lblTargetStatus.TabIndex = 5
            Me.lblTargetStatus.Text = "Move Status:"
            '
            ' cboTargetStatus
            '
            Me.cboTargetStatus.DropDownStyle = ComboBoxStyle.DropDownList
            Me.cboTargetStatus.FormattingEnabled = True
            Me.cboTargetStatus.Location = New Point(613, 16)
            Me.cboTargetStatus.Name = "cboTargetStatus"
            Me.cboTargetStatus.Size = New Size(180, 23)
            Me.cboTargetStatus.TabIndex = 4
            '
            ' btnChangeStatus
            '
            Me.btnChangeStatus.Location = New Point(800, 12)
            Me.btnChangeStatus.Name = "btnChangeStatus"
            Me.btnChangeStatus.Size = New Size(125, 30)
            Me.btnChangeStatus.TabIndex = 3
            Me.btnChangeStatus.Text = "Update Status"
            Me.btnChangeStatus.UseVisualStyleBackColor = True
            '
            ' btnLogDiscussion
            '
            Me.btnLogDiscussion.Location = New Point(270, 12)
            Me.btnLogDiscussion.Name = "btnLogDiscussion"
            Me.btnLogDiscussion.Size = New Size(130, 30)
            Me.btnLogDiscussion.TabIndex = 2
            Me.btnLogDiscussion.Text = "Log Discussion"
            Me.btnLogDiscussion.UseVisualStyleBackColor = True
            '
            ' btnPauseTimer
            '
            Me.btnPauseTimer.BackColor = Color.FromArgb(CType(CType(200, Byte), Integer), CType(CType(50, Byte), Integer), CType(CType(50, Byte), Integer))
            Me.btnPauseTimer.FlatStyle = FlatStyle.Flat
            Me.btnPauseTimer.Font = New Font("Segoe UI Semibold", 9.0!, FontStyle.Bold, GraphicsUnit.Point, CType(0, Byte))
            Me.btnPauseTimer.ForeColor = Color.White
            Me.btnPauseTimer.Location = New Point(140, 12)
            Me.btnPauseTimer.Name = "btnPauseTimer"
            Me.btnPauseTimer.Size = New Size(115, 30)
            Me.btnPauseTimer.TabIndex = 1
            Me.btnPauseTimer.Text = "Pause Timer"
            Me.btnPauseTimer.UseVisualStyleBackColor = False
            '
            ' btnStartTimer
            '
            Me.btnStartTimer.BackColor = Color.FromArgb(CType(CType(40, Byte), Integer), CType(CType(160, Byte), Integer), CType(CType(70, Byte), Integer))
            Me.btnStartTimer.FlatStyle = FlatStyle.Flat
            Me.btnStartTimer.Font = New Font("Segoe UI Semibold", 9.0!, FontStyle.Bold, GraphicsUnit.Point, CType(0, Byte))
            Me.btnStartTimer.ForeColor = Color.White
            Me.btnStartTimer.Location = New Point(15, 12)
            Me.btnStartTimer.Name = "btnStartTimer"
            Me.btnStartTimer.Size = New Size(115, 30)
            Me.btnStartTimer.TabIndex = 0
            Me.btnStartTimer.Text = "Start Timer"
            Me.btnStartTimer.UseVisualStyleBackColor = False
            '
            ' pnlDiscussion
            '
            Me.pnlDiscussion.BorderStyle = BorderStyle.FixedSingle
            Me.pnlDiscussion.Controls.Add(Me.btnSubmitDiscussion)
            Me.pnlDiscussion.Controls.Add(Me.txtDiscNotes)
            Me.pnlDiscussion.Controls.Add(Me.lblDiscNotes)
            Me.pnlDiscussion.Controls.Add(Me.txtDiscDuration)
            Me.pnlDiscussion.Controls.Add(Me.lblDiscDuration)
            Me.pnlDiscussion.Controls.Add(Me.cboOutcome)
            Me.pnlDiscussion.Controls.Add(Me.lblOutcome)
            Me.pnlDiscussion.Controls.Add(Me.cboCommType)
            Me.pnlDiscussion.Controls.Add(Me.lblCommType)
            Me.pnlDiscussion.Location = New Point(20, 410)
            Me.pnlDiscussion.Name = "pnlDiscussion"
            Me.pnlDiscussion.Size = New Size(944, 135)
            Me.pnlDiscussion.TabIndex = 3
            Me.pnlDiscussion.Visible = False
            '
            ' btnSubmitDiscussion
            '
            Me.btnSubmitDiscussion.BackColor = Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(120, Byte), Integer), CType(CType(215, Byte), Integer))
            Me.btnSubmitDiscussion.FlatStyle = FlatStyle.Flat
            Me.btnSubmitDiscussion.Font = New Font("Segoe UI Semibold", 9.0!, FontStyle.Bold, GraphicsUnit.Point, CType(0, Byte))
            Me.btnSubmitDiscussion.ForeColor = Color.White
            Me.btnSubmitDiscussion.Location = New Point(800, 85)
            Me.btnSubmitDiscussion.Name = "btnSubmitDiscussion"
            Me.btnSubmitDiscussion.Size = New Size(125, 32)
            Me.btnSubmitDiscussion.TabIndex = 8
            Me.btnSubmitDiscussion.Text = "Save Discussion"
            Me.btnSubmitDiscussion.UseVisualStyleBackColor = False
            '
            ' txtDiscNotes
            '
            Me.txtDiscNotes.Location = New Point(15, 80)
            Me.txtDiscNotes.Multiline = True
            Me.txtDiscNotes.Name = "txtDiscNotes"
            Me.txtDiscNotes.Size = New Size(765, 40)
            Me.txtDiscNotes.TabIndex = 7
            '
            ' lblDiscNotes
            '
            Me.lblDiscNotes.AutoSize = True
            Me.lblDiscNotes.Location = New Point(15, 62)
            Me.lblDiscNotes.Name = "lblDiscNotes"
            Me.lblDiscNotes.Size = New Size(100, 15)
            Me.lblDiscNotes.TabIndex = 6
            Me.lblDiscNotes.Text = "Discussion Notes"
            '
            ' txtDiscDuration
            '
            Me.txtDiscDuration.Location = New Point(530, 30)
            Me.txtDiscDuration.Name = "txtDiscDuration"
            Me.txtDiscDuration.Size = New Size(100, 23)
            Me.txtDiscDuration.TabIndex = 5
            Me.txtDiscDuration.Text = "15"
            '
            ' lblDiscDuration
            '
            Me.lblDiscDuration.AutoSize = True
            Me.lblDiscDuration.Location = New Point(530, 12)
            Me.lblDiscDuration.Name = "lblDiscDuration"
            Me.lblDiscDuration.Size = New Size(89, 15)
            Me.lblDiscDuration.TabIndex = 4
            Me.lblDiscDuration.Text = "Duration (Mins)"
            '
            ' cboOutcome
            '
            Me.cboOutcome.DropDownStyle = ComboBoxStyle.DropDownList
            Me.cboOutcome.FormattingEnabled = True
            Me.cboOutcome.Location = New Point(270, 30)
            Me.cboOutcome.Name = "cboOutcome"
            Me.cboOutcome.Size = New Size(230, 23)
            Me.cboOutcome.TabIndex = 3
            '
            ' lblOutcome
            '
            Me.lblOutcome.AutoSize = True
            Me.lblOutcome.Location = New Point(270, 12)
            Me.lblOutcome.Name = "lblOutcome"
            Me.lblOutcome.Size = New Size(137, 15)
            Me.lblOutcome.TabIndex = 2
            Me.lblOutcome.Text = "Communication Outcome"
            '
            ' cboCommType
            '
            Me.cboCommType.DropDownStyle = ComboBoxStyle.DropDownList
            Me.cboCommType.FormattingEnabled = True
            Me.cboCommType.Location = New Point(15, 30)
            Me.cboCommType.Name = "cboCommType"
            Me.cboCommType.Size = New Size(230, 23)
            Me.cboCommType.TabIndex = 1
            '
            ' lblCommType
            '
            Me.lblCommType.AutoSize = True
            Me.lblCommType.Location = New Point(15, 12)
            Me.lblCommType.Name = "lblCommType"
            Me.lblCommType.Size = New Size(117, 15)
            Me.lblCommType.TabIndex = 0
            Me.lblCommType.Text = "Communication Type"
            '
            ' errProvider
            '
            Me.errProvider.ContainerControl = Me
            '
            ' FrmTaskWorkspace
            '
            Me.AutoScaleDimensions = New SizeF(7.0!, 15.0!)
            Me.AutoScaleMode = AutoScaleMode.Font
            Me.ClientSize = New Size(984, 561)
            Me.Controls.Add(Me.pnlDiscussion)
            Me.Controls.Add(Me.pnlActions)
            Me.Controls.Add(Me.dgvMyTasks)
            Me.Controls.Add(Me.pnlHeader)
            Me.FormBorderStyle = FormBorderStyle.FixedSingle
            Me.MaximizeBox = False
            Me.Name = "FrmTaskWorkspace"
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.Text = "Employee Daily Task Workspace"
            Me.pnlHeader.ResumeLayout(False)
            Me.pnlHeader.PerformLayout()
            CType(Me.dgvMyTasks, ISupportInitialize).BeginInit()
            Me.pnlActions.ResumeLayout(False)
            Me.pnlActions.PerformLayout()
            Me.pnlDiscussion.ResumeLayout(False)
            Me.pnlDiscussion.PerformLayout()
            CType(Me.errProvider, ISupportInitialize).BeginInit()
            Me.ResumeLayout(False)
        End Sub
    End Class
End Namespace
