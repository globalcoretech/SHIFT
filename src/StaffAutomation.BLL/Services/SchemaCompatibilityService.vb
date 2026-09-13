Imports System.Threading.Tasks
Imports StaffAutomation.Core.Constants
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.Core.Logging
Imports StaffAutomation.DAL.Interfaces

Namespace Services
    Public Class SchemaCompatibilityService
        Implements ISchemaCompatibilityService

        Private ReadOnly _schemaRepo As ISchemaVersionRepository
        Private ReadOnly _logger As IAppLogger

        Public Sub New(schemaRepo As ISchemaVersionRepository, logger As IAppLogger)
            If schemaRepo Is Nothing Then Throw New ArgumentNullException(NameOf(schemaRepo))
            If logger Is Nothing Then Throw New ArgumentNullException(NameOf(logger))
            
            _schemaRepo = schemaRepo
            _logger = logger
        End Sub

        Public Async Function GetDatabaseSchemaVersionAsync() As Task(Of Integer) Implements ISchemaCompatibilityService.GetDatabaseSchemaVersionAsync
            Try
                Return Await _schemaRepo.GetCurrentSchemaVersionAsync()
            Catch ex As Exception
                _logger.LogError("Failed to get current database schema version", "SchemaCompatibilityService", ex)
                Return 0
            End Try
        End Function

        Public Async Function EvaluateCompatibilityAsync() As Task(Of SchemaCompatibilityStatus) Implements ISchemaCompatibilityService.EvaluateCompatibilityAsync
            Try
                Dim requiredVersion As Integer = AppConstants.RequiredDatabaseSchemaVersion
                Dim currentVersion As Integer = Await _schemaRepo.GetCurrentSchemaVersionAsync()

                If currentVersion = 0 Then
                    ' Schema tracking unavailable, but we assume it might be a new or pre-tracking database.
                    Return SchemaCompatibilityStatus.Unknown
                End If

                If currentVersion = requiredVersion Then
                    Return SchemaCompatibilityStatus.Compatible
                ElseIf currentVersion < requiredVersion Then
                    Return SchemaCompatibilityStatus.MigrationRequired
                Else
                    Return SchemaCompatibilityStatus.AppUpdateRequired
                End If
            Catch ex As Exception
                _logger.LogError("Error evaluating schema compatibility", "SchemaCompatibilityService", ex)
                Return SchemaCompatibilityStatus.Unknown
            End Try
        End Function
    End Class
End Namespace
