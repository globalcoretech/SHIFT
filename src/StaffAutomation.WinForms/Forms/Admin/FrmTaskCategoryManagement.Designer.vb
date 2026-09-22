Imports System.Drawing
Imports System.Windows.Forms

Namespace Forms.Admin
    Partial Class FrmTaskCategoryManagement
        Inherits Form

        Private components As System.ComponentModel.IContainer
        Friend WithEvents pnlTop As Panel
        Friend WithEvents lblTitle As Label
        Friend WithEvents dgvCategories As DataGridView
        Friend WithEvents pnlBottom As Panel
        Friend WithEvents lblCategoryName As Label
        Friend WithEvents txtCategoryName As TextBox
        Friend WithEvents lblCategoryCode As Label
        Friend WithEvents txtCategoryCode As TextBox
        Friend WithEvents chkIsActive As CheckBox
        Friend WithEvents btnSave As Button
        Friend WithEvents btnClear As Button

        <System.Diagnostics.DebuggerNonUserCode()>
        Protected Overrides Sub Dispose(ByVal disposing As Boolean)
            Try
                If disposing AndAlso components IsNot Nothing Then
                    components.Dispose()
                End If
            Finally
                MyBase.Dispose(disposing)
            End Try
        End Sub

        <System.Diagnostics.DebuggerStepThrough()>
        Private Sub InitializeComponent()
            Me.pnlTop = New Panel()
            Me.lblTitle = New Label()
            Me.dgvCategories = New DataGridView()
            Me.pnlBottom = New Panel()
            Me.lblCategoryName = New Label()
            Me.txtCategoryName = New TextBox()
            Me.lblCategoryCode = New Label()
            Me.txtCategoryCode = New TextBox()
            Me.chkIsActive = New CheckBox()
            Me.btnSave = New Button()
            Me.btnClear = New Button()
            
            Me.pnlTop.SuspendLayout()
            CType(Me.dgvCategories, System.ComponentModel.ISupportInitialize).BeginInit()
            Me.pnlBottom.SuspendLayout()
            Me.SuspendLayout()
            
            ' pnlTop
            Me.pnlTop.Controls.Add(Me.lblTitle)
            Me.pnlTop.Dock = DockStyle.Top
            Me.pnlTop.Height = 50
            Me.pnlTop.BackColor = Color.FromArgb(45, 52, 54)
            
            ' lblTitle
            Me.lblTitle.AutoSize = True
            Me.lblTitle.Font = New Font("Segoe UI", 14.0!, FontStyle.Bold)
            Me.lblTitle.ForeColor = Color.White
            Me.lblTitle.Location = New Point(20, 10)
            Me.lblTitle.Text = "Manage Task Categories"
            
            ' pnlBottom
            Me.pnlBottom.Controls.Add(Me.lblCategoryName)
            Me.pnlBottom.Controls.Add(Me.txtCategoryName)
            Me.pnlBottom.Controls.Add(Me.lblCategoryCode)
            Me.pnlBottom.Controls.Add(Me.txtCategoryCode)
            Me.pnlBottom.Controls.Add(Me.chkIsActive)
            Me.pnlBottom.Controls.Add(Me.btnSave)
            Me.pnlBottom.Controls.Add(Me.btnClear)
            Me.pnlBottom.Dock = DockStyle.Bottom
            Me.pnlBottom.Height = 100
            
            ' lblCategoryName
            Me.lblCategoryName.AutoSize = True
            Me.lblCategoryName.Location = New Point(20, 20)
            Me.lblCategoryName.Text = "Category Name:"
            
            ' txtCategoryName
            Me.txtCategoryName.Location = New Point(120, 17)
            Me.txtCategoryName.Width = 200
            
            ' lblCategoryCode
            Me.lblCategoryCode.AutoSize = True
            Me.lblCategoryCode.Location = New Point(20, 50)
            Me.lblCategoryCode.Text = "Category Code:"
            
            ' txtCategoryCode
            Me.txtCategoryCode.Location = New Point(120, 47)
            Me.txtCategoryCode.Width = 100
            
            ' chkIsActive
            Me.chkIsActive.AutoSize = True
            Me.chkIsActive.Location = New Point(250, 49)
            Me.chkIsActive.Text = "Is Active"
            Me.chkIsActive.Checked = True
            
            ' btnSave
            Me.btnSave.Location = New Point(350, 15)
            Me.btnSave.Size = New Size(90, 35)
            Me.btnSave.Text = "Save"
            Me.btnSave.BackColor = Color.FromArgb(0, 122, 204)
            Me.btnSave.ForeColor = Color.White
            Me.btnSave.FlatStyle = FlatStyle.Flat
            
            ' btnClear
            Me.btnClear.Location = New Point(450, 15)
            Me.btnClear.Size = New Size(90, 35)
            Me.btnClear.Text = "Clear"
            Me.btnClear.FlatStyle = FlatStyle.Flat
            
            ' dgvCategories
            Me.dgvCategories.Dock = DockStyle.Fill
            Me.dgvCategories.AllowUserToAddRows = False
            Me.dgvCategories.AllowUserToDeleteRows = False
            Me.dgvCategories.ReadOnly = True
            Me.dgvCategories.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            Me.dgvCategories.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            Me.dgvCategories.BackgroundColor = Color.White
            
            ' FrmTaskCategoryManagement
            Me.ClientSize = New Size(600, 450)
            Me.Controls.Add(Me.dgvCategories)
            Me.Controls.Add(Me.pnlBottom)
            Me.Controls.Add(Me.pnlTop)
            Me.Name = "FrmTaskCategoryManagement"
            Me.Text = "Task Categories"
            Me.StartPosition = FormStartPosition.CenterParent
            
            Me.pnlTop.ResumeLayout(False)
            Me.pnlTop.PerformLayout()
            CType(Me.dgvCategories, System.ComponentModel.ISupportInitialize).EndInit()
            Me.pnlBottom.ResumeLayout(False)
            Me.pnlBottom.PerformLayout()
            Me.ResumeLayout(False)
        End Sub
    End Class
End Namespace
