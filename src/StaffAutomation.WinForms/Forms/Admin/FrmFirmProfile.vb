Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports Krypton.Toolkit
Imports StaffAutomation.BLL.Logging
Imports StaffAutomation.BLL.Security
Imports StaffAutomation.BLL.Services
Imports StaffAutomation.Core.Configuration
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Logging
Imports StaffAutomation.Core.Security
Imports StaffAutomation.DAL.Configuration
Imports StaffAutomation.DAL.Core
Imports StaffAutomation.DAL.Interfaces
Imports StaffAutomation.DAL.Repositories
Imports StaffAutomation.WinForms.Forms.Common
Imports StaffAutomation.WinForms.Forms.Main

Namespace Forms.Admin
    ''' <summary>
    ''' WinForms Configuration Form for managing CA Firm Master Profile singleton.
    ''' Styled according to the Krypton ThemeConstants design system.
    ''' </summary>
    Public Class FrmFirmProfile
        Inherits Form

        Private ReadOnly _firmService As IFirmProfileService
        Private ReadOnly _authzService As IAuthorizationService

        Private pnlHeader As Panel
        Private lblTitle As Label
        Private lblSubtitle As Label

        Private pnlBody As Panel
        Private tableInputs As TableLayoutPanel

        Private txtFirmName As KryptonTextBox
        Private txtLegalName As KryptonTextBox
        Private txtFRN As KryptonTextBox
        Private txtPAN As KryptonTextBox
        Private txtGSTIN As KryptonTextBox
        Private txtPhone As KryptonTextBox
        Private txtEmail As KryptonTextBox
        Private txtWebsite As KryptonTextBox
        Private txtAddressLine1 As KryptonTextBox
        Private txtAddressLine2 As KryptonTextBox
        Private txtCity As KryptonTextBox
        Private txtStateName As KryptonTextBox
        Private txtPincode As KryptonTextBox

        Private pnlFooter As Panel
        Private btnSave As KryptonButton
        Private btnCancel As KryptonButton

        Public Sub New()
            ' Construct DAL and BLL dependencies using standard WinForms pattern
            Dim config As IAppConfiguration = New AppConfiguration()
            Dim connFactory As IDatabaseConnectionFactory = New DbConnectionFactory(config)
            Dim sqlHelper As ISqlHelper = New SqlHelper(connFactory)
            Dim appLogger As IAppLogger = New AppLogger(config)
            Dim auditLogger As IAuditLogger = New AuditLogger(sqlHelper)

            Dim firmRepo As IFirmProfileRepository = New FirmProfileRepository(sqlHelper)
            _firmService = New FirmProfileService(firmRepo, appLogger, auditLogger)
            _authzService = New AuthorizationService()

            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Text = "🏢 Firm Master Profile"
            Me.Size = New Size(760, 680)
            Me.MinimumSize = New Size(700, 620)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.BackColor = ThemeConstants.WorkspaceBackground

            ' 1. Header Panel
            pnlHeader = New Panel()
            lblTitle = New Label() With {.Text = "🏢 CA Firm Master Profile"}
            lblSubtitle = New Label() With {.Text = "Configure legal entity name, FRN, PAN, GSTIN, and official correspondence address."}
            ThemeConstants.ApplyHeaderStyle(pnlHeader, lblTitle, lblSubtitle)
            pnlHeader.Controls.Add(lblSubtitle)
            pnlHeader.Controls.Add(lblTitle)

            ' 2. Footer Panel (DockStyle.Bottom - Always Visible)
            pnlFooter = New Panel() With {
                .Dock = DockStyle.Bottom,
                .Height = 60,
                .BackColor = Color.White,
                .Padding = New Padding(16, 12, 16, 12)
            }

            Dim pnlButtons As New FlowLayoutPanel() With {
                .Dock = DockStyle.Right,
                .AutoSize = True,
                .FlowDirection = FlowDirection.RightToLeft,
                .WrapContents = False,
                .BackColor = Color.Transparent
            }

            btnCancel = New KryptonButton() With {
                .Text = "Cancel",
                .Size = New Size(100, 36),
                .Margin = New Padding(8, 0, 0, 0)
            }
            ThemeConstants.ApplyKryptonSecondaryButton(btnCancel)
            AddHandler btnCancel.Click, Sub(s, e) Me.Close()

            btnSave = New KryptonButton() With {
                .Text = "💾 Save Profile",
                .Size = New Size(140, 36),
                .Margin = New Padding(0)
            }
            ThemeConstants.ApplyKryptonPrimaryButton(btnSave)
            AddHandler btnSave.Click, Async Sub(s, e) Await SaveProfileAsync()

            pnlButtons.Controls.Add(btnCancel)
            pnlButtons.Controls.Add(btnSave)
            pnlFooter.Controls.Add(pnlButtons)

            ' 3. Body Container
            pnlBody = New Panel() With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(20),
                .AutoScroll = True,
                .BackColor = Color.White
            }

            tableInputs = New TableLayoutPanel() With {
                .Dock = DockStyle.Top,
                .AutoSize = True,
                .ColumnCount = 2,
                .RowCount = 7,
                .Padding = New Padding(10)
            }
            tableInputs.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0!))
            tableInputs.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0!))

            ' Controls Initialization
            txtFirmName = CreateInputControl("Firm Name *", 0, 0)
            txtLegalName = CreateInputControl("Legal Entity Name *", 1, 0)

            txtFRN = CreateInputControl("Firm Reg. Number (FRN) *", 0, 1)
            txtPAN = CreateInputControl("Income Tax PAN *", 1, 1)

            txtGSTIN = CreateInputControl("GSTIN (Optional)", 0, 2)
            txtPhone = CreateInputControl("Official Phone *", 1, 2)

            txtEmail = CreateInputControl("Official Email *", 0, 3)
            txtWebsite = CreateInputControl("Website URL (Optional)", 1, 3)

            txtAddressLine1 = CreateInputControl("Address Line 1 *", 0, 4)
            txtAddressLine2 = CreateInputControl("Address Line 2 (Optional)", 1, 4)

            txtCity = CreateInputControl("City *", 0, 5)
            txtStateName = CreateInputControl("State *", 1, 5)

            txtPincode = CreateInputControl("Pincode *", 0, 6)

            pnlBody.Controls.Add(tableInputs)

            ' Add controls in reverse dock order so pnlFooter remains anchored at bottom
            Me.Controls.Add(pnlBody)
            Me.Controls.Add(pnlHeader)
            Me.Controls.Add(pnlFooter)
            pnlFooter.BringToFront()
        End Sub

        Private Function CreateInputControl(labelText As String, col As Integer, row As Integer) As KryptonTextBox
            Dim pnlCell As New Panel() With {
                .Height = 62,
                .Dock = DockStyle.Fill,
                .Padding = New Padding(4, 2, 4, 2)
            }

            Dim lbl As New Label() With {
                .Text = labelText,
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Dock = DockStyle.Top,
                .Height = 18
            }

            Dim txt As New KryptonTextBox() With {
                .Dock = DockStyle.Top,
                .Height = 32
            }
            ThemeConstants.ApplyAppTextBoxStyle(txt)

            pnlCell.Controls.Add(txt)
            pnlCell.Controls.Add(lbl)

            tableInputs.Controls.Add(pnlCell, col, row)
            Return txt
        End Function

        Protected Overrides Async Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)

            ' Authorization Guard
            If Not _authzService.IsAuthorized(UserRole.Admin) AndAlso Not _authzService.IsAuthorized(UserRole.Owner) Then
                FrmInAppAlert.ShowModal(Me, "Access Denied", "Access Denied: Only Firm Owners or Administrators can modify Firm Master Profile.", AlertType.WarningAlert)
                Me.Close()
                Return
            End If

            Await LoadProfileAsync()
        End Sub

        Private Async Function LoadProfileAsync() As Task
            Try
                Dim dto = Await _firmService.GetFirmProfileAsync()
                If dto IsNot Nothing Then
                    txtFirmName.Text = dto.FirmName
                    txtLegalName.Text = dto.LegalName
                    txtFRN.Text = dto.FRN
                    txtPAN.Text = dto.PAN
                    txtGSTIN.Text = dto.GSTIN
                    txtAddressLine1.Text = dto.AddressLine1
                    txtAddressLine2.Text = dto.AddressLine2
                    txtCity.Text = dto.City
                    txtStateName.Text = dto.StateName
                    txtPincode.Text = dto.Pincode
                    txtPhone.Text = dto.Phone
                    txtEmail.Text = dto.Email
                    txtWebsite.Text = dto.Website
                End If
            Catch ex As Exception
                FrmInAppAlert.ShowModal(Me, "Error", "Failed to load Firm Profile: " & ex.Message, AlertType.WarningAlert)
            End Try
        End Function

        Private Async Function SaveProfileAsync() As Task
            btnSave.Enabled = False
            Try
                Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 1)

                Dim dto As New FirmProfileDto() With {
                    .FirmId = 1,
                    .FirmName = txtFirmName.Text.Trim(),
                    .LegalName = txtLegalName.Text.Trim(),
                    .FRN = txtFRN.Text.Trim(),
                    .PAN = txtPAN.Text.Trim(),
                    .GSTIN = txtGSTIN.Text.Trim(),
                    .AddressLine1 = txtAddressLine1.Text.Trim(),
                    .AddressLine2 = txtAddressLine2.Text.Trim(),
                    .City = txtCity.Text.Trim(),
                    .StateName = txtStateName.Text.Trim(),
                    .Pincode = txtPincode.Text.Trim(),
                    .Phone = txtPhone.Text.Trim(),
                    .Email = txtEmail.Text.Trim(),
                    .Website = txtWebsite.Text.Trim()
                }

                Dim success = Await _firmService.SaveFirmProfileAsync(dto, currentUserId)
                If success Then
                    FrmInAppAlert.ShowModal(Me, "Success", "Firm Master Profile updated successfully.", AlertType.SuccessAlert)
                    Me.DialogResult = DialogResult.OK
                    Me.Close()
                Else
                    FrmInAppAlert.ShowModal(Me, "Error", "Failed to update Firm Profile.", AlertType.WarningAlert)
                End If
            Catch valEx As ValidationException
                FrmInAppAlert.ShowModal(Me, "Validation Error", valEx.Message, AlertType.WarningAlert)
            Catch bizEx As BusinessException
                FrmInAppAlert.ShowModal(Me, "Business Error", bizEx.Message, AlertType.WarningAlert)
            Catch ex As Exception
                FrmInAppAlert.ShowModal(Me, "Error", "Unexpected failure: " & ex.Message, AlertType.WarningAlert)
            Finally
                btnSave.Enabled = True
            End Try
        End Function
    End Class
End Namespace
