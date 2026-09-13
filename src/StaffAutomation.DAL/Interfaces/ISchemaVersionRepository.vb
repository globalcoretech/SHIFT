Imports System.Threading.Tasks

Namespace Interfaces
    ''' <summary>
    ''' Repository for fetching the current database schema version.
    ''' </summary>
    Public Interface ISchemaVersionRepository
        ''' <summary>
        ''' Gets the highest applied schema version from the database.
        ''' Returns 0 if the schema history table does not exist.
        ''' </summary>
        Function GetCurrentSchemaVersionAsync() As Task(Of Integer)
    End Interface
End Namespace
