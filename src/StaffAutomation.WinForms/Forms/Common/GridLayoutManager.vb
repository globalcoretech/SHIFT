Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.IO
Imports System.Linq
Imports System.Text.Json
Imports System.Windows.Forms

Namespace Forms.Common
    ''' <summary>
    ''' DataGridView Layout and Persistence Manager.
    ''' Eliminates horizontal scrollbars via responsive Fill weight calculation,
    ''' enables user drag-and-drop column reordering and visibility customization,
    ''' and persists user layout preferences to local application storage.
    ''' </summary>
    Public Class GridLayoutManager
        Public Class ColumnConfig
            Public Property ColumnDataPropertyName As String
            Public Property HeaderText As String
            Public Property DisplayIndex As Integer
            Public Property Visible As Boolean
            Public Property FillWeight As Single
        End Class

        Private ReadOnly _dgv As DataGridView
        Private ReadOnly _gridId As String
        Private Shared ReadOnly ConfigDirectory As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StaffAutomation", "Layouts")

        Public Sub New(dgv As DataGridView, gridId As String)
            _dgv = dgv
            _gridId = gridId

            EnsureConfigDirectory()
            ConfigureGridDefaults()
        End Sub

        Private Sub ConfigureGridDefaults()
            ' Turn off Horizontal Scrollbar permanently & enable Fill auto-sizing
            _dgv.ScrollBars = ScrollBars.Vertical
            _dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            _dgv.AllowUserToOrderColumns = True
            _dgv.AllowUserToResizeColumns = True

            ' Hook ColumnDisplayIndexChanged to auto-save layout
            AddHandler _dgv.ColumnDisplayIndexChanged, AddressOf Dgv_ColumnDisplayIndexChanged
        End Sub

        Private Sub EnsureConfigDirectory()
            Try
                If Not Directory.Exists(ConfigDirectory) Then
                    Directory.CreateDirectory(ConfigDirectory)
                End If
            Catch
            End Try
        End Sub

        Private Function GetConfigFilePath() As String
            Return Path.Combine(ConfigDirectory, $"{_gridId}_columns.json")
        End Function

        ''' <summary>
        ''' Saves current DataGridView column display indexes, visibility, and weights to local JSON config.
        ''' </summary>
        Public Sub SaveLayout()
            Try
                Dim list As New List(Of ColumnConfig)()
                For Each col As DataGridViewColumn In _dgv.Columns
                    list.Add(New ColumnConfig With {
                        .ColumnDataPropertyName = If(String.IsNullOrEmpty(col.DataPropertyName), col.Name, col.DataPropertyName),
                        .HeaderText = col.HeaderText,
                        .DisplayIndex = col.DisplayIndex,
                        .Visible = col.Visible,
                        .FillWeight = col.FillWeight
                    })
                Next

                Dim jsonStr = JsonSerializer.Serialize(list, New JsonSerializerOptions With {.WriteIndented = True})
                File.WriteAllText(GetConfigFilePath(), jsonStr)
            Catch
            End Try
        End Sub

        ''' <summary>
        ''' Loads saved column layout config if exists and applies to DataGridView.
        ''' </summary>
        Public Sub LoadLayout()
            Try
                Dim filePath = GetConfigFilePath()
                If Not File.Exists(filePath) Then Return

                Dim jsonStr = File.ReadAllText(filePath)
                Dim list = JsonSerializer.Deserialize(Of List(Of ColumnConfig))(jsonStr)
                If list Is Nothing OrElse list.Count = 0 Then Return

                ' Sort by DisplayIndex to apply safely
                Dim sortedConfigs = list.OrderBy(Function(c) c.DisplayIndex).ToList()

                For Each cfg In sortedConfigs
                    Dim targetCol As DataGridViewColumn = Nothing
                    For Each col As DataGridViewColumn In _dgv.Columns
                        Dim propKey = If(String.IsNullOrEmpty(col.DataPropertyName), col.Name, col.DataPropertyName)
                        If String.Equals(propKey, cfg.ColumnDataPropertyName, StringComparison.OrdinalIgnoreCase) Then
                            targetCol = col
                            Exit For
                        End If
                    Next

                    If targetCol IsNot Nothing Then
                        targetCol.Visible = cfg.Visible
                        If cfg.FillWeight > 5 Then targetCol.FillWeight = cfg.FillWeight
                        If cfg.DisplayIndex >= 0 AndAlso cfg.DisplayIndex < _dgv.Columns.Count Then
                            targetCol.DisplayIndex = Math.Min(cfg.DisplayIndex, _dgv.Columns.Count - 1)
                        End If
                    End If
                Next
            Catch
            End Try
        End Sub

        ''' <summary>
        ''' Resets grid columns to default order and visibility.
        ''' </summary>
        Public Sub ResetLayoutToDefault()
            Try
                Dim filePath = GetConfigFilePath()
                If File.Exists(filePath) Then
                    File.Delete(filePath)
                End If

                Dim idx As Integer = 0
                For Each col As DataGridViewColumn In _dgv.Columns
                    col.Visible = True
                    col.DisplayIndex = idx
                    idx += 1
                Next
            Catch
            End Try
        End Sub

        Private Sub Dgv_ColumnDisplayIndexChanged(sender As Object, e As DataGridViewColumnEventArgs)
            SaveLayout()
        End Sub
    End Class
End Namespace
