Imports System
Imports System.Net.Http
Imports System.Text.Json
Imports System.Threading.Tasks
Imports System.IO
Imports System.Security.Cryptography
Imports StaffAutomation.Core.Configuration
Imports StaffAutomation.Core.Constants
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Logging

Namespace Services
    Public Class UpdateService
        Implements IUpdateService

        Private ReadOnly _config As IAppConfiguration
        Private ReadOnly _logger As IAppLogger
        Private Shared ReadOnly _httpClient As New HttpClient()

        Shared Sub New()
            _httpClient.Timeout = TimeSpan.FromSeconds(10)
        End Sub

        Public Sub New(config As IAppConfiguration, logger As IAppLogger)
            If config Is Nothing Then Throw New ArgumentNullException(NameOf(config))
            If logger Is Nothing Then Throw New ArgumentNullException(NameOf(logger))
            
            _config = config
            _logger = logger
        End Sub

        Public Async Function CheckForUpdatesAsync() As Task(Of UpdateCheckResult) Implements IUpdateService.CheckForUpdatesAsync
            Try
                Dim url = _config.GetSetting("UpdateManifestUrl", "")
                If String.IsNullOrWhiteSpace(url) Then
                    Return New UpdateCheckResult With {.IsSuccess = False, .ErrorMessage = "Update check failed due to missing configuration."}
                End If

                Dim response = Await _httpClient.GetAsync(url)
                If Not response.IsSuccessStatusCode Then
                    _logger.LogWarn($"Update manifest fetch failed with status: {response.StatusCode}", "UpdateService")
                    Return New UpdateCheckResult With {.IsSuccess = False, .ErrorMessage = "Unable to connect to the update server."}
                End If

                Dim json = Await response.Content.ReadAsStringAsync()
                Dim manifest As UpdateManifestDto = Nothing

                Try
                    Dim options As New JsonSerializerOptions With {
                        .PropertyNameCaseInsensitive = True
                    }
                    manifest = JsonSerializer.Deserialize(Of UpdateManifestDto)(json, options)
                Catch ex As Exception
                    _logger.LogError("Failed to deserialize update manifest", "UpdateService", ex)
                    Return New UpdateCheckResult With {.IsSuccess = False, .ErrorMessage = "Update server returned invalid data."}
                End Try

                If manifest Is Nothing OrElse String.IsNullOrWhiteSpace(manifest.LatestVersion) Then
                    Return New UpdateCheckResult With {.IsSuccess = False, .ErrorMessage = "Update server returned an invalid manifest."}
                End If

                ' Version Comparison
                Dim currentVersion As Version = Nothing
                Dim latestVersion As Version = Nothing

                If Version.TryParse(AppConstants.AppVersion, currentVersion) AndAlso Version.TryParse(manifest.LatestVersion, latestVersion) Then
                    If latestVersion > currentVersion Then
                        Return New UpdateCheckResult With {.IsSuccess = True, .IsUpdateAvailable = True, .Manifest = manifest}
                    End If
                Else
                    Return New UpdateCheckResult With {.IsSuccess = False, .ErrorMessage = "Failed to parse application version data."}
                End If

                Return New UpdateCheckResult With {.IsSuccess = True, .IsUpdateAvailable = False}
            Catch ex As Exception
                _logger.LogError("Update check encountered an unexpected error", "UpdateService", ex)
                Return New UpdateCheckResult With {.IsSuccess = False, .ErrorMessage = "Unable to check for updates right now. Please try again later."}
            End Try
        End Function
        Public Async Function DownloadAndVerifyUpdateAsync(manifest As UpdateManifestDto, progress As IProgress(Of Integer)) As Task(Of UpdateCheckResult) Implements IUpdateService.DownloadAndVerifyUpdateAsync
            Try
                ' 1. Validate URI
                If String.IsNullOrWhiteSpace(manifest.DownloadUrl) Then
                    Return New UpdateCheckResult With {.IsSuccess = False, .ErrorMessage = "Update manifest is missing a download URL."}
                End If

                Dim downloadUri As Uri = Nothing
                If Not Uri.TryCreate(manifest.DownloadUrl, UriKind.Absolute, downloadUri) OrElse
                   Not downloadUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) Then
                    Return New UpdateCheckResult With {.IsSuccess = False, .ErrorMessage = "Update URL is invalid or insecure (HTTPS is required)."}
                End If

                ' 2. Validate SHA256 Format
                If String.IsNullOrWhiteSpace(manifest.SHA256) OrElse manifest.SHA256.Trim().Length <> 64 Then
                    Return New UpdateCheckResult With {.IsSuccess = False, .ErrorMessage = "Update manifest is missing a valid SHA-256 hash."}
                End If
                Dim expectedHash = manifest.SHA256.Trim().ToLowerInvariant()

                ' 3. Validate Version (Prevent Downgrades)
                Dim currentVersion As Version = Nothing
                Dim latestVersion As Version = Nothing
                If Not Version.TryParse(AppConstants.AppVersion, currentVersion) OrElse
                   Not Version.TryParse(manifest.LatestVersion, latestVersion) OrElse
                   latestVersion <= currentVersion Then
                    Return New UpdateCheckResult With {.IsSuccess = False, .ErrorMessage = "The update version is older or the same as the current application version."}
                End If

                ' 4. Setup temporary path
                Dim updateDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StaffAutomation", "Updates")
                If Not Directory.Exists(updateDir) Then
                    Directory.CreateDirectory(updateDir)
                End If

                Dim tempFilePath = Path.Combine(updateDir, $"update_{latestVersion}.tmp")
                Dim finalFilePath = Path.Combine(updateDir, $"StaffAutomation_v{latestVersion}_verified.exe")

                ' Cleanup existing stale/failed files
                If File.Exists(tempFilePath) Then File.Delete(tempFilePath)
                If File.Exists(finalFilePath) Then
                    ' Re-verify existing file
                    Dim existingHash = ComputeFileHash(finalFilePath)
                    If String.Equals(existingHash, expectedHash, StringComparison.OrdinalIgnoreCase) Then
                        progress?.Report(100)
                        Return New UpdateCheckResult With {.IsSuccess = True, .DownloadedFilePath = finalFilePath}
                    Else
                        File.Delete(finalFilePath)
                    End If
                End If

                ' 5. Download
                Using response = Await _httpClient.GetAsync(downloadUri, HttpCompletionOption.ResponseHeadersRead)
                    If Not response.IsSuccessStatusCode Then
                        Return New UpdateCheckResult With {.IsSuccess = False, .ErrorMessage = "Failed to download update package."}
                    End If

                    Dim totalBytes = response.Content.Headers.ContentLength.GetValueOrDefault()
                    Using contentStream = Await response.Content.ReadAsStreamAsync()
                        Using fileStream = New FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, True)
                            Dim buffer(8191) As Byte
                            Dim totalRead As Long = 0
                            Dim bytesRead As Integer

                            Do
                                bytesRead = Await contentStream.ReadAsync(buffer, 0, buffer.Length)
                                If bytesRead = 0 Then Exit Do

                                Await fileStream.WriteAsync(buffer, 0, bytesRead)
                                totalRead += bytesRead

                                If totalBytes > 0 AndAlso progress IsNot Nothing Then
                                    Dim pct = CInt((totalRead * 100) / totalBytes)
                                    progress.Report(pct)
                                End If
                            Loop
                        End Using
                    End Using
                End Using

                ' 6. Verify File Size
                Dim fileInfo As New FileInfo(tempFilePath)
                If fileInfo.Length = 0 Then
                    File.Delete(tempFilePath)
                    Return New UpdateCheckResult With {.IsSuccess = False, .ErrorMessage = "Downloaded file is empty."}
                End If

                ' 7. SHA-256 Hash Calculation
                Dim actualHash = ComputeFileHash(tempFilePath)
                If Not String.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase) Then
                    File.Delete(tempFilePath)
                    Return New UpdateCheckResult With {.IsSuccess = False, .ErrorMessage = "Update verification failed. The update was not installed."}
                End If

                ' 8. Finalize
                File.Move(tempFilePath, finalFilePath)
                Return New UpdateCheckResult With {.IsSuccess = True, .DownloadedFilePath = finalFilePath}

            Catch ex As Exception
                _logger.LogError("Update download failed", "UpdateService", ex)
                Return New UpdateCheckResult With {.IsSuccess = False, .ErrorMessage = "An unexpected error occurred while downloading the update."}
            End Try
        End Function

        Private Function ComputeFileHash(filePath As String) As String
            Using sha256 = System.Security.Cryptography.SHA256.Create()
                Using stream = File.OpenRead(filePath)
                    Dim hashBytes = sha256.ComputeHash(stream)
                    Return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant()
                End Using
            End Using
        End Function
    End Class
End Namespace
