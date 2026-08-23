Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms

Namespace Forms.Common
    ''' <summary>
    ''' Ultra-Premium Column Layout Customizer Dialog Window.
    ''' Allows users to toggle column visibility, reorder table columns, and save layout preferences.
    ''' </summary>
    Public Class FrmColumnCustomizer
        Inherits Form

        Private ReadOnly _dgv As DataGridView
        Private ReadOnly _layoutManager As GridLayoutManager

        Private pnlHeader As Panel
        Private lblTitle As Label
        Private btnCloseHeader As Button

        Private pnlContent As Panel
        Private clbColumns As CheckedListBox
        Private btnMoveUp As ModernButton
        Private btnMoveDown As ModernButton
        Private lblHelpText As Label

        Private pnlFooter As Panel
        Private btnApply As ModernButton
        Private btnReset As ModernButton

        Public Sub New(dgv As DataGridView, layoutManager As GridLayoutManager)
            _dgv = dgv
            _layoutManager = layoutManager
            InitializeComponent()
            PopulateColumnsList()
        End Sub

        Private Sub InitializeComponent()
            Me.SuspendLayout()

            Me.FormBorderStyle = FormBorderStyle.None
            Me.StartPosition = FormStartPosition.CenterParent
            Me.Size = New Size(440, 420)
            Me.BackColor = Color.White
            Me.ShowInTaskbar = False
            Me.TopMost = True
            Me.DoubleBuffered = True

            ' 1. Header Bar (#1E1B4B)
            pnlHeader = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 42,
                .BackColor = Color.FromArgb(30, 27, 75)
            }

            lblTitle = New Label() With {
                .Text = "⚙️ Table Column Layout Customizer",
                .Font = New Font("Segoe UI", 10.0!, FontStyle.Bold),
                .ForeColor = Color.White,
                .Location = New Point(14, 10),
                .AutoSize = True
            }

            btnCloseHeader = New Button() With {
                .Text = "✕",
                .Font = New Font("Segoe UI", 9.0!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(199, 210, 254),
                .BackColor = Color.Transparent,
                .FlatStyle = FlatStyle.Flat,
                .Size = New Size(30, 28),
                .Location = New Point(402, 7),
                .Cursor = Cursors.Hand
            }
            btnCloseHeader.FlatAppearance.BorderSize = 0
            btnCloseHeader.FlatAppearance.MouseOverBackColor = Color.FromArgb(239, 68, 68)
            AddHandler btnCloseHeader.Click, Sub(s, e) Me.Close()

            pnlHeader.Controls.Add(lblTitle)
            pnlHeader.Controls.Add(btnCloseHeader)

            ' 2. Footer Action Bar (#F8FAFC)
            pnlFooter = New Panel() With {
                .Dock = DockStyle.Bottom,
                .Height = 54,
                .BackColor = Color.FromArgb(248, 250, 252)
            }
            AddHandler pnlFooter.Paint, Sub(s, e)
                                            Using p As New Pen(Color.FromArgb(226, 232, 240), 1)
                                                e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0)
                                            End Using
                                        End Sub

            btnReset = New ModernButton() With {
                .Text = "Reset Default",
                .Scheme = ModernButton.ButtonScheme.Secondary,
                .Size = New Size(115, 34),
                .Location = New Point(14, 10)
            }
            AddHandler btnReset.Click, AddressOf btnReset_Click

            btnApply = New ModernButton() With {
                .Text = "Save & Apply Layout",
                .Scheme = ModernButton.ButtonScheme.Primary,
                .Size = New Size(150, 34),
                .Location = New Point(275, 10)
            }
            AddHandler btnApply.Click, AddressOf btnApply_Click

            pnlFooter.Controls.Add(btnReset)
            pnlFooter.Controls.Add(btnApply)

            ' 3. Main Content Panel
            pnlContent = New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = Color.White,
                .Padding = New Padding(14)
            }

            lblHelpText = New Label() With {
                .Text = "Check/uncheck columns to show or hide them. Use Move Up / Down to reorder columns in the grid table.",
                .Font = New Font("Segoe UI", 8.75!, FontStyle.Regular),
                .ForeColor = Color.FromArgb(71, 85, 105),
                .Location = New Point(14, 10),
                .Size = New Size(410, 32)
            }

            clbColumns = New CheckedListBox() With {
                .Font = New Font("Segoe UI", 9.0!, FontStyle.Bold),
                .ForeColor = Color.FromArgb(30, 41, 59),
                .Location = New Point(14, 46),
                .Size = New Size(300, 260),
                .BorderStyle = BorderStyle.FixedSingle,
                .CheckOnClick = True
            }

            btnMoveUp = New ModernButton() With {
                .Text = "▲ Move Up",
                .Scheme = ModernButton.ButtonScheme.Secondary,
                .Size = New Size(100, 34),
                .Location = New Point(324, 46)
            }
            AddHandler btnMoveUp.Click, AddressOf btnMoveUp_Click

            btnMoveDown = New ModernButton() With {
                .Text = "▼ Move Down",
                .Scheme = ModernButton.ButtonScheme.Secondary,
                .Size = New Size(100, 34),
                .Location = New Point(324, 90)
            }
            AddHandler btnMoveDown.Click, AddressOf btnMoveDown_Click

            pnlContent.Controls.Add(lblHelpText)
            pnlContent.Controls.Add(clbColumns)
            pnlContent.Controls.Add(btnMoveUp)
            pnlContent.Controls.Add(btnMoveDown)

            Me.Controls.Add(pnlContent)
            Me.Controls.Add(pnlFooter)
            Me.Controls.Add(pnlHeader)

            AddHandler Me.Paint, Sub(s, e)
                                     Using p As New Pen(Color.FromArgb(203, 213, 225), 1)
                                         e.Graphics.DrawRectangle(p, 0, 0, Me.Width - 1, Me.Height - 1)
                                     End Using
                                 End Sub

            Me.ResumeLayout(False)
        End Sub

        Private Class ColumnListItem
            Public Property Column As DataGridViewColumn
            Public Property HeaderText As String

            Public Overrides Function ToString() As String
                Return HeaderText
            End Function
        End Class

        Private Sub PopulateColumnsList()
            clbColumns.Items.Clear()

            ' Sort columns by current DisplayIndex
            Dim orderedCols = _dgv.Columns.Cast(Of DataGridViewColumn)().OrderBy(Function(c) c.DisplayIndex).ToList()

            For Each col In orderedCols
                Dim item As New ColumnListItem With {.Column = col, .HeaderText = col.HeaderText}
                clbColumns.Items.Add(item, col.Visible)
            Next

            If clbColumns.Items.Count > 0 Then
                clbColumns.SelectedIndex = 0
            End If
        End Sub

        Private Sub btnMoveUp_Click(sender As Object, e As EventArgs)
            Dim idx = clbColumns.SelectedIndex
            If idx <= 0 Then Return

            Dim item = clbColumns.Items(idx)
            Dim isChecked = clbColumns.GetItemChecked(idx)

            clbColumns.Items.RemoveAt(idx)
            clbColumns.Items.Insert(idx - 1, item)
            clbColumns.SetItemChecked(idx - 1, isChecked)
            clbColumns.SelectedIndex = idx - 1
        End Sub

        Private Sub btnMoveDown_Click(sender As Object, e As EventArgs)
            Dim idx = clbColumns.SelectedIndex
            If idx < 0 OrElse idx >= clbColumns.Items.Count - 1 Then Return

            Dim item = clbColumns.Items(idx)
            Dim isChecked = clbColumns.GetItemChecked(idx)

            clbColumns.Items.RemoveAt(idx)
            clbColumns.Items.Insert(idx + 1, item)
            clbColumns.SetItemChecked(idx + 1, isChecked)
            clbColumns.SelectedIndex = idx + 1
        End Sub

        Private Sub btnReset_Click(sender As Object, e As EventArgs)
            _layoutManager.ResetLayoutToDefault()
            PopulateColumnsList()
            MessageBox.Show("Table columns reset to default layout.", "Layout Reset", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Sub

        Private Sub btnApply_Click(sender As Object, e As EventArgs)
            ' Apply new visibility and DisplayIndex
            For i As Integer = 0 To clbColumns.Items.Count - 1
                Dim item = CType(clbColumns.Items(i), ColumnListItem)
                item.Column.Visible = clbColumns.GetItemChecked(i)
                item.Column.DisplayIndex = i
            Next

            _layoutManager.SaveLayout()
            Me.DialogResult = DialogResult.OK
            Me.Close()
        End Sub
    End Class
End Namespace
