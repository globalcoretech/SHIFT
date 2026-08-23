Option Strict On
Option Explicit On

Imports System
Imports StaffAutomation.Core.Interfaces

Namespace Services
    ''' <summary>
    ''' Implementation of IClockProvider returning DateTime.Now from the system environment.
    ''' </summary>
    Public Class SystemClockProvider
        Implements IClockProvider

        Public ReadOnly Property Now As DateTime Implements IClockProvider.Now
            Get
                Return DateTime.Now
            End Get
        End Property
    End Class
End Namespace
