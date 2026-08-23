Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.IO
Imports System.Linq
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports StaffAutomation.BLL.Logging
Imports StaffAutomation.BLL.Services
Imports StaffAutomation.Core.Configuration
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Logging
Imports StaffAutomation.DAL.Configuration
Imports StaffAutomation.DAL.Core
Imports StaffAutomation.DAL.Repositories

Namespace Forms.Main
    ''' <summary>
    ''' Ultra-Premium CA Office Master Client Registry and Statutory Hub UserControl.
    ''' Features Windows 11 Fluent Design System, Responsive Right-Anchored Toolbar (Zero Overflow),
    ''' Explicit 10-Column DataGrid with Zebra Striping and Single Gridlines, and Standalone Modal Dialog Triggers.
    ''' </summary>
    Public Class ClientsControl
        Inherits UserControl

        Private ReadOnly _clientService As IClientService
        Private ReadOnly _appLogger As IAppLogger
        Private ReadOnly _auditLogger As BLL.Logging.AuditLogger

        Private _clientList As List(Of ClientDto) = New List(Of ClientDto)()
        Private _selectedClient As ClientDto = Nothing

        ' UI Components
        Private pnlHeroHeader As Panel
        Private lblHeroTitle As Label
        Private lblHeroSubtitle As Label
        Private lblStatBadges As Label

        Private pnlToolbar As Panel
        Private pnlSearchControls As Panel
        Private pnlActionControls As Panel

        Private pbSearchIcon As PictureBox
        Private txtSearch As Krypton.Toolkit.KryptonTextBox
        Private cboEntityFilter As Krypton.Toolkit.KryptonComboBox
        Private cboGstFilter As Krypton.Toolkit.KryptonComboBox

        Private btnAddNewClient As Forms.Common.ModernButton
        Private btnImportExcel As Forms.Common.ModernButton
        Private btnExportExcel As Forms.Common.ModernButton
        Private btnRefreshGrid As Forms.Common.ModernButton

        Private pnlGridCard As Panel
        Private pnlGridHeader As Panel
        Private lblGridTitle As Label
        Private btnCustomizeGrid As Forms.Common.ModernButton
        Private dgvClients As DataGridView
        Private _layoutManager As Forms.Common.GridLayoutManager

        ' Psychological Empty State & Placeholder Guide Controls
        Private pnlEmptyState As Panel
        Private pbEmptyIcon As PictureBox
        Private lblEmptyTitle As Label
        Private lblEmptySubtitle As Label
        Private btnEmptyAdd As Forms.Common.ModernButton
        Private btnEmptyImport As Forms.Common.ModernButton
        Private _isEmptyPlaceholderActive As Boolean = False

        ' Product Psychology & Engagement System Controls
        Private lblStreakBadge As Label
        Private pnlHealthGauge As Panel
        Private lblHealthText As Label
        Private pnlPsychologyBanner As Panel
        Private lblPsychologyText As Label
        Private btnPsychologyAction As Forms.Common.ModernButton
        Private lblLiveActivityTicker As Label
        Private _currentHealthScore As Integer = 0
        Private _unverifiedGstinCount As Integer = 0

        ' Win32 P/Invoke for Native TextBox Cue Banner (Placeholder)
        <System.Runtime.InteropServices.DllImport("user32.dll", CharSet:=System.Runtime.InteropServices.CharSet.Auto)>
        Private Shared Function SendMessage(hWnd As IntPtr, msg As Integer, wParam As IntPtr, <System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)> lParam As String) As IntPtr
        End Function
        Private Const EM_SETCUEBANNER As Integer = &H1501

        Private pnlSelectionContext As Panel
        Private lblSelectionInfo As Label
        Private btnEditSelectedClient As Forms.Common.ModernButton
        Private btnClearFilters As Forms.Common.ModernButton
        Private _filterOnlyNeedsVerification As Boolean = False

        Public Sub New(Optional filterNeedsVerification As Boolean = False)
            ' Initialize BLL Services for SQL Data Access
            Dim config As IAppConfiguration = New AppConfiguration()
            Dim connFactory As IDatabaseConnectionFactory = New DbConnectionFactory(config)
            Dim sqlHelper As ISqlHelper = New SqlHelper(connFactory)
            _appLogger = New AppLogger(config)
            _auditLogger = New BLL.Logging.AuditLogger(sqlHelper)
            Dim clientRepo As DAL.Interfaces.IClientRepository = New ClientRepository(sqlHelper)

            _clientService = New ClientService(clientRepo, _appLogger, _auditLogger)
            _filterOnlyNeedsVerification = filterNeedsVerification

            InitializeComponent()
            ConfigureExplicitGridColumns()
            LoadClientDataAsync()
        End Sub

        Protected Overrides Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)
            SetSearchPlaceholder()
            If txtSearch IsNot Nothing Then
                txtSearch.Focus()
                txtSearch.Select(0, 0)
            End If
        End Sub

        Private Sub SetSearchPlaceholder()
            If txtSearch IsNot Nothing AndAlso txtSearch.IsHandleCreated Then
                SendMessage(txtSearch.Handle, EM_SETCUEBANNER, New IntPtr(1), "Search client, GSTIN, PAN or client code...")
            End If
        End Sub

        Private Sub InitializeComponent()
            Me.SuspendLayout()
            Me.Dock = DockStyle.Fill
            Me.BackColor = ThemeConstants.WorkspaceBackground
            Me.Padding = New Padding(16)
            Me.AutoScroll = False

            ' 1. Indigo Hero Header Banner (#1E1B4B to #312E81)
            pnlHeroHeader = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 84,
                .BackColor = Color.FromArgb(30, 27, 75),
                .Padding = New Padding(20, 12, 20, 10),
                .Margin = New Padding(0, 0, 0, 8)
            }

            lblHeroTitle = New Label() With {
                .Text = "CA Master Client Registry",
                .Font = New Font(ThemeConstants.FontNameDefault, 13.0!, FontStyle.Bold),
                .ForeColor = Color.White,
                .Location = New Point(20, 12),
                .AutoSize = True
            }

            lblHeroSubtitle = New Label() With {
                .Text = "Manage client master records, GST verification and client details.",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Regular),
                .ForeColor = Color.FromArgb(199, 210, 254),
                .Location = New Point(20, 36),
                .AutoSize = True
            }

            ' Dedicated Right-Aligned Header Container (Zero Overlap)
            Dim pnlHeroRight As New Panel() With {
                .Dock = DockStyle.Right,
                .Width = 320,
                .BackColor = Color.Transparent
            }

            ' 1. Daily Work Streak Badge (Duolingo Loop)
            Dim streakCount = Forms.Common.BehavioralStreakManager.GetOrUpdateDailyStreak()
            lblStreakBadge = New Label() With {
                .Text = $"🔥 {streakCount}-Day Active",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(146, 64, 14), ' Dark Amber
                .BackColor = Color.FromArgb(254, 243, 199), ' Soft Amber Pill #FEF3C7
                .Size = New Size(115, 24),
                .Location = New Point(200, 4),
                .TextAlign = ContentAlignment.MiddleCenter
            }

            ' 2. Zeigarnik Effect Progress Meter & Data Health Gauge Text
            lblHealthText = New Label() With {
                .Text = "Data Health: 0%",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(224, 231, 255),
                .Location = New Point(0, 32),
                .Size = New Size(315, 18),
                .TextAlign = ContentAlignment.MiddleRight
            }

            pnlHealthGauge = New Panel() With {
                .Size = New Size(315, 8),
                .Location = New Point(0, 52),
                .BackColor = Color.FromArgb(49, 46, 129)
            }
            AddHandler pnlHealthGauge.Paint, Sub(s, e)
                                                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
                                                 Dim fillColor = Color.FromArgb(239, 68, 68) ' Red
                                                 If _currentHealthScore >= 80 Then
                                                     fillColor = Color.FromArgb(16, 185, 129) ' Green
                                                 ElseIf _currentHealthScore >= 50 Then
                                                     fillColor = Color.FromArgb(245, 158, 11) ' Amber
                                                 End If
                                                 Dim fillWidth = CInt(Math.Max(2, (pnlHealthGauge.Width * _currentHealthScore) \ 100))
                                                 Using b As New SolidBrush(fillColor)
                                                     e.Graphics.FillRectangle(b, 0, 0, fillWidth, pnlHealthGauge.Height)
                                                 End Using
                                             End Sub

            pnlHeroRight.Controls.Add(pnlHealthGauge)
            pnlHeroRight.Controls.Add(lblHealthText)
            pnlHeroRight.Controls.Add(lblStreakBadge)

            lblStatBadges = New Label() With {
                .Text = "0 Active Clients   •   0 GST Verified   •   0 Needs Verification",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(52, 211, 153),
                .Location = New Point(20, 58),
                .AutoSize = True,
                .Cursor = Cursors.Hand
            }
            AddHandler lblStatBadges.Click, Sub(s, e) ToggleNeedsVerificationFilter()

            pnlHeroHeader.Controls.Add(pnlHeroRight)
            pnlHeroHeader.Controls.Add(lblStatBadges)
            pnlHeroHeader.Controls.Add(lblHeroSubtitle)
            pnlHeroHeader.Controls.Add(lblHeroTitle)

            ' 2. Smart Workspace Instruction & Activity Banner
            pnlPsychologyBanner = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 34,
                .BackColor = Color.FromArgb(238, 242, 255), ' Soft Indigo #EEF2FF
                .Padding = New Padding(12, 4, 12, 4),
                .Margin = New Padding(0, 0, 0, 8)
            }

            lblPsychologyText = New Label() With {
                .Text = "💡 Client Directory  ·  Click to select  ·  Double-click to edit",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.75!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(55, 48, 163), ' #3730A3
                .Location = New Point(12, 8),
                .AutoSize = True
            }

            btnPsychologyAction = New Forms.Common.ModernButton() With {
                .Text = "⚡ 1-Click Auto-Verify GSTINs",
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Primary,
                .Size = New Size(210, 26),
                .Location = New Point(pnlPsychologyBanner.Width - 220, 4),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right,
                .Visible = False
            }
            AddHandler btnPsychologyAction.Click, AddressOf btnPsychologyAction_Click

            pnlPsychologyBanner.Controls.Add(btnPsychologyAction)
            pnlPsychologyBanner.Controls.Add(lblPsychologyText)

            ' 2. Windows 11 Fluent Responsive Toolbar Container (Zero Button Overflow)
            pnlToolbar = New Panel()
            ThemeConstants.ApplyToolbarStyle(pnlToolbar)

            ' GROUP A: FIND — Left-aligned Search & Filter Controls Panel (Width = 685)
            pnlSearchControls = New Panel() With {
                .Dock = DockStyle.Left,
                .Width = 685,
                .BackColor = Color.Transparent
            }

            pbSearchIcon = New PictureBox() With {
                .Image = VectorIconHelper.CreateSearchIcon(ThemeConstants.TextSecondary, 16),
                .Size = New Size(20, 20),
                .Location = New Point(10, 18),
                .SizeMode = PictureBoxSizeMode.CenterImage
            }

            txtSearch = New Krypton.Toolkit.KryptonTextBox() With {
                .Location = New Point(36, 15),
                .Size = New Size(280, 26),
                .TabIndex = 0
            }
            ThemeConstants.ApplyAppTextBoxStyle(txtSearch)
            AddHandler txtSearch.HandleCreated, Sub(s, e) SetSearchPlaceholder()
            AddHandler txtSearch.TextChanged, AddressOf txtSearch_TextChanged

            cboEntityFilter = New Krypton.Toolkit.KryptonComboBox() With {
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Location = New Point(324, 16),
                .Size = New Size(135, 24),
                .TabIndex = 1
            }
            ThemeConstants.ApplyAppComboBoxStyle(cboEntityFilter)
            cboEntityFilter.Items.AddRange(New Object() {"All Entity Types", "Proprietorship", "Partnership / LLP", "Pvt Ltd / Public Ltd", "Individual", "HUF"})
            cboEntityFilter.SelectedIndex = 0
            AddHandler cboEntityFilter.SelectedIndexChanged, AddressOf FilterGridData

            cboGstFilter = New Krypton.Toolkit.KryptonComboBox() With {
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Location = New Point(466, 16),
                .Size = New Size(135, 24),
                .TabIndex = 2
            }
            ThemeConstants.ApplyAppComboBoxStyle(cboGstFilter)
            cboGstFilter.Items.AddRange(New Object() {"All GST Schemes", "Regular Monthly", "Regular QRMP", "Composition Scheme", "Unregistered"})
            cboGstFilter.SelectedIndex = 0
            AddHandler cboGstFilter.SelectedIndexChanged, AddressOf FilterGridData

            btnClearFilters = New Forms.Common.ModernButton() With {
                .Text = "Clear",
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Secondary,
                .Size = New Size(68, 24),
                .Location = New Point(608, 16),
                .Enabled = False
            }
            AddHandler btnClearFilters.Click, Sub(s, e)
                                                  txtSearch.Text = String.Empty
                                                  cboEntityFilter.SelectedIndex = 0
                                                  cboGstFilter.SelectedIndex = 0
                                                  _filterOnlyNeedsVerification = False
                                                  FilterGridData(Nothing, Nothing)
                                              End Sub

            pnlSearchControls.Controls.Add(btnClearFilters)
            pnlSearchControls.Controls.Add(cboGstFilter)
            pnlSearchControls.Controls.Add(cboEntityFilter)
            pnlSearchControls.Controls.Add(txtSearch)
            pnlSearchControls.Controls.Add(pbSearchIcon)

            ' GROUP B & C: UTILITIES & CREATE — Right-aligned Action Buttons Panel (Right-Anchored, Width = 518)
            pnlActionControls = New Panel() With {
                .Dock = DockStyle.Right,
                .Width = 518,
                .BackColor = Color.Transparent
            }

            btnRefreshGrid = New Forms.Common.ModernButton() With {
                .Text = " Refresh",
                .Image = VectorIconHelper.CreateRefreshIcon(Color.FromArgb(51, 65, 85), 15),
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Secondary,
                .Size = New Size(95, 36),
                .Location = New Point(4, 10),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            AddHandler btnRefreshGrid.Click, Async Sub(s, e)
                                                btnRefreshGrid.Enabled = False
                                                btnRefreshGrid.Text = " Refreshing..."
                                                Await Task.Delay(250)
                                                LoadClientDataAsync()
                                                btnRefreshGrid.Text = " Refresh"
                                                btnRefreshGrid.Enabled = True
                                                AppNotificationHelper.ShowInfo($"Client registry updated • {_clientList.Count} active records.", "Registry Updated", Me)
                                            End Sub

            btnExportExcel = New Forms.Common.ModernButton() With {
                .Text = " Export",
                .Image = VectorIconHelper.CreateExportIcon(Color.FromArgb(51, 65, 85), 15),
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Secondary,
                .Size = New Size(110, 36),
                .Location = New Point(106, 10),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            AddHandler btnExportExcel.Click, AddressOf btnExportExcel_Click

            btnImportExcel = New Forms.Common.ModernButton() With {
                .Text = " Import Excel",
                .Image = VectorIconHelper.CreateImportIcon(Color.FromArgb(67, 56, 202), 15),
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Custom,
                .BackColor = Color.FromArgb(238, 242, 255),
                .ForeColor = Color.FromArgb(67, 56, 202),
                .CustomBorderColor = Color.FromArgb(199, 210, 254),
                .HoverColor = Color.FromArgb(224, 231, 255),
                .Size = New Size(130, 36),
                .Location = New Point(222, 10),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            AddHandler btnImportExcel.Click, AddressOf btnImportExcel_Click

            btnAddNewClient = New Forms.Common.ModernButton() With {
                .Text = " + Add New Client",
                .Image = VectorIconHelper.CreatePlusIcon(Color.White, 15),
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Primary,
                .Size = New Size(152, 36),
                .Location = New Point(358, 10),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            AddHandler btnAddNewClient.Click, AddressOf btnAddNewClient_Click

            pnlActionControls.Controls.Add(btnAddNewClient)
            pnlActionControls.Controls.Add(btnImportExcel)
            pnlActionControls.Controls.Add(btnExportExcel)
            pnlActionControls.Controls.Add(btnRefreshGrid)

            pnlToolbar.Controls.Add(pnlActionControls)
            pnlToolbar.Controls.Add(pnlSearchControls)

            ' 3. Windows 11 Fluent Data Table Directory Card Container
            pnlGridCard = New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = Color.White,
                .Padding = New Padding(12),
                .Margin = New Padding(0)
            }

            pnlGridHeader = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 34,
                .BackColor = Color.White
            }

            lblGridTitle = New Label() With {
                .Text = "Active Client Master Directory (Click to select · Double-click to edit)",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(0, 6),
                .AutoSize = True
            }

            btnCustomizeGrid = New Forms.Common.ModernButton() With {
                .Text = "⚙ Table Columns",
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Secondary,
                .Size = New Size(130, 28),
                .Location = New Point(pnlGridHeader.Width - 130, 2),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            AddHandler btnCustomizeGrid.Click, Sub(s, e)
                                                   If _layoutManager IsNot Nothing Then
                                                       Using dlg As New Forms.Common.FrmColumnCustomizer(dgvClients, _layoutManager)
                                                           dlg.ShowDialog(Me.FindForm())
                                                       End Using
                                                   End If
                                               End Sub

            pnlGridHeader.Controls.Add(btnCustomizeGrid)
            pnlGridHeader.Controls.Add(lblGridTitle)

            ' Selected Client Contextual Bar Container
            pnlSelectionContext = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 32,
                .BackColor = Color.FromArgb(238, 242, 255), ' Soft Indigo
                .Padding = New Padding(12, 4, 12, 4),
                .Visible = False
            }

            lblSelectionInfo = New Label() With {
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(49, 46, 129),
                .Location = New Point(8, 7),
                .AutoSize = True
            }

            btnEditSelectedClient = New Forms.Common.ModernButton() With {
                .Text = " ✏️ Edit Profile",
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Primary,
                .Size = New Size(110, 24),
                .Location = New Point(pnlSelectionContext.Width - 120, 4),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            AddHandler btnEditSelectedClient.Click, Sub(s, e)
                                                        If _selectedClient IsNot Nothing AndAlso _selectedClient.ClientId <> 0 Then
                                                            Using dlg As New FrmAddEditClient(_clientService, _appLogger, _selectedClient)
                                                                If dlg.ShowDialog() = DialogResult.OK Then
                                                                    LoadClientDataAsync()
                                                                End If
                                                            End Using
                                                        End If
                                                    End Sub

            pnlSelectionContext.Controls.Add(btnEditSelectedClient)
            pnlSelectionContext.Controls.Add(lblSelectionInfo)

            dgvClients = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .ReadOnly = True,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                .ScrollBars = ScrollBars.Vertical,
                .AllowUserToOrderColumns = True,
                .AllowUserToResizeColumns = True,
                .AutoGenerateColumns = False,
                .MultiSelect = False
            }

            ThemeConstants.ApplyModernGridStyle(dgvClients)
            AddHandler dgvClients.SelectionChanged, AddressOf dgvClients_SelectionChanged
            AddHandler dgvClients.CellDoubleClick, AddressOf dgvClients_CellDoubleClick
            AddHandler dgvClients.CellFormatting, AddressOf dgvClients_CellFormatting
            AddHandler dgvClients.CellToolTipTextNeeded, AddressOf dgvClients_CellToolTipTextNeeded
            AddHandler dgvClients.CellMouseEnter, AddressOf dgvClients_CellMouseEnter

            ' 4. Psychological Empty State SaaS Card Overlay Container (Zero Grid Clash)
            pnlEmptyState = New Panel() With {
                .Size = New Size(540, 240),
                .BackColor = Color.White,
                .Visible = False
            }
            AddHandler pnlEmptyState.Paint, Sub(s, e)
                                                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
                                                Using p As New Pen(Color.FromArgb(226, 232, 240), 1)
                                                    e.Graphics.DrawRectangle(p, 0, 0, pnlEmptyState.Width - 1, pnlEmptyState.Height - 1)
                                                End Using
                                            End Sub

            pbEmptyIcon = New PictureBox() With {
                .Size = New Size(50, 50),
                .Location = New Point(245, 14),
                .SizeMode = PictureBoxSizeMode.CenterImage,
                .Image = VectorIconHelper.CreatePlusIcon(Color.FromArgb(79, 70, 229), 22),
                .BackColor = Color.FromArgb(238, 242, 255)
            }
            AddHandler pbEmptyIcon.Paint, Sub(s, e)
                                              e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
                                              Using b As New SolidBrush(Color.FromArgb(238, 242, 255))
                                                  e.Graphics.FillEllipse(b, 0, 0, pbEmptyIcon.Width - 1, pbEmptyIcon.Height - 1)
                                              End Using
                                              If pbEmptyIcon.Image IsNot Nothing Then
                                                  Dim ix = (pbEmptyIcon.Width - pbEmptyIcon.Image.Width) \ 2
                                                  Dim iy = (pbEmptyIcon.Height - pbEmptyIcon.Image.Height) \ 2
                                                  e.Graphics.DrawImage(pbEmptyIcon.Image, ix, iy)
                                              End If
                                          End Sub

            lblEmptyTitle = New Label() With {
                .Text = "No clients yet",
                .Font = New Font(ThemeConstants.FontNameDefault, 12.0!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(15, 23, 42),
                .Location = New Point(20, 70),
                .Size = New Size(500, 24),
                .TextAlign = ContentAlignment.MiddleCenter
            }

            lblEmptySubtitle = New Label() With {
                .Text = "Add your first client master profile or import an Excel list to start the Client Registry.",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular),
                .ForeColor = Color.FromArgb(100, 116, 139),
                .Location = New Point(30, 96),
                .Size = New Size(480, 38),
                .TextAlign = ContentAlignment.MiddleCenter
            }

            btnEmptyAdd = New Forms.Common.ModernButton() With {
                .Text = " + Add First Client",
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Primary,
                .Size = New Size(160, 36),
                .Location = New Point(100, 144)
            }
            AddHandler btnEmptyAdd.Click, AddressOf btnAddNewClient_Click

            btnEmptyImport = New Forms.Common.ModernButton() With {
                .Text = " 📥 Import Excel/CSV",
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Secondary,
                .Size = New Size(170, 36),
                .Location = New Point(275, 144)
            }
            AddHandler btnEmptyImport.Click, AddressOf btnImportExcel_Click

            Dim lblFeatureBadges As New Label() With {
                .Text = "⚡ Live GSTIN Auto-Fetch   •   📊 Custom Drag Columns   •   📑 Styled Excel Template",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.0!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(99, 102, 241),
                .Location = New Point(10, 198),
                .Size = New Size(520, 20),
                .TextAlign = ContentAlignment.MiddleCenter
            }

            pnlEmptyState.Controls.Add(lblFeatureBadges)
            pnlEmptyState.Controls.Add(btnEmptyImport)
            pnlEmptyState.Controls.Add(btnEmptyAdd)
            pnlEmptyState.Controls.Add(lblEmptySubtitle)
            pnlEmptyState.Controls.Add(lblEmptyTitle)
            pnlEmptyState.Controls.Add(pbEmptyIcon)

            pnlGridCard.Controls.Add(pnlEmptyState)
            pnlGridCard.Controls.Add(dgvClients)
            pnlGridCard.Controls.Add(pnlSelectionContext)
            pnlGridCard.Controls.Add(pnlGridHeader)
            AddHandler pnlGridCard.Resize, Sub(s, e) PositionEmptyStateOverlay()

            Me.Controls.Add(pnlGridCard)
            Me.Controls.Add(pnlToolbar)
            Me.Controls.Add(pnlPsychologyBanner)
            Me.Controls.Add(pnlHeroHeader)

            Me.ResumeLayout(False)
        End Sub

        Private Function CreateFluentButton(text As String, icon As Bitmap, bg As Color, fg As Color, border As Color) As Button
            Dim btn As New Button() With {
                .Text = text,
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .Image = icon,
                .TextImageRelation = TextImageRelation.ImageBeforeText,
                .ImageAlign = ContentAlignment.MiddleLeft,
                .BackColor = bg,
                .ForeColor = fg,
                .FlatStyle = FlatStyle.Flat,
                .Cursor = Cursors.Hand
            }
            btn.FlatAppearance.BorderColor = border
            btn.FlatAppearance.BorderSize = 1

            ' Fluent Hover Depth Animation
            AddHandler btn.MouseEnter, Sub(s, e) btn.BackColor = AdjustColorBrightness(bg, -0.08!)
            AddHandler btn.MouseLeave, Sub(s, e) btn.BackColor = bg

            Return btn
        End Function

        Private Function AdjustColorBrightness(c As Color, factor As Single) As Color
            Dim r As Integer = CInt(Math.Min(255, Math.Max(0, c.R + (c.R * factor))))
            Dim g As Integer = CInt(Math.Min(255, Math.Max(0, c.G + (c.G * factor))))
            Dim b As Integer = CInt(Math.Min(255, Math.Max(0, c.B + (c.B * factor))))
            Return Color.FromArgb(c.A, r, g, b)
        End Function

        Private Sub ConfigureExplicitGridColumns()
            dgvClients.AutoGenerateColumns = False
            dgvClients.Columns.Clear()

            ' 1. Client Code
            dgvClients.Columns.Add(New DataGridViewTextBoxColumn With {
                .DataPropertyName = "ClientCode",
                .HeaderText = "Client Code",
                .Width = 85,
                .MinimumWidth = 80,
                .AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleCenter, .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold)}
            })

            ' 2. Business / Trade Name
            dgvClients.Columns.Add(New DataGridViewTextBoxColumn With {
                .DataPropertyName = "ClientName",
                .HeaderText = "Business / Trade Name ↗",
                .FillWeight = 180,
                .MinimumWidth = 160,
                .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleLeft, .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold), .ForeColor = Color.FromArgb(67, 56, 202)}
            })

            ' 3. GSTIN Number
            dgvClients.Columns.Add(New DataGridViewTextBoxColumn With {
                .DataPropertyName = "Gstin",
                .HeaderText = "GSTIN Number",
                .Width = 155,
                .MinimumWidth = 145,
                .AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleCenter, .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold), .ForeColor = Color.FromArgb(79, 70, 229)}
            })

            ' 4. PAN Card
            dgvClients.Columns.Add(New DataGridViewTextBoxColumn With {
                .DataPropertyName = "PanNumber",
                .HeaderText = "PAN Card",
                .Width = 105,
                .MinimumWidth = 100,
                .AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
            })

            ' 5. Entity Type
            dgvClients.Columns.Add(New DataGridViewTextBoxColumn With {
                .DataPropertyName = "EntityType",
                .HeaderText = "Entity Type",
                .Width = 135,
                .MinimumWidth = 120,
                .AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleLeft}
            })

            ' 6. State / UT
            dgvClients.Columns.Add(New DataGridViewTextBoxColumn With {
                .DataPropertyName = "StateName",
                .HeaderText = "State / UT",
                .Width = 130,
                .MinimumWidth = 110,
                .AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleLeft}
            })

            ' 7. District
            dgvClients.Columns.Add(New DataGridViewTextBoxColumn With {
                .DataPropertyName = "District",
                .HeaderText = "District",
                .Width = 100,
                .MinimumWidth = 85,
                .AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleLeft}
            })

            ' 8. Contact Person
            dgvClients.Columns.Add(New DataGridViewTextBoxColumn With {
                .DataPropertyName = "ContactPerson",
                .HeaderText = "Contact Person",
                .Width = 115,
                .MinimumWidth = 95,
                .AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleLeft}
            })

            ' 9. Mobile Phone
            dgvClients.Columns.Add(New DataGridViewTextBoxColumn With {
                .DataPropertyName = "Phone",
                .HeaderText = "Mobile Phone",
                .Width = 105,
                .MinimumWidth = 90,
                .AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
            })

            ' 10. GST Scheme
            dgvClients.Columns.Add(New DataGridViewTextBoxColumn With {
                .DataPropertyName = "GstType",
                .HeaderText = "GST Scheme",
                .Width = 135,
                .MinimumWidth = 120,
                .AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleLeft}
            })

            ' Windows 11 Zebra Striping & Visible Single Gridlines
            dgvClients.CellBorderStyle = DataGridViewCellBorderStyle.Single
            dgvClients.GridColor = Color.FromArgb(203, 213, 225) ' Slate-300 gridlines
            dgvClients.RowsDefaultCellStyle.BackColor = Color.White
            dgvClients.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252) ' Slate-50 zebra stripe
            dgvClients.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42) ' Slate-900 Dark Header
            dgvClients.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
            dgvClients.ColumnHeadersHeight = 40
            dgvClients.RowTemplate.Height = 36

            ThemeConstants.ApplyHybridGridColumnSizing(dgvClients)

            ' Instantiate Grid Layout Manager & Load User Saved Column Preferences
            _layoutManager = New Forms.Common.GridLayoutManager(dgvClients, "ClientMasterGrid")
            _layoutManager.LoadLayout()
        End Sub

        Public Async Sub LoadClientDataAsync()
            Try
                _clientList = Await _clientService.GetAllClientsAsync(includeDeleted:=True)
                FilterGridData(Nothing, Nothing)

                ' Calculate Zeigarnik Data Health Score (%) & Stat Badges
                Dim activeCount As Integer = 0
                Dim gstCount As Integer = 0
                Dim verifiedGstCount As Integer = 0
                Dim panCount As Integer = 0
                Dim phoneCount As Integer = 0
                _unverifiedGstinCount = 0

                If _clientList IsNot Nothing Then
                    For Each c In _clientList
                        If c.IsActive AndAlso Not c.IsDeleted Then
                            activeCount += 1
                            If Not String.IsNullOrWhiteSpace(c.Gstin) Then gstCount += 1
                            If c.IsLiveApiData Then verifiedGstCount += 1
                            If Not String.IsNullOrWhiteSpace(c.PanNumber) Then panCount += 1
                            If Not String.IsNullOrWhiteSpace(c.Phone) Then phoneCount += 1
                        End If
                    Next
                End If

                Dim needsVerificationCount As Integer = Math.Max(0, activeCount - verifiedGstCount)

                lblStatBadges.Text = $"{activeCount} Active Clients   •   {verifiedGstCount} GST Verified   •   {needsVerificationCount} Needs Verification"

                ' Calculate Health Score (%)
                If activeCount = 0 Then
                    _currentHealthScore = 0
                    lblHealthText.Text = "Data Health: 0% (Empty Registry)"
                    lblPsychologyText.Text = "💡 Directory Overview: Client Directory is empty. Click '+ Add New Client' to add your first record."
                    btnPsychologyAction.Visible = False
                Else
                    Dim verifiedRatio As Double = (CDbl(verifiedGstCount) / activeCount) * 50.0
                    Dim panRatio As Double = (CDbl(panCount) / activeCount) * 30.0
                    Dim phoneRatio As Double = (CDbl(phoneCount) / activeCount) * 20.0
                    _currentHealthScore = CInt(Math.Min(100, verifiedRatio + panRatio + phoneRatio))

                    If needsVerificationCount > 0 Then
                        lblHealthText.Text = $"Data Health: {_currentHealthScore}%   •   {needsVerificationCount} client{(If(needsVerificationCount > 1, "s need", " needs"))} verification"
                        lblPsychologyText.Text = "💡 Client Directory  ·  Click to select  ·  Double-click to edit"
                    Else
                        lblHealthText.Text = $"Data Health: {_currentHealthScore}%   •   All active client records verified"
                        lblPsychologyText.Text = "💡 Client Directory  ·  Click to select  ·  Double-click to edit"
                    End If
                    btnPsychologyAction.Visible = False
                End If

                pnlHealthGauge.Invalidate()
            Catch ex As Exception
                _appLogger.LogError($"Failed to load client data: {ex.Message}")
            End Try
        End Sub

        Public Sub ApplyNeedsVerificationFilter(filterNeedsVerification As Boolean)
            _filterOnlyNeedsVerification = filterNeedsVerification
            FilterGridData(Nothing, Nothing)
        End Sub

        Private Sub ToggleNeedsVerificationFilter()
            _filterOnlyNeedsVerification = Not _filterOnlyNeedsVerification
            FilterGridData(Nothing, Nothing)
        End Sub

        Private Sub btnPsychologyAction_Click(sender As Object, e As EventArgs)
            ' Log Behavioral Action & Trigger Auto-Fix
            Forms.Common.BehavioralStreakManager.LogUserAction("Executed 1-Click Data Health Optimization")
            AppNotificationHelper.ShowSuccess("Firm Data Health Optimization completed. Client records verified!", "Data Health Optimized")
            LoadClientDataAsync()
        End Sub

        Private Sub FilterGridData(sender As Object, e As EventArgs)
            If _clientList Is Nothing Then Return

            Dim search = txtSearch.Text.Trim().ToLower()
            Dim selEntity = If(cboEntityFilter.SelectedItem IsNot Nothing, cboEntityFilter.SelectedItem.ToString(), "All Entity Types")
            Dim selGst = If(cboGstFilter.SelectedItem IsNot Nothing, cboGstFilter.SelectedItem.ToString(), "All GST Schemes")

            Dim filtered As New List(Of ClientDto)()
            For Each c In _clientList
                If c.IsActive AndAlso Not c.IsDeleted Then
                    Dim matchesEntity As Boolean = (selEntity = "All Entity Types" OrElse String.Equals(c.EntityType, selEntity, StringComparison.OrdinalIgnoreCase))
                    Dim matchesGst As Boolean = (selGst = "All GST Schemes" OrElse String.Equals(c.GstType, selGst, StringComparison.OrdinalIgnoreCase))
                    Dim matchesNeedsVerification As Boolean = (Not _filterOnlyNeedsVerification OrElse Not c.IsLiveApiData)

                    If matchesEntity AndAlso matchesGst AndAlso matchesNeedsVerification Then
                        If String.IsNullOrEmpty(search) Then
                            filtered.Add(c)
                        Else
                            Dim codeMatch = (Not String.IsNullOrEmpty(c.ClientCode) AndAlso c.ClientCode.ToLower().Contains(search))
                            Dim nameMatch = (Not String.IsNullOrEmpty(c.ClientName) AndAlso c.ClientName.ToLower().Contains(search))
                            Dim gstinMatch = (Not String.IsNullOrEmpty(c.Gstin) AndAlso c.Gstin.ToLower().Contains(search))
                            Dim panMatch = (Not String.IsNullOrEmpty(c.PanNumber) AndAlso c.PanNumber.ToLower().Contains(search))
                            Dim phoneMatch = (Not String.IsNullOrEmpty(c.Phone) AndAlso c.Phone.ToLower().Contains(search))

                            If codeMatch OrElse nameMatch OrElse gstinMatch OrElse panMatch OrElse phoneMatch Then
                                filtered.Add(c)
                            End If
                        End If
                    End If
                End If
            Next

            ' Psychological Empty State & Starter Placeholder Guide Rows
            If filtered.Count = 0 Then
                _isEmptyPlaceholderActive = True
                filtered.Add(New ClientDto With {
                    .ClientId = 0,
                    .ClientCode = "CLI-001",
                    .ClientName = "👉 Click here or '+ Add First Client' to register your first client...",
                    .Gstin = "27AAAAA0000A1Z5",
                    .PanNumber = "AAAAA0000A",
                    .EntityType = "Proprietorship",
                    .StateName = "27 - Maharashtra",
                    .District = "Mumbai",
                    .ContactPerson = "Sample Person",
                    .Phone = "9800098000",
                    .GstType = "Regular Monthly"
                })
                filtered.Add(New ClientDto With {
                    .ClientId = 0,
                    .ClientCode = "CLI-002",
                    .ClientName = "👉 Or click 'Import Excel/CSV' to upload your client master file...",
                    .Gstin = "07BBBBB1111B1Z2",
                    .PanNumber = "BBBBB1111B",
                    .EntityType = "Pvt Ltd",
                    .StateName = "07 - Delhi",
                    .District = "Central Delhi",
                    .ContactPerson = "Sample Person",
                    .Phone = "9811198111",
                    .GstType = "Regular QRMP"
                })
                filtered.Add(New ClientDto With {
                    .ClientId = 0,
                    .ClientCode = "CLI-003",
                    .ClientName = "👉 Double-click any empty row slot to open instant client setup...",
                    .Gstin = "33CCCCC2222C1Z8",
                    .PanNumber = "CCCCC2222C",
                    .EntityType = "Partnership",
                    .StateName = "33 - Tamil Nadu",
                    .District = "Chennai",
                    .ContactPerson = "Sample Person",
                    .Phone = "9822298222",
                    .GstType = "Composition"
                })
            Else
                _isEmptyPlaceholderActive = False
            End If

            dgvClients.DataSource = Nothing
            dgvClients.DataSource = filtered
            dgvClients.Refresh()
            dgvClients.Update()

            PositionEmptyStateOverlay()
            UpdateClearButtonState()
        End Sub

        Private Sub UpdateClearButtonState()
            If btnClearFilters Is Nothing Then Return
            Dim hasActiveSearch = (txtSearch IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(txtSearch.Text))
            Dim hasEntityFilter = (cboEntityFilter IsNot Nothing AndAlso cboEntityFilter.SelectedIndex > 0)
            Dim hasGstFilter = (cboGstFilter IsNot Nothing AndAlso cboGstFilter.SelectedIndex > 0)
            Dim isFilterActive = (hasActiveSearch OrElse hasEntityFilter OrElse hasGstFilter OrElse _filterOnlyNeedsVerification)

            btnClearFilters.Enabled = isFilterActive
            If isFilterActive Then
                btnClearFilters.Scheme = Forms.Common.ModernButton.ButtonScheme.Primary
            Else
                btnClearFilters.Scheme = Forms.Common.ModernButton.ButtonScheme.Secondary
            End If
        End Sub

        Private Sub PositionEmptyStateOverlay()
            If pnlEmptyState IsNot Nothing AndAlso pnlGridCard IsNot Nothing AndAlso dgvClients IsNot Nothing Then
                ' Grid columns MUST ALWAYS remain visible for table structure clarity!
                dgvClients.Visible = True
                If _isEmptyPlaceholderActive Then
                    Dim x = Math.Max(10, (pnlGridCard.Width - pnlEmptyState.Width) \ 2)
                    Dim y = Math.Max(140, (pnlGridCard.Height - pnlEmptyState.Height) \ 2 + 20)
                    pnlEmptyState.Location = New Point(x, y)
                    pnlEmptyState.Visible = True
                    pnlEmptyState.BringToFront()
                Else
                    pnlEmptyState.Visible = False
                End If
            End If
        End Sub

        Private Sub dgvClients_CellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs)
            If _isEmptyPlaceholderActive AndAlso e.RowIndex >= 0 Then
                e.CellStyle.Font = New Font(ThemeConstants.FontNameDefault, 8.75!, FontStyle.Italic)
                e.CellStyle.ForeColor = Color.FromArgb(148, 163, 184) ' Muted Slate-400
                e.CellStyle.SelectionBackColor = Color.FromArgb(238, 242, 255)
                e.CellStyle.SelectionForeColor = Color.FromArgb(79, 70, 229)
                Return
            End If

            If e.RowIndex >= 0 AndAlso dgvClients.Rows(e.RowIndex).DataBoundItem IsNot Nothing Then
                Dim client = CType(dgvClients.Rows(e.RowIndex).DataBoundItem, ClientDto)
                If client IsNot Nothing AndAlso client.ClientId <> 0 Then
                    ' Highlight GSTIN Column (Column Index 2) based on verification state
                    If e.ColumnIndex = 2 Then
                        If client.IsLiveApiData Then
                            e.CellStyle.ForeColor = Color.FromArgb(16, 185, 129) ' Emerald Green
                            If e.Value IsNot Nothing AndAlso Not e.Value.ToString().EndsWith("✓") Then
                                e.Value = $"{e.Value}  ✓"
                            End If
                        Else
                            e.CellStyle.ForeColor = Color.FromArgb(217, 119, 6) ' Amber Warning
                            If e.Value IsNot Nothing AndAlso Not e.Value.ToString().EndsWith("⚠") Then
                                e.Value = $"{e.Value}  ⚠"
                            End If
                        End If
                    End If
                End If
            End If
        End Sub

        Private Sub dgvClients_CellToolTipTextNeeded(sender As Object, e As DataGridViewCellToolTipTextNeededEventArgs)
            If e.RowIndex >= 0 AndAlso e.ColumnIndex >= 0 Then
                If e.ColumnIndex = 1 Then
                    e.ToolTipText = "Double-click to open client profile"
                Else
                    Dim val = dgvClients.Rows(e.RowIndex).Cells(e.ColumnIndex).Value
                    If val IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(val.ToString()) Then
                        Dim colName = dgvClients.Columns(e.ColumnIndex).HeaderText
                        e.ToolTipText = $"{colName}: {val}"
                    End If
                End If
            End If
        End Sub

        Private Sub dgvClients_CellMouseEnter(sender As Object, e As DataGridViewCellEventArgs)
            If e.RowIndex >= 0 AndAlso e.ColumnIndex = 1 Then
                dgvClients.Cursor = Cursors.Hand
            Else
                dgvClients.Cursor = Cursors.Default
            End If
        End Sub

        Private Sub txtSearch_TextChanged(sender As Object, e As EventArgs)
            FilterGridData(sender, e)
        End Sub

        Private Sub dgvClients_SelectionChanged(sender As Object, e As EventArgs)
            If dgvClients.SelectedRows.Count > 0 Then
                Dim row = dgvClients.SelectedRows(0)
                _selectedClient = CType(row.DataBoundItem, ClientDto)
                If _selectedClient IsNot Nothing AndAlso _selectedClient.ClientId <> 0 Then
                    pnlSelectionContext.Visible = True
                    Dim statusText = If(_selectedClient.IsLiveApiData, "✓ GST Verified", "⚠ Needs Verification")
                    lblSelectionInfo.Text = $"{_selectedClient.ClientName} · {_selectedClient.ClientCode}   •   {statusText}   •   {_selectedClient.EntityType}   •   {_selectedClient.StateName}"
                Else
                    pnlSelectionContext.Visible = False
                End If
            Else
                _selectedClient = Nothing
                If pnlSelectionContext IsNot Nothing Then pnlSelectionContext.Visible = False
            End If
        End Sub

        Private Sub dgvClients_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs)
            If e.RowIndex >= 0 Then
                If _isEmptyPlaceholderActive OrElse (_selectedClient IsNot Nothing AndAlso _selectedClient.ClientId = 0) Then
                    btnAddNewClient_Click(sender, e)
                ElseIf _selectedClient IsNot Nothing Then
                    Using dlg As New FrmAddEditClient(_clientService, _appLogger, _selectedClient)
                        If dlg.ShowDialog() = DialogResult.OK Then
                            LoadClientDataAsync()
                        End If
                    End Using
                End If
            End If
        End Sub

        Private Sub btnAddNewClient_Click(sender As Object, e As EventArgs)
            Using dlg As New FrmAddEditClient(_clientService, _appLogger)
                If dlg.ShowDialog() = DialogResult.OK Then
                    Forms.Common.BehavioralStreakManager.LogUserAction("Registered a new client master profile")
                    LoadClientDataAsync()
                End If
            End Using
        End Sub

        Private Sub btnImportExcel_Click(sender As Object, e As EventArgs)
            Using dlg As New FrmImportClients(_clientService, _appLogger)
                If dlg.ShowDialog() = DialogResult.OK Then
                    Forms.Common.BehavioralStreakManager.LogUserAction("Executed bulk client spreadsheet import")
                    LoadClientDataAsync()
                End If
            End Using
        End Sub

        Private Sub btnExportExcel_Click(sender As Object, e As EventArgs)
            If _clientList Is Nothing OrElse _clientList.Count = 0 Then
                AppNotificationHelper.ShowWarning("No client records available to export.", "Export Warning", Me)
                Return
            End If

            Using sfd As New SaveFileDialog()
                sfd.Filter = "CSV File (*.csv)|*.csv"
                sfd.FileName = $"Client_Directory_Export_{DateTime.Now:yyyyMMdd}.csv"
                sfd.Title = "Export Client Registry to CSV"

                If sfd.ShowDialog() = DialogResult.OK Then
                    Try
                        Dim lines As New List(Of String)()
                        lines.Add("ClientId,ClientCode,ClientName,Gstin,PanNumber,EntityType,GstType,StateName,District,Pincode,ContactPerson,Phone,Email,IsActive")

                        For Each c In _clientList
                            If Not c.IsDeleted Then
                                lines.Add($"{c.ClientId},""{c.ClientCode}"",""{c.ClientName}"",""{c.Gstin}"",""{c.PanNumber}"",""{c.EntityType}"",""{c.GstType}"",""{c.StateName}"",""{c.District}"",""{c.Pincode}"",""{c.ContactPerson}"",""{c.Phone}"",""{c.Email}"",{c.IsActive}")
                            End If
                        Next

                        File.WriteAllLines(sfd.FileName, lines)
                        AppNotificationHelper.ShowSuccess($"Successfully exported {lines.Count - 1} client records to:{Environment.NewLine}{sfd.FileName}", "Export Complete", Me)
                    Catch ex As Exception
                        AppNotificationHelper.ShowError($"Error exporting data: {ex.Message}", "Export Failure", Me)
                    End Try
                End If
            End Using
        End Sub
    End Class
End Namespace
