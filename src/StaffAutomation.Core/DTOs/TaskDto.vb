Imports StaffAutomation.Core.Enums

Namespace DTOs
    ''' <summary>
    ''' Data Transfer Object representing Task details, assigned metadata, and deadline status.
    ''' </summary>
    Public Class TaskDto
        Public Property TaskId As Integer
        Public Property TaskCode As String = String.Empty
        Public Property Title As String = String.Empty
        Public Property Description As String = String.Empty
        Public Property TaskType As String = String.Empty
        Public Property CategoryId As Integer
        Public Property CategoryCode As String = String.Empty
        Public Property CategoryName As String = String.Empty
        Public Property FinancialYearId As Integer
        Public Property ClientId As Integer
        Public Property ClientName As String = String.Empty
        Public Property AssignedToUserId As Integer
        Public Property AssignedToName As String = String.Empty
        Public Property AssignedByUserId As Integer
        Public Property AssignedByName As String = String.Empty
        Public Property Department As DepartmentType
        Public Property Priority As TaskPriority
        Public Property WorkflowState As TaskWorkflowState
        Public Property AssignmentDate As DateTime
        Public Property TargetDueDate As DateTime
        Public Property ReminderDate As Nullable(Of DateTime)
        Public Property ModifiedOn As Nullable(Of DateTime)
        Public Property DaysRemaining As Integer
        Public Property DaysOverdue As Integer
        Public Property IsOverdue As Boolean
        Public Property IsDeleted As Boolean
        Public Property TotalWorkMinutes As Integer
    End Class
End Namespace
