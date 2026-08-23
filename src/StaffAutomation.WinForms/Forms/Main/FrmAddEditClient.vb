Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Linq
Imports System.Text.RegularExpressions
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports StaffAutomation.BLL.Logging
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Logging

Namespace Forms.Main
    ''' <summary>
    ''' Custom Panel that overrides ScrollToControl to prevent WinForms AutoScroll
    ''' from auto-scrolling down when child textboxes gain focus.
    ''' </summary>
    Public Class CustomMainPanel
        Inherits Panel

        Public Sub New()
            Me.AutoScroll = False
            Me.DoubleBuffered = True
        End Sub

        Protected Overrides Function ScrollToControl(activeControl As Control) As Point
            Return Point.Empty
        End Function
    End Class

    ''' <summary>
    ''' Behavior-First / Psychological UX Modal Dialog for Adding and Editing Clients.
    ''' Mental Model: ① VERIFY GSTIN → ② REVIEW DETAILS → ③ BUSINESS LOCATION → ④ CONTACT AND ASSIGNMENT → SAVE CLIENT.
    ''' Organizes Steps 1 to 4 in explicit linear panel containers to guarantee zero overlap or clipping.
    ''' </summary>
    Public Class FrmAddEditClient
        Inherits Form

        Private ReadOnly _clientService As IClientService
        Private ReadOnly _appLogger As IAppLogger

        Private _clientId As Integer = 0
        Private _clientDto As ClientDto = Nothing
        Private _userModifiedCodeManually As Boolean = False
        Private _isLiveApiVerified As Boolean = False
        Private _hasUnsavedChanges As Boolean = False
        Private _isInitializing As Boolean = True

        ' UI Containers
        Private pnlHeader As Panel
        Private lblTitle As Label
        Private lblSubtitle As Label

        Private pnlMainBody As CustomMainPanel

        ' Step Containers inside pnlMainBody
        Private pnlGstHero As Panel
        Private pnlStep2Container As Panel
        Private pnlStep3Container As Panel
        Private pnlStep4Container As Panel

        ' Step 1 Controls
        Private lblGstHeroTitle As Label
        Private lblGstHelperText As Label
        Private txtGstin As TextBox
        Private btnFetchGst As Forms.Common.ModernButton
        Private btnOpenGovtGstSearch As Forms.Common.ModernButton
        Private pnlVerifiedSummary As Panel
        Private lblVerifiedSummaryText As Label

        ' Field Level Verification Badges
        Private lblBadgeName As Label
        Private lblBadgePan As Label
        Private lblBadgeEntity As Label
        Private lblBadgeState As Label

        ' Step 2 Controls
        Private lblSec1Header As Label
        Private txtName As TextBox
        Private txtCode As TextBox
        Private cboGstType As ComboBox
        Private lblEntityTag As Label
        Private cboEntityType As ComboBox
        Private txtPan As TextBox

        ' Step 3 Controls
        Private lblSec2Header As Label
        Private cboState As ComboBox
        Private txtDistrict As TextBox
        Private txtPincode As TextBox
        Private txtAddress As TextBox

        ' Step 4 Controls
        Private lblSec3Header As Label
        Private txtContact As TextBox
        Private txtPhone As TextBox
        Private txtEmail As TextBox
        Private cboDept As ComboBox
        Private chkActive As CheckBox

        ' Sticky Footer Action Bar
        Private pnlFooter As Panel
        Private lblFooterStatus As Label
        Private btnSaveClient As Forms.Common.ModernButton
        Private btnArchiveClient As Forms.Common.ModernButton
        Private btnCancel As Forms.Common.ModernButton

        Public Sub New(clientService As IClientService, appLogger As IAppLogger, Optional clientToEdit As ClientDto = Nothing)
            _clientService = clientService
            _appLogger = appLogger
            _clientDto = clientToEdit
            If _clientDto IsNot Nothing Then _clientId = _clientDto.ClientId

            InitializeComponent()
            PopulateFormValues()
            _isInitializing = False
            _hasUnsavedChanges = False
        End Sub

        Private Sub InitializeComponent()
            Me.SuspendLayout()

            Me.Text = If(_clientId = 0, "Add New Client", $"Edit Client: {_clientDto?.ClientName}")
            Me.Size = New Size(860, 700)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.BackColor = ThemeConstants.WorkspaceBackground

            ' =================================================================
            ' 1. Form Header Banner (Dark Slate #0F172A, Height = 48, Dock = Top)
            ' =================================================================
            pnlHeader = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 48,
                .BackColor = Color.FromArgb(15, 23, 42),
                .Padding = New Padding(16, 6, 16, 6)
            }

            lblTitle = New Label() With {
                .Text = If(_clientId = 0, "⚡ Add New Client", $"✏️ Edit Client: {_clientDto?.ClientName}"),
                .Font = New Font(ThemeConstants.FontNameDefault, 11.0!, FontStyle.Bold),
                .ForeColor = Color.White,
                .Location = New Point(16, 4),
                .AutoSize = True
            }

            lblSubtitle = New Label() With {
                .Text = "Verify GST details and create the client master record.",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.25!, FontStyle.Regular),
                .ForeColor = Color.FromArgb(148, 163, 184),
                .Location = New Point(16, 26),
                .AutoSize = True
            }

            pnlHeader.Controls.Add(lblSubtitle)
            pnlHeader.Controls.Add(lblTitle)

            ' =================================================================
            ' 2. Sticky Footer Action Bar (Dark Slate #0F172A, Height = 64, Dock = Bottom)
            ' =================================================================
            pnlFooter = New Panel() With {
                .Dock = DockStyle.Bottom,
                .Height = 64,
                .BackColor = Color.FromArgb(15, 23, 42),
                .Padding = New Padding(16, 12, 16, 12)
            }

            lblFooterStatus = New Label() With {
                .Text = "○ GST not verified    ○ Required details pending",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(148, 163, 184),
                .Location = New Point(16, 22),
                .AutoSize = True
            }

            btnArchiveClient = New Forms.Common.ModernButton() With {
                .Text = " 🗑️ Archive",
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Danger,
                .Size = New Size(110, 38),
                .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right,
                .Visible = (_clientId <> 0)
            }
            AddHandler btnArchiveClient.Click, AddressOf btnArchiveClient_Click

            btnCancel = New Forms.Common.ModernButton() With {
                .Text = "Cancel",
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Secondary,
                .Size = New Size(100, 38),
                .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
            }
            AddHandler btnCancel.Click, AddressOf btnCancel_Click

            ' Visually Dominant Emerald Save Action Button
            btnSaveClient = New Forms.Common.ModernButton() With {
                .Text = " 💾 SAVE CLIENT",
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Secondary,
                .Size = New Size(164, 38),
                .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right,
                .TabIndex = 16
            }
            AddHandler btnSaveClient.Click, AddressOf btnSaveClient_Click

            pnlFooter.Controls.Add(btnSaveClient)
            pnlFooter.Controls.Add(btnCancel)
            pnlFooter.Controls.Add(btnArchiveClient)
            pnlFooter.Controls.Add(lblFooterStatus)

            AddHandler pnlFooter.Resize, Sub(s, e) PositionFooterControls()
            AddHandler Me.Shown, AddressOf FrmAddEditClient_Shown

            ' =================================================================
            ' 3. Main Form Body Panel (Dock = Fill, Fills Space Between Header & Footer)
            ' =================================================================
            pnlMainBody = New CustomMainPanel() With {
                .Dock = DockStyle.Fill,
                .BackColor = Color.White,
                .Padding = New Padding(16, 24, 16, 16)
            }

            ' -----------------------------------------------------------------
            ' STEP 1: ① VERIFY GSTIN (Hero Section Panel - High Focus Container)
            ' -----------------------------------------------------------------
            pnlGstHero = New Panel() With {
                .Location = New Point(16, 50),
                .Size = New Size(812, 86),
                .BackColor = Color.FromArgb(238, 242, 255), ' Soft Indigo #EEF2FF
                .BorderStyle = BorderStyle.FixedSingle,
                .Padding = New Padding(10)
            }

            lblGstHeroTitle = New Label() With {
                .Text = "① VERIFY GSTIN",
                .Font = New Font(ThemeConstants.FontNameDefault, 10.5!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(67, 56, 202), ' Bold Indigo #4338CA
                .Location = New Point(12, 10),
                .AutoSize = True
            }

            lblGstHelperText = New Label() With {
                .Text = "Enter the 15-digit GSTIN to automatically verify and fill client details.",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Regular),
                .ForeColor = Color.FromArgb(79, 70, 229),
                .Location = New Point(150, 12),
                .AutoSize = True
            }

            txtGstin = New TextBox() With {
                .CharacterCasing = CharacterCasing.Upper,
                .MaxLength = 15,
                .Font = New Font(ThemeConstants.FontNameDefault, 11.0!, FontStyle.Bold),
                .Location = New Point(12, 38),
                .Size = New Size(385, 27),
                .TabIndex = 1
            }
            AddHandler txtGstin.TextChanged, AddressOf txtGstin_TextChanged
            AddHandler txtGstin.TextChanged, AddressOf OnFieldChanged

            btnFetchGst = New Forms.Common.ModernButton() With {
                .Text = " ⚡ FETCH GST DETAILS",
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Success,
                .Size = New Size(205, 36),
                .Location = New Point(405, 34),
                .TabIndex = 2
            }
            AddHandler btnFetchGst.Click, AddressOf btnFetchGst_Click

            btnOpenGovtGstSearch = New Forms.Common.ModernButton() With {
                .Text = " Open GST Portal ↗",
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Secondary,
                .Size = New Size(160, 36),
                .Location = New Point(620, 34),
                .TabIndex = 3
            }
            AddHandler btnOpenGovtGstSearch.Click, AddressOf btnOpenGovtGstSearch_Click

            ' Compact Inline Verification Summary Box
            pnlVerifiedSummary = New Panel() With {
                .Location = New Point(12, 76),
                .Size = New Size(782, 28),
                .BackColor = Color.FromArgb(209, 250, 229), ' Soft Emerald
                .Visible = False
            }
            lblVerifiedSummaryText = New Label() With {
                .Text = "✓ GST VERIFIED: Taxpayer active and registered details auto-filled below.",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(6, 95, 70),
                .Location = New Point(8, 6),
                .AutoSize = True
            }
            pnlVerifiedSummary.Controls.Add(lblVerifiedSummaryText)

            pnlGstHero.Controls.Add(pnlVerifiedSummary)
            pnlGstHero.Controls.Add(btnOpenGovtGstSearch)
            pnlGstHero.Controls.Add(btnFetchGst)
            pnlGstHero.Controls.Add(txtGstin)
            pnlGstHero.Controls.Add(lblGstHelperText)
            pnlGstHero.Controls.Add(lblGstHeroTitle)

            ' -----------------------------------------------------------------
            ' STEP 2 CONTAINER: ② REVIEW CLIENT DETAILS
            ' -----------------------------------------------------------------
            pnlStep2Container = New Panel() With {
                .Location = New Point(16, 144),
                .Size = New Size(812, 116),
                .BackColor = Color.White
            }

            lblSec1Header = New Label() With {
                .Text = "② REVIEW CLIENT DETAILS",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(100, 116, 139), ' Quiet Muted Slate in fresh state
                .Location = New Point(0, 4),
                .AutoSize = True
            }

            ' Business Name & Client Code
            Dim lblNameTag As New Label() With {.Text = "Business / Trade Name *", .Font = New Font(ThemeConstants.FontNameDefault, 8.25!, FontStyle.Bold), .Location = New Point(0, 24), .AutoSize = True}
            lblBadgeName = New Label() With {.Text = "✓ Verified", .Font = New Font(ThemeConstants.FontNameDefault, 7.5!, FontStyle.Bold), .ForeColor = Color.FromArgb(16, 185, 129), .Location = New Point(155, 24), .AutoSize = True, .Visible = False}
            txtName = New TextBox() With {.Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold), .Location = New Point(0, 40), .Size = New Size(490, 23), .TabIndex = 4}
            AddHandler txtName.TextChanged, AddressOf txtName_TextChanged
            AddHandler txtName.TextChanged, AddressOf OnFieldChanged

            Dim lblCodeTag As New Label() With {.Text = "Client Code *", .Font = New Font(ThemeConstants.FontNameDefault, 8.25!, FontStyle.Bold), .Location = New Point(504, 24), .AutoSize = True}
            txtCode = New TextBox() With {.CharacterCasing = CharacterCasing.Upper, .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold), .Location = New Point(504, 40), .Size = New Size(296, 23), .TabIndex = 5}
            AddHandler txtCode.KeyPress, Sub(s, e) _userModifiedCodeManually = True
            AddHandler txtCode.TextChanged, AddressOf OnFieldChanged

            ' PAN Number, Entity Type & GST Scheme
            Dim lblPanTag As New Label() With {.Text = "PAN Card Number (10 Chars) *", .Font = New Font(ThemeConstants.FontNameDefault, 8.25!, FontStyle.Bold), .Location = New Point(0, 68), .AutoSize = True}
            lblBadgePan = New Label() With {.Text = "✓ Verified", .Font = New Font(ThemeConstants.FontNameDefault, 7.5!, FontStyle.Bold), .ForeColor = Color.FromArgb(16, 185, 129), .Location = New Point(185, 68), .AutoSize = True, .Visible = False}
            txtPan = New TextBox() With {.CharacterCasing = CharacterCasing.Upper, .MaxLength = 10, .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold), .Location = New Point(0, 84), .Size = New Size(230, 23), .TabIndex = 6}
            AddHandler txtPan.TextChanged, AddressOf OnFieldChanged

            lblEntityTag = New Label() With {.Text = "Constitution of Business *", .Font = New Font(ThemeConstants.FontNameDefault, 8.25!, FontStyle.Bold), .Location = New Point(244, 68), .AutoSize = True}
            lblBadgeEntity = New Label() With {.Text = "✓ Verified", .Font = New Font(ThemeConstants.FontNameDefault, 7.5!, FontStyle.Bold), .ForeColor = Color.FromArgb(16, 185, 129), .Location = New Point(404, 68), .AutoSize = True, .Visible = False}
            cboEntityType = New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList, .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Regular), .Location = New Point(244, 84), .Size = New Size(246, 23), .TabIndex = 7}
            cboEntityType.Items.AddRange(New Object() {
                "-- Select / Awaiting GST Verification --",
                "Proprietorship",
                "Partnership / LLP",
                "Pvt Ltd / Public Ltd",
                "HUF",
                "Trust",
                "Individual"
            })
            cboEntityType.SelectedIndex = 0
            AddHandler cboEntityType.SelectedIndexChanged, AddressOf OnFieldChanged

            Dim lblGstTypeTag As New Label() With {.Text = "GST Registration Scheme", .Font = New Font(ThemeConstants.FontNameDefault, 8.25!, FontStyle.Regular), .Location = New Point(504, 68), .AutoSize = True}
            cboGstType = New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList, .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Regular), .Location = New Point(504, 84), .Size = New Size(296, 23), .TabIndex = 8}
            cboGstType.Items.AddRange(New Object() {
                "-- Select / Awaiting GST Verification --",
                "Regular Monthly",
                "Regular QRMP (Quarterly)",
                "Composition Scheme",
                "Unregistered / Exempt"
            })
            cboGstType.SelectedIndex = 0
            AddHandler cboGstType.SelectedIndexChanged, AddressOf OnFieldChanged

            pnlStep2Container.Controls.Add(cboGstType)
            pnlStep2Container.Controls.Add(lblGstTypeTag)
            pnlStep2Container.Controls.Add(lblBadgeEntity)
            pnlStep2Container.Controls.Add(cboEntityType)
            pnlStep2Container.Controls.Add(lblEntityTag)
            pnlStep2Container.Controls.Add(lblBadgePan)
            pnlStep2Container.Controls.Add(txtPan)
            pnlStep2Container.Controls.Add(lblPanTag)
            pnlStep2Container.Controls.Add(txtCode)
            pnlStep2Container.Controls.Add(lblCodeTag)
            pnlStep2Container.Controls.Add(lblBadgeName)
            pnlStep2Container.Controls.Add(txtName)
            pnlStep2Container.Controls.Add(lblNameTag)
            pnlStep2Container.Controls.Add(lblSec1Header)

            ' -----------------------------------------------------------------
            ' STEP 3 CONTAINER: ③ BUSINESS LOCATION (14px Breathing Space)
            ' -----------------------------------------------------------------
            pnlStep3Container = New Panel() With {
                .Location = New Point(16, 274),
                .Size = New Size(812, 118),
                .BackColor = Color.White
            }

            lblSec2Header = New Label() With {
                .Text = "③ BUSINESS LOCATION",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(71, 85, 105),
                .Location = New Point(0, 4),
                .AutoSize = True
            }

            Dim lblStateTag As New Label() With {.Text = "State / UT *", .Font = New Font(ThemeConstants.FontNameDefault, 8.25!, FontStyle.Bold), .Location = New Point(0, 24), .AutoSize = True}
            lblBadgeState = New Label() With {.Text = "✓ Verified", .Font = New Font(ThemeConstants.FontNameDefault, 7.5!, FontStyle.Bold), .ForeColor = Color.FromArgb(16, 185, 129), .Location = New Point(85, 24), .AutoSize = True, .Visible = False}
            cboState = New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList, .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Regular), .Location = New Point(0, 40), .Size = New Size(280, 23), .TabIndex = 9}
            PopulateAllIndianStates(cboState)
            AddHandler cboState.SelectedIndexChanged, AddressOf OnFieldChanged

            Dim lblDistrictTag As New Label() With {.Text = "District Name", .Font = New Font(ThemeConstants.FontNameDefault, 8.25!, FontStyle.Regular), .Location = New Point(294, 24), .AutoSize = True}
            txtDistrict = New TextBox() With {.Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Regular), .Location = New Point(294, 40), .Size = New Size(240, 23), .TabIndex = 10}
            AddHandler txtDistrict.TextChanged, AddressOf OnFieldChanged

            Dim lblPincodeTag As New Label() With {.Text = "Pincode (6 Digits)", .Font = New Font(ThemeConstants.FontNameDefault, 8.25!, FontStyle.Regular), .Location = New Point(548, 24), .AutoSize = True}
            txtPincode = New TextBox() With {.MaxLength = 6, .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Regular), .Location = New Point(548, 40), .Size = New Size(252, 23), .TabIndex = 11}
            AddHandler txtPincode.TextChanged, AddressOf OnFieldChanged

            Dim lblAddressTag As New Label() With {.Text = "Principal Business Address *", .Font = New Font(ThemeConstants.FontNameDefault, 8.25!, FontStyle.Bold), .Location = New Point(0, 68), .AutoSize = True}
            txtAddress = New TextBox() With {.Multiline = True, .ScrollBars = ScrollBars.Vertical, .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Regular), .Location = New Point(0, 84), .Size = New Size(800, 34), .TabIndex = 12}
            AddHandler txtAddress.TextChanged, AddressOf OnFieldChanged
            AddHandler txtAddress.Enter, Sub(s, e) Me.AcceptButton = Nothing
            AddHandler txtAddress.Leave, Sub(s, e) Me.AcceptButton = btnSaveClient

            pnlStep3Container.Controls.Add(txtAddress)
            pnlStep3Container.Controls.Add(lblAddressTag)
            pnlStep3Container.Controls.Add(txtPincode)
            pnlStep3Container.Controls.Add(lblPincodeTag)
            pnlStep3Container.Controls.Add(txtDistrict)
            pnlStep3Container.Controls.Add(lblDistrictTag)
            pnlStep3Container.Controls.Add(lblBadgeState)
            pnlStep3Container.Controls.Add(cboState)
            pnlStep3Container.Controls.Add(lblStateTag)
            pnlStep3Container.Controls.Add(lblSec2Header)

            ' -----------------------------------------------------------------
            ' STEP 4 CONTAINER: ④ CONTACT AND ASSIGNMENT (14px Breathing Space)
            ' -----------------------------------------------------------------
            pnlStep4Container = New Panel() With {
                .Location = New Point(16, 406),
                .Size = New Size(812, 100),
                .BackColor = Color.White
            }

            lblSec3Header = New Label() With {
                .Text = "④ CONTACT AND ASSIGNMENT",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(100, 116, 139), ' Soft Muted Slate
                .Location = New Point(0, 4),
                .AutoSize = True
            }

            Dim lblContactTag As New Label() With {.Text = "Contact Person (Optional)", .Font = New Font(ThemeConstants.FontNameDefault, 8.0!, FontStyle.Regular), .ForeColor = Color.FromArgb(100, 116, 139), .Location = New Point(0, 24), .AutoSize = True}
            txtContact = New TextBox() With {.Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Regular), .Location = New Point(0, 40), .Size = New Size(250, 23), .TabIndex = 13}
            AddHandler txtContact.TextChanged, AddressOf OnFieldChanged

            Dim lblPhoneTag As New Label() With {.Text = "Mobile Phone (Optional)", .Font = New Font(ThemeConstants.FontNameDefault, 8.0!, FontStyle.Regular), .ForeColor = Color.FromArgb(100, 116, 139), .Location = New Point(264, 24), .AutoSize = True}
            txtPhone = New TextBox() With {.Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Regular), .Location = New Point(264, 40), .Size = New Size(250, 23), .TabIndex = 14}
            AddHandler txtPhone.TextChanged, AddressOf OnFieldChanged

            Dim lblEmailTag As New Label() With {.Text = "Email Address (Optional)", .Font = New Font(ThemeConstants.FontNameDefault, 8.0!, FontStyle.Regular), .ForeColor = Color.FromArgb(100, 116, 139), .Location = New Point(528, 24), .AutoSize = True}
            txtEmail = New TextBox() With {.Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Regular), .Location = New Point(528, 40), .Size = New Size(272, 23), .TabIndex = 15}
            AddHandler txtEmail.TextChanged, AddressOf OnFieldChanged

            Dim lblDeptTag As New Label() With {.Text = "Department Incharge *", .Font = New Font(ThemeConstants.FontNameDefault, 8.25!, FontStyle.Bold), .Location = New Point(0, 68), .AutoSize = True}
            cboDept = New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList, .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Regular), .Location = New Point(0, 82), .Size = New Size(250, 23), .TabIndex = 16}
            cboDept.DataSource = [Enum].GetValues(GetType(DepartmentType))
            AddHandler cboDept.SelectedIndexChanged, AddressOf OnFieldChanged

            chkActive = New CheckBox() With {
                .Text = "Active Client Master Record",
                .Checked = True,
                .Font = New Font(ThemeConstants.FontNameDefault, 8.0!, FontStyle.Regular),
                .ForeColor = Color.FromArgb(100, 116, 139),
                .Location = New Point(264, 84),
                .AutoSize = True
            }
            AddHandler chkActive.CheckedChanged, AddressOf OnFieldChanged

            pnlStep4Container.Controls.Add(chkActive)
            pnlStep4Container.Controls.Add(cboDept)
            pnlStep4Container.Controls.Add(lblDeptTag)
            pnlStep4Container.Controls.Add(txtEmail)
            pnlStep4Container.Controls.Add(lblEmailTag)
            pnlStep4Container.Controls.Add(txtPhone)
            pnlStep4Container.Controls.Add(lblPhoneTag)
            pnlStep4Container.Controls.Add(txtContact)
            pnlStep4Container.Controls.Add(lblContactTag)
            pnlStep4Container.Controls.Add(lblSec3Header)

            ' Add Step Containers to pnlMainBody in Sequential Top-to-Bottom Order
            pnlMainBody.Controls.Add(pnlStep4Container)
            pnlMainBody.Controls.Add(pnlStep3Container)
            pnlMainBody.Controls.Add(pnlStep2Container)
            pnlMainBody.Controls.Add(pnlGstHero)

            ' WINFORMS DOCKING LAYOUT ARCHITECTURE:
            ' Add Header, Footer, then MainBody (Dock = Fill) and SendToBack
            Me.Controls.Add(pnlMainBody)
            Me.Controls.Add(pnlFooter)
            Me.Controls.Add(pnlHeader)

            pnlMainBody.SendToBack()

            AddHandler Me.FormClosing, AddressOf FrmAddEditClient_FormClosing

            Me.ResumeLayout(False)
        End Sub

        Private Sub FrmAddEditClient_Shown(sender As Object, e As EventArgs)
            PositionFooterControls()
            UpdateFetchButtonState()
            If _clientId = 0 Then
                txtGstin.Focus()
                txtGstin.Select(0, 0)
            End If
        End Sub

        Private Sub PositionFooterControls()
            If pnlFooter Is Nothing OrElse btnSaveClient Is Nothing OrElse btnCancel Is Nothing Then Return
            Dim footerWidth = pnlFooter.ClientSize.Width
            btnSaveClient.Location = New Point(footerWidth - 180, 13)
            btnSaveClient.Size = New Size(164, 38)
            btnCancel.Location = New Point(footerWidth - 290, 13)
            btnCancel.Size = New Size(100, 38)
            If btnArchiveClient IsNot Nothing AndAlso btnArchiveClient.Visible Then
                btnArchiveClient.Location = New Point(footerWidth - 410, 13)
            End If
        End Sub

        Private Sub OnFieldChanged(sender As Object, e As EventArgs)
            If _isInitializing Then Return
            _hasUnsavedChanges = True
            UpdateCompletionStatus()
        End Sub

        Private Sub UpdateFetchButtonState()
            If btnFetchGst Is Nothing OrElse txtGstin Is Nothing Then Return
            Dim gstin = txtGstin.Text.Trim().ToUpper()
            Dim isValid = (gstin.Length = 15 AndAlso GstVerificationEngine.IsValidGstinSyntax(gstin))

            btnFetchGst.Enabled = isValid
            If isValid Then
                btnFetchGst.Scheme = Forms.Common.ModernButton.ButtonScheme.Success
                btnFetchGst.Text = " ⚡ FETCH GST DETAILS"
            Else
                btnFetchGst.Scheme = Forms.Common.ModernButton.ButtonScheme.Secondary
                btnFetchGst.Text = " ⚡ FETCH GST DETAILS"
            End If
        End Sub

        Private Sub FocusFirstEditableRequiredField()
            If String.IsNullOrWhiteSpace(txtCode.Text) Then
                txtCode.Focus()
            ElseIf String.IsNullOrWhiteSpace(txtName.Text) Then
                txtName.Focus()
            ElseIf String.IsNullOrWhiteSpace(txtPan.Text) OrElse txtPan.Text.Trim().Length <> 10 Then
                txtPan.Focus()
            ElseIf cboEntityType.SelectedIndex <= 0 Then
                cboEntityType.Focus()
            ElseIf cboState.SelectedIndex <= 0 Then
                cboState.Focus()
            ElseIf String.IsNullOrWhiteSpace(txtAddress.Text) Then
                txtAddress.Focus()
            Else
                txtContact.Focus()
            End If
        End Sub

        Private Sub UpdateCompletionStatus()
            Dim isGstVerified = _isLiveApiVerified
            Dim isNameFilled = Not String.IsNullOrWhiteSpace(txtName.Text)
            Dim isPanFilled = (txtPan.Text.Trim().Length = 10)
            Dim isEntitySelected = (cboEntityType.SelectedIndex > 0)
            Dim isStateSelected = (cboState.SelectedIndex > 0)
            Dim isAddressFilled = Not String.IsNullOrWhiteSpace(txtAddress.Text)

            Dim isAllRequiredComplete = isNameFilled AndAlso isPanFilled AndAlso isEntitySelected AndAlso isStateSelected AndAlso isAddressFilled

            If isGstVerified AndAlso isAllRequiredComplete Then
                lblFooterStatus.Text = "✓ GST verified    ✓ Required details complete"
                lblFooterStatus.ForeColor = Color.FromArgb(52, 211, 153) ' Mint Green
                btnSaveClient.Scheme = Forms.Common.ModernButton.ButtonScheme.Success
                btnSaveClient.Text = " 💾 SAVE CLIENT"

                lblSec1Header.Text = "✓ ② CLIENT DETAILS COMPLETE"
                lblSec1Header.ForeColor = Color.FromArgb(6, 95, 70)
                lblSec2Header.Text = "✓ ③ LOCATION COMPLETE"
                lblSec2Header.ForeColor = Color.FromArgb(6, 95, 70)
                lblSec3Header.Text = "✓ ④ ASSIGNMENT READY"
                lblSec3Header.ForeColor = Color.FromArgb(6, 95, 70)
            ElseIf isGstVerified Then
                lblFooterStatus.Text = "✓ GST verified    ⚠ Required details pending"
                lblFooterStatus.ForeColor = Color.FromArgb(251, 191, 36) ' Amber
                btnSaveClient.Scheme = Forms.Common.ModernButton.ButtonScheme.Secondary
                btnSaveClient.Text = "Save Client"

                lblSec1Header.Text = "② REVIEW CLIENT DETAILS (ACTIVE)"
                lblSec1Header.ForeColor = Color.FromArgb(67, 56, 202)
                lblSec2Header.Text = "③ BUSINESS LOCATION"
                lblSec2Header.ForeColor = Color.FromArgb(71, 85, 105)
                lblSec3Header.Text = "④ CONTACT AND ASSIGNMENT"
                lblSec3Header.ForeColor = Color.FromArgb(100, 116, 139)
            ElseIf isAllRequiredComplete Then
                lblFooterStatus.Text = "○ GST unverified    ✓ Manual details complete"
                lblFooterStatus.ForeColor = Color.FromArgb(148, 163, 184)
                btnSaveClient.Scheme = Forms.Common.ModernButton.ButtonScheme.Success
                btnSaveClient.Text = " 💾 SAVE CLIENT"

                lblSec1Header.Text = "✓ ② CLIENT DETAILS COMPLETE"
                lblSec1Header.ForeColor = Color.FromArgb(6, 95, 70)
                lblSec2Header.Text = "✓ ③ LOCATION COMPLETE"
                lblSec2Header.ForeColor = Color.FromArgb(6, 95, 70)
                lblSec3Header.Text = "✓ ④ ASSIGNMENT READY"
                lblSec3Header.ForeColor = Color.FromArgb(6, 95, 70)
            Else
                lblFooterStatus.Text = "○ GST not verified    ○ Required details pending"
                lblFooterStatus.ForeColor = Color.FromArgb(148, 163, 184)
                btnSaveClient.Scheme = Forms.Common.ModernButton.ButtonScheme.Secondary
                btnSaveClient.Text = "Save Client"

                lblSec1Header.Text = "② REVIEW CLIENT DETAILS"
                lblSec1Header.ForeColor = Color.FromArgb(100, 116, 139) ' Quiet Slate in unverified state
                lblSec2Header.Text = "③ BUSINESS LOCATION"
                lblSec2Header.ForeColor = Color.FromArgb(148, 163, 184)
                lblSec3Header.Text = "④ CONTACT AND ASSIGNMENT"
                lblSec3Header.ForeColor = Color.FromArgb(148, 163, 184)
            End If
        End Sub

        Private Sub PopulateFormValues()
            If _clientDto Is Nothing Then Return

            _clientId = _clientDto.ClientId
            txtCode.Text = _clientDto.ClientCode
            txtCode.ReadOnly = True
            txtName.Text = _clientDto.ClientName
            txtGstin.Text = _clientDto.Gstin
            txtPan.Text = _clientDto.PanNumber
            txtAddress.Text = _clientDto.Address
            txtDistrict.Text = _clientDto.District
            txtPincode.Text = _clientDto.Pincode
            txtContact.Text = _clientDto.ContactPerson
            txtPhone.Text = _clientDto.Phone
            txtEmail.Text = _clientDto.Email
            chkActive.Checked = _clientDto.IsActive
            _isLiveApiVerified = _clientDto.IsLiveApiData

            If _isLiveApiVerified Then
                SetVerifiedStateUI(True, _clientDto.ClientName, _clientDto.Gstin, _clientDto.EntityType, _clientDto.StateName, _clientDto.District)
            End If

            If cboDept.Items.Count > 0 Then cboDept.SelectedItem = _clientDto.Department
            If Not String.IsNullOrEmpty(_clientDto.GstType) Then SelectComboItemByText(cboGstType, _clientDto.GstType)
            If Not String.IsNullOrEmpty(_clientDto.EntityType) Then SelectComboItemByText(cboEntityType, NormalizeEntityType(_clientDto.EntityType))
            If Not String.IsNullOrEmpty(_clientDto.StateName) Then SelectComboItemByText(cboState, _clientDto.StateName)
            UpdateFetchButtonState()
            UpdateCompletionStatus()
        End Sub

        Private Sub SetVerifiedStateUI(isVerified As Boolean, tradeName As String, gstin As String, entityType As String, stateName As String, district As String)
            _isLiveApiVerified = isVerified
            lblBadgeName.Visible = isVerified
            lblBadgePan.Visible = isVerified
            lblBadgeEntity.Visible = isVerified
            lblBadgeState.Visible = isVerified

            If isVerified Then
                pnlGstHero.Height = 118
                pnlVerifiedSummary.Visible = True
                lblVerifiedSummaryText.Text = $"✓ GST VERIFIED: {tradeName} ({gstin}) • {entityType} • {stateName} • {district}"
                
                lblGstHeroTitle.Text = "✓ ① GSTIN VERIFIED"
                lblGstHeroTitle.ForeColor = Color.FromArgb(6, 95, 70)
                lblSec1Header.Text = "② REVIEW CLIENT DETAILS (ACTIVE)"
                lblSec1Header.ForeColor = Color.FromArgb(67, 56, 202)

                ' Reposition Step Containers downwards cleanly to preserve breathing space
                pnlStep2Container.Location = New Point(16, 176)
                pnlStep3Container.Location = New Point(16, 306)
                pnlStep4Container.Location = New Point(16, 438)
            Else
                pnlGstHero.Height = 86
                pnlVerifiedSummary.Visible = False
                lblGstHeroTitle.Text = "① VERIFY GSTIN"
                lblGstHeroTitle.ForeColor = Color.FromArgb(67, 56, 202)
                lblSec1Header.Text = "② REVIEW CLIENT DETAILS"
                lblSec1Header.ForeColor = Color.FromArgb(100, 116, 139) ' Quiet Slate in unverified state

                pnlStep2Container.Location = New Point(16, 144)
                pnlStep3Container.Location = New Point(16, 274)
                pnlStep4Container.Location = New Point(16, 406)
            End If
            UpdateCompletionStatus()
        End Sub

        Private Sub PopulateAllIndianStates(cbo As ComboBox)
            cbo.Items.Clear()
            cbo.Items.Add("-- Select State / UT --")
            cbo.Items.AddRange(New Object() {
                "01 - Jammu & Kashmir", "02 - Himachal Pradesh", "03 - Punjab", "04 - Chandigarh", "05 - Uttarakhand",
                "06 - Haryana", "07 - Delhi", "08 - Rajasthan", "09 - Uttar Pradesh", "10 - Bihar",
                "11 - Sikkim", "12 - Arunachal Pradesh", "13 - Nagaland", "14 - Manipur", "15 - Mizoram",
                "16 - Tripura", "17 - Meghalaya", "18 - Assam", "19 - West Bengal", "20 - Jharkhand",
                "21 - Odisha", "22 - Chhattisgarh", "23 - Madhya Pradesh", "24 - Gujarat", "25 - Daman & Diu",
                "26 - Dadra & Nagar Haveli", "27 - Maharashtra", "28 - Andhra Pradesh", "29 - Karnataka", "30 - Goa",
                "31 - Lakshadweep", "32 - Kerala", "33 - Tamil Nadu", "34 - Puducherry", "35 - Andaman & Nicobar",
                "36 - Telangana", "37 - Andhra Pradesh (New)", "38 - Ladakh", "Other / International"
            })
            cbo.SelectedIndex = 0 ' Default unselected placeholder!
        End Sub

        Private Sub txtName_TextChanged(sender As Object, e As EventArgs)
            If _clientId <> 0 OrElse _userModifiedCodeManually Then Return

            Dim businessName = txtName.Text.Trim()
            If String.IsNullOrWhiteSpace(businessName) Then
                txtCode.Text = String.Empty
                Return
            End If

            Dim noiseWords As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {"and", "&", "pvt", "ltd", "co", "llp", "traders", "firm"}
            Dim words = businessName.Split(New Char() {" "c, "-"c, "_"c}, StringSplitOptions.RemoveEmptyEntries) _
                                   .Where(Function(w) Not noiseWords.Contains(w.Trim())) _
                                   .ToArray()

            Dim prefix As String = String.Empty
            If words.Length >= 2 Then
                For Each w In words.Take(3)
                    If w.Length > 0 AndAlso Char.IsLetterOrDigit(w(0)) Then prefix &= w(0)
                    Next
            ElseIf words.Length = 1 Then
                Dim clean = Regex.Replace(words(0), "[^a-zA-Z0-9]", "")
                prefix = If(clean.Length >= 3, clean.Substring(0, 3), clean)
            End If

            prefix = If(String.IsNullOrWhiteSpace(prefix), "CLI", prefix.ToUpper())
            txtCode.Text = $"{prefix}-001"
        End Sub

        Private Function NormalizeEntityType(rawEntity As String) As String
            If String.IsNullOrWhiteSpace(rawEntity) Then Return "Proprietorship"
            Dim clean = rawEntity.Trim()

            If clean.Contains("Private Limited", StringComparison.OrdinalIgnoreCase) OrElse
               clean.Contains("Pvt Ltd", StringComparison.OrdinalIgnoreCase) OrElse
               clean.Contains("Public Limited", StringComparison.OrdinalIgnoreCase) OrElse
               clean.Contains("Company", StringComparison.OrdinalIgnoreCase) Then
                Return "Pvt Ltd / Public Ltd"
            ElseIf clean.Contains("Proprietor", StringComparison.OrdinalIgnoreCase) Then
                Return "Proprietorship"
            ElseIf clean.Contains("Partnership", StringComparison.OrdinalIgnoreCase) OrElse
                   clean.Contains("LLP", StringComparison.OrdinalIgnoreCase) OrElse
                   clean.Contains("Limited Liability Partnership", StringComparison.OrdinalIgnoreCase) Then
                Return "Partnership / LLP"
            ElseIf clean.Contains("HUF", StringComparison.OrdinalIgnoreCase) OrElse
                   clean.Contains("Hindu Undivided Family", StringComparison.OrdinalIgnoreCase) Then
                Return "HUF"
            ElseIf clean.Contains("Trust", StringComparison.OrdinalIgnoreCase) OrElse
                   clean.Contains("Society", StringComparison.OrdinalIgnoreCase) OrElse
                   clean.Contains("Club", StringComparison.OrdinalIgnoreCase) OrElse
                   clean.Contains("Association", StringComparison.OrdinalIgnoreCase) Then
                Return "Trust"
            Else
                Return "Individual"
            End If
        End Function

        Private Sub txtGstin_TextChanged(sender As Object, e As EventArgs)
            UpdateFetchButtonState()
            Dim gstin = txtGstin.Text.Trim().ToUpper()
            If gstin.Length = 15 Then
                Dim rec = GstVerificationEngine.ExtractStatutoryDetailsFromGstin(gstin)
                If Not String.IsNullOrWhiteSpace(rec.PanNumber) Then txtPan.Text = rec.PanNumber
                If Not String.IsNullOrWhiteSpace(rec.StateCode) Then SelectComboItemByText(cboState, rec.StateCode)
                If Not String.IsNullOrWhiteSpace(rec.EntityType) Then SelectComboItemByText(cboEntityType, NormalizeEntityType(rec.EntityType))
            End If
        End Sub

        Private Async Sub btnFetchGst_Click(sender As Object, e As EventArgs)
            Dim gstin = txtGstin.Text.Trim().ToUpper()
            If String.IsNullOrEmpty(gstin) Then
                AppNotificationHelper.ShowInfo("Please enter a 15-digit GSTIN number.", "GSTIN Required", Me)
                txtGstin.Focus()
                Return
            End If

            If Not GstVerificationEngine.IsValidGstinSyntax(gstin) Then
                AppNotificationHelper.ShowWarning("Please enter a valid 15-character GSTIN (e.g. 22ACIPJ4496P1Z1).", "Invalid GSTIN", Me)
                txtGstin.Focus()
                Return
            End If

            btnFetchGst.Enabled = False
            btnFetchGst.Text = " ⏳ QUERYING..."

            Try
                pnlMainBody.SuspendLayout()

                Dim result = Await GstVerificationEngine.VerifyGstinWithProviderAsync(gstin, _appLogger)
                Dim rec = result.Record

                txtPan.Text = rec.PanNumber
                SelectComboItemByText(cboState, rec.StateCode)

                Dim currentSelectedState = If(cboState.SelectedItem IsNot Nothing AndAlso cboState.SelectedIndex > 0, cboState.SelectedItem.ToString(), rec.StateName)
                Dim normalizedEntity = NormalizeEntityType(rec.EntityType)

                If result.IsLiveApiData Then
                    Dim tradeName = If(Not String.IsNullOrWhiteSpace(rec.TradeName), rec.TradeName, rec.LegalName)
                    txtName.Text = tradeName
                    txtDistrict.Text = rec.District
                    txtPincode.Text = rec.Pincode
                    txtAddress.Text = rec.Address
                    chkActive.Checked = rec.IsActive
                    SelectComboItemByText(cboEntityType, normalizedEntity)
                    SelectComboItemByText(cboGstType, rec.GstType)

                    SetVerifiedStateUI(True, tradeName, gstin, normalizedEntity, currentSelectedState, rec.District)
                    FocusFirstEditableRequiredField()
                Else
                    SetVerifiedStateUI(False, String.Empty, String.Empty, String.Empty, String.Empty, String.Empty)
                    txtName.Text = String.Empty
                    txtAddress.Text = String.Empty
                    txtDistrict.Text = String.Empty
                    txtPincode.Text = String.Empty
                    chkActive.Checked = False
                    SelectComboItemByText(cboEntityType, normalizedEntity)

                    FocusFirstEditableRequiredField()

                    AppNotificationHelper.ShowWarning("GST verification could not be completed. Please check your connection or enter firm details manually.", "GST Verification Pending", Me)
                End If

            Catch ex As Exception
                AppNotificationHelper.ShowError($"GST verification error: {ex.Message}", "Verification Error", Me)
            Finally
                pnlMainBody.ResumeLayout(True)
                UpdateFetchButtonState()
            End Try
        End Sub

        Private Sub btnOpenGovtGstSearch_Click(sender As Object, e As EventArgs)
            Dim gstin = txtGstin.Text.Trim().ToUpper()
            Try
                If Not String.IsNullOrWhiteSpace(gstin) Then Clipboard.SetText(gstin)
                System.Diagnostics.Process.Start(New System.Diagnostics.ProcessStartInfo("https://services.gst.gov.in/services/searchtp") With {.UseShellExecute = True})
            Catch ex As Exception
                AppNotificationHelper.ShowError($"Unable to open browser: {ex.Message}", "Portal Error", Me)
            End Try
        End Sub

        Private Async Sub btnSaveClient_Click(sender As Object, e As EventArgs)
            ' 1. Highlight missing required fields
            Dim businessName = txtName.Text.Trim()
            If String.IsNullOrWhiteSpace(businessName) Then
                lblFooterStatus.Text = "⚠ Business / Trade Name is required"
                lblFooterStatus.ForeColor = Color.FromArgb(245, 158, 11)
                txtName.Focus()
                Return
            End If

            Dim clientCode = txtCode.Text.Trim().ToUpper()
            If String.IsNullOrWhiteSpace(clientCode) Then
                lblFooterStatus.Text = "⚠ Client Code is required"
                lblFooterStatus.ForeColor = Color.FromArgb(245, 158, 11)
                txtCode.Focus()
                Return
            End If

            Dim panNumber = txtPan.Text.Trim().ToUpper()
            If String.IsNullOrWhiteSpace(panNumber) OrElse Not Regex.IsMatch(panNumber, "^[A-Z]{5}[0-9]{4}[A-Z]{1}$") Then
                lblFooterStatus.Text = "⚠ Valid 10-character PAN Card number is required"
                lblFooterStatus.ForeColor = Color.FromArgb(245, 158, 11)
                txtPan.Focus()
                Return
            End If

            If cboState.SelectedIndex <= 0 Then
                lblFooterStatus.Text = "⚠ State / UT selection is required"
                lblFooterStatus.ForeColor = Color.FromArgb(245, 158, 11)
                cboState.Focus()
                Return
            End If

            Dim gstinNumber = txtGstin.Text.Trim().ToUpper()
            If Not String.IsNullOrWhiteSpace(gstinNumber) AndAlso Not GstVerificationEngine.IsValidGstinSyntax(gstinNumber) Then
                lblFooterStatus.Text = "⚠ Valid 15-character GSTIN format is required"
                lblFooterStatus.ForeColor = Color.FromArgb(245, 158, 11)
                txtGstin.Focus()
                Return
            End If

            Dim addressText = txtAddress.Text.Trim()
            If String.IsNullOrWhiteSpace(addressText) Then
                lblFooterStatus.Text = "⚠ Principal Business Address is required"
                lblFooterStatus.ForeColor = Color.FromArgb(245, 158, 11)
                txtAddress.Focus()
                Return
            End If

            ' 2. Duplicate Client Protection (Requirement 15)
            If _clientId = 0 Then
                Try
                    Dim allClients = Await _clientService.GetAllClientsAsync(includeDeleted:=False)

                    If Not String.IsNullOrWhiteSpace(gstinNumber) Then
                        Dim dupGst = allClients.Find(Function(c) Not String.IsNullOrEmpty(c.Gstin) AndAlso c.Gstin.Equals(gstinNumber, StringComparison.OrdinalIgnoreCase))
                        If dupGst IsNot Nothing Then
                            Dim res = MessageBox.Show(Me, $"This GSTIN is already registered for client '{dupGst.ClientName}' ({dupGst.ClientCode})." & Environment.NewLine & Environment.NewLine & "Would you like to open the existing client profile?", "This GSTIN is already registered", MessageBoxButtons.YesNo, MessageBoxIcon.Information)
                            If res = DialogResult.Yes Then
                                Me.DialogResult = DialogResult.Cancel
                                Me.Close()
                            End If
                            Return
                        End If
                    End If

                    Dim dupCode = allClients.Find(Function(c) c.ClientCode.Equals(clientCode, StringComparison.OrdinalIgnoreCase))
                    If dupCode IsNot Nothing Then
                        AppNotificationHelper.ShowWarning($"A client record already exists with Client Code '{clientCode}': {dupCode.ClientName}.", "Duplicate Client Code", Me)
                        txtCode.Focus()
                        Return
                    End If
                Catch ex As Exception
                    ' Continue to BLL
                End Try
            End If

            ' 3. Anti-Double Click / Saving State
            btnSaveClient.Enabled = False
            btnSaveClient.Text = " ⏳ Saving..."

            Try
                Dim clientDto As New ClientDto With {
                    .ClientId = _clientId,
                    .ClientCode = clientCode,
                    .ClientName = businessName,
                    .Gstin = gstinNumber,
                    .PanNumber = panNumber,
                    .EntityType = If(cboEntityType.SelectedIndex > 0, cboEntityType.SelectedItem.ToString(), "Proprietorship"),
                    .GstType = If(cboGstType.SelectedIndex > 0, cboGstType.SelectedItem.ToString(), "Regular Monthly"),
                    .StateName = cboState.SelectedItem.ToString(),
                    .District = txtDistrict.Text.Trim(),
                    .Pincode = txtPincode.Text.Trim(),
                    .Address = addressText,
                    .ContactPerson = txtContact.Text.Trim(),
                    .Phone = txtPhone.Text.Trim(),
                    .Email = txtEmail.Text.Trim(),
                    .Department = CType(If(cboDept.SelectedItem, DepartmentType.IncomeTax), DepartmentType),
                    .IsActive = chkActive.Checked,
                    .IsLiveApiData = _isLiveApiVerified
                }

                If _clientId = 0 Then
                    Await _clientService.CreateClientAsync(clientDto)
                Else
                    Await _clientService.UpdateClientAsync(clientDto)
                End If

                ' Success Behaviour (Concise Toast Notification)
                _hasUnsavedChanges = False
                AppNotificationHelper.ShowSuccess($"Client saved successfully.{Environment.NewLine}{Environment.NewLine}• Business Name: {clientDto.ClientName}{Environment.NewLine}• Client Code: {clientDto.ClientCode}{Environment.NewLine}• GSTIN: {If(String.IsNullOrEmpty(clientDto.Gstin), "N/A", clientDto.Gstin)}", "Client Saved", Me)

                Me.DialogResult = DialogResult.OK
                Me.Close()

            Catch ex As Exception
                AppNotificationHelper.ShowError($"Unable to save Client record: {ex.Message}", "Save Error", Me)
            Finally
                btnSaveClient.Enabled = True
                btnSaveClient.Text = " 💾 SAVE CLIENT"
            End Try
        End Sub

        Private Sub btnCancel_Click(sender As Object, e As EventArgs)
            Me.Close()
        End Sub

        Private Sub FrmAddEditClient_FormClosing(sender As Object, e As FormClosingEventArgs)
            If Me.DialogResult = DialogResult.OK Then Return

            If _hasUnsavedChanges Then
                Dim dlgRes = MessageBox.Show(Me, "You have unsaved changes. What would you like to do?" & Environment.NewLine & Environment.NewLine & "• Click [Yes] to SAVE your changes." & Environment.NewLine & "• Click [No] to DISCARD your changes." & Environment.NewLine & "• Click [Cancel] to keep editing.", "Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question)
                If dlgRes = DialogResult.Yes Then
                    e.Cancel = True
                    btnSaveClient_Click(sender, e)
                ElseIf dlgRes = DialogResult.Cancel Then
                    e.Cancel = True
                End If
            End If
        End Sub

        Private Async Sub btnArchiveClient_Click(sender As Object, e As EventArgs)
            If _clientId = 0 Then Return

            Dim result = AppNotificationHelper.ShowQuestion($"Are you sure you want to archive client '{txtName.Text}'?", "Confirm Archive", Me)
            If result = DialogResult.Yes Then
                Try
                    Dim deleted = Await _clientService.SoftDeleteClientAsync(_clientId)
                    If deleted Then
                        _hasUnsavedChanges = False
                        AppNotificationHelper.ShowSuccess("Client record archived successfully.", "Client Archived", Me)
                        Me.DialogResult = DialogResult.OK
                        Me.Close()
                    End If
                Catch ex As Exception
                    AppNotificationHelper.ShowError($"Error archiving client: {ex.Message}", "Archive Failure", Me)
                End Try
            End If
        End Sub

        Private Sub SelectComboItemByText(cbo As ComboBox, textToFind As String)
            If cbo Is Nothing OrElse cbo.Items.Count = 0 OrElse String.IsNullOrEmpty(textToFind) Then Return
            For i As Integer = 0 To cbo.Items.Count - 1
                Dim itemText = cbo.Items(i).ToString()
                If itemText.ToLower().Contains(textToFind.ToLower()) Then
                    cbo.SelectedIndex = i
                    Return
                End If
            Next
        End Sub
    End Class
End Namespace
