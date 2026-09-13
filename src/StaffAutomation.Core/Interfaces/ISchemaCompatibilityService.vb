Imports System.Threading.Tasks
Imports StaffAutomation.Core.Enums

Namespace Interfaces
    Public Interface ISchemaCompatibilityService
        ''' <summary>
        ''' Evaluates the compatibility between the required application schema version and the current database schema version.
        ''' </summary>
        Function EvaluateCompatibilityAsync() As Task(Of SchemaCompatibilityStatus)
        
        ''' <summary>
        ''' Gets the current database schema version.
        ''' </summary>
        Function GetDatabaseSchemaVersionAsync() As Task(Of Integer)
    End Interface
End Namespace
