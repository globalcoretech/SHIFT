Imports System

Namespace Common
    ''' <summary>
    ''' System clock implementation returning system UTC and Local dates/times.
    ''' </summary>
    Public Class SystemDateTimeProvider
        Implements IDateTimeProvider

        Public ReadOnly Property UtcNow As DateTime Implements IDateTimeProvider.UtcNow
            Get
                Return DateTime.UtcNow
            End Get
        End Property

        Public ReadOnly Property LocalNow As DateTime Implements IDateTimeProvider.LocalNow
            Get
                Return DateTime.Now
            End Get
        End Property

        Public ReadOnly Property Today As DateTime Implements IDateTimeProvider.Today
            Get
                Return DateTime.Today
            End Get
        End Property
    End Class
End Namespace
