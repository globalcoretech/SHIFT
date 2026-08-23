Imports StaffAutomation.Core.Enums

Namespace Entities
    ''' <summary>
    ''' Central Task Domain Entity representing work deliverables assigned to employees.
    ''' </summary>
    Public Class TaskEntity
        Public Property TaskId As Integer
        Public Property TaskCode As String = String.Empty
        Public Property Title As String = String.Empty
        Public Property Description As String = String.Empty
        Public Property ClientId As Integer
        Public Property CategoryId As Integer
        Public Property FinancialYearId As Integer
        Public Property AssignedToUserId As Integer
        Public Property AssignedByUserId As Integer
        Public Property Department As DepartmentType
        Public Property Priority As TaskPriority
        Public Property WorkflowState As TaskWorkflowState
        Public Property AssignmentDate As DateTime = DateTime.UtcNow
        Public Property TargetDueDate As DateTime
        Public Property ReminderDate As Nullable(Of DateTime)
        Public Property CompletionDate As Nullable(Of DateTime)
        Public Property IsDeleted As Boolean = False
        Public Property CreatedOn As DateTime = DateTime.UtcNow
        Public Property CreatedBy As Integer
        Public Property ModifiedOn As Nullable(Of DateTime)
        Public Property ModifiedBy As Nullable(Of Integer)
    End Class
End Namespace
