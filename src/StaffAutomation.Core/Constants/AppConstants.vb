Namespace Constants
    ''' <summary>
    ''' System-wide centralized constants enforcing the No Magic Values Policy.
    ''' </summary>
    Public Class AppConstants
        Public Const AppName As String = "CA Office Workforce Productivity Automation System"
        Public Const AppVersion As String = "1.0.0.0"
        Public Const DefaultConnectionStringName As String = "StaffAutomationDb"
        Public Const DefaultLogFolderPath As String = "logs"
        Public Const DefaultAutoSaveIntervalSeconds As Integer = 60
        Public Const DefaultMaxDashboardRefreshMs As Integer = 500
        Public Const MinimumPasswordLength As Integer = 8
        Public Const LegacySentinelHash As String = "zJ3H0S0g1X2k3L4m5N6o7P8q9R0s1T2u3V4w5X6y7Z8="

        ' Department Codes
        Public Const DepartmentCodeAccounting As String = "ACCT"
        Public Const DepartmentCodeIncomeTax As String = "TAX"
        Public Const DepartmentCodeAdmin As String = "ADMIN"

        ' Role Names
        Public Const RoleNameEmployee As String = "Employee"
        Public Const RoleNameOwner As String = "Owner"
        Public Const RoleNameAdmin As String = "Admin"

        ' Date Formats
        Public Const DisplayDateFormat As String = "dd-MMM-yyyy"
        Public Const DisplayTimeFormat As String = "hh:mm tt"
        Public Const DisplayDateTimeFormat As String = "dd-MMM-yyyy hh:mm tt"
        Public Const IsoDateFormat As String = "yyyy-MM-dd"
    End Class
End Namespace
