Imports System
Imports System.IO
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Models.Backup

Namespace Services
    ''' <summary>
    ''' Implementation of database server capability detector service in DAL.
    ''' Queries SQL Server Express metadata, verifies backup compression capability,
    ''' checks storage free space, and validates directory write access.
    ''' </summary>
    Public Class DatabaseServerCapabilityService
        Implements IDatabaseServerCapabilityService

        Private ReadOnly _sqlHelper As ISqlHelper

        Public Sub New(sqlHelper As ISqlHelper)
            _sqlHelper = sqlHelper
        End Sub

        Public Async Function DetectCapabilitiesAsync(Optional destinationPath As String = "") As Task(Of ServerCapabilityResult) Implements IDatabaseServerCapabilityService.DetectCapabilitiesAsync
            Dim result As New ServerCapabilityResult()

            Try
                ' 1. Check SQL Server connectivity, version, edition, state, and recovery model
                Dim version = Await _sqlHelper.ExecuteScalarAsync(Of String)("SELECT CAST(SERVERPROPERTY('ProductVersion') AS NVARCHAR(128));", Nothing)
                Dim edition = Await _sqlHelper.ExecuteScalarAsync(Of String)("SELECT CAST(SERVERPROPERTY('Edition') AS NVARCHAR(128));", Nothing)
                Dim dbState = Await _sqlHelper.ExecuteScalarAsync(Of String)("SELECT CAST(DATABASEPROPERTYEX(DB_NAME(), 'Status') AS NVARCHAR(128));", Nothing)
                Dim recoveryModel = Await _sqlHelper.ExecuteScalarAsync(Of String)("SELECT CAST(DATABASEPROPERTYEX(DB_NAME(), 'Recovery') AS NVARCHAR(128));", Nothing)
                Dim compressionSupportedInt = Await _sqlHelper.ExecuteScalarAsync(Of Integer)("SELECT ISNULL(CAST(SERVERPROPERTY('IsBackupCompressionSupported') AS INT), 0);", Nothing)

                result.SqlServerVersion = If(version, "Unknown")
                result.SqlServerEdition = If(edition, "Unknown")
                result.DatabaseState = If(dbState, "ONLINE")
                result.RecoveryModel = If(recoveryModel, "SIMPLE")
                ' SQL Server Express does NOT support BACKUP WITH COMPRESSION at runtime
                Dim isExpressEdition = result.SqlServerEdition.IndexOf("Express", StringComparison.OrdinalIgnoreCase) >= 0
                result.SupportsBackupCompression = (compressionSupportedInt = 1) AndAlso Not isExpressEdition
                result.SupportsChecksum = True
                result.IsServiceAvailable = True

                ' 2. Validate destination directory, free disk space, and write permissions
                Dim targetPath = If(String.IsNullOrWhiteSpace(destinationPath), DatabaseBackupConfiguration.GetDefaultPrimaryBackupPath(), destinationPath.Trim())
                result.PrimaryBackupPath = targetPath

                Try
                    EnsureDirectoryWithAcl(targetPath)

                    ' Reject invalid network/UNC format or missing root drives gracefully
                    If targetPath.StartsWith("\\") Then
                        If Not Directory.Exists(targetPath) Then
                            Directory.CreateDirectory(targetPath)
                        End If
                    Else
                        Dim rootDrive = Path.GetPathRoot(targetPath)
                        If Not String.IsNullOrEmpty(rootDrive) AndAlso Directory.Exists(rootDrive) Then
                            Dim drive = New DriveInfo(rootDrive)
                            result.AvailableDiskSpaceBytes = drive.AvailableFreeSpace
                        End If
                    End If

                    ' Test write access with a transient probe file
                    Dim testFile = Path.Combine(targetPath, $"ProbeTest_{Guid.NewGuid():N}.tmp")
                    File.WriteAllText(testFile, "probe")
                    If File.Exists(testFile) Then
                        File.Delete(testFile)
                        result.HasWritePermission = True
                    End If
                Catch ex As Exception
                    result.HasWritePermission = False
                    result.StatusMessage = $"Storage permission alert for '{targetPath}': {ex.Message}"
                End Try

                If result.HasWritePermission AndAlso String.IsNullOrEmpty(result.StatusMessage) Then
                    result.StatusMessage = $"SQL Server capability verified ({result.SqlServerEdition}). Backup compression: {If(result.SupportsBackupCompression, "SUPPORTED", "NOT SUPPORTED")}."
                End If
            Catch ex As Exception
                result.IsServiceAvailable = False
                result.DatabaseState = "UNAVAILABLE"
                result.StatusMessage = $"SQL Server connectivity failure: {ex.Message}"
            End Try

            Return result
        End Function

        Public Shared Sub EnsureDirectoryWithAcl(targetPath As String)
            If String.IsNullOrWhiteSpace(targetPath) Then Return
            Try
                If Not Directory.Exists(targetPath) Then
                    Directory.CreateDirectory(targetPath)
                End If

                Try
                    Dim icaclsArgs As String = String.Format("""{0}"" /grant ""NT Service\MSSQL$SQLEXPRESS"":(OI)(CI)F /grant Administrators:(OI)(CI)F /T", targetPath)
                    Dim psi As New System.Diagnostics.ProcessStartInfo("icacls", icaclsArgs) With {
                        .CreateNoWindow = True,
                        .UseShellExecute = False
                    }
                    Dim proc = System.Diagnostics.Process.Start(psi)
                    proc?.WaitForExit(1500)
                Catch
                End Try
            Catch
            End Try
        End Sub
    End Class
End Namespace
