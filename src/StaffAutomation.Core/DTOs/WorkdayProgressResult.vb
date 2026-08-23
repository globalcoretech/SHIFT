Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic

Namespace DTOs
    ''' <summary>
    ''' Pure domain facts DTO returning all calculated workday metrics, dynamic exit times, state codes, 
    ''' UI action flags, and dynamic timeline item collections. Pure facts with zero UI text coupling.
    ''' </summary>
    Public Class WorkdayProgressResult
        ' Inputs / Context
        Public Property ClockInTime As Nullable(Of DateTime)
        Public Property ClockOutTime As Nullable(Of DateTime)
        Public Property BreakStartTime As Nullable(Of DateTime)
        Public Property CurrentTime As DateTime

        ' Duration Facts (in Minutes)
        Public Property GrossWorkedMinutes As Integer
        Public Property LunchDeductedMinutes As Integer
        Public Property NetWorkedMinutes As Integer
        Public Property RemainingMinutes As Integer
        Public Property OvertimeMinutes As Integer
        Public Property TargetWorkingMinutes As Integer
        Public Property TotalBreakMinutes As Integer

        ' Dynamic Timestamps
        Public Property ExpectedExitTime As Nullable(Of DateTime)
        Public Property ActualExitTime As Nullable(Of DateTime)
        Public Property OfficeClosingTime As DateTime
        
        ''' <summary>Daily progress ratio capped strictly at 100%.</summary>
        Public Property ProgressPercentage As Integer

        ' State Codes / Enums
        Public Property AttendanceState As String = "NotPunchedIn"                 ' NotPunchedIn, Working, OnBreak, DayCompleted
        Public Property LunchState As String = "BeforeLunch"                      ' BeforeLunch, LunchInProgress, LunchCompleted, Disabled

        ' UI Action State Flags
        Public Property CanPunchIn As Boolean
        Public Property CanPunchOut As Boolean
        Public Property CanPunchBreakOut As Boolean
        Public Property CanPunchBreakIn As Boolean
        Public Property IsWorkdayCompleted As Boolean

        Public ReadOnly Property IsOnLunchBreak As Boolean
            Get
                Return BreakStartTime.HasValue OrElse AttendanceState = "OnBreak" OrElse LunchState = "LunchInProgress" OrElse CanPunchBreakIn
            End Get
        End Property
        Public Property IsLate As Boolean
        Public Property IsEarlyLeave As Boolean
        Public Property IsHalfDay As Boolean
        Public Property IsOvertime As Boolean
        Public Property IsOverbreak As Boolean

        ' Dynamic Timeline Collection
        Public Property TimelineItems As New List(Of TimelineItemDto)()
    End Class
End Namespace
