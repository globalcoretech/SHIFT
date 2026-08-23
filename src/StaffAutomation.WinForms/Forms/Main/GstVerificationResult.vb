Option Strict On
Option Explicit On

Imports System.Collections.Generic

Namespace Forms.Main
    ''' <summary>
    ''' Encapsulates the complete result of a GST verification attempt,
    ''' including status, diagnostic details, missing fields, and structured GstRecord data.
    ''' </summary>
    Public Class GstVerificationResult
        ''' <summary>
        ''' Gets or sets whether the verification operation succeeded (live API success or valid statutory syntax fallback).
        ''' </summary>
        Public Property Success As Boolean = False

        ''' <summary>
        ''' Gets or sets whether taxpayer details were successfully returned and verified by a live API.
        ''' </summary>
        Public Property IsLiveApiData As Boolean = False

        ''' <summary>
        ''' Gets or sets the HTTP status code returned by the API (200, 401, 403, 404, 429, etc.).
        ''' </summary>
        Public Property HttpStatusCode As Integer = 0

        ''' <summary>
        ''' Gets or sets human-readable error or diagnostic message.
        ''' </summary>
        Public Property ErrorMessage As String = String.Empty

        ''' <summary>
        ''' Gets or sets the name of the executing provider.
        ''' </summary>
        Public Property ProviderName As String = String.Empty

        ''' <summary>
        ''' Gets or sets the verified or derived GST record DTO.
        ''' </summary>
        Public Property Record As GstVerificationEngine.GstRecord = New GstVerificationEngine.GstRecord()

        ''' <summary>
        ''' Gets or sets a list of optional or expected fields missing from the provider response.
        ''' </summary>
        Public Property MissingFields As List(Of String) = New List(Of String)()
    End Class
End Namespace
