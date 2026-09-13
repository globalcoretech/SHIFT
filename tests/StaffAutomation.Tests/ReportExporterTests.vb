Imports System
Imports System.Data
Imports System.IO
Imports System.Threading.Tasks
Imports Xunit
Imports StaffAutomation.Reports.Services

Namespace Tests
    Public Class ReportExporterTests
        <Fact>
        Public Async Function TestCsvExport_FormatsHeaderMetadata_AndSummaryTotals() As Task
            Dim service As New ReportGeneratorService()

            Dim dt As New DataTable()
            dt.Columns.Add("Staff Name")
            dt.Columns.Add("Duration (Mins)", GetType(Integer))
            dt.Columns.Add("Duration (Hours)", GetType(Double))
            dt.Columns.Add("Status")

            dt.Rows.Add("Alice", 120, 2.0, "Completed")
            dt.Rows.Add("Bob", 180, 3.0, "Completed")

            Dim tempFile = Path.Combine(Path.GetTempPath(), $"test_export_{Guid.NewGuid():N}.csv")

            Try
                Dim success = Await service.ExportToCsvAsync(dt, tempFile, "Daily Staff Work Timeline (Chronological)")

                Assert.True(success)
                Assert.True(File.Exists(tempFile))

                Dim content = Await File.ReadAllTextAsync(tempFile)

                ' Check Metadata Header
                Assert.Contains("SHIFT WORKFORCE PRODUCTIVITY SYSTEM", content)
                Assert.Contains("Report Title: Daily Staff Work Timeline (Chronological)", content)

                ' Check Columns
                Assert.Contains("""Staff Name"",""Duration (Mins)"",""Duration (Hours)"",""Status""", content)

                ' Check Data Rows
                Assert.Contains("""Alice"",""120"",""2"",""Completed""", content)
                Assert.Contains("""Bob"",""180"",""3"",""Completed""", content)

                ' Check Automated Summary Totals Row (120 + 180 = 300 mins; 2.0 + 3.0 = 5.0 hours)
                Assert.Contains("TOTALS", content)
                Assert.Contains("300", content)
                Assert.Contains("* Note: Running / Provisional tasks represent active work in progress", content)
            Finally
                If File.Exists(tempFile) Then
                    File.Delete(tempFile)
                End If
            End Try
        End Function
    End Class
End Namespace
