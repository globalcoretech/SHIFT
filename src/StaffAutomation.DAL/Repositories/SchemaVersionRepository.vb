Imports System
Imports System.Data
Imports System.Threading.Tasks
Imports StaffAutomation.DAL.Core
Imports StaffAutomation.DAL.Interfaces
Imports StaffAutomation.Core.Interfaces

Namespace Repositories
    ''' <summary>
    ''' Implementation for fetching schema versions safely.
    ''' </summary>
    Public Class SchemaVersionRepository
        Implements ISchemaVersionRepository

        Private ReadOnly _sqlHelper As ISqlHelper

        Public Sub New(sqlHelper As ISqlHelper)
            If sqlHelper Is Nothing Then Throw New ArgumentNullException(NameOf(sqlHelper))
            _sqlHelper = sqlHelper
        End Sub

        Public Async Function GetCurrentSchemaVersionAsync() As Task(Of Integer) Implements ISchemaVersionRepository.GetCurrentSchemaVersionAsync
            Dim sql = "
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_SchemaHistory]') AND type in (N'U'))
                BEGIN
                    SELECT ISNULL(MAX(MigrationNumber), 0) FROM dbo.tbl_SchemaHistory
                END
                ELSE
                BEGIN
                    SELECT 0
                END"
            
            Return Await _sqlHelper.ExecuteScalarAsync(Of Integer)(sql, Nothing)
        End Function
    End Class
End Namespace
