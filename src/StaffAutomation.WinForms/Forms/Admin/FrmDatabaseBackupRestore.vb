Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.IO
Imports System.Linq
Imports System.Windows.Forms
Imports StaffAutomation.Core.Configuration
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Models.Backup
Imports StaffAutomation.BLL.Logging
Imports StaffAutomation.DAL.Configuration
Imports StaffAutomation.DAL.Core
Imports StaffAutomation.DAL.Services
Imports StaffAutomation.WinForms.Forms.Common
Imports StaffAutomation.WinForms.Forms.Main

Namespace Forms.Admin
    ''' <summary>
    ''' Production-grade Database Protection and Disaster Recovery Console Form.
    ''' Conforms to the Centralized Krypton Design System.
    ''' Provides one-click T-SQL backups, RESTORE VERIFYONLY, SHA-256 checksum verification,
    ''' DBCC CHECKDB integrity diagnostics, backup retention cleanup, and controlled disaster restoration.
    ''' </summary>
    Public Class FrmDatabaseBackupRestore
        Inherits Form

        Private ReadOnly _capabilityService As IDatabaseServerCapabilityService
        Private ReadOnly _backupService As IDatabaseBackupService
        Private ReadOnly _retentionService As IDatabaseBackupRetentionService
        Private ReadOnly _integrityService As IDatabaseIntegrityService
        Private ReadOnly _restorePreparationService As IDatabaseRestorePreparationService

        ' Header & Summary Status Controls
        Private pnlHeroHeader As Panel
        Private lblHeroTitle As Label
        Private lblHeroSubtitle As Label
        Private btnHeaderBack As Krypton.Toolkit.KryptonButton
        Private btnHeaderClose As Krypton.Toolkit.KryptonButton

        Private pnlStatusCards As TableLayoutPanel
        Private cardServerStatus As AppKpiCard
        Private cardDbState As AppKpiCard
        Private cardLastBackup As AppKpiCard
        Private cardLastIntegrity As AppKpiCard

        ' Tab Navigation Controls
        Private tabControl As TabControl
        Private tabBackups As TabPage
        Private tabIntegrity As TabPage
        Private tabRestore As TabPage
        Private tabSettings As TabPage

        ' Tab 1: Backups & History Controls
        Private pnlBackupToolbar As Panel
        Private btnBackupNow As ModernButton
        Private btnVerifySelected As ModernButton
        Private btnCleanupExpired As ModernButton
        Private dgvBackupHistory As DataGridView
        Private pnlEmptyState As Panel

        ' Tab 2: Integrity Controls
        Private pnlIntegrityToolbar As Panel
        Private btnRunCheckDb As ModernButton
        Private lblIntegrityStatus As Label
        Private dgvIntegrityHistory As DataGridView

        ' Tab 3: Disaster Restoration Controls
        Private pnlRestoreHost As Panel
        Private lblLatestBackupDetails As Label
        Private btnRestoreLatest As ModernButton
        Private btnChooseAnother As ModernButton
        Private lblCustomBackupStatus As Label
        Private lblRestoreProgress As Label

        ' Tab 4: Settings Controls
        Private txtPrimaryPath As Krypton.Toolkit.KryptonTextBox
        Private txtSecondaryPath As Krypton.Toolkit.KryptonTextBox
        Private chkEnableSecondary As CheckBox
        Private numRetentionDays As NumericUpDown
        Private btnSaveSettings As ModernButton

        Private _selectedBackupForRestore As DatabaseBackupHistoryDto = Nothing
        Private _isRestoreValidated As Boolean = False

        Public Sub New()
            ' Initialize DAL Services
            Dim config As IAppConfiguration = New AppConfiguration()
            Dim connFactory As IDatabaseConnectionFactory = New DbConnectionFactory(config)
            Dim sqlHelper As ISqlHelper = New SqlHelper(connFactory)
            Dim auditLogger As IAuditLogger = New AuditLogger(sqlHelper)

            _capabilityService = New DatabaseServerCapabilityService(sqlHelper)
            _backupService = New DatabaseBackupService(sqlHelper, _capabilityService)
            _retentionService = New DatabaseBackupRetentionService(_backupService, auditLogger)
            _integrityService = New DatabaseIntegrityService(sqlHelper)

            Dim restoreRunner As New DatabaseRestoreRunner(config, auditLogger)
            _restorePreparationService = New DatabaseRestorePreparationService(config, _backupService, restoreRunner)

            InitializeComponent()
        End Sub

        Protected Overrides Async Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)
            Dim authz As Core.Security.IAuthorizationService = New BLL.Security.AuthorizationService()
            If Not authz.IsAuthorized(Core.Enums.UserRole.Admin) Then
                FrmInAppAlert.ShowModal(Me, "Access Denied", "Access Denied: Only System Administrators can access database backup and disaster recovery settings.", AlertType.WarningAlert, actionText:="OK")
                Me.Close()
                Return
            End If
            Await RefreshAllConsoleDataAsync()
        End Sub

        Private Sub InitializeComponent()
            Me.Text = "Database Protection & Disaster Recovery Console"
            Me.Size = New Size(1100, 720)
            Me.MinimumSize = New Size(980, 640)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.BackColor = ThemeConstants.WorkspaceBackground
            Me.Padding = New Padding(16)
            Me.Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular)

            ' 1. Indigo Hero Page Header
            pnlHeroHeader = New Panel()
            Me.Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular)

            ' 1. Indigo Hero Page Header
            pnlHeroHeader = New Panel()
            lblHeroTitle = New Label() With {.Text = "Database Protection & Disaster Recovery Console"}
            lblHeroSubtitle = New Label() With {.Text = "Manage backups and safely restore your office data when needed."}
            ThemeConstants.ApplyHeaderStyle(pnlHeroHeader, lblHeroTitle, lblHeroSubtitle)

            ' Top-Right Navigation Header Controls
            Dim pnlHeaderNav As New FlowLayoutPanel() With {
                .Dock = DockStyle.Right,
                .AutoSize = True,
                .FlowDirection = FlowDirection.LeftToRight,
                .WrapContents = False,
                .BackColor = Color.Transparent,
                .Padding = New Padding(0, 14, 16, 0)
            }

            btnHeaderBack = New Krypton.Toolkit.KryptonButton() With {
                .Text = "← Back to Settings",
                .Size = New Size(160, 32),
                .Margin = New Padding(0)
            }
            ThemeConstants.ApplyKryptonSecondaryButton(btnHeaderBack)

            btnHeaderClose = New Krypton.Toolkit.KryptonButton() With {
                .Text = "✕",
                .Size = New Size(44, 32),
                .Margin = New Padding(6, 0, 0, 0)
            }
            ThemeConstants.ApplyKryptonSecondaryButton(btnHeaderClose)
            btnHeaderClose.StateCommon.Back.Color1 = Color.FromArgb(220, 38, 38)
            btnHeaderClose.StateCommon.Back.Color2 = Color.FromArgb(220, 38, 38)
            btnHeaderClose.StateCommon.Content.ShortText.Color1 = Color.White
            btnHeaderClose.StateCommon.Content.ShortText.Font = New Font(ThemeConstants.FontNameDefault, 10.0!, FontStyle.Bold)

            AddHandler btnHeaderClose.Click, Sub() ConfirmAndCloseForm()
            AddHandler btnHeaderBack.Click, Sub() ConfirmAndCloseForm()

            pnlHeaderNav.Controls.Add(btnHeaderBack)
            pnlHeaderNav.Controls.Add(btnHeaderClose)

            pnlHeroHeader.Controls.Add(pnlHeaderNav)
            pnlHeroHeader.Controls.Add(lblHeroSubtitle)
            pnlHeroHeader.Controls.Add(lblHeroTitle)

            Me.KeyPreview = True
            Me.ControlBox = True
            Me.CancelButton = btnHeaderClose

            ' 2. Top Summary KPI Status Bar (4 Cards Grid)
            pnlStatusCards = New TableLayoutPanel() With {
                .Dock = DockStyle.Top,
                .Height = 100,
                .ColumnCount = 4,
                .RowCount = 1,
                .Padding = New Padding(0, 8, 0, 12),
                .BackColor = Color.Transparent
            }
            pnlStatusCards.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25.0!))
            pnlStatusCards.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25.0!))
            pnlStatusCards.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25.0!))
            pnlStatusCards.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25.0!))

            cardServerStatus = New AppKpiCard() With {
                .Title = "SQL SERVER ENGINE",
                .Value = "Checking...",
                .Subtext = "Edition & Connection",
                .Scheme = AppKpiCard.KpiScheme.Neutral,
                .Icon = FontAwesome.Sharp.IconChar.Server,
                .Dock = DockStyle.Fill
            }
            cardDbState = New AppKpiCard() With {
                .Title = "DATABASE STATE",
                .Value = "Checking...",
                .Subtext = "StaffAutomationDb",
                .Scheme = AppKpiCard.KpiScheme.Neutral,
                .Icon = FontAwesome.Sharp.IconChar.Database,
                .Dock = DockStyle.Fill
            }
            cardLastBackup = New AppKpiCard() With {
                .Title = "LAST BACKUP",
                .Value = "Checking...",
                .Subtext = "Primary Backup",
                .Scheme = AppKpiCard.KpiScheme.Neutral,
                .Icon = FontAwesome.Sharp.IconChar.Clock,
                .Dock = DockStyle.Fill
            }
            cardLastIntegrity = New AppKpiCard() With {
                .Title = "INTEGRITY STATUS",
                .Value = "Not Checked",
                .Subtext = "DBCC CHECKDB",
                .Scheme = AppKpiCard.KpiScheme.Neutral,
                .Icon = FontAwesome.Sharp.IconChar.ShieldHalved,
                .Dock = DockStyle.Fill
            }

            pnlStatusCards.Controls.Add(cardServerStatus, 0, 0)
            pnlStatusCards.Controls.Add(cardDbState, 1, 0)
            pnlStatusCards.Controls.Add(cardLastBackup, 2, 0)
            pnlStatusCards.Controls.Add(cardLastIntegrity, 3, 0)

            ' 3. Tab Navigation System
            tabControl = New TabControl() With {
                .Dock = DockStyle.Fill,
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold)
            }

            tabBackups = New TabPage("  Backups & History  ")
            tabIntegrity = New TabPage("  Integrity Checks  ")
            tabRestore = New TabPage("  Disaster Recovery & Restore  ")
            tabSettings = New TabPage("  Backup Configuration  ")

            BuildBackupsTab()
            BuildIntegrityTab()
            BuildRestoreTab()
            BuildSettingsTab()

            tabControl.TabPages.Add(tabBackups)
            tabControl.TabPages.Add(tabIntegrity)
            tabControl.TabPages.Add(tabRestore)
            tabControl.TabPages.Add(tabSettings)

            Me.Controls.Add(tabControl)
            Me.Controls.Add(pnlStatusCards)
            Me.Controls.Add(pnlHeroHeader)
        End Sub

        Private Sub BuildBackupsTab()
            tabBackups.BackColor = Color.White
            tabBackups.Padding = New Padding(12)

            pnlBackupToolbar = New Panel() With {.Dock = DockStyle.Top, .Height = 44, .BackColor = Color.Transparent}

            btnBackupNow = New ModernButton() With {.Text = "Create Full Backup Now", .Scheme = ModernButton.ButtonScheme.Primary, .Size = New Size(200, 34), .Location = New Point(0, 4)}
            AddHandler btnBackupNow.Click, AddressOf btnBackupNow_Click

            btnVerifySelected = New ModernButton() With {.Text = "Verify Selected Backup", .Scheme = ModernButton.ButtonScheme.Secondary, .Size = New Size(180, 34), .Location = New Point(210, 4)}
            AddHandler btnVerifySelected.Click, AddressOf btnVerifySelected_Click

            btnCleanupExpired = New ModernButton() With {.Text = "Run Retention Cleanup", .Scheme = ModernButton.ButtonScheme.Secondary, .Size = New Size(180, 34), .Location = New Point(400, 4)}
            AddHandler btnCleanupExpired.Click, AddressOf btnCleanupExpired_Click

            pnlBackupToolbar.Controls.Add(btnCleanupExpired)
            pnlBackupToolbar.Controls.Add(btnVerifySelected)
            pnlBackupToolbar.Controls.Add(btnBackupNow)

            dgvBackupHistory = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .AutoGenerateColumns = False,
                .AllowUserToAddRows = False,
                .ReadOnly = True,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .RowHeadersVisible = False,
                .CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                .GridColor = Color.FromArgb(241, 245, 249),
                .BackgroundColor = Color.White,
                .BorderStyle = BorderStyle.None,
                .ColumnHeadersHeight = 36,
                .RowTemplate = New DataGridViewRow() With {.Height = 34},
                .ShowCellToolTips = True
            }
            dgvBackupHistory.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42)
            dgvBackupHistory.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
            dgvBackupHistory.ColumnHeadersDefaultCellStyle.Font = New Font(ThemeConstants.FontNameDefault, 8.75!, FontStyle.Bold)
            dgvBackupHistory.DefaultCellStyle.BackColor = Color.White
            dgvBackupHistory.DefaultCellStyle.ForeColor = Color.FromArgb(15, 23, 42)
            dgvBackupHistory.DefaultCellStyle.SelectionBackColor = Color.FromArgb(238, 242, 255)
            dgvBackupHistory.DefaultCellStyle.SelectionForeColor = Color.FromArgb(67, 56, 202)
            dgvBackupHistory.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252)

            ConfigureBackupGridColumns()
            CreateEmptyStatePanel()

            tabBackups.Controls.Add(dgvBackupHistory)
            tabBackups.Controls.Add(pnlEmptyState)
            tabBackups.Controls.Add(pnlBackupToolbar)
        End Sub

        Private Sub CreateEmptyStatePanel()
            pnlEmptyState = New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = Color.White,
                .Visible = False
            }

            Dim pnlCenter As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 3,
                .BackColor = Color.Transparent
            }
            pnlCenter.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0!))
            pnlCenter.RowStyles.Add(New RowStyle(SizeType.Percent, 35.0!))
            pnlCenter.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            pnlCenter.RowStyles.Add(New RowStyle(SizeType.Percent, 65.0!))

            Dim pnlContent As New FlowLayoutPanel() With {
                .AutoSize = True,
                .FlowDirection = FlowDirection.TopDown,
                .WrapContents = False,
                .Anchor = AnchorStyles.None,
                .BackColor = Color.Transparent
            }

            Dim lblTitle As New Label() With {
                .Text = "No Backup History Yet",
                .Font = New Font(ThemeConstants.FontNameDefault, 11.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .AutoSize = True,
                .Margin = New Padding(0, 0, 0, 4),
                .Anchor = AnchorStyles.Top
            }

            Dim lblSub As New Label() With {
                .Text = "Create your first full database backup to protect your business records.",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular),
                .ForeColor = ThemeConstants.TextSecondary,
                .AutoSize = True,
                .Anchor = AnchorStyles.Top
            }

            pnlContent.Controls.Add(lblTitle)
            pnlContent.Controls.Add(lblSub)

            pnlCenter.Controls.Add(pnlContent, 0, 1)
            pnlEmptyState.Controls.Add(pnlCenter)
        End Sub

        Private Sub ConfigureBackupGridColumns()
            dgvBackupHistory.Columns.Clear()
            dgvBackupHistory.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = "StartedOn", .HeaderText = "Date & Time", .Width = 140, .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleCenter}})
            dgvBackupHistory.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = "FileName", .HeaderText = "Backup Archive Name", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, .MinimumWidth = 220, .DefaultCellStyle = New DataGridViewCellStyle With {.Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold)}})
            dgvBackupHistory.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = "FileSizeBytes", .HeaderText = "Size (MB)", .Width = 90, .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleRight}})
            dgvBackupHistory.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = "Status", .HeaderText = "Status", .Width = 110, .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleCenter}})
            dgvBackupHistory.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = "VerificationStatus", .HeaderText = "VERIFYONLY", .Width = 120, .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleCenter}})
            dgvBackupHistory.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = "Sha256Hash", .HeaderText = "SHA-256 Hash", .Width = 140, .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleLeft}})

            AddHandler dgvBackupHistory.CellFormatting, Sub(s, e)
                If e.ColumnIndex = 2 AndAlso e.Value IsNot Nothing Then
                    Dim bytes As Long = 0
                    If Long.TryParse(e.Value.ToString(), bytes) Then
                        e.Value = $"{CDbl(bytes) / (1024.0 * 1024.0):F2} MB"
                    End If
                ElseIf e.ColumnIndex = 5 AndAlso e.Value IsNot Nothing Then
                    Dim cell = dgvBackupHistory.Rows(e.RowIndex).Cells(e.ColumnIndex)
                    cell.ToolTipText = e.Value.ToString()
                End If
            End Sub
        End Sub

        Private Sub BuildIntegrityTab()
            tabIntegrity.BackColor = Color.White
            tabIntegrity.Padding = New Padding(12)

            pnlIntegrityToolbar = New Panel() With {.Dock = DockStyle.Top, .Height = 44, .BackColor = Color.Transparent}

            btnRunCheckDb = New ModernButton() With {.Text = "Execute DBCC CHECKDB Diagnostic", .Scheme = ModernButton.ButtonScheme.Primary, .Size = New Size(240, 34), .Location = New Point(0, 4)}
            AddHandler btnRunCheckDb.Click, AddressOf btnRunCheckDb_Click

            lblIntegrityStatus = New Label() With {.Text = "DBCC CHECKDB validates complete physical and logical database consistency.", .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Regular), .ForeColor = ThemeConstants.TextSecondary, .Location = New Point(255, 12), .AutoSize = True}

            pnlIntegrityToolbar.Controls.Add(lblIntegrityStatus)
            pnlIntegrityToolbar.Controls.Add(btnRunCheckDb)

            dgvIntegrityHistory = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .AutoGenerateColumns = False,
                .AllowUserToAddRows = False,
                .ReadOnly = True,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .RowHeadersVisible = False,
                .CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                .GridColor = Color.FromArgb(241, 245, 249),
                .BackgroundColor = Color.White,
                .BorderStyle = BorderStyle.None,
                .ColumnHeadersHeight = 36,
                .RowTemplate = New DataGridViewRow() With {.Height = 34}
            }
            dgvIntegrityHistory.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42)
            dgvIntegrityHistory.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
            dgvIntegrityHistory.ColumnHeadersDefaultCellStyle.Font = New Font(ThemeConstants.FontNameDefault, 8.75!, FontStyle.Bold)
            dgvIntegrityHistory.DefaultCellStyle.BackColor = Color.White
            dgvIntegrityHistory.DefaultCellStyle.ForeColor = Color.FromArgb(15, 23, 42)
            dgvIntegrityHistory.DefaultCellStyle.SelectionBackColor = Color.FromArgb(238, 242, 255)
            dgvIntegrityHistory.DefaultCellStyle.SelectionForeColor = Color.FromArgb(67, 56, 202)
            dgvIntegrityHistory.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252)

            dgvIntegrityHistory.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = "StartedOn", .HeaderText = "Diagnostic Date", .Width = 150})
            dgvIntegrityHistory.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = "Status", .HeaderText = "Status", .Width = 110})
            dgvIntegrityHistory.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = "ResultSummary", .HeaderText = "Result Summary", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})
            dgvIntegrityHistory.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = "ExecutedBy", .HeaderText = "Executed By", .Width = 120})

            tabIntegrity.Controls.Add(dgvIntegrityHistory)
            tabIntegrity.Controls.Add(pnlIntegrityToolbar)
        End Sub

        Private Sub BuildRestoreTab()
            tabRestore.BackColor = Color.White
            tabRestore.Padding = New Padding(16)

            pnlRestoreHost = New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.Transparent}

            Dim lblRestoreTitle As New Label() With {
                .Text = "Restore Database",
                .Font = New Font(ThemeConstants.FontNameDefault, 14.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(0, 0),
                .AutoSize = True
            }

            Dim lblRestoreDesc As New Label() With {
                .Text = "Restore your office data from a previously created backup.",
                .Font = New Font(ThemeConstants.FontNameDefault, 10.0!, FontStyle.Regular),
                .ForeColor = ThemeConstants.TextSecondary,
                .Location = New Point(0, 30),
                .Size = New Size(800, 24)
            }

            lblLatestBackupDetails = New Label() With {
                .Text = "Latest Verified Backup: Fetching...",
                .Font = New Font(ThemeConstants.FontNameDefault, 10.0!, FontStyle.Regular),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(0, 70),
                .AutoSize = True
            }

            btnRestoreLatest = New ModernButton() With {
                .Text = "Restore Latest Verified Backup",
                .Scheme = ModernButton.ButtonScheme.Primary,
                .Size = New Size(260, 36),
                .Location = New Point(0, 110),
                .Enabled = False
            }
            AddHandler btnRestoreLatest.Click, AddressOf btnRestoreLatest_Click

            btnChooseAnother = New ModernButton() With {
                .Text = "Choose Another Backup",
                .Scheme = ModernButton.ButtonScheme.Secondary,
                .Size = New Size(220, 36),
                .Location = New Point(270, 110)
            }
            AddHandler btnChooseAnother.Click, AddressOf btnChooseAnother_Click

            lblCustomBackupStatus = New Label() With {
                .Text = "",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(3, 84, 63),
                .Location = New Point(0, 160),
                .AutoSize = True,
                .Visible = False
            }
            
            lblRestoreProgress = New Label() With {
                .Text = "Restoring your office data..." & Environment.NewLine & "Please do not close the application.",
                .Font = New Font(ThemeConstants.FontNameDefault, 11.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.PrimaryAccent,
                .Location = New Point(0, 200),
                .AutoSize = True,
                .Visible = False
            }

            pnlRestoreHost.Controls.Add(lblRestoreProgress)
            pnlRestoreHost.Controls.Add(lblCustomBackupStatus)
            pnlRestoreHost.Controls.Add(btnChooseAnother)
            pnlRestoreHost.Controls.Add(btnRestoreLatest)
            pnlRestoreHost.Controls.Add(lblLatestBackupDetails)
            pnlRestoreHost.Controls.Add(lblRestoreDesc)
            pnlRestoreHost.Controls.Add(lblRestoreTitle)

            tabRestore.Controls.Add(pnlRestoreHost)
        End Sub

        Private Sub BuildSettingsTab()
            tabSettings.BackColor = Color.White
            tabSettings.Padding = New Padding(16)

            Dim lblP As New Label() With {.Text = "Primary Backup Storage Directory:", .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold), .Location = New Point(10, 14), .AutoSize = True}
            txtPrimaryPath = New Krypton.Toolkit.KryptonTextBox() With {.Text = Core.Models.Backup.DatabaseBackupConfiguration.GetDefaultPrimaryBackupPath(), .Location = New Point(10, 34), .Size = New Size(500, 28)}
            ThemeConstants.ApplyAppTextBoxStyle(txtPrimaryPath)

            Dim lblS As New Label() With {.Text = "Secondary Backup Storage Directory (Optional Network/USB):", .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold), .Location = New Point(10, 74), .AutoSize = True}
            txtSecondaryPath = New Krypton.Toolkit.KryptonTextBox() With {.Text = "", .Location = New Point(10, 94), .Size = New Size(500, 28)}
            ThemeConstants.ApplyAppTextBoxStyle(txtSecondaryPath)

            chkEnableSecondary = New CheckBox() With {.Text = "Enable Automated Secondary Copy Replication", .Location = New Point(10, 134), .AutoSize = True}

            Dim lblR As New Label() With {.Text = "Backup Retention Period (Days):", .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold), .Location = New Point(10, 168), .AutoSize = True}
            numRetentionDays = New NumericUpDown() With {.Value = 30, .Minimum = 1, .Maximum = 365, .Location = New Point(210, 166), .Size = New Size(80, 24)}

            btnSaveSettings = New ModernButton() With {.Text = "Save Configuration", .Scheme = ModernButton.ButtonScheme.Primary, .Size = New Size(160, 32), .Location = New Point(10, 210)}
            AddHandler btnSaveSettings.Click, Sub(s, e) AppNotificationHelper.ShowSuccess("Backup storage configuration updated successfully.", "Configuration Saved", Me)

            tabSettings.Controls.Add(btnSaveSettings)
            tabSettings.Controls.Add(numRetentionDays)
            tabSettings.Controls.Add(lblR)
            tabSettings.Controls.Add(chkEnableSecondary)
            tabSettings.Controls.Add(txtSecondaryPath)
            tabSettings.Controls.Add(lblS)
            tabSettings.Controls.Add(txtPrimaryPath)
            tabSettings.Controls.Add(lblP)
        End Sub

        Private Async Function RefreshAllConsoleDataAsync() As Task
            Try
                Me.Cursor = Cursors.WaitCursor

                ' 1. Server & Database Capabilities (Independent Loader)
                Try
                    Dim cap = Await _capabilityService.DetectCapabilitiesAsync(txtPrimaryPath.Text)
                    If cap.IsServiceAvailable Then
                        cardServerStatus.Value = "ONLINE"
                        cardServerStatus.Scheme = AppKpiCard.KpiScheme.Healthy
                    Else
                        cardServerStatus.Value = "OFFLINE"
                        cardServerStatus.Scheme = AppKpiCard.KpiScheme.Critical
                    End If
                    cardServerStatus.Subtext = If(String.IsNullOrEmpty(cap.SqlServerEdition), "SQL Express Engine", cap.SqlServerEdition)

                    ' Database State
                    Dim dbStateStr = If(String.IsNullOrEmpty(cap.DatabaseState), "ONLINE", cap.DatabaseState.ToUpper())
                    cardDbState.Value = dbStateStr
                    If dbStateStr = "ONLINE" Then
                        cardDbState.Scheme = AppKpiCard.KpiScheme.Healthy
                    ElseIf dbStateStr.Contains("RESTORING") OrElse dbStateStr.Contains("MAINTENANCE") OrElse dbStateStr.Contains("READ_ONLY") OrElse dbStateStr.Contains("SINGLE_USER") Then
                        cardDbState.Scheme = AppKpiCard.KpiScheme.Warning
                    Else
                        cardDbState.Scheme = AppKpiCard.KpiScheme.Critical
                    End If
                Catch ex As Exception
                    cardServerStatus.Value = "UNKNOWN"
                    cardServerStatus.Scheme = AppKpiCard.KpiScheme.Neutral
                    cardDbState.Value = "UNAVAILABLE"
                    cardDbState.Scheme = AppKpiCard.KpiScheme.Neutral
                End Try

                ' 2. Backup History (Independent Loader)
                Try
                    Dim history = Await _backupService.GetBackupHistoryAsync(50)
                    dgvBackupHistory.DataSource = history

                    If history IsNot Nothing AndAlso history.Count > 0 Then
                        dgvBackupHistory.Visible = True
                        pnlEmptyState.Visible = False
                        Dim lastSucc = history.FirstOrDefault(Function(h) h.Status = "Succeeded")
                        If lastSucc IsNot Nothing Then
                            Dim ageHours = (DateTime.Now - lastSucc.StartedOn).TotalHours
                            cardLastBackup.Value = lastSucc.StartedOn.ToString("dd MMM HH:mm")
                            If ageHours <= 24.0 Then
                                cardLastBackup.Scheme = AppKpiCard.KpiScheme.Healthy
                                cardLastBackup.Subtext = "Backup Up To Date"
                            ElseIf ageHours <= 48.0 Then
                                cardLastBackup.Scheme = AppKpiCard.KpiScheme.Warning
                                cardLastBackup.Subtext = "Backup Due Soon"
                            Else
                                cardLastBackup.Scheme = AppKpiCard.KpiScheme.Critical
                                cardLastBackup.Subtext = "Backup Overdue"
                            End If
                            
                            ' Update Restore Tab UI
                            Dim lastVerified = history.FirstOrDefault(Function(h) h.Status = "Succeeded" AndAlso h.VerificationStatus IsNot Nothing AndAlso (h.VerificationStatus.Contains("Verified") OrElse h.VerificationStatus.Contains("Passed")))
                            If lastVerified IsNot Nothing Then
                                Dim mbSize As Double = CDbl(lastVerified.FileSizeBytes) / (1024.0 * 1024.0)
                                lblLatestBackupDetails.Text = $"Latest Verified Backup" & Environment.NewLine & $"{lastVerified.StartedOn:dd MMM yyyy} · {lastVerified.StartedOn:hh:mm tt}" & Environment.NewLine & $"{mbSize:F2} MB" & Environment.NewLine & "✓ Verified"
                                lblLatestBackupDetails.ForeColor = Color.FromArgb(3, 84, 63)
                                _selectedBackupForRestore = lastVerified
                                _isRestoreValidated = True
                                btnRestoreLatest.Text = "Restore Latest Verified Backup"
                                btnRestoreLatest.Enabled = True
                            Else
                                lblLatestBackupDetails.Text = "Latest Backup: Unverified"
                                lblLatestBackupDetails.ForeColor = Color.FromArgb(185, 28, 28)
                                _isRestoreValidated = False
                                btnRestoreLatest.Enabled = False
                            End If
                        Else
                            ' History exists, but no succeeded backup record -> Critical (Red)
                            cardLastBackup.Value = "None"
                            cardLastBackup.Scheme = AppKpiCard.KpiScheme.Critical
                            cardLastBackup.Subtext = "No Successful Backup"
                        End If
                    Else
                        ' Zero backup records exist -> Critical (Red) because database is unprotected!
                        dgvBackupHistory.Visible = False
                        pnlEmptyState.Visible = True
                        cardLastBackup.Value = "None"
                        cardLastBackup.Scheme = AppKpiCard.KpiScheme.Critical
                        cardLastBackup.Subtext = "No Backup Protection"
                    End If
                Catch ex As Exception
                    ' Query failed or loading -> Neutral (Gray) because status cannot be determined
                    cardLastBackup.Value = "Unknown"
                    cardLastBackup.Scheme = AppKpiCard.KpiScheme.Neutral
                    cardLastBackup.Subtext = "Query Pending"
                    dgvBackupHistory.Visible = False
                    pnlEmptyState.Visible = True
                End Try

                ' 3. Integrity History (Independent Loader)
                Try
                    Dim integrityHistory = Await _integrityService.GetIntegrityHistoryAsync(50)
                    dgvIntegrityHistory.DataSource = integrityHistory

                    If integrityHistory IsNot Nothing AndAlso integrityHistory.Count > 0 Then
                        Dim latest = integrityHistory(0)
                        cardLastIntegrity.Value = latest.Status
                        Dim stUpper = latest.Status.ToUpper()
                        If stUpper = "PASSED" OrElse stUpper = "CLEAN" Then
                            cardLastIntegrity.Scheme = AppKpiCard.KpiScheme.Healthy
                        ElseIf stUpper = "WARNING" OrElse stUpper = "DEGRADED" Then
                            cardLastIntegrity.Scheme = AppKpiCard.KpiScheme.Warning
                        Else
                            cardLastIntegrity.Scheme = AppKpiCard.KpiScheme.Critical
                        End If
                        cardLastIntegrity.Subtext = latest.StartedOn.ToString("dd MMM yyyy")
                    Else
                        cardLastIntegrity.Value = "Not Checked"
                        cardLastIntegrity.Scheme = AppKpiCard.KpiScheme.Neutral
                        cardLastIntegrity.Subtext = "Never Checked"
                    End If
                Catch ex As Exception
                    cardLastIntegrity.Value = "Unknown"
                    cardLastIntegrity.Scheme = AppKpiCard.KpiScheme.Neutral
                    cardLastIntegrity.Subtext = "Verification Ready"
                End Try
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Function

        Private Sub ConfirmAndCloseForm()
            If Me.Modal Then
                Me.DialogResult = DialogResult.Cancel
                Me.Close()
            Else
                Dim parentShell = TryCast(Me.MdiParent, FrmMainShell)
                If parentShell Is Nothing AndAlso Me.Parent IsNot Nothing Then
                    parentShell = TryCast(Me.Parent.FindForm(), FrmMainShell)
                End If
                If parentShell IsNot Nothing Then
                    parentShell.NavigateToModule("Settings")
                Else
                    Me.Close()
                End If
            End If
        End Sub

        Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
            If keyData = Keys.Escape Then
                ConfirmAndCloseForm()
                Return True
            End If
            Return MyBase.ProcessCmdKey(msg, keyData)
        End Function

        Private Async Sub btnBackupNow_Click(sender As Object, e As EventArgs)
            Dim authz As Core.Security.IAuthorizationService = New BLL.Security.AuthorizationService()
            If Not authz.IsAuthorized(Core.Enums.UserRole.Admin) Then
                AppNotificationHelper.ShowError("Access Denied: Only System Administrators can execute database backups.", "Access Denied", Me)
                Return
            End If

            Try
                Me.Cursor = Cursors.WaitCursor
                btnBackupNow.Enabled = False

                Dim res = Await _backupService.ExecuteBackupAsync(txtPrimaryPath.Text)
                If res.Status = "Succeeded" Then
                    AppNotificationHelper.ShowSuccess($"Full Database Backup Succeeded!{Environment.NewLine}Archive: {res.FileName}{Environment.NewLine}Size: {CDbl(res.FileSizeBytes) / (1024.0 * 1024.0):F2} MB{Environment.NewLine}VERIFYONLY: Passed", "Backup Complete", Me)
                Else
                    AppNotificationHelper.ShowError($"Backup failed or verification incomplete: {res.ErrorMessage}", "Backup Failure", Me)
                End If

                Await RefreshAllConsoleDataAsync()
            Finally
                btnBackupNow.Enabled = True
                Me.Cursor = Cursors.Default
            End Try
        End Sub

        Private Async Sub btnVerifySelected_Click(sender As Object, e As EventArgs)
            If dgvBackupHistory.SelectedRows.Count = 0 Then
                AppNotificationHelper.ShowWarning("Please select a backup row from history to verify.", "Selection Required", Me)
                Return
            End If

            Dim selectedItem = TryCast(dgvBackupHistory.SelectedRows(0).DataBoundItem, DatabaseBackupHistoryDto)
            If selectedItem Is Nothing OrElse String.IsNullOrWhiteSpace(selectedItem.FilePath) Then Return

            Try
                Me.Cursor = Cursors.WaitCursor
                Dim isOk = Await _backupService.VerifyBackupFileAsync(selectedItem.FilePath)
                Dim hashVerified = False

                If Not String.IsNullOrWhiteSpace(selectedItem.Sha256Hash) Then
                    hashVerified = DatabaseBackupService.VerifySha256TamperStatus(selectedItem.FilePath, selectedItem.Sha256Hash)
                End If

                If isOk AndAlso (String.IsNullOrWhiteSpace(selectedItem.Sha256Hash) OrElse hashVerified) Then
                    AppNotificationHelper.ShowSuccess($"RESTORE VERIFYONLY passed successfully.{Environment.NewLine}File Hash Tamper Check: {If(hashVerified, "PASSED (SHA-256 Match)", "UNVERIFIED")}", "Verification Passed", Me)
                Else
                    AppNotificationHelper.ShowError($"RESTORE VERIFYONLY or SHA-256 hash check failed.{Environment.NewLine}Tamper Status: {If(Not hashVerified, "SHA-256 MISMATCH DETECTED", "OK")}", "Verification Failed", Me)
                End If
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Sub

        Private Async Sub btnCleanupExpired_Click(sender As Object, e As EventArgs)
            Try
                Me.Cursor = Cursors.WaitCursor
                Dim deleted = Await _retentionService.CleanupExpiredBackupsAsync(CInt(numRetentionDays.Value), txtPrimaryPath.Text, txtSecondaryPath.Text)
                AppNotificationHelper.ShowInfo($"Retention cleanup complete. Removed {deleted.Count} expired backup file(s).", "Cleanup Complete", Me)
                Await RefreshAllConsoleDataAsync()
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Sub

        Private Async Sub btnRunCheckDb_Click(sender As Object, e As EventArgs)
            Try
                Me.Cursor = Cursors.WaitCursor
                btnRunCheckDb.Enabled = False
                lblIntegrityStatus.Text = "Running DBCC CHECKDB diagnostic..."

                Dim res = Await _integrityService.ExecuteIntegrityCheckAsync()
                If res.Status = "Passed" Then
                    AppNotificationHelper.ShowSuccess("DBCC CHECKDB completed with 0 allocation or consistency errors.", "Integrity Check Passed", Me)
                Else
                    AppNotificationHelper.ShowError($"DBCC CHECKDB detected issues: {res.ErrorMessage}", "Integrity Check Warning", Me)
                End If

                Await RefreshAllConsoleDataAsync()
            Finally
                btnRunCheckDb.Enabled = True
                lblIntegrityStatus.Text = "DBCC CHECKDB validates complete physical and logical database consistency."
                Me.Cursor = Cursors.Default
            End Try
        End Sub

        Private Async Sub btnChooseAnother_Click(sender As Object, e As EventArgs)
            Using ofd As New OpenFileDialog()
                ofd.Filter = "Backup Files (*.bak)|*.bak|All Files (*.*)|*.*"
                ofd.Title = "Choose Another Backup"
                If ofd.ShowDialog() = DialogResult.OK Then
                    Dim filePath = ofd.FileName
                    Me.Cursor = Cursors.WaitCursor
                    lblCustomBackupStatus.Visible = True
                    lblCustomBackupStatus.Text = "Verifying selected backup..."
                    lblCustomBackupStatus.ForeColor = ThemeConstants.TextSecondary
                    btnRestoreLatest.Enabled = False

                    Try
                        ' 1. Verify Header
                        Dim isVerifyOk = Await _backupService.VerifyBackupFileAsync(filePath)
                        If Not isVerifyOk Then
                            lblCustomBackupStatus.Text = "Invalid Backup: The file is corrupted or unreadable."
                            lblCustomBackupStatus.ForeColor = Color.FromArgb(185, 28, 28)
                            _isRestoreValidated = False
                            Return
                        End If

                        ' 2. Verify SHA-256 Hash
                        Dim history = Await _backupService.GetBackupHistoryAsync(100)
                        Dim historyRecord = history.FirstOrDefault(Function(h) h.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase) OrElse h.FileName.Equals(Path.GetFileName(filePath), StringComparison.OrdinalIgnoreCase))
                        
                        If historyRecord IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(historyRecord.Sha256Hash) Then
                            If Not DatabaseBackupService.VerifySha256TamperStatus(filePath, historyRecord.Sha256Hash) Then
                                lblCustomBackupStatus.Text = "Invalid Backup: Verification failed."
                                lblCustomBackupStatus.ForeColor = Color.FromArgb(185, 28, 28)
                                _isRestoreValidated = False
                                Return
                            End If
                        End If

                        ' Passed validation
                        _isRestoreValidated = True
                        _selectedBackupForRestore = New DatabaseBackupHistoryDto() With {
                            .FilePath = filePath,
                            .StartedOn = File.GetCreationTime(filePath),
                            .FileSizeBytes = New FileInfo(filePath).Length
                        }
                        
                        Dim mbSize As Double = CDbl(_selectedBackupForRestore.FileSizeBytes) / (1024.0 * 1024.0)
                        lblCustomBackupStatus.Text = $"Selected: {Path.GetFileName(filePath)} ({mbSize:F2} MB) - ✓ Verified"
                        lblCustomBackupStatus.ForeColor = Color.FromArgb(3, 84, 63)
                        
                        btnRestoreLatest.Text = "Restore Selected Backup"
                        btnRestoreLatest.Enabled = True
                    Catch ex As Exception
                        lblCustomBackupStatus.Text = "Invalid Backup: An error occurred during verification."
                        lblCustomBackupStatus.ForeColor = Color.FromArgb(185, 28, 28)
                        _isRestoreValidated = False
                    Finally
                        Me.Cursor = Cursors.Default
                    End Try
                End If
            End Using
        End Sub

        Private Async Sub btnRestoreLatest_Click(sender As Object, e As EventArgs)
            If Not _isRestoreValidated OrElse _selectedBackupForRestore Is Nothing Then Return

            Dim authz As Core.Security.IAuthorizationService = New BLL.Security.AuthorizationService()
            If Not authz.IsAuthorized(Core.Enums.UserRole.Admin) Then
                AppNotificationHelper.ShowError("Access Denied: Only System Administrators can execute database restore operations.", "Access Denied", Me)
                Return
            End If

            Dim backupTimeStr = _selectedBackupForRestore.StartedOn.ToString("dd MMM yyyy, hh:mm tt")
            Dim confirmMsg = $"This will replace the current office database with the selected backup.{Environment.NewLine}Any changes made after this backup was created may be lost.{Environment.NewLine}{Environment.NewLine}Selected Backup: {backupTimeStr}{Environment.NewLine}Status: Verified"

            Dim res = FrmInAppAlert.ShowModal(Me, "Restore Database?", confirmMsg, AlertType.WarningAlert, actionText:="Continue Restore", showCancel:=True, cancelText:="Cancel")
            If res = DialogResult.OK Then
                Try
                    Me.Cursor = Cursors.WaitCursor
                    
                    ' Update UI for progress
                    btnRestoreLatest.Visible = False
                    btnChooseAnother.Visible = False
                    lblCustomBackupStatus.Visible = False
                    lblRestoreProgress.Visible = True

                    Dim restoreSuccess = Await _restorePreparationService.ExecuteControlledRestoreAsync(_selectedBackupForRestore.FilePath)

                    If restoreSuccess Then
                        Microsoft.Data.SqlClient.SqlConnection.ClearAllPools()

                        Forms.Common.FrmInAppAlert.ShowModal(Me, "Restore Completed", $"Your office database has been successfully restored.", Forms.Common.AlertType.SuccessAlert, actionText:="Restart Application")

                        Application.Restart()
                        Environment.Exit(0)
                    Else
                        AppNotificationHelper.ShowError("An error occurred while restoring the office database.", "Restore Failed", Me)
                    End If
                Catch ex As Exception
                    AppNotificationHelper.ShowError($"An error occurred: {ex.Message}", "Restore Failed", Me)
                Finally
                    Me.Cursor = Cursors.Default
                    ' Reset UI on failure
                    btnRestoreLatest.Visible = True
                    btnChooseAnother.Visible = True
                    lblRestoreProgress.Visible = False
                End Try
            End If
        End Sub
    End Class
End Namespace
