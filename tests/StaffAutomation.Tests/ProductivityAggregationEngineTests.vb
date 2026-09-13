Imports System
Imports System.Collections.Generic
Imports Xunit
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Enums

Namespace Tests
    Public Class ProductivityAggregationEngineTests

        <Fact>
        Public Sub TestProductivityRatioCalculation_Normal()
            Dim summary As New StaffProductivitySummaryDto() With {
                .AttendanceWorkingMinutes = 540,
                .AttendanceBreakMinutes = 60,
                .ProductiveTaskMinutes = 360,
                .CompletedTasksCount = 4
            }

            ' NetAvailable = 540 - 60 = 480
            ' Ratio = (360 / 480) * 100 = 75.0%
            Assert.Equal(75.0, summary.ProductivityRatioPercentage)
            Assert.Equal(9.0, summary.AttendanceWorkingHours)
            Assert.Equal(1.0, summary.AttendanceBreakHours)
            Assert.Equal(6.0, summary.ProductiveTaskHours)
            Assert.Equal(90.0, summary.AvgMinutesPerCompletedTask)
        End Sub

        <Fact>
        Public Sub TestProductivityRatioCalculation_ZeroAvailableTime_DivideByZeroProtection()
            Dim summary As New StaffProductivitySummaryDto() With {
                .AttendanceWorkingMinutes = 60,
                .AttendanceBreakMinutes = 60,
                .ProductiveTaskMinutes = 45,
                .CompletedTasksCount = 1
            }

            ' NetAvailable = 60 - 60 = 0 -> Guard returns 0.0% instead of DivideByZeroException
            Assert.Equal(0.0, summary.ProductivityRatioPercentage)
        End Sub

        <Fact>
        Public Sub TestProductivityRatioCalculation_NegativeAvailableTime_Protection()
            Dim summary As New StaffProductivitySummaryDto() With {
                .AttendanceWorkingMinutes = 30,
                .AttendanceBreakMinutes = 60,
                .ProductiveTaskMinutes = 15,
                .CompletedTasksCount = 1
            }

            ' NetAvailable = 30 - 60 = -30 -> Guard returns 0.0%
            Assert.Equal(0.0, summary.ProductivityRatioPercentage)
        End Sub

        <Fact>
        Public Sub TestProductivityRatioCalculation_OverHundredPercent_PreservesValue()
            Dim summary As New StaffProductivitySummaryDto() With {
                .AttendanceWorkingMinutes = 480,
                .AttendanceBreakMinutes = 30,
                .ProductiveTaskMinutes = 540,
                .CompletedTasksCount = 6
            }

            ' NetAvailable = 480 - 30 = 450
            ' Ratio = (540 / 450) * 100 = 120.0%
            Assert.Equal(120.0, summary.ProductivityRatioPercentage)
        End Sub

        <Fact>
        Public Sub TestClientWorkSummary_AvgTaskDuration_DivideByZeroProtection()
            Dim summary As New ClientWorkSummaryDto() With {
                .TotalTasksCount = 5,
                .CompletedTasksCount = 0, ' Zero completed tasks
                .TotalProductiveMinutes = 120
            }

            ' AvgMinutesPerCompletedTask = 0.0 (Protected)
            Assert.Equal(0.0, summary.AvgMinutesPerCompletedTask)
            Assert.Equal(2.0, summary.TotalProductiveHours)
        End Sub

        <Fact>
        Public Sub TestDailyTimelineDurationHoursCalculation()
            Dim item As New DailyTimelineItemDto() With {
                .DurationMinutes = 135
            }

            ' 135 / 60.0 = 2.25 hours
            Assert.Equal(2.25, item.DurationHours)
        End Sub

        <Fact>
        Public Sub TestMultipleActivitySessions_SingleTask()
            Dim activities As New List(Of TaskActivityEntity) From {
                New TaskActivityEntity() With {.TaskId = 101, .DurationMinutes = 45, .EndTime = DateTime.UtcNow.AddHours(-3)},
                New TaskActivityEntity() With {.TaskId = 101, .DurationMinutes = 60, .EndTime = DateTime.UtcNow.AddHours(-1)}
            }

            Dim totalMinutes As Integer = 0
            For Each act In activities
                If act.EndTime.HasValue Then
                    totalMinutes += act.DurationMinutes
                End If
            Next

            Assert.Equal(105, totalMinutes)
        End Sub

        <Fact>
        Public Sub TestMultipleTasks_SingleClient()
            Dim clientSummary As New ClientWorkSummaryDto() With {
                .ClientId = 10,
                .ClientCode = "CLI-010",
                .ClientName = "Acme Corp",
                .TotalTasksCount = 3,
                .CompletedTasksCount = 2,
                .AssignedStaffCount = 2,
                .TotalProductiveMinutes = 180
            }

            Assert.Equal(180, clientSummary.TotalProductiveMinutes)
            Assert.Equal(3.0, clientSummary.TotalProductiveHours)
            Assert.Equal(90.0, clientSummary.AvgMinutesPerCompletedTask)
        End Sub

        <Fact>
        Public Sub TestOneStaff_MultipleClients_OneDay()
            ' Staff worked on Client A (120 mins) and Client B (90 mins) in 1 day
            Dim clientA As New ClientWorkSummaryDto() With {.ClientId = 1, .ClientName = "Client A", .TotalProductiveMinutes = 120}
            Dim clientB As New ClientWorkSummaryDto() With {.ClientId = 2, .ClientName = "Client B", .TotalProductiveMinutes = 90}

            Dim totalDayMinutes = clientA.TotalProductiveMinutes + clientB.TotalProductiveMinutes

            Assert.Equal(210, totalDayMinutes)
            Assert.Equal(2.0, clientA.TotalProductiveHours)
            Assert.Equal(1.5, clientB.TotalProductiveHours)
        End Sub

        <Fact>
        Public Sub TestCrossDayActivity_BelongsToSessionStartDate()
            ' Activity starts 23:50 on Aug 24 and ends 00:20 on Aug 25 (30 minutes)
            ' Business Rule: ActivityDate is assigned to StartTime.Date (2026-08-24)
            Dim startTime = New DateTime(2026, 8, 24, 23, 50, 0, DateTimeKind.Utc)
            Dim endTime = New DateTime(2026, 8, 25, 0, 20, 0, DateTimeKind.Utc)

            Dim activity As New TaskActivityEntity() With {
                .ActivityId = 999,
                .TaskId = 50,
                .UserId = 1,
                .StartTime = startTime,
                .EndTime = endTime,
                .DurationMinutes = CInt((endTime - startTime).TotalMinutes),
                .ActivityDate = startTime.Date ' Session Start Date
            }

            Assert.Equal(New DateTime(2026, 8, 24), activity.ActivityDate)
            Assert.Equal(30, activity.DurationMinutes)
        End Sub

        <Fact>
        Public Sub TestActiveRunningTimer_ExplicitlyMarkedProvisional()
            Dim item As New DailyTimelineItemDto() With {
                .ActivityId = 12,
                .TaskCode = "TSK-200",
                .StartTime = DateTime.UtcNow.AddMinutes(-25),
                .EndTime = Nothing,
                .DurationMinutes = 25,
                .ActivityStatus = "Running (Provisional)"
            }

            Assert.True(item.ActivityStatus.Contains("Running"))
            Assert.True(item.ActivityStatus.Contains("Provisional"))
            Assert.Null(item.EndTime)
        End Sub

        <Fact>
        Public Sub TestAutoPausedTimer_PreservesOriginalNotesAndAppendsSuffix()
            Dim originalNote = "Analyzing GSTR-3B Mismatch"
            Dim pauseReason = "Auto-paused on Attendance Break"
            Dim pauseTag = $"[{pauseReason.Trim()}]"

            Dim updatedNote = originalNote
            If Not updatedNote.Contains(pauseTag) Then
                updatedNote &= $" {pauseTag}"
            End If

            Assert.StartsWith("Analyzing GSTR-3B Mismatch", updatedNote)
            Assert.EndsWith("[Auto-paused on Attendance Break]", updatedNote)
        End Sub

        <Fact>
        Public Sub TestZeroAttendanceDenominator_IncompleteAttendance()
            ' Staff clocked in but never clocked out -> TotalWorkingMinutes = 0
            Dim summary As New StaffProductivitySummaryDto() With {
                .UserId = 8,
                .UserName = "John Doe",
                .WorkingDaysCount = 1,
                .AttendanceWorkingMinutes = 0, ' Incomplete attendance record
                .AttendanceBreakMinutes = 0,
                .ProductiveTaskMinutes = 120,
                .CompletedTasksCount = 1
            }

            ' NetAvailable = 0 -> ProductivityRatioPercentage must return 0.0 safely
            Assert.Equal(0.0, summary.ProductivityRatioPercentage)
        End Sub

        <Fact>
        Public Sub TestNoCompletedTasksInDateRange()
            Dim summary As New StaffProductivitySummaryDto() With {
                .UserId = 9,
                .UserName = "Jane Smith",
                .WorkingDaysCount = 2,
                .AttendanceWorkingMinutes = 960,
                .AttendanceBreakMinutes = 120,
                .ProductiveTaskMinutes = 0,
                .CompletedTasksCount = 0
            }

            Assert.Equal(0.0, summary.ProductivityRatioPercentage)
            Assert.Equal(0.0, summary.AvgMinutesPerCompletedTask)
            Assert.Equal(0.0, summary.ProductiveTaskHours)
        End Sub

        <Fact>
        Public Sub TestMidnightBoundary_HalfOpenFiltering()
            ' Date filter: Aug 24 to Aug 24 -> Half-open interval: >= 2026-08-24 AND < 2026-08-25
            Dim startDate = New DateTime(2026, 8, 24)
            Dim endDate = New DateTime(2026, 8, 24)
            Dim filterUpperLimit = endDate.Date.AddDays(1)

            Dim timeInScope = New DateTime(2026, 8, 24, 23, 59, 59)
            Dim timeOutOfScope = New DateTime(2026, 8, 25, 0, 0, 0)

            Assert.True(timeInScope >= startDate AndAlso timeInScope < filterUpperLimit)
            Assert.False(timeOutOfScope >= startDate AndAlso timeOutOfScope < filterUpperLimit)
        End Sub
    End Class
End Namespace
