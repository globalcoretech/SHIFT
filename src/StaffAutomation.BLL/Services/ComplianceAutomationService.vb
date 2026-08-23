Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.DAL.Interfaces
Imports StaffAutomation.DAL.Repositories

Namespace Services
    ''' <summary>
    ''' Business Logic Service for Compliance Automation rules, statutory deadline calculations,
    ''' automated task creation (with default UNASSIGNED allocation), duplicate prevention, and history logging.
    ''' </summary>
    Public Class ComplianceAutomationService
        Implements IComplianceAutomationService

        Private ReadOnly _compRepo As ComplianceRepository
        Private ReadOnly _taskRepo As ITaskRepository
        Private ReadOnly _auditLogger As IAuditLogger

        Public Sub New(compRepo As ComplianceRepository, Optional taskRepo As ITaskRepository = Nothing, Optional auditLogger As IAuditLogger = Nothing)
            _compRepo = compRepo
            _taskRepo = taskRepo
            _auditLogger = auditLogger
        End Sub

        Public Async Function GetAllRulesAsync(Optional includeInactive As Boolean = False) As Task(Of List(Of ComplianceRuleDto)) Implements IComplianceAutomationService.GetAllRulesAsync
            Return Await _compRepo.GetAllRulesAsync(includeInactive)
        End Function

        Public Async Function GetRuleByIdAsync(ruleId As Integer) As Task(Of ComplianceRuleDto) Implements IComplianceAutomationService.GetRuleByIdAsync
            Return Await _compRepo.GetRuleByIdAsync(ruleId)
        End Function

        Public Async Function SaveRuleAsync(ruleDto As ComplianceRuleDto, modifiedByUserId As Integer) As Task(Of Boolean) Implements IComplianceAutomationService.SaveRuleAsync
            If String.IsNullOrWhiteSpace(ruleDto.ComplianceName) Then
                Throw New ValidationException("Compliance Rule Name is required.", "ComplianceName")
            End If
            If ruleDto.DueDateRuleDay < 1 OrElse ruleDto.DueDateRuleDay > 31 Then
                Throw New ValidationException("Due Date Rule Day must be between 1 and 31.", "DueDateRuleDay")
            End If

            Dim success = Await _compRepo.SaveRuleAsync(ruleDto, modifiedByUserId)
            If success AndAlso _auditLogger IsNot Nothing Then
                Dim action = If(ruleDto.ComplianceRuleId > 0, "COMPLIANCE_RULE_UPDATE", "COMPLIANCE_RULE_CREATE")
                Await _auditLogger.LogAuditAsync(modifiedByUserId, action, "ComplianceAutomationService", $"Compliance rule '{ruleDto.ComplianceName}' saved.")
            End If
            Return success
        End Function

        Public Async Function ToggleRuleStateAsync(ruleId As Integer, isActive As Boolean, modifiedByUserId As Integer) As Task(Of Boolean) Implements IComplianceAutomationService.ToggleRuleStateAsync
            Dim success = Await _compRepo.ToggleRuleStateAsync(ruleId, isActive, modifiedByUserId)
            If success AndAlso _auditLogger IsNot Nothing Then
                Dim stateStr = If(isActive, "ACTIVATED", "DEACTIVATED")
                Await _auditLogger.LogAuditAsync(modifiedByUserId, "COMPLIANCE_RULE_TOGGLE", "ComplianceAutomationService", $"Rule ID {ruleId} {stateStr}.")
            End If
            Return success
        End Function

        Public Function CalculateDeadlineDate(rule As ComplianceRuleDto, referenceDate As DateTime) As DateTime Implements IComplianceAutomationService.CalculateDeadlineDate
            Dim ref = referenceDate.Date
            Dim targetYear = ref.Year
            Dim targetMonth = ref.Month

            If String.Equals(rule.Frequency, "Monthly", StringComparison.OrdinalIgnoreCase) Then
                Dim daysInMonth = DateTime.DaysInMonth(targetYear, targetMonth)
                Dim day = Math.Min(rule.DueDateRuleDay, daysInMonth)
                Dim deadline = New DateTime(targetYear, targetMonth, day)

                ' If reference date is past this month's deadline, target next month
                If ref > deadline Then
                    Dim nextM = ref.AddMonths(1)
                    Dim daysInNext = DateTime.DaysInMonth(nextM.Year, nextM.Month)
                    Return New DateTime(nextM.Year, nextM.Month, Math.Min(rule.DueDateRuleDay, daysInNext))
                End If
                Return deadline
            ElseIf String.Equals(rule.Frequency, "Quarterly", StringComparison.OrdinalIgnoreCase) Then
                ' Quarterly Statutory Return (Q1: Jul 31, Q2: Oct 31, Q3: Jan 31, Q4: Apr 30)
                If ref.Month >= 4 AndAlso ref.Month <= 6 Then
                    Return New DateTime(ref.Year, 7, Math.Min(rule.DueDateRuleDay, 31))
                ElseIf ref.Month >= 7 AndAlso ref.Month <= 9 Then
                    Return New DateTime(ref.Year, 10, Math.Min(rule.DueDateRuleDay, 31))
                ElseIf ref.Month >= 10 AndAlso ref.Month <= 12 Then
                    Return New DateTime(ref.Year + 1, 1, Math.Min(rule.DueDateRuleDay, 31))
                Else
                    Return New DateTime(ref.Year, 4, Math.Min(rule.DueDateRuleDay, 30))
                End If
            ElseIf String.Equals(rule.Frequency, "Annual", StringComparison.OrdinalIgnoreCase) Then
                Dim m = If(rule.DueDateRuleMonth.HasValue AndAlso rule.DueDateRuleMonth.Value >= 1 AndAlso rule.DueDateRuleMonth.Value <= 12, rule.DueDateRuleMonth.Value, 9)
                Dim d = Math.Min(rule.DueDateRuleDay, DateTime.DaysInMonth(ref.Year, m))
                Dim deadline = New DateTime(ref.Year, m, d)
                If ref > deadline Then
                    deadline = New DateTime(ref.Year + 1, m, Math.Min(rule.DueDateRuleDay, DateTime.DaysInMonth(ref.Year + 1, m)))
                End If
                Return deadline
            End If

            Return ref.AddDays(7)
        End Function

        Public Async Function EvaluateAutomationAsync(referenceDate As DateTime) As Task(Of Integer) Implements IComplianceAutomationService.EvaluateAutomationAsync
            Dim activeRules = Await _compRepo.GetAllRulesAsync(includeInactive:=False)
            Dim tasksCreatedCount As Integer = 0

            For Each rule In activeRules
                Dim deadline = CalculateDeadlineDate(rule, referenceDate)
                Dim periodName = GetPeriodName(rule, deadline)

                ' Mandatory Duplicate Prevention: Skip if task already generated for this rule + period
                Dim alreadyExecuted = Await _compRepo.HasAutomationExecutedAsync(rule.ComplianceRuleId, periodName)
                If alreadyExecuted Then Continue For

                ' Check if any alert trigger date is reached
                Dim shouldTrigger As Boolean = False
                Dim triggerDate As DateTime = deadline

                If rule.AlertSchedules.Count > 0 Then
                    For Each s In rule.AlertSchedules
                        If Not s.IsEnabled Then Continue For
                        Dim triggerPoint = deadline.AddDays(-s.DaysBeforeDeadline)
                        If referenceDate.Date >= triggerPoint.Date Then
                            shouldTrigger = True
                            triggerDate = triggerPoint
                            Exit For
                        End If
                    Next
                Else
                    ' Default trigger: 7 days before deadline
                    If referenceDate.Date >= deadline.AddDays(-7).Date Then
                        shouldTrigger = True
                        triggerDate = deadline.AddDays(-7)
                    End If
                End If

                If shouldTrigger Then
                    Dim createdTaskId As Nullable(Of Integer) = Nothing

                    If _taskRepo IsNot Nothing Then
                        Dim pEnum As TaskPriority = TaskPriority.High
                        If String.Equals(rule.Priority, "Critical", StringComparison.OrdinalIgnoreCase) Then pEnum = TaskPriority.Urgent
                        If String.Equals(rule.Priority, "Normal", StringComparison.OrdinalIgnoreCase) Then pEnum = TaskPriority.Medium

                        Dim taskCode = $"TSK-COMP-{rule.ComplianceRuleId}-{DateTime.UtcNow:yyyyMMddHHmmss}"
                        Dim newEntity As New TaskEntity() With {
                            .TaskCode = taskCode,
                            .Title = $"{rule.ComplianceName} — {periodName}",
                            .Description = $"Statutory compliance filing requirement for {rule.ComplianceName} ({periodName}). Prepare and complete filing before statutory deadline {deadline:dd MMM yyyy}.",
                            .ClientId = 1,
                            .CategoryId = 1,
                            .FinancialYearId = 1,
                            .AssignedToUserId = 0, ' MANDATORY DEFAULT: UNASSIGNED — REQUIRES ADMIN ALLOCATION
                            .AssignedByUserId = 1,
                            .Department = DepartmentType.IncomeTax,
                            .Priority = pEnum,
                            .WorkflowState = TaskWorkflowState.NewTask,
                            .AssignmentDate = DateTime.UtcNow,
                            .TargetDueDate = deadline,
                            .CreatedBy = 1,
                            .CreatedOn = DateTime.UtcNow
                        }

                        Try
                            Dim taskId = Await _taskRepo.AddAsync(newEntity)
                            If taskId > 0 Then
                                createdTaskId = taskId
                                tasksCreatedCount += 1
                            End If
                        Catch
                        End Try
                    End If

                    ' Record Automation History
                    Dim hist As New ComplianceAutomationHistoryDto() With {
                        .ComplianceRuleId = rule.ComplianceRuleId,
                        .ComplianceName = rule.ComplianceName,
                        .CompliancePeriod = periodName,
                        .DeadlineDate = deadline,
                        .TriggerDate = triggerDate,
                        .TaskId = createdTaskId,
                        .AlertGenerated = True,
                        .TaskGenerated = createdTaskId.HasValue,
                        .Status = "Triggered"
                    }
                    Await _compRepo.RecordAutomationHistoryAsync(hist)

                    If _auditLogger IsNot Nothing Then
                        Await _auditLogger.LogAuditAsync(1, "COMPLIANCE_AUTOMATION_TRIGGER", "ComplianceAutomationService", $"Automated task generated for '{rule.ComplianceName}' ({periodName}). Task ID: {If(createdTaskId.HasValue, createdTaskId.Value.ToString(), "Unassigned")}.")
                    End If
                End If
            Next

            Return tasksCreatedCount
        End Function

        Public Async Function GetUpcomingPreviewsAsync() As Task(Of List(Of UpcomingComplianceAutomationPreviewDto)) Implements IComplianceAutomationService.GetUpcomingPreviewsAsync
            Dim list As New List(Of UpcomingComplianceAutomationPreviewDto)()
            Dim activeRules = Await _compRepo.GetAllRulesAsync(includeInactive:=False)
            Dim refDate = DateTime.Today

            For Each rule In activeRules
                Dim deadline = CalculateDeadlineDate(rule, refDate)
                Dim periodName = GetPeriodName(rule, deadline)

                Dim firstTrigger = deadline.AddDays(-7)
                If rule.AlertSchedules.Count > 0 Then
                    Dim maxDays = 7
                    For Each s In rule.AlertSchedules
                        If s.IsEnabled AndAlso s.DaysBeforeDeadline > maxDays Then
                            maxDays = s.DaysBeforeDeadline
                        End If
                    Next
                    firstTrigger = deadline.AddDays(-maxDays)
                End If

                list.Add(New UpcomingComplianceAutomationPreviewDto() With {
                    .RuleId = rule.ComplianceRuleId,
                    .ComplianceName = rule.ComplianceName,
                    .PeriodName = periodName,
                    .DeadlineDate = deadline,
                    .NextAlertDate = firstTrigger,
                    .TaskCreationDate = firstTrigger,
                    .Status = "Scheduled"
                })
            Next

            Return list
        End Function

        Public Async Function GetHistoryAsync(Optional topCount As Integer = 50) As Task(Of List(Of ComplianceAutomationHistoryDto)) Implements IComplianceAutomationService.GetHistoryAsync
            Return Await _compRepo.GetHistoryAsync(topCount)
        End Function

        Private Function GetPeriodName(rule As ComplianceRuleDto, deadline As DateTime) As String
            If String.Equals(rule.Frequency, "Monthly", StringComparison.OrdinalIgnoreCase) Then
                Return deadline.AddMonths(-1).ToString("MMMM yyyy")
            ElseIf String.Equals(rule.Frequency, "Quarterly", StringComparison.OrdinalIgnoreCase) Then
                Return $"Q{(deadline.Month / 3)} {deadline:yyyy}"
            Else
                Return $"FY {deadline.Year - 1}-{deadline.ToString("yy")}"
            End If
        End Function
    End Class
End Namespace
