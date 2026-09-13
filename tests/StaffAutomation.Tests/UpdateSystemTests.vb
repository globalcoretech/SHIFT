Imports System.IO
Imports System.Net
Imports System.Net.Http
Imports System.Reflection
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.Json
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports StaffAutomation.BLL.Services
Imports StaffAutomation.Core.Configuration
Imports StaffAutomation.Core.Constants
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Logging
Imports Moq

Namespace StaffAutomation.Tests
    <TestClass>
    Public Class UpdateSystemTests
        Private _mockConfig As Mock(Of IAppConfiguration)
        Private _mockLogger As Mock(Of IAppLogger)
        Private _updateService As UpdateService
        Private _tempFolder As String

        <TestInitialize>
        Public Sub Setup()
            _mockConfig = New Mock(Of IAppConfiguration)()
            _mockLogger = New Mock(Of IAppLogger)()
            _updateService = New UpdateService(_mockConfig.Object, _mockLogger.Object)

            _tempFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StaffAutomation", "Updates")
        End Sub

        Private Function CreateTestHash(content As String) As String
            Using sha = SHA256.Create()
                Dim bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(content))
                Return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant()
            End Using
        End Function

        <TestMethod>
        Public Async Function DownloadAndVerify_InvalidUrl_Rejected() As Task
            Dim manifest As New UpdateManifestDto With {
                .LatestVersion = "9.9.9.9",
                .DownloadUrl = "invalid-url",
                .SHA256 = New String("a"c, 64)
            }

            Dim result = Await _updateService.DownloadAndVerifyUpdateAsync(manifest, Nothing)
            Assert.IsFalse(result.IsSuccess)
            Assert.IsTrue(result.ErrorMessage.Contains("invalid or insecure"))
        End Function

        <TestMethod>
        Public Async Function DownloadAndVerify_HttpUrl_Rejected() As Task
            Dim manifest As New UpdateManifestDto With {
                .LatestVersion = "9.9.9.9",
                .DownloadUrl = "http://example.com/update.exe",
                .SHA256 = New String("a"c, 64)
            }

            Dim result = Await _updateService.DownloadAndVerifyUpdateAsync(manifest, Nothing)
            Assert.IsFalse(result.IsSuccess)
            Assert.IsTrue(result.ErrorMessage.Contains("HTTPS is required"))
        End Function

        <TestMethod>
        Public Async Function DownloadAndVerify_MissingUrl_Rejected() As Task
            Dim manifest As New UpdateManifestDto With {
                .LatestVersion = "9.9.9.9",
                .DownloadUrl = "",
                .SHA256 = New String("a"c, 64)
            }

            Dim result = Await _updateService.DownloadAndVerifyUpdateAsync(manifest, Nothing)
            Assert.IsFalse(result.IsSuccess)
            Assert.IsTrue(result.ErrorMessage.Contains("missing a download URL"))
        End Function

        <TestMethod>
        Public Async Function DownloadAndVerify_MissingSHA256_Rejected() As Task
            Dim manifest As New UpdateManifestDto With {
                .LatestVersion = "9.9.9.9",
                .DownloadUrl = "https://example.com/update.exe",
                .SHA256 = ""
            }

            Dim result = Await _updateService.DownloadAndVerifyUpdateAsync(manifest, Nothing)
            Assert.IsFalse(result.IsSuccess)
            Assert.IsTrue(result.ErrorMessage.Contains("missing a valid SHA-256"))
        End Function

        <TestMethod>
        Public Async Function DownloadAndVerify_InvalidSHA256_Rejected() As Task
            Dim manifest As New UpdateManifestDto With {
                .LatestVersion = "9.9.9.9",
                .DownloadUrl = "https://example.com/update.exe",
                .SHA256 = "12345"
            }

            Dim result = Await _updateService.DownloadAndVerifyUpdateAsync(manifest, Nothing)
            Assert.IsFalse(result.IsSuccess)
            Assert.IsTrue(result.ErrorMessage.Contains("missing a valid SHA-256"))
        End Function

        <TestMethod>
        Public Async Function DownloadAndVerify_OlderVersion_Rejected() As Task
            ' Just use a hardcoded older version. AppVersion is at least 1.0.0.
            Dim oldVer = New Version(0, 9, 0, 0)

            Dim manifest As New UpdateManifestDto With {
                .LatestVersion = oldVer.ToString(),
                .DownloadUrl = "https://example.com/update.exe",
                .SHA256 = New String("a"c, 64)
            }

            Dim result = Await _updateService.DownloadAndVerifyUpdateAsync(manifest, Nothing)
            Assert.IsFalse(result.IsSuccess)
            Assert.IsTrue(result.ErrorMessage.Contains("older or the same"))
        End Function
        
        <TestMethod>
        Public Async Function DownloadAndVerify_SameVersion_Rejected() As Task
            Dim manifest As New UpdateManifestDto With {
                .LatestVersion = AppConstants.AppVersion,
                .DownloadUrl = "https://example.com/update.exe",
                .SHA256 = New String("a"c, 64)
            }

            Dim result = Await _updateService.DownloadAndVerifyUpdateAsync(manifest, Nothing)
            Assert.IsFalse(result.IsSuccess)
            Assert.IsTrue(result.ErrorMessage.Contains("older or the same"))
        End Function
        ' ---------------- E2E LOCAL NETWORK TESTS ----------------

        Private Const TEST_URL As String = "https://raw.githubusercontent.com/dotnet/aspnetcore/v8.0.0/LICENSE.txt"

        <TestMethod>
        Public Async Function DownloadAndVerify_E2E_Case1_CorrectSHA256_Passes() As Task
            ' Fetch the real file to determine its actual SHA-256 for the test
            Dim expectedSha256 As String
            Using tempClient As New HttpClient()
                ' Use a small dummy text file for E2E
                Dim contentBytes = Await tempClient.GetByteArrayAsync(TEST_URL)
                Using sha = SHA256.Create()
                    Dim bytes = sha.ComputeHash(contentBytes)
                    expectedSha256 = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant()
                End Using
            End Using

            Dim manifest As New UpdateManifestDto With {
                .LatestVersion = "99.99.99.99",
                .DownloadUrl = TEST_URL,
                .SHA256 = expectedSha256
            }

            Dim result = Await _updateService.DownloadAndVerifyUpdateAsync(manifest, Nothing)
            
            Assert.IsTrue(result.IsSuccess, $"Download should succeed. Actual Error: {result.ErrorMessage}")
            Assert.IsFalse(String.IsNullOrEmpty(result.DownloadedFilePath), "Should provide a valid path")
            Assert.IsTrue(File.Exists(result.DownloadedFilePath), "Verified file should exist")
            
            ' Temp file shouldn't exist (it ends with .tmp)
            Dim tempPath = result.DownloadedFilePath & ".tmp"
            Assert.IsFalse(File.Exists(tempPath), "Temp file should be cleaned up")

            ' Cleanup verified file
            File.Delete(result.DownloadedFilePath)
        End Function

        <TestMethod>
        Public Async Function DownloadAndVerify_E2E_Case2_IncorrectSHA256_Fails() As Task
            ' Provide INTENTIONALLY INCORRECT SHA
            Dim expectedSha256 = New String("f"c, 64)

            Dim manifest As New UpdateManifestDto With {
                .LatestVersion = "99.99.99.99",
                .DownloadUrl = TEST_URL,
                .SHA256 = expectedSha256
            }

            Dim result = Await _updateService.DownloadAndVerifyUpdateAsync(manifest, Nothing)
            
            Assert.IsFalse(result.IsSuccess, "Download should fail SHA verification")
            Assert.IsTrue(result.ErrorMessage.Contains("Update verification failed"), $"Error message should reflect SHA mismatch. Actual Error: {result.ErrorMessage}")
            Assert.IsTrue(String.IsNullOrEmpty(result.DownloadedFilePath), "Should NOT provide a verified path")
            
            ' Check directory is empty of any temp or verified files matching this version
            If Directory.Exists(_tempFolder) Then
                Dim files = Directory.GetFiles(_tempFolder, "*99.99.99.99*")
                Assert.AreEqual(0, files.Length, "Invalid package and temp files must be cleaned up")
            End If
        End Function

    End Class
End Namespace
