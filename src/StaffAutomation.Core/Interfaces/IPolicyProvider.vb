Option Strict On
Option Explicit On

Imports System
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Configuration

Namespace Interfaces
    ''' <summary>
    ''' Interface for policy providers retrieving effective attendance policy configurations.
    ''' Allows future Settings, Branch, Department, or User-specific policy overrides.
    ''' </summary>
    Public Interface IPolicyProvider
        Function GetEffectivePolicyAsync(userId As Integer, attendanceDate As DateTime) As Task(Of AttendancePolicyConfig)
    End Interface
End Namespace
