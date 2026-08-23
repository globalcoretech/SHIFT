Imports System.Collections.Generic
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Security

Namespace Security
    ''' <summary>
    ''' Implementation of Permission Provider resolving permissions bound to UserRoles.
    ''' </summary>
    Public Class PermissionProvider
        Implements IPermissionProvider

        Public Function GetPermissionsForRole(role As UserRole) As List(Of String) Implements IPermissionProvider.GetPermissionsForRole
            Dim list As New List(Of String)()
            Select Case role
                Case UserRole.Employee
                    list.Add("Task.ViewAssigned")
                    list.Add("Task.LogActivity")
                    list.Add("Discussion.Log")
                    list.Add("Attendance.Punch")
                Case UserRole.Owner
                    list.Add("Task.ViewAll")
                    list.Add("Task.Assign")
                    list.Add("Dashboard.ViewLive")
                    list.Add("Reports.ExportExcel")
                    list.Add("Client.Manage")
                    list.Add("Attendance.Monitor")
                    list.Add("Attendance.Correct")
                Case UserRole.Admin
                    list.Add("Task.ViewAll")
                    list.Add("Task.Assign")
                    list.Add("User.Manage")
                    list.Add("Client.Manage")
                    list.Add("System.AuditView")
                    list.Add("System.CatalogManage")
                    list.Add("Attendance.Monitor")
                    list.Add("Attendance.Correct")
            End Select
            Return list
        End Function
    End Class
End Namespace
