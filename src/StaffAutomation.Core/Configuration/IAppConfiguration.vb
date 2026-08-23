Namespace Configuration
    ''' <summary>
    ''' Central configuration contract delivering validated settings and encrypted connection strings.
    ''' </summary>
    Public Interface IAppConfiguration
        Function GetConnectionString(Optional name As String = "StaffAutomationDb") As String
        Function GetSetting(key As String, Optional defaultValue As String = "") As String
        Function GetSettingInt(key As String, Optional defaultValue As Integer = 0) As Integer
        Sub ValidateConfiguration()
    End Interface
End Namespace
