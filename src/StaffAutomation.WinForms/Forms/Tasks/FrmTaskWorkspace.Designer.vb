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

        Private WithEvents pnlTaskMetadataCard As GroupBox
        Private WithEvents lblTaskHeaderTitle As Label
        Private WithEvents lblClientName As Label
        Private WithEvents lblDepartment As Label
        Private WithEvents lblDueDate As Label
        Private WithEvents lblCategoryCode As Label
        Private WithEvents lblWorkflowName As Label

        Private WithEvents pnlWorkflowCard As GroupBox
        Private WithEvents lblWorkflowTitle As Label
        Private WithEvents pnlWorkflowStepsHost As FlowLayoutPanel

        <System.Diagnostics.DebuggerStepThrough()>
        Private Sub InitializeComponent()
            Me.pnlHeader = New Panel()
            Me.lblTitle = New Label()
            Me.btnBackToTasks = New Button()
            Me.pnlTaskMetadataCard = New GroupBox()
            Me.lblTaskHeaderTitle = New Label()
            Me.lblClientName = New Label()
            Me.lblDepartment = New Label()
            Me.lblDueDate = New Label()
            Me.lblCategoryCode = New Label()
            Me.lblWorkflowName = New Label()
            Me.pnlWorkflowCard = New GroupBox()
            Me.lblWorkflowTitle = New Label()
            Me.pnlWorkflowStepsHost = New FlowLayoutPanel()
            Me.pnlHeader.SuspendLayout()
            Me.pnlTaskMetadataCard.SuspendLayout()
            Me.pnlWorkflowCard.SuspendLayout()
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
            Me.btnBackToTasks.Location = New Point(770, 14)
            Me.btnBackToTasks.Name = "btnBackToTasks"
            Me.btnBackToTasks.Size = New Size(194, 32)
            Me.btnBackToTasks.TabIndex = 1
            Me.btnBackToTasks.Text = "← Close / Back to Daily Tasks"
            Me.btnBackToTasks.UseVisualStyleBackColor = False
            '
            ' lblTitle
            '
            Me.lblTitle.AutoSize = True
            Me.lblTitle.Font = New Font("Segoe UI Semibold", 14.0!, FontStyle.Bold)
            Me.lblTitle.ForeColor = Color.White
            Me.lblTitle.Location = New Point(20, 15)
            Me.lblTitle.Name = "lblTitle"
            Me.lblTitle.Size = New Size(254, 25)
            Me.lblTitle.TabIndex = 0
            Me.lblTitle.Text = "Admin Task Workflow Viewer"
            '
            ' pnlTaskMetadataCard
            '
            Me.pnlTaskMetadataCard.Controls.Add(Me.lblWorkflowName)
            Me.pnlTaskMetadataCard.Controls.Add(Me.lblCategoryCode)
            Me.pnlTaskMetadataCard.Controls.Add(Me.lblDueDate)
            Me.pnlTaskMetadataCard.Controls.Add(Me.lblDepartment)
            Me.pnlTaskMetadataCard.Controls.Add(Me.lblClientName)
            Me.pnlTaskMetadataCard.Controls.Add(Me.lblTaskHeaderTitle)
            Me.pnlTaskMetadataCard.Font = New Font("Segoe UI Semibold", 9.0!, FontStyle.Bold)
            Me.pnlTaskMetadataCard.ForeColor = Color.FromArgb(CType(CType(30, Byte), Integer), CType(CType(41, Byte), Integer), CType(CType(59, Byte), Integer))
            Me.pnlTaskMetadataCard.Location = New Point(20, 72)
            Me.pnlTaskMetadataCard.Name = "pnlTaskMetadataCard"
            Me.pnlTaskMetadataCard.Size = New Size(944, 115)
            Me.pnlTaskMetadataCard.TabIndex = 1
            Me.pnlTaskMetadataCard.TabStop = False
            Me.pnlTaskMetadataCard.Text = "📌 Task Context & Details"
            '
            ' lblTaskHeaderTitle
            '
            Me.lblTaskHeaderTitle.AutoSize = True
            Me.lblTaskHeaderTitle.Font = New Font("Segoe UI", 11.5!, FontStyle.Bold)
            Me.lblTaskHeaderTitle.ForeColor = Color.FromArgb(CType(CType(15, Byte), Integer), CType(CType(23, Byte), Integer), CType(CType(42, Byte), Integer))
            Me.lblTaskHeaderTitle.Location = New Point(15, 22)
            Me.lblTaskHeaderTitle.Name = "lblTaskHeaderTitle"
            Me.lblTaskHeaderTitle.Size = New Size(220, 21)
            Me.lblTaskHeaderTitle.TabIndex = 0
            Me.lblTaskHeaderTitle.Text = "Loading Task Context..."
            '
            ' lblClientName
            '
            Me.lblClientName.AutoSize = True
            Me.lblClientName.Font = New Font("Segoe UI", 9.0!)
            Me.lblClientName.Location = New Point(15, 52)
            Me.lblClientName.Name = "lblClientName"
            Me.lblClientName.Size = New Size(80, 15)
            Me.lblClientName.TabIndex = 1
            Me.lblClientName.Text = "Client: --"
            '
            ' lblDepartment
            '
            Me.lblDepartment.AutoSize = True
            Me.lblDepartment.Font = New Font("Segoe UI", 9.0!)
            Me.lblDepartment.Location = New Point(380, 52)
            Me.lblDepartment.Name = "lblDepartment"
            Me.lblDepartment.Size = New Size(110, 15)
            Me.lblDepartment.TabIndex = 2
            Me.lblDepartment.Text = "Department: --"
            '
            ' lblDueDate
            '
            Me.lblDueDate.AutoSize = True
            Me.lblDueDate.Font = New Font("Segoe UI", 9.0!)
            Me.lblDueDate.Location = New Point(680, 52)
            Me.lblDueDate.Name = "lblDueDate"
            Me.lblDueDate.Size = New Size(95, 15)
            Me.lblDueDate.TabIndex = 3
            Me.lblDueDate.Text = "Due Date: --"
            '
            ' lblCategoryCode
            '
            Me.lblCategoryCode.AutoSize = True
            Me.lblCategoryCode.Font = New Font("Segoe UI", 9.0!)
            Me.lblCategoryCode.Location = New Point(15, 80)
            Me.lblCategoryCode.Name = "lblCategoryCode"
            Me.lblCategoryCode.Size = New Size(110, 15)
            Me.lblCategoryCode.TabIndex = 4
            Me.lblCategoryCode.Text = "Category Code: --"
            '
            ' lblWorkflowName
            '
            Me.lblWorkflowName.AutoSize = True
            Me.lblWorkflowName.Font = New Font("Segoe UI Semibold", 9.0!, FontStyle.Bold)
            Me.lblWorkflowName.ForeColor = Color.FromArgb(CType(CType(2, Byte), Integer), CType(CType(132, Byte), Integer), CType(CType(199, Byte), Integer))
            Me.lblWorkflowName.Location = New Point(380, 80)
            Me.lblWorkflowName.Name = "lblWorkflowName"
            Me.lblWorkflowName.Size = New Size(130, 15)
            Me.lblWorkflowName.TabIndex = 5
            Me.lblWorkflowName.Text = "Resolved Workflow: --"
            '
            ' pnlWorkflowCard
            '
            Me.pnlWorkflowCard.Controls.Add(Me.pnlWorkflowStepsHost)
            Me.pnlWorkflowCard.Controls.Add(Me.lblWorkflowTitle)
            Me.pnlWorkflowCard.Font = New Font("Segoe UI Semibold", 9.0!, FontStyle.Bold)
            Me.pnlWorkflowCard.ForeColor = Color.FromArgb(CType(CType(30, Byte), Integer), CType(CType(41, Byte), Integer), CType(CType(59, Byte), Integer))
            Me.pnlWorkflowCard.Location = New Point(20, 198)
            Me.pnlWorkflowCard.Name = "pnlWorkflowCard"
            Me.pnlWorkflowCard.Size = New Size(944, 275)
            Me.pnlWorkflowCard.TabIndex = 2
            Me.pnlWorkflowCard.TabStop = False
            Me.pnlWorkflowCard.Text = "📋 Standard Ordered Workflow Steps (Read-Only Viewer)"
            '
            ' lblWorkflowTitle
            '
            Me.lblWorkflowTitle.AutoSize = True
            Me.lblWorkflowTitle.Font = New Font("Segoe UI", 8.5!, FontStyle.Italic)
            Me.lblWorkflowTitle.ForeColor = Color.FromArgb(CType(CType(100, Byte), Integer), CType(CType(116, Byte), Integer), CType(CType(139, Byte), Integer))
            Me.lblWorkflowTitle.Location = New Point(15, 22)
            Me.lblWorkflowTitle.Name = "lblWorkflowTitle"
            Me.lblWorkflowTitle.Size = New Size(260, 15)
            Me.lblWorkflowTitle.TabIndex = 0
            Me.lblWorkflowTitle.Text = "Resolving canonical workflow steps..."
            '
            ' pnlWorkflowStepsHost
            '
            Me.pnlWorkflowStepsHost.AutoScroll = True
            Me.pnlWorkflowStepsHost.FlowDirection = FlowDirection.TopDown
            Me.pnlWorkflowStepsHost.WrapContents = False
            Me.pnlWorkflowStepsHost.Location = New Point(15, 45)
            Me.pnlWorkflowStepsHost.Name = "pnlWorkflowStepsHost"
            Me.pnlWorkflowStepsHost.Size = New Size(914, 215)
            Me.pnlWorkflowStepsHost.TabIndex = 1
            '
            ' FrmTaskWorkspace
            '
            Me.AutoScaleDimensions = New SizeF(7.0!, 15.0!)
            Me.AutoScaleMode = AutoScaleMode.Font
            Me.ClientSize = New Size(984, 490)
            Me.Controls.Add(Me.pnlWorkflowCard)
            Me.Controls.Add(Me.pnlTaskMetadataCard)
            Me.Controls.Add(Me.pnlHeader)
            Me.FormBorderStyle = FormBorderStyle.FixedSingle
            Me.MaximizeBox = False
            Me.Name = "FrmTaskWorkspace"
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.Text = "Admin Task Workflow Viewer"
            Me.pnlHeader.ResumeLayout(False)
            Me.pnlHeader.PerformLayout()
            Me.pnlTaskMetadataCard.ResumeLayout(False)
            Me.pnlTaskMetadataCard.PerformLayout()
            Me.pnlWorkflowCard.ResumeLayout(False)
            Me.pnlWorkflowCard.PerformLayout()
            Me.ResumeLayout(False)
        End Sub
    End Class
End Namespace
