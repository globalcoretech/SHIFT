Imports System.Windows.Forms
Imports StaffAutomation.Core.Security

Namespace UIHelpers
    ''' <summary>
    ''' Implementation of IViewNavigator managing application form navigation and shell transitions.
    ''' </summary>
    Public Class ViewNavigator
        Implements IViewNavigator

        Private _mainShellInstance As Form

        Public Sub NavigateToLogin() Implements IViewNavigator.NavigateToLogin
            If _mainShellInstance IsNot Nothing AndAlso Not _mainShellInstance.IsDisposed Then
                Dim shell = TryCast(_mainShellInstance, Forms.Main.FrmMainShell)
                If shell IsNot Nothing Then
                    shell.IsExplicitExit = True
                End If
                _mainShellInstance.Hide()
                _mainShellInstance.Close()
                _mainShellInstance.Dispose()
                _mainShellInstance = Nothing
            End If

            ' Clear session context on navigate to login
            CurrentUserContext.ClearSession()

            Dim loginForm = Factory.CreateLoginForm(Me)
            loginForm.Show()
        End Sub

        Public Sub NavigateToEmployeeDashboard() Implements IViewNavigator.NavigateToEmployeeDashboard
            OpenMainShell()
        End Sub

        Public Sub NavigateToOwnerDashboard() Implements IViewNavigator.NavigateToOwnerDashboard
            OpenMainShell()
        End Sub

        Public Sub NavigateToAdminConsole() Implements IViewNavigator.NavigateToAdminConsole
            OpenMainShell()
        End Sub

        Private Sub OpenMainShell()
            _mainShellInstance = Factory.CreateMainShellForm(Me)
            _mainShellInstance.Show()
        End Sub
    End Class

    ''' <summary>
    ''' Service locator factory for instantiating WinForms screens with injected service dependencies.
    ''' </summary>
    Public Class Factory
        Public Shared Function CreateLoginForm(navigator As IViewNavigator) As Form
            Return New Forms.Auth.FrmLogin(navigator)
        End Function

        Public Shared Function CreateMainShellForm(navigator As IViewNavigator) As Form
            Return New Forms.Main.FrmMainShell(navigator)
        End Function

        Public Shared Function CreateClientManagementForm() As Form
            Return New Forms.Admin.FrmClientManagement()
        End Function

        Public Shared Function CreateUserManagementForm() As Form
            Return New Forms.Admin.FrmUserManagement()
        End Function

        Public Shared Function CreateTaskManagementForm() As Form
            Return New Forms.Tasks.FrmTaskManagement()
        End Function

        Public Shared Function CreateDailyTasksControl(Optional mainShellHost As Forms.Main.FrmMainShell = Nothing) As Control
            Return New Forms.Tasks.DailyTasksControl(mainShellHost)
        End Function

        Public Shared Function CreateTaskWorkspaceForm() As Form
            Return New Forms.Tasks.FrmTaskWorkspace()
        End Function
    End Class
End Namespace
