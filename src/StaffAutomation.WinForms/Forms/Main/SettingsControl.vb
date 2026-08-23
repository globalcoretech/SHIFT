Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports StaffAutomation.WinForms.Forms.Common

Namespace Forms.Main
    ''' <summary>
    ''' System Settings and Configuration Master Console with full-width responsive layout.
    ''' Uses centralized Krypton design system ThemeConstants and reusable AppActionCard components.
    ''' </summary>
    Public Class SettingsControl
        Inherits UserControl

        Private pnlHeroHeader As Panel
        Private lblHeroTitle As Label
        Private lblHeroSubtitle As Label
        Private flowContainer As FlowLayoutPanel

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.SuspendLayout()
            Me.Dock = DockStyle.Fill
            Me.BackColor = ThemeConstants.WorkspaceBackground
            Me.Padding = New Padding(16)
            Me.AutoScroll = False

            ' 1. Hero Page Header
            pnlHeroHeader = New Panel()
            lblHeroTitle = New Label() With {.Text = "⚙ System Settings & Firm Configuration"}
            lblHeroSubtitle = New Label() With {.Text = "Manage CA firm profile, financial year cutoffs, user security, SQL automated backups, and filing alert templates."}
            ThemeConstants.ApplyHeaderStyle(pnlHeroHeader, lblHeroTitle, lblHeroSubtitle)

            pnlHeroHeader.Controls.Add(lblHeroSubtitle)
            pnlHeroHeader.Controls.Add(lblHeroTitle)

            ' 2. Responsive Cards Grid FlowLayoutPanel
            flowContainer = New FlowLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .AutoScroll = True,
                .FlowDirection = FlowDirection.LeftToRight,
                .WrapContents = True,
                .Padding = New Padding(0, 12, 0, 0),
                .BackColor = Color.Transparent
            }

            Dim cardDb = CreateActionCard("💾  SQL Database & Backups", "Configure automated daily SQL Server database backups, local directory paths, and point-in-time restore points.", "SQL Express Connected", Color.FromArgb(13, 148, 136), "Configure →")
            AddHandler cardDb.ActionClicked, Sub(s, e)
                                                 Using dlg As New Forms.Admin.FrmDatabaseBackupRestore()
                                                     dlg.ShowDialog(Me.FindForm())
                                                 End Using
                                             End Sub

            Dim cardFirm = CreateActionCard("🏢  Firm Master Profile", "Configure CA firm legal name, FRN registration number, GSTIN, branch addresses, and official letterhead header formats.", "FRN Registered • Active", ThemeConstants.PrimaryAccent, "Configure →")
            AddHandler cardFirm.ActionClicked, Sub(s, e)
                                                    Using dlg As New Forms.Admin.FrmFirmProfile()
                                                        dlg.ShowDialog(Me.FindForm())
                                                    End Using
                                                End Sub

            Dim cardFY = CreateActionCard("📅  Active Financial Year", "Set active accounting period (Current: FY 2026-27), statutory filing cutoff dates, and audit completion rules.", "FY 2026-27 Active", ThemeConstants.SuccessGreen, "Configure →")
            AddHandler cardFY.ActionClicked, Sub(s, e)
                                                  Using dlg As New Forms.Admin.FrmFinancialYearManagement()
                                                      dlg.ShowDialog(Me.FindForm())
                                                  End Using
                                              End Sub

            Dim cardSecurity = CreateActionCard("🛡️  Security Center", "Control who can access what across your office via Role Permissions, User Access Exceptions, and Security Audit Logs.", "7-Step Security Active", Color.FromArgb(124, 58, 237), "Configure →")
            AddHandler cardSecurity.ActionClicked, Sub(s, e)
                                                       Using dlg As New Forms.Admin.FrmSecurityCenter(0)
                                                           dlg.ShowDialog(Me.FindForm())
                                                       End Using
                                                   End Sub

            Dim cardCompliance = CreateActionCard("⚙️  Compliance Automation", "Automatically prepare reminders and create compliance tasks before statutory deadlines (GSTR-1, GSTR-3B, TDS, Tax Audit).", "Automated Compliance ON", Color.FromArgb(217, 119, 6), "Configure →")
            AddHandler cardCompliance.ActionClicked, Sub(s, e)
                                                         Using dlg As New Forms.Admin.FrmComplianceAutomation()
                                                             dlg.ShowDialog(Me.FindForm())
                                                         End Using
                                                     End Sub

            Dim cardAudit = CreateActionCard("🔒  Audit & Activity Log", "Track system security logs, user login history, record modification timestamps, and administrative override logs.", "Audit Log Verified", Color.FromArgb(71, 85, 105), "Configure →")
            AddHandler cardAudit.ActionClicked, Sub(s, e)
                                                    Using dlg As New Forms.Admin.FrmSecurityCenter(2)
                                                        dlg.ShowDialog(Me.FindForm())
                                                    End Using
                                                End Sub

            flowContainer.Controls.Add(cardFirm)
            flowContainer.Controls.Add(cardFY)
            flowContainer.Controls.Add(cardSecurity)
            flowContainer.Controls.Add(cardDb)
            flowContainer.Controls.Add(cardCompliance)
            flowContainer.Controls.Add(cardAudit)

            ' Dynamic card width recalculation on container resize for 1366x768 support
            AddHandler flowContainer.Resize, Sub(s, e) RecalculateCardWidths()

            Me.Controls.Add(flowContainer)
            Me.Controls.Add(pnlHeroHeader)
            Me.ResumeLayout(False)

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
            AddHandler card.ActionClicked, Sub(s, e)
                                               AppNotificationHelper.ShowInfo($"Opened settings module: {title}", "System Settings", Me)
                                           End Sub
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
