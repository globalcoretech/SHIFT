Imports System.ComponentModel
Imports System.Drawing
Imports System.Windows.Forms

Namespace Forms.Admin
    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
    Partial Class FrmClientManagement
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
        Private WithEvents lblGstin As Label
        Private WithEvents txtGstin As TextBox
        Private WithEvents btnFetchGst As Button
        Private WithEvents lblCode As Label
        Private WithEvents txtCode As TextBox
        Private WithEvents lblName As Label
        Private WithEvents txtName As TextBox
        Private WithEvents lblGstType As Label
        Private WithEvents cboGstType As ComboBox
        Private WithEvents lblEntityType As Label
        Private WithEvents cboEntityType As ComboBox
        Private WithEvents lblPan As Label
        Private WithEvents txtPan As TextBox
        Private WithEvents lblState As Label
        Private WithEvents cboState As ComboBox
        Private WithEvents lblContact As Label
        Private WithEvents txtContact As TextBox
        Private WithEvents lblPhone As Label
        Private WithEvents txtPhone As TextBox
        Private WithEvents lblEmail As Label
        Private WithEvents txtEmail As TextBox
        Private WithEvents lblDept As Label
        Private WithEvents cboDept As ComboBox
        Private WithEvents chkActive As CheckBox
        Private WithEvents btnSave As Button
        Private WithEvents btnNew As Button
        Private WithEvents btnArchive As Button
        Private WithEvents dgvClients As DataGridView
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
            Me.lblGstin = New Label()
            Me.txtGstin = New TextBox()
            Me.btnFetchGst = New Button()
            Me.lblName = New Label()
            Me.txtName = New TextBox()
            Me.lblCode = New Label()
            Me.txtCode = New TextBox()
            Me.lblGstType = New Label()
            Me.cboGstType = New ComboBox()
            Me.lblEntityType = New Label()
            Me.cboEntityType = New ComboBox()
            Me.lblPan = New Label()
            Me.txtPan = New TextBox()
            Me.lblState = New Label()
            Me.cboState = New ComboBox()
            Me.lblContact = New Label()
            Me.txtContact = New TextBox()
            Me.lblPhone = New Label()
            Me.txtPhone = New TextBox()
            Me.lblEmail = New Label()
            Me.txtEmail = New TextBox()
            Me.lblDept = New Label()
            Me.cboDept = New ComboBox()
            Me.chkActive = New CheckBox()
            Me.btnSave = New Button()
            Me.btnNew = New Button()
            Me.btnArchive = New Button()
            Me.dgvClients = New DataGridView()
            Me.txtSearch = New TextBox()
            Me.lblSearch = New Label()
            Me.lblFilter = New Label()
            Me.cboStatusFilter = New ComboBox()
            Me.btnRefresh = New Button()
            Me.errProvider = New ErrorProvider(Me.components)
            Me.pnlHeader.SuspendLayout()
            Me.pnlForm.SuspendLayout()
            CType(Me.dgvClients, ISupportInitialize).BeginInit()
            CType(Me.errProvider, ISupportInitialize).BeginInit()
            Me.SuspendLayout()
            '
            ' pnlHeader
            '
            Me.pnlHeader.BackColor = Color.FromArgb(30, 27, 75)
            Me.pnlHeader.Controls.Add(Me.lblSubtitle)
            Me.pnlHeader.Controls.Add(Me.lblTitle)
            Me.pnlHeader.Dock = DockStyle.Top
            Me.pnlHeader.Location = New Point(0, 0)
            Me.pnlHeader.Name = "pnlHeader"
            Me.pnlHeader.Size = New Size(984, 65)
            Me.pnlHeader.TabIndex = 0
            '
            ' lblTitle
            '
            Me.lblTitle.AutoSize = True
            Me.lblTitle.Font = New Font("Segoe UI Semibold", 13.5!, FontStyle.Bold)
            Me.lblTitle.ForeColor = Color.White
            Me.lblTitle.Location = New Point(20, 10)
            Me.lblTitle.Name = "lblTitle"
            Me.lblTitle.Size = New Size(340, 25)
            Me.lblTitle.TabIndex = 0
            Me.lblTitle.Text = "⚡ Client Master & Statutory Registry"
            '
            ' lblSubtitle
            '
            Me.lblSubtitle.AutoSize = True
            Me.lblSubtitle.Font = New Font("Segoe UI", 9.0!)
            Me.lblSubtitle.ForeColor = Color.FromArgb(199, 210, 254)
            Me.lblSubtitle.Location = New Point(20, 38)
            Me.lblSubtitle.Name = "lblSubtitle"
            Me.lblSubtitle.Size = New Size(540, 15)
            Me.lblSubtitle.TabIndex = 1
            Me.lblSubtitle.Text = "Comprehensive CA Client Onboarding • GSTIN Auto-Fetch Engine • PAN Extraction • Statutory Filing Schemes"
            '
            ' pnlForm
            '
            Me.pnlForm.BackColor = Color.White
            Me.pnlForm.BorderStyle = BorderStyle.None
            Me.pnlForm.Controls.Add(Me.btnArchive)
            Me.pnlForm.Controls.Add(Me.btnNew)
            Me.pnlForm.Controls.Add(Me.btnSave)
            Me.pnlForm.Controls.Add(Me.chkActive)
            Me.pnlForm.Controls.Add(Me.cboDept)
            Me.pnlForm.Controls.Add(Me.lblDept)
            Me.pnlForm.Controls.Add(Me.txtEmail)
            Me.pnlForm.Controls.Add(Me.lblEmail)
            Me.pnlForm.Controls.Add(Me.txtPhone)
            Me.pnlForm.Controls.Add(Me.lblPhone)
            Me.pnlForm.Controls.Add(Me.txtContact)
            Me.pnlForm.Controls.Add(Me.lblContact)
            Me.pnlForm.Controls.Add(Me.cboState)
            Me.pnlForm.Controls.Add(Me.lblState)
            Me.pnlForm.Controls.Add(Me.txtPan)
            Me.pnlForm.Controls.Add(Me.lblPan)
            Me.pnlForm.Controls.Add(Me.cboEntityType)
            Me.pnlForm.Controls.Add(Me.lblEntityType)
            Me.pnlForm.Controls.Add(Me.cboGstType)
            Me.pnlForm.Controls.Add(Me.lblGstType)
            Me.pnlForm.Controls.Add(Me.txtCode)
            Me.pnlForm.Controls.Add(Me.lblCode)
            Me.pnlForm.Controls.Add(Me.txtName)
            Me.pnlForm.Controls.Add(Me.lblName)
            Me.pnlForm.Controls.Add(Me.btnFetchGst)
            Me.pnlForm.Controls.Add(Me.txtGstin)
            Me.pnlForm.Controls.Add(Me.lblGstin)
            Me.pnlForm.Location = New Point(20, 75)
            Me.pnlForm.Name = "pnlForm"
            Me.pnlForm.Size = New Size(355, 475)
            Me.pnlForm.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left
            Me.pnlForm.TabIndex = 1
            '
            ' lblGstin
            '
            Me.lblGstin.AutoSize = True
            Me.lblGstin.Font = New Font("Segoe UI Semibold", 8.5!, FontStyle.Bold)
            Me.lblGstin.ForeColor = Color.FromArgb(0, 102, 204)
            Me.lblGstin.Location = New Point(12, 10)
            Me.lblGstin.Name = "lblGstin"
            Me.lblGstin.Size = New Size(115, 15)
            Me.lblGstin.Text = "GSTIN (15 Digits)"
            '
            ' txtGstin
            '
            Me.txtGstin.CharacterCasing = CharacterCasing.Upper
            Me.txtGstin.Location = New Point(15, 27)
            Me.txtGstin.MaxLength = 15
            Me.txtGstin.Name = "txtGstin"
            Me.txtGstin.Size = New Size(215, 23)
            Me.txtGstin.TabIndex = 0
            '
            ' btnFetchGst
            '
            Me.btnFetchGst.BackColor = Color.FromArgb(16, 185, 129)
            Me.btnFetchGst.FlatStyle = FlatStyle.Flat
            Me.btnFetchGst.Font = New Font("Segoe UI Semibold", 8.5!, FontStyle.Bold)
            Me.btnFetchGst.ForeColor = Color.White
            Me.btnFetchGst.Location = New Point(236, 26)
            Me.btnFetchGst.Name = "btnFetchGst"
            Me.btnFetchGst.Size = New Size(100, 25)
            Me.btnFetchGst.TabIndex = 1
            Me.btnFetchGst.Text = "🔍 Fetch GST"
            Me.btnFetchGst.UseVisualStyleBackColor = False
            '
            ' lblName
            '
            Me.lblName.AutoSize = True
            Me.lblName.Location = New Point(12, 56)
            Me.lblName.Name = "lblName"
            Me.lblName.Size = New Size(125, 15)
            Me.lblName.Text = "Client / Business Name"
            '
            ' txtName
            '
            Me.txtName.Location = New Point(15, 73)
            Me.txtName.Name = "txtName"
            Me.txtName.Size = New Size(215, 23)
            Me.txtName.TabIndex = 2
            '
            ' lblCode
            '
            Me.lblCode.AutoSize = True
            Me.lblCode.Location = New Point(236, 56)
            Me.lblCode.Name = "lblCode"
            Me.lblCode.Size = New Size(69, 15)
            Me.lblCode.Text = "Client Code"
            '
            ' txtCode
            '
            Me.txtCode.CharacterCasing = CharacterCasing.Upper
            Me.txtCode.Location = New Point(236, 73)
            Me.txtCode.Name = "txtCode"
            Me.txtCode.Size = New Size(100, 23)
            Me.txtCode.TabIndex = 3
            '
            ' lblGstType
            '
            Me.lblGstType.AutoSize = True
            Me.lblGstType.Location = New Point(12, 102)
            Me.lblGstType.Name = "lblGstType"
            Me.lblGstType.Size = New Size(115, 15)
            Me.lblGstType.Text = "GST Registration Type"
            '
            ' cboGstType
            '
            Me.cboGstType.DropDownStyle = ComboBoxStyle.DropDownList
            Me.cboGstType.FormattingEnabled = True
            Me.cboGstType.Items.AddRange(New Object() {"Regular Monthly", "Regular QRMP (Quarterly)", "Composition Scheme", "Unregistered / Exempt", "Consumer / Overseas"})
            Me.cboGstType.Location = New Point(15, 119)
            Me.cboGstType.Name = "cboGstType"
            Me.cboGstType.Size = New Size(155, 23)
            Me.cboGstType.TabIndex = 4
            '
            ' lblEntityType
            '
            Me.lblEntityType.AutoSize = True
            Me.lblEntityType.Location = New Point(180, 102)
            Me.lblEntityType.Name = "lblEntityType"
            Me.lblEntityType.Size = New Size(65, 15)
            Me.lblEntityType.Text = "Entity Type"
            '
            ' cboEntityType
            '
            Me.cboEntityType.DropDownStyle = ComboBoxStyle.DropDownList
            Me.cboEntityType.FormattingEnabled = True
            Me.cboEntityType.Items.AddRange(New Object() {"Proprietorship", "Partnership / LLP", "Pvt Ltd / Public Ltd", "HUF", "Trust / AOP", "Individual"})
            Me.cboEntityType.Location = New Point(180, 119)
            Me.cboEntityType.Name = "cboEntityType"
            Me.cboEntityType.Size = New Size(156, 23)
            Me.cboEntityType.TabIndex = 5
            '
            ' lblPan
            '
            Me.lblPan.AutoSize = True
            Me.lblPan.Location = New Point(12, 148)
            Me.lblPan.Name = "lblPan"
            Me.lblPan.Size = New Size(100, 15)
            Me.lblPan.Text = "PAN Card Number"
            '
            ' txtPan
            '
            Me.txtPan.CharacterCasing = CharacterCasing.Upper
            Me.txtPan.Location = New Point(15, 165)
            Me.txtPan.MaxLength = 10
            Me.txtPan.Name = "txtPan"
            Me.txtPan.Size = New Size(155, 23)
            Me.txtPan.TabIndex = 6
            '
            ' lblState
            '
            Me.lblState.AutoSize = True
            Me.lblState.Location = New Point(180, 148)
            Me.lblState.Name = "lblState"
            Me.lblState.Size = New Size(69, 15)
            Me.lblState.Text = "State Name"
            '
            ' cboState
            '
            Me.cboState.DropDownStyle = ComboBoxStyle.DropDownList
            Me.cboState.FormattingEnabled = True
            Me.cboState.Items.AddRange(New Object() {"Maharashtra", "Delhi", "Gujarat", "Uttar Pradesh", "Karnataka", "Tamil Nadu", "West Bengal", "Telangana", "Rajasthan", "Madhya Pradesh", "Punjab", "Haryana", "Bihar", "Odisha", "Kerala", "Assam", "Other"})
            Me.cboState.Location = New Point(180, 165)
            Me.cboState.Name = "cboState"
            Me.cboState.Size = New Size(156, 23)
            Me.cboState.TabIndex = 7
            '
            ' lblContact
            '
            Me.lblContact.AutoSize = True
            Me.lblContact.Location = New Point(12, 194)
            Me.lblContact.Name = "lblContact"
            Me.lblContact.Size = New Size(88, 15)
            Me.lblContact.Text = "Contact Person"
            '
            ' txtContact
            '
            Me.txtContact.Location = New Point(15, 211)
            Me.txtContact.Name = "txtContact"
            Me.txtContact.Size = New Size(155, 23)
            Me.txtContact.TabIndex = 8
            '
            ' lblPhone
            '
            Me.lblPhone.AutoSize = True
            Me.lblPhone.Location = New Point(180, 194)
            Me.lblPhone.Name = "lblPhone"
            Me.lblPhone.Size = New Size(88, 15)
            Me.lblPhone.Text = "Mobile Phone"
            '
            ' txtPhone
            '
            Me.txtPhone.Location = New Point(180, 211)
            Me.txtPhone.Name = "txtPhone"
            Me.txtPhone.Size = New Size(156, 23)
            Me.txtPhone.TabIndex = 9
            '
            ' lblEmail
            '
            Me.lblEmail.AutoSize = True
            Me.lblEmail.Location = New Point(12, 240)
            Me.lblEmail.Name = "lblEmail"
            Me.lblEmail.Size = New Size(80, 15)
            Me.lblEmail.Text = "Email Address"
            '
            ' txtEmail
            '
            Me.txtEmail.Location = New Point(15, 257)
            Me.txtEmail.Name = "txtEmail"
            Me.txtEmail.Size = New Size(155, 23)
            Me.txtEmail.TabIndex = 10
            '
            ' lblDept
            '
            Me.lblDept.AutoSize = True
            Me.lblDept.Location = New Point(180, 240)
            Me.lblDept.Name = "lblDept"
            Me.lblDept.Size = New Size(70, 15)
            Me.lblDept.Text = "Department"
            '
            ' cboDept
            '
            Me.cboDept.DropDownStyle = ComboBoxStyle.DropDownList
            Me.cboDept.FormattingEnabled = True
            Me.cboDept.Location = New Point(180, 257)
            Me.cboDept.Name = "cboDept"
            Me.cboDept.Size = New Size(156, 23)
            Me.cboDept.TabIndex = 11
            '
            ' chkActive
            '
            Me.chkActive.AutoSize = True
            Me.chkActive.Checked = True
            Me.chkActive.CheckState = CheckState.Checked
            Me.chkActive.Location = New Point(15, 292)
            Me.chkActive.Name = "chkActive"
            Me.chkActive.Size = New Size(130, 19)
            Me.chkActive.TabIndex = 12
            Me.chkActive.Text = "Active Client Profile"
            Me.chkActive.UseVisualStyleBackColor = True
            '
            ' btnSave
            '
            Me.btnSave.BackColor = Color.FromArgb(79, 70, 229)
            Me.btnSave.FlatAppearance.BorderSize = 0
            Me.btnSave.FlatStyle = FlatStyle.Flat
            Me.btnSave.Font = New Font("Segoe UI Semibold", 9.0!, FontStyle.Bold)
            Me.btnSave.ForeColor = Color.White
            Me.btnSave.Location = New Point(226, 325)
            Me.btnSave.Name = "btnSave"
            Me.btnSave.Size = New Size(110, 32)
            Me.btnSave.TabIndex = 15
            Me.btnSave.Text = "💾 Save Profile"
            Me.btnSave.UseVisualStyleBackColor = False
            '
            ' btnNew
            '
            Me.btnNew.BackColor = Color.FromArgb(16, 185, 129)
            Me.btnNew.FlatAppearance.BorderSize = 0
            Me.btnNew.FlatStyle = FlatStyle.Flat
            Me.btnNew.Font = New Font("Segoe UI Semibold", 9.0!, FontStyle.Bold)
            Me.btnNew.ForeColor = Color.White
            Me.btnNew.Location = New Point(15, 325)
            Me.btnNew.Name = "btnNew"
            Me.btnNew.Size = New Size(100, 32)
            Me.btnNew.TabIndex = 13
            Me.btnNew.Text = "➕ New"
            Me.btnNew.UseVisualStyleBackColor = False
            '
            ' btnArchive
            '
            Me.btnArchive.BackColor = Color.FromArgb(239, 68, 68)
            Me.btnArchive.FlatAppearance.BorderSize = 0
            Me.btnArchive.FlatStyle = FlatStyle.Flat
            Me.btnArchive.Font = New Font("Segoe UI Semibold", 9.0!, FontStyle.Bold)
            Me.btnArchive.ForeColor = Color.White
            Me.btnArchive.Location = New Point(122, 325)
            Me.btnArchive.Name = "btnArchive"
            Me.btnArchive.Size = New Size(98, 32)
            Me.btnArchive.TabIndex = 14
            Me.btnArchive.Text = "🗑️ Archive"
            Me.btnArchive.UseVisualStyleBackColor = False
            '
            ' dgvClients
            '
            Me.dgvClients.AllowUserToAddRows = False
            Me.dgvClients.AllowUserToDeleteRows = False
            Me.dgvClients.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
            Me.dgvClients.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            Me.dgvClients.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            Me.dgvClients.Location = New Point(390, 110)
            Me.dgvClients.MultiSelect = False
            Me.dgvClients.Name = "dgvClients"
            Me.dgvClients.ReadOnly = True
            Me.dgvClients.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            Me.dgvClients.Size = New Size(574, 430)
            Me.dgvClients.TabIndex = 2
            '
            ' lblSearch
            '
            Me.lblSearch.AutoSize = True
            Me.lblSearch.Location = New Point(390, 78)
            Me.lblSearch.Name = "lblSearch"
            Me.lblSearch.Size = New Size(45, 15)
            Me.lblSearch.TabIndex = 4
            Me.lblSearch.Text = "Search:"
            '
            ' txtSearch
            '
            Me.txtSearch.Location = New Point(440, 75)
            Me.txtSearch.Name = "txtSearch"
            Me.txtSearch.Size = New Size(170, 23)
            Me.txtSearch.TabIndex = 3
            '
            ' lblFilter
            '
            Me.lblFilter.AutoSize = True
            Me.lblFilter.Location = New Point(620, 78)
            Me.lblFilter.Name = "lblFilter"
            Me.lblFilter.Size = New Size(42, 15)
            Me.lblFilter.TabIndex = 5
            Me.lblFilter.Text = "Status:"
            '
            ' cboStatusFilter
            '
            Me.cboStatusFilter.DropDownStyle = ComboBoxStyle.DropDownList
            Me.cboStatusFilter.FormattingEnabled = True
            Me.cboStatusFilter.Items.AddRange(New Object() {"Active", "Archived / Deleted", "All"})
            Me.cboStatusFilter.Location = New Point(665, 75)
            Me.cboStatusFilter.Name = "cboStatusFilter"
            Me.cboStatusFilter.Size = New Size(140, 23)
            Me.cboStatusFilter.TabIndex = 6
            '
            ' btnRefresh
            '
            Me.btnRefresh.Anchor = AnchorStyles.Top Or AnchorStyles.Right
            Me.btnRefresh.Location = New Point(884, 74)
            Me.btnRefresh.Name = "btnRefresh"
            Me.btnRefresh.Size = New Size(80, 25)
            Me.btnRefresh.TabIndex = 7
            Me.btnRefresh.Text = "🔄 Refresh"
            Me.btnRefresh.UseVisualStyleBackColor = True
            '
            ' errProvider
            '
            Me.errProvider.ContainerControl = Me
            '
            ' FrmClientManagement
            '
            Me.AutoScaleDimensions = New SizeF(7.0!, 15.0!)
            Me.AutoScaleMode = AutoScaleMode.Font
            Me.ClientSize = New Size(984, 561)
            Me.Controls.Add(Me.btnRefresh)
            Me.Controls.Add(Me.cboStatusFilter)
            Me.Controls.Add(Me.lblFilter)
            Me.Controls.Add(Me.lblSearch)
            Me.Controls.Add(Me.txtSearch)
            Me.Controls.Add(Me.dgvClients)
            Me.Controls.Add(Me.pnlForm)
            Me.Controls.Add(Me.pnlHeader)
            Me.FormBorderStyle = FormBorderStyle.FixedSingle
            Me.MaximizeBox = False
            Me.Name = "FrmClientManagement"
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.Text = "Client Registry Master"
            Me.pnlHeader.ResumeLayout(False)
            Me.pnlHeader.PerformLayout()
            Me.pnlForm.ResumeLayout(False)
            Me.pnlForm.PerformLayout()
            CType(Me.dgvClients, ISupportInitialize).BeginInit()
            CType(Me.errProvider, ISupportInitialize).BeginInit()
            Me.ResumeLayout(False)
            Me.PerformLayout()
        End Sub
    End Class
End Namespace
