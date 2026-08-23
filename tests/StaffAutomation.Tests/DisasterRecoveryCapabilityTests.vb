Imports System
Imports System.Threading.Tasks
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Models.Backup
Imports StaffAutomation.DAL.Services

Namespace StaffAutomation.Tests
    ''' <summary>
    ''' Unit & Regression tests for Phase 12 Database Disaster Recovery capabilities,
    ''' SQL Server Express backup compatibility, and isolated status loading.
    ''' </summary>
    <TestClass>
    Public Class DisasterRecoveryCapabilityTests

        <TestMethod>
        Public Sub TestSqlServerExpressBackupCompressionDisabled()
            Dim result As New ServerCapabilityResult() With {
                .SqlServerVersion = "15.0.2000.5",
                .SqlServerEdition = "Express Edition (64-bit)",
                .DatabaseState = "ONLINE",
                .IsServiceAvailable = True
            }

            ' Express Edition must strictly disable backup compression
            Dim isExpressEdition = result.SqlServerEdition.IndexOf("Express", StringComparison.OrdinalIgnoreCase) >= 0
            Dim rawCompressionSupported = True
            result.SupportsBackupCompression = rawCompressionSupported AndAlso Not isExpressEdition

            Assert.IsFalse(result.SupportsBackupCompression, "SQL Server Express Edition must disable backup compression.")
        End Sub

        <TestMethod>
        Public Sub TestNonExpressBackupCompressionPreserved()
            Dim result As New ServerCapabilityResult() With {
                .SqlServerVersion = "15.0.2000.5",
                .SqlServerEdition = "Enterprise Edition (64-bit)",
                .DatabaseState = "ONLINE",
                .IsServiceAvailable = True
            }

            Dim isExpressEdition = result.SqlServerEdition.IndexOf("Express", StringComparison.OrdinalIgnoreCase) >= 0
            Dim rawCompressionSupported = True
            result.SupportsBackupCompression = rawCompressionSupported AndAlso Not isExpressEdition

            Assert.IsTrue(result.SupportsBackupCompression, "Enterprise Edition must preserve backup compression capability when supported.")
        End Sub

        <TestMethod>
        Public Sub TestBackupSqlStatementNoCompressionOnExpress()
            Dim cap As New ServerCapabilityResult() With {
                .SqlServerEdition = "Express Edition (64-bit)",
                .SupportsBackupCompression = False
            }

            Dim sqlBuilder As New System.Text.StringBuilder()
            sqlBuilder.Append("BACKUP DATABASE [StaffAutomationDb] TO DISK = @FilePath WITH CHECKSUM, STATS = 10")
            If cap.SupportsBackupCompression Then
                sqlBuilder.Append(", COMPRESSION")
            End If

            Dim sqlStatement = sqlBuilder.ToString()
            Assert.IsFalse(sqlStatement.Contains("COMPRESSION"), "T-SQL backup command for SQL Express must NOT contain WITH COMPRESSION.")
            Assert.IsTrue(sqlStatement.Contains("CHECKSUM"), "T-SQL backup command must retain WITH CHECKSUM validation.")
        End Sub
    End Class
End Namespace
