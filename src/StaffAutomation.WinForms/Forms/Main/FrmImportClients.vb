Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Drawing
Imports System.IO
Imports System.Linq
Imports System.Text.RegularExpressions
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports StaffAutomation.BLL.Logging
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Logging
Imports StaffAutomation.Core.Security
Imports StaffAutomation.DAL.Configuration
Imports StaffAutomation.DAL.Repositories

Namespace Forms.Main
    ''' <summary>
    ''' Ultra-Premium Excel / CSV Bulk Client Import Dialog and Live Validation Engine.
    ''' Provides Sample Template Downloader, Multi-Column Field Auto-Mapping, GSTIN/PAN Format Validation, and Async DB Bulk Insertion.
    ''' </summary>
    Public Class FrmImportClients
        Inherits Form

        Private ReadOnly _clientService As IClientService
        Private ReadOnly _appLogger As IAppLogger

        Private _importedClients As List(Of ClientImportRow) = New List(Of ClientImportRow)()
        Private _selectedFilePath As String = String.Empty

        ' UI Components
        Private pnlHeader As Panel
        Private lblTitle As Label
        Private lblSubtitle As Label

        Private pnlToolbar As Panel
        Private btnBrowseFile As Forms.Common.ModernButton
        Private btnDownloadTemplate As Forms.Common.ModernButton
        Private lblFilePath As Label

        Private pnlGridCard As Panel
        Private dgvPreview As DataGridView

        Private pnlFooter As Panel
        Private lblSummary As Label
        Private btnExecuteImport As Forms.Common.ModernButton
        Private btnCloseDialog As Forms.Common.ModernButton

        Public Sub New(clientService As IClientService, appLogger As IAppLogger)
            _clientService = clientService
            _appLogger = appLogger

            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.SuspendLayout()

            Me.Text = "Excel / CSV Bulk Client Registry Import Engine"
            Me.Size = New Size(940, 640)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.BackColor = ThemeConstants.WorkspaceBackground

            ' 1. Header Banner (#1E1B4B to #312E81)
            pnlHeader = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 70,
                .BackColor = Color.FromArgb(30, 27, 75),
                .Padding = New Padding(20, 12, 20, 12)
            }

            lblTitle = New Label() With {
                .Text = "Bulk Client Import & Verification Engine",
                .Font = New Font(ThemeConstants.FontNameDefault, 13.0!, FontStyle.Bold),
                .ForeColor = Color.White,
                .Location = New Point(20, 10),
                .AutoSize = True
            }

            lblSubtitle = New Label() With {
                .Text = "Upload CSV / Excel spreadsheets to batch import client masters with automatic GSTIN & PAN validation",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Regular),
                .ForeColor = Color.FromArgb(199, 210, 254),
                .Location = New Point(20, 38),
                .AutoSize = True
            }

            pnlHeader.Controls.Add(lblSubtitle)
            pnlHeader.Controls.Add(lblTitle)

            ' 2. Top Controls (Browse / Status)
            pnlToolbar = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 52,
                .BackColor = Color.White,
                .Padding = New Padding(14, 10, 14, 10)
            }

            btnBrowseFile = New Forms.Common.ModernButton() With {
                .Text = " Browse File (.CSV / .XLSX)",
                .Image = VectorIconHelper.CreateImportIcon(Color.White, 16),
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Primary,
                .Size = New Size(215, 34),
                .Location = New Point(14, 8)
            }
            AddHandler btnBrowseFile.Click, AddressOf btnBrowseFile_Click

            btnDownloadTemplate = New Forms.Common.ModernButton() With {
                .Text = " Download Sample Excel (.xlsx)",
                .Image = VectorIconHelper.CreateExcelIcon(Color.FromArgb(16, 122, 68), 16),
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Custom,
                .BackColor = Color.FromArgb(240, 253, 244),
                .ForeColor = Color.FromArgb(16, 122, 68),
                .CustomBorderColor = Color.FromArgb(187, 247, 208),
                .HoverColor = Color.FromArgb(220, 252, 231),
                .Size = New Size(235, 34),
                .Location = New Point(238, 8)
            }
            AddHandler btnDownloadTemplate.Click, AddressOf btnDownloadTemplate_Click

            lblFilePath = New Label() With {
                .Text = "No file selected. Click Browse to load client spreadsheet.",
                .Font = New Font(ThemeConstants.FontNameDefault, 8.5!, FontStyle.Italic),
                .ForeColor = ThemeConstants.TextSecondary,
                .Location = New Point(474, 17),
                .AutoSize = True
            }

            pnlToolbar.Controls.Add(lblFilePath)
            pnlToolbar.Controls.Add(btnDownloadTemplate)
            pnlToolbar.Controls.Add(btnBrowseFile)

            ' 3. Data Preview Grid Container
            pnlGridCard = New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = Color.White,
                .Padding = New Padding(14)
            }

            dgvPreview = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .ReadOnly = True,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            }
            ThemeConstants.ApplyModernGridStyle(dgvPreview)
            AddHandler dgvPreview.CellFormatting, AddressOf dgvPreview_CellFormatting

            pnlGridCard.Controls.Add(dgvPreview)

            ' 4. Footer Action Panel
            pnlFooter = New Panel() With {
                .Dock = DockStyle.Bottom,
                .Height = 58,
                .Width = 940,
                .BackColor = Color.White,
                .Padding = New Padding(16, 10, 16, 10)
            }

            lblSummary = New Label() With {
                .Text = "Total Rows: 0  |  Ready to Import: 0  |  Invalid: 0  |  Duplicates: 0",
                .Font = New Font(ThemeConstants.FontNameDefault, 9.0!, FontStyle.Bold),
                .ForeColor = ThemeConstants.TextPrimary,
                .Location = New Point(16, 18),
                .AutoSize = True
            }

            btnCloseDialog = New Forms.Common.ModernButton() With {
                .Text = " Cancel",
                .Image = VectorIconHelper.CreateCloseIcon(ThemeConstants.TextSecondary, 14),
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Secondary,
                .Size = New Size(100, 36),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right,
                .Location = New Point(600, 10)
            }
            AddHandler btnCloseDialog.Click, Sub(s, e) Me.Close()

            btnExecuteImport = New Forms.Common.ModernButton() With {
                .Text = " Execute Bulk Import",
                .Image = VectorIconHelper.CreateCheckIcon(Color.White, 16),
                .Scheme = Forms.Common.ModernButton.ButtonScheme.Success,
                .Size = New Size(195, 36),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right,
                .Location = New Point(712, 10),
                .Enabled = False
            }
            AddHandler btnExecuteImport.Click, AddressOf btnExecuteImport_Click

            pnlFooter.Controls.Add(btnExecuteImport)
            pnlFooter.Controls.Add(btnCloseDialog)
            pnlFooter.Controls.Add(lblSummary)

            ' Assemble Form Layout
            Me.Controls.Add(pnlGridCard)
            Me.Controls.Add(pnlToolbar)
            Me.Controls.Add(pnlHeader)
            Me.Controls.Add(pnlFooter)

            Me.ResumeLayout(False)
        End Sub

        Private Async Sub btnBrowseFile_Click(sender As Object, e As EventArgs)
            Using ofd As New OpenFileDialog()
                ofd.Filter = "Spreadsheet Files (*.xlsx;*.csv;*.txt;*.tsv)|*.xlsx;*.csv;*.txt;*.tsv|Excel Worksheets (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*"
                ofd.Title = "Select Client Master CSV / Excel File"

                If ofd.ShowDialog() = DialogResult.OK Then
                    _selectedFilePath = ofd.FileName
                    lblFilePath.Text = Path.GetFileName(_selectedFilePath)
                    Await ParseAndValidateFileAsync(_selectedFilePath)
                End If
            End Using
        End Sub

        Private Sub btnDownloadTemplate_Click(sender As Object, e As EventArgs)
            Using sfd As New SaveFileDialog()
                sfd.Filter = "Excel Workbook (*.xlsx)|*.xlsx|CSV File (*.csv)|*.csv"
                sfd.FileName = "Sample_Client_Import_Template.xlsx"
                sfd.Title = "Save Sample Client Import Template"

                If sfd.ShowDialog() = DialogResult.OK Then
                    Try
                        If sfd.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) Then
                            NativeExcelEngine.GenerateStyledTemplateXlsx(sfd.FileName)
                            AppNotificationHelper.ShowSuccess($"Professionally Styled Excel Template (.xlsx) saved successfully to:{Environment.NewLine}{sfd.FileName}{Environment.NewLine}{Environment.NewLine}Includes Dark Navy Header, Pre-Formatted Column Widths, Cell Gridlines & Sample Records.", "Excel Template Export Complete", Me)
                        Else
                            Dim csvContent As String = "GSTIN,ClientCode,ClientName,PanNumber,EntityType,GstType,StateName,District,Pincode,Address,ContactPerson,Phone,Email" & Environment.NewLine &
                                                       "27AABCA1234F1Z5,CLI-001,Apex Global Logistics Pvt Ltd,AABCA1234F,Pvt Ltd,Regular Monthly,27 - Maharashtra,Mumbai,400001,Plot 42 Commercial Complex,Rajesh Sharma,9820098200,info@apexglobal.com" & Environment.NewLine &
                                                       "07AAAFR9876E1ZB,CLI-002,Royal Traders & Co,AAAFR9876E,Proprietorship,Regular QRMP,07 - Delhi,Central Delhi,110001,12 Connaught Place,Amit Roy,9811198111,contact@royaltraders.com" & Environment.NewLine &
                                                       ",CLI-003,Star Consultancy Services,AABCS5432K,Individual,Unregistered,27 - Maharashtra,Pune,411001,88 MG Road,Suresh Kumar,9890098900,suresh@starconsult.in"

                            File.WriteAllText(sfd.FileName, csvContent)
                            AppNotificationHelper.ShowSuccess($"Sample CSV template saved successfully to:{Environment.NewLine}{sfd.FileName}", "CSV Template Download Complete", Me)
                        End If
                    Catch ex As Exception
                        AppNotificationHelper.ShowError($"Error saving sample template: {ex.Message}", "Template Export Error", Me)
                    End Try
                End If
            End Using
        End Sub

        Private Async Function ParseAndValidateFileAsync(filePath As String) As Task
            _importedClients.Clear()

            Try
                Dim rawRows As New List(Of String())()
                Dim ext = Path.GetExtension(filePath).ToLower()

                If ext = ".xlsx" Then
                    rawRows = NativeExcelEngine.ReadXlsxRows(filePath)
                Else
                    Dim lines = File.ReadAllLines(filePath)
                    For Each line In lines
                        Dim rawLine = line.Trim()
                        If Not String.IsNullOrWhiteSpace(rawLine) Then
                            Dim cols = rawLine.Split(","c).Select(Function(c) c.Trim().Trim(""""c)).ToArray()
                            rawRows.Add(cols)
                        End If
                    Next
                End If

                If rawRows.Count <= 1 Then
                    AppNotificationHelper.ShowWarning("The selected file is empty or missing data rows.", "File Load Warning", Me)
                    Return
                End If

                ' Fetch existing DB Clients for Async Duplicate Pre-checking
                Dim existingDbClients = Await _clientService.GetAllClientsAsync()
                Dim dbGstins As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
                Dim dbCodes As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
                Dim dbPans As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

                If existingDbClients IsNot Nothing Then
                    For Each c In existingDbClients
                        If Not String.IsNullOrWhiteSpace(c.Gstin) Then dbGstins.Add(c.Gstin.Trim())
                        If Not String.IsNullOrWhiteSpace(c.ClientCode) Then dbCodes.Add(c.ClientCode.Trim())
                        If Not String.IsNullOrWhiteSpace(c.PanNumber) Then dbPans.Add(c.PanNumber.Trim())
                    Next
                End If

                Dim fileGstins As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
                Dim fileCodes As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
                Dim filePans As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

                Dim headers = rawRows(0).Select(Function(h) h.Trim().ToLower()).ToArray()

                For i As Integer = 1 To rawRows.Count - 1
                    Dim cols = rawRows(i)
                    If cols.Length < 2 OrElse cols.All(Function(c) String.IsNullOrWhiteSpace(c)) Then Continue For

                    Dim row As New ClientImportRow()
                    row.RowIndex = i
                    row.Gstin = GetColValue(cols, headers, "gstin", 0)
                    row.ClientCode = GetColValue(cols, headers, "clientcode", 1)
                    row.ClientName = GetColValue(cols, headers, "clientname", 2)
                    row.PanNumber = GetColValue(cols, headers, "pannumber", 3)
                    row.EntityType = GetColValue(cols, headers, "entitytype", 4)
                    row.GstType = GetColValue(cols, headers, "gsttype", 5)
                    row.StateName = GetColValue(cols, headers, "statename", 6)
                    row.District = GetColValue(cols, headers, "district", 7)
                    row.Pincode = GetColValue(cols, headers, "pincode", 8)
                    row.Address = GetColValue(cols, headers, "address", 9)
                    row.ContactPerson = GetColValue(cols, headers, "contactperson", 10)
                    row.Phone = GetColValue(cols, headers, "phone", 11)
                    row.Email = GetColValue(cols, headers, "email", 12)

                    ' Format & DB Duplicate Validation
                    ValidateRowFormat(row, dbGstins, dbCodes, dbPans, fileGstins, fileCodes, filePans)
                    _importedClients.Add(row)
                Next

                dgvPreview.DataSource = Nothing
                dgvPreview.DataSource = _importedClients
                dgvPreview.Refresh()

                Dim validCount = _importedClients.Where(Function(r) r.ValidationStatus = "READY").Count()
                Dim dupCount = _importedClients.Where(Function(r) r.ValidationStatus.StartsWith("DUPLICATE")).Count()
                Dim warnCount = _importedClients.Where(Function(r) r.ValidationStatus.StartsWith("WARNING") OrElse r.ValidationStatus.StartsWith("ERROR")).Count()
                lblSummary.Text = $"Total Rows: {_importedClients.Count}  |  Ready to Import: {validCount}  |  Invalid: {warnCount}  |  Duplicates: {dupCount}"

                Dim importableRowsCount = validCount
                btnExecuteImport.Enabled = (importableRowsCount > 0)
                If importableRowsCount > 0 Then
                    btnExecuteImport.Text = $"  Import Valid Rows ({importableRowsCount})"
                Else
                    btnExecuteImport.Text = "  Execute Bulk Import"
                End If

            Catch ex As Exception
                AppNotificationHelper.ShowError($"Error reading file: {ex.Message}", "File Read Failure", Me)
            End Try
        End Function

        Private Function GetColValue(cols As String(), headers As String(), key As String, fallbackIndex As Integer) As String
            For idx As Integer = 0 To headers.Length - 1
                If headers(idx).Contains(key) AndAlso idx < cols.Length Then
                    Return cols(idx)
                End If
            Next
            If fallbackIndex < cols.Length Then Return cols(fallbackIndex)
            Return String.Empty
        End Function

        Private Sub ValidateRowFormat(row As ClientImportRow, dbGstins As HashSet(Of String), dbCodes As HashSet(Of String), dbPans As HashSet(Of String), fileGstins As HashSet(Of String), fileCodes As HashSet(Of String), filePans As HashSet(Of String))
            Dim issues As New List(Of String)()
            Dim isDuplicate As Boolean = False

            If String.IsNullOrWhiteSpace(row.ClientName) Then
                issues.Add("Missing Firm Name")
            End If

            Dim cleanPan = If(row.PanNumber, String.Empty).Trim().ToUpperInvariant()
            row.PanNumber = cleanPan
            If Not String.IsNullOrWhiteSpace(cleanPan) Then
                If Not Regex.IsMatch(cleanPan, "^[A-Z]{5}[0-9]{4}[A-Z]{1}$") Then
                    issues.Add("PAN format invalid")
                Else
                    If dbPans.Contains(cleanPan) Then
                        issues.Add("DUPLICATE: PAN already in DB")
                        isDuplicate = True
                    ElseIf filePans.Contains(cleanPan) Then
                        issues.Add("DUPLICATE: In-file duplicate PAN")
                        isDuplicate = True
                    Else
                        filePans.Add(cleanPan)
                    End If
                End If
            End If

            Dim cleanGstin = If(row.Gstin, String.Empty).Trim().ToUpperInvariant()
            row.Gstin = cleanGstin
            If Not String.IsNullOrWhiteSpace(cleanGstin) Then
                If Not Regex.IsMatch(cleanGstin, "^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$") Then
                    issues.Add("GSTIN format invalid")
                Else
                    Dim stateCode As Integer
                    If Not Integer.TryParse(cleanGstin.Substring(0, 2), stateCode) OrElse stateCode < 1 OrElse stateCode > 38 Then
                        issues.Add("Invalid State Code in GSTIN")
                    ElseIf Not String.IsNullOrWhiteSpace(cleanPan) AndAlso cleanGstin.Substring(2, 10) <> cleanPan Then
                        issues.Add("GSTIN and PAN do not match")
                    End If

                    If dbGstins.Contains(cleanGstin) Then
                        issues.Add("DUPLICATE: GSTIN already in DB")
                        isDuplicate = True
                    ElseIf fileGstins.Contains(cleanGstin) Then
                        issues.Add("DUPLICATE: In-file duplicate GSTIN")
                        isDuplicate = True
                    Else
                        fileGstins.Add(cleanGstin)
                    End If
                End If
            End If

            Dim cleanCode = If(row.ClientCode, String.Empty).Trim()
            If Not String.IsNullOrWhiteSpace(cleanCode) Then
                If dbCodes.Contains(cleanCode) Then
                    issues.Add("DUPLICATE: Client Code in DB")
                    isDuplicate = True
                ElseIf fileCodes.Contains(cleanCode) Then
                    issues.Add("DUPLICATE: In-file duplicate Code")
                    isDuplicate = True
                Else
                    fileCodes.Add(cleanCode)
                End If
            End If

            If issues.Count = 0 Then
                row.ValidationStatus = "READY"
            ElseIf isDuplicate Then
                row.ValidationStatus = "DUPLICATE: " & String.Join("; ", issues)
            ElseIf issues.Any(Function(i) i.StartsWith("Missing") OrElse i.Contains("invalid") OrElse i.Contains("Invalid") OrElse i.Contains("match")) Then
                row.ValidationStatus = "ERROR: " & String.Join("; ", issues)
            Else
                row.ValidationStatus = "WARNING: " & String.Join("; ", issues)
            End If
        End Sub

        Private Sub dgvPreview_CellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs)
            If e.RowIndex < 0 OrElse e.RowIndex >= dgvPreview.Rows.Count Then Return

            Dim row = TryCast(dgvPreview.Rows(e.RowIndex).DataBoundItem, ClientImportRow)
            If row IsNot Nothing Then
                Dim status = If(row.ValidationStatus, String.Empty)
                If status.StartsWith("DUPLICATE") OrElse status.StartsWith("ERROR") Then
                    e.CellStyle.BackColor = Color.FromArgb(254, 226, 226)
                    e.CellStyle.ForeColor = Color.FromArgb(153, 27, 27)
                ElseIf status.StartsWith("WARNING") Then
                    e.CellStyle.BackColor = Color.FromArgb(254, 243, 199)
                    e.CellStyle.ForeColor = Color.FromArgb(146, 64, 14)
                ElseIf status = "READY" Then
                    e.CellStyle.BackColor = Color.FromArgb(240, 253, 244)
                    e.CellStyle.ForeColor = Color.FromArgb(22, 101, 52)
                End If
            End If
        End Sub

        Private Async Sub btnExecuteImport_Click(sender As Object, e As EventArgs)
            If _importedClients Is Nothing OrElse _importedClients.Count = 0 Then Return
            
            Dim currentUserId = If(CurrentUserContext.IsAuthenticated, CurrentUserContext.CurrentUser.UserId, 0)
            If currentUserId <= 0 Then
                AppNotificationHelper.ShowError("Could not retrieve the current authenticated User ID. Please log in again.", "Authentication Error", Me)
                Return
            End If

            Dim validImportRows = _importedClients.Where(Function(r) r.ValidationStatus = "READY").ToList()

            If validImportRows.Count = 0 Then
                AppNotificationHelper.ShowWarning("No valid non-duplicate client records found to import.", "Bulk Import Action", Me)
                Return
            End If

            Dim result = AppNotificationHelper.ShowQuestion($"{validImportRows.Count} client records are ready to be imported. Do you want to save these records?", "Import Clients?", Me)
            If result <> DialogResult.Yes AndAlso result <> DialogResult.OK Then Return

            btnExecuteImport.Enabled = False
            btnExecuteImport.Text = "  Importing..."

            Try
                Dim dtos As New List(Of ClientDto)()
                For Each row In validImportRows
                    Dim dto As New ClientDto With {
                        .ClientCode = If(String.IsNullOrWhiteSpace(row.ClientCode), $"CLI-{DateTime.Now.Ticks.ToString().Substring(12)}", row.ClientCode),
                        .ClientName = row.ClientName,
                        .Gstin = row.Gstin,
                        .PanNumber = row.PanNumber,
                        .EntityType = If(String.IsNullOrWhiteSpace(row.EntityType), "Proprietorship", row.EntityType),
                        .GstType = If(String.IsNullOrWhiteSpace(row.GstType), "Regular Monthly", row.GstType),
                        .StateName = If(String.IsNullOrWhiteSpace(row.StateName), "27 - Maharashtra", row.StateName),
                        .District = row.District,
                        .Pincode = row.Pincode,
                        .Address = row.Address,
                        .ContactPerson = row.ContactPerson,
                        .Phone = row.Phone,
                        .Email = row.Email,
                        .Department = Nothing,
                        .IsActive = True
                    }
                    dtos.Add(dto)
                Next

                Dim successCount = Await _clientService.CreateClientsBulkAsync(dtos)

                AppNotificationHelper.ShowSuccess($"{successCount} client records imported successfully.", "Import Completed", Me)
                Me.DialogResult = DialogResult.OK
                Me.Close()

            Catch ex As Exception
                AppNotificationHelper.ShowError($"Error executing bulk import: {ex.Message}", "Import Failure", Me)
            Finally
                btnExecuteImport.Enabled = True
                btnExecuteImport.Text = "  Execute Bulk Import"
            End Try
        End Sub

        Public Class ClientImportRow
            Public Property RowIndex As Integer
            Public Property ClientCode As String
            Public Property ClientName As String
            Public Property Gstin As String
            Public Property PanNumber As String
            Public Property EntityType As String
            Public Property GstType As String
            Public Property StateName As String
            Public Property District As String
            Public Property Pincode As String
            Public Property Address As String
            Public Property ContactPerson As String
            Public Property Phone As String
            Public Property Email As String
            Public Property ValidationStatus As String
        End Class
    End Class
End Namespace

