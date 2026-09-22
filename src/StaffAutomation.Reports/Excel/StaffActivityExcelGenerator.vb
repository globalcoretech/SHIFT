Imports System.Collections.Generic
Imports System.IO
Imports ClosedXML.Excel
Imports StaffAutomation.Core.DTOs.Reports

Namespace Excel
    Public Class StaffActivityExcelGenerator
        Public Sub GenerateExcel(data As List(Of StaffDailyActivitySummaryDto), filePath As String)
            Using wb As New XLWorkbook()
                Dim ws = wb.Worksheets.Add("Staff Daily Activity")
                
                ' Header
                ws.Cell(1, 1).Value = "Staff Name"
                ws.Cell(1, 2).Value = "Date"
                ws.Cell(1, 3).Value = "Attendance Status"
                ws.Cell(1, 4).Value = "Clock In"
                ws.Cell(1, 5).Value = "Lunch Break"
                ws.Cell(1, 6).Value = "Clock Out"
                ws.Cell(1, 7).Value = "Total Work (Mins)"
                ws.Cell(1, 8).Value = "Task Title"
                ws.Cell(1, 9).Value = "Client Name"
                ws.Cell(1, 10).Value = "Task Category"
                ws.Cell(1, 11).Value = "Task Status"
                ws.Cell(1, 12).Value = "Task Duration (Mins)"
                
                ws.Range(1, 1, 1, 12).Style.Font.Bold = True
                ws.Range(1, 1, 1, 12).Style.Fill.BackgroundColor = XLColor.Navy
                ws.Range(1, 1, 1, 12).Style.Font.FontColor = XLColor.White

                Dim currentRow As Integer = 2

                For Each staff In data
                    For Each dayDto In staff.Days
                        Dim attStatus = dayDto.AttendanceStatus
                        Dim clockIn = If(dayDto.ClockInTime.HasValue, dayDto.ClockInTime.Value.ToString("hh:mm tt"), "-")
                        Dim clockOut = If(dayDto.ClockOutTime.HasValue, dayDto.ClockOutTime.Value.ToString("hh:mm tt"), "-")
                        Dim lunchBreak = If(dayDto.BreakStartTime.HasValue, $"{dayDto.BreakStartTime.Value.ToString("hh:mm tt")} ({dayDto.TotalBreakMinutes}m)", "-")
                        Dim totalWorkMins = If(dayDto.HasAttendance, dayDto.TotalWorkingMinutes.ToString(), "-")

                        If dayDto.Tasks.Count > 0 Then
                            For Each task In dayDto.Tasks
                                ws.Cell(currentRow, 1).Value = staff.StaffName
                                ws.Cell(currentRow, 2).Value = dayDto.ActivityDate.ToString("dd-MMM-yyyy")
                                ws.Cell(currentRow, 3).Value = attStatus
                                ws.Cell(currentRow, 4).Value = clockIn
                                ws.Cell(currentRow, 5).Value = lunchBreak
                                ws.Cell(currentRow, 6).Value = clockOut
                                ws.Cell(currentRow, 7).Value = totalWorkMins
                                
                                ws.Cell(currentRow, 8).Value = task.TaskTitle
                                ws.Cell(currentRow, 9).Value = task.ClientName
                                ws.Cell(currentRow, 10).Value = task.TimeCategoryName
                                ws.Cell(currentRow, 11).Value = task.TaskStatus
                                ws.Cell(currentRow, 12).Value = task.DurationMinutes
                                currentRow += 1
                            Next
                        Else
                            ' Print attendance row even if no tasks logged
                            ws.Cell(currentRow, 1).Value = staff.StaffName
                            ws.Cell(currentRow, 2).Value = dayDto.ActivityDate.ToString("dd-MMM-yyyy")
                            ws.Cell(currentRow, 3).Value = attStatus
                            ws.Cell(currentRow, 4).Value = clockIn
                            ws.Cell(currentRow, 5).Value = lunchBreak
                            ws.Cell(currentRow, 6).Value = clockOut
                            ws.Cell(currentRow, 7).Value = totalWorkMins
                            
                            ws.Cell(currentRow, 8).Value = "-"
                            ws.Cell(currentRow, 9).Value = "-"
                            ws.Cell(currentRow, 10).Value = "-"
                            ws.Cell(currentRow, 11).Value = "-"
                            ws.Cell(currentRow, 12).Value = "-"
                            currentRow += 1
                        End If
                    Next
                Next
                
                ws.Columns().AdjustToContents()

                wb.SaveAs(filePath)
            End Using
        End Sub
    End Class
End Namespace
