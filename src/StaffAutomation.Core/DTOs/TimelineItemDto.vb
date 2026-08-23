Option Strict On
Option Explicit On

Imports System

Namespace DTOs
    ''' <summary>
    ''' Data Transfer Object representing a dynamic timeline event item for rendering present and future timeline collections.
    ''' </summary>
    Public Class TimelineItemDto
        Public Property Title As String = String.Empty
        Public Property TimeText As String = String.Empty
        Public Property SubText As String = String.Empty
        Public Property IconSymbol As String = "⚪"
        Public Property IsCompleted As Boolean
    End Class
End Namespace
