Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports Microsoft.Data.SqlClient
Imports StaffAutomation.BLL.Security
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Security
Imports StaffAutomation.DAL.Configuration
Imports StaffAutomation.WinForms.Forms.Common
Imports StaffAutomation.WinForms.Forms.Main

Namespace Forms.Admin
    ''' <summary>
    ''' Centralized SQL Server Database Connection Configuration Form.
    ''' Restricted exclusively to System Administrators and Firm Owners.
    ''' Allows configuring central server IP/hostname, instance, port, auth mode, and DPAPI protected credentials.
    ''' Includes an asynchronous Test Connection engine.
    ''' </summary>
    Public Class FrmDatabaseConnectionConfig
        Inherits Form

        Private ReadOnly _authzService As IAuthorizationService

        ' Header Controls
        Private pnlHeroHeader As Panel
        Private lblHeroTitle As Label
        Private lblHeroSubtitle As Label
        Private btnHeaderClose As Krypton.Toolkit.KryptonButton

        ' Form Field Controls
        Private txtServer As Krypton.Toolkit.KryptonTextBox
        Private txtInstance As Krypton.Toolkit.KryptonTextBox
        Private numPort As NumericUpDown
        Private txtDatabase As Krypton.Toolkit.KryptonTextBox
        Private cboAuthMode As AppComboBox
        Private txtUserId As Krypton.Toolkit.KryptonTextBox
        Private txtPassword As Krypton.Toolkit.KryptonTextBox
        Private numTimeout As NumericUpDown
        Private chkTrustCert As CheckBox
        Private chkEncrypt As CheckBox

        ' Action Controls
        Private lblTestStatus As Label
        Private btnTestConnection As ModernButton
        Private btnSaveConfig As ModernButton
        Private btnCancel As Krypton.Toolkit.KryptonButton

        Public Sub New()
            _authzService = New AuthorizationService()
            InitializeComponent()
            LoadExistingSettings()
        End Sub

        Protected Overrides Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)

            ' Strict Authorization Security Gate: Restrict exclusively to Admin and Owner roles (Bypass for First-Run Setup)
            Dim isFirstRun As Boolean = False
            Try
                Dim config As Core.Configuration.IAppConfiguration = New DAL.Configuration.AppConfiguration()
                config.ValidateConfiguration()
            Catch ex As Core.Exceptions.ConfigurationException
                If ex.SettingKey = "ERR_CFG_MISSING" Then isFirstRun = True
            Catch
            End Try

            If Not isFirstRun AndAlso Not _authzService.IsAuthorizedAny(UserRole.Admin, UserRole.Owner) Then
                FrmInAppAlert.ShowModal(Me, "Access Denied", "Access Denied: Database server configuration is restricted to System Administrators and Firm Owners.", AlertType.WarningAlert, actionText:="OK")
                Me.Close()
                Return
            End If
        End Sub

        Private Sub InitializeComponent()
            Me.Text = "Central Database Connection Configuration"
            Me.Size = New Size(720, 620)
            Me.MinimumSize = New Size(680, 580)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.BackColor = ThemeConstants.WorkspaceBackground
            Me.Padding = New Padding(16)
            Me.Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Regular)

            ' 1. Hero Header
            pnlHeroHeader = New Panel() With {.Dock = DockStyle.Top, .Height = 70}
            lblHeroTitle = New Label() With {.Text = "🌐 Central SQL Server Connection Configuration"}
            lblHeroSubtitle = New Label() With {.Text = "Configure central database host, IP address, instance, authentication mode, and test connection."}
            ThemeConstants.ApplyHeaderStyle(pnlHeroHeader, lblHeroTitle, lblHeroSubtitle)

            btnHeaderClose = New Krypton.Toolkit.KryptonButton() With {
                .Text = "✕",
                .Size = New Size(36, 28),
                .Location = New Point(640, 14),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            ThemeConstants.ApplyKryptonSecondaryButton(btnHeaderClose)
            AddHandler btnHeaderClose.Click, Sub(s, e) Me.Close()
            pnlHeroHeader.Controls.Add(btnHeaderClose)

            ' 2. Configuration Panel Container
            Dim pnlContent As New Panel() With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(16),
                .BackColor = Color.White
            }

            ' Field Layout
            Dim lblServerHeader As New Label() With {.Text = "SQL Server Hostname or IP Address:", .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold), .Location = New Point(20, 15), .AutoSize = True}
            txtServer = New Krypton.Toolkit.KryptonTextBox() With {.Location = New Point(20, 36), .Size = New Size(380, 28)}
            ThemeConstants.ApplyAppTextBoxStyle(txtServer)

            Dim lblInstanceHeader As New Label() With {.Text = "Instance (Optional):", .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold), .Location = New Point(420, 15), .AutoSize = True}
            txtInstance = New Krypton.Toolkit.KryptonTextBox() With {.Location = New Point(420, 36), .Size = New Size(220, 28)}
            ThemeConstants.ApplyAppTextBoxStyle(txtInstance)

            Dim lblPortHeader As New Label() With {.Text = "TCP Port (0 = Default 1433):", .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold), .Location = New Point(20, 75), .AutoSize = True}
            numPort = New NumericUpDown() With {.Location = New Point(20, 96), .Size = New Size(180, 26), .Minimum = 0, .Maximum = 65535, .Value = 0}

            Dim lblDbHeader As New Label() With {.Text = "Database Name:", .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold), .Location = New Point(220, 75), .AutoSize = True}
            txtDatabase = New Krypton.Toolkit.KryptonTextBox() With {.Text = "StaffAutomationDb", .Location = New Point(220, 96), .Size = New Size(420, 28)}
            ThemeConstants.ApplyAppTextBoxStyle(txtDatabase)

            ' Authentication Section
            Dim lblAuthHeader As New Label() With {.Text = "Authentication Mode:", .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold), .Location = New Point(20, 135), .AutoSize = True}
            cboAuthMode = New AppComboBox() With {.Location = New Point(20, 156), .Size = New Size(620, 28), .DropDownStyle = ComboBoxStyle.DropDownList}
            cboAuthMode.Items.Add("Windows Authentication (Integrated Security)")
            cboAuthMode.Items.Add("SQL Server Authentication (SQL User & Password)")
            cboAuthMode.SelectedIndex = 0
            AddHandler cboAuthMode.SelectedIndexChanged, AddressOf cboAuthMode_SelectedIndexChanged

            Dim lblUserHeader As New Label() With {.Text = "SQL Username:", .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold), .Location = New Point(20, 195), .AutoSize = True}
            txtUserId = New Krypton.Toolkit.KryptonTextBox() With {.Location = New Point(20, 216), .Size = New Size(300, 28), .Enabled = False}
            ThemeConstants.ApplyAppTextBoxStyle(txtUserId)

            Dim lblPassHeader As New Label() With {.Text = "SQL Password (DPAPI Protected):", .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold), .Location = New Point(340, 195), .AutoSize = True}
            txtPassword = New Krypton.Toolkit.KryptonTextBox() With {.Location = New Point(340, 216), .Size = New Size(300, 28), .Enabled = False, .PasswordChar = "*"c}
            ThemeConstants.ApplyAppTextBoxStyle(txtPassword)

            ' Options
            Dim lblTimeoutHeader As New Label() With {.Text = "Connection Timeout (Seconds):", .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold), .Location = New Point(20, 255), .AutoSize = True}
            numTimeout = New NumericUpDown() With {.Location = New Point(20, 276), .Size = New Size(180, 26), .Minimum = 3, .Maximum = 120, .Value = 15}

            chkTrustCert = New CheckBox() With {.Text = "Trust Server Certificate (Recommended for LAN)", .Location = New Point(220, 276), .AutoSize = True, .Checked = True}
            chkEncrypt = New CheckBox() With {.Text = "Encrypt Connection", .Location = New Point(480, 276), .AutoSize = True, .Checked = False}

            ' Status & Action Buttons
            lblTestStatus = New Label() With {
                .Text = "Status: Ready to test or save database configuration.",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextSecondary,
                .Location = New Point(20, 320),
                .Size = New Size(620, 44)
            }

            btnTestConnection = New ModernButton() With {
                .Text = "⚡ Test Connection",
                .Scheme = ModernButton.ButtonScheme.Secondary,
                .Size = New Size(160, 36),
                .Location = New Point(20, 380)
            }
            AddHandler btnTestConnection.Click, AddressOf btnTestConnection_Click

            btnSaveConfig = New ModernButton() With {
                .Text = "💾 Save Configuration",
                .Scheme = ModernButton.ButtonScheme.Primary,
                .Size = New Size(180, 36),
                .Location = New Point(190, 380)
            }
            AddHandler btnSaveConfig.Click, AddressOf btnSaveConfig_Click

            btnCancel = New Krypton.Toolkit.KryptonButton() With {
                .Text = "Cancel",
                .Size = New Size(100, 36),
                .Location = New Point(380, 380)
            }
            ThemeConstants.ApplyKryptonSecondaryButton(btnCancel)
            AddHandler btnCancel.Click, Sub(s, e) Me.Close()

            pnlContent.Controls.Add(btnCancel)
            pnlContent.Controls.Add(btnSaveConfig)
            pnlContent.Controls.Add(btnTestConnection)
            pnlContent.Controls.Add(lblTestStatus)
            pnlContent.Controls.Add(chkEncrypt)
            pnlContent.Controls.Add(chkTrustCert)
            pnlContent.Controls.Add(numTimeout)
            pnlContent.Controls.Add(lblTimeoutHeader)
            pnlContent.Controls.Add(txtPassword)
            pnlContent.Controls.Add(lblPassHeader)
            pnlContent.Controls.Add(txtUserId)
            pnlContent.Controls.Add(lblUserHeader)
            pnlContent.Controls.Add(cboAuthMode)
            pnlContent.Controls.Add(lblAuthHeader)
            pnlContent.Controls.Add(txtDatabase)
            pnlContent.Controls.Add(lblDbHeader)
            pnlContent.Controls.Add(numPort)
            pnlContent.Controls.Add(lblPortHeader)
            pnlContent.Controls.Add(txtInstance)
            pnlContent.Controls.Add(lblInstanceHeader)
            pnlContent.Controls.Add(txtServer)
            pnlContent.Controls.Add(lblServerHeader)

            Me.Controls.Add(pnlContent)
            Me.Controls.Add(pnlHeroHeader)
        End Sub

        Private Sub cboAuthMode_SelectedIndexChanged(sender As Object, e As EventArgs)
            Dim isSqlAuth = (cboAuthMode.SelectedIndex = 1)
            txtUserId.Enabled = isSqlAuth
            txtPassword.Enabled = isSqlAuth
        End Sub

        Private Sub LoadExistingSettings()
            Try
                Dim machinePath = DatabaseConnectionSettings.GetDefaultMachineConfigPath()
                Dim settings = DatabaseConnectionSettings.LoadFromFile(machinePath)

                If settings Is Nothing Then
                    Dim fallbackPath = DatabaseConnectionSettings.GetFallbackLocalConfigPath()
                    settings = DatabaseConnectionSettings.LoadFromFile(fallbackPath)
                End If

                If settings IsNot Nothing Then
                    txtServer.Text = settings.Server
                    txtInstance.Text = settings.Instance
                    numPort.Value = Math.Max(0, Math.Min(65535, settings.Port))
                    txtDatabase.Text = If(String.IsNullOrWhiteSpace(settings.Database), "StaffAutomationDb", settings.Database)
                    cboAuthMode.SelectedIndex = If(settings.UseIntegratedSecurity, 0, 1)
                    txtUserId.Text = settings.UserId
                    If Not String.IsNullOrWhiteSpace(settings.EncryptedPassword) AndAlso OperatingSystem.IsWindows() Then
                        txtPassword.Text = UnprotectPassword(settings.EncryptedPassword)
                    End If
                    numTimeout.Value = Math.Max(3, Math.Min(120, settings.ConnectTimeout))
                    chkTrustCert.Checked = settings.TrustServerCertificate
                    chkEncrypt.Checked = settings.Encrypt
                Else
                    txtServer.Text = ".\SQLEXPRESS"
                    txtDatabase.Text = "StaffAutomationDb"
                    cboAuthMode.SelectedIndex = 0
                End If
            Catch ex As Exception
            End Try
        End Sub

        Private Function BuildSettingsFromInput() As DatabaseConnectionSettings
            Dim settings As New DatabaseConnectionSettings() With {
                .Server = txtServer.Text.Trim(),
                .Instance = txtInstance.Text.Trim(),
                .Port = CInt(numPort.Value),
                .Database = txtDatabase.Text.Trim(),
                .UseIntegratedSecurity = (cboAuthMode.SelectedIndex = 0),
                .UserId = txtUserId.Text.Trim(),
                .ConnectTimeout = CInt(numTimeout.Value),
                .TrustServerCertificate = chkTrustCert.Checked,
                .Encrypt = chkEncrypt.Checked
            }

            If Not settings.UseIntegratedSecurity AndAlso Not String.IsNullOrWhiteSpace(txtPassword.Text) AndAlso OperatingSystem.IsWindows() Then
                ' Protect password using Windows DPAPI before storing
                settings.EncryptedPassword = ProtectPassword(txtPassword.Text)
            End If

            Return settings
        End Function

        Private Async Sub btnTestConnection_Click(sender As Object, e As EventArgs)
            If String.IsNullOrWhiteSpace(txtServer.Text) Then
                AppNotificationHelper.ShowWarning("Please enter a valid SQL Server hostname or IP address.", "Host Required", Me)
                Return
            End If

            Try
                Me.Cursor = Cursors.WaitCursor
                btnTestConnection.Enabled = False
                lblTestStatus.Text = "Status: Testing connection to SQL Server..."
                lblTestStatus.ForeColor = ThemeConstants.TextSecondary

                Dim settings = BuildSettingsFromInput()
                ' Override timeout to 5 seconds for fast test feedback
                settings.ConnectTimeout = 5
                Dim connStr = settings.BuildConnectionString()

                Dim startTime = DateTime.Now
                Using conn As New SqlConnection(connStr)
                    Await conn.OpenAsync()
                    Dim latencyMs = CInt((DateTime.Now - startTime).TotalMilliseconds)

                    Dim serverVersion As String = ""
                    Using cmd As SqlCommand = conn.CreateCommand()
                        cmd.CommandText = "SELECT @@VERSION;"
                        cmd.CommandTimeout = 5
                        Dim verObj = Await cmd.ExecuteScalarAsync()
                        If verObj IsNot Nothing Then
                            Dim fullVer = verObj.ToString()
                            Dim lineIdx = fullVer.IndexOf(vbCr)
                            If lineIdx > 0 Then fullVer = fullVer.Substring(0, lineIdx)
                            serverVersion = fullVer.Trim()
                        End If
                    End Using

                    lblTestStatus.Text = $"✅ SUCCESS: Connection Established in {latencyMs} ms!{Environment.NewLine}SQL Engine: {serverVersion}"
                    lblTestStatus.ForeColor = ThemeConstants.SuccessGreen
                End Using
            Catch ex As Exception
                ' Sanitize error message to guarantee no password exposure in logs/UI
                Dim safeError = ex.Message
                If safeError.Contains("Password=") OrElse safeError.Contains("PWD=") Then
                    safeError = "SQL Authentication failed for specified user credentials."
                End If

                lblTestStatus.Text = $"❌ CONNECTION FAILED: {safeError}"
                lblTestStatus.ForeColor = ThemeConstants.DangerRed
            Finally
                btnTestConnection.Enabled = True
                Me.Cursor = Cursors.Default
            End Try
        End Sub

        Private Async Sub btnSaveConfig_Click(sender As Object, e As EventArgs)
            If String.IsNullOrWhiteSpace(txtServer.Text) Then
                AppNotificationHelper.ShowWarning("Please enter a valid SQL Server hostname or IP address.", "Host Required", Me)
                Return
            End If

            Try
                Me.Cursor = Cursors.WaitCursor
                btnSaveConfig.Enabled = False
                lblTestStatus.Text = "Status: Validating configuration before saving..."

                Dim settings = BuildSettingsFromInput()
                
                ' Validate Connection
                Dim testSettings = BuildSettingsFromInput()
                testSettings.ConnectTimeout = 5
                Dim connStr = testSettings.BuildConnectionString()
                
                Using conn As New SqlConnection(connStr)
                    Await conn.OpenAsync()
                End Using

                Dim machinePath = DatabaseConnectionSettings.GetDefaultMachineConfigPath()
                settings.SaveToFile(machinePath)

                AppNotificationHelper.ShowSuccess($"Database connection configuration saved successfully to:{Environment.NewLine}{machinePath}", "Configuration Saved", Me)
                Me.DialogResult = DialogResult.OK
                Me.Close()
            Catch ex As Exception
                Dim safeError = ex.Message
                If safeError.Contains("Password=") OrElse safeError.Contains("PWD=") Then
                    safeError = "SQL Authentication failed for specified user credentials."
                End If
                AppNotificationHelper.ShowError($"Connection testing failed. Configuration was not saved:{Environment.NewLine}{safeError}", "Validation Failure", Me)
            Finally
                btnSaveConfig.Enabled = True
                Me.Cursor = Cursors.Default
                lblTestStatus.Text = "Status: Ready to test or save database configuration."
            End Try
        End Sub
    End Class
End Namespace
