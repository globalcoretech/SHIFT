Option Strict On
Option Explicit On

Imports System
Imports System.Runtime.Versioning
Imports System.Security.Cryptography
Imports System.Text

Namespace Configuration
    ''' <summary>
    ''' Provides Windows DPAPI encryption and decryption for persisted database user credentials.
    ''' Protects SQL Authentication passwords from being stored as plain text on disk.
    ''' </summary>
    <SupportedOSPlatform("windows")>
    Public Module DatabaseCredentialProtector
        ''' <summary>
        ''' Encrypts a plain-text password using Windows DPAPI (DataProtectionScope.CurrentUser).
        ''' Returns Base64-encoded encrypted string.
        ''' </summary>
        <SupportedOSPlatform("windows")>
        Public Function ProtectPassword(plainPassword As String) As String
            If String.IsNullOrEmpty(plainPassword) Then Return ""
            Try
                Dim bytes = Encoding.UTF8.GetBytes(plainPassword)
                Dim encryptedBytes = ProtectedData.Protect(bytes, Nothing, DataProtectionScope.CurrentUser)
                Return Convert.ToBase64String(encryptedBytes)
            Catch ex As Exception
                ' Fallback or re-throw configuration exception if DPAPI is unavailable
                Throw New InvalidOperationException("Failed to secure database credentials via Windows DPAPI.", ex)
            End Try
        End Function

        ''' <summary>
        ''' Decrypts a DPAPI Base64-encoded encrypted password string.
        ''' Returns original plain-text password for connection execution.
        ''' </summary>
        <SupportedOSPlatform("windows")>
        Public Function UnprotectPassword(encryptedPassword As String) As String
            If String.IsNullOrEmpty(encryptedPassword) Then Return ""
            Try
                Dim encryptedBytes = Convert.FromBase64String(encryptedPassword)
                Dim bytes = ProtectedData.Unprotect(encryptedBytes, Nothing, DataProtectionScope.CurrentUser)
                Return Encoding.UTF8.GetString(bytes)
            Catch ex As Exception
                ' Suppress sensitive exception details to avoid leaking credential context
                Return ""
            End Try
        End Function
    End Module
End Namespace
