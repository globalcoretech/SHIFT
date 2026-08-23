Option Strict On
Option Explicit On

Imports System.Threading.Tasks

Namespace Forms.Main
    ''' <summary>
    ''' Standardized contract abstraction for GSTIN verification providers.
    ''' </summary>
    Public Interface IGstVerificationProvider
        ''' <summary>
        ''' Gets the human-readable identifier of the provider.
        ''' </summary>
        ReadOnly Property ProviderName As String

        ''' <summary>
        ''' Verifies a 15-character GSTIN against the provider contract.
        ''' </summary>
        Function VerifyGstinAsync(gstin As String) As Task(Of GstVerificationResult)
    End Interface
End Namespace
