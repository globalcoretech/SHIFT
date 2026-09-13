Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Threading.Tasks
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports StaffAutomation.BLL.Services
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Interfaces
Imports StaffAutomation.DAL.Interfaces
Imports StaffAutomation.DAL.Repositories

Namespace StaffAutomation.Tests
    ''' <summary>
    ''' Automated unit tests for Phase 12.6 Compliance Automation system.
    ''' Verifies deadline calculations, default UNASSIGNED task allocation, duplicate task prevention,
    ''' and upcoming preview calculations.
    ''' </summary>
    <TestClass>
    Public Class ComplianceAutomationTests

        ' 1. Test Monthly Statutory Return Deadline Calculation
        <TestMethod>
        Public Sub TestMonthlyDeadlineCalculation()
            Dim fakeSql As New FakeAuthSqlHelper()
            Dim repo As New ComplianceRepository(fakeSql)
            Dim service As New ComplianceAutomationService(repo)

            Dim rule As New ComplianceRuleDto() With {
                .ComplianceName = "GSTR-1 Monthly Return",
                .Category = "GST",
                .Frequency = "Monthly",
                .DueDateRuleDay = 11
            }

            Dim refDate As New DateTime(2026, 8, 5)
            Dim deadline = service.CalculateDeadlineDate(rule, refDate)

            If deadline <> New DateTime(2026, 8, 11) Then
                Throw New InvalidOperationException($"Expected monthly deadline 2026-08-11, got {deadline:yyyy-MM-dd}.")
            End If
        End Sub

        ' 2. Test Quarterly Statutory Return Deadline Calculation
        <TestMethod>
        Public Sub TestQuarterlyDeadlineCalculation()
            Dim fakeSql As New FakeAuthSqlHelper()
            Dim repo As New ComplianceRepository(fakeSql)
            Dim service As New ComplianceAutomationService(repo)

            Dim rule As New ComplianceRuleDto() With {
                .ComplianceName = "Quarterly TDS Return",
                .Category = "TDS",
                .Frequency = "Quarterly",
                .DueDateRuleDay = 31
            }

            Dim refDate As New DateTime(2026, 5, 10) ' Q1 (Apr-Jun)
            Dim deadline = service.CalculateDeadlineDate(rule, refDate)

            If deadline <> New DateTime(2026, 7, 31) Then
                Throw New InvalidOperationException($"Expected quarterly deadline 2026-07-31 for Q1, got {deadline:yyyy-MM-dd}.")
            End If
        End Sub

        ' 3. Test Annual Statutory Return Deadline Calculation
        <TestMethod>
        Public Sub TestAnnualDeadlineCalculation()
            Dim fakeSql As New FakeAuthSqlHelper()
            Dim repo As New ComplianceRepository(fakeSql)
            Dim service As New ComplianceAutomationService(repo)

            Dim rule As New ComplianceRuleDto() With {
                .ComplianceName = "Tax Audit (Sec 44AB)",
                .Category = "Tax Audit",
                .Frequency = "Annual",
                .DueDateRuleDay = 30,
                .DueDateRuleMonth = 9
            }

            Dim refDate As New DateTime(2026, 6, 1)
            Dim deadline = service.CalculateDeadlineDate(rule, refDate)

            If deadline <> New DateTime(2026, 9, 30) Then
                Throw New InvalidOperationException($"Expected annual Tax Audit deadline 2026-09-30, got {deadline:yyyy-MM-dd}.")
            End If
        End Sub

        ' 4. Test Automated Task Creation Defaults to UNASSIGNED
        <TestMethod>
        Public Async Function TestTaskCreationDefaultsToUnassignedAsync() As Task
            Dim fakeSql As New FakeAuthSqlHelper()
            Dim compRepo As New ComplianceRepository(fakeSql)
            Dim fakeTaskRepo As New FakeTaskRepository()

            Dim service As New ComplianceAutomationService(compRepo, fakeTaskRepo)

            ' Add rule
            Dim rule As New ComplianceRuleDto() With {
                .ComplianceRuleId = 0,
                .ComplianceName = "Test GSTR-1",
                .Category = "GST",
                .Frequency = "Monthly",
                .DueDateRuleDay = 11,
                .Priority = "High",
                .IsActive = True
            }
            rule.AlertSchedules.Add(New ComplianceAlertScheduleDto() With {.DaysBeforeDeadline = 10, .IsEnabled = True})
            Await compRepo.SaveRuleAsync(rule, 1)

            ' Trigger date reached (e.g. Aug 5 for Aug 11 deadline)
            Dim createdCount = Await service.EvaluateAutomationAsync(New DateTime(2026, 8, 5))

            If createdCount <> 1 Then
                Throw New InvalidOperationException($"Expected 1 task created, got {createdCount}.")
            End If

            If fakeTaskRepo.CreatedTasks.Count = 0 Then
                Throw New InvalidOperationException("Task was not added to repository.")
            End If

            Dim createdTask = fakeTaskRepo.CreatedTasks(0)
            If createdTask.AssignedToUserId <> 0 Then
                Throw New InvalidOperationException($"Generated task MUST default to UNASSIGNED (AssignedToUserId = 0). Got {createdTask.AssignedToUserId}.")
            End If
        End Function

        ' 5. Test Duplicate Prevention Per Compliance Period
        <TestMethod>
        Public Async Function TestDuplicateTaskPreventionPerPeriodAsync() As Task
            Dim fakeSql As New FakeAuthSqlHelper()
            Dim compRepo As New ComplianceRepository(fakeSql)
            Dim fakeTaskRepo As New FakeTaskRepository()

            Dim service As New ComplianceAutomationService(compRepo, fakeTaskRepo)

            Dim rule As New ComplianceRuleDto() With {
                .ComplianceRuleId = 0,
                .ComplianceName = "Test GSTR-1",
                .Category = "GST",
                .Frequency = "Monthly",
                .DueDateRuleDay = 11,
                .Priority = "High",
                .IsActive = True
            }
            rule.AlertSchedules.Add(New ComplianceAlertScheduleDto() With {.DaysBeforeDeadline = 10, .IsEnabled = True})
            Await compRepo.SaveRuleAsync(rule, 1)

            ' First execution
            Dim firstCount = Await service.EvaluateAutomationAsync(New DateTime(2026, 8, 5))
            ' Second execution (Same period)
            Dim secondCount = Await service.EvaluateAutomationAsync(New DateTime(2026, 8, 6))

            If firstCount <> 1 Then
                Throw New InvalidOperationException($"First run expected 1 task, got {firstCount}.")
            End If

            If secondCount <> 0 Then
                Throw New InvalidOperationException($"Second run for same period MUST skip duplicate creation. Got {secondCount}.")
            End If
        End Function
    End Class

    Public Class FakeTaskRepository
        Implements ITaskRepository

        Public Property CreatedTasks As New List(Of TaskEntity)()

        Public Function AddAsync(taskItem As TaskEntity) As Task(Of Integer) Implements ITaskRepository.AddAsync
            CreatedTasks.Add(taskItem)
            Return Task.FromResult(CreatedTasks.Count)
        End Function

        Public Function GetByIdAsync(taskId As Integer) As Task(Of TaskEntity) Implements ITaskRepository.GetByIdAsync
            Return Task.FromResult(CType(Nothing, TaskEntity))
        End Function

        Public Function GetByTaskCodeAsync(taskCode As String) As Task(Of TaskEntity) Implements ITaskRepository.GetByTaskCodeAsync
            Return Task.FromResult(CType(Nothing, TaskEntity))
        End Function

        Public Function GetTasksByAssigneeAsync(userId As Integer, Optional state As Nullable(Of TaskWorkflowState) = Nothing) As Task(Of List(Of TaskEntity)) Implements ITaskRepository.GetTasksByAssigneeAsync
            Return Task.FromResult(New List(Of TaskEntity)())
        End Function

        Public Function GetTasksByClientAsync(clientId As Integer) As Task(Of List(Of TaskEntity)) Implements ITaskRepository.GetTasksByClientAsync
            Return Task.FromResult(New List(Of TaskEntity)())
        End Function

        Public Function GetAllActiveTasksAsync() As Task(Of List(Of TaskEntity)) Implements ITaskRepository.GetAllActiveTasksAsync
            Return Task.FromResult(New List(Of TaskEntity)())
        End Function

        Public Function GetAllTasksAsync(Optional includeDeleted As Boolean = False) As Task(Of List(Of TaskEntity)) Implements ITaskRepository.GetAllTasksAsync
            Return Task.FromResult(New List(Of TaskEntity)())
        End Function

        Public Function UpdateTaskAsync(taskItem As TaskEntity) As Task(Of Boolean) Implements ITaskRepository.UpdateTaskAsync
            Return Task.FromResult(True)
        End Function

        Public Function UpdateStateAsync(taskId As Integer, newState As TaskWorkflowState, modifiedBy As Integer) As Task(Of Boolean) Implements ITaskRepository.UpdateStateAsync
            Return Task.FromResult(True)
        End Function

        Public Function UpdateTaskAcceptanceStateAsync(taskId As Integer, assignedStatusId As Integer, inProgressStatusId As Integer, modifiedBy As Integer, originalModifiedOn As Nullable(Of DateTime), Optional transaction As IDbTransaction = Nothing) As Task(Of Boolean) Implements ITaskRepository.UpdateTaskAcceptanceStateAsync
            Return Task.FromResult(True)
        End Function

        Public Function UpdateTaskCompletionStateAsync(taskId As Integer, statusId As Integer, completionDate As Nullable(Of DateTime), modifiedBy As Integer) As Task(Of Boolean) Implements ITaskRepository.UpdateTaskCompletionStateAsync
            Return Task.FromResult(True)
        End Function

        Public Function UpdateTaskAsync(updateDto As UpdateTaskDto, categoryId As Integer, departmentId As Integer, modifiedBy As Integer, Optional transaction As IDbTransaction = Nothing) As Task(Of Boolean) Implements ITaskRepository.UpdateTaskAsync
            Return Task.FromResult(True)
        End Function

        Public Function SoftDeleteAsync(taskId As Integer, originalModifiedOn As Nullable(Of DateTime), modifiedBy As Integer, Optional transaction As IDbTransaction = Nothing) As Task(Of Boolean) Implements ITaskRepository.SoftDeleteAsync
            Return Task.FromResult(True)
        End Function

        Public Function GetCategoryIdByCodeAsync(categoryCode As String) As Task(Of Integer) Implements ITaskRepository.GetCategoryIdByCodeAsync
            Return Task.FromResult(1)
        End Function

        Public Function GetActiveFinancialYearIdAsync() As Task(Of Integer) Implements ITaskRepository.GetActiveFinancialYearIdAsync
            Return Task.FromResult(1)
        End Function

        Public Function GetStatusIdByStateAsync(state As TaskWorkflowState) As Task(Of Integer) Implements ITaskRepository.GetStatusIdByStateAsync
            Return Task.FromResult(1)
        End Function

        Public Function GetPriorityIdByEnumAsync(priority As TaskPriority) As Task(Of Integer) Implements ITaskRepository.GetPriorityIdByEnumAsync
            Return Task.FromResult(1)
        End Function

        Public Function GetDepartmentIdByEnumAsync(dept As DepartmentType) As Task(Of Integer) Implements ITaskRepository.GetDepartmentIdByEnumAsync
            Return Task.FromResult(1)
        End Function

        Public Function GetCategoryInfoMapAsync() As Task(Of Dictionary(Of Integer, Tuple(Of String, String))) Implements ITaskRepository.GetCategoryInfoMapAsync
            Return Task.FromResult(New Dictionary(Of Integer, Tuple(Of String, String))())
        End Function
        Public Function RestoreAsync(taskId As Integer, modifiedBy As Integer) As Task(Of Boolean) Implements ITaskRepository.RestoreAsync
            Return Task.FromResult(True)
        End Function

        Public Function HardDeleteAsync(taskId As Integer) As Task(Of Boolean) Implements ITaskRepository.HardDeleteAsync
            Return Task.FromResult(True)
        End Function

        Public Function GetTaskDependencyCountsAsync(taskId As Integer) As Task(Of Tuple(Of Integer, Integer)) Implements ITaskRepository.GetTaskDependencyCountsAsync
            Return Task.FromResult(New Tuple(Of Integer, Integer)(0, 0))
        End Function
    End Class
End Namespace
