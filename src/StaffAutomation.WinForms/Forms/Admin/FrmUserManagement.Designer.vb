Imports System.ComponentModel
Imports System.Drawing
Imports System.Windows.Forms

Namespace Forms.Admin
    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
    Partial Class FrmUserManagement
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
        Private WithEvents pnlForm As Panel
        Private WithEvents lblUsername As Label
        Private WithEvents txtUsername As TextBox
        Private WithEvents lblFullName As Label
        Private WithEvents txtFullName As TextBox
        Private WithEvents lblRole As Label
        Private WithEvents cboRole As ComboBox
        Private WithEvents lblDept As Label
        Private WithEvents cboDept As ComboBox
        Private WithEvents chkActive As CheckBox
        Private WithEvents btnSave As Button
        Private WithEvents btnNew As Button
        Private WithEvents btnResetPassword As Button
        Private WithEvents dgvUsers As DataGridView
        Private WithEvents txtSearch As TextBox
        Private WithEvents lblSearch As Label
        Private WithEvents errProvider As ErrorProvider

        <System.Diagnostics.DebuggerStepThrough()>
        Private Sub InitializeComponent()
            Me.components = New Container()
            Me.pnlHeader = New Panel()
            Me.lblTitle = New Label()
            Me.pnlForm = New Panel()
            Me.btnResetPassword = New Button()
            Me.btnNew = New Button()
            Me.btnSave = New Button()
            Me.chkActive = New CheckBox()
            Me.cboDept = New ComboBox()
            Me.lblDept = New Label()
            Me.cboRole = New ComboBox()
            Me.lblRole = New Label()
            Me.txtFullName = New TextBox()
            Me.lblFullName = New Label()
            Me.txtUsername = New TextBox()
            Me.lblUsername = New Label()
            Me.dgvUsers = New DataGridView()
            Me.txtSearch = New TextBox()
            Me.lblSearch = New Label()
            Me.errProvider = New ErrorProvider(Me.components)
            Me.pnlHeader.SuspendLayout()
            Me.pnlForm.SuspendLayout()
            CType(Me.dgvUsers, ISupportInitialize).BeginInit()
            CType(Me.errProvider, ISupportInitialize).BeginInit()
            Me.SuspendLayout()
            '
            ' pnlHeader
            '
            Me.pnlHeader.BackColor = Color.FromArgb(CType(CType(24, Byte), Integer), CType(CType(43, Byte), Integer), CType(CType(73, Byte), Integer))
            Me.pnlHeader.Controls.Add(Me.lblTitle)
            Me.pnlHeader.Dock = DockStyle.Top
            Me.pnlHeader.Location = New Point(0, 0)
            Me.pnlHeader.Name = "pnlHeader"
            Me.pnlHeader.Size = New Size(1220, 60)
            Me.pnlHeader.TabIndex = 0
            '
            ' lblTitle
            '
            Me.lblTitle.AutoSize = True
            Me.lblTitle.Font = New Font("Segoe UI Semibold", 14.0!, FontStyle.Bold, GraphicsUnit.Point, CType(0, Byte))
            Me.lblTitle.ForeColor = Color.White
            Me.lblTitle.Location = New Point(20, 15)
            Me.lblTitle.Name = "lblTitle"
            Me.lblTitle.Size = New Size(320, 25)
            Me.lblTitle.TabIndex = 0
            Me.lblTitle.Text = "👥 Staff Account & Access Setup"
            '
            ' pnlForm
            '
            Me.pnlForm.BorderStyle = BorderStyle.FixedSingle
            Me.pnlForm.Controls.Add(Me.btnResetPassword)
            Me.pnlForm.Controls.Add(Me.btnNew)
            Me.pnlForm.Controls.Add(Me.btnSave)
            Me.pnlForm.Controls.Add(Me.chkActive)
            Me.pnlForm.Controls.Add(Me.cboDept)
            Me.pnlForm.Controls.Add(Me.lblDept)
            Me.pnlForm.Controls.Add(Me.cboRole)
            Me.pnlForm.Controls.Add(Me.lblRole)
            Me.pnlForm.Controls.Add(Me.txtFullName)
            Me.pnlForm.Controls.Add(Me.lblFullName)
            Me.pnlForm.Controls.Add(Me.txtUsername)
            Me.pnlForm.Controls.Add(Me.lblUsername)
            Me.pnlForm.Location = New Point(16, 76)
            Me.pnlForm.Name = "pnlForm"
            Me.pnlForm.Size = New Size(590, 544)
            Me.pnlForm.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left
            Me.pnlForm.TabIndex = 1
            '
            ' lblUsername
            '
            Me.lblUsername.AutoSize = True
            Me.lblUsername.Location = New Point(15, 15)
            Me.lblUsername.Name = "lblUsername"
            Me.lblUsername.Size = New Size(60, 15)
            Me.lblUsername.TabIndex = 0
            Me.lblUsername.Text = "Username"
            '
            ' txtUsername
            '
            Me.txtUsername.Location = New Point(18, 33)
            Me.txtUsername.Name = "txtUsername"
            Me.txtUsername.Size = New Size(280, 23)
            Me.txtUsername.TabIndex = 1
            '
            ' lblFullName
            '
            Me.lblFullName.AutoSize = True
            Me.lblFullName.Location = New Point(315, 15)
            Me.lblFullName.Name = "lblFullName"
            Me.lblFullName.Size = New Size(61, 15)
            Me.lblFullName.TabIndex = 2
            Me.lblFullName.Text = "Full Name"
            '
            ' txtFullName
            '
            Me.txtFullName.Location = New Point(318, 33)
            Me.txtFullName.Name = "txtFullName"
            Me.txtFullName.Size = New Size(280, 23)
            Me.txtFullName.TabIndex = 3
            '
            ' lblRole
            '
            Me.lblRole.AutoSize = True
            Me.lblRole.Location = New Point(15, 65)
            Me.lblRole.Name = "lblRole"
            Me.lblRole.Size = New Size(79, 15)
            Me.lblRole.TabIndex = 4
            Me.lblRole.Text = "Assigned Role"
            Me.lblRole.Visible = False
            '
            ' cboRole
            '
            Me.cboRole.DropDownStyle = ComboBoxStyle.DropDownList
            Me.cboRole.FormattingEnabled = True
            Me.cboRole.Location = New Point(18, 83)
            Me.cboRole.Name = "cboRole"
            Me.cboRole.Size = New Size(280, 23)
            Me.cboRole.TabIndex = 5
            Me.cboRole.Visible = False
            '
            ' lblDept
            '
            Me.lblDept.AutoSize = True
            Me.lblDept.Location = New Point(15, 65)
            Me.lblDept.Name = "lblDept"
            Me.lblDept.Size = New Size(70, 15)
            Me.lblDept.TabIndex = 6
            Me.lblDept.Text = "Department"
            '
            ' cboDept
            '
            Me.cboDept.DropDownStyle = ComboBoxStyle.DropDownList
            Me.cboDept.FormattingEnabled = True
            Me.cboDept.Location = New Point(18, 83)
            Me.cboDept.Name = "cboDept"
            Me.cboDept.Size = New Size(280, 23)
            Me.cboDept.TabIndex = 7
            '
            ' chkActive
            '
            Me.chkActive.AutoSize = True
            Me.chkActive.Checked = True
            Me.chkActive.CheckState = CheckState.Checked
            Me.chkActive.Location = New Point(318, 85)
            Me.chkActive.Name = "chkActive"
            Me.chkActive.Size = New Size(105, 19)
            Me.chkActive.TabIndex = 8
            Me.chkActive.Text = "Account Active"
            Me.chkActive.UseVisualStyleBackColor = True
            '
            ' btnSave
            '
            Me.btnSave.BackColor = Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(120, Byte), Integer), CType(CType(215, Byte), Integer))
            Me.btnSave.FlatStyle = FlatStyle.Flat
            Me.btnSave.Font = New Font("Segoe UI Semibold", 9.0!, FontStyle.Bold, GraphicsUnit.Point, CType(0, Byte))
            Me.btnSave.ForeColor = Color.White
            Me.btnSave.Location = New Point(390, 595)
            Me.btnSave.Name = "btnSave"
            Me.btnSave.Size = New Size(210, 34)
            Me.btnSave.TabIndex = 9
            Me.btnSave.Text = "💾 Save Staff Account"
            Me.btnSave.UseVisualStyleBackColor = False
            '
            ' btnNew
            '
            Me.btnNew.Location = New Point(18, 595)
            Me.btnNew.Name = "btnNew"
            Me.btnNew.Size = New Size(140, 34)
            Me.btnNew.TabIndex = 10
            Me.btnNew.Text = "➕ New Account"
            Me.btnNew.UseVisualStyleBackColor = True
            '
            ' btnResetPassword
            '
            Me.btnResetPassword.Location = New Point(170, 595)
            Me.btnResetPassword.Name = "btnResetPassword"
            Me.btnResetPassword.Size = New Size(210, 34)
            Me.btnResetPassword.TabIndex = 11
            Me.btnResetPassword.Text = "🔑 Reset Temp Password"
            Me.btnResetPassword.UseVisualStyleBackColor = True
            '
            ' dgvUsers
            '
            Me.dgvUsers.AllowUserToAddRows = False
            Me.dgvUsers.AllowUserToDeleteRows = False
            Me.dgvUsers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            Me.dgvUsers.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            Me.dgvUsers.Location = New Point(620, 115)
            Me.dgvUsers.MultiSelect = False
            Me.dgvUsers.Name = "dgvUsers"
            Me.dgvUsers.ReadOnly = True
            Me.dgvUsers.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            Me.dgvUsers.Size = New Size(520, 505)
            Me.dgvUsers.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
            Me.dgvUsers.TabIndex = 2
            '
            ' txtSearch
            '
            Me.txtSearch.Location = New Point(685, 80)
            Me.txtSearch.Name = "txtSearch"
            Me.txtSearch.Size = New Size(455, 23)
            Me.txtSearch.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
            Me.txtSearch.TabIndex = 3
            '
            ' lblSearch
            '
            Me.lblSearch.AutoSize = True
            Me.lblSearch.Location = New Point(620, 83)
            Me.lblSearch.Name = "lblSearch"
            Me.lblSearch.Size = New Size(55, 15)
            Me.lblSearch.TabIndex = 4
            Me.lblSearch.Text = "🔍 Search"
            '
            ' errProvider
            '
            Me.errProvider.ContainerControl = Me
            '
            ' FrmUserManagement
            '
            Me.AutoScaleDimensions = New SizeF(7.0!, 15.0!)
            Me.AutoScaleMode = AutoScaleMode.Font
            Me.ClientSize = New Size(1160, 640)
            Me.MinimumSize = New Size(980, 580)
            Me.Controls.Add(Me.lblSearch)
            Me.Controls.Add(Me.txtSearch)
            Me.Controls.Add(Me.dgvUsers)
            Me.Controls.Add(Me.pnlForm)
            Me.Controls.Add(Me.pnlHeader)
            Me.FormBorderStyle = FormBorderStyle.FixedSingle
            Me.MaximizeBox = False
            Me.Name = "FrmUserManagement"
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.Text = "👥 Staff Account & Access Setup"
            Me.pnlHeader.ResumeLayout(False)
            Me.pnlHeader.PerformLayout()
            Me.pnlForm.ResumeLayout(False)
            Me.pnlForm.PerformLayout()
            CType(Me.dgvUsers, ISupportInitialize).EndInit()
            CType(Me.errProvider, ISupportInitialize).EndInit()
            Me.ResumeLayout(False)
            Me.PerformLayout()
        End Sub
    End Class
End Namespace
