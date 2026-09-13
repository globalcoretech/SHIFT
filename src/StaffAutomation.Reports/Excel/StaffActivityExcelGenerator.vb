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
                ws.Cell(1, 1).Value = "Staff / Date"
                ws.Cell(1, 2).Value = "Task / Details"
                ws.Cell(1, 3).Value = "Client Name"
                ws.Cell(1, 4).Value = "Duration (Mins)"
                ws.Cell(1, 5).Value = "Category"
                
                ws.Range(1, 1, 1, 5).Style.Font.Bold = True
                ws.Range(1, 1, 1, 5).Style.Fill.BackgroundColor = XLColor.LightGray

                Dim currentRow As Integer = 2

                For Each staff In data
                    ' Level 1: Staff
                    ws.Cell(currentRow, 1).Value = staff.SummaryText
                    ws.Cell(currentRow, 1).Style.Font.Bold = True
                    ws.Cell(currentRow, 1).Style.Fill.BackgroundColor = XLColor.LightBlue
                    Dim staffStartRow = currentRow
                    currentRow += 1
                    
                    For Each dayDto In staff.Days
                        ' Level 2: Day
                        ws.Cell(currentRow, 1).Value = dayDto.SummaryText
                        ws.Cell(currentRow, 1).Style.Font.Italic = True
                        ws.Cell(currentRow, 1).Style.Fill.BackgroundColor = XLColor.LightYellow
                        Dim dayStartRow = currentRow
                        currentRow += 1
                        
                        If dayDto.Tasks.Count > 0 Then
                            For Each task In dayDto.Tasks
                                ' Level 3: Task
                                ws.Cell(currentRow, 2).Value = task.TaskTitle
                                ws.Cell(currentRow, 3).Value = task.ClientName
                                ws.Cell(currentRow, 4).Value = task.DurationMinutes
                                ws.Cell(currentRow, 5).Value = task.TimeCategoryName
                                currentRow += 1
                            Next
                            ' Outline Tasks under Day
                            ws.Rows(dayStartRow + 1, currentRow - 1).Group()
                        Else
                            ws.Cell(currentRow, 2).Value = "No tasks logged"
                            ws.Cell(currentRow, 2).Style.Font.Italic = True
                            currentRow += 1
                            ws.Rows(dayStartRow + 1, currentRow - 1).Group()
                        End If
                    Next
                    ' Outline Days under Staff
                    ws.Rows(staffStartRow + 1, currentRow - 1).Group()
                Next
                
                ws.Columns().AdjustToContents()
                
                ' Collapse outlines by default
                ws.CollapseRows()

                wb.SaveAs(filePath)
            End Using
        End Sub
    End Class
End Namespace
