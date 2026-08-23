Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.Core.DTOs

Namespace Interfaces
    ''' <summary>
    ''' Service interface for Compliance Automation rule configuration, deadline calculations,
    ''' automated task generation, duplicate prevention, and automation previews.
    ''' </summary>
    Public Interface IComplianceAutomationService
        Function GetAllRulesAsync(Optional includeInactive As Boolean = False) As Task(Of List(Of ComplianceRuleDto))
        Function GetRuleByIdAsync(ruleId As Integer) As Task(Of ComplianceRuleDto)
        Function SaveRuleAsync(ruleDto As ComplianceRuleDto, modifiedByUserId As Integer) As Task(Of Boolean)
        Function ToggleRuleStateAsync(ruleId As Integer, isActive As Boolean, modifiedByUserId As Integer) As Task(Of Boolean)
        Function CalculateDeadlineDate(rule As ComplianceRuleDto, referenceDate As DateTime) As DateTime
        Function EvaluateAutomationAsync(referenceDate As DateTime) As Task(Of Integer)
        Function GetUpcomingPreviewsAsync() As Task(Of List(Of UpcomingComplianceAutomationPreviewDto))
        Function GetHistoryAsync(Optional topCount As Integer = 50) As Task(Of List(Of ComplianceAutomationHistoryDto))
    End Interface
End Namespace
