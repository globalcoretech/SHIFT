Imports System

Namespace Common
    ''' <summary>
    ''' System Clock Abstraction interface contract enabling testable, deterministic date/time operations.
    ''' </summary>
    Public Interface IDateTimeProvider
        ReadOnly Property UtcNow As DateTime
        ReadOnly Property LocalNow As DateTime
        ReadOnly Property Today As DateTime
    End Interface
End Namespace
