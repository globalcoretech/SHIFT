Option Strict On
Option Explicit On

Imports System.Windows.Forms

Namespace Forms.Main
    ''' <summary>
    ''' Manages embedding and lifecycle of child views (UserControls and Forms) inside the Shell workspace panel.
    ''' Ensures zero MDI usage, strict non-floating form containment, and proper resource disposal.
    ''' </summary>
    Public Class WorkspaceLoader
        ''' <summary>
        ''' Embeds the specified Control (UserControl or Form) into the target workspace panel.
        ''' </summary>
        ''' <param name="targetPanel">The parent host panel container (pnlWorkspace).</param>
        ''' <param name="viewControl">The view control or form to embed.</param>
        Public Shared Sub LoadControlIntoPanel(targetPanel As Panel, viewControl As Control)
            If targetPanel Is Nothing OrElse viewControl Is Nothing Then Return

            targetPanel.SuspendLayout()

            ' Remove existing workspace controls from host container to allow instant view re-attachment
            For i As Integer = targetPanel.Controls.Count - 1 To 0 Step -1
                Dim ctrl As Control = targetPanel.Controls(i)
                targetPanel.Controls.RemoveAt(i)
            Next

            If TypeOf viewControl Is Form Then
                Dim frm = CType(viewControl, Form)
                frm.TopLevel = False
                frm.FormBorderStyle = FormBorderStyle.None
                frm.WindowState = FormWindowState.Normal
                frm.AutoSize = False
            ElseIf TypeOf viewControl Is ScrollableControl Then
                CType(viewControl, ScrollableControl).AutoScroll = False
            End If

            viewControl.Bounds = targetPanel.DisplayRectangle
            viewControl.Size = targetPanel.ClientSize
            viewControl.Dock = DockStyle.Fill

            targetPanel.Controls.Add(viewControl)
            viewControl.Show()
            viewControl.BringToFront()
            viewControl.PerformLayout()

            targetPanel.ResumeLayout(True)
            targetPanel.PerformLayout()
        End Sub

        Public Shared Sub LoadFormIntoPanel(targetPanel As Panel, childForm As Form)
            LoadControlIntoPanel(targetPanel, childForm)
        End Sub
    End Class
End Namespace
