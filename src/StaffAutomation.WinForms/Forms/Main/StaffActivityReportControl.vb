Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms
Imports StaffAutomation.Core.DTOs.Reports

Namespace Forms.Main
    Public Class StaffActivityReportControl
        Inherits UserControl

        Private ReadOnly tvStaff As TreeView
        Private ReadOnly dgvTasks As DataGridView
        Private ReadOnly lblSummary As Label
        Private _currentData As List(Of StaffDailyActivitySummaryDto)

        Public Sub New()
            Me.Dock = DockStyle.Fill
            
            Dim splitContainer = New SplitContainer() With {
                .Dock = DockStyle.Fill,
                .SplitterDistance = 350,
                .FixedPanel = FixedPanel.Panel1
            }

            tvStaff = New TreeView() With {
                .Dock = DockStyle.Fill,
                .Font = New Font("Segoe UI", 9.5!),
                .HideSelection = False
            }
            AddHandler tvStaff.AfterSelect, AddressOf TvStaff_AfterSelect

            Dim pnlRight = New Panel() With {.Dock = DockStyle.Fill}
            
            lblSummary = New Label() With {
                .Dock = DockStyle.Top,
                .Height = 40,
                .Font = New Font("Segoe UI", 10.0!, FontStyle.Bold),
                .TextAlign = ContentAlignment.MiddleLeft,
                .Padding = New Padding(5, 0, 0, 0)
            }

            dgvTasks = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .ReadOnly = True,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                .BackgroundColor = Color.White,
                .RowHeadersVisible = False
            }

            pnlRight.Controls.Add(dgvTasks)
            pnlRight.Controls.Add(lblSummary)

            splitContainer.Panel1.Controls.Add(tvStaff)
            splitContainer.Panel2.Controls.Add(pnlRight)
            
            Me.Controls.Add(splitContainer)
        End Sub

        Public Sub LoadData(data As List(Of StaffDailyActivitySummaryDto))
            _currentData = data
            tvStaff.Nodes.Clear()
            dgvTasks.DataSource = Nothing
            lblSummary.Text = ""

            If data Is Nothing OrElse data.Count = 0 Then Return

            For Each staff In data
                Dim staffNode = New TreeNode(staff.SummaryText)
                staffNode.Tag = staff

                For Each day In staff.Days
                    Dim dayNode = New TreeNode(day.SummaryText)
                    dayNode.Tag = day
                    staffNode.Nodes.Add(dayNode)
                Next

                tvStaff.Nodes.Add(staffNode)
            Next
            
            tvStaff.ExpandAll()
        End Sub

        Private Sub TvStaff_AfterSelect(sender As Object, e As TreeViewEventArgs)
            Dim node = e.Node
            If node Is Nothing OrElse node.Tag Is Nothing Then Return

            If TypeOf node.Tag Is StaffDailyActivitySummaryDto Then
                Dim staff = DirectCast(node.Tag, StaffDailyActivitySummaryDto)
                lblSummary.Text = $"Staff Summary: {staff.SummaryText} - Select a date to view tasks."
                dgvTasks.DataSource = Nothing
            ElseIf TypeOf node.Tag Is StaffDailyActivityDayDto Then
                Dim day = DirectCast(node.Tag, StaffDailyActivityDayDto)
                lblSummary.Text = $"Daily Tasks: {day.SummaryText}"
                dgvTasks.DataSource = day.Tasks
                
                If dgvTasks.Columns.Count > 0 Then
                    dgvTasks.Columns("TaskId").Visible = False
                    dgvTasks.Columns("TaskTitle").HeaderText = "Task Title"
                    dgvTasks.Columns("ClientName").HeaderText = "Client Name"
                    dgvTasks.Columns("DurationMinutes").HeaderText = "Duration (Mins)"
                    dgvTasks.Columns("TimeCategoryName").HeaderText = "Category"
                End If
            End If
        End Sub
        
        Public Function GetRawData() As List(Of StaffDailyActivitySummaryDto)
            Return _currentData
        End Function
    End Class
End Namespace
