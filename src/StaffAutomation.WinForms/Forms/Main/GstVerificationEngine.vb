Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Text.RegularExpressions
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Logging

Namespace Forms.Main
    ''' <summary>
    ''' Ultra-Robust Statutory GSTIN Verification and Live GSP API Lookup Engine.
    ''' Uses clean Provider Architecture (IGstVerificationProvider) for live Sandbox.co.in GSP verification
    ''' and offline Statutory Syntax and PAN Inference fallback.
    ''' Never injects synthetic/dummy fake names or addresses.
    ''' </summary>
    Public Class GstVerificationEngine
        Public Class GstRecord
            Public Property Gstin As String = String.Empty
            Public Property TradeName As String = String.Empty
            Public Property LegalName As String = String.Empty
            Public Property PanNumber As String = String.Empty
            Public Property StateCode As String = String.Empty
            Public Property StateName As String = String.Empty
            Public Property District As String = String.Empty
            Public Property Pincode As String = String.Empty
            Public Property Address As String = String.Empty
            Public Property EntityType As String = "Inferred from PAN (Unverified)"
            Public Property GstType As String = "Regular Monthly"
            Public Property GstStatus As String = "Not Verified"
            Public Property IsActive As Boolean = False
            Public Property ContactPerson As String = String.Empty
            Public Property Phone As String = String.Empty
            Public Property Email As String = String.Empty
            Public Property IsLiveApiData As Boolean = False
        End Class

        ''' <summary>
        ''' Validates standard 15-character Indian Statutory GSTIN Format (2 state digits + 10 PAN chars + 1 entity digit + 'Z' + 1 check digit).
        ''' </summary>
        Public Shared Function IsValidGstinSyntax(gstin As String) As Boolean
            If String.IsNullOrWhiteSpace(gstin) Then Return False
            Dim clean = gstin.Trim().ToUpper()
            If clean.Length <> 15 Then Return False
            Return Regex.IsMatch(clean, "^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$")
        End Function

        ''' <summary>
        ''' Synchronously extracts statutory PAN, State Code, State Name, and PAN Category Inference from 15-digit GSTIN syntax.
        ''' </summary>
        Public Shared Function ExtractStatutoryDetailsFromGstin(gstin As String) As GstRecord
            If Not IsValidGstinSyntax(gstin) Then Return New GstRecord()
            Dim cleanGstin = gstin.Trim().ToUpper()
            Dim stateCode = cleanGstin.Substring(0, 2)
            Dim stateName = GetStateNameFromCode(stateCode)
            Dim pan = cleanGstin.Substring(2, 10)
            Dim entityType = InferEntityTypeFromPanCategory(pan)

            Return New GstRecord With {
                .Gstin = cleanGstin,
                .PanNumber = pan,
                .StateCode = stateCode,
                .StateName = stateName,
                .EntityType = entityType,
                .GstStatus = "Not Verified",
                .IsActive = False,
                .IsLiveApiData = False
            }
        End Function

        ''' <summary>
        ''' Primary API Entry Point: Executes GST verification using the configured IGstVerificationProvider.
        ''' Returns comprehensive GstVerificationResult containing status, diagnostic details, HTTP codes, and GstRecord.
        ''' </summary>
        Public Shared Async Function VerifyGstinWithProviderAsync(gstin As String, Optional logger As IAppLogger = Nothing) As Task(Of GstVerificationResult)
            If String.IsNullOrWhiteSpace(gstin) Then
                Throw New ArgumentException("Please enter a GSTIN number.")
            End If

            Dim cleanGstin = gstin.Trim().ToUpper()
            If Not IsValidGstinSyntax(cleanGstin) Then
                Throw New ArgumentException("Please enter a valid 15-character GSTIN (e.g. 27AAAAA0000A1Z5).")
            End If

            ' Safe Audit Logging (Masked Secrets & Masked GSTIN)
            If logger IsNot Nothing Then
                logger.LogInfo($"GST verification requested for GSTIN: {MaskGstinForAudit(cleanGstin)}")
            End If

            ' Determine Provider
            Dim providerTypeConfig = ConfigurationManager.AppSettings("GstProviderType")
            Dim provider As IGstVerificationProvider

            If String.Equals(providerTypeConfig, "SandboxGstProvider", StringComparison.OrdinalIgnoreCase) Then
                provider = New SandboxGstProvider()
            Else
                provider = New StatutoryFallbackGstProvider()
            End If

            ' Execute Provider
            Dim result = Await provider.VerifyGstinAsync(cleanGstin)

            ' Log Diagnostic Outcome (Secret-safe)
            If logger IsNot Nothing Then
                logger.LogInfo($"GST Provider [{result.ProviderName}] completed for [{MaskGstinForAudit(cleanGstin)}]. HTTP Status: {result.HttpStatusCode}, Success: {result.Success}, IsLiveApiData: {result.IsLiveApiData}")
            End If

            Return result
        End Function

        ''' <summary>
        ''' Backwards-compatible overload returning GstRecord.
        ''' Calls VerifyGstinWithProviderAsync and returns the resulting Record object.
        ''' </summary>
        Public Shared Async Function FetchGstDetailsAsync(gstin As String) As Task(Of GstRecord)
            Dim result = Await VerifyGstinWithProviderAsync(gstin)
            Return result.Record
        End Function

        ''' <summary>
        ''' Derives statutory PAN-category inference when official GST Constitution of Business is unavailable.
        ''' Clearly labels the result as an inference.
        ''' </summary>
        Public Shared Function InferEntityTypeFromPanCategory(pan As String) As String
            If String.IsNullOrEmpty(pan) OrElse pan.Length < 4 Then Return "Inferred from PAN (Unverified)"
            Select Case pan(3)
                Case "C"c : Return "Inferred from PAN: Company (Unverified)"
                Case "F"c : Return "Inferred from PAN: Partnership/LLP (Unverified)"
                Case "P"c : Return "Inferred from PAN: Proprietorship (Unverified)"
                Case "H"c : Return "Inferred from PAN: HUF (Unverified)"
                Case "T"c : Return "Inferred from PAN: Trust (Unverified)"
                Case "A"c : Return "Inferred from PAN: Association (Unverified)"
                Case Else : Return "Inferred from PAN: Individual (Unverified)"
            End Select
        End Function

        Public Shared Function GetStateNameFromCode(code As String) As String
            Select Case code
                Case "01" : Return "01 - Jammu & Kashmir"
                Case "02" : Return "02 - Himachal Pradesh"
                Case "03" : Return "03 - Punjab"
                Case "04" : Return "04 - Chandigarh"
                Case "05" : Return "05 - Uttarakhand"
                Case "06" : Return "06 - Haryana"
                Case "07" : Return "07 - Delhi"
                Case "08" : Return "08 - Rajasthan"
                Case "09" : Return "09 - Uttar Pradesh"
                Case "10" : Return "10 - Bihar"
                Case "11" : Return "11 - Sikkim"
                Case "12" : Return "12 - Arunachal Pradesh"
                Case "13" : Return "13 - Nagaland"
                Case "14" : Return "14 - Manipur"
                Case "15" : Return "15 - Mizoram"
                Case "16" : Return "16 - Tripura"
                Case "17" : Return "17 - Meghalaya"
                Case "18" : Return "18 - Assam"
                Case "19" : Return "19 - West Bengal"
                Case "20" : Return "20 - Jharkhand"
                Case "21" : Return "21 - Odisha"
                Case "22" : Return "22 - Chhattisgarh"
                Case "23" : Return "23 - Madhya Pradesh"
                Case "24" : Return "24 - Gujarat"
                Case "25" : Return "25 - Daman & Diu"
                Case "26" : Return "26 - Dadra & Nagar Haveli"
                Case "27" : Return "27 - Maharashtra"
                Case "28" : Return "28 - Andhra Pradesh"
                Case "29" : Return "29 - Karnataka"
                Case "30" : Return "30 - Goa"
                Case "31" : Return "31 - Lakshadweep"
                Case "32" : Return "32 - Kerala"
                Case "33" : Return "33 - Tamil Nadu"
                Case "34" : Return "34 - Puducherry"
                Case "35" : Return "35 - Andaman & Nicobar"
                Case "36" : Return "36 - Telangana"
                Case "37" : Return "37 - Andhra Pradesh (New)"
                Case "38" : Return "38 - Ladakh"
                Case Else : Return "27 - Maharashtra"
            End Select
        End Function

        Public Shared Function MaskGstinForAudit(gstin As String) As String
            If String.IsNullOrEmpty(gstin) OrElse gstin.Length < 15 Then Return "*****"
            Return gstin.Substring(0, 2) & "*****" & gstin.Substring(11)
        End Function
    End Class
End Namespace
