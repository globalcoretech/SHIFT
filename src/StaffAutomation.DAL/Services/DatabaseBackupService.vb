Imports System
Imports System.Collections.Generic
Imports System.Data
Imports Microsoft.Data.SqlClient
Imports System.IO
Imports System.Security.Cryptography
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Models.Backup

Namespace Services
    ''' <summary>
    ''' Implementation of Database Backup Service performing T-SQL backups, RESTORE VERIFYONLY,
    ''' SHA-256 checksum hashing, tamper verification, and audit history tracking in tbl_DatabaseBackupHistory.
    ''' </summary>
    Public Class DatabaseBackupService
        Implements IDatabaseBackupService

        Private ReadOnly _sqlHelper As ISqlHelper
        Private ReadOnly _capabilityService As IDatabaseServerCapabilityService

        Public Sub New(sqlHelper As ISqlHelper, capabilityService As IDatabaseServerCapabilityService)
            _sqlHelper = sqlHelper
            _capabilityService = capabilityService
        End Sub

        Public Async Function ExecuteBackupAsync(Optional destinationDirectory As String = "", Optional createdBy As String = "") As Task(Of DatabaseBackupHistoryDto) Implements IDatabaseBackupService.ExecuteBackupAsync
            Dim record As New DatabaseBackupHistoryDto() With {
                .BackupId = Guid.NewGuid(),
                .DatabaseName = "StaffAutomationDb",
                .BackupType = "FULL",
                .StartedOn = DateTime.Now,
                .Status = "Running",
                .CreatedBy = If(String.IsNullOrWhiteSpace(createdBy), Environment.UserName, createdBy)
            }

            Try
                ' 1. Pre-implementation capability & pre-flight storage check
                Dim cap = Await _capabilityService.DetectCapabilitiesAsync(destinationDirectory)
                record.SqlServerVersion = cap.SqlServerVersion
                record.SqlServerEdition = cap.SqlServerEdition
                record.IsChecksumEnabled = cap.SupportsChecksum
                record.IsCompressed = cap.SupportsBackupCompression

                If Not cap.IsServiceAvailable Then
                    Throw New InvalidOperationException($"Backup aborted: SQL Server is unavailable. {cap.StatusMessage}")
                End If

                If Not cap.DatabaseState.Equals("ONLINE", StringComparison.OrdinalIgnoreCase) Then
                    Throw New InvalidOperationException($"Backup aborted: Database 'StaffAutomationDb' is in state '{cap.DatabaseState}'.")
                End If

                If Not cap.HasWritePermission Then
                    Throw New InvalidOperationException($"Backup aborted: Service account has no write permission to backup directory '{cap.PrimaryBackupPath}'.")
                End If

                ' 2. Generate unique non-overwriting backup filename
                Dim targetDir = cap.PrimaryBackupPath
                If Not Directory.Exists(targetDir) Then
                    Directory.CreateDirectory(targetDir)
                End If

                Dim timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss")
                Dim fileName = $"StaffAutomationDb_FULL_{timeStamp}.bak"
                Dim fullPath = Path.Combine(targetDir, fileName)

                If File.Exists(fullPath) Then
                    fileName = $"StaffAutomationDb_FULL_{timeStamp}_{Guid.NewGuid():N}.bak"
                    fullPath = Path.Combine(targetDir, fileName)
                End If

                record.FileName = fileName
                record.FilePath = fullPath

                ' Insert initial PENDING/RUNNING tracking record into tbl_DatabaseBackupHistory
                Await LogBackupRecordAsync(record)

                ' 3. Construct parameter-safe T-SQL BACKUP command
                Dim sqlBuilder As New System.Text.StringBuilder()
                sqlBuilder.Append("BACKUP DATABASE [StaffAutomationDb] TO DISK = @FilePath WITH CHECKSUM, STATS = 10")
                If cap.SupportsBackupCompression Then
                    sqlBuilder.Append(", COMPRESSION")
                End If

                Dim parameters As IDbDataParameter() = {
                    New SqlParameter("@FilePath", SqlDbType.NVarChar, 512) With {.Value = fullPath}
                }

                Await _sqlHelper.ExecuteNonQueryAsync(sqlBuilder.ToString(), parameters)

                ' 4. Inspect file size and calculate SHA-256 hash
                record.CompletedOn = DateTime.Now
                If File.Exists(fullPath) Then
                    Dim fi As New FileInfo(fullPath)
                    record.FileSizeBytes = fi.Length
                    record.Sha256Hash = CalculateSha256Hash(fullPath)
                End If

                ' 5. Post-backup RESTORE VERIFYONLY check
                Dim isVerified = Await VerifyBackupFileAsync(fullPath)
                If isVerified Then
                    record.Status = "Succeeded"
                    record.VerificationStatus = "Verified"
                    record.VerificationMessage = "Backup header structure and CHECKSUM verified successfully via RESTORE VERIFYONLY."
                Else
                    record.Status = "VerificationFailed"
                    record.VerificationStatus = "VerificationFailed"
                    record.VerificationMessage = "RESTORE VERIFYONLY failed to validate backup file structure."
                End If
            Catch ex As Exception
                record.Status = "Failed"
                record.VerificationStatus = "VerificationFailed"
                record.ErrorMessage = ex.Message
                record.CompletedOn = DateTime.Now
            End Try

            ' Update final backup record status in database
            Await UpdateBackupRecordStatusAsync(record)
            Return record
        End Function

        Public Async Function VerifyBackupFileAsync(backupFilePath As String) As Task(Of Boolean) Implements IDatabaseBackupService.VerifyBackupFileAsync
            If String.IsNullOrWhiteSpace(backupFilePath) OrElse Not File.Exists(backupFilePath) Then
                Return False
            End If

            Try
                Dim sql = "RESTORE VERIFYONLY FROM DISK = @FilePath WITH CHECKSUM;"
                Dim parameters As IDbDataParameter() = {
                    New SqlParameter("@FilePath", SqlDbType.NVarChar, 512) With {.Value = backupFilePath}
                }
                Await _sqlHelper.ExecuteNonQueryAsync(sql, parameters)
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Async Function GetBackupHistoryAsync(Optional topCount As Integer = 50) As Task(Of List(Of DatabaseBackupHistoryDto)) Implements IDatabaseBackupService.GetBackupHistoryAsync
            Dim sql = $"SELECT TOP ({Math.Max(1, topCount)}) BackupId, DatabaseName, BackupType, FilePath, FileName, FileSizeBytes, Sha256Hash, StartedOn, CompletedOn, Status, VerificationStatus, VerificationMessage, SecondaryCopyStatus, SecondaryFilePath, SqlServerVersion, SqlServerEdition, IsCompressed, IsChecksumEnabled, ErrorMessage, CreatedBy FROM tbl_DatabaseBackupHistory ORDER BY StartedOn DESC;"
            Return Await _sqlHelper.ExecuteReaderAsync(sql, Nothing, Function(reader) MapHistory(reader))
        End Function

        Public Shared Function CalculateSha256Hash(filePath As String) As String
            If String.IsNullOrWhiteSpace(filePath) OrElse Not File.Exists(filePath) Then Return String.Empty
            Try
                Using stream = File.OpenRead(filePath)
                    Using sha256Algo As System.Security.Cryptography.SHA256 = System.Security.Cryptography.SHA256.Create()
                        Dim hashBytes = sha256Algo.ComputeHash(stream)
                        Return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant()
                    End Using
                End Using
            Catch ex As Exception
                Return String.Empty
            End Try
        End Function

        Public Shared Function VerifySha256TamperStatus(filePath As String, expectedHash As String) As Boolean
            If String.IsNullOrWhiteSpace(filePath) OrElse String.IsNullOrWhiteSpace(expectedHash) Then Return False
            Dim currentHash = CalculateSha256Hash(filePath)
            Return String.Equals(currentHash, expectedHash, StringComparison.OrdinalIgnoreCase)
        End Function

        Private Async Function LogBackupRecordAsync(record As DatabaseBackupHistoryDto) As Task
            Dim sql = "INSERT INTO tbl_DatabaseBackupHistory (BackupId, DatabaseName, BackupType, FilePath, FileName, FileSizeBytes, Sha256Hash, StartedOn, CompletedOn, Status, VerificationStatus, VerificationMessage, SecondaryCopyStatus, SecondaryFilePath, SqlServerVersion, SqlServerEdition, IsCompressed, IsChecksumEnabled, ErrorMessage, CreatedBy) VALUES (@BackupId, @DatabaseName, @BackupType, @FilePath, @FileName, @FileSizeBytes, @Sha256Hash, @StartedOn, @CompletedOn, @Status, @VerificationStatus, @VerificationMessage, @SecondaryCopyStatus, @SecondaryFilePath, @SqlServerVersion, @SqlServerEdition, @IsCompressed, @IsChecksumEnabled, @ErrorMessage, @CreatedBy);"
            Dim params = BuildHistoryParameters(record)
            Await _sqlHelper.ExecuteNonQueryAsync(sql, params)
        End Function

        Private Async Function UpdateBackupRecordStatusAsync(record As DatabaseBackupHistoryDto) As Task
            Dim sql = "UPDATE tbl_DatabaseBackupHistory SET FileSizeBytes = @FileSizeBytes, Sha256Hash = @Sha256Hash, CompletedOn = @CompletedOn, Status = @Status, VerificationStatus = @VerificationStatus, VerificationMessage = @VerificationMessage, ErrorMessage = @ErrorMessage WHERE BackupId = @BackupId;"
            Dim params As IDbDataParameter() = {
                New SqlParameter("@BackupId", record.BackupId),
                New SqlParameter("@FileSizeBytes", record.FileSizeBytes),
                New SqlParameter("@Sha256Hash", If(record.Sha256Hash, CType(DBNull.Value, Object))),
                New SqlParameter("@CompletedOn", If(record.CompletedOn.HasValue, record.CompletedOn.Value, CType(DBNull.Value, Object))),
                New SqlParameter("@Status", record.Status),
                New SqlParameter("@VerificationStatus", If(record.VerificationStatus, CType(DBNull.Value, Object))),
                New SqlParameter("@VerificationMessage", If(record.VerificationMessage, CType(DBNull.Value, Object))),
                New SqlParameter("@ErrorMessage", If(record.ErrorMessage, CType(DBNull.Value, Object)))
            }
            Await _sqlHelper.ExecuteNonQueryAsync(sql, params)
        End Function

        Private Function BuildHistoryParameters(r As DatabaseBackupHistoryDto) As IDbDataParameter()
            Return New IDbDataParameter() {
                New SqlParameter("@BackupId", r.BackupId),
                New SqlParameter("@DatabaseName", r.DatabaseName),
                New SqlParameter("@BackupType", r.BackupType),
                New SqlParameter("@FilePath", r.FilePath),
                New SqlParameter("@FileName", r.FileName),
                New SqlParameter("@FileSizeBytes", r.FileSizeBytes),
                New SqlParameter("@Sha256Hash", If(r.Sha256Hash, CType(DBNull.Value, Object))),
                New SqlParameter("@StartedOn", r.StartedOn),
                New SqlParameter("@CompletedOn", If(r.CompletedOn.HasValue, r.CompletedOn.Value, CType(DBNull.Value, Object))),
                New SqlParameter("@Status", r.Status),
                New SqlParameter("@VerificationStatus", If(r.VerificationStatus, CType(DBNull.Value, Object))),
                New SqlParameter("@VerificationMessage", If(r.VerificationMessage, CType(DBNull.Value, Object))),
                New SqlParameter("@SecondaryCopyStatus", If(r.SecondaryCopyStatus, CType(DBNull.Value, Object))),
                New SqlParameter("@SecondaryFilePath", If(r.SecondaryFilePath, CType(DBNull.Value, Object))),
                New SqlParameter("@SqlServerVersion", If(r.SqlServerVersion, CType(DBNull.Value, Object))),
                New SqlParameter("@SqlServerEdition", If(r.SqlServerEdition, CType(DBNull.Value, Object))),
                New SqlParameter("@IsCompressed", r.IsCompressed),
                New SqlParameter("@IsChecksumEnabled", r.IsChecksumEnabled),
                New SqlParameter("@ErrorMessage", If(r.ErrorMessage, CType(DBNull.Value, Object))),
                New SqlParameter("@CreatedBy", r.CreatedBy)
            }
        End Function

        Private Function MapHistory(r As IDataReader) As DatabaseBackupHistoryDto
            Return New DatabaseBackupHistoryDto() With {
                .BackupId = If(r("BackupId") Is DBNull.Value, Guid.Empty, CType(r("BackupId"), Guid)),
                .DatabaseName = If(r("DatabaseName") Is DBNull.Value, "", r("DatabaseName").ToString()),
                .BackupType = If(r("BackupType") Is DBNull.Value, "", r("BackupType").ToString()),
                .FilePath = If(r("FilePath") Is DBNull.Value, "", r("FilePath").ToString()),
                .FileName = If(r("FileName") Is DBNull.Value, "", r("FileName").ToString()),
                .FileSizeBytes = If(r("FileSizeBytes") Is DBNull.Value, 0L, Convert.ToInt64(r("FileSizeBytes"))),
                .Sha256Hash = If(r("Sha256Hash") Is DBNull.Value, "", r("Sha256Hash").ToString()),
                .StartedOn = If(r("StartedOn") Is DBNull.Value, DateTime.Now, Convert.ToDateTime(r("StartedOn"))),
                .CompletedOn = If(r("CompletedOn") Is DBNull.Value, CType(Nothing, Nullable(Of DateTime)), Convert.ToDateTime(r("CompletedOn"))),
                .Status = If(r("Status") Is DBNull.Value, "", r("Status").ToString()),
                .VerificationStatus = If(r("VerificationStatus") Is DBNull.Value, "", r("VerificationStatus").ToString()),
                .VerificationMessage = If(r("VerificationMessage") Is DBNull.Value, "", r("VerificationMessage").ToString()),
                .SecondaryCopyStatus = If(r("SecondaryCopyStatus") Is DBNull.Value, "", r("SecondaryCopyStatus").ToString()),
                .SecondaryFilePath = If(r("SecondaryFilePath") Is DBNull.Value, "", r("SecondaryFilePath").ToString()),
                .SqlServerVersion = If(r("SqlServerVersion") Is DBNull.Value, "", r("SqlServerVersion").ToString()),
                .SqlServerEdition = If(r("SqlServerEdition") Is DBNull.Value, "", r("SqlServerEdition").ToString()),
                .IsCompressed = If(r("IsCompressed") Is DBNull.Value, False, Convert.ToBoolean(r("IsCompressed"))),
                .IsChecksumEnabled = If(r("IsChecksumEnabled") Is DBNull.Value, True, Convert.ToBoolean(r("IsChecksumEnabled"))),
                .ErrorMessage = If(r("ErrorMessage") Is DBNull.Value, "", r("ErrorMessage").ToString()),
                .CreatedBy = If(r("CreatedBy") Is DBNull.Value, "", r("CreatedBy").ToString())
            }
        End Function
    End Class
End Namespace
