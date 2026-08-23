Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text.RegularExpressions
Imports System.Windows.Forms
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
Imports StaffAutomation.DAL.Repositories

Namespace Forms.Admin
    ''' <summary>
    ''' Client Registry Master Form delegating all business logic and soft deletion to BLL ClientService.
    ''' Features Smart GSTIN Auto-Fetch, PAN parsing, State detection, and Auto Client Code generation.
    ''' </summary>
    Public Class FrmClientManagement
        Private ReadOnly _clientService As IClientService
        Private _clientList As List(Of ClientDto) = New List(Of ClientDto)()
        Private _selectedClientId As Integer = 0
        Private _userModifiedCodeManually As Boolean = False

        Public Sub New()
            InitializeComponent()
            _clientService = InitializeClientService()
        End Sub

        Private Async Sub FrmClientManagement_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            Dim authz As IAuthorizationService = New AuthorizationService()
            If Not authz.IsAuthorizedAny(UserRole.Admin, UserRole.Owner) Then
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Access Denied", "Access Denied: Access to Client Master Registry requires Owner or Admin privileges.", Forms.Common.AlertType.WarningAlert, actionText:="OK")
                Me.Close()
                Return
            End If

            Forms.Main.ThemeConstants.ApplyModernGridStyle(dgvClients)
            PopulateDepartmentCombo()
            PopulateDefaultDropdowns()

            If cboStatusFilter.Items.Count > 0 Then
                cboStatusFilter.SelectedIndex = 0 ' Default to Active
            End If

            Await RefreshClientGridAsync()
            ClearFormInputs()
        End Sub

        Private Sub PopulateDepartmentCombo()
            cboDept.DataSource = [Enum].GetValues(GetType(DepartmentType))
        End Sub

        Private Sub PopulateDefaultDropdowns()
            If cboGstType.Items.Count > 0 Then cboGstType.SelectedIndex = 0
            If cboEntityType.Items.Count > 0 Then cboEntityType.SelectedIndex = 0
            If cboState.Items.Count > 0 Then cboState.SelectedIndex = 0
        End Sub

        Private Async Function RefreshClientGridAsync() As System.Threading.Tasks.Task
            Try
                Me.Cursor = Cursors.WaitCursor
                _clientList = Await _clientService.GetAllClientsAsync(includeDeleted:=True)
                ApplyFilter()
            Catch ex As Exception
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Error", "Failed to load client registry: " & ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Function

        Private Sub ApplyFilter()
            Dim searchText = txtSearch.Text.Trim().ToLower()
            Dim filterMode = If(cboStatusFilter.SelectedItem IsNot Nothing, cboStatusFilter.SelectedItem.ToString(), "Active")

            Dim filtered = _clientList.FindAll(Function(c)
                ' Status Filter
                Dim matchesStatus As Boolean = True
                If filterMode = "Active" Then
                    matchesStatus = (c.IsActive AndAlso Not c.IsDeleted)
                ElseIf filterMode = "Archived / Deleted" Then
                    matchesStatus = (c.IsDeleted OrElse Not c.IsActive)
                Else ' All
                    matchesStatus = True
                End If

                If Not matchesStatus Then Return False

                ' Search Text Filter (Client Code, Client Name, Contact Person, Phone, GSTIN, PAN)
                If String.IsNullOrEmpty(searchText) Then Return True

                Dim codeMatch = (Not String.IsNullOrEmpty(c.ClientCode) AndAlso c.ClientCode.ToLower().Contains(searchText))
                Dim nameMatch = (Not String.IsNullOrEmpty(c.ClientName) AndAlso c.ClientName.ToLower().Contains(searchText))
                Dim contactMatch = (Not String.IsNullOrEmpty(c.ContactPerson) AndAlso c.ContactPerson.ToLower().Contains(searchText))
                Dim phoneMatch = (Not String.IsNullOrEmpty(c.Phone) AndAlso c.Phone.ToLower().Contains(searchText))
                Dim gstinMatch = (Not String.IsNullOrEmpty(c.Gstin) AndAlso c.Gstin.ToLower().Contains(searchText))
                Dim panMatch = (Not String.IsNullOrEmpty(c.PanNumber) AndAlso c.PanNumber.ToLower().Contains(searchText))

                Return codeMatch OrElse nameMatch OrElse contactMatch OrElse phoneMatch OrElse gstinMatch OrElse panMatch
            End Function)

            dgvClients.DataSource = Nothing
            dgvClients.DataSource = filtered
        End Sub

        Private Sub txtSearch_TextChanged(sender As Object, e As EventArgs) Handles txtSearch.TextChanged
            ApplyFilter()
        End Sub

        Private Sub cboStatusFilter_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboStatusFilter.SelectedIndexChanged
            ApplyFilter()
        End Sub

        Private Async Sub btnRefresh_Click(sender As Object, e As EventArgs) Handles btnRefresh.Click
            Await RefreshClientGridAsync()
        End Sub

        Private Sub dgvClients_SelectionChanged(sender As Object, e As EventArgs) Handles dgvClients.SelectionChanged
            If dgvClients.SelectedRows.Count > 0 Then
                Dim row = dgvClients.SelectedRows(0)
                Dim client = CType(row.DataBoundItem, ClientDto)
                If client IsNot Nothing Then
                    _selectedClientId = client.ClientId
                    txtCode.Text = client.ClientCode
                    txtCode.ReadOnly = True ' Code immutable after creation
                    txtName.Text = client.ClientName
                    txtGstin.Text = client.Gstin
                    txtPan.Text = client.PanNumber
                    txtContact.Text = client.ContactPerson
                    txtPhone.Text = client.Phone
                    txtEmail.Text = client.Email
                    cboDept.SelectedItem = client.Department
                    chkActive.Checked = client.IsActive

                    If Not String.IsNullOrEmpty(client.GstType) Then cboGstType.SelectedItem = client.GstType
                    If Not String.IsNullOrEmpty(client.EntityType) Then cboEntityType.SelectedItem = client.EntityType
                    If Not String.IsNullOrEmpty(client.StateName) Then cboState.SelectedItem = client.StateName
                End If
            End If
        End Sub

        Private Sub btnNew_Click(sender As Object, e As EventArgs) Handles btnNew.Click
            ClearFormInputs()
        End Sub

        Private Sub ClearFormInputs()
            _selectedClientId = 0
            _userModifiedCodeManually = False
            txtGstin.Text = String.Empty
            txtCode.Text = String.Empty
            txtCode.ReadOnly = False
            txtName.Text = String.Empty
            txtPan.Text = String.Empty
            txtContact.Text = String.Empty
            txtPhone.Text = String.Empty
            txtEmail.Text = String.Empty
            chkActive.Checked = True

            PopulateDefaultDropdowns()
            errProvider.Clear()
            txtGstin.Focus()
        End Sub

        ''' <summary>
        ''' Smart Auto Client Code Generator based on Business / Firm Name Initials.
        ''' </summary>
        Private Sub txtName_TextChanged(sender As Object, e As EventArgs) Handles txtName.TextChanged
            If _selectedClientId <> 0 OrElse _userModifiedCodeManually Then Return

            Dim businessName = txtName.Text.Trim()
            If String.IsNullOrWhiteSpace(businessName) Then
                txtCode.Text = String.Empty
                Return
            End If

            ' Noise words to filter out when generating client shortcode
            Dim noiseWords As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {
                "and", "&", "pvt", "ltd", "co", "inc", "the", "proprietorship", "llp", "firm", "traders", "enterprises", "company"
            }

            Dim words = businessName.Split(New Char() {" "c, "-"c, "_"c}, StringSplitOptions.RemoveEmptyEntries) _
                                   .Where(Function(w) Not noiseWords.Contains(w.Trim())) _
                                   .ToArray()

            Dim shortCodePrefix As String = String.Empty

            If words.Length >= 2 Then
                ' Take 1st letter of first 3 words (e.g., Sharma Sons Traders -> SST)
                For Each w In words.Take(3)
                    If w.Length > 0 AndAlso Char.IsLetterOrDigit(w(0)) Then
                        shortCodePrefix &= w(0)
                    End If
                Next
            ElseIf words.Length = 1 Then
                ' Single word -> take first 3 letters (e.g., Reliance -> REL)
                Dim cleanWord = Regex.Replace(words(0), "[^a-zA-Z0-9]", "")
                If cleanWord.Length >= 3 Then
                    shortCodePrefix = cleanWord.Substring(0, 3)
                Else
                    shortCodePrefix = cleanWord
                End If
            End If

            If String.IsNullOrWhiteSpace(shortCodePrefix) Then
                shortCodePrefix = "CLI"
            End If

            shortCodePrefix = shortCodePrefix.ToUpper()
            txtCode.Text = $"{shortCodePrefix}-001"
        End Sub

        Private Sub txtCode_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtCode.KeyPress
            ' If user manually types in code box, stop auto-generating from name
            _userModifiedCodeManually = True
        End Sub

        ''' <summary>
        ''' Smart GSTIN Auto-Fetch and Auto-Fill Details Parser Engine
        ''' </summary>
        Private Sub btnFetchGst_Click(sender As Object, e As EventArgs) Handles btnFetchGst.Click
            FetchAndParseGstDetails()
        End Sub

        Private Sub FetchAndParseGstDetails()
            Dim gstin = txtGstin.Text.Trim().ToUpper()
            If String.IsNullOrEmpty(gstin) Then
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Input Required", "Please enter a 15-digit GSTIN number to fetch details.", Forms.Common.AlertType.WarningAlert, actionText:="OK")
                Return
            End If

            ' Validate 15-character GSTIN regex format: 27AAACR5055K1Z2
            Dim gstRegex As New Regex("^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$")
            If Not gstRegex.IsMatch(gstin) AndAlso gstin.Length <> 15 Then
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Invalid GSTIN", "Please enter a valid 15-character GSTIN format (e.g. 27AAACR5055K1Z2).", Forms.Common.AlertType.WarningAlert, actionText:="OK")
                Return
            End If

            ' 1. State Code Detection (First 2 Digits)
            Dim stateCode = gstin.Substring(0, 2)
            Dim detectedState = GetStateNameFromCode(stateCode)
            SelectComboItemIfAvailable(cboState, detectedState)

            ' 2. PAN Extraction (Characters 3 to 12)
            Dim extractedPan = gstin.Substring(2, 10)
            txtPan.Text = extractedPan

            ' 3. Entity Type Detection (4th Character of PAN)
            Dim entityChar = extractedPan(3)
            Dim detectedEntity As String = "Proprietorship"
            Select Case entityChar
                Case "C"c : detectedEntity = "Pvt Ltd / Public Ltd"
                Case "P"c : detectedEntity = "Proprietorship"
                Case "F"c : detectedEntity = "Partnership / LLP"
                Case "H"c : detectedEntity = "HUF"
                Case "T"c, "A"c : detectedEntity = "Trust / AOP"
            End Select
            SelectComboItemIfAvailable(cboEntityType, detectedEntity)

            ' 4. Default GST Type Selector
            cboGstType.SelectedItem = "Regular Monthly"

            ' 5. Auto-populate Client Trade Name if empty
            If String.IsNullOrWhiteSpace(txtName.Text) Then
                txtName.Text = $"Client GSTIN ({gstin})"
            End If

            Forms.Common.FrmInAppAlert.ShowModal(Me, "GSTIN Verified & Auto-Fetched",
                $"✅ GSTIN Verified Successfully!{Environment.NewLine}{Environment.NewLine}" &
                $"📍 State: {detectedState} (Code: {stateCode}){Environment.NewLine}" &
                $"💳 PAN: {extractedPan}{Environment.NewLine}" &
                $"🏢 Entity: {detectedEntity}{Environment.NewLine}" &
                $"📑 GST Type: Regular Monthly",
                Forms.Common.AlertType.SuccessAlert, actionText:="OK")
        End Sub

        Private Function GetStateNameFromCode(stateCode As String) As String
            Select Case stateCode
                Case "01" : Return "Jammu & Kashmir"
                Case "02" : Return "Himachal"
                Case "03" : Return "Punjab"
                Case "06" : Return "Haryana"
                Case "07" : Return "Delhi"
                Case "08" : Return "Rajasthan"
                Case "09" : Return "Uttar Pradesh"
                Case "10" : Return "Bihar"
                Case "19" : Return "West Bengal"
                Case "21" : Return "Odisha"
                Case "22" : Return "Chhattisgarh"
                Case "23" : Return "Madhya Pradesh"
                Case "24" : Return "Gujarat"
                Case "27" : Return "Maharashtra"
                Case "29" : Return "Karnataka"
                Case "32" : Return "Kerala"
                Case "33" : Return "Tamil Nadu"
                Case "36" : Return "Telangana"
                Case "37" : Return "Other"
                Case Else : Return "Other"
            End Select
        End Function

        Private Sub SelectComboItemIfAvailable(cbo As ComboBox, value As String)
            For i As Integer = 0 To cbo.Items.Count - 1
                If cbo.Items(i).ToString().Equals(value, StringComparison.OrdinalIgnoreCase) Then
                    cbo.SelectedIndex = i
                    Return
                End If
            Next
            If cbo.Items.Count > 0 Then cbo.SelectedIndex = 0
        End Sub

        Private Async Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
            Dim authz As IAuthorizationService = New AuthorizationService()
            If Not authz.IsAuthorizedAny(UserRole.Admin, UserRole.Owner) Then
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Access Denied", "Access Denied: Only Owner or Admin can register or modify clients.", Forms.Common.AlertType.WarningAlert, actionText:="OK")
                Return
            End If

            errProvider.Clear()

            If String.IsNullOrWhiteSpace(txtCode.Text) Then
                errProvider.SetError(txtCode, "Client Code is required.")
                Return
            End If
            If String.IsNullOrWhiteSpace(txtName.Text) Then
                errProvider.SetError(txtName, "Client Name is required.")
                Return
            End If

            Dim dto As New ClientDto() With {
                .ClientId = _selectedClientId,
                .ClientCode = txtCode.Text.Trim(),
                .ClientName = txtName.Text.Trim(),
                .Gstin = txtGstin.Text.Trim(),
                .PanNumber = txtPan.Text.Trim(),
                .GstType = If(cboGstType.SelectedItem IsNot Nothing, cboGstType.SelectedItem.ToString(), "Regular Monthly"),
                .EntityType = If(cboEntityType.SelectedItem IsNot Nothing, cboEntityType.SelectedItem.ToString(), "Proprietorship"),
                .StateName = If(cboState.SelectedItem IsNot Nothing, cboState.SelectedItem.ToString(), "Maharashtra"),
                .ContactPerson = txtContact.Text.Trim(),
                .Phone = txtPhone.Text.Trim(),
                .Email = txtEmail.Text.Trim(),
                .Department = CType(cboDept.SelectedItem, DepartmentType),
                .IsActive = chkActive.Checked
            }

            Try
                Me.Cursor = Cursors.WaitCursor
                If _selectedClientId = 0 Then
                    Await _clientService.CreateClientAsync(dto)
                    Forms.Common.FrmInAppAlert.ShowModal(Me, "Success", "Client registered successfully with GST & PAN details.", Forms.Common.AlertType.SuccessAlert, actionText:="OK")
                Else
                    Await _clientService.UpdateClientAsync(dto)
                    Forms.Common.FrmInAppAlert.ShowModal(Me, "Success", "Client profile updated successfully.", Forms.Common.AlertType.SuccessAlert, actionText:="OK")
                End If
                Await RefreshClientGridAsync()
                ClearFormInputs()
            Catch ex As BusinessException
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Business Rule Error", ex.Message, Forms.Common.AlertType.WarningAlert, actionText:="OK")
            Catch ex As Exception
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Error", "An unexpected error occurred while saving client: " & ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Sub

        Private Async Sub btnArchive_Click(sender As Object, e As EventArgs) Handles btnArchive.Click
            Dim authz As IAuthorizationService = New AuthorizationService()
            If Not authz.IsAuthorized(UserRole.Admin) Then
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Access Denied", "Access Denied: Only System Administrators can archive client records.", Forms.Common.AlertType.WarningAlert, actionText:="OK")
                Return
            End If

            If _selectedClientId = 0 Then
                Forms.Common.FrmInAppAlert.ShowModal(Me, "Selection Required", "Please select a client from the grid to archive.", Forms.Common.AlertType.WarningAlert, actionText:="OK")
                Return
            End If

            Dim confirm = Forms.Common.FrmInAppAlert.ShowModal(Me, "Confirm Archive", $"Are you sure you want to soft-delete / archive client '{txtCode.Text}'?{Environment.NewLine}Historical task records will remain intact.", Forms.Common.AlertType.WarningAlert, actionText:="YES, ARCHIVE CLIENT", showCancel:=True, cancelText:="CANCEL")
            If confirm = DialogResult.OK Then
                Try
                    Me.Cursor = Cursors.WaitCursor
                    Await _clientService.SoftDeleteClientAsync(_selectedClientId)
                    Forms.Common.FrmInAppAlert.ShowModal(Me, "Archived", "Client soft-deleted successfully.", Forms.Common.AlertType.SuccessAlert, actionText:="OK")
                    Await RefreshClientGridAsync()
                    ClearFormInputs()
                Catch ex As Exception
                    Forms.Common.FrmInAppAlert.ShowModal(Me, "Error", "Failed to archive client: " & ex.Message, Forms.Common.AlertType.ErrorAlert, actionText:="OK")
                Finally
                    Me.Cursor = Cursors.Default
                End Try
            End If
        End Sub

        ' Composition Root Factory
        Private Function InitializeClientService() As IClientService
            Dim config As IAppConfiguration = New AppConfiguration()
            Dim connFactory As IDatabaseConnectionFactory = New DbConnectionFactory(config)
            Dim sqlHelper As ISqlHelper = New SqlHelper(connFactory)
            Dim repo As DAL.Interfaces.IClientRepository = New ClientRepository(sqlHelper)
            Dim appLogger As IAppLogger = New AppLogger(config)
            Dim auditLogger As New AuditLogger(sqlHelper)

            Return New ClientService(repo, appLogger, auditLogger)
        End Function
    End Class
End Namespace
