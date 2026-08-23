Imports System
Imports System.Threading.Tasks

Namespace Interfaces
    ''' <summary>
    ''' Interface defining transient error detection and retry policy execution with exponential backoff.
    ''' </summary>
    Public Interface ISqlTransientRetryPolicy
        Function ExecuteAsync(Of T)(operation As Func(Of Task(Of T))) As Task(Of T)
        Function IsTransientException(ex As Exception) As Boolean
    End Interface
End Namespace
