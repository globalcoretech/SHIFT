Option Strict On
Option Explicit On

Imports System

Namespace Entities
    ''' <summary>
    ''' Represents an individual checklist item for a specific task.
    ''' </summary>
    Public Class TaskChecklistItemEntity
        Public Property ChecklistId As Integer
        Public Property TaskId As Integer
        Public Property ItemDescription As String = String.Empty
        Public Property RequiresProof As Boolean = False
        Public Property IsCompleted As Boolean = False
        Public Property CompletedByUserId As Nullable(Of Integer)
        Public Property CompletedOn As Nullable(Of DateTime)
        Public Property Remarks As String
        Public Property AttachmentPath As String
        Public Property SortOrder As Integer = 0
        Public Property CreatedOn As DateTime = DateTime.UtcNow
    End Class
End Namespace
