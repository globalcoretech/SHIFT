Imports System.ComponentModel
Imports System.Drawing
Imports System.Windows.Forms

Namespace Forms.Tasks
    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
    Partial Class FrmTaskManagement
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
        Private WithEvents lblSubtitle As Label
        Private WithEvents pnlForm As Panel
        Private WithEvents lblClient As Label
        Private WithEvents cboClient As ComboBox
        Private WithEvents lblTaskTitle As Label
        Private WithEvents txtTaskTitle As TextBox
        Private WithEvents lblAssignee As Label
        Private WithEvents cboAssignee As ComboBox
        Private WithEvents lblPriority As Label
        Private WithEvents cboPriority As ComboBox
        Private WithEvents lblStatus As Label
        Private WithEvents cboStatus As ComboBox
        Private WithEvents lblStartDate As Label
        Private WithEvents dtpStartDate As DateTimePicker
        Private WithEvents lblDueDate As Label
        Private WithEvents dtpDueDate As DateTimePicker
        Private WithEvents lblDesc As Label
        Private WithEvents txtDesc As TextBox
        Private WithEvents btnSave As Button
        Private WithEvents btnNew As Button
        Private WithEvents btnComplete As Button
        Private WithEvents btnDelete As Button
        Private WithEvents dgvTasks As DataGridView
        Private WithEvents txtSearch As TextBox
        Private WithEvents lblSearch As Label
        Private WithEvents lblFilter As Label
        Private WithEvents cboStatusFilter As ComboBox
        Private WithEvents btnRefresh As Button
        Private WithEvents errProvider As ErrorProvider

        <System.Diagnostics.DebuggerStepThrough()>
        Private Sub InitializeComponent()
            Me.components = New Container()
            Me.pnlHeader = New Panel()
            Me.lblTitle = New Label()
            Me.lblSubtitle = New Label()
            Me.pnlForm = New Panel()
            Me.btnDelete = New Button()
            Me.btnComplete = New Button()
            Me.btnNew = New Button()
            Me.btnSave = New Button()
            Me.txtDesc = New TextBox()
            Me.lblDesc = New Label()
            Me.dtpDueDate = New DateTimePicker()
            Me.lblDueDate = New Label()
            Me.dtpStartDate = New DateTimePicker()
            Me.lblStartDate = New Label()
            Me.cboStatus = New ComboBox()
            Me.lblStatus = New Label()
            Me.cboPriority = New ComboBox()
            Me.lblPriority = New Label()
            Me.cboAssignee = New ComboBox()
            Me.lblAssignee = New Label()
            Me.txtTaskTitle = New TextBox()
            Me.lblTaskTitle = New Label()
            Me.cboClient = New ComboBox()
            Me.lblClient = New Label()
            Me.dgvTasks = New DataGridView()
            Me.txtSearch = New TextBox()
            Me.lblSearch = New Label()
            Me.lblFilter = New Label()
            Me.cboStatusFilter = New ComboBox()
            Me.btnRefresh = New Button()
            Me.errProvider = New ErrorProvider(Me.components)
            Me.pnlHeader.SuspendLayout()
            Me.pnlForm.SuspendLayout()
            CType(Me.dgvTasks, ISupportInitialize).BeginInit()
            CType(Me.errProvider, ISupportInitialize).BeginInit()
            Me.SuspendLayout()
            '
            ' pnlHeader
            '
            Me.pnlHeader.BackColor = Color.FromArgb(CType(CType(24, Byte), Integer), CType(CType(43, Byte), Integer), CType(CType(73, Byte), Integer))
            Me.pnlHeader.Controls.Add(Me.lblSubtitle)
            Me.pnlHeader.Controls.Add(Me.lblTitle)
            Me.pnlHeader.Dock = DockStyle.Top
            Me.pnlHeader.Location = New Point(0, 0)
            Me.pnlHeader.Name = "pnlHeader"
            Me.pnlHeader.Size = New Size(984, 60)
            Me.pnlHeader.TabIndex = 0
            '
            ' lblTitle
            '
            Me.lblTitle.AutoSize = True
            Me.lblTitle.Font = New Font("Segoe UI Semibold", 13.0!, FontStyle.Bold, GraphicsUnit.Point, CType(0, Byte))
            Me.lblTitle.ForeColor = Color.White
            Me.lblTitle.Location = New Point(20, 10)
            Me.lblTitle.Name = "lblTitle"
            Me.lblTitle.Size = New Size(215, 25)
            Me.lblTitle.TabIndex = 0
            Me.lblTitle.Text = "Daily Task Management"
            '
            ' lblSubtitle
            '
            Me.lblSubtitle.AutoSize = True
            Me.lblSubtitle.Font = New Font("Segoe UI", 9.0!, FontStyle.Regular, GraphicsUnit.Point, CType(0, Byte))
            Me.lblSubtitle.ForeColor = Color.FromArgb(CType(CType(208, Byte), Integer), CType(CType(215, Byte), Integer), CType(CType(222, Byte), Integer))
            Me.lblSubtitle.Location = New Point(20, 35)
            Me.lblSubtitle.Name = "lblSubtitle"
            Me.lblSubtitle.Size = New Size(440, 15)
            Me.lblSubtitle.TabIndex = 1
            Me.lblSubtitle.Text = "Create, assign, edit, track, complete, and manage firm client tasks with SQL audit trail."
            '
            ' pnlForm
            '
            Me.pnlForm.BorderStyle = BorderStyle.FixedSingle
            Me.pnlForm.Controls.Add(Me.btnDelete)
            Me.pnlForm.Controls.Add(Me.btnComplete)
            Me.pnlForm.Controls.Add(Me.btnNew)
            Me.pnlForm.Controls.Add(Me.btnSave)
            Me.pnlForm.Controls.Add(Me.txtDesc)
            Me.pnlForm.Controls.Add(Me.lblDesc)
            Me.pnlForm.Controls.Add(Me.dtpDueDate)
            Me.pnlForm.Controls.Add(Me.lblDueDate)
            Me.pnlForm.Controls.Add(Me.dtpStartDate)
            Me.pnlForm.Controls.Add(Me.lblStartDate)
            Me.pnlForm.Controls.Add(Me.cboStatus)
            Me.pnlForm.Controls.Add(Me.lblStatus)
            Me.pnlForm.Controls.Add(Me.cboPriority)
            Me.pnlForm.Controls.Add(Me.lblPriority)
            Me.pnlForm.Controls.Add(Me.cboAssignee)
            Me.pnlForm.Controls.Add(Me.lblAssignee)
            Me.pnlForm.Controls.Add(Me.txtTaskTitle)
            Me.pnlForm.Controls.Add(Me.lblTaskTitle)
            Me.pnlForm.Controls.Add(Me.cboClient)
            Me.pnlForm.Controls.Add(Me.lblClient)
            Me.pnlForm.Location = New Point(20, 75)
            Me.pnlForm.Name = "pnlForm"
            Me.pnlForm.Size = New Size(340, 465)
            Me.pnlForm.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left
            Me.pnlForm.TabIndex = 1
            '
            ' lblClient
            '
            Me.lblClient.AutoSize = True
            Me.lblClient.Location = New Point(12, 10)
            Me.lblClient.Name = "lblClient"
            Me.lblClient.Size = New Size(38, 15)
            Me.lblClient.TabIndex = 0
            Me.lblClient.Text = "Client"
            '
            ' cboClient
            '
            Me.cboClient.DropDownStyle = ComboBoxStyle.DropDownList
            Me.cboClient.FormattingEnabled = True
            Me.cboClient.Location = New Point(15, 26)
            Me.cboClient.Name = "cboClient"
            Me.cboClient.Size = New Size(305, 23)
            Me.cboClient.TabIndex = 1
            '
            ' lblTaskTitle
            '
            Me.lblTaskTitle.AutoSize = True
            Me.lblTaskTitle.Location = New Point(12, 53)
            Me.lblTaskTitle.Name = "lblTaskTitle"
            Me.lblTaskTitle.Size = New Size(100, 15)
            Me.lblTaskTitle.TabIndex = 2
            Me.lblTaskTitle.Text = "Task Type / Title"
            '
            ' txtTaskTitle
            '
            Me.txtTaskTitle.Location = New Point(15, 69)
            Me.txtTaskTitle.Name = "txtTaskTitle"
            Me.txtTaskTitle.Size = New Size(305, 23)
            Me.txtTaskTitle.TabIndex = 3
            '
            ' lblAssignee
            '
            Me.lblAssignee.AutoSize = True
            Me.lblAssignee.Location = New Point(12, 96)
            Me.lblAssignee.Name = "lblAssignee"
            Me.lblAssignee.Size = New Size(104, 15)
            Me.lblAssignee.TabIndex = 4
            Me.lblAssignee.Text = "Assigned Employee"
            '
            ' cboAssignee
            '
            Me.cboAssignee.DropDownStyle = ComboBoxStyle.DropDownList
            Me.cboAssignee.FormattingEnabled = True
            Me.cboAssignee.Location = New Point(15, 112)
            Me.cboAssignee.Name = "cboAssignee"
            Me.cboAssignee.Size = New Size(305, 23)
            Me.cboAssignee.TabIndex = 5
            '
            ' lblPriority
            '
            Me.lblPriority.AutoSize = True
            Me.lblPriority.Location = New Point(12, 139)
            Me.lblPriority.Name = "lblPriority"
            Me.lblPriority.Size = New Size(45, 15)
            Me.lblPriority.TabIndex = 6
            Me.lblPriority.Text = "Priority"
            '
            ' cboPriority
            '
            Me.cboPriority.DropDownStyle = ComboBoxStyle.DropDownList
            Me.cboPriority.FormattingEnabled = True
            Me.cboPriority.Location = New Point(15, 155)
            Me.cboPriority.Name = "cboPriority"
            Me.cboPriority.Size = New Size(145, 23)
            Me.cboPriority.TabIndex = 7
            '
            ' lblStatus
            '
            Me.lblStatus.AutoSize = True
            Me.lblStatus.Location = New Point(172, 139)
            Me.lblStatus.Name = "lblStatus"
            Me.lblStatus.Size = New Size(39, 15)
            Me.lblStatus.TabIndex = 8
            Me.lblStatus.Text = "Status"
            '
            ' cboStatus
            '
            Me.cboStatus.DropDownStyle = ComboBoxStyle.DropDownList
            Me.cboStatus.FormattingEnabled = True
            Me.cboStatus.Location = New Point(175, 155)
            Me.cboStatus.Name = "cboStatus"
            Me.cboStatus.Size = New Size(145, 23)
            Me.cboStatus.TabIndex = 9
            '
            ' lblStartDate
            '
            Me.lblStartDate.AutoSize = True
            Me.lblStartDate.Location = New Point(12, 183)
            Me.lblStartDate.Name = "lblStartDate"
            Me.lblStartDate.Size = New Size(58, 15)
            Me.lblStartDate.TabIndex = 10
            Me.lblStartDate.Text = "Start Date"
            '
            ' dtpStartDate
            '
            Me.dtpStartDate.Format = DateTimePickerFormat.Short
            Me.dtpStartDate.Location = New Point(15, 199)
            Me.dtpStartDate.Name = "dtpStartDate"
            Me.dtpStartDate.Size = New Size(145, 23)
            Me.dtpStartDate.TabIndex = 11
            '
            ' lblDueDate
            '
            Me.lblDueDate.AutoSize = True
            Me.lblDueDate.Location = New Point(172, 183)
            Me.lblDueDate.Name = "lblDueDate"
            Me.lblDueDate.Size = New Size(55, 15)
            Me.lblDueDate.TabIndex = 12
            Me.lblDueDate.Text = "Due Date"
            '
            ' dtpDueDate
            '
            Me.dtpDueDate.Format = DateTimePickerFormat.Short
            Me.dtpDueDate.Location = New Point(175, 199)
            Me.dtpDueDate.Name = "dtpDueDate"
            Me.dtpDueDate.Size = New Size(145, 23)
            Me.dtpDueDate.TabIndex = 13
            '
            ' lblDesc
            '
            Me.lblDesc.AutoSize = True
            Me.lblDesc.Location = New Point(12, 226)
            Me.lblDesc.Name = "lblDesc"
            Me.lblDesc.Size = New Size(91, 15)
            Me.lblDesc.TabIndex = 14
            Me.lblDesc.Text = "Task Description"
            '
            ' txtDesc
            '
            Me.txtDesc.Location = New Point(15, 242)
            Me.txtDesc.Multiline = True
            Me.txtDesc.Name = "txtDesc"
            Me.txtDesc.Size = New Size(305, 120)
            Me.txtDesc.TabIndex = 15
            '
            ' btnSave
            '
            Me.btnSave.BackColor = Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(120, Byte), Integer), CType(CType(215, Byte), Integer))
            Me.btnSave.FlatStyle = FlatStyle.Flat
            Me.btnSave.Font = New Font("Segoe UI Semibold", 8.5!, FontStyle.Bold, GraphicsUnit.Point, CType(0, Byte))
            Me.btnSave.ForeColor = Color.White
            Me.btnSave.Location = New Point(105, 375)
            Me.btnSave.Name = "btnSave"
            Me.btnSave.Size = New Size(100, 32)
            Me.btnSave.TabIndex = 17
            Me.btnSave.Text = "Save Task"
            Me.btnSave.UseVisualStyleBackColor = False
            '
            ' btnNew
            '
            Me.btnNew.Location = New Point(15, 375)
            Me.btnNew.Name = "btnNew"
            Me.btnNew.Size = New Size(85, 32)
            Me.btnNew.TabIndex = 16
            Me.btnNew.Text = "+ Add Task"
            Me.btnNew.UseVisualStyleBackColor = True
            '
            ' btnComplete
            '
            Me.btnComplete.BackColor = Color.FromArgb(CType(CType(40, Byte), Integer), CType(CType(167, Byte), Integer), CType(CType(69, Byte), Integer))
            Me.btnComplete.FlatStyle = FlatStyle.Flat
            Me.btnComplete.Font = New Font("Segoe UI Semibold", 8.5!, FontStyle.Bold, GraphicsUnit.Point, CType(0, Byte))
            Me.btnComplete.ForeColor = Color.White
            Me.btnComplete.Location = New Point(210, 375)
            Me.btnComplete.Name = "btnComplete"
            Me.btnComplete.Size = New Size(110, 32)
            Me.btnComplete.TabIndex = 18
            Me.btnComplete.Text = "Mark Completed"
            Me.btnComplete.UseVisualStyleBackColor = False
            '
            ' btnDelete
            '
            Me.btnDelete.BackColor = Color.FromArgb(CType(CType(220, Byte), Integer), CType(CType(53, Byte), Integer), CType(CType(69, Byte), Integer))
            Me.btnDelete.FlatStyle = FlatStyle.Flat
            Me.btnDelete.Font = New Font("Segoe UI Semibold", 8.5!, FontStyle.Bold, GraphicsUnit.Point, CType(0, Byte))
            Me.btnDelete.ForeColor = Color.White
            Me.btnDelete.Location = New Point(210, 415)
            Me.btnDelete.Name = "btnDelete"
            Me.btnDelete.Size = New Size(110, 32)
            Me.btnDelete.TabIndex = 19
            Me.btnDelete.Text = "Delete Task"
            Me.btnDelete.UseVisualStyleBackColor = False
            '
            ' dgvTasks
            '
            Me.dgvTasks.AllowUserToAddRows = False
            Me.dgvTasks.AllowUserToDeleteRows = False
            Me.dgvTasks.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            Me.dgvTasks.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            Me.dgvTasks.Location = New Point(380, 110)
            Me.dgvTasks.MultiSelect = False
            Me.dgvTasks.Name = "dgvTasks"
            Me.dgvTasks.ReadOnly = True
            Me.dgvTasks.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            Me.dgvTasks.Size = New Size(584, 430)
            Me.dgvTasks.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
            Me.dgvTasks.TabIndex = 2
            '
            ' lblSearch
            '
            Me.lblSearch.AutoSize = True
            Me.lblSearch.Location = New Point(380, 78)
            Me.lblSearch.Name = "lblSearch"
            Me.lblSearch.Size = New Size(45, 15)
            Me.lblSearch.TabIndex = 4
            Me.lblSearch.Text = "Search:"
            '
            ' txtSearch
            '
            Me.txtSearch.Location = New Point(430, 75)
            Me.txtSearch.Name = "txtSearch"
            Me.txtSearch.Size = New Size(170, 23)
            Me.txtSearch.TabIndex = 3
            '
            ' lblFilter
            '
            Me.lblFilter.AutoSize = True
            Me.lblFilter.Location = New Point(610, 78)
            Me.lblFilter.Name = "lblFilter"
            Me.lblFilter.Size = New Size(42, 15)
            Me.lblFilter.TabIndex = 5
            Me.lblFilter.Text = "Filter:"
            '
            ' cboStatusFilter
            '
            Me.cboStatusFilter.DropDownStyle = ComboBoxStyle.DropDownList
            Me.cboStatusFilter.FormattingEnabled = True
            Me.cboStatusFilter.Items.AddRange(New Object() {"Pending", "In Progress", "Completed", "Overdue", "All"})
            Me.cboStatusFilter.Location = New Point(655, 75)
            Me.cboStatusFilter.Name = "cboStatusFilter"
            Me.cboStatusFilter.Size = New Size(140, 23)
            Me.cboStatusFilter.TabIndex = 6
            '
            ' btnRefresh
            '
            Me.btnRefresh.Location = New Point(884, 74)
            Me.btnRefresh.Name = "btnRefresh"
            Me.btnRefresh.Size = New Size(80, 25)
            Me.btnRefresh.Anchor = AnchorStyles.Top Or AnchorStyles.Right
            Me.btnRefresh.TabIndex = 7
            Me.btnRefresh.Text = "🔄 Refresh"
            Me.btnRefresh.UseVisualStyleBackColor = True
            '
            ' errProvider
            '
            Me.errProvider.ContainerControl = Me
            '
            ' FrmTaskManagement
            '
            Me.AutoScaleDimensions = New SizeF(7.0!, 15.0!)
            Me.AutoScaleMode = AutoScaleMode.Font
            Me.ClientSize = New Size(984, 561)
            Me.Controls.Add(Me.btnRefresh)
            Me.Controls.Add(Me.cboStatusFilter)
            Me.Controls.Add(Me.lblFilter)
            Me.Controls.Add(Me.lblSearch)
            Me.Controls.Add(Me.txtSearch)
            Me.Controls.Add(Me.dgvTasks)
            Me.Controls.Add(Me.pnlForm)
            Me.Controls.Add(Me.pnlHeader)
            Me.FormBorderStyle = FormBorderStyle.FixedSingle
            Me.MaximizeBox = False
            Me.Name = "FrmTaskManagement"
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.Text = "Daily Task Management"
            Me.pnlHeader.ResumeLayout(False)
            Me.pnlHeader.PerformLayout()
            Me.pnlForm.ResumeLayout(False)
            Me.pnlForm.PerformLayout()
            CType(Me.dgvTasks, ISupportInitialize).BeginInit()
            CType(Me.errProvider, ISupportInitialize).BeginInit()
            Me.ResumeLayout(False)
            Me.PerformLayout()
        End Sub
    End Class
End Namespace
