Imports System
Imports System.IO
Imports System.Management
Imports System.Security.Cryptography
Imports System.Text
Imports System.Runtime.Versioning
Imports StaffAutomation.Core.DTOs

Namespace Security
    <SupportedOSPlatform("windows")>
    Public Class DeviceFingerprintHelper

        Private Shared ReadOnly IdentityFilePath As String = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GlobalCoreTech", "StaffAutomation", "DeviceIdentity.dat"
        )

        ''' <summary>
        ''' Gets the comprehensive client info required for device validation.
        ''' </summary>
        Public Shared Function GetClientInfo() As DeviceClientInfoDto
            Return New DeviceClientInfoDto() With {
                .DeviceGuid = GetOrCreateDeviceGuid(),
                .HardwareFingerprint = GenerateHardwareFingerprint(),
                .MachineName = Environment.MachineName,
                .OSVersion = Environment.OSVersion.ToString()
            }
        End Function

        ''' <summary>
        ''' Gets the existing Device GUID or generates and protects a new one using DPAPI.
        ''' </summary>
        Private Shared Function GetOrCreateDeviceGuid() As Guid
            Try
                If File.Exists(IdentityFilePath) Then
                    Dim encryptedBytes = File.ReadAllBytes(IdentityFilePath)
                    Dim decryptedBytes = ProtectedData.Unprotect(encryptedBytes, Nothing, DataProtectionScope.CurrentUser)
                    Dim guidString = Encoding.UTF8.GetString(decryptedBytes)
                    Dim parsedGuid As Guid
                    If Guid.TryParse(guidString, parsedGuid) Then
                        Return parsedGuid
                    End If
                End If
            Catch ex As Exception
                ' Silently swallow file or DPAPI errors and fallback to generating a new one
            End Try

            ' Generate new
            Dim newGuid = Guid.NewGuid()
            Try
                Dim dir = Path.GetDirectoryName(IdentityFilePath)
                If Not Directory.Exists(dir) Then
                    Directory.CreateDirectory(dir)
                End If
                Dim rawBytes = Encoding.UTF8.GetBytes(newGuid.ToString())
                Dim encryptedBytes = ProtectedData.Protect(rawBytes, Nothing, DataProtectionScope.CurrentUser)
                File.WriteAllBytes(IdentityFilePath, encryptedBytes)
            Catch ex As Exception
                ' If we cannot persist, return the new guid for this session
            End Try

            Return newGuid
        End Function

        ''' <summary>
        ''' Generates a SHA-256 hash of the hardware components (Motherboard, CPU, Volume Serial).
        ''' Handles unavailable WMI properties gracefully.
        ''' </summary>
        Private Shared Function GenerateHardwareFingerprint() As String
            Dim rawFingerprint As New StringBuilder()

            rawFingerprint.Append(GetWmiProperty("Win32_BaseBoard", "SerialNumber"))
            rawFingerprint.Append("|")
            rawFingerprint.Append(GetWmiProperty("Win32_Processor", "ProcessorId"))
            rawFingerprint.Append("|")
            
            Dim drive As String = Path.GetPathRoot(Environment.SystemDirectory).Substring(0, 2)
            rawFingerprint.Append(GetVolumeSerial(drive))

            Using sha256 = System.Security.Cryptography.SHA256.Create()
                Dim bytes = Encoding.UTF8.GetBytes(rawFingerprint.ToString())
                Dim hashBytes As Byte() = sha256.ComputeHash(bytes)
                Dim hexString As New StringBuilder(hashBytes.Length * 2)
                For Each b As Byte In hashBytes
                    hexString.AppendFormat("{0:x2}", b)
                Next
                Return hexString.ToString()
            End Using
        End Function

        Private Shared Function GetWmiProperty(wmiClass As String, wmiProperty As String) As String
            Try
                Using searcher As New ManagementObjectSearcher($"SELECT {wmiProperty} FROM {wmiClass}")
                    For Each mo As ManagementObject In searcher.Get()
                        Dim val = mo(wmiProperty)
                        If val IsNot Nothing Then
                            Return val.ToString().Trim()
                        End If
                    Next
                End Using
            Catch
                ' Gracefully fallback if WMI is unavailable or access is denied
            End Try
            Return "UNKNOWN"
        End Function

        Private Shared Function GetVolumeSerial(drive As String) As String
            Try
                Using searcher As New ManagementObjectSearcher($"SELECT VolumeSerialNumber FROM Win32_LogicalDisk WHERE DeviceID='{drive}'")
                    For Each mo As ManagementObject In searcher.Get()
                        Dim val = mo("VolumeSerialNumber")
                        If val IsNot Nothing Then
                            Return val.ToString().Trim()
                        End If
                    Next
                End Using
            Catch
            End Try
            Return "UNKNOWN"
        End Function

    End Class
End Namespace
