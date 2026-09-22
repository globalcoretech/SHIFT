Imports System
Imports System.Windows.Forms
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Interfaces

Namespace Forms.Admin
    Public Class FrmTaskCategoryManagement
        Private ReadOnly _categoryService As ITaskCategoryService
        Private ReadOnly _currentUserId As Integer
        Private _editingCategoryId As Integer = 0

        Public Sub New(categoryService As ITaskCategoryService, currentUserId As Integer)
            InitializeComponent()
            _categoryService = categoryService
            _currentUserId = currentUserId
        End Sub

        Private Async Sub FrmTaskCategoryManagement_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            Await LoadCategoriesAsync()
        End Sub

        Private Async Function LoadCategoriesAsync() As Task
            Try
                Dim categories = Await _categoryService.GetAllCategoriesAsync(True)
                dgvCategories.DataSource = categories
                
                If dgvCategories.Columns.Contains("CategoryId") Then
                    dgvCategories.Columns("CategoryId").Visible = False
                End If
            Catch ex As Exception
                MessageBox.Show("Error loading categories: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Function

        Private Sub dgvCategories_SelectionChanged(sender As Object, e As EventArgs) Handles dgvCategories.SelectionChanged
            If dgvCategories.SelectedRows.Count > 0 Then
                Dim row = dgvCategories.SelectedRows(0)
                _editingCategoryId = Convert.ToInt32(row.Cells("CategoryId").Value)
                txtCategoryName.Text = Convert.ToString(row.Cells("CategoryName").Value)
                txtCategoryCode.Text = Convert.ToString(row.Cells("CategoryCode").Value)
                chkIsActive.Checked = Convert.ToBoolean(row.Cells("IsActive").Value)
                btnSave.Text = "Update"
            End If
        End Sub

        Private Sub btnClear_Click(sender As Object, e As EventArgs) Handles btnClear.Click
            ClearForm()
        End Sub

        Private Sub ClearForm()
            _editingCategoryId = 0
            txtCategoryName.Clear()
            txtCategoryCode.Clear()
            chkIsActive.Checked = True
            btnSave.Text = "Save"
            dgvCategories.ClearSelection()
        End Sub

        Private Async Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
            If String.IsNullOrWhiteSpace(txtCategoryName.Text) Then
                MessageBox.Show("Category Name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim dto As New TaskCategoryDto With {
                .CategoryId = _editingCategoryId,
                .CategoryName = txtCategoryName.Text.Trim(),
                .CategoryCode = txtCategoryCode.Text.Trim(),
                .IsActive = chkIsActive.Checked
            }

            Try
                If _editingCategoryId = 0 Then
                    Await _categoryService.AddCategoryAsync(dto, _currentUserId)
                    MessageBox.Show("Category added successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Else
                    Await _categoryService.UpdateCategoryAsync(dto, _currentUserId)
                    MessageBox.Show("Category updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
                
                ClearForm()
                Await LoadCategoriesAsync()
            Catch ex As Exception
                MessageBox.Show("Error saving category: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub
    End Class
End Namespace
