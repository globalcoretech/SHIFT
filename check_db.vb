Imports System.Data.SqlClient

Module Program
    Sub Main()
        Dim connStr = "Server=.\SQLEXPRESS;Database=StaffAutomationDb;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;Connect Timeout=15;"
        Try
            Using conn As New SqlConnection(connStr)
                conn.Open()
                Dim sql = "
                    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_SchemaHistory]') AND type in (N'U'))
                    BEGIN
                        SELECT ISNULL(MAX(MigrationNumber), 0) FROM dbo.tbl_SchemaHistory
                    END
                    ELSE
                    BEGIN
                        SELECT 0
                    END"
                Using cmd As New SqlCommand(sql, conn)
                    Dim result = cmd.ExecuteScalar()
                    Console.WriteLine("CURRENT_VERSION=" & result.ToString())
                End Using
            End Using
        Catch ex As Exception
            Console.WriteLine("ERROR: " & ex.Message)
        End Try
    End Sub
End Module
