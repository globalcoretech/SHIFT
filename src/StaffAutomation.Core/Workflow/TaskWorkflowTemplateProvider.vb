Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports StaffAutomation.Core.Enums

Namespace Workflow
    ''' <summary>
    ''' Template definition for standard task types, statutory workflow steps, and department mapping.
    ''' </summary>
    Public Class TaskWorkflowTemplate
        Public Property TaskType As String = String.Empty
        Public Property CategoryCode As String = String.Empty
        Public Property Department As DepartmentType = DepartmentType.IncomeTax
        Public Property DefaultDueDateDays As Integer = 3
        Public Property StandardSteps As New List(Of String)()
    End Class

    ''' <summary>
    ''' Centralized authoritative provider for task-type workflow templates and standard checklist definitions.
    ''' Prevents hardcoded workflow step conditions in presentation UI components.
    ''' </summary>
    Public Class TaskWorkflowTemplateProvider
        Private Shared ReadOnly _templates As New Dictionary(Of String, TaskWorkflowTemplate)(StringComparer.OrdinalIgnoreCase)

        Shared Sub New()
            _templates.Add("GSTR-3B Filing", New TaskWorkflowTemplate With {
                .TaskType = "GSTR-3B Filing",
                .CategoryCode = "GSTR_3B",
                .Department = DepartmentType.IncomeTax,
                .DefaultDueDateDays = 3,
                .StandardSteps = New List(Of String) From {
                    "Verify purchase & sales data",
                    "Match with GSTR-2B ITC statement",
                    "Prepare return summary draft",
                    "File return on GST Govt Portal"
                }
            })

            _templates.Add("GSTR-1 Filing", New TaskWorkflowTemplate With {
                .TaskType = "GSTR-1 Filing",
                .CategoryCode = "GSTR_1",
                .Department = DepartmentType.IncomeTax,
                .DefaultDueDateDays = 3,
                .StandardSteps = New List(Of String) From {
                    "Export B2B & B2C sales register",
                    "Validate HSN summary & tax rates",
                    "Generate JSON & upload to portal",
                    "File GSTR-1 return with EVC/DSC"
                }
            })

            _templates.Add("Income Tax Return (ITR)", New TaskWorkflowTemplate With {
                .TaskType = "Income Tax Return (ITR)",
                .CategoryCode = "ITR_FILING",
                .Department = DepartmentType.IncomeTax,
                .DefaultDueDateDays = 5,
                .StandardSteps = New List(Of String) From {
                    "Collect AIS / TIS & Form 26AS",
                    "Reconcile bank statements & TDS",
                    "Prepare tax computation & draft",
                    "File ITR & verify via EVC"
                }
            })

            _templates.Add("TDS Quarterly Return (26Q/27Q)", New TaskWorkflowTemplate With {
                .TaskType = "TDS Quarterly Return (26Q/27Q)",
                .CategoryCode = "TDS_RETURN",
                .Department = DepartmentType.IncomeTax,
                .DefaultDueDateDays = 4,
                .StandardSteps = New List(Of String) From {
                    "Collect deduction vouchers & Challans",
                    "Validate PAN numbers via Traces",
                    "Generate FVU file & verify errors",
                    "File return & generate Form 16/16A"
                }
            })

            _templates.Add("GST Verification & Audit", New TaskWorkflowTemplate With {
                .TaskType = "GST Verification & Audit",
                .CategoryCode = "TAX_AUDIT",
                .Department = DepartmentType.IncomeTax,
                .DefaultDueDateDays = 7,
                .StandardSteps = New List(Of String) From {
                    "Verify GSTIN registration status",
                    "Cross-examine GSTR-3B vs 2A/2B",
                    "Audit turnover & liability mismatches",
                    "Prepare audit observations report"
                }
            })

            _templates.Add("ROC Annual Filing", New TaskWorkflowTemplate With {
                .TaskType = "ROC Annual Filing",
                .CategoryCode = "STAT_AUDIT",
                .Department = DepartmentType.Accounting,
                .DefaultDueDateDays = 10,
                .StandardSteps = New List(Of String) From {
                    "Prepare AOC-4 & MGT-7 drafts",
                    "Attach Auditor's Report & Financials",
                    "Obtain Director DSC signatures",
                    "Upload MCA forms & pay fee"
                }
            })

            _templates.Add("Statutory Audit", New TaskWorkflowTemplate With {
                .TaskType = "Statutory Audit",
                .CategoryCode = "STAT_AUDIT",
                .Department = DepartmentType.Accounting,
                .DefaultDueDateDays = 14,
                .StandardSteps = New List(Of String) From {
                    "Conduct audit sampling & ledger analysis",
                    "Verify statutory compliance & registers",
                    "Draft independent auditor's report",
                    "Finalize audit certificate with Partner"
                }
            })

            _templates.Add("Accounting & Bookkeeping", New TaskWorkflowTemplate With {
                .TaskType = "Accounting & Bookkeeping",
                .CategoryCode = "BOOKKEEPING",
                .Department = DepartmentType.Accounting,
                .DefaultDueDateDays = 5,
                .StandardSteps = New List(Of String) From {
                    "Collect bank statements & purchase bills",
                    "Pass accounting journal entries in software",
                    "Reconcile trial balance & bank books",
                    "Generate financial statements summary"
                }
            })

            _templates.Add("Client Onboarding", New TaskWorkflowTemplate With {
                .TaskType = "Client Onboarding",
                .CategoryCode = "BOOKKEEPING",
                .Department = DepartmentType.Administration,
                .DefaultDueDateDays = 2,
                .StandardSteps = New List(Of String) From {
                    "Collect PAN, GSTIN & Incorporate docs",
                    "Verify portal login credentials",
                    "Create client master profile in system",
                    "Assign primary engagement staff"
                }
            })
        End Sub

        Public Shared Function GetAllTaskTypes() As List(Of String)
            Return _templates.Keys.ToList()
        End Function

        Public Shared Function GetTemplate(taskType As String) As TaskWorkflowTemplate
            If String.IsNullOrWhiteSpace(taskType) OrElse Not _templates.ContainsKey(taskType) Then
                Return New TaskWorkflowTemplate With {
                    .TaskType = If(String.IsNullOrWhiteSpace(taskType), "Other Task", taskType),
                    .Department = DepartmentType.IncomeTax,
                    .DefaultDueDateDays = 3,
                    .StandardSteps = New List(Of String) From {
                        "Review task instructions & scope",
                        "Execute operational work steps",
                        "Verify output with Senior Partner",
                        "Mark task complete upon delivery"
                    }
                }
            End If
            Return _templates(taskType)
        End Function

        ''' <summary>
        ''' Centralized business rule for calculating suggested task priority based on target due date.
        ''' Overdue / Due Today = Urgent
        ''' 1 to 3 Days = High
        ''' 4 to 7 Days = Medium
        ''' > 7 Days = Low
        ''' </summary>
        Public Shared Function CalculatePriorityFromDueDate(targetDueDate As DateTime) As TaskPriority
            Dim daysDiff As Integer = CInt((targetDueDate.Date - DateTime.Today).TotalDays)
            If daysDiff <= 0 Then
                Return TaskPriority.Urgent
            ElseIf daysDiff <= 3 Then
                Return TaskPriority.High
            ElseIf daysDiff <= 7 Then
                Return TaskPriority.Medium
            Else
                Return TaskPriority.Low
            End If
        End Function

        ''' <summary>
        ''' Centralized authoritative task title generator based on task type and filing period month.
        ''' </summary>
        Public Shared Function GenerateDefaultTitle(taskType As String, targetDueDate As DateTime) As String
            Dim typeName = If(String.IsNullOrWhiteSpace(taskType), "Task", taskType.Trim())
            Return $"{typeName} - {targetDueDate.ToString("MMM yyyy")}"
        End Function
    End Class
End Namespace
