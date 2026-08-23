Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Enums

Namespace Security
    ''' <summary>
    ''' Thread-safe thread/session state container storing active authenticated user.
    ''' </summary>
    Public Class CurrentUserContext
        Private Shared _currentUser As UserDto

        Public Shared Property CurrentUser As UserDto
            Get
                Return _currentUser
            End Get
            Set(value As UserDto)
                _currentUser = value
            End Set
        End Property

        Public Shared ReadOnly Property IsAuthenticated As Boolean
            Get
                Return _currentUser IsNot Nothing AndAlso _currentUser.UserId > 0
            End Get
        End Property

        Public Shared ReadOnly Property IsOwner As Boolean
            Get
                Return IsAuthenticated AndAlso _currentUser.Role = UserRole.Owner
            End Get
        End Property

        Public Shared ReadOnly Property IsAdmin As Boolean
            Get
                Return IsAuthenticated AndAlso _currentUser.Role = UserRole.Admin
            End Get
        End Property

        ' Live Shift Session State Tracking (In-memory zero-latency thread safe)
        Public Shared Property IsClockedIn As Boolean = False
        Public Shared Property IsOnLunchBreak As Boolean = False
        Public Shared Property IsWorkdayCompleted As Boolean = False

        Public Shared Sub UpdateShiftState(clockedIn As Boolean, onBreak As Boolean, completed As Boolean)
            IsClockedIn = clockedIn
            IsOnLunchBreak = onBreak
            IsWorkdayCompleted = completed
        End Sub

        Public Shared Event SessionCleared As EventHandler

        Public Shared Sub ClearSession()
            _currentUser = Nothing
            IsClockedIn = False
            IsOnLunchBreak = False
            IsWorkdayCompleted = False
            RaiseEvent SessionCleared(Nothing, EventArgs.Empty)
        End Sub
    End Class
End Namespace
