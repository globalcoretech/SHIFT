Imports System.Threading

Namespace Forms.Common
    ''' <summary>
    ''' Thread-safe data state tracker for event-driven cache invalidation.
    ''' </summary>
    Public Module DataStateTracker
        Private _tasksDataVersion As Long = 1
        Private _clientsDataVersion As Long = 1
        Private _attendanceDataVersion As Long = 1

        Public ReadOnly Property TasksDataVersion As Long
            Get
                Return Interlocked.Read(_tasksDataVersion)
            End Get
        End Property

        Public ReadOnly Property ClientsDataVersion As Long
            Get
                Return Interlocked.Read(_clientsDataVersion)
            End Get
        End Property

        Public ReadOnly Property AttendanceDataVersion As Long
            Get
                Return Interlocked.Read(_attendanceDataVersion)
            End Get
        End Property

        Public Sub MarkTasksChanged()
            Interlocked.Increment(_tasksDataVersion)
        End Sub

        Public Sub MarkClientsChanged()
            Interlocked.Increment(_clientsDataVersion)
        End Sub

        Public Sub MarkAttendanceChanged()
            Interlocked.Increment(_attendanceDataVersion)
        End Sub
    End Module
End Namespace
