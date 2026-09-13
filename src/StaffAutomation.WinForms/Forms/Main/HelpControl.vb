Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports StaffAutomation.WinForms.Forms.Common

Namespace Forms.Main
    ''' <summary>
    ''' Help and Knowledgebase Support Center UserControl with full-width responsive layout.
    ''' Uses centralized Krypton design system ThemeConstants and reusable AppActionCard components.
    ''' </summary>
    Public Class HelpControl
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
            lblHeroTitle = New Label() With {.Text = "❓ Help & Knowledgebase Support Center"}
            lblHeroSubtitle = New Label() With {.Text = "Access CA office operating manuals, keyboard shortcuts, technical support hotline, and system information."}
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

            ' Build 6 Master Help Cards with explicit action handlers
            Dim cardManual = CreateActionCard("📘  User Operating Manual", "Step-by-step documentation for Task workflow management, Client onboarding, Staff attendance tracking, and Report exports.", "Documentation Active", ThemeConstants.PrimaryAccent, "View Guide →")
            AddHandler cardManual.ActionClicked, Sub(s, e)
                                                     FrmInAppAlert.ShowModal(Me.FindForm(), "User Operating Manual", "CA Office Automation Operating Workflow:" & Environment.NewLine & "1. Client Onboarding: Add PAN/GSTIN/Firm details in Client Master." & Environment.NewLine & "2. Task Allocation: Assign staff tasks with statutory filing cutoffs." & Environment.NewLine & "3. Attendance: Log daily check-in/out and leave requests." & Environment.NewLine & "4. Disaster Recovery: Perform daily T-SQL backups in Database Console.", AlertType.SuccessAlert)
                                                 End Sub

            Dim cardSupport = CreateActionCard("☎️  Technical Support Desk", "Need assistance with the application? Contact your system administrator or support team for help with setup, user access, and application issues.", "Helpdesk Online", ThemeConstants.SuccessGreen, "View Guide →")
            AddHandler cardSupport.ActionClicked, Sub(s, e)
                                                      FrmInAppAlert.ShowModal(Me.FindForm(), "Technical Support Desk", "CA Office Technical Support:" & Environment.NewLine & "• Email: support@caofficeautomation.com" & Environment.NewLine & "• Phone: +91 (022) 2200-1122" & Environment.NewLine & "• Hours: Mon-Sat 09:30 AM - 07:00 PM IST", AlertType.SuccessAlert)
                                                  End Sub

            Dim cardShortcuts = CreateActionCard("⌨️  Keyboard Shortcuts", "Quick hotkey reference for navigating between Dashboard, Daily Tasks, Client Master, Attendance, and Executive Reports.", "Hotkey Cheatsheet Ready", Color.FromArgb(124, 58, 237), "View Guide →")
            AddHandler cardShortcuts.ActionClicked, Sub(s, e)
                                                        FrmInAppAlert.ShowModal(Me.FindForm(), "Keyboard Hotkey Reference", "System Hotkeys:" & Environment.NewLine & "• Alt + D : Executive Dashboard" & Environment.NewLine & "• Alt + T : Daily Tasks & Timeline" & Environment.NewLine & "• Alt + C : Client Master Directory" & Environment.NewLine & "• Alt + A : Staff Attendance Logging" & Environment.NewLine & "• Alt + S : Settings & Security Console", AlertType.SuccessAlert)
                                                    End Sub

            Dim cardDiagnostics = CreateActionCard("ℹ️  System Information", $"CA Office Workforce Productivity Automation System v{Core.Constants.AppConstants.AppVersion} Enterprise Build.", $"v{Core.Constants.AppConstants.AppVersion} Enterprise Build", Color.FromArgb(13, 148, 136), "View Guide →")
            AddHandler cardDiagnostics.ActionClicked, Sub(s, e)
                                                           FrmInAppAlert.ShowModal(Me.FindForm(), "System Information", "Application Specifications:" & Environment.NewLine & "• Product: CA Office Workforce Automation" & Environment.NewLine & $"• Version: v{Core.Constants.AppConstants.AppVersion} Enterprise Build" & Environment.NewLine & "• Edition: Commercial Suite" & Environment.NewLine & "• License: Active", AlertType.SuccessAlert)
                                                       End Sub

            Dim cardCompliance = CreateActionCard("🏛️  CA Statutory Compliance Guide", "Pre-configured tax audit, GSTR-1, GSTR-3B, TDS return, and corporate MCA compliance workflow checklists.", "Tax Suite Integrated", Color.FromArgb(217, 119, 6), "View Guide →")
            AddHandler cardCompliance.ActionClicked, Sub(s, e)
                                                         FrmInAppAlert.ShowModal(Me.FindForm(), "CA Statutory Compliance Guide", "Statutory Return Workflows:" & Environment.NewLine & "• GST Suite: GSTR-1, GSTR-3B, GSTR-9 Annual Return" & Environment.NewLine & "• Direct Tax: Tax Audit (Sec 44AB), ITR-3, ITR-6" & Environment.NewLine & "• TDS Suite: Form 24Q, Form 26Q Quarterly Returns" & Environment.NewLine & "• Corporate: MCA AOC-4 & MGT-7 Annual Filing", AlertType.SuccessAlert)
                                                     End Sub

            Dim cardLicense = CreateActionCard("🏢  Commercial Suite Edition", "Enterprise edition licensed specifically for Chartered Accountants, Tax Practitioners, and Audit Consultants.", "Commercial License Active", Color.FromArgb(71, 85, 105), "View Guide →")
            AddHandler cardLicense.ActionClicked, Sub(s, e)
                                                      FrmInAppAlert.ShowModal(Me.FindForm(), "Commercial License & Edition", "Commercial License Details:" & Environment.NewLine & "• License Type: Enterprise Firm Site License" & Environment.NewLine & "• Target Users: Chartered Accountants & Tax Consultants" & Environment.NewLine & "• Unlimited Staff Accounts & Client Master Directories" & Environment.NewLine & "• Security Engine: Granular 7-Step Role Authorization", AlertType.SuccessAlert)
                                                  End Sub

            flowContainer.Controls.Add(cardManual)
            flowContainer.Controls.Add(cardSupport)
            flowContainer.Controls.Add(cardShortcuts)
            flowContainer.Controls.Add(cardDiagnostics)
            flowContainer.Controls.Add(cardCompliance)
            flowContainer.Controls.Add(cardLicense)

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
                                               AppNotificationHelper.ShowInfo($"Opened guide: {title}", "Knowledgebase Center", Me)
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
