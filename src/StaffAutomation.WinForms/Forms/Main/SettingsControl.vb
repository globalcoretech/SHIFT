Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Collections.Generic
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.BLL.Security
Imports StaffAutomation.Core.Security
Imports StaffAutomation.WinForms.Forms.Common
Imports StaffAutomation.Core.Configuration
Imports StaffAutomation.DAL.Configuration
Imports StaffAutomation.Core.Logging
Imports StaffAutomation.BLL.Logging
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.BLL.Services
Namespace Forms.Main
    ''' <summary>
    ''' System Settings and Configuration Master Console with full-width responsive layout.
    ''' Uses centralized Krypton design system ThemeConstants and reusable AppActionCard components.
    ''' Features a category-based view for easier navigation.
    ''' </summary>
    Public Class SettingsControl
        Inherits UserControl

        Private pnlHeroHeader As Panel
        Private lblHeroTitle As Label
        Private lblHeroSubtitle As Label
        
        Private splitContainer As SplitContainer
        Private pnlSidebar As FlowLayoutPanel
        Private flowContainer As FlowLayoutPanel

        Private _cardsByCategory As New Dictionary(Of String, List(Of AppActionCard))
        Private _categoryButtons As New List(Of Button)

        Public Sub New()
            InitializeComponent()
            InitializeCards()
            PopulateCategories()
            
            ' Select the first category by default
            If _categoryButtons.Count > 0 Then
                SelectCategory(_categoryButtons(0).Text, _categoryButtons(0))
            End If
        End Sub

        Private Sub InitializeComponent()
            Me.SuspendLayout()
            Me.Dock = DockStyle.Fill
            Me.BackColor = ThemeConstants.WorkspaceBackground
            Me.Padding = New Padding(16)
            Me.AutoScroll = False

            ' 1. Hero Page Header
            pnlHeroHeader = New Panel()
            lblHeroTitle = New Label() With {.Text = "⚙ System Settings & Configuration Center"}
            lblHeroSubtitle = New Label() With {.Text = "Manage CA firm profile, financial year, user security, database, and automation."}
            ThemeConstants.ApplyHeaderStyle(pnlHeroHeader, lblHeroTitle, lblHeroSubtitle)

            pnlHeroHeader.Controls.Add(lblHeroSubtitle)
            pnlHeroHeader.Controls.Add(lblHeroTitle)

            ' 2. Split Container for Sidebar and Content
            splitContainer = New SplitContainer() With {
                .Dock = DockStyle.Fill,
                .FixedPanel = FixedPanel.Panel1,
                .SplitterDistance = 250,
                .SplitterWidth = 16,
                .BackColor = Color.Transparent,
                .IsSplitterFixed = True
            }

            ' 3. Sidebar FlowLayoutPanel
            pnlSidebar = New FlowLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .FlowDirection = FlowDirection.TopDown,
                .WrapContents = False,
                .BackColor = ThemeConstants.NavigationBackground,
                .Padding = New Padding(8)
            }
            ThemeConstants.EnableDoubleBuffering(pnlSidebar)

            ' 4. Content FlowLayoutPanel
            flowContainer = New FlowLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .AutoScroll = True,
                .FlowDirection = FlowDirection.LeftToRight,
                .WrapContents = True,
                .Padding = New Padding(0),
                .BackColor = Color.Transparent
            }

            splitContainer.Panel1.Controls.Add(pnlSidebar)
            splitContainer.Panel2.Controls.Add(flowContainer)

            ' Dynamic card width recalculation on container resize
            AddHandler flowContainer.Resize, Sub(s, e) RecalculateCardWidths()

            Me.Controls.Add(splitContainer)
            Me.Controls.Add(pnlHeroHeader)
            Me.ResumeLayout(False)
        End Sub

        Private Sub InitializeCards()
            _cardsByCategory.Clear()

            Dim isEmployee = CurrentUserContext.IsAuthenticated AndAlso CurrentUserContext.CurrentUser.Role = UserRole.Employee

            ' --- Firm & Financial ---
            If Not isEmployee Then
                Dim firmCards As New List(Of AppActionCard)()
            
            Dim cardFirm = CreateActionCard("🏢  Firm Master Profile", "Configure CA firm legal name, FRN registration number, GSTIN, branch addresses, and official letterhead header formats.", "FRN Registered • Active", ThemeConstants.PrimaryAccent, "Configure →")
            AddHandler cardFirm.ActionClicked, Sub(s, e)
                                                    Using dlg As New Forms.Admin.FrmFirmProfile()
                                                        dlg.ShowDialog(Me.FindForm())
                                                    End Using
                                                End Sub
            firmCards.Add(cardFirm)

            Dim cardFY = CreateActionCard("📅  Active Financial Year", "Set active accounting period (Current: FY 2026-27), statutory filing cutoff dates, and audit completion rules.", "FY 2026-27 Active", ThemeConstants.SuccessGreen, "Configure →")
            AddHandler cardFY.ActionClicked, Sub(s, e)
                                                  Using dlg As New Forms.Admin.FrmFinancialYearManagement()
                                                      dlg.ShowDialog(Me.FindForm())
                                                  End Using
                                              End Sub
            firmCards.Add(cardFY)
            _cardsByCategory.Add("Firm & Financial", firmCards)
            End If

            ' --- Staff & Security ---
            Dim securityCards As New List(Of AppActionCard)()

            Dim cardMyAccount = CreateActionCard("🔑  My Account", "Manage your profile, login ID and password", "Current User", ThemeConstants.PrimaryAccent, "Open →")
            AddHandler cardMyAccount.ActionClicked, Sub(s, e)
                                                        Using dlg As New Forms.Admin.FrmMyAccount()
                                                            dlg.ShowDialog(Me.FindForm())
                                                        End Using
                                                    End Sub
            securityCards.Add(cardMyAccount)

            If Not isEmployee Then
                Dim cardUsers = CreateActionCard("👤  Staff Users", "Manage staff user accounts, credentials, role permissions (Owner / Admin / Employee), and department assignments.", "Role Security Active", ThemeConstants.PrimaryAccent, "Open →")
                AddHandler cardUsers.ActionClicked, Sub(s, e)
                                                        Dim authz As IAuthorizationService = New AuthorizationService()
                                                        If Not authz.IsAuthorized(UserRole.Admin) Then
                                                            FrmInAppAlert.ShowModal(Me.FindForm(), "Access Denied", "Access Denied: User account management requires System Administrator privileges.", AlertType.WarningAlert, actionText:="OK")
                                                            Return
                                                        End If
                                                        Using dlg As New Forms.Admin.FrmUserManagement()
                                                            dlg.ShowDialog(Me.FindForm())
                                                        End Using
                                                    End Sub
                securityCards.Add(cardUsers)

                Dim cardSecurity = CreateActionCard("🛡️  Advanced Permissions", "Manage user access and permissions", "Security Active", Color.FromArgb(124, 58, 237), "Open →")
                AddHandler cardSecurity.ActionClicked, Sub(s, e)
                                                            Dim authz As IAuthorizationService = New AuthorizationService()
                                                        If Not authz.IsAuthorized(UserRole.Admin) Then
                                                            FrmInAppAlert.ShowModal(Me.FindForm(), "Access Denied", "Access Denied: Security Console requires System Administrator privileges.", AlertType.WarningAlert, actionText:="OK")
                                                            Return
                                                        End If
                                                        Using dlg As New Forms.Admin.FrmSecurityCenter(0)
                                                            dlg.ShowDialog(Me.FindForm())
                                                        End Using
                                                    End Sub
                securityCards.Add(cardSecurity)

                Dim cardDevices = CreateActionCard("📱  Device Approvals", "Review and approve login access for new staff devices.", "Pending Approvals", Color.FromArgb(16, 185, 129), "Review Devices →")
                AddHandler cardDevices.ActionClicked, Sub(s, e)
                                                       Dim authz As IAuthorizationService = New AuthorizationService()
                                                       If Not authz.IsAuthorizedAny(UserRole.Admin, UserRole.Owner) Then
                                                           FrmInAppAlert.ShowModal(Me.FindForm(), "Access Denied", "Access Denied: Device approvals are restricted to Administrators and Firm Owners.", AlertType.WarningAlert, actionText:="OK")
                                                           Return
                                                       End If
                                                       Using dlg As New Forms.Admin.FrmDeviceApprovals()
                                                           dlg.ShowDialog(Me.FindForm())
                                                       End Using
                                                   End Sub
                securityCards.Add(cardDevices)

                ' Remove Audit Logs card as requested to keep UI simple
                _cardsByCategory.Add("Staff & Security", securityCards)

                ' --- Attendance ---
                Dim attendanceCards As New List(Of AppActionCard)()
                Dim cardAttendance = CreateActionCard("⏱️  Attendance Configuration", "Attendance is managed from the Attendance module.", "Active", ThemeConstants.PrimaryAccent, "Open Attendance →")
                AddHandler cardAttendance.ActionClicked, Sub(s, e)
                                                             Dim shell = TryCast(Me.FindForm(), FrmMainShell)
                                                             If shell IsNot Nothing Then shell.NavigateToModule("Attendance")
                                                         End Sub
                attendanceCards.Add(cardAttendance)
                _cardsByCategory.Add("Attendance", attendanceCards)

                ' --- Automation ---
            Dim automationCards As New List(Of AppActionCard)()
            Dim cardCompliance = CreateActionCard("⚙️  Compliance Automation", "Automatically prepare reminders and create compliance tasks before statutory deadlines (GSTR-1, GSTR-3B, TDS, Tax Audit).", "Automated Compliance ON", Color.FromArgb(217, 119, 6), "Configure →")
            AddHandler cardCompliance.ActionClicked, Sub(s, e)
                                                          Using dlg As New Forms.Admin.FrmComplianceAutomation()
                                                              dlg.ShowDialog(Me.FindForm())
                                                          End Using
                                                      End Sub
            automationCards.Add(cardCompliance)
            _cardsByCategory.Add("Automation", automationCards)

            ' --- Database & Backup ---
            Dim dbCards As New List(Of AppActionCard)()
            Dim cardDb = CreateActionCard("💾  SQL Database Backups", "Configure automated daily SQL Server database backups, local directory paths, and point-in-time restore points.", "SQL Express Connected", Color.FromArgb(13, 148, 136), "Configure →")
            AddHandler cardDb.ActionClicked, Sub(s, e)
                                                 Dim authz As IAuthorizationService = New AuthorizationService()
                                                 If Not authz.IsAuthorized(UserRole.Admin) Then
                                                     FrmInAppAlert.ShowModal(Me.FindForm(), "Access Denied", "Access Denied: Only System Administrators can access database backup and disaster recovery settings.", AlertType.WarningAlert, actionText:="OK")
                                                     Return
                                                 End If
                                                 Using dlg As New Forms.Admin.FrmDatabaseBackupRestore()
                                                     dlg.ShowDialog(Me.FindForm())
                                                 End Using
                                             End Sub
            dbCards.Add(cardDb)
            _cardsByCategory.Add("Database & Backup", dbCards)

            ' --- Advanced / System ---
            Dim advancedCards As New List(Of AppActionCard)()
            Dim cardDbConn = CreateActionCard("🌐  Central Database Setup", "Configure central SQL Server hostname, IP address, instance, authentication credentials, and test connection.", "Multi-System Config", Color.FromArgb(37, 99, 235), "Configure →")
            AddHandler cardDbConn.ActionClicked, Sub(s, e)
                                                     Dim authz As IAuthorizationService = New AuthorizationService()
                                                     If Not authz.IsAuthorizedAny(UserRole.Admin, UserRole.Owner) Then
                                                         FrmInAppAlert.ShowModal(Me.FindForm(), "Access Denied", "Access Denied: Database server configuration is restricted to System Administrators and Firm Owners.", AlertType.WarningAlert, actionText:="OK")
                                                         Return
                                                     End If
                                                     Using dlg As New Forms.Admin.FrmDatabaseConnectionConfig()
                                                         dlg.ShowDialog(Me.FindForm())
                                                     End Using
                                                 End Sub
            advancedCards.Add(cardDbConn)
            _cardsByCategory.Add("Advanced / System", advancedCards)
            End If

            ' --- System Information ---
            Dim sysInfoCards As New List(Of AppActionCard)()
            Dim cardUpdates = CreateActionCard("🔄  Application Updates", "Check for software updates, view current version, and download the latest release of CA Staff Workforce Automation.", $"v{Core.Constants.AppConstants.AppVersion}", Color.FromArgb(16, 185, 129), "Check for Updates →")
            AddHandler cardUpdates.ActionClicked, Async Sub(s, e)
                                                      ' Implement manual update checking here
                                                      cardUpdates.Enabled = False
                                                      Dim originalText = cardUpdates.ActionText
                                                      cardUpdates.ActionText = "Checking..."
                                                      Dim config As IAppConfiguration = New AppConfiguration()
                                                      Dim logger As IAppLogger = New AppLogger(config)
                                                      Try
                                                          Dim updateSvc As IUpdateService = New UpdateService(config, logger)
                                                          
                                                          Dim result = Await updateSvc.CheckForUpdatesAsync()
                                                          If result.IsSuccess Then
                                                              If result.IsUpdateAvailable Then
                                                                  Dim msg = $"A new version (v{result.Manifest.LatestVersion}) is available. Release Notes: {result.Manifest.ReleaseNotes}{Environment.NewLine}{Environment.NewLine}Would you like to download this update now?"
                                                                  Dim resultAlert = MessageBox.Show(Me.FindForm(), msg, "Update Available", MessageBoxButtons.YesNo, MessageBoxIcon.Information)
                                                                  If resultAlert = DialogResult.Yes Then
                                                                      cardUpdates.ActionText = "Downloading... 0%"
                                                                      Dim progress = New Progress(Of Integer)(Sub(pct) cardUpdates.ActionText = $"Downloading... {pct}%")
                                                                      Dim downloadResult = Await updateSvc.DownloadAndVerifyUpdateAsync(result.Manifest, progress)
                                                                      
                                                                      If downloadResult.IsSuccess Then
                                                                          cardUpdates.ActionText = "Update Ready"
                                                                          FrmInAppAlert.ShowModal(Me.FindForm(), "Update Ready", "The update has been downloaded and verified.", AlertType.SuccessAlert, actionText:="OK")
                                                                      Else
                                                                          cardUpdates.ActionText = originalText
                                                                          cardUpdates.Enabled = True
                                                                          FrmInAppAlert.ShowModal(Me.FindForm(), "Download Failed", downloadResult.ErrorMessage, AlertType.ErrorAlert, actionText:="OK")
                                                                      End If
                                                                  Else
                                                                      cardUpdates.ActionText = originalText
                                                                      cardUpdates.Enabled = True
                                                                  End If
                                                              Else
                                                                  cardUpdates.ActionText = originalText
                                                                  cardUpdates.Enabled = True
                                                                  FrmInAppAlert.ShowModal(Me.FindForm(), "Up to Date", "You're up to date.", AlertType.SuccessAlert, actionText:="OK")
                                                              End If
                                                          Else
                                                              cardUpdates.ActionText = originalText
                                                              cardUpdates.Enabled = True
                                                              ' Hide technical details and show a clear client-friendly failure message
                                                              FrmInAppAlert.ShowModal(Me.FindForm(), "Update Check Failed", "Unable to check for updates right now. Please try again later.", AlertType.WarningAlert, actionText:="OK")
                                                          End If
                                                      Catch ex As Exception
                                                          cardUpdates.ActionText = originalText
                                                          cardUpdates.Enabled = True
                                                          logger.LogError("Update flow error", "SettingsControl", ex)
                                                      End Try
                                                  End Sub
            sysInfoCards.Add(cardUpdates)
            _cardsByCategory.Add("System Information", sysInfoCards)
        End Sub

        Private Sub PopulateCategories()
            pnlSidebar.SuspendLayout()
            pnlSidebar.Controls.Clear()
            _categoryButtons.Clear()

            For Each category In _cardsByCategory.Keys
                Dim btnCategory As New Button() With {
                    .Text = category,
                    .Width = pnlSidebar.ClientSize.Width - 16,
                    .Height = 45,
                    .FlatStyle = FlatStyle.Flat,
                    .TextAlign = ContentAlignment.MiddleLeft,
                    .Padding = New Padding(12, 0, 0, 0),
                    .Cursor = Cursors.Hand,
                    .Font = New Font(ThemeConstants.FontNameDefault, 10.0!, FontStyle.Regular),
                    .BackColor = ThemeConstants.NavigationBackground,
                    .ForeColor = ThemeConstants.NavigationText,
                    .Margin = New Padding(0, 0, 0, 4)
                }
                btnCategory.FlatAppearance.BorderSize = 0

                AddHandler btnCategory.Click, Sub(s, e) SelectCategory(category, btnCategory)
                
                _categoryButtons.Add(btnCategory)
                pnlSidebar.Controls.Add(btnCategory)
            Next

            pnlSidebar.ResumeLayout(True)
        End Sub

        Private Sub SelectCategory(categoryName As String, selectedBtn As Button)
            ' Update button styles
            For Each btn In _categoryButtons
                If btn Is selectedBtn Then
                    btn.BackColor = ThemeConstants.NavigationItemSelected
                    btn.ForeColor = Color.White
                    btn.Font = New Font(ThemeConstants.FontNameDefault, 10.0!, FontStyle.Bold)
                Else
                    btn.BackColor = ThemeConstants.NavigationBackground
                    btn.ForeColor = ThemeConstants.NavigationText
                    btn.Font = New Font(ThemeConstants.FontNameDefault, 10.0!, FontStyle.Regular)
                End If
            Next

            ' Update cards
            flowContainer.SuspendLayout()
            flowContainer.Controls.Clear()

            If _cardsByCategory.ContainsKey(categoryName) Then
                For Each card In _cardsByCategory(categoryName)
                    flowContainer.Controls.Add(card)
                Next
            End If

            flowContainer.ResumeLayout(True)
            RecalculateCardWidths()
        End Sub

        Private Function CreateActionCard(title As String, desc As String, badge As String, accentColor As Color, actionText As String) As AppActionCard
            Dim card As New AppActionCard() With {
                .Title = title,
                .Description = desc,
                .BadgeText = badge,
                .AccentColor = accentColor,
                .ActionText = actionText,
                .Size = New Size(510, 155)
            }
            Return card
        End Function

        Private Sub RecalculateCardWidths()
            If flowContainer IsNot Nothing AndAlso flowContainer.ClientSize.Width > 500 Then
                Dim targetWidth As Integer = Math.Max(460, (flowContainer.ClientSize.Width - 36) \ 2)
                For Each ctrl As Control In flowContainer.Controls
                    If TypeOf ctrl Is AppActionCard Then
                        ctrl.Width = targetWidth
                    End If
                Next
            End If
        End Sub
    End Class
End Namespace
