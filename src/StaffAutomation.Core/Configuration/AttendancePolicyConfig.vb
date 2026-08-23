Option Strict On
Option Explicit On

Imports System

Namespace Configuration
    ''' <summary>
    ''' Domain configuration model for staff attendance policies, timing windows, thresholds, and policy feature toggles.
    ''' Fully decoupled from UI and ready for future Settings, Branch, Department, and Employee overrides.
    ''' </summary>
    Public Class AttendancePolicyConfig
        ''' <summary>Official office start time (Default: 11:00 AM).</summary>
        Public Property OfficeStartTime As TimeSpan = New TimeSpan(11, 0, 0)

        ''' <summary>Official office closing time (Default: 08:00 PM).</summary>
        Public Property OfficeEndTime As TimeSpan = New TimeSpan(20, 0, 0)

        ''' <summary>Official lunch break start time (Default: 02:00 PM).</summary>
        Public Property LunchStartTime As TimeSpan = New TimeSpan(14, 0, 0)

        ''' <summary>Official lunch break end time (Default: 03:00 PM).</summary>
        Public Property LunchEndTime As TimeSpan = New TimeSpan(15, 0, 0)

        ''' <summary>Required net working duration in minutes (Default: 480 mins = 8 Hours).</summary>
        Public Property TargetWorkingMinutes As Integer = 480

        ''' <summary>Grace time allowed for late arrival in minutes (Default: 15 mins).</summary>
        Public Property GraceMinutes As Integer = 15

        ''' <summary>Feature flag enabling automatic lunch overlap deduction.</summary>
        Public Property EnableLunchDeduction As Boolean = True

        ''' <summary>Feature flag enabling late arrival grace period calculation.</summary>
        Public Property EnableGrace As Boolean = True

        ''' <summary>Feature flag enabling overtime hours tracking.</summary>
        Public Property EnableOvertime As Boolean = True

        ''' <summary>Feature flag enabling half-day threshold calculation.</summary>
        Public Property EnableHalfDay As Boolean = True

        ''' <summary>Feature flag enabling dynamic expected exit time calculation.</summary>
        Public Property EnableAutoExpectedExit As Boolean = True

        ''' <summary>Feature flag enabling manual break punch (Lunch Out / Lunch In).</summary>
        Public Property EnableManualBreakPunch As Boolean = True

        ''' <summary>Maximum allowed lunch break duration in minutes (Default: 60 mins).</summary>
        Public Property MaxAllowedBreakMinutes As Integer = 60
    End Class
End Namespace
