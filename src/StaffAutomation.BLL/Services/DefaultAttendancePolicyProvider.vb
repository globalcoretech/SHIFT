Option Strict On
Option Explicit On

Imports System
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Configuration
Imports StaffAutomation.Core.Interfaces

Namespace Services
    ''' <summary>
    ''' Default implementation of IPolicyProvider returning system default AttendancePolicyConfig.
    ''' Acts as the baseline policy provider ready for future Settings module integration.
    ''' </summary>
    Public Class DefaultAttendancePolicyProvider
        Implements IPolicyProvider

        Public Function GetEffectivePolicyAsync(userId As Integer, attendanceDate As DateTime) As Task(Of AttendancePolicyConfig) Implements IPolicyProvider.GetEffectivePolicyAsync
            Return Task.FromResult(New AttendancePolicyConfig())
        End Function
    End Class
End Namespace
