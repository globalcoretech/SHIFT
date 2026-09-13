Namespace Enums
    Public Enum SchemaCompatibilityStatus
        ''' <summary>
        ''' Application and database schema versions are compatible.
        ''' </summary>
        Compatible

        ''' <summary>
        ''' The database schema is older than what the application requires. A migration is needed.
        ''' </summary>
        MigrationRequired

        ''' <summary>
        ''' The database schema is newer than what the application supports. The application must be updated.
        ''' </summary>
        AppUpdateRequired

        ''' <summary>
        ''' The database schema could not be verified (e.g. database offline or missing tracking table).
        ''' </summary>
        Unknown
    End Enum
End Namespace
