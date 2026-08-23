Option Strict On
Option Explicit On

Imports System.Threading.Tasks

Namespace Forms.Main
    ''' <summary>
    ''' Offline Statutory Syntax and PAN Inference Fallback Provider.
    ''' Extracts statutory PAN Number, State Code, and State Name with 100% mathematical certainty from GSTIN structure.
    ''' Explicitly labels entity type as "Inferred from PAN (Unverified)" and leaves Trade/Legal Name and Address empty.
    ''' Sets GstStatus = "Not Verified", IsActive = False, and IsLiveApiData = False.
    ''' </summary>
    Public Class StatutoryFallbackGstProvider
        Implements IGstVerificationProvider

        Public ReadOnly Property ProviderName As String Implements IGstVerificationProvider.ProviderName
            Get
                Return "Statutory Syntax and PAN Inference Fallback"
            End Get
        End Property

        Public Function VerifyGstinAsync(gstin As String) As Task(Of GstVerificationResult) Implements IGstVerificationProvider.VerifyGstinAsync
            Dim record = GstVerificationEngine.ExtractStatutoryDetailsFromGstin(gstin)

            ' Mandatory Failure/Unverified State Rules
            record.LegalName = String.Empty
            record.TradeName = String.Empty
            record.Address = String.Empty
            record.District = String.Empty
            record.Pincode = String.Empty
            record.EntityType = GstVerificationEngine.InferEntityTypeFromPanCategory(record.PanNumber)
            record.GstStatus = "Not Verified"
            record.IsActive = False
            record.IsLiveApiData = False

            Dim result As New GstVerificationResult With {
                .ProviderName = Me.ProviderName,
                .Success = True,
                .IsLiveApiData = False,
                .HttpStatusCode = 200,
                .ErrorMessage = "Statutory GSTIN syntax verified. PAN and State derived. Live API credentials unavailable for name/address lookup.",
                .Record = record
            }

            Return Task.FromResult(result)
        End Function
    End Class
End Namespace
