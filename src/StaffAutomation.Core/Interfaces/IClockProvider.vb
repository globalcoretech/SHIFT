Option Strict On
Option Explicit On

Imports System

Namespace Interfaces
    ''' <summary>
    ''' System clock abstraction for deterministic time injection during policy calculations and unit testing.
    ''' </summary>
    Public Interface IClockProvider
        ReadOnly Property Now As DateTime
    End Interface
End Namespace
