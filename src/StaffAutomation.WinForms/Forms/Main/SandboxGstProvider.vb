Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Net.Http
Imports System.Text
Imports System.Text.Json
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Logging

Namespace Forms.Main
    ''' <summary>
    ''' Official Integration Provider for Sandbox.co.in GST Compliance API.
    ''' Follows official 2-Step Authentication and Search Flow:
    ''' Step 1: POST https://api.sandbox.co.in/authenticate (x-api-key, x-api-secret) -> Obtains JWT Access Token
    ''' Step 2: POST https://api.sandbox.co.in/gst/compliance/public/gstin/search -> Searches GSTIN with JWT in 'authorization' header
    ''' Protects secrets and tokens from audit logs and caches JWT tokens safely in memory.
    ''' </summary>
    Public Class SandboxGstProvider
        Implements IGstVerificationProvider

        ' Token Cache (In-Memory 24h Expiry Management)
        Private Shared _cachedJwtToken As String = String.Empty
        Private Shared _tokenExpiry As DateTime = DateTime.MinValue
        Private Shared ReadOnly _tokenLock As New Object()

        Private Class AuthResult
            Public Property Success As Boolean = False
            Public Property Token As String = String.Empty
            Public Property HttpStatusCode As Integer = 0
            Public Property ErrorMessage As String = String.Empty
        End Class

        Public ReadOnly Property ProviderName As String Implements IGstVerificationProvider.ProviderName
            Get
                Return "Sandbox.co.in GST Compliance API"
            End Get
        End Property

        Public Async Function VerifyGstinAsync(gstin As String) As Task(Of GstVerificationResult) Implements IGstVerificationProvider.VerifyGstinAsync
            Dim result As New GstVerificationResult With {
                .ProviderName = Me.ProviderName,
                .Success = False,
                .IsLiveApiData = False,
                .HttpStatusCode = 0,
                .Record = GstVerificationEngine.ExtractStatutoryDetailsFromGstin(gstin)
            }

            ' Reset failure states on record
            result.Record.GstStatus = "Not Verified"
            result.Record.IsActive = False
            result.Record.IsLiveApiData = False

            ' Read Configuration via AppConfiguration (checks Environment variables first)
            Dim appConfig As Core.Configuration.IAppConfiguration = New DAL.Configuration.AppConfiguration()
            Dim apiKey As String = appConfig.GetSetting("GstApiKey")
            Dim apiSecret As String = appConfig.GetSetting("GstApiSecret")
            Dim searchEndpointConfig As String = appConfig.GetSetting("GstApiUrl")

            If String.IsNullOrWhiteSpace(searchEndpointConfig) Then
                searchEndpointConfig = "https://api.sandbox.co.in/gst/compliance/public/gstin/search"
            End If

            ' 1. Guard: Missing Credentials (Internal state: HttpStatusCode = 0)
            If String.IsNullOrWhiteSpace(apiKey) OrElse String.IsNullOrWhiteSpace(apiSecret) Then
                result.HttpStatusCode = 0
                result.ErrorMessage = "API CREDENTIALS MISSING: GST API key or secret is not configured in Environment variables (GST_API_KEY / GST_API_SECRET) or App.config."
                Return result
            End If

            Dim cleanGstin As String = gstin.Trim().ToUpper()

            Try
                Using client As New HttpClient()
                    client.Timeout = TimeSpan.FromSeconds(8.0)

                    ' 2. Obtain / Refresh JWT Access Token via POST /authenticate
                    Dim authRes = Await AuthenticateAsync(client, apiKey, apiSecret)
                    If Not authRes.Success Then
                        result.HttpStatusCode = authRes.HttpStatusCode
                        result.ErrorMessage = $"Sandbox Authentication Failed (HTTP {authRes.HttpStatusCode}): {authRes.ErrorMessage}"
                        Return result
                    End If
                    Dim jwtToken As String = authRes.Token

                    ' 3. Execute GSTIN Search API POST /gst/compliance/public/gstin/search
                    client.DefaultRequestHeaders.Clear()
                    client.DefaultRequestHeaders.Add("User-Agent", "StaffAutomation/1.0 (.NET 8 WinForms)")
                    client.DefaultRequestHeaders.Add("Accept", "application/json")
                    client.DefaultRequestHeaders.Add("x-api-key", apiKey)
                    client.DefaultRequestHeaders.Add("x-api-version", "1.0")
                    ' Official Sandbox Auth Header: 'authorization' value without Bearer prefix
                    client.DefaultRequestHeaders.Add("authorization", jwtToken)

                    ' Construct Request Payload cleanly
                    Dim payloadJson As String = "{""gstin"":""" & cleanGstin & """}"
                    Dim content As New StringContent(payloadJson, Encoding.UTF8, "application/json")

                    Dim httpResponse = Await client.PostAsync(searchEndpointConfig, content)
                    result.HttpStatusCode = CInt(httpResponse.StatusCode)

                    If Not httpResponse.IsSuccessStatusCode Then
                        result.ErrorMessage = $"Sandbox GSTIN Search API returned HTTP status {result.HttpStatusCode} ({httpResponse.ReasonPhrase})."
                        Return result
                    End If

                    Dim jsonContent = Await httpResponse.Content.ReadAsStringAsync()
                    Return ParseSandboxGstResponse(jsonContent, cleanGstin, result)
                End Using
            Catch ex As TaskCanceledException
                result.HttpStatusCode = 408
                result.ErrorMessage = "Request to Sandbox GST Verification API timed out (8s limit)."
                Return result
            Catch ex As Exception
                result.HttpStatusCode = 500
                result.ErrorMessage = $"Network or communication error with Sandbox API: {ex.Message}"
                Return result
            End Try
        End Function

        ''' <summary>
        ''' Safe JWT Token Acquisition: Calls POST https://api.sandbox.co.in/authenticate
        ''' Caches token in memory until expiration (23 hours).
        ''' Secrets and tokens are never logged.
        ''' </summary>
        Private Async Function AuthenticateAsync(client As HttpClient, apiKey As String, apiSecret As String) As Task(Of AuthResult)
            SyncLock _tokenLock
                If Not String.IsNullOrWhiteSpace(_cachedJwtToken) AndAlso DateTime.UtcNow < _tokenExpiry Then
                    Return New AuthResult With {
                        .Success = True,
                        .Token = _cachedJwtToken,
                        .HttpStatusCode = 200,
                        .ErrorMessage = "Cached token valid"
                    }
                End If
            End SyncLock

            Dim authUrl As String = "https://api.sandbox.co.in/authenticate"

            Try
                client.DefaultRequestHeaders.Clear()
                client.DefaultRequestHeaders.Add("User-Agent", "StaffAutomation/1.0 (.NET 8 WinForms)")
                client.DefaultRequestHeaders.Add("Accept", "application/json")
                client.DefaultRequestHeaders.Add("x-api-key", apiKey)
                client.DefaultRequestHeaders.Add("x-api-secret", apiSecret)
                client.DefaultRequestHeaders.Add("x-api-version", "1.0")

                ' Empty POST body for /authenticate
                Dim emptyContent As New StringContent("{}", Encoding.UTF8, "application/json")
                Dim response = Await client.PostAsync(authUrl, emptyContent)
                Dim statusCode As Integer = CInt(response.StatusCode)

                If Not response.IsSuccessStatusCode Then
                    Return New AuthResult With {
                        .Success = False,
                        .Token = String.Empty,
                        .HttpStatusCode = statusCode,
                        .ErrorMessage = $"Authentication endpoint returned HTTP {statusCode} ({response.ReasonPhrase}). Check GstApiKey and GstApiSecret."
                    }
                End If

                Dim json As String = Await response.Content.ReadAsStringAsync()
                Using doc As JsonDocument = JsonDocument.Parse(json)
                    Dim root = doc.RootElement
                    Dim token As String = String.Empty

                    Dim tokenElem As JsonElement = Nothing
                    Dim dataElem As JsonElement = Nothing
                    Dim subTokenElem As JsonElement = Nothing

                    If root.TryGetProperty("access_token", tokenElem) AndAlso tokenElem.ValueKind = JsonValueKind.String Then
                        token = tokenElem.GetString()
                    ElseIf root.TryGetProperty("data", dataElem) AndAlso dataElem.ValueKind = JsonValueKind.Object Then
                        If dataElem.TryGetProperty("access_token", subTokenElem) AndAlso subTokenElem.ValueKind = JsonValueKind.String Then
                            token = subTokenElem.GetString()
                        End If
                    ElseIf root.TryGetProperty("token", tokenElem) AndAlso tokenElem.ValueKind = JsonValueKind.String Then
                        token = tokenElem.GetString()
                    End If

                    If String.IsNullOrWhiteSpace(token) Then
                        Return New AuthResult With {
                            .Success = False,
                            .Token = String.Empty,
                            .HttpStatusCode = statusCode,
                            .ErrorMessage = "Authentication response did not contain an access_token."
                        }
                    End If

                    SyncLock _tokenLock
                        _cachedJwtToken = token.Trim()
                        _tokenExpiry = DateTime.UtcNow.AddHours(23)
                    End SyncLock

                    Return New AuthResult With {
                        .Success = True,
                        .Token = _cachedJwtToken,
                        .HttpStatusCode = 200,
                        .ErrorMessage = "Authentication successful"
                    }
                End Using
            Catch ex As Exception
                Return New AuthResult With {
                    .Success = False,
                    .Token = String.Empty,
                    .HttpStatusCode = 500,
                    .ErrorMessage = $"Authentication exception: {ex.Message}"
                }
            End Try
        End Function

        Private Function ParseSandboxGstResponse(json As String, gstin As String, result As GstVerificationResult) As GstVerificationResult
            Try
#If DEBUG Then
                ' Save safe debug JSON in DEBUG builds only
                SaveDebugJson(json)
#End If

                Using doc As JsonDocument = JsonDocument.Parse(json)
                    Dim root = doc.RootElement

                    ' Deep Unwrap: Navigate root -> data -> data (or result / taxpayerDetails)
                    Dim current As JsonElement = root
                    For i As Integer = 0 To 3
                        If current.ValueKind = JsonValueKind.Object Then
                            Dim tempElem As JsonElement = Nothing
                            If current.TryGetProperty("lgnm", tempElem) OrElse current.TryGetProperty("tradeNam", tempElem) OrElse current.TryGetProperty("legal_name", tempElem) Then
                                ' Reached actual taxpayer object!
                                Exit For
                            End If
                            If current.TryGetProperty("data", tempElem) AndAlso tempElem.ValueKind = JsonValueKind.Object Then
                                current = tempElem
                            ElseIf current.TryGetProperty("result", tempElem) AndAlso tempElem.ValueKind = JsonValueKind.Object Then
                                current = tempElem
                            ElseIf current.TryGetProperty("taxpayerDetails", tempElem) AndAlso tempElem.ValueKind = JsonValueKind.Object Then
                                current = tempElem
                            Else
                                Exit For
                            End If
                        Else
                            Exit For
                        End If
                    Next
                    Dim dataElement As JsonElement = current

                    If dataElement.ValueKind <> JsonValueKind.Object Then
                        result.ErrorMessage = "Provider response does not contain a valid JSON data object."
                        Return result
                    End If

                    Dim legalName = GetJsonString(dataElement, "lgnm", "legalName", "legal_name", "legal_name_of_business")
                    Dim tradeName = GetJsonString(dataElement, "tradeNam", "tradeName", "trade_name")
                    Dim rawStatus = GetJsonString(dataElement, "sts", "status", "gstStatus", "gst_status")
                    Dim constitution = GetJsonString(dataElement, "ctb", "constitution_of_business", "constitution")
                    Dim taxpayerType = GetJsonString(dataElement, "dty", "taxpayer_type", "gstType")
                    Dim regDate = GetJsonString(dataElement, "rgdt", "registration_date")

                    If String.IsNullOrWhiteSpace(legalName) AndAlso String.IsNullOrWhiteSpace(tradeName) Then
                        result.ErrorMessage = "Provider response missing Legal Name and Trade Name fields."
                        Return result
                    End If

                    ' Address extraction from pradr.addr or principal_place_address
                    Dim district As String = String.Empty
                    Dim pincode As String = String.Empty
                    Dim stateName As String = result.Record.StateName
                    Dim constructedAddress As String = String.Empty

                    Dim pradrProp As JsonElement = Nothing
                    Dim addrProp As JsonElement = Nothing

                    If dataElement.TryGetProperty("pradr", pradrProp) AndAlso pradrProp.ValueKind = JsonValueKind.Object Then
                        addrProp = pradrProp
                        Dim subAddr As JsonElement = Nothing
                        If pradrProp.TryGetProperty("addr", subAddr) AndAlso subAddr.ValueKind = JsonValueKind.Object Then
                            addrProp = subAddr
                        End If
                    ElseIf dataElement.TryGetProperty("principal_place_address", pradrProp) AndAlso pradrProp.ValueKind = JsonValueKind.Object Then
                        addrProp = pradrProp
                    ElseIf dataElement.TryGetProperty("addr", pradrProp) AndAlso pradrProp.ValueKind = JsonValueKind.Object Then
                        addrProp = pradrProp
                    Else
                        addrProp = dataElement
                    End If

                    If addrProp.ValueKind = JsonValueKind.Object Then
                        Dim flno = GetJsonString(addrProp, "flno", "floor")
                        Dim bno = GetJsonString(addrProp, "bno", "building", "building_name")
                        Dim bnm = GetJsonString(addrProp, "bnm", "building_name")
                        Dim st = GetJsonString(addrProp, "st", "street")
                        Dim loc = GetJsonString(addrProp, "loc", "location")
                        Dim locality = GetJsonString(addrProp, "locality")
                        Dim landmark = GetJsonString(addrProp, "landMark", "landmark")
                        district = GetJsonString(addrProp, "dst", "district", "city")
                        pincode = GetJsonString(addrProp, "pncd", "pincode", "zip")
                        Dim stcd = GetJsonString(addrProp, "stcd", "state")

                        If Not String.IsNullOrWhiteSpace(stcd) Then
                            Dim mappedState = GstVerificationEngine.GetStateNameFromCode(stcd)
                            If Not String.IsNullOrWhiteSpace(mappedState) Then
                                stateName = mappedState
                                result.Record.StateCode = stcd
                            End If
                        End If

                        Dim parts As New List(Of String)()
                        If Not String.IsNullOrWhiteSpace(flno) Then parts.Add(flno)
                        If Not String.IsNullOrWhiteSpace(bno) Then parts.Add(bno)
                        If Not String.IsNullOrWhiteSpace(bnm) AndAlso Not bnm.Equals(bno, StringComparison.OrdinalIgnoreCase) Then parts.Add(bnm)
                        If Not String.IsNullOrWhiteSpace(st) Then parts.Add(st)
                        If Not String.IsNullOrWhiteSpace(loc) Then parts.Add(loc)
                        If Not String.IsNullOrWhiteSpace(locality) AndAlso Not locality.Equals(loc, StringComparison.OrdinalIgnoreCase) Then parts.Add(locality)
                        If Not String.IsNullOrWhiteSpace(landmark) Then parts.Add($"Near {landmark}")

                        constructedAddress = String.Join(", ", parts).Trim()
                    End If

                    ' Trade Name Fallback Rule
                    If String.IsNullOrWhiteSpace(tradeName) Then
                        tradeName = legalName
                        result.MissingFields.Add("TradeName (Fell back to LegalName)")
                    End If

                    ' Status & Active State Rule
                    Dim isActiveStatus As Boolean = False
                    Dim finalStatusText As String = "Inactive"
                    If Not String.IsNullOrWhiteSpace(rawStatus) Then
                        finalStatusText = rawStatus.Trim()
                        If finalStatusText.Equals("Active", StringComparison.OrdinalIgnoreCase) Then
                            isActiveStatus = True
                        End If
                    End If

                    ' Scheme Mapping
                    Dim mappedGstType As String = "Regular Monthly"
                    If Not String.IsNullOrWhiteSpace(taxpayerType) Then
                        If taxpayerType.ToLower().Contains("composition") Then
                            mappedGstType = "Composition Scheme"
                        ElseIf taxpayerType.ToLower().Contains("qrmp") Then
                            mappedGstType = "Regular QRMP (Quarterly)"
                        ElseIf taxpayerType.ToLower().Contains("unregistered") OrElse taxpayerType.ToLower().Contains("exempt") Then
                            mappedGstType = "Unregistered / Exempt"
                        End If
                    End If

                    ' Construct Verified Record
                    result.Record.LegalName = legalName
                    result.Record.TradeName = tradeName
                    result.Record.StateName = stateName
                    result.Record.District = district
                    result.Record.Pincode = pincode
                    result.Record.Address = constructedAddress
                    result.Record.EntityType = If(Not String.IsNullOrWhiteSpace(constitution), constitution, result.Record.EntityType)
                    result.Record.GstType = mappedGstType
                    result.Record.GstStatus = finalStatusText
                    result.Record.IsActive = isActiveStatus
                    result.Record.IsLiveApiData = True

                    result.Success = True
                    result.IsLiveApiData = True
                    result.ErrorMessage = "Taxpayer record verified successfully via Sandbox GST Compliance API."
                    Return result
                End Using
            Catch ex As Exception
                result.ErrorMessage = $"Failed to parse provider JSON response: {ex.Message}"
                Return result
            End Try
        End Function

#If DEBUG Then
        Private Shared Sub SaveDebugJson(json As String)
            Try
                Dim logDir = IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs")
                If Not IO.Directory.Exists(logDir) Then IO.Directory.CreateDirectory(logDir)
                Dim filePath = IO.Path.Combine(logDir, "gst_debug_last_response.json")
                IO.File.WriteAllText(filePath, json)
            Catch ex As Exception
            End Try
        End Sub
#End If

        Private Function GetJsonString(element As JsonElement, ParamArray keys() As String) As String
            If element.ValueKind <> JsonValueKind.Object Then Return String.Empty
            For Each k In keys
                Dim prop As JsonElement = Nothing
                If element.TryGetProperty(k, prop) Then
                    If prop.ValueKind = JsonValueKind.String Then
                        Dim val = prop.GetString()
                        If Not String.IsNullOrWhiteSpace(val) Then Return val.Trim()
                    End If
                End If
            Next
            Return String.Empty
        End Function
    End Class
End Namespace
