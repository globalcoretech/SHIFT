Imports System

Namespace Exceptions
    ''' <summary>
    ''' Exception thrown when application settings or connection string configuration is missing or invalid.
    ''' </summary>
    Public Class ConfigurationException
        Inherits StaffAutomationException

        Public Property SettingKey As String = String.Empty

        Public Sub New()
            MyBase.New()
        End Sub

        Public Sub New(message As String)
            MyBase.New(message)
        End Sub

        Public Sub New(message As String, settingKey As String)
            MyBase.New(message)
            Me.SettingKey = settingKey
        End Sub

        Public Sub New(message As String, innerException As Exception)
            MyBase.New(message, innerException)
        End Sub
    End Class
End Namespace
