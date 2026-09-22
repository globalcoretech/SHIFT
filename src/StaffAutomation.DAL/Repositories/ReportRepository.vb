Imports System
Imports System.Collections.Generic
Imports Microsoft.Data.SqlClient
Imports System.Linq
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Configuration
Imports StaffAutomation.Core.DTOs.Reports
Imports StaffAutomation.DAL.Interfaces

Namespace Repositories
    Public Class ReportRepository
        Implements IReportRepository

        Private ReadOnly _config As IAppConfiguration

        Public Sub New(config As IAppConfiguration)
            _config = config
        End Sub

        Public Async Function GetStaffDailyActivityReportAsync(userId As Integer?, startDate As Date, endDate As Date) As Task(Of List(Of StaffDailyActivitySummaryDto)) Implements IReportRepository.GetStaffDailyActivityReportAsync
            Dim summaries As New Dictionary(Of Integer, StaffDailyActivitySummaryDto)()

            Dim connectionString = _config.GetConnectionString()
            Using conn As New SqlConnection(connectionString)
                Await conn.OpenAsync()

                ' Query 1: Users
                Dim sqlUsers = "SELECT UserId, FullName FROM dbo.tbl_Users WHERE IsDeleted = 0 " & If(userId.HasValue, "AND UserId = @UserId", "")
                Using cmdUsers As New SqlCommand(sqlUsers, conn)
                    If userId.HasValue Then cmdUsers.Parameters.AddWithValue("@UserId", userId.Value)
                    Using reader = Await cmdUsers.ExecuteReaderAsync()
                        While Await reader.ReadAsync()
                            Dim uId = reader.GetInt32(0)
                            summaries(uId) = New StaffDailyActivitySummaryDto With {
                                .UserId = uId,
                                .StaffName = reader.GetString(1)
                            }
                        End While
                    End Using
                End Using

                ' Pre-populate all days in range for each user to track 'Absent'
                Dim currDate = startDate.Date
                While currDate <= endDate.Date
                    For Each kvp In summaries
                        kvp.Value.Days.Add(New StaffDailyActivityDayDto With {
                            .ActivityDate = currDate,
                            .HasAttendance = False,
                            .AttendanceStatus = "Absent"
                        })
                    Next
                    currDate = currDate.AddDays(1)
                End While

                ' Query 2: Attendance
                Dim sqlAtt = "SELECT UserId, AttendanceDate, ClockInTime, ClockOutTime, BreakStartTime, TotalBreakMinutes, TotalWorkingMinutes " &
                             "FROM dbo.tbl_Attendance WHERE AttendanceDate >= @StartDate AND AttendanceDate <= @EndDate " &
                             If(userId.HasValue, "AND UserId = @UserId", "")
                Using cmdAtt As New SqlCommand(sqlAtt, conn)
                    cmdAtt.Parameters.AddWithValue("@StartDate", startDate.Date)
                    cmdAtt.Parameters.AddWithValue("@EndDate", endDate.Date)
                    If userId.HasValue Then cmdAtt.Parameters.AddWithValue("@UserId", userId.Value)

                    Using reader = Await cmdAtt.ExecuteReaderAsync()
                        While Await reader.ReadAsync()
                            Dim uId = reader.GetInt32(0)
                            If Not summaries.ContainsKey(uId) Then Continue While

                            Dim attDate = reader.GetDateTime(1)
                            Dim dayDto = summaries(uId).Days.FirstOrDefault(Function(d) d.ActivityDate = attDate)
                            If dayDto IsNot Nothing Then
                                dayDto.HasAttendance = True
                                dayDto.ClockInTime = reader.GetDateTime(2)
                                dayDto.ClockOutTime = If(reader.IsDBNull(3), CType(Nothing, Date?), reader.GetDateTime(3))
                                dayDto.BreakStartTime = If(reader.IsDBNull(4), CType(Nothing, Date?), reader.GetDateTime(4))
                                dayDto.TotalBreakMinutes = reader.GetInt32(5)
                                dayDto.TotalWorkingMinutes = reader.GetInt32(6)

                                If Not dayDto.ClockOutTime.HasValue Then
                                    dayDto.AttendanceStatus = "Missing Punch Out"
                                ElseIf dayDto.ClockOutTime.Value.TimeOfDay < New TimeSpan(14, 0, 0) OrElse dayDto.TotalWorkingMinutes < 420 Then
                                    dayDto.AttendanceStatus = "Half Day"
                                Else
                                    dayDto.AttendanceStatus = "Present"
                                End If
                            End If
                        End While
                    End Using
                End Using

                ' Query 3: Task Activities
                Dim sqlTasks = "SELECT a.UserId, a.ActivityDate, a.TaskId, t.Title, c.ClientName, a.DurationMinutes, tc.CategoryName, t.StatusId " &
                               "FROM dbo.tbl_TaskActivities a " &
                               "JOIN dbo.tbl_Tasks t ON a.TaskId = t.TaskId " &
                               "JOIN dbo.tbl_Clients c ON t.ClientId = c.ClientId " &
                               "JOIN dbo.tbl_TimeCategories tc ON a.TimeCategoryId = tc.CategoryId " &
                               "WHERE a.ActivityDate >= @StartDate AND a.ActivityDate <= @EndDate " &
                               If(userId.HasValue, "AND a.UserId = @UserId", "")
                Using cmdTasks As New SqlCommand(sqlTasks, conn)
                    cmdTasks.Parameters.AddWithValue("@StartDate", startDate.Date)
                    cmdTasks.Parameters.AddWithValue("@EndDate", endDate.Date)
                    If userId.HasValue Then cmdTasks.Parameters.AddWithValue("@UserId", userId.Value)

                    Using reader = Await cmdTasks.ExecuteReaderAsync()
                        While Await reader.ReadAsync()
                            Dim uId = reader.GetInt32(0)
                            If Not summaries.ContainsKey(uId) Then Continue While

                            Dim actDate = reader.GetDateTime(1)
                            Dim dayDto = summaries(uId).Days.FirstOrDefault(Function(d) d.ActivityDate = actDate)
                            If dayDto IsNot Nothing Then
                                Dim statusId = reader.GetInt32(7)
                                Dim statusName = [Enum].GetName(GetType(StaffAutomation.Core.Enums.TaskWorkflowState), statusId)
                                If String.IsNullOrEmpty(statusName) Then statusName = "Unknown"

                                dayDto.Tasks.Add(New StaffDailyActivityTaskDto With {
                                    .TaskId = reader.GetInt32(2),
                                    .TaskTitle = reader.GetString(3),
                                    .ClientName = reader.GetString(4),
                                    .DurationMinutes = reader.GetInt32(5),
                                    .TimeCategoryName = reader.GetString(6),
                                    .TaskStatus = statusName
                                })
                            End If
                        End While
                    End Using
                End Using
            End Using

            For Each summary In summaries.Values
                summary.Days = summary.Days.OrderBy(Function(d) d.ActivityDate).ToList()
                summary.TotalDaysPresent = summary.Days.Where(Function(d) d.HasAttendance).Count()
                summary.TotalAttendanceHours = summary.Days.Sum(Function(d) d.TotalWorkingMinutes) / 60.0
                summary.TotalWorkingHours = summary.Days.Sum(Function(d) d.TotalTaskDurationMinutes) / 60.0
            Next

            Return summaries.Values.OrderBy(Function(s) s.StaffName).ToList()
        End Function
    End Class
End Namespace
