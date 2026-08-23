Namespace UIHelpers
    ''' <summary>
    ''' Contract for decoupled view navigation and screen transitions.
    ''' </summary>
    Public Interface IViewNavigator
        Sub NavigateToLogin()
        Sub NavigateToEmployeeDashboard()
        Sub NavigateToOwnerDashboard()
        Sub NavigateToAdminConsole()
    End Interface
End Namespace
