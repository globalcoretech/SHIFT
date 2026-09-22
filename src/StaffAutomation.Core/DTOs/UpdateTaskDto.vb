Option Strict On
Option Explicit On

Imports System
Imports StaffAutomation.Core.Enums

Namespace DTOs
    ''' <summary>
    ''' Data Transfer Object carrying specific fields permitted for task editing.
    ''' Prevents accidental overwrites of immutable metadata (TaskCode, FinancialYearId, StatusId, CreatedBy, etc.).
    ''' </summary>
    Public Class UpdateTaskDto
        Public Property TaskId As Integer
        Public Property Title As String = String.Empty
        Public Property Description As String = String.Empty
        Public Property TaskType As String = String.Empty
        Public Property CategoryId As Integer
        Public Property ClientId As Integer
        Public Property AssignedToUserId As Integer
        Public Property Priority As TaskPriority
        Public Property TargetDueDate As DateTime
        Public Property ReminderDate As Nullable(Of DateTime)
        Public Property OriginalModifiedOn As Nullable(Of DateTime)
    End Class
End Namespace
